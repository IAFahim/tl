using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Bench;
using Tl;
using Tl.Systems.Tests;

namespace Kinds;

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
    public const int TimelinesC = 100;
    public double Sink;

    ViewScope _scopeA = null!;
    ViewScope _scopeC = null!;
    TimelineWorld _planA = null!;
    TimelineWorld _planC = null!;
    PlaybackTable _playersA = null!;
    PlaybackTable _playersC = null!;
    float[] _planFxA = null!;
    float[] _planFxC = null!;

    Library _libA = null!;
    Library _libC = null!;
    Library _libD = null!;
    Library _libE2 = null!;
    Library _libE64 = null!;
    Library _libEmpty = null!;
    World _a = null!;
    World _b = null!;
    World _c = null!;
    World _d = null!;
    World _e2 = null!;
    World _e64 = null!;
    World _e64Blocked = null!;
    World _f1 = null!;
    World _f8 = null!;
    ColumnSet _colsA = null!;
    ColumnSet _colsB = null!;
    ColumnSet _colsC = null!;
    ColumnSet _colsE2 = null!;
    ColumnSet _colsE64 = null!;
    ColumnSet _colsE64Blocked = null!;
    ColumnSet _colsF1 = null!;
    ColumnSet _colsF8 = null!;

    static ViewScope.TimelineSpec PlanTimeline(ulong pair, float amount) => new()
    {
        Loop = 1,
        Duration = 40,
        Steps =
        {
            new ViewScope.StepSpec
            {
                PairKey = pair,
                Track = BitConverter.GetBytes(2f),
                First = BitConverter.GetBytes(amount),
                WindowStart = 0,
                WindowEnd = 40,
                Lane = PlanLane(40, amount, 2f),
            },
        },
    };

    static byte[] PlanLane(uint window, float amount, float scale)
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

    static TimelineWorld PlanWorld(ViewScope scope, IList<ViewScope.TimelineSpec> timelines)
    {
        scope.Build(timelines);
        var world = new TimelineWorld();
        scope.InjectInto(world);
        GC.SuppressFinalize(world);
        return world;
    }

    static float AmountOf(int timeline) => 1 + timeline % 7;

    static int BlockOf(int e) => (int)((long)e * 32 / N);

    static int TimelineOfC(int e) => (int)((uint)(e * 2654435761u) % TimelinesC);

    static Dictionary<byte, (int, int)> Shapes(int kinds, int inline, int valueBytes)
    {
        var d = new Dictionary<byte, (int, int)>();
        for (var k = 0; k < kinds; k++) d[(byte)k] = (inline, valueBytes);
        return d;
    }

    [GlobalSetup]
    public void Setup()
    {
        var pair = global::TlSystems.TlSystem_Bench_global__Bench_SA.PairKeys[0];
        _scopeA = new ViewScope();
        _scopeC = new ViewScope();
        _planA = PlanWorld(_scopeA, new[] { PlanTimeline(pair, 3f) });
        var planC = new ViewScope.TimelineSpec[TimelinesC];
        for (var t = 0; t < TimelinesC; t++) planC[t] = PlanTimeline(pair, AmountOf(t));
        _planC = PlanWorld(_scopeC, planC);
        _playersA = new PlaybackTable(N);
        _playersC = new PlaybackTable(N);
        _planFxA = new float[N];
        _planFxC = new float[N];
        for (var e = 0; e < N; e++)
        {
            _playersA.Start(e, 1);
            _playersC.Start(e, (ushort)(TimelineOfC(e) + 1), (ushort)(e % 40));
        }
        _planA.ApplyRequests(_playersA);
        _planC.ApplyRequests(_playersC);

        _libA = new Library(new[] { Scripts.Walk(40, 3f, 2f, KindId.Fx) });
        var c = new TimelineSpec[TimelinesC];
        for (var t = 0; t < TimelinesC; t++) c[t] = Scripts.Walk(40, AmountOf(t), 2f, KindId.Fx);
        _libC = new Library(c);
        _libD = new Library(new[] { Scripts.Walk(65535, 3f, 2f, KindId.Fx) });
        var e2 = new TimelineSpec[32];
        for (var j = 0; j < 32; j++) e2[j] = new TimelineSpec { Duration = 40, Loop = true }.Add(0, 40, 0, ClipMode.Span, 3f, 2f).Add(0, 40, 1, ClipMode.Span, 3f, 2f);
        _libE2 = new Library(e2);
        var e64 = new TimelineSpec[32];
        for (var j = 0; j < 32; j++) e64[j] = new TimelineSpec { Duration = 40, Loop = true }.Add(0, 40, (byte)(2 * j), ClipMode.Span, 3f, 2f).Add(0, 40, (byte)(2 * j + 1), ClipMode.Span, 3f, 2f);
        _libE64 = new Library(e64);
        _libEmpty = new Library(new[] { new TimelineSpec { Duration = 1 } });

        var fx = Shapes(5, 1, 0);
        _a = new World(_libA, N, _ => 1ul << KindId.Fx, fx, KindId.Damage);
        _b = new World(_libA, N, _ => 1ul << KindId.Fx, fx, KindId.Damage);
        _c = new World(_libC, N, _ => 1ul << KindId.Fx, fx, KindId.Damage);
        _d = new World(_libD, N, _ => 1ul << KindId.Fx, fx, KindId.Damage);
        _e2 = new World(_libE2, N, _ => 0b11, Shapes(2, 1, 0), 63);
        _e64 = new World(_libE64, N, e => 0b11ul << (2 * (e % 32)), Shapes(64, 1, 0), 63);
        _e64Blocked = new World(_libE64, N, e => 0b11ul << (2 * BlockOf(e)), Shapes(64, 1, 0), 63);
        _f1 = new World(_libEmpty, N, _ => 1ul << KindId.Damage, Shapes(5, 1, 4), KindId.Damage);
        _f8 = new World(_libEmpty, N, _ => 1ul << KindId.Damage, Shapes(5, 8, 4), KindId.Damage);
        _colsA = new ColumnSet(N);
        _colsB = new ColumnSet(N);
        _colsC = new ColumnSet(N);
        _colsE2 = new ColumnSet(N);
        _colsE64 = new ColumnSet(N);
        _colsE64Blocked = new ColumnSet(N);
        _colsF1 = new ColumnSet(N);
        _colsF8 = new ColumnSet(N);
        for (var e = 0; e < N; e++)
        {
            _a.Playbacks.Start(e, 1);
            if (e % 10 == 0) _b.Playbacks.Start(e, 1);
            _c.Playbacks.Start(e, (ushort)(TimelineOfC(e) + 1), (ushort)(e % 40));
            _d.Playbacks.Start(e, 1, (ushort)(1000 + e % 40));
            _e2.Playbacks.Start(e, (ushort)(e % 32 + 1));
            _e64.Playbacks.Start(e, (ushort)(e % 32 + 1));
            _e64Blocked.Playbacks.Start(e, (ushort)(BlockOf(e) + 1));
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var w in new[] { _a, _b, _c, _d, _e2, _e64, _e64Blocked, _f1, _f8 }) w.Dispose();
        foreach (var l in new[] { _libA, _libC, _libD, _libE2, _libE64, _libEmpty }) l.Dispose();
        foreach (var s in new[] { _colsA, _colsB, _colsC, _colsE2, _colsE64, _colsE64Blocked, _colsF1, _colsF8 }) s.Dispose();
        _playersA.Dispose();
        _playersC.Dispose();
        _planA.Dispose();
        _planC.Dispose();
        _scopeA.Dispose();
        _scopeC.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void PlanOneA()
    {
        var system = new SA { Fx = _planFxA };
        for (var f = 0; f < Frames; f++)
        {
            _planA.ApplyRequests(_playersA);
            system.Run(_planA, _playersA, true);
            _planA.Advance(_playersA, true);
        }
        Sink += _planFxA[0] + _planFxA[N - 1];
    }

    [Benchmark]
    public void PlanOneC()
    {
        var system = new SA { Fx = _planFxC };
        for (var f = 0; f < Frames; f++)
        {
            _planC.ApplyRequests(_playersC);
            system.Run(_planC, _playersC, true);
            _planC.Advance(_playersC, true);
        }
        Sink += _planFxC[0] + _planFxC[N - 1];
    }

    [Benchmark]
    public void KindsA() => FxFrames(_a, _colsA);

    [Benchmark]
    public void KindsB() => FxFrames(_b, _colsB);

    [Benchmark]
    public void KindsC() => FxFrames(_c, _colsC);

    [Benchmark]
    public void KindsDTickOnly()
    {
        for (var f = 0; f < Frames; f++) _d.Tick(1);
        Sink += _d.Playbacks.SlotOf(0, 0)->Tick;
    }

    [Benchmark]
    public void KindsE2()
    {
        var c = _colsE2.Columns;
        for (var f = 0; f < Frames; f++)
        {
            _e2.Tick(1);
            _e2.RunKind<Fx, float>(_e2.Tables[0], c, 1);
            _e2.RunKind<Fx, float>(_e2.Tables[1], c, 1);
        }
        Sink += c.Fx[0] + c.Fx[N - 1];
    }

    [Benchmark]
    public void KindsE64()
    {
        var c = _colsE64.Columns;
        for (var f = 0; f < Frames; f++)
        {
            _e64.Tick(1);
            for (var k = 0; k < 64; k++) _e64.RunKind<Fx, float>(_e64.Tables[k], c, 1);
        }
        Sink += c.Fx[0] + c.Fx[N - 1];
    }

    [Benchmark]
    public void KindsE64Blocked()
    {
        var c = _colsE64Blocked.Columns;
        for (var f = 0; f < Frames; f++)
        {
            _e64Blocked.Tick(1);
            for (var k = 0; k < 64; k++) _e64Blocked.RunKind<Fx, float>(_e64Blocked.Tables[k], c, 1);
        }
        Sink += c.Fx[0] + c.Fx[N - 1];
    }

    [Benchmark]
    public void KindsF1() => DamageFrames(_f1, _colsF1, 1);

    [Benchmark]
    public void KindsF8() => DamageFrames(_f8, _colsF8, 8);

    void FxFrames(World world, ColumnSet cols)
    {
        var c = cols.Columns;
        var table = world.Tables[KindId.Fx];
        for (var f = 0; f < Frames; f++)
        {
            world.Tick(1);
            world.RunKind<Fx, float>(table, c, 1);
        }
        Sink += c.Fx[0] + c.Fx[N - 1];
    }

    void DamageFrames(World world, ColumnSet cols, int pulses)
    {
        var c = cols.Columns;
        var table = world.Tables[KindId.Damage];
        for (var f = 0; f < Frames; f++)
        {
            for (var e = 0; e < N; e++)
                for (var k = 0; k < pulses; k++) world.Send(e, 1);
            world.Tick<Damage>(1);
            world.RunKind<Damage, int>(table, c, 1);
        }
        Sink += c.Health[0] + c.Health[N - 1];
    }

    public bool ParityA()
    {
        for (var e = 0; e < N; e++)
            if (BitConverter.SingleToInt32Bits(_planFxA[e]) != BitConverter.SingleToInt32Bits(_colsA.Fx[e])) return false;
        return true;
    }

    public bool ParityC()
    {
        for (var e = 0; e < N; e++)
            if (BitConverter.SingleToInt32Bits(_planFxC[e]) != BitConverter.SingleToInt32Bits(_colsC.Fx[e])) return false;
        return true;
    }

    public long BytesPerEntity(string world, int entity) => world switch
    {
        "E2" => _e2.KindBytesPerEntity(entity),
        "E64" => _e64.KindBytesPerEntity(entity),
        _ => _a.KindBytesPerEntity(entity),
    };

    public int PlayingB()
    {
        var count = 0;
        for (var w = 0; w < _b.Playbacks.Words; w++) count += System.Numerics.BitOperations.PopCount(_b.Playbacks.Playing[w]);
        return count;
    }

    public (int max, int overflows) DamageStats(bool eight) => eight ? (_f8.Tables[KindId.Damage]!.MaxLength, _f8.Tables[KindId.Damage]!.Overflows) : (_f1.Tables[KindId.Damage]!.MaxLength, _f1.Tables[KindId.Damage]!.Overflows);
}

