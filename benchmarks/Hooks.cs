using System.Runtime.CompilerServices;

namespace Tl.Hooks;

public interface IClip;
public interface ITimelineForward { void OnForward(in Frame frame); }

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

// The composed API shape:
//   Timeline<HealthTrack, HealthClip, Player>.Forward(ref player, t0, t1, t2, t3)
//
// One callback per tick per direction, always through IForwardTracks or
// IBackwardTracks. The "frame" is never materialized, and blending is
// resolved before the consumer runs: each active track exposes exactly ONE
// clip — an original, or the IBlend result of an overlapping pair held in a
// stackalloc'd slot. Consumers never see weights, pairs, or blend windows.
//
// Movement facts (entering, leaving, completing) are NOT callbacks: the
// engine folds them into the returned Playback status word and the Tracks
// view, so one branch on bits replaces a family of push hooks.
public interface IForward<TClip, TData>
    where TClip : struct
    where TData : struct
{
    void Forward(in TClip clip, uint tick, ref TData data);
}

public interface IBackward<TClip, TData>
    where TClip : struct
    where TData : struct
{
    void Backward(in TClip clip, uint tick, ref TData data);
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
    void Forward(uint tick, in Tracks<TTrack, TClip> tracks, ref TData data);
}

public interface IBackwardTracks<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Backward(uint tick, in Tracks<TTrack, TClip> tracks, ref TData data);
}

// Movement facts live in the top six bits; completed cycles fill the rest.
// An 8-byte Playback is one qword: passed `in`, returned in registers,
// blittable for save games and rewind ring buffers.
[Flags]
public enum PlaybackFlags : uint
{
    None     = 0,
    Enter    = 1u << 26,
    First    = 1u << 27,
    Active   = 1u << 28,
    Last     = 1u << 29,
    Complete = 1u << 30,
    Exit     = 1u << 31,
}

public readonly struct Playback
{
    public const uint MaxCycles = 0x03FF_FFFFu;
    public readonly uint Tick;
    private readonly uint _packed;

    public Playback(uint tick, uint cycles, PlaybackFlags flags)
    {
        if (cycles > MaxCycles)
            throw new ArgumentOutOfRangeException(nameof(cycles));
        if (((uint)flags & MaxCycles) != 0)
            throw new ArgumentOutOfRangeException(nameof(flags));
        Tick = tick;
        _packed = cycles | (uint)flags;
    }

    public uint Cycles => _packed & 0x03FF_FFFFu;
    public PlaybackFlags Flags => (PlaybackFlags)(_packed & 0xFC00_0000u);
    public bool Has(PlaybackFlags flag) => (_packed & (uint)flag) != 0;

    // Positions silently: no movement happens, so nothing enters or exits.
    public static Playback Start(uint at = 0) => new(at, 0, 0);
}

// True [Start, End) window of one clip instance — authored alongside the
// tables, off the hot sampling path. Region cuts and clip edges can differ:
// a clip spanning several regions is still one edge.
public readonly record struct ClipEdge(uint Start, uint End);

// One step, both engines, both directions. First/Last/Active are positional
// facts about the destination tick; Enter/Exit are facts about the movement
// span; Complete marks the far end of a non-looping timeline.
internal static class PlaybackCore
{
    public static Playback Advance<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<byte> regionFlags,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForwardTracks<TTrack, TClip, TData>, IBackwardTracks<TTrack, TClip, TData>
    {
        var duration = starts[^1];
        var state = from;
        var previousRegion = -1;

        foreach (var tick in ticks)
        {
            var (tEff, prevEff, cycles, wrapped, full) = Position(state.Tick, tick, duration, loops, backward);

            var region = Locate(starts, tEff, previousRegion);

            var row = regionRows[region];

            var flags = Flags(
                edges, regionFlags, starts, region, row.TrackCount, previousRegion,
                tEff, prevEff, duration, cycles, wrapped, full, backward, loops);

            if (!backward && cycles > Playback.MaxCycles - state.Cycles)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Playback cycle capacity exceeded.");
            var newCycles = backward ? state.Cycles - Math.Min(state.Cycles, cycles) : state.Cycles + cycles;
            var next = new Playback(tick, newCycles, flags);

            var tracks = new Tracks<TTrack, TClip>(
                tEff, flags,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows, trackData, clipData, resolved);

            if (backward)
                data.Backward(tEff, in tracks, ref data);
            else
                data.Forward(tEff, in tracks, ref data);

            state = next;
            previousRegion = region;
        }

        return state;
    }

