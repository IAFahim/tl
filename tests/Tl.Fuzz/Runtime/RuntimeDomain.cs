namespace Tl.Fuzz.RuntimeDomain;

public struct PlainMotion
{
    public int X;
    public float FX;
}

public struct PlainStats
{
    public int Starts;
    public int Ends;
    public int Interrupts;
    public int BatchHits;
    public int Markers;
}

public struct PlainInput
{
    public int Mult;
}

public struct FuzzCue
{
    public int Tick;
    public int Tag;
    public int Direction;
}

public struct FuzzPlainClip
{
    public int Delta;
    public int Tag;
    public ushort TriggerTimeline;
}

public struct FuzzPlainTrack : IBlend<FuzzPlainClip>
{
    public int Gain;

    public void Blend(in FuzzPlainClip first, in FuzzPlainClip second, float factor, out FuzzPlainClip result)
        => result = first;
}

public struct FuzzBatchClip
{
    public int Weight;
}

public struct FuzzBatchTrack : IBlend<FuzzBatchClip>
{
    public int Bias;

    public void Blend(in FuzzBatchClip first, in FuzzBatchClip second, float factor, out FuzzBatchClip result)
        => result = first;
}

public struct FuzzPlainMarker
{
    public int Code;
}

[TimelineSystemMember(typeof(PlainFuzzSystem))]
public readonly struct FuzzPlainConsumer : ITrack<FuzzPlainTrack, FuzzPlainClip>
{
    public static void Fold(in Frame<FuzzPlainTrack, FuzzPlainClip> frame, out int delta)
        => delta = frame.Direction * frame.Clip.Delta * frame.Track.Gain;

    public static void ExecuteActive(
        in Frame<FuzzPlainTrack, FuzzPlainClip> frame,
        in int delta,
        ref PlainMotion motion,
        in PlainInput input,
        ref Requests requests,
        ref Events<FuzzCue> cues)
    {
        motion.X += delta * input.Mult;
        motion.FX += delta * 0.25f;
        if (frame.Clip.TriggerTimeline != 0 && frame.Direction > 0 && frame.WithinClip == 0)
            requests.Start(frame.Clip.TriggerTimeline);
        if ((frame.Clip.Tag & 1) != 0)
            cues.Raise(new FuzzCue { Tick = frame.TimelineTick, Tag = frame.Clip.Tag, Direction = frame.Direction });
    }

    public static void OnClipStart(in Frame<FuzzPlainTrack, FuzzPlainClip> frame, ref PlainStats stats, ref Events<FuzzCue> cues)
    {
        stats.Starts += frame.Direction;
        cues.Raise(new FuzzCue { Tick = frame.TimelineTick, Tag = 1000 + frame.Clip.Tag, Direction = frame.Direction });
    }

    public static void OnClipEnd(in Frame<FuzzPlainTrack, FuzzPlainClip> frame, ref PlainStats stats, ref Events<FuzzCue> cues)
    {
        stats.Ends += frame.Direction;
        if (frame.Has(FrameFlags.Interrupted))
            stats.Interrupts++;
        cues.Raise(new FuzzCue
        {
            Tick = frame.TimelineTick,
            Tag = frame.Has(FrameFlags.Interrupted) ? 3000 + frame.Clip.Tag : 2000 + frame.Clip.Tag,
            Direction = frame.Direction,
        });
    }
}

[TimelineSystemMember(typeof(PlainFuzzSystem))]
public readonly struct FuzzBatchConsumer : ITrack<FuzzBatchTrack, FuzzBatchClip>
{
    public static void Fold(in Frame<FuzzBatchTrack, FuzzBatchClip> frame, out int weight)
        => weight = frame.Direction * (frame.Clip.Weight + frame.Track.Bias);

    public static void ExecuteActive(in Frame<FuzzBatchTrack, FuzzBatchClip> frame, in int weight, ref PlainStats stats)
        => stats.BatchHits += weight;

    public static void OnClipStart(in Frame<FuzzBatchTrack, FuzzBatchClip> frame)
    {
    }
}

