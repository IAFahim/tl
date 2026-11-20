using PeakPack;
using Tl;

namespace PeakGame
{
    [TlState]
    public partial struct Gain
    {
        public float Scale;
    }

    [TlClip("Dip")]
    public struct DipClip
    {
        public float Depth;
    }

    public struct DipTrack : ITlTrack<DipClip>
    {
        [Feeds(typeof(PeakKind))]
        public static float Evaluate(in TlFrame<DipClip> frame, in Gain gain) => -1f - frame.Clip.Depth * gain.Scale - frame.Tick * (frame.IsBackward ? 0.5f : 1f);
    }
}
