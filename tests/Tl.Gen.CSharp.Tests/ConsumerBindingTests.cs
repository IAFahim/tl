using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class ConsumerBindingTests
{
    internal const string Source = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public struct Resistance { public float Scale; }
        public struct Health { public float Value; }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, in Resistance resistance, ref Health health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health.Value += frame.IsBackward ? amount : -amount;
            }
        }
        public readonly record struct HealClip(float Amount);
        public readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
        {
            public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
                => result = new HealClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyHeal : ITrack<HealTrack, HealClip>
        {
            public static void OnActive(in Frame<HealTrack, HealClip> frame, in Resistance resistance, ref Health health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health.Value += frame.IsBackward ? -amount : amount;
            }
        }
        """;

    private const string StandaloneSource = """
        using Tl;
        namespace Domain;
        public readonly record struct BuffClip(float Amount);
        public readonly record struct BuffTrack(float Multiplier) : IBlend<BuffClip>
        {
            public void Blend(in BuffClip first, in BuffClip second, float factor, out BuffClip result)
                => result = new BuffClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public struct Armor { public float Value; }
        public readonly struct ApplyBuff : ITrack<BuffTrack, BuffClip>
        {
            public static void OnActive(in Frame<BuffTrack, BuffClip> frame, ref Armor armor) { }
        }
        """;

    [Fact]
    public void EmitsOneColdInstallerWithTypedThunksForEveryDiscoveredJobPairing()
    {
        var generated = Generate();
        var binding = Assert.Single(generated).Value;
        Assert.DoesNotContain("\r", binding);
        Assert.StartsWith("internal static unsafe class TlConsumerBinding\n{\n[global::System.Runtime.CompilerServices.ModuleInitializer]\ninternal static void Install()\n{\n", binding);
        var installEnd = binding.IndexOf("}", StringComparison.Ordinal);
        var install = binding[..installEnd];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume(&OnActive_ApplyDamage, &OnActiveRange_ApplyDamage, &Bind_ApplyDamage);",
            "global::Tl.PairRuntime<global::Domain.HealTrack, global::Domain.HealClip>.Consume(&OnActive_ApplyHeal, &OnActiveRange_ApplyHeal, &Bind_ApplyHeal);",
        ], install.Split('\n')[5..^1]);
        Assert.Contains("private static void OnActive_ApplyDamage(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)", binding);
        Assert.Contains("private static void OnActiveRange_ApplyDamage(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRowStart, int __tlRowCount)", binding);
        Assert.Contains("global::Domain.DamageClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.DamageTrack, global::Domain.DamageClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("var @resistance = (global::Domain.Resistance*)__tlColumns[0];", binding);
        Assert.Contains("var @health = (global::Domain.Health*)__tlColumns[1];", binding);
        Assert.Contains("global::Domain.ApplyDamage.OnActive(in __tlTyped, in @resistance[__tlRow], ref @health[__tlRow]);", binding);
        Assert.Contains("for (var __tlRow = __tlRowStart; __tlRow < __tlRowStart + __tlRowCount; __tlRow++)", binding);
        Assert.Contains("private static void Bind_ApplyDamage(ulong* __tlKeys, int __tlKeyCount, byte* __tlIndices)", binding);
        Assert.Contains("var __tlIdx0 = FindKey(__tlKeys, __tlKeyCount, global::Tl.TypeKey<global::Domain.Resistance>.Value);", binding);
        Assert.Contains("if (__tlIdx0 < 0) throw new global::System.ArgumentException(\"global::Domain.ApplyDamage: required column missing for registered consumer: global::Domain.Resistance\");", binding);
        Assert.Contains("__tlIndices[0] = (byte)(__tlIdx0 + 1);", binding);
    }

    [Fact]
    public void EmitsBindingForStandaloneJobWithoutAuthoredTimeline()
    {
        var (sources, diagnostics) = GenerateWithDiagnostics(StandaloneSource);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.PairRuntime<global::Domain.BuffTrack, global::Domain.BuffClip>.Consume(&OnActive_ApplyBuff, &OnActiveRange_ApplyBuff, &Bind_ApplyBuff);", binding);
    }

    [Fact]
    public void RejectsOpenGenericJobWithDiagnostic()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct DamageClip(float Amount);
            public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
            {
                public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
            }
            public readonly struct GenericJob<T> : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN65", diagnostic.Id);
        Assert.Contains("cannot be generic; open type parameters cannot be registered as timeline jobs", diagnostic.GetMessage());
    }

    [Fact]
    public void JobDiagnosticsUseTheJobDeclarationSourceSpan()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct DamageClip(float Amount);
            public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
            {
                public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
            }
            public readonly struct GenericJob<T> : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
            }
            """;
        var compilation = Compilation(source);
        var result = Driver().RunGenerators(compilation).GetRunResult();

        var declaration = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Single(static type => type.Identifier.ValueText == "GenericJob");
        var diagnostic = Assert.Single(result.Diagnostics, static candidate => candidate.Id == "TLGEN65");
        Assert.Equal(declaration.Span, diagnostic.Location.SourceSpan);
        Assert.Equal("Domain.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal("Tl.Generation", diagnostic.Descriptor.Category);
    }

    [Fact]
    public void RejectsJobWithNonUnmanagedTrackOrClipWithDiagnostic()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct Clip(float Amount);
            public class ManagedTrack : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }
            public readonly struct Job : ITrack<ManagedTrack, Clip>
            {
                public static void OnActive(in Frame<ManagedTrack, Clip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN65", diagnostic.Id);
        Assert.Contains("must be an unmanaged type", diagnostic.GetMessage());
    }

    [Fact]
    public void RejectsJobWhereTrackDoesNotImplementIBlendWithDiagnostic()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct Clip(float Amount);
            public readonly record struct Track(float Multiplier);
            public readonly struct Job : ITrack<Track, Clip>
            {
                public static void OnActive(in Frame<Track, Clip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN65", diagnostic.Id);
        Assert.Contains("must implement Tl.IBlend<global::Domain.Clip>", diagnostic.GetMessage());
    }

    [Fact]
    public void MultiPairingJobRegistersEveryPairingInLexicalOrder()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct AlphaClip(int Value);
            public readonly record struct BetaClip(float Amount);
            public readonly record struct DualTrack(int Code) : IBlend<AlphaClip>, IBlend<BetaClip>
            {
                public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
                public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
            }
            public struct Health { public float Value; }
            public readonly struct DualJob : ITrack<DualTrack, BetaClip>, ITrack<DualTrack, AlphaClip>
            {
                public static void OnActive(in Frame<DualTrack, AlphaClip> frame, ref Health health) { }
                public static void OnActive(in Frame<DualTrack, BetaClip> frame, ref Health health) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(source);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        var installEnd = binding.IndexOf("}", StringComparison.Ordinal);
        var install = binding[..installEnd];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.AlphaClip>.Consume(&OnActive_DualJob, &OnActiveRange_DualJob, &Bind_DualJob);",
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.BetaClip>.Consume(&OnActive_DualJob_, &OnActiveRange_DualJob_, &Bind_DualJob_);",
        ], install.Split('\n')[5..^1]);
        Assert.Contains("global::Tl.TickFrame.ToFrame<global::Domain.DualTrack, global::Domain.AlphaClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("global::Tl.TickFrame.ToFrame<global::Domain.DualTrack, global::Domain.BetaClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("global::Domain.DualJob.OnActive(in __tlTyped, ref @health[__tlRow]);", binding);
    }

    [Fact]
    public void CliReexportHitsTheCacheAndPreservesBindingTimestamps()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tl-consumer-binding-cli", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var sourcePath = Path.Combine(directory, "Domain.cs");
            var referencesPath = Path.Combine(directory, "references.txt");
            File.WriteAllText(sourcePath, Source);
            File.WriteAllLines(referencesPath, ReferencePaths().Append(typeof(ITrack<,>).Assembly.Location + "\tplatform\tfalse"));

            var first = Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]);
            Assert.Equal(0, first.exit);
            Assert.Contains("cache miss", first.output);
            var report = File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName));
            Assert.Contains("format\t4", report);
            Assert.Contains("backend\tcsharp", report);
            Assert.Contains("consumers\t2", report);
            Assert.Contains("bakes\t0", report);
            Assert.Contains("generated-source-files\t1", report);
            Assert.Contains("artifact\tTlConsumerBinding.g.cs\tutf8-bytes=", report);

            var bindingPath = Path.Combine(directory, "TlConsumerBinding.g.cs");
            var timestamp = File.GetLastWriteTimeUtc(bindingPath);
            var before = File.ReadAllText(bindingPath);

            var second = Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]);
            Assert.Equal(0, second.exit);
            Assert.Contains("cache hit", second.output);
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(bindingPath));
            Assert.Equal(before, File.ReadAllText(bindingPath));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void AnalysisComparerUsesOnlyTheStableFingerprint()
    {
        var first = new TimelineIncrementalGenerator.Analysis([], [], "same");
        var equivalent = new TimelineIncrementalGenerator.Analysis([], [], "same");
        var different = new TimelineIncrementalGenerator.Analysis([], [], "different");
        var comparer = TimelineIncrementalGenerator.AnalysisComparer.Instance;

        Assert.True(comparer.Equals(first, first));
        Assert.True(comparer.Equals(first, equivalent));
        Assert.False(comparer.Equals(first, different));
        Assert.False(comparer.Equals(first, null));
        Assert.False(comparer.Equals(null, first));
        Assert.True(comparer.Equals(null, null));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(equivalent));
        Assert.NotEqual(comparer.GetHashCode(first), comparer.GetHashCode(different));
    }

    [Fact]
    public void SourceLessDiagnosticsUseNoLocation()
    {
        var diagnostic = new DeclarationDiagnostic("", 0, 0, "TLGEN00", "source unavailable");

        Assert.Equal(Location.None, TimelineIncrementalGenerator.Location(diagnostic));
    }

    [Fact]
    public void GeneratedOutputBindsAndMatchesTheCliByteForByte()
    {
        var compilation = Compilation(Source);
        var driver = Driver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var analyzer = Sources(driver);
        var directory = Path.Combine(Path.GetTempPath(), "tl-consumer-binding-cli", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            var sourcePath = Path.Combine(directory, "Domain.cs");
            var referencesPath = Path.Combine(directory, "references.txt");
            File.WriteAllText(sourcePath, Source);
            File.WriteAllLines(referencesPath, ReferencePaths());

            Assert.Equal(0, GeneratorCli.Main(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]));

            var cli = File.ReadAllLines(Path.Combine(directory, CompileGenerationCache.SourceListFileName))
                .ToDictionary(static path => path, path => File.ReadAllText(Path.Combine(directory, path)), StringComparer.Ordinal);
            Assert.Equal(cli, analyzer);
            var report = File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName));
            Assert.Contains("format\t4", report);
            Assert.Contains("consumers\t2", report);
            Assert.Contains("bakes\t0", report);
            Assert.Contains("generated-source-utf8-bytes", report);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void AnalysisOutputIsStableAcrossUnrelatedEditsAndChangesForJobSignatures()
    {
        var initial = Compilation(StandaloneSource).AddSyntaxTrees(Tree("namespace Unrelated; internal sealed class Value { }", "Other.cs"));
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
            unrelated.SyntaxTrees.Single(static tree => tree.FilePath == "Domain.cs"),
            Tree(StandaloneSource.Replace("ref Armor armor) { }", "ref Armor armor, ref int extra) { }", StringComparison.Ordinal), "Domain.cs"));
        driver = driver.RunGenerators(changed);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver));
        Assert.NotEqual(original, Sources(driver));
        Assert.Contains("var @extra = (int*)__tlColumns[1];", Assert.Single(Sources(driver)).Value);
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
            var first = Sources(Driver().RunGenerators(Compilation(Source)));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");
            var second = Sources(Driver().RunGenerators(Compilation(Source)));
            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static (int exit, string output) Run(string[] arguments)
    {
        var standardOut = new StringWriter();
        var previous = Console.Out;
        Console.SetOut(standardOut);
        try
        {
            var code = GeneratorCli.Main(arguments);
            return (code, standardOut.ToString());
        }
        finally
        {
            Console.SetOut(previous);
        }
    }

    internal static Dictionary<string, string> Generate()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("ConsumerBindingEmission",
            [CSharpSyntaxTree.ParseText(Source, options, "Domain.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static source => source.HintName, static source => source.SourceText.ToString(), StringComparer.Ordinal);
    }

    internal static (Dictionary<string, string> Sources, ImmutableArray<Diagnostic> Diagnostics) GenerateWithDiagnostics(string source)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("ConsumerBindingEmission" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, options, "Domain.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static s => s.HintName, static s => s.SourceText.ToString(), StringComparer.Ordinal);
        return (sources, diagnostics);
    }

    private static CSharpCompilation Compilation(string source)
        => CSharpCompilation.Create(
            "ConsumerBindingTests",
            [Tree(source, "Domain.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

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

    private static IEnumerable<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();
}
