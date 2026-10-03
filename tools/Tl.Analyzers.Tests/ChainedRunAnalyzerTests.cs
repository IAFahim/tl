using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tl.Analyzers;
using Tl.Gen.CSharp;
using Tl;
using Xunit;

namespace Tl.Analyzers.Tests;

public sealed class ChainedRunAnalyzerTests
{
    const string Host = """
        using Microsoft.CodeAnalysis;
        using Tl.Gen.CSharp;

        static class Unrelated
        {
            internal static void Run(CSharpCompilation compilation, ISet<SyntaxTree> synthesized, ISet<string>? consumers = null, string id = "Hosted") { }
        }

        static class HostedGeneration
        {
            static readonly string dynamicId = "Hosted";

            internal static void Run(CSharpCompilation compilation, ISet<SyntaxTree> synthesized)
            {
                <CALL>;
            }
        }
        """;

    static (CSharpCompilation Compilation, string Text) Build(string call)
    {
        var text = Host.Replace("<CALL>", call);
        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location)
            .Append(typeof(TimelineGeneration).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ChainedRunAnalyzerTests", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (compilation, text);
    }

    static async Task<ImmutableArray<Diagnostic>> Analyze(string call)
        => await Build(call).Compilation.WithAnalyzers([new ChainedRunAnalyzer()]).GetAnalyzerDiagnosticsAsync();

    static Diagnostic Single(ImmutableArray<Diagnostic> diagnostics, string id)
        => Assert.Single(diagnostics.Where(d => d.Id == id));

    [Fact]
    public async Task AuditReproShapesFireAtEditTime()
    {
        var diagnostics = await Analyze("""TimelineGeneration.Run(compilation, synthesized, new[] { "Hosted.ApplyBurn" }, "my-host")""");

        var id = Single(diagnostics, "TLGEN85");
        Assert.Equal(DiagnosticSeverity.Error, id.Severity);
        Assert.Contains("'my-host'", id.GetMessage(), StringComparison.Ordinal);

        var filter = Single(diagnostics, "TLGEN86");
        Assert.Equal(DiagnosticSeverity.Warning, filter.Severity);
        Assert.Contains("'Hosted.ApplyBurn'", filter.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("global::Hosted.ApplyBurn", filter.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"my-host\"")]
    [InlineData("\"3host\"")]
    [InlineData("\"spaced id\"")]
    public async Task InvalidIdLiteralsAreErrors(string literal)
    {
        var diagnostics = await Analyze($"TimelineGeneration.Run(compilation, synthesized, id: {literal})");

        var id = Single(diagnostics, "TLGEN85");
        Assert.Equal(DiagnosticSeverity.Error, id.Severity);
    }

    [Fact]
    public async Task NonLiteralIdIsSkipped()
    {
        var diagnostics = await Analyze("TimelineGeneration.Run(compilation, synthesized, id: dynamicId)");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnqualifiedFiltersFirePerEntryAcrossCollectionShapes()
    {
        var array = await Analyze("""TimelineGeneration.Run(compilation, synthesized, new[] { "global::Hosted.ApplyBurn", "Hosted.ApplyBurn" })""");
        Assert.Equal("TLGEN86", Single(array, "TLGEN86").Id);

        var collection = await Analyze("""TimelineGeneration.Run(compilation, synthesized, ["Hosted.ApplyBurn", "Hosted.ApplyHeal"])""");
        Assert.Equal(2, collection.Count(d => d.Id == "TLGEN86"));

        var set = await Analyze("""TimelineGeneration.Run(compilation, synthesized, new HashSet<string> { "Hosted.ApplyBurn" })""");
        Assert.Equal("TLGEN86", Single(set, "TLGEN86").Id);
    }
    [Fact]
    public async Task QualifiedFiltersAndCleanCallsProduceNoDiagnostics()
    {
        Assert.Empty(await Analyze("TimelineGeneration.Run(compilation, synthesized)"));
        Assert.Empty(await Analyze("""TimelineGeneration.Run(compilation, synthesized, ["global::Hosted.ApplyBurn"], "hosted")"""));
        Assert.Empty(await Analyze("""TimelineGeneration.Run(compilation, synthesized, consumers: null, id: "Hosted")"""));
        Assert.Empty(await Analyze("""TimelineGeneration.Run(compilation, synthesized, consumers: [nameof(HostedGeneration)])"""));
    }

    [Fact]
    public async Task OtherRunMethodsAreIgnored()
    {
        var diagnostics = await Analyze("""Unrelated.Run(compilation, synthesized, new[] { "Hosted.ApplyBurn" }, "my-host")""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CombinedMisuseReportsBothRulesIndependently()
    {
        var diagnostics = await Analyze("""TimelineGeneration.Run(compilation, synthesized, consumers: ["Domain.ApplyDamage"], id: "")""");

        Assert.Equal(2, diagnostics.Length);
        Assert.Contains(diagnostics, d => d.Id is "TLGEN85");
        Assert.Contains(diagnostics, d => d.Id is "TLGEN86");
    }

    [Fact]
    public async Task DiagnosticsAnchorOnTheOffendingTokens()
    {
        var call = """TimelineGeneration.Run(compilation, synthesized, new[] { "Hosted.ApplyBurn" }, "my-host")""";
        var (compilation, text) = Build(call);
        var diagnostics = await compilation.WithAnalyzers([new ChainedRunAnalyzer()]).GetAnalyzerDiagnosticsAsync();

        var filter = Single(diagnostics, "TLGEN86");
        Assert.Equal(text.IndexOf("\"Hosted.ApplyBurn\"", StringComparison.Ordinal), filter.Location.SourceSpan.Start);

        var id = Single(diagnostics, "TLGEN85");
        Assert.Equal(text.IndexOf("\"my-host\"", StringComparison.Ordinal), id.Location.SourceSpan.Start);
    }
}
