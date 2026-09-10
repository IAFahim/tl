using System.Runtime.CompilerServices;
using Tl;

internal static class Direct
{
    private const long Duration = 64;
    private static readonly SumTrack s_sumTrack = new();
    private static readonly SumClip s_sumFirst = new(1f);
    private static readonly SumClip s_sumSecond = new(3f);
    private static readonly SumClip s_sumLast = new(5f);
    private static readonly AnimationTrack s_animationTrack = new();
    private static readonly DamageTrack s_damageTrack = new();
    private static readonly AnimationClip s_animationFirst = new(2f, 1f);
    private static readonly AnimationClip s_animationSecond = new(6f, 3f);
    private static readonly DamageClip s_damage = new(5f);
    private static readonly DamageClip s_zeroDamage = new(0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sum(int direction, ref long position, ref uint gameTick, ref float sum)
    {
        Locate(direction, position, gameTick, out var local, out var cycle, out var frameGameTick, out var flags);
        SumClip clip;
        if (local < 16u)
            clip = s_sumFirst;
        else if (local < 32u)
        {
            var factor = (local - 16u) / 15f;
            s_sumTrack.Blend(in s_sumFirst, in s_sumSecond, factor, out clip);
        }
        else if (local < 48u)
            clip = s_sumSecond;
        else
            clip = s_sumLast;
        if (local is 0u or 16u or 48u)
            flags |= FrameFlags.ClipStart;
        if (local is 31u or 47u or 63u)
            flags |= FrameFlags.ClipEnd;
        var frame = new Frame<SumTrack, SumClip>(in s_sumTrack, in clip, frameGameTick, local, cycle, 0, flags);
        SumTrack.Seek(in frame, ref sum);
        Advance(direction, ref position, ref gameTick);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Combat(
        int direction,
        ref long position,
        ref uint gameTick,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        in Health currentHealth,
        in DamageSettings damageSettings,
        ref Pose nextPose,
        ref Trace trace,
        ref Health nextHealth)
    {
        Locate(direction, position, gameTick, out var local, out var cycle, out var frameGameTick, out var flags);
        if (local < 48u)
        {
            AnimationClip clip;
            if (local < 16u)
                clip = s_animationFirst;
            else if (local < 32u)
            {
                var factor = (local - 16u) / 15f;
                s_animationTrack.Blend(in s_animationFirst, in s_animationSecond, factor, out clip);
            }
            else
                clip = s_animationSecond;
            var animationFlags = flags;
            if (local is 0u or 16u)
                animationFlags |= FrameFlags.ClipStart;
            if (local is 31u or 47u)
                animationFlags |= FrameFlags.ClipEnd;
            var frame = new Frame<AnimationTrack, AnimationClip>(
                in s_animationTrack,
                in clip,
                frameGameTick,
                local,
                cycle,
                0,
                animationFlags);
            AnimationTrack.Seek(in frame, in currentPose, in animationSettings, out nextPose, ref trace);
        }

        if (local is >= 8u and < 56u)
        {
            var damageFlags = flags;
            if (local == 8u)
                damageFlags |= FrameFlags.ClipStart;
            if (local == 55u)
                damageFlags |= FrameFlags.ClipEnd;
            var frame = new Frame<DamageTrack, DamageClip>(
                in s_damageTrack,
                in s_damage,
                frameGameTick,
                local,
                cycle,
                1,
                damageFlags);
            DamageTrack.Seek(in frame, in currentHealth, in damageSettings, out nextHealth, ref trace);
        }
        else if (local == 63u)
        {
            var frame = new Frame<DamageTrack, DamageClip>(
                in s_damageTrack,
                in s_zeroDamage,
                frameGameTick,
                local,
                cycle,
                1,
                flags | FrameFlags.ClipStart | FrameFlags.ClipEnd);
            DamageTrack.Seek(in frame, in currentHealth, in damageSettings, out nextHealth, ref trace);
        }

        Advance(direction, ref position, ref gameTick);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Locate(
        int direction,
        long position,
        uint gameTick,
        out uint local,
        out long cycle,
        out uint frameGameTick,
        out FrameFlags flags)
    {
        var framePosition = direction > 0 ? position : position - 1L;
        cycle = framePosition / Duration;
        var remainder = framePosition - cycle * Duration;
        if (remainder < 0L)
        {
            remainder += Duration;
            cycle--;
        }
        local = (uint)remainder;
        frameGameTick = direction > 0 ? gameTick : unchecked(gameTick - 1u);
        flags = FrameFlags.Looping;
        if (direction < 0)
            flags |= FrameFlags.Reverse;
        if (local == 0u)
            flags |= FrameFlags.TimelineStart;
        if (local == 63u)
            flags |= FrameFlags.TimelineEnd;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Advance(int direction, ref long position, ref uint gameTick)
    {
        position += direction;
        gameTick = unchecked(gameTick + (uint)direction);
    }
}
