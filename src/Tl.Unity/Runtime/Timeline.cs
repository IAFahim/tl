using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Tl
{
    public static class Timeline
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Playback<TTimeline> Start<TTimeline>(uint gameTick)
            where TTimeline : unmanaged, ITimeline
        {
            return new Playback<TTimeline>(0L, gameTick, PlaybackFlags.Started);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryStop<TTimeline>(
            in Playback<TTimeline> playback,
            out Playback<TTimeline> stopped)
            where TTimeline : unmanaged, ITimeline
        {
            if (!playback.Has(PlaybackFlags.Started))
            {
                stopped = playback;
                return false;
            }

            stopped = playback.Has(PlaybackFlags.Stopped)
                ? playback
                : new Playback<TTimeline>(playback.Position, playback.GameTick, playback.Flags | PlaybackFlags.Stopped);
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Playback<TTimeline> CreateTypedPlayback<TTimeline>(
            long position,
            uint gameTick,
            PlaybackFlags flags)
            where TTimeline : unmanaged, ITimeline
        {
            return new Playback<TTimeline>(position, gameTick, flags);
        }
    }

    public unsafe readonly ref struct Frame<TTrack, TClip>
        where TTrack : unmanaged
        where TClip : unmanaged
    {
        private readonly TTrack* _track;
        private readonly TClip* _clip;

        public Frame(
            in TTrack track,
            in TClip clip,
            uint gameTick,
            uint timelineTick,
            long cycle,
            ushort trackIndex,
            FrameFlags flags)
        {
            _track = (TTrack*)UnsafeUtilityExtensions.AddressOf(in track);
            _clip = (TClip*)UnsafeUtilityExtensions.AddressOf(in clip);
            GameTick = gameTick;
            TimelineTick = timelineTick;
            Cycle = cycle;
            TrackIndex = trackIndex;
            Flags = flags;
        }

        public ref readonly TTrack Track
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref *_track; }
        }

        public ref readonly TClip Clip
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref *_clip; }
        }

        public uint GameTick { get; }
        public uint TimelineTick { get; }
        public long Cycle { get; }
        public ushort TrackIndex { get; }
        public FrameFlags Flags { get; }

        public int Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Has(FrameFlags.Reverse) ? -1 : 1; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(FrameFlags flags)
        {
            return (Flags & flags) == flags;
        }
    }
}
