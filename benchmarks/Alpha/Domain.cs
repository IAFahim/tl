using System.Runtime.CompilerServices;
using Tl;

public enum TickPattern
{
    Sequential,
    Random,
}

public readonly record struct SumClip(float Amount);

public readonly struct SumTrack : ITrack<SumClip>
{
    public void Blend(in SumClip first, in SumClip second, float factor, out SumClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(in Frame<SumTrack, SumClip> frame, ref float sum)
        => sum += frame.Clip.Amount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(in Frame<SumTrack, SumClip> frame, ref float sum)
        => sum -= frame.Clip.Amount;
}

public readonly partial struct SumTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var values = builder.Track(new SumTrack());
        builder.Clip(values, new SumClip(1f), 0u, 32u);
        builder.Clip(values, new SumClip(3f), 16u, 48u);
        builder.Clip(values, new SumClip(5f), 48u, 64u);
    }
}

public readonly record struct Pose(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct AnimationSettings(float Weight);
public readonly record struct DamageSettings(float Multiplier);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct DamageClip(float Amount);

public struct Trace
{
    public long TickSum;
    public int Calls;
    public int Enters;
    public int Stays;
    public int Exits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint tick, ushort trackIndex, ClipState state)
    {
        TickSum += tick + trackIndex;
        Calls++;
        switch (state)
        {
            case ClipState.Enter:
                Enters++;
                break;
            case ClipState.Stay:
                Stays++;
                break;
            case ClipState.Exit:
                Exits++;
                break;
        }
    }
}

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        nextPose = new(
            currentPose.X + frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Clip.Y * animationSettings.Weight);
        trace.Add(frame.Tick, frame.TrackIndex, frame.State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        nextPose = new(
            currentPose.X - frame.Clip.X * animationSettings.Weight,
            currentPose.Y - frame.Clip.Y * animationSettings.Weight);
        trace.Add(frame.Tick, frame.TrackIndex, frame.State);
    }
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value - frame.Clip.Amount * damageSettings.Multiplier);
        trace.Add(frame.Tick, frame.TrackIndex, frame.State);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value + frame.Clip.Amount * damageSettings.Multiplier);
        trace.Add(frame.Tick, frame.TrackIndex, frame.State);
    }
}

public readonly partial struct CombatTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        var damage = builder.Track(new DamageTrack());
        builder.Clip(animation, new AnimationClip(2f, 1f), 0u, 32u);
        builder.Clip(animation, new AnimationClip(6f, 3f), 16u, 48u);
        builder.Clip(damage, new DamageClip(5f), 8u, 56u);
        builder.Clip(damage, new DamageClip(0f), 63u, 64u);
    }
}

public readonly record struct SumReceipt(
    uint Tick,
    ushort Cycles,
    ushort Owner,
    PlaybackFlags Flags,
    int SumBits,
    int Successes)
{
    public static SumReceipt Capture(in Playback playback, float sum, int successes)
        => new(
            playback.Tick,
            playback.Cycles,
            playback.Owner,
            playback.Flags,
            BitConverter.SingleToInt32Bits(sum),
            successes);
}

public readonly record struct CombatReceipt(
    uint Tick,
    ushort Cycles,
    ushort Owner,
    PlaybackFlags Flags,
    int PoseXBits,
    int PoseYBits,
    int HealthBits,
    long TickSum,
    int Calls,
    int Enters,
    int Stays,
    int Exits,
    int Successes)
{
    public static CombatReceipt Capture(
        in Playback playback,
        in Pose pose,
        in Health health,
        in Trace trace,
        int successes)
        => new(
            playback.Tick,
            playback.Cycles,
            playback.Owner,
            playback.Flags,
            BitConverter.SingleToInt32Bits(pose.X),
            BitConverter.SingleToInt32Bits(pose.Y),
            BitConverter.SingleToInt32Bits(health.Value),
            trace.TickSum,
            trace.Calls,
            trace.Enters,
            trace.Stays,
            trace.Exits,
            successes);
}
