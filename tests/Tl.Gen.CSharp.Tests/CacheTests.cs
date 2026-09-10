using Xunit;
using System.Text.Json;

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
        Assert.Equal("report missing", CompileGenerationCache.MissReason(_directory, key, manifest));
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
    public void InvalidManifestsAreRejected()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, CompileGenerationCache.ManifestFileName);
        File.WriteAllText(path, "{");
        Assert.Null(CompileGenerationCache.Load(_directory));

        foreach (var manifest in InvalidManifests())
        {
            var json = JsonSerializer.Serialize(manifest, CompileManifestJsonContext.Default.CompileGenerationManifest);
            File.WriteAllText(path, json);
            Assert.Null(CompileGenerationCache.Load(_directory));
        }
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
        Assert.Equal("report changed", CompileGenerationCache.MissReason(_directory, key, manifest));
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
        Assert.Equal("manifest missing", CompileGenerationCache.MissReason(_directory, key, null));
    }

    [Fact]
    public void EveryCachedFileParticipatesInTheHitDecision()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, CompileGenerationCache.ManifestFileName), "{");
        var key = CompileGenerationCache.GetKey([], [], []);
        Assert.Equal("manifest invalid", CompileGenerationCache.MissReason(_directory, key, null));
        Assert.Equal("inputs changed", MissReason(static (_, manifest) => manifest.CacheKey = "changed"));
        Assert.Equal("manifest outputs invalid", MissReason(static (_, manifest) => manifest.Outputs = null));
        Assert.Equal("manifest outputs invalid", MissReason(static (_, manifest) => manifest.Outputs!.Add(null)));
        Assert.Equal("source list manifest changed", MissReason(static (_, manifest) => manifest.SourceListHash = new string('0', 64)));
        Assert.Equal("source list missing", MissReason(static directory =>
            File.Delete(Path.Combine(directory, CompileGenerationCache.SourceListFileName))));
        Assert.Equal("source list changed", MissReason(static directory =>
            File.WriteAllText(Path.Combine(directory, CompileGenerationCache.SourceListFileName), "changed\n")));
        Assert.Equal("report missing", MissReason(static directory =>
            File.Delete(Path.Combine(directory, CompileGenerationCache.ReportFileName))));
        Assert.Equal("report changed", MissReason(static directory =>
            File.WriteAllText(Path.Combine(directory, CompileGenerationCache.ReportFileName), "changed\n")));
        Assert.Equal("artifact missing: Tl0.g.cs", MissReason(static directory =>
            File.Delete(Path.Combine(directory, "Tl0.g.cs"))));
        Assert.Equal("artifact changed: Tl0.g.cs", MissReason(static directory =>
            File.WriteAllText(Path.Combine(directory, "Tl0.g.cs"), "changed\n")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/Tl0.g.cs")]
    [InlineData("nested/Tl0.g.cs")]
    [InlineData("nested\\Tl0.g.cs")]
    [InlineData("Tl0\r.g.cs")]
    [InlineData("Tl0\n.g.cs")]
    [InlineData("Tl0.cs")]
    [InlineData("0Tl.g.cs")]
    public void SynchronizeRejectsUnsafeArtifactPaths(string relativePath)
    {
        var error = Assert.Throws<InvalidOperationException>(() => CompileGenerationCache.Synchronize(
            _directory,
            "key",
            [new CompileArtifact(relativePath, "content\n")],
            "report\n",
            null));

        Assert.Contains(relativePath, error.Message);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void SynchronizeRejectsCaseInsensitiveDuplicateArtifactPaths()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CompileGenerationCache.Synchronize(
            _directory,
            "key",
            [new CompileArtifact("Tl0.g.cs", "first\n"), new CompileArtifact("tl0.g.cs", "second\n")],
            "report\n",
            null));

        Assert.Contains("tl0.g.cs", error.Message);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void SynchronizeRemovesOnlyUnchangedOwnedFiles()
    {
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(
            _directory,
            key,
            [
                new CompileArtifact("Tl0.g.cs", "first\n"),
                new CompileArtifact("Tl1.g.cs", "second\n"),
                new CompileArtifact("Tl2.g.cs", "third\n"),
            ],
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
        Assert.False(File.Exists(Path.Combine(_directory, "Tl2.g.cs")));
        Assert.Equal(["Tl0.g.cs"], File.ReadAllLines(Path.Combine(_directory, CompileGenerationCache.SourceListFileName)));
    }

    private string? MissReason(Action<string> mutate)
        => MissReason((directory, _) => mutate(directory));

    private string? MissReason(Action<string, CompileGenerationManifest> mutate)
    {
        var directory = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
        var key = CompileGenerationCache.GetKey([], [], []);
        CompileGenerationCache.Synchronize(
            directory,
            key,
            [new CompileArtifact("Tl0.g.cs", "content\n")],
            "report\n",
            null);
        var manifest = CompileGenerationCache.Load(directory)!;
        mutate(directory, manifest);
        return CompileGenerationCache.MissReason(directory, key, manifest);
    }

    private static IEnumerable<CompileGenerationManifest> InvalidManifests()
    {
        var hash = new string('0', 64);
        var output = new CompileGenerationOutput { RelativePath = "Tl0.g.cs", ContentHash = hash };

        yield return new() { FormatVersion = 4, SourceListHash = hash, ReportHash = hash, Outputs = [output] };
        yield return new() { FormatVersion = 5, SourceListHash = hash, ReportHash = hash, Outputs = null };
        yield return new() { FormatVersion = 5, SourceListHash = "short", ReportHash = hash, Outputs = [output] };
        yield return new() { FormatVersion = 5, SourceListHash = hash, ReportHash = "short", Outputs = [output] };
        yield return new() { FormatVersion = 5, SourceListHash = hash, ReportHash = hash, Outputs = [null] };
        yield return new()
        {
            FormatVersion = 5,
            SourceListHash = hash,
            ReportHash = hash,
            Outputs = [new() { RelativePath = "../Tl0.g.cs", ContentHash = hash }],
        };
        yield return new()
        {
            FormatVersion = 5,
            SourceListHash = hash,
            ReportHash = hash,
            Outputs = [output, new() { RelativePath = "tl0.g.cs", ContentHash = hash }],
        };
        yield return new()
        {
            FormatVersion = 5,
            SourceListHash = hash,
            ReportHash = hash,
            Outputs = [new() { RelativePath = "Tl0.g.cs", ContentHash = "short" }],
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
