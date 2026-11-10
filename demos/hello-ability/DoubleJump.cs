using Tl;

namespace HelloAbility
{
    [TlClip("Hello/DoubleJumpWindow")]
    public struct DoubleJumpWindowClip
    {
        public bool CanDoubleJump;
    }

    public struct DoubleJumpWindowTrack : ITlTrack<DoubleJumpWindowClip>
    {
        [Feeds(typeof(DoubleJumpKind))]
        public static bool WhetherDoubleJumpIsAllowed(in TlFrame<DoubleJumpWindowClip> frame)
            => frame.Clip.CanDoubleJump;
    }

    [TlKind]
    public struct DoubleJumpKind
    {
        public static void Combine(ref bool total, in bool next) => total |= next;

        public static void Apply(in bool allowed, ref Abilities player)
            => player.CanDoubleJump = allowed;
    }

    [TlState]
    public partial struct Abilities
    {
        public bool CanDoubleJump;
    }
}
