using System.Runtime.InteropServices;

namespace UnifiedTimelineProof
{
    public readonly partial struct DamageJob
    {
        public static void Execute(in TimelineState timeline, in Resistance resistance, ref Health health)
        {
            if (!timeline.Selection.Damage)
                return;
            var track = new DamageTrack(2f);
            var clip = new DamageClip(timeline.Asset.Kind == 2 ? 20f : 10f);
            var frame = new Frame<DamageTrack, DamageClip>(
                MemoryMarshal.CreateReadOnlySpan(ref track, 1),
                MemoryMarshal.CreateReadOnlySpan(ref clip, 1),
                timeline.Selection.TimelineTick, timeline.Selection.GameTick, 0,
                timeline.Selection.Reverse ? FrameFlags.Reverse : FrameFlags.None);
            Execute(in frame, in resistance, ref health);
        }
    }

    public readonly partial struct AnimationJob
    {
        public static void Execute(in TimelineState timeline, in Health health, ref Pose pose)
        {
            if (!timeline.Selection.Animation)
                return;
            var track = new AnimationTrack();
            var clip = new AnimationClip(1f);
            var frame = new Frame<AnimationTrack, AnimationClip>(
                MemoryMarshal.CreateReadOnlySpan(ref track, 1),
                MemoryMarshal.CreateReadOnlySpan(ref clip, 1),
                timeline.Selection.TimelineTick, timeline.Selection.GameTick, 1,
                timeline.Selection.Reverse ? FrameFlags.Reverse : FrameFlags.None);
            Execute(in frame, in health, ref pose);
        }
    }
}
