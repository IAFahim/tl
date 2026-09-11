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

    internal static BenchmarkReceipt Capture(ReadOnlySpan<ShapeCatalog.State> states, ReadOnlySpan<Accumulator> accumulators)
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

    internal static BenchmarkReceipt Capture(ReadOnlySpan<MixedShapeCatalog.State> states, ReadOnlySpan<Accumulator> accumulators)
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

internal static class DirectShapes
{
    private const uint Duration = 64u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Tick(
        TimelineShape shape,
        uint gameTick,
        int delta,
        ref ReferenceState state,
        ref Accumulator accumulator)
    {
        switch (shape)
        {
            case TimelineShape.OneTrack:
                TickOneTrack(gameTick, delta, ref state, ref accumulator);
                return;
            case TimelineShape.ThreeTracks:
                Direct.Tick(gameTick, delta, ref state, ref accumulator);
                return;
            case TimelineShape.SixteenTracks:
                TickSixteenTracks(gameTick, delta, ref state, ref accumulator);
                return;
            case TimelineShape.Gap:
                TickGap(gameTick, delta, ref state, ref accumulator);
                return;
            case TimelineShape.Blend:
                TickBlend(gameTick, delta, ref state, ref accumulator);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TickOneTrack(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
    {
        Move(gameTick, delta, ref state, out _, out var operationGameTick, out var direction);
        Kernels.Alpha(direction, 1, 1, operationGameTick, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TickSixteenTracks(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
    {
        Move(gameTick, delta, ref state, out _, out var operationGameTick, out var direction);
        if (direction > 0)
        {
            Kernels.Alpha(1, 1, 1, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 2, 2, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 3, 3, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 4, 4, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 5, 5, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 6, 6, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 7, 7, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 8, 8, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 9, 9, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 10, 10, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 11, 11, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 12, 12, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 13, 13, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 14, 14, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 15, 15, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 16, 16, operationGameTick, ref accumulator);
            return;
        }
        Kernels.Alpha(-1, 16, 16, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 15, 15, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 14, 14, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 13, 13, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 12, 12, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 11, 11, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 10, 10, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 9, 9, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 8, 8, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 7, 7, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 6, 6, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 5, 5, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 4, 4, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 3, 3, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 2, 2, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 1, 1, operationGameTick, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TickGap(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
    {
        Move(gameTick, delta, ref state, out var tick, out var operationGameTick, out var direction);
        if (tick < 16u)
            Kernels.Alpha(direction, 4, 1, operationGameTick, ref accumulator);
        else if (tick >= 32u)
            Kernels.Alpha(direction, 4, 3, operationGameTick, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TickBlend(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
    {
        Move(gameTick, delta, ref state, out var tick, out var operationGameTick, out var direction);
        var factor = tick / 63f;
        var amount = (int)(1 + 8 * factor);
        Kernels.Alpha(direction, 5, amount, operationGameTick, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TickComponent(
        uint gameTick,
        int delta,
        in FirstInput first,
        in SecondInput second,
        in ThirdInput third,
        ref ReferenceState state,
        ref Accumulator accumulator)
    {
        Move(gameTick, delta, ref state, out _, out var operationGameTick, out var direction);
        Kernels.Component(direction, 1, 1, operationGameTick, first.Value, second.Value, third.Value, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Move(
        uint gameTick,
        int delta,
        ref ReferenceState state,
        out uint tick,
        out uint operationGameTick,
        out int direction)
    {
        if (delta == -1)
        {
            direction = -1;
            operationGameTick = unchecked(gameTick - 1u);
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
            return;
        }

        if (delta != 1)
            throw new ArgumentOutOfRangeException(nameof(delta));

        direction = 1;
        operationGameTick = gameTick;
        tick = state.Position;
        if (tick == Duration - 1u)
        {
            state.Position = 0u;
            state.Cycle = unchecked(state.Cycle + 1L);
        }
        else
        {
            state.Position = tick + 1u;
        }
    }
}
