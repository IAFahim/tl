using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

// ---------------------------------------------------------------------------
// The redesigned consumer surface: "work in Tracks".
//
// Exactly THREE interfaces. IBlend lives on the track payload; IForward and
// IBackward live on the consumer's data type and receive ONE callback per
// non-empty tick, carrying the blend-resolved Tracks view. Signature order is
// the user's convention: `ref data` FIRST, then `in` payloads, tick LAST.
//
//   Timeline<HealthTrack, HealthClip>.Build(b => ...)
//   var pb = Timeline.Start(index);
//   pb = Timeline.Forward(index, in pb, ref player, tick);
//
// The "frame" is never materialized, and blending is resolved before the
// consumer runs: each active track exposes exactly ONE clip — an original, or
// the IBlend result of an overlapping pair held in a stackalloc'd slot.
// Consumers never see weights, pairs, or blend windows.
//
// Movement facts are per-work, not callbacks: each TrackWork carries an
// Enter/Stay/Exit ClipState (pinned semantics below), so one switch replaces
// a family of push hooks. The aggregate PlaybackFlags word carries only
// lifecycle and completion facts: Started/Stopped, LastLoopFrame, Completed.
//
// PINNED SEMANTICS (each one receipt-checked under --verify):
// - Enter: this step crossed the work's start edge (forward: prevEff <
//   clip.Start <= tEff, with the wrapped-span rule; backward mirror: entering
//   through the END edge). Sequentially it coincides with the first active
//   frame.
// - Exit: POSITIONAL — the destination is the work's last active frame
//   (forward: t == End-1; backward mirror: t == clip.Start, the last frame
//   visited moving backward). Exit takes precedence over Enter when both
//   apply (one-frame windows, jumps landing on the last frame): every visit's
//   final frame is always Exit.
// - Stay otherwise. A blend-resolved work reports the pair's OUTER window
//   (min Start, max End). Fully-crossed clips during multi-cycle jumps are
//   invisible (not in the destination rows). Stateless sampling carries no
//   movement, so it never reports Enter — Exit is positional and still does.
// - Effects on boundary frames: fixtures and the domain example apply work
//   ONLY on Stay — Enter/Exit frames notify but do not accumulate (the
//   Vitals.Forward switch below). The engine does not enforce this; it is the
//   consumer's switch.
// - Empty ticks: NO callback. The hook fires only when Count > 0. Playback
//   still updates (tick/cycles/flags).
// - Lifecycle: Start(index, at) mints a fresh Playback with Started set and
//   positions silently. default(Playback) is uninitialized: Forward/Backward
//   with a pb lacking Started throws InvalidOperationException BEFORE any
//   callback. Stop(index, in pb) returns pb | Stopped (no callbacks; requires
//   Started, else throws). Forward/Backward on a Stopped pb throws before any
//   callback. A new Start clears lifecycle bits fresh.
// - Flags: non-looping forward sets Completed at destination >= duration-1
//   (backward: == 0). Looping sets LastLoopFrame when the destination lands
//   ON the loop's last local frame (tEff == duration-1). Cycles: ushort,
//   forward adds wraps exactly, backward subtracts saturating at 0; forward
//   overflow past ushort.MaxValue throws before callbacks.
// ---------------------------------------------------------------------------

[Flags]
public enum PlaybackFlags : ushort
{
    None          = 0,
    Started       = 1 << 0,
    Stopped       = 1 << 1,
    LastLoopFrame = 1 << 2,
    Completed     = 1 << 3,
}

public enum ClipState : byte
{
    Enter,
    Stay,
    Exit,
}

// An 8-byte blittable value (Sequential: uint Tick, ushort Cycles, ushort
// Flags): passed `in`, returned in registers, snapshot-friendly for save
// games and rewind ring buffers. PINNED DECISION: uint Tick + ushort Cycles —
// the design's ushort Tick / uint Cycles swap would cap ticks at 65,535,
// which the 65,536-op benchmark fixture literally exceeds; same 8 bytes.
[StructLayout(LayoutKind.Sequential)]
public readonly struct Playback
{
    public readonly uint Tick;
    public readonly ushort Cycles;
    public readonly PlaybackFlags Flags;

    internal Playback(uint tick, ushort cycles, PlaybackFlags flags)
    {
        Tick = tick;
        Cycles = cycles;
        Flags = flags;
    }

    public bool Has(PlaybackFlags flags) => (Flags & flags) == flags;
}

// Caller-owned region cache for single-tick stepping, kept BETWEEN calls:
// `Playback` stays 8 bytes; this side state is opt-in per caller. The cache
// validates against the registry Entry itself — a rebuilt timeline is a NEW
// Entry, and loop mode is per-entry immutable, so ReferenceEquals replaces
// both the owner and the revision checks of the earlier design — plus the
// source tick (a repositioned or snapshot-restored Playback must re-search).
// Purely advisory: any mismatch falls back to searching and can never change
// results. The runtime registry path only; generated timelines bake their
// own schedule.
public struct Cursor
{
    internal object? Owner;
    internal uint Tick;
    internal int Region;
}

public interface IBlend<TClip>
    where TClip : struct
{
    void Blend(in TClip first, in TClip second, float t, out TClip result);
}

public interface IForward<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Forward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}

public interface IBackward<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Backward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}

// True [Start, End) window of one clip instance — authored alongside the
// tables, off the hot sampling path. Region cuts and clip edges can differ:
// a clip spanning several regions is still one edge.
public readonly record struct ClipEdge(uint Start, uint End);

