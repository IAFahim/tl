using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Compare;
using Tl;
using ViewScope = SystemsCompare.CompareView;
namespace SystemsCompare;

public sealed class CompareConfig : ManualConfig
{
    public CompareConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(8)
            .WithIterationCount(16)
            .WithIterationTime(TimeInterval.FromMilliseconds(200))
            .WithToolchain(BenchmarkDotNet.Toolchains.InProcess.NoEmit.InProcessNoEmitToolchain.Instance));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(CompareConfig))]
public unsafe class CompareBenchmarks
{
    // Same scale as the P0 LiveBatch harness.
    public const int N = 100000;
    public const ushort Duration = 97;

    public double Sink;

    TimelineWorld _wA = null!;
    TimelineWorld _wB = null!;
    TimelineWorld _wC = null!;
    TimelineWorld _wD = null!;
    ViewScope _scopeA = null!;
    ViewScope _scopeB = null!;
    ViewScope _scopeC = null!;
    ViewScope _scopeD = null!;
    PlaybackTable _pA = null!;
    PlaybackTable _pB = null!;
    PlaybackTable _pC = null!;
    PlaybackTable _pD = null!;

    float[] _y = null!;
    float[] _h = null!;
    int[] _pushes = null!;
    float[] _x = null!;
    F0[] _w = null!;
    F1[] _x1 = null!;
    F2[] _y2 = null!;
    F3[] _z3 = null!;

    static TimelineWorld BuildWorld(ViewScope scope, IList<ViewScope.TimelineSpec> timelines)
    {
        scope.Build(timelines);
        var world = new TimelineWorld();
        scope.InjectInto(world);
        GC.SuppressFinalize(world);
        return world;
    }

    static ViewScope.TimelineSpec LoopingTimeline(uint duration, params ViewScope.StepSpec[] steps)
    {
        var timeline = new ViewScope.TimelineSpec { Loop = 1, Duration = duration };
        foreach (var step in steps) timeline.Steps.Add(step);
        return timeline;
    }

    static ViewScope.StepSpec FoldStep(ulong pair, float scale, float amount, uint start, uint end, float[] lane)
    {
        var bytes = new byte[lane.Length * 4];
        for (var i = 0; i < lane.Length; i++)
            MemoryMarshal.Write(bytes.AsSpan(i * 4, 4), in lane[i]);
        return new ViewScope.StepSpec
        {
            PairKey = pair,
            Track = BitConverter.GetBytes(scale),
            First = BitConverter.GetBytes(amount),
            WindowStart = start,
            WindowEnd = end,
            Lane = bytes,
        };
    }

    static ViewScope.StepSpec ActionStep(ulong pair, float scale, float amount, uint start, uint end) => new()
    {
        PairKey = pair,
        Track = BitConverter.GetBytes(scale),
        First = BitConverter.GetBytes(amount),
        WindowStart = start,
        WindowEnd = end,
    };

    static float[] FoldLane(uint start, uint end, float amount, float scale, Func<float, float, float>? combine = null)
    {
        var lane = new float[(end - start) * 2];
        var unit = combine is null ? amount * scale : combine(amount, scale);
        for (uint tick = start; tick < end; tick++)
            for (var dir = 0; dir < 2; dir++)
                lane[(tick - start) * 2 + (uint)dir] = (dir == 1 ? -1f : 1f) * unit;
        return lane;
    }

    [GlobalSetup]
    public void Setup()
    {
        var aKeys = global::TlSystems.TlSystem_Compare_global__Compare_CA.PairKeys;
        var bKeys = global::TlSystems.TlSystem_Compare_global__Compare_CB.PairKeys;
        var cKeys = global::TlSystems.TlSystem_Compare_global__Compare_CC.PairKeys;
        var dKeys = global::TlSystems.TlSystem_Compare_global__Compare_CD.PairKeys;
        _scopeA = new ViewScope();
        _scopeB = new ViewScope();
        _scopeC = new ViewScope();
        _scopeD = new ViewScope();

        var aTimelines = new ViewScope.TimelineSpec[8];
        var bTimelines = new ViewScope.TimelineSpec[8];
        var cTimelines = new ViewScope.TimelineSpec[8];
        var dTimelines = new ViewScope.TimelineSpec[8];
        for (var p = 0; p < 8; p++)
        {
            var aScale = 1 + 0.5f * p;
            aTimelines[p] = LoopingTimeline(Duration,
                FoldStep(aKeys[0], aScale, 1 + p, 0, 64, FoldLane(0, 64, 1 + p, aScale)),
                FoldStep(aKeys[0], aScale, 2 + 0.25f * p, 33, 97, FoldLane(33, 97, 2 + 0.25f * p, aScale)));
            bTimelines[p] = LoopingTimeline(Duration,
                FoldStep(bKeys[0], 2f, 5 + p, 0, 64, FoldLane(0, 64, 5 + p, 2f)),
                FoldStep(bKeys[0], 2f, 3f, 33, 97, FoldLane(33, 97, 3f, 2f)));
            cTimelines[p] = LoopingTimeline(Duration,
                FoldStep(cKeys[0], 1.5f, 2 + p, 0, 97, FoldLane(0, 97, 2 + p, 1.5f)));
            dTimelines[p] = LoopingTimeline(Duration,
                FoldStep(dKeys[0], 1 + p, 2f, 0, 97, FoldLane(0, 97, 2f, 1 + p)),
                FoldStep(dKeys[1], 1f, 3 + p, 0, 97, FoldLane(0, 97, 3 + p, 1f)),
                FoldStep(dKeys[2], 1f, 4f, 0, 97, FoldLane(0, 97, 4f, 1f, static (a, s) => a * (1 + s))),
                FoldStep(dKeys[3], 1f, 5f, 0, 97, FoldLane(0, 97, 5f, 1f, static (a, s) => 2 * a * s)));
        }
        _wA = BuildWorld(_scopeA, aTimelines);
        _wB = BuildWorld(_scopeB, bTimelines);
        _wC = BuildWorld(_scopeC, cTimelines);
        _wD = BuildWorld(_scopeD, dTimelines);

        _pA = new PlaybackTable(N);
        _pB = new PlaybackTable(N);
        _pC = new PlaybackTable(N);
        _pD = new PlaybackTable(N);
        StartSpread(_pA, _wA);
        StartSpread(_pB, _wB);
        StartSpread(_pC, _wC);
        StartSpread(_pD, _wD);

        _y = new float[N];
        _h = new float[N];
        _pushes = new int[N];
        for (var i = 0; i < N; i++) _pushes[i] = 1 + i % 4;
        _x = new float[N];
        _w = new F0[N];
        _x1 = new F1[N];
        _y2 = new F2[N];
        _z3 = new F3[N];
    }

