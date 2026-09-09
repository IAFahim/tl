using Tl;

var input = new PackageInput();
var result = new PackageResult();
var playback = PackageTimeline.Start();
playback = PackageTimeline.Forward(in playback, in input, ref result, 1u);
Console.WriteLine(result.Sum);

public readonly record struct PackageClip(int Value);

public readonly struct PackageTrack : IBlend<PackageClip>
{
    public void Blend(in PackageClip first, in PackageClip second, float t, out PackageClip result)
        => result = t < 0.5f ? first : second;
}

public readonly struct PackageInput { }

public struct PackageResult : ITrack<PackageTrack, PackageClip, PackageInput, PackageResult>
{
    public int Sum;

    public static void Forward(int ordinal, int count, ushort index,
        in PackageTrack track, in PackageClip clip, ClipState state,
        in uint tick, in PackageInput input, ref PackageResult result)
        => result.Sum += clip.Value;

    public static void Backward(int ordinal, int count, ushort index,
        in PackageTrack track, in PackageClip clip, ClipState state,
        in uint tick, in PackageInput input, ref PackageResult result)
        => result.Sum -= clip.Value;
}

public readonly partial struct PackageTimeline : ITimeline<PackageTrack, PackageClip>
{
    public static void Define(scoped TimelineBuilder<PackageTrack, PackageClip> timeline)
    {
        var track = timeline.Track(new PackageTrack());
        timeline.Clip(in track, new PackageClip(7), 0u, 4u);
    }
}
