using Tl;
using Tl.TestSupport;

using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
    .Track<PackageTrack, PackageClip>(new PackageTrack())
    .Clip(0, 0u, 4u, new PackageClip(7))
    .Bake()));
var positions = new ushort[1];
var values = new float[1];

Timeline<PackageTrack, PackageClip>.Advance(asset, positions, true, values);

if (positions[0] != 1 || values[0] != 7f)
    return 1;

Timeline<PackageTrack, PackageClip>.Advance(asset, positions, false, values);

if (positions[0] != 0 || values[0] != 0f)
    return 2;

Timeline<PackageTrack, PackageClip>.Advance(asset, positions, true, values);

if (BakeRuntime<PackageTrack, PackageClip>.BakeCount != 1
    || BakeRuntime<PackageTrack, PackageClip>.BakeContextCount(0) != 1
    || BakeRuntime<PackageTrack, PackageClip>.BakeContextKey(0, 0) != TypeKey<PackageHost>.Value)
    return 3;

Console.WriteLine((int)values[0]);
return 0;

public readonly record struct PackageClip(int Value);
public readonly struct PackageTrack : IBlend<PackageClip>
{
    public void Blend(in PackageClip first, in PackageClip second, float factor, out PackageClip result)
        => result = factor < 0.5f ? first : second;
}

public sealed class PackageHost { public int Marks; }

public readonly struct PackageJob : ITrack<PackageTrack, PackageClip>, IBake<PackageJob, PackageHost>
{
    public static void Execute(in Frame<PackageTrack, PackageClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Value;

    public static void Bake(PackageJob consumer, PackageHost host) { host.Marks++; }
}
