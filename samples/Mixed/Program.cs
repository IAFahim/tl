using Tl;

var positions = new ushort[] { 0 };
var vitality = new float[] { 0f };
var cycles = new long[] { 0 };

using var attack = TimelineAsset.Load(new DataBaker()
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(1))
    .Track<DamageTrack, DamageClip>(new DamageTrack(2))
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(3))
    .Clip(0, 0u, 2u, new AnimationClip(2f, 1f))
    .Clip(0, 1u, 2u, new AnimationClip(6f, 3f))
    .Clip(1, 0u, 2u, new DamageClip(10f))
    .Clip(2, 0u, 2u, new AnimationClip(1f, 0f))
    .Bake());
BakedLane<AnimationTrack, AnimationClip>.Bind(attack);

Timeline<BakedLane<AnimationTrack, AnimationClip>>.Seek(positions, true).Apply(vitality, cycles);
Timeline<BakedLane<AnimationTrack, AnimationClip>>.Seek(positions, true).Apply(vitality, cycles);

if (positions[0] != 2u || vitality[0] != -9f || cycles[0] != 0L)
    return 1;

Timeline<BakedLane<AnimationTrack, AnimationClip>>.Seek(positions, false).Apply(vitality, cycles);
Timeline<BakedLane<AnimationTrack, AnimationClip>>.Seek(positions, false).Apply(vitality, cycles);

if (positions[0] != 0u || vitality[0] != 0f || cycles[0] != 0L)
    return 2;

Console.WriteLine($"vitality={vitality[0]} position={positions[0]} cycle={cycles[0]}");
return 0;

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
        ref float vitality)
        => vitality += frame.Direction * (frame.Clip.X + frame.Clip.Y);
}

public readonly struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount;
}
