using System.Runtime.CompilerServices;

namespace Tl.Hooks;

public interface IClip;
public interface ITrack;
public interface ITimelineForward { void OnForward(in Frame frame); }
public interface ITimelineBackward { void OnBackward(in Frame frame); }
public interface ITimelineStart { void OnStart(); }
public interface ITimelineStop { void OnStop(); }
public interface ITimelineLoop { void OnLoop(long cycle); }

public readonly record struct Frame(int Tick, float Weight);
public readonly record struct Receipt(float Sum, long Ticks, int Count);

public struct Receiver : IClip, ITimelineForward
{
    public float Value;
    public float Sum;
    public long Ticks;
    public int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnForward(in Frame frame)
    {
        Sum += Value * frame.Weight;
        Ticks += frame.Tick;
        Count++;
    }

    public readonly Receipt Result => new(Sum, Ticks, Count);
}

public struct ExplicitReceiver : IClip, ITimelineForward
{
    public Receiver Data;

    void ITimelineForward.OnForward(in Frame frame) => Data.OnForward(in frame);
}

public struct OtherReceiver : IClip, ITimelineForward
{
    public Receiver Data;

    public void OnForward(in Frame frame) => Data.OnForward(in frame);
}

public delegate void ForwardAction(ref Receiver receiver, in Frame frame);

public static class Calls
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Constrained<T>(ref T receiver, in Frame frame)
        where T : struct, ITimelineForward
        => receiver.OnForward(in frame);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Boxed(ITimelineForward receiver, in Frame frame)
        => receiver.OnForward(in frame);
}

// The "timeline mutates my live data through ref" shape: the data type
// declares only the hooks it wants; everything else stays plain fields.
public interface IHealthHook
{
    void OnHit(in Hit hit);
}

public readonly record struct Hit(int Damage);

public struct Player : IHealthHook
{
    public float Health;
    public long Ticks;
    public int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnHit(in Hit hit)
    {
        Health -= hit.Damage;
        Ticks += hit.Damage;
        Count++;
    }

    public readonly Receipt Result => new(Health, Ticks, Count);
}

// Timeline side stays generic over the data; the hook is a constrained call.
public static class DamageTimeline
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<TD>(in Frame frame, ref TD data)
        where TD : struct, IHealthHook
        => data.OnHit(new Hit(frame.Tick & 1023));
}

// The "just interface, override without data" shape: hooks with default
// bodies so implementers that don't care still satisfy the contract.
public interface IIdleHook
{
    int Count { get; set; }

    void OnIdle()
    {
        Count++;
    }
}

public struct IdlePlayer : IIdleHook
{
    public int Count { get; set; }
}

public struct BusyPlayer : IIdleHook
{
    public int Count { get; set; }

    public void OnIdle()
    {
        Count++;
    }
}

public static class IdleCalls
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dim<T>(ref T player)
        where T : struct, IIdleHook
        => player.OnIdle();
}

// The composed API shape under discussion:
//   Timeline<HealthTrack, HealthClip, Player>.Forward(ref player, t0, t1, t2, t3)
//
// One callback per tick, always through IForwardTracks. The "frame" is not
// materialized, and blending is resolved before the consumer runs: each
// active track exposes exactly ONE clip — an original, or the IBlend result
// of an overlapping pair held in a stackalloc'd slot. Consumers never see
// weights, pairs, or blend windows.
public interface IForward<TClip, TData>
    where TClip : struct
    where TData : struct
{
    void Forward(in TClip clip, uint tick, ref TData data);
}

public interface IBlend<TClip>
    where TClip : struct
{
    void Blend(in TClip first, in TClip second, float factor, out TClip result);
}

public interface IForwardTracks<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Forward(uint tick, in ForwardTracks<TTrack, TClip> tracks, ref TData data);
}

public readonly record struct RegionRow(ushort TrackStart, ushort TrackCount);
public readonly record struct TrackRow(ushort TrackIndex, ushort ClipStart, ushort ClipCount);
public readonly record struct ClipRow(ushort ClipIndex, uint FactorStart, uint FactorLength);

public interface ITrackTables<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    static abstract ReadOnlySpan<uint> RegionStarts { get; }
    static abstract ReadOnlySpan<RegionRow> RegionRows { get; }
    static abstract ReadOnlySpan<TrackRow> TrackRows { get; }
    static abstract ReadOnlySpan<ClipRow> ClipRows { get; }
    static abstract ReadOnlySpan<TTrack> TrackData { get; }
    static abstract ReadOnlySpan<TClip> ClipData { get; }
    static abstract int MaxActiveTracks { get; }
}

public readonly ref struct ForwardTracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly uint _tick;
    private readonly ReadOnlySpan<TrackRow> _trackRows;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly Span<TClip> _resolved;

    internal ForwardTracks(
        uint tick,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
    {
        _tick = tick;
        _trackRows = trackRows;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _resolved = resolved;
    }

    public int Count => _trackRows.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(_tick, _trackRows, _clipRows, _trackData, _clipData, _resolved);

    public ref struct Enumerator
    {
        private readonly uint _tick;
        private readonly ReadOnlySpan<TrackRow> _trackRows;
        private readonly ReadOnlySpan<ClipRow> _clipRows;
        private readonly ReadOnlySpan<TTrack> _trackData;
        private readonly ReadOnlySpan<TClip> _clipData;
        private readonly Span<TClip> _resolved;
        private readonly TTrack _blender;
        private int _i;

        internal Enumerator(
            uint tick,
            ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
            ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
            Span<TClip> resolved)
        {
            _tick = tick;
            _trackRows = trackRows;
            _clipRows = clipRows;
            _trackData = trackData;
            _clipData = clipData;
            _resolved = resolved;
            _blender = default;
            _i = -1;
        }

        public readonly ForwardItem<TTrack, TClip> Current
            => new(_trackRows[_i], _clipRows, _trackData, _clipData, _resolved, _i);

        // Resolution fused with traversal: each blending track collapses to
        // one clip the moment it is reached. Items that are never visited are
        // never blended.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var i = ++_i;
            if (i >= _trackRows.Length)
                return false;

            var row = _trackRows[i];

            if (row.ClipCount != 2)
                return true;

            var first = _clipRows[row.ClipStart];
            var second = _clipRows[row.ClipStart + 1];
            var factor = (_tick - first.FactorStart) / (float)(first.FactorLength - 1);
            _blender.Blend(
                in _clipData[first.ClipIndex],
                in _clipData[second.ClipIndex],
                factor,
                out _resolved[i]);

            return true;
        }
    }
}

// One resolved (track, clip) pair: Track points into the payload table; Clip
// points into the table for an original clip or into the resolved buffer for
// a blended one. Either way the consumer sees exactly one clip.
public readonly ref struct ForwardItem<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    private readonly TrackRow _row;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly ReadOnlySpan<TClip> _resolved;
    private readonly int _slot;

    internal ForwardItem(
        TrackRow row, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<TClip> resolved, int slot)
    {
        _row = row;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _resolved = resolved;
        _slot = slot;
    }

    public ref readonly TTrack Track => ref _trackData[_row.TrackIndex];

    public ref readonly TClip Clip
    {
        get
        {
            if (_row.ClipCount == 1)
                return ref _clipData[_clipRows[_row.ClipStart].ClipIndex];

            return ref _resolved[_slot];
        }
    }
}

public readonly struct VitalsClip : IForward<VitalsClip, Vitals>
{
    public readonly float Amount;

    public VitalsClip(float amount) => Amount = amount;

    public void Forward(in VitalsClip clip, uint tick, ref Vitals data)
    {
        data.Health -= clip.Amount;
        data.Ticks += tick;
        data.Count++;
    }
}

public struct Vitals : IForwardTracks<VitalsTrack, VitalsClip, Vitals>
{
    public float Health;
    public long Ticks;
    public int Count;

    public readonly Receipt Result => new(Health, Ticks, Count);

    // Blend-ignorant on purpose: every active track has exactly one resolved
    // clip — an original or a blend result, indistinguishable here.
    public void Forward(uint tick, in ForwardTracks<VitalsTrack, VitalsClip> tracks, ref Vitals data)
    {
        foreach (var item in tracks)
        {
            data.Ticks += item.Track.Offset;
            data.Health += item.Clip.Amount;
        }

        data.Count++;
    }
}

public struct VitalsTrack : ITrackTables<VitalsTrack, VitalsClip>, IBlend<VitalsClip>
{
    public readonly int Offset;

    public VitalsTrack(int offset) => Offset = offset;

    private static readonly uint[] s_regionStarts = [0, 3, 7, 11, 18, 29, 47, 76, 123, 200, 321, 515];

    // Region r -> slice of s_trackRows; TrackCount 0 marks a gap.
    private static readonly RegionRow[] s_regionRows =
    [
        new(0, 1), new(1, 3), new(4, 2), new(6, 0), new(6, 2), new(8, 1),
        new(9, 2), new(11, 3), new(14, 0), new(14, 1), new(15, 3), new(18, 1),
    ];