    public static Playback AdvanceClips<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<byte> regionFlags,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
        where TTrack : struct, IBlend<TClip>
        where TClip : unmanaged, IForward<TClip, TData>, IBackward<TClip, TData>
        where TData : struct
    {
        var duration = starts[^1];
        var state = from;
        var previousRegion = -1;

        foreach (var tick in ticks)
        {
            var (tEff, prevEff, cycles, wrapped, full) = Position(state.Tick, tick, duration, loops, backward);

            var region = Locate(starts, tEff, previousRegion);

            var row = regionRows[region];

            var flags = Flags(
                edges, regionFlags, starts, region, row.TrackCount, previousRegion,
                tEff, prevEff, duration, cycles, wrapped, full, backward, loops);

            if (!backward && cycles > Playback.MaxCycles - state.Cycles)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Playback cycle capacity exceeded.");
            var newCycles = backward ? state.Cycles - Math.Min(state.Cycles, cycles) : state.Cycles + cycles;
            var next = new Playback(tick, newCycles, flags);

            var tracks = new Tracks<TTrack, TClip>(
                tEff, flags,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows, trackData, clipData, resolved);

            // Clip-level hooks: one call per active clip (blend-resolved).
            foreach (var item in tracks)
            {
                var clip = item.Clip;
                if (backward)
                    clip.Backward(in clip, tEff, ref data);
                else
                    clip.Forward(in clip, tEff, ref data);
            }

            state = next;
            previousRegion = region;
        }

        return state;
    }

    // Effective positions, wrap counts, and coverage shape for one step.
    // Forward steps count wraps exactly (a multi-cycle jump is "full"
    // coverage: every clip entered and exited). Backward steps count wraps
    // exactly while destinations descend; a destination above the previous
    // effective position is the expressed single wrap past zero, and Cycles
    // saturates at zero rather than going negative.
    private static (uint TEff, uint PrevEff, uint Cycles, bool Wrapped, bool Full) Position(
        uint previous, uint tick, uint duration, bool loops, bool backward)
    {
        if (!loops || duration == 0)
            return (tick, previous, 0u, false, false);

        var prevEff = previous % duration;
        var tEff = tick % duration;

        if (!backward)
        {
            var cycles = tick >= previous
                ? tick / duration - previous / duration
                : tEff < prevEff ? 1u : 0u;
            return (tEff, prevEff, cycles, cycles == 1, cycles >= 2);
        }

        if (tick <= previous)
        {
            var cycles = previous / duration - tick / duration;
            return (tEff, prevEff, cycles, cycles == 1, cycles >= 2);
        }

        return tEff > prevEff
            ? (tEff, prevEff, 1u, true, false)
            : (tEff, prevEff, 0u, false, false);
    }

    // Region cut bits: which clip edges coincide with each region boundary.
    private const byte StartCut = 1;
    private const byte EndCut = 2;
    private const byte RegionEndIsClipEnd = 4;