// The movement span of one step, distilled for per-work facts: where the
// step came from (PrevEff), its direction, and the wrap shape of the span.
// Stateless sampling has no movement, so those paths pass all-false and
// works carry positional facts only (Exit is positional and still fires).
internal readonly record struct MovementSpan(uint PrevEff, bool Backward, bool Wrapped, bool Full)
{
    // Was `boundary` crossed by the step's span? A plain span covers
    // (PrevEff, tEff] forward or (tEff, PrevEff] backward; a wrapped span is
    // two ranges (the same expressions with || instead of &&); full coverage
    // (a multi-cycle jump) crossed everything. A repeated position crosses
    // nothing, which is what keeps Enter silent on stateless sampling.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Crossed(uint boundary, uint tEff)
    {
        if (Full)
            return true;
        if (Backward)
            return Wrapped ? boundary > tEff || boundary <= PrevEff
                           : boundary > tEff && boundary <= PrevEff;
        return Wrapped ? boundary > PrevEff || boundary <= tEff
                       : boundary > PrevEff && boundary <= tEff;
    }
}

// One step, both directions. The aggregate flags word is lifecycle plus
// completion facts only; movement facts live per work in TrackWork.State.
internal static class PlaybackCore
{
    public static void RequireRunnable(in Playback playback)
    {
        if (!playback.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
        if (playback.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
    }

    public static Playback Advance<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
        => Advance(
            in from, backward, loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved,
            -1, out _);

    // `regionHint` is a CALLER-VALIDATED region for the effective position
    // of `from.Tick` (a persistent Cursor); -1 means none. `finalRegion`
    // returns the region of the last processed tick's effective position —
    // pass it back as the next call's hint after storing `result.Tick`
    // alongside it. An empty tick span returns `from` and echoes the hint
    // unchanged, so a valid cursor survives it.
    public static Playback Advance<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved,
        int regionHint, out int finalRegion)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        var duration = starts[^1];
        var state = from;
        var previousRegion = regionHint;
        // Only the caller-validated across-call hint may probe the +-1
        // neighborhood in small tables; the call-local cursor keeps its
        // existing scan-from-zero behavior there.
        var hinted = regionHint >= 0;

        foreach (var tick in ticks)
        {
            var (tEff, prevEff, cycles, wrapped, full) = Position(state.Tick, tick, duration, loops, backward);

            var region = Locate(starts, tEff, previousRegion, hinted);
            hinted = false;
            var row = regionRows[region];

            // Forward wraps are counted exactly; the cycle capacity guard
            // throws before this tick's callback, like the old 26-bit guard.
            if (!backward && cycles > ushort.MaxValue - state.Cycles)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Playback cycle capacity exceeded.");
            var newCycles = backward
                ? (ushort)(state.Cycles - Math.Min(state.Cycles, cycles))
                : (ushort)(state.Cycles + cycles);

            var flags = PlaybackFlags.Started;
            if (loops)
            {
                if (duration != 0 && tEff == duration - 1)
                    flags |= PlaybackFlags.LastLoopFrame;
            }
            else if (duration == 0 || (backward ? tEff == 0 : tEff >= duration - 1))
            {
                flags |= PlaybackFlags.Completed;
            }

            var next = new Playback(tick, newCycles, flags);

            // Empty ticks: no callback. Playback still updated above.
            if (row.TrackCount > 0)
            {
                var tracks = new Tracks<TTrack, TClip>(
                    tEff,
                    trackRows.Slice(row.TrackStart, row.TrackCount),
                    clipRows, trackData, clipData, resolved,
                    edges,
                    new MovementSpan(prevEff, backward, wrapped, full));

                // The hook sees the EFFECTIVE tick — normalized on looping
                // timelines, exactly the position the view's works describe
                // (Playback.Tick keeps the raw authored destination).
                if (backward)
                    data.Backward(ref data, in tracks, in tEff);
                else
                    data.Forward(ref data, in tracks, in tEff);
            }

            state = next;
            previousRegion = region;
        }

        finalRegion = previousRegion;
        return state;
    }

    // Sampling only, no movement facts: the stateless path.
    public static void Sample<TTrack, TClip, TData>(
        bool backward, bool loops, ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        var duration = starts[^1];
        var wrap = loops && duration != 0;
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
                region = Locate(starts, localTick, region);
            }

            var row = regionRows[region];
            if (row.TrackCount == 0)
                continue;

            // Stateless sampling has no movement: positional facts only. The
            // span carries the destination as its own origin — a repeated
            // position crosses nothing — so Enter can never fire, while the
            // direction flag keeps Exit's positional mirror honest.
            var tracks = new Tracks<TTrack, TClip>(
                localTick,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                resolved,
                edges,
                new MovementSpan(localTick, backward, Wrapped: false, Full: false));

            if (backward)
                data.Backward(ref data, in tracks, in localTick);
            else
                data.Forward(ref data, in tracks, in localTick);
        }
    }

    // Effective positions, wrap counts, and coverage shape for one step.
    // Forward steps count wraps exactly (a multi-cycle jump is "full"
    // coverage: every boundary crossed). Backward steps count wraps exactly
    // while destinations descend; a destination above the previous effective
    // position is the expressed single wrap past zero, and Cycles saturates
    // at zero rather than going negative.
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

    // `anySize` lets a caller-validated across-call hint use the +-1
    // neighborhood probe even in small tables (the call-local cursor stays
    // on the scan-from-zero path there, matching measured behavior).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Locate(ReadOnlySpan<uint> starts, uint tick, int previousRegion, bool anySize = false)
    {
        if (previousRegion >= 0 && (anySize || starts.Length > 16))
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
    // Worst-case blended tracks in one region: the scratch a call must
    // reserve. Standalone clips resolve in place and need no slot, so a
    // zero-blend timeline requires no blend scratch at all.
    static abstract int MaxActiveBlends { get; }
    static abstract bool Loops { get; }
}

// Bounded blend scratch: the convenient stack path reserves at most
// StackBytes of blend-resolution storage per call; timelines whose worst
// case exceeds the budget (or callers that prefer one buffer for many
// calls) use the scratch-buffer overloads. Never a pool — pooling is not
// zero allocation, and each simultaneous or reentrant call needs its own
// buffer.
internal static class BlendScratch
{
    internal const int StackBytes = 4096;

