using System.Diagnostics;
using Pulse;
using Tl;

// The parity battery: ONE authored timeline (Authoring.Author), TWO players
// of the same instance — the in-memory interpreter (Tl.Core's Timeline.Build
// path) and the generated kernel (CompiledPulse, specialized at build time
// by the Tl.Gen compile reader). Every battery asserts bit-exact equality;
// any divergence is a bug, not a tolerance. Run with no arguments for the
// battery, or "bench" for the Stopwatch A/B used for the NativeAOT column.
internal static class Program
{
    private static int Main(string[] args)
    {
        var input = new PulseInput(Seed: 100f);

        // The interpreter leg: a normal runtime registration + explicit bind
        // (the NativeAOT-safe form).
        ushort id = Timeline<PulseTrack, PulseClip>.Build(Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(id);

        // The compiled leg: the generated static kernel. No registry entry,
        // no runtime build — the declaration was consumed at build time.
        var interp = new Leg(id, input, compiled: false);
        var kernel = new Leg(id, input, compiled: true);

        // Shape pins: both legs describe the same authored timeline.
        Check(CompiledPulse.Duration == Timeline.Duration(id), "duration pin");
        Check(Timeline.IsLooping(id), "looping pin");

        // 1. Sequential forward walk across two full loops (wraps, cycles,
        //    LastLoopFrame frames, blends, the gap, the clamp region).
        var walk = Sequence(0u, 1200u, static t => t + 1u).ToArray();
        Compare("sequential forward walk",
            RunScript(interp, ForwardScript(walk)),
            RunScript(kernel, ForwardScript(walk)));

        // 2. Backward walk from a high position down through zero (backward
        //    wraps, cycle saturation, Completed-free looping flags).
        var down = Sequence(1200u, 0u, static t => t - 1u).ToArray();
        Compare("sequential backward walk",
            RunScript(interp, BackwardScript(down)),
            RunScript(kernel, BackwardScript(down)));

        // 3. Backward mirror: forward the whole loop, then rewind it.
        var mirror = ForwardScript(Sequence(0u, 600u, static t => t + 1u))
            .Concat(BackwardScript(Sequence(599u, 0u, static t => t - 1u))).ToArray();
        Compare("forward/backward mirror",
            RunScript(interp, mirror),
            RunScript(kernel, mirror));

        // 4. Jump battery: fixed-seed pseudo-random jumps, direction
        //    flipping in blocks, multi-cycle forward wraps, backward jumps
        //    below and above the previous position.
        var random = 0xA312AFD5u;
        var jumps = new (bool Back, uint Tick)[4096];
        var position = 0u;
        for (var i = 0; i < jumps.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            var delta = (int)(random % 1501u) - 500;
            var next = (int)position + delta;
            if (next < 0)
                next += 600;
            position = (uint)Math.Min(next, 2400);
            jumps[i] = (((i >> 4) & 1) == 1, position);
        }

        Compare("jump battery",
            RunScript(interp, jumps),
            RunScript(kernel, jumps));

        // 5. Batch vs singles: one span call over the walk must land on the
        //    same final state as 1201 single-tick calls, on both legs.
        Compare("batch vs singles (interpreter)",
            [RunBatch(interp, walk)], [SinglesFinal(interp, walk)]);
        Compare("batch vs singles (kernel)",
            [RunBatch(kernel, walk)], [SinglesFinal(kernel, walk)]);

        // 6. State-code totals: per-state counts over the sequential walk.
        var interpWalk = RunScript(interp, ForwardScript(walk));
        var kernelWalk = RunScript(kernel, ForwardScript(walk));
        Console.WriteLine($"state-code totals: enters {interpWalk[^1].Enters}, stays {interpWalk[^1].Stays}, exits {interpWalk[^1].Exits}, ticks {interpWalk[^1].Ticks}, sum-bits {interpWalk[^1].SumBits:X8}");

        // 7. Empty ticks: the gap region [11,18) fires no callback on
        //    either leg. (On this looping timeline every tick past the
        //    duration wraps to an active position — the wrap shapes are
        //    already exercised by the jump battery.)
        var gapTicks = Sequence(11u, 17u, static t => t + 1u).ToArray();
        var interpEmpty = RunScript(interp, ForwardScript(gapTicks));
        var kernelEmpty = RunScript(kernel, ForwardScript(gapTicks));
        Check(interpEmpty.All(static r => r.Count == 0) && kernelEmpty.All(static r => r.Count == 0), "empty ticks fire no callback");
        Compare("gap-region walk", interpEmpty, kernelEmpty);

        // 8. Lifecycle: Stop semantics and rejection of stopped/unstarted
        //    playbacks agree.
        var stoppedInterp = Timeline.Stop(id, interp.Start(0));
        var stoppedKernel = CompiledPulse.Stop(kernel.Start(0));
        Check(stoppedInterp.Has(PlaybackFlags.Stopped) && stoppedKernel.Has(PlaybackFlags.Stopped), "stop flags agree");

        var resultA = new PulseResult();
        var stoppedPlaybackA = Timeline.Stop(id, Timeline.Start(id));
        AssertThrows<InvalidOperationException>("stopped interpreter rejects Forward",
            () => Timeline.Forward(id, in stoppedPlaybackA, in input, ref resultA, 0u));
        var resultB = new PulseResult();
        var stoppedPlaybackB = CompiledPulse.Stop(CompiledPulse.Start());
        AssertThrows<InvalidOperationException>("stopped kernel rejects Forward",
            () => CompiledPulse.Forward(in stoppedPlaybackB, in input, ref resultB, 0u));

        var fresh = new PulseResult();
        AssertThrows<InvalidOperationException>("unstarted interpreter rejects Forward",
            () => Timeline.Forward(id, new Playback(), in input, ref fresh, 0u));
        AssertThrows<InvalidOperationException>("unstarted kernel rejects Forward",
            () => CompiledPulse.Forward(new Playback(), in input, ref fresh, 0u));

        Console.WriteLine("PARITY OK: interpreter and compiled kernel are behaviorally identical across the battery.");
        Console.WriteLine($"final walk receipt: tick {kernelWalk[^1].Tick}, cycles {kernelWalk[^1].Cycles}, flags {kernelWalk[^1].Flags}");

        if (args.Length > 0 && args[0] == "bench")
            Bench(input, id);

        return 0;
    }

    private static void Bench(PulseInput input, ushort id)
    {
        const int steps = 2_000_000;

        var interpreterResult = new PulseResult();
        var interpreterPlayback = Timeline.Start(id);
        var kernelResult = new PulseResult();
        var kernelPlayback = CompiledPulse.Start();

        // Interpreter.
        var sw = Stopwatch.StartNew();
        for (var i = 0u; i < steps; i++)
            interpreterPlayback = Timeline.Forward(id, in interpreterPlayback, in input, ref interpreterResult, i % 600u);
        sw.Stop();
        var interpreterNanos = sw.Elapsed.TotalNanoseconds / steps;

        // Kernel.
        sw.Restart();
        for (var i = 0u; i < steps; i++)
            kernelPlayback = CompiledPulse.Forward(in kernelPlayback, in input, ref kernelResult, i % 600u);
        sw.Stop();
        var kernelNanos = sw.Elapsed.TotalNanoseconds / steps;

        Console.WriteLine($"bench (Stopwatch, sequential ticks, consumer retained):");
        Console.WriteLine($"  interpreter: {interpreterNanos:F2} ns/tick");
        Console.WriteLine($"  kernel:      {kernelNanos:F2} ns/tick  ({interpreterNanos / kernelNanos:F1}x)");
    }

    private static (bool Back, uint Tick)[] ForwardScript(IEnumerable<uint> ticks)
        => ticks.Select(static t => (false, t)).ToArray();

    private static (bool Back, uint Tick)[] BackwardScript(IEnumerable<uint> ticks)
        => ticks.Select(static t => (true, t)).ToArray();

    private static IEnumerable<uint> Sequence(uint from, uint to, Func<uint, uint> step)
    {
        var t = from;
        while (true)
        {
            yield return t;
            if (t == to)
                yield break;
            t = step(t);
        }
    }

    private static List<Receipt> RunScript(Leg leg, ReadOnlySpan<(bool Back, uint Tick)> script)
    {
        var receipts = new List<Receipt>(script.Length);
        var result = new PulseResult();
        var playback = leg.Start(0);
        foreach (var (back, tick) in script)
        {
            playback = back
                ? leg.Backward(in playback, ref result, [tick])
                : leg.Forward(in playback, ref result, [tick]);
            receipts.Add(Receipt.Capture(in playback, ref result));
        }

        return receipts;
    }

    private static Receipt RunBatch(Leg leg, ReadOnlySpan<uint> ticks)
    {
        var result = new PulseResult();
        var playback = leg.Start(0);
        playback = leg.Forward(in playback, ref result, ticks);
        return Receipt.Capture(in playback, ref result);
    }

    private static Receipt SinglesFinal(Leg leg, ReadOnlySpan<uint> ticks)
        => RunScript(leg, ForwardScript(ticks.ToArray()))[^1];

    private static void Compare(string name, List<Receipt> expected, List<Receipt> actual)
    {
        if (expected.Count != actual.Count)
            Fail($"{name}: receipt count {expected.Count} vs {actual.Count}");

        for (var i = 0; i < expected.Count; i++)
        {
            if (expected[i].Equals(actual[i]))
                continue;

            Fail($"{name}: first divergence at step {i}: interpreter {expected[i]} vs kernel {actual[i]}");
        }

        Console.WriteLine($"{name}: OK ({expected.Count} steps)");
    }

    private static void AssertThrows<TException>(string name, Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine($"{name}: OK");
            return;
        }

        Fail($"{name}: no {typeof(TException).Name}");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            Fail(name);
        Console.WriteLine($"{name}: OK");
    }

