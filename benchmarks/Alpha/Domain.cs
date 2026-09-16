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
    TwoHundredFiftySixTracks,
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
    public static void Execute(in Frame<AlphaTrack, AlphaClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Amount;
}

public readonly struct BetaJob : ITimelineJob<BetaTrack, BetaClip>
{
    public static void Execute(in Frame<BetaTrack, BetaClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Amount;
}
