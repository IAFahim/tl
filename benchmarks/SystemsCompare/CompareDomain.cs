using Tl;

namespace Compare;

// Unit column wrappers (systems require distinct column element types).
public struct F0 { public float V; public F0(float v) { V = v; } }
public struct F1 { public float V; public F1(float v) { V = v; } }
public struct F2 { public float V; public F2(float v) { V = v; } }
public struct F3 { public float V; public F3(float v) { V = v; } }

[Tl.TimelineSystem]
public ref partial struct CA
{
    public global::System.Span<float> Y;
}

[Tl.TimelineSystem]
public ref partial struct CB
{
    public global::System.Span<float> H;
    public global::System.Span<int> P;
}

[Tl.TimelineSystem]
public ref partial struct CC
{
    public global::System.Span<float> X;
}

[Tl.TimelineSystem]
public ref partial struct CD
{
    public global::System.Span<F0> W;
    public global::System.Span<F1> X;
    public global::System.Span<F2> Y;
    public global::System.Span<F3> Z;
}

// A: folded `y += lane[tick]` (mirrors LiveBatch LiftFold math).
[Tl.TimelineSystemMember(typeof(CA))]
public readonly struct NewAFold : ITrack<LiveBatch.LiftTrack, LiveBatch.LiftClip>
{
    public static void Fold(in Frame<LiveBatch.LiftTrack, LiveBatch.LiftClip> frame, out float lift)
        => lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale;
    public static void ExecuteActive(in Frame<LiveBatch.LiftTrack, LiveBatch.LiftClip> frame, in float lift, ref float y) { y += lift; }
    public static void OnClipEnd(in Frame<LiveBatch.LiftTrack, LiveBatch.LiftClip> frame) { }
}

// B: fold-fed compose `height += arc * pushes` (mirrors LiveBatch ArcCompose math).
[Tl.TimelineSystemMember(typeof(CB))]
public readonly struct NewBCompose : ITrack<LiveBatch.ArcTrack, LiveBatch.ArcClip>
{
    public static void Fold(in Frame<LiveBatch.ArcTrack, LiveBatch.ArcClip> frame, out float arc)
        => arc = frame.Direction * frame.Clip.Height * frame.Track.Scale;
    public static void ExecuteActive(in Frame<LiveBatch.ArcTrack, LiveBatch.ArcClip> frame, in float arc, in int pushes, ref float height) { height += arc * pushes; }
    public static void OnClipEnd(in Frame<LiveBatch.ArcTrack, LiveBatch.ArcClip> frame) { }
}

// C: frame consumer `x += Direction * Gain * speed` (mirrors LiveBatch PushFrame math).
[Tl.TimelineSystemMember(typeof(CC))]
public readonly struct NewCFrame : ITrack<LiveBatch.PushTrack, LiveBatch.PushClip>
{
    public static void ExecuteActive(in Frame<LiveBatch.PushTrack, LiveBatch.PushClip> frame, ref float x) { x += frame.Direction * frame.Clip.Height * frame.Track.Scale; }
    public static void OnClipEnd(in Frame<LiveBatch.PushTrack, LiveBatch.PushClip> frame) { }
}

// D: four pairs, one system (mirrors LiveBatch D0Job..D3Job math).
[Tl.TimelineSystemMember(typeof(CD))]
public readonly struct NewD0 : ITrack<LiveBatch.D0Track, LiveBatch.D0Clip>
{
    public static void ExecuteActive(in Frame<LiveBatch.D0Track, LiveBatch.D0Clip> frame, ref F0 slot) { slot.V += frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void OnClipEnd(in Frame<LiveBatch.D0Track, LiveBatch.D0Clip> frame) { }
}

[Tl.TimelineSystemMember(typeof(CD))]
public readonly struct NewD1 : ITrack<LiveBatch.D1Track, LiveBatch.D1Clip>
{
    public static void ExecuteActive(in Frame<LiveBatch.D1Track, LiveBatch.D1Clip> frame, ref F1 slot) { slot.V -= frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void OnClipEnd(in Frame<LiveBatch.D1Track, LiveBatch.D1Clip> frame) { }
}

[Tl.TimelineSystemMember(typeof(CD))]
public readonly struct NewD2 : ITrack<LiveBatch.D2Track, LiveBatch.D2Clip>
{
    public static void ExecuteActive(in Frame<LiveBatch.D2Track, LiveBatch.D2Clip> frame, ref F2 slot) { slot.V += frame.Direction * frame.Clip.Amount * (1 + frame.Track.Scale); }
    public static void OnClipEnd(in Frame<LiveBatch.D2Track, LiveBatch.D2Clip> frame) { }
}

[Tl.TimelineSystemMember(typeof(CD))]
public readonly struct NewD3 : ITrack<LiveBatch.D3Track, LiveBatch.D3Clip>
{
    public static void ExecuteActive(in Frame<LiveBatch.D3Track, LiveBatch.D3Clip> frame, ref F3 slot) { slot.V += 2 * frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void OnClipEnd(in Frame<LiveBatch.D3Track, LiveBatch.D3Clip> frame) { }
}
