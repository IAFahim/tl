using Tl;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GeneratorCliCollection
{
    public const string Name = "Generator CLI";
}

[Collection(GeneratorCliCollection.Name)]
public sealed class GeneratorCliTests : IDisposable
{
    private const string Declaration = """
        using Tl;
        namespace Cli;
        public readonly record struct Clip(int Value);
        public readonly struct Track : IBlend<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
        }
        public readonly struct Job : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, ref int value) => value += frame.Direction * frame.Clip.Value;
        }
        public readonly partial struct Asset : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track()).Use<Job>();
                builder.Clip(track, new Clip(3), 0u, 4u);
                builder.Looping();
            }
        }
        public readonly struct Rows;
        public readonly partial struct Catalog : ITimelineCatalog
        {
            public static void Define(scoped CatalogBuilder builder)
            {
                builder.Schema<Rows>().Asset<Asset>();
            }
        }
        """;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tl-cli-tests", Guid.NewGuid().ToString("N"));

    public static TheoryData<string[], string> InvalidArguments => new()
    {
        { [], "--compile is required" },
        { ["build"], "--compile is required" },
        { ["--compile"], "--output is required" },
        { ["--compile", "--source"], "invalid argument '--source'" },
        { ["--compile", "--output"], "invalid argument '--output'" },
        { ["--compile", "--unknown"], "invalid argument '--unknown'" },
        { ["--compile", "--output", "generated", "--backend", "native"], "unsupported backend 'native'" },
    };

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void RejectsInvalidArguments(string[] arguments, string message)
    {
        var result = Run(arguments);

        Assert.Equal(1, result.Code);
        Assert.Contains("TLGEN00", result.Error);
        Assert.Contains(message, result.Error);
    }

    [Fact]
    public void CompilesSourceListsOptionsAliasesAndCacheHits()
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Timeline.cs");
        var sources = Path.Combine(_directory, "sources.txt");
        var references = Path.Combine(_directory, "references.txt");
        var options = Path.Combine(_directory, "options.txt");
        var output = Path.Combine(_directory, "generated");
        File.WriteAllText(source, Declaration);
        File.WriteAllLines(sources, [source, source]);
        File.WriteAllLines(references,
            ReferencePaths().Append(typeof(ITimeline).Assembly.Location + "\tplatform\tfalse"));
        File.WriteAllLines(options,
        [
            "language-version=preview",
            "nullable=enable",
            "allow-unsafe=true",
            "check-overflow=true",
            "ignored",
            "=ignored",
        ]);

        var arguments = new[]
        {
            "--compile",
            "--output", output,
            "--source", source,
            "--source-list", sources,
            "--reference-list", references,
            "--option-list", options,
            "--define", "ALPHA; BETA,,GAMMA",
        };
        var first = Run(arguments);
        var second = Run(arguments);

        Assert.True(first.Code == 0, first.Error);
        Assert.Contains("cache miss", first.Output);
        Assert.Contains("1 timeline(s), 1 catalog(s)", first.Output);
        Assert.True(second.Code == 0, second.Error);
        Assert.Contains("cache hit", second.Output);
        var report = File.ReadAllText(Path.Combine(output, CompileGenerationCache.ReportFileName));
        Assert.Contains("timeline\tCli.Asset", report);
        Assert.Contains("loops=true", report);
        Assert.Contains("catalog\tCli.Catalog\tschemas=1\tassets=1", report);
    }

    [Fact]
    public void ReportsGlobalNamespaceAssetsWithoutLeadingDots()
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Global.cs");
        var references = Path.Combine(_directory, "references.txt");
        var output = Path.Combine(_directory, "global");
        File.WriteAllText(source, Declaration.Replace("namespace Cli;\n", "", StringComparison.Ordinal));
        File.WriteAllLines(references, ReferencePaths());

        var result = Run([
            "--compile",
            "--output", output,
            "--source", source,
            "--reference-list", references,
        ]);

        Assert.Equal(0, result.Code);
        var report = File.ReadAllText(Path.Combine(output, CompileGenerationCache.ReportFileName));
        Assert.Contains("timeline\tAsset\t", report);
        Assert.Contains("catalog\tCatalog\t", report);
        Assert.DoesNotContain("timeline\t.Asset", report);
        Assert.DoesNotContain("catalog\t.Catalog", report);
    }

    [Fact]
    public void UnityBackendRequiresCatalogBeforeWritingArtifacts()
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Timeline.cs");
        var references = Path.Combine(_directory, "references.txt");
        var output = Path.Combine(_directory, "generated");
        File.WriteAllText(source, Declaration[..Declaration.IndexOf("public readonly struct Rows;", StringComparison.Ordinal)]);
        File.WriteAllLines(references, ReferencePaths());

        var result = Run([
            "--compile", "--backend", "unity-entities", "--output", output,
            "--source", source, "--reference-list", references,
        ]);

        Assert.Equal(2, result.Code);
        Assert.Contains("TLUNITY01", result.Error);
        Assert.Contains("requires an explicit timeline catalog", result.Error);
        Assert.False(Directory.Exists(output));
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("annotations")]
    [InlineData("warnings")]
    [InlineData("enable")]
    public void AcceptsEveryNullableMode(string mode)
    {
        Directory.CreateDirectory(_directory);
        var options = Path.Combine(_directory, mode + ".options");
        File.WriteAllText(options, "nullable=" + mode);
        var output = Path.Combine(_directory, mode);

        var result = Run(["--compile", "--output", output, "--option-list", options]);

        Assert.Equal(0, result.Code);
        Assert.Contains("cache miss", result.Output);
    }

    [Theory]
    [InlineData("language-version=not-a-version", "Invalid C# language version")]
    [InlineData("nullable=not-a-mode", "Invalid nullable mode")]
    public void ReportsInvalidCompilationOptions(string option, string message)
    {
        Directory.CreateDirectory(_directory);
        var options = Path.Combine(_directory, "invalid.options");
        File.WriteAllText(options, option);

        var result = Run(["--compile", "--output", Path.Combine(_directory, "output"), "--option-list", options]);

        Assert.Equal(2, result.Code);
        Assert.Contains("TLGEN00", result.Error);
        Assert.Contains(message, result.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReportsUnreadableAndInvalidReferences(bool exists)
    {
        Directory.CreateDirectory(_directory);
        var invalidReference = Path.Combine(_directory, "invalid.dll");
        if (exists)
            File.WriteAllText(invalidReference, "not an assembly");
        var references = Path.Combine(_directory, "references.txt");
        File.WriteAllText(references, invalidReference + "\talias\ttrue");

        var result = Run([
            "--compile",
            "--output", Path.Combine(_directory, "output"),
            "--reference-list", references,
        ]);

        Assert.Equal(2, result.Code);
        Assert.Contains("TLGEN00", result.Error);
    }

    [Fact]
    public void ReportsDeclarationDiagnosticsAtTheirSource()
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Invalid.cs");
        var references = Path.Combine(_directory, "references.txt");
        File.WriteAllText(source, Declaration.Replace("builder.Clip(track,", "builder.Clip(missing,", StringComparison.Ordinal));
        File.WriteAllLines(references, ReferencePaths());

        var result = Run([
            "--compile",
            "--output", Path.Combine(_directory, "output"),
            "--source", source,
            "--reference-list", references,
        ]);

        Assert.Equal(2, result.Code);
        Assert.Contains("TLGEN68", result.Error);
        Assert.Contains("Invalid.cs", result.Error);
    }

    private static (int Code, string Output, string Error) Run(string[] arguments)
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var code = GeneratorCli.Main(arguments);
            return (code, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
