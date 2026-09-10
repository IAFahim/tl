using Tl.Gen.CSharp;
using Tl.Gen.Model;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class CompatibleRoutingTests
{
    [Fact]
    public void CompilationPartitionsEveryUshortRouteAcrossByteModules()
    {
        var timelines = Enumerable.Range(0, 257)
            .Select(index => Timeline($"Timeline{index:D3}", [], []))
            .ToArray();

        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines);
        var modules = Assert.Single(artifacts, static artifact => artifact.RelativePath == "TlModules.g.cs").Content;

        Assert.Contains("Module0 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.Contains("Module1 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.DoesNotContain("Module2 = global::Tl.Timeline.RegisterModule()", modules);
        Assert.Contains("CompiledRoute(global::__TlGeneratedModules.Module0, 255)", artifacts.Single(static artifact => artifact.RelativePath == "Tl255.g.cs").Content);
        Assert.Contains("CompiledRoute(global::__TlGeneratedModules.Module1, 0)", artifacts.Single(static artifact => artifact.RelativePath == "Tl256.g.cs").Content);
        Assert.Single(artifacts, static artifact => artifact.RelativePath.StartsWith("TlSchema", StringComparison.Ordinal));
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
        var largeRouter = Assert.Single(artifacts, static artifact => artifact.RelativePath == "TlSchema0.g.cs").Content;
        var largeKernel = Assert.Single(artifacts, static artifact => artifact.RelativePath == "Tl0.g.cs").Content;
        var smallRouter = Assert.Single(artifacts, static artifact => artifact.RelativePath == "TlSchema1.g.cs").Content;
        var smallKernel = Assert.Single(artifacts, static artifact => artifact.RelativePath == "Tl1.g.cs").Content;

        Assert.Contains("new global::Fix.__TlGeneratedSchema1.Data(in data._pose, ref data._nextPose)", largeRouter);
        Assert.Contains("global::Fix.Small.DynamicSeekKernel", largeRouter);
        Assert.DoesNotContain("global::Fix.Large.DynamicSeekKernel", smallRouter);
        Assert.Equal(1, Occurrences(largeRouter, "Timeline.TryGetCompiledRoute(id, out var route)"));
        Assert.DoesNotContain("TryGetCompiledRoute", largeKernel);
        Assert.Equal(1, Occurrences(smallKernel, "internal static bool DynamicSeekKernel("));
        Assert.Equal(1, Occurrences(smallKernel, "private static bool SeekCore("));
    }

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
