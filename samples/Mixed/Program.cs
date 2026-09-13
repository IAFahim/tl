using Tl;

var poses = new[] { new Pose(0f, 0f) };
var traces = new[] { new Trace(0, 0) };
var health = new[] { new Health(100f) };

using var attack = TimelineAsset.Load(new DataBaker()
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(1))
    .Track<DamageTrack, DamageClip>(new DamageTrack(2))
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(3))
    .Clip(0, 0u, 2u, new AnimationClip(2f, 1f))
    .Clip(0, 1u, 2u, new AnimationClip(6f, 3f))
    .Clip(1, 0u, 2u, new DamageClip(10f))
    .Clip(2, 0u, 2u, new AnimationClip(1f, 0f))
    .Bake());
var rows = new[] { new TimelineComponent(attack.Reference) };
var query = Timeline.Rows(rows).Write(poses).Write(traces).Write(health);

query.Tick(200_000u, 2);

if (rows[0].Position != 2u
    || poses[0] != new Pose(8f, 3f)
    || health[0] != new Health(80f)
    || traces[0] != new Trace(123_123, 200_001u))
    return 1;

query.Tick(200_002u, -1);

if (rows[0].Position != 1u
    || poses[0] != new Pose(3f, 1f)
    || health[0] != new Health(90f)
    || traces[0] != new Trace(123_123_321, 200_001u))
    return 2;

Console.WriteLine($"{poses[0]} {health[0]} order={traces[0].Order} gameTick={traces[0].LastGameTick} position={rows[0].Position}");
return 0;

public readonly record struct Pose(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct Trace(long Order, uint LastGameTick);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct DamageClip(float Amount);

public readonly record struct AnimationTrack(int Code) : IBlend<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);
}

public readonly record struct DamageTrack(int Code) : IBlend<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct AnimationJob : ITimelineJob<AnimationTrack, AnimationClip>
{
    public static void Execute(
        in Frame<AnimationTrack, AnimationClip> frame,
        ref Pose pose,
        ref Trace trace)
    {
        pose = new(
            pose.X + frame.Direction * frame.Clip.X,
            pose.Y + frame.Direction * frame.Clip.Y);
        trace = new(unchecked(trace.Order * 10 + frame.Track.Code), frame.GameTick);
    }
}

public readonly struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        ref Trace trace,
        ref Health health)
    {
        health = new(health.Value - frame.Direction * frame.Clip.Amount);
        trace = new(unchecked(trace.Order * 10 + frame.Track.Code), frame.GameTick);
    }
}