    // Clip starts and ends are always region cuts, so every non-wrapped step
    // — sequential or jump — answers its flags from cut bits: positional
    // facts from the destination region, movement facts from the boundaries
    // crossed between the two positions. Only wraps scan the edge table.
    private static PlaybackFlags Flags(
        ReadOnlySpan<ClipEdge> edges, ReadOnlySpan<byte> regionFlags,
        ReadOnlySpan<uint> starts, int region, int trackCount, int previousRegion,
        uint tEff, uint prevEff, uint duration,
        uint cycles, bool wrapped, bool full, bool backward, bool loops)
    {
        if (duration == 0)
            return loops ? PlaybackFlags.None : PlaybackFlags.Complete;

        if (wrapped || full)
            return StepFlags(edges, prevEff, tEff, duration, backward, loops, wrapped, full);

        var flags = PlaybackFlags.None;

        if (trackCount > 0)
            flags |= PlaybackFlags.Active;

        var rs = starts[region];
        var re = region + 1 < starts.Length ? starts[region + 1] : duration;
        var here = regionFlags[region];

        if (tEff == rs && (here & StartCut) != 0)
            flags |= PlaybackFlags.First;
        if (tEff == re - 1 && (here & RegionEndIsClipEnd) != 0)
            flags |= PlaybackFlags.Last;

        if (!loops && (backward ? tEff == 0 : tEff >= duration - 1))
            flags |= PlaybackFlags.Complete;

        if (backward ? tEff >= prevEff : tEff <= prevEff)
            return flags;

        if (previousRegion < 0)
            previousRegion = RegionOf(starts, prevEff);
        var from = backward ? region : previousRegion;
        var to = backward ? previousRegion : region;

        for (var i = from + 1; i <= to; i++)
        {
            if ((flags & (PlaybackFlags.Enter | PlaybackFlags.Exit)) == (PlaybackFlags.Enter | PlaybackFlags.Exit))
                break;

            var cut = regionFlags[i];

            if (backward)
            {
                // Entering through right edges (a boundary that is an end),
                // exiting through left edges (a boundary that is a start).
                if ((cut & EndCut) != 0)
                    flags |= PlaybackFlags.Enter;
                if ((cut & StartCut) != 0)
                    flags |= PlaybackFlags.Exit;
            }
            else
            {
                if ((cut & StartCut) != 0)
                    flags |= PlaybackFlags.Enter;
                if ((cut & EndCut) != 0)
                    flags |= PlaybackFlags.Exit;
            }
        }

        return flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Locate(ReadOnlySpan<uint> starts, uint tick, int previousRegion)
    {
        if (previousRegion >= 0 && starts.Length > 16)
        {
            if (tick >= starts[previousRegion])
            {
                var next = previousRegion + 1;
                if (next == starts.Length || tick < starts[next])
                    return previousRegion;
                if (next + 1 == starts.Length || tick < starts[next + 1])
                    return next;
            }
            else if (previousRegion > 0 && tick >= starts[previousRegion - 1])
            {
                return previousRegion - 1;
            }
        }
        return RegionOf(starts, tick);
    }

    // Index of the last region start at or below the tick (clamped).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int RegionOf(ReadOnlySpan<uint> starts, uint tick)
    {
        if (starts.Length <= 16)
        {
            int region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= tick)
                region++;
            return region;
        }

        int lo = 0, hi = starts.Length - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (starts[mid] <= tick)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }

    private static PlaybackFlags StepFlags(
        ReadOnlySpan<ClipEdge> edges, uint prevEff, uint tEff, uint duration,
        bool backward, bool loops, bool wrapped, bool full)
    {
        var flags = PlaybackFlags.None;

        foreach (ref readonly var edge in edges)
        {
            var start = edge.Start;
            var end = edge.End;

            if (start <= tEff && tEff < end)
                flags |= PlaybackFlags.Active;
            if (start == tEff)
                flags |= PlaybackFlags.First;
            if (end - 1 == tEff)
                flags |= PlaybackFlags.Last;

            if (full)
            {
                flags |= PlaybackFlags.Enter | PlaybackFlags.Exit;
            }
            else if (backward)
            {
                // Entering through the clip's right edge, exiting through
                // its left: the mirror of forward movement.
                if (InSpan(end, tEff, prevEff, wrapped))
                    flags |= PlaybackFlags.Enter;
                if (InSpan(start, tEff, prevEff, wrapped))
                    flags |= PlaybackFlags.Exit;
            }
            else
            {
                if (InSpan(start, prevEff, tEff, wrapped))
                    flags |= PlaybackFlags.Enter;
                if (InSpan(end, prevEff, tEff, wrapped))
                    flags |= PlaybackFlags.Exit;
            }
        }

        if (!loops && (backward ? tEff == 0 : tEff >= duration - 1))
            flags |= PlaybackFlags.Complete;

        return flags;
    }

    // Span membership over (lo, hi]; a wrapped span is two ranges, which
    // degenerates to the same expression with || instead of &&.
    private static bool InSpan(uint v, uint lo, uint hi, bool wrapped)
        => wrapped ? v > lo || v <= hi : v > lo && v <= hi;
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
    static abstract ReadOnlySpan<byte> RegionFlags { get; }
    static abstract ReadOnlySpan<TrackRow> TrackRows { get; }
    static abstract ReadOnlySpan<ClipRow> ClipRows { get; }
    static abstract ReadOnlySpan<ClipEdge> ClipEdges { get; }
    static abstract ReadOnlySpan<TTrack> TrackData { get; }
    static abstract ReadOnlySpan<TClip> ClipData { get; }
    static abstract int MaxActiveTracks { get; }
    static abstract bool Loops { get; }
}

