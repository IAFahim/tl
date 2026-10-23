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
            public static void ExecuteActive(in Frame<DamageTrack, DamageClip> frame, in Resistance resistance, ref Health health)
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
            public static void ExecuteActive(in Frame<HealTrack, HealClip> frame, in Resistance resistance, ref Health health)
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
        public struct Mana { public float Value; }
        public readonly struct ApplyBuff : ITrack<BuffTrack, BuffClip>
        {
            public static void ExecuteActive(in Frame<BuffTrack, BuffClip> frame, ref float armor) { }
        }
        """;

    [Fact]
    public void EmitsOneColdInstallerWithTypedThunksForEveryDiscoveredJobPairing()
    {
        var generated = Generate();
        var binding = Assert.Single(generated).Value;
        Assert.DoesNotContain("\r", binding);
        Assert.StartsWith(
            "[assembly: global::TlConsumerLayoutAttribute(typeof(global::Domain.DamageTrack), typeof(global::Domain.DamageClip), 1653526390476323667UL)]\n"
            + "[assembly: global::TlConsumerLayoutAttribute(typeof(global::Domain.HealTrack), typeof(global::Domain.HealClip), 5952046634645506417UL)]\n"
            + "[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]\n"
            + "internal sealed class TlConsumerLayoutAttribute : global::System.Attribute\n"
            + "{\n"
            + "internal TlConsumerLayoutAttribute(global::System.Type track, global::System.Type clip, ulong layout) { }\n"
            + "}\n"
            + "internal static unsafe class TlConsumerBinding\n{\n"
            + "[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields, typeof(global::Domain.DamageTrack))]\n" + "[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields, typeof(global::Domain.DamageClip))]\n" + "[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields, typeof(global::Domain.HealTrack))]\n" + "[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields, typeof(global::Domain.HealClip))]\n"
            + "[global::System.Runtime.CompilerServices.ModuleInitializer]\ninternal static void Install()\n{\n", binding);
        var at = binding.IndexOf("internal static void Install()", StringComparison.Ordinal);
        var install = binding[at..binding.IndexOf("}", at, StringComparison.Ordinal)];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.ConsumeDispatch(&ExecuteActive_ApplyDamage, &LiveKeys_ApplyDamage, &Diag_ApplyDamage, \"ConsumerBindingEmission\\u0000global::Domain.ApplyDamage\");",
            "global::Tl.PairRuntime<global::Domain.HealTrack, global::Domain.HealClip>.ConsumeDispatch(&ExecuteActive_ApplyHeal, &LiveKeys_ApplyHeal, &Diag_ApplyHeal, \"ConsumerBindingEmission\\u0000global::Domain.ApplyHeal\");",
            "global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.VerifyLayout(1653526390476323667UL);",
            "global::Tl.PairRuntime<global::Domain.HealTrack, global::Domain.HealClip>.VerifyLayout(5952046634645506417UL);",
        ], install.Split('\n')[2..^1]);
        Assert.Contains("private static void ExecuteActive_ApplyDamage(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)", binding);
        Assert.Contains("global::Domain.DamageClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.DamageTrack, global::Domain.DamageClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("var @resistance = (global::Domain.Resistance*)__tlColumns[0];", binding);
        Assert.Contains("var @health = (global::Domain.Health*)__tlColumns[1];", binding);
        Assert.Contains("global::Domain.ApplyDamage.ExecuteActive(in __tlTyped, in @resistance[__tlRow], ref @health[__tlRow]);", binding);
        Assert.Contains("private static int LiveKeys_ApplyDamage(ulong* __tlKeys, byte* __tlMeta)", binding);
        Assert.Contains("__tlKeys[0] = global::Tl.TypeKey<global::Domain.Resistance>.Value; __tlMeta[0] = 4;", binding);
        Assert.Contains("__tlKeys[1] = global::Tl.TypeKey<global::Domain.Health>.Value; __tlMeta[1] = 36;", binding);
        Assert.Contains("if (__tlSlot == 0) throw new global::System.ArgumentException(\"Timeline<Domain.DamageTrack, Domain.DamageClip> consumer 'Domain.ApplyDamage' ExecuteActive requires a column of type Domain.Resistance (resistance); none was passed.\");", binding);
        Assert.DoesNotContain("Bind_ApplyDamage", binding);
    }

    [Fact]
    public void ZeroSlotConsumerRegistersAsDispatchOnlyWithoutColumnBind()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct BellClip(float Tone);
            public readonly record struct BellTrack(float Gain) : IBlend<BellClip>
            {
                public void Blend(in BellClip first, in BellClip second, float factor, out BellClip result) => result = first;
            }
            public readonly struct RingBell : ITrack<BellTrack, BellClip>
            {
                public static void ExecuteActive(in Frame<BellTrack, BellClip> frame) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(source);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.PairRuntime<global::Domain.BellTrack, global::Domain.BellClip>.ConsumeDispatch(&ExecuteActive_RingBell, \"ConsumerBindingEmission\\u0000global::Domain.RingBell\");", binding);
        Assert.Contains("private static void ExecuteActive_RingBell(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)", binding);
        Assert.Contains("global::Domain.RingBell.ExecuteActive(in __tlTyped);", binding);
        Assert.DoesNotContain("Range_", binding);
        Assert.DoesNotContain("Bind_RingBell", binding);
    }

    [Fact]
    public void EmitsBindingForStandaloneJobWithoutAuthoredTimeline()
    {
        var (sources, diagnostics) = GenerateWithDiagnostics(StandaloneSource);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.PairRuntime<global::Domain.BuffTrack, global::Domain.BuffClip>.Consume(&ExecuteActive_ApplyBuff, &Bind_ApplyBuff, \"ConsumerBindingEmission\\u0000global::Domain.ApplyBuff\");", binding);
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
                public static void ExecuteActive(in Frame<DamageTrack, DamageClip> frame) { }
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
                public static void ExecuteActive(in Frame<DamageTrack, DamageClip> frame) { }
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
                public static void ExecuteActive(in Frame<ManagedTrack, Clip> frame) { }
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
                public static void ExecuteActive(in Frame<Track, Clip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN65", diagnostic.Id);
        Assert.Contains("must implement Tl.IBlend<global::Domain.Clip>", diagnostic.GetMessage());
    }

    [Fact]
    public void RejectsJobWithNonReadOnlyTrackWithDiagnostic()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct Clip(float Amount);
            public record struct Track(float Multiplier) : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }
            public readonly struct Job : ITrack<Track, Clip>
            {
                public static void ExecuteActive(in Frame<Track, Clip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN65", diagnostic.Id);
        Assert.Contains("track type 'global::Domain.Track' must be a readonly struct", diagnostic.GetMessage());
    }

    [Fact]
    public void BindsJobWithNonReadOnlyClipBecauseBlendReceivesClipCopies()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public record struct Clip(float Amount);
            public readonly record struct Track(float Multiplier) : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }
            public readonly struct Job : ITrack<Track, Clip>
            {
                public static void ExecuteActive(in Frame<Track, Clip> frame) { }
            }
            """;
        var (_, diagnostics) = GenerateWithDiagnostics(source);
        Assert.Empty(diagnostics);
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
            public readonly struct DualJob : ITrack<DualTrack, BetaClip>, ITrack<DualTrack, AlphaClip>
            {
                public static void ExecuteActive(in Frame<DualTrack, AlphaClip> frame, ref float health) { }
                public static void ExecuteActive(in Frame<DualTrack, BetaClip> frame, ref float health) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(source);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        var at = binding.IndexOf("internal static void Install()", StringComparison.Ordinal);
        var install = binding[at..binding.IndexOf("}", at, StringComparison.Ordinal)];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.AlphaClip>.Consume(&ExecuteActive_DualJob, &Bind_DualJob, \"ConsumerBindingEmission\\u0000global::Domain.DualJob\");",
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.BetaClip>.Consume(&ExecuteActive_DualJob_, &Bind_DualJob_, \"ConsumerBindingEmission\\u0000global::Domain.DualJob\");",
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.AlphaClip>.VerifyLayout(13605547736430830029UL);",
            "global::Tl.PairRuntime<global::Domain.DualTrack, global::Domain.BetaClip>.VerifyLayout(14545621562080599017UL);",
        ], install.Split('\n')[2..^1]);
        Assert.Contains("global::Tl.TickFrame.ToFrame<global::Domain.DualTrack, global::Domain.AlphaClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("global::Tl.TickFrame.ToFrame<global::Domain.DualTrack, global::Domain.BetaClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("global::Domain.DualJob.ExecuteActive(in __tlTyped, ref @health[__tlRow]);", binding);
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
        var compilation = Compilation(Source, "Tl.Generated.Authoring");
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
        Assert.Equal(IncrementalStepRunReason.Cached, Reason(driver));
        Assert.Equal(original, Sources(driver));

        var changed = unrelated.ReplaceSyntaxTree(
            unrelated.SyntaxTrees.Single(static tree => tree.FilePath == "Domain.cs"),
            Tree(StandaloneSource.Replace("ref float armor) { }", "ref float armor, ref Mana extra) { }", StringComparison.Ordinal), "Domain.cs"));
        driver = driver.RunGenerators(changed);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver));
        Assert.NotEqual(original, Sources(driver));
        Assert.Contains("var @extra = (global::Domain.Mana*)__tlColumns[1];", Assert.Single(Sources(driver)).Value);
    }

    private const string NeighborSource = """
        using Tl;
        namespace Frost;
        public readonly record struct FrostClip(float Amount);
        public readonly record struct FrostTrack(float Multiplier) : IBlend<FrostClip>
        {
            public void Blend(in FrostClip first, in FrostClip second, float factor, out FrostClip result)
                => result = new FrostClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyFrost : ITrack<FrostTrack, FrostClip>
        {
            public static void ExecuteActive(in Frame<FrostTrack, FrostClip> frame, ref float total) { }
        }
        """;

    [Fact]
    public void EditTouchingOneTypeReRunsOnlyThatTypesPipeline()
    {
        var compilation = CSharpCompilation.Create(
            "ConsumerBindingEmission",
            [Tree(StandaloneSource, "Domain.cs"), Tree(NeighborSource, "Frost.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TimelineIncrementalGenerator().AsSourceGenerator()],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));
        driver = driver.RunGenerators(compilation);

        var edited = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Single(static tree => tree.FilePath == "Domain.cs"),
            Tree(StandaloneSource.Replace("ref float armor) { }", "ref float armor, ref Mana extra) { }", StringComparison.Ordinal), "Domain.cs"));
        driver = driver.RunGenerators(edited);

        var reasons = TypeReasons(driver);
        Assert.Equal(IncrementalStepRunReason.Modified, reasons["global::Domain.ApplyBuff"]);
        Assert.Equal(IncrementalStepRunReason.Unchanged, reasons["global::Frost.ApplyFrost"]);
        Assert.Equal(IncrementalStepRunReason.Modified, Reason(driver));
        Assert.Contains("var @extra = (global::Domain.Mana*)__tlColumns[1];", Sources(driver).Values.Single());
        var modified = driver.GetRunResult().Results.Single().TrackedSteps[TimelineIncrementalGenerator.TypeStep]
            .SelectMany(static step => step.Outputs, static (step, output) => output.Reason)
            .Count(static reason => reason == IncrementalStepRunReason.Modified);
        Assert.Equal(1, modified);
    }

    [Fact]
    public void AddingTheContractReferenceAfterABrokenRunEmitsBindings()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = Tree(StandaloneSource, "Domain.cs");
        var broken = CSharpCompilation.Create("ConsumerBindingEmission",
            [tree],
            ReferencePaths().Where(static path => !path.Contains("Tl.Core", StringComparison.Ordinal)).Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TimelineIncrementalGenerator().AsSourceGenerator()],
            parseOptions: options,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));
        driver = driver.RunGenerators(broken);
        Assert.Empty(driver.GetRunResult().Results.Single().GeneratedSources);
        Assert.Contains("TLGEN60", driver.GetRunResult().Results.Single().Diagnostics.Select(static diagnostic => diagnostic.Id));

        driver = driver.RunGenerators(broken.AddReferences(MetadataReference.CreateFromFile(typeof(ITrack<,>).Assembly.Location)));
        var sources = Sources(driver);
        Assert.Single(sources);
        Assert.Contains("global::Tl.PairRuntime<global::Domain.BuffTrack, global::Domain.BuffClip>.Consume(&ExecuteActive_ApplyBuff, &Bind_ApplyBuff, \"ConsumerBindingEmission\\u0000global::Domain.ApplyBuff\");", sources.Values.Single());
    }

    internal static Dictionary<string, IncrementalStepRunReason> TypeReasons(GeneratorDriver driver)
        => driver.GetRunResult().Results.Single().TrackedSteps[TimelineIncrementalGenerator.TypeStep]
            .SelectMany(static step => step.Outputs, static (step, output) => (Model: output.Value as TypeAnalysis, output.Reason))
            .Where(static entry => entry.Model is not null)
            .GroupBy(static entry => entry.Model!.TypeName, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last().Reason, StringComparer.Ordinal);

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

    private static Dictionary<string, string> Generate()
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
        var compilation = CSharpCompilation.Create("ConsumerBindingEmission",
            [CSharpSyntaxTree.ParseText(source, options, "Domain.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static s => s.HintName, static s => s.SourceText.ToString(), StringComparer.Ordinal);
        return (sources, diagnostics);
    }

    private static CSharpCompilation Compilation(string source, string name = "ConsumerBindingTests")
        => CSharpCompilation.Create(
            name,
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

    internal static IEnumerable<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();
}
