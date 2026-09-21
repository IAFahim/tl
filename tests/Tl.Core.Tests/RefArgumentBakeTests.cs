using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct RefProbeClip(int Value);

public readonly record struct RefProbeTrack(int Code) : IBlend<RefProbeClip>
{
    public void Blend(in RefProbeClip first, in RefProbeClip second, float factor, out RefProbeClip result) => result = first;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct RefBakeToken(int Level);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct RefBakeTag(int Level);

public readonly record struct RefBakeCounter(int Value);

public readonly record struct RefBakePair(int Value);

public readonly record struct RefBakeTrio(int Value);

public unsafe class RefArgumentBakeTests
{
    private static readonly List<string> Log = [];

    private delegate void InputBake<T0>(ushort timeline, in T0 argument0);

    private delegate void ReferenceBake<T0>(ushort timeline, ref T0 argument0);

    private static byte[] ProbeBake(int clip) => new DomainBaker()
        .Track<RefProbeTrack, RefProbeClip>(new RefProbeTrack(1))
        .Clip(0, 0, 1, new RefProbeClip(clip))
        .Bake();

    [ModuleInitializer]
    internal static void Install()
    {
        BakeRuntime<RefProbeTrack, RefProbeClip>.Bake(&Increment, TypeKey<RefBakeCounter>.Value);
        BakeRuntime<RefProbeTrack, RefProbeClip>.Bake(&AddLevel, TypeKey<RefBakeToken>.Value, TypeKey<RefBakeCounter>.Value);
        BakeRuntime<RefProbeTrack, RefProbeClip>.Bake(&AddTag, TypeKey<RefBakeTag>.Value, TypeKey<RefBakeCounter>.Value);
        BakeRuntime<RefProbeTrack, RefProbeClip>.Bake(&AddPair, TypeKey<RefBakePair>.Value, TypeKey<RefBakePair>.Value);
        BakeRuntime<RefProbeTrack, RefProbeClip>.Bake(&AddTokenTrio, TypeKey<RefBakeToken>.Value, TypeKey<RefBakeTrio>.Value, TypeKey<RefBakeTrio>.Value);
    }

    [Fact]
    public void RefCallSiteMutationReachesTheCallerVariable()
    {
        var counter = new RefBakeCounter(7);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.BakeRef(timeline.Index, ref counter);

        Assert.Equal(8, counter.Value);
        Assert.Equal(["ref:increment:8"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void PlainAndExplicitInCallSitesStillBindTheInputFamily()
    {
        var counter = new RefBakeCounter(7);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.Bake(timeline.Index, counter);
        Assert.Equal(8, counter.Value);

        var explicitIn = new RefBakeCounter(20);
        Timeline.Bake(timeline.Index, in explicitIn);
        Assert.Equal(21, explicitIn.Value);

        Assert.Equal(["ref:increment:8", "ref:increment:21"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void MixedArityCallMutatesTheRefArgumentThroughTheDeclaredBinding()
    {
        var counter = new RefBakeCounter(7);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.BakeRef(timeline.Index, new RefBakeToken(5), ref counter);

        Assert.Equal(13, counter.Value);
        Assert.Equal(["ref:increment:8", "ref:addlevel:13"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void ReorderedArityThreeArgumentsStillBindByTypeKeyAndMutateTheRefArgument()
    {
        var counter = new RefBakeCounter(7);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.BakeRef(timeline.Index, new RefBakeTag(10), new RefBakeToken(5), ref counter);

        Assert.Equal(23, counter.Value);
        Assert.Equal(["ref:increment:8", "ref:addlevel:13", "ref:addtag:23"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void RepeatedTypeArgumentsBindDistinctParameterSlots()
    {
        var first = new RefBakePair(7);
        var second = new RefBakePair(20);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.Bake(timeline.Index, in first, in second);

        Assert.Equal(107, first.Value);
        Assert.Equal(21, second.Value);
        Assert.Equal(["ref:addpair:107:21"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void RepeatedTypeArgumentsInterleavedWithOtherTypesKeepTypeKeyedOrder()
    {
        var first = new RefBakeTrio(1);
        var second = new RefBakeTrio(2);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.Bake(timeline.Index, in first, new RefBakeToken(5), in second);

        Assert.Equal(6, first.Value);
        Assert.Equal(52, second.Value);
        Assert.Equal(["ref:tokentrio:6:52"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void RepeatedTypeParametersWithoutEnoughDistinctArgumentsDoNotFire()
    {
        var only = new RefBakePair(7);
        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;

        Timeline.Bake(timeline.Index, in only, new RefBakeToken(9));

        Assert.Equal(7, only.Value);
        Assert.Equal([], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void MethodGroupsBindBakeToTheInputFamilyAndBakeRefToTheRefFamily()
    {
        InputBake<RefBakeCounter> input = Timeline.Bake<RefBakeCounter>;
        ReferenceBake<RefBakeCounter> reference = Timeline.BakeRef<RefBakeCounter>;

        Assert.Equal("Bake", input.Method.Name);
        Assert.Equal("BakeRef", reference.Method.Name);
        Assert.True(input.Method.GetParameters()[1].ParameterType.IsByRef);
        Assert.True(reference.Method.GetParameters()[1].ParameterType.IsByRef);

        using var timeline = TimelineAsset.LoadAsset(ProbeBake(1));
        var before = Log.Count;
        var counter = new RefBakeCounter(7);

        reference(timeline.Index, ref counter);
        Assert.Equal(8, counter.Value);
        Assert.Equal(["ref:increment:8"], Log.GetRange(before, Log.Count - before));
    }

    [Fact]
    public void BakeRefSurfacesDeclareAWritableFinalRefParameter()
    {
        var bakeRef = typeof(Timeline).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "BakeRef" && method.GetParameters().Length >= 2)
            .OrderBy(method => method.GetParameters().Length)
            .ToList();

        Assert.Equal(4, bakeRef.Count);
        foreach (var method in bakeRef)
        {
            var final = method.GetParameters()[^1];
            Assert.True(final.ParameterType.IsByRef);
            Assert.False(final.IsIn);
            Assert.False(final.IsOut);
        }

        var bake = typeof(Timeline).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Bake" && method.GetParameters().Length >= 2)
            .ToList();

        Assert.Equal(4, bake.Count);
        foreach (var method in bake)
        {
            var final = method.GetParameters()[^1];
            Assert.True(final.ParameterType.IsByRef);
            Assert.True(final.IsIn);
            Assert.False(final.IsOut);
        }
    }

    [Fact]
    public void UnknownIndicesThrowTheInternTableDiagnosticThroughTheRefFamily()
    {
        var counter = new RefBakeCounter(7);
        var before = Log.Count;

        var thrown = Assert.Throws<ArgumentException>(() => Timeline.BakeRef(4242, ref counter));

        Assert.Contains("Timeline index 4242 is not loaded", thrown.Message);
        Assert.Equal([], Log.GetRange(before, Log.Count - before));
    }

    private static void Increment(byte** arguments)
    {
        ref var counter = ref Unsafe.AsRef<RefBakeCounter>(arguments[0]);
        counter = new RefBakeCounter(counter.Value + 1);
        Log.Add("ref:increment:" + counter.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AddLevel(byte** arguments)
    {
        ref var counter = ref Unsafe.AsRef<RefBakeCounter>(arguments[1]);
        counter = new RefBakeCounter(counter.Value + Unsafe.AsRef<RefBakeToken>(arguments[0]).Level);
        Log.Add("ref:addlevel:" + counter.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AddTag(byte** arguments)
    {
        ref var counter = ref Unsafe.AsRef<RefBakeCounter>(arguments[1]);
        counter = new RefBakeCounter(counter.Value + Unsafe.AsRef<RefBakeTag>(arguments[0]).Level);
        Log.Add("ref:addtag:" + counter.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AddPair(byte** arguments)
    {
        ref var first = ref Unsafe.AsRef<RefBakePair>(arguments[0]);
        ref var second = ref Unsafe.AsRef<RefBakePair>(arguments[1]);
        first = new RefBakePair(first.Value + 100);
        second = new RefBakePair(second.Value + 1);
        Log.Add("ref:addpair:" + first.Value.ToString(CultureInfo.InvariantCulture) + ":" + second.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AddTokenTrio(byte** arguments)
    {
        var level = Unsafe.AsRef<RefBakeToken>(arguments[0]).Level;
        ref var first = ref Unsafe.AsRef<RefBakeTrio>(arguments[1]);
        ref var second = ref Unsafe.AsRef<RefBakeTrio>(arguments[2]);
        first = new RefBakeTrio(first.Value + level);
        second = new RefBakeTrio(second.Value + level * 10);
        Log.Add("ref:tokentrio:" + first.Value.ToString(CultureInfo.InvariantCulture) + ":" + second.Value.ToString(CultureInfo.InvariantCulture));
    }
}
