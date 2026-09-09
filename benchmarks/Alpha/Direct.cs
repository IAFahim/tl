using System.Runtime.CompilerServices;
using Tl;

internal static class Direct
{
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
    public static Playback Sum(ushort id, in Playback playback, uint tick, ref float sum)
    {
        SumClip clip;
        if (tick < 16u)
            clip = s_sumFirst;
        else if (tick < 32u)
        {
            var factor = (tick - 16u) / 15f;
            s_sumTrack.Blend(in s_sumFirst, in s_sumSecond, factor, out clip);
        }
        else if (tick < 48u)
            clip = s_sumSecond;
        else
            clip = s_sumLast;

        var state = tick == 47u || tick == 63u
            ? ClipState.Exit
            : tick == 48u && playback.Tick < 48u ? ClipState.Enter : ClipState.Stay;
        var frame = new Frame<SumTrack, SumClip>(in s_sumTrack, in clip, tick, state, 0);
        SumTrack.Forward(in frame, ref sum);
        return Next(id, in playback, tick);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Combat(
        ushort id,
        in Playback playback,
        uint tick,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        in Health currentHealth,
        in DamageSettings damageSettings,
        ref Pose nextPose,
        ref Trace trace,
        ref Health nextHealth)
    {
        if (tick < 16u)
        {
            var frame = new Frame<AnimationTrack, AnimationClip>(
                in s_animationTrack,
                in s_animationFirst,
                tick,
                ClipState.Stay,
                0);
            AnimationTrack.Forward(in frame, in currentPose, in animationSettings, out nextPose, ref trace);
        }
        else if (tick < 32u)
        {
            var factor = (tick - 16u) / 15f;
            s_animationTrack.Blend(in s_animationFirst, in s_animationSecond, factor, out var clip);
            var frame = new Frame<AnimationTrack, AnimationClip>(
                in s_animationTrack,
                in clip,
                tick,
                ClipState.Stay,
                0);
            AnimationTrack.Forward(in frame, in currentPose, in animationSettings, out nextPose, ref trace);
        }
        else if (tick < 48u)
        {
            var state = tick == 47u
                ? ClipState.Exit
                : playback.Tick < 16u ? ClipState.Enter : ClipState.Stay;
            var frame = new Frame<AnimationTrack, AnimationClip>(
                in s_animationTrack,
                in s_animationSecond,
                tick,
                state,
                0);
            AnimationTrack.Forward(in frame, in currentPose, in animationSettings, out nextPose, ref trace);
        }

        if (tick >= 8u && tick < 56u)
        {
            var state = tick == 55u
                ? ClipState.Exit
                : playback.Tick < 8u ? ClipState.Enter : ClipState.Stay;
            var frame = new Frame<DamageTrack, DamageClip>(
                in s_damageTrack,
                in s_damage,
                tick,
                state,
                1);
            DamageTrack.Forward(in frame, in currentHealth, in damageSettings, out nextHealth, ref trace);
        }
        else if (tick == 63u)
        {
            var frame = new Frame<DamageTrack, DamageClip>(
                in s_damageTrack,
                in s_zeroDamage,
                tick,
                ClipState.Exit,
                1);
            DamageTrack.Forward(in frame, in currentHealth, in damageSettings, out nextHealth, ref trace);
        }

        return Next(id, in playback, tick);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback Next(ushort id, in Playback playback, uint tick)
    {
        var flags = PlaybackFlags.Started;
        if (tick >= 63u)
            flags |= PlaybackFlags.Completed;
        return Timeline.CreateCompiledPlayback(id, tick, playback.Cycles, flags);
    }
}
