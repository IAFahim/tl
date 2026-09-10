using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public interface ITimeline
{
    static abstract void Define(scoped Builder builder);
}

public interface ITrack<TClip> : IBlend<TClip>
    where TClip : unmanaged;

public interface IHook;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct CompiledRoute(byte Module, byte Ordinal);

public readonly ref struct TrackRef<TTrack>
    where TTrack : unmanaged;

public readonly ref struct Builder
{
    public TrackRef<TTrack> Track<TTrack>(in TTrack track)
        where TTrack : unmanaged
        => default;

    public void Clip<TTrack, TClip>(in TrackRef<TTrack> track, in TClip clip, uint start, uint end)
        where TTrack : unmanaged, ITrack<TClip>
        where TClip : unmanaged
    {
    }

    public void Looping()
    {
    }

    public void Before<THook>() where THook : unmanaged, IHook
    {
    }

    public void After<THook>() where THook : unmanaged, IHook
    {
    }

    public void Include<TTimeline>() where TTimeline : unmanaged, ITimeline
    {
    }
}

public readonly ref struct Frame<TTrack, TClip>
    where TTrack : unmanaged, ITrack<TClip>
    where TClip : unmanaged
{
    private readonly ref readonly TTrack _track;
    private readonly ref readonly TClip _clip;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Frame(in TTrack track, in TClip clip, uint tick, ClipState state, ushort trackIndex)
    {
        _track = ref track;
        _clip = ref clip;
        Tick = tick;
        State = state;
        TrackIndex = trackIndex;
    }

    public ref readonly TTrack Track => ref _track;
    public ref readonly TClip Clip => ref _clip;
    public uint Tick { get; }
    public ClipState State { get; }
    public ushort TrackIndex { get; }
}

public interface ITimelineInput<TSelf, TOutput>
    where TSelf : ITimelineInput<TSelf, TOutput>, allows ref struct
    where TOutput : allows ref struct
{
    static abstract bool TryForward(
        ushort id,
        in Playback playback,
        uint tick,
        scoped in TSelf input,
        scoped ref TOutput output,
        out Playback next);

    static abstract bool TryBackward(
        ushort id,
        in Playback playback,
        uint tick,
        scoped in TSelf input,
        scoped ref TOutput output,
        out Playback next);

    static abstract bool TryForward(
        ushort id,
        in Playback playback,
        ReadOnlySpan<uint> ticks,
        scoped in TSelf input,
        scoped ref TOutput output,
        out Playback next);

    static abstract bool TryBackward(
        ushort id,
        in Playback playback,
        ReadOnlySpan<uint> ticks,
        scoped in TSelf input,
        scoped ref TOutput output,
        out Playback next);
}

public static unsafe class Timeline
{
    private const nint CompiledTag = 1;
    private const int CompiledDurationShift = 2;
    private const int CompiledRouteShift = 34;
    private const nint CompiledLoopMask = 2;

    private static nint s_pages;
    private static int s_nextIndex;
    private static int s_nextModule;
    private static int s_gate;
    private static long s_registryRetainedBytes;

