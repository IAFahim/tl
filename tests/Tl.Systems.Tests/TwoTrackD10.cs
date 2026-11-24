using System;
using Tl;

namespace D10;

public readonly struct TwoTrackFoldClip
{
    public readonly float Amount;
    public TwoTrackFoldClip(float amount) { Amount = amount; }
}

public readonly struct TwoTrackFoldTrack : IBlend<TwoTrackFoldClip>
{
    public readonly float Scale;
    public TwoTrackFoldTrack(float scale) { Scale = scale; }
    public void Blend(in TwoTrackFoldClip first, in TwoTrackFoldClip second, float factor, out TwoTrackFoldClip result)
        => result = new TwoTrackFoldClip(first.Amount + (second.Amount - first.Amount) * factor);
}

[TimelineSystemMember(typeof(TwoTrack))]
public readonly struct TwoTrackFold : ITrack<TwoTrackFoldTrack, TwoTrackFoldClip>
{
    public static void Fold(in Frame<TwoTrackFoldTrack, TwoTrackFoldClip> frame, out float lift)
        => lift = frame.Direction * frame.Track.Scale * frame.Clip.Amount;

    [Feeds(typeof(TrackEffect))]
    public static float Evaluate(in float lift) => lift;
}

[TimelineKind(Mode = KindMode.Each)]
public struct TrackEffect
{
    public static void Apply(in float lift, ref float total, ref int runs)
    {
        total += lift;
        runs++;
    }

}

[TimelineSystem]
public ref partial struct TwoTrack
{
    public Span<float> Total;
    public Span<int> Runs;
    
}
