using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using Tl.Gen.Model;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class GeneratedSeekTests
{
    private const string Source = """
        using Tl;
        namespace Generated;

        public readonly record struct Clip(int Value);
        public readonly record struct Input(int Value);
        public readonly record struct Output(int Value);

        public readonly struct Track : ITrack<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            public static void Seek(in Frame<Track, Clip> frame, in Input input, ref Output output) { }
        }

        public readonly struct Before : IHook
        {
            public static void Forward(in Input input) { }
            public static void Backward(in Input input) { }
        }

        public readonly struct After : IHook
        {
            public static void Forward(ref Output output) { }
            public static void Backward(ref Output output) { }
        }

        public readonly partial struct TimelineA : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<Before>();
                var track = builder.Track(new Track());
                builder.Clip(track, new Clip(1), 0u, 2u);
                builder.Clip(track, new Clip(2), 1u, 3u);
                builder.After<After>();
            }
        }

        public readonly partial struct TimelineB : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track());
                builder.Clip(track, new Clip(3), 0u, 1u);
                builder.Looping();
            }
        }
        """;

    [Fact]
    public void EmittedSignedSurfaceCompilesWithoutLegacyConcepts()
    {
        var (timelines, diagnostics) = HeterogeneousReader.Read([("Input.cs", Source)]);
        Assert.Empty(diagnostics);
        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines);

        var errors = Compile(Source, artifacts);

        Assert.Empty(errors);
        var all = string.Join("\n", artifacts.Select(static artifact => artifact.Content));
        Assert.Equal(2, Occurrences(all, "public static bool TrySeek(scoped ref Data data, int delta)"));
        Assert.Equal(2, Occurrences(all, "static bool global::Tl.ITimelineData<DynamicData>.TrySeek"));
        Assert.Equal(1, Occurrences(all, "Timeline.TryGetCompiledRoute(id, out var route)"));
        Assert.Contains("public ref struct Data", all);
        Assert.Contains("public ref struct DynamicData", all);
        Assert.Contains("internal readonly ref readonly global::Generated.Input _input", all);
        Assert.Contains("internal readonly ref global::Generated.Output _output", all);
        Assert.Contains("if (delta == 1)", all);
        Assert.Contains("if (delta == -1)", all);
        Assert.Contains("if (delta > 1)", all);
        Assert.Equal(1, Occurrences(all, "beforePosition < 0L || beforePosition > Duration"));
        Assert.DoesNotContain("public static bool TryForward(", all);
        Assert.DoesNotContain("public static bool TryBackward(", all);
        Assert.DoesNotContain("ReadOnlySpan<uint>", all);
        Assert.DoesNotContain("Span<uint>", all);
        Assert.DoesNotContain("stackalloc uint", all);
        Assert.DoesNotContain("MaxBatchLength", all);
        Assert.DoesNotContain("public readonly ref struct Input", all);
        Assert.DoesNotContain("public ref struct Output", all);
        Assert.DoesNotContain("ClipState", all);
        Assert.DoesNotContain("out global::Tl.Playback next", all);
        Assert.DoesNotContain(".Forward(in frame_", all);
        Assert.DoesNotContain(".Backward(in frame_", all);
        Assert.DoesNotContain("-delta", all);
        Assert.DoesNotContain("Math.Abs", all);
    }

    [Fact]
    public void ReverseApplyIsTheStructuralReverseOfForward()
    {
        var (timelines, diagnostics) = HeterogeneousReader.Read([("Input.cs", Source)]);
        Assert.Empty(diagnostics);
        var generated = HeterogeneousEmitter.Emit(timelines.Single(static timeline => timeline.Name == "TimelineA"));
        var forward = generated.IndexOf("private static void ApplyForward", StringComparison.Ordinal);
        var reverse = generated.IndexOf("private static void ApplyReverse", StringComparison.Ordinal);

        Assert.True(forward < generated.IndexOf("global::Generated.Before.Forward", forward, StringComparison.Ordinal));
        Assert.True(generated.IndexOf("global::Generated.Track.Seek", forward, StringComparison.Ordinal) < generated.IndexOf("global::Generated.After.Forward", forward, StringComparison.Ordinal));
        Assert.True(reverse < generated.IndexOf("global::Generated.After.Backward", reverse, StringComparison.Ordinal));
        Assert.True(generated.IndexOf("global::Generated.Track.Seek", reverse, StringComparison.Ordinal) < generated.IndexOf("global::Generated.Before.Backward", reverse, StringComparison.Ordinal));
        Assert.Contains("FrameFlags.ClipStart", generated);
        Assert.Contains("FrameFlags.ClipEnd", generated);
        Assert.Contains("FrameFlags.TimelineStart", generated);
        Assert.Contains("FrameFlags.TimelineEnd", generated);
        Assert.Contains("FrameFlags.CompletedBefore", generated);
        Assert.Contains("FrameFlags.CompletedAfter", generated);
        Assert.Contains("FrameFlags.Reverse", generated);
    }

    [Fact]
    public void EmissionIsDeterministicAndSchemasIgnoreTimelineNamespace()
    {
        var slot = new TimelineSlot("value", "global::Shared.Value", SlotMode.Reference);
        HeterogeneousTimeline[] timelines =
        [
            new("First", "One", false, [], [], [], [], [], [], [slot]),
            new("Second", "Two", false, [], [], [], [], [], [], [slot]),
        ];

        var first = HeterogeneousEmitter.EmitCompilation(timelines);
        var second = HeterogeneousEmitter.EmitCompilation(timelines);

        Assert.Equal(first, second);
        Assert.Single(first, static artifact => artifact.RelativePath.StartsWith("TlSchema", StringComparison.Ordinal));
        Assert.Contains("global::One.__TlGeneratedSchema0.Data", first.Single(static artifact => artifact.RelativePath == "Tl1.g.cs").Content);
        Assert.All(first.Where(static artifact => artifact.RelativePath is "Tl0.g.cs" or "Tl1.g.cs"),
            static artifact => Assert.Contains("beforePosition < 0L || beforePosition > Duration", artifact.Content));
    }

    private static IReadOnlyList<Diagnostic> Compile(string source, IReadOnlyList<CompileArtifact> artifacts)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var trees = new[] { CSharpSyntaxTree.ParseText(source, parseOptions, "Input.cs") }
            .Concat(artifacts.Select(artifact => CSharpSyntaxTree.ParseText(artifact.Content, parseOptions, artifact.RelativePath)));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GeneratedSeek",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
    }

    private static int Occurrences(string source, string value)
    {
        var count = 0;
        for (var start = 0; (start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0; start += value.Length)
            count++;
        return count;
    }
}
