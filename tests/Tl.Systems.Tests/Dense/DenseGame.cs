using System;
using Tl;

namespace DenseGame
{

public struct Body
{
    public int Speed;
    public int SpeedPriority;
    public float Rise;
    public int Gain;
    public int Peak;
    public int Count;
    public int Hits;
    public int Marks;
    public int Applied;
}

public struct Pick { public int Value; public int Priority; }

public struct StepClip { public int Speed; public int Priority; public float Rise; }
public struct StepTrack : IBlend<StepClip>
{
    public float Scale;
    public void Blend(in StepClip first, in StepClip second, float factor, out StepClip result)
    {
        result = factor < 0.5f ? first : second;
        result.Rise = first.Rise + (second.Rise - first.Rise) * factor;
    }
}

public struct PushClip { public int Gain; }
public struct PushTrack : IBlend<PushClip>
{
    public float Scale;
    public void Blend(in PushClip first, in PushClip second, float factor, out PushClip result) => result = factor < 0.5f ? first : second;
}

public struct TallyClip { public int Count; public int Hit; }
public struct TallyTrack : IBlend<TallyClip>
{
    public float Scale;
    public void Blend(in TallyClip first, in TallyClip second, float factor, out TallyClip result) => result = first;
}

[TimelineKind(Associative = true)]
public struct Speed
{
    public static void Combine(ref Pick total, in Pick next) { if (next.Priority >= total.Priority) total = next; }
    public static void Apply(in Pick total, ref Body body)
    {
        body.Speed = total.Value;
        body.SpeedPriority = total.Priority;
        body.Applied++;
    }
}

[TimelineKind(Associative = true)]
public struct Rise
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref Body body) => body.Rise = body.Rise * 0.5f + total;
}

[TimelineKind(Associative = true)]
public struct Gain
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Body body) => body.Gain = body.Gain * 3 + total;
}

[TimelineKind(Associative = true)]
public struct Peak
{
    public static void Combine(ref int total, in int next) { if (next >= total) total = next; }
    public static void Apply(in int total, ref Body body) => body.Peak = body.Peak * 5 + total;
}

[TimelineKind(Accepts = KindEntries.Both, Associative = true)]
public struct Count
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Body body) => body.Count = body.Count * 7 + total;
}

[TimelineKind(Accepts = KindEntries.Pulses)]
public struct Hits
{
    public static void Combine(ref int total, in int next) => total = total * 3 + next;
    public static void Apply(in int total, ref Body body) => body.Hits = body.Hits * 5 + total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct Marks
{
    public static void Apply(in int delta, ref Body body) => body.Marks = body.Marks * 3 + delta;
}

[TimelineSystemMember(typeof(MoveSystem))]
public struct Walk : ITrack<StepTrack, StepClip>
{
    [Feeds(typeof(Speed))]
    public static Pick Choose(in Frame<StepTrack, StepClip> f) => new Pick { Value = f.Direction * f.Clip.Speed + f.Tick, Priority = f.Clip.Priority };

    public static void Fold(in Frame<StepTrack, StepClip> f, out float rise) => rise = f.Direction * f.Clip.Rise * f.Track.Scale + f.WithinClip * 0.125f;

    [Feeds(typeof(Rise))]
    public static float Up(in float rise) => rise;
}

[TimelineSystemMember(typeof(PushSystem))]
public struct Push : ITrack<PushTrack, PushClip>
{
    [Feeds(typeof(Gain))]
    public static int Add(in Frame<PushTrack, PushClip> f) => f.Direction * f.Clip.Gain + f.WithinClip;

    [Feeds(typeof(Peak))]
    public static int Top(in Frame<PushTrack, PushClip> f) => f.Clip.Gain - f.Tick;
}

[TimelineSystemMember(typeof(TallySystem))]
public struct Tally : ITrack<TallyTrack, TallyClip>
{
    [Feeds(typeof(Count))]
    public static int Steps(in Frame<TallyTrack, TallyClip> f) => f.Direction * (f.Clip.Count + f.Tick);

    [Pulse(typeof(Count), On = ClipEvent.Start)]
    public static int Begin(in Frame<TallyTrack, TallyClip> f) => 1000 * f.Direction;

    [Pulse(typeof(Hits), On = ClipEvent.Start)]
    public static int Strike(in Frame<TallyTrack, TallyClip> f) => f.Direction * f.Clip.Hit;

    [Pulse(typeof(Marks), On = ClipEvent.End)]
    public static int Close(in Frame<TallyTrack, TallyClip> f) => f.Clip.Hit + 2 * f.Direction;
}

[TimelineSystem]
public ref partial struct MoveSystem
{
    public Span<Body> Body;
}

[TimelineSystem]
public ref partial struct PushSystem
{
    public Span<Body> Body;
}

[TimelineSystem]
public ref partial struct TallySystem
{
    public Span<Body> Body;
}

}