public readonly ref struct Tracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly uint _tick;
    private readonly PlaybackFlags _status;
    private readonly ReadOnlySpan<TrackRow> _trackRows;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly Span<TClip> _resolved;

    internal Tracks(
        uint tick, PlaybackFlags status,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
    {
        _tick = tick;
        _status = status;
        _trackRows = trackRows;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _resolved = resolved;
    }

    public int Count => _trackRows.Length;

    // The step's movement facts — the same bits the returned Playback holds.
    public PlaybackFlags Status => _status;

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
            _i = -1;
        }

        public readonly TrackItem<TTrack, TClip> Current
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
            var factor = first.FactorLength <= 1 ? 0.5f : (_tick - first.FactorStart) / (float)(first.FactorLength - 1);
            _trackData[row.TrackIndex].Blend(
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
public readonly ref struct TrackItem<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    private readonly TrackRow _row;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly ReadOnlySpan<TClip> _resolved;
    private readonly int _slot;

    internal TrackItem(
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

public readonly struct VitalsClip : IForward<VitalsClip, Vitals>, IBackward<VitalsClip, Vitals>
{
    public readonly float Amount;

    public VitalsClip(float amount) => Amount = amount;

    public void Forward(in VitalsClip clip, uint tick, ref Vitals data)
    {
        data.Health -= clip.Amount;
        data.Ticks += tick;
        data.Count++;
    }

    public void Backward(in VitalsClip clip, uint tick, ref Vitals data)
    {
        data.Health += clip.Amount;
        data.Ticks -= tick;
        data.Back++;
    }
}

public struct Vitals :
    IForwardTracks<VitalsTrack, VitalsClip, Vitals>,
    IBackwardTracks<VitalsTrack, VitalsClip, Vitals>,
    IForwardTracks<LoopVitalsTrack, VitalsClip, Vitals>,
    IBackwardTracks<LoopVitalsTrack, VitalsClip, Vitals>
{
    public float Health;
    public long Ticks;
    public int Count;
    public int Back;

    public readonly Receipt Result => new(Health, Ticks, Count);

    // Blend-ignorant on purpose: every active track has exactly one resolved
    // clip — an original or a blend result, indistinguishable here. Backward
    // is the exact inverse, so a forward walk rewound restores the state.
    public void Forward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref Vitals data)
    {
        foreach (var item in tracks)
        {
            data.Ticks += item.Track.Offset;
            data.Health += item.Clip.Amount;
        }

        data.Count++;
    }

    public void Backward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref Vitals data)
    {
        foreach (var item in tracks)
        {
            data.Ticks -= item.Track.Offset;
            data.Health -= item.Clip.Amount;
        }

        data.Back++;
    }

    public void Forward(uint tick, in Tracks<LoopVitalsTrack, VitalsClip> tracks, ref Vitals data)
    {
        foreach (var item in tracks)
        {
            data.Ticks += item.Track.Offset;
            data.Health += item.Clip.Amount;
        }

        data.Count++;
    }

    public void Backward(uint tick, in Tracks<LoopVitalsTrack, VitalsClip> tracks, ref Vitals data)
    {
        foreach (var item in tracks)
        {
            data.Ticks -= item.Track.Offset;
            data.Health -= item.Clip.Amount;
        }

        data.Back++;
    }
}

public struct VitalsTrack : ITrackTables<VitalsTrack, VitalsClip>, IBlend<VitalsClip>
{
    public readonly int Offset;

    public VitalsTrack(int offset) => Offset = offset;

    private static readonly uint[] s_regionStarts = [0, 3, 7, 11, 18, 29, 47, 76, 123, 200, 321, 515, 600];

    // Region r -> slice of s_trackRows; TrackCount 0 marks a gap.
    private static readonly RegionRow[] s_regionRows =
    [
        new(0, 1), new(1, 3), new(4, 2), new(6, 0), new(6, 2), new(8, 1),
        new(9, 2), new(11, 3), new(14, 0), new(14, 1), new(15, 3), new(18, 1), new(19, 0),
    ];

