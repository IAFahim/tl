using System.Reflection;
using System.Security.Cryptography;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbLayoutFixtureTests
{
    [Theory]
    [InlineData("minimal", "5835fb500684924b15e4c3eed022745426560558172e6e48c93cd54cfb3c0900")]
    [InlineData("blended", "e33a22a7d847691a022a8380d73f7ccbd8bcbb9aa059a8abfef966af24176b02")]
    public void Fixture_HasPinnedContentHash(string name, string sha256)
    {
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FixturePath(name + ".tlb")))).ToLowerInvariant());
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("blended")]
    public void Fixture_LoadsThroughTheRuntime_AndInspectsToTheCommittedRendering(string name)
    {
        var bytes = File.ReadAllBytes(FixturePath(name + ".tlb"));
        TimelineAsset.Load(bytes);
        Assert.Equal(File.ReadAllText(FixturePath(name + ".inspect.json")), TlbInspection.Inspect(bytes));
    }

    [Fact]
    public void Fixture_InspectRendering_StableAcrossLoads()
    {
        var bytes = File.ReadAllBytes(FixturePath("blended.tlb"));
        Assert.Equal(TlbInspection.Inspect(bytes), TlbInspection.Inspect(bytes));
    }

    static string FixturePath(string file) => Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "golden", "fixtures", file);
}
