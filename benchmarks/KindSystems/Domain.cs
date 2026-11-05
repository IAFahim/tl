using System;
using Tl;

namespace KindBench
{

[TimelineSystem]
public ref partial struct KSys
{
    public Span<float> Fx;
    public Span<int> Hp;
}

[TimelineSystem]
public ref partial struct LSys
{
    public Span<float> Fx;
}

public struct LegacyClip { public float Amount; }
public struct LegacyTrack : IBlend<LegacyClip>
{
    public float Scale;
    public void Blend(in LegacyClip a, in LegacyClip b, float t, out LegacyClip r) => r = a;
}
[TimelineSystemMember(typeof(LSys))]
public struct Legacy : ITrack<LegacyTrack, LegacyClip>
{
    public static void ExecuteActive(in Frame<LegacyTrack, LegacyClip> f, ref float fx) => fx += f.Direction * f.Clip.Amount * f.Track.Scale;
    public static void OnClipEnd(in Frame<LegacyTrack, LegacyClip> f)
    {
    }
}

[TimelineKind(Accepts = KindEntries.Pulses)]
public struct Dmg
{
    public static void Combine(ref int t, in int n) => t += n;
    public static void Apply(in int t, ref int hp) => hp -= t;
}
public struct HitClip { public int Amount; }
public struct HitTrack : IBlend<HitClip>
{
    public float Scale;
    public void Blend(in HitClip a, in HitClip b, float t, out HitClip r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct Hit : ITrack<HitTrack, HitClip>
{
    [Pulse(typeof(Dmg), On = ClipEvent.Start)]
    public static int Start(in Frame<HitTrack, HitClip> f) => f.Clip.Amount;
}

[TimelineKind]
public struct K0
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V0 { public float Amount; }
public struct T0 : IBlend<V0>
{
    public float Scale;
    public void Blend(in V0 a, in V0 b, float t, out V0 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C0 : ITrack<T0, V0>
{
    [Feeds(typeof(K0))]
    public static float Evaluate(in Frame<T0, V0> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K1
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V1 { public float Amount; }
public struct T1 : IBlend<V1>
{
    public float Scale;
    public void Blend(in V1 a, in V1 b, float t, out V1 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C1 : ITrack<T1, V1>
{
    [Feeds(typeof(K1))]
    public static float Evaluate(in Frame<T1, V1> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K2
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V2 { public float Amount; }
public struct T2 : IBlend<V2>
{
    public float Scale;
    public void Blend(in V2 a, in V2 b, float t, out V2 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C2 : ITrack<T2, V2>
{
    [Feeds(typeof(K2))]
    public static float Evaluate(in Frame<T2, V2> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K3
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V3 { public float Amount; }
public struct T3 : IBlend<V3>
{
    public float Scale;
    public void Blend(in V3 a, in V3 b, float t, out V3 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C3 : ITrack<T3, V3>
{
    [Feeds(typeof(K3))]
    public static float Evaluate(in Frame<T3, V3> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K4
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V4 { public float Amount; }
public struct T4 : IBlend<V4>
{
    public float Scale;
    public void Blend(in V4 a, in V4 b, float t, out V4 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C4 : ITrack<T4, V4>
{
    [Feeds(typeof(K4))]
    public static float Evaluate(in Frame<T4, V4> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K5
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V5 { public float Amount; }
public struct T5 : IBlend<V5>
{
    public float Scale;
    public void Blend(in V5 a, in V5 b, float t, out V5 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C5 : ITrack<T5, V5>
{
    [Feeds(typeof(K5))]
    public static float Evaluate(in Frame<T5, V5> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K6
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V6 { public float Amount; }
public struct T6 : IBlend<V6>
{
    public float Scale;
    public void Blend(in V6 a, in V6 b, float t, out V6 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C6 : ITrack<T6, V6>
{
    [Feeds(typeof(K6))]
    public static float Evaluate(in Frame<T6, V6> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K7
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V7 { public float Amount; }
public struct T7 : IBlend<V7>
{
    public float Scale;
    public void Blend(in V7 a, in V7 b, float t, out V7 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C7 : ITrack<T7, V7>
{
    [Feeds(typeof(K7))]
    public static float Evaluate(in Frame<T7, V7> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K8
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V8 { public float Amount; }
public struct T8 : IBlend<V8>
{
    public float Scale;
    public void Blend(in V8 a, in V8 b, float t, out V8 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C8 : ITrack<T8, V8>
{
    [Feeds(typeof(K8))]
    public static float Evaluate(in Frame<T8, V8> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K9
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V9 { public float Amount; }
public struct T9 : IBlend<V9>
{
    public float Scale;
    public void Blend(in V9 a, in V9 b, float t, out V9 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C9 : ITrack<T9, V9>
{
    [Feeds(typeof(K9))]
    public static float Evaluate(in Frame<T9, V9> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K10
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V10 { public float Amount; }
public struct T10 : IBlend<V10>
{
    public float Scale;
    public void Blend(in V10 a, in V10 b, float t, out V10 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C10 : ITrack<T10, V10>
{
    [Feeds(typeof(K10))]
    public static float Evaluate(in Frame<T10, V10> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K11
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V11 { public float Amount; }
public struct T11 : IBlend<V11>
{
    public float Scale;
    public void Blend(in V11 a, in V11 b, float t, out V11 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C11 : ITrack<T11, V11>
{
    [Feeds(typeof(K11))]
    public static float Evaluate(in Frame<T11, V11> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K12
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V12 { public float Amount; }
public struct T12 : IBlend<V12>
{
    public float Scale;
    public void Blend(in V12 a, in V12 b, float t, out V12 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C12 : ITrack<T12, V12>
{
    [Feeds(typeof(K12))]
    public static float Evaluate(in Frame<T12, V12> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K13
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V13 { public float Amount; }
public struct T13 : IBlend<V13>
{
    public float Scale;
    public void Blend(in V13 a, in V13 b, float t, out V13 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C13 : ITrack<T13, V13>
{
    [Feeds(typeof(K13))]
    public static float Evaluate(in Frame<T13, V13> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K14
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V14 { public float Amount; }
public struct T14 : IBlend<V14>
{
    public float Scale;
    public void Blend(in V14 a, in V14 b, float t, out V14 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C14 : ITrack<T14, V14>
{
    [Feeds(typeof(K14))]
    public static float Evaluate(in Frame<T14, V14> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K15
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V15 { public float Amount; }
public struct T15 : IBlend<V15>
{
    public float Scale;
    public void Blend(in V15 a, in V15 b, float t, out V15 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C15 : ITrack<T15, V15>
{
    [Feeds(typeof(K15))]
    public static float Evaluate(in Frame<T15, V15> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K16
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V16 { public float Amount; }
public struct T16 : IBlend<V16>
{
    public float Scale;
    public void Blend(in V16 a, in V16 b, float t, out V16 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C16 : ITrack<T16, V16>
{
    [Feeds(typeof(K16))]
    public static float Evaluate(in Frame<T16, V16> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K17
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V17 { public float Amount; }
public struct T17 : IBlend<V17>
{
    public float Scale;
    public void Blend(in V17 a, in V17 b, float t, out V17 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C17 : ITrack<T17, V17>
{
    [Feeds(typeof(K17))]
    public static float Evaluate(in Frame<T17, V17> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K18
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V18 { public float Amount; }
public struct T18 : IBlend<V18>
{
    public float Scale;
    public void Blend(in V18 a, in V18 b, float t, out V18 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C18 : ITrack<T18, V18>
{
    [Feeds(typeof(K18))]
    public static float Evaluate(in Frame<T18, V18> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K19
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V19 { public float Amount; }
public struct T19 : IBlend<V19>
{
    public float Scale;
    public void Blend(in V19 a, in V19 b, float t, out V19 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C19 : ITrack<T19, V19>
{
    [Feeds(typeof(K19))]
    public static float Evaluate(in Frame<T19, V19> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K20
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V20 { public float Amount; }
public struct T20 : IBlend<V20>
{
    public float Scale;
    public void Blend(in V20 a, in V20 b, float t, out V20 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C20 : ITrack<T20, V20>
{
    [Feeds(typeof(K20))]
    public static float Evaluate(in Frame<T20, V20> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K21
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V21 { public float Amount; }
public struct T21 : IBlend<V21>
{
    public float Scale;
    public void Blend(in V21 a, in V21 b, float t, out V21 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C21 : ITrack<T21, V21>
{
    [Feeds(typeof(K21))]
    public static float Evaluate(in Frame<T21, V21> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K22
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V22 { public float Amount; }
public struct T22 : IBlend<V22>
{
    public float Scale;
    public void Blend(in V22 a, in V22 b, float t, out V22 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C22 : ITrack<T22, V22>
{
    [Feeds(typeof(K22))]
    public static float Evaluate(in Frame<T22, V22> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K23
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V23 { public float Amount; }
public struct T23 : IBlend<V23>
{
    public float Scale;
    public void Blend(in V23 a, in V23 b, float t, out V23 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C23 : ITrack<T23, V23>
{
    [Feeds(typeof(K23))]
    public static float Evaluate(in Frame<T23, V23> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K24
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V24 { public float Amount; }
public struct T24 : IBlend<V24>
{
    public float Scale;
    public void Blend(in V24 a, in V24 b, float t, out V24 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C24 : ITrack<T24, V24>
{
    [Feeds(typeof(K24))]
    public static float Evaluate(in Frame<T24, V24> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K25
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V25 { public float Amount; }
public struct T25 : IBlend<V25>
{
    public float Scale;
    public void Blend(in V25 a, in V25 b, float t, out V25 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C25 : ITrack<T25, V25>
{
    [Feeds(typeof(K25))]
    public static float Evaluate(in Frame<T25, V25> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K26
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V26 { public float Amount; }
public struct T26 : IBlend<V26>
{
    public float Scale;
    public void Blend(in V26 a, in V26 b, float t, out V26 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C26 : ITrack<T26, V26>
{
    [Feeds(typeof(K26))]
    public static float Evaluate(in Frame<T26, V26> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K27
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V27 { public float Amount; }
public struct T27 : IBlend<V27>
{
    public float Scale;
    public void Blend(in V27 a, in V27 b, float t, out V27 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C27 : ITrack<T27, V27>
{
    [Feeds(typeof(K27))]
    public static float Evaluate(in Frame<T27, V27> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K28
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V28 { public float Amount; }
public struct T28 : IBlend<V28>
{
    public float Scale;
    public void Blend(in V28 a, in V28 b, float t, out V28 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C28 : ITrack<T28, V28>
{
    [Feeds(typeof(K28))]
    public static float Evaluate(in Frame<T28, V28> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K29
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V29 { public float Amount; }
public struct T29 : IBlend<V29>
{
    public float Scale;
    public void Blend(in V29 a, in V29 b, float t, out V29 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C29 : ITrack<T29, V29>
{
    [Feeds(typeof(K29))]
    public static float Evaluate(in Frame<T29, V29> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K30
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V30 { public float Amount; }
public struct T30 : IBlend<V30>
{
    public float Scale;
    public void Blend(in V30 a, in V30 b, float t, out V30 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C30 : ITrack<T30, V30>
{
    [Feeds(typeof(K30))]
    public static float Evaluate(in Frame<T30, V30> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K31
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V31 { public float Amount; }
public struct T31 : IBlend<V31>
{
    public float Scale;
    public void Blend(in V31 a, in V31 b, float t, out V31 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C31 : ITrack<T31, V31>
{
    [Feeds(typeof(K31))]
    public static float Evaluate(in Frame<T31, V31> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K32
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V32 { public float Amount; }
public struct T32 : IBlend<V32>
{
    public float Scale;
    public void Blend(in V32 a, in V32 b, float t, out V32 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C32 : ITrack<T32, V32>
{
    [Feeds(typeof(K32))]
    public static float Evaluate(in Frame<T32, V32> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K33
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V33 { public float Amount; }
public struct T33 : IBlend<V33>
{
    public float Scale;
    public void Blend(in V33 a, in V33 b, float t, out V33 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C33 : ITrack<T33, V33>
{
    [Feeds(typeof(K33))]
    public static float Evaluate(in Frame<T33, V33> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K34
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V34 { public float Amount; }
public struct T34 : IBlend<V34>
{
    public float Scale;
    public void Blend(in V34 a, in V34 b, float t, out V34 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C34 : ITrack<T34, V34>
{
    [Feeds(typeof(K34))]
    public static float Evaluate(in Frame<T34, V34> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K35
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V35 { public float Amount; }
public struct T35 : IBlend<V35>
{
    public float Scale;
    public void Blend(in V35 a, in V35 b, float t, out V35 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C35 : ITrack<T35, V35>
{
    [Feeds(typeof(K35))]
    public static float Evaluate(in Frame<T35, V35> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K36
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V36 { public float Amount; }
public struct T36 : IBlend<V36>
{
    public float Scale;
    public void Blend(in V36 a, in V36 b, float t, out V36 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C36 : ITrack<T36, V36>
{
    [Feeds(typeof(K36))]
    public static float Evaluate(in Frame<T36, V36> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K37
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V37 { public float Amount; }
public struct T37 : IBlend<V37>
{
    public float Scale;
    public void Blend(in V37 a, in V37 b, float t, out V37 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C37 : ITrack<T37, V37>
{
    [Feeds(typeof(K37))]
    public static float Evaluate(in Frame<T37, V37> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K38
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V38 { public float Amount; }
public struct T38 : IBlend<V38>
{
    public float Scale;
    public void Blend(in V38 a, in V38 b, float t, out V38 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C38 : ITrack<T38, V38>
{
    [Feeds(typeof(K38))]
    public static float Evaluate(in Frame<T38, V38> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K39
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V39 { public float Amount; }
public struct T39 : IBlend<V39>
{
    public float Scale;
    public void Blend(in V39 a, in V39 b, float t, out V39 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C39 : ITrack<T39, V39>
{
    [Feeds(typeof(K39))]
    public static float Evaluate(in Frame<T39, V39> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K40
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V40 { public float Amount; }
public struct T40 : IBlend<V40>
{
    public float Scale;
    public void Blend(in V40 a, in V40 b, float t, out V40 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C40 : ITrack<T40, V40>
{
    [Feeds(typeof(K40))]
    public static float Evaluate(in Frame<T40, V40> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K41
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V41 { public float Amount; }
public struct T41 : IBlend<V41>
{
    public float Scale;
    public void Blend(in V41 a, in V41 b, float t, out V41 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C41 : ITrack<T41, V41>
{
    [Feeds(typeof(K41))]
    public static float Evaluate(in Frame<T41, V41> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K42
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V42 { public float Amount; }
public struct T42 : IBlend<V42>
{
    public float Scale;
    public void Blend(in V42 a, in V42 b, float t, out V42 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C42 : ITrack<T42, V42>
{
    [Feeds(typeof(K42))]
    public static float Evaluate(in Frame<T42, V42> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K43
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V43 { public float Amount; }
public struct T43 : IBlend<V43>
{
    public float Scale;
    public void Blend(in V43 a, in V43 b, float t, out V43 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C43 : ITrack<T43, V43>
{
    [Feeds(typeof(K43))]
    public static float Evaluate(in Frame<T43, V43> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K44
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V44 { public float Amount; }
public struct T44 : IBlend<V44>
{
    public float Scale;
    public void Blend(in V44 a, in V44 b, float t, out V44 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C44 : ITrack<T44, V44>
{
    [Feeds(typeof(K44))]
    public static float Evaluate(in Frame<T44, V44> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K45
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V45 { public float Amount; }
public struct T45 : IBlend<V45>
{
    public float Scale;
    public void Blend(in V45 a, in V45 b, float t, out V45 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C45 : ITrack<T45, V45>
{
    [Feeds(typeof(K45))]
    public static float Evaluate(in Frame<T45, V45> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K46
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V46 { public float Amount; }
public struct T46 : IBlend<V46>
{
    public float Scale;
    public void Blend(in V46 a, in V46 b, float t, out V46 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C46 : ITrack<T46, V46>
{
    [Feeds(typeof(K46))]
    public static float Evaluate(in Frame<T46, V46> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K47
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V47 { public float Amount; }
public struct T47 : IBlend<V47>
{
    public float Scale;
    public void Blend(in V47 a, in V47 b, float t, out V47 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C47 : ITrack<T47, V47>
{
    [Feeds(typeof(K47))]
    public static float Evaluate(in Frame<T47, V47> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K48
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V48 { public float Amount; }
public struct T48 : IBlend<V48>
{
    public float Scale;
    public void Blend(in V48 a, in V48 b, float t, out V48 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C48 : ITrack<T48, V48>
{
    [Feeds(typeof(K48))]
    public static float Evaluate(in Frame<T48, V48> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K49
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V49 { public float Amount; }
public struct T49 : IBlend<V49>
{
    public float Scale;
    public void Blend(in V49 a, in V49 b, float t, out V49 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C49 : ITrack<T49, V49>
{
    [Feeds(typeof(K49))]
    public static float Evaluate(in Frame<T49, V49> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K50
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V50 { public float Amount; }
public struct T50 : IBlend<V50>
{
    public float Scale;
    public void Blend(in V50 a, in V50 b, float t, out V50 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C50 : ITrack<T50, V50>
{
    [Feeds(typeof(K50))]
    public static float Evaluate(in Frame<T50, V50> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K51
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V51 { public float Amount; }
public struct T51 : IBlend<V51>
{
    public float Scale;
    public void Blend(in V51 a, in V51 b, float t, out V51 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C51 : ITrack<T51, V51>
{
    [Feeds(typeof(K51))]
    public static float Evaluate(in Frame<T51, V51> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K52
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V52 { public float Amount; }
public struct T52 : IBlend<V52>
{
    public float Scale;
    public void Blend(in V52 a, in V52 b, float t, out V52 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C52 : ITrack<T52, V52>
{
    [Feeds(typeof(K52))]
    public static float Evaluate(in Frame<T52, V52> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K53
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V53 { public float Amount; }
public struct T53 : IBlend<V53>
{
    public float Scale;
    public void Blend(in V53 a, in V53 b, float t, out V53 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C53 : ITrack<T53, V53>
{
    [Feeds(typeof(K53))]
    public static float Evaluate(in Frame<T53, V53> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K54
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V54 { public float Amount; }
public struct T54 : IBlend<V54>
{
    public float Scale;
    public void Blend(in V54 a, in V54 b, float t, out V54 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C54 : ITrack<T54, V54>
{
    [Feeds(typeof(K54))]
    public static float Evaluate(in Frame<T54, V54> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K55
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V55 { public float Amount; }
public struct T55 : IBlend<V55>
{
    public float Scale;
    public void Blend(in V55 a, in V55 b, float t, out V55 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C55 : ITrack<T55, V55>
{
    [Feeds(typeof(K55))]
    public static float Evaluate(in Frame<T55, V55> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K56
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V56 { public float Amount; }
public struct T56 : IBlend<V56>
{
    public float Scale;
    public void Blend(in V56 a, in V56 b, float t, out V56 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C56 : ITrack<T56, V56>
{
    [Feeds(typeof(K56))]
    public static float Evaluate(in Frame<T56, V56> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K57
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V57 { public float Amount; }
public struct T57 : IBlend<V57>
{
    public float Scale;
    public void Blend(in V57 a, in V57 b, float t, out V57 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C57 : ITrack<T57, V57>
{
    [Feeds(typeof(K57))]
    public static float Evaluate(in Frame<T57, V57> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K58
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V58 { public float Amount; }
public struct T58 : IBlend<V58>
{
    public float Scale;
    public void Blend(in V58 a, in V58 b, float t, out V58 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C58 : ITrack<T58, V58>
{
    [Feeds(typeof(K58))]
    public static float Evaluate(in Frame<T58, V58> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K59
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V59 { public float Amount; }
public struct T59 : IBlend<V59>
{
    public float Scale;
    public void Blend(in V59 a, in V59 b, float t, out V59 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C59 : ITrack<T59, V59>
{
    [Feeds(typeof(K59))]
    public static float Evaluate(in Frame<T59, V59> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K60
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V60 { public float Amount; }
public struct T60 : IBlend<V60>
{
    public float Scale;
    public void Blend(in V60 a, in V60 b, float t, out V60 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C60 : ITrack<T60, V60>
{
    [Feeds(typeof(K60))]
    public static float Evaluate(in Frame<T60, V60> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

[TimelineKind]
public struct K61
{
    public static void Combine(ref float t, in float n) => t += n;
    public static void Apply(in float t, ref float fx) => fx += t;
}
public struct V61 { public float Amount; }
public struct T61 : IBlend<V61>
{
    public float Scale;
    public void Blend(in V61 a, in V61 b, float t, out V61 r) => r = a;
}
[TimelineSystemMember(typeof(KSys))]
public struct C61 : ITrack<T61, V61>
{
    [Feeds(typeof(K61))]
    public static float Evaluate(in Frame<T61, V61> f) => f.Direction * f.Clip.Amount * f.Track.Scale;
}

}