[TimelineSystemMember(typeof(PlainFuzzSystem))]
public readonly struct FuzzMarkerConsumer : IMarker<FuzzPlainMarker>
{
    public static void OnMarker(in MarkerFrame<FuzzPlainMarker> frame, ref PlainStats stats, ref Events<FuzzCue> cues)
    {
        stats.Markers += 1;
        cues.Raise(new FuzzCue { Tick = frame.Tick, Tag = 4000 + frame.Marker.Code, Direction = frame.Direction });
    }
}

[TimelineSystem]
public ref partial struct PlainFuzzSystem
{
    public Span<PlainMotion> Motion;
    public Span<PlainStats> Stats;
    public ReadOnlySpan<PlainInput> Input;
    public Requests Requests;
    public Events<FuzzCue> Cues;
}

public struct KindIntCol
{
    public int Sum;
}

public struct KindEachCol
{
    public int Sum;
}

public struct KindIdleCol
{
    public int Value;
}

public struct KindFloatCol
{
    public float Value;
}

public struct KindInputCol
{
    public int Factor;
}

public struct KCombineClip
{
    public int Delta;
    public int StartPulse;
    public int EndPulse;
}

public struct KCombineTrack : IBlend<KCombineClip>
{
    public int Scale;

    public void Blend(in KCombineClip first, in KCombineClip second, float factor, out KCombineClip result)
        => result = first;
}

public struct KEachClip
{
    public int Delta;
    public int StartPulse;
    public int EndPulse;
}

public struct KEachTrack : IBlend<KEachClip>
{
    public int Scale;

    public void Blend(in KEachClip first, in KEachClip second, float factor, out KEachClip result)
        => result = first;
}

public struct KIdleClip
{
    public int Code;
}

public struct KIdleTrack : IBlend<KIdleClip>
{
    public int Bias;

    public void Blend(in KIdleClip first, in KIdleClip second, float factor, out KIdleClip result)
        => result = first;
}

public struct KFloatClip
{
    public float Delta;
}

public struct KFloatTrack : IBlend<KFloatClip>
{
    public float Scale;

    public void Blend(in KFloatClip first, in KFloatClip second, float factor, out KFloatClip result)
        => result = first;
}

public struct KPulseMarker
{
    public int Delta;
}

public struct KEachMarker
{
    public int Delta;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct KCombineInt
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref KindIntCol col) => col.Sum += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Each)]
public struct KEachInt
{
    public static void Apply(in int delta, ref KindEachCol col) => col.Sum += delta;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine)]
public struct KIdleInt
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int code, ref KindIdleCol col) => col.Value = code;
    public static void Idle(ref KindIdleCol col) => col.Value = -777;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine)]
public struct KCombineFloat
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref KindFloatCol col) => col.Value += total;
}

[TimelineSystemMember(typeof(KindFuzzSystem))]
public readonly struct KCombineConsumer : ITrack<KCombineTrack, KCombineClip>
{
    [Feeds(typeof(KCombineInt))]
    public static int Evaluate(in Frame<KCombineTrack, KCombineClip> frame, in KindInputCol input)
        => frame.Direction * frame.Clip.Delta * frame.Track.Scale * input.Factor;

    [Pulse(typeof(KCombineInt), On = ClipEvent.Start)]
    public static int OnStart(in Frame<KCombineTrack, KCombineClip> frame)
        => frame.Direction * frame.Clip.StartPulse;

    [Pulse(typeof(KCombineInt), On = ClipEvent.End)]
    public static int OnEnd(in Frame<KCombineTrack, KCombineClip> frame)
        => frame.Direction * frame.Clip.EndPulse;
}

[TimelineSystemMember(typeof(KindFuzzSystem))]
public readonly struct KEachConsumer : ITrack<KEachTrack, KEachClip>
{
    [Feeds(typeof(KEachInt))]
    public static int Evaluate(in Frame<KEachTrack, KEachClip> frame)
        => frame.Direction * frame.Clip.Delta * frame.Track.Scale;

    [Pulse(typeof(KEachInt), On = ClipEvent.Start)]
    public static int OnStart(in Frame<KEachTrack, KEachClip> frame)
        => frame.Direction * frame.Clip.StartPulse;

    [Pulse(typeof(KEachInt), On = ClipEvent.End)]
    public static int OnEnd(in Frame<KEachTrack, KEachClip> frame)
        => frame.Direction * frame.Clip.EndPulse;
}

