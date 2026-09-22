using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class TlbCliCoverageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tlb_cli_cov_" + Guid.NewGuid().ToString("N"));

    private static readonly string FixtureAssembly = typeof(Tlb.AlphaTrack).Assembly.Location;

    private const string AlphaDoc =
        """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":4}}]}]}""";

    public TlbCliCoverageTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private string WriteInput(string name = "alpha.json")
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, AlphaDoc);
        return path;
    }

    [Fact]
    public void BakeArgumentErrors_ExitNonZero()
    {
        WriteInput();
        var input = Path.Combine(_root, "alpha.json");
        Assert.NotEqual(0, Program.Main([input, "--assembly"]));
        Assert.NotEqual(0, Program.Main([input, "--cache"]));
        Assert.NotEqual(0, Program.Main([input, Path.Combine(_root, "out.tlb"), Path.Combine(_root, "extra.json")]));
    }

    [Fact]
    public void MissingInputFile_ExitNonZero()
    {
        Assert.NotEqual(0, Program.Main([Path.Combine(_root, "missing.json")]));
    }

    [Fact]
    public void MissingAssemblyFile_ExitNonZero()
    {
        var input = WriteInput();
        Assert.NotEqual(0, Program.Main([input, Path.Combine(_root, "out.tlb"), "--assembly", Path.Combine(_root, "nope.dll")]));
    }

    [Fact]
    public void BakeWithCache_MissThenHit_RestoresTheAsset()
    {
        var input = WriteInput();
        var output = Path.Combine(_root, "out", "alpha.tlb");
        var cache = Path.Combine(_root, "cache");

        Assert.Equal(0, Program.Main([input, output, "--assembly", FixtureAssembly, "--cache", cache]));
        var baked = File.ReadAllBytes(output);
        Assert.Single(Directory.EnumerateFiles(cache, "*.tlb"));

        File.Delete(output);
        Assert.Equal(0, Program.Main([input, output, "--assembly", FixtureAssembly, "--cache", cache]));
        Assert.Equal(baked, File.ReadAllBytes(output));

        Assert.Equal(0, Program.Main([input, output, "--assembly", FixtureAssembly, "--cache", cache]));
        Assert.Equal(baked, File.ReadAllBytes(output));
    }

    [Fact]
    public void JsonMode_MissingAssemblyArgument_ExitNonZero()
    {
        Assert.NotEqual(0, Program.Main(["--json", "--assembly"]));
    }

    [Fact]
    public void Strip_LazyForms_AndFullStrip()
    {
        Assert.NotEqual(0, Program.Main(["--strip"]));
        Assert.NotEqual(0, Program.Main(["--strip", Path.Combine(_root, "missing.tlb"), Path.Combine(_root, "out.tlb")]));

        var asset = TimelineBaker.BakeJson(AlphaDoc, Resolver());
        var input = Path.Combine(_root, "asset.tlb");
        var output = Path.Combine(_root, "stripped.tlb");
        File.WriteAllBytes(input, asset);

        Assert.Equal(0, Program.Main(["--strip", input, output]));
        Assert.Equal(TlbMetadata.Strip(asset), File.ReadAllBytes(output));
    }

    [Fact]
    public void Report_LazyForms_AndFullReport()
    {
        Assert.NotEqual(0, Program.Main(["--report"]));
        Assert.NotEqual(0, Program.Main(["--report", Path.Combine(_root, "missing.tlb")]));

        var asset = TimelineBaker.BakeJson(AlphaDoc, Resolver());
        var input = Path.Combine(_root, "asset.tlb");
        File.WriteAllBytes(input, asset);

        Assert.Equal(0, Program.Main(["--report", input]));
    }

    private static BakerAssemblyResolver Resolver() =>
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    [Fact]
    public void FindAssembly_WithoutAuthorablePairs_ReportsTheRepair()
    {
        var trackless = Path.Combine(_root, "trackless.json");
        File.WriteAllText(trackless, """{"duration":10,"tracks":[]}""");

        Assert.Null(Lazy.FindAssembly(trackless));
    }

    [Fact]
    public void CacheLoad_WithCorruptDatabase_StartsEmpty()
    {
        File.WriteAllText(Path.Combine(_root, "tlb.db"), "{not json");
        var cache = Lazy.Cache.Load(_root);
        Assert.False(cache.TryGet(Path.Combine(_root, "x.json"), "fingerprint", out var assembly));
        Assert.Equal(string.Empty, assembly);
    }

    [Fact]
    public void CacheLoad_WithoutDatabase_StartsEmpty()
    {
        Assert.False(Lazy.Cache.Load(_root).TryGet("x", "y", out _));
    }

    [Fact]
    public void DefinesAll_NonAssemblyFile_ReturnsFalse()
    {
        var fake = Path.Combine(_root, "fake.dll");
        File.WriteAllText(fake, "this is not an assembly");
        Assert.False(Lazy.DefinesAll(
            fake, [new ValueTuple<string?, string>("Tlb", "AlphaTrack")]));
    }

    [Fact]
    public void DefinesAll_MissingFile_ReturnsFalse()
    {
        Assert.False(Lazy.DefinesAll(
            Path.Combine(_root, "gone.dll"), [new ValueTuple<string?, string>("Tlb", "AlphaTrack")]));
    }

    [Fact]
    public void DefinesAll_IsolatedAssemblyCopy_ToleratesPartialMetadata()
    {
        var isolated = Path.Combine(_root, "isolated");
        Directory.CreateDirectory(isolated);
        File.Copy(FixtureAssembly, Path.Combine(isolated, "Tl.Bake.Tests.dll"));

        Assert.False(Lazy.DefinesAll(
            Path.Combine(isolated, "Tl.Bake.Tests.dll"),
            [new ValueTuple<string?, string>("No.Such.Namespace", "GhostType")]));
    }
}
