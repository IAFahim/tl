using Tl;
using Tl.TestSupport;

namespace LiveBatch;

public readonly record struct LiftClip(float Amount);

public readonly record struct LiftTrack(float Scale) : IBlend<LiftClip>
{
    public void Blend(in LiftClip first, in LiftClip second, float factor, out LiftClip result)
        => result = new LiftClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct LiftFold : ITrack<LiftTrack, LiftClip>
{
    public static void Fold(in Frame<LiftTrack, LiftClip> frame, out float lift)
        => lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

public readonly record struct ArcClip(int Height);

public readonly record struct ArcTrack(float Scale) : IBlend<ArcClip>
{
    public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result)
        => result = first;
}

public readonly struct ArcCompose : ITrack<ArcTrack, ArcClip>
{
    public static void Fold(in Frame<ArcTrack, ArcClip> frame, out float arc)
        => arc = frame.Direction * frame.Clip.Height * frame.Track.Scale;

    public static void ExecuteActive(in float arc, ref float height, in int pushes)
        => height += arc * pushes;
}

public readonly record struct PushClip(int Height);

public readonly record struct PushTrack(float Scale) : IBlend<PushClip>
{
    public void Blend(in PushClip first, in PushClip second, float factor, out PushClip result)
        => result = first;
}

public readonly struct PushFrame : ITrack<PushTrack, PushClip>
{
    public static void ExecuteActive(in Frame<PushTrack, PushClip> frame, ref float total)
        => total += frame.Direction * frame.Clip.Height * frame.Track.Scale;
}

public readonly record struct D0Clip(int Amount);

public readonly record struct D0Track(float Scale) : IBlend<D0Clip>
{
    public void Blend(in D0Clip first, in D0Clip second, float factor, out D0Clip result)
        => result = first;
}

public readonly struct D0Job : ITrack<D0Track, D0Clip>
{
    public static void ExecuteActive(in Frame<D0Track, D0Clip> frame, ref float slot)
        => slot += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

public readonly record struct D1Clip(int Amount);

public readonly record struct D1Track(float Scale) : IBlend<D1Clip>
{
    public void Blend(in D1Clip first, in D1Clip second, float factor, out D1Clip result)
        => result = first;
}

public readonly struct D1Job : ITrack<D1Track, D1Clip>
{
    public static void ExecuteActive(in Frame<D1Track, D1Clip> frame, ref float slot)
        => slot -= frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

public readonly record struct D2Clip(int Amount);

public readonly record struct D2Track(float Scale) : IBlend<D2Clip>
{
    public void Blend(in D2Clip first, in D2Clip second, float factor, out D2Clip result)
        => result = first;
}

public readonly struct D2Job : ITrack<D2Track, D2Clip>
{
    public static void ExecuteActive(in Frame<D2Track, D2Clip> frame, ref float slot)
        => slot += frame.Direction * frame.Clip.Amount * (1 + frame.Track.Scale);
}

public readonly record struct D3Clip(int Amount);

public readonly record struct D3Track(float Scale) : IBlend<D3Clip>
{
    public void Blend(in D3Clip first, in D3Clip second, float factor, out D3Clip result)
        => result = first;
}

public readonly struct D3Job : ITrack<D3Track, D3Clip>
{
    public static void ExecuteActive(in Frame<D3Track, D3Clip> frame, ref float slot)
        => slot += 2 * frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal static class Host
{
    internal const int Variants = 8;
    internal const ushort Duration = 97;

    static TimelineAsset[]? _a;
    static TimelineAsset[]? _b;
    static TimelineAsset[]? _c;
    static TimelineAsset[]? _d;

    internal static ushort[] A => Indices(ref _a, static pattern => new DomainBaker()
        .Track<LiftTrack, LiftClip>(new LiftTrack(1 + 0.5f * pattern))
        .Clip(0, 0, 64, new LiftClip(1 + pattern))
        .Clip(0, 33, 97, new LiftClip(2 + 0.25f * pattern))
        .Looping().Bake());

    internal static ushort[] B => Indices(ref _b, static pattern => new DomainBaker()
        .Track<ArcTrack, ArcClip>(new ArcTrack(2))
        .Clip(0, 0, 64, new ArcClip(5 + pattern))
        .Clip(0, 33, 97, new ArcClip(3))
        .Looping().Bake());

    internal static ushort[] C => Indices(ref _c, static pattern => new DomainBaker()
        .Track<PushTrack, PushClip>(new PushTrack(1.5f))
        .Clip(0, 0, 97, new PushClip(2 + pattern))
        .Looping().Bake());

    internal static ushort[] D => Indices(ref _d, static pattern => new DomainBaker()
        .Track<D0Track, D0Clip>(new D0Track(1 + pattern))
        .Clip(0, 0, 97, new D0Clip(2))
        .Track<D1Track, D1Clip>(new D1Track(1))
        .Clip(1, 0, 97, new D1Clip(3 + pattern))
        .Track<D2Track, D2Clip>(new D2Track(1))
        .Clip(2, 0, 97, new D2Clip(4))
        .Track<D3Track, D3Clip>(new D3Track(1))
        .Clip(3, 0, 97, new D3Clip(5))
        .Looping().Bake());

    static ushort[] Indices(ref TimelineAsset[]? owned, Func<int, byte[]> bake)
    {
        owned ??= Enumerable.Range(0, Variants)
            .Select(pattern => TimelineAsset.Of(TimelineAsset.Load(bake(pattern)))).ToArray();
        return Array.ConvertAll(owned, static asset => asset.Index);
    }
}
