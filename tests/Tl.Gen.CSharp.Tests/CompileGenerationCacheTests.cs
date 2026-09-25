using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class CompileGenerationCacheTests
{
    private const string AlphaContent = "// alpha generated";
    private const string BetaContent = "// beta generated";
    private const string ReportBody = "format\t4\nconsumers\t0\n";

    [Fact]
    public void SynchronizedCacheHitsWithNoMissReason()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));

        Assert.Null(CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsChangedInputs()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));

        Assert.Equal("inputs changed", CompileGenerationCache.MissReason(cache.Directory, cache.Key + "-other", cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsInvalidOutputsWhenOutputsAreNull()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        var manifest = cache.Manifest();
        manifest.Outputs = null;

        Assert.Equal("manifest outputs invalid", CompileGenerationCache.MissReason(cache.Directory, cache.Key, manifest));
    }

    [Fact]
    public void MissReasonReportsChangedSourceListManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        var manifest = cache.Manifest();
        manifest.SourceListHash = new string('b', 64);

        Assert.Equal("source list manifest changed", CompileGenerationCache.MissReason(cache.Directory, cache.Key, manifest));
    }

    [Fact]
    public void MissReasonReportsMissingSourceListFile()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.Delete(Path.Combine(cache.Directory, CompileGenerationCache.SourceListFileName));

        Assert.Equal("source list missing", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsChangedSourceListFile()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.WriteAllText(Path.Combine(cache.Directory, CompileGenerationCache.SourceListFileName), "Tampered.g.cs\n");

        Assert.Equal("source list changed", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsMissingReportFile()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.Delete(Path.Combine(cache.Directory, CompileGenerationCache.ReportFileName));

        Assert.Equal("report missing", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsChangedReportFile()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.WriteAllText(Path.Combine(cache.Directory, CompileGenerationCache.ReportFileName), "tampered");

        Assert.Equal("report changed", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonDistinguishesAMissingFromAnInvalidManifest()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tl-compile-cache-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Equal("manifest missing", CompileGenerationCache.MissReason(directory, "key", null));

            File.WriteAllText(Path.Combine(directory, CompileGenerationCache.ManifestFileName), "{ not a valid manifest");

            Assert.Equal("manifest invalid", CompileGenerationCache.MissReason(directory, "key", null));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void MissReasonReportsMissingArtifact()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.Delete(Path.Combine(cache.Directory, "Alpha.g.cs"));

        Assert.Equal("artifact missing: Alpha.g.cs", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void MissReasonReportsChangedArtifact()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        File.WriteAllText(Path.Combine(cache.Directory, "Alpha.g.cs"), "// tampered");

        Assert.Equal("artifact changed: Alpha.g.cs", CompileGenerationCache.MissReason(cache.Directory, cache.Key, cache.Manifest()));
    }

    [Fact]
    public void LoadReturnsNullWhenManifestIsMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tl-compile-cache-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Null(CompileGenerationCache.Load(directory));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LoadReturnsNullForMalformedJson()
    {
        using var cache = new CacheDirectory();
        File.WriteAllText(Path.Combine(cache.Directory, CompileGenerationCache.ManifestFileName), "{ not json");

        Assert.Null(CompileGenerationCache.Load(cache.Directory));
    }

    [Theory]
    [InlineData("""{"formatVersion":4,"cacheKey":"k","sourceListHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","reportHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","outputs":[]}""")]
    [InlineData("""{"formatVersion":5,"cacheKey":"k","sourceListHash":null,"reportHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","outputs":[]}""")]
    [InlineData("""{"formatVersion":5,"cacheKey":"k","sourceListHash":"short","reportHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","outputs":[]}""")]
    [InlineData("""{"formatVersion":5,"cacheKey":"k","sourceListHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","reportHash":"","outputs":[]}""")]
    [InlineData("""{"formatVersion":5,"cacheKey":"k","sourceListHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","reportHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","outputs":null}""")]
    public void LoadReturnsNullForInvalidManifestHeader(string manifestJson)
    {
        using var cache = new CacheDirectory();
        File.WriteAllText(Path.Combine(cache.Directory, CompileGenerationCache.ManifestFileName), manifestJson);

        Assert.Null(CompileGenerationCache.Load(cache.Directory));
    }

    [Theory]
    [InlineData("""[null]""")]
    [InlineData("""[{"relativePath":"Nested/Alpha.g.cs","contentHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]""")]
    [InlineData("""[{"relativePath":"Alpha.cs","contentHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]""")]
    [InlineData("""[{"relativePath":"1Alpha.g.cs","contentHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]""")]
    [InlineData("""[{"relativePath":"Alpha.g.cs","contentHash":"zz"}]""")]
    [InlineData("""[{"relativePath":"Alpha.g.cs","contentHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},{"relativePath":"Alpha.g.cs","contentHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]""")]
    public void LoadReturnsNullForInvalidOutputEntries(string outputsJson)
    {
        using var cache = new CacheDirectory();
        var manifestJson = $$"""
            {
              "formatVersion": 5,
              "cacheKey": "k",
              "sourceListHash": "{{new string('a', 64)}}",
              "reportHash": "{{new string('a', 64)}}",
              "outputs": {{outputsJson}}
            }
            """;
        File.WriteAllText(Path.Combine(cache.Directory, CompileGenerationCache.ManifestFileName), manifestJson);

        Assert.Null(CompileGenerationCache.Load(cache.Directory));
    }

    [Fact]
    public void LoadRoundTripsASynchronizedManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        var manifest = cache.Manifest();

        Assert.Equal(cache.Key, manifest.CacheKey);
        var output = Assert.Single(manifest.Outputs!);
        Assert.Equal("Alpha.g.cs", output!.RelativePath);
        Assert.Equal(64, output.ContentHash.Length);
    }

    [Theory]
    [InlineData("Nested/Alpha.g.cs")]
    [InlineData("Alpha.cs")]
    [InlineData("")]
    [InlineData("../Alpha.g.cs")]
    public void SynchronizeThrowsForUnownedArtifactPaths(string relativePath)
    {
        using var cache = new CacheDirectory();

        Assert.Throws<InvalidOperationException>(() => CompileGenerationCache.Synchronize(
            cache.Directory, cache.Key, [new CompileArtifact(relativePath, "content")], ReportBody, null));
    }

    [Fact]
    public void SynchronizeCleansTheTempFileWhenAnArtifactTargetIsADirectory()
    {
        using var cache = new CacheDirectory();
        var targetPath = Path.Combine(cache.Directory, "Alpha.g.cs");
        Directory.CreateDirectory(targetPath);

        var failure = Assert.ThrowsAny<Exception>(() => CompileGenerationCache.Synchronize(
            cache.Directory, cache.Key, [new CompileArtifact("Alpha.g.cs", AlphaContent)], ReportBody, null));
        Assert.True(failure is IOException or UnauthorizedAccessException, failure.GetType().Name);

        Assert.True(Directory.Exists(targetPath));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(cache.Directory),
            static path => path.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public void SynchronizeThrowsForDuplicateArtifactPaths()
    {
        using var cache = new CacheDirectory();

        Assert.Throws<InvalidOperationException>(() => CompileGenerationCache.Synchronize(
            cache.Directory,
            cache.Key,
            [CompileArtifact("Alpha.g.cs", AlphaContent), CompileArtifact("Alpha.g.cs", BetaContent)],
            ReportBody,
            null));
    }

    [Fact]
    public void SynchronizeDeletesStaleOutputsFromThePreviousManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent), CompileArtifact("Beta.g.cs", BetaContent));
        var stalePath = Path.Combine(cache.Directory, "Beta.g.cs");

        CompileGenerationCache.Synchronize(cache.Directory, cache.Key, [CompileArtifact("Alpha.g.cs", AlphaContent)], ReportBody, cache.Manifest());

        Assert.False(File.Exists(stalePath));
        Assert.True(File.Exists(Path.Combine(cache.Directory, "Alpha.g.cs")));
        Assert.Null(CompileGenerationCache.MissReason(cache.Directory, cache.Key, CompileGenerationCache.Load(cache.Directory)));
    }

    [Fact]
    public void SynchronizeKeepsStaleOutputsWhoseContentNoLongerMatchesTheManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent), CompileArtifact("Beta.g.cs", BetaContent));
        var stalePath = Path.Combine(cache.Directory, "Beta.g.cs");
        File.WriteAllText(stalePath, "// hand-edited");
        var manifest = cache.Manifest();

        CompileGenerationCache.Synchronize(cache.Directory, cache.Key, [CompileArtifact("Alpha.g.cs", AlphaContent)], ReportBody, manifest);

        Assert.Equal("// hand-edited", File.ReadAllText(stalePath));
    }

    [Fact]
    public void SynchronizeToleratesNullEntriesInThePreviousManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        var manifest = cache.Manifest();
        manifest.Outputs!.Add(null);

        CompileGenerationCache.Synchronize(cache.Directory, cache.Key, [CompileArtifact("Alpha.g.cs", AlphaContent)], ReportBody, manifest);

        Assert.True(File.Exists(Path.Combine(cache.Directory, "Alpha.g.cs")));
        Assert.Null(CompileGenerationCache.MissReason(cache.Directory, cache.Key, CompileGenerationCache.Load(cache.Directory)));
    }

    [Fact]
    public void SynchronizePreservesUnchangedArtifactsSourceListReportAndManifest()
    {
        using var cache = new CacheDirectory(CompileArtifact("Alpha.g.cs", AlphaContent));
        var stamp = new DateTime(2001, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var paths = new[]
        {
            Path.Combine(cache.Directory, "Alpha.g.cs"),
            Path.Combine(cache.Directory, CompileGenerationCache.SourceListFileName),
            Path.Combine(cache.Directory, CompileGenerationCache.ReportFileName),
            Path.Combine(cache.Directory, CompileGenerationCache.ManifestFileName),
        };
        var before = paths.ToDictionary(static path => path, File.ReadAllText, StringComparer.Ordinal);
        foreach (var path in paths)
            File.SetLastWriteTimeUtc(path, stamp);

        CompileGenerationCache.Synchronize(cache.Directory, cache.Key, [CompileArtifact("Alpha.g.cs", AlphaContent)], ReportBody, null);

        foreach (var path in paths)
        {
            Assert.Equal(before[path], File.ReadAllText(path));
            Assert.True((File.GetLastWriteTimeUtc(path) - stamp).Duration() < TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public void GetKeyDependsOnSymbolsSemanticInputsAndSources()
    {
        var sources = new[] { new CompileSource("Domain.cs", "content") };
        var baseline = CompileGenerationCache.GetKey([], []);

        Assert.NotEqual(baseline, CompileGenerationCache.GetKey([], ["SYMBOL_A"]));
        Assert.NotEqual(CompileGenerationCache.GetKey([], ["SYMBOL_A"]), CompileGenerationCache.GetKey([], ["SYMBOL_B"], null));
        Assert.NotEqual(CompileGenerationCache.GetKey([], ["SYMBOL_A"]), CompileGenerationCache.GetKey([], ["SYMBOL_A"], ["backend=unity-entities"]));
        Assert.NotEqual(CompileGenerationCache.GetKey([], []), CompileGenerationCache.GetKey(sources, []));
        Assert.Equal(CompileGenerationCache.GetKey(sources, ["A", "B"]), CompileGenerationCache.GetKey(sources, ["A", "B"]));
    }

    private static CompileArtifact CompileArtifact(string relativePath, string content) => new(relativePath, content);

    private sealed class CacheDirectory : IDisposable
    {
        public CacheDirectory(params CompileArtifact[] artifacts)
        {
            Directory = Path.Combine(Path.GetTempPath(), "tl-compile-cache-tests", Guid.NewGuid().ToString("N"));
            Key = CompileGenerationCache.GetKey([], ["CacheDirectoryProbe"], ["backend=csharp"]);
            CompileGenerationCache.Synchronize(Directory, Key, artifacts, ReportBody, null);
        }

        public string Directory { get; }

        public string Key { get; }

        public CompileGenerationManifest Manifest()
            => CompileGenerationCache.Load(Directory) ?? throw new InvalidOperationException("cache manifest missing");

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, true);
        }
    }
}
