using Tl.Mathematics;

namespace Tl.Pack.Motion
{
    [System.Serializable]
    public struct Rgba
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Rgba(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Rgba Lerp(Rgba a, Rgba b, float t) => new Rgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
    }

    public static class Ease
    {
        public static float3 Lerp(float3 a, float3 b, float t) => a + (b - a) * t;
    }

    [TlState]
    public partial struct Pose
    {
        public float3 Position;
        public float3 Scale;
        public float Yaw;
        public byte Has;
    }

    [TlState]
    public partial struct Tint
    {
        public Rgba Color;
        public byte Has;
    }

    public struct Placed
    {
        public float3 Value;
        public int Priority;
    }

    [TlClip("Motion/Move")]
    public struct MoveClip
    {
        public float3 From;
        public float3 To;
        public TlCurve Ease;
        public int Priority;
    }

    [TlClip("Motion/Scale")]
    public struct ScaleClip
    {
        public float3 From;
        public float3 To;
        public TlCurve Ease;
    }

    [TlClip("Motion/Spin")]
    public struct SpinClip
    {
        public float FromYaw;
        public float ToYaw;
        public TlCurve Ease;
    }

    [TlClip("Motion/Tint")]
    public struct TintClip
    {
        public Rgba From;
        public Rgba To;
        public TlCurve Ease;
    }

    [TlKind]
    public struct MotionPosition
    {
        public static void Combine(ref Placed total, in Placed next)
        {
            if (next.Priority >= total.Priority) total = next;
        }

        public static void Apply(in Placed total, ref Pose pose)
        {
            pose.Position = total.Value;
            pose.Has |= 1;
        }
    }

    [TlKind]
    public struct MotionScale
    {
        public static void Combine(ref float3 total, in float3 next) => total = next;

        public static void Apply(in float3 total, ref Pose pose)
        {
            pose.Scale = total;
            pose.Has |= 2;
        }
    }

    [TlKind]
    public struct MotionYaw
    {
        public static void Combine(ref float total, in float next) => total = next;

        public static void Apply(in float total, ref Pose pose)
        {
            pose.Yaw = total;
            pose.Has |= 4;
        }
    }

    [TlKind]
    public struct MotionTint
    {
        public static void Combine(ref Rgba total, in Rgba next) => total = next;

        public static void Apply(in Rgba total, ref Tint tint)
        {
            tint.Color = total;
            tint.Has = 1;
        }
    }

    public struct MoveTrack : ITlTrack<MoveClip>
    {
        [Feeds(typeof(MotionPosition))]
        public static Placed Evaluate(in TlFrame<MoveClip> frame)
            => new Placed { Value = Ease.Lerp(frame.Clip.From, frame.Clip.To, frame.Clip.Ease.Evaluate(frame.Progress)), Priority = frame.Clip.Priority };
    }

    public struct ScaleTrack : ITlTrack<ScaleClip>
    {
        [Feeds(typeof(MotionScale))]
        public static float3 Evaluate(in TlFrame<ScaleClip> frame) => Ease.Lerp(frame.Clip.From, frame.Clip.To, frame.Clip.Ease.Evaluate(frame.Progress));
    }

    public struct SpinTrack : ITlTrack<SpinClip>
    {
        [Feeds(typeof(MotionYaw))]
        public static float Evaluate(in TlFrame<SpinClip> frame) => frame.Clip.FromYaw + (frame.Clip.ToYaw - frame.Clip.FromYaw) * frame.Clip.Ease.Evaluate(frame.Progress);
    }

    public struct TintTrack : ITlTrack<TintClip>
    {
        [Feeds(typeof(MotionTint))]
        public static Rgba Evaluate(in TlFrame<TintClip> frame) => Rgba.Lerp(frame.Clip.From, frame.Clip.To, frame.Clip.Ease.Evaluate(frame.Progress));
    }
}
