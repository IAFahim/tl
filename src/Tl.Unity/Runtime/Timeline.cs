using System.Runtime.CompilerServices;

namespace Tl
{
    public interface ITimelineInput<TOutput> where TOutput : struct
    {
        bool TryForward(ushort id, in Playback playback, uint tick, ref TOutput output, out Playback next);
        bool TryBackward(ushort id, in Playback playback, uint tick, ref TOutput output, out Playback next);
    }

    public readonly struct TimelineHandle
    {
        public readonly ushort Id;

        public TimelineHandle(ushort id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryForward<TInput, TOutput>(in Playback playback, uint tick, in TInput input, ref TOutput output, out Playback next)
            where TInput : struct, ITimelineInput<TOutput>
            where TOutput : struct
        {
            return Timeline.TryForward(Id, in playback, tick, in input, ref output, out next);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryBackward<TInput, TOutput>(in Playback playback, uint tick, in TInput input, ref TOutput output, out Playback next)
            where TInput : struct, ITimelineInput<TOutput>
            where TOutput : struct
        {
            return Timeline.TryBackward(Id, in playback, tick, in input, ref output, out next);
        }
    }

    public readonly struct TimelineCollection
    {
        public TimelineHandle this[ushort id]
        {
            get { return new TimelineHandle(id); }
        }
    }

    public static class Timeline
    {
        public static TimelineCollection All
        {
            get { return default(TimelineCollection); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Playback Start(ushort id, uint at = 0)
        {
            return new Playback(at, 0, id, PlaybackFlags.Started);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryForward<TInput, TOutput>(ushort id, in Playback playback, uint tick, in TInput input, ref TOutput output, out Playback next)
            where TInput : struct, ITimelineInput<TOutput>
            where TOutput : struct
        {
            return input.TryForward(id, in playback, tick, ref output, out next);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryBackward<TInput, TOutput>(ushort id, in Playback playback, uint tick, in TInput input, ref TOutput output, out Playback next)
            where TInput : struct, ITimelineInput<TOutput>
            where TOutput : struct
        {
            return input.TryBackward(id, in playback, tick, ref output, out next);
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
                : new Playback(playback.Tick, playback.Cycles, id, playback.Flags | PlaybackFlags.Stopped);
            return true;
        }
    }
}
