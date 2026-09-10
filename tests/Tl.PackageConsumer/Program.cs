using Tl;

var states = new[] { new PackageCatalog.State(PackageCatalog.Asset.PackageTimeline) };
var totals = new[] { default(PackageTotal) };
var query = new PackageCatalog.Query().PackageRows(states, totals);

query.Tick(0u);

if (states[0].Position != 1u || totals[0].Value != 7)
    return 1;

query.Tick(1u, -1);

if (states[0].Position != 0u || totals[0] != default)
    return 2;

query.Tick(0u);
Console.WriteLine(totals[0].Value);
return 0;

public readonly record struct PackageTotal(int Value);
public readonly record struct PackageClip(int Value);

public readonly struct PackageTrack : IBlend<PackageClip>
{
    public void Blend(in PackageClip first, in PackageClip second, float factor, out PackageClip result)
        => result = factor < 0.5f ? first : second;
}

public readonly struct PackageJob : ITimelineJob<PackageTrack, PackageClip>
{
    public static void Execute(in Frame<PackageTrack, PackageClip> frame, ref PackageTotal total)
        => total = new(total.Value + frame.Direction * frame.Clip.Value);
}

public readonly partial struct PackageTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new PackageTrack()).Use<PackageJob>();
        builder.Clip(track, new PackageClip(7), 0u, 4u);
    }
}

public readonly struct PackageRows;

public readonly partial struct PackageCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<PackageRows>().Asset<PackageTimeline>();
    }
}
