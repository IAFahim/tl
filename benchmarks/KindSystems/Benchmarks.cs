using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Tl;

namespace KindBench;

public sealed class KindsConfig : ManualConfig
{
    public KindsConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(6)
            .WithIterationCount(15)
            .WithIterationTime(Perfolizer.Horology.TimeInterval.FromMilliseconds(250))
            .WithToolchain(BenchmarkDotNet.Toolchains.InProcess.NoEmit.InProcessNoEmitToolchain.Instance));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(KindsConfig))]
public unsafe class KindBenchmarks
{
    public const int N = 100_000;
    public const int Frames = 32;
    const ushort Hit = 1, Long = 2, Walk = 3, WalkLegacy = 4;
    public double Sink;

    TimelineWorld _shapes = null!, _many = null!, _pairs = null!;
    PlaybackTable _plA = null!, _pA = null!, _pB = null!, _pC = null!, _pD = null!, _pE2 = null!, _pE64 = null!, _pF1 = null!, _pF8 = null!;
    KindStore _sA = null!, _sB = null!, _sC = null!, _sD = null!, _sE2 = null!, _sE64 = null!, _sF1 = null!, _sF8 = null!;
    KindPulses<int, DmgFold> _bullets1, _bullets8;
    float[] _fxPlan = null!, _fxA = null!, _fxB = null!, _fxC = null!, _fxD = null!, _fxE2 = null!, _fxE64 = null!, _fxF = null!;
    int[] _hp1 = null!, _hp8 = null!;

    static string Lib(string name) => Path.Combine(AppContext.BaseDirectory, "libs", name + ".tlib");

    static string[] Names(int count)
    {
        var names = new string[count];
        for (var i = 0; i < count; i++) names[i] = "K" + i;
        return names;
    }

    static ulong[] Reach(int entities, Func<int, ulong> of)
    {
        var reach = new ulong[entities];
        for (var e = 0; e < entities; e++) reach[e] = of(e);
        return reach;
    }

    [GlobalSetup]
    public void Setup()
    {
        _shapes = new TimelineWorld(File.ReadAllBytes(Lib("shapes")));
        _many = new TimelineWorld(File.ReadAllBytes(Lib("many")));
        _pairs = new TimelineWorld(File.ReadAllBytes(Lib("pairs")));
        var k0 = new[] { "K0" };
        var one = Reach(N, _ => 1ul);
        var dmg = new[] { "Dmg" };
        _plA = new PlaybackTable(N);
        _pA = new PlaybackTable(N);
        _pB = new PlaybackTable(N);
        _pC = new PlaybackTable(N);
        _pD = new PlaybackTable(N);
        _pE2 = new PlaybackTable(N);
        _pE64 = new PlaybackTable(N);
        _pF1 = new PlaybackTable(N);
        _pF8 = new PlaybackTable(N);
        _sA = new KindStore(_shapes, _pA, k0, one);
        _sB = new KindStore(_shapes, _pB, k0, one);
        _sC = new KindStore(_many, _pC, k0, one);
        _sD = new KindStore(_shapes, _pD, k0, one);
        var names = Names(62);
        _sE2 = new KindStore(_pairs, _pE2, names, Reach(N, _ => 0b11ul));
        _sE64 = new KindStore(_pairs, _pE64, names, Reach(N, e => 0b11ul << (2 * Block(e))));
        _sF1 = new KindStore(_shapes, _pF1, dmg, one);
        _sF8 = new KindStore(_shapes, _pF8, dmg, one);
        _bullets1 = _sF1.Pulses<int, DmgFold>("Dmg");
        _bullets8 = _sF8.Pulses<int, DmgFold>("Dmg");
        _fxPlan = new float[N];
        _fxA = new float[N];
        _fxB = new float[N];
        _fxC = new float[N];
        _fxD = new float[N];
        _fxE2 = new float[N];
        _fxE64 = new float[N];
        _fxF = new float[N];
        _hp1 = new int[N];
        _hp8 = new int[N];
        for (var e = 0; e < N; e++)
        {
            _plA.Start(e, WalkLegacy);
            _pA.Start(e, Walk);
            if (e % 10 == 0) _pB.Start(e, Walk);
            _pC.Start(e, (ushort)(1 + (int)((uint)(e * 2654435761u) % 100)), (ushort)(e % 40));
            _pD.Start(e, Long, (ushort)(e % 40000));
            _pE2.Start(e, (ushort)(33 + Block(e)));
            _pE64.Start(e, (ushort)(2 + Block(e)));
        }
        _shapes.ApplyRequests(_plA);
    }

