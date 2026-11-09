namespace Tl.Pack.Stats
{
    [TlColumn]
    public partial struct Health
    {
        public int Value;
        public int Max;
    }

    [TlColumn]
    public partial struct Attack
    {
        public int Base;
        public int Bonus;
        public int Value => Base + Bonus;
    }

    [TlClip("Stats/Hit")]
    public struct HitClip
    {
        public int Amount;
    }

    [TlClip("Stats/Heal")]
    public struct HealClip
    {
        public int Amount;
    }

    [TlClip("Stats/Poison")]
    public struct PoisonClip
    {
        public int Amount;
        public int Every;
    }

    [TlClip("Stats/Buff")]
    public struct BuffClip
    {
        public int AttackBonus;
    }

    [TlKind(Accepts = KindEntries.Both, GamePulses = 8)]
    public struct StatsDamage
    {
        public static void Combine(ref int total, in int next) => total += next;
        public static void Apply(in int total, ref Health health) => health.Value -= total;
    }

    [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct StatsAttackBonus
    {
        public static void Apply(in int delta, ref Attack attack) => attack.Bonus += delta;
    }

    public struct HitTrack : ITlTrack<HitClip>
    {
        [Pulse(typeof(StatsDamage), On = ClipEvent.Start)]
        public static int Hit(in TlFrame<HitClip> frame) => frame.Direction * frame.Clip.Amount;
    }

    public struct HealTrack : ITlTrack<HealClip>
    {
        [Pulse(typeof(StatsDamage), On = ClipEvent.Start)]
        public static int Heal(in TlFrame<HealClip> frame) => -frame.Direction * frame.Clip.Amount;
    }

    public struct PoisonTrack : ITlTrack<PoisonClip>
    {
        [Feeds(typeof(StatsDamage))]
        public static int Tick(in TlFrame<PoisonClip> frame) => frame.Clip.Every > 0 && frame.WithinClip % frame.Clip.Every == 0 ? frame.Direction * frame.Clip.Amount : 0;
    }

    public struct BuffTrack : ITlTrack<BuffClip>
    {
        [Pulse(typeof(StatsAttackBonus), On = ClipEvent.Start)]
        public static int Gain(in TlFrame<BuffClip> frame) => frame.Direction * frame.Clip.AttackBonus;

        [Pulse(typeof(StatsAttackBonus), On = ClipEvent.End)]
        public static int Lose(in TlFrame<BuffClip> frame) => -frame.Direction * frame.Clip.AttackBonus;
    }
}
