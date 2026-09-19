using Tl;
using Tl.TestSupport;

var positions = new ushort[] { 0 };
var vitality = new float[] { 0f };

using var attack = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(1))
    .Track<DamageTrack, DamageClip>(new DamageTrack(2))
    .Track<AnimationTrack, AnimationClip>(new AnimationTrack(3))
    .Clip(0, 0u, 2u, new AnimationClip(2f, 1f))
    .Clip(0, 1u, 2u, new AnimationClip(6f, 3f))
    .Clip(1, 0u, 2u, new DamageClip(10f))
    .Clip(2, 0u, 2u, new AnimationClip(1f, 0f))
    .Bake()));

Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, true, vitality); Timeline.Step(attack, positions, true);
Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, true, vitality); Timeline.Step(attack, positions, true);

if (positions[0] != 2 || vitality[0] != -9f)
    return 1;

Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, false, vitality); Timeline.Step(attack, positions, false);
Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, false, vitality); Timeline.Step(attack, positions, false);

if (positions[0] != 0 || vitality[0] != 0f)
    return 2;

Console.WriteLine($"vitality={vitality[0]} position={positions[0]}");
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

public readonly struct AnimationJob : ITrack<AnimationTrack, AnimationClip>
{
    public static void Execute(
        in Frame<AnimationTrack, AnimationClip> frame,
        ref float vitality)
        => vitality += frame.Direction * (frame.Clip.X + frame.Clip.Y);
}

public readonly struct DamageJob : ITrack<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount;
}