public static class Smoke
{
    public static int Profile(string shape)
    {
        var bench = new KindBenchmarks();
        bench.Setup();
        var run = typeof(KindBenchmarks).GetMethod(shape)!.CreateDelegate<Action>(bench);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var ops = 0;
        while (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds < 8)
        {
            run();
            ops++;
        }
        Console.WriteLine($"PROFILE {shape} ops={ops} ms/op={8000.0 / ops:F2}");
        bench.Cleanup();
        return 0;
    }

    public static int Run()
    {
        var bench = new KindBenchmarks();
        bench.Setup();
        var shapes = new (string name, Action run)[]
        {
            ("PlanOneA", bench.PlanOneA),
            ("PlanOneC", bench.PlanOneC),
            ("KindsA", bench.KindsA),
            ("KindsB", bench.KindsB),
            ("KindsC", bench.KindsC),
            ("KindsDTickOnly", bench.KindsDTickOnly),
            ("KindsE2", bench.KindsE2),
            ("KindsE64", bench.KindsE64),
            ("KindsE64Blocked", bench.KindsE64Blocked),
            ("KindsF1", bench.KindsF1),
            ("KindsF8", bench.KindsF8),
        };
        foreach (var (_, run) in shapes)
            for (var w = 0; w < 30; w++) run();
        foreach (var (name, run) in shapes)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var r = 0; r < 20; r++) run();
            var ms = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds / 20;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"SMOKE {name} frames={KindBenchmarks.Frames} entities={KindBenchmarks.N} ms/op={ms:F2} bytes/20ops={bytes}");
        }
        var parityA = bench.ParityA();
        var parityC = bench.ParityC();
        Console.WriteLine($"SMOKE parityA={parityA} parityC={parityC}");
        Console.WriteLine($"SMOKE kindBytesPerEntity A={bench.BytesPerEntity("A", 0)} E2={bench.BytesPerEntity("E2", 0)} E64={bench.BytesPerEntity("E64", 0)}");
        Console.WriteLine($"SMOKE playingB={bench.PlayingB()} f1={bench.DamageStats(false)} f8={bench.DamageStats(true)}");
        bench.Cleanup();
        return parityA && parityC ? 0 : 1;
    }
}
