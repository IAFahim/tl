using System.Runtime.CompilerServices;
using Tl;

internal static unsafe class TimelineKernel_c18e63530589e30b2e9f1bc1f10269926c66b60202c04f86de8a960522ad23e0
{
    [ModuleInitializer]
    internal static void Install() =>
        TimelineKernels.Register(0x0BE3890553638EC1, 0x926902F1C11B9F2E, 0x864FC00202B6666C, 0xE023AD2205968ADE, &Tick);

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
            if (TimelineMovement.Select(new TimelineState(1, uniformPosition, uniformCycle), 64u, true, multiReverse, out var uniformNext, out var uniformTick, out var uniformOutCycle, out var uniformFlags))
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
                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), 64u, true, multiReverse, out var memoNext, out memoTick, out memoOutCycle, out memoFlags);
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
                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), 64u, true, multiReverse, out var commitNext, out _, out _, out _);
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
        if (!reverse)
        {
            F0_0(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_1(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_2(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_3(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_4(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_5(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_6(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_7(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_8(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_9(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_10(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_11(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_12(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_13(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_14(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            F0_15(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
        }
        else
        {
            R0_15(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_14(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_13(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_12(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_11(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_10(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_9(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_8(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_7(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_6(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_5(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_4(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_3(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_2(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_1(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
            R0_0(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);
        }
    }

    static unsafe void Run(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int rowStart, int rowCount, byte* asset, int* heads, void** columns)
    {
        for (var row = rowStart; row < rowStart + rowCount; row++)
            Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);
    }

    static unsafe void F0_0(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2224u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2288u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2320u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2352u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2384u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2416u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2448u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2480u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2512u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2544u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2576u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2608u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_0(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2608u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2576u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2544u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2512u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2480u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2448u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2416u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2384u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2352u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2320u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2288u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2224u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2128u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_1(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2640u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2672u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2704u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2736u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2768u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2800u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2832u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2864u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2896u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2928u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2960u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 2992u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3024u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3056u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3088u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3120u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_1(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3120u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3088u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3056u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3024u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2992u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2960u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2928u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2896u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2864u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2832u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2800u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2768u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2736u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2704u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2672u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 2640u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_2(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3152u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3184u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3216u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3248u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3280u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3312u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3344u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3376u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3408u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3440u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3472u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3504u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3536u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3568u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3600u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3632u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_2(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3632u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3600u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3568u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3536u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3504u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3472u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3440u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3408u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3376u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3344u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3312u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3280u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3248u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3216u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3184u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3152u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_3(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3664u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3696u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3728u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3760u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3792u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3824u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3856u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3888u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3920u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3952u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 3984u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4016u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4048u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4080u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4112u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4144u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_3(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4144u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4112u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4080u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4048u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4016u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3984u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3952u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3920u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3888u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3856u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3824u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3792u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3760u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3728u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3696u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 3664u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_4(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4176u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4208u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4240u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4272u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4656u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_4(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4656u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4272u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4240u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4208u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4176u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_5(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4688u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4720u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4752u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4784u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4816u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4848u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4880u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4912u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4944u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 4976u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5008u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5040u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5072u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5104u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5136u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5168u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_5(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5168u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5136u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5104u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5072u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5040u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5008u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4976u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4944u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4912u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4880u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4848u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4816u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4784u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4752u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4720u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 4688u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_6(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5200u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5232u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5264u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5296u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5328u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5360u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5392u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5424u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5456u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5488u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5520u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5552u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5584u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5616u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5648u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5680u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_6(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5680u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5648u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5616u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5584u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5552u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5520u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5488u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5456u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5424u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5392u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5360u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5328u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5296u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5264u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5232u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5200u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_7(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5712u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5744u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5776u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5808u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5840u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5872u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5904u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5936u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 5968u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6000u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6032u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6064u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6096u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6192u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_7(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6096u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6064u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6032u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6000u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5968u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5936u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5904u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5872u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5840u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5808u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5776u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5744u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 5712u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_8(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6224u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6288u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6320u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6352u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6384u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6416u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6448u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6480u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6512u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6544u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6576u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6608u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6640u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6672u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6704u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_8(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6704u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6672u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6640u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6608u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6576u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6544u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6512u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6480u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6448u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6416u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6384u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6352u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6320u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6288u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6224u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_9(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6736u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6768u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6800u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6832u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6864u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6896u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6928u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6960u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 6992u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7024u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7056u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7088u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7120u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7152u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7184u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7216u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_9(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7216u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7184u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7152u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7120u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7088u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7056u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7024u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6992u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6960u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6928u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6896u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6864u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6832u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6800u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6768u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 6736u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_10(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7248u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7280u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7312u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7344u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7376u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7408u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7440u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7472u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7504u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7536u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7568u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7600u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7632u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7664u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7696u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7728u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_10(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7728u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7696u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7664u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7632u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7600u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7568u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7536u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7504u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7472u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7440u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7408u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7376u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7344u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7312u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7280u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7248u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_11(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7760u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7792u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7824u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7856u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7888u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7920u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7952u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 7984u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8016u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8048u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8080u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8112u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8144u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8176u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8208u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8240u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_11(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8240u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8208u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8176u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8144u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8112u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8080u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8048u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8016u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7984u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7952u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7920u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7888u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7856u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7824u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7792u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 7760u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_12(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8272u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8656u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8688u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8720u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8752u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_12(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8752u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8720u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8688u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8656u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8624u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8592u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8560u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8528u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8496u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8464u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8432u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8400u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8368u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8336u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8304u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8272u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_13(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8784u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8816u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8848u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8880u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8912u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8944u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 8976u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9008u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9040u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9072u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9104u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9136u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9168u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9200u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9232u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9264u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_13(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9264u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9232u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9200u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9168u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9136u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9104u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9072u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9040u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9008u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8976u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8944u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8912u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8880u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8848u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8816u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 8784u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_14(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9296u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9328u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9360u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9392u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9424u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9456u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9488u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9520u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9552u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9584u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9616u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9648u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9680u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9712u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9744u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9776u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_14(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9776u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9744u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9712u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9680u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9648u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9616u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9584u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9552u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9520u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9488u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9456u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9424u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9392u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9360u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9328u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9296u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void F0_15(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9808u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9840u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9872u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9904u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9936u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 9968u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10000u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10032u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10064u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10096u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10224u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], false, scratch, asset + 10288u, gameTick, tick, cycle, flags, columns, row);
    }
    static unsafe void R0_15(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)
    {
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10288u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10256u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10224u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10192u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10160u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10128u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10096u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10064u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10032u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 10000u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9968u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9936u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9904u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9872u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9840u, gameTick, tick, cycle, flags, columns, row);
            TimelineKernels.Chain(heads[0], true, scratch, asset + 9808u, gameTick, tick, cycle, flags, columns, row);
    }}