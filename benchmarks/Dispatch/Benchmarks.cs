using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using System.Runtime.CompilerServices;

namespace Tl.Hooks;

public sealed class Config : ManualConfig
{
    public Config()
    {
        var job = Job.Default.WithWarmupCount(16).WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        AddJob(job.WithId("Jit"));
        AddJob(job.WithId("NoTiering").WithEnvironmentVariable("DOTNET_TieredCompilation", "0"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(Config))]
public class Dispatch
{
    public const int Operations = 65536;
    private Frame[] _frames = null!;
    private Receiver _receiver;
    private ITimelineForward _boxed = null!;
    private ITimelineForward _other = null!;
    private static readonly ForwardAction Callback = static (ref Receiver receiver, in Frame frame) => receiver.OnForward(in frame);

    [GlobalSetup]
    public void Setup()
    {
        _frames = new Frame[Operations];
        _receiver.Value = 0.731f;
        _boxed = new Receiver { Value = _receiver.Value };
        _other = new OtherReceiver { Data = new Receiver { Value = _receiver.Value } };
        uint random = 0xA312AFD5;
        for (var i = 0; i < _frames.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _frames[i] = new Frame((int)(random & 65535), (random >> 16) / 65535f);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt Direct()
    {
        var receiver = new Receiver { Value = _receiver.Value };
        foreach (ref readonly var frame in _frames.AsSpan())
            receiver.OnForward(in frame);
        return receiver.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt GeneratedLink()
    {
        var receiver = new Receiver { Value = _receiver.Value };
        foreach (ref readonly var frame in _frames.AsSpan())
            ReceiverLink.Forward(ref receiver, in frame);
        return receiver.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt Constrained()
    {
        var receiver = new Receiver { Value = _receiver.Value };
        foreach (ref readonly var frame in _frames.AsSpan())
            Calls.Constrained(ref receiver, in frame);
        return receiver.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ExplicitConstrained()
    {
        var receiver = new ExplicitReceiver { Data = new Receiver { Value = _receiver.Value } };
        foreach (ref readonly var frame in _frames.AsSpan())
            Calls.Constrained(ref receiver, in frame);
        return receiver.Data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt CachedDelegate()
    {
        var receiver = new Receiver { Value = _receiver.Value };
        foreach (ref readonly var frame in _frames.AsSpan())
            Callback(ref receiver, in frame);
        return receiver.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt BoxedOnce()
    {
        ref var receiver = ref Unsafe.Unbox<Receiver>(_boxed);
        receiver = new Receiver { Value = _receiver.Value };
        foreach (ref readonly var frame in _frames.AsSpan())
            Calls.Boxed(_boxed, in frame);
        return receiver.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt MixedBoxes()
    {
        ref var first = ref Unsafe.Unbox<Receiver>(_boxed);
        ref var second = ref Unsafe.Unbox<OtherReceiver>(_other);
        first = new Receiver { Value = _receiver.Value };
        second = new OtherReceiver { Data = first };
        var index = 0;
        foreach (ref readonly var frame in _frames.AsSpan())
            Calls.Boxed((index++ & 1) == 0 ? _boxed : _other, in frame);
        return new Receipt(first.Sum + second.Data.Sum, first.Ticks + second.Data.Ticks, first.Count + second.Data.Count);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public long EscapingBoxPerCall()
    {
        long count = 0;
        foreach (ref readonly var frame in _frames.AsSpan())
        {
            _boxed = new Receiver { Value = _receiver.Value };
            Calls.Boxed(_boxed, in frame);
            count += Unsafe.Unbox<Receiver>(_boxed).Count;
        }
        return count;
    }
}

[Config(typeof(Config))]
public class DataFlow
{
    public const int Operations = 65536;
    private Frame[] _frames = null!;

    [GlobalSetup]
    public void Setup()
    {
        _frames = new Frame[Operations];
        uint random = 0xA312AFD5;
        for (var i = 0; i < _frames.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _frames[i] = new Frame((int)(random & 65535), (random >> 16) / 65535f);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt DirectData()
    {
        var player = new Player { Health = 1_000_000f };
        foreach (ref readonly var frame in _frames.AsSpan())
            player.OnHit(new Hit(frame.Tick & 1023));
        return player.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt RefGenericData()
    {
        var player = new Player { Health = 1_000_000f };
        foreach (ref readonly var frame in _frames.AsSpan())
            DamageTimeline.Update(in frame, ref player);
        return player.Result;
    }
}

[Config(typeof(Config))]
public class DimTrap
{
    public const int Operations = 65536;

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public int Overridden()
    {
        var player = new BusyPlayer();
        for (var i = 0; i < Operations; i++)
            IdleCalls.Dim(ref player);
        return player.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public int NotOverridden()
    {
        var player = new IdlePlayer();
        for (var i = 0; i < Operations; i++)
            IdleCalls.Dim(ref player);
        return player.Count;
    }
}

[Config(typeof(Config))]
public class ApiShape
{
    public const int Operations = 65536;
    private uint[] _ticks = null!;
    private ushort _timeline;

    [GlobalSetup]
    public void Setup()
    {
        _timeline = BuildTimeline();
        _ticks = new uint[Operations];
        uint random = 0xB19F4C27;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = random % 600;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt DirectTicks()
    {
        var data = new Vitals { Health = 1_000_000f };
        var blender = new VitalsTrack();
        foreach (var tick in _ticks.AsSpan())
        {
            var starts = VitalsTrack.RegionStarts;
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;

            var row = VitalsTrack.RegionRows[region];
            for (var t = 0; t < row.TrackCount; t++)
            {
                var track = VitalsTrack.TrackRows[row.TrackStart + t];
                var first = VitalsTrack.ClipRows[track.ClipStart];

                if (track.ClipCount == 1)
                {
                    data.Ticks += VitalsTrack.TrackData[track.TrackIndex].Offset;
                    data.Health += VitalsTrack.ClipData[first.ClipIndex].Amount;
                }
                else
                {
                    var second = VitalsTrack.ClipRows[track.ClipStart + 1];
                    var factor = (tick - first.FactorStart) / (float)(first.FactorLength - 1);
                    blender.Blend(
                        in VitalsTrack.ClipData[first.ClipIndex],
                        in VitalsTrack.ClipData[second.ClipIndex],
                        factor,
                        out var resolved);

                    data.Ticks += VitalsTrack.TrackData[track.TrackIndex].Offset;
                    data.Health += resolved.Amount;
                }
            }

            data.Count++;
        }
        return data.Result;
    }

    // The same fixture the static tables encode, authored at run time inside
    // a Build callback; only the assigned ushort index escapes.
    public ushort BuildTimeline(bool loops = false)
        => Timeline<VitalsTrack, VitalsClip>.Build(b =>
        {
            b.Track(new VitalsTrack(1));
            b.Track(new VitalsTrack(2));
            b.Track(new VitalsTrack(3));
            b.Track(new VitalsTrack(4));

            b.Clip(0, new VitalsClip(1f), 0, 7);
            b.Clip(0, new VitalsClip(13f), 29, 47);
            b.Clip(0, new VitalsClip(21f), 29, 47);
            b.Clip(0, new VitalsClip(13f), 47, 76);
            b.Clip(0, new VitalsClip(1f), 321, 515);

            b.Clip(1, new VitalsClip(2f), 3, 7);
            b.Clip(1, new VitalsClip(3f), 3, 11);
            b.Clip(1, new VitalsClip(2f), 47, 76);
            b.Clip(1, new VitalsClip(2f), 76, 123);
            b.Clip(1, new VitalsClip(3f), 515, 600);

            b.Clip(2, new VitalsClip(8f), 18, 29);
            b.Clip(2, new VitalsClip(8f), 76, 123);
            b.Clip(2, new VitalsClip(8f), 321, 515);

            b.Clip(3, new VitalsClip(5f), 3, 11);
            b.Clip(3, new VitalsClip(5f), 18, 29);
            b.Clip(3, new VitalsClip(34f), 76, 123);
            b.Clip(3, new VitalsClip(55f), 76, 123);
            b.Clip(3, new VitalsClip(5f), 200, 321);
            b.Clip(3, new VitalsClip(5f), 321, 515);

            if (loops)
                b.Looping();
        });

    public Receipt RunInstance(ushort timeline)
    {
        var data = new Vitals { Health = 1_000_000f };
        foreach (var tick in _ticks.AsSpan())
            Timeline<VitalsTrack, VitalsClip>.Forward(timeline, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt InstanceSingle()
    {
        var data = new Vitals { Health = 1_000_000f };
        foreach (var tick in _ticks.AsSpan())
            Timeline<VitalsTrack, VitalsClip>.Forward(_timeline, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt InstanceParamsFour()
    {
        var data = new Vitals { Health = 1_000_000f };
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            Timeline<VitalsTrack, VitalsClip>.Forward(_timeline, ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ShellSingle()
    {
        var data = new Vitals { Health = 1_000_000f };
        foreach (var tick in _ticks.AsSpan())
            GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ShellParamsFour()
    {
        var data = new Vitals { Health = 1_000_000f };
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    // The Playback path: same sampling, plus movement facts folded into the
    // returned 8-byte status word. Receipts must still match DirectTicks.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackSingle()
    {
        var data = new Vitals { Health = 1_000_000f };
        var pb = Playback.Start();
        foreach (var tick in _ticks.AsSpan())
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pb, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackParamsFour()
    {
        var data = new Vitals { Health = 1_000_000f };
        var pb = Playback.Start();
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pb, ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackBackwardSingle()
    {
        var data = new Vitals { Health = 1_000_000f };
        var pb = Playback.Start();
        foreach (var tick in _ticks.AsSpan())
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pb, ref data, tick);
        return data.Result;
    }

    // Clip-level hooks: TClip itself receives one call per active clip.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ClipHooksSingle()
    {
        var data = new Vitals { Health = 1_000_000f };
        var pb = Playback.Start();
        foreach (var tick in _ticks.AsSpan())
            pb = ClipTimeline<VitalsTrack, VitalsClip>.Forward(in pb, ref data, tick);
        return data.Result;
    }
}

// The 8-byte Playback claim: `in`, by-value, and `ref` passing of one qword
// should be indistinguishable.
[Config(typeof(Config))]
public class PassPlayback
{
    private Playback _pass = new(123, 456, PlaybackFlags.Active);
    private ulong _sink;

    private static Playback StepByValue(Playback p) => new(p.Tick + 1, p.Cycles, p.Flags);
    private static Playback StepIn(in Playback p) => new(p.Tick + 1, p.Cycles, p.Flags);
    private static void StepRef(ref Playback p) => p = new Playback(p.Tick + 1, p.Cycles, p.Flags);

    [Benchmark(Baseline = true)]
    public ulong PassByValue()
    {
        var p = _pass;
        p = StepByValue(p);
        return _sink = p.Tick;
    }

    [Benchmark]
    public ulong PassIn()
    {
        var p = _pass;
        p = StepIn(in p);
        return _sink = p.Tick;
    }

    [Benchmark]
    public ulong PassRef()
    {
        var p = _pass;
        StepRef(ref p);
        return _sink = p.Tick;
    }
}

[DisassemblyDiagnoser(maxDepth: 3, printSource: true, exportCombinedDisassemblyReport: true)]
[MemoryDiagnoser]
[WarmupCount(16)]
[IterationCount(6)]
[IterationTime(250)]
public class Codegen
{
    private Receiver _receiver = new() { Value = 0.731f };
    private Frame _frame = new(47, 0.61f);

    [Benchmark(Baseline = true)]
    public Receipt Direct()
    {
        _receiver.OnForward(in _frame);
        return _receiver.Result;
    }

    [Benchmark]
    public Receipt GeneratedLink()
    {
        ReceiverLink.Forward(ref _receiver, in _frame);
        return _receiver.Result;
    }

    [Benchmark]
    public Receipt Constrained()
    {
        Calls.Constrained(ref _receiver, in _frame);
        return _receiver.Result;
    }
}

public abstract class SparseBase<T> where T : struct, IRadixFixture
{
    public const int Operations = 65536;
    protected ushort[] Queries = null!;

    [GlobalSetup]
    public void Setup()
    {
        var indices = T.Indices.ToArray();
        Queries = new ushort[Operations];
        uint random = 0x51F3AC2E;

        for (var i = 0; i < Queries.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            Queries[i] = indices[random % (uint)indices.Length];
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public DispatchReceipt Binary()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.Binary(query, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt Linear()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.Linear(query, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt FlatSwitch()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.FlatSwitch(query, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt Radix8()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.Radix8(query, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt Radix4()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.Radix4(query, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt DenseFp()
    {
        var sink = default(DispatchSink);
        foreach (var query in Queries.AsSpan())
            T.DenseFp(query, ref sink);
        return sink.Result;
    }
}

[GenericTypeArguments(typeof(Sparse256))]
[GenericTypeArguments(typeof(Sparse4096))]
public class Sparse<T> : SparseBase<T> where T : struct, IRadixFixture
{
}

public abstract class FusedBase<T> where T : struct, IFusedFixture
{
    public const int Operations = 65536;
    protected ushort[] Queries = null!;
    protected int[] DenseIds = null!;
    protected uint[] Ticks = null!;

    [GlobalSetup]
    public void Setup()
    {
        var indices = T.Indices.ToArray();
        Queries = new ushort[Operations];
        DenseIds = new int[Operations];
        Ticks = new uint[Operations];
        uint random = 0x3C6EF35F;

        for (var i = 0; i < Queries.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            var id = (int)(random % (uint)indices.Length);
            Queries[i] = indices[id];
            DenseIds[i] = id;
            Ticks[i] = (random >> 8) % 600;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public DispatchReceipt TwoLevelFp()
    {
        var sink = default(DispatchSink);
        for (var i = 0; i < Queries.Length; i++)
            T.TwoLevelFp(Queries[i], Ticks[i], ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt FusedSparse()
    {
        var sink = default(DispatchSink);
        for (var i = 0; i < Queries.Length; i++)
            T.FusedSparse(Queries[i], Ticks[i], ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public DispatchReceipt FusedDense()
    {
        var sink = default(DispatchSink);
        for (var i = 0; i < Queries.Length; i++)
            T.FusedDense(DenseIds[i], Ticks[i], ref sink);
        return sink.Result;
    }
}

[GenericTypeArguments(typeof(Fused256))]
[GenericTypeArguments(typeof(Fused4096))]
public class Fused<T> : FusedBase<T> where T : struct, IFusedFixture
{
}

// The frozen path: the same sampling and movement facts with every table row
// specialized into compile-time constants at generation time (region branch
// trees, immediate payloads, rank-compare movement facts). No baselines in
// this class - the ApiShape arms stay the reference; these arms are compared
// against them in docs/benchmarks.md.
public sealed class FrozenConfig : ManualConfig
{
    public FrozenConfig()
    {
        var job = Job.Default.WithWarmupCount(16).WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        AddJob(job.WithId("Jit"));
        AddJob(job.WithId("NoTiering").WithEnvironmentVariable("DOTNET_TieredCompilation", "0"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(FrozenConfig))]
public class Frozen
{
    public const int Operations = 65536;
    private uint[] _vitalsTicks = null!;
    private uint[] _fusedTicks = null!;
    private uint[] _vitalsSeq = null!;
    private uint[] _fusedSeq = null!;

    [GlobalSetup]
    public void Setup()
    {
        _vitalsTicks = new uint[Operations];
        _fusedTicks = new uint[Operations];
        _vitalsSeq = new uint[Operations];
        _fusedSeq = new uint[Operations];
        uint random = 0x6D2B79F5;
        for (var i = 0; i < Operations; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _vitalsTicks[i] = random % 600;
            _fusedTicks[i] = random % 63;
            _vitalsSeq[i] = (uint)i % 600;
            _fusedSeq[i] = (uint)i % 63;
        }
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float FrozenVitalsSingle()
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        foreach (var tick in _vitalsTicks.AsSpan())
            pb = VitalsFrozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float FrozenVitalsBatch8()
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        var ticks = _vitalsTicks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.Slice(i, 8);
            pb = VitalsFrozen.Forward(in pb, ref sink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float Fused16Single()
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        foreach (var tick in _fusedTicks.AsSpan())
            pb = Fused16Frozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float Fused16Batch8()
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        var ticks = _fusedTicks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.Slice(i, 8);
            pb = Fused16Frozen.Forward(in pb, ref sink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }
        return sink.Sum + sink.Flags + sink.Count;
    }

    // Predictable streams are where folding pays: identical code, ticks in
    // order, so the branch tree and its data stay hot and predicted.
    [Benchmark(OperationsPerInvoke = Operations)]
    public float FrozenVitalsSequential()
    {
        var sink = default(FrozenSink);
        var pb = VitalsFrozen.Start();
        foreach (var tick in _vitalsSeq.AsSpan())
            pb = VitalsFrozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float Fused16Sequential()
    {
        var sink = default(FrozenSink);
        var pb = Fused16Frozen.Start();
        foreach (var tick in _fusedSeq.AsSpan())
            pb = Fused16Frozen.Forward(in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    // The hub arms route the same tick arrays through FrozenHub (the dense
    // switch over the frozen registry) instead of the direct static calls:
    // registry index 1 is Fused16Frozen, index 0 is VitalsFrozen. No
    // baselines here either - the deltas against the direct arms above are
    // the measurement, reported in docs/benchmarks.md.
    [Benchmark(OperationsPerInvoke = Operations)]
    public float HubFused16Single()
    {
        var sink = default(FrozenSink);
        var pb = FrozenHub.Start(1);
        foreach (var tick in _fusedTicks.AsSpan())
            pb = FrozenHub.Forward(1, in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float HubFused16Sequential()
    {
        var sink = default(FrozenSink);
        var pb = FrozenHub.Start(1);
        foreach (var tick in _fusedSeq.AsSpan())
            pb = FrozenHub.Forward(1, in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float HubFused16Batch8()
    {
        var sink = default(FrozenSink);
        var pb = FrozenHub.Start(1);
        var ticks = _fusedTicks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 8)
        {
            var t = ticks.Slice(i, 8);
            pb = FrozenHub.Forward(1, in pb, ref sink, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]);
        }
        return sink.Sum + sink.Flags + sink.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float HubVitalsSingle()
    {
        var sink = default(FrozenSink);
        var pb = FrozenHub.Start(0);
        foreach (var tick in _vitalsTicks.AsSpan())
            pb = FrozenHub.Forward(0, in pb, ref sink, tick);
        return sink.Sum + sink.Flags + sink.Count;
    }
}
