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
// IBackward live on the consumer's RESULT type and receive ONE callback per
// non-empty tick, carrying the blend-resolved Tracks view. The API v0.2 split:
// the data pair is an immutable input (read-only context, `in`) plus a
// mutable result (the accumulator, `ref`) — RESULT implements the hooks,
// INPUT never changes through a call. Callback order: tracks, input, tick,
// result; entry points keep the pair adjacent in the old `ref data` slot
// with ticks last (params must come last).
//
//   Timeline<HealthTrack, HealthClip>.Build(b => ...)
//   var pb = Timeline.Start(index);
//   pb = Timeline.Forward(index, in pb, in input, ref player, tick);
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

public interface IForward<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TInput : struct
    where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
{
    void Forward(in Tracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
}

public interface IBackward<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TInput : struct
    where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
{
    void Backward(in Tracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
}

// True [Start, End) window of one clip instance — authored alongside the
// tables, off the hot sampling path. Region cuts and clip edges can differ:
// a clip spanning several regions is still one edge.
public readonly record struct ClipEdge(uint Start, uint End);

// The movement span of one step, distilled for per-work facts: where the
// step came from (PrevEff), its direction, and the wrap shape of the span.
// Stateless sampling has no movement, so those paths pass all-false and
// works carry positional facts only (Exit is positional and still fires).
// EnterPossible is the prefix-count gate (faster queue #3): false when the
// crossed region range provably contains no start (forward) or end
// (backward) cut, so no work of this step can report Enter. Conservative
// by construction — true keeps the full Enter check, so an absent or
// empty CutCounts table never changes results.
//
// The per-work Enter test itself lives in WorkSlot's entry references: for
// a work ACTIVE at the destination (the only kind a region hands out) the
// crossed-span question reduces to ONE prev-compare — forward Enter iff
// PrevEff < window Start (activity pins Start <= tick), backward Enter iff
// PrevEff >= window End (activity pins tick < End); Wrapped and Full spans
// crossed the entry edge unconditionally.
//
// The four facts pack into one flag byte so the per-tick view handoff is a
// single store, not five fields.
[Flags]
internal enum MovementFlags : byte
{
    None = 0,
    Backward = 1 << 0,
    EnterPossible = 1 << 1,
    CrossedAlways = 1 << 2, // wrapped or full span: every boundary crossed
}

internal readonly record struct MovementSpan(uint PrevEff, MovementFlags Flags)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal MovementSpan(uint prevEff, bool backward, bool wrapped, bool full, bool enterPossible = true)
        : this(
            prevEff,
            (backward ? MovementFlags.Backward : 0)
                | (enterPossible ? MovementFlags.EnterPossible : 0)
                | (wrapped || full ? MovementFlags.CrossedAlways : 0))
    {
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

    public static Playback Advance<TTrack, TClip, TInput, TResult>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, in TInput input, ref TResult result,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved,
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
        => Advance(
            in from, backward, loops, ticks, in input, ref result,
            starts, regionRows, ReadOnlySpan<CutCounts>.Empty, trackData, clipData, resolved, workSlots,
            -1, out _);

    // `regionHint` is a CALLER-VALIDATED region for the effective position
    // of `from.Tick` (a persistent Cursor); -1 means none. `finalRegion`
    // returns the region of the last processed tick's effective position —
    // pass it back as the next call's hint after storing `result.Tick`
    // alongside it. An empty tick span returns `from` and echoes the hint
    // unchanged, so a valid cursor survives it. `cutCounts` (when non-empty)
    // gates the per-work Enter checks with two prefix-count loads per step.
    // `workSlots` is the BUILD-TIME materialized slot table, one WorkSlot
    // per track row: the active work set is constant inside a region, so
    // region r's view is simply workSlots.Slice(row.TrackStart,
    // row.TrackCount) — per tick there is no track-row walking, no
    // payload-map hops, no bundle copies, and no materialization work at
    // all (a first cut re-materialized the slots per region entry per CALL
    // and cost +8 ns/tick on a non-consuming callback — single-tick calls
    // are a region entry every time; see docs/benchmarks.md v0.3).
    public static Playback Advance<TTrack, TClip, TInput, TResult>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, in TInput input, ref TResult result,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<CutCounts> cutCounts,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved,
        ReadOnlySpan<WorkSlot> workSlots,
        int regionHint, out int finalRegion)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
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

            // Prefix-count gate: some start (forward) or end (backward) cut
            // lies in the crossed region range iff the cumulative counts
            // differ at the two ends — clip starts and ends are always
            // region cuts. Wraps and multi-cycle jumps keep the full scan,
            // and an absent table or an unlocated previous region cannot
            // answer, so the gate only ever turns the Enter check OFF when
            // provably nothing crossed.
            var enterPossible = wrapped || full || previousRegion < 0 || cutCounts.IsEmpty;
            if (!enterPossible)
                enterPossible = backward
                    ? cutCounts[previousRegion].Ends != cutCounts[region].Ends
                    : cutCounts[region].Starts != cutCounts[previousRegion].Starts;

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
                    workSlots.Slice(row.TrackStart, row.TrackCount),
                    tEff,
                    resolved,
                    trackData, clipData,
                    new MovementSpan(prevEff, backward, wrapped, full, enterPossible));

                // The hook sees the EFFECTIVE tick — normalized on looping
                // timelines, exactly the position the view's works describe
                // (Playback.Tick keeps the raw authored destination).
                if (backward)
                    result.Backward(in tracks, in input, in tEff, ref result);
                else
                    result.Forward(in tracks, in input, in tEff, ref result);
            }

            state = next;
            previousRegion = region;
        }

        finalRegion = previousRegion;
        return state;
    }

    // Sampling only, no movement facts: the stateless path.
    public static void Sample<TTrack, TClip, TInput, TResult>(
        bool backward, bool loops, ReadOnlySpan<uint> ticks, in TInput input, ref TResult result,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        Span<TClip> resolved,
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
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
                workSlots.Slice(row.TrackStart, row.TrackCount),
                localTick,
                resolved,
                trackData, clipData,
                new MovementSpan(localTick, backward, wrapped: false, full: false));

            if (backward)
                result.Backward(in tracks, in input, in localTick, ref result);
            else
                result.Forward(in tracks, in input, in localTick, ref result);
        }
    }

    // BUILD-TIME region materialization: one WorkSlot per track row of the
    // whole table, so region r's per-tick view is simply
    // slots.Slice(row.TrackStart, row.TrackCount). Blend-scratch ordinals
    // are dense PER REGION (region-local counters), matching the scratch
    // sizing (MaxActiveBlends covers any single region). Computed once:
    // the registry path runs this in Timeline.Compile (after storage
    // dedup, so aliased row runs share aliased slots), the generated-table
    // shell through a per-closure static cache, and a future generator can
    // bake the table at emission. NEVER per tick — the active work set is
    // constant for the lifetime of the tables.
    internal static WorkSlot[] MaterializeWorkSlots(
        ReadOnlySpan<TrackRow> trackRows,
        ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ushort> payloadMap,
        ReadOnlySpan<ClipEdge> clipEdges,
        ReadOnlySpan<RegionRow> regionRows)
    {
        var slots = new WorkSlot[trackRows.Length];
        for (var r = 0; r < regionRows.Length; r++)
        {
            var row = regionRows[r];
            var blendOrdinal = 0;
            for (var i = row.TrackStart; i < row.TrackStart + row.TrackCount; i++)
            {
                var trackRow = trackRows[i];
                var first = clipRows[trackRow.ClipStart];

                // Standalone clip: payload-resolved index, own window edges,
                // factor window zero (the single discriminator).
                if (trackRow.ClipCount != 2)
                {
                    var authored = first.ClipIndex;
                    slots[i] = new WorkSlot(
                        trackRow.TrackIndex,
                        payloadMap.IsEmpty ? authored : payloadMap[authored],
                        WorkSlot.Single,
                        clipEdges[authored].Start,
                        clipEdges[authored].End,
                        0u, 0u, 0);
                    continue;
                }

                // Blend pair: both payload indices resolved, the pair's
                // OUTER window (the State convention), the shared blend
                // window, and the next region-local scratch ordinal.
                var second = clipRows[trackRow.ClipStart + 1];
                var a = clipEdges[first.ClipIndex];
                var b = clipEdges[second.ClipIndex];
                slots[i] = new WorkSlot(
                    trackRow.TrackIndex,
                    payloadMap.IsEmpty ? first.ClipIndex : payloadMap[first.ClipIndex],
                    payloadMap.IsEmpty ? second.ClipIndex : payloadMap[second.ClipIndex],
                    a.Start < b.Start ? a.Start : b.Start,
                    a.End > b.End ? a.End : b.End,
                    first.FactorStart, first.FactorLength,
                    checked((ushort)blendOrdinal++));
            }
        }

        return slots;
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

// Cumulative cut facts per region (built once from the authored clip
// edges, checked conversions): counts[i].Starts/Ends = regions 0..i whose
// start position carries a clip start/end. Two 4-byte record loads answer
// "did the crossed region range contain a start/end cut?" without touching
// the per-work windows. Playback falls back to the full per-work check
// whenever the table is empty or the shape cannot be answered — the counts
// only ever switch the Enter check off, provably.
public readonly record struct CutCounts(ushort Starts, ushort Ends);

// One MATERIALIZED active track of a region: everything a per-tick view of
// the work needs, resolved at BUILD time (PlaybackCore.MaterializeWorkSlots
// — once per timeline, not per tick or per call) into a table parallel to
// the track rows. The active work set is constant inside a region — and for
// the lifetime of the tables; only the tick (blend factors, movement
// facts) varies per tick.
//   Index       the track's authored index — also its payload-table index
//               (the track payload table is one row per authored track)
//   First       singles: the payload-map-RESOLVED payload index of the one
//               clip (Second carries the Single sentinel); blends: the
//               first partner's resolved payload index
//   Second      blends: the second partner's resolved payload index
//   EnterF/EnterB  the work's OUTER window entry-reference edges — Start
//               (forward entry) and End (backward entry). For a work active
//               at the destination the crossed-span Enter test reduces to
//               ONE prev-compare against them (activity pins
//               Start <= tick < End); wrapped spans and multi-cycle jumps
//               crossed the entry edge unconditionally. The positional Exit
//               compares ride the same pair (tick == EnterB-1 forward,
//               tick == EnterF backward)
//   FactorStart/FactorLength  the blend window (FactorLength 0 marks a
//               standalone clip — the authored table convention)
//   BlendOrdinal  dense region-local scratch ordinal for blends, assigned
//               in slot order (visit order for a forward consumer); ONLY a
//               Clip read ever writes the slot, so works the consumer
//               never visits are never blended and never take scratch
internal readonly struct WorkSlot(
    ushort index,
    ushort first,
    ushort second,
    uint enterF,
    uint enterB,
    uint factorStart,
    uint factorLength,
    ushort blendOrdinal)
{
    public const ushort Single = ushort.MaxValue;

    public readonly ushort Index = index;
    public readonly ushort First = first;
    public readonly ushort Second = second;
    public readonly uint EnterF = enterF;
    public readonly uint EnterB = enterB;
    public readonly uint FactorStart = factorStart;
    public readonly uint FactorLength = factorLength;
    public readonly ushort BlendOrdinal = blendOrdinal;
}

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

// The per-tick view: a thin, zero-copy slice of the region's MATERIALIZED
// WorkSlot table plus the per-tick facts (effective tick, blend scratch,
// movement span, payload tables). Count, indexing, slicing and foreach are
// the whole surface — the aggregate status word is gone (its facts are per
// work), and the hook fires only when Count is positive. The engine builds
// the slot table once per region ENTRY, so a per-tick handoff is a slice
// and a tick, not a table walk.
public readonly ref struct Tracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly ReadOnlySpan<WorkSlot> _slots;
    private readonly uint _tick;
    private readonly Span<TClip> _blendScratch;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly MovementSpan _movement;

    internal Tracks(
        ReadOnlySpan<WorkSlot> slots, uint tick,
        Span<TClip> blendScratch,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        MovementSpan movement)
    {
        _slots = slots;
        _tick = tick;
        _blendScratch = blendScratch;
        _trackData = trackData;
        _clipData = clipData;
        _movement = movement;
    }

    public int Count => _slots.Length;

    // First-class indexing: a view of one work, bounds-checked by the span.
    public TrackWork<TTrack, TClip> this[int index]
        => new(in _slots[index], _tick, _blendScratch, _trackData, _clipData, _movement);

    // Zero-copy sub-view of works [start, start + length); bounds-checked.
    public Tracks<TTrack, TClip> Slice(int start, int length)
        => new(_slots.Slice(start, length), _tick, _blendScratch, _trackData, _clipData, _movement);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(_slots, _tick, _blendScratch, _trackData, _clipData, _movement);

    // Foreach sugar: a trivial walk over the slot span. The enumerator
    // carries the view's handful of fields, NOT the table bundle — Current
    // is a thin view of one slot.
    public ref struct Enumerator
    {
        private ReadOnlySpan<WorkSlot> _slots;
        private readonly uint _tick;
        private readonly Span<TClip> _blendScratch;
        private readonly ReadOnlySpan<TTrack> _trackData;
        private readonly ReadOnlySpan<TClip> _clipData;
        private readonly MovementSpan _movement;
        private int _i;

        internal Enumerator(
            ReadOnlySpan<WorkSlot> slots, uint tick,
            Span<TClip> blendScratch,
            ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
            MovementSpan movement)
        {
            _slots = slots;
            _tick = tick;
            _blendScratch = blendScratch;
            _trackData = trackData;
            _clipData = clipData;
            _movement = movement;
            _i = -1;
        }

        public TrackWork<TTrack, TClip> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(in _slots[_i], _tick, _blendScratch, _trackData, _clipData, _movement);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var i = _i + 1;
            if ((uint)i >= (uint)_slots.Length)
                return false;
            _i = i;
            return true;
        }
    }
}

