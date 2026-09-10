using System.Runtime.CompilerServices;

internal struct ReferenceState
{
    internal uint Position;
    internal long Cycle;
}

internal static class Direct
{
    private const uint Duration = 64u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Tick(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
    {
        if (delta < 0)
        {
            gameTick = unchecked(gameTick - 1u);
            uint tick;
            if (state.Position == 0u)
            {
                tick = Duration - 1u;
                state.Cycle = unchecked(state.Cycle - 1L);
            }
            else
            {
                tick = state.Position - 1u;
            }
            state.Position = tick;
            Kernels.Alpha(-1, 3, 3, gameTick, ref accumulator);
            Kernels.Beta(-1, 2, 2, gameTick, ref accumulator);
            Kernels.Alpha(-1, 1, 1, gameTick, ref accumulator);
            return;
        }

        var forwardTick = state.Position;
        Kernels.Alpha(1, 1, 1, gameTick, ref accumulator);
        Kernels.Beta(1, 2, 2, gameTick, ref accumulator);
        Kernels.Alpha(1, 3, 3, gameTick, ref accumulator);
        if (forwardTick == Duration - 1u)
        {
            state.Position = 0u;
            state.Cycle = unchecked(state.Cycle + 1L);
        }
        else
        {
            state.Position = forwardTick + 1u;
        }
    }

    internal static BenchmarkReceipt Capture(ReadOnlySpan<ReferenceState> states, ReadOnlySpan<Accumulator> accumulators)
    {
        long stateHash = 0;
        long valueHash = 0;
        long orderHash = 0;
        long gameTickHash = 0;
        long calls = 0;
        for (var row = 0; row < states.Length; row++)
        {
            stateHash = unchecked((stateHash * 397 + states[row].Position) * 397 + states[row].Cycle);
            valueHash = unchecked(valueHash * 397 + accumulators[row].Value);
            orderHash = unchecked(orderHash * 397 + accumulators[row].Order);
            gameTickHash = unchecked(gameTickHash * 397 + accumulators[row].GameTickSum);
            calls += accumulators[row].Calls;
        }
        return new(stateHash, valueHash, orderHash, gameTickHash, calls);
    }

    internal static BenchmarkReceipt Capture(ReadOnlySpan<BenchmarkCatalog.State> states, ReadOnlySpan<Accumulator> accumulators)
    {
        long stateHash = 0;
        long valueHash = 0;
        long orderHash = 0;
        long gameTickHash = 0;
        long calls = 0;
        for (var row = 0; row < states.Length; row++)
        {
            stateHash = unchecked((stateHash * 397 + states[row].Position) * 397 + states[row].Cycle);
            valueHash = unchecked(valueHash * 397 + accumulators[row].Value);
            orderHash = unchecked(orderHash * 397 + accumulators[row].Order);
            gameTickHash = unchecked(gameTickHash * 397 + accumulators[row].GameTickSum);
            calls += accumulators[row].Calls;
        }
        return new(stateHash, valueHash, orderHash, gameTickHash, calls);
    }
}
