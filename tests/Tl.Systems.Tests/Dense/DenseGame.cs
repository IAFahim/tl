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
    public float FSum;
    public int FBits;
    public int ISum;
    public int Top;
    public int Low;
    public uint Mask;
    public int Chosen;
    public int ChosenPriority;
    public float DriftX;
    public int DriftY;
    public int Blend;
    public int Pings;
    public int Hurt;
}

public struct PingClip { public int Value; }
public struct PingTrack : IBlend<PingClip>
{
    public float Scale;
    public void Blend(in PingClip first, in PingClip second, float factor, out PingClip result) => result = first;
}

[TimelineKind(Accepts = KindEntries.Pulses)]
public struct Ping
{
    public static void Combine(ref int total, in int next) => total = total * 7 + next;
    public static void Apply(in int total, ref Body body) => body.Pings = body.Pings * 13 + total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Combine = KindCombine.Sum)]
public struct Hurt
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Body body) => body.Hurt = body.Hurt * 31 + total;
}

[TimelineSystemMember(typeof(PingSystem))]
public struct Pinger : ITrack<PingTrack, PingClip>
{
    [Pulse(typeof(Ping), On = ClipEvent.Start)]
    public static int Hit(in Frame<PingTrack, PingClip> f) => f.Clip.Value + 7 * f.Tick;

    [Pulse(typeof(Hurt), On = ClipEvent.Start)]
    public static int Sting(in Frame<PingTrack, PingClip> f) => f.Direction * (f.Clip.Value + 11 * f.Tick) + 3;

    [Pulse(typeof(Hurt), On = ClipEvent.End)]
    public static int Burn(in Frame<PingTrack, PingClip> f) => 100 * f.Clip.Value - f.Direction * f.Tick;
}

[TimelineSystem]
public ref partial struct PingSystem
{
    public Span<Body> Body;
}

public struct Choice { public int Value; public short Priority; }

public struct Mix { public float X; public int Y; }

public struct LaneClip { public float F; public int I; public int Hi; public int Lo; public int Bits; public int Value; public int Priority; public float X; public int Y; }
public struct LaneTrack : IBlend<LaneClip>
{
    public float Scale;
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result) => result = factor < 0.5f ? first : second;
}

[TimelineKind(Combine = KindCombine.Sum)]
public struct FSum
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref Body body)
    {
        body.FSum = total;
        body.FBits = body.FBits * 31 + BitConverter.SingleToInt32Bits(total);
    }
}

[TimelineKind(Combine = KindCombine.Sum)]
public struct ISum
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Body body) => body.ISum = body.ISum * 7 + total;
}

[TimelineKind(Combine = KindCombine.Max)]
public struct Top
{
    public static void Combine(ref int total, in int next) => total = Math.Max(total, next);
    public static void Apply(in int total, ref Body body) => body.Top = body.Top * 3 + total;
}

[TimelineKind(Combine = KindCombine.Min)]
public struct Low
{
    public static void Combine(ref int total, in int next) { if (next < total) total = next; }
    public static void Apply(in int total, ref Body body) => body.Low = body.Low * 5 + total;
}

[TimelineKind(Combine = KindCombine.Or)]
public struct Mask
{
    public static void Combine(ref uint total, in uint next) => total |= next;
    public static void Apply(in uint total, ref Body body) => body.Mask = body.Mask * 33 + total;
}

[TimelineKind(Combine = KindCombine.Pick, Priority = nameof(Choice.Priority))]
public struct Choose
{
    public static void Combine(ref Choice total, in Choice next) { if (next.Priority >= total.Priority) total = next; }
    public static void Apply(in Choice total, ref Body body)
    {
        body.Chosen = body.Chosen * 9 + total.Value;
        body.ChosenPriority = total.Priority;
    }
}

[TimelineKind(Combine = KindCombine.Sum)]
public struct Drift
{
    public static void Combine(ref Mix total, in Mix next)
    {
        total.X += next.X;
        total.Y += next.Y;
    }

    public static void Apply(in Mix total, ref Body body)
    {
        body.DriftX = total.X;
        body.DriftY = body.DriftY * 3 + total.Y;
    }
}

[TimelineKind(Associative = true)]
public struct Blend
{
    public static void Combine(ref int total, in int next) => total = total * 3 + next;
    public static void Apply(in int total, ref Body body) => body.Blend = body.Blend * 11 + total;
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
    public static int Strike(in Frame<TallyTrack, TallyClip> f) => f.Direction * f.Clip.Hit + 3 * f.Tick;

    [Pulse(typeof(Marks), On = ClipEvent.End)]
    public static int Close(in Frame<TallyTrack, TallyClip> f) => f.Clip.Hit + 2 * f.Direction + 5 * f.Tick;
}

[TimelineSystemMember(typeof(LaneSystem))]
public struct Lane : ITrack<LaneTrack, LaneClip>
{
    [Feeds(typeof(FSum))]
    public static float F(in Frame<LaneTrack, LaneClip> f) => f.Clip.F;

    [Feeds(typeof(ISum))]
    public static int I(in Frame<LaneTrack, LaneClip> f) => f.Clip.I + f.Direction * f.WithinClip;

    [Feeds(typeof(Top))]
    public static int Hi(in Frame<LaneTrack, LaneClip> f) => f.Clip.Hi;

    [Feeds(typeof(Low))]
    public static int Lo(in Frame<LaneTrack, LaneClip> f) => f.Clip.Lo;

    [Feeds(typeof(Mask))]
    public static uint Bits(in Frame<LaneTrack, LaneClip> f) => (uint)f.Clip.Bits << (f.WithinClip & 3);

    [Feeds(typeof(Choose))]
    public static Choice Pick(in Frame<LaneTrack, LaneClip> f) => new Choice { Value = f.Clip.Value + f.Tick, Priority = (short)f.Clip.Priority };

    [Feeds(typeof(Drift))]
    public static Mix Move(in Frame<LaneTrack, LaneClip> f) => new Mix { X = f.Clip.X * f.Direction, Y = f.Clip.Y };

    [Feeds(typeof(Blend))]
    public static int Mixed(in Frame<LaneTrack, LaneClip> f) => f.Clip.I - f.Tick;
}

[TimelineSystem]
public ref partial struct LaneSystem
{
    public Span<Body> Body;
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
