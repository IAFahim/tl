using BenchmarkDotNet.Running;
using Systems;

if (args.Contains("--repro-olda"))
    return ReproOldA.Run();


if (args.Contains("--smoke"))
    return Smoke.Run();

BenchmarkRunner.Run<SystemsBenchmarks>(null, args);
return 0;

internal static class Smoke
{
    public static int Run()
    {
        var first = Pass();
        var second = Pass();
        Console.WriteLine($"SMOKE hash1={first:x16} hash2={second:x16} equal={first == second}");
        return first == second ? 0 : 1;
    }

    static ulong Pass()
    {
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        RunShape(bench, "NewA", () => bench.NewA());
        RunShape(bench, "NewABackward", () => bench.NewABackward());
        RunShape(bench, "NewB", () => bench.NewB());
        RunShape(bench, "NewC", () => bench.NewC());
        RunShape(bench, "NewD", () => bench.NewD());
        RunShape(bench, "S2OnePlayback", () => bench.S2OnePlayback());
        RunShape(bench, "S2TwoPlaybacks", () => bench.S2TwoPlaybacks());
        RunShape(bench, "S64TwoActive", () => bench.S64TwoActive());
        Console.WriteLine($"SMOKE sink={bench.Sink}");
        return bench.Checksum();
    }

    static void RunShape(SystemsBenchmarks bench, string name, Action run)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = DateTime.UtcNow;
        run();
        var elapsed = DateTime.UtcNow - start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"SMOKE {name} frames={SystemsBenchmarks.Frames} entities={SystemsBenchmarks.N} ms={elapsed.TotalMilliseconds:F1} bytes={allocated} sum={bench.Checksum():x16} ticks={bench.LastTicks}");
    }
}

internal static class Stress
{
    public static int Run()
    {
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        Loop("OldA", 3000, () => bench.OldA());
        Loop("NewA", 3000, () => bench.NewA());
        Loop("NewABackward", 3000, () => bench.NewABackward());
        Loop("OldB", 3000, () => bench.OldB());
        Loop("NewB", 3000, () => bench.NewB());
        Loop("OldC", 3000, () => bench.OldC());
        Loop("NewC", 3000, () => bench.NewC());
        Loop("OldD", 3000, () => bench.OldD());
        Loop("NewD", 3000, () => bench.NewD());
        Loop("S2OnePlayback", 3000, () => bench.S2OnePlayback());
        Loop("S2TwoPlaybacks", 3000, () => bench.S2TwoPlaybacks());
        Loop("S64TwoActive", 3000, () => bench.S64TwoActive());
        Console.WriteLine($"STRESS sink={bench.Sink}");
        return 0;
    }

    static void Loop(string name, int invokes, Action run)
    {
        for (var i = 0; i < invokes; i++) run();
        Console.WriteLine($"STRESS {name} ok");
    }
}

internal static class ReproOldA
{
    public static int Run()
    {
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        var asset = Tl.TimelineAsset.Load(SystemsBenchmarks.BakeA());
        const int n = 1024;
        var ids = new ushort[n];
        var pos = new ushort[n];
        var fx = new float[n];
        Array.Fill(ids, asset);
        for (var i = 0; i < 100000; i++)
        {
            var set = new Tl.ColumnSet();
            set.Add<float>(fx);
            for (var f = 0; f < 32; f++)
            {
                Tl.Timeline<Bench.BTrack, Bench.BClip>.Apply(ids, pos, true, in set);
                Tl.Timeline.Advance(asset, pos, true);
            }
            if ((i & 127) == 0) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        Console.WriteLine($"REPRO OldA done fx0={fx[0]}");
        return 0;
    }
}
