using System.Reflection;
using System.Security.Cryptography;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbLayoutFixtureTests
{
    [Theory]
    [InlineData("minimal", "d223843e1a457b388f11c7965af8c05f6bedbd1de72219b202ef929fbc9a6e8f")]
    [InlineData("blended", "a3b735b1a682e0acb6780cf22f687abd5347bad16e1ad27c8d4f0bdbed038855")]
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
