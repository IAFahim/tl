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

// The redesigned API shape on the Vitals fixture: work-in-Tracks callbacks,
// Stay-only accumulation (Enter/Exit frames notify), empty ticks fire
// nothing. DirectTicks is the hand-rolled STATELESS sampling oracle —
// per tick, resolve every active track to one window and one clip, Exit is
// positional at the window's last frame, everything else Stay. The stateful
// arms (Playback*/HubDispatch) additionally cross entry edges as Enter when
// the destination advances past a window start; StatefulOracle mirrors that
// path for the --verify receipts.
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
        var seed = new VitalsInput(1_000_000f);
        var data = seed.Seed();
        var blender = new VitalsTrack();
        foreach (var tick in _ticks.AsSpan())
        {
            var starts = VitalsTrack.RegionStarts;
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;

            var row = VitalsTrack.RegionRows[region];
            if (row.TrackCount == 0)
                continue;

            for (var t = 0; t < row.TrackCount; t++)
            {
                var track = VitalsTrack.TrackRows[row.TrackStart + t];
                var first = VitalsTrack.ClipRows[track.ClipStart];

                uint start, end;
                float amount;
                if (track.ClipCount == 1)
                {
                    var edge = VitalsTrack.ClipEdges[first.ClipIndex];
                    start = edge.Start;
                    end = edge.End;
                    amount = VitalsTrack.ClipData[first.ClipIndex].Amount;
                }
                else
                {
                    var second = VitalsTrack.ClipRows[track.ClipStart + 1];
                    var edgeA = VitalsTrack.ClipEdges[first.ClipIndex];
                    var edgeB = VitalsTrack.ClipEdges[second.ClipIndex];
                    start = edgeA.Start < edgeB.Start ? edgeA.Start : edgeB.Start;
                    end = edgeA.End > edgeB.End ? edgeA.End : edgeB.End;
                    var factor = first.FactorLength <= 1 ? 0.5f : (tick - first.FactorStart) / (float)(first.FactorLength - 1);
                    blender.Blend(
                        in VitalsTrack.ClipData[first.ClipIndex],
                        in VitalsTrack.ClipData[second.ClipIndex],
                        factor,
                        out var resolved);
                    amount = resolved.Amount;
                }

                // Stateless sampling: Exit is positional, everything else
                // Stay — no movement, so nothing enters.
                if (tick == end - 1)
                    continue;

                data.Ticks += VitalsTrack.TrackData[track.TrackIndex].Offset;
                data.Health += amount;
            }

            data.Count++;
        }
        return data.Result;
    }

    // The stateful mirror of DirectTicks: tracks the previous tick the way
    // the Playback path does, so an advancing step that crosses a window
    // start reports Enter and does not accumulate. Receipts-only helper —
    // not a benchmark arm.
    public Receipt StatefulOracle()
    {
        var seed = new VitalsInput(1_000_000f);
        var data = seed.Seed();
        var blender = new VitalsTrack();
        uint prev = 0;
        foreach (var tick in _ticks.AsSpan())
        {
            var starts = VitalsTrack.RegionStarts;
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;

            var row = VitalsTrack.RegionRows[region];
            if (row.TrackCount != 0)
            {
                for (var t = 0; t < row.TrackCount; t++)
                {
                    var track = VitalsTrack.TrackRows[row.TrackStart + t];
                    var first = VitalsTrack.ClipRows[track.ClipStart];

                    uint start, end;
                    float amount;
                    if (track.ClipCount == 1)
                    {
                        var edge = VitalsTrack.ClipEdges[first.ClipIndex];
                        start = edge.Start;
                        end = edge.End;
                        amount = VitalsTrack.ClipData[first.ClipIndex].Amount;
                    }
                    else
                    {
                        var second = VitalsTrack.ClipRows[track.ClipStart + 1];
                        var edgeA = VitalsTrack.ClipEdges[first.ClipIndex];
                        var edgeB = VitalsTrack.ClipEdges[second.ClipIndex];
                        start = edgeA.Start < edgeB.Start ? edgeA.Start : edgeB.Start;
                        end = edgeA.End > edgeB.End ? edgeA.End : edgeB.End;
                        var factor = first.FactorLength <= 1 ? 0.5f : (tick - first.FactorStart) / (float)(first.FactorLength - 1);
                        blender.Blend(
                            in VitalsTrack.ClipData[first.ClipIndex],
                            in VitalsTrack.ClipData[second.ClipIndex],
                            factor,
                            out var resolved);
                        amount = resolved.Amount;
                    }

                    if (tick == end - 1 || prev < start)
                        continue;

                    data.Ticks += VitalsTrack.TrackData[track.TrackIndex].Offset;
                    data.Health += amount;
                }

                data.Count++;
            }

            prev = tick;
        }
        return data.Result;
    }

    // The same fixture the static tables encode, authored at run time inside
    // a Build callback; only the assigned global ushort index escapes.
    public ushort BuildTimeline(bool loops = false)
        => Timeline<VitalsTrack, VitalsClip>.Build(b =>
        {
            TrackRef track0 = b.Track(new VitalsTrack(1));
            TrackRef track1 = b.Track(new VitalsTrack(2));
            TrackRef track2 = b.Track(new VitalsTrack(3));
            TrackRef track3 = b.Track(new VitalsTrack(4));

            b.Clip(track0, new VitalsClip(1f), 0, 7);
            b.Clip(track0, new VitalsClip(13f), 29, 47);
            b.Clip(track0, new VitalsClip(21f), 29, 47);
            b.Clip(track0, new VitalsClip(13f), 47, 76);
            b.Clip(track0, new VitalsClip(1f), 321, 515);

            b.Clip(track1, new VitalsClip(2f), 3, 7);
            b.Clip(track1, new VitalsClip(3f), 3, 11);
            b.Clip(track1, new VitalsClip(2f), 47, 76);
            b.Clip(track1, new VitalsClip(2f), 76, 123);
            b.Clip(track1, new VitalsClip(3f), 515, 600);

            b.Clip(track2, new VitalsClip(8f), 18, 29);
            b.Clip(track2, new VitalsClip(8f), 76, 123);
            b.Clip(track2, new VitalsClip(8f), 321, 515);

            b.Clip(track3, new VitalsClip(5f), 3, 11);
            b.Clip(track3, new VitalsClip(5f), 18, 29);
            b.Clip(track3, new VitalsClip(34f), 76, 123);
            b.Clip(track3, new VitalsClip(55f), 76, 123);
            b.Clip(track3, new VitalsClip(5f), 200, 321);
            b.Clip(track3, new VitalsClip(5f), 321, 515);

            if (loops)
                b.Looping();
        });

    public Receipt RunInstance(ushort timeline)
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        foreach (var tick in _ticks.AsSpan())
            Timeline.Forward(timeline, in input, ref data, tick);
        return data.Result;
    }

    // The runtime-authored timeline through the global hub, stateless.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt InstanceSingle()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        foreach (var tick in _ticks.AsSpan())
            Timeline.Forward(_timeline, in input, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt InstanceParamsFour()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            Timeline.Forward(_timeline, in input, ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    // The compiled-tables shell, stateless.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ShellSingle()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        foreach (var tick in _ticks.AsSpan())
            GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in input, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ShellParamsFour()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in input, ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    // The Playback path: same sampling, plus the lifecycle/loop/completion
    // flags word folded into the returned 8-byte Playback and the per-work
    // movement facts in the view. Receipts must still match StatefulOracle.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackSingle()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        foreach (var tick in _ticks.AsSpan())
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pb, in input, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackParamsFour()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pb, in input, ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt PlaybackBackwardSingle()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        foreach (var tick in _ticks.AsSpan())
            pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pb, in input, ref data, tick);
        return data.Result;
    }

    // The dispatch cost of the global hub: Timeline.Forward(index, ...) —
    // bounds + lifecycle checks, the per-(entry, TData) function-pointer
    // lookup, then the same engine the closure shell calls directly
    // (PlaybackSingle). Expected within ~1-2 ns of the direct call.
    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt HubDispatch()
    {
        var input = new VitalsInput(1_000_000f);
        var data = input.Seed();
        var pb = Timeline.Start(_timeline);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_timeline, in pb, in input, ref data, tick);
        return data.Result;
    }
}

