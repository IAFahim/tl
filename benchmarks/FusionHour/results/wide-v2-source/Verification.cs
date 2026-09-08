using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pulse;
using Tl;

namespace Tl.FusionExperiment;

public static class Verification
{
    private enum Engine { Interpreter, Compiled, Fused }
    private readonly record struct Outcome(SumReceipt Receipt, string? Error, string? Parameter);
    private readonly record struct AuthoredClip(int Track, uint Start, uint End, float Amount);

    private static readonly AuthoredClip[] Clips =
    [
        new(0, 0, 7, 1), new(0, 29, 47, 13), new(0, 29, 47, 21), new(0, 47, 76, 13), new(0, 321, 515, 1),
        new(1, 3, 7, 2), new(1, 3, 11, 3), new(1, 18, 29, 8), new(1, 76, 123, 8), new(1, 515, 600, 3),
        new(2, 47, 76, 2), new(2, 123, 321, 2), new(2, 321, 515, 8),
        new(3, 3, 11, 5), new(3, 18, 29, 5), new(3, 76, 123, 34), new(3, 76, 123, 55), new(3, 200, 321, 5), new(3, 321, 515, 5)
    ];

    public static void Run()
    {
        if (!BitConverter.IsLittleEndian || Unsafe.SizeOf<Playback>() != 8
            || Marshal.OffsetOf<Playback>(nameof(Playback.Tick)) != 0
            || Marshal.OffsetOf<Playback>(nameof(Playback.Cycles)) != 4
            || Marshal.OffsetOf<Playback>(nameof(Playback.Flags)) != 6)
            throw new InvalidOperationException("The experimental Playback mint requires the current little-endian 8-byte ABI.");

        var id = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, SumConsumer>(id);
        long comparisons = 0;
        try
        {
            uint[] edges = [0, 1, 2, 3, 6, 7, 10, 11, 12, 17, 18, 28, 29, 46, 47, 75, 76, 122, 123, 199, 200, 320, 321, 514, 515, 598, 599, 600, 601, 1199, 1200, 65535, uint.MaxValue - 1, uint.MaxValue];
            float[] seeds = [0f, BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)), 0.1f, -12345.75f, float.PositiveInfinity, float.NegativeInfinity, BitConverter.Int32BitsToSingle(0x7fc00001)];
            foreach (var previous in edges)
                foreach (var next in edges)
                    foreach (var backward in new[] { false, true })
                        foreach (var seed in seeds)
                            Check(id, Timeline.Start(id, previous), seed, [next], backward, false, ref comparisons);

            foreach (var backward in new[] { false, true })
            {
                foreach (var from in new[] { default(Playback), Timeline.Stop(id, Timeline.Start(id)), Mint(0, 65535, PlaybackFlags.Started), Mint(1200, 1, PlaybackFlags.Started), Mint(3, 1, PlaybackFlags.Started | PlaybackFlags.Completed | (PlaybackFlags)32768) })
                {
                    Check(id, from, -0f, [], backward, true, ref comparisons);
                    Check(id, from, 0.1f, [3, 599, 600, uint.MaxValue], backward, true, ref comparisons);
                    Check(id, from, 0.1f, [600], backward, false, ref comparisons);
                }

                var sequence = Enumerable.Range(0, 65536).Select(static i => (uint)i).ToArray();
                var random = new uint[65536];
                uint state = 0xA312AFD5;
                for (var i = 0; i < random.Length; i++)
                {
                    state ^= state << 13;
                    state ^= state >> 17;
                    state ^= state << 5;
                    random[i] = state % 2400u;
                }
                foreach (var ticks in new[] { sequence, sequence.Reverse().ToArray(), random, Enumerable.Repeat(321u, 65536).ToArray() })
                {
                    Check(id, Timeline.Start(id), 0.1f, ticks, backward, true, ref comparisons);
                    foreach (var engine in Enum.GetValues<Engine>())
                    {
                        var single = Run(engine, id, Timeline.Start(id), 0.1f, ticks, backward, false);
                        var batch = Run(engine, id, Timeline.Start(id), 0.1f, ticks, backward, true);
                        if (single != batch)
                            throw new InvalidOperationException($"{engine}: batch composition differs: {single} / {batch}");
                        comparisons++;
                    }
                }
            }

