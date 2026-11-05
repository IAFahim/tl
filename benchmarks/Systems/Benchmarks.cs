using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Bench;
using Tl;
using Tl.TestSupport;
using Tl.Systems.Tests;

namespace Systems;

public sealed class SystemsConfig : ManualConfig
{
    public SystemsConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(8)
            .WithIterationCount(16)
            .WithIterationTime(Perfolizer.Horology.TimeInterval.FromMilliseconds(200))
            .WithToolchain(BenchmarkDotNet.Toolchains.InProcess.NoEmit.InProcessNoEmitToolchain.Instance));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(SystemsConfig))]
public unsafe class SystemsBenchmarks
{
    public const int N = 1024;
    public const int Frames = 32;

    public double Sink;

    TimelineWorld _wA = null!;
    TimelineWorld _wB = null!;
    TimelineWorld _wC = null!;
    TimelineWorld _wD = null!;
    TimelineWorld _wS2 = null!;
    TimelineWorld _wS64 = null!;
    ViewScope _scopeA = null!;
    ViewScope _scopeB = null!;
    ViewScope _scopeC = null!;
    ViewScope _scopeD = null!;
    ViewScope _scopeS2 = null!;
    ViewScope _scopeS64 = null!;
    PlaybackTable _pA = null!;
    PlaybackTable _pB = null!;
    PlaybackTable _pC = null!;
    PlaybackTable _pD = null!;
    PlaybackTable _pS2 = null!;
    PlaybackTable _pS2One = null!;
    PlaybackTable _pS64 = null!;

    float[] _fxA = null!;
    float[] _fxB = null!;
    int[] _ixB = null!;
    int[] _gainC = null!;
    float[] _fxC = null!;
    float[] _d0 = null!;
    int[] _d1 = null!;
    short[] _d2 = null!;
    sbyte[] _d3 = null!;
    int[] _s2x = null!;
    short[] _s2y = null!;
    int _s64ticks;

    ushort _assetA;
    ushort _assetB;
    ushort _assetC;
    ushort _assetD0;
    ushort _assetD1;
    ushort[] _idsA = null!;
    ushort[] _posA = null!;
    ushort[] _idsB = null!;
    ushort[] _posB = null!;
    ushort[] _idsC = null!;
    ushort[] _posC = null!;
    ushort[] _idsD0 = null!;
    ushort[] _idsD1 = null!;
    ushort[] _idsD2 = null!;
    ushort[] _idsD3 = null!;
    ushort[] _posD0 = null!;
    ushort[] _posD1 = null!;
    ushort[] _posD2 = null!;
    ushort[] _posD3 = null!;
    ushort _assetD2;
    ushort _assetD3;
    float[] _oldA = null!;
    float[] _oldBf = null!;
    int[] _oldBi = null!;
    int[] _oldCg = null!;
    float[] _oldCf = null!;
    float[] _oldD0 = null!;
    int[] _oldD1 = null!;
    short[] _oldD2 = null!;
    sbyte[] _oldD3 = null!;

    internal static byte[] BakeA() =>
        new DomainBaker().Track<BTrack, BClip>(new BTrack(2f)).Clip(0, 0u, 40u, new BClip(3f)).Looping().Bake();

    internal static byte[] BakeB() =>
        new DomainBaker().Track<CTrack, CClip>(new CTrack(2f)).Clip(0, 0u, 40u, new CClip(3f)).Looping().Bake();

    internal static byte[] BakeC() =>
        new DomainBaker().Track<DTrack, DClip>(new DTrack(2f)).Clip(0, 0u, 40u, new DClip(3f)).Looping().Bake();

    internal static byte[] BakeD() =>
        new DomainBaker()
            .Track<ETrack, EClip>(new ETrack(2f)).Track<FTrack, FClip>(new FTrack(2f))
            .Track<GTrack, GClip>(new GTrack(2f)).Track<HTrack, HClip>(new HTrack(2f))
            .Clip(0, 0u, 40u, new EClip(3f)).Clip(1, 0u, 40u, new FClip(3f))
            .Clip(2, 0u, 40u, new GClip(3f)).Clip(3, 0u, 40u, new HClip(3f))
            .Looping().Bake();

    internal static byte[] BakeS2H() =>
        new DomainBaker().Track<ETrack, EClip>(new ETrack(2f)).Clip(0, 0u, 40u, new EClip(3f)).Looping().Bake();