// The persistent caller-owned cursor at 16/64/512 clips (33/129/1025
// regions — all past the binary-search threshold), sequential and random
// single-tick streams, against the no-cursor reference on the same runtime
// registry path. The invalid arm nulls the owner before every call, so it
// measures full validation plus the search fallback.
public readonly record struct CursorClip(float Value);

public struct CursorTrack : IBlend<CursorClip>
{
    public void Blend(in CursorClip first, in CursorClip second, float t, out CursorClip result)
        => result = new(first.Value * (1f - t) + second.Value * t);
}

internal struct CursorData :
    IForward<CursorTrack, CursorClip, NoInput, CursorData>,
    IBackward<CursorTrack, CursorClip, NoInput, CursorData>
{
    public float Sum;
    public int Count;

    public void Forward(in Tracks<CursorTrack, CursorClip> tracks, in NoInput input, in uint tick, ref CursorData result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Value;
        result.Count++;
    }

    public void Backward(in Tracks<CursorTrack, CursorClip> tracks, in NoInput input, in uint tick, ref CursorData result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Value;
        result.Count--;
    }
}

[Config(typeof(Config))]
public class CursorShape
{
    public const int Operations = 65536;
    [Params(16, 64, 512)] public int Clips { get; set; }
    [Params(false, true)] public bool Sequential { get; set; }
    private uint[] _ticks = null!;
    private ushort _timeline;

