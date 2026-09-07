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

    public static void Forward<TData>(ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        Span<TClip> scratch = stackalloc TClip[BlendScratch.StackCount<TClip>(TTrack.MaxActiveBlends)];
        Forward(ref data, scratch, ticks);
    }

    public static void Forward<TData>(ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
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
            backward: false, TTrack.Loops, ticks, ref data,
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, ReadOnlySpan<ushort>.Empty, scratch);
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
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, ReadOnlySpan<ushort>.Empty, scratch);
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
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, ReadOnlySpan<ushort>.Empty, scratch);
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
            starts, regionRows, trackRows, clipRows, edges, trackData, clipData, ReadOnlySpan<ushort>.Empty, scratch);
    }
}
