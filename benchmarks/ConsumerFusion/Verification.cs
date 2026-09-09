using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pulse;

namespace Tl.ConsumerFusion;

public static class Verification
{
    private const uint Duration = 600;

    private enum Engine { Interpreter, Compiled, Fused }

    private readonly record struct Outcome(ConsumerReceipt Receipt, string? Error, string? Parameter);
    private readonly record struct StopOutcome(Playback Playback, string? Error, string? Parameter);
    private readonly record struct AuthoredClip(int Track, uint Start, uint End, float Amount);
    private readonly record struct InputBits(int Scale, int Bias, uint FailTick, ushort FailTrack)
    {
        public static InputBits Capture(in ConsumerInput input)
            => new(BitConverter.SingleToInt32Bits(input.Scale), BitConverter.SingleToInt32Bits(input.Bias), input.FailTick, input.FailTrack);
    }

    private static readonly AuthoredClip[] Clips =
    [
        new(0, 0, 7, 1), new(0, 29, 47, 13), new(0, 29, 47, 21), new(0, 47, 76, 13), new(0, 321, 515, 1),
        new(1, 3, 7, 2), new(1, 3, 11, 3), new(1, 18, 29, 8), new(1, 76, 123, 8), new(1, 515, 600, 3),
        new(2, 47, 76, 2), new(2, 123, 321, 2), new(2, 321, 515, 8),
        new(3, 3, 11, 5), new(3, 18, 29, 5), new(3, 76, 123, 34), new(3, 76, 123, 55), new(3, 200, 321, 5), new(3, 321, 515, 5)
    ];

