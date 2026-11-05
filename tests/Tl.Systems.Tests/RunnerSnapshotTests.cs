using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class RunnerSnapshotTests
{
    internal static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        Assert.True(File.Exists(Path.Combine(dir, "tl.slnx")), "Runner snapshot test must run from a repository checkout.");
        return dir;
    }

    internal static string RunnerSource()
        => File.ReadAllText(Path.Combine(RepoRoot(), "samples", "Runner", "GameLogic.cs"));

    internal static string ExpectedSnapshot()
        => File.ReadAllText(Path.Combine(RepoRoot(), "samples", "Runner", "Generated", "TlSystemsRunner.g.cs")).Replace("\r\n", "\n");

    internal static CSharpCompilation RunnerCompilation()
    {
        var tree = CSharpSyntaxTree.ParseText(RunnerSource(), new CSharpParseOptions(LanguageVersion.Latest), "GameLogic.cs");
        return CSharpCompilation.Create("Runner", [tree], EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    [Fact]
    public void RunnerToolboxProducesZeroDiagnostics()
    {
        var compilation = RunnerCompilation();
        var read = Tl.Gen.CSharp.Analysis.SystemReader.Read(compilation);
        Assert.True(read.Diagnostics.Count == 0, string.Join("\n", read.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public void RunnerEmittedSourceMatchesSnapshot()
    {
        var compilation = RunnerCompilation();
        var emitted = SystemGeneration.Emit(compilation, "Runner");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(emitted.Artifacts);
        var actual = artifact.Content.Replace("\r\n", "\n");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1")
            File.WriteAllText(Path.Combine(RepoRoot(), "samples", "Runner", "Generated", "TlSystemsRunner.g.cs"), actual);
        Assert.True(actual == ExpectedSnapshot(), "Runner systems snapshot differs; regenerate with TL_UPDATE_SNAPSHOT=1.");
    }
}