// One resolved (track, clip) pair: a thin view of ONE materialized WorkSlot
// plus the per-tick facts. Index is the track's authored index, Track points
// into the payload table, and Clip points into the payload table for an
// original clip or into the blend scratch for a blended one — resolved on
// first visit, so works the consumer never reads are never blended. Either
// way the consumer sees exactly one clip. State is the per-work movement
// fact.
public readonly ref struct TrackWork<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly ref readonly WorkSlot _slot;
    private readonly uint _tick;
    private readonly Span<TClip> _blendScratch;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly MovementSpan _movement;

    internal TrackWork(
        in WorkSlot slot, uint tick,
        Span<TClip> blendScratch,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        MovementSpan movement)
    {
        _slot = ref slot;
        _tick = tick;
        _blendScratch = blendScratch;
        _trackData = trackData;
        _clipData = clipData;
        _movement = movement;
    }

    public ushort Index => _slot.Index;

    public ref readonly TTrack Track => ref _trackData[_slot.Index];

    public ref readonly TClip Clip
    {
        get
        {
            // Standalone clip: the slot carries the payload-map-RESOLVED
            // index (materialized at region entry) — a direct payload read.
            if (_slot.FactorLength == 0)
                return ref _clipData[_slot.First];

            // Blend pair: resolve on first visit. The scratch ordinal was
            // assigned at materialization in slot order; ONLY this read
            // blends, so works never visited are never blended and never
            // take scratch. The factor is the engine's exact expression.
            var ordinal = _slot.BlendOrdinal;
            var factor = _slot.FactorLength <= 1
                ? 0.5f
                : (_tick - _slot.FactorStart) / (float)(_slot.FactorLength - 1);
            _trackData[_slot.Index].Blend(
                in _clipData[_slot.First],
                in _clipData[_slot.Second],
                factor,
                out _blendScratch[ordinal]);
            return ref _blendScratch[ordinal];
        }
    }

    // The per-work movement fact — the aggregate status word's replacement:
    // - Exit: POSITIONAL. Forward: destination == window End-1 (the work's
    //   last active frame); backward mirror: destination == window Start
    //   (the last frame visited moving backward). Exit takes precedence
    //   over Enter, so every visit's final frame is Exit even for
    //   one-frame windows and jumps landing on the last frame.
    // - Enter: the step crossed the entry edge — forward the OUTER window's
    //   Start (prevEff < Start <= tick), backward through its End. For a
    //   work ACTIVE at the destination the span test reduces to ONE
    //   prev-compare against the slot's entry references (activity pins
    //   Start <= tick < End); wrapped spans and multi-cycle jumps crossed
    //   the entry edge unconditionally. Sequentially Enter coincides with
    //   the first active frame. A stateless sample or repeated position
    //   crosses nothing: no Enter.
    // - Stay otherwise.
    //
    // CONVENTION (pinned word-for-word by the --verify per-work oracle):
    // a blend-resolved work reports the pair's OUTER window — min Start and
    // max End of the two clips — materialized into the slot's entry
    // references at region entry.
    public ClipState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var flags = _movement.Flags;
            var backward = (flags & MovementFlags.Backward) != 0;
            if (backward ? _tick == _slot.EnterF : _tick == _slot.EnterB - 1u)
                return ClipState.Exit;

            if ((flags & MovementFlags.EnterPossible) == 0)
                return ClipState.Stay;

            var crossed = (flags & MovementFlags.CrossedAlways) != 0
                || (backward ? _movement.PrevEff >= _slot.EnterB : _movement.PrevEff < _slot.EnterF);
            return crossed ? ClipState.Enter : ClipState.Stay;
        }
    }
}

