using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using static Tl.Gen.CSharp.Tests.ConsumerBindingTests;

namespace Tl.Gen.CSharp.Tests;

public sealed class ChainedGenerationTests
{
    private const string Synthesized = """
        using Tl;
        namespace Hosted;
        public readonly record struct BurnClip(float Amount);
        public readonly record struct BurnTrack(float Multiplier) : IBlend<BurnClip>
        {
            public void Blend(in BurnClip first, in BurnClip second, float factor, out BurnClip result)
                => result = new BurnClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public struct Armor { public float Scale; }
        public readonly struct ApplyBurn : ITrack<BurnTrack, BurnClip>
        {
            public static void ExecuteActive(in Frame<BurnTrack, BurnClip> frame, in Armor armor, ref float heat)
                => heat += frame.Clip.Amount * frame.Track.Multiplier * armor.Scale;
        }
        public readonly struct BurnBake : IBake<ApplyBurn>
        {
            public static void Bake(ref float seed) { }
        }
        """;

    private const string AuthoredBake = """
        using Tl;
        namespace Domain;
        public readonly struct AuthoredDamageBake : IBake<ApplyDamage>
        {
            public static void Bake(ref float seed) { }
        }
        """;

    private static CSharpParseOptions Options => CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    private static CSharpCompilationOptions CompilationOptions => new(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable);

    private static CSharpCompilation Augmented(out SyntaxTree synthesized, bool withAuthoredBake = false)
    {
        var original = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(Source, Options, "Domain.cs") };
        if (withAuthoredBake)
            original.Add(CSharpSyntaxTree.ParseText(AuthoredBake, Options, "AuthoredBake.cs"));
        synthesized = CSharpSyntaxTree.ParseText(Synthesized, Options, "Synthesized.g.cs");
        return CSharpCompilation.Create("ChainedGeneration" + Guid.NewGuid().ToString("N"), original, References(), CompilationOptions)
            .AddSyntaxTrees(synthesized);
    }

    [Fact]
    public void ChainedRunEmitsTheSynthesizedConsumerUnderHostedNames()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized }, new HashSet<string> { "global::Hosted.ApplyBurn" });

        Assert.Empty(result.Diagnostics);
        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("TlConsumerBindingHosted.g.cs", artifact.RelativePath);
        Assert.Contains("internal static unsafe class TlConsumerBindingHosted", artifact.Content);
        Assert.Contains("internal sealed class TlConsumerLayoutHostedAttribute", artifact.Content);
        Assert.Contains("Hosted.ApplyBurn.ExecuteActive", artifact.Content);
    }

    [Fact]
    public void ConsumerFilterKeepsAuthoredConsumersOutOfTheChainedArtifact()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized }, new HashSet<string> { "global::Hosted.ApplyBurn" });

        Assert.DoesNotContain("Domain.ApplyDamage", Assert.Single(result.Artifacts).Content);
        Assert.DoesNotContain("Domain.ApplyHeal", Assert.Single(result.Artifacts).Content);
    }

    [Fact]
    public void UnfilteredChainedRunEmitsOnlySynthesizedConsumers()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized });

        Assert.Empty(result.Diagnostics);
        var content = Assert.Single(result.Artifacts).Content;
        Assert.Contains("Hosted.ApplyBurn", content);
        Assert.DoesNotContain("Domain.ApplyDamage", content);
        Assert.DoesNotContain("Domain.ApplyHeal", content);
    }

    [Fact]
    public void BakesForSynthesizedConsumersRideAlongAndAuthoredBakesStayBehind()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized, withAuthoredBake: true), new HashSet<SyntaxTree> { synthesized });

        Assert.Empty(result.Diagnostics);
        var content = Assert.Single(result.Artifacts).Content;
        Assert.Contains("BakeRuntime<global::Hosted.BurnTrack, global::Hosted.BurnClip>", content);
        Assert.Contains("BurnBake.Bake", content);
        Assert.DoesNotContain("AuthoredDamageBake", content);
    }

    [Fact]
    public void UnmatchedFilterEntryIsReportedInsteadOfSilentlyMatchingNothing()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized }, new HashSet<string> { "Hosted.ApplyBurn" });

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN83", diagnostic.Code);
        Assert.Contains("Hosted.ApplyBurn", diagnostic.Message);
        Assert.Empty(result.Artifacts);
    }

    [Fact]
    public void FilterNamingAnAuthoredConsumerIsReported()
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized }, new HashSet<string> { "global::Domain.ApplyDamage" });

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN84", diagnostic.Code);
        Assert.Contains("global::Domain.ApplyDamage", diagnostic.Message);
        Assert.Empty(result.Artifacts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("my-host")]
    [InlineData("3host")]
    public void InvalidIdIsRejectedBeforeAnyGeneration(string id)
    {
        var result = TimelineGeneration.Run(Augmented(out var synthesized), new HashSet<SyntaxTree> { synthesized }, id: id);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN82", diagnostic.Code);
        Assert.Empty(result.Artifacts);
    }

    [Fact]
    public void InvalidSynthesizedConsumerSurfacesDiagnosticsAndEmitsNothing()
    {
        var options = Options;
        var broken = CSharpSyntaxTree.ParseText(Synthesized.Replace("ref float heat", "ref float heat, ref float heatAgain"), options, "Synthesized.g.cs");
        var compilation = CSharpCompilation.Create("ChainedGenerationBroken" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(Source, options, "Domain.cs")],
            References(),
            CompilationOptions)
            .AddSyntaxTrees(broken);

        var result = TimelineGeneration.Run(compilation, new HashSet<SyntaxTree> { broken });

        Assert.NotEmpty(result.Diagnostics);
        Assert.Empty(result.Artifacts);
    }
}
