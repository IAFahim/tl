using System;
using Tl;

using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "game.tlib")));
using var players = new PlaybackTable(entityCapacity: 1);
var values = new float[1];
players.Start(0, 1);

float[] forward = [7f, 14f, 21f];
for (var frame = 0; frame < forward.Length; frame++)
{
    world.ApplyRequests(players);
    new PackageSystem { Values = values }.Run(world, players, true);
    world.Advance(players, true);
    if (values[0] != forward[frame]) return 1;
}

float[] rewind = [14f, 7f, 0f];
for (var frame = 0; frame < rewind.Length; frame++)
{
    world.ApplyRequests(players);
    new PackageSystem { Values = values }.Run(world, players, false);
    world.Advance(players, false);
    if (values[0] != rewind[frame]) return 2;
}

world.ApplyRequests(players);
new PackageSystem { Values = values }.Run(world, players, true);
world.Advance(players, true);
if (values[0] != 7f) return 7;

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
    public static void Fold(in Frame<PackageTrack, PackageClip> frame, out float push)
        => push = frame.Direction * frame.Clip.Value * frame.Track.Scale;

    public static void ExecuteActive(in Frame<PackageTrack, PackageClip> frame, in float push, ref float value)
        => value += push;

    public static void OnClipEnd(in Frame<PackageTrack, PackageClip> frame)
    {
    }
}
