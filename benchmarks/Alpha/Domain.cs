using Tl;
using System.Diagnostics.CodeAnalysis;

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

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly struct AlphaJob : ITrack<AlphaTrack, AlphaClip>
{
    public static void OnActive(in Frame<AlphaTrack, AlphaClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Amount;
}

public readonly struct BetaJob : ITrack<BetaTrack, BetaClip>
{
    public static void OnActive(in Frame<BetaTrack, BetaClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Amount;
}
