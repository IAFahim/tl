using Tl;

var total = default(PackageTotal);
var playback = PackageTimeline.Start(0u);
var data = new PackageTimeline.Data(ref playback, ref total);
if (!PackageTimeline.TrySeek(ref data, 1))
    return 1;
Console.WriteLine(total.Value);
return 0;

public readonly record struct PackageTotal(int Value);
public readonly record struct PackageClip(int Value);

public readonly struct PackageTrack : ITrack<PackageClip>
{
    public void Blend(in PackageClip first, in PackageClip second, float factor, out PackageClip result)
        => result = factor < 0.5f ? first : second;

    public static void Seek(in Frame<PackageTrack, PackageClip> frame, ref PackageTotal total)
        => total = new(total.Value + frame.Direction * frame.Clip.Value);
}

public readonly partial struct PackageTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new PackageTrack());
        builder.Clip(in track, new PackageClip(7), 0u, 4u);
    }
}
