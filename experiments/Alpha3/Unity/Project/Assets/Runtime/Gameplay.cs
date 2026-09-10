using System;

namespace UnifiedTimelineProof
{
    public interface ITimelineJob<TTrack, TClip>
        where TTrack : unmanaged
        where TClip : unmanaged { }

    public readonly struct DamageTrack
    {
        public readonly float Multiplier;
        public DamageTrack(float multiplier) => Multiplier = multiplier;
    }

    public readonly struct DamageClip
    {
        public readonly float Amount;
        public DamageClip(float amount) => Amount = amount;
    }

    public readonly struct AnimationTrack { }

    public readonly struct AnimationClip
    {
        public readonly float Distance;
        public AnimationClip(float distance) => Distance = distance;
    }

    public struct Resistance { public float Scale; }
    public struct Health { public float Value; }
    public struct Pose { public float X; }

    [Flags]
    public enum FrameFlags : byte { None = 0, Reverse = 1 }

    public readonly ref struct Frame<TTrack, TClip>
        where TTrack : unmanaged
        where TClip : unmanaged
    {
        private readonly ReadOnlySpan<TTrack> _track;
        private readonly ReadOnlySpan<TClip> _clip;
        public readonly uint TimelineTick;
        public readonly uint GameTick;
        public readonly ushort TrackIndex;
        public readonly FrameFlags Flags;

        internal Frame(ReadOnlySpan<TTrack> track, ReadOnlySpan<TClip> clip, uint timelineTick, uint gameTick, ushort trackIndex, FrameFlags flags)
        {
            _track = track;
            _clip = clip;
            TimelineTick = timelineTick;
            GameTick = gameTick;
            TrackIndex = trackIndex;
            Flags = flags;
        }

        public ref readonly TTrack Track => ref _track[0];
        public ref readonly TClip Clip => ref _clip[0];
        public int Direction => (Flags & FrameFlags.Reverse) == 0 ? 1 : -1;
    }

    public readonly partial struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
    {
        public static void Execute(in Frame<DamageTrack, DamageClip> frame, in Resistance resistance, ref Health health)
            => health.Value -= frame.Direction * frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
    }

    public readonly partial struct AnimationJob : ITimelineJob<AnimationTrack, AnimationClip>
    {
        public static void Execute(in Frame<AnimationTrack, AnimationClip> frame, in Health health, ref Pose pose)
            => pose.X += frame.Direction * frame.Clip.Distance * health.Value;
    }
}
