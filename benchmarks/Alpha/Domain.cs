using System.Runtime.CompilerServices;
using Tl;

public enum TickPattern
{
    Forward,
    Alternating,
}

public enum TimelineShape
{
    OneTrack,
    ThreeTracks,
    SixteenTracks,
    Gap,
    Blend,
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

public readonly partial struct OneTrackTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct SixteenTrackTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track01 = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        var track02 = builder.Track(new AlphaTrack(2)).Use<AlphaJob>();
        var track03 = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        var track04 = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        var track05 = builder.Track(new AlphaTrack(5)).Use<AlphaJob>();
        var track06 = builder.Track(new AlphaTrack(6)).Use<AlphaJob>();
        var track07 = builder.Track(new AlphaTrack(7)).Use<AlphaJob>();
        var track08 = builder.Track(new AlphaTrack(8)).Use<AlphaJob>();
        var track09 = builder.Track(new AlphaTrack(9)).Use<AlphaJob>();
        var track10 = builder.Track(new AlphaTrack(10)).Use<AlphaJob>();
        var track11 = builder.Track(new AlphaTrack(11)).Use<AlphaJob>();
        var track12 = builder.Track(new AlphaTrack(12)).Use<AlphaJob>();
        var track13 = builder.Track(new AlphaTrack(13)).Use<AlphaJob>();
        var track14 = builder.Track(new AlphaTrack(14)).Use<AlphaJob>();
        var track15 = builder.Track(new AlphaTrack(15)).Use<AlphaJob>();
        var track16 = builder.Track(new AlphaTrack(16)).Use<AlphaJob>();
        builder.Clip(track01, new AlphaClip(1), 0u, 64u);
        builder.Clip(track02, new AlphaClip(2), 0u, 64u);
        builder.Clip(track03, new AlphaClip(3), 0u, 64u);
        builder.Clip(track04, new AlphaClip(4), 0u, 64u);
        builder.Clip(track05, new AlphaClip(5), 0u, 64u);
        builder.Clip(track06, new AlphaClip(6), 0u, 64u);
        builder.Clip(track07, new AlphaClip(7), 0u, 64u);
        builder.Clip(track08, new AlphaClip(8), 0u, 64u);
        builder.Clip(track09, new AlphaClip(9), 0u, 64u);
        builder.Clip(track10, new AlphaClip(10), 0u, 64u);
        builder.Clip(track11, new AlphaClip(11), 0u, 64u);
        builder.Clip(track12, new AlphaClip(12), 0u, 64u);
        builder.Clip(track13, new AlphaClip(13), 0u, 64u);
        builder.Clip(track14, new AlphaClip(14), 0u, 64u);
        builder.Clip(track15, new AlphaClip(15), 0u, 64u);
        builder.Clip(track16, new AlphaClip(16), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct GapTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 16u);
        builder.Clip(track, new AlphaClip(3), 32u, 64u);
        builder.Looping();
    }
}

public readonly partial struct BlendTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(5)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 64u);
        builder.Clip(track, new AlphaClip(9), 0u, 64u);
        builder.Looping();
    }
}

public readonly record struct FirstInput(int Value);
public readonly record struct SecondInput(int Value);
public readonly record struct ThirdInput(int Value);
public readonly record struct ComponentTrack(int Code) : IBlend<ComponentClip>
{
    public void Blend(in ComponentClip first, in ComponentClip second, float factor, out ComponentClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly record struct ComponentClip(int Amount);

public readonly struct ComponentJob : ITimelineJob<ComponentTrack, ComponentClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(
        in Frame<ComponentTrack, ComponentClip> frame,
        in FirstInput first,
        in SecondInput second,
        in ThirdInput third,
        ref Accumulator accumulator)
        => Kernels.Component(
            frame.Direction,
            frame.Track.Code,
            frame.Clip.Amount,
            frame.GameTick,
            first.Value,
            second.Value,
            third.Value,
            ref accumulator);
}

public readonly partial struct ComponentTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new ComponentTrack(1)).Use<ComponentJob>();
        builder.Clip(track, new ComponentClip(1), 0u, 64u);
        builder.Looping();
    }
}

public readonly struct BenchmarkRows;
public readonly struct ShapeRows;
public readonly struct ComponentRows;

public readonly partial struct BenchmarkCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<BenchmarkRows>().Asset<MixedTimeline>();
    }
}

public readonly partial struct ShapeCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<ShapeRows>()
            .Asset<OneTrackTimeline>()
            .Asset<MixedTimeline>()
            .Asset<SixteenTrackTimeline>()
            .Asset<GapTimeline>()
            .Asset<BlendTimeline>();
        builder.Schema<ComponentRows>().Asset<ComponentTimeline>();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Component(
        int direction,
        int code,
        int amount,
        uint gameTick,
        int first,
        int second,
        int third,
        ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 31 + direction * (amount + first + second + third));
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }
}