    public static int StackCount<TClip>(int maxActiveBlends) where TClip : struct
    {
        var capacity = StackBytes / Unsafe.SizeOf<TClip>();
        if ((uint)maxActiveBlends > (uint)capacity)
            throw new InvalidOperationException(
                "Blend scratch exceeds the stack byte budget; use the scratch-buffer overload.");
        return maxActiveBlends;
    }
}

// The per-tick view: only Count and enumeration — the aggregate status word
// is gone (its facts are per work now), and the hook fires only when Count
// is positive.
public readonly ref struct Tracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly uint _tick;
    private readonly ReadOnlySpan<TrackRow> _trackRows;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly Span<TClip> _resolved;
    private readonly ReadOnlySpan<ClipEdge> _clipEdges;
    private readonly MovementSpan _movement;

    internal Tracks(
        uint tick,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved,
        ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement)
    {
        _tick = tick;
        _trackRows = trackRows;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _resolved = resolved;
        _clipEdges = clipEdges;
        _movement = movement;
    }

    public int Count => _trackRows.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(_tick, _trackRows, _clipRows, _trackData, _clipData, _resolved, _clipEdges, _movement);

    public ref struct Enumerator
    {
        private readonly uint _tick;
        private readonly ReadOnlySpan<TrackRow> _trackRows;
        private readonly ReadOnlySpan<ClipRow> _clipRows;
        private readonly ReadOnlySpan<TTrack> _trackData;
        private readonly ReadOnlySpan<TClip> _clipData;
        private readonly Span<TClip> _resolved;
        private readonly ReadOnlySpan<ClipEdge> _clipEdges;
        private readonly MovementSpan _movement;
        private int _i;
        // Ordinal of the last blend this enumerator resolved: blend results
        // take scratch slots in visit order, so only blended tracks consume
        // scratch. -1 until the first blend is reached.
        private int _blendSlot;

        internal Enumerator(
            uint tick,
            ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
            ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
            Span<TClip> resolved,
            ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement)
        {
            _tick = tick;
            _trackRows = trackRows;
            _clipRows = clipRows;
            _trackData = trackData;
            _clipData = clipData;
            _resolved = resolved;
            _clipEdges = clipEdges;
            _movement = movement;
            _i = -1;
            _blendSlot = -1;
        }

        public readonly TrackWork<TTrack, TClip> Current
            => new(_trackRows[_i], _tick, _clipRows, _trackData, _clipData, _resolved, _clipEdges, _movement, _blendSlot);

        // Resolution fused with traversal: each blending track collapses to
        // one clip the moment it is reached. Works that are never visited are
        // never blended — and never take a scratch slot.
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
            var slot = ++_blendSlot;
            _trackData[row.TrackIndex].Blend(
                in _clipData[first.ClipIndex],
                in _clipData[second.ClipIndex],
                factor,
                out _resolved[slot]);

            return true;
        }
    }
}

// One resolved (track, clip) pair: Index is the track's authored index, Track
// points into the payload table, and Clip points into the table for an
// original clip or into the resolved buffer for a blended one. Either way the
// consumer sees exactly one clip. State is the per-work movement fact.
public readonly ref struct TrackWork<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    private readonly TrackRow _row;
    private readonly uint _tick;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly ReadOnlySpan<TClip> _resolved;
    private readonly ReadOnlySpan<ClipEdge> _clipEdges;
    private readonly MovementSpan _movement;
    private readonly int _slot;

    internal TrackWork(
        TrackRow row, uint tick, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<TClip> resolved,
        ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement, int slot)
    {
        _row = row;
        _tick = tick;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _resolved = resolved;
        _clipEdges = clipEdges;
        _movement = movement;
        _slot = slot;
    }

    public ushort Index => _row.TrackIndex;

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

    // The per-work movement fact — the aggregate status word's replacement:
    // - Exit: POSITIONAL. Forward: destination == window End-1 (the clip's
    //   last active frame); backward mirror: destination == clip.Start (the
    //   last frame visited moving backward). Exit takes precedence over
    //   Enter, so every visit's final frame is Exit even for one-frame
    //   windows and jumps that land on the last frame.
    // - Enter: the step crossed the entry edge — forward through Start
    //   (prevEff < Start <= tEff, wrapped-span rule), backward through End.
    //   Sequentially it coincides with the first active frame. A stateless
    //   sample or repeated position crosses nothing: no Enter.
    // - Stay otherwise.
    //
    // CONVENTION (pinned word-for-word by the --verify per-work oracle):
    // a blend-resolved work reports the pair's OUTER window — min Start and
    // max End of the two clips. ClipRow.ClipIndex therefore indexes both the
    // payload table and ClipEdges: one payload row per authored instance, no
    // dedup (Timeline.Build guarantees the same).
    public ClipState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            uint start, end;
            if (_row.ClipCount == 1)
            {
                var edge = _clipEdges[_clipRows[_row.ClipStart].ClipIndex];
                start = edge.Start;
                end = edge.End;
            }
            else
            {
                var first = _clipEdges[_clipRows[_row.ClipStart].ClipIndex];
                var second = _clipEdges[_clipRows[_row.ClipStart + 1].ClipIndex];
                start = first.Start < second.Start ? first.Start : second.Start;
                end = first.End > second.End ? first.End : second.End;
            }

            if (_movement.Backward ? _tick == start : _tick == end - 1)
                return ClipState.Exit;

            return _movement.Crossed(_movement.Backward ? end : start, _tick) ? ClipState.Enter : ClipState.Stay;
        }
    }
}

// The clip payload for the Vitals fixture.
public readonly struct VitalsClip
{
    public readonly float Amount;

    public VitalsClip(float amount) => Amount = amount;
}

