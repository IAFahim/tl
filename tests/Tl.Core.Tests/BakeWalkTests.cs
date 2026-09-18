using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public sealed class BakeHost(string name)
{
    public readonly string Name = name;
}

public readonly record struct BakeId(int Value);

public readonly record struct BakeMissingToken(int Level);

public unsafe class BakeWalkTests
{
    private static readonly List<string> Log = [];

    private static byte[] TwoPairBake(int alphaClip, int gammaClip) => new DomainBaker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
        .Clip(0, 0, 1, new AlphaClip(alphaClip))
        .Track<GammaTrack, GammaClip>(new GammaTrack(1))
        .Clip(1, 0, 1, new GammaClip(gammaClip))
        .Bake();

    private static List<string> GammaLast(params string[] alpha) =>
        PairRuntime<AlphaTrack, AlphaClip>.Key < PairRuntime<GammaTrack, GammaClip>.Key
            ? [.. alpha, "gamma:world:A"]
            : ["gamma:world:A", .. alpha];

    [ModuleInitializer]
    internal static void Install()
    {
        BakeRuntime<AlphaTrack, AlphaClip>.Bake(&AlphaWide, TypeKey<BakeHost>.Value, TypeKey<BakeId>.Value);
        BakeRuntime<AlphaTrack, AlphaClip>.Bake(&AlphaOnlyId, TypeKey<BakeId>.Value);
        BakeRuntime<AlphaTrack, AlphaClip>.Bake(&AlphaZero);
        BakeRuntime<AlphaTrack, AlphaClip>.Bake(&AlphaMissing, TypeKey<BakeHost>.Value, TypeKey<BakeId>.Value, TypeKey<BakeMissingToken>.Value);
        BakeRuntime<GammaTrack, GammaClip>.Bake(&GammaWorld, TypeKey<BakeHost>.Value);
    }

    [Fact]
    public void ZeroArgumentBakeRunsOnlyZeroContextBakes()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(1, 2));

        Timeline.Bake(timeline.Index);

        Assert.Equal(["alpha:zero"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void SubsetSatisfactionRunsEveryMatchInChainOrderAcrossPairsInPairKeyOrder()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(3, 4));

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeId(5));

        Assert.Equal(GammaLast("alpha:wide:A:5", "alpha:id:5", "alpha:zero"), Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void RepeatedBakesAreDeterministic()
    {
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(5, 6));
        var first = Log.Count;

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeId(6));
        var recorded = Log.GetRange(first, Log.Count - first);
        var second = Log.Count;

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeId(6));

        Assert.Equal(recorded, Log.GetRange(second, Log.Count - second));
        Assert.Equal(GammaLast("alpha:wide:A:6", "alpha:id:6", "alpha:zero"), recorded);
    }

    [Fact]
    public void CallerArgumentOrderDoesNotChangeTheDeclaredBinding()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(7, 8));

        Timeline.Bake(timeline.Index, new BakeId(7), new BakeHost("A"));

        Assert.Equal(GammaLast("alpha:wide:A:7", "alpha:id:7", "alpha:zero"), Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void BakesWhoseContextTypesAreNotPresentStaySilent()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(9, 10));

        Timeline.Bake(timeline.Index, new BakeHost("A"));

        Assert.Equal(GammaLast("alpha:zero"), Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void ExtraArgumentTypesAreIgnoredAndUnrelatedTypesDoNotSatisfy()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(11, 12));

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeId(9), 42);

        Assert.Equal(GammaLast("alpha:wide:A:9", "alpha:id:9", "alpha:zero"), Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void DuplicateArgumentTypesSatisfyAContextOnceWithTheFirstArgument()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(13, 14));

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeHost("B"));

        Assert.Equal(GammaLast("alpha:zero"), Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void ContextIdentityIsTheStaticArgumentType()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(TwoPairBake(15, 16));

        Timeline.Bake(timeline.Index, (object)new BakeHost("A"), new BakeId(11));

        Assert.Equal(["alpha:id:11", "alpha:zero"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void PairsWithoutInstalledBakesAreSilent()
    {
        var before = Log.Count;
        using var timeline = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<BetaTrack, BetaClip>(new BetaTrack(1))
            .Clip(0, 0, 1, new BetaClip(17))
            .Bake());

        Timeline.Bake(timeline.Index, new BakeHost("A"), new BakeId(13));

        Assert.Equal([], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void UnknownIndicesThrowTheInternTableDiagnostic()
    {
        var before = Log.Count;

        var thrown = Assert.Throws<ArgumentException>(() => Timeline.Bake(4242, new BakeHost("A")));

        Assert.Contains("Timeline index 4242 is not loaded", thrown.Message);
        Assert.Equal([], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void DeadIndicesThrowTheInternTableDiagnostic()
    {
        var before = Log.Count;
        var timeline = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(19))
            .Bake());
        var index = timeline.Index;
        timeline.Dispose();

        var thrown = Assert.Throws<ArgumentException>(() => Timeline.Bake(index, new BakeHost("A"), new BakeId(15)));

        Assert.Contains($"Timeline index {index} is not loaded", thrown.Message);
        Assert.Equal([], Log.GetRange(before, Log.Count - before));
    }

    private static void AlphaWide(object[] arguments)
        => Log.Add("alpha:wide:" + ((BakeHost)arguments[0]).Name + ":" + ((BakeId)arguments[1]).Value.ToString(CultureInfo.InvariantCulture));

    private static void AlphaOnlyId(object[] arguments)
        => Log.Add("alpha:id:" + ((BakeId)arguments[0]).Value.ToString(CultureInfo.InvariantCulture));

    private static void AlphaZero(object[] arguments) => Log.Add("alpha:zero");

    private static void AlphaMissing(object[] arguments) => Log.Add("alpha:never");

    private static void GammaWorld(object[] arguments) => Log.Add("gamma:world:" + ((BakeHost)arguments[0]).Name);
}
