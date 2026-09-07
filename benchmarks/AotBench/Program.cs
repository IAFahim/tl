using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tl.Hooks;

// Manual measurement harness for the frozen path under NativeAOT.
//
// BenchmarkDotNet cannot run inside a NativeAOT publish: `dotnet publish
// /p:PublishAot=true` on the Dispatch project dies with trim and single-file
// analysis errors (IL2104/IL3053/IL3000 in BDN, CommandLine, TraceEvent,
// Roslyn and Capstone), and BDN's default toolchain would spawn generated JIT
// child projects anyway - useless for measuring the AOT binary. This harness
// is the fallback, labeled "manual harness" in docs/benchmarks.md.
//
// The tick generation and every arm body are copied verbatim from the Frozen
// benchmark class in ../Dispatch/Benchmarks.cs: seed 0x6D2B79F5, one xorshift
// step per index, Vitals %= 600 and Fused16 %= 63 from the same random value,
// sequential arms = i % duration. Each sample times one full 65536-tick pass
// (BDN's OperationsPerInvoke = 65536 gives the same amortization) and reports
// ns per tick; the number written down is the median over 512 samples per
// round, then the median across 5 rounds.
//
// The harness also runs unchanged under the JIT, so the same-harness JIT
// column cross-checks the BDN numbers before the AOT column is compared
// against them.
//
// Modes:
//   --check   parity self-checks + the checksum table only (JIT vs AOT diff)
//   (default) checks, then the measurement

if (args is ["--check"])
{
    AotBench.RunChecks();
    return;
}

AotBench.RunChecks();
AotBench.RunMeasurement();

internal static class AotBench
{
    public const int Operations = 65536;

    private static readonly uint[] VitalsTicks = new uint[Operations];
    private static readonly uint[] FusedTicks = new uint[Operations];
    private static readonly uint[] VitalsSeq = new uint[Operations];
    private static readonly uint[] FusedSeq = new uint[Operations];

    private static float s_accumulator;

    static AotBench()
    {
        // Frozen.Setup, verbatim.
        uint random = 0x6D2B79F5;
        for (var i = 0; i < Operations; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            VitalsTicks[i] = random % 600;
            FusedTicks[i] = random % 63;
            VitalsSeq[i] = (uint)i % 600;
            FusedSeq[i] = (uint)i % 63;
        }
    }

    // The six arms, bodies verbatim from the Frozen benchmark class (the
    // arrays become the ReadOnlySpan parameter; everything inside is the
    // same, including the params-expanded 8-arg batch calls).