    internal static byte[] BakeS2I() =>
        new DomainBaker().Track<FTrack, FClip>(new FTrack(2f)).Clip(0, 0u, 40u, new FClip(3f)).Looping().Bake();

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

    static ViewScope.StepSpec FoldStep(ulong pair, float scale, float amount, uint duration) => new()
    {
        PairKey = pair,
        Track = BitConverter.GetBytes(scale),
        First = BitConverter.GetBytes(amount),
        WindowStart = 0,
        WindowEnd = duration,
        Lane = FoldLane(duration, amount, scale),
    };

    static ViewScope.StepSpec FoldStep2(ulong pair, float scale, float amount, uint duration, int kind) => new()
    {
        PairKey = pair,
        Track = BitConverter.GetBytes(scale),
        First = BitConverter.GetBytes(amount),
        WindowStart = 0,
        WindowEnd = duration,
        Lane = FoldLane2(duration, amount, scale, kind),
    };

    static ViewScope.StepSpec ActionStep(ulong pair, float scale, float amount, uint duration) => new()
    {
        PairKey = pair,
        Track = BitConverter.GetBytes(scale),
        First = BitConverter.GetBytes(amount),
        WindowStart = 0,
        WindowEnd = duration,
    };

    static ViewScope.StepSpec WindowedActionStep(ulong pair, float scale, float amount, uint start, uint end) => new()
    {
        PairKey = pair,
        Track = BitConverter.GetBytes(scale),
        First = BitConverter.GetBytes(amount),
        WindowStart = start,
        WindowEnd = end,
    };
    static byte[] FoldLane(uint window, float amount, float scale)
    {
        var lane = new byte[window * 2 * 4];
        for (uint tick = 0; tick < window; tick++)
            for (var dir = 0; dir < 2; dir++)
            {
                var value = (dir == 1 ? -1f : 1f) * amount * scale;
                MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + (uint)dir) * 4, 4), in value);
            }
        return lane;
    }

    static byte[] FoldLane2(uint window, float amount, float scale, int kind)
    {
        var lane = new byte[window * 2 * 8];
        for (uint tick = 0; tick < window; tick++)
            for (var dir = 0; dir < 2; dir++)
            {
                var lift = (dir == 1 ? -1f : 1f) * amount * scale;
                MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + (uint)dir) * 8, 4), in lift);
                MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + (uint)dir) * 8 + 4, 4), in kind);
            }
        return lane;
    }

    [GlobalSetup]
    public void Setup()
    {
        var saKeys = global::TlSystems.TlSystem_Bench_global__Bench_SA.PairKeys;
        var sbKeys = global::TlSystems.TlSystem_Bench_global__Bench_SB.PairKeys;
        var scKeys = global::TlSystems.TlSystem_Bench_global__Bench_SC.PairKeys;
        var sdKeys = global::TlSystems.TlSystem_Bench_global__Bench_SD.PairKeys;
        var s2Keys = global::TlSystems.TlSystem_Bench_global__Bench_S2.PairKeys;
        var s64Keys = global::TlSystems.TlSystem_Bench_global__Bench_S64.PairKeys;
        _scopeA = new ViewScope();
        _scopeB = new ViewScope();
        _scopeC = new ViewScope();
        _scopeD = new ViewScope();
        _scopeS2 = new ViewScope();
        _scopeS64 = new ViewScope();
        _wA = BuildWorld(_scopeA, new[] { LoopingTimeline(40, FoldStep(saKeys[0], 2f, 3f, 40)) });
        _wB = BuildWorld(_scopeB, new[] { LoopingTimeline(40, FoldStep2(sbKeys[0], 2f, 3f, 40, 30)) });
        _wC = BuildWorld(_scopeC, new[] { LoopingTimeline(40, ActionStep(scKeys[0], 2f, 3f, 40)) });
        _wD = BuildWorld(_scopeD, new[] { LoopingTimeline(40, FoldStep(sdKeys[0], 2f, 3f, 40), FoldStep(sdKeys[1], 2f, 3f, 40), FoldStep(sdKeys[2], 2f, 3f, 40), FoldStep(sdKeys[3], 2f, 3f, 40)) });
        _wS2 = BuildWorld(_scopeS2, new[] { LoopingTimeline(40, ActionStep(s2Keys[0], 2f, 3f, 40)), LoopingTimeline(40, ActionStep(s2Keys[1], 2f, 3f, 40)) });
        var s64Timelines = new ViewScope.TimelineSpec[8];
        for (var i = 0; i < 8; i++)
        {
            var steps = new ViewScope.StepSpec[8];
            for (var k = 0; k < 8; k++) steps[k] = WindowedActionStep(s64Keys[i * 8], 1f, i, (uint)(k * 5), (uint)(k * 5 + 5));
            s64Timelines[i] = LoopingTimeline(40, steps);
        }
        _wS64 = BuildWorld(_scopeS64, s64Timelines);

        _pA = new PlaybackTable(N);
        _pB = new PlaybackTable(N);
        _pC = new PlaybackTable(N);
        _pD = new PlaybackTable(N);
        _pS2 = new PlaybackTable(N);
        _pS2One = new PlaybackTable(N);
        _pS64 = new PlaybackTable(N);

        _fxA = new float[N];
        _fxB = new float[N];
        _ixB = new int[N];
        _gainC = new int[N];
        _fxC = new float[N];
        _d0 = new float[N];
        _d1 = new int[N];
        _d2 = new short[N];
        _d3 = new sbyte[N];
        _s2x = new int[N];
        _s2y = new short[N];
        for (var i = 0; i < N; i++) _gainC[i] = 1 + i % 4;

        _assetA = TimelineAsset.Load(BakeA());
        _assetB = TimelineAsset.Load(BakeB());
        _assetC = TimelineAsset.Load(BakeC());
        _assetD0 = TimelineAsset.Load(new DomainBaker().Track<ETrack, EClip>(new ETrack(2f)).Clip(0, 0u, 40u, new EClip(3f)).Looping().Bake());
        _assetD1 = TimelineAsset.Load(new DomainBaker().Track<FTrack, FClip>(new FTrack(2f)).Clip(0, 0u, 40u, new FClip(3f)).Looping().Bake());
        _assetD2 = TimelineAsset.Load(new DomainBaker().Track<GTrack, GClip>(new GTrack(2f)).Clip(0, 0u, 40u, new GClip(3f)).Looping().Bake());
        _assetD3 = TimelineAsset.Load(new DomainBaker().Track<HTrack, HClip>(new HTrack(2f)).Clip(0, 0u, 40u, new HClip(3f)).Looping().Bake());
        _idsA = Filled(_assetA);
        _posA = new ushort[N];
        _idsB = Filled(_assetB);
        _posB = new ushort[N];
        _idsC = Filled(_assetC);
        _posC = new ushort[N];
        _idsD0 = Filled(_assetD0);
        _idsD1 = Filled(_assetD1);
        _idsD2 = Filled(_assetD2);
        _idsD3 = Filled(_assetD3);
        _posD0 = new ushort[N];
        _posD1 = new ushort[N];
        _posD2 = new ushort[N];
        _posD3 = new ushort[N];
        _oldA = new float[N];
        _oldBf = new float[N];
        _oldBi = new int[N];
        _oldCg = ArrayCopy(_gainC);
        _oldCf = new float[N];
        _oldD0 = new float[N];
        _oldD1 = new int[N];
        _oldD2 = new short[N];
        _oldD3 = new sbyte[N];

        ResetNew(_pA, _wA, 1);
        ResetNew(_pB, _wB, 1);
        ResetNew(_pC, _wC, 1);
        ResetNew(_pD, _wD, 1);
        ResetNew(_pS2, _wS2, 1, 2);
        ResetNew(_pS2One, _wS2, 1);
        ResetS64();
    }

    static ushort[] Filled(ushort asset)
    {
        var ids = new ushort[N];
        Array.Fill(ids, asset);
        return ids;
    }

    static int[] ArrayCopy(int[] source)
    {
        var copy = new int[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    static byte[] BakePair<TTrack, TClip>(TTrack track, TClip clip)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged =>
        new DomainBaker().Track<TTrack, TClip>(track).Clip(0, 0u, 40u, clip).Looping().Bake();

    static void ResetNew(PlaybackTable players, TimelineWorld world, params ushort[] timelines)
    {
        for (var e = 0; e < N; e++) players.StopAll(e);
        world.ApplyRequests(players);
        for (var e = 0; e < N; e++) foreach (var timeline in timelines) players.Start(e, timeline);
        world.ApplyRequests(players);
    }

    void ResetS64()
    {
        for (var e = 0; e < N; e++) _pS64.StopAll(e);
        _wS64.ApplyRequests(_pS64);
        for (var e = 0; e < N; e++)
        {
            _pS64.Start(e, (ushort)((e % 8) + 1));
            _pS64.Start(e, (ushort)(((e + 5) % 8) + 1));
        }
        _wS64.ApplyRequests(_pS64);
    }

    // Steady-state only: playbacks persist across iterations so the dispatch
    // cache stays hot (this is the measured warm path). Smoke calls Reset once.
    public void Reset()
    {
        Array.Fill(_posA, (ushort)0);
        Array.Fill(_posB, (ushort)0);
        Array.Fill(_posC, (ushort)0);
        Array.Fill(_posD0, (ushort)0);
        Array.Fill(_posD1, (ushort)0);
        Array.Fill(_posD2, (ushort)0);
        Array.Fill(_posD3, (ushort)0);
        ResetNew(_pA, _wA, 1);
        ResetNew(_pB, _wB, 1);
        ResetNew(_pC, _wC, 1);
        ResetNew(_pD, _wD, 1);
        ResetNew(_pS2, _wS2, 1, 2);
        ResetS64();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pA.Dispose();
        _pB.Dispose();
        _pC.Dispose();
        _pD.Dispose();
        _pS2.Dispose();
        _pS2One.Dispose();
        _pS64.Dispose();
        _wA.Dispose();
        _wB.Dispose();
        _wC.Dispose();
        _wD.Dispose();
        _wS2.Dispose();
        _wS64.Dispose();
        _scopeA.Dispose();
        _scopeB.Dispose();
        _scopeC.Dispose();
        _scopeD.Dispose();
        _scopeS2.Dispose();
        _scopeS64.Dispose();
    }

    [Benchmark]
    public void OldA()
    {
        var set = new ColumnSet();
        set.Add<float>(_oldA);
        for (var f = 0; f < Frames; f++)
        {
            Timeline<BTrack, BClip>.Apply(_idsA, _posA, true, in set);
            Timeline.Advance(_assetA, _posA, true);
        }
        Sink += _oldA[0] + _oldA[N - 1];
    }

    [Benchmark]
    public void NewA()
    {
        var system = new SA { Fx = _fxA };
        for (var f = 0; f < Frames; f++)
        {
            _wA.ApplyRequests(_pA);
            system.Run(_wA, _pA, true);
            _wA.Advance(_pA, true);
        }
        Sink += _fxA[0] + _fxA[N - 1];
    }

    [Benchmark]
    public void NewABackward()
    {
        var system = new SA { Fx = _fxA };
        for (var f = 0; f < Frames; f++)
        {
            _wA.ApplyRequests(_pA);
            system.Run(_wA, _pA, false);
            _wA.Advance(_pA, false);
        }
        Sink += _fxA[0] + _fxA[N - 1];
    }

    [Benchmark]
    public void OldB()
    {
        var set = new ColumnSet();
        set.Add<float>(_oldBf);
        set.Add<int>(_oldBi);
        for (var f = 0; f < Frames; f++)
        {
            Timeline<CTrack, CClip>.Apply(_idsB, _posB, true, in set);
            Timeline.Advance(_assetB, _posB, true);
        }
        Sink += _oldBf[0] + _oldBi[N - 1];
    }

    [Benchmark]
    public void NewB()
    {
        var system = new SB { Fx = _fxB, Ix = _ixB };
        for (var f = 0; f < Frames; f++)
        {
            _wB.ApplyRequests(_pB);
            system.Run(_wB, _pB, true);
            _wB.Advance(_pB, true);
        }
        Sink += _fxB[0] + _ixB[N - 1];
    }

    [Benchmark]
    public void OldC()
    {
        var set = new ColumnSet();
        set.Add<int>(_oldCg);
        set.Add<float>(_oldCf);
        for (var f = 0; f < Frames; f++)
        {
            Timeline<DTrack, DClip>.Apply(_idsC, _posC, true, in set);
            Timeline.Advance(_assetC, _posC, true);
        }
        Sink += _oldCf[0] + _oldCg[N - 1];
    }

    [Benchmark]
    public void NewC()
    {
        var system = new SC { C = _gainC, F = _fxC };
        for (var f = 0; f < Frames; f++)
        {
            _wC.ApplyRequests(_pC);
            system.Run(_wC, _pC, true);
            _wC.Advance(_pC, true);
        }
        Sink += _fxC[0] + _gainC[N - 1];
    }

    [Benchmark]
    public void OldD()
    {
        var set0 = new ColumnSet();
        set0.Add<float>(_oldD0);
        var set1 = new ColumnSet();
        set1.Add<int>(_oldD1);
        var set2 = new ColumnSet();
        set2.Add<short>(_oldD2);
        var set3 = new ColumnSet();
        set3.Add<sbyte>(_oldD3);
        for (var f = 0; f < Frames; f++)
        {
            Timeline<ETrack, EClip>.Apply(_idsD0, _posD0, true, in set0);
            Timeline<FTrack, FClip>.Apply(_idsD1, _posD1, true, in set1);
            Timeline<GTrack, GClip>.Apply(_idsD2, _posD2, true, in set2);
            Timeline<HTrack, HClip>.Apply(_idsD3, _posD3, true, in set3);
            Timeline.Advance(_assetD0, _posD0, true);
            Timeline.Advance(_assetD1, _posD1, true);
            Timeline.Advance(_assetD2, _posD2, true);
            Timeline.Advance(_assetD3, _posD3, true);
        }
        Sink += _oldD0[0] + _oldD1[N - 1] + _oldD2[0] + _oldD3[N - 1];
    }

    [Benchmark]
    public void NewD()
    {
        var system = new SD { D0 = _d0, D1 = _d1, D2 = _d2, D3 = _d3 };
        for (var f = 0; f < Frames; f++)
        {
            _wD.ApplyRequests(_pD);
            system.Run(_wD, _pD, true);
            _wD.Advance(_pD, true);
        }
        Sink += _d0[0] + _d1[N - 1] + _d2[0] + _d3[N - 1];
    }

    [Benchmark]
    public void S2OnePlayback()
    {
        var system = new S2 { X = _s2x, Y = _s2y };
        for (var f = 0; f < Frames; f++)
        {
            _wS2.ApplyRequests(_pS2One);
            system.Run(_wS2, _pS2One, true);
            _wS2.Advance(_pS2One, true);
        }
        Sink += _s2x[0] + _s2y[N - 1];
    }
    [Benchmark]
    public void S2TwoPlaybacks()
    {
        var system = new S2 { X = _s2x, Y = _s2y };
        for (var f = 0; f < Frames; f++)
        {
            _wS2.ApplyRequests(_pS2);
            system.Run(_wS2, _pS2, true);
            _wS2.Advance(_pS2, true);
        }
        Sink += _s2x[0] + _s2y[N - 1];
    }

    internal long LastTicks;

    [Benchmark]
    public void S64TwoActive()
    {
        var system = new S64 { Ctx = new Shared(0) };
        for (var f = 0; f < Frames; f++)
        {
            _wS64.ApplyRequests(_pS64);
            system.Run(_wS64, _pS64, true);
            _wS64.Advance(_pS64, true);
        }
        LastTicks = system.Ctx.Ticks;
        Sink += system.Ctx.Ticks + _s64ticks;
        _s64ticks = system.Ctx.Ticks;
    }

    internal ulong Checksum()
    {
        var hash = 14695981039346656037ul;
        hash = Mix(hash, _fxA);
        hash = Mix(hash, _fxB);
        hash = Mix(hash, _ixB);
        hash = Mix(hash, _gainC);
        hash = Mix(hash, _fxC);
        hash = Mix(hash, _d0);
        hash = Mix(hash, _d1);
        hash = Mix(hash, _d2);
        hash = Mix(hash, _d3);
        hash = Mix(hash, _s2x);
        hash = Mix(hash, _s2y);
        return hash;
    }

    static ulong Mix(ulong hash, Array array)
    {
        foreach (var value in array)
        {
            var bits = BitConverter.DoubleToInt64Bits(Convert.ToDouble(value));
            hash = (hash ^ (ulong)(bits & 0xffffffff)) * 1099511628211ul;
            hash = (hash ^ (ulong)(((long)bits) >> 32)) * 1099511628211ul;
        }
        return hash;
    }
}
