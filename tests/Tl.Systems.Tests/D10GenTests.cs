using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class D10GenTests
{
    internal static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        Assert.True(File.Exists(Path.Combine(dir, "tl.slnx")), "D10 snapshot test must run from a repository checkout.");
        return dir;
    }

    internal static (IReadOnlyList<CompileArtifact> Artifacts, IReadOnlyList<DeclarationDiagnostic> Diagnostics) EmitD10()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "tests", "Tl.Systems.Tests", "TwoTrackD10.cs"));
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "TwoTrackD10.cs");
        var compilation = CSharpCompilation.Create("Tl.Systems.Tests", [tree], EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return SystemGeneration.Emit(compilation, "D10");
    }

    [Fact]
    public void D10EmissionMatchesCommitted()
    {
        var (artifacts, diagnostics) = EmitD10();
        Assert.True(diagnostics.Count == 0, string.Join("\n", diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var actual = Assert.Single(artifacts).Content.Replace("\r\n", "\n");
        var path = Path.Combine(RepoRoot(), "tests", "Tl.Systems.Tests", "Generated", "TlSystemsD10.g.cs");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_D10") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
        }
        Assert.True(File.Exists(path), "D10 snapshot missing; run with TL_UPDATE_D10=1 to create it.");
        var expected = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.True(actual == expected, $"D10 emission differs at offset {EmissionTests.FirstDifference(actual, expected)}.");
    }
}
