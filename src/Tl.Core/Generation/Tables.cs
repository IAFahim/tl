using Tl.Internal;

namespace Tl.Generation;

public readonly record struct RegionRow(ushort TrackStart, ushort TrackCount);
public readonly record struct TrackRow(ushort TrackIndex, ushort ClipStart, ushort ClipCount);
public readonly record struct ClipRow(ushort ClipIndex, uint FactorStart, uint FactorLength);

public interface ITrackTables<TTrack, TClip>
    where TTrack : unmanaged
    where TClip : unmanaged
{
    static abstract ReadOnlySpan<uint> RegionStarts { get; }
    static abstract ReadOnlySpan<RegionRow> RegionRows { get; }
    static abstract ReadOnlySpan<TrackRow> TrackRows { get; }
    static abstract ReadOnlySpan<ClipRow> ClipRows { get; }
    static abstract ReadOnlySpan<ClipEdge> ClipEdges { get; }
    static abstract ReadOnlySpan<TTrack> TrackData { get; }
    static abstract ReadOnlySpan<TClip> ClipData { get; }
    static abstract bool Loops { get; }
}

public static class GeneratedTimeline<TTrack, TClip>
    where TTrack : unmanaged, ITrackTables<TTrack, TClip>, IBlend<TClip>
    where TClip : unmanaged
{
    public static Playback Start(uint at = 0) => new(at, 0, PlaybackFlags.Started);

    // The build-time work-slot table is a per-closure static cache derived
    // once from the authored tables (the registry path precomputes the same
    // table at Build; a future generator can bake it at emission) and the
    // static-abstract table fetches happen once per call — generic-dictionary
    // indirections; never inside the per-track loop.
    private static class WorkTables
    {
        public static readonly WorkSlot[] Slots = PlaybackCore.MaterializeWorkSlots(
            TTrack.TrackRows, TTrack.ClipRows, ReadOnlySpan<ushort>.Empty, TTrack.ClipEdges, TTrack.RegionRows);
    }

    public static void Forward<TInput, TResult>(in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
            backward: false, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, WorkTables.Slots);
    }

    public static void Backward<TInput, TResult>(in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
            backward: true, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, WorkTables.Slots);
    }

    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        PlaybackCore.RequireRunnable(in from);
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
            in from, backward: false, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, WorkTables.Slots);
    }

    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        PlaybackCore.RequireRunnable(in from);
        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
            in from, backward: true, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, WorkTables.Slots);
    }
}