    [GlobalSetup]
    public void Setup()
    {
        _timeline = Timeline<CursorTrack, CursorClip>.Build(b =>
        {
            var track = b.Track(new CursorTrack());
            for (uint i = 0; i < (uint)Clips; i++)
                b.Clip(track, new CursorClip(i + 1), i * 4, i * 4 + 3);
        });
        _ticks = new uint[Operations];
        uint duration = (uint)Clips * 4;
        uint random = 0xB19F4C27;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = Sequential ? (uint)i % duration : random % duration;
        }

        // Receipt: cursor arms must match the no-cursor arm exactly.
        var expected = Single();
        if (SingleCursor() != expected || SingleCursorInvalid() != expected)
            throw new InvalidOperationException($"CursorShape receipts differ for {Clips}/{Sequential}.");
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public float Single()
    {
        var data = new CursorData();
        var pb = Timeline.Start(_timeline);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_timeline, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float SingleCursor()
    {
        var data = new CursorData();
        var pb = Timeline.Start(_timeline);
        var cursor = default(Cursor);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_timeline, in pb, ref cursor, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float SingleCursorInvalid()
    {
        var data = new CursorData();
        var pb = Timeline.Start(_timeline);
        var cursor = default(Cursor);
        foreach (var tick in _ticks.AsSpan())
        {
            cursor.Owner = null; // deliberately stale before every call
            pb = Timeline.Forward(_timeline, in pb, ref cursor, default(NoInput), ref data, tick);
        }
        return data.Sum + data.Count;
    }
}

// Blend-sized scratch: a four-track region where two tracks carry
// crossfading pairs (260 B payloads) and two hold standalone clips. The
// stack path reserves MaxActiveBlends (2 x 260 B) per call; the buffer arms
// reuse one caller-owned buffer across the whole walk. The zero-blend arms
// run a timeline with no pairs at all — no scratch reserved either way.
public unsafe struct BlendClip
{
    public fixed byte Pad[256];
    public float Value;

    public BlendClip(float value)
    {
        Value = value;
    }
}

public struct BlendTrack : IBlend<BlendClip>
{
    public void Blend(in BlendClip first, in BlendClip second, float t, out BlendClip result)
        => result = new BlendClip(first.Value * (1f - t) + second.Value * t);
}

internal struct BlendData :
    IForward<BlendTrack, BlendClip, NoInput, BlendData>,
    IBackward<BlendTrack, BlendClip, NoInput, BlendData>
{
    public float Sum;
    public int Count;

    public void Forward(in Tracks<BlendTrack, BlendClip> tracks, in NoInput input, in uint tick, ref BlendData result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Value;
        result.Count++;
    }

    public void Backward(in Tracks<BlendTrack, BlendClip> tracks, in NoInput input, in uint tick, ref BlendData result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Value;
        result.Count--;
    }
}

[Config(typeof(Config))]
public class BlendShape
{
    public const int Operations = 65536;
    public const int Duration = 64;
    private uint[] _ticks = null!;
    private ushort _blended;
    private ushort _zeroBlend;
    private BlendClip[] _buffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _blended = Timeline<BlendTrack, BlendClip>.Build(b =>
        {
            // Two crossfading pairs and two standalone clips, all live over
            // the whole duration: MaxActiveTracks 4, MaxActiveBlends 2.
            TrackRef firstPair = b.Track(new BlendTrack());
            TrackRef secondPair = b.Track(new BlendTrack());
            TrackRef soloA = b.Track(new BlendTrack());
            TrackRef soloB = b.Track(new BlendTrack());
            b.Clip(firstPair, new BlendClip(4f), 0, Duration);
            b.Clip(firstPair, new BlendClip(8f), 0, Duration);
            b.Clip(secondPair, new BlendClip(6f), 0, Duration);
            b.Clip(secondPair, new BlendClip(12f), 0, Duration);
            b.Clip(soloA, new BlendClip(3f), 0, Duration);
            b.Clip(soloB, new BlendClip(5f), 0, Duration);
        });
        _zeroBlend = Timeline<BlendTrack, BlendClip>.Build(b =>
        {
            for (var t = 0; t < 4; t++)
            {
                TrackRef track = b.Track(new BlendTrack());
                b.Clip(track, new BlendClip(t + 1), 0, Duration);
            }
        });
        _ticks = new uint[Operations];
        for (var i = 0; i < _ticks.Length; i++)
            _ticks[i] = (uint)i % Duration;

        // The caller-owned buffer: two blend slots, retained for the whole
        // walk — stack bytes per call become zero, retained bytes 2 * 260.
        _buffer = new BlendClip[2];

        // Receipts: every arm must produce the identical sum.
        var expected = StackSingle();
        if (BufferSingle() != expected || StackBatchFour() != expected || BufferBatchFour() != expected)
            throw new InvalidOperationException("BlendShape receipts differ across arms.");
        var zero = ZeroBlendSingle();
        if (ZeroBlendBatchFour() != zero)
            throw new InvalidOperationException("BlendShape zero-blend receipts differ across arms.");
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float StackSingle()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_blended);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_blended, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float StackBatchFour()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_blended);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_blended, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float BufferSingle()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_blended);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline<BlendTrack, BlendClip>.Forward(_blended, in pb, default(NoInput), ref data, _buffer, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float BufferBatchFour()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_blended);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline<BlendTrack, BlendClip>.Forward(_blended, in pb, default(NoInput), ref data, _buffer, t);
        }
        return data.Sum + data.Count;
    }

    // No pairs anywhere: the scratch reservation is zero on every path.
    [Benchmark(OperationsPerInvoke = Operations)]
    public float ZeroBlendSingle()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_zeroBlend);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_zeroBlend, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float ZeroBlendBatchFour()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_zeroBlend);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_zeroBlend, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Count;
    }
}

