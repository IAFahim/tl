using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed class B2DerivedIncrementalGeneratorTests
{
    static readonly MetadataReference[] Refs =
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(static a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(static a => MetadataReference.CreateFromFile(a.Location)),
        MetadataReference.CreateFromFile(typeof(TimelineWorld).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(TlClipAttribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Sum<>).Assembly.Location),
    ];

    static (GeneratorDriverRunResult Run, ImmutableArray<Diagnostic> Errors) RunGenerator(string source)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "Repro.DerivedITrack",
            [CSharpSyntaxTree.ParseText(source, parse, "Repro.cs")],
            Refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));

        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var errors = output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        return (driver.GetRunResult(), errors);
    }

    [Fact]
    public void DerivedKindWithITrackAndTimelineSystem_EmitsSystemAndDerivedKinds()
    {
        const string source = """
            using System;
            using Tl;

            namespace ReproDerived;

            public struct PushState { public float X; }
            public struct PushClip { public float X; }
            public readonly struct PushTrack : IBlend<PushClip>
            {
                public void Blend(in PushClip a, in PushClip b, float t, out PushClip r) => r = a;
            }

            [TimelineSystem]
            public ref partial struct PushSystem
            {
                public Span<PushState> Push;
            }

            public struct PushConsumer : ITrack<PushTrack, PushClip>
            {
                [Feeds(typeof(Sum<PushState>))]
                public static PushState Eval(in Frame<PushTrack, PushClip> frame) => new() { X = frame.Clip.X };
            }

            public static class Caller
            {
                public static void Tick(TimelineWorld world, PlaybackTable players, Span<PushState> push)
                    => new PushSystem { Push = push }.Run(world, players, forwards: true);
            }
            """;

        var (run, errors) = RunGenerator(source);
        Assert.NotEmpty(run.GeneratedTrees);
        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidDerivedKindWithITrack_ReportsTlGen110InsteadOfSilentEmptyOutput()
    {
        const string source = """
            using System;
            using Tl;

            namespace ReproDerivedBad;

            public struct BadBoolState { public bool Flag; }
            public struct PushClip { public float X; }
            public readonly struct PushTrack : IBlend<PushClip>
            {
                public void Blend(in PushClip a, in PushClip b, float t, out PushClip r) => r = a;
            }

            [TimelineSystem]
            public ref partial struct BadSystem
            {
                public Span<BadBoolState> Bad;
            }

            public struct BadConsumer : ITrack<PushTrack, PushClip>
            {
                [Feeds(typeof(Sum<BadBoolState>))]
                public static BadBoolState Eval(in Frame<PushTrack, PushClip> frame) => default;
            }
            """;

        var (run, _) = RunGenerator(source);
        Assert.Contains(run.Diagnostics, static d => d.Id == "TLGEN110" && d.Severity == DiagnosticSeverity.Error);
    }
}