    // Cut facts per region, precomputed from the clip edges: bit 1 = the
    // region starts on a clip start, bit 2 = on a clip end, bit 4 = the
    // region ends on a clip end. Every non-wrapped step — sequential or
    // jump — reads flags from these bits; only wraps scan the edges.
    private static readonly byte[] s_regionFlags =
    [
        1, 1 | 4, 2 | 4, 2, 1 | 4, 1 | 2 | 4,
        1 | 2 | 4, 1 | 2 | 4, 2, 1 | 4, 1 | 2 | 4, 1 | 2 | 4, 2,
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

    // True clip windows, one per authored instance — the flag oracle.
    private static readonly ClipEdge[] s_clipEdges =
    [
        new(0, 7), new(29, 47), new(29, 47), new(47, 76), new(321, 515),
        new(3, 7), new(3, 11), new(47, 76), new(76, 123), new(515, 600),
        new(18, 29), new(76, 123), new(321, 515),
        new(3, 11), new(18, 29), new(76, 123), new(76, 123), new(200, 321), new(321, 515),
    ];

    private static readonly VitalsTrack[] s_trackData = [new(1), new(2), new(3), new(4)];

    private static readonly VitalsClip[] s_clipData =
        [new(1), new(2), new(3), new(5), new(8), new(13), new(21), new(34), new(55)];

    public static ReadOnlySpan<uint> RegionStarts => s_regionStarts;
    public static ReadOnlySpan<RegionRow> RegionRows => s_regionRows;
    public static ReadOnlySpan<byte> RegionFlags => s_regionFlags;
    public static ReadOnlySpan<TrackRow> TrackRows => s_trackRows;
    public static ReadOnlySpan<ClipRow> ClipRows => s_clipRows;
    public static ReadOnlySpan<ClipEdge> ClipEdges => s_clipEdges;
    public static ReadOnlySpan<VitalsTrack> TrackData => s_trackData;
    public static ReadOnlySpan<VitalsClip> ClipData => s_clipData;
    public static int MaxActiveTracks => 3;
    public static bool Loops => false;

    public void Blend(in VitalsClip first, in VitalsClip second, float factor, out VitalsClip result)
        => result = new VitalsClip(first.Amount * (1f - factor) + second.Amount * factor);
}

// The same tables, marked looping: ticks wrap modulo the duration and each
// crossed boundary moves the Cycles counter instead of setting Complete.
public struct LoopVitalsTrack : ITrackTables<LoopVitalsTrack, VitalsClip>, IBlend<VitalsClip>
{
    public readonly int Offset;

    public LoopVitalsTrack(int offset) => Offset = offset;

    private static readonly LoopVitalsTrack[] s_trackData = [new(1), new(2), new(3), new(4)];

    public static ReadOnlySpan<uint> RegionStarts => VitalsTrack.RegionStarts;
    public static ReadOnlySpan<RegionRow> RegionRows => VitalsTrack.RegionRows;
    public static ReadOnlySpan<byte> RegionFlags => VitalsTrack.RegionFlags;
    public static ReadOnlySpan<TrackRow> TrackRows => VitalsTrack.TrackRows;
    public static ReadOnlySpan<ClipRow> ClipRows => VitalsTrack.ClipRows;
    public static ReadOnlySpan<ClipEdge> ClipEdges => VitalsTrack.ClipEdges;
    public static ReadOnlySpan<LoopVitalsTrack> TrackData => s_trackData;
    public static ReadOnlySpan<VitalsClip> ClipData => VitalsTrack.ClipData;
    public static int MaxActiveTracks => VitalsTrack.MaxActiveTracks;
    public static bool Loops => true;

    public void Blend(in VitalsClip first, in VitalsClip second, float factor, out VitalsClip result)
        => result = new VitalsClip(first.Amount * (1f - factor) + second.Amount * factor);
}

public static class GeneratedTimeline<TTrack, TClip, TData>
    where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
    where TClip : unmanaged
    where TData : struct, IForwardTracks<TTrack, TClip, TData>, IBackwardTracks<TTrack, TClip, TData>
{
    public static Playback Start(uint at = 0) => Playback.Start(at);

    // Sampling only, no movement facts: the stateless path.
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

        var duration = starts[^1];
        var wrap = TTrack.Loops && duration != 0;
        var region = -1;
        foreach (var tick in ticks)
        {
            var localTick = wrap ? tick % duration : tick;
            if (starts.Length <= 16)
            {
                region = 0;
                while (region + 1 < starts.Length && starts[region + 1] <= localTick)
                    region++;
            }
            else
            {
                region = PlaybackCore.Locate(starts, localTick, region);
            }
            var row = regionRows[region];

            var tracks = new Tracks<TTrack, TClip>(
                localTick,
                PlaybackFlags.None,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                resolved);

            data.Forward(localTick, in tracks, ref data);
        }
    }