// Prefix counts for the per-work Enter gate (faster queue #3): four tracks
// each tiling Clips/4 non-overlapping clips, so every region carries four
// works whose State is consumed (a state-code checksum). Two builds of the
// same content — CutCounts emitted (gate on) versus empty (full per-work
// Crossed check, the pre-experiment behavior) — single-tick and batch4,
// sequential and random.
public struct CountsTrack : IBlend<CursorClip>
{
    public void Blend(in CursorClip first, in CursorClip second, float t, out CursorClip result)
        => result = new(first.Value * (1f - t) + second.Value * t);
}

internal struct CountsData :
    IForward<CountsTrack, CursorClip, NoInput, CountsData>,
    IBackward<CountsTrack, CursorClip, NoInput, CountsData>
{
    public float Sum;
    public long Codes;
    public int Count;

    public void Forward(in Tracks<CountsTrack, CursorClip> tracks, in NoInput input, in uint tick, ref CountsData result)
    {
        foreach (var work in tracks)
        {
            result.Sum += work.Clip.Value;
            result.Codes += (uint)work.State;
        }
        result.Count++;
    }

    public void Backward(in Tracks<CountsTrack, CursorClip> tracks, in NoInput input, in uint tick, ref CountsData result)
    {
        foreach (var work in tracks)
        {
            result.Sum -= work.Clip.Value;
            result.Codes -= (uint)work.State;
        }
        result.Count--;
    }
}

[Config(typeof(Config))]
public class CountsShape
{
    public const int Operations = 65536;
    [Params(16, 64, 512)] public int Clips { get; set; }
    [Params(false, true)] public bool Sequential { get; set; }
    private uint[] _ticks = null!;
    private ushort _counted;
    private ushort _scanned;

    private static ushort BuildCounts(bool emit, int clips)
    {
        Timeline.EmitCutCounts = emit;
        return Timeline<CountsTrack, CursorClip>.Build(b =>
        {
            for (var t = 0; t < 4; t++)
            {
                TrackRef track = b.Track(new CountsTrack());
                for (uint i = 0; i < (uint)clips / 4; i++)
                    b.Clip(track, new CursorClip(i + 1), i * 4, i * 4 + 3);
            }
        });
    }

