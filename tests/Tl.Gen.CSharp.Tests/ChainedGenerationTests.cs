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
        """;

    private static CSharpCompilation Augmented()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        return CSharpCompilation.Create("ChainedGeneration" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(Source, options, "Domain.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(Synthesized, options, "Synthesized.g.cs"));
    }

    [Fact]
    public void ChainedRunEmitsTheSynthesizedConsumerUnderHostedNames()
    {
        var result = TimelineGeneration.Run(Augmented(), new HashSet<string> { "global::Hosted.ApplyBurn" });

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
        var result = TimelineGeneration.Run(Augmented(), new HashSet<string> { "global::Hosted.ApplyBurn" });

        Assert.DoesNotContain("Domain.ApplyDamage", Assert.Single(result.Artifacts).Content);
        Assert.DoesNotContain("Domain.ApplyHeal", Assert.Single(result.Artifacts).Content);
    }

    [Fact]
    public void UnfilteredChainedRunEmitsEveryConsumerItCanSee()
    {
        var result = TimelineGeneration.Run(Augmented());

        var content = Assert.Single(result.Artifacts).Content;
        Assert.Contains("Domain.ApplyDamage", content);
        Assert.Contains("Hosted.ApplyBurn", content);
    }

    [Fact]
    public void InvalidSynthesizedConsumerSurfacesDiagnosticsAndEmitsNothing()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var broken = Synthesized.Replace("ref float heat", "ref float heat, ref float heatAgain");
        var compilation = CSharpCompilation.Create("ChainedGenerationBroken" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(broken, options, "Synthesized.g.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

        var result = TimelineGeneration.Run(compilation);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Empty(result.Artifacts);
    }
}
