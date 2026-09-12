using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_697d1806343a75b35ab9edea6c84a83cfcbc87bad6e328c292343be147d2f075
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0xB3753A3406187D69, 0x3CA8846CEAEDB95A, 0xC228E3D6BA87BCFC, 0x75F0D247E13B3492, &Tick);

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
                if (TimelineMovement.Select(state, 1u, false, reverse, out var next, out var tick, out var cycle, out var flags))
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
                if (!TimelineMovement.Select(state, 1u, false, isReverse, out var next, out var tick, out var cycle, out var flags)) break;
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
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 1u, false, multiReverse, out _, out var tick, out var cycle, out var flags))
                {
                    moved = true;
                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
                }
            }
            if (!moved) break;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 1u, false, multiReverse, out var next, out _, out _, out _))
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
        int* scratch = stackalloc int[64];
        if (!reverse)
        {
            TimelineKernels.Chain(heads[1], reverse, scratch, asset + 128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[1], reverse, scratch, asset + 192u, gameTick, tick, cycle, flags, columns, row);
        }
        else
        {
            TimelineKernels.Chain(heads[1], reverse, scratch, asset + 192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[1], reverse, scratch, asset + 128u, gameTick, tick, cycle, flags, columns, row);
        }
    }
}