    // Track rows: (track index, slice of s_clipRows).
    private static readonly TrackRow[] s_trackRows =
    [
        new(0, 0, 1), new(0, 0, 1), new(1, 1, 2), new(3, 4, 1),
        new(1, 3, 1), new(3, 4, 1),
        new(2, 5, 1), new(3, 4, 1),
        new(0, 6, 2),
        new(0, 8, 1), new(1, 9, 1),
        new(1, 9, 1), new(2, 5, 1), new(3, 10, 2),
        new(3, 4, 1),
        new(0, 0, 1), new(2, 5, 1), new(3, 4, 1),
        new(1, 3, 1),
    ];

    // Clip rows: (clip index, blend window). FactorLength 0 = standalone clip;
    // an overlapping pair shares one window and Blend receives the factor.
    private static readonly ClipRow[] s_clipRows =
    [
        new(0, 0, 0),
        new(1, 3, 4), new(2, 3, 4),
        new(2, 0, 0),
        new(3, 0, 0),
        new(4, 0, 0),
        new(5, 29, 18), new(6, 29, 18),
        new(5, 0, 0),
        new(1, 0, 0),
        new(7, 76, 47), new(8, 76, 47),
    ];

    private static readonly VitalsTrack[] s_trackData = [new(1), new(2), new(3), new(4)];

    private static readonly VitalsClip[] s_clipData =
        [new(1), new(2), new(3), new(5), new(8), new(13), new(21), new(34), new(55)];

    public static ReadOnlySpan<uint> RegionStarts => s_regionStarts;
    public static ReadOnlySpan<RegionRow> RegionRows => s_regionRows;
    public static ReadOnlySpan<TrackRow> TrackRows => s_trackRows;
    public static ReadOnlySpan<ClipRow> ClipRows => s_clipRows;
    public static ReadOnlySpan<VitalsTrack> TrackData => s_trackData;
    public static ReadOnlySpan<VitalsClip> ClipData => s_clipData;
    public static int MaxActiveTracks => 3;

    public void Blend(in VitalsClip first, in VitalsClip second, float factor, out VitalsClip result)
        => result = new VitalsClip(first.Amount * (1f - factor) + second.Amount * factor);
}

public static class GeneratedTimeline<TTrack, TClip, TData>
    where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
    where TClip : unmanaged
    where TData : struct, IForwardTracks<TTrack, TClip, TData>
{
    public static void Forward(ref TData data, params ReadOnlySpan<uint> ticks)
    {
        // Static-abstract fetches are generic-dictionary indirections — take
        // them once per call, never inside the per-track loop.
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        // One hoisted resolution buffer per call — sized to the timeline's
        // known maximum, exactly what generated code would emit. Blending is
        // fused into iteration: each blending track collapses to one clip the
        // moment the consumer reaches it.
        Span<TClip> resolved = stackalloc TClip[TTrack.MaxActiveTracks];

        foreach (var tick in ticks)
        {
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;

            var row = regionRows[region];

            var tracks = new ForwardTracks<TTrack, TClip>(
                tick,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                resolved);

            data.Forward(tick, in tracks, ref data);
        }
    }
}

