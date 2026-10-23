namespace Tl.Core.Tests;

internal readonly record struct OracleStep(bool Crossed, ushort Next, ushort Tick, FrameFlags Flags)
{
    internal static OracleStep Rest(ushort position) => new(false, position, 0, FrameFlags.None);
}

internal static class PlaybackOracle
{
    internal static readonly int[] SweepDurations =
        Enumerable.Range(0, 65).Concat(new[] { 255, 256, 1023, 1024, 2047, 2048, 2049, 4095, 4096, 65534, 65535 }).ToArray();

    internal static OracleStep Step(ushort duration, bool looping, bool forward, ushort position)
    {
        if (duration == 0 || position > duration) return OracleStep.Rest(position);
        if (looping)
        {
            if (position == duration) return OracleStep.Rest(position);
            if (forward)
            {
                var tick = position;
                return new OracleStep(true, (ushort)(tick + 1 == duration ? 0 : tick + 1), tick, Traversal(false, tick, duration) | FrameFlags.Looping);
            }
            var wrap = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
            return new OracleStep(true, wrap, wrap, Traversal(true, wrap, duration) | FrameFlags.Looping);
        }
        if (forward)
        {
            if (position == duration) return OracleStep.Rest(position);
            var tick = position;
            var completion = tick == duration - 1 ? FrameFlags.CompletedAfter : FrameFlags.None;
            return new OracleStep(true, (ushort)(tick + 1), tick, Traversal(false, tick, duration) | completion);
        }
        if (position == 0) return OracleStep.Rest(position);
        var back = (ushort)(position - 1);
        var boundary = position == duration ? FrameFlags.CompletedBefore : FrameFlags.None;
        return new OracleStep(true, back, back, Traversal(true, back, duration) | boundary);
    }

    static FrameFlags Traversal(bool reverse, int tick, int duration)
    {
        var flags = FrameFlags.None;
        if (reverse) flags |= FrameFlags.Reverse;
        if (tick == 0) flags |= FrameFlags.TimelineStart;
        if (tick == duration - 1) flags |= FrameFlags.TimelineEnd;
        return flags;
    }

    internal static FrameFlags InvertDirection(FrameFlags flags)
    {
        var swapped = flags & ~(FrameFlags.CompletedAfter | FrameFlags.CompletedBefore);
        if ((flags & FrameFlags.CompletedAfter) != 0) swapped |= FrameFlags.CompletedBefore;
        if ((flags & FrameFlags.CompletedBefore) != 0) swapped |= FrameFlags.CompletedAfter;
        return swapped ^ FrameFlags.Reverse;
    }
}
