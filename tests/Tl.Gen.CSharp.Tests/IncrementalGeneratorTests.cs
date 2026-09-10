using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class IncrementalGeneratorTests
{
    private const string Declaration = """
        using Tl;
        namespace Incremental;
        public readonly record struct Clip(int Value);
        public readonly struct Track : IBlend<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
        }
        public readonly struct Job : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, ref int value) => value += frame.Direction * frame.Clip.Value;
        }
        public readonly partial struct Timeline : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track()).Use<Job>();
                builder.Clip(track, new Clip(2), 0u, 3u);
            }
        }
        """;

    [Fact]
    public void GeneratedOutputBindsAndMatchesTheCliByteForByte()
    {
        var compilation = Compilation(Declaration);
        var driver = Driver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var analyzer = Sources(driver);
        Assert.DoesNotContain("internal static void ExecuteForward(uint", string.Join("\n", analyzer.Values));
        var directory = Path.Combine(Path.GetTempPath(), "tl-incremental-tests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            var sourcePath = Path.Combine(directory, "Timeline.cs");
            var referencesPath = Path.Combine(directory, "references.txt");
            File.WriteAllText(sourcePath, Declaration);
            File.WriteAllLines(referencesPath, ReferencePaths());

            Assert.Equal(0, GeneratorCli.Main(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]));

            var cli = File.ReadAllLines(Path.Combine(directory, CompileGenerationCache.SourceListFileName))
                .ToDictionary(static path => path, path => File.ReadAllText(Path.Combine(directory, path)), StringComparer.Ordinal);
            Assert.Equal(cli, analyzer);
            var report = File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName));
            Assert.Contains("format\t2", report);
            Assert.Contains("timeline\tIncremental.Timeline\ttracks=1\tclips=1\tduration=3", report);
            Assert.Contains("generated-source-utf8-bytes", report);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void DiagnosticsUseTheInvalidBuilderCallSourceSpan()
    {
        var source = Declaration.Replace("builder.Clip(track,", "builder.Clip(missing,", StringComparison.Ordinal);
        var compilation = Compilation(source);

        var result = Driver().RunGenerators(compilation).GetRunResult();

        var call = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(static invocation => invocation.Expression.ToString() == "builder.Clip");
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == "TLGEN68" && diagnostic.Location.SourceSpan == call.Span);
        Assert.Equal(call.Span, diagnostic.Location.SourceSpan);
        Assert.Equal("Timeline.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal("Tl.Generation", diagnostic.Descriptor.Category);
    }

    [Fact]
    public void AnalysisOutputIsStableAcrossUnrelatedEditsAndChangesForDeclarations()
    {
        var initial = Compilation(Declaration).AddSyntaxTrees(Tree("namespace Unrelated; internal sealed class Value { }", "Other.cs"));
        var driver = Driver().RunGenerators(initial);
        Assert.Equal(IncrementalStepRunReason.New, Reason(driver));
        var original = Sources(driver);

        driver = driver.RunGenerators(initial);
        Assert.Equal(IncrementalStepRunReason.Cached, Reason(driver));

        var unrelated = initial.ReplaceSyntaxTree(
            initial.SyntaxTrees.Single(static tree => tree.FilePath == "Other.cs"),
            Tree("namespace Unrelated; internal sealed class Value { internal int Number; }", "Other.cs"));
        driver = driver.RunGenerators(unrelated);
        Assert.Equal(IncrementalStepRunReason.Unchanged, Reason(driver));
        Assert.Equal(original, Sources(driver));

        var changed = unrelated.ReplaceSyntaxTree(
            unrelated.SyntaxTrees.Single(static tree => tree.FilePath == "Timeline.cs"),
            Tree(Declaration.Replace("0u, 3u", "0u, 4u", StringComparison.Ordinal), "Timeline.cs"));
        driver = driver.RunGenerators(changed);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver));
        Assert.NotEqual(original, Sources(driver));
    }

    [Fact]
    public void SameIdentityReferenceReplacementRefreshesTheJobSlots()
    {
        const string source = """
            using Tl;
            using External;
            public readonly partial struct Referenced : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track()).Use<Job>();
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """;
        var firstReference = Reference("ExternalReference", ExternalJob(""));
        var secondReference = Reference("ExternalReference", ExternalJob(", ref int value"));
        var first = Compilation(source).AddReferences(firstReference);
        var driver = Driver().RunGenerators(first);
        Assert.DoesNotContain("ref int @value", string.Join("\n", Sources(driver).Values));

        driver = driver.RunGenerators(first.ReplaceReference(firstReference, secondReference));

        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver));
        Assert.Contains("ref int @value", string.Join("\n", Sources(driver).Values));
    }

    [Fact]
    public void OrderingAndTextAreCultureIndependent()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = Sources(Driver().RunGenerators(Compilation(Declaration)));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");
            var second = Sources(Driver().RunGenerators(Compilation(Declaration)));
            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static CSharpCompilation Compilation(string source)
        => CSharpCompilation.Create(
            "IncrementalTests",
            [Tree(source, "Timeline.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static SyntaxTree Tree(string source, string path)
        => CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path);

    private static GeneratorDriver Driver()
        => CSharpGeneratorDriver.Create(
            [new TimelineIncrementalGenerator().AsSourceGenerator()],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));

    private static IncrementalStepRunReason Reason(GeneratorDriver driver)
    {
        var steps = driver.GetRunResult().Results.Single().TrackedSteps["Tl.Analysis"];
        return Assert.Single(Assert.Single(steps).Outputs).Reason;
    }

    private static Dictionary<string, string> Sources(GeneratorDriver driver)
        => driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static source => source.HintName, static source => source.SourceText.ToString(), StringComparer.Ordinal);

    private static ImmutableArray<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

    private static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();

    private static MetadataReference Reference(string name, string source)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [Tree(source, "Reference.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name + "\0" + source)));
        var path = Path.Combine(Path.GetTempPath(), "tl-incremental-tests", key + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var result = compilation.Emit(path);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromFile(path);
    }

    private static string ExternalJob(string slot)
        => $$"""
            using Tl;
            namespace External;
            public readonly record struct Clip(int Value);
            public readonly struct Track : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }
            public readonly struct Job : ITimelineJob<Track, Clip>
            {
                public static void Execute(in Frame<Track, Clip> frame{{slot}}) { }
            }
            """;
}
