using System.Runtime.CompilerServices;
using Tl;

public enum SeekPattern
{
    Forward,
    Alternating,
}

public readonly record struct SumClip(float Amount);

public readonly struct SumTrack : ITrack<SumClip>
{
    public void Blend(in SumClip first, in SumClip second, float factor, out SumClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Seek(in Frame<SumTrack, SumClip> frame, ref float sum)
        => sum += frame.Direction * frame.Clip.Amount;
}

public readonly partial struct SumTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var values = builder.Track(new SumTrack());
        builder.Clip(values, new SumClip(1f), 0u, 32u);
        builder.Clip(values, new SumClip(3f), 16u, 48u);
        builder.Clip(values, new SumClip(5f), 48u, 64u);
        builder.Looping();
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
    public int Starts;
    public int Interior;
    public int Ends;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint timelineTick, ushort trackIndex, FrameFlags flags)
    {
        TickSum += timelineTick + trackIndex;
        Calls++;
        var boundary = flags & (FrameFlags.ClipStart | FrameFlags.ClipEnd);
        Starts += (flags & FrameFlags.ClipStart) != 0 ? 1 : 0;
        Interior += boundary == 0 ? 1 : 0;
        Ends += (flags & FrameFlags.ClipEnd) != 0 ? 1 : 0;
    }
}

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Seek(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        nextPose = new(
            currentPose.X + frame.Direction * frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Direction * frame.Clip.Y * animationSettings.Weight);
        trace.Add(frame.TimelineTick, frame.TrackIndex, frame.Flags);
    }
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Seek(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value - frame.Direction * frame.Clip.Amount * damageSettings.Multiplier);
        trace.Add(frame.TimelineTick, frame.TrackIndex, frame.Flags);
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
        builder.Looping();
    }
}

public readonly record struct SumReceipt(
    long Position,
    uint GameTick,
    PlaybackFlags Flags,
    int SumBits,
    int Successes)
{
    public static SumReceipt Capture(long position, uint gameTick, PlaybackFlags flags, float sum, int successes)
        => new(position, gameTick, flags, BitConverter.SingleToInt32Bits(sum), successes);
}

public readonly record struct CombatReceipt(
    long Position,
    uint GameTick,
    PlaybackFlags Flags,
    int PoseXBits,
    int PoseYBits,
    int HealthBits,
    long TickSum,
    int Calls,
    int Starts,
    int Interior,
    int Ends,
    int Successes)
{
    public static CombatReceipt Capture(
        long position,
        uint gameTick,
        PlaybackFlags flags,
        in Pose pose,
        in Health health,
        in Trace trace,
        int successes)
        => new(
            position,
            gameTick,
            flags,
            BitConverter.SingleToInt32Bits(pose.X),
            BitConverter.SingleToInt32Bits(pose.Y),
            BitConverter.SingleToInt32Bits(health.Value),
            trace.TickSum,
            trace.Calls,
            trace.Starts,
            trace.Interior,
            trace.Ends,
            successes);
}
