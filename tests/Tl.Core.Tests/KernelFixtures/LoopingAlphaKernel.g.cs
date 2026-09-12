using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_03fc503e1a7a2bd3898e24d2fa4547b448bde05b102a28edf7a0c51ff71ebe8b
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0xD32B7A1A3E50FC03, 0xB44745FAD2248E89, 0xED282A105BE0BD48, 0x8BBE1EF71FC5A0F7, &Tick);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe void Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
    {
        if (delta == 0 || rowCount == 0) return;
        if (rowCount == 1)
        {
            var row = rows;
            var state = new TimelineState(1, row->Position, row->Cycle);
            if (delta == 1 || delta == -1)
            {
                var reverse = delta < 0;
                if (TimelineMovement.Select(state, 2u, true, reverse, out var next, out var tick, out var cycle, out var flags))
                {
                    Execute(reverse, tick, reverse ? gameTick - 1u : gameTick, cycle, flags, 0, asset, heads, columns);
                    row->Position = next.Position;
                    row->Cycle = next.Cycle;
                }
                return;
            }
            var isReverse = delta < 0;
            var remaining = isReverse ? -(long)delta : delta;
            while (remaining-- != 0)
            {
                if (isReverse) gameTick--;
                if (!TimelineMovement.Select(state, 2u, true, isReverse, out var next, out var tick, out var cycle, out var flags)) break;
                Execute(isReverse, tick, gameTick, cycle, flags, 0, asset, heads, columns);
                state = new TimelineState(1, next.Position, next.Cycle);
                row->Position = next.Position;
                row->Cycle = next.Cycle;
                if (!isReverse) gameTick++;
            }
            return;
        }
        var multiReverse = delta < 0;
        var multiRemaining = multiReverse ? -(long)delta : delta;
        var moved = true;
        while (moved && multiRemaining-- != 0)
        {
            if (multiReverse) gameTick--;
            moved = false;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 2u, true, multiReverse, out _, out var tick, out var cycle, out var flags))
                {
                    moved = true;
                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
                }
            }
            if (!moved) break;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 2u, true, multiReverse, out var next, out _, out _, out _))
                {
                    component->Position = next.Position;
                    component->Cycle = next.Cycle;
                }
            }
            if (!multiReverse) gameTick++;
        }
    }

    static unsafe void Execute(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns)
    {
        TimelineKernels.Chain(heads[0], reverse, asset + 96u, gameTick, tick, cycle, flags, columns, row);
    }
}