// The clip payload for the Vitals fixture.
public readonly struct VitalsClip
{
    public readonly float Amount;

    public VitalsClip(float amount) => Amount = amount;
}

// The empty input for pure-accumulator consumers: read-only context the
// callbacks never read and never write. One fixture type shared by every
// consumer that carries no seed into its walk.
internal readonly struct NoInput { }

// The immutable input half of the domain example: the seed health a walk
// starts from (the 100_000f / 1_000_000f the call sites seed with). The
// result half below is minted from it ONCE (Seed) and then accumulates —
// the callbacks receive the input read-only and never write it.
public readonly struct VitalsInput
{
    public readonly float Health;

    public VitalsInput(float health) => Health = health;

    // The result accumulator, initialized from the seed: identical float
    // arithmetic to the pre-split shape, where the one struct was both the
    // seed carrier and the accumulator.
    public Vitals Seed() => new() { Health = Health };
}

// The domain example: apply work only on Stay; Enter/Exit frames notify but
// do not accumulate. Backward is the exact inverse on sequential walks, so a
// forward walk rewound restores the state (boundary frames are notification
// only — see the pinned semantics). Result half of the v0.2 split: it
// implements the hooks and accumulates; the seed arrives via VitalsInput.
public struct Vitals :
    IForward<VitalsTrack, VitalsClip, VitalsInput, Vitals>,
    IBackward<VitalsTrack, VitalsClip, VitalsInput, Vitals>,
    IForward<LoopVitalsTrack, VitalsClip, VitalsInput, Vitals>,
    IBackward<LoopVitalsTrack, VitalsClip, VitalsInput, Vitals>
{
    public float Health;
    public long Ticks;
    public int Count;
    public int Back;

    public readonly Receipt Result => new(Health, Ticks, Count);

    // Blend-ignorant on purpose: every active track has exactly one resolved
    // clip — an original or a blend result, indistinguishable here.
    public void Forward(in Tracks<VitalsTrack, VitalsClip> tracks, in VitalsInput input, in uint tick, ref Vitals result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    result.Ticks += work.Track.Offset;
                    result.Health += work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    // entering: notify only — no accumulation on the boundary frame
                    break;

                case ClipState.Exit:
                    // leaving: the destination is the clip's last active frame
                    break;
            }
        }

        result.Count++;
    }

    public void Backward(in Tracks<VitalsTrack, VitalsClip> tracks, in VitalsInput input, in uint tick, ref Vitals result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    result.Ticks -= work.Track.Offset;
                    result.Health -= work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
        }

        result.Back++;
    }

    public void Forward(in Tracks<LoopVitalsTrack, VitalsClip> tracks, in VitalsInput input, in uint tick, ref Vitals result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    result.Ticks += work.Track.Offset;
                    result.Health += work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
        }

        result.Count++;
    }

    public void Backward(in Tracks<LoopVitalsTrack, VitalsClip> tracks, in VitalsInput input, in uint tick, ref Vitals result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Stay:
                    result.Ticks -= work.Track.Offset;
                    result.Health -= work.Clip.Amount;
                    break;

                case ClipState.Enter:
                    break;

                case ClipState.Exit:
                    break;
            }
        }

        result.Back++;
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
    // the byte budget); zero-blend timelines reserve nothing. The region
    // work-slot scratch is engine-internal: reserved in the runner below.
    public static void Forward<TInput, TResult>(in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Forward(in input, ref result, scratch, ticks);
    }

    // Caller-provided scratch: one buffer can serve many calls, but each
    // simultaneous or reentrant call needs its own.
    public static void Forward<TInput, TResult>(in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        Sample(backward: false, in input, ref result, scratch, ticks);
    }

    public static void Backward<TInput, TResult>(in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Backward(in input, ref result, scratch, ticks);
    }

    public static void Backward<TInput, TResult>(in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        Sample(backward: true, in input, ref result, scratch, ticks);
    }

    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        return Forward(in from, in input, ref result, scratch, ticks);
    }

    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        PlaybackCore.RequireRunnable(in from);
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        return Advance(backward: false, in from, in input, ref result, scratch, ticks);
    }

    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        return Backward(in from, in input, ref result, scratch, ticks);
    }

    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        PlaybackCore.RequireRunnable(in from);
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        return Advance(backward: true, in from, in input, ref result, scratch, ticks);
    }

    // The two engine entry points, one per flavor. The build-time work-slot
    // table is a per-closure static cache derived once from the authored
    // tables (the registry path precomputes the same table at Build; a
    // future generator bakes it at emission) and the static-abstract table
    // fetches happen once per call — generic-dictionary indirections;
    // never inside the per-track loop.
    private static class WorkTables
    {
        public static readonly WorkSlot[] Slots = PlaybackCore.MaterializeWorkSlots(
            TTrack.TrackRows, TTrack.ClipRows, ReadOnlySpan<ushort>.Empty, TTrack.ClipEdges, TTrack.RegionRows);
    }

    private static void Sample<TInput, TResult>(
        bool backward, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
            backward, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
    }

    private static Playback Advance<TInput, TResult>(
        bool backward, in Playback from, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
            in from, backward, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
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

        // Authored-clip index -> payload slot. Empty means identity: one
        // payload row per authored instance, no dedup.
        public required ushort[] PayloadMap { get; init; }
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

        // Prefix counts from the authored clip edges, checked conversions:
        // counts fit a ushort because each of the at most 65,535 authored
        // clips contributes at most one start cut and one end cut. Bit 1
        // marks a region whose start position is a clip start, bit 2 a clip
        // end — the same cut facts the static fixtures' RegionFlags carry.
        var cutStarts = new HashSet<uint>();
        var cutEnds = new HashSet<uint>();
        foreach (var clip in authoring.Clips)
        {
            cutStarts.Add(clip.Start);
            cutEnds.Add(clip.End);
        }

        var cutCounts = new CutCounts[Timeline.EmitCutCounts ? regionStarts.Length : 0];
        if (cutCounts.Length != 0)
        {
            uint startsSeen = 0, endsSeen = 0;
            for (var i = 0; i < regionStarts.Length; i++)
            {
                if (cutStarts.Contains(regionStarts[i]))
                    startsSeen++;
                if (cutEnds.Contains(regionStarts[i]))
                    endsSeen++;
                cutCounts[i] = new(checked((ushort)startsSeen), checked((ushort)endsSeen));
            }
        }

        // Build-time storage dedup (faster queue #4). Payloads merge only
        // on bitwise-equal bytes — float bit patterns are preserved, +0/-0
        // and distinct NaN payloads never merge — through an indirection
        // map that keeps authored-instance identity (the ClipIndex ==
        // ClipEdges 1:1 invariant; payload slots, not row renumbering).
        // Identical CSR row sequences alias one canonical run: regions with
        // byte-identical track-row slices share storage, and track rows
        // with byte-identical clip-row runs share theirs. Storage merges
        // never merge two authored instances — per-region row counts,
        // order and track indices are untouched.
        ushort[] payloadMap;
        if (Timeline.DedupStorage)
        {
            var comparer = new ByteArrayComparer();
            var unique = new Dictionary<byte[], ushort>(comparer);
            var slots = new List<TClip>();
            payloadMap = new ushort[authoring.Clips.Count];
            for (var i = 0; i < authoring.Clips.Count; i++)
            {
                var bytes = new byte[Unsafe.SizeOf<TClip>()];
                var authored = authoring.Clips[i].Clip;
                MemoryMarshal.Write(bytes, in authored);
                if (unique.TryGetValue(bytes, out var slot))
                {
                    payloadMap[i] = slot;
                    continue;
                }

                slot = checked((ushort)slots.Count);
                unique.Add(bytes, slot);
                slots.Add(authoring.Clips[i].Clip);
                payloadMap[i] = slot;
            }

            clipData = [.. slots];
        }
        else
        {
            payloadMap = [];
        }

        TrackRow[] compactTracks;
        if (Timeline.DedupStorage)
        {
            // 1. Track rows whose clip-row runs are byte-identical (same
            //    track index, same authored clip indices and blend windows)
            //    alias the first kept run.
            var rowComparer = new ByteArrayComparer();
            var clipRuns = new Dictionary<byte[], ushort>(rowComparer);
            var keptClips = new List<ClipRow>(clipRows.Count);
            var remapped = new TrackRow[trackRows.Count];
            for (var i = 0; i < trackRows.Count; i++)
            {
                var row = trackRows[i];
                var count = row.ClipCount;
                var key = new byte[4 + Unsafe.SizeOf<ClipRow>() * count];
                var trackIndex = row.TrackIndex;
                MemoryMarshal.Write(key.AsSpan(0, 2), in trackIndex);
                MemoryMarshal.Write(key.AsSpan(2, 2), in count);
                MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(clipRows).Slice(row.ClipStart, count))
                    .CopyTo(key.AsSpan(4));
                if (clipRuns.TryGetValue(key, out var canonical))
                {
                    remapped[i] = new TrackRow(row.TrackIndex, canonical, count);
                    continue;
                }

                canonical = checked((ushort)keptClips.Count);
                clipRuns.Add(key, canonical);
                for (var c = 0; c < count; c++)
                    keptClips.Add(clipRows[row.ClipStart + c]);
                remapped[i] = new TrackRow(row.TrackIndex, canonical, count);
            }

            clipRows = keptClips;

            // 2. Regions whose (remapped) track-row slices are byte-identical
            //    alias the first kept slice.
            var regionSlices = new Dictionary<byte[], ushort>(rowComparer);
            var keptTracks = new List<TrackRow>(trackRows.Count);
            var sliceBytes = new byte[Unsafe.SizeOf<TrackRow>()];
            for (var r = 0; r < regionRows.Length; r++)
            {
                var row = regionRows[r];
                var key = new byte[Unsafe.SizeOf<TrackRow>() * row.TrackCount];
                for (var t = 0; t < row.TrackCount; t++)
                {
                    var source = remapped[row.TrackStart + t];
                    MemoryMarshal.Write(sliceBytes.AsSpan(0, Unsafe.SizeOf<TrackRow>()), in source);
                    sliceBytes.AsSpan(0, Unsafe.SizeOf<TrackRow>()).CopyTo(key.AsSpan(Unsafe.SizeOf<TrackRow>() * t));
                }

                if (regionSlices.TryGetValue(key, out var canonical))
                {
                    regionRows[r] = new RegionRow(canonical, row.TrackCount);
                    continue;
                }

                canonical = checked((ushort)keptTracks.Count);
                regionSlices.Add(key, canonical);
                for (var t = 0; t < row.TrackCount; t++)
                    keptTracks.Add(remapped[row.TrackStart + t]);
                regionRows[r] = new RegionRow(canonical, row.TrackCount);
            }

            compactTracks = [.. keptTracks];
        }
        else
        {
            compactTracks = [.. trackRows];
        }

        // Build-time region materialization: one WorkSlot per final track
        // row (post-dedup, so aliased row runs share aliased slots), region
        // rows referencing them by slice. Per tick, the engine slices this
        // table — no per-call materialization work at all.
        var workSlots = PlaybackCore.MaterializeWorkSlots(
            compactTracks, CollectionsMarshal.AsSpan(clipRows), payloadMap, clipEdges, regionRows);

        return Timeline.Register(new Timeline.Entry
        {
            RegionStarts = regionStarts,
            RegionRows = regionRows,
            TrackRows = compactTracks,
            ClipRows = [.. clipRows],
            ClipEdges = clipEdges,
            WorkSlots = workSlots,
            Payload = new Tables
            {
                TrackData = [.. authoring.Tracks],
                ClipData = clipData,
                PayloadMap = payloadMap,
            },
            MaxActiveTracks = maxActive,
            MaxActiveBlends = maxBlends,
            CutCounts = cutCounts,
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
    public static unsafe Playback Forward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Timeline.Live(index);
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = entry.Bind(typeof(TInput), typeof(TResult), Timeline.TokenOf<TInput, TResult>());
        return run.ForwardScratch(entry, in playback, ScratchPointer(scratch), scratch.Length, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    public static unsafe Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Timeline.Live(index);
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = entry.Bind(typeof(TInput), typeof(TResult), Timeline.TokenOf<TInput, TResult>());
        return run.BackwardScratch(entry, in playback, ScratchPointer(scratch), scratch.Length, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    // The raw pointer the scratch runners receive; null for empty spans
    // (zero-blend timelines reserve nothing).
    private static unsafe void* ScratchPointer(Span<TClip> scratch)
        => scratch.IsEmpty ? null : (void*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(scratch));

    // The one-time, per-(entry, input/result pair) bridge instantiation. The
    // non-generic hub cannot constrain its types against this closure (the
    // CS0699-style discovery the design anticipated), so binding goes through
    // a generic method closed by reflection once per (entry, pair); the bound
    // function pointers are cached on the entry and the steady state is a
    // plain pointer call.
    // AOT: reflection is unavailable without dynamic code — Bind<TInput,
    // TResult>(index) is the static, instantiation-at-call-site path for
    // ahead-of-time consumers; this automatic path guards and throws a named
    // error.
    private static readonly MethodInfo s_bind =
        typeof(Timeline<TTrack, TClip>).GetMethod(nameof(BindData), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(BindData));

    // The explicit, reflection-free bind: one static generic call per
    // (timeline index, input/result pair), so the instantiation exists at a
    // call site the AOT compiler can see. Repeat calls are idempotent
    // (Install overwrites the same token slot).
    public static void Bind<TInput, TResult>(ushort index)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new InvalidOperationException($"Timeline index {index} does not belong to the <{typeof(TTrack).Name}, {typeof(TClip).Name}> closure.");
        BindData<TInput, TResult>(entry);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
        Justification = "The generic method is closed over the input/result pair; AOT consumers use Bind<TInput, TResult> instead.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:MakeGenericMethod",
        Justification = "Guarded by the IsDynamicCodeSupported check; AOT consumers use Bind<TInput, TResult> instead.")]
    private static void BindType(Type input, Type result, Timeline.Entry entry)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException(
                $"The automatic (entry, consumer) bridge bind needs dynamic code; under NativeAOT call " +
                $"{typeof(Timeline<TTrack, TClip>).Name}.Bind<TInput, TResult>(index) once per pair instead.");
        s_bind.MakeGenericMethod(input, result).Invoke(null, [entry]);
    }

    private static void BindData<TInput, TResult>(Timeline.Entry entry)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
        => Bridge<TInput, TResult>.Install(entry);

    private static unsafe class Bridge<TInput, TResult>
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        public static void Install(Timeline.Entry entry)
            => entry.Install(
                DataToken<TInput, TResult>.Id,
                new Timeline.Entry.Run(&RunForward, &RunBackward, &RunForwardCursor, &RunBackwardCursor, &RunForwardScratch, &RunBackwardScratch, &SampleForward, &SampleBackward));

        // The caller's data pair arrives as two stack-pinned void* pointers —
        // `in TInput` first, `ref TResult` second — and is rehydrated here:
        // the result pointer mutates the caller's instance, zero allocation,
        // no boxing; the input pointer is read-only context the callbacks
        // receive by reference.
        private static Playback RunForward(Timeline.Entry entry, in Playback from, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        private static Playback RunBackward(Timeline.Entry entry, in Playback from, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        // Cursor-primed runners: the caller's `ref cursor` rides through as a
        // stack-pinned void* ahead of the data pair. The cache validates
        // against the entry itself (a rebuilt timeline is a new Entry; loop
        // mode is per-entry immutable) plus the source tick; any mismatch
        // falls back to searching and can never change results. Stored only
        // after the call returns; an empty tick span echoes a valid region
        // through unchanged.
        private static Playback RunForwardCursor(Timeline.Entry entry, in Playback from, void* cursor, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var cache = ref Unsafe.AsRef<Cursor>(cursor);
            var valid = ReferenceEquals(cache.Owner, entry) && cache.Tick == from.Tick;
            var hint = valid ? cache.Region : -1;

            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                hint, out var region);

            cache = new Cursor { Owner = entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        private static Playback RunBackwardCursor(Timeline.Entry entry, in Playback from, void* cursor, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var cache = ref Unsafe.AsRef<Cursor>(cursor);
            var valid = ReferenceEquals(cache.Owner, entry) && cache.Tick == from.Tick;
            var hint = valid ? cache.Region : -1;

            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                hint, out var region);

            cache = new Cursor { Owner = entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        // Caller-scratch runners: the caller's buffer arrives raw (pointer
        // plus element count — the hub cannot name TClip) and is rehydrated
        // here where the closure knows it, ahead of the data pair. The public
        // overload checked the length before the call; the engine is
        // otherwise identical.
        private static Playback RunForwardScratch(Timeline.Entry entry, in Playback from, void* scratch, int scratchLength, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            var resolved = new Span<TClip>(scratch, scratchLength);

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        private static Playback RunBackwardScratch(Timeline.Entry entry, in Playback from, void* scratch, int scratchLength, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var cutCounts = entry.CutCounts.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            var resolved = new Span<TClip>(scratch, scratchLength);

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, cutCounts, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        private static void SampleForward(Timeline.Entry entry, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: false, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, trackData, clipData, resolved, workSlots);
        }

        private static void SampleBackward(Timeline.Entry entry, void* input, void* result, ReadOnlySpan<uint> ticks)
        {
            ref var context = ref Unsafe.AsRef<TInput>(input);
            ref var consumer = ref Unsafe.AsRef<TResult>(result);
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: true, entry.Loops, ticks, in context, ref consumer,
                starts, regionRows, trackData, clipData, resolved, workSlots);
        }
    }
}

// Per-(TInput, TResult) static token id: a CAS counter, so every closed
// input/result pair gets one small id used to index the per-entry binding
// cache.
internal static class DataToken<TInput, TResult>
    where TInput : struct
    where TResult : struct
{
    public static int Id;
}

// Build-time key equality for the storage dedup: structural byte
// equality, so equal keys mean bitwise-identical payloads/rows and a hash
// collision is resolved by the full comparison (never by hash alone).
internal sealed class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);

    public int GetHashCode(byte[] obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj);
        return hash.ToHashCode();
    }
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
// Dispatch: the hub's types cannot be constrained against the entry's
// unknown closure, so each entry lazily binds and caches per-(TInput, TResult)
// unsafe function pointers created from a bridge generic instantiation of the
// closure's engine (the caller's data pair rides through as two stack-pinned
// void* pointers — `in TInput` first, `ref TResult` second — and is
// rehydrated inside; zero allocation, no boxing, first call per (entry, pair)
// does the one-time reflection bind). All lifecycle and bounds checks run
// BEFORE the pointer call.
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

        // Build-time region materialization: one WorkSlot per track row,
        // parallel to TrackRows — a region's per-tick view is a slice.
        public required WorkSlot[] WorkSlots { get; init; }

        // The closure-typed half (Timeline<TTrack, TClip>.Tables), stored as
        // object and rehydrated without a cast by the bridge.
        public required object Payload { get; init; }

        // The timeline's duration as a ushort (the authored final cut).
        // Durations beyond 65,535 ticks truncate — the fixture timelines all
        // fit; a wider return type is a future widening if ever needed.
        public required ushort Duration { get; init; }

        public required int MaxActiveTracks { get; init; }
        public required int MaxActiveBlends { get; init; }
        // Cumulative cut counts per region (4 B each), or empty when built
        // with the flag off — playback then always runs the full per-work
        // Enter check.
        public required CutCounts[] CutCounts { get; init; }
        public required bool Loops { get; init; }

        public required Action<Type, Type, Entry> Binder { get; init; }

        // One bound (forward, backward, cursor-primed forward/backward,
        // caller-scratch forward/backward, sample-forward, sample-backward)
        // pointer set per input/result-pair token id. Every shape carries the
        // data pair as two stack-pinned void* pointers — input first, result
        // second, adjacent in the old `ref data` slot; the cursor variants
        // carry the caller's Cursor ahead of the pair; the scratch variants
        // carry the caller's blend buffer as a raw pointer plus element count
        // (the hub cannot name TClip) before the pair.
        public readonly struct Run(
            delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> forward,
            delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> backward,
            delegate*<Entry, in Playback, void*, void*, void*, ReadOnlySpan<uint>, Playback> forwardCursor,
            delegate*<Entry, in Playback, void*, void*, void*, ReadOnlySpan<uint>, Playback> backwardCursor,
            delegate*<Entry, in Playback, void*, int, void*, void*, ReadOnlySpan<uint>, Playback> forwardScratch,
            delegate*<Entry, in Playback, void*, int, void*, void*, ReadOnlySpan<uint>, Playback> backwardScratch,
            delegate*<Entry, void*, void*, ReadOnlySpan<uint>, void> sampleForward,
            delegate*<Entry, void*, void*, ReadOnlySpan<uint>, void> sampleBackward)
        {
            public readonly delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> Forward = forward;
            public readonly delegate*<Entry, in Playback, void*, void*, ReadOnlySpan<uint>, Playback> Backward = backward;
            public readonly delegate*<Entry, in Playback, void*, void*, void*, ReadOnlySpan<uint>, Playback> ForwardCursor = forwardCursor;
            public readonly delegate*<Entry, in Playback, void*, void*, void*, ReadOnlySpan<uint>, Playback> BackwardCursor = backwardCursor;
            public readonly delegate*<Entry, in Playback, void*, int, void*, void*, ReadOnlySpan<uint>, Playback> ForwardScratch = forwardScratch;
            public readonly delegate*<Entry, in Playback, void*, int, void*, void*, ReadOnlySpan<uint>, Playback> BackwardScratch = backwardScratch;
            public readonly delegate*<Entry, void*, void*, ReadOnlySpan<uint>, void> SampleForward = sampleForward;
            public readonly delegate*<Entry, void*, void*, ReadOnlySpan<uint>, void> SampleBackward = sampleBackward;

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

        public Run Bind(Type input, Type result, int token)
        {
            var run = Lookup(token);
            if (run.IsBound)
                return run;

            // The one-time reflection bind; MakeGenericMethod validates that
            // the input/result pair closes this entry's consumer hooks.
            Binder(input, result, this);

            var bound = Lookup(token);
            return bound.IsBound
                ? bound
                : throw new InvalidOperationException($"The consumer pair <{input}, {result}> does not implement this timeline's IForward/IBackward closure.");
        }
    }

    private static readonly object s_gate = new();
    private static Entry?[] s_slots = [];
    private static int s_nextIndex;

    // Build-time switch for the prefix-count experiment (faster queue #3),
    // verdict REJECT on receipts: false (default) leaves the table empty and
    // playback always runs the full per-work Enter check — measured parity
    // or regressions in every shape (the redesign already made the per-work
    // check O(1); the counts only remove a well-predicted branch at the
    // price of 4 B per region and two extra loads per step). Benchmark A/B
    // only — not API.
    internal static bool EmitCutCounts = false;

    // Build-time switch for storage dedup (faster queue #4), verdict KEEP
    // as an opt-in: false (default) keeps one row per authored instance and
    // the zero-indirection payload read — small timelines dedup to nothing
    // and the map hop measurably taxes their hot arms; true shares payload
    // slots (bitwise) and aliases identical CSR row sequences for
    // duplication-heavy content, where it retains 6x less with playback
    // parity. Benchmark A/B only — not API.
    internal static bool DedupStorage = false;

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

    public static Playback Forward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, in uint tick)
        where TInput : struct
        where TResult : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Forward(index, in playback, in input, ref result, ticks);
    }

    public static Playback Forward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        return run.Forward(entry, in playback, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, in uint tick)
        where TInput : struct
        where TResult : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, in input, ref result, ticks);
    }

    // Cursor-primed entry points: same playback, plus a caller-owned region
    // cache carried between calls. Any mismatch (different entry — index
    // switch, rebuild, loop-mode change — or a repositioned Playback) falls
    // back to searching. Separate callers keep separate cursors; sharing one
    // is legal but only ever validated against its own owner.
    public static Playback Forward<TInput, TResult>(ushort index, in Playback playback, ref Cursor cursor, in TInput input, ref TResult result, in uint tick)
        where TInput : struct
        where TResult : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Forward(index, in playback, ref cursor, in input, ref result, ticks);
    }

    public static Playback Forward<TInput, TResult>(ushort index, in Playback playback, ref Cursor cursor, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        return run.ForwardCursor(entry, in playback, Unsafe.AsPointer(ref cursor), Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, ref Cursor cursor, in TInput input, ref TResult result, in uint tick)
        where TInput : struct
        where TResult : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, ref cursor, in input, ref result, ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, ref Cursor cursor, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        return run.BackwardCursor(entry, in playback, Unsafe.AsPointer(ref cursor), Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        return run.Backward(entry, in playback, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    // Sampling only, no movement facts and no lifecycle: the stateless path.
    public static void Forward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        run.SampleForward(entry, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    public static void Backward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var run = entry.Bind(typeof(TInput), typeof(TResult), TokenOf<TInput, TResult>());
        run.SampleBackward(entry, Unsafe.AsPointer(ref Unsafe.AsRef(in input)), Unsafe.AsPointer(ref result), ticks);
    }

    internal static int TokenOf<TInput, TResult>()
        where TInput : struct
        where TResult : struct
    {
        var id = DataToken<TInput, TResult>.Id;
        if (id != 0)
            return id;

        // First use of this input/result pair anywhere: take a token and
        // publish it. A benign race can burn one token id; ids only need
        // uniqueness.
        Interlocked.CompareExchange(ref DataToken<TInput, TResult>.Id, TokenCounter.Take(), 0);
        return DataToken<TInput, TResult>.Id;
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
