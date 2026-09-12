using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_3340e1e12aedecf95f213403bae35a7dd720d7aa69b67c0ddd9867cd02f5c60b
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0xF9ECED2AE1E14033, 0x7D5AE3BA0334215F, 0x0D7CB669AAD720D7, 0x0BC6F502CD6798DD, &Tick);

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
                if (TimelineMovement.Select(state, 3u, false, reverse, out var next, out var tick, out var cycle, out var flags))
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
                if (!TimelineMovement.Select(state, 3u, false, isReverse, out var next, out var tick, out var cycle, out var flags)) break;
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
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 3u, false, multiReverse, out _, out var tick, out var cycle, out var flags))
                {
                    moved = true;
                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
                }
            }
            if (!moved) break;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 3u, false, multiReverse, out var next, out _, out _, out _))
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
        if (tick < 1u)
        {
            TimelineKernels.Chain(heads[0], reverse, asset + 128u, gameTick, tick, cycle, flags, columns, row);
        }
        else if (tick < 2u)
        {
        }
        else if (tick < 3u)
        {
            TimelineKernels.Chain(heads[0], reverse, asset + 160u, gameTick, tick, cycle, flags, columns, row);
        }
    }
}