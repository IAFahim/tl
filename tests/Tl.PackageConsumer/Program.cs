using System;
using Tl;

using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "game.tlib")));
using var players = new PlaybackTable(entityCapacity: 1);
using var kinds = new KindStore(world, players);
var values = new float[1];
players.Start(0, 1);

float[] forward = [7f, 14f, 21f];
for (var frame = 0; frame < forward.Length; frame++)
{
    world.Tick(players, true);
    new PackageSystem { Values = values }.Run(world, players, true);
    if (values[0] != forward[frame]) return 1;
}

float[] rewind = [14f, 7f, 0f];
for (var frame = 0; frame < rewind.Length; frame++)
{
    world.Tick(players, false);
    new PackageSystem { Values = values }.Run(world, players, false);
    if (values[0] != rewind[frame]) return 2;
}

using var image = new PlaybackImage();
players.Save(image);
var saved = values[0];
world.Tick(players, true);
new PackageSystem { Values = values }.Run(world, players, true);
if (values[0] != 7f) return 7;

players.Restore(image);
values[0] = saved;
world.Tick(players, true);
new PackageSystem { Values = values }.Run(world, players, true);
if (values[0] != 7f) return 8;

Console.WriteLine((int)values[0]);
return 0;

public readonly struct PackageClip
{
    public readonly float Value;
    public PackageClip(float value) { Value = value; }
}

public readonly struct PackageTrack : IBlend<PackageClip>
{
    public readonly float Scale;
    public PackageTrack(float scale) { Scale = scale; }
    public void Blend(in PackageClip first, in PackageClip second, float factor, out PackageClip result)
        => result = new PackageClip(first.Value + ((second.Value - first.Value) * factor));
}

[TimelineSystem]
public ref partial struct PackageSystem
{
    public Span<float> Values;
}

public readonly struct PackageMove : ITrack<PackageTrack, PackageClip>
{
    [Feeds(typeof(PackagePush))]
    public static float Evaluate(in Frame<PackageTrack, PackageClip> frame)
        => frame.Direction * frame.Clip.Value * frame.Track.Scale;
}

[TimelineKind(Mode = KindMode.Each)]
public struct PackagePush
{
    public static void Apply(in float push, ref float value) => value += push;
}