// The domain example: apply work only on Stay; Enter/Exit frames notify but
// do not accumulate. Backward is the exact inverse on sequential walks, so a
// forward walk rewound restores the state (boundary frames are notification
// only — see the pinned semantics).
public struct Vitals :
    IForward<VitalsTrack, VitalsClip, Vitals>,
    IBackward<VitalsTrack, VitalsClip, Vitals>,
    IForward<LoopVitalsTrack, VitalsClip, Vitals>,
    IBackward<LoopVitalsTrack, VitalsClip, Vitals>
{
    public float Health;
    public long Ticks;
    public int Count;
    public int Back;

    public readonly Receipt Result => new(Health, Ticks, Count);

    // Blend-ignorant on purpose: every active track has exactly one resolved
    // clip — an original or a blend result, indistinguishable here.
    public void Forward(ref Vitals data, in Tracks<VitalsTrack, VitalsClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    data.Ticks += work.Track.Offset;
                    data.Health += work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    // entering: notify only — no accumulation on the boundary frame
                    break;

                case ClipState.Exit:
                    // leaving: the destination is the clip's last active frame
                    break;
            }
        }

        data.Count++;
    }

    public void Backward(ref Vitals data, in Tracks<VitalsTrack, VitalsClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    data.Ticks -= work.Track.Offset;
                    data.Health -= work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
        }

        data.Back++;
    }

    public void Forward(ref Vitals data, in Tracks<LoopVitalsTrack, VitalsClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    data.Ticks += work.Track.Offset;
                    data.Health += work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
        }

        data.Count++;
    }

    public void Backward(ref Vitals data, in Tracks<LoopVitalsTrack, VitalsClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    data.Ticks -= work.Track.Offset;
                    data.Health -= work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
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

    // Cut facts per region, precomputed from the clip edges (bit 1 = the
    // region starts on a clip start, bit 2 = on a clip end, bit 4 = the
    // region ends on a clip end). The redesigned engine no longer reads
    // these on the hot path — the aggregate movement bits are gone — but the
    // tables stay: the frozen emitter cross-checks them at emission time.
    private static readonly byte[] s_regionFlags =
    [
        1, 1 | 4, 2 | 4, 2, 1 | 4, 1 | 2 | 4,
        1 | 2 | 4, 1 | 2 | 4, 2, 1 | 4, 1 | 2 | 4, 1 | 2 | 4, 2,
    ];

    // Track rows: (track index, slice of s_clipRows), one row per region in
    // track order; the pool below grows in first-use order across regions.
    private static readonly TrackRow[] s_trackRows =
    [
        new(0, 0, 1),
        new(0, 0, 1), new(1, 1, 2), new(3, 3, 1),
        new(1, 4, 1), new(3, 3, 1),
        new(2, 5, 1), new(3, 6, 1),
        new(0, 7, 2),
        new(0, 9, 1), new(1, 10, 1),
        new(1, 11, 1), new(2, 12, 1), new(3, 13, 2),
        new(3, 15, 1),
        new(0, 16, 1), new(2, 17, 1), new(3, 18, 1),
        new(1, 19, 1),
    ];

    // Clip rows: (authored clip index, blend window). FactorLength 0 =
    // standalone clip; an overlapping pair shares one window and Blend
    // receives the factor. ClipIndex indexes BOTH s_clipData and
    // s_clipEdges — one payload row per authored instance, no dedup, the
    // same invariant Timeline.Build guarantees — so a work's true window is
    // always ClipEdges[ClipIndex] (a pair: the outer window of its two
    // edges). That is what TrackWork.State reports.
    private static readonly ClipRow[] s_clipRows =
    [
        new(0, 0, 0),
        new(5, 3, 4), new(6, 3, 4),
        new(13, 0, 0),
        new(6, 0, 0),
        new(10, 0, 0),
        new(14, 0, 0),
        new(1, 29, 18), new(2, 29, 18),
        new(3, 0, 0),
        new(7, 0, 0),
        new(8, 0, 0),
        new(11, 0, 0),
        new(15, 76, 47), new(16, 76, 47),
        new(17, 0, 0),
        new(4, 0, 0),
        new(12, 0, 0),
        new(18, 0, 0),
        new(9, 0, 0),
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

    // One payload per authored instance (in s_clipEdges order), so ClipIndex
    // is an edge index too; amounts are the same values the old deduplicated
    // payload table produced, keeping every receipt's arithmetic identical.
    private static readonly VitalsClip[] s_clipData =
    [
        new(1), new(13), new(21), new(13), new(1),
        new(2), new(3), new(2), new(2), new(3),
        new(8), new(8), new(8),
        new(5), new(5), new(34), new(55), new(5), new(5),
    ];

    public static ReadOnlySpan<uint> RegionStarts => s_regionStarts;
    public static ReadOnlySpan<RegionRow> RegionRows => s_regionRows;
    public static ReadOnlySpan<byte> RegionFlags => s_regionFlags;
    public static ReadOnlySpan<TrackRow> TrackRows => s_trackRows;
    public static ReadOnlySpan<ClipRow> ClipRows => s_clipRows;
    public static ReadOnlySpan<ClipEdge> ClipEdges => s_clipEdges;
    public static ReadOnlySpan<VitalsTrack> TrackData => s_trackData;
    public static ReadOnlySpan<VitalsClip> ClipData => s_clipData;
    public static int MaxActiveTracks => 3;
    // At most one blended track is active in any region (regions 1, 5 and 7
    // each hold exactly one pair); the other concurrent rows are standalone
    // clips that resolve in place.
    public static int MaxActiveBlends => 1;
    public static bool Loops => false;

    public void Blend(in VitalsClip first, in VitalsClip second, float t, out VitalsClip result)
        => result = new VitalsClip(first.Amount * (1f - t) + second.Amount * t);
}

// The same tables, marked looping: ticks wrap modulo the duration and each
// wrap moves the Cycles counter instead of setting Completed.
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
    public static int MaxActiveBlends => VitalsTrack.MaxActiveBlends;
    public static bool Loops => true;

    public void Blend(in VitalsClip first, in VitalsClip second, float t, out VitalsClip result)
        => result = new VitalsClip(first.Amount * (1f - t) + second.Amount * t);
}

