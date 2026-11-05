using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;
namespace Tl.Systems.Tests;

public sealed class BenchGenTests
{
    internal static string DomainSource(string name)
        => EmissionTests.ReadResource("Tl.Systems.Tests.BenchDomain." + name);

    internal static (IReadOnlyList<CompileArtifact> Artifacts, IReadOnlyList<DeclarationDiagnostic> Diagnostics) EmitBench()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(DomainSource("BenchShapes.cs"), new CSharpParseOptions(LanguageVersion.Latest), "BenchShapes.cs"),
            CSharpSyntaxTree.ParseText(DomainSource("BenchS64Base.cs"), new CSharpParseOptions(LanguageVersion.Latest), "BenchS64Base.cs"),
            CSharpSyntaxTree.ParseText(DomainSource("BenchS64Consumers.cs"), new CSharpParseOptions(LanguageVersion.Latest), "BenchS64Consumers.cs"),
        };
        var compilation = CSharpCompilation.Create("Systems", trees, EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return SystemGeneration.Emit(compilation, "Bench");
    }

    [Fact]
    public void BenchDomainProducesZeroDiagnostics()
    {
        var (_, diagnostics) = EmitBench();
        Assert.True(diagnostics.Count == 0, string.Join("\n", diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public void BenchEmissionParsesAsCSharp9()
    {
        var (artifacts, diagnostics) = EmitBench();
        Assert.True(diagnostics.Count == 0, string.Join("\n", diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(artifacts);
        var parsed = CSharpSyntaxTree.ParseText(artifact.Content, new CSharpParseOptions(LanguageVersion.CSharp9), artifact.RelativePath);
        var errors = parsed.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public void BenchEmissionMatchesCommitted()
    {
        var (artifacts, diagnostics) = EmitBench();
        Assert.True(diagnostics.Count == 0, string.Join("\n", diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var actual = Assert.Single(artifacts).Content.Replace("\r\n", "\n");
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(root, "Tl.Systems.Tests.csproj")); i++) root = Path.GetDirectoryName(root)!;
        var path = Path.Combine(root, "..", "..", "benchmarks", "Systems", "Generated", "TlSystemsBench.g.cs");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_BENCH") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
        }
        Assert.True(File.Exists(path), "Bench snapshot missing; run with TL_UPDATE_BENCH=1 to create it.");
        var expected = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.True(actual == expected, $"Bench emission differs at offset {EmissionTests.FirstDifference(actual, expected)}.");
    }
}
