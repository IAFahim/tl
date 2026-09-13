using System;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class CacheKeyTests
{
    private static readonly byte[] Json = [1, 2, 3, 4];
    private static readonly byte[] AssemblyA = [10, 11];
    private static readonly byte[] AssemblyB = [20, 21, 22];

    [Fact]
    public void SameInputs_ProduceIdenticalKeys()
    {
        var first = BakeCacheKey.Compute(Json, [AssemblyA, AssemblyB], kernel: true, strip: false);
        var second = BakeCacheKey.Compute(Json, [AssemblyA, AssemblyB], kernel: true, strip: false);
        Assert.Equal(first, second);
    }

    [Fact]
    public void FieldBoundaries_AreLengthPrefixed_SoContentCannotBlurAcrossFields()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute([1, 2], [[3]], kernel: false, strip: false),
            BakeCacheKey.Compute([1], [[2, 3]], kernel: false, strip: false));
    }

    [Fact]
    public void AssemblyOrder_IsSignificant()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute(Json, [AssemblyA, AssemblyB], kernel: false, strip: false),
            BakeCacheKey.Compute(Json, [AssemblyB, AssemblyA], kernel: false, strip: false));
    }

    [Fact]
    public void AssemblyContent_IsSignificant()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute(Json, [AssemblyA], kernel: false, strip: false),
            BakeCacheKey.Compute(Json, [[10, 11, 99]], kernel: false, strip: false));
    }

    [Fact]
    public void KernelFlag_IsSignificant()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute(Json, [AssemblyA], kernel: true, strip: false),
            BakeCacheKey.Compute(Json, [AssemblyA], kernel: false, strip: false));
    }

    [Fact]
    public void StripFlag_IsSignificant()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute(Json, [AssemblyA], kernel: false, strip: true),
            BakeCacheKey.Compute(Json, [AssemblyA], kernel: false, strip: false));
    }

    [Fact]
    public void ToolVersion_IsSignificant()
    {
        Assert.NotEqual(
            BakeCacheKey.Compute(BakeCacheKey.ToolVersion, Json, [AssemblyA], kernel: false, strip: false),
            BakeCacheKey.Compute(BakeCacheKey.ToolVersion + "-next", Json, [AssemblyA], kernel: false, strip: false));
    }

    [Fact]
    public void KeyIsFullSha256_PrefixIsTwelveLowercaseHex()
    {
        var key = BakeCacheKey.Compute(Json, [AssemblyA], kernel: false, strip: false);
        Assert.Equal(32, key.Length);
        var prefix = BakeCacheKey.Prefix(key);
        Assert.Equal(12, prefix.Length);
        Assert.Equal(Convert.ToHexString(key, 0, 6).ToLowerInvariant(), prefix);
    }
}
