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
    public Frame(
        in TTrack track,
        in TClip clip,
        uint gameTick,
        uint timelineTick,
        long cycle,
        ushort trackIndex,
        FrameFlags flags)
    {
        _track = ref track;
        _clip = ref clip;
        GameTick = gameTick;
        TimelineTick = timelineTick;
        Cycle = cycle;
        TrackIndex = trackIndex;
        Flags = flags;
    }

    public ref readonly TTrack Track => ref _track;
    public ref readonly TClip Clip => ref _clip;
    public uint GameTick { get; }
    public uint TimelineTick { get; }
    public long Cycle { get; }
    public ushort TrackIndex { get; }
    public FrameFlags Flags { get; }
    public int Direction => Has(FrameFlags.Reverse) ? -1 : 1;
    public bool Has(FrameFlags flags) => (Flags & flags) == flags;
}

public interface ITimelineData<TSelf>
    where TSelf : ITimelineData<TSelf>, allows ref struct
{
    static abstract bool TrySeek(ushort id, scoped ref TSelf data, int delta);
}

public static unsafe class Timeline
{
    private const nint CompiledTag = 1;
    private const int CompiledDurationShift = 2;
    private const int CompiledRouteShift = 34;
    private const nint CompiledLoopMask = 2;

    private static nint _sPages;
    private static int _sNextIndex;
    private static int _sNextModule;
    private static int _sGate;
    private static long _sRegistryRetainedBytes;

    public static long RegistryRetainedBytes => Volatile.Read(ref _sRegistryRetainedBytes);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte RegisterModule()
    {
        int next;
        do
        {
            next = Volatile.Read(ref _sNextModule);
            if (next > byte.MaxValue)
                throw new InvalidOperationException("Timeline module capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref _sNextModule, next + 1, next) != next);

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
            Interlocked.Add(ref _sRegistryRetainedBytes, sizeof(CompiledMetadata));
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
    public static Playback CreateCompiledPlayback(ushort owner, long position, uint gameTick, PlaybackFlags flags)
        => new(position, gameTick, owner, flags);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Playback<TTimeline> CreateTypedPlayback<TTimeline>(long position, uint gameTick, PlaybackFlags flags)
        where TTimeline : unmanaged, ITimeline
        => new(position, gameTick, flags);

    public static bool TryStart(ushort id, uint gameTick, out Playback playback)
    {
        if (!IsValid(id))
        {
            playback = default;
            return false;
        }

        playback = new Playback(0, gameTick, id, PlaybackFlags.Started);
        return true;
    }

    public static bool TryStop(ushort id, in Playback playback, out Playback stopped)
    {
        if (!IsValid(id) || playback.Owner != id || !playback.Has(PlaybackFlags.Started))
        {
            stopped = playback;
            return false;
        }

        stopped = playback.Has(PlaybackFlags.Stopped)
            ? playback
            : new Playback(playback.Position, playback.GameTick, id, playback.Flags | PlaybackFlags.Stopped);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySeek<TData>(ushort id, scoped ref TData data, int delta)
        where TData : ITimelineData<TData>, allows ref struct
        => TData.TrySeek(id, ref data, delta);

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
            next = Volatile.Read(ref _sNextIndex);
            if (next > ushort.MaxValue)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref _sNextIndex, next + 1, next) != next);

        return (ushort)next;
    }

    private static void PublishSlot(ushort index, nint slot)
    {
        Acquire(ref _sGate);
        try
        {
            var pages = (nint*)_sPages;
            if (pages == null)
            {
                pages = (nint*)NativeMemory.AllocZeroed(256, (nuint)sizeof(nint));
                if (pages == null)
                    throw new OutOfMemoryException();
                Interlocked.Add(ref _sRegistryRetainedBytes, 256L * sizeof(nint));
                Volatile.Write(ref _sPages, (nint)pages);
            }

            var pageIndex = index >> 8;
            var page = (nint*)pages[pageIndex];
            if (page == null)
            {
                page = (nint*)NativeMemory.AllocZeroed(256, (nuint)sizeof(nint));
                if (page == null)
                    throw new OutOfMemoryException();
                Interlocked.Add(ref _sRegistryRetainedBytes, 256L * sizeof(nint));
                Volatile.Write(ref pages[pageIndex], (nint)page);
            }

            Volatile.Write(ref page[(byte)index], slot);
        }
        finally
        {
            Release(ref _sGate);
        }
    }

    private static bool TryReadSlot(ushort id, out nint slot)
    {
        var pages = (nint*)Volatile.Read(ref _sPages);
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