// The compiled-tables flavor: per-closure shells over the shared engine, new
// hooks, no global registry. This is the frozen/receipts path.
public static class GeneratedTimeline<TTrack, TClip>
    where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
    where TClip : unmanaged
{
    public static Playback Start(uint at = 0) => new(at, 0, PlaybackFlags.Started);

    // Sampling only, no movement facts: the stateless path. The stack
    // overload reserves only the timeline's worst-case blend scratch (within
    // the byte budget); zero-blend timelines reserve nothing.
    public static void Forward<TData>(ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Forward(ref data, scratch, ticks);
    }

    // Caller-provided scratch: one buffer can serve many calls, but each
    // simultaneous or reentrant call needs its own.
    public static void Forward<TData>(ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));

        // Static-abstract fetches are generic-dictionary indirections — take
        // them once per call, never inside the per-track loop.
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TData>(
            backward: false, TTrack.Loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, scratch);
    }

    public static void Backward<TData>(ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Backward(ref data, scratch, ticks);
    }

    public static void Backward<TData>(ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TData>(
            backward: true, TTrack.Loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, scratch);
    }

    public static Playback Forward<TData>(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        return Forward(in from, ref data, scratch, ticks);
    }

    public static Playback Forward<TData>(in Playback from, ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        PlaybackCore.RequireRunnable(in from);
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TData>(
            in from, backward: false, TTrack.Loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, scratch);
    }

    public static Playback Backward<TData>(in Playback from, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        return Backward(in from, ref data, scratch, ticks);
    }

    public static Playback Backward<TData>(in Playback from, ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        PlaybackCore.RequireRunnable(in from);
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackRows = TTrack.TrackRows;
        var clipRows = TTrack.ClipRows;
        var edges = TTrack.ClipEdges;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TData>(
            in from, backward: true, TTrack.Loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, scratch);
    }
}

// Typed handle to an authored track: a ref struct, so it cannot be stored or
// escape the authoring callback. Only TimelineBuilder.Track mints one.
public readonly ref struct TrackRef
{
    internal ushort Index { get; }

    internal TrackRef(ushort index) => Index = index;
}

// Authoring surface: a stack-scoped builder handed to the definition
// delegate, so no reference to a timeline can escape into game code — the
// only thing that leaves authoring is the assigned global ushort index.
// The definition delegate receives the builder by value (the
// SpanAction<T,TArg> shape), so every authoring mutation must land behind
// the one Authoring reference.
public readonly ref struct TimelineBuilder<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    internal sealed class Authoring
    {
        public List<TTrack> Tracks = [];
        public List<(ushort Track, TClip Clip, uint Start, uint End)> Clips = [];
        public bool Loops;
    }

    internal readonly Authoring _state;

    // The key parameter keeps construction internal: a parameterless struct
    // constructor would have to be public, so this is the closest an
    // "internal constructor" gets for a ref struct. Only Timeline.Build can
    // construct a builder: authoring lives inside the callback and dies when
    // it returns.
    internal readonly struct Key;

    internal TimelineBuilder(Key key) => _state = new Authoring();

    public TrackRef Track(in TTrack track)
    {
        if (_state.Tracks.Count == ushort.MaxValue)
            throw new InvalidOperationException("Track capacity exceeded.");
        _state.Tracks.Add(track);
        return new TrackRef((ushort)(_state.Tracks.Count - 1));
    }

    public void Clip(in TrackRef track, in TClip clip, uint start, uint end)
    {
        if ((uint)track.Index >= (uint)_state.Tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(track), track.Index, "TrackRef does not name a track of this timeline.");
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "Clip end must be after its start.");
        if (_state.Clips.Count == ushort.MaxValue)
            throw new InvalidOperationException("Clip capacity exceeded.");

        _state.Clips.Add((track.Index, clip, start, end));
    }

    public void Looping() => _state.Loops = true;
}

public delegate void TimelineBuild<TTrack, TClip>(scoped TimelineBuilder<TTrack, TClip> builder)
    where TTrack : struct
    where TClip : struct;

public delegate void TimelineBuild<TTrack, TClip, TSource>(scoped TimelineBuilder<TTrack, TClip> builder, TSource source)
    where TTrack : struct
    where TClip : struct;

// Runtime-authored timelines: the same CSR tables as the generated flavor,
// built at run time inside a Build callback and registered in the PROCESS-
// GLOBAL index space (see the non-generic Timeline below). Only the assigned
// ushort index escapes.
public static class Timeline<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
{
    // The typed payload half of one registered timeline; stored as plain
    // object in the non-generic Entry and rehydrated without a cast through
    // Unsafe.As inside the bridge (the bridge knows the closure).
    internal sealed class Tables
    {
        public required TTrack[] TrackData { get; init; }
        public required TClip[] ClipData { get; init; }
    }

