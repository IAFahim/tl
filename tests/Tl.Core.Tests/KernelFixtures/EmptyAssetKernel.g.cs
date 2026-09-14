using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_f02e36d5e9a325eabe0a55d9ce9ec61dbb7613c050ca2dfc81d2257694db01b6
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0xEA25A3E9D5362EF0, 0x1DC69ECED9550ABE, 0xFC2DCA50C01376BB, 0xB601DB947625D281, &Tick);

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
                if (TimelineMovement.Select(state, 0u, false, reverse, out var next, out var tick, out var cycle, out var flags))
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
                if (!TimelineMovement.Select(state, 0u, false, isReverse, out var multiNext, out var multiTick, out var multiCycle, out var multiFlags)) break;
                Execute(isReverse, multiTick, gameTick, multiCycle, multiFlags, 0, asset, heads, columns);
                state = new TimelineState(1, multiNext.Position, multiNext.Cycle);
                row->Position = multiNext.Position;
                row->Cycle = multiNext.Cycle;
                if (!isReverse) gameTick++;
            }
            return true;
        }
        var probe = Probe(asset, rows, rowCount, out _, out _, out _);
        if (probe == 0) return false;
        return TickScalar(asset, heads, columns, rows, rowCount, gameTick, delta);
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
    static unsafe bool TickScalar(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
    {
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
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 0u, false, multiReverse, out _, out var tick, out var cycle, out var flags))
                {
                    moved = true;
                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
                }
            }
            if (!moved) break;
            for (var row = 0; row < rowCount; row++)
            {
                var component = rows + row;
                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), 0u, false, multiReverse, out var next, out _, out _, out _))
                {
                    component->Position = next.Position;
                    component->Cycle = next.Cycle;
                }
            }
            if (!multiReverse) gameTick++;
        }
        return true;
    }

    static unsafe void Execute(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns)
    {
    }
}