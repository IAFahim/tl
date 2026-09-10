using Tl;

var typedTotal = default(PackageTotal);
var typedPlayback = PackageTimeline.Start(0u);
var typedData = new PackageTimeline.Data(ref typedPlayback, ref typedTotal);
if (!PackageTimeline.TrySeek(ref typedData, 1))
    return 1;

var dynamicTotal = default(PackageTotal);
if (!Timeline.TryStart(PackageTimeline.Id, 0u, out var dynamicPlayback))
    return 2;
var dynamicData = new PackageTimeline.DynamicData(ref dynamicPlayback, ref dynamicTotal);
if (!Timeline.TrySeek(PackageTimeline.Id, ref dynamicData, 1))
    return 3;
if (typedTotal != dynamicTotal || typedPlayback.Position != dynamicPlayback.Position)
    return 4;

Console.WriteLine(typedTotal.Value);
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