    public static ushort Build(TimelineBuild<TTrack, TClip> build)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(default);
        build(builder);
        return Compile(builder._state);
    }

    public static ushort Build<TSource>(TSource source, TimelineBuild<TTrack, TClip, TSource> build)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(default);
        build(builder, source);
        return Compile(builder._state);
    }

    // Event sweep: clip edges become region boundaries; each region records
    // its active tracks, and an overlapping pair shares one blend window.
    private static ushort Compile(TimelineBuilder<TTrack, TClip>.Authoring authoring)
    {
        var cuts = new SortedSet<uint> { 0 };
        foreach (var clip in authoring.Clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var regionStarts = cuts.ToArray();
        var clipRows = new List<ClipRow>();
        var trackRows = new List<TrackRow>();
        var regionRows = new RegionRow[regionStarts.Length];
        var clipData = new TClip[authoring.Clips.Count];
        var clipEdges = new ClipEdge[authoring.Clips.Count];

        for (var i = 0; i < authoring.Clips.Count; i++)
        {
            clipData[i] = authoring.Clips[i].Clip;
            clipEdges[i] = new ClipEdge(authoring.Clips[i].Start, authoring.Clips[i].End);
        }

        var maxActive = 0;
        var maxBlends = 0;

        for (var r = 0; r < regionRows.Length; r++)
        {
            var lo = regionStarts[r];
            var rowStart = trackRows.Count;

            for (var t = 0; t < authoring.Tracks.Count; t++)
            {
                var first = -1;
                var second = -1;

                for (var c = 0; c < authoring.Clips.Count; c++)
                {
                    var clip = authoring.Clips[c];
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
                    if (authoring.Clips[first].Start > authoring.Clips[second].Start)
                        (first, second) = (second, first);
                    var a = authoring.Clips[first];
                    var b = authoring.Clips[second];
                    var factorStart = a.Start > b.Start ? a.Start : b.Start;
                    var factorEnd = a.End < b.End ? a.End : b.End;
                    clipRows.Add(new ClipRow(checked((ushort)first), factorStart, factorEnd - factorStart));
                    clipRows.Add(new ClipRow(checked((ushort)second), factorStart, factorEnd - factorStart));
                    trackRows.Add(new TrackRow(checked((ushort)t), checked((ushort)clipStart), 2));
                }
            }

            var count = trackRows.Count - rowStart;
            regionRows[r] = new RegionRow(checked((ushort)rowStart), checked((ushort)count));

            if (count > maxActive)
                maxActive = count;
            // Blend rows are the pairs: exactly two active clips on one
            // track. Only those consume scratch slots at play time.
            var blends = 0;
            for (var t = rowStart; t < trackRows.Count; t++)
                if (trackRows[t].ClipCount == 2)
                    blends++;
            if (blends > maxBlends)
                maxBlends = blends;
        }

        return Timeline.Register(new Timeline.Entry
        {
            RegionStarts = regionStarts,
            RegionRows = regionRows,
            TrackRows = [.. trackRows],
            ClipRows = [.. clipRows],
            ClipEdges = clipEdges,
            Payload = new Tables
            {
                TrackData = [.. authoring.Tracks],
                ClipData = clipData,
            },
            MaxActiveTracks = maxActive,
            MaxActiveBlends = maxBlends,
            Loops = authoring.Loops,
            Duration = (ushort)regionStarts[^1],
            Binder = BindType,
        });
    }

    // Caller-provided blend scratch: one buffer can serve many calls, but
    // each simultaneous or reentrant call needs its own. Length is in TClip
    // elements and must cover the timeline's worst-case blends (checked
    // before any callback); zero-blend timelines accept an empty span.
    // Lives here — not on the non-generic hub — because only this closure
    // can name TClip.
    public static unsafe Playback Forward<TData>(ushort index, in Playback playback, ref TData data, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Timeline.Live(index);
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = entry.Bind(typeof(TData), Timeline.TokenOf<TData>());
        return run.ForwardScratch(entry, in playback, ScratchPointer(scratch), scratch.Length, Unsafe.AsPointer(ref data), ticks);
    }

    public static unsafe Playback Backward<TData>(ushort index, in Playback playback, ref TData data, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Timeline.Live(index);
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = entry.Bind(typeof(TData), Timeline.TokenOf<TData>());
        return run.BackwardScratch(entry, in playback, ScratchPointer(scratch), scratch.Length, Unsafe.AsPointer(ref data), ticks);
    }

    // The raw pointer the scratch runners receive; null for empty spans
    // (zero-blend timelines reserve nothing).
    private static unsafe void* ScratchPointer(Span<TClip> scratch)
        => scratch.IsEmpty ? null : (void*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(scratch));

    // The one-time, per-(entry, TData) bridge instantiation. The non-generic
    // hub cannot constrain its TData against this closure (the CS0699-style
    // discovery the design anticipated), so binding goes through a generic
    // method closed by reflection once per (entry, TData) pair; the bound
    // function pointers are cached on the entry and the steady state is a
    // plain pointer call.
    // AOT: reflection is unavailable without dynamic code — Bind<TData>(index)
    // is the static, instantiation-at-call-site path for ahead-of-time
    // consumers; this automatic path guards and throws a named error.
    private static readonly MethodInfo s_bind =
        typeof(Timeline<TTrack, TClip>).GetMethod(nameof(BindData), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(BindData));

    // The explicit, reflection-free bind: one static generic call per
    // (timeline index, consumer type), so the instantiation exists at a
    // call site the AOT compiler can see. Repeat calls are idempotent
    // (Install overwrites the same token slot).
    public static void Bind<TData>(ushort index)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new InvalidOperationException($"Timeline index {index} does not belong to the <{typeof(TTrack).Name}, {typeof(TClip).Name}> closure.");
        BindData<TData>(entry);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
        Justification = "The generic method is closed over the consumer type; AOT consumers use Bind<TData> instead.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:MakeGenericMethod",
        Justification = "Guarded by the IsDynamicCodeSupported check; AOT consumers use Bind<TData> instead.")]
    private static void BindType(Type data, Timeline.Entry entry)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException(
                $"The automatic (entry, consumer) bridge bind needs dynamic code; under NativeAOT call " +
                $"{typeof(Timeline<TTrack, TClip>).Name}.Bind<TConsumer>(index) once per pair instead.");
        s_bind.MakeGenericMethod(data).Invoke(null, [entry]);
    }

    private static void BindData<TData>(Timeline.Entry entry)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
        => Bridge<TData>.Install(entry);

    private static unsafe class Bridge<TData>
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        public static void Install(Timeline.Entry entry)
            => entry.Install(
                DataToken<TData>.Id,
                new Timeline.Entry.Run(&RunForward, &RunBackward, &RunForwardCursor, &RunBackwardCursor, &RunForwardScratch, &RunBackwardScratch, &SampleForward, &SampleBackward));

        // The caller's `ref data` arrives as a stack-pinned void* and is
        // rehydrated here: the pointer mutates the caller's instance, zero
        // allocation, no boxing.
        private static Playback RunForward(Timeline.Entry entry, in Playback from, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }

        private static Playback RunBackward(Timeline.Entry entry, in Playback from, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }

        // Cursor-primed runners: the caller's `ref cursor` rides through as a
        // second stack-pinned void*. The cache validates against the entry
        // itself (a rebuilt timeline is a new Entry; loop mode is per-entry
        // immutable) plus the source tick; any mismatch falls back to
        // searching and can never change results. Stored only after the call
        // returns; an empty tick span echoes a valid region through unchanged.
        private static Playback RunForwardCursor(Timeline.Entry entry, in Playback from, void* cursor, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var cache = ref Unsafe.AsRef<Cursor>(cursor);
            var valid = ReferenceEquals(cache.Owner, entry) && cache.Tick == from.Tick;
            var hint = valid ? cache.Region : -1;

            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var result = PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved,
                hint, out var region);

            cache = new Cursor { Owner = entry, Tick = result.Tick, Region = region };
            return result;
        }

        private static Playback RunBackwardCursor(Timeline.Entry entry, in Playback from, void* cursor, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var cache = ref Unsafe.AsRef<Cursor>(cursor);
            var valid = ReferenceEquals(cache.Owner, entry) && cache.Tick == from.Tick;
            var hint = valid ? cache.Region : -1;

            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var result = PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved,
                hint, out var region);

            cache = new Cursor { Owner = entry, Tick = result.Tick, Region = region };
            return result;
        }

        // Caller-scratch runners: the caller's buffer arrives raw (pointer
        // plus element count — the hub cannot name TClip) and is rehydrated
        // here where the closure knows it. The public overload checked the
        // length before the call; the engine is otherwise identical.
        private static Playback RunForwardScratch(Timeline.Entry entry, in Playback from, void* scratch, int scratchLength, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var resolved = new Span<TClip>(scratch, scratchLength);

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }

        private static Playback RunBackwardScratch(Timeline.Entry entry, in Playback from, void* scratch, int scratchLength, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var resolved = new Span<TClip>(scratch, scratchLength);

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }

        private static void SampleForward(Timeline.Entry entry, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TData>(
                backward: false, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }

        private static void SampleBackward(Timeline.Entry entry, void* data, ReadOnlySpan<uint> ticks)
        {
            ref var consumer = ref Unsafe.AsRef<TData>(data);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TData>(
                backward: true, entry.Loops, ticks, ref consumer,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, resolved);
        }
    }
}