    public static long RegistryRetainedBytes => Volatile.Read(ref s_registryRetainedBytes);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte RegisterModule()
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextModule);
            if (next > byte.MaxValue)
                throw new InvalidOperationException("Timeline module capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref s_nextModule, next + 1, next) != next);

        return (byte)next;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ushort RegisterCompiled(uint duration, bool loops, CompiledRoute route)
    {
        if (sizeof(nint) == sizeof(long))
            return RegisterSlot(EncodeCompiled(duration, loops, route));

        var metadata = (CompiledMetadata*)NativeMemory.Alloc((nuint)sizeof(CompiledMetadata));
        if (metadata == null)
            throw new OutOfMemoryException();
        metadata->TimelineDuration = duration;
        metadata->Loops = loops ? (byte)1 : (byte)0;
        metadata->Module = route.Module;
        metadata->Ordinal = route.Ordinal;
        try
        {
            var index = RegisterSlot((nint)metadata | CompiledTag);
            Interlocked.Add(ref s_registryRetainedBytes, sizeof(CompiledMetadata));
            return index;
        }
        catch
        {
            NativeMemory.Free(metadata);
            throw;
        }
    }

    public static bool IsValid(ushort id) => TryReadSlot(id, out _);

    public static uint Duration(ushort id)
        => CompiledDuration(RequireSlot(id));

    public static bool IsLooping(ushort id)
        => CompiledLoops(RequireSlot(id));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool TryGetCompiledRoute(ushort id, out CompiledRoute route)
    {
        if (!TryReadSlot(id, out var slot))
        {
            route = default;
            return false;
        }

        route = CompiledRouting(slot);
        return true;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Playback CreateCompiledPlayback(ushort owner, uint tick, ushort cycles, PlaybackFlags flags)
        => new(tick, cycles, owner, flags);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Playback<TTimeline> CreateTypedPlayback<TTimeline>(uint tick, ushort cycles, PlaybackFlags flags)
        where TTimeline : unmanaged, ITimeline
        => new(tick, cycles, flags);

    public static bool TryStart(ushort id, uint at, out Playback playback)
    {
        if (!IsValid(id))
        {
            playback = default;
            return false;
        }

        playback = new Playback(at, 0, id, PlaybackFlags.Started);
        return true;
    }

    public static bool TryStart(ushort id, out Playback playback)
        => TryStart(id, 0, out playback);

    public static bool TryStop(ushort id, in Playback playback, out Playback stopped)
    {
        if (!IsValid(id) || playback.Owner != id || !playback.Has(PlaybackFlags.Started))
        {
            stopped = playback;
            return false;
        }

        stopped = playback.Has(PlaybackFlags.Stopped)
            ? playback
            : new Playback(playback.Tick, playback.Cycles, id, playback.Flags | PlaybackFlags.Stopped);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryForward<TInput, TOutput>(
        ushort id,
        in Playback playback,
        uint tick,
        scoped in TInput input,
        scoped ref TOutput output,
        out Playback next)
        where TInput : ITimelineInput<TInput, TOutput>, allows ref struct
        where TOutput : allows ref struct
        => TInput.TryForward(id, in playback, tick, in input, ref output, out next);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryBackward<TInput, TOutput>(
        ushort id,
        in Playback playback,
        uint tick,
        scoped in TInput input,
        scoped ref TOutput output,
        out Playback next)
        where TInput : ITimelineInput<TInput, TOutput>, allows ref struct
        where TOutput : allows ref struct
        => TInput.TryBackward(id, in playback, tick, in input, ref output, out next);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryForward<TInput, TOutput>(
        ushort id,
        in Playback playback,
        ReadOnlySpan<uint> ticks,
        scoped in TInput input,
        scoped ref TOutput output,
        out Playback next)
        where TInput : ITimelineInput<TInput, TOutput>, allows ref struct
        where TOutput : allows ref struct
        => TInput.TryForward(id, in playback, ticks, in input, ref output, out next);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryBackward<TInput, TOutput>(
        ushort id,
        in Playback playback,
        ReadOnlySpan<uint> ticks,
        scoped in TInput input,
        scoped ref TOutput output,
        out Playback next)
        where TInput : ITimelineInput<TInput, TOutput>, allows ref struct
        where TOutput : allows ref struct
        => TInput.TryBackward(id, in playback, ticks, in input, ref output, out next);

    private static ushort RegisterSlot(nint slot)
    {
        var index = ReserveIndex();
        PublishSlot(index, slot);
        return index;
    }

    private static ushort ReserveIndex()
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextIndex);
            if (next > ushort.MaxValue)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref s_nextIndex, next + 1, next) != next);

        return (ushort)next;
    }

    private static void PublishSlot(ushort index, nint slot)
    {
        Acquire(ref s_gate);
        try
        {
            var pages = (nint*)s_pages;
            if (pages == null)
            {
                pages = (nint*)NativeMemory.AllocZeroed(256, (nuint)sizeof(nint));
                if (pages == null)
                    throw new OutOfMemoryException();
                Interlocked.Add(ref s_registryRetainedBytes, 256L * sizeof(nint));
                Volatile.Write(ref s_pages, (nint)pages);
            }

            var pageIndex = index >> 8;
            var page = (nint*)pages[pageIndex];
            if (page == null)
            {
                page = (nint*)NativeMemory.AllocZeroed(256, (nuint)sizeof(nint));
                if (page == null)
                    throw new OutOfMemoryException();
                Interlocked.Add(ref s_registryRetainedBytes, 256L * sizeof(nint));
                Volatile.Write(ref pages[pageIndex], (nint)page);
            }

            Volatile.Write(ref page[(byte)index], slot);
        }
        finally
        {
            Release(ref s_gate);
        }
    }

    private static bool TryReadSlot(ushort id, out nint slot)
    {
        var pages = (nint*)Volatile.Read(ref s_pages);
        if (pages == null)
        {
            slot = 0;
            return false;
        }

        var page = (nint*)Volatile.Read(ref pages[id >> 8]);
        if (page == null)
        {
            slot = 0;
            return false;
        }

        slot = Volatile.Read(ref page[(byte)id]);
        return slot != 0;
    }

    private static nint RequireSlot(ushort id)
    {
        if (!TryReadSlot(id, out var slot))
            throw new ArgumentOutOfRangeException(nameof(id), id, "Timeline index is not live.");
        return slot;
    }

    private static nint EncodeCompiled(uint duration, bool loops, CompiledRoute route)
        => (nint)(
            ((ulong)route.Module << (CompiledRouteShift + 8)) |
            ((ulong)route.Ordinal << CompiledRouteShift) |
            ((ulong)duration << CompiledDurationShift) |
            (loops ? (ulong)CompiledLoopMask : 0) |
            (ulong)CompiledTag);

    private static uint CompiledDuration(nint slot)
        => sizeof(nint) == sizeof(long)
            ? (uint)((ulong)slot >> CompiledDurationShift)
            : ((CompiledMetadata*)(slot & ~CompiledTag))->TimelineDuration;

    private static bool CompiledLoops(nint slot)
        => sizeof(nint) == sizeof(long)
            ? (slot & CompiledLoopMask) != 0
            : ((CompiledMetadata*)(slot & ~CompiledTag))->Loops != 0;

    private static CompiledRoute CompiledRouting(nint slot)
        => sizeof(nint) == sizeof(long)
            ? new(
                (byte)((ulong)slot >> (CompiledRouteShift + 8)),
                (byte)((ulong)slot >> CompiledRouteShift))
            : new(
                ((CompiledMetadata*)(slot & ~CompiledTag))->Module,
                ((CompiledMetadata*)(slot & ~CompiledTag))->Ordinal);

    private static void Acquire(ref int gate)
    {
        while (Interlocked.CompareExchange(ref gate, 1, 0) != 0)
            Thread.SpinWait(16);
    }

    private static void Release(ref int gate) => Volatile.Write(ref gate, 0);

    [StructLayout(LayoutKind.Sequential)]
    private struct CompiledMetadata
    {
        public uint TimelineDuration;
        public byte Module;
        public byte Ordinal;
        public byte Loops;
    }
}
