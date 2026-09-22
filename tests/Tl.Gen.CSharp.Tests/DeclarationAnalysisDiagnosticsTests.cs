using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class DeclarationAnalysisDiagnosticsTests
{
    private const string Domain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
        }
        """;

    private const string FrostPair = """
        public readonly record struct FrostClip(float Amount);
        public readonly record struct FrostTrack(float Multiplier) : IBlend<FrostClip>
        {
            public void Blend(in FrostClip first, in FrostClip second, float factor, out FrostClip result) => result = first;
        }
        public readonly struct ChillDamage : ITrack<FrostTrack, FrostClip>
        {
            public static void OnActive(in Frame<FrostTrack, FrostClip> frame) { }
        }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
        }
        """;

    [Fact]
    public void DiagnosticToStringFormatsFileLineColumnCodeAndMessage()
    {
        var diagnostic = new DeclarationDiagnostic("Domain.cs", 3, 7, "TLGEN66", "message text");

        Assert.Equal("Domain.cs(3,7): error TLGEN66: message text", diagnostic.ToString());
    }

    [Fact]
    public void ReadThrowsForNullCompilation()
    {
        Assert.Throws<ArgumentNullException>(() => JobReader.Read(null!));
    }

    [Fact]
    public void UnresolvedContractsWithoutMarkerTextYieldNoDiagnostics()
    {
        var result = JobReader.Read(CompileWithoutReferences("""
            namespace Clean;
            internal struct Value { public int Number; }
            """));

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Consumers);
        Assert.Empty(result.Bakes);
    }

    [Fact]
    public void UnresolvedContractsWithMarkerTextReportTlgen60()
    {
        var result = JobReader.Read(CompileWithoutReferences("""
            namespace Broken;
            internal interface ITrack<TTrack, TClip> { }
            """));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN60", diagnostic.Code);
        Assert.Contains("contracts could not be resolved", diagnostic.Message);
        Assert.Contains("error TLGEN60", diagnostic.ToString());
        Assert.Empty(result.Consumers);
        Assert.Empty(result.Bakes);
    }

    [Fact]
    public void ValidJobProducesConsumerWithSlots()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Slotted : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, in DamageTrack track, ref DamageClip clip) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.Equal("global::Domain.Slotted", consumer.Job.TypeName);
        Assert.Empty(consumer.Job.Slots);
        Assert.Equal(
        [
            new TimelineSlot("track", "global::Domain.DamageTrack", SlotMode.Input),
            new TimelineSlot("clip", "global::Domain.DamageClip", SlotMode.Reference),
        ], consumer.Job.LiveColumns);
    }

    [Fact]
    public void JobWithoutOnActiveReportsTlgen66()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Silent : ITrack<DamageTrack, DamageClip> { }
            """);

        AssertConsumerRejected(result, "TLGEN66");
    }

    [Fact]
    public void JobWithAmbiguousOnActiveReportsTlgen66()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Ambiguous : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, int extra) { }
            }
            """);

        AssertConsumerRejected(result, "TLGEN66");
    }

    [Theory]
    [InlineData("out int amount")]
    [InlineData("int amount = 3")]
    [InlineData("string label")]
    public void DisallowedGameplayParameterReportsTlgen78(string parameter)
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, {{parameter}}) { amount = 0; }
            }
            """);

        AssertConsumerRejected(result, "TLGEN78");
    }

    [Fact]
    public void BakeWithOpenConsumerTypeParameterReportsTlgen70()
    {
        var result = Read($$"""
            {{Domain}}
            public sealed class Wrapper<T>
            {
                public readonly struct OpenBake : IBake<T>
                {
                    public static void Bake() { }
                }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN70", diagnostic.Code);
        Assert.Contains("open type parameter", diagnostic.Message);
        Assert.Empty(result.Bakes);
    }

    [Fact]
    public void BakeImplementingTwoPairingsSharesOneDeclarationOrderedByConsumer()
    {
        var result = Read($$"""
            {{Domain}}
            {{FrostPair}}
            public readonly struct SharedBake : IBake<ChillDamage>, IBake<ApplyDamage>
            {
                public static void Bake(int marks) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Bakes.Count);
        Assert.Equal("global::Domain.SharedBake", result.Bakes[0].TypeName);
        Assert.Equal("global::Domain.ApplyDamage", result.Bakes[0].ConsumerTypeName);
        Assert.Equal("global::Domain.ChillDamage", result.Bakes[1].ConsumerTypeName);
        var parameter = Assert.Single(result.Bakes[0].Parameters);
        Assert.Equal(new BakeParameter("int", BakeModifier.Value, false), parameter);
        Assert.Equal(result.Bakes[0].SignatureKey, result.Bakes[1].SignatureKey);
    }

    [Fact]
    public void UnityEmitterWithoutConsumersOrBakesEmitsNothing()
    {
        Assert.Empty(UnityJobEmitter.Emit(new JobReadResult([], [], [])));
    }

    [Fact]
    public void UnityEmitterWithConsumerEmitsSingleBindingArtifact()
    {
        var consumer = new JobConsumer(
            "global::Domain.DamageTrack",
            "global::Domain.DamageClip",
            new JobDefinition("global::Domain.ApplyDamage", [new TimelineSlot("world", "global::Domain.World", SlotMode.Reference)]));

        var artifact = Assert.Single(UnityJobEmitter.Emit(new JobReadResult([consumer], [], [])));

        Assert.Equal("TlConsumerBinding.g.cs", artifact.RelativePath);
        Assert.Contains("global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume", artifact.Content);
    }

    private static void AssertConsumerRejected(JobReadResult result, string code)
    {
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Empty(result.Consumers);
        Assert.Empty(result.Bakes);
    }

    private static JobReadResult Read(string source)
        => JobReader.Read(Compile(source, "DeclarationAnalysisDiagnosticsTests" + Guid.NewGuid().ToString("N")));

    private static CSharpCompilation Compile(string source, string assemblyName)
        => CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

    private static CSharpCompilation CompileWithoutReferences(string source)
        => CSharpCompilation.Create("Unresolved" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")],
            [],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static IEnumerable<MetadataReference> References()
        => CliFrontDoorTests.ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));
}
