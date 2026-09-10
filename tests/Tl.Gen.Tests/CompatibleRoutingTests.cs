using Tl.Gen.CSharp;
using Tl.Gen.Model;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class CompatibleRoutingTests
{
    [Fact]
    public void SingleCompatibleTargetUsesItsKernelWithoutRouterState()
    {
        HeterogeneousTimeline[] timelines = [Timeline("Only", [], [])];

        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines, out var sharedBytes);
        var schema = Artifact(artifacts, "TlSchema0.g.cs");
        var timeline = Artifact(artifacts, "Tl0.g.cs");

        Assert.Equal(1, sharedBytes);
        Assert.DoesNotContain("ModuleMap", schema);
        Assert.DoesNotContain("TryGetCompiledRoute", schema);
        Assert.DoesNotContain("internal static bool TrySeek(", schema);
        Assert.Contains("return global::Fix.Only.DynamicSeekKernel(id, ref data._playback, delta, in data._context);", timeline);
    }

    [Fact]
    public void CompatibleTargetsInOneModuleUseDirectModuleAndOrdinalDispatch()
    {
        var timelines = Enumerable.Range(0, 16)
            .Select(index => Timeline($"Timeline{index:D3}", [], []))
            .ToArray();

        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines, out var sharedBytes);
        var schema = Artifact(artifacts, "TlSchema0.g.cs");

        Assert.Equal(1, sharedBytes);
        Assert.DoesNotContain("ModuleMap", schema);
        Assert.Contains("if (route.Module != global::__TlGeneratedModules.Module0)", schema);
        Assert.Contains("switch (route.Ordinal)", schema);
        Assert.Contains("case 0:", schema);
        Assert.Contains("case 15:", schema);
    }

    [Fact]
    public void CompatibleTargetsAcrossModulesUseOneByteMap()
    {
        var timelines = Enumerable.Range(0, 257)
            .Select(index => Timeline($"Timeline{index:D3}", [], []))
            .ToArray();

        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines, out var sharedBytes);
        var modules = Artifact(artifacts, "TlModules.g.cs");
        var schema = Artifact(artifacts, "TlSchema0.g.cs");

        Assert.Equal(258, sharedBytes);
        Assert.Contains("Module0 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.Contains("Module1 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.DoesNotContain("Module2 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.Contains("private byte _element0;", schema);
        Assert.DoesNotContain("private ushort _element0;", schema);
        Assert.Contains("modules[global::__TlGeneratedModules.Module0] = 1;", schema);
        Assert.Contains("modules[global::__TlGeneratedModules.Module1] = 2;", schema);
        Assert.Contains("CompiledRoute(global::__TlGeneratedModules.Module0, 255)", Artifact(artifacts, "Tl255.g.cs"));
        Assert.Contains("CompiledRoute(global::__TlGeneratedModules.Module1, 0)", Artifact(artifacts, "Tl256.g.cs"));
    }

    [Fact]
    public void MaximumModuleUsesDirectSentinelBranchAtFullCapacity()
    {
        var caller = Timeline(
            "Caller",
            [new TimelineSlot("caller", "CallerState", SlotMode.Input)],
            []);
        var filler = Timeline(
            "Filler",
            [new TimelineSlot("filler", "FillerState", SlotMode.Input)],
            []);
        var timelines = Enumerable.Repeat(filler, ushort.MaxValue + 1).ToArray();
        timelines[0] = caller;
        timelines[254 * 256] = caller;
        timelines[255 * 256] = caller;
        timelines[ushort.MaxValue] = caller;

        var metrics = HeterogeneousEmitter.RoutingMetrics(timelines, 0);
        var schema = HeterogeneousEmitter.EmitSchema(timelines, 0);
        var sentinel = schema.IndexOf("if (route.Module == global::__TlGeneratedModules.Module255)", StringComparison.Ordinal);
        var map = schema.IndexOf("switch (s_modules[route.Module])", StringComparison.Ordinal);

        Assert.Equal((4, 3, true, 256), metrics);
        Assert.True(sentinel >= 0 && sentinel < map);
        Assert.Contains("modules[global::__TlGeneratedModules.Module254] = 255;", schema);
        Assert.DoesNotContain("modules[global::__TlGeneratedModules.Module255]", schema);
        Assert.Contains("case 255:", schema);

        var oneSchema = Enumerable.Repeat(caller, ushort.MaxValue + 1).ToArray();
        Assert.Equal(512, HeterogeneousEmitter.SharedDispatchValueBytes(oneSchema));
    }

    [Fact]
    public void SupersetSchemaRoutesToSubsetKernelWithoutDuplicatingKernel()
    {
        TimelineSlot[] smallInputs = [new("pose", "Pose", SlotMode.Input)];
        TimelineSlot[] smallOutputs = [new("nextPose", "Pose", SlotMode.Reference)];
        TimelineSlot[] largeInputs = [new("health", "Health", SlotMode.Input), .. smallInputs];
        TimelineSlot[] largeOutputs = [new("nextHealth", "Health", SlotMode.Reference), .. smallOutputs];
        HeterogeneousTimeline[] timelines =
        [
            Timeline("Large", largeInputs, largeOutputs),
            Timeline("Small", smallInputs, smallOutputs),
        ];

        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines);
        var largeRouter = Artifact(artifacts, "TlSchema0.g.cs");
        var largeKernel = Artifact(artifacts, "Tl0.g.cs");
        var smallSchema = Artifact(artifacts, "TlSchema1.g.cs");
        var smallKernel = Artifact(artifacts, "Tl1.g.cs");

        Assert.DoesNotContain("ModuleMap", largeRouter);
        Assert.Contains("if (route.Module != global::__TlGeneratedModules.Module0)", largeRouter);
        Assert.Contains("new global::Fix.__TlGeneratedSchema1.Data(in data._pose, ref data._nextPose)", largeRouter);
        Assert.Contains("global::Fix.Small.DynamicSeekKernel", largeRouter);
        Assert.DoesNotContain("global::Fix.Large.DynamicSeekKernel", smallSchema);
        Assert.DoesNotContain("TryGetCompiledRoute", smallSchema);
        Assert.Equal(1, Occurrences(largeRouter, "Timeline.TryGetCompiledRoute(id, out var route)"));
        Assert.DoesNotContain("TryGetCompiledRoute", largeKernel);
        Assert.Equal(1, Occurrences(smallKernel, "internal static bool DynamicSeekKernel("));
        Assert.Equal(1, Occurrences(smallKernel, "private static bool SeekCore("));
    }

    private static string Artifact(IReadOnlyList<CompileArtifact> artifacts, string path)
        => Assert.Single(artifacts, artifact => artifact.RelativePath == path).Content;

    private static HeterogeneousTimeline Timeline(
        string name,
        IReadOnlyList<TimelineSlot> inputs,
        IReadOnlyList<TimelineSlot> outputs)
        => new(name, "Fix", false, [], [], [], [], [], inputs, outputs);

    private static int Occurrences(string source, string value)
    {
        var count = 0;
        for (var start = 0; (start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0; start += value.Length)
            count++;
        return count;
    }
}
