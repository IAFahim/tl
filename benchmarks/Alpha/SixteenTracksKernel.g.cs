using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_9c774c722525c151e597dbf054df0d0c4b4cf70fa1972cc2a79f86ed14f14704
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0x51C12525724C779C, 0x0C0DDF54F0DB97E5, 0xC22C97A10FF74C4B, 0x0447F114ED869FA7, &Tick);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe bool Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
    {
        if (delta == 0 || rowCount == 0) return true;
        if (rowCount == 1)
        {
            if (*(nint*)rows != (nint)asset) return false;
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
                return true;
            }
            var isReverse = delta < 0;
            var remaining = isReverse ? -(long)delta : delta;
            while (remaining-- != 0)
            {
                if (isReverse) gameTick--;
                if (!TimelineMovement.Select(state, 64u, true, isReverse, out var multiNext, out var multiTick, out var multiCycle, out var multiFlags)) break;
                Execute(isReverse, multiTick, gameTick, multiCycle, multiFlags, 0, asset, heads, columns);
                state = new TimelineState(1, multiNext.Position, multiNext.Cycle);
                row->Position = multiNext.Position;
                row->Cycle = multiNext.Cycle;
                if (!isReverse) gameTick++;
            }
            return true;
        }
        var probe = Probe(asset, rows, rowCount, out var uniformPosition, out var uniformCycle);
        if (probe == 0) return false;
        if (probe == 1) return TickUniform(asset, heads, columns, rows, rowCount, gameTick, delta, uniformPosition, uniformCycle);
        return TickMixed(asset, heads, columns, rows, rowCount, gameTick, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe int Probe(byte* asset, TimelineComponent* rows, int rowCount, out uint uniformPosition, out long uniformCycle)
    {
        var assetAddress = (nint)asset;
        uniformPosition = 0; uniformCycle = 0;
        if (*(nint*)rows != assetAddress) return 0;
        uniformPosition = rows->Position;
        uniformCycle = rows->Cycle;
        var uniformPositionWord = *(long*)&rows->Position & 4294967295L;
        var uniform = true;
        var scan = 1;
        while (scan < rowCount)
        {
            var component = rows + scan;
            if (*(nint*)component != assetAddress) return 0;
            if (uniform && (*(long*)&component->Position & 4294967295L) != uniformPositionWord | component->Cycle != uniformCycle) uniform = false;
            scan++;
            if (scan == rowCount) break;
            component = rows + scan;
            if (*(nint*)component != assetAddress) return 0;
            if (uniform && (*(long*)&component->Position & 4294967295L) != uniformPositionWord | component->Cycle != uniformCycle) uniform = false;
            scan++;
        }
        return uniform ? 1 : 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe bool TickUniform(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta, uint uniformPosition, long uniformCycle)
    {
        var multiReverse = delta < 0;
        var multiRemaining = multiReverse ? -(long)delta : delta;
        var moved = true;
        while (moved && multiRemaining-- != 0)
        {
            if (multiReverse) gameTick--;
            moved = false;
            if (TimelineMovement.Select(new TimelineState(1, uniformPosition, uniformCycle), 64u, true, multiReverse, out var uniformNext, out var uniformTick, out var uniformOutCycle, out var uniformFlags))
                {
                    moved = true;
                    Run(multiReverse, uniformTick, gameTick, uniformOutCycle, uniformFlags, 0, rowCount, asset, heads, columns);
                    var commit = 0;
                    while (commit < rowCount)
                    {
                        var component = rows + commit;
                        component->Position = uniformNext.Position;
                        component->Cycle = uniformNext.Cycle;
                        commit++;
                        if (commit == rowCount) break;
                        component = rows + commit;
                        component->Position = uniformNext.Position;
                        component->Cycle = uniformNext.Cycle;
                        commit++;
                    }
                    uniformPosition = uniformNext.Position;
                    uniformCycle = uniformNext.Cycle;
                }
                if (!moved) break;
                if (!multiReverse) gameTick++;
            }
            return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe bool TickMixed(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
    {
        var multiReverse = delta < 0;
        var multiRemaining = multiReverse ? -(long)delta : delta;
        var moved = true;
        while (moved && multiRemaining-- != 0)
        {
            if (multiReverse) gameTick--;
            moved = false;
            var memoReady = false;
            var memoPosition = 0u;
            var memoCycle = 0L;
            var memoSelected = false;
            var memoTick = 0u;
            var memoOutCycle = 0L;
            var memoFlags = FrameFlags.None;
            var memoNextPosition = 0u;
            var memoNextCycle = 0L;
            var runOpen = false;
            var runStart = 0;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (memoReady && component->Position == memoPosition && component->Cycle == memoCycle)
                {
                    if (memoSelected)
                    {
                        component->Position = memoNextPosition;
                        component->Cycle = memoNextCycle;
                    }
                    continue;
                }
                if (runOpen)
                {
                    Run(multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, row - runStart, asset, heads, columns);
                    runOpen = false;
                }
                memoReady = true;
                memoPosition = component->Position;
                memoCycle = component->Cycle;
                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), 64u, true, multiReverse, out var memoNext, out memoTick, out memoOutCycle, out memoFlags);
                memoNextPosition = memoNext.Position;
                memoNextCycle = memoNext.Cycle;
                if (!memoSelected) continue;
                moved = true;
                component->Position = memoNextPosition;
                component->Cycle = memoNextCycle;
                runStart = row;
                runOpen = true;
            }
            if (runOpen) Run(multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, rowCount - runStart, asset, heads, columns);
            if (!multiReverse) gameTick++;
        }
        return true;
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

    static unsafe void Run(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int rowStart, int rowCount, byte* asset, int* heads, void** columns)
    {
        for (var row = rowStart; row < rowStart + rowCount; row++)
            Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
    }
}