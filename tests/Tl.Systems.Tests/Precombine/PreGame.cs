using System;
using Tl;

namespace PreGame
{

public struct Pre
{
    public float Lift;
    public int Count;
    public float Pick;
    public int PickPriority;
    public int Shown;
    public int Seen;
    public int Applied;
}

public struct Pick { public float Value; public int Priority; }

public struct RiseClip { public float Rise; public int Count; public float Speed; public int Priority; }
public struct RiseTrack : IBlend<RiseClip>
{
    public float Scale;
    public void Blend(in RiseClip first, in RiseClip second, float factor, out RiseClip result)
    {
        result = factor < 0.5f ? first : second;
        result.Rise = first.Rise + (second.Rise - first.Rise) * factor;
    }
}

public struct MarkClip { public int Line; }
public struct MarkTrack : IBlend<MarkClip>
{
    public float Scale;
    public void Blend(in MarkClip first, in MarkClip second, float factor, out MarkClip result) => result = first;
}

[TimelineKind(Associative = true)]
public struct Lift
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref Pre pre)
    {
        pre.Lift = total;
        pre.Applied++;
    }
}

[TimelineKind(Accepts = KindEntries.Both, Associative = true)]
public struct Count
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Pre pre) => pre.Count = total;
}

[TimelineKind(Associative = true)]
public struct Choice
{
    public static void Combine(ref Pick total, in Pick next) { if (next.Priority >= total.Priority) total = next; }
    public static void Apply(in Pick total, ref Pre pre)
    {
        pre.Pick = total.Value;
        pre.PickPriority = total.Priority;
    }
}

[TimelineKind(Associative = true)]
public struct Shown
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int total, ref Pre pre) => pre.Shown = total;
    public static void Idle(ref Pre pre) => pre.Shown = -1;
}

[TimelineKind(Associative = true)]
public struct Seen
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Pre pre) => pre.Seen += total;
}

public struct Rise : ITrack<RiseTrack, RiseClip>
{
    public static void Fold(in Frame<RiseTrack, RiseClip> f, out float rise) => rise = f.Direction * f.Clip.Rise * f.Track.Scale + f.WithinClip * 0.125f;

    [Feeds(typeof(Lift))]
    public static float Up(in float rise) => rise;

    [Feeds(typeof(Count))]
    public static int Steps(in Frame<RiseTrack, RiseClip> f) => f.Direction * (f.Clip.Count + f.Tick);

    [Pulse(typeof(Count), On = ClipEvent.Start)]
    public static int Begin(in Frame<RiseTrack, RiseClip> f) => 1000 * f.Direction;

    [Feeds(typeof(Choice))]
    public static Pick Choose(in Frame<RiseTrack, RiseClip> f) => new Pick { Value = f.Direction * f.Clip.Speed + f.WithinClip, Priority = f.Clip.Priority };

    [Feeds(typeof(Seen))]
    public static int Spot(in Frame<RiseTrack, RiseClip> f) => f.Has(FrameFlags.Synthetic) ? 1 : 0;
}

public struct Mark : ITrack<MarkTrack, MarkClip>
{
    [Feeds(typeof(Shown))]
    public static int Which(in Frame<MarkTrack, MarkClip> f) => f.Clip.Line * f.Direction;

    [Feeds(typeof(Lift))]
    public static float Nudge(in Frame<MarkTrack, MarkClip> f) => f.Clip.Line * 0.3f * f.Direction;
}

[TimelineSystem]
public ref partial struct PreSystem
{
    public Span<Pre> Pre;
}

}
