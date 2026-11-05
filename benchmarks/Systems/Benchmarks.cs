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

        ResetNew(_pA, _wA, 1);
        ResetNew(_pB, _wB, 1);
        ResetNew(_pC, _wC, 1);
        ResetNew(_pD, _wD, 1);
        ResetNew(_pS2, _wS2, 1, 2);
        ResetNew(_pS2One, _wS2, 1);
        ResetS64();
    }

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