    private static float Fused16Single(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        foreach (var tick in ticks)
            pb = Fused16Frozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    private static float Fused16Batch8(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.Slice(i, 8);
            pb = Fused16Frozen.Forward(in pb, ref sink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }
        return sink.Sum + sink.Flags + sink.Count;
    }

    private static float Fused16Sequential(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        foreach (var tick in ticks)
            pb = Fused16Frozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    private static float VitalsSingle(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        foreach (var tick in ticks)
            pb = VitalsFrozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    private static float VitalsBatch8(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.Slice(i, 8);
            pb = VitalsFrozen.Forward(in pb, ref sink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }
        return sink.Sum + sink.Flags + sink.Count;
    }

    private static float VitalsSequential(ReadOnlySpan<uint> ticks)
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        foreach (var tick in ticks)
            pb = VitalsFrozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    // Parity self-checks, run before any timing (the full oracle parity
    // lives in Dispatch --verify; these are the receipts that need no extra
    // tables, so they run under AOT too): the forward/backward mirror over
    // the full duration restores the sink exactly, and one 8-arg batch call
    // per group equals eight single calls bit-for-bit over the whole random
    // array. Also exercises the static-abstract IFrozen face under AOT.

    public static void RunChecks()
    {
        Console.WriteLine($"runtime: {(RuntimeFeature.IsDynamicCodeSupported ? "jit" : "nativeaot")}");
        CheckMirror<VitalsFrozen>(600u);
        CheckMirror<Fused16Frozen>(63u);
        CheckBatch(FusedTicks, fused: true);
        CheckBatch(VitalsTicks, fused: false);
        Console.WriteLine("checks: batch-vs-single bit-exact, forward/backward mirror exact (Vitals + Fused16)");
        Console.WriteLine("checksums (JIT and AOT output must be identical):");
        Console.WriteLine($"  Fused16Single(random)   {Fused16Single(FusedTicks):R}");
        Console.WriteLine($"  Fused16Batch8(random)   {Fused16Batch8(FusedTicks):R}");
        Console.WriteLine($"  Fused16Sequential       {Fused16Sequential(FusedSeq):R}");
        Console.WriteLine($"  VitalsSingle(random)    {VitalsSingle(VitalsTicks):R}");
        Console.WriteLine($"  VitalsBatch8(random)    {VitalsBatch8(VitalsTicks):R}");
        Console.WriteLine($"  VitalsSequential        {VitalsSequential(VitalsSeq):R}");
    }

    private static void CheckMirror<TFrozen>(uint duration)
        where TFrozen : struct, IFrozen
    {
        var sink = default(FrozenSink);
        var pb = TFrozen.Start();
        for (uint t = 0; t < duration; t++)
            pb = TFrozen.Forward(in pb, ref sink, t);
        for (var t = (int)(duration - 1); t >= 0; t--)
            pb = TFrozen.Backward(in pb, ref sink, (uint)t);
        if (pb.Tick != 0 || sink.Count != 0 || sink.Flags != 0L || Math.Abs(sink.Sum) > 1e-2f)
            throw new InvalidOperationException(
                $"{typeof(TFrozen).Name} mirror did not restore: tick {pb.Tick}, sum {sink.Sum:R}, flags {sink.Flags}, count {sink.Count}.");
    }

    private static void CheckBatch(uint[] ticks, bool fused)
    {
        var singleSink = default(FrozenSink);
        var single = fused ? Fused16Frozen.Start() : VitalsFrozen.Start();
        foreach (var tick in ticks)
            single = fused
                ? Fused16Frozen.Forward(in single, ref singleSink, tick)
                : VitalsFrozen.Forward(in single, ref singleSink, tick);

        var batchSink = default(FrozenSink);
        var batch = fused ? Fused16Frozen.Start() : VitalsFrozen.Start();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.AsSpan(i, 8);
            batch = fused
                ? Fused16Frozen.Forward(in batch, ref batchSink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7])
                : VitalsFrozen.Forward(in batch, ref batchSink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }

        if (single.Tick != batch.Tick || single.Cycles != batch.Cycles || single.Flags != batch.Flags
            || singleSink.Sum != batchSink.Sum || singleSink.Flags != batchSink.Flags || singleSink.Count != batchSink.Count)
            throw new InvalidOperationException(
                $"{(fused ? "Fused16" : "Vitals")} batch-vs-single mismatch: ({single.Tick}/{single.Cycles}/{single.Flags} {singleSink.Sum:R}/{singleSink.Flags}/{singleSink.Count}) vs ({batch.Tick}/{batch.Cycles}/{batch.Flags} {batchSink.Sum:R}/{batchSink.Flags}/{batchSink.Count}).");
    }

    public static void RunMeasurement()
    {
        Console.WriteLine($"measurement: manual Stopwatch harness, {Operations} ticks per sample");
        var arms = new (string Name, Func<ReadOnlySpan<uint>, float> Arm, uint[] Ticks)[]
        {
            ("Fused16Single(random)", Fused16Single, FusedTicks),
            ("Fused16Batch8(random)", Fused16Batch8, FusedTicks),
            ("Fused16Sequential", Fused16Sequential, FusedSeq),
            ("VitalsSingle(random)", VitalsSingle, VitalsTicks),
            ("VitalsBatch8(random)", VitalsBatch8, VitalsTicks),
            ("VitalsSequential", VitalsSequential, VitalsSeq),
        };

        const int rounds = 5;
        const int samples = 512;
        var perTick = new double[samples];
        var roundMedians = new double[rounds];
        foreach (var arm in arms)
        {
            long allocated = 0;
            for (var round = 0; round < rounds; round++)
            {
                Warmup(arm.Arm, arm.Ticks, round == 0 ? 2000 : 200);
                if (round == 0)
                    allocated = GC.GetTotalAllocatedBytes(precise: true);
                roundMedians[round] = Sample(arm.Arm, arm.Ticks, perTick);
                if (round == 0)
                    allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
            }

            Span<double> sorted = [.. roundMedians];
            sorted.Sort();
            var median = sorted[rounds / 2];
            var best = sorted[0];
            Console.WriteLine(
                $"  {arm.Name,-22} median {median,8:F3} ns/tick   rounds [{string.Join(", ", roundMedians.Select(m => m.ToString("F3")))}]   best-round {best,8:F3}   alloc {allocated} B");
        }

        Console.WriteLine($"sink accumulator (DCE guard): {s_accumulator:R}");
    }

    private static void Warmup(Func<ReadOnlySpan<uint>, float> arm, uint[] ticks, int milliseconds)
    {
        var start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(start).TotalMilliseconds < milliseconds)
            s_accumulator += arm(ticks);
    }

    private static double Sample(Func<ReadOnlySpan<uint>, float> arm, uint[] ticks, double[] perTick)
    {
        for (var s = 0; s < perTick.Length; s++)
        {
            var t0 = Stopwatch.GetTimestamp();
            s_accumulator += arm(ticks);
            var t1 = Stopwatch.GetTimestamp();
            perTick[s] = Stopwatch.GetElapsedTime(t0, t1).TotalNanoseconds / Operations;
        }

        Array.Sort(perTick);
        return perTick[perTick.Length / 2];
    }
}
