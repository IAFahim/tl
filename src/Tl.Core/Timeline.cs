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
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        return run.Forward(entry, in playback, in input, ref result, ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, in uint tick)
        where TInput : struct
        where TResult : struct
    {
        ReadOnlySpan<uint> ticks = [tick];
        return Backward(index, in playback, in input, ref result, ticks);
    }

    public static Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        PlaybackCore.RequireRunnable(in playback);
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        return run.Backward(entry, in playback, in input, ref result, ticks);
    }

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
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        return run.ForwardCursor(entry, in playback, ref cursor, in input, ref result, ticks);
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
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        return run.BackwardCursor(entry, in playback, ref cursor, in input, ref result, ticks);
    }

    public static void Forward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        run.SampleForward(entry, in input, ref result, ticks);
    }

    public static void Backward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var run = BindingCache<TInput, TResult>.Get(index, entry);
        run.SampleBackward(entry, in input, ref result, ticks);
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

    public static void Bind<TInput, TResult>(ushort index)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new InvalidOperationException($"Timeline index {index} does not belong to the <{typeof(TTrack).Name}, {typeof(TClip).Name}> closure.");
        Bridge<TInput, TResult>.Install(index, entry);
    }

    public static unsafe Playback Forward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new ArgumentException("Timeline belongs to a different track/clip pair.", nameof(index));
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = ScratchCache<TTrack, TClip, TInput, TResult>.Get(index, entry);
        return run.Forward(entry, in playback, in input, ref result, scratch, ticks);
    }

    public static unsafe Playback Backward<TInput, TResult>(ushort index, in Playback playback, in TInput input, ref TResult result, Span<TClip> scratch, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Timeline.Live(index);
        if (entry.Payload is not Tables)
            throw new ArgumentException("Timeline belongs to a different track/clip pair.", nameof(index));
        PlaybackCore.RequireRunnable(in playback);
        if (scratch.Length < entry.MaxActiveBlends)
            throw new ArgumentException("Scratch buffer is too small for this timeline's blends.", nameof(scratch));
        var run = ScratchCache<TTrack, TClip, TInput, TResult>.Get(index, entry);
        return run.Backward(entry, in playback, in input, ref result, scratch, ticks);
    }

    private static readonly MethodInfo s_bind =
        typeof(Timeline<TTrack, TClip>).GetMethod(nameof(BindEntryData), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(BindEntryData));

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
        Justification = "The generic method is closed over the input/result pair; AOT consumers use Bind<TInput,TResult> instead.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:MakeGenericMethod",
        Justification = "Guarded by the IsDynamicCodeSupported check; AOT consumers use Bind<TInput,TResult> instead.")]
    private static void BindType(Type input, Type result, Timeline.Entry entry)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException(
                $"The automatic (entry, input, result) bridge bind needs dynamic code; under NativeAOT call " +
                $"{typeof(Timeline<TTrack, TClip>).Name}.Bind<TInput,TResult>(index) once per pair instead.");
        s_bind.MakeGenericMethod(input, result).Invoke(null, [entry]);
    }

    private static void BindEntryData<TInput, TResult>(Timeline.Entry entry)
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
        => Bridge<TInput, TResult>.Install(entry.Index, entry);

    private static unsafe class Bridge<TInput, TResult>
        where TInput : struct
        where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
    {
        public static void Install(ushort index, Timeline.Entry entry)
        {
            BindingCache<TInput, TResult>.Install(
                index,
                entry,
                new Run<TInput, TResult>(
                    entry,
                    &RunForward,
                    &RunBackward,
                    &RunForwardCursor,
                    &RunBackwardCursor,
                    &SampleForward,
                    &SampleBackward));

            ScratchCache<TTrack, TClip, TInput, TResult>.Install(
                index,
                entry,
                new ScratchRun<TTrack, TClip, TInput, TResult>(
                    entry,
                    &RunForwardScratch,
                    &RunBackwardScratch));
        }

        private static Playback RunForward(Timeline.Entry entry, in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        private static Playback RunBackward(Timeline.Entry entry, in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots,
                -1, out _);
        }

        private static Playback RunForwardCursor(Timeline.Entry entry, in Playback from, ref Cursor cursor, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var valid = ReferenceEquals(cursor.Owner, entry) && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots,
                hint, out var region);

            cursor = new Cursor { Owner = entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        private static Playback RunBackwardCursor(Timeline.Entry entry, in Playback from, ref Cursor cursor, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var valid = ReferenceEquals(cursor.Owner, entry) && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots,
                hint, out var region);

            cursor = new Cursor { Owner = entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        private static Playback RunForwardScratch(Timeline.Entry entry, in Playback from, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, scratch, workSlots,
                -1, out _);
        }

        private static Playback RunBackwardScratch(Timeline.Entry entry, in Playback from, in TInput input, ref TResult result, Span<TClip> scratch, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, scratch, workSlots,
                -1, out _);
        }

        private static void SampleForward(Timeline.Entry entry, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: false, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots);
        }

        private static void SampleBackward(Timeline.Entry entry, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var tables = Unsafe.As<Tables>(entry.Payload);
            var starts = entry.RegionStarts.AsSpan();
            var regionRows = entry.RegionRows.AsSpan();
            var trackData = tables.TrackData.AsSpan();
            var clipData = tables.ClipData.AsSpan();
            var workSlots = entry.WorkSlots.AsSpan();
            Span<TClip> resolved = stackalloc TClip[BlendScratch.StackCount<TClip>(entry.MaxActiveBlends)];

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: true, entry.Loops, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, resolved, workSlots);
        }
    }
}