    private static void Fail(string message)
    {
        Console.Error.WriteLine($"PARITY FAIL: {message}");
        Environment.Exit(1);
    }

    private readonly record struct Receipt(
        uint Tick, ushort Cycles, ushort Flags,
        int SumBits, long Ticks, int Count, int Enters, int Stays, int Exits)
    {
        public static Receipt Capture(in Playback playback, ref readonly PulseResult result) => new(
            playback.Tick,
            playback.Cycles,
            (ushort)playback.Flags,
            BitConverter.SingleToInt32Bits(result.Sum),
            result.Ticks,
            result.Count,
            result.Enters,
            result.Stays,
            result.Exits);
    }

    // One player of the shared authored timeline: either the registered
    // interpreter entry or the generated static kernel.
    private sealed class Leg
    {
        private readonly ushort _id;
        private readonly PulseInput _input;
        private readonly bool _compiled;

        public Leg(ushort id, PulseInput input, bool compiled)
        {
            _id = id;
            _input = input;
            _compiled = compiled;
        }

        public Playback Start(uint at) => _compiled ? CompiledPulse.Start(at) : Timeline.Start(_id, at);

        public Playback Forward(in Playback from, ref PulseResult result, ReadOnlySpan<uint> ticks)
            => _compiled
                ? CompiledPulse.Forward(in from, in _input, ref result, ticks)
                : Timeline.Forward(_id, in from, in _input, ref result, ticks);

        public Playback Backward(in Playback from, ref PulseResult result, ReadOnlySpan<uint> ticks)
            => _compiled
                ? CompiledPulse.Backward(in from, in _input, ref result, ticks)
                : Timeline.Backward(_id, in from, in _input, ref result, ticks);
    }
}
