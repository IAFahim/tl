using Tl;

var currentPose = new Pose(0f, 0f);
var animationSettings = new AnimationSettings(1f);
var currentHealth = new Health(100f);
var damageSettings = new DamageSettings(1f);
var nextPose = currentPose;
var nextHealth = currentHealth;
var playback = Attack.Start(0u);
var data = new Attack.Data(
    ref playback,
    in animationSettings,
    in currentHealth,
    in currentPose,
    in damageSettings,
    ref nextHealth,
    ref nextPose);

if (!Attack.TrySeek(ref data, 11))
    return 1;

if (nextPose != new Pose(2f, 1f) || nextHealth != new Health(90f))
    return 2;

if (!Attack.TrySeek(ref data, -6))
    return 3;

if (playback.Position != 5L || playback.GameTick != 5u)
    return 4;

Console.WriteLine($"{nextPose} {nextHealth} {playback}");
return 0;

public readonly record struct Pose(float X, float Y);
public readonly record struct AnimationSettings(float Weight);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct DamageSettings(float Multiplier);
public readonly record struct DamageClip(float Amount);

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Seek(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose)
        => nextPose = new(
            currentPose.X + frame.Direction * frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Direction * frame.Clip.Y * animationSettings.Weight);
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Seek(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth)
        => nextHealth = new(currentHealth.Value - frame.Direction * frame.Clip.Amount * damageSettings.Multiplier);
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        var damage = builder.Track(new DamageTrack());
        builder.Clip(animation, new AnimationClip(2f, 1f), 0u, 40u);
        builder.Clip(animation, new AnimationClip(6f, 3f), 20u, 60u);
        builder.Clip(damage, new DamageClip(10f), 10u, 11u);
        builder.Clip(damage, new DamageClip(20f), 40u, 41u);
    }
}
