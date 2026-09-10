using System.Runtime.CompilerServices;
using Tl;

public enum TickPattern
{
    Forward,
    Alternating,
}

public struct Accumulator
{
    public long Value;
    public long Order;
    public long GameTickSum;
    public int Calls;
}

public readonly record struct AlphaClip(int Amount);
public readonly record struct BetaClip(int Amount);

public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly struct AlphaJob : ITimelineJob<AlphaTrack, AlphaClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(in Frame<AlphaTrack, AlphaClip> frame, ref Accumulator accumulator)
        => Kernels.Alpha(frame.Direction, frame.Track.Code, frame.Clip.Amount, frame.GameTick, ref accumulator);
}

public readonly struct BetaJob : ITimelineJob<BetaTrack, BetaClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(in Frame<BetaTrack, BetaClip> frame, ref Accumulator accumulator)
        => Kernels.Beta(frame.Direction, frame.Track.Code, frame.Clip.Amount, frame.GameTick, ref accumulator);
}

public readonly partial struct MixedTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var first = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        var middle = builder.Track(new BetaTrack(2)).Use<BetaJob>();
        var last = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        builder.Clip(first, new AlphaClip(1), 0u, 64u);
        builder.Clip(middle, new BetaClip(2), 0u, 64u);
        builder.Clip(last, new AlphaClip(3), 0u, 64u);
        builder.Looping();
    }
}

public readonly struct BenchmarkRows;

public readonly partial struct BenchmarkCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<BenchmarkRows>().Asset<MixedTimeline>();
    }
}

public readonly record struct BenchmarkReceipt(
    long StateHash,
    long ValueHash,
    long OrderHash,
    long GameTickHash,
    long Calls);

internal static class Kernels
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Alpha(int direction, int code, int amount, uint gameTick, ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 31 + direction * amount);
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Beta(int direction, int code, int amount, uint gameTick, ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 37 + direction * amount);
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }
}
