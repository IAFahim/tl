using System.Runtime.CompilerServices;
using Tl.Generation;

namespace Tl.Internal;

internal readonly record struct MovementSpan(uint PrevEff, bool Backward, bool Wrapped, bool Full)
{
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

internal static class BlendScratch
{
    internal const int StackBytes = 4096;

    public static int StackCount<TClip>(int maxActiveBlends) where TClip : struct
    {
        var size = Unsafe.SizeOf<TClip>();
        var capacity = size == 0 ? maxActiveBlends : StackBytes / size;
        if ((uint)maxActiveBlends > (uint)capacity)
            throw new InvalidOperationException(
                "Blend scratch exceeds the stack byte budget; use the scratch-buffer overload.");
        return maxActiveBlends;
    }
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

    public static Playback Advance<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<ushort> payloadMap,
        Span<TClip> resolved)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
        => Advance(
            in from, backward, loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved,
            -1, out _);

    public static Playback Advance<TTrack, TClip, TData>(
        in Playback from, bool backward, bool loops,
        ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<ushort> payloadMap,
        Span<TClip> resolved,
        int regionHint, out int finalRegion)
        where TTrack : struct, IBlend<TClip>
        where TClip : struct
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
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

            if (row.TrackCount > 0)
            {
                var tracks = new Tracks<TTrack, TClip>(
                    tEff,
                    trackRows.Slice(row.TrackStart, row.TrackCount),
                    clipRows, trackData, clipData, payloadMap, resolved,
                    edges,
                    new MovementSpan(prevEff, backward, wrapped, full));

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

    public static void Sample<TTrack, TClip, TData>(
        bool backward, bool loops, ReadOnlySpan<uint> ticks, ref TData data,
        ReadOnlySpan<uint> starts, ReadOnlySpan<RegionRow> regionRows,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<ClipEdge> edges,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<ushort> payloadMap,
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

            var tracks = new Tracks<TTrack, TClip>(
                localTick,
                trackRows.Slice(row.TrackStart, row.TrackCount),
                clipRows,
                trackData,
                clipData,
                payloadMap,
                resolved,
                edges,
                new MovementSpan(localTick, backward, Wrapped: false, Full: false));

            if (backward)
                data.Backward(ref data, in tracks, in localTick);
            else
                data.Forward(ref data, in tracks, in localTick);
        }
    }

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
