using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class IncrementalTests
{
    const string SourceTemplate = """
        using Tl;
        namespace Portable;
        [TlState] public partial struct Push { public float X; }
        [TlClip("Push")] public struct PushClip { public float X; }
        [TlKind] public struct PushKind
        {
            public static void Combine(ref float total, in float next) => total += next;
            public static void Apply(in float total, ref Push push) => push.X += total;
        }
        public struct PushTrack : ITlTrack<PushClip>
        {
            [Feeds(typeof(PushKind))]
            public static float Evaluate(in TlFrame<PushClip> frame) => frame.Clip.X;
        }
        public static class Unrelated
        {
            public static int Value => __VALUE__;
        }
        """;

    [Fact]
    public void EditingAnUnrelatedMethodBodyLeavesPerTypeStepsCachedOrUnchanged()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToList();
        var first = CSharpCompilation.Create("Portable.Pack", [CSharpSyntaxTree.ParseText(SourceTemplate.Replace("__VALUE__", "1"), parse, "Push.cs")], references, options);
        var second = CSharpCompilation.Create("Portable.Pack", [CSharpSyntaxTree.ParseText(SourceTemplate.Replace("__VALUE__", "2"), parse, "Push.cs")], references, options);
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: parse, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));
        driver = driver.RunGeneratorsAndUpdateCompilation(first, out _, out _);
        var firstTrees = driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()).ToArray();
        Assert.NotEmpty(firstTrees);
        driver = driver.RunGeneratorsAndUpdateCompilation(second, out _, out _);
        var run = driver.GetRunResult();
        var secondTrees = run.GeneratedTrees.Select(tree => tree.ToString()).ToArray();
        Assert.Equal(firstTrees, secondTrees);
        var tracked = run.Results[0].TrackedSteps[TimelineIncrementalGenerator.TypeStep] ;
        foreach (var step in tracked)
            foreach (var output in step.Outputs)
                Assert.Contains(output.Reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged });
    }
}
