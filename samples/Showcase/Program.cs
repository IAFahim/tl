using Showcase;
using Tl;

ushort jumpTimeline = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));

var ids  = new ushort[] { jumpTimeline, jumpTimeline, jumpTimeline, jumpTimeline };
var tick = new ushort[] { 0, 0, 0, 0 };
var y    = new float[] { 0f, 0f, 0f, 0f };

Console.WriteLine("four characters jump, one call per frame:");
for (var frame = 1; frame <= 30; frame++)
{
    Timeline<JumpTrack, JumpClip>.Apply(ids, tick, true, y); Timeline.Advance(ids, tick, true);
    if (frame % 3 == 0)
        Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', Math.Max(0, (int)Math.Round(y[0] / 3)))}");
}

Console.WriteLine();
Console.WriteLine("rewind walks the arc back exactly:");
for (var frame = 0; frame < 30; frame++)
    { Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, false, y); Timeline.Advance(jumpTimeline, tick, false); }
Console.WriteLine($"  after 30 back ticks: y = {y[0]:0.0} m, tick = {tick[0]}");

Console.WriteLine();
var world = new World();
Timeline.Bake(jumpTimeline, world, 42);
Timeline.Bake(jumpTimeline, world, 43);
Console.WriteLine($"Timeline.Bake marked entities {string.Join(", ", world.Jumping)} as jumping; unmarked entities never reach the advance");

namespace Showcase
{
    public readonly record struct JumpClip(float Velocity);

    public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
    {
        public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
            => result = new(first.Velocity + (second.Velocity - first.Velocity) * factor);
    }

    public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
    {
        public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
            => y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
    }

    public readonly struct AttachJumping : IBake<MoveY>
    {
        public static void Bake(World world, int entity)
            => world.MarkJumping(entity);
    }

    public sealed class World
    {
        public readonly List<int> Jumping = [];
        public void MarkJumping(int entity) => Jumping.Add(entity);
    }
}
