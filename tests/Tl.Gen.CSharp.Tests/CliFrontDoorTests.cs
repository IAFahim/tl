using System.Text;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

[Collection("Tl.Gen.CSharp.Tests.ConsumerBindingTests")]
public sealed class CliFrontDoorTests
{
    private const string Domain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
        }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
        }
        """;

    private const string NoConsumerDomain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
        }
        """;

    private const string UnresolvedContracts = """
        namespace Broken;
        internal interface ITrack<TTrack, TClip> { }
        """;

    [Fact]
    public void MainWithoutCompileArgumentExitsOne()
    {
        var result = Run([]);

        Assert.Equal(1, result.Exit);
        Assert.Contains("TLGEN00", result.StandardError);
        Assert.Contains("--compile is required", result.StandardError);
    }

    [Fact]
    public void MainRejectsUnknownArgumentExitsOne()
    {
        var result = Run(["--compile", "--unknown"]);

        Assert.Equal(1, result.Exit);
        Assert.Contains("--unknown", result.StandardError);
    }

    [Fact]
    public void MainWithoutOutputExitsOne()
    {
        var result = Run(["--compile", "--source", "Domain.cs"]);

        Assert.Equal(1, result.Exit);
        Assert.Contains("--output is required", result.StandardError);
    }

    [Fact]
    public void MainRejectsUnsupportedBackendExitsOne()
    {
        var result = Run(["--compile", "--output", "unused", "--backend", "python", "--source", "Domain.cs"]);

        Assert.Equal(1, result.Exit);
        Assert.Contains("unsupported backend 'python'", result.StandardError);
    }

    [Fact]
    public void MainReadsSourceListOptionListDefinesAndRerunsFromCache()
    {
        var directory = NewDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "Domain.cs");
            var sourceListPath = Path.Combine(directory, "sources.txt");
            var referencesPath = Path.Combine(directory, "references.txt");
            var optionsPath = Path.Combine(directory, "options.txt");
            File.WriteAllText(sourcePath, Domain);
            File.WriteAllLines(sourceListPath, [sourcePath]);
            File.WriteAllLines(referencesPath, ReferencePaths());
            File.WriteAllLines(optionsPath,
            [
                "nullable=annotations",
                "allow-unsafe=true",
                "check-overflow=true",
                "this line carries no separator",
                "=no-key-here",
            ]);

            var arguments = (string[] path) => new[]
            {
                "--compile",
                "--output", directory,
                "--source-list", path[0],
                "--reference-list", path[1],
                "--option-list", path[2],
                "--define", "TL_PROBE_A;TL_PROBE_B,TL_PROBE_C",
            };

            var first = Run(arguments([sourceListPath, referencesPath, optionsPath]));
            Assert.Equal(0, first.Exit);
            var bindingPath = Path.Combine(directory, "TlConsumerBinding.g.cs");
            var bindingStamp = File.GetLastWriteTimeUtc(bindingPath);
            var bindingContent = File.ReadAllText(bindingPath);
            Assert.True(File.Exists(Path.Combine(directory, CompileGenerationCache.ManifestFileName)));
            Assert.True(File.Exists(Path.Combine(directory, CompileGenerationCache.SourceListFileName)));
            Assert.True(File.Exists(Path.Combine(directory, CompileGenerationCache.ReportFileName)));

            var second = Run(arguments([sourceListPath, referencesPath, optionsPath]));
            Assert.Equal(0, second.Exit);
            Assert.Equal(bindingStamp, File.GetLastWriteTimeUtc(bindingPath));
            Assert.Equal(bindingContent, File.ReadAllText(bindingPath));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("annotations")]
    [InlineData("warnings")]
    [InlineData("enable")]
    public void MainAppliesEveryNullableMode(string mode)
    {
        var directory = NewDirectory();
        try
        {
            var result = CompileWithOptions(directory, Domain, ["nullable=" + mode]);

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(directory, "TlConsumerBinding.g.cs")));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainRejectsInvalidNullableModeExitsTwo()
    {
        var directory = NewDirectory();
        try
        {
            var exit = CompileWithOptions(directory, Domain, ["nullable=banana"]);

            Assert.Equal(2, exit);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainRejectsInvalidLanguageVersionExitsTwo()
    {
        var directory = NewDirectory();
        try
        {
            var exit = CompileWithOptions(directory, Domain, ["language-version=banana"]);

            Assert.Equal(2, exit);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainReportsUnresolvedContractDiagnosticsAndExitsTwo()
    {
        var directory = NewDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "Broken.cs");
            File.WriteAllText(sourcePath, UnresolvedContracts);
            var result = Run(["--compile", "--output", directory, "--source", sourcePath]);

            Assert.Equal(2, result.Exit);
            Assert.Contains("TLGEN60", result.StandardError);
            Assert.Contains("error TLGEN60", result.StandardError);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainFailsWhenAReferenceListEntryIsMissing()
    {
        var directory = NewDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "Domain.cs");
            var referencesPath = Path.Combine(directory, "references.txt");
            File.WriteAllText(sourcePath, Domain);
            File.WriteAllLines(referencesPath, [Path.Combine(directory, "tl-missing.reference.dll")]);

            var result = Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]);

            Assert.Equal(2, result.Exit);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainUnityBackendEmitsConsumerBinding()
    {
        var directory = NewDirectory();
        try
        {
            var result = CompileWithBackend(directory, Domain, "unity-entities");

            Assert.Equal(0, result);
            var binding = File.ReadAllText(Path.Combine(directory, "TlConsumerBinding.g.cs"));
            Assert.Contains("global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume", binding);
            Assert.Contains("backend\tunity-entities", File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName)));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MainUnityBackendWithoutConsumersEmitsNothing()
    {
        var directory = NewDirectory();
        try
        {
            var result = CompileWithBackend(directory, NoConsumerDomain, "unity-entities");

            Assert.Equal(0, result);
            Assert.False(File.Exists(Path.Combine(directory, "TlConsumerBinding.g.cs")));
            Assert.Contains("consumers\t0", File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName)));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static (int Exit, string StandardError) Run(string[] arguments)
    {
        var standardError = new StringWriter();
        var previousError = Console.Error;
        Console.SetError(standardError);
        try
        {
            var exit = GeneratorCli.Main(arguments);
            return (exit, standardError.ToString());
        }
        finally
        {
            Console.SetError(previousError);
        }
    }

    private static int CompileWithOptions(string directory, string source, string[] options)
    {
        var sourcePath = Path.Combine(directory, "Domain.cs");
        var referencesPath = Path.Combine(directory, "references.txt");
        var optionsPath = Path.Combine(directory, "options.txt");
        File.WriteAllText(sourcePath, source);
        File.WriteAllLines(referencesPath, ReferencePaths());
        File.WriteAllLines(optionsPath, options);
        return Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath, "--option-list", optionsPath]).Exit;
    }

    private static int CompileWithBackend(string directory, string source, string backend)
    {
        var sourcePath = Path.Combine(directory, "Domain.cs");
        var referencesPath = Path.Combine(directory, "references.txt");
        File.WriteAllText(sourcePath, source);
        File.WriteAllLines(referencesPath, ReferencePaths());
        return Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath, "--backend", backend]).Exit;
    }

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Tl.IBake<>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tl-cli-front-door-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
