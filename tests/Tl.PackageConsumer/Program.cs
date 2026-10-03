using Tl;
using Tl.TestSupport;

using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
    .Track<PackageTrack, PackageClip>(new PackageTrack())
    .Clip(0, 0u, 4u, new PackageClip(7))
    .Bake()));
var positions = new ushort[1];
var values = new float[1];

ReadOnlySpan<float> golden = [7f, 14f, 21f];
for (var frame = 0; frame < golden.Length; frame++)
{
    Timeline<PackageTrack, PackageClip>.Apply(asset, positions, true, values);
    Timeline.Advance(asset, positions, true);
    if (values[0] != golden[frame] || positions[0] != (frame + 1) % 4)
        return 1;
}

ReadOnlySpan<float> rewind = [14f, 7f, 0f];
for (var frame = 0; frame < rewind.Length; frame++)
{
    Timeline<PackageTrack, PackageClip>.Apply(asset, positions, false, values);
    Timeline.Advance(asset, positions, false);
    if (values[0] != rewind[frame] || positions[0] != 2 - frame)
        return 2;
}

Timeline<PackageTrack, PackageClip>.Apply(asset, positions, true, values);
Timeline.Advance(asset, positions, true);

if (positions[0] != 1 || values[0] != 7f)
    return 7;

unsafe
{
    var view = Timeline<PackageTrack, PackageClip>.View(asset.Index);
    if (view.Duration != 4 || view.TableTicks != 5
        || view.AbiVersion != SlotView.AbiVersionV3 || view.Generation == 0
        || view.Forward == null || view.Backward == null
        || view.LaneKeys == null)
        return 4;

    if (view.Forward[0] != 7f || view.Forward[4] != 0f || view.Backward[3] != -7f)
        return 5;

    var next = (ushort)(0 + 1);
    var tail = 3 + 1 == view.Duration ? SlotView.Skipped : next;
    if (view.Forward[next] != 7f || tail != SlotView.Skipped)
        return 6;
}

if (BakeRuntime<PackageTrack, PackageClip>.BakeCount != 1
    || BakeRuntime<PackageTrack, PackageClip>.BakeParameterCount(0) != 1
    || BakeRuntime<PackageTrack, PackageClip>.BakeParameterKey(0, 0) != TypeKey<PackageHost>.Value)
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

public readonly struct PackageJob : ITrack<PackageTrack, PackageClip>, IBake<PackageJob>
{
    public static void ExecuteActive(in Frame<PackageTrack, PackageClip> frame, ref float value)
        => value += frame.Direction * frame.Clip.Value;

    public static void Bake(PackageHost host) { host.Marks++; }
}