    [GlobalSetup]
    public void Setup()
    {
        _counted = BuildCounts(true, Clips);
        _scanned = BuildCounts(false, Clips);
        Timeline.EmitCutCounts = true;
        _ticks = new uint[Operations];
        uint duration = (uint)Clips / 4 * 4;
        uint random = 0xB19F4C27;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = Sequential ? (uint)i % duration : random % duration;
        }

        // Receipt: the gate must not change the state-code checksum.
        var expected = ScanSingle();
        if (CountsSingle() != expected || ScanBatchFour() != expected || CountsBatchFour() != expected)
            throw new InvalidOperationException($"CountsShape receipts differ for {Clips}/{Sequential}.");
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public float ScanSingle()
    {
        var data = new CountsData();
        var pb = Timeline.Start(_scanned);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_scanned, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Codes + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float CountsSingle()
    {
        var data = new CountsData();
        var pb = Timeline.Start(_counted);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_counted, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Codes + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float ScanBatchFour()
    {
        var data = new CountsData();
        var pb = Timeline.Start(_scanned);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_scanned, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Codes + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float CountsBatchFour()
    {
        var data = new CountsData();
        var pb = Timeline.Start(_counted);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_counted, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Codes + data.Count;
    }
}

// Storage dedup at Build time (faster queue #4): 64 long clips spanning 256
// four-tick regions (payloads drawn from eight distinct 260-byte values)
// plus a 256-clip sweeper — the shape where long clips duplicate their CSR
// rows per region and payloads repeat heavily. Plain builds keep one row
// per authored instance; deduped builds share payload slots and alias
// identical row runs. Warm playback arms A/B the indirection cost; the
// Build arms are the cold preparation (allocation visible to the
// diagnoser); retained bytes are receipted in setup.
[Config(typeof(Config))]
public class DedupShape
{
    public const int Operations = 65536;
    public const int Regions = 256;
    private uint[] _ticks = null!;
    private ushort _plain;
    private ushort _deduped;
    public long PlainRetained;
    public long DedupRetained;

    private static ushort Build(bool dedup)
    {
        Timeline.DedupStorage = dedup;
        var index = Timeline<BlendTrack, BlendClip>.Build(b =>
        {
            for (var t = 0; t < 64; t++)
            {
                TrackRef track = b.Track(new BlendTrack());
                b.Clip(track, new BlendClip((t % 8) + 1), 0, (uint)Regions * 4);
            }

            TrackRef sweeper = b.Track(new BlendTrack());
            for (uint i = 0; i < Regions; i++)
                b.Clip(sweeper, new BlendClip(i % 4), i * 4, i * 4 + 3);
        });
        Timeline.DedupStorage = true;
        return index;
    }

    private static long RetainedOf(bool dedup)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(true);
        var index = Build(dedup);
        var after = GC.GetTotalMemory(true);
        _ = Timeline.Duration(index); // the entry stays registered: retained
        return after - before;
    }

    [GlobalSetup]
    public void Setup()
    {
        _plain = Build(false);
        _deduped = Build(true);
        _ticks = new uint[Operations];
        uint random = 0xB19F4C27;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = random % (uint)(Regions * 4);
        }

        PlainRetained = RetainedOf(false);
        DedupRetained = RetainedOf(true);
        Console.WriteLine($"DedupShape retained bytes: plain {PlainRetained:N0}, deduped {DedupRetained:N0} ({100.0 * DedupRetained / PlainRetained:F1} percent)");
        if (DedupRetained >= PlainRetained)
            throw new InvalidOperationException("The deduped build must not retain more than the plain build on this fixture.");

        // Receipts: warm arms must agree exactly.
        var expected = PlainSingle();
        if (DedupSingle() != expected || PlainBatchFour() != expected || DedupBatchFour() != expected)
            throw new InvalidOperationException("DedupShape receipts differ across arms.");
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float PlainSingle()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_plain);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_plain, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float DedupSingle()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_deduped);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_deduped, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float PlainBatchFour()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_plain);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_plain, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Count;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float DedupBatchFour()
    {
        var data = new BlendData();
        var pb = Timeline.Start(_deduped);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_deduped, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Sum + data.Count;
    }

    // Cold preparation: one Build per invoke (each registers a fresh index).
    [Benchmark(OperationsPerInvoke = 4)]
    public float BuildPlain()
    {
        var sum = 0f;
        for (var i = 0; i < 4; i++)
            sum += Timeline.Duration(Build(false));
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 4)]
    public float BuildDedup()
    {
        var sum = 0f;
        for (var i = 0; i < 4; i++)
            sum += Timeline.Duration(Build(true));
        return sum;
    }
}

