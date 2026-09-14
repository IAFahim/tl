using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_1e6b61e62f517dea2c81a0ce2a16845186d4abac579c4e0dd94123a2e1c8ef7a
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0xEA7D512FE6616B1E, 0x5184162ACEA0812C, 0x0D4E9C57ACABD486, 0x7AEFC8E1A22341D9, &Tick);

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
                if (TimelineMovement.Select(state, 8u, false, reverse, out var next, out var tick, out var cycle, out var flags))
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
                if (!TimelineMovement.Select(state, 8u, false, isReverse, out var multiNext, out var multiTick, out var multiCycle, out var multiFlags)) break;
                Execute(isReverse, multiTick, gameTick, multiCycle, multiFlags, 0, asset, heads, columns);
                state = new TimelineState(1, multiNext.Position, multiNext.Cycle);
                row->Position = multiNext.Position;
                row->Cycle = multiNext.Cycle;
                if (!isReverse) gameTick++;
            }
            return true;
        }
        var probe = Probe(asset, rows, rowCount, out var uniformPosition, out var uniformCycle, out var uniformPositionWord);
        if (probe == 0) return false;
        if (probe == 1) return TickUniform(asset, heads, columns, rows, rowCount, gameTick, delta, uniformPosition, uniformCycle, uniformPositionWord);
        return TickMixed(asset, heads, columns, rows, rowCount, gameTick, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe int Probe(byte* asset, TimelineComponent* rows, int rowCount, out uint uniformPosition, out long uniformCycle, out long uniformPositionWord)
    {
        var assetAddress = (nint)asset;
        if (*(nint*)rows != assetAddress) { uniformPosition = 0; uniformCycle = 0; uniformPositionWord = 0; return 0; }
        uniformPositionWord = *(long*)&rows->Position;
        uniformPosition = rows->Position;
        uniformCycle = rows->Cycle;
        var uniform = true;
        var scan = 1;
        while (scan < rowCount)
        {
            var component = rows + scan;
            if (*(nint*)component != assetAddress) return 0;
            if (uniform && (*(long*)&component->Position != uniformPositionWord | component->Cycle != uniformCycle)) uniform = false;
            scan++;
            if (scan == rowCount) break;
            component = rows + scan;
            if (*(nint*)component != assetAddress) return 0;
            if (uniform && (*(long*)&component->Position != uniformPositionWord | component->Cycle != uniformCycle)) uniform = false;
            scan++;
        }
        return uniform ? 1 : 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe bool TickUniform(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta, uint uniformPosition, long uniformCycle, long uniformPositionWord)
    {
        var multiReverse = delta < 0;
        var multiRemaining = multiReverse ? -(long)delta : delta;
        var moved = true;
        while (moved && multiRemaining-- != 0)
        {
            if (multiReverse) gameTick--;
            moved = false;
            if (TimelineMovement.Select(new TimelineState(1, uniformPosition, uniformCycle), 8u, false, multiReverse, out var uniformNext, out var uniformTick, out var uniformOutCycle, out var uniformFlags))
                {
                    moved = true;
                    Run(multiReverse, uniformTick, gameTick, uniformOutCycle, uniformFlags, 0, rowCount, asset, heads, columns);
                    var uniformNextWord = (uniformPositionWord & -4294967296L) | uniformNext.Position;
                    var uniformNextCycle = uniformNext.Cycle;
                    var commit = 0;
                    while (commit < rowCount)
                    {
                        var component = rows + commit;
                        *(long*)&component->Position = uniformNextWord;
                        component->Cycle = uniformNextCycle;
                        commit++;
                        if (commit == rowCount) break;
                        component = rows + commit;
                        *(long*)&component->Position = uniformNextWord;
                        component->Cycle = uniformNextCycle;
                        commit++;
                    }
                    uniformPositionWord = uniformNextWord;
                    uniformPosition = uniformNext.Position;
                    uniformCycle = uniformNextCycle;
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
                if (memoReady && component->Position == memoPosition && component->Cycle == memoCycle) continue;
                if (runOpen)
                {
                    Run(multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, row - runStart, asset, heads, columns);
                    runOpen = false;
                }
                memoReady = true;
                memoPosition = component->Position;
                memoCycle = component->Cycle;
                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), 8u, false, multiReverse, out var memoNext, out memoTick, out memoOutCycle, out memoFlags);
                memoNextPosition = memoNext.Position;
                memoNextCycle = memoNext.Cycle;
                if (!memoSelected) continue;
                moved = true;
                runStart = row;
                runOpen = true;
            }
            if (runOpen) Run(multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, rowCount - runStart, asset, heads, columns);
            if (!moved) break;
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
                memoReady = true;
                memoPosition = component->Position;
                memoCycle = component->Cycle;
                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), 8u, false, multiReverse, out var commitNext, out _, out _, out _);
                memoNextPosition = commitNext.Position;
                memoNextCycle = commitNext.Cycle;
                if (memoSelected)
                {
                    component->Position = memoNextPosition;
                    component->Cycle = memoNextCycle;
                }
            }
            if (!multiReverse) gameTick++;
        }
        return true;
    }

    static unsafe void Execute(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns)
    {
        int* scratch = stackalloc int[64];
        if (tick < 1u)
        {
            if (!reverse)
            {
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 304u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 336u, gameTick, tick, cycle, flags, columns, row);
            }
            else
            {
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 336u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 304u, gameTick, tick, cycle, flags, columns, row);
            }
        }
        else if (tick < 3u)
        {
            if (!reverse)
            {
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 368u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 400u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 432u, gameTick, tick, cycle, flags, columns, row);
            }
            else
            {
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 432u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 400u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 368u, gameTick, tick, cycle, flags, columns, row);
            }
        }
        else if (tick < 4u)
        {
            if (!reverse)
            {
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 464u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 496u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 528u, gameTick, tick, cycle, flags, columns, row);
            }
            else
            {
                TimelineKernels.Chain(heads[2], reverse, scratch, asset + 528u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 496u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 464u, gameTick, tick, cycle, flags, columns, row);
            }
        }
        else if (tick < 6u)
        {
            if (!reverse)
            {
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 560u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 592u, gameTick, tick, cycle, flags, columns, row);
            }
            else
            {
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 592u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 560u, gameTick, tick, cycle, flags, columns, row);
            }
        }
        else if (tick < 7u)
        {
            if (!reverse)
            {
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 624u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 656u, gameTick, tick, cycle, flags, columns, row);
            }
            else
            {
                TimelineKernels.Chain(heads[0], reverse, scratch, asset + 656u, gameTick, tick, cycle, flags, columns, row);
                TimelineKernels.Chain(heads[1], reverse, scratch, asset + 624u, gameTick, tick, cycle, flags, columns, row);
            }
        }
        else if (tick < 8u)
        {
            TimelineKernels.Chain(heads[0], reverse, scratch, asset + 688u, gameTick, tick, cycle, flags, columns, row);
        }
    }

    static unsafe void Run(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int rowStart, int rowCount, byte* asset, int* heads, void** columns)
    {
        int* scratch = stackalloc int[64];
        if (tick < 1u)
        {
            for (var row = rowStart; row < rowStart + rowCount; row++)
                Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
        }
        else if (tick < 3u)
        {
            for (var row = rowStart; row < rowStart + rowCount; row++)
                Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
        }
        else if (tick < 4u)
        {
            for (var row = rowStart; row < rowStart + rowCount; row++)
                Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
        }
        else if (tick < 6u)
        {
            for (var row = rowStart; row < rowStart + rowCount; row++)
                Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
        }
        else if (tick < 7u)
        {
            for (var row = rowStart; row < rowStart + rowCount; row++)
                Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
        }
        else if (tick < 8u)
        {
            TimelineKernels.ChainRange(heads[0], reverse, scratch, asset + 688u, gameTick, tick, cycle, flags, columns, rowStart, rowCount);
        }
    }
}