using Tl;

namespace ManyEntities;

public readonly record struct MoveTrack(float Mult) : IBlend<MoveClip>
{
    public void Blend(in MoveClip first, in MoveClip second, float factor, out MoveClip result) => result = first;
}
public readonly record struct MoveClip(int Amount);

public readonly record struct PulseTrack(int Power) : IBlend<PulseClip>
{
    public void Blend(in PulseClip first, in PulseClip second, float factor, out PulseClip result) => result = first;
}
public readonly record struct PulseClip(int Amount);

public readonly record struct WindowTrack(int Power) : IBlend<WindowClip>
{
    public void Blend(in WindowClip first, in WindowClip second, float factor, out WindowClip result) => result = first;
}
public readonly record struct WindowClip(int Amount);

public struct MoveJob : ITimelineJob<MoveTrack, MoveClip>
{
    public static void Execute(in Frame<MoveTrack, MoveClip> f, ref float value)
        => value += f.Clip.Amount * f.Track.Mult + f.TimelineTick;
}

public struct PulseJob : ITimelineJob<PulseTrack, PulseClip>
{
    public static void Execute(in Frame<PulseTrack, PulseClip> f, ref float value)
        => value += f.Clip.Amount * f.Track.Power;
}

public struct WindowJob : ITimelineJob<WindowTrack, WindowClip>
{
    public static void Execute(in Frame<WindowTrack, WindowClip> f, ref float value)
        => value += f.Clip.Amount * f.Track.Power;
}