[TimelineSystemMember(typeof(KindFuzzSystem))]
public readonly struct KIdleConsumer : ITrack<KIdleTrack, KIdleClip>
{
    [Feeds(typeof(KIdleInt))]
    public static int Evaluate(in Frame<KIdleTrack, KIdleClip> frame)
        => frame.Clip.Code + frame.Track.Bias;
}

[TimelineSystemMember(typeof(KindFuzzSystem))]
public readonly struct KFloatConsumer : ITrack<KFloatTrack, KFloatClip>
{
    [Feeds(typeof(KCombineFloat))]
    public static float Evaluate(in Frame<KFloatTrack, KFloatClip> frame)
        => frame.Direction * frame.Clip.Delta * frame.Track.Scale;
}

[TimelineSystemMember(typeof(KindFuzzSystem))]
public readonly struct KPulseMarkerConsumer : IMarker<KPulseMarker>
{
    [Pulse(typeof(KCombineInt), On = ClipEvent.Marker)]
    public static int OnStep(in MarkerFrame<KPulseMarker> frame)
        => frame.Direction * frame.Marker.Delta;
}

[TimelineSystem]
public ref partial struct KindFuzzSystem
{
    public Span<KindIntCol> CombineCol;
    public Span<KindEachCol> EachCol;
    public Span<KindIdleCol> IdleCol;
    public Span<KindFloatCol> FloatCol;
    public ReadOnlySpan<KindInputCol> Input;
}

public struct IntAddFold : IPulseFold<int>
{
    public void Fold(ref int total, in int next) => total += next;
}

public struct FoldPulseOut { public int Value; }
public struct UnfoldPulseOut { public int Value; }

public struct FoldPulseClip { public int SpanVal; public int PulseVal; }
public struct FoldPulseTrack : IBlend<FoldPulseClip>
{
    public void Blend(in FoldPulseClip first, in FoldPulseClip second, float factor, out FoldPulseClip result) => result = first;
}
public struct UnfoldPulseClip { public int SpanVal; public int PulseVal; }
public struct UnfoldPulseTrack : IBlend<UnfoldPulseClip>
{
    public void Blend(in UnfoldPulseClip first, in UnfoldPulseClip second, float factor, out UnfoldPulseClip result) => result = first;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = true)]
public struct FoldedPulseKind
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref FoldPulseOut col) => col.Value += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = false)]
public struct UnfoldedPulseKind
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref UnfoldPulseOut col) => col.Value += total;
}

[TimelineSystemMember(typeof(FoldedPulseSystem))]
public readonly struct FoldedPulseConsumer : ITrack<FoldPulseTrack, FoldPulseClip>
{
    [Feeds(typeof(FoldedPulseKind))]
    public static int Evaluate(in Frame<FoldPulseTrack, FoldPulseClip> frame) => frame.Direction * frame.Clip.SpanVal;

    [Pulse(typeof(FoldedPulseKind), On = ClipEvent.Start)]
    public static int OnStart(in Frame<FoldPulseTrack, FoldPulseClip> frame) => frame.Direction * frame.Clip.PulseVal;
}

[TimelineSystemMember(typeof(UnfoldedPulseSystem))]
public readonly struct UnfoldedPulseConsumer : ITrack<UnfoldPulseTrack, UnfoldPulseClip>
{
    [Feeds(typeof(UnfoldedPulseKind))]
    public static int Evaluate(in Frame<UnfoldPulseTrack, UnfoldPulseClip> frame) => frame.Direction * frame.Clip.SpanVal;

    [Pulse(typeof(UnfoldedPulseKind), On = ClipEvent.Start)]
    public static int OnStart(in Frame<UnfoldPulseTrack, UnfoldPulseClip> frame) => frame.Direction * frame.Clip.PulseVal;
}

[TimelineSystem]
public ref partial struct FoldedPulseSystem
{
    public Span<FoldPulseOut> Out;
}

[TimelineSystem]
public ref partial struct UnfoldedPulseSystem
{
    public Span<UnfoldPulseOut> Out;
}

public struct GrpOutA { public int V; }
public struct GrpOutB { public int V; }
public struct UngrpOutA { public int V; }
public struct UngrpOutB { public int V; }

