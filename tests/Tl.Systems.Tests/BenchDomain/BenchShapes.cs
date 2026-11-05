using Tl;

namespace Bench;

public readonly struct BTrack : IBlend<BClip>
{
    public readonly float Scale;
    public BTrack(float scale) { Scale = scale; }
    public void Blend(in BClip first, in BClip second, float factor, out BClip result)
    {
        result = new BClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct BClip
{
    public float Amount;
    public BClip(float amount) { Amount = amount; }
}

public readonly struct CTrack : IBlend<CClip>
{
    public readonly float Scale;
    public CTrack(float scale) { Scale = scale; }
    public void Blend(in CClip first, in CClip second, float factor, out CClip result)
    {
        result = new CClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct CClip
{
    public float Amount;
    public CClip(float amount) { Amount = amount; }
}

public readonly struct DTrack : IBlend<DClip>
{
    public readonly float Scale;
    public DTrack(float scale) { Scale = scale; }
    public void Blend(in DClip first, in DClip second, float factor, out DClip result)
    {
        result = new DClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}
public struct DClip
{
    public float Amount;
    public DClip(float amount) { Amount = amount; }
}

public readonly struct ETrack : IBlend<EClip>
{
    public readonly float Scale;
    public ETrack(float scale) { Scale = scale; }
    public void Blend(in EClip first, in EClip second, float factor, out EClip result)
    {
        result = new EClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct EClip
{
    public float Amount;
    public EClip(float amount) { Amount = amount; }
}

public readonly struct FTrack : IBlend<FClip>
{
    public readonly float Scale;
    public FTrack(float scale) { Scale = scale; }
    public void Blend(in FClip first, in FClip second, float factor, out FClip result)
    {
        result = new FClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct FClip
{
    public float Amount;
    public FClip(float amount) { Amount = amount; }
}

public readonly struct GTrack : IBlend<GClip>
{
    public readonly float Scale;
    public GTrack(float scale) { Scale = scale; }
    public void Blend(in GClip first, in GClip second, float factor, out GClip result)
    {
        result = new GClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct GClip
{
    public float Amount;
    public GClip(float amount) { Amount = amount; }
}

public readonly struct HTrack : IBlend<HClip>
{
    public readonly float Scale;
    public HTrack(float scale) { Scale = scale; }
    public void Blend(in HClip first, in HClip second, float factor, out HClip result)
    {
        result = new HClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public struct HClip
{
    public float Amount;
    public HClip(float amount) { Amount = amount; }
}

[TimelineSystem]
public ref partial struct SA
{
    public global::System.Span<float> Fx;
}

[TimelineSystem]
public ref partial struct SB
{
    public global::System.Span<float> Fx;
    public global::System.Span<int> Ix;
}

[TimelineSystem]
public ref partial struct SC
{
    public global::System.Span<int> C;
    public global::System.Span<float> F;
}

[TimelineSystem]
public ref partial struct SD
{
    public global::System.Span<float> D0;
    public global::System.Span<int> D1;
    public global::System.Span<short> D2;
    public global::System.Span<sbyte> D3;
}

public readonly struct OldA : ITrack<BTrack, BClip>
{
    public static void Fold(in Frame<BTrack, BClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<BTrack, BClip> frame, in float lift, ref float fx) { fx += lift; }
}

public readonly struct OldB : ITrack<CTrack, CClip>
{
    public static void Fold(in Frame<CTrack, CClip> frame, out float lift, out int kind) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; kind = (int)(frame.Clip.Amount * 10); }
    public static void ExecuteActive(in Frame<CTrack, CClip> frame, in float lift, in int kind, ref float fx, ref int ix) { fx += lift; ix += kind; }
}

public readonly struct OldC : ITrack<DTrack, DClip>
{
    public static void ExecuteActive(in Frame<DTrack, DClip> frame, in int gain, ref float c) { c += gain * frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
}

public readonly struct OldD0 : ITrack<ETrack, EClip>
{
    public static void Fold(in Frame<ETrack, EClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<ETrack, EClip> frame, in float lift, ref float d) { d += lift; }
}

public readonly struct OldD1 : ITrack<FTrack, FClip>
{
    public static void Fold(in Frame<FTrack, FClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<FTrack, FClip> frame, in float lift, ref int d) { d += (int)lift; }
}

public readonly struct OldD2 : ITrack<GTrack, GClip>
{
    public static void Fold(in Frame<GTrack, GClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<GTrack, GClip> frame, in float lift, ref short d) { d += (short)lift; }
}

public readonly struct OldD3 : ITrack<HTrack, HClip>
{
    public static void Fold(in Frame<HTrack, HClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<HTrack, HClip> frame, in float lift, ref sbyte d) { d += (sbyte)lift; }
}

[Tl.TimelineSystemMember(typeof(SA))]
public readonly struct NewA : ITrack<BTrack, BClip>
{
    public static void Fold(in Frame<BTrack, BClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<BTrack, BClip> frame, in float lift, ref float fx) { fx += lift; }
    public static void OnClipEnd(in Frame<BTrack, BClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SB))]
public readonly struct NewB : ITrack<CTrack, CClip>
{
    public static void Fold(in Frame<CTrack, CClip> frame, out float lift, out int kind) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; kind = (int)(frame.Clip.Amount * 10); }
    public static void ExecuteActive(in Frame<CTrack, CClip> frame, in float lift, in int kind, ref float fx, ref int ix) { fx += lift; ix += kind; }
    public static void OnClipEnd(in Frame<CTrack, CClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SC))]
public readonly struct NewC : ITrack<DTrack, DClip>
{
    public static void ExecuteActive(in Frame<DTrack, DClip> frame, in int gain, ref float c) { c += gain * frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void OnClipEnd(in Frame<DTrack, DClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SD))]
public readonly struct NewD0 : ITrack<ETrack, EClip>
{
    public static void Fold(in Frame<ETrack, EClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<ETrack, EClip> frame, in float lift, ref float d) { d += lift; }
    public static void OnClipEnd(in Frame<ETrack, EClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SD))]
public readonly struct NewD1 : ITrack<FTrack, FClip>
{
    public static void Fold(in Frame<FTrack, FClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<FTrack, FClip> frame, in float lift, ref int d) { d += (int)lift; }
    public static void OnClipEnd(in Frame<FTrack, FClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SD))]
public readonly struct NewD2 : ITrack<GTrack, GClip>
{
    public static void Fold(in Frame<GTrack, GClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<GTrack, GClip> frame, in float lift, ref short d) { d += (short)lift; }
    public static void OnClipEnd(in Frame<GTrack, GClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(SD))]
public readonly struct NewD3 : ITrack<HTrack, HClip>
{
    public static void Fold(in Frame<HTrack, HClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
    public static void ExecuteActive(in Frame<HTrack, HClip> frame, in float lift, ref sbyte d) { d += (sbyte)lift; }
    public static void OnClipEnd(in Frame<HTrack, HClip> frame) { }
}

[Tl.TimelineSystem]
public ref partial struct S2
{
    public global::System.Span<int> X;
    public global::System.Span<short> Y;
}

[Tl.TimelineSystemMember(typeof(S2))]
public readonly struct NewH : ITrack<ETrack, EClip>
{
    public static void ExecuteActive(in Frame<ETrack, EClip> frame, ref int x) { x++; }
    public static void OnClipEnd(in Frame<ETrack, EClip> frame) { }
}

[Tl.TimelineSystemMember(typeof(S2))]
public readonly struct NewI : ITrack<FTrack, FClip>
{
    public static void ExecuteActive(in Frame<FTrack, FClip> frame, ref short y) { y++; }
    public static void OnClipEnd(in Frame<FTrack, FClip> frame) { }
}
