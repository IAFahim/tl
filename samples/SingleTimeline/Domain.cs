using Tl;

namespace SingleTimeline;

public readonly record struct AlphaTrack(float Mult) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
}
public readonly record struct AlphaClip(int Amount);

public readonly record struct BetaTrack(float Mult) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
}
public readonly record struct BetaClip(int Amount);

public readonly record struct GammaTrack(float Mult) : IBlend<GammaClip>
{
    public void Blend(in GammaClip first, in GammaClip second, float factor, out GammaClip result) => result = first;
}
public readonly record struct GammaClip(int Amount);

public struct AlphaJob : ITimelineJob<AlphaTrack, AlphaClip>
{
    public static void Execute(in Frame<AlphaTrack, AlphaClip> f, ref int column)
        => column -= f.Clip.Amount * (int)f.Track.Mult;
}

public struct BetaJob : ITimelineJob<BetaTrack, BetaClip>
{
    public static void Execute(in Frame<BetaTrack, BetaClip> f, ref float column)
        => column -= f.Clip.Amount * f.Track.Mult;
}

public struct GammaJob : ITimelineJob<GammaTrack, GammaClip>
{
    public static void Execute(in Frame<GammaTrack, GammaClip> f, ref long column)
        => column -= f.Clip.Amount * (int)f.Track.Mult;
}
