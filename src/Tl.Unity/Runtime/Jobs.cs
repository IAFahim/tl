using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl
{
    public interface IBlend<TClip>
        where TClip : unmanaged
    {
        void Blend(in TClip first, in TClip second, float factor, out TClip result);
    }

    public interface ITimelineJob<TTrack, TClip>
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
    }

    public interface ITimelineCatalog
    {
    }

    public interface IHook
    {
    }

    public readonly ref struct TrackRef<TTrack>
        where TTrack : unmanaged
    {
        public TrackRef<TTrack, TJob> Use<TJob>()
            where TJob : unmanaged
        {
            return default(TrackRef<TTrack, TJob>);
        }
    }

    public readonly ref struct TrackRef<TTrack, TJob>
        where TTrack : unmanaged
        where TJob : unmanaged
    {
    }

    public readonly ref struct Builder
    {
        public TrackRef<TTrack> Track<TTrack>(in TTrack track)
            where TTrack : unmanaged
        {
            return default(TrackRef<TTrack>);
        }

        public void Clip<TTrack, TJob, TClip>(in TrackRef<TTrack, TJob> track, in TClip clip, uint start, uint end)
            where TTrack : unmanaged, IBlend<TClip>
            where TJob : unmanaged, ITimelineJob<TTrack, TClip>
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

    public readonly ref struct CatalogBuilder
    {
        public SchemaBuilder<TSchema> Schema<TSchema>()
            where TSchema : unmanaged
        {
            return default(SchemaBuilder<TSchema>);
        }
    }

    public readonly ref struct SchemaBuilder<TSchema>
        where TSchema : unmanaged
    {
        public SchemaBuilder<TSchema> Asset<TTimeline>()
            where TTimeline : unmanaged, ITimeline
        {
            return this;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TimelineFrame
    {
        public TimelineFrame(uint gameTick, uint timelineTick, long cycle, FrameFlags flags)
        {
            GameTick = gameTick;
            TimelineTick = timelineTick;
            Cycle = cycle;
            Flags = flags;
        }

        public uint GameTick { get; }
        public uint TimelineTick { get; }
        public long Cycle { get; }
        public FrameFlags Flags { get; }
        public int Direction { get { return Has(FrameFlags.Reverse) ? -1 : 1; } }
        public bool Has(FrameFlags flags) { return (Flags & flags) == flags; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TimelineState
    {
        public readonly uint Asset;
        public readonly uint Position;
        public readonly long Cycle;

        public TimelineState(uint asset, uint position = 0, long cycle = 0)
        {
            Asset = asset;
            Position = position;
            Cycle = cycle;
        }
    }

    public static class TimelineMovement
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Select(
            in TimelineState state,
            uint duration,
            bool looping,
            bool reverse,
            out TimelineState next,
            out uint tick,
            out long cycle,
            out FrameFlags flags)
        {
            next = state;
            tick = 0;
            cycle = 0;
            flags = FrameFlags.None;
            if (state.Asset == 0 || duration == 0 || state.Position > duration || looping && state.Position == duration)
                return false;

            if (looping)
            {
                flags = reverse ? FrameFlags.Looping | FrameFlags.Reverse : FrameFlags.Looping;
                if (reverse)
                {
                    if (state.Position == 0)
                    {
                        tick = duration - 1u;
                        cycle = unchecked(state.Cycle - 1L);
                    }
                    else
                    {
                        tick = state.Position - 1u;
                        cycle = state.Cycle;
                    }
                    next = new TimelineState(state.Asset, tick, cycle);
                }
                else
                {
                    tick = state.Position;
                    cycle = state.Cycle;
                    next = tick == duration - 1u
                        ? new TimelineState(state.Asset, 0, unchecked(state.Cycle + 1L))
                        : new TimelineState(state.Asset, tick + 1u, state.Cycle);
                }

                if (tick == 0)
                    flags |= FrameFlags.TimelineStart;
                if (tick == duration - 1u)
                    flags |= FrameFlags.TimelineEnd;
                return true;
            }

            if (reverse)
            {
                if (state.Position == 0)
                    return false;
                tick = state.Position - 1u;
                flags = FrameFlags.Reverse;
                if (tick == 0)
                    flags |= FrameFlags.TimelineStart;
                if (tick == duration - 1u)
                    flags |= FrameFlags.TimelineEnd;
                if (state.Position == duration)
                    flags |= FrameFlags.CompletedBefore;
            }
            else
            {
                if (state.Position == duration)
                    return false;
                tick = state.Position;
                if (tick == 0)
                    flags = FrameFlags.TimelineStart;
                if (tick == duration - 1u)
                    flags |= FrameFlags.TimelineEnd | FrameFlags.CompletedAfter;
            }

            next = new TimelineState(state.Asset, reverse ? tick : tick + 1u);
            return true;
        }
    }
}
