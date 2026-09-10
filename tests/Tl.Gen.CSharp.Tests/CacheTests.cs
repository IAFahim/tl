using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class CacheTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tl-cache-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void EqualInputsPreserveGeneratedFiles()
    {
        var source = new CompileSource("/src/Timeline.cs", "source");
        var key = CompileGenerationCache.GetKey([source], ["A"], ["reference=first"]);
        CompileGenerationCache.Synchronize(_directory, key, [new CompileArtifact("Tl0.g.cs", "content\n")], "report\n", null);
        var path = Path.Combine(_directory, "Tl0.g.cs");
        var reportPath = Path.Combine(_directory, CompileGenerationCache.ReportFileName);
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, timestamp);
        File.SetLastWriteTimeUtc(reportPath, timestamp);

        var manifest = CompileGenerationCache.Load(_directory);

        Assert.True(CompileGenerationCache.IsHit(_directory, key, manifest));
        CompileGenerationCache.Synchronize(_directory, key, [new CompileArtifact("Tl0.g.cs", "content\n")], "report\n", manifest);
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(reportPath));
    }

    [Fact]
    public void MissingReportInvalidatesTheCache()
    {
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(_directory, key, [], "report\n", null);
        var manifest = CompileGenerationCache.Load(_directory);

        File.Delete(Path.Combine(_directory, CompileGenerationCache.ReportFileName));

        Assert.False(CompileGenerationCache.IsHit(_directory, key, manifest));
    }

    [Fact]
    public void NullOutputInvalidatesTheManifest()
    {
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(
            _directory,
            key,
            [new CompileArtifact("Tl0.g.cs", "content\n")],
            "report\n",
            null);
        var path = Path.Combine(_directory, CompileGenerationCache.ManifestFileName);
        var content = File.ReadAllText(path);
        var malformed = content.Replace("\"outputs\": [", "\"outputs\": [\n    null,", StringComparison.Ordinal);
        Assert.NotEqual(content, malformed);
        File.WriteAllText(path, malformed);

        Assert.Null(CompileGenerationCache.Load(_directory));
    }

    [Fact]
    public void ChangedReportInvalidatesAndRestoresTheCache()
    {
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(_directory, key, [], "report\n", null);
        var manifest = CompileGenerationCache.Load(_directory);
        var reportPath = Path.Combine(_directory, CompileGenerationCache.ReportFileName);

        File.WriteAllText(reportPath, "changed\n");

        Assert.False(CompileGenerationCache.IsHit(_directory, key, manifest));
        CompileGenerationCache.Synchronize(_directory, key, [], "report\n", manifest);
        Assert.Equal("report\n", File.ReadAllText(reportPath));
    }

    [Fact]
    public void SourcesDefinesReferencesAndOptionsChangeTheKey()
    {
        var source = new CompileSource("/src/Timeline.cs", "source");
        var key = CompileGenerationCache.GetKey([source], ["A"], ["reference=first", "nullable=enable"]);

        Assert.NotEqual(key, CompileGenerationCache.GetKey([source with { Content = "changed" }], ["A"], ["reference=first", "nullable=enable"]));
        Assert.NotEqual(key, CompileGenerationCache.GetKey([source], ["B"], ["reference=first", "nullable=enable"]));
        Assert.NotEqual(key, CompileGenerationCache.GetKey([source], ["A"], ["reference=second", "nullable=enable"]));
        Assert.NotEqual(key, CompileGenerationCache.GetKey([source], ["A"], ["reference=first", "nullable=disable"]));
    }

    [Fact]
    public void SynchronizeRemovesOnlyUnchangedOwnedFiles()
    {
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(
            _directory,
            key,
            [new CompileArtifact("Tl0.g.cs", "first\n"), new CompileArtifact("Tl1.g.cs", "second\n")],
            "report\n",
            null);
        var previous = CompileGenerationCache.Load(_directory);
        File.WriteAllText(Path.Combine(_directory, "Tl1.g.cs"), "owned by user\n");

        CompileGenerationCache.Synchronize(
            _directory,
            key + "changed",
            [new CompileArtifact("Tl0.g.cs", "first\n")],
            "report\n",
            previous);

        Assert.True(File.Exists(Path.Combine(_directory, "Tl1.g.cs")));
        Assert.Equal(["Tl0.g.cs"], File.ReadAllLines(Path.Combine(_directory, CompileGenerationCache.SourceListFileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
