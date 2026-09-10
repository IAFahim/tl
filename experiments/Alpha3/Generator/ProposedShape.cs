namespace GeneratorPathProposal;

public interface ITimeline
{
    static abstract void Define(scoped Builder builder);
}

public interface ITimelineSet
{
    static abstract void Define(scoped TimelineSetBuilder builder);
}

public interface ITimelineJob<TTrack, TClip>
    where TTrack : unmanaged
    where TClip : unmanaged;

public readonly ref struct TrackRef<TTrack>
    where TTrack : unmanaged
{
    public TrackRef<TTrack, TJob> Use<TJob>()
        where TJob : unmanaged
        => default;
}

public readonly ref struct TrackRef<TTrack, TJob>
    where TTrack : unmanaged
    where TJob : unmanaged;

public readonly ref struct Builder
{
    public TrackRef<TTrack> Track<TTrack>(in TTrack track)
        where TTrack : unmanaged
        => default;

    public void Clip<TTrack, TJob, TClip>(
        in TrackRef<TTrack, TJob> track,
        in TClip clip,
        uint start,
        uint end)
        where TJob : unmanaged, ITimelineJob<TTrack, TClip>
        where TTrack : unmanaged
        where TClip : unmanaged
    {
    }
}

public readonly ref struct TimelineSetBuilder
{
    public void Include<TTimeline>()
        where TTimeline : unmanaged, ITimeline
    {
    }
}

public readonly record struct DamageTrack(int Multiplier);
public readonly record struct DamageClip(int Amount);
public readonly record struct OtherClip(int Amount);
public readonly record struct AnimationTrack(int Layer);
public readonly record struct AnimationClip(int Step);
public readonly record struct Resistance(int Scale);
public record struct Health(int Value);
public record struct Pose(int Frame);

public readonly partial struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        in Resistance resistance,
        ref Health health)
        => health.Value -= frame.Direction * frame.Clip.Amount
            * frame.Track.Multiplier * resistance.Scale;
}

public readonly partial struct AnimationJob : ITimelineJob<AnimationTrack, AnimationClip>
{
    public static void Execute(
        in Frame<AnimationTrack, AnimationClip> frame,
        ref Pose pose)
        => pose.Frame += frame.Direction * frame.Clip.Step;
}

public readonly ref struct Frame<TTrack, TClip>
    where TTrack : unmanaged
    where TClip : unmanaged
{
    private readonly ref readonly TTrack _track;
    private readonly ref readonly TClip _clip;

    public Frame(in TTrack track, in TClip clip, int direction)
    {
        _track = ref track;
        _clip = ref clip;
        Direction = direction;
    }

    public ref readonly TTrack Track => ref _track;
    public ref readonly TClip Clip => ref _clip;
    public int Direction { get; }
}

public readonly partial struct DamageOnlyTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
        builder.Clip(damage, new DamageClip(7), 0u, 10u);
#if INVALID_MAPPING
        builder.Clip(damage, new OtherClip(7), 0u, 10u);
#endif
    }
}

public readonly partial struct DamageAnimationTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
        var animation = builder.Track(new AnimationTrack(1)).Use<AnimationJob>();
        builder.Clip(damage, new DamageClip(7), 0u, 10u);
        builder.Clip(animation, new AnimationClip(1), 0u, 10u);
    }
}

public readonly partial struct Combat : ITimelineSet
{
    public static void Define(scoped TimelineSetBuilder builder)
    {
        builder.Include<DamageOnlyTimeline>();
        builder.Include<DamageAnimationTimeline>();
    }
}

public readonly record struct TimelineState(uint Asset, long Position);

public readonly partial struct Combat
{
    public readonly ref struct Query
    {
        public DamageOnlyView DamageOnly(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health)
            => new(timelines, resistances, health);

        public MixedView Mixed(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health,
            Span<Pose> poses)
            => new(timelines, resistances, health, poses);
    }

    public readonly ref struct DamageOnlyView
    {
        public DamageOnlyView(
            Span<TimelineState> timelines,
            ReadOnlySpan<Resistance> resistances,
            Span<Health> health)
        {
        }

        public void Tick(uint gameTick, int delta = 1)
        {
        }
    }

    public readonly ref struct MixedView
    {
        public MixedView(
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
        query.DamageOnly(timelines, resistances, health).Tick(200000u);
        query.Mixed(timelines, resistances, health, poses).Tick(200000u);
    }
}