    static void StartSpread(PlaybackTable players, TimelineWorld world)
    {
        for (var e = 0; e < N; e++) players.Start(e, (ushort)((e % 8) + 1), (ushort)(e % Duration));
        world.ApplyRequests(players);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pA.Dispose();
        _pB.Dispose();
        _pC.Dispose();
        _pD.Dispose();
        _wA.Dispose();
        _wB.Dispose();
        _wC.Dispose();
        _wD.Dispose();
        _scopeA.Dispose();
        _scopeB.Dispose();
        _scopeC.Dispose();
        _scopeD.Dispose();
    }

    [Benchmark]
    public void NewA()
    {
        var system = new CA { Y = _y };
        _wA.ApplyRequests(_pA);
        system.RunFused(_wA, _pA, true);
        Sink += _y[0];
    }

    [Benchmark]
    public void NewB()
    {
        var system = new CB { H = _h, P = _pushes };
        _wB.ApplyRequests(_pB);
        system.Run(_wB, _pB, true);
        Sink += _h[0];
    }

    [Benchmark]
    public void NewC()
    {
        var system = new CC { X = _x };
        _wC.ApplyRequests(_pC);
        system.RunFused(_wC, _pC, true);
        Sink += _x[0];
    }

    [Benchmark]
    public void NewD()
    {
        var system = new CD { W = _w, X = _x1, Y = _y2, Z = _z3 };
        _wD.ApplyRequests(_pD);
        system.RunFused(_wD, _pD, true);
        Sink += _w[0].V + _x1[0].V + _y2[0].V + _z3[0].V;
    }

    internal void NewBStep()
    {
        var system = new CB { H = _h, P = _pushes };
        _wB.ApplyRequests(_pB);
        system.Run(_wB, _pB, true);
        _wB.Advance(_pB, true);
    }
    internal void NewASlow()
    {
        var system = new CA { Y = _y };
        _wA.ApplyRequests(_pA);
        system.Run(_wA, _pA, true);
        _wA.Advance(_pA, true);
    }

    internal void NewCSlow()
    {
        var system = new CC { X = _x };
        _wC.ApplyRequests(_pC);
        system.Run(_wC, _pC, true);
        _wC.Advance(_pC, true);
    }

    internal void NewDSlow()
    {
        var system = new CD { W = _w, X = _x1, Y = _y2, Z = _z3 };
        _wD.ApplyRequests(_pD);
        system.Run(_wD, _pD, true);
        _wD.Advance(_pD, true);
    }

    internal static string VerifyClosedForm()
    {
        var bench = new CompareBenchmarks();
        bench.Setup();
        for (var f = 0; f < 97; f++)
        {
            bench.NewA();
            bench.NewBStep();
            bench.NewC();
            bench.NewD();
        }
        var failures = 0;
        failures += Check("A y[0]", bench._y[0], 192f);
        failures += Check("B h[0]", bench._h[0], 1024f);
        failures += Check("C x[0]", bench._x[0], 291f);
        failures += Check("D w[0]", bench._w[0].V, 194f);
        failures += Check("D x[0]", bench._x1[0].V, -291f);
        failures += Check("D y[0]", bench._y2[0].V, 776f);
        failures += Check("D z[0]", bench._z3[0].V, 970f);
        return failures == 0 ? "VERIFY closed-form: all 7 match" : $"VERIFY closed-form: {failures} MISMATCH";
    }

    static int Check(string name, float actual, float expected)
    {
        if (actual == expected) { Console.WriteLine($"VERIFY {name} ok ({actual})"); return 0; }
        Console.WriteLine($"VERIFY {name} MISMATCH actual={actual} expected={expected}");
        return 1;
    }
    internal ulong Checksum()
    {
        var hash = 14695981039346656037ul;
        hash = Mix(hash, _y);
        hash = Mix(hash, _h);
        hash = Mix(hash, _pushes);
        hash = Mix(hash, _x);
        return hash;
    }

    static ulong Mix(ulong hash, Array array)
    {
        foreach (var value in array)
        {
            var bits = BitConverter.DoubleToInt64Bits(Convert.ToDouble(value));
            hash = (hash ^ (ulong)(bits & 0xffffffff)) * 1099511628211ul;
            hash = (hash ^ (ulong)(bits >> 32)) * 1099511628211ul;
        }
        return hash;
    }
}
