using Tl;

var total = default(PackageTotal);
var input = new PackageTimeline.Input();
var output = new PackageTimeline.Output(ref total);
if (!Timeline.TryStart(PackageTimeline.Id, out var playback)
    || !Timeline.TryForward(PackageTimeline.Id, in playback, 1u, in input, ref output, out playback))
    return 1;
Console.WriteLine(total.Value);
return 0;

public readonly record struct PackageTotal(int Value);
public readonly record struct PackageClip(int Value);

public readonly struct PackageTrack : ITrack<PackageClip>
{
    public void Blend(in PackageClip first, in PackageClip second, float factor, out PackageClip result)
        => result = factor < 0.5f ? first : second;

    public static void Forward(in Frame<PackageTrack, PackageClip> frame, ref PackageTotal total)
        => total = new(total.Value + frame.Clip.Value);

    public static void Backward(in Frame<PackageTrack, PackageClip> frame, ref PackageTotal total)
        => total = new(total.Value - frame.Clip.Value);
}

public readonly partial struct PackageTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new PackageTrack());
        builder.Clip(in track, new PackageClip(7), 0u, 4u);
    }
}