    private static readonly ConsumerInput[] Inputs =
    [
        new(1.25f, 0.125f),
        new(-0.75f, -3f),
        new(0.5f, 5f),
        new(2f, BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)))
    ];

    private static readonly float[] FloatSeeds =
    [
        0f,
        BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)),
        0.1f,
        -12345.75f,
        float.PositiveInfinity,
        float.NegativeInfinity,
        BitConverter.Int32BitsToSingle(0x7fc00001),
        BitConverter.Int32BitsToSingle(unchecked((int)0xffc12345))
    ];

    public static void Run()
    {
        Console.WriteLine($"Generated source SHA-256: {BuildIdentity.SourceSha256}");
        Console.WriteLine($"Consumer source SHA-256: {BuildIdentity.ConsumerSha256}");
        RequirePlaybackAbi();

        var id = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<ConsumerInput, SumConsumer>(id);
        Timeline<PulseTrack, PulseClip>.Bind<ConsumerInput, StateConsumer>(id);
        Timeline<PulseTrack, PulseClip>.Bind<ConsumerInput, EffectConsumer>(id);

        long comparisons = 0;
        try
        {
            var edges = BuildEdges();
            CheckStartStop(id, edges, ref comparisons);
            CheckScalarMatrix(id, edges, ref comparisons);
            CheckAuthoredOracle(id, edges.Where(static tick => tick <= 1200).ToArray(), ref comparisons);
            CheckLifecycle(id, ref comparisons);
            CheckComposition(id, ref comparisons);
            CheckVaryingInputs(id, ref comparisons);
            CheckConsumerFailure(id, ref comparisons);
            CheckOverflowAfterEffects(id, ref comparisons);

            Console.WriteLine($"{comparisons:N0} exact comparisons passed for all consumers: both directions, authored edges and movement states, full Playback, all result fields, exact float bits, readonly inputs, scalar/span composition, wraps, saturation, lifecycle, partial failures, and overflow ordering.");
            Console.WriteLine($"runtime: {(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT")}");
        }
        finally
        {
            Timeline.Destroy(id);
        }
    }

    private static void CheckStartStop(ushort id, uint[] edges, ref long comparisons)
    {
        foreach (var tick in edges)
        {
            var expected = Timeline.Start(id, tick);
            RequireEqual("compiled Start", CompiledPulse.Start(tick), expected, ref comparisons);
            RequireEqual("fused Start", FusedPulse.Start(tick), expected, ref comparisons);

            var expectedStop = Stop(Engine.Interpreter, id, in expected);
            foreach (var engine in new[] { Engine.Compiled, Engine.Fused })
            {
                var actualStop = Stop(engine, id, in expected);
                RequireEqual($"{engine} Stop", actualStop, expectedStop, ref comparisons);
                var once = actualStop.Playback;
                var twice = Stop(engine, id, in once);
                RequireEqual($"{engine} repeated Stop", twice, actualStop, ref comparisons);
            }
        }

        var invalid = default(Playback);
        var expectedInvalid = Stop(Engine.Interpreter, id, in invalid);
        RequireEqual("compiled unstarted Stop", Stop(Engine.Compiled, id, in invalid), expectedInvalid, ref comparisons);
        RequireEqual("fused unstarted Stop", Stop(Engine.Fused, id, in invalid), expectedInvalid, ref comparisons);
    }

    private static void CheckScalarMatrix(ushort id, uint[] edges, ref long comparisons)
    {
        for (var p = 0; p < edges.Length; p++)
        for (var n = 0; n < edges.Length; n++)
        foreach (var backward in new[] { false, true })
        for (var s = 0; s < FloatSeeds.Length; s++)
        {
            var from = Timeline.Start(id, edges[p]);
            var input = Inputs[(p * 17 + n * 7 + s + (backward ? 1 : 0)) & 3];
            uint[] ticks = [edges[n]];
            Check(id, in from, in input, SumSeed(FloatSeeds[s]), ticks, backward, batch: false, "scalar edge matrix", ref comparisons);
            Check(id, in from, in input, StateSeed(FloatSeeds[s]), ticks, backward, batch: false, "scalar edge matrix", ref comparisons);
            Check(id, in from, in input, EffectSeed(FloatSeeds[s]), ticks, backward, batch: false, "scalar edge matrix", ref comparisons);
        }
    }

    private static void CheckAuthoredOracle(ushort id, uint[] points, ref long comparisons)
    {
        for (var p = 0; p < points.Length; p++)
        for (var n = 0; n < points.Length; n++)
        foreach (var backward in new[] { false, true })
        {
            var cycles = (ushort)(backward ? (p + n) % 3 : (p * 3 + n) % 7);
            var from = Mint(points[p], cycles, PlaybackFlags.Started | (PlaybackFlags)0x4000);
            var input = Inputs[(p * 5 + n * 3 + (backward ? 1 : 0)) & 3];
            var seed = FloatSeeds[(p + n) % FloatSeeds.Length];
            uint[] ticks = [points[n]];

            CompareWithOracle(id, in from, in input, SumSeed(seed), OracleSum(in from, in input, SumSeed(seed), points[n], backward), ticks, backward, ref comparisons);
            CompareWithOracle(id, in from, in input, StateSeed(seed), OracleState(in from, in input, StateSeed(seed), points[n], backward), ticks, backward, ref comparisons);
            CompareWithOracle(id, in from, in input, EffectSeed(seed), OracleEffect(in from, in input, EffectSeed(seed), points[n], backward), ticks, backward, ref comparisons);
        }
    }

    private static void CheckLifecycle(ushort id, ref long comparisons)
    {
        var started = Timeline.Start(id, 10);
        var stopped = Timeline.Stop(id, in started);
        var unknownRunnable = Mint(3, 7, PlaybackFlags.Started | PlaybackFlags.Completed | (PlaybackFlags)0x8000);
        var unknownStopped = Mint(3, 7, PlaybackFlags.Started | PlaybackFlags.Stopped | (PlaybackFlags)0x8000);
        var input = Inputs[1];

        foreach (var backward in new[] { false, true })
        {
            foreach (var from in new[] { default(Playback), stopped, unknownStopped })
            {
                CheckAllConsumers(id, in from, in input, [], backward, batch: true, "invalid empty span", ref comparisons);
                CheckAllConsumers(id, in from, in input, [3], backward, batch: false, "invalid scalar", ref comparisons);
                CheckAllConsumers(id, in from, in input, [3, 599], backward, batch: true, "invalid span", ref comparisons);
            }

            CheckAllConsumers(id, in unknownRunnable, in input, [], backward, batch: true, "runnable empty span", ref comparisons);
            CheckAllConsumers(id, in started, in input, [12], backward, batch: false, "authored empty region", ref comparisons);
            CheckAllConsumers(id, in unknownRunnable, in input, [600, uint.MaxValue], backward, batch: true, "unknown flags", ref comparisons);
        }
    }

    private static void CheckComposition(ushort id, ref long comparisons)
    {
        var sequential = Enumerable.Range(0, 1800).Select(static value => (uint)value).ToArray();
        var random = RandomTicks(2048);
        uint[][] scripts =
        [
            sequential,
            sequential.Reverse().ToArray(),
            random,
            Enumerable.Repeat(321u, 257).ToArray(),
            [599, 600, 1200, 3, 1803, 2399]
        ];

        foreach (var backward in new[] { false, true })
        for (var i = 0; i < scripts.Length; i++)
        {
            var from = Timeline.Start(id, i == 4 ? 599u : 0u);
            var input = Inputs[i & 3];
            CheckComposition(id, in from, in input, StateSeed(0.1f), scripts[i], backward, ref comparisons);
            CheckComposition(id, in from, in input, EffectSeed(-0f), scripts[i], backward, ref comparisons);
            CheckComposition(id, in from, in input, SumSeed(float.NegativeInfinity), scripts[i], backward, ref comparisons);
        }
    }

    private static void CheckVaryingInputs(ushort id, ref long comparisons)
    {
        var ticks = RandomTicks(4096);
        foreach (var backward in new[] { false, true })
        {
            var from = Timeline.Start(id, backward ? 1800u : 0u);
            CheckVarying(id, in from, SumSeed(-0f), ticks, backward, ref comparisons);
            CheckVarying(id, in from, StateSeed(0.1f), ticks, backward, ref comparisons);
            CheckVarying(id, in from, EffectSeed(float.PositiveInfinity), ticks, backward, ref comparisons);
        }
    }

    private static void CheckConsumerFailure(ushort id, ref long comparisons)
    {
        uint[] ticks = [0, 3];
        foreach (var backward in new[] { false, true })
        {
            var from = Timeline.Start(id);
            var input = new ConsumerInput(1.25f, 0.125f, FailTick: 3, FailTrack: 1);
            var seed = EffectSeed(-0f);
            var expected = OracleEffectBatch(in from, in input, seed, ticks, backward);
            if (expected.Error != typeof(ConsumerFailureException).FullName
                || expected.Parameter is not null
                || expected.Receipt.Playback != from
                || expected.Receipt.Callbacks != seed.Callbacks + 3)
                throw new InvalidOperationException($"The independent failure fixture is malformed: {expected}.");

            foreach (var engine in Enum.GetValues<Engine>())
            {
                var actual = Execute(engine, id, in from, in input, seed, ticks, backward, batch: true);
                RequireEqual($"{engine} consumer failure at second work of second tick", actual, expected, ref comparisons);
            }
        }
    }

    private static void CheckOverflowAfterEffects(ushort id, ref long comparisons)
    {
        var from = Mint(0, ushort.MaxValue, PlaybackFlags.Started);
        var input = Inputs[0];
        uint[] successfulTick = [3];
        uint[] batch = [3, 600];

        foreach (var consumer in new[] { 0, 1, 2 })
        foreach (var engine in Enum.GetValues<Engine>())
        {
            Outcome successful;
            Outcome failed;
            switch (consumer)
            {
                case 0:
                {
                    var seed = SumSeed(0.1f);
                    successful = Execute(engine, id, in from, in input, seed, successfulTick, backward: false, batch: false);
                    failed = Execute(engine, id, in from, in input, seed, batch, backward: false, batch: true);
                    break;
                }
                case 1:
                {
                    var seed = StateSeed(-0f);
                    successful = Execute(engine, id, in from, in input, seed, successfulTick, backward: false, batch: false);
                    failed = Execute(engine, id, in from, in input, seed, batch, backward: false, batch: true);
                    break;
                }
                default:
                {
                    var seed = EffectSeed(float.NegativeInfinity);
                    successful = Execute(engine, id, in from, in input, seed, successfulTick, backward: false, batch: false);
                    failed = Execute(engine, id, in from, in input, seed, batch, backward: false, batch: true);
                    break;
                }
            }

            if (failed.Error != typeof(ArgumentOutOfRangeException).FullName
                || failed.Parameter != "ticks"
                || failed.Receipt.Playback != from
                || successful.Error is not null
                || !SameResultFields(failed.Receipt, successful.Receipt))
                throw new InvalidOperationException($"{engine} did not preserve prior effects or caller Playback when the second batch tick overflowed: {failed} / {successful}.");
            comparisons++;
        }

        Check(id, in from, in input, SumSeed(0.1f), batch, backward: false, batch: true, "overflow after effects", ref comparisons);
        Check(id, in from, in input, StateSeed(-0f), batch, backward: false, batch: true, "overflow after effects", ref comparisons);
        Check(id, in from, in input, EffectSeed(float.NegativeInfinity), batch, backward: false, batch: true, "overflow after effects", ref comparisons);

        var saturation = Mint(1200, 1, PlaybackFlags.Started);
        uint[] backwardTicks = [600, 0, 599];
        CheckAllConsumers(id, in saturation, in input, backwardTicks, backward: true, batch: true, "backward cycle saturation", ref comparisons);
    }

    private static void CheckAllConsumers(
        ushort id, in Playback from, in ConsumerInput input, uint[] ticks,
        bool backward, bool batch, string context, ref long comparisons)
    {
        Check(id, in from, in input, SumSeed(-0f), ticks, backward, batch, context, ref comparisons);
        Check(id, in from, in input, StateSeed(0.1f), ticks, backward, batch, context, ref comparisons);
        Check(id, in from, in input, EffectSeed(float.PositiveInfinity), ticks, backward, batch, context, ref comparisons);
    }

    private static void Check<TConsumer>(
        ushort id, in Playback from, in ConsumerInput input, TConsumer seed, uint[] ticks,
        bool backward, bool batch, string context, ref long comparisons)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        var expected = Execute(Engine.Interpreter, id, in from, in input, seed, ticks, backward, batch);
        foreach (var engine in new[] { Engine.Compiled, Engine.Fused })
        {
            var actual = Execute(engine, id, in from, in input, seed, ticks, backward, batch);
            RequireEqual($"{context}: {typeof(TConsumer).Name}/{engine}, from={from}, ticks={FormatTicks(ticks)}, backward={backward}, batch={batch}", actual, expected, ref comparisons);
        }
    }

    private static void CompareWithOracle<TConsumer>(
        ushort id, in Playback from, in ConsumerInput input, TConsumer seed,
        ConsumerReceipt oracle, uint[] ticks, bool backward, ref long comparisons)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        var expected = new Outcome(oracle, null, null);
        foreach (var engine in Enum.GetValues<Engine>())
        {
            var actual = Execute(engine, id, in from, in input, seed, ticks, backward, batch: false);
            RequireEqual($"authored oracle: {typeof(TConsumer).Name}/{engine}, from={from}, tick={ticks[0]}, backward={backward}", actual, expected, ref comparisons);
        }
    }

    private static void CheckComposition<TConsumer>(
        ushort id, in Playback from, in ConsumerInput input, TConsumer seed,
        uint[] ticks, bool backward, ref long comparisons)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        Check(id, in from, in input, seed, ticks, backward, batch: true, "successful span", ref comparisons);
        foreach (var engine in Enum.GetValues<Engine>())
        {
            var scalar = Execute(engine, id, in from, in input, seed, ticks, backward, batch: false);
            var span = Execute(engine, id, in from, in input, seed, ticks, backward, batch: true);
            if (scalar.Error is not null || span.Error is not null)
                throw new InvalidOperationException($"Successful composition fixture failed for {typeof(TConsumer).Name}/{engine}: {scalar} / {span}.");
            RequireEqual($"{typeof(TConsumer).Name}/{engine} scalar-span composition", span, scalar, ref comparisons);
        }
    }

    private static void CheckVarying<TConsumer>(
        ushort id, in Playback from, TConsumer seed, uint[] ticks,
        bool backward, ref long comparisons)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        var expected = ExecuteVarying(Engine.Interpreter, id, in from, seed, ticks, backward, chunked: false);
        foreach (var engine in new[] { Engine.Compiled, Engine.Fused })
            RequireEqual($"{typeof(TConsumer).Name}/{engine} varying scalar inputs", ExecuteVarying(engine, id, in from, seed, ticks, backward, chunked: false), expected, ref comparisons);

        foreach (var engine in Enum.GetValues<Engine>())
        {
            var scalar = ExecuteVarying(engine, id, in from, seed, ticks, backward, chunked: false);
            var spans = ExecuteVarying(engine, id, in from, seed, ticks, backward, chunked: true);
            RequireEqual($"{typeof(TConsumer).Name}/{engine} varying-input eight-tick spans", spans, scalar, ref comparisons);
        }
    }

    private static Outcome Execute<TConsumer>(
        Engine engine, ushort id, in Playback from, in ConsumerInput suppliedInput,
        TConsumer seed, uint[] ticks, bool backward, bool batch)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        var input = suppliedInput;
        var originalInput = InputBits.Capture(in input);
        var result = seed;
        var playback = from;
        string? error = null;
        string? parameter = null;
        try
        {
            if (batch)
            {
                playback = engine switch
                {
                    Engine.Interpreter => backward
                        ? Timeline.Backward(id, in playback, in input, ref result, ticks)
                        : Timeline.Forward(id, in playback, in input, ref result, ticks),
                    Engine.Compiled => backward
                        ? CompiledPulse.Backward(in playback, in input, ref result, ticks)
                        : CompiledPulse.Forward(in playback, in input, ref result, ticks),
                    Engine.Fused => backward
                        ? FusedPulse.Backward(in playback, in input, ref result, ticks)
                        : FusedPulse.Forward(in playback, in input, ref result, ticks),
                    _ => throw new ArgumentOutOfRangeException(nameof(engine))
                };
            }
            else
            {
                foreach (var tick in ticks)
                    playback = engine switch
                    {
                        Engine.Interpreter => backward
                            ? Timeline.Backward(id, in playback, in input, ref result, tick)
                            : Timeline.Forward(id, in playback, in input, ref result, tick),
                        Engine.Compiled => backward
                            ? CompiledPulse.Backward(in playback, in input, ref result, tick)
                            : CompiledPulse.Forward(in playback, in input, ref result, tick),
                        Engine.Fused => backward
                            ? FusedPulse.Backward(in playback, in input, ref result, tick)
                            : FusedPulse.Forward(in playback, in input, ref result, tick),
                        _ => throw new ArgumentOutOfRangeException(nameof(engine))
                    };
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ConsumerFailureException)
        {
            error = exception.GetType().FullName;
            parameter = (exception as ArgumentException)?.ParamName;
        }

        if (InputBits.Capture(in input) != originalInput)
            throw new InvalidOperationException($"{engine}/{typeof(TConsumer).Name} mutated its readonly input.");
        return new Outcome(result.Capture(in playback), error, parameter);
    }

    private static Outcome ExecuteVarying<TConsumer>(
        Engine engine, ushort id, in Playback from, TConsumer seed,
        uint[] ticks, bool backward, bool chunked)
        where TConsumer : unmanaged, IConsumer<TConsumer>
    {
        var result = seed;
        var playback = from;
        string? error = null;
        string? parameter = null;
        try
        {
            var width = chunked ? 8 : 1;
            for (var i = 0; i < ticks.Length; i += width)
            {
                var input = Inputs[(i >> 3) & 3];
                var originalInput = InputBits.Capture(in input);
                if (chunked)
                {
                    var slice = ticks.AsSpan(i, Math.Min(width, ticks.Length - i));
                    playback = engine switch
                    {
                        Engine.Interpreter => backward
                            ? Timeline.Backward(id, in playback, in input, ref result, slice)
                            : Timeline.Forward(id, in playback, in input, ref result, slice),
                        Engine.Compiled => backward
                            ? CompiledPulse.Backward(in playback, in input, ref result, slice)
                            : CompiledPulse.Forward(in playback, in input, ref result, slice),
                        Engine.Fused => backward
                            ? FusedPulse.Backward(in playback, in input, ref result, slice)
                            : FusedPulse.Forward(in playback, in input, ref result, slice),
                        _ => throw new ArgumentOutOfRangeException(nameof(engine))
                    };
                }
                else
                {
                    playback = engine switch
                    {
                        Engine.Interpreter => backward
                            ? Timeline.Backward(id, in playback, in input, ref result, ticks[i])
                            : Timeline.Forward(id, in playback, in input, ref result, ticks[i]),
                        Engine.Compiled => backward
                            ? CompiledPulse.Backward(in playback, in input, ref result, ticks[i])
                            : CompiledPulse.Forward(in playback, in input, ref result, ticks[i]),
                        Engine.Fused => backward
                            ? FusedPulse.Backward(in playback, in input, ref result, ticks[i])
                            : FusedPulse.Forward(in playback, in input, ref result, ticks[i]),
                        _ => throw new ArgumentOutOfRangeException(nameof(engine))
                    };
                }
                if (InputBits.Capture(in input) != originalInput)
                    throw new InvalidOperationException($"{engine}/{typeof(TConsumer).Name} mutated a varying readonly input.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ConsumerFailureException)
        {
            error = exception.GetType().FullName;
            parameter = (exception as ArgumentException)?.ParamName;
        }

        return new Outcome(result.Capture(in playback), error, parameter);
    }

    private static StopOutcome Stop(Engine engine, ushort id, in Playback from)
    {
        try
        {
            var playback = engine switch
            {
                Engine.Interpreter => Timeline.Stop(id, in from),
                Engine.Compiled => CompiledPulse.Stop(in from),
                Engine.Fused => FusedPulse.Stop(in from),
                _ => throw new ArgumentOutOfRangeException(nameof(engine))
            };
            return new StopOutcome(playback, null, null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return new StopOutcome(from, exception.GetType().FullName, (exception as ArgumentException)?.ParamName);
        }
    }

    private static ConsumerReceipt OracleSum(
        in Playback from, in ConsumerInput input, SumConsumer result, uint tick, bool backward)
    {
        var position = Position(from.Tick, tick, backward);
        for (var track = 0; track < 4; track++)
        {
            if (!TryWork(track, position.TEff, out var amount, out _, out _))
                continue;
            result.Sum = backward ? result.Sum - amount : result.Sum + amount;
        }
        var playback = OraclePlayback(in from, tick, position, backward);
        return result.Capture(in playback);
    }

    private static ConsumerReceipt OracleState(
        in Playback from, in ConsumerInput input, StateConsumer result, uint tick, bool backward)
    {
        var position = Position(from.Tick, tick, backward);
        for (var track = 0; track < 4; track++)
        {
            if (!TryWork(track, position.TEff, out var amount, out var enterF, out var enterB))
                continue;
            var state = OracleClipState(position, enterF, enterB, backward);
            ApplyOracleState(ref result, track + 1, amount, state, in input, backward);
        }
        var playback = OraclePlayback(in from, tick, position, backward);
        return result.Capture(in playback);
    }

    private static ConsumerReceipt OracleEffect(
        in Playback from, in ConsumerInput input, EffectConsumer result, uint tick, bool backward)
    {
        var position = Position(from.Tick, tick, backward);
        for (var track = 0; track < 4; track++)
        {
            if (!TryWork(track, position.TEff, out var amount, out var enterF, out var enterB))
                continue;
            var state = OracleClipState(position, enterF, enterB, backward);
            ApplyOracleEffect(ref result, (ushort)track, track + 1, amount, state, position.TEff, in input, backward);
        }
        var playback = OraclePlayback(in from, tick, position, backward);
        return result.Capture(in playback);
    }

    private static Outcome OracleEffectBatch(
        in Playback from, in ConsumerInput input, EffectConsumer result, uint[] ticks, bool backward)
    {
        var state = from;
        foreach (var tick in ticks)
        {
            var position = Position(state.Tick, tick, backward);
            if (!backward && position.Cycles > ushort.MaxValue - state.Cycles)
                return new Outcome(result.Capture(in from), typeof(ArgumentOutOfRangeException).FullName, "ticks");
            var next = OraclePlayback(in state, tick, position, backward);
            for (var track = 0; track < 4; track++)
            {
                if (!TryWork(track, position.TEff, out var amount, out var enterF, out var enterB))
                    continue;
                var clipState = OracleClipState(position, enterF, enterB, backward);
                ApplyOracleEffect(ref result, (ushort)track, track + 1, amount, clipState, position.TEff, in input, backward);
                if (position.TEff == input.FailTick && track == input.FailTrack)
                    return new Outcome(result.Capture(in from), typeof(ConsumerFailureException).FullName, null);
            }
            state = next;
        }
        return new Outcome(result.Capture(in state), null, null);
    }

    private static void ApplyOracleState(
        ref StateConsumer result, int offset, float amount, ClipState state,
        in ConsumerInput input, bool backward)
    {
        result.TrackOffsets += backward ? -offset : offset;
        switch (state)
        {
            case ClipState.Enter:
                result.Enters++;
                break;
            case ClipState.Stay:
                result.Stays++;
                var contribution = amount * input.Scale + input.Bias;
                result.Sum = backward ? result.Sum - contribution : result.Sum + contribution;
                break;
            case ClipState.Exit:
                result.Exits++;
                break;
            default:
                throw new InvalidOperationException($"Unknown oracle clip state {state}.");
        }
    }

    private static void ApplyOracleEffect(
        ref EffectConsumer result, ushort index, int offset, float amount,
        ClipState state, uint tick, in ConsumerInput input, bool backward)
    {
        ApplyOracleState(ref result.State, offset, amount, state, in input, backward);
        result.Callbacks++;
        var value = ((ulong)tick << 32) ^ ((ulong)index << 16) ^ (uint)BitConverter.SingleToInt32Bits(amount) ^ (uint)state;
        result.Audit = unchecked((result.Audit ^ value) * 1099511628211UL);
    }

    private static bool TryWork(int track, uint tick, out float amount, out uint enterF, out uint enterB)
    {
        AuthoredClip first = default;
        AuthoredClip second = default;
        var count = 0;
        foreach (var clip in Clips)
        {
            if (clip.Track != track || tick < clip.Start || tick >= clip.End)
                continue;
            if (count == 0)
                first = clip;
            else if (count == 1)
                second = clip;
            count++;
        }

        if (count == 0)
        {
            amount = 0;
            enterF = 0;
            enterB = 0;
            return false;
        }
        if (count > 2)
            throw new InvalidOperationException($"Authored oracle found {count} simultaneous clips on track {track} at tick {tick}.");

        if (count == 1)
        {
            amount = first.Amount;
            enterF = first.Start;
            enterB = first.End;
            return true;
        }

        var factorStart = Math.Max(first.Start, second.Start);
        var factorLength = Math.Min(first.End, second.End) - factorStart;
        var factor = factorLength <= 1 ? 0.5f : (tick - factorStart) / (float)(factorLength - 1);
        amount = first.Amount + (second.Amount - first.Amount) * factor;
        enterF = Math.Min(first.Start, second.Start);
        enterB = Math.Max(first.End, second.End);
        return true;
    }

    private static ClipState OracleClipState(
        (uint TEff, uint PrevEff, uint Cycles, bool Wrapped, bool Full) position,
        uint enterF, uint enterB, bool backward)
    {
        if (backward ? position.TEff == enterF : position.TEff == enterB - 1)
            return ClipState.Exit;
        if (position.Wrapped || position.Full
            || (backward ? position.PrevEff >= enterB : position.PrevEff < enterF))
            return ClipState.Enter;
        return ClipState.Stay;
    }

    private static (uint TEff, uint PrevEff, uint Cycles, bool Wrapped, bool Full) Position(
        uint previous, uint tick, bool backward)
    {
        var prevEff = previous % Duration;
        var tEff = tick % Duration;
        if (!backward)
        {
            var cycles = tick >= previous
                ? tick / Duration - previous / Duration
                : tEff < prevEff ? 1u : 0u;
            return (tEff, prevEff, cycles, cycles == 1, cycles >= 2);
        }

        if (tick <= previous)
        {
            var cycles = previous / Duration - tick / Duration;
            return (tEff, prevEff, cycles, cycles == 1, cycles >= 2);
        }
        return tEff > prevEff
            ? (tEff, prevEff, 1u, true, false)
            : (tEff, prevEff, 0u, false, false);
    }

    private static Playback OraclePlayback(
        in Playback from, uint tick,
        (uint TEff, uint PrevEff, uint Cycles, bool Wrapped, bool Full) position,
        bool backward)
    {
        var cycles = backward
            ? (ushort)(from.Cycles - Math.Min((uint)from.Cycles, position.Cycles))
            : checked((ushort)(from.Cycles + position.Cycles));
        var flags = PlaybackFlags.Started;
        if (position.TEff == Duration - 1)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(tick, cycles, flags);
    }

    private static SumConsumer SumSeed(float sum) => new() { Sum = sum };

    private static StateConsumer StateSeed(float sum) => new()
    {
        Sum = sum,
        TrackOffsets = -1_234_567_890_123,
        Enters = -31,
        Stays = 37,
        Exits = -41
    };

    private static EffectConsumer EffectSeed(float sum) => new()
    {
        State = StateSeed(sum),
        Callbacks = -43,
        Audit = 0xD6E8FEB86659FD93UL
    };

    private static uint[] BuildEdges()
    {
        var values = new HashSet<uint>
        {
            0, 1, 2, 3, 599, 600, 601, 1199, 1200, 1201,
            65535, uint.MaxValue - 1, uint.MaxValue
        };
        foreach (var clip in Clips)
        {
            AddEdge(values, clip.Start);
            AddEdge(values, clip.End);
        }
        return values.Order().ToArray();
    }

    private static void AddEdge(HashSet<uint> values, uint edge)
    {
        values.Add(edge);
        if (edge > 0)
            values.Add(edge - 1);
        if (edge < uint.MaxValue)
            values.Add(edge + 1);
    }

    private static uint[] RandomTicks(int count)
    {
        var ticks = new uint[count];
        uint state = 0xA312AFD5;
        for (var i = 0; i < ticks.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            ticks[i] = state % 2400;
        }
        return ticks;
    }

    private static bool SameResultFields(in ConsumerReceipt left, in ConsumerReceipt right)
        => left.SumBits == right.SumBits
            && left.TrackOffsets == right.TrackOffsets
            && left.Enters == right.Enters
            && left.Stays == right.Stays
            && left.Exits == right.Exits
            && left.Callbacks == right.Callbacks
            && left.Audit == right.Audit;

    private static string FormatTicks(uint[] ticks)
        => ticks.Length <= 8 ? string.Join(',', ticks) : string.Join(',', ticks.AsSpan(0, 8).ToArray()) + ",...";

    private static void RequireEqual<T>(string context, T actual, T expected, ref long comparisons)
        where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException($"{context}: {actual} != {expected}.");
        comparisons++;
    }

    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);

    private static void RequirePlaybackAbi()
    {
        if (!BitConverter.IsLittleEndian
            || Unsafe.SizeOf<Playback>() != 8
            || Marshal.OffsetOf<Playback>(nameof(Playback.Tick)) != 0
            || Marshal.OffsetOf<Playback>(nameof(Playback.Cycles)) != 4
            || Marshal.OffsetOf<Playback>(nameof(Playback.Flags)) != 6)
            throw new InvalidOperationException("The verification Playback mint requires the current little-endian 8-byte ABI.");
    }
}
