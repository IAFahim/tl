using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Authoring;
using Tl.Generation;
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
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var forward = (delegate*<NativeEntry*, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback>)(void*)slot.Forward;
        return forward(entry, in playback, in input, ref result, ticks);
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
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var backward = (delegate*<NativeEntry*, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback>)(void*)slot.Backward;
        return backward(entry, in playback, in input, ref result, ticks);
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
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var forward = (delegate*<NativeEntry*, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback>)(void*)slot.ForwardCursor;
        return forward(entry, in playback, ref cursor, in input, ref result, ticks);
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
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var backward = (delegate*<NativeEntry*, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback>)(void*)slot.BackwardCursor;
        return backward(entry, in playback, ref cursor, in input, ref result, ticks);
    }

    public static void Forward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var sample = (delegate*<NativeEntry*, in TInput, ref TResult, ReadOnlySpan<uint>, void>)(void*)slot.SampleForward;
        sample(entry, in input, ref result, ticks);
    }

    public static void Backward<TInput, TResult>(ushort index, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)
        where TInput : struct
        where TResult : struct
    {
        var entry = Live(index);
        var slot = NativeBinding.Get<TInput, TResult>(entry);
        var sample = (delegate*<NativeEntry*, in TInput, ref TResult, ReadOnlySpan<uint>, void>)(void*)slot.SampleBackward;
        sample(entry, in input, ref result, ticks);
    }
}

