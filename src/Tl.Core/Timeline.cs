using System.Reflection;
using System.Runtime.CompilerServices;
using Tl.Authoring;
using Tl.Internal;

namespace Tl;

public static unsafe partial class Timeline
{
    public static Playback Start(ushort index, uint at = 0)
    {
        _ = Live(index);
        return new Playback(at, 0, PlaybackFlags.Started);
    }

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
        var run = BindingCache<TData>.Get(index, entry);
        return run.Forward(entry, in playback, ref data, ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref TData data, in uint tick)
        where TData : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, ref data, ticks);
    }

    public static Playback Backward<TData>(ushort index, in Playback playback, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = BindingCache<TData>.Get(index, entry);
        return run.Backward(entry, in playback, ref data, ticks);
    }

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
        var run = BindingCache<TData>.Get(index, entry);
        return run.ForwardCursor(entry, in playback, ref cursor, ref data, ticks);
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
        var run = BindingCache<TData>.Get(index, entry);
        return run.BackwardCursor(entry, in playback, ref cursor, ref data, ticks);
    }

    public static void Forward<TData>(ushort index, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        var run = BindingCache<TData>.Get(index, entry);
        run.SampleForward(entry, ref data, ticks);
    }

    public static void Backward<TData>(ushort index, ref TData data, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Live(index);
        var run = BindingCache<TData>.Get(index, entry);
        run.SampleBackward(entry, ref data, ticks);
    }
}

public static class Timeline<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
{
    internal sealed class Tables
    {
        public required TTrack[] TrackData { get; init; }
        public required TClip[] ClipData { get; init; }
        public required ushort[] PayloadMap { get; init; }
    }

    public static ushort Build(TimelineBuild<TTrack, TClip> build)
        => Build(build, TimelineOptions.Default);

    public static ushort Build(TimelineBuild<TTrack, TClip> build, TimelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(options);
        build(builder);
        return TimelineCompiler.Compile(builder._state, BindType);
    }

    public static ushort Build<TSource>(TSource source, TimelineBuild<TTrack, TClip, TSource> build)
        => Build(source, build, TimelineOptions.Default);

    public static ushort Build<TSource>(TSource source, TimelineBuild<TTrack, TClip, TSource> build, TimelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(options);
        build(builder, source);
        return TimelineCompiler.Compile(builder._state, BindType);
    }

    public static void Bind<TData>(ushort index)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new InvalidOperationException($"Timeline index {index} does not belong to the <{typeof(TTrack).Name}, {typeof(TClip).Name}> closure.");
        Bridge<TData>.Install(index, entry);
    }

    public static unsafe Playback Forward<TData>(ushort index, in Playback playback, ref TData data, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new ArgumentException("Timeline belongs to a different track/clip pair.", nameof(index));
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = ScratchCache<TTrack, TClip, TData>.Get(index, entry);
        return run.Forward(entry, in playback, ref data, scratch, ticks);
    }

    public static unsafe Playback Backward<TData>(ushort index, in Playback playback, ref TData data, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TData : struct
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new ArgumentException("Timeline belongs to a different track/clip pair.", nameof(index));
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = ScratchCache<TTrack, TClip, TData>.Get(index, entry);
        return run.Backward(entry, in playback, ref data, scratch, ticks);
    }

    private static readonly MethodInfo s_bind =
        typeof(Timeline<TTrack, TClip>).GetMethod(nameof(BindEntryData), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(BindEntryData));

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

    private static void BindEntryData<TData>(Timeline.Entry entry)
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
        => Bridge<TData>.Install(entry.Index, entry);

    private static unsafe class Bridge<TData>
        where TData : struct, IForward<TTrack, TClip, TData>, IBackward<TTrack, TClip, TData>
    {
        public static void Install(ushort index, Timeline.Entry entry)
        {
            BindingCache<TData>.Install(
                index,
                entry,
                new Run<TData>(
                    entry,
                    &RunForward,
                    &RunBackward,
                    &RunForwardCursor,
                    &RunBackwardCursor,
                    &SampleForward,
                    &SampleBackward));

            ScratchCache<TTrack, TClip, TData>.Install(
                index,
                entry,
                new ScratchRun<TTrack, TClip, TData>(
                    entry,
                    &RunForwardScratch,
                    &RunBackwardScratch));
        }

        private static Playback RunForward(Timeline.Entry entry, in Playback from, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved,
                -1, out _);
        }

        private static Playback RunBackward(Timeline.Entry entry, in Playback from, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved,
                -1, out _);
        }

        private static Playback RunForwardCursor(Timeline.Entry entry, in Playback from, ref Cursor cursor, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var valid = ReferenceEquals(cursor.Owner, entry) && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var result = PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved,
                hint, out var region);

            cursor = new Cursor { Owner = entry, Tick = result.Tick, Region = region };
            return result;
        }

        private static Playback RunBackwardCursor(Timeline.Entry entry, in Playback from, ref Cursor cursor, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var valid = ReferenceEquals(cursor.Owner, entry) && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var result = PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved,
                hint, out var region);

            cursor = new Cursor { Owner = entry, Tick = result.Tick, Region = region };
            return result;
        }

        private static Playback RunForwardScratch(Timeline.Entry entry, in Playback from, ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: false, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, scratch,
                -1, out _);
        }

        private static Playback RunBackwardScratch(Timeline.Entry entry, in Playback from, ref TData data, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();

            return PlaybackCore.Advance<TTrack, TClip, TData>(
                in from, backward: true, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, scratch,
                -1, out _);
        }

        private static void SampleForward(Timeline.Entry entry, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TData>(
                backward: false, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved);
        }

        private static void SampleBackward(Timeline.Entry entry, ref TData data, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackRows = entry.TrackRows.AsSpan();
            var clipRows = entry.ClipRows.AsSpan();
            var edges = entry.ClipEdges.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var payloadMap = tables.PayloadMap.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TData>(
                backward: true, entry.Loops, ticks, ref data,
                starts, regionRows, trackRows, clipRows, edges, trackData, clipData, payloadMap, resolved);
        }
    }
}