// The 8-byte Playback claim: `in`, by-value, and `ref` passing of one qword
// should be indistinguishable.
[Config(typeof(Config))]
public class PassPlayback
{
    private Playback _pass = new(123, 456, PlaybackFlags.Started);
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

// TrackViewDecomp — where does a single tick's time go? One region, four
// tracks live on every tick of a 64-tick duration, so region lookup and
// movement scans are constant and the ladder isolates the per-tick view
// machinery. Each result type adds exactly one cost on top of the last:
//   Calls      the callback fires and counts — engine + dispatch floor
//   Walk       + foreach traversal (enumerator; TrackWork reads nothing)
//   Index      + one TrackWork field read (construction becomes live)
//   State      + per-work ClipState derivation
//   Clip       + full clip resolution (the payload read every consumer does)
//   ClipBlend  the Clip read on the blended fixture — adds the Blend pair
//              machinery (factor math, scratch slot, resolved-buffer clip)
// References for the verdict: PlaybackSingle (real Vitals consumer, mixed
// regions/blends/gaps) and the frozen floor in docs/benchmarks.md.
public readonly record struct DecompClip(float Value);

public struct DecompTrack : IBlend<DecompClip>
{
    public void Blend(in DecompClip first, in DecompClip second, float t, out DecompClip result)
        => result = new(first.Value * (1f - t) + second.Value * t);
}

internal struct DecompCalls :
    IForward<DecompTrack, DecompClip, NoInput, DecompCalls>,
    IBackward<DecompTrack, DecompClip, NoInput, DecompCalls>
{
    public int Calls;

    public void Forward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompCalls result)
        => result.Calls++;

    public void Backward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompCalls result)
        => result.Calls--;
}

internal struct DecompWalk :
    IForward<DecompTrack, DecompClip, NoInput, DecompWalk>,
    IBackward<DecompTrack, DecompClip, NoInput, DecompWalk>
{
    public int Calls;

    public void Forward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompWalk result)
    {
        foreach (var work in tracks)
        {
            _ = work; // traverse only: enumerator runs, nothing is read
        }
        result.Calls++;
    }

    public void Backward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompWalk result)
    {
        foreach (var work in tracks)
        {
            _ = work;
        }
        result.Calls--;
    }
}

internal struct DecompIndex :
    IForward<DecompTrack, DecompClip, NoInput, DecompIndex>,
    IBackward<DecompTrack, DecompClip, NoInput, DecompIndex>
{
    public long Sink;
    public int Calls;

    public void Forward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompIndex result)
    {
        foreach (var work in tracks)
            result.Sink += work.Index;
        result.Calls++;
    }

    public void Backward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompIndex result)
    {
        foreach (var work in tracks)
            result.Sink -= work.Index;
        result.Calls--;
    }
}

internal struct DecompState :
    IForward<DecompTrack, DecompClip, NoInput, DecompState>,
    IBackward<DecompTrack, DecompClip, NoInput, DecompState>
{
    public long Sink;
    public int Calls;

    public void Forward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompState result)
    {
        foreach (var work in tracks)
            result.Sink += (long)work.State;
        result.Calls++;
    }

    public void Backward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompState result)
    {
        foreach (var work in tracks)
            result.Sink -= (long)work.State;
        result.Calls--;
    }
}

internal struct DecompClipRead :
    IForward<DecompTrack, DecompClip, NoInput, DecompClipRead>,
    IBackward<DecompTrack, DecompClip, NoInput, DecompClipRead>
{
    public float Sum;
    public int Calls;

    public void Forward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompClipRead result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Value;
        result.Calls++;
    }

    public void Backward(in Tracks<DecompTrack, DecompClip> tracks, in NoInput input, in uint tick, ref DecompClipRead result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Value;
        result.Calls--;
    }
}

[Config(typeof(Config))]
public class TrackViewDecomp
{
    public const int Operations = 65536;
    public const uint Duration = 64;
    private uint[] _ticks = null!;
    private ushort _singles;
    private ushort _blends;

