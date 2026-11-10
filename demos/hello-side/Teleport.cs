using Tl;

namespace HelloSide
{
    public struct Move
    {
        public float Forward;
        public float Side;
    }

    [TlClip("Hello/Teleport")]
    public struct TeleportClip
    {
        public float Distance;
        public bool ComeBack;
        public float Side;
    }

    public struct TeleportTrack : ITlTrack<TeleportClip>
    {
        [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
        public static Move JumpAtClipStart(in TlFrame<TeleportClip> frame)
            => new Move { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };

        [Pulse(typeof(TeleportKind), On = ClipEvent.End)]
        public static Move ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
            => frame.Clip.ComeBack
                ? new Move { Forward = -frame.Direction * frame.Clip.Distance, Side = -frame.Direction * frame.Clip.Side }
                : default;
    }

    [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct TeleportKind
    {
        public static void Apply(in Move move, ref Teleported player)
        {
            player.Offset += move.Forward;
            player.Side += move.Side;
        }
    }

    [TlState]
    public partial struct Teleported
    {
        public float Offset;
        public float Side;
    }
}
