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
    internal static void TickForward(uint gameTick, ref ReferenceState state, ref Accumulator accumulator)
    {
        var tick = state.Position;
        Kernels.Alpha(1, 1, 1, gameTick, ref accumulator);
        Kernels.Beta(1, 2, 2, gameTick, ref accumulator);
        Kernels.Alpha(1, 3, 3, gameTick, ref accumulator);
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

    internal static BenchmarkReceipt Capture(ReadOnlySpan<Tl.TimelineComponent> states, ReadOnlySpan<Accumulator> accumulators)
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
    internal static void TickForward(
        TimelineShape shape,
        uint gameTick,
        ref ReferenceState state,
        ref Accumulator accumulator)
    {
        switch (shape)
        {
            case TimelineShape.OneTrack:
                TickOneTrackForward(gameTick, ref state, ref accumulator);
                return;
            case TimelineShape.ThreeTracks:
                Direct.TickForward(gameTick, ref state, ref accumulator);
                return;
            case TimelineShape.SixteenTracks:
                TickSixteenTracksForward(gameTick, ref state, ref accumulator);
                return;
            case TimelineShape.TwoHundredFiftySixTracks:
                TickTwoHundredFiftySixTracksForward(gameTick, ref state, ref accumulator);
                return;
            case TimelineShape.Gap:
                TickGapForward(gameTick, ref state, ref accumulator);
                return;
            case TimelineShape.Blend:
                TickBlendForward(gameTick, ref state, ref accumulator);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }

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
            case TimelineShape.TwoHundredFiftySixTracks:
                TickTwoHundredFiftySixTracks(gameTick, delta, ref state, ref accumulator);
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
    internal static void TickOneTrackForward(uint gameTick, ref ReferenceState state, ref Accumulator accumulator)
    {
        MoveForward(ref state, out _);
        Kernels.Alpha(1, 1, 1, gameTick, ref accumulator);
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
    internal static void TickSixteenTracksForward(uint gameTick, ref ReferenceState state, ref Accumulator accumulator)
    {
        MoveForward(ref state, out _);
        Kernels.Alpha(1, 1, 1, gameTick, ref accumulator);
        Kernels.Alpha(1, 2, 2, gameTick, ref accumulator);
        Kernels.Alpha(1, 3, 3, gameTick, ref accumulator);
        Kernels.Alpha(1, 4, 4, gameTick, ref accumulator);
        Kernels.Alpha(1, 5, 5, gameTick, ref accumulator);
        Kernels.Alpha(1, 6, 6, gameTick, ref accumulator);
        Kernels.Alpha(1, 7, 7, gameTick, ref accumulator);
        Kernels.Alpha(1, 8, 8, gameTick, ref accumulator);
        Kernels.Alpha(1, 9, 9, gameTick, ref accumulator);
        Kernels.Alpha(1, 10, 10, gameTick, ref accumulator);
        Kernels.Alpha(1, 11, 11, gameTick, ref accumulator);
        Kernels.Alpha(1, 12, 12, gameTick, ref accumulator);
        Kernels.Alpha(1, 13, 13, gameTick, ref accumulator);
        Kernels.Alpha(1, 14, 14, gameTick, ref accumulator);
        Kernels.Alpha(1, 15, 15, gameTick, ref accumulator);
        Kernels.Alpha(1, 16, 16, gameTick, ref accumulator);
    }

    internal static void TickTwoHundredFiftySixTracks(uint gameTick, int delta, ref ReferenceState state, ref Accumulator accumulator)
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
            Kernels.Alpha(1, 17, 17, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 18, 18, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 19, 19, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 20, 20, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 21, 21, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 22, 22, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 23, 23, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 24, 24, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 25, 25, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 26, 26, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 27, 27, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 28, 28, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 29, 29, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 30, 30, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 31, 31, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 32, 32, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 33, 33, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 34, 34, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 35, 35, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 36, 36, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 37, 37, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 38, 38, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 39, 39, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 40, 40, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 41, 41, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 42, 42, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 43, 43, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 44, 44, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 45, 45, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 46, 46, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 47, 47, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 48, 48, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 49, 49, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 50, 50, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 51, 51, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 52, 52, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 53, 53, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 54, 54, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 55, 55, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 56, 56, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 57, 57, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 58, 58, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 59, 59, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 60, 60, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 61, 61, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 62, 62, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 63, 63, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 64, 64, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 65, 65, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 66, 66, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 67, 67, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 68, 68, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 69, 69, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 70, 70, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 71, 71, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 72, 72, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 73, 73, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 74, 74, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 75, 75, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 76, 76, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 77, 77, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 78, 78, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 79, 79, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 80, 80, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 81, 81, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 82, 82, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 83, 83, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 84, 84, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 85, 85, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 86, 86, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 87, 87, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 88, 88, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 89, 89, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 90, 90, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 91, 91, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 92, 92, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 93, 93, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 94, 94, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 95, 95, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 96, 96, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 97, 97, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 98, 98, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 99, 99, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 100, 100, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 101, 101, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 102, 102, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 103, 103, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 104, 104, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 105, 105, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 106, 106, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 107, 107, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 108, 108, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 109, 109, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 110, 110, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 111, 111, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 112, 112, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 113, 113, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 114, 114, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 115, 115, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 116, 116, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 117, 117, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 118, 118, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 119, 119, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 120, 120, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 121, 121, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 122, 122, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 123, 123, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 124, 124, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 125, 125, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 126, 126, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 127, 127, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 128, 128, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 129, 129, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 130, 130, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 131, 131, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 132, 132, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 133, 133, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 134, 134, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 135, 135, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 136, 136, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 137, 137, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 138, 138, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 139, 139, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 140, 140, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 141, 141, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 142, 142, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 143, 143, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 144, 144, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 145, 145, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 146, 146, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 147, 147, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 148, 148, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 149, 149, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 150, 150, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 151, 151, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 152, 152, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 153, 153, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 154, 154, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 155, 155, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 156, 156, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 157, 157, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 158, 158, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 159, 159, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 160, 160, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 161, 161, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 162, 162, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 163, 163, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 164, 164, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 165, 165, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 166, 166, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 167, 167, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 168, 168, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 169, 169, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 170, 170, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 171, 171, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 172, 172, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 173, 173, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 174, 174, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 175, 175, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 176, 176, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 177, 177, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 178, 178, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 179, 179, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 180, 180, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 181, 181, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 182, 182, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 183, 183, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 184, 184, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 185, 185, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 186, 186, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 187, 187, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 188, 188, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 189, 189, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 190, 190, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 191, 191, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 192, 192, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 193, 193, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 194, 194, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 195, 195, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 196, 196, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 197, 197, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 198, 198, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 199, 199, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 200, 200, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 201, 201, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 202, 202, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 203, 203, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 204, 204, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 205, 205, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 206, 206, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 207, 207, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 208, 208, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 209, 209, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 210, 210, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 211, 211, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 212, 212, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 213, 213, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 214, 214, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 215, 215, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 216, 216, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 217, 217, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 218, 218, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 219, 219, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 220, 220, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 221, 221, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 222, 222, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 223, 223, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 224, 224, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 225, 225, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 226, 226, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 227, 227, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 228, 228, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 229, 229, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 230, 230, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 231, 231, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 232, 232, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 233, 233, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 234, 234, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 235, 235, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 236, 236, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 237, 237, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 238, 238, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 239, 239, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 240, 240, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 241, 241, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 242, 242, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 243, 243, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 244, 244, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 245, 245, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 246, 246, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 247, 247, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 248, 248, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 249, 249, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 250, 250, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 251, 251, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 252, 252, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 253, 253, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 254, 254, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 255, 255, operationGameTick, ref accumulator);
            Kernels.Alpha(1, 256, 256, operationGameTick, ref accumulator);
            return;
        }
        Kernels.Alpha(-1, 256, 256, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 255, 255, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 254, 254, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 253, 253, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 252, 252, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 251, 251, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 250, 250, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 249, 249, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 248, 248, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 247, 247, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 246, 246, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 245, 245, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 244, 244, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 243, 243, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 242, 242, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 241, 241, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 240, 240, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 239, 239, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 238, 238, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 237, 237, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 236, 236, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 235, 235, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 234, 234, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 233, 233, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 232, 232, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 231, 231, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 230, 230, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 229, 229, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 228, 228, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 227, 227, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 226, 226, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 225, 225, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 224, 224, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 223, 223, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 222, 222, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 221, 221, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 220, 220, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 219, 219, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 218, 218, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 217, 217, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 216, 216, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 215, 215, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 214, 214, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 213, 213, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 212, 212, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 211, 211, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 210, 210, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 209, 209, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 208, 208, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 207, 207, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 206, 206, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 205, 205, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 204, 204, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 203, 203, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 202, 202, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 201, 201, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 200, 200, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 199, 199, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 198, 198, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 197, 197, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 196, 196, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 195, 195, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 194, 194, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 193, 193, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 192, 192, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 191, 191, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 190, 190, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 189, 189, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 188, 188, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 187, 187, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 186, 186, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 185, 185, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 184, 184, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 183, 183, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 182, 182, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 181, 181, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 180, 180, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 179, 179, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 178, 178, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 177, 177, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 176, 176, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 175, 175, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 174, 174, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 173, 173, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 172, 172, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 171, 171, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 170, 170, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 169, 169, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 168, 168, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 167, 167, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 166, 166, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 165, 165, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 164, 164, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 163, 163, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 162, 162, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 161, 161, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 160, 160, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 159, 159, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 158, 158, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 157, 157, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 156, 156, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 155, 155, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 154, 154, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 153, 153, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 152, 152, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 151, 151, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 150, 150, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 149, 149, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 148, 148, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 147, 147, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 146, 146, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 145, 145, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 144, 144, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 143, 143, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 142, 142, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 141, 141, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 140, 140, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 139, 139, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 138, 138, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 137, 137, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 136, 136, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 135, 135, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 134, 134, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 133, 133, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 132, 132, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 131, 131, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 130, 130, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 129, 129, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 128, 128, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 127, 127, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 126, 126, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 125, 125, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 124, 124, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 123, 123, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 122, 122, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 121, 121, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 120, 120, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 119, 119, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 118, 118, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 117, 117, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 116, 116, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 115, 115, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 114, 114, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 113, 113, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 112, 112, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 111, 111, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 110, 110, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 109, 109, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 108, 108, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 107, 107, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 106, 106, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 105, 105, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 104, 104, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 103, 103, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 102, 102, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 101, 101, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 100, 100, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 99, 99, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 98, 98, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 97, 97, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 96, 96, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 95, 95, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 94, 94, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 93, 93, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 92, 92, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 91, 91, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 90, 90, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 89, 89, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 88, 88, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 87, 87, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 86, 86, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 85, 85, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 84, 84, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 83, 83, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 82, 82, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 81, 81, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 80, 80, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 79, 79, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 78, 78, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 77, 77, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 76, 76, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 75, 75, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 74, 74, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 73, 73, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 72, 72, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 71, 71, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 70, 70, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 69, 69, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 68, 68, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 67, 67, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 66, 66, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 65, 65, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 64, 64, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 63, 63, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 62, 62, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 61, 61, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 60, 60, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 59, 59, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 58, 58, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 57, 57, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 56, 56, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 55, 55, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 54, 54, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 53, 53, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 52, 52, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 51, 51, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 50, 50, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 49, 49, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 48, 48, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 47, 47, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 46, 46, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 45, 45, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 44, 44, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 43, 43, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 42, 42, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 41, 41, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 40, 40, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 39, 39, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 38, 38, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 37, 37, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 36, 36, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 35, 35, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 34, 34, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 33, 33, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 32, 32, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 31, 31, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 30, 30, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 29, 29, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 28, 28, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 27, 27, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 26, 26, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 25, 25, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 24, 24, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 23, 23, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 22, 22, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 21, 21, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 20, 20, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 19, 19, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 18, 18, operationGameTick, ref accumulator);
        Kernels.Alpha(-1, 17, 17, operationGameTick, ref accumulator);
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

    internal static void TickTwoHundredFiftySixTracksForward(
        uint gameTick,
        ref ReferenceState state,
        ref Accumulator accumulator)
    {
        MoveForward(ref state, out _);
        Kernels.Alpha(1, 1, 1, gameTick, ref accumulator);
        Kernels.Alpha(1, 2, 2, gameTick, ref accumulator);
        Kernels.Alpha(1, 3, 3, gameTick, ref accumulator);
        Kernels.Alpha(1, 4, 4, gameTick, ref accumulator);
        Kernels.Alpha(1, 5, 5, gameTick, ref accumulator);
        Kernels.Alpha(1, 6, 6, gameTick, ref accumulator);
        Kernels.Alpha(1, 7, 7, gameTick, ref accumulator);
        Kernels.Alpha(1, 8, 8, gameTick, ref accumulator);
        Kernels.Alpha(1, 9, 9, gameTick, ref accumulator);
        Kernels.Alpha(1, 10, 10, gameTick, ref accumulator);
        Kernels.Alpha(1, 11, 11, gameTick, ref accumulator);
        Kernels.Alpha(1, 12, 12, gameTick, ref accumulator);
        Kernels.Alpha(1, 13, 13, gameTick, ref accumulator);
        Kernels.Alpha(1, 14, 14, gameTick, ref accumulator);
        Kernels.Alpha(1, 15, 15, gameTick, ref accumulator);
        Kernels.Alpha(1, 16, 16, gameTick, ref accumulator);
        Kernels.Alpha(1, 17, 17, gameTick, ref accumulator);
        Kernels.Alpha(1, 18, 18, gameTick, ref accumulator);
        Kernels.Alpha(1, 19, 19, gameTick, ref accumulator);
        Kernels.Alpha(1, 20, 20, gameTick, ref accumulator);
        Kernels.Alpha(1, 21, 21, gameTick, ref accumulator);
        Kernels.Alpha(1, 22, 22, gameTick, ref accumulator);
        Kernels.Alpha(1, 23, 23, gameTick, ref accumulator);
        Kernels.Alpha(1, 24, 24, gameTick, ref accumulator);
        Kernels.Alpha(1, 25, 25, gameTick, ref accumulator);
        Kernels.Alpha(1, 26, 26, gameTick, ref accumulator);
        Kernels.Alpha(1, 27, 27, gameTick, ref accumulator);
        Kernels.Alpha(1, 28, 28, gameTick, ref accumulator);
        Kernels.Alpha(1, 29, 29, gameTick, ref accumulator);
        Kernels.Alpha(1, 30, 30, gameTick, ref accumulator);
        Kernels.Alpha(1, 31, 31, gameTick, ref accumulator);
        Kernels.Alpha(1, 32, 32, gameTick, ref accumulator);
        Kernels.Alpha(1, 33, 33, gameTick, ref accumulator);
        Kernels.Alpha(1, 34, 34, gameTick, ref accumulator);
        Kernels.Alpha(1, 35, 35, gameTick, ref accumulator);
        Kernels.Alpha(1, 36, 36, gameTick, ref accumulator);
        Kernels.Alpha(1, 37, 37, gameTick, ref accumulator);
        Kernels.Alpha(1, 38, 38, gameTick, ref accumulator);
        Kernels.Alpha(1, 39, 39, gameTick, ref accumulator);
        Kernels.Alpha(1, 40, 40, gameTick, ref accumulator);
        Kernels.Alpha(1, 41, 41, gameTick, ref accumulator);
        Kernels.Alpha(1, 42, 42, gameTick, ref accumulator);
        Kernels.Alpha(1, 43, 43, gameTick, ref accumulator);
        Kernels.Alpha(1, 44, 44, gameTick, ref accumulator);
        Kernels.Alpha(1, 45, 45, gameTick, ref accumulator);
        Kernels.Alpha(1, 46, 46, gameTick, ref accumulator);
        Kernels.Alpha(1, 47, 47, gameTick, ref accumulator);
        Kernels.Alpha(1, 48, 48, gameTick, ref accumulator);
        Kernels.Alpha(1, 49, 49, gameTick, ref accumulator);
        Kernels.Alpha(1, 50, 50, gameTick, ref accumulator);
        Kernels.Alpha(1, 51, 51, gameTick, ref accumulator);
        Kernels.Alpha(1, 52, 52, gameTick, ref accumulator);
        Kernels.Alpha(1, 53, 53, gameTick, ref accumulator);
        Kernels.Alpha(1, 54, 54, gameTick, ref accumulator);
        Kernels.Alpha(1, 55, 55, gameTick, ref accumulator);
        Kernels.Alpha(1, 56, 56, gameTick, ref accumulator);
        Kernels.Alpha(1, 57, 57, gameTick, ref accumulator);
        Kernels.Alpha(1, 58, 58, gameTick, ref accumulator);
        Kernels.Alpha(1, 59, 59, gameTick, ref accumulator);
        Kernels.Alpha(1, 60, 60, gameTick, ref accumulator);
        Kernels.Alpha(1, 61, 61, gameTick, ref accumulator);
        Kernels.Alpha(1, 62, 62, gameTick, ref accumulator);
        Kernels.Alpha(1, 63, 63, gameTick, ref accumulator);
        Kernels.Alpha(1, 64, 64, gameTick, ref accumulator);
        Kernels.Alpha(1, 65, 65, gameTick, ref accumulator);
        Kernels.Alpha(1, 66, 66, gameTick, ref accumulator);
        Kernels.Alpha(1, 67, 67, gameTick, ref accumulator);
        Kernels.Alpha(1, 68, 68, gameTick, ref accumulator);
        Kernels.Alpha(1, 69, 69, gameTick, ref accumulator);
        Kernels.Alpha(1, 70, 70, gameTick, ref accumulator);
        Kernels.Alpha(1, 71, 71, gameTick, ref accumulator);
        Kernels.Alpha(1, 72, 72, gameTick, ref accumulator);
        Kernels.Alpha(1, 73, 73, gameTick, ref accumulator);
        Kernels.Alpha(1, 74, 74, gameTick, ref accumulator);
        Kernels.Alpha(1, 75, 75, gameTick, ref accumulator);
        Kernels.Alpha(1, 76, 76, gameTick, ref accumulator);
        Kernels.Alpha(1, 77, 77, gameTick, ref accumulator);
        Kernels.Alpha(1, 78, 78, gameTick, ref accumulator);
        Kernels.Alpha(1, 79, 79, gameTick, ref accumulator);
        Kernels.Alpha(1, 80, 80, gameTick, ref accumulator);
        Kernels.Alpha(1, 81, 81, gameTick, ref accumulator);
        Kernels.Alpha(1, 82, 82, gameTick, ref accumulator);
        Kernels.Alpha(1, 83, 83, gameTick, ref accumulator);
        Kernels.Alpha(1, 84, 84, gameTick, ref accumulator);
        Kernels.Alpha(1, 85, 85, gameTick, ref accumulator);
        Kernels.Alpha(1, 86, 86, gameTick, ref accumulator);
        Kernels.Alpha(1, 87, 87, gameTick, ref accumulator);
        Kernels.Alpha(1, 88, 88, gameTick, ref accumulator);
        Kernels.Alpha(1, 89, 89, gameTick, ref accumulator);
        Kernels.Alpha(1, 90, 90, gameTick, ref accumulator);
        Kernels.Alpha(1, 91, 91, gameTick, ref accumulator);
        Kernels.Alpha(1, 92, 92, gameTick, ref accumulator);
        Kernels.Alpha(1, 93, 93, gameTick, ref accumulator);
        Kernels.Alpha(1, 94, 94, gameTick, ref accumulator);
        Kernels.Alpha(1, 95, 95, gameTick, ref accumulator);
        Kernels.Alpha(1, 96, 96, gameTick, ref accumulator);
        Kernels.Alpha(1, 97, 97, gameTick, ref accumulator);
        Kernels.Alpha(1, 98, 98, gameTick, ref accumulator);
        Kernels.Alpha(1, 99, 99, gameTick, ref accumulator);
        Kernels.Alpha(1, 100, 100, gameTick, ref accumulator);
        Kernels.Alpha(1, 101, 101, gameTick, ref accumulator);
        Kernels.Alpha(1, 102, 102, gameTick, ref accumulator);
        Kernels.Alpha(1, 103, 103, gameTick, ref accumulator);
        Kernels.Alpha(1, 104, 104, gameTick, ref accumulator);
        Kernels.Alpha(1, 105, 105, gameTick, ref accumulator);
        Kernels.Alpha(1, 106, 106, gameTick, ref accumulator);
        Kernels.Alpha(1, 107, 107, gameTick, ref accumulator);
        Kernels.Alpha(1, 108, 108, gameTick, ref accumulator);
        Kernels.Alpha(1, 109, 109, gameTick, ref accumulator);
        Kernels.Alpha(1, 110, 110, gameTick, ref accumulator);
        Kernels.Alpha(1, 111, 111, gameTick, ref accumulator);
        Kernels.Alpha(1, 112, 112, gameTick, ref accumulator);
        Kernels.Alpha(1, 113, 113, gameTick, ref accumulator);
        Kernels.Alpha(1, 114, 114, gameTick, ref accumulator);
        Kernels.Alpha(1, 115, 115, gameTick, ref accumulator);
        Kernels.Alpha(1, 116, 116, gameTick, ref accumulator);
        Kernels.Alpha(1, 117, 117, gameTick, ref accumulator);
        Kernels.Alpha(1, 118, 118, gameTick, ref accumulator);
        Kernels.Alpha(1, 119, 119, gameTick, ref accumulator);
        Kernels.Alpha(1, 120, 120, gameTick, ref accumulator);
        Kernels.Alpha(1, 121, 121, gameTick, ref accumulator);
        Kernels.Alpha(1, 122, 122, gameTick, ref accumulator);
        Kernels.Alpha(1, 123, 123, gameTick, ref accumulator);
        Kernels.Alpha(1, 124, 124, gameTick, ref accumulator);
        Kernels.Alpha(1, 125, 125, gameTick, ref accumulator);
        Kernels.Alpha(1, 126, 126, gameTick, ref accumulator);
        Kernels.Alpha(1, 127, 127, gameTick, ref accumulator);
        Kernels.Alpha(1, 128, 128, gameTick, ref accumulator);
        Kernels.Alpha(1, 129, 129, gameTick, ref accumulator);
        Kernels.Alpha(1, 130, 130, gameTick, ref accumulator);
        Kernels.Alpha(1, 131, 131, gameTick, ref accumulator);
        Kernels.Alpha(1, 132, 132, gameTick, ref accumulator);
        Kernels.Alpha(1, 133, 133, gameTick, ref accumulator);
        Kernels.Alpha(1, 134, 134, gameTick, ref accumulator);
        Kernels.Alpha(1, 135, 135, gameTick, ref accumulator);
        Kernels.Alpha(1, 136, 136, gameTick, ref accumulator);
        Kernels.Alpha(1, 137, 137, gameTick, ref accumulator);
        Kernels.Alpha(1, 138, 138, gameTick, ref accumulator);
        Kernels.Alpha(1, 139, 139, gameTick, ref accumulator);
        Kernels.Alpha(1, 140, 140, gameTick, ref accumulator);
        Kernels.Alpha(1, 141, 141, gameTick, ref accumulator);
        Kernels.Alpha(1, 142, 142, gameTick, ref accumulator);
        Kernels.Alpha(1, 143, 143, gameTick, ref accumulator);
        Kernels.Alpha(1, 144, 144, gameTick, ref accumulator);
        Kernels.Alpha(1, 145, 145, gameTick, ref accumulator);
        Kernels.Alpha(1, 146, 146, gameTick, ref accumulator);
        Kernels.Alpha(1, 147, 147, gameTick, ref accumulator);
        Kernels.Alpha(1, 148, 148, gameTick, ref accumulator);
        Kernels.Alpha(1, 149, 149, gameTick, ref accumulator);
        Kernels.Alpha(1, 150, 150, gameTick, ref accumulator);
        Kernels.Alpha(1, 151, 151, gameTick, ref accumulator);
        Kernels.Alpha(1, 152, 152, gameTick, ref accumulator);
        Kernels.Alpha(1, 153, 153, gameTick, ref accumulator);
        Kernels.Alpha(1, 154, 154, gameTick, ref accumulator);
        Kernels.Alpha(1, 155, 155, gameTick, ref accumulator);
        Kernels.Alpha(1, 156, 156, gameTick, ref accumulator);
        Kernels.Alpha(1, 157, 157, gameTick, ref accumulator);
        Kernels.Alpha(1, 158, 158, gameTick, ref accumulator);
        Kernels.Alpha(1, 159, 159, gameTick, ref accumulator);
        Kernels.Alpha(1, 160, 160, gameTick, ref accumulator);
        Kernels.Alpha(1, 161, 161, gameTick, ref accumulator);
        Kernels.Alpha(1, 162, 162, gameTick, ref accumulator);
        Kernels.Alpha(1, 163, 163, gameTick, ref accumulator);
        Kernels.Alpha(1, 164, 164, gameTick, ref accumulator);
        Kernels.Alpha(1, 165, 165, gameTick, ref accumulator);
        Kernels.Alpha(1, 166, 166, gameTick, ref accumulator);
        Kernels.Alpha(1, 167, 167, gameTick, ref accumulator);
        Kernels.Alpha(1, 168, 168, gameTick, ref accumulator);
        Kernels.Alpha(1, 169, 169, gameTick, ref accumulator);
        Kernels.Alpha(1, 170, 170, gameTick, ref accumulator);
        Kernels.Alpha(1, 171, 171, gameTick, ref accumulator);
        Kernels.Alpha(1, 172, 172, gameTick, ref accumulator);
        Kernels.Alpha(1, 173, 173, gameTick, ref accumulator);
        Kernels.Alpha(1, 174, 174, gameTick, ref accumulator);
        Kernels.Alpha(1, 175, 175, gameTick, ref accumulator);
        Kernels.Alpha(1, 176, 176, gameTick, ref accumulator);
        Kernels.Alpha(1, 177, 177, gameTick, ref accumulator);
        Kernels.Alpha(1, 178, 178, gameTick, ref accumulator);
        Kernels.Alpha(1, 179, 179, gameTick, ref accumulator);
        Kernels.Alpha(1, 180, 180, gameTick, ref accumulator);
        Kernels.Alpha(1, 181, 181, gameTick, ref accumulator);
        Kernels.Alpha(1, 182, 182, gameTick, ref accumulator);
        Kernels.Alpha(1, 183, 183, gameTick, ref accumulator);
        Kernels.Alpha(1, 184, 184, gameTick, ref accumulator);
        Kernels.Alpha(1, 185, 185, gameTick, ref accumulator);
        Kernels.Alpha(1, 186, 186, gameTick, ref accumulator);
        Kernels.Alpha(1, 187, 187, gameTick, ref accumulator);
        Kernels.Alpha(1, 188, 188, gameTick, ref accumulator);
        Kernels.Alpha(1, 189, 189, gameTick, ref accumulator);
        Kernels.Alpha(1, 190, 190, gameTick, ref accumulator);
        Kernels.Alpha(1, 191, 191, gameTick, ref accumulator);
        Kernels.Alpha(1, 192, 192, gameTick, ref accumulator);
        Kernels.Alpha(1, 193, 193, gameTick, ref accumulator);
        Kernels.Alpha(1, 194, 194, gameTick, ref accumulator);
        Kernels.Alpha(1, 195, 195, gameTick, ref accumulator);
        Kernels.Alpha(1, 196, 196, gameTick, ref accumulator);
        Kernels.Alpha(1, 197, 197, gameTick, ref accumulator);
        Kernels.Alpha(1, 198, 198, gameTick, ref accumulator);
        Kernels.Alpha(1, 199, 199, gameTick, ref accumulator);
        Kernels.Alpha(1, 200, 200, gameTick, ref accumulator);
        Kernels.Alpha(1, 201, 201, gameTick, ref accumulator);
        Kernels.Alpha(1, 202, 202, gameTick, ref accumulator);
        Kernels.Alpha(1, 203, 203, gameTick, ref accumulator);
        Kernels.Alpha(1, 204, 204, gameTick, ref accumulator);
        Kernels.Alpha(1, 205, 205, gameTick, ref accumulator);
        Kernels.Alpha(1, 206, 206, gameTick, ref accumulator);
        Kernels.Alpha(1, 207, 207, gameTick, ref accumulator);
        Kernels.Alpha(1, 208, 208, gameTick, ref accumulator);
        Kernels.Alpha(1, 209, 209, gameTick, ref accumulator);
        Kernels.Alpha(1, 210, 210, gameTick, ref accumulator);
        Kernels.Alpha(1, 211, 211, gameTick, ref accumulator);
        Kernels.Alpha(1, 212, 212, gameTick, ref accumulator);
        Kernels.Alpha(1, 213, 213, gameTick, ref accumulator);
        Kernels.Alpha(1, 214, 214, gameTick, ref accumulator);
        Kernels.Alpha(1, 215, 215, gameTick, ref accumulator);
        Kernels.Alpha(1, 216, 216, gameTick, ref accumulator);
        Kernels.Alpha(1, 217, 217, gameTick, ref accumulator);
        Kernels.Alpha(1, 218, 218, gameTick, ref accumulator);
        Kernels.Alpha(1, 219, 219, gameTick, ref accumulator);
        Kernels.Alpha(1, 220, 220, gameTick, ref accumulator);
        Kernels.Alpha(1, 221, 221, gameTick, ref accumulator);
        Kernels.Alpha(1, 222, 222, gameTick, ref accumulator);
        Kernels.Alpha(1, 223, 223, gameTick, ref accumulator);
        Kernels.Alpha(1, 224, 224, gameTick, ref accumulator);
        Kernels.Alpha(1, 225, 225, gameTick, ref accumulator);
        Kernels.Alpha(1, 226, 226, gameTick, ref accumulator);
        Kernels.Alpha(1, 227, 227, gameTick, ref accumulator);
        Kernels.Alpha(1, 228, 228, gameTick, ref accumulator);
        Kernels.Alpha(1, 229, 229, gameTick, ref accumulator);
        Kernels.Alpha(1, 230, 230, gameTick, ref accumulator);
        Kernels.Alpha(1, 231, 231, gameTick, ref accumulator);
        Kernels.Alpha(1, 232, 232, gameTick, ref accumulator);
        Kernels.Alpha(1, 233, 233, gameTick, ref accumulator);
        Kernels.Alpha(1, 234, 234, gameTick, ref accumulator);
        Kernels.Alpha(1, 235, 235, gameTick, ref accumulator);
        Kernels.Alpha(1, 236, 236, gameTick, ref accumulator);
        Kernels.Alpha(1, 237, 237, gameTick, ref accumulator);
        Kernels.Alpha(1, 238, 238, gameTick, ref accumulator);
        Kernels.Alpha(1, 239, 239, gameTick, ref accumulator);
        Kernels.Alpha(1, 240, 240, gameTick, ref accumulator);
        Kernels.Alpha(1, 241, 241, gameTick, ref accumulator);
        Kernels.Alpha(1, 242, 242, gameTick, ref accumulator);
        Kernels.Alpha(1, 243, 243, gameTick, ref accumulator);
        Kernels.Alpha(1, 244, 244, gameTick, ref accumulator);
        Kernels.Alpha(1, 245, 245, gameTick, ref accumulator);
        Kernels.Alpha(1, 246, 246, gameTick, ref accumulator);
        Kernels.Alpha(1, 247, 247, gameTick, ref accumulator);
        Kernels.Alpha(1, 248, 248, gameTick, ref accumulator);
        Kernels.Alpha(1, 249, 249, gameTick, ref accumulator);
        Kernels.Alpha(1, 250, 250, gameTick, ref accumulator);
        Kernels.Alpha(1, 251, 251, gameTick, ref accumulator);
        Kernels.Alpha(1, 252, 252, gameTick, ref accumulator);
        Kernels.Alpha(1, 253, 253, gameTick, ref accumulator);
        Kernels.Alpha(1, 254, 254, gameTick, ref accumulator);
        Kernels.Alpha(1, 255, 255, gameTick, ref accumulator);
        Kernels.Alpha(1, 256, 256, gameTick, ref accumulator);
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
    internal static void TickGapForward(uint gameTick, ref ReferenceState state, ref Accumulator accumulator)
    {
        MoveForward(ref state, out var tick);
        if (tick < 16u)
            Kernels.Alpha(1, 4, 1, gameTick, ref accumulator);
        else if (tick >= 32u)
            Kernels.Alpha(1, 4, 3, gameTick, ref accumulator);
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
    internal static void TickBlendForward(uint gameTick, ref ReferenceState state, ref Accumulator accumulator)
    {
        MoveForward(ref state, out var tick);
        var factor = tick / 63f;
        var amount = (int)(1 + 8 * factor);
        Kernels.Alpha(1, 5, amount, gameTick, ref accumulator);
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
    internal static void TickComponentForward(
        uint gameTick,
        in FirstInput first,
        in SecondInput second,
        in ThirdInput third,
        ref ReferenceState state,
        ref Accumulator accumulator)
    {
        MoveForward(ref state, out _);
        Kernels.Component(1, 1, 1, gameTick, first.Value, second.Value, third.Value, ref accumulator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MoveForward(ref ReferenceState state, out uint tick)
    {
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
