namespace GeneratorPathProposal;

public interface ITimeline
{
    static abstract void Define(scoped Builder builder);
}

public interface ITimelineCatalog
{
    static abstract void Define(scoped CatalogBuilder builder);
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

public readonly ref struct CatalogSchema<TSchema>
    where TSchema : unmanaged
{
    public void Asset<TTimeline>()
        where TTimeline : unmanaged, ITimeline
    {
    }
}

public readonly ref struct CatalogBuilder
{
    public CatalogSchema<TSchema> Schema<TSchema>()
        where TSchema : unmanaged
        => default;
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

public readonly struct DamageRows;
public readonly struct MixedRows;

public readonly partial struct Combat : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<DamageRows>().Asset<DamageOnlyTimeline>();
        builder.Schema<MixedRows>().Asset<DamageAnimationTimeline>();
    }
}