public struct GrpClipA { public int Delta; public int Pulse; }
public struct GrpTrackA : IBlend<GrpClipA> { public void Blend(in GrpClipA a, in GrpClipA b, float t, out GrpClipA r) => r = a; }
public struct GrpClipB { public int Delta; public int Pulse; }
public struct GrpTrackB : IBlend<GrpClipB> { public void Blend(in GrpClipB a, in GrpClipB b, float t, out GrpClipB r) => r = a; }

public struct UngrpClipA { public int Delta; public int Pulse; }
public struct UngrpTrackA : IBlend<UngrpClipA> { public void Blend(in UngrpClipA a, in UngrpClipA b, float t, out UngrpClipA r) => r = a; }
public struct UngrpClipB { public int Delta; public int Pulse; }
public struct UngrpTrackB : IBlend<UngrpClipB> { public void Blend(in UngrpClipB a, in UngrpClipB b, float t, out UngrpClipB r) => r = a; }

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "FuzzGrp")]
public struct GrpKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref GrpOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "FuzzGrp")]
public struct GrpKindB
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int total, ref GrpOutB col) => col.V = total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct UngrpKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref UngrpOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct UngrpKindB
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int total, ref UngrpOutB col) => col.V = total;
}

[TimelineSystemMember(typeof(GroupedFuzzSystem))]
public readonly struct GrpConsumerA : ITrack<GrpTrackA, GrpClipA>
{
    [Feeds(typeof(GrpKindA))]
    public static int Eval(in Frame<GrpTrackA, GrpClipA> f) => f.Direction * f.Clip.Delta;
    [Pulse(typeof(GrpKindA), On = ClipEvent.Start)]
    public static int Start(in Frame<GrpTrackA, GrpClipA> f) => f.Direction * f.Clip.Pulse;
}

[TimelineSystemMember(typeof(GroupedFuzzSystem))]
public readonly struct GrpConsumerB : ITrack<GrpTrackB, GrpClipB>
{
    [Feeds(typeof(GrpKindB))]
    public static int Eval(in Frame<GrpTrackB, GrpClipB> f) => f.Clip.Delta;
    [Pulse(typeof(GrpKindB), On = ClipEvent.End)]
    public static int End(in Frame<GrpTrackB, GrpClipB> f) => f.Clip.Pulse;
}

[TimelineSystemMember(typeof(UngroupedFuzzSystem))]
public readonly struct UngrpConsumerA : ITrack<UngrpTrackA, UngrpClipA>
{
    [Feeds(typeof(UngrpKindA))]
    public static int Eval(in Frame<UngrpTrackA, UngrpClipA> f) => f.Direction * f.Clip.Delta;
    [Pulse(typeof(UngrpKindA), On = ClipEvent.Start)]
    public static int Start(in Frame<UngrpTrackA, UngrpClipA> f) => f.Direction * f.Clip.Pulse;
}

[TimelineSystemMember(typeof(UngroupedFuzzSystem))]
public readonly struct UngrpConsumerB : ITrack<UngrpTrackB, UngrpClipB>
{
    [Feeds(typeof(UngrpKindB))]
    public static int Eval(in Frame<UngrpTrackB, UngrpClipB> f) => f.Clip.Delta;
    [Pulse(typeof(UngrpKindB), On = ClipEvent.End)]
    public static int End(in Frame<UngrpTrackB, UngrpClipB> f) => f.Clip.Pulse;
}

[TimelineSystem]
public ref partial struct GroupedFuzzSystem
{
    public Span<GrpOutA> A;
    public Span<GrpOutB> B;
}

[TimelineSystem]
public ref partial struct UngroupedFuzzSystem
{
    public Span<UngrpOutA> A;
    public Span<UngrpOutB> B;
}

[TlState]
public partial struct FuzzSumState { public int A; public float B; }

[TlState]
public partial struct FuzzSetState { public int Mode; public float Angle; }

[TlState]
public partial struct FuzzWhileState { public bool On; public int Level; public float Power; }

public struct HandSumState { public int A; public float B; }
public struct HandSetState { public int Mode; public float Angle; }
public struct HandWhileState { public bool On; public int Level; public float Power; }

[TlClip("Fuzz/DiffSum")]
public struct DiffSumClip { public int A; public float B; public int PulseA; public float PulseB; }
[TlClip("Fuzz/DiffSet")]
public struct DiffSetClip { public int Mode; public float Angle; public int SnapMode; public float SnapAngle; }
[TlClip("Fuzz/DiffWhile")]
public struct DiffWhileClip { public int Level; public float Power; }

