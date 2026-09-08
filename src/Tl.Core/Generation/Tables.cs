using Tl.Internal;

namespace Tl.Generation;

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
    static abstract int MaxActiveBlends { get; }
    static abstract bool Loops { get; }
}

public static class GeneratedTimeline<TTrack, TClip>
    where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
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
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Forward(in input, ref result, scratch, ticks);
    }

    public static void Forward<TInput, TResult>(in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        if (scratch.Length < TTrack.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
            backward: false, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
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

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
            backward: true, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
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

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
            in from, backward: false, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
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

        var starts = TTrack.RegionStarts;
        var regionRows = TTrack.RegionRows;
        var trackData = TTrack.TrackData;
        var clipData = TTrack.ClipData;

        return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
            in from, backward: true, TTrack.Loops, ticks, in input, ref result,
            starts, regionRows, trackData, clipData, scratch, WorkTables.Slots);
    }
}
