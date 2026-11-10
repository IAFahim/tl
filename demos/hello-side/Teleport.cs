using Tl;

namespace HelloSide
{
    [TlClip("Hello/Teleport")]
    public struct TeleportClip
    {
        public float Distance;
        public bool ComeBack;
        public float SideSwing; // 1. the clip carries the value
    }

    public struct TeleportTrack : ITlTrack<TeleportClip>
    {
        [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
        public static float JumpAtClipStart(in TlFrame<TeleportClip> frame)
            => frame.Direction * frame.Clip.Distance;

        // 2. a track method sends it to a kind. Without this line nothing reads SideSwing and the column stays 0.
        [Pulse(typeof(SideKind), On = ClipEvent.Start)]
        public static float SwingAtClipStart(in TlFrame<TeleportClip> frame)
            => frame.Direction * frame.Clip.SideSwing;

        [Pulse(typeof(TeleportKind), On = ClipEvent.End)]
        public static float ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
            => frame.Clip.ComeBack ? -frame.Direction * frame.Clip.Distance : 0f;
    }

    [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct TeleportKind
    {
        public static void Apply(in float jumpDistance, ref Teleported player)
            => player.Offset += jumpDistance;
    }

    // 3. the kind writes it into the column
    [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct SideKind
    {
        public static void Apply(in float swing, ref Teleported player)
            => player.SideSwing += swing;
    }

    [TlState]
    public partial struct Teleported
    {
        public float Offset;
        public float SideSwing; // 4. the column holds the result
    }
}