[TlClip("Fuzz/HandSum")]
public struct HandSumClip { public int A; public float B; public int PulseA; public float PulseB; }
[TlClip("Fuzz/HandSet")]
public struct HandSetClip { public int Mode; public float Angle; public int SnapMode; public float SnapAngle; }
[TlClip("Fuzz/HandWhile")]
public struct HandWhileClip { public int Level; public float Power; }

[TlKind(Accepts = KindEntries.Both)]
public struct HandSumKind
{
    public static void Combine(ref HandSumState total, in HandSumState next)
    {
        total.A += next.A;
        total.B += next.B;
    }

    public static void Apply(in HandSumState total, ref HandSumState col)
    {
        col.A += total.A;
        col.B += total.B;
    }
}

[TlKind(Accepts = KindEntries.Both)]
public struct HandSetKind
{
    public static void Combine(ref HandSetState total, in HandSetState next) => total = next;
    public static void Apply(in HandSetState total, ref HandSetState col) => col = total;
}

[TlKind(Accepts = KindEntries.Spans)]
public struct HandWhileKind
{
    public static void Combine(ref HandWhileState total, in HandWhileState next)
    {
        total.On |= next.On;
        total.Level |= next.Level;
        total.Power = Math.Max(total.Power, next.Power);
    }

    public static void Apply(in HandWhileState total, ref HandWhileState col) => col = total;
    public static void Idle(ref HandWhileState col) => col = default;
}

[TimelineSystemMember(typeof(DerivedDiffSystem))]
public struct DiffSumConsumer : ITlTrack<DiffSumClip>
{
    [Feeds(typeof(Sum<FuzzSumState>))]
    public static FuzzSumState Eval(in TlFrame<DiffSumClip> f)
        => new() { A = f.Direction * f.Clip.A, B = f.Direction * f.Clip.B };

    [Pulse(typeof(Sum<FuzzSumState>), On = ClipEvent.Start)]
    public static FuzzSumState Start(in TlFrame<DiffSumClip> f)
        => new() { A = f.Direction * f.Clip.PulseA, B = f.Direction * f.Clip.PulseB };
}

[TimelineSystemMember(typeof(DerivedDiffSystem))]
public struct DiffSetConsumer : ITlTrack<DiffSetClip>
{
    [Feeds(typeof(Set<FuzzSetState>))]
    public static FuzzSetState Eval(in TlFrame<DiffSetClip> f)
        => new() { Mode = f.Clip.Mode, Angle = f.Clip.Angle };

    [Pulse(typeof(Set<FuzzSetState>), On = ClipEvent.Start)]
    public static FuzzSetState Start(in TlFrame<DiffSetClip> f)
        => new() { Mode = f.Clip.SnapMode, Angle = f.Clip.SnapAngle };
}

[TimelineSystemMember(typeof(DerivedDiffSystem))]
public struct DiffWhileConsumer : ITlTrack<DiffWhileClip>
{
    [Feeds(typeof(While<FuzzWhileState>))]
    public static FuzzWhileState Eval(in TlFrame<DiffWhileClip> f)
        => new() { On = true, Level = f.Clip.Level, Power = f.Clip.Power };
}

[TimelineSystemMember(typeof(HandDiffSystem))]
public struct HandSumConsumer : ITlTrack<HandSumClip>
{
    [Feeds(typeof(HandSumKind))]
    public static HandSumState Eval(in TlFrame<HandSumClip> f)
        => new() { A = f.Direction * f.Clip.A, B = f.Direction * f.Clip.B };

    [Pulse(typeof(HandSumKind), On = ClipEvent.Start)]
    public static HandSumState Start(in TlFrame<HandSumClip> f)
        => new() { A = f.Direction * f.Clip.PulseA, B = f.Direction * f.Clip.PulseB };
}

[TimelineSystemMember(typeof(HandDiffSystem))]
public struct HandSetConsumer : ITlTrack<HandSetClip>
{
    [Feeds(typeof(HandSetKind))]
    public static HandSetState Eval(in TlFrame<HandSetClip> f)
        => new() { Mode = f.Clip.Mode, Angle = f.Clip.Angle };