            for (uint tick = 0; tick < 600; tick++)
                foreach (var seed in seeds)
                    foreach (var backward in new[] { false, true })
                    {
                        var actual = Run(Engine.Fused, id, FusedPulse.Start(tick), seed, [tick], backward, false);
                        var expected = BitConverter.SingleToInt32Bits(SampleOracle(tick, seed, backward));
                        if (actual.Error is not null || actual.Receipt.SumBits != expected)
                            throw new InvalidOperationException($"Independent sample oracle failed at {tick}, backward={backward}: {actual} / {expected}");
                        comparisons++;
                    }

            var start = FusedPulse.Start(599);
            if (start != Timeline.Start(id, 599) || FusedPulse.Stop(in start) != Timeline.Stop(id, in start))
                throw new InvalidOperationException("Start/Stop parity failed.");
            var stopped = FusedPulse.Stop(in start);
            if (FusedPulse.Stop(in stopped) != stopped)
                throw new InvalidOperationException("Stop is not idempotent.");
            RequireUnstartedStopRejects();
            Console.WriteLine($"{comparisons:N0} exact comparisons passed: both directions, full Playback, float bits, boundaries, gaps, wraps, overflow before effects, lifecycle, batch composition, independent sample oracle.");
            Console.WriteLine($"runtime: {(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT")}");
        }
        finally
        {
            Timeline.Destroy(id);
        }
    }

    private static void Check(ushort id, Playback from, float seed, uint[] ticks, bool backward, bool batch, ref long comparisons)
    {
        var expected = Run(Engine.Interpreter, id, from, seed, ticks, backward, batch);
        foreach (var engine in new[] { Engine.Compiled, Engine.Fused })
        {
            var actual = Run(engine, id, from, seed, ticks, backward, batch);
            if (actual != expected)
                throw new InvalidOperationException($"{engine}, from={from}, tick={string.Join(',', ticks.Take(8))}, backward={backward}, batch={batch}: {actual} != {expected}");
            comparisons++;
        }
    }

    private static Outcome Run(Engine engine, ushort id, Playback from, float seed, uint[] ticks, bool backward, bool batch)
    {
        var input = default(PulseInput);
        var result = new SumConsumer { Sum = seed };
        var playback = from;
        string? error = null, parameter = null;
        try
        {
            if (batch)
                playback = engine switch
                {
                    Engine.Interpreter => backward ? Timeline.Backward(id, in playback, in input, ref result, ticks) : Timeline.Forward(id, in playback, in input, ref result, ticks),
                    Engine.Compiled => backward ? CompiledPulse.Backward(in playback, in input, ref result, ticks) : CompiledPulse.Forward(in playback, in input, ref result, ticks),
                    Engine.Fused => backward ? FusedPulse.Backward(in playback, ref result.Sum, ticks) : FusedPulse.Forward(in playback, ref result.Sum, ticks),
                    _ => throw new ArgumentOutOfRangeException(nameof(engine))
                };
            else
                foreach (var tick in ticks)
                    playback = engine switch
                    {
                        Engine.Interpreter => backward ? Timeline.Backward(id, in playback, in input, ref result, tick) : Timeline.Forward(id, in playback, in input, ref result, tick),
                        Engine.Compiled => backward ? CompiledPulse.Backward(in playback, in input, ref result, tick) : CompiledPulse.Forward(in playback, in input, ref result, tick),
                        Engine.Fused => backward ? FusedPulse.Backward(in playback, ref result.Sum, tick) : FusedPulse.Forward(in playback, ref result.Sum, tick),
                        _ => throw new ArgumentOutOfRangeException(nameof(engine))
                    };
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            error = exception.GetType().FullName;
            parameter = (exception as ArgumentException)?.ParamName;
        }
        return new(SumReceipt.Capture(in playback, result.Sum), error, parameter);
    }

    private static float SampleOracle(uint tick, float sum, bool backward)
    {
        for (var track = 0; track < 4; track++)
        {
            var active = Clips.Where(clip => clip.Track == track && clip.Start <= tick && tick < clip.End).ToArray();
            if (active.Length == 0)
                continue;
            var amount = active[0].Amount;
            if (active.Length == 2)
            {
                var start = Math.Max(active[0].Start, active[1].Start);
                var length = Math.Min(active[0].End, active[1].End) - start;
                var factor = length <= 1 ? 0.5f : (tick - start) / (float)(length - 1);
                amount = active[0].Amount + (active[1].Amount - active[0].Amount) * factor;
            }
            if (backward)
                sum -= amount;
            else
                sum += amount;
        }
        return sum;
    }

    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);

    private static void RequireUnstartedStopRejects()
    {
        try { FusedPulse.Stop(default); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Unstarted stop must reject.");
    }
}
