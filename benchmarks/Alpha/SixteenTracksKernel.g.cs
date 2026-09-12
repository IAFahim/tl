using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_9c774c722525c151e597dbf054df0d0c4b4cf70fa1972cc2a79f86ed14f14704
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0x51C12525724C779C, 0x0C0DDF54F0DB97E5, 0xC22C97A10FF74C4B, 0x0447F114ED869FA7, &Tick);

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
                if (TimelineMovement.Select(state, 64u, true, reverse, out var next, out var tick, out var cycle, out var flags))
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
                if (!TimelineMovement.Select(state, 64u, true, isReverse, out var next, out var tick, out var cycle, out var flags)) break;
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
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 64u, true, multiReverse, out _, out var tick, out var cycle, out var flags))
                {
                    moved = true;
                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
                }
            }
            if (!moved) break;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 64u, true, multiReverse, out var next, out _, out _, out _))
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
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 208u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 240u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 272u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 656u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 688u, gameTick, tick, cycle, flags, columns, row);
        }
        else
        {
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 688u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 656u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 272u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 240u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 208u, gameTick, tick, cycle, flags, columns, row);
        }
    }
}