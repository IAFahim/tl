using Tl;

var currentPose = new Pose(0f, 0f);
var animationSettings = new AnimationSettings(1f);
var currentHealth = new Health(100f);
var damageSettings = new DamageSettings(1f);
var nextPose = currentPose;
var nextHealth = currentHealth;
var input = new Attack.Input(
    currentPose: in currentPose,
    animationSettings: in animationSettings,
    currentHealth: in currentHealth,
    damageSettings: in damageSettings);
var output = new Attack.Output(nextPose: ref nextPose, nextHealth: ref nextHealth);

if (!Timeline.TryStart(Attack.Id, out var playback))
    return 1;

if (!Timeline.TryForward(Attack.Id, in playback, 10u, in input, ref output, out playback))
    return 2;

if (nextPose != new Pose(2f, 1f) || nextHealth != new Health(90f))
    return 3;

if (!Timeline.All[Attack.Id].TryBackward(in playback, 5u, in input, ref output, out playback))
    return 4;

Console.WriteLine($"{nextPose} {nextHealth} {playback}");
return 0;

public readonly record struct Pose(float X, float Y);
public readonly record struct AnimationSettings(float Weight);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct DamageSettings(float Multiplier);
public readonly record struct DamageClip(float Amount);

public readonly partial struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Forward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose)
        => nextPose = new(
            currentPose.X + frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Clip.Y * animationSettings.Weight);

    public static void Backward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose)
        => nextPose = new(
            currentPose.X - frame.Clip.X * animationSettings.Weight,
            currentPose.Y - frame.Clip.Y * animationSettings.Weight);
}

public readonly partial struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Forward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth)
        => nextHealth = new(currentHealth.Value - frame.Clip.Amount * damageSettings.Multiplier);

    public static void Backward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth)
        => nextHealth = new(currentHealth.Value + frame.Clip.Amount * damageSettings.Multiplier);
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
