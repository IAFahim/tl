using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        public readonly struct ApplyDamage : ITimelineJob<DamageTrack, DamageClip>
        {
            public static void Execute(in Frame<DamageTrack, DamageClip> frame, in Resistance resistance, ref Health health)
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
        public readonly struct ApplyHeal : ITimelineJob<HealTrack, HealClip>
        {
            public static void Execute(in Frame<HealTrack, HealClip> frame, in Resistance resistance, ref Health health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health.Value += frame.IsBackward ? -amount : amount;
            }
        }
        public readonly partial struct DamageAsset : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new DamageTrack(2f)).Use<ApplyDamage>();
                builder.Clip(track, new DamageClip(8f), 0u, 1u);
            }
        }
        public readonly partial struct HealAsset : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new HealTrack(0.5f)).Use<ApplyHeal>();
                builder.Clip(track, new HealClip(8f), 0u, 1u);
            }
        }
        """;

    [Fact]
    public void EmitsOneColdInstallerWithTypedThunksForEveryDiscoveredJobPairing()
    {
        var generated = Generate();
        Assert.Equal(3, generated.Count);
        var binding = generated["TlConsumerBinding.g.cs"];
        Assert.DoesNotContain("\r", binding);
        Assert.StartsWith("internal static unsafe class TlConsumerBinding\n{\n[global::System.Runtime.CompilerServices.ModuleInitializer]\ninternal static void Install()\n{\n", binding);
        var installEnd = binding.IndexOf("}", StringComparison.Ordinal);
        var install = binding[..installEnd];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume(&Execute_ApplyDamage, &Bind_ApplyDamage);",
            "global::Tl.PairRuntime<global::Domain.HealTrack, global::Domain.HealClip>.Consume(&Execute_ApplyHeal, &Bind_ApplyHeal);",
        ], install.Split('\n')[5..^1]);
        Assert.Contains("private static void Execute_ApplyDamage(byte* __tlSlot, uint __tlGameTick, uint __tlTick, long __tlCycle, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)", binding);
        Assert.Contains("global::Domain.DamageClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.DamageTrack, global::Domain.DamageClip>(__tlSlot, __tlGameTick, __tlTick, __tlCycle, __tlFlags, ref __tlClip);", binding);
        Assert.Contains("var @resistance = (global::Domain.Resistance*)__tlColumns[0];", binding);
        Assert.Contains("var @health = (global::Domain.Health*)__tlColumns[1];", binding);
        Assert.Contains("global::Domain.ApplyDamage.Execute(in __tlTyped, in @resistance[__tlRow], ref @health[__tlRow]);", binding);
        Assert.Contains("private static void Bind_ApplyDamage(in global::Tl.TimelineQuery __tlQuery, void** __tlColumns)", binding);
        Assert.Contains("var __tlPtr0 = __tlQuery.ColumnPointer(global::Tl.TypeKey<global::Domain.Resistance>.Value);", binding);
        Assert.Contains("if (__tlPtr0 == null) throw new global::System.ArgumentException(\"global::Domain.ApplyDamage: required column missing for registered consumer: global::Domain.Resistance\");", binding);
        Assert.Contains("__tlColumns[0] = __tlPtr0;", binding);
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
            File.WriteAllLines(referencesPath, ReferencePaths().Append(typeof(ITimeline).Assembly.Location + "\tplatform\tfalse"));

            var first = Run(["--compile", "--output", directory, "--source", sourcePath, "--reference-list", referencesPath]);
            Assert.Equal(0, first.exit);
            Assert.Contains("cache miss", first.output);
            var report = File.ReadAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName));
            Assert.Contains("generated-source-files\t3", report);
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

    private static IEnumerable<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();
}