// Per-TData static token id: a CAS counter, so every closed consumer type
// gets one small id used to index the per-entry binding cache.
internal static class DataToken<TData>
{
    public static int Id;
}

internal static class TokenCounter
{
    private static int s_next;

    public static int Take()
    {
        int current;
        int next;
        do
        {
            current = Volatile.Read(ref s_next);
            next = current + 1;
        }
        while (Interlocked.CompareExchange(ref s_next, next, current) != current);

        return next;
    }
}

// The PROCESS-GLOBAL registry: ONE ushort index space across every
// (TTrack, TClip) closure, so a player can hold indices of heterogeneous
// timelines side by side. None is reserved, capacity is 65,535 usable
// indices, tombstones are never reused, and Destroy stays here.
//
// Dispatch: the hub's TData cannot be constrained against the entry's
// unknown closure, so each entry lazily binds and caches per-TData unsafe
// function pointers created from a bridge generic instantiation of the
// closure's engine (the caller's `ref data` rides through as a
// stack-pinned void* and is rehydrated inside; zero allocation, no boxing,
// first call per (entry, TData) pair does the one-time reflection bind).
// All lifecycle and bounds checks run BEFORE the pointer call.
public static unsafe partial class Timeline
{
    public const ushort None = ushort.MaxValue;

    internal sealed unsafe class Entry
    {
        public required uint[] RegionStarts { get; init; }
        public required RegionRow[] RegionRows { get; init; }
        public required TrackRow[] TrackRows { get; init; }
        public required ClipRow[] ClipRows { get; init; }
        public required ClipEdge[] ClipEdges { get; init; }

        // The closure-typed half (Timeline<TTrack, TClip>.Tables), stored as
        // object and rehydrated without a cast by the bridge.
        public required object Payload { get; init; }

        // The timeline's duration as a ushort (the authored final cut).
        // Durations beyond 65,535 ticks truncate — the fixture timelines all
        // fit; a wider return type is a future widening if ever needed.
        public required ushort Duration { get; init; }

        public required int MaxActiveTracks { get; init; }
        public required int MaxActiveBlends { get; init; }
        public required bool Loops { get; init; }

        public required Action<Type, Entry> Binder { get; init; }

        // One bound (forward, backward, cursor-primed forward/backward,
        // caller-scratch forward/backward, sample-forward, sample-backward)
        // pointer set per consumer token id. The cursor variants carry the
        // caller's stack-pinned Cursor alongside the data pointer; the
        // scratch variants carry the caller's blend buffer as a raw pointer
        // plus element count (the hub cannot name TClip).
        public readonly struct Run(
            delegate*<Entry, in Playback, void*, ReadOnlySpan<uint>, Playback> forward,
            delegate*<Entry, in Playback, void*, ReadOnlySpan<uint>, Playback> backward,
            delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> forwardCursor,
            delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> backwardCursor,
            delegate*<Entry, in Playback, void*, int, void*, ReadOnlySpan<uint>, Playback> forwardScratch,
            delegate*<Entry, in Playback, void*, int, void*, ReadOnlySpan<uint>, Playback> backwardScratch,
            delegate*<Entry, void*, ReadOnlySpan<uint>, void> sampleForward,
            delegate*<Entry, void*, ReadOnlySpan<uint>, void> sampleBackward)
        {
            public readonly delegate*<Entry, in Playback, void*, ReadOnlySpan<uint>, Playback> Forward = forward;
            public readonly delegate*<Entry, in Playback, void*, ReadOnlySpan<uint>, Playback> Backward = backward;
            public readonly delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> ForwardCursor = forwardCursor;
            public readonly delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> BackwardCursor = backwardCursor;
            public readonly delegate*<Entry, in Playback, void*, int, void*, ReadOnlySpan<uint>, Playback> ForwardScratch = forwardScratch;
            public readonly delegate*<Entry, in Playback, void*, int, void*, ReadOnlySpan<uint>, Playback> BackwardScratch = backwardScratch;
            public readonly delegate*<Entry, void*, ReadOnlySpan<uint>, void> SampleForward = sampleForward;
            public readonly delegate*<Entry, void*, ReadOnlySpan<uint>, void> SampleBackward = sampleBackward;

            public bool IsBound => Forward != null;
        }