    [Pulse(typeof(HandSetKind), On = ClipEvent.Start)]
    public static HandSetState Start(in TlFrame<HandSetClip> f)
        => new() { Mode = f.Clip.SnapMode, Angle = f.Clip.SnapAngle };
}

[TimelineSystemMember(typeof(HandDiffSystem))]
public struct HandWhileConsumer : ITlTrack<HandWhileClip>
{
    [Feeds(typeof(HandWhileKind))]
    public static HandWhileState Eval(in TlFrame<HandWhileClip> f)
        => new() { On = true, Level = f.Clip.Level, Power = f.Clip.Power };
}

[TimelineSystem]
public ref partial struct DerivedDiffSystem
{
    public Span<FuzzSumState> Sum;
    public Span<FuzzSetState> Set;
    public Span<FuzzWhileState> While;
}

[TimelineSystem]
public ref partial struct HandDiffSystem
{
    public Span<HandSumState> Sum;
    public Span<HandSetState> Set;
    public Span<HandWhileState> While;
}

public struct SpanGrpOutA { public int V; }
public struct SpanGrpOutB { public int V; }
public struct SpanUngrpOutA { public int V; }
public struct SpanUngrpOutB { public int V; }

public struct SpanGrpClipA { public int Delta; }
public struct SpanGrpTrackA : IBlend<SpanGrpClipA> { public void Blend(in SpanGrpClipA a, in SpanGrpClipA b, float t, out SpanGrpClipA r) => r = a; }
public struct SpanGrpClipB { public int Delta; }
public struct SpanGrpTrackB : IBlend<SpanGrpClipB> { public void Blend(in SpanGrpClipB a, in SpanGrpClipB b, float t, out SpanGrpClipB r) => r = a; }

public struct SpanUngrpClipA { public int Delta; }
public struct SpanUngrpTrackA : IBlend<SpanUngrpClipA> { public void Blend(in SpanUngrpClipA a, in SpanUngrpClipA b, float t, out SpanUngrpClipA r) => r = a; }
public struct SpanUngrpClipB { public int Delta; }
public struct SpanUngrpTrackB : IBlend<SpanUngrpClipB> { public void Blend(in SpanUngrpClipB a, in SpanUngrpClipB b, float t, out SpanUngrpClipB r) => r = a; }

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "FuzzSpanGrp")]
public struct GrpSpanKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref SpanGrpOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "FuzzSpanGrp")]
public struct GrpSpanKindB
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int total, ref SpanGrpOutB col) => col.V = total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct UngrpSpanKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref SpanUngrpOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct UngrpSpanKindB
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int total, ref SpanUngrpOutB col) => col.V = total;
}

[TimelineSystemMember(typeof(GroupedSpanFuzzSystem))]
public readonly struct GrpSpanConsumerA : ITrack<SpanGrpTrackA, SpanGrpClipA>
{
    [Feeds(typeof(GrpSpanKindA))]
    public static int Eval(in Frame<SpanGrpTrackA, SpanGrpClipA> f) => f.Direction * f.Clip.Delta;
}

[TimelineSystemMember(typeof(GroupedSpanFuzzSystem))]
public readonly struct GrpSpanConsumerB : ITrack<SpanGrpTrackB, SpanGrpClipB>
{
    [Feeds(typeof(GrpSpanKindB))]
    public static int Eval(in Frame<SpanGrpTrackB, SpanGrpClipB> f) => f.Clip.Delta;
}

[TimelineSystemMember(typeof(UngroupedSpanFuzzSystem))]
public readonly struct UngrpSpanConsumerA : ITrack<SpanUngrpTrackA, SpanUngrpClipA>
{
    [Feeds(typeof(UngrpSpanKindA))]
    public static int Eval(in Frame<SpanUngrpTrackA, SpanUngrpClipA> f) => f.Direction * f.Clip.Delta;
}

[TimelineSystemMember(typeof(UngroupedSpanFuzzSystem))]
public readonly struct UngrpSpanConsumerB : ITrack<SpanUngrpTrackB, SpanUngrpClipB>
{
    [Feeds(typeof(UngrpSpanKindB))]
    public static int Eval(in Frame<SpanUngrpTrackB, SpanUngrpClipB> f) => f.Clip.Delta;
}

