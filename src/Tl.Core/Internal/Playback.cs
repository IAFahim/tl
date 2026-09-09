using System.Runtime.CompilerServices;
using Tl.Generation;

namespace Tl.Internal;

// The movement span of one step, distilled for per-work facts: where the
// step came from (PrevEff), its direction, and the wrap shape of the span.
// Stateless sampling has no movement, so those paths pass all-false and
// works carry positional facts only (Exit is positional and still fires).
// EnterPossible is the prefix-count gate of the benchmarks sandbox (faster
// queue #3): false when the crossed region range provably contains no start
// (forward) or end (backward) cut, so no work of this step can report
// Enter. Conservative by construction — true keeps the full Enter check, so
// an absent or empty CutCounts table never changes results. The src engine
// never emits the counts (the experiment's verdict was REJECT), so the
// engine-side constructor always leaves the gate open; the flag keeps the
// view's State honest either way.
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

internal readonly struct WorkSlot(
    ushort index,
    ushort first,
    ushort second,
    uint enterF,
    uint enterB,
    uint factorStart,
    uint factorLength)
{
    public const ushort Single = ushort.MaxValue;

    public readonly uint EnterF = enterF;
    public readonly uint EnterB = enterB;
    public readonly uint FactorStart = factorStart;
    public readonly uint FactorLength = factorLength;
    public readonly ushort Index = index;
    public readonly ushort First = first;
    public readonly ushort Second = second;
}

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
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
        => Advance(
            in from, backward, loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, workSlots,
            -1, out _);

    // `regionHint` is a CALLER-VALIDATED region for the effective position
    // of `from.Tick` (a persistent Cursor); -1 means none. `finalRegion`
    // returns the region of the last processed tick's effective position —
    // pass it back as the next call's hint after storing `result.Tick`
    // alongside it. An empty tick span returns `from` and echoes the hint
    // unchanged, so a valid cursor survives it.
    //
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
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<WorkSlot> workSlots,
        int regionHint, out int finalRegion)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var duration = starts[^1];
        var state = from;
        var previousRegion = regionHint;
        var hinted = regionHint >= 0;

        foreach (var tick in ticks)
        {
            var (tEff, prevEff, cycles, wrapped, full) = Position(state.Tick, tick, duration, loops, backward);

            var region = Locate(starts, tEff, previousRegion, hinted);
            hinted = false;
            var row = regionRows[region];

            // Forward wraps are counted exactly; the cycle capacity guard
            // throws before this tick's callback.
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
                var movement = new MovementSpan(prevEff, backward, wrapped, full);
                if (backward)
                    BackwardWorks<TTrack, TClip, TInput, TResult>(
                        row, tEff, in movement, in input, ref result,
                        trackData, clipData, workSlots);
                else
                    ForwardWorks<TTrack, TClip, TInput, TResult>(
                        row, tEff, in movement, in input, ref result,
                        trackData, clipData, workSlots);
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
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
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

            var movement = new MovementSpan(localTick, backward, wrapped: false, full: false);
            if (backward)
                BackwardWorks<TTrack, TClip, TInput, TResult>(
                    row, localTick, in movement, in input, ref result,
                    trackData, clipData, workSlots);
            else
                ForwardWorks<TTrack, TClip, TInput, TResult>(
                    row, localTick, in movement, in input, ref result,
                    trackData, clipData, workSlots);
        }
    }

    private static void ForwardWorks<TTrack, TClip, TInput, TResult>(
        RegionRow row,
        uint tick,
        in MovementSpan movement,
        in TInput input,
        ref TResult result,
        ReadOnlySpan<TTrack> trackData,
        ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var slots = workSlots.Slice(row.TrackStart, row.TrackCount);
        for (var ordinal = 0; ordinal < slots.Length; ordinal++)
        {
            ref readonly var slot = ref slots[ordinal];
            ref readonly var track = ref trackData[slot.Index];
            var state = ForwardState(in slot, tick, in movement);
            if (slot.Second == WorkSlot.Single)
            {
                TResult.Forward(
                    ordinal, slots.Length, slot.Index,
                    in track, in clipData[slot.First], state,
                    in tick, in input, ref result);
                continue;
            }

            track.Blend(
                in clipData[slot.First],
                in clipData[slot.Second],
                BlendFactor(in slot, tick),
                out var resolved);
            TResult.Forward(
                ordinal, slots.Length, slot.Index,
                in track, in resolved, state,
                in tick, in input, ref result);
        }
    }

    private static void BackwardWorks<TTrack, TClip, TInput, TResult>(
        RegionRow row,
        uint tick,
        in MovementSpan movement,
        in TInput input,
        ref TResult result,
        ReadOnlySpan<TTrack> trackData,
        ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<WorkSlot> workSlots)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var slots = workSlots.Slice(row.TrackStart, row.TrackCount);
        for (var ordinal = 0; ordinal < slots.Length; ordinal++)
        {
            ref readonly var slot = ref slots[ordinal];
            ref readonly var track = ref trackData[slot.Index];
            var state = BackwardState(in slot, tick, in movement);
            if (slot.Second == WorkSlot.Single)
            {
                TResult.Backward(
                    ordinal, slots.Length, slot.Index,
                    in track, in clipData[slot.First], state,
                    in tick, in input, ref result);
                continue;
            }

            track.Blend(
                in clipData[slot.First],
                in clipData[slot.Second],
                BlendFactor(in slot, tick),
                out var resolved);
            TResult.Backward(
                ordinal, slots.Length, slot.Index,
                in track, in resolved, state,
                in tick, in input, ref result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float BlendFactor(in WorkSlot slot, uint tick)
        => slot.FactorLength <= 1
            ? 0.5f
            : (tick - slot.FactorStart) / (float)(slot.FactorLength - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ClipState ForwardState(in WorkSlot slot, uint tick, in MovementSpan movement)
    {
        if (tick == slot.EnterB - 1u)
            return ClipState.Exit;
        if ((movement.Flags & MovementFlags.EnterPossible) == 0)
            return ClipState.Stay;
        return (movement.Flags & MovementFlags.CrossedAlways) != 0 || movement.PrevEff < slot.EnterF
            ? ClipState.Enter
            : ClipState.Stay;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ClipState BackwardState(in WorkSlot slot, uint tick, in MovementSpan movement)
    {
        if (tick == slot.EnterF)
            return ClipState.Exit;
        if ((movement.Flags & MovementFlags.EnterPossible) == 0)
            return ClipState.Stay;
        return (movement.Flags & MovementFlags.CrossedAlways) != 0 || movement.PrevEff >= slot.EnterB
            ? ClipState.Enter
            : ClipState.Stay;
    }

    // BUILD-TIME region materialization: one WorkSlot per track row of the
    // whole table, so region r's per-tick view is simply
    // slots.Slice(row.TrackStart, row.TrackCount). Computed once:
    // the registry path runs this in TimelineCompiler.Compile (after
    // storage dedup, so aliased row runs share aliased slots — identical
    // row runs yield identical ordinals, so the re-walk is idempotent),
    // the generated-table shell through a per-closure static cache, and a
    // future generator can bake the table at emission. NEVER per tick —
    // the active work set is constant for the lifetime of the tables.
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
                        0u, 0u);
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
                    first.FactorStart, first.FactorLength);
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
