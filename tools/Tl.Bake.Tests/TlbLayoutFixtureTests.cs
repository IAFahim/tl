using System.Reflection;
using System.Security.Cryptography;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbLayoutFixtureTests
{
    [Theory]
    [InlineData("minimal", "679a442cfe1640cffdd8597bf67ca6d00b46f9edd716d821a1205977d848b436")]
    [InlineData("blended", "b463b5e6fc222c9518ac4e94216ded8d12c3aa14059fdcc49335d43e682e97d9")]
    public void Fixture_HasPinnedContentHash(string name, string sha256)
    {
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FixturePath(name + ".tlb")))).ToLowerInvariant());
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("blended")]
    public void Fixture_InspectsToTheCommittedRendering(string name)
    {
        var bytes = File.ReadAllBytes(FixturePath(name + ".tlb"));
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