[TimelineSystem]
public ref partial struct GroupedSpanFuzzSystem
{
    public Span<SpanGrpOutA> A;
    public Span<SpanGrpOutB> B;
}

[TimelineSystem]
public ref partial struct UngroupedSpanFuzzSystem
{
    public Span<SpanUngrpOutA> A;
    public Span<SpanUngrpOutB> B;
}

public struct B3SplitOutA { public int V; }
public struct B3SplitOutB { public int V; }
public struct B3SplitOutC { public int V; }
public struct B3CombOutA { public int V; }
public struct B3CombOutB { public int V; }
public struct B3CombOutC { public int V; }
public struct B3UngrpOutA { public int V; }
public struct B3UngrpOutB { public int V; }
public struct B3UngrpOutC { public int V; }

public struct B3SplitClipA { public int SpanVal; public int PulseVal; }
public struct B3SplitTrackA : IBlend<B3SplitClipA> { public void Blend(in B3SplitClipA a, in B3SplitClipA b, float t, out B3SplitClipA r) => r = a; }
public struct B3SplitClipB { public int SpanVal; public int PulseVal; }
public struct B3SplitTrackB : IBlend<B3SplitClipB> { public void Blend(in B3SplitClipB a, in B3SplitClipB b, float t, out B3SplitClipB r) => r = a; }
public struct B3SplitClipC { public int PulseVal; }
public struct B3SplitTrackC : IBlend<B3SplitClipC> { public void Blend(in B3SplitClipC a, in B3SplitClipC b, float t, out B3SplitClipC r) => r = a; }

public struct B3CombClipA { public int SpanVal; public int PulseVal; }
public struct B3CombTrackA : IBlend<B3CombClipA> { public void Blend(in B3CombClipA a, in B3CombClipA b, float t, out B3CombClipA r) => r = a; }
public struct B3CombClipB { public int SpanVal; public int PulseVal; }
public struct B3CombTrackB : IBlend<B3CombClipB> { public void Blend(in B3CombClipB a, in B3CombClipB b, float t, out B3CombClipB r) => r = a; }
public struct B3CombClipC { public int PulseVal; }
public struct B3CombTrackC : IBlend<B3CombClipC> { public void Blend(in B3CombClipC a, in B3CombClipC b, float t, out B3CombClipC r) => r = a; }

public struct B3UngrpClipA { public int SpanVal; public int PulseVal; }
public struct B3UngrpTrackA : IBlend<B3UngrpClipA> { public void Blend(in B3UngrpClipA a, in B3UngrpClipA b, float t, out B3UngrpClipA r) => r = a; }
public struct B3UngrpClipB { public int SpanVal; public int PulseVal; }
public struct B3UngrpTrackB : IBlend<B3UngrpClipB> { public void Blend(in B3UngrpClipB a, in B3UngrpClipB b, float t, out B3UngrpClipB r) => r = a; }
public struct B3UngrpClipC { public int PulseVal; }
public struct B3UngrpTrackC : IBlend<B3UngrpClipC> { public void Blend(in B3UngrpClipC a, in B3UngrpClipC b, float t, out B3UngrpClipC r) => r = a; }

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "B3SplitGrp")]
public struct B3SplitKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3SplitOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = true, Group = "B3SplitGrp")]
public struct B3SplitKindB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3SplitOutB col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each, Group = "B3SplitGrp")]
public struct B3SplitKindC
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int value, ref B3SplitOutC col) => col.V += value;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "B3CombGrp")]
public struct B3CombKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3CombOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = true, Group = "B3CombGrp")]
public struct B3CombKindB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3CombOutB col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each, Group = "B3CombGrp")]
public struct B3CombKindC
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int value, ref B3CombOutC col) => col.V += value;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct B3UngrpKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3UngrpOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = true)]
public struct B3UngrpKindB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3UngrpOutB col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct B3UngrpKindC
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int value, ref B3UngrpOutC col) => col.V += value;
}

[TimelineSystemMember(typeof(B3SplitSysA))]
public readonly struct B3SplitConA : ITrack<B3SplitTrackA, B3SplitClipA>
{
    [Feeds(typeof(B3SplitKindA))]
    public static int Eval(in Frame<B3SplitTrackA, B3SplitClipA> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3SplitKindA), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3SplitTrackA, B3SplitClipA> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3SplitSysB))]
