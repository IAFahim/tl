using Tl;
using System.Diagnostics.CodeAnalysis;

namespace Tlb;

public readonly struct JobClip(float amount, int steps)
{
    public readonly float Amount = amount;
    public readonly int Steps = steps;
}

public readonly struct JobTrack(float multiplier) : IBlend<JobClip>
{
    public readonly float Multiplier = multiplier;

    public void Blend(in JobClip first, in JobClip second, float factor, out JobClip result)
        => result = new JobClip(first.Amount + (second.Amount - first.Amount) * factor, first.Steps);
}

public readonly struct DamageJob : ITrack<JobTrack, JobClip>
{
    public static void OnActive(in Frame<JobTrack, JobClip> frame, ref float health)
        => health += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

public readonly struct Temperature
{
    [SuppressMessage("ReSharper", "UnassignedReadonlyField", Justification = "fixture model field")]
    public readonly float Degrees;
}

public readonly struct SensorJob : ITrack<JobTrack, JobClip>
{
    public static void OnActive(
        in Frame<JobTrack, JobClip> frame,
        in Temperature ambient,
        ref double reading,
        out int samples)
    {
        reading += frame.Clip.Amount + ambient.Degrees;
        samples = frame.Clip.Steps;
    }
}

public readonly struct PropertyClip(float amount)
{
    public readonly float Amount { get; } = amount;
}

public readonly struct PropertyTrack(float scale) : IBlend<PropertyClip>
{
    public readonly float Scale = scale;

    public void Blend(in PropertyClip first, in PropertyClip second, float factor, out PropertyClip result)
        => result = new PropertyClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct FoldClip(float height)
{
    public readonly float Height = height;
}

public readonly struct FoldTrack(float scale) : IBlend<FoldClip>
{
    public readonly float Scale = scale;

    public void Blend(in FoldClip first, in FoldClip second, float factor, out FoldClip result)
        => result = new FoldClip(first.Height + (second.Height - first.Height) * factor);
}

public readonly struct FoldJob : ITrack<FoldTrack, FoldClip>
{
    public static void OnMemo(in Frame<FoldTrack, FoldClip> frame, out float arc, out int ticks)
    {
        arc = frame.Direction * frame.Clip.Height * frame.Track.Scale;
        ticks = (int)frame.Clip.Height;
    }
}

public readonly struct FoldLiveJob : ITrack<FoldTrack, FoldClip>
{
    public static void OnMemo(in Frame<FoldTrack, FoldClip> frame, out double charge) => charge = 0d;

    public static void OnActive(in Frame<FoldTrack, FoldClip> frame, ref float health)
        => health += frame.Direction;
}
