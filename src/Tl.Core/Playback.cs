using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

[Flags]
public enum FrameFlags : byte
{
    None = 0,
    ClipStart = 1 << 0,
    ClipEnd = 1 << 1,
    TimelineStart = 1 << 2,
    TimelineEnd = 1 << 3,
    CompletedBefore = 1 << 4,
    CompletedAfter = 1 << 5,
    Looping = 1 << 6,
    Reverse = 1 << 7,
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
