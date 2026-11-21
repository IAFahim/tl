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

    static readonly CSharpParseOptions Parse = new(LanguageVersion.Latest);

    static CSharpCompilation Compile(params (string Path, string Text)[] files)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToList();
        return CSharpCompilation.Create("Portable.Pack", files.Select(file => CSharpSyntaxTree.ParseText(file.Text, Parse, file.Path)), references, options);
    }

    static GeneratorDriver NewDriver()
        => CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: Parse, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));

    static (GeneratorDriverRunResult Run, string[] Trees) Step(ref GeneratorDriver driver, Compilation compilation)
    {
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        var run = driver.GetRunResult();
        return (run, run.GeneratedTrees.Select(tree => tree.FilePath + "\n" + tree).ToArray());
    }

    static IncrementalStepRunReason[] Reasons(GeneratorDriverRunResult run, string step)
        => run.Results[0].TrackedSteps[step].SelectMany(trace => trace.Outputs).Select(output => output.Reason).ToArray();

    [Fact]
    public void EditingAnUnrelatedMethodBodyLeavesEveryStepCachedOrUnchanged()
    {
        var driver = NewDriver();
        var (_, firstTrees) = Step(ref driver, Compile(("Push.cs", SourceTemplate.Replace("__VALUE__", "1"))));
        Assert.NotEmpty(firstTrees);
        var (run, secondTrees) = Step(ref driver, Compile(("Push.cs", SourceTemplate.Replace("__VALUE__", "2222"))));
        Assert.Equal(firstTrees, secondTrees);
        var typeReasons = Reasons(run, TimelineIncrementalGenerator.TypeStep);
        Assert.NotEmpty(typeReasons);
        Assert.All(typeReasons, reason => Assert.Contains(reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }));
        Assert.All(Reasons(run, "Tl.Models"), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        Assert.All(Reasons(run, "Tl.Ordered"), reason => Assert.Equal(IncrementalStepRunReason.Unchanged, reason));
        var assigned = Reasons(run, "Tl.Systems");
        Assert.NotEmpty(assigned);
        Assert.All(assigned, reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    [Fact]
    public void EditingAnUnrelatedFileDoesNotRunAssignment()
    {
        var driver = NewDriver();
        var (_, firstTrees) = Step(ref driver, Compile(("Push.cs", SourceTemplate.Replace("__VALUE__", "1")), ("Other.cs", "public static class Other { public static int V => 1; }")));
        Assert.NotEmpty(firstTrees);
        var (run, secondTrees) = Step(ref driver, Compile(("Push.cs", SourceTemplate.Replace("__VALUE__", "1")), ("Other.cs", "public static class Other { public static int V => 1; public static int W => 2; }\npublic class Extra { }")));
        Assert.Equal(firstTrees, secondTrees);
        Assert.All(Reasons(run, "Tl.Systems"), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        Assert.All(Reasons(run, TimelineIncrementalGenerator.TypeStep), reason => Assert.Contains(reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }));
    }

    [Fact]
    public void AddingIBlendToOnlyTheClipFileSwitchesTheFrameToTheClipsBlendAndRemovingItSwitchesBack()
    {
        const string track = "using Tl;\nnamespace Portable;\n[TlState] public partial struct Glow { public float X; }\n[TlKind] public struct GlowKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Glow glow) => glow.X = total; }\npublic struct GlowTrack : ITlTrack<GlowClip> { [Feeds(typeof(GlowKind))] public static float Evaluate(in TlFrame<GlowClip> frame) => frame.Clip.X; }\n";
        const string plainClip = "using Tl;\nnamespace Portable;\n[TlClip(\"Glow\")] public struct GlowClip { public float X; }\n";
        const string blendingClip = "using Tl;\nnamespace Portable;\n[TlClip(\"Glow\")] public struct GlowClip : IBlend<GlowClip> { public float X; public void Blend(in GlowClip first, in GlowClip second, float factor, out GlowClip result) => result = new GlowClip { X = first.X + (second.X - first.X) * factor }; }\n";
        var driver = NewDriver();
        var (_, plain) = Step(ref driver, Compile(("Track.cs", track), ("Clip.cs", plainClip)));
        Assert.Contains(plain, tree => tree.Contains("track.Blend(in clip, in __second, blend, out clip);", StringComparison.Ordinal));
        Assert.DoesNotContain(plain, tree => tree.Contains("clip = __Blend(clip, in __second, blend);", StringComparison.Ordinal));
        var (_, blending) = Step(ref driver, Compile(("Track.cs", track), ("Clip.cs", blendingClip)));
        Assert.Contains(blending, tree => tree.Contains("clip = __Blend(clip, in __second, blend);", StringComparison.Ordinal));
        Assert.DoesNotContain(blending, tree => tree.Contains("track.Blend(in clip, in __second, blend, out clip);", StringComparison.Ordinal));
        var (_, back) = Step(ref driver, Compile(("Track.cs", track), ("Clip.cs", plainClip)));
        Assert.Equal(plain, back);
    }

    [Fact]
    public void EditingOnlyThePartialFileThatHoldsAFeedersFoldMovesItsKindOnAndOffThePrecombinedPath()
    {
        const string feeder = "using System;\nusing Tl;\nnamespace Portable;\npublic struct Lift { public float Y; }\npublic struct LiftClip { public float Y; }\npublic struct LiftTrack : IBlend<LiftClip> { public void Blend(in LiftClip first, in LiftClip second, float factor, out LiftClip result) => result = first; }\n[TimelineSystem] public ref partial struct LiftSystem { public Span<Lift> Lift; }\n[TimelineKind(Associative = true)] public struct LiftKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lift lift) => lift.Y += total; }\npublic partial struct Jump : ITrack<LiftTrack, LiftClip> { [Feeds(typeof(LiftKind))] public static float Rise(in float height) => height; }\n";
        const string pureFold = "using Tl;\nnamespace Portable;\npublic partial struct Jump { public static void Fold(in Frame<LiftTrack, LiftClip> frame, out float height) => height = frame.Clip.Y; }\n";
        const string syntheticFold = "using Tl;\nnamespace Portable;\npublic partial struct Jump { public static void Fold(in Frame<LiftTrack, LiftClip> frame, out float height) => height = frame.Has(FrameFlags.Synthetic) ? 0f : frame.Clip.Y; }\n";
        static string[] Codes(GeneratorDriverRunResult run) => run.Diagnostics.Select(static d => d.Id + ": " + d.GetMessage()).ToArray();
        static (string[] Codes, string[] Trees) Fresh(Compilation compilation)
        {
            var driver = NewDriver();
            var (run, trees) = Step(ref driver, compilation);
            return (Codes(run), trees);
        }
        var parse = (string text) => CSharpSyntaxTree.ParseText(text, Parse, "Fold.cs");
        var pure = Compile(("Jump.cs", feeder), ("Fold.cs", pureFold));
        var foldTree = pure.SyntaxTrees.Single(static tree => tree.FilePath == "Fold.cs");
        var incremental = NewDriver();
        var (run, precombined) = Step(ref incremental, pure);
        Assert.Empty(Codes(run));
        Assert.Contains(precombined, static tree => tree.Contains("SpanValues<float, KindSpans_0>", StringComparison.Ordinal));
        Assert.Equal(Fresh(pure).Trees, precombined);
        var synthetic = pure.ReplaceSyntaxTree(foldTree, parse(syntheticFold));
        Assert.Same(pure.SyntaxTrees.Single(static tree => tree.FilePath == "Jump.cs"), synthetic.SyntaxTrees.Single(static tree => tree.FilePath == "Jump.cs"));
        (run, var walked) = Step(ref incremental, synthetic);
        var info = Assert.Single(Codes(run));
        Assert.StartsWith("TLGEN115: Kind 'LiftKind' declares Associative but keeps the per-span path in system 'global::Portable.LiftSystem': feeder 'Portable.Jump.Rise' takes a fold whose Fold reads FrameFlags.Synthetic.", info);
        Assert.DoesNotContain(walked, static tree => tree.Contains("SpanValues<", StringComparison.Ordinal));
        var fresh = Fresh(synthetic);
        Assert.Equal(fresh.Codes, Codes(run));
        Assert.Equal(fresh.Trees, walked);
        var back = synthetic.ReplaceSyntaxTree(synthetic.SyntaxTrees.Single(static tree => tree.FilePath == "Fold.cs"), parse(pureFold));
        (run, var again) = Step(ref incremental, back);
        Assert.Empty(Codes(run));
        Assert.Equal(precombined, again);
    }

    [Fact]
    public void FlagReadsOfMatchesExactOverloadWhenFeederNameAndParameterCountCollide()
    {
        foreach (var pureFirst in new[] { true, false })
        {
            const string shared = """
                using System;
                using Tl;
                namespace Portable;
                public struct OverState { public float A; public float B; }
                public struct OverClip { public float V; }
                public struct OverTrack : IBlend<OverClip> { public void Blend(in OverClip first, in OverClip second, float factor, out OverClip result) => result = first; }
                [TimelineSystem] public ref partial struct OverSystem { public Span<OverState> State; }
                [TimelineKind(Associative = true)] public struct KindA { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref OverState state) => state.A += total; }
                [TimelineKind(Associative = true)] public struct KindB { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref OverState state) => state.B += total; }
                """;
            const string pureMethod = "[Feeds(typeof(KindA))] public static float Eval(in float v) => v;";
            const string syntheticMethod = "[Feeds(typeof(KindB))] public static float Eval(in Frame<OverTrack, OverClip> f) => f.Has(FrameFlags.Synthetic) ? 0f : f.Clip.V;";
            var methods = pureFirst ? $"{pureMethod}\n    {syntheticMethod}" : $"{syntheticMethod}\n    {pureMethod}";
            var consumer = shared + "\npublic struct OverTrackConsumer : ITrack<OverTrack, OverClip>\n{\n    public static void Fold(in Frame<OverTrack, OverClip> frame, out float v) => v = frame.Clip.V;\n    " + methods + "\n}\n";
            var driver = NewDriver();
            var (run, trees) = Step(ref driver, Compile(("Over.cs", consumer)));
            var info = Assert.Single(run.Diagnostics);
            Assert.Equal("TLGEN115", info.Id);
            Assert.Contains("Kind 'KindB'", info.GetMessage());
            Assert.Contains("SpanValues<float, KindSpans_0>", string.Join("\n", trees));
            Assert.DoesNotContain("KindSpans_1", string.Join("\n", trees));
        }
    }

    [Fact]
    public void FlagReadOfIgnoresZeroParameterFoldHelpersAndPartialFoldEditsWithoutThrowing()
    {
        const string feeder = "using System;\nusing Tl;\nnamespace Portable;\npublic struct Lift { public float Y; }\npublic struct LiftClip { public float Y; }\npublic struct LiftTrack : IBlend<LiftClip> { public void Blend(in LiftClip first, in LiftClip second, float factor, out LiftClip result) => result = first; }\n[TimelineSystem] public ref partial struct LiftSystem { public Span<Lift> Lift; }\n[TimelineKind(Associative = true)] public struct LiftKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lift lift) => lift.Y += total; }\npublic partial struct Jump : ITrack<LiftTrack, LiftClip> { [Feeds(typeof(LiftKind))] public static float Rise(in float height) => height; }\n";
        const string pureWithZeroArgHelper = "using Tl;\nnamespace Portable;\npublic partial struct Jump { static int Fold() => 0; public static void Fold(in Frame<LiftTrack, LiftClip> frame, out float height) => height = frame.Clip.Y; }\n";
        const string zeroParamFold = "using Tl;\nnamespace Portable;\npublic partial struct Jump { public static void Fold() { } }\n";
        var driver = NewDriver();
        var (run, trees) = Step(ref driver, Compile(("Jump.cs", feeder), ("Fold.cs", pureWithZeroArgHelper)));
        Assert.Empty(run.Diagnostics);
        Assert.Contains(trees, static tree => tree.Contains("SpanValues<float, KindSpans_0>", StringComparison.Ordinal));
        var validFold = Compile(("Jump.cs", feeder), ("Fold.cs", "using Tl;\nnamespace Portable;\npublic partial struct Jump { public static void Fold(in Frame<LiftTrack, LiftClip> frame, out float height) => height = frame.Clip.Y; }\n"));
        var inc = NewDriver();
        Step(ref inc, validFold);
        var edited = validFold.ReplaceSyntaxTree(validFold.SyntaxTrees.Single(static t => t.FilePath == "Fold.cs"), CSharpSyntaxTree.ParseText(zeroParamFold, Parse, "Fold.cs"));
        var (editedRun, _) = Step(ref inc, edited);
        Assert.DoesNotContain(editedRun.Diagnostics, static d => d.Id == "TLGEN115");
    }

    [Fact]
    public void EditingARelevantTypeModifiesItsStepAndReassigns()
    {
        var driver = NewDriver();
        var (_, firstTrees) = Step(ref driver, Compile(("Push.cs", SourceTemplate.Replace("__VALUE__", "1"))));
        var edited = SourceTemplate.Replace("__VALUE__", "1").Replace("PushKind", "ShoveKind");
        var (run, secondTrees) = Step(ref driver, Compile(("Push.cs", edited)));
        Assert.NotEqual(firstTrees, secondTrees);
        Assert.Contains(IncrementalStepRunReason.Modified, Reasons(run, TimelineIncrementalGenerator.TypeStep));
        Assert.Contains(IncrementalStepRunReason.Modified, Reasons(run, "Tl.Systems"));
    }

    [Fact]
    public void AutoColumnsFollowSyntaxTreeOrderNotTypeNameOrder()
    {
        const string zed = "using Tl;\nnamespace Portable;\n[TlState] public partial struct Zed { public float X; }\n[TlKind] public struct ZedKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Zed state) => state.X += total; }\n";
        const string amp = "using Tl;\nnamespace Portable;\n[TlState] public partial struct Amp { public float X; }\n[TlKind] public struct AmpKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Amp state) => state.X += total; }\n";
        var driver = NewDriver();
        var (_, zedFirst) = Step(ref driver, Compile(("Z.cs", zed), ("A.cs", amp)));
        var auto = Assert.Single(zedFirst, tree => tree.StartsWith("Tl.Gen.CSharp/Tl.Gen.CSharp.TimelineIncrementalGenerator/TlSystemsPortable_Pack.Auto.g.cs", StringComparison.Ordinal) || tree.Contains("Auto.g.cs\n"));
        Assert.Contains("C0_Zed", auto);
        Assert.Contains("C1_Amp", auto);
        driver = NewDriver();
        var (_, ampFirst) = Step(ref driver, Compile(("A.cs", amp), ("Z.cs", zed)));
        var swapped = Assert.Single(ampFirst, tree => tree.Contains("Auto.g.cs\n"));
        Assert.Contains("C0_Amp", swapped);
        Assert.Contains("C1_Zed", swapped);
    }
}