// The runtime-authored timeline under the unmanaged boundary: authoring
// syntax (Build) produces an authoring token; the terminal operation
// (InMemory) lowers it into one native block and returns the ushort
// handle. TTrack joins TClip in the unmanaged constraint — the payload
// tables are copied into native memory, which is only correct for types
// the GC never needs to see (the honest price of native storage).
public static unsafe class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    // Closure identity: one machine word per closed Timeline<,> type,
    // minted from consumer code's generic instantiation — binding
    // metadata (like the (TInput, TResult) token), not timeline state.
    internal static readonly nint ClosureId = Timeline.NextClosureId();

    public static TimelineAuthoring<TTrack, TClip> Build(TimelineBuild<TTrack, TClip> build)
        => Build(build, TimelineOptions.Default);

    public static TimelineAuthoring<TTrack, TClip> Build(TimelineBuild<TTrack, TClip> build, TimelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(options);
        build(builder);
        return new TimelineAuthoring<TTrack, TClip>(builder._state);
    }

    public static TimelineAuthoring<TTrack, TClip> Build<TSource>(TSource source, TimelineBuild<TTrack, TClip, TSource> build)
        => Build(source, build, TimelineOptions.Default);

    public static TimelineAuthoring<TTrack, TClip> Build<TSource>(TSource source, TimelineBuild<TTrack, TClip, TSource> build, TimelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(build);

        var builder = new TimelineBuilder<TTrack, TClip>(options);
        build(builder, source);
        return new TimelineAuthoring<TTrack, TClip>(builder._state);
    }

    public static void Bind<TInput, TResult>(ushort index)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        var entry = Timeline.Live(index);
        if (entry->ClosureId != ClosureId)
            throw new InvalidOperationException($"Timeline index {index} does not belong to the <{typeof(TTrack).Name}, {typeof(TClip).Name}> closure.");
        Bridge<TInput, TResult>.Install(entry);
    }

    private static readonly MethodInfo s_bind =
        typeof(Timeline<TTrack, TClip>).GetMethod(nameof(BindEntryData), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(BindEntryData));

    // The automatic (input, result) bridge bind for a Type pair — the cold
    // path a first playback call takes when no explicit Bind ran. Stored in
    // the native entry as a function pointer (its address also pins the
    // closure identity the lowerer recorded).
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
        Justification = "The generic method is closed over the input/result pair; AOT consumers use Bind<TInput,TResult> instead.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:MakeGenericMethod",
        Justification = "Guarded by the IsDynamicCodeSupported check; AOT consumers use Bind<TInput,TResult> instead.")]
    internal static void BindByType(Type input, Type result, ushort index)
    {
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException(
                $"The automatic (entry, input, result) bridge bind needs dynamic code; under NativeAOT call " +
                $"{typeof(Timeline<TTrack, TClip>).Name}.Bind<TInput,TResult>(index) once per pair instead.");
        s_bind.MakeGenericMethod(input, result).Invoke(null, [index]);
    }

    private static void BindEntryData<TInput, TResult>(ushort index)
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
        => Bridge<TInput, TResult>.Install(Timeline.Live(index));

    // The bridge: the eight entry points of one (TInput, TResult) pair over
    // this closure's native representation. Each body wraps the SAME
    // span-based PlaybackCore the generated tables shell uses — spans over
    // native memory, adapter only at this boundary.
    private static unsafe class Bridge<TInput, TResult>
        where TInput : struct
        where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>
    {
        public static void Install(NativeEntry* entry)
        {
            NativeBinding.Install(entry, new NativeBindSlot
            {
                Token = BindingIds<TInput, TResult>.Token,
                Forward = Address(&RunForward),
                Backward = Address(&RunBackward),
                ForwardCursor = Address(&RunForwardCursor),
                BackwardCursor = Address(&RunBackwardCursor),
                SampleForward = Address(&SampleForward),
                SampleBackward = Address(&SampleBackward),
            });
        }

        private static nint Address(delegate*<NativeEntry*, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> f)
            => (nint)(void*)f;

        private static nint Address(delegate*<NativeEntry*, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> f)
            => (nint)(void*)f;

        private static nint Address(delegate*<NativeEntry*, in TInput, ref TResult, ReadOnlySpan<uint>, void> f)
            => (nint)(void*)f;

        private static Playback RunForward(NativeEntry* entry, in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots,
                -1, out _);
        }

        private static Playback RunBackward(NativeEntry* entry, in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            return PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots,
                -1, out _);
        }

        private static Playback RunForwardCursor(NativeEntry* entry, in Playback from, ref Cursor cursor, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var valid = cursor.Owner == (nint)entry && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: false, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots,
                hint, out var region);

            cursor = new Cursor { Owner = (nint)entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        private static Playback RunBackwardCursor(NativeEntry* entry, in Playback from, ref Cursor cursor, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            var valid = cursor.Owner == (nint)entry && cursor.Tick == from.Tick;
            var hint = valid ? cursor.Region : -1;

            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            var playback = PlaybackCore.Advance<TTrack, TClip, TInput, TResult>(
                in from, backward: true, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots,
                hint, out var region);

            cursor = new Cursor { Owner = (nint)entry, Tick = playback.Tick, Region = region };
            return playback;
        }

        private static void SampleForward(NativeEntry* entry, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: false, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots);
        }

        private static void SampleBackward(NativeEntry* entry, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        {
            View(entry, out var starts, out var regionRows, out var trackData, out var clipData, out var workSlots);

            PlaybackCore.Sample<TTrack, TClip, TInput, TResult>(
                backward: true, entry->Loops != 0, ticks, in input, ref result,
                starts, regionRows, trackData, clipData, workSlots);
        }

        // Spans over the native tables: the whole adaptation to unmanaged
        // storage. PlaybackCore is unchanged — it consumes spans either way.
        private static void View(
            NativeEntry* entry,
            out ReadOnlySpan<uint> starts,
            out ReadOnlySpan<RegionRow> regionRows,
            out ReadOnlySpan<TTrack> trackData,
            out ReadOnlySpan<TClip> clipData,
            out ReadOnlySpan<WorkSlot> workSlots)
        {
            starts = new ReadOnlySpan<uint>(entry->RegionStarts, entry->RegionCount);
            regionRows = new ReadOnlySpan<RegionRow>(entry->RegionRows, entry->RegionCount);
            trackData = new ReadOnlySpan<TTrack>(entry->TrackData, entry->TrackCount);
            clipData = new ReadOnlySpan<TClip>(entry->ClipData, entry->PayloadCount);
            workSlots = new ReadOnlySpan<WorkSlot>(entry->WorkSlots, entry->WorkSlotCount);
        }
    }
}
