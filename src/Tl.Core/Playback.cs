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
    public readonly ushort Position;

    public TimelineState(uint asset, ushort position = 0)
    {
        Asset = asset;
        Position = position;
    }
}

public static class TimelineMovement
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Advance(
        ushort duration,
        bool looping,
        bool reverse,
        ushort position,
        out ushort nextPosition,
        out ushort tick,
        out FrameFlags flags)
    {
        nextPosition = position; tick = 0; flags = FrameFlags.None;
        if (duration == 0 || position > duration || looping && position == duration) return false;
        if (looping)
        {
            flags = reverse ? FrameFlags.Looping | FrameFlags.Reverse : FrameFlags.Looping;
            if (reverse)
            {
                tick = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
                nextPosition = tick;
            }
            else
            {
                tick = position;
                nextPosition = tick == duration - 1 ? (ushort)0 : (ushort)(tick + 1);
            }
            if (tick == 0) flags |= FrameFlags.TimelineStart;
            if (tick == duration - 1) flags |= FrameFlags.TimelineEnd;
            return true;
        }
        if (reverse)
        {
            if (position == 0) return false;
            tick = (ushort)(position - 1); flags = FrameFlags.Reverse;
            if (tick == 0) flags |= FrameFlags.TimelineStart;
            if (tick == duration - 1) flags |= FrameFlags.TimelineEnd;
            if (position == duration) flags |= FrameFlags.CompletedBefore;
            nextPosition = tick;
            return true;
        }
        if (position == duration) return false;
        tick = position;
        if (tick == 0) flags = FrameFlags.TimelineStart;
        if (tick == duration - 1) flags |= FrameFlags.TimelineEnd | FrameFlags.CompletedAfter;
        nextPosition = (ushort)(tick + 1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Select(
        in TimelineState state,
        ushort duration,
        bool looping,
        bool reverse,
        out TimelineState next,
        out ushort tick,
        out FrameFlags flags)
    {
        next = state; tick = 0; flags = FrameFlags.None;
        if (state.Asset == 0 || !Advance(duration, looping, reverse, state.Position, out var np, out tick, out flags))
            return false;
        next = new TimelineState(state.Asset, np);
        return true;
    }
}
