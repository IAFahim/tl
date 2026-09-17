using System.Diagnostics;

namespace HandleInterning;

public static class Measure
{
    const int ColdPairs = 256;
    const long WarmReps = 50_000_000;
    const int WarmRuns = 7;

    public static int RunAll()
    {
        var table = new InternTable(4096);
        var asset = table.Load(Rng.Payload(11, 4096));
        try
        {
            Run(asset);
        }
        finally
        {
            asset.Dispose();
            table.Dispose();
        }
        InternTable.DrainGraveyardForced();
        PairFolds.ResetAll();
        var ok = PairFolds.FoldAllocBytes == PairFolds.FoldFreeBytes &&
                 InternTable.AllocBytes == InternTable.FreeBytes &&
                 InternTable.AllocCount == InternTable.FreeCount;
        Console.WriteLine($"  measure balance: fold {PairFolds.FoldAllocBytes} == {PairFolds.FoldFreeBytes}, blocks {InternTable.AllocCount} == {InternTable.FreeCount} -> {(ok ? "PASS" : "FAIL")}");
        return ok ? 0 : 1;
    }

    static void Run(Asset asset)
    {
        Console.WriteLine($"hand measurement: fold work over {ColdPairs} cold pairs, warm loops {WarmReps} reps x {WarmRuns} runs (median), Stopwatch");
        var foldNs = new double[ColdPairs];
        ushort pos = 0;
        for (var i = 0; i < ColdPairs; i++)
        {
            TypedLane.Advance(asset, (ulong)i + 1, ref pos);
            foldNs[i] = 0;
        }
        for (var i = 0; i < ColdPairs; i++)
        {
            var pair = (ulong)(i + 1 + ColdPairs);
            pos = 0;
            var sw = Stopwatch.StartNew();
            TypedLane.Advance(asset, pair, ref pos);
            foldNs[i] = sw.Elapsed.TotalMilliseconds * 1e6;
        }
        Array.Sort(foldNs);
        var foldMedian = foldNs[ColdPairs / 2];
        var guard = WarmNs((asset, PairWarm), 0);
        var direct = WarmNs((asset, PairWarm), 1);
        var indirect = WarmNs((asset, PairWarm), 2);
        Console.WriteLine();
        Console.WriteLine($"  cold first-use (probe miss + fold + publish + resolve): median {foldMedian:F0} ns ({foldMedian / 1000.0:F1} us), min {foldNs[0]:F0} ns, max {foldNs[ColdPairs - 1]:F0} ns");
        Console.WriteLine($"  steady-state guarded hit (owner+pair check then record walk): {guard:F3} ns/advance");
        Console.WriteLine($"  no-guard direct statics (eager-bind world):                   {direct:F3} ns/advance");
        Console.WriteLine($"  unconditional indirect (hash probe per advance):              {indirect:F3} ns/advance");
        Console.WriteLine($"  guard overhead vs no-guard: {guard - direct:+0.000;-0.000} ns; guard saving vs indirect: {indirect - guard:+0.000;-0.000} ns");
        Console.WriteLine();
        Console.WriteLine("  amortized first-call cost (fold + N guarded uses, vs N no-guard uses):");
        Console.WriteLine("   N          guarded total     no-guard total    amortized guard overhead/use");
        foreach (var n in new[] { 1L, 1_000L, 1_000_000L })
        {
            var guarded = foldMedian + n * guard;
            var baseline = n * direct;
            var perUse = (guarded - baseline) / n;
            Console.WriteLine($"   {n,9}   {guarded,14:F0} ns   {baseline,14:F0} ns   {perUse,+8:F3} ns");
        }
        Console.WriteLine();
    }

    public const ulong PairWarm = 0xC0FFEEC0FFEEC0FFUL;

    static double WarmNs((Asset, ulong) args, int variant)
    {
        var (asset, pair) = args;
        var sink = 0.0;
        ushort pos = 0;
        for (var i = 0; i < 5_000_000; i++) sink += Advance(variant, asset, pair, ref pos);
        var samples = new double[WarmRuns];
        for (var r = 0; r < WarmRuns; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < WarmReps; i++) sink += Advance(variant, asset, pair, ref pos);
            samples[r] = sw.Elapsed.TotalMilliseconds * 1e6 / WarmReps;
        }
        Array.Sort(samples);
        if (sink == -1) Console.WriteLine(sink);
        return samples[WarmRuns / 2];
    }

    static float Advance(int variant, Asset asset, ulong pair, ref ushort pos)
    {
        return variant switch
        {
            0 => TypedLane.Advance(asset, pair, ref pos),
            1 => TypedLane.AdvanceDirect(ref pos),
            _ => TypedLane.AdvanceIndirect(asset, pair, ref pos),
        };
    }
}