        private readonly object _bindGate = new();
        private Run[] _runs = [];

        public void Install(int token, Run run)
        {
            lock (_bindGate)
            {
                if (token >= _runs.Length)
                {
                    var grown = new Run[Math.Max(token + 1, _runs.Length * 2)];
                    Array.Copy(_runs, grown, _runs.Length);
                    Volatile.Write(ref _runs, grown);
                }

                _runs[token] = run;
            }
        }

        public Run Lookup(int token)
        {
            var runs = Volatile.Read(ref _runs);
            return (uint)token < (uint)runs.Length ? runs[token] : default;
        }

        public Run Bind(Type data, int token)
        {
            var run = Lookup(token);
            if (run.IsBound)
                return run;

            // The one-time reflection bind; MakeGenericMethod validates that
            // the consumer type implements this entry's closure hooks.
            Binder(data, this);

            var bound = Lookup(token);
            return bound.IsBound
                ? bound
                : throw new InvalidOperationException($"The consumer type {data} does not implement this timeline's IForward/IBackward closure.");
        }
    }

    private static readonly object s_gate = new();
    private static Entry?[] s_slots = [];
    private static int s_nextIndex;

    internal static ushort Register(Entry entry)
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextIndex);
            if (next >= None)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref s_nextIndex, next + 1, next) != next);

        lock (s_gate)
        {
            if (s_slots.Length <= next)
            {
                var grown = new Entry?[s_slots.Length == 0 ? 16 : s_slots.Length * 2];
                Array.Copy(s_slots, grown, s_slots.Length);
                Volatile.Write(ref s_slots, grown);
            }

            s_slots[next] = entry;
        }

        return (ushort)next;
    }

    // Dead, unknown, or the None sentinel reject before any callback or
    // state change.
    internal static Entry Live(ushort index)
    {
        if (index == None)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
        var slots = Volatile.Read(ref s_slots);
        if ((uint)index >= (uint)slots.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
        return Volatile.Read(ref slots[index])
            ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
    }

    public static bool IsValid(ushort index)
    {
        if (index == None)
            return false;
        var slots = Volatile.Read(ref s_slots);
        return (uint)index < (uint)slots.Length && Volatile.Read(ref slots[index]) != null;
    }

    public static ushort Duration(ushort index) => Live(index).Duration;

    public static bool IsLooping(ushort index) => Live(index).Loops;

    public static void Destroy(ushort index)
    {
        _ = Live(index);

        lock (s_gate)
            s_slots[index] = null;
    }

    // Positions silently: no movement happens, so nothing enters or exits.
    // A fresh Start clears the lifecycle bits: only Started is set.
    public static Playback Start(ushort index, uint at = 0)
    {
        _ = Live(index);
        return new Playback(at, 0, PlaybackFlags.Started);
    }

    // No callbacks; requires Started, else throws. Idempotent on an already
    // stopped playback (the Stopped bit just stays).
    public static Playback Stop(ushort index, in Playback playback)
    {
        _ = Live(index);
        if (!playback.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Cannot stop a playback that was never started.");
        return new Playback(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);
    }

    public static Playback Forward<TData>(ushort index, in Playback playback, ref TData data, in uint tick)
        where TData : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Forward(index, in playback, ref data, ticks);
    }

    public static Playback Forward<TData>(ushort index, in Playback playback, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        return run.Forward(entry, in playback, Unsafe.AsPointer(ref data), ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref TData data, in uint tick)
        where TData : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, ref data, ticks);
    }

    // Cursor-primed entry points: same playback, plus a caller-owned region
    // cache carried between calls. Any mismatch (different entry — index
    // switch, rebuild, loop-mode change — or a repositioned Playback) falls
    // back to searching. Separate callers keep separate cursors; sharing one
    // is legal but only ever validated against its own owner.
    public static Playback Forward<TData>(ushort index, in Playback playback, ref Cursor cursor, ref TData data, in uint tick)
        where TData : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Forward(index, in playback, ref cursor, ref data, ticks);
    }

    public static Playback Forward<TData>(ushort index, in Playback playback, ref Cursor cursor, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        return run.ForwardCursor(entry, in playback, Unsafe.AsPointer(ref cursor), Unsafe.AsPointer(ref data), ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref Cursor cursor, ref TData data, in uint tick)
        where TData : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, ref cursor, ref data, ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref Cursor cursor, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        return run.BackwardCursor(entry, in playback, Unsafe.AsPointer(ref cursor), Unsafe.AsPointer(ref data), ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        return run.Backward(entry, in playback, Unsafe.AsPointer(ref data), ticks);
    }

    // Sampling only, no movement facts and no lifecycle: the stateless path.
    public static void Forward<TData>(ushort index, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        run.SampleForward(entry, Unsafe.AsPointer(ref data), ticks);
    }

    public static void Backward<TData>(ushort index, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        var run = entry.Bind(typeof(TData), TokenOf<TData>());
        run.SampleBackward(entry, Unsafe.AsPointer(ref data), ticks);
    }

    internal static int TokenOf<TData>()
    {
        var id = DataToken<TData>.Id;
        if (id != 0)
            return id;

        // First use of this consumer type anywhere: take a token and publish
        // it. A benign race can burn one token id; ids only need uniqueness.
        Interlocked.CompareExchange(ref DataToken<TData>.Id, TokenCounter.Take(), 0);
        return DataToken<TData>.Id;
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
