using Tl;

namespace PeakPack
{
    [TlState]
    public partial struct Peak
    {
        public float Top;
        public int Applied;
    }

    [TlClip("Peak")]
    public struct PeakClip
    {
        public float Height;
    }

    [TlKind(Associative = true)]
    public struct PeakKind
    {
        public static void Combine(ref float total, in float next)
        {
            if (next > total) total = next;
        }

        public static void Apply(in float total, ref Peak peak)
        {
            peak.Top = total;
            peak.Applied++;
        }
    }

    public struct PeakTrack : ITlTrack<PeakClip>
    {
        [Feeds(typeof(PeakKind))]
        public static float Evaluate(in TlFrame<PeakClip> frame) => (-1f - frame.Clip.Height - frame.WithinClip) * (frame.IsBackward ? 2f : 1f);
    }
}
