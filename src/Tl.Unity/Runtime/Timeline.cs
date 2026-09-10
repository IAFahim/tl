using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Tl
{
    public static class Timeline
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Playback Start(ushort id, uint gameTick)
        {
            return new Playback(0L, gameTick, id, PlaybackFlags.Started);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryStop(ushort id, in Playback playback, out Playback stopped)
        {
            if (playback.Owner != id || !playback.Has(PlaybackFlags.Started))
            {
                stopped = playback;
                return false;
            }

            stopped = playback.Has(PlaybackFlags.Stopped)
                ? playback
                : new Playback(playback.Position, playback.GameTick, id, playback.Flags | PlaybackFlags.Stopped);
            return true;
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
