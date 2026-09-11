namespace GeneratorPathProposal;

// This is a handwritten compile-only placeholder for the surface TL would generate.
// The current Tl.Gen.CSharp generator does not emit this partial type.
public readonly record struct TimelineState(uint Asset, long Position);

public readonly partial struct Combat
{
    public readonly ref struct Query
    {
        public DamageRowsView DamageRows(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health)
            => new(timelines, resistances, health);

        public MixedRowsView MixedRows(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health,
            Span<Pose> poses)
            => new(timelines, resistances, health, poses);
    }

    public readonly ref struct DamageRowsView
    {
        public DamageRowsView(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health)
        {
        }

        public void Tick(uint gameTick, int delta = 1)
        {
        }
    }

    public readonly ref struct MixedRowsView
    {
        public MixedRowsView(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health,
            Span<Pose> poses)
        {
        }

        public void Tick(uint gameTick, int delta = 1)
        {
        }
    }
}

internal static class ProposalReceipt
{
    internal static void Verify()
    {
        DamageOnlyTimeline.Define(default);
        DamageAnimationTimeline.Define(default);
        Combat.Define(default);

        Span<TimelineState> timelines = stackalloc TimelineState[1];
        Span<Resistance> resistances = stackalloc Resistance[1];
        Span<Health> health = stackalloc Health[1];
        Span<Pose> poses = stackalloc Pose[1];
        var query = new Combat.Query();
        query.DamageRows(timelines, resistances, health).Tick(200000u);
        query.MixedRows(timelines, resistances, health, poses).Tick(200000u);
    }
}