// Runtime-authored timeline: the same CSR tables as the generated flavor,
// built at run time from AddTrack/AddClip. Each closed generic type assigns
// sequential ushort indices, so one player can hold several timeline
// instances (Timeline<HealthTrack, HealthClip, Player> A = ..., B = ...;
// A.Index == 0, B.Index == 1).
public sealed class Timeline<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
    where TData : struct, IForwardTracks<TTrack, TClip, TData>
{
    private static int s_nextIndex;

    private readonly List<TTrack> _tracks = [];
    private readonly List<(int Track, TClip Clip, uint Start, uint End)> _clips = [];
    private bool _built;

    private uint[] _regionStarts = [];
    private RegionRow[] _regionRows = [];
    private TrackRow[] _trackRows = [];
    private ClipRow[] _clipRows = [];
    private TTrack[] _trackData = [];
    private TClip[] _clipData = [];
    private int _maxActiveTracks;

    public ushort Index { get; }

    public Timeline() => Index = (ushort)(Interlocked.Increment(ref s_nextIndex) - 1);

    public int AddTrack(in TTrack track)
    {
        _built = false;
        _tracks.Add(track);
        return _tracks.Count - 1;
    }

    public void AddClip(int track, in TClip clip, uint start, uint end)
    {
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "Clip end must be after its start.");

        _built = false;
        _clips.Add((track, clip, start, end));
    }

    // Event sweep: clip edges become region boundaries; each region records
    // its active tracks, and an overlapping pair shares one blend window.
    public void Build()
    {
        var cuts = new SortedSet<uint>();
        foreach (var clip in _clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var regionStarts = cuts.ToArray();
        var clipRows = new List<ClipRow>();
        var trackRows = new List<TrackRow>();
        var regionRows = new RegionRow[regionStarts.Length - 1];
        var clipData = new TClip[_clips.Count];

        for (var i = 0; i < _clips.Count; i++)
            clipData[i] = _clips[i].Clip;

        var maxActive = 0;

        for (var r = 0; r < regionRows.Length; r++)
        {
            var lo = regionStarts[r];
            var rowStart = trackRows.Count;

            for (var t = 0; t < _tracks.Count; t++)
            {
                var first = -1;
                var second = -1;

                for (var c = 0; c < _clips.Count; c++)
                {
                    var clip = _clips[c];
                    if (clip.Track != t || clip.Start > lo || clip.End <= lo)
                        continue;

                    if (first < 0)
                        first = c;
                    else if (second < 0)
                        second = c;
                    else
                        throw new NotSupportedException("At most two overlapping clips per track per region.");
                }

                if (first < 0)
                    continue;

                var clipStart = clipRows.Count;

                if (second < 0)
                {
                    clipRows.Add(new ClipRow((ushort)first, 0, 0));
                    trackRows.Add(new TrackRow((ushort)t, (ushort)clipStart, 1));
                }
                else
                {
                    var a = _clips[first];
                    var b = _clips[second];
                    var factorStart = a.Start > b.Start ? a.Start : b.Start;
                    var factorEnd = a.End < b.End ? a.End : b.End;
                    clipRows.Add(new ClipRow((ushort)first, factorStart, factorEnd - factorStart));
                    clipRows.Add(new ClipRow((ushort)second, factorStart, factorEnd - factorStart));
                    trackRows.Add(new TrackRow((ushort)t, (ushort)clipStart, 2));
                }
            }

            var count = trackRows.Count - rowStart;
            regionRows[r] = new RegionRow((ushort)rowStart, (ushort)count);

            if (count > maxActive)
                maxActive = count;
        }

        _regionStarts = regionStarts;
        _regionRows = regionRows;
        _trackRows = [.. trackRows];
        _clipRows = [.. clipRows];
        _trackData = [.. _tracks];
        _clipData = clipData;
        _maxActiveTracks = maxActive;
        _built = true;
    }

    public void Forward(ref TData data, params ReadOnlySpan<uint> ticks)
    {
        if (!_built)
            throw new InvalidOperationException("Call Build() before playback.");

        // Same rule as the generated shell: hoist every table once per call.
        var starts = _regionStarts.AsSpan();
        var regionRows = _regionRows.AsSpan();
        var trackRows = _trackRows.AsSpan();
        var clipRows = _clipRows.AsSpan();
        var trackData = _trackData.AsSpan();
        var clipData = _clipData.AsSpan();
        Span<TClip> resolved = stackalloc TClip[_maxActiveTracks];

        foreach (var tick in ticks)
        {
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;

            var row = regionRows[region];

            var tracks = new ForwardTracks<TTrack, TClip>(
                tick,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                resolved);

            data.Forward(tick, in tracks, ref data);
        }
    }
}

// Sparse ushort dispatch: what does it cost to turn a timeline index into
// generated code? See benchmarks/Dispatch/Generated/*.g.cs for the fixtures.
public struct DispatchSink
{
    public long Sum;
    public int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Visit(int slot)
    {
        Sum += slot + 1;
        Count++;
    }

    public readonly DispatchReceipt Result => new(Sum, Count);
}

public readonly record struct DispatchReceipt(long Sum, int Count);

public interface IRadixFixture
{
    static abstract ReadOnlySpan<ushort> Indices { get; }
    static abstract void Linear(ushort index, ref DispatchSink sink);
    static abstract void Binary(ushort index, ref DispatchSink sink);
    static abstract void FlatSwitch(ushort index, ref DispatchSink sink);
    static abstract void Radix8(ushort index, ref DispatchSink sink);
    static abstract void Radix4(ushort index, ref DispatchSink sink);
    static abstract void DenseFp(ushort index, ref DispatchSink sink);
}

public interface IFusedFixture
{
    static abstract ReadOnlySpan<ushort> Indices { get; }
    static abstract void FusedSparse(ushort index, uint tick, ref DispatchSink sink);
    static abstract void FusedDense(int id, uint tick, ref DispatchSink sink);
    static abstract void TwoLevelFp(ushort index, uint tick, ref DispatchSink sink);
}