    [GlobalSetup]
    public void Setup()
    {
        _singles = Timeline<DecompTrack, DecompClip>.Build(b =>
        {
            for (var t = 0; t < 4; t++)
            {
                TrackRef track = b.Track(new DecompTrack());
                b.Clip(track, new DecompClip(t * 2 + 3), 0, Duration);
            }
        });
        _blends = Timeline<DecompTrack, DecompClip>.Build(b =>
        {
            TrackRef firstPair = b.Track(new DecompTrack());
            TrackRef secondPair = b.Track(new DecompTrack());
            TrackRef soloA = b.Track(new DecompTrack());
            TrackRef soloB = b.Track(new DecompTrack());
            b.Clip(firstPair, new DecompClip(4f), 0, Duration);
            b.Clip(firstPair, new DecompClip(8f), 0, Duration);
            b.Clip(secondPair, new DecompClip(6f), 0, Duration);
            b.Clip(secondPair, new DecompClip(12f), 0, Duration);
            b.Clip(soloA, new DecompClip(3f), 0, Duration);
            b.Clip(soloB, new DecompClip(5f), 0, Duration);
        });
        _ticks = new uint[Operations];
        uint random = 0x6D2B79F5;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = random % Duration;
        }

        // Receipts, hand-derived: one region and Start(0) at the clip starts
        // means every work on every tick is Stay; the blended pairs resolve
        // with factor tick/63 in track-row order.
        var clip = new DecompClipRead();
        var pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref clip, tick);
        if (clip.Calls != Operations || MathF.Abs(clip.Sum - 24f * Operations) > 0.01f)
            throw new InvalidOperationException($"TrackViewDecomp singles receipt mismatch: {clip.Sum} / {clip.Calls}.");

        var index = new DecompIndex();
        var state = new DecompState();
        var walk = new DecompWalk();
        var calls = new DecompCalls();
        pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
        {
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref index, tick);
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref state, tick);
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref walk, tick);
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref calls, tick);
        }
        if (index.Sink != 6L * Operations || index.Calls != Operations)
            throw new InvalidOperationException($"TrackViewDecomp index receipt mismatch: {index.Sink} / {index.Calls}.");
        // Random ticks cross entry edges in both directions, so the state mix
        // is stream-dependent (measured run: 266184 vs 262144 all-Stay — the
        // Enter surplus of a backward-crossing random walk). The exact
        // per-work state semantics are gated by EdgeVerification's oracle;
        // here only the callback count is structural.
        if (state.Calls != Operations)
            throw new InvalidOperationException($"TrackViewDecomp state receipt mismatch: {state.Calls}.");
        if (walk.Calls != Operations || calls.Calls != Operations)
            throw new InvalidOperationException("TrackViewDecomp walk/calls receipt mismatch.");

        var blended = new DecompClipRead();
        pb = Timeline.Start(_blends);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_blends, in pb, default(NoInput), ref blended, tick);
        var expected = 0f;
        foreach (var tick in _ticks.AsSpan())
        {
            var factor = tick / 63f;
            expected += 4f * (1f - factor) + 8f * factor;
            expected += 6f * (1f - factor) + 12f * factor;
            expected += 3f;
            expected += 5f;
        }
        if (blended.Calls != Operations || MathF.Abs(blended.Sum - expected) > 0.01f)
            throw new InvalidOperationException($"TrackViewDecomp blends receipt mismatch: {blended.Sum:R} vs {expected:R}.");
    }

    private float Run(ushort index, ref DecompClipRead data)
    {
        var pb = Timeline.Start(index);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(index, in pb, default(NoInput), ref data, tick);
        return data.Sum + data.Calls;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float Calls()
    {
        var data = new DecompCalls();
        var pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref data, tick);
        return data.Calls;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float Walk()
    {
        var data = new DecompWalk();
        var pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref data, tick);
        return data.Calls;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float IndexRead()
    {
        var data = new DecompIndex();
        var pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref data, tick);
        return data.Sink + data.Calls;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float StateRead()
    {
        var data = new DecompState();
        var pb = Timeline.Start(_singles);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_singles, in pb, default(NoInput), ref data, tick);
        return data.Sink + data.Calls;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float ClipRead()
    {
        var data = new DecompClipRead();
        return Run(_singles, ref data);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public float ClipReadBlend()
    {
        var data = new DecompClipRead();
        return Run(_blends, ref data);
    }
}
