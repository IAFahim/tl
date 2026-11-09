using Tl.Mathematics;

namespace Tl.Pack.Force
{
    [TlColumn]
    public partial struct Force
    {
        public float3 Value;
    }

    [TlClip("Force/Force")]
    public struct ForceClip
    {
        public float3 Direction;
        public float Strength;
        public TlCurve Falloff;
    }

    [TlKind]
    public struct ForceKind
    {
        public static void Combine(ref float3 total, in float3 next) => total += next;
        public static void Apply(in float3 total, ref Force force) => force.Value += total;
    }

    public struct ForceTrack : ITlTrack<ForceClip>
    {
        [Feeds(typeof(ForceKind))]
        public static float3 Evaluate(in TlFrame<ForceClip> frame)
            => frame.Clip.Direction * frame.Clip.Strength * frame.Clip.Falloff.Evaluate(frame.Progress);
    }
}
