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
        public readonly struct Track : ITrack<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            public static void Seek(in Frame<Track, Clip> frame, ref int value) => value += frame.Direction * frame.Clip.Value;
        }
        public readonly partial struct Timeline : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track());
                builder.Clip(track, new Clip(2), 0u, 3u);
            }
        }
        """;

    [Fact]
    public void GeneratedOutputBindsAndMatchesTheCliByteForByte()
    {
        var compilation = Compilation(Declaration);
        var driver = Driver();

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);
        Assert.Empty(output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var analyzer = Sources(driver);
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
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void DiagnosticsUseTheIncompleteDeclarationSourceSpan()
    {
        const string source = """
            using Tl;
            namespace Incremental;
            public readonly partial struct Broken : ITimeline
            {
            """;
        var compilation = Compilation(source);

        var result = Driver().RunGenerators(compilation).GetRunResult();

        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Id == "TLGEN23");
        var declaration = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>().Single();
        Assert.Equal(declaration.Span, diagnostic.Location.SourceSpan);
        Assert.Equal("Timeline.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal(declaration.GetLocation().GetLineSpan().StartLinePosition, diagnostic.Location.GetLineSpan().StartLinePosition);
    }

    [Fact]
    public void TrackedCompilationStepRespondsOnlyToStructuralInputs()
    {
        var initial = Compilation(Declaration).AddSyntaxTrees(Tree("namespace Unrelated; internal sealed class Value { }", "Other.cs"));
        var driver = Driver();
        driver = driver.RunGenerators(initial);
        Assert.Equal(IncrementalStepRunReason.New, Reason(driver, "Tl.Compilation"));

        driver = driver.RunGenerators(initial);
        Assert.Equal(IncrementalStepRunReason.Cached, Reason(driver, "Tl.Compilation"));

        var unrelated = initial.ReplaceSyntaxTree(
            initial.SyntaxTrees.Single(static tree => tree.FilePath == "Other.cs"),
            Tree("namespace Unrelated; internal sealed class Value { internal int Number; }", "Other.cs"));
        driver = driver.RunGenerators(unrelated);
        Assert.Equal(IncrementalStepRunReason.Cached, Reason(driver, "Tl.Compilation"));

        var changed = unrelated.ReplaceSyntaxTree(
            unrelated.SyntaxTrees.Single(static tree => tree.FilePath == "Timeline.cs"),
            Tree(Declaration.Replace("0u, 3u", "0u, 4u", StringComparison.Ordinal), "Timeline.cs"));
        driver = driver.RunGenerators(changed);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Compilation"));

        var addedTree = Tree("""
            using Tl;
            namespace Incremental;
            public readonly partial struct Second : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """, "Second.cs");
        var added = changed.AddSyntaxTrees(addedTree);
        driver = driver.RunGenerators(added);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Compilation"));
        Assert.Equal(4, Sources(driver).Count);

        driver = driver.RunGenerators(added.RemoveSyntaxTrees(addedTree));
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Compilation"));
        Assert.Equal(3, Sources(driver).Count);
    }

    [Fact]
    public void OptionsAndReferencesInvalidateTheEnvironmentStep()
    {
        var compilation = Compilation(Declaration);
        var driver = Driver().RunGenerators(compilation);

        driver = driver.RunGenerators(compilation.WithOptions(((CSharpCompilationOptions)compilation.Options).WithOverflowChecks(true)));
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Environment"));
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Compilation"));

        var reference = Reference("IncrementalReference");
        driver = driver.RunGenerators(compilation.AddReferences(reference));
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Environment"));
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver, "Tl.Compilation"));
    }

    [Fact]
    public void OrderingAndTextAreCultureIndependent()
    {
        var source = Declaration + """

            public readonly partial struct Alpha : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = Sources(Driver().RunGenerators(Compilation(source)));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");
            var second = Sources(Driver().RunGenerators(Compilation(source)));
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

    private static IncrementalStepRunReason Reason(GeneratorDriver driver, string name)
    {
        var steps = driver.GetRunResult().Results.Single().TrackedSteps[name];
        var step = Assert.Single(steps);
        return Assert.Single(step.Outputs).Reason;
    }

    private static Dictionary<string, string> Sources(GeneratorDriver driver)
        => driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static source => source.HintName, static source => source.SourceText.ToString(), StringComparer.Ordinal);

    private static ImmutableArray<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

    private static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static MetadataReference Reference(string name)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [Tree("public sealed class AddedReference { }", "Reference.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