public readonly struct B3SplitConB : ITrack<B3SplitTrackB, B3SplitClipB>
{
    [Feeds(typeof(B3SplitKindB))]
    public static int Eval(in Frame<B3SplitTrackB, B3SplitClipB> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3SplitKindB), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3SplitTrackB, B3SplitClipB> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3SplitSysC))]
public readonly struct B3SplitConC : ITrack<B3SplitTrackC, B3SplitClipC>
{
    [Pulse(typeof(B3SplitKindC), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3SplitTrackC, B3SplitClipC> f) => f.Direction * f.Clip.PulseVal;
    [Pulse(typeof(B3SplitKindC), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3SplitTrackC, B3SplitClipC> f) => f.Direction * (f.Clip.PulseVal + 3);
}

[TimelineSystemMember(typeof(B3CombSys))]
public readonly struct B3CombConA : ITrack<B3CombTrackA, B3CombClipA>
{
    [Feeds(typeof(B3CombKindA))]
    public static int Eval(in Frame<B3CombTrackA, B3CombClipA> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3CombKindA), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3CombTrackA, B3CombClipA> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3CombSys))]
public readonly struct B3CombConB : ITrack<B3CombTrackB, B3CombClipB>
{
    [Feeds(typeof(B3CombKindB))]
    public static int Eval(in Frame<B3CombTrackB, B3CombClipB> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3CombKindB), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3CombTrackB, B3CombClipB> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3CombSys))]
public readonly struct B3CombConC : ITrack<B3CombTrackC, B3CombClipC>
{
    [Pulse(typeof(B3CombKindC), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3CombTrackC, B3CombClipC> f) => f.Direction * f.Clip.PulseVal;
    [Pulse(typeof(B3CombKindC), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3CombTrackC, B3CombClipC> f) => f.Direction * (f.Clip.PulseVal + 3);
}

[TimelineSystemMember(typeof(B3UngrpSysA))]
public readonly struct B3UngrpConA : ITrack<B3UngrpTrackA, B3UngrpClipA>
{
    [Feeds(typeof(B3UngrpKindA))]
    public static int Eval(in Frame<B3UngrpTrackA, B3UngrpClipA> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3UngrpKindA), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3UngrpTrackA, B3UngrpClipA> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3UngrpSysB))]
public readonly struct B3UngrpConB : ITrack<B3UngrpTrackB, B3UngrpClipB>
{
    [Feeds(typeof(B3UngrpKindB))]
    public static int Eval(in Frame<B3UngrpTrackB, B3UngrpClipB> f) => f.Direction * f.Clip.SpanVal;
    [Pulse(typeof(B3UngrpKindB), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3UngrpTrackB, B3UngrpClipB> f) => f.Direction * f.Clip.PulseVal;
}

[TimelineSystemMember(typeof(B3UngrpSysC))]
public readonly struct B3UngrpConC : ITrack<B3UngrpTrackC, B3UngrpClipC>
{
    [Pulse(typeof(B3UngrpKindC), On = ClipEvent.Start)]
    public static int OnStart(in Frame<B3UngrpTrackC, B3UngrpClipC> f) => f.Direction * f.Clip.PulseVal;
    [Pulse(typeof(B3UngrpKindC), On = ClipEvent.End)]
    public static int OnEnd(in Frame<B3UngrpTrackC, B3UngrpClipC> f) => f.Direction * (f.Clip.PulseVal + 3);
}

[TimelineSystem] public ref partial struct B3SplitSysA { public Span<B3SplitOutA> A; }
[TimelineSystem] public ref partial struct B3SplitSysB { public Span<B3SplitOutB> B; }
[TimelineSystem] public ref partial struct B3SplitSysC { public Span<B3SplitOutC> C; }
[TimelineSystem] public ref partial struct B3CombSys { public Span<B3CombOutA> A; public Span<B3CombOutB> B; public Span<B3CombOutC> C; }
[TimelineSystem] public ref partial struct B3UngrpSysA { public Span<B3UngrpOutA> A; }
[TimelineSystem] public ref partial struct B3UngrpSysB { public Span<B3UngrpOutB> B; }
[TimelineSystem] public ref partial struct B3UngrpSysC { public Span<B3UngrpOutC> C; }