    static int Block(int e) => (int)((long)e * 31 / N);

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var s in new[] { _sA, _sB, _sC, _sD, _sE2, _sE64, _sF1, _sF8 }) s.Dispose();
        foreach (var p in new[] { _plA, _pA, _pB, _pC, _pD, _pE2, _pE64, _pF1, _pF8 }) p.Dispose();
        _shapes.Dispose();
        _many.Dispose();
        _pairs.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void PlanA()
    {
        for (var f = 0; f < Frames; f++)
        {
            _shapes.ApplyRequests(_plA);
            new LSys { Fx = _fxPlan }.Run(_shapes, _plA, true);
            _shapes.Advance(_plA, true);
        }
        Sink += _fxPlan[0] + _fxPlan[N - 1];
    }

    void Frames_(TimelineWorld world, PlaybackTable players, float[] fx, int[] hp)
    {
        for (var f = 0; f < Frames; f++)
        {
            world.Tick(players);
            new KSys { Fx = fx, Hp = hp }.Run(world, players, true);
        }
        Sink += fx[0] + fx[N - 1];
    }

    [Benchmark] public void KindsA() => Frames_(_shapes, _pA, _fxA, _hp8);

    [Benchmark] public void KindsB() => Frames_(_shapes, _pB, _fxB, _hp8);

    [Benchmark] public void KindsC() => Frames_(_many, _pC, _fxC, _hp8);

    [Benchmark]
    public void KindsDTickOnly()
    {
        for (var f = 0; f < Frames; f++) _shapes.Tick(_pD);
        Sink += _pD.SlotsOf(0)->Tick;
    }

    [Benchmark] public void KindsE2() => Frames_(_pairs, _pE2, _fxE2, _hp8);

    [Benchmark] public void KindsE64() => Frames_(_pairs, _pE64, _fxE64, _hp8);

    void Damage(PlaybackTable players, KindPulses<int, DmgFold> bullets, int pulses, int[] hp)
    {
        for (var f = 0; f < Frames; f++)
        {
            for (var e = 0; e < N; e++)
                for (var k = 0; k < pulses; k++) bullets.To(e, 1);
            _shapes.Tick(players);
            new KSys { Fx = _fxF, Hp = hp }.Run(_shapes, players, true);
        }
        Sink += hp[0] + hp[N - 1];
    }

    [Benchmark] public void KindsF1() => Damage(_pF1, _bullets1, 1, _hp1);

    [Benchmark] public void KindsF8() => Damage(_pF8, _bullets8, 8, _hp8);

    public bool Parity() => _fxPlan.AsSpan().SequenceEqual(_fxA);

    public (long A, long E2, long E64) BytesPerEntity() => (Row(_sA), Row(_sE2), Row(_sE64));

    long Row(KindStore s)
    {
        long bytes = 0;
        for (var k = 0; k < s.KindCount; k++) if (s.RowOf(0, k) >= 0) bytes += s.GetStats(k).BytesPerRow;
        return bytes;
    }

    public int Active(PlaybackTable p)
    {
        var n = 0;
        for (var e = 0; e < N; e++) if (p.CountOf(e) > 0) n++;
        return n;
    }

    public (int Max, int Overflows) Damage1() => (_sF1.GetStats(_sF1.IndexOf("Dmg")).MaxLength, _sF1.GetStats(_sF1.IndexOf("Dmg")).Overflows);

    public (int Max, int Overflows) Damage8() => (_sF8.GetStats(_sF8.IndexOf("Dmg")).MaxLength, _sF8.GetStats(_sF8.IndexOf("Dmg")).Overflows);
}

public static class Smoke
{
    static (string, Action)[] Shapes(KindBenchmarks b) =>
    [
        ("PlanA", b.PlanA), ("KindsA", b.KindsA), ("KindsB", b.KindsB), ("KindsC", b.KindsC), ("KindsDTickOnly", b.KindsDTickOnly),
        ("KindsE2", b.KindsE2), ("KindsE64", b.KindsE64), ("KindsF1", b.KindsF1), ("KindsF8", b.KindsF8),
    ];

    public static int Run()
    {
        var bench = new KindBenchmarks();
        bench.Setup();
        var shapes = Shapes(bench);
        for (var i = 0; i < 2; i++) { bench.PlanA(); bench.KindsA(); }
        var parity = bench.Parity();
        foreach (var (_, run) in shapes) for (var w = 0; w < 30; w++) run();
        foreach (var (name, run) in shapes)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var r = 0; r < 20; r++) run();
            var ms = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds / 20;
            Console.WriteLine($"SMOKE {name} frames={KindBenchmarks.Frames} entities={KindBenchmarks.N} ms/op={ms:F2} bytes/20ops={GC.GetAllocatedBytesForCurrentThread() - before}");
        }
        Console.WriteLine($"SMOKE parityPlanVsKinds={parity}");
        Console.WriteLine($"SMOKE kindBytesPerEntity A/E2/E64={bench.BytesPerEntity()} damage1={bench.Damage1()} damage8={bench.Damage8()}");
        bench.Cleanup();
        return parity ? 0 : 1;
    }

    public static int Profile(string shape)
    {
        var bench = new KindBenchmarks();
        bench.Setup();
        var run = typeof(KindBenchmarks).GetMethod(shape)!.CreateDelegate<Action>(bench);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var ops = 0;
        while (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds < 8) { run(); ops++; }
        Console.WriteLine($"PROFILE {shape} ops={ops} ms/op={8000.0 / ops:F2}");
        bench.Cleanup();
        return 0;
    }
}