    public static Playback Forward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var regionFlags = TTrack.RegionFlags;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;
        Span<TClip> resolved = stackalloc TClip[TTrack.MaxActiveTracks];

        return PlaybackCore.Advance(
            in from, backward: false, TTrack.Loops, ticks, ref data,
            starts, regionRows, regionFlags, trackRows, clipRows, edges, trackData, clipData, resolved);
    }

    public static Playback Backward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var regionFlags = TTrack.RegionFlags;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;
        Span<TClip> resolved = stackalloc TClip[TTrack.MaxActiveTracks];

        return PlaybackCore.Advance(
            in from, backward: true, TTrack.Loops, ticks, ref data,
            starts, regionRows, regionFlags, trackRows, clipRows, edges, trackData, clipData, resolved);
    }
}

// Clip-level playback for simple consumers: TClip itself implements
// IForward/IBackward and receives one call per active (blend-resolved) clip.
public static class ClipTimeline<TTrack, TClip, TData>
    where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
    where TClip : unmanaged, IForward<TClip, TData>, IBackward<TClip, TData>
    where TData : struct
{
    public static Playback Start(uint at = 0) => Playback.Start(at);

    public static Playback Forward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
        => Run(in from, backward: false, ticks, ref data);

    public static Playback Backward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
        => Run(in from, backward: true, ticks, ref data);

    private static Playback Run(in Playback from, bool backward, ReadOnlySpan<uint> ticks, ref TData data)
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var regionFlags = TTrack.RegionFlags;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;
        Span<TClip> resolved = stackalloc TClip[TTrack.MaxActiveTracks];

        return PlaybackCore.AdvanceClips(
            in from, backward, TTrack.Loops, ticks, ref data,
            starts, regionRows, regionFlags, trackRows, clipRows, edges, trackData, clipData, resolved);
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
    where TData : struct, IForwardTracks<TTrack, TClip, TData>, IBackwardTracks<TTrack, TClip, TData>
{
    private static int s_nextIndex;

    private readonly List<TTrack> _tracks = [];
    private readonly List<(int Track, TClip Clip, uint Start, uint End)> _clips = [];
    private bool _built;

    private uint[] _regionStarts = [];
    private RegionRow[] _regionRows = [];
    private byte[] _regionFlags = [];
    private TrackRow[] _trackRows = [];
    private ClipRow[] _clipRows = [];
    private ClipEdge[] _clipEdges = [];
    private TTrack[] _trackData = [];
    private TClip[] _clipData = [];
    private int _maxActiveTracks;

    public ushort Index { get; }

    public bool IsLooping { get; set; }

    public Timeline()
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextIndex);
            if (next > ushort.MaxValue)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        } while (Interlocked.CompareExchange(ref s_nextIndex, next + 1, next) != next);
        Index = (ushort)next;
    }

    public int AddTrack(in TTrack track)
    {
        if (_tracks.Count == ushort.MaxValue)
            throw new InvalidOperationException("Track capacity exceeded.");
        _built = false;
        _tracks.Add(track);
        return _tracks.Count - 1;
    }

    public void AddClip(int track, in TClip clip, uint start, uint end)
    {
        if ((uint)track >= (uint)_tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(track));
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "Clip end must be after its start.");
        if (_clips.Count == ushort.MaxValue)
            throw new InvalidOperationException("Clip capacity exceeded.");

        _built = false;
        _clips.Add((track, clip, start, end));
    }

    // Event sweep: clip edges become region boundaries; each region records
    // its active tracks, and an overlapping pair shares one blend window.
    public void Build()
    {
        var cuts = new SortedSet<uint> { 0 };
        foreach (var clip in _clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var regionStarts = cuts.ToArray();
        var clipRows = new List<ClipRow>();
        var trackRows = new List<TrackRow>();
        var regionRows = new RegionRow[regionStarts.Length];
        var regionFlags = new byte[regionStarts.Length];
        var clipData = new TClip[_clips.Count];
        var clipEdges = new ClipEdge[_clips.Count];

        for (var i = 0; i < _clips.Count; i++)
        {
            clipData[i] = _clips[i].Clip;
            clipEdges[i] = new ClipEdge(_clips[i].Start, _clips[i].End);
        }

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
                    clipRows.Add(new ClipRow(checked((ushort)first), 0, 0));
                    trackRows.Add(new TrackRow(checked((ushort)t), checked((ushort)clipStart), 1));
                }
                else
                {
                    if (_clips[first].Start > _clips[second].Start)
                        (first, second) = (second, first);
                    var a = _clips[first];
                    var b = _clips[second];
                    var factorStart = a.Start > b.Start ? a.Start : b.Start;
                    var factorEnd = a.End < b.End ? a.End : b.End;
                    clipRows.Add(new ClipRow(checked((ushort)first), factorStart, factorEnd - factorStart));
                    clipRows.Add(new ClipRow(checked((ushort)second), factorStart, factorEnd - factorStart));
                    trackRows.Add(new TrackRow(checked((ushort)t), checked((ushort)clipStart), 2));
                }
            }

            var count = trackRows.Count - rowStart;
            regionRows[r] = new RegionRow(checked((ushort)rowStart), checked((ushort)count));

            // Cut facts: which clip edges sit on this region's boundaries.
            var rs = regionStarts[r];
            var re = r + 1 < regionStarts.Length ? regionStarts[r + 1] : 0;
            byte flag = 0;

            foreach (var clip in _clips)
            {
                if (clip.Start == rs)
                    flag |= 1;
                if (clip.End == rs)
                    flag |= 2;
                if (clip.End == re)
                    flag |= 4;
            }

            regionFlags[r] = flag;

            if (count > maxActive)
                maxActive = count;
        }

        _regionStarts = regionStarts;
        _regionRows = regionRows;
        _regionFlags = regionFlags;
        _trackRows = [.. trackRows];
        _clipRows = [.. clipRows];
        _clipEdges = clipEdges;
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

        var duration = starts[^1];
        var wrap = IsLooping && duration != 0;
        var region = -1;
        foreach (var tick in ticks)
        {
            var localTick = wrap ? tick % duration : tick;
            if (starts.Length <= 16)
            {
                region = 0;
                while (region + 1 < starts.Length && starts[region + 1] <= localTick)
                    region++;
            }
            else
            {
                region = PlaybackCore.Locate(starts, localTick, region);
            }
            var row = regionRows[region];

            var tracks = new Tracks<TTrack, TClip>(
                localTick,
                PlaybackFlags.None,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                resolved);

            data.Forward(localTick, in tracks, ref data);
        }
    }

    public Playback Start(uint at = 0) => Playback.Start(at);

    public Playback Forward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
    {
        if (!_built)
            throw new InvalidOperationException("Call Build() before playback.");

        var starts = _regionStarts.AsSpan();
        var regionRows = _regionRows.AsSpan();
        var regionFlags = _regionFlags.AsSpan();
        var trackRows = _trackRows.AsSpan();
        var clipRows = _clipRows.AsSpan();
        var edges = _clipEdges.AsSpan();
        var trackData = _trackData.AsSpan();
        var clipData = _clipData.AsSpan();
        Span<TClip> resolved = stackalloc TClip[_maxActiveTracks];

        return PlaybackCore.Advance(
            in from, backward: false, IsLooping, ticks, ref data,
            starts, regionRows, regionFlags, trackRows, clipRows, edges, trackData, clipData, resolved);
    }

    public Playback Backward(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
    {
        if (!_built)
            throw new InvalidOperationException("Call Build() before playback.");

        var starts = _regionStarts.AsSpan();
        var regionRows = _regionRows.AsSpan();
        var regionFlags = _regionFlags.AsSpan();
        var trackRows = _trackRows.AsSpan();
        var clipRows = _clipRows.AsSpan();
        var edges = _clipEdges.AsSpan();
        var trackData = _trackData.AsSpan();
        var clipData = _clipData.AsSpan();
        Span<TClip> resolved = stackalloc TClip[_maxActiveTracks];

        return PlaybackCore.Advance(
            in from, backward: true, IsLooping, ticks, ref data,
            starts, regionRows, regionFlags, trackRows, clipRows, edges, trackData, clipData, resolved);
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
