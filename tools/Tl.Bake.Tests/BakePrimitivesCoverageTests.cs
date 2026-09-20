using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class BakePrimitivesCoverageTests
{
    private static readonly BakerAssemblyResolver Resolver =
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    private const string AlphaDoc =
        """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":4}}]}]}""";

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Theory]
    [InlineData(" ", 0, 1)]
    [InlineData("-", 0, 1)]
    [InlineData("-9223372036854775809", 0, 20)]
    [InlineData("9223372036854775808", 0, 19)]
    [InlineData("abc", 0, 3)]
    [InlineData("0x", 0, 2)]
    [InlineData("-0x", 0, 3)]
    public void ScanSignedInteger_RejectsOutOfRangeAndMalformedTokens(string text, int start, int bound)
    {
        Assert.False(FastNumber.ScanSignedInteger(
            Utf8(text), start, bound, allowNegative: true, long.MinValue, long.MaxValue, out _, out _));
    }

    [Theory]
    [InlineData("-9223372036854775808", -9223372036854775808, 20)]
    [InlineData("-0", 0, 2)]
    [InlineData("0", 0, 1)]
    [InlineData(" -7 ", -7, 3)]
    public void ScanSignedInteger_AcceptsBoundaryValues(string text, long expected, int expectedEnd)
    {
        Assert.True(FastNumber.ScanSignedInteger(
            Utf8(text), 0, text.Length, allowNegative: true, long.MinValue, long.MaxValue, out var end, out var value));
        Assert.Equal(expected, value);
        Assert.Equal(expectedEnd, end);
    }

    [Fact]
    public void ScanSignedInteger_RespectsTheSignedDomain()
    {
        Assert.True(FastNumber.ScanSignedInteger(
            Utf8("127"), 0, 3, allowNegative: false, sbyte.MinValue, sbyte.MaxValue, out _, out var inRange));
        Assert.Equal(127, inRange);

        Assert.False(FastNumber.ScanSignedInteger(
            Utf8("-1"), 0, 2, allowNegative: false, long.MinValue, long.MaxValue, out _, out _));
    }

    [Fact]
    public void ScanUnsigned64_RejectsSignsAndOverflow()
    {
        Assert.False(FastNumber.ScanUnsigned64(Utf8(""), 0, 0, out _, out _));
        Assert.False(FastNumber.ScanUnsigned64(Utf8(" \t "), 0, 3, out _, out _));
        Assert.False(FastNumber.ScanUnsigned64(Utf8("-1"), 0, 2, out _, out _));
        Assert.False(FastNumber.ScanUnsigned64(Utf8("18446744073709551616"), 0, 20, out _, out _));

        Assert.True(FastNumber.ScanUnsigned64(Utf8("0"), 0, 1, out var zeroEnd, out var zero));
        Assert.Equal(0UL, zero);
        Assert.Equal(1, zeroEnd);

        Assert.True(FastNumber.ScanUnsigned64(Utf8(" 18446744073709551615 "), 0, 22, out _, out var max));
        Assert.Equal(18446744073709551615UL, max);
    }

    [Fact]
    public void HotLength_RejectsAnOutofRangeHotLength()
    {
        var header = new byte[64];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0), 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), 3u);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(48), (uint)header.Length);
        Assert.Throws<ArgumentException>(() => TlbMetadata.HotLength(header));

        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(44), (uint)header.Length + 1);
        Assert.Throws<ArgumentException>(() => TlbMetadata.HotLength(header));
    }

    [Fact]
    public void PartsToDouble_RejectsHardAndOutOfRangeParts()
    {
        Assert.True(double.IsNaN(FastNumber.PartsToDouble(1, 2, negative: false, hard: true)));
        Assert.True(double.IsNaN(FastNumber.PartsToDouble(1, 23, negative: false, hard: false)));
        Assert.True(double.IsNaN(FastNumber.PartsToDouble(1, -23, negative: false, hard: false)));
        Assert.True(double.IsNaN(FastNumber.PartsToDouble(1UL << 53, 0, negative: false, hard: false)));
        Assert.Equal(-0.5d, FastNumber.PartsToDouble(5, -1, negative: true, hard: false));
    }

    [Fact]
    public void Prefix_RequiresAFullDigest()
    {
        Assert.Throws<ArgumentException>(() => BakeCacheKey.Prefix(new byte[3]));
        var prefix = BakeCacheKey.Prefix(SHA256.HashData(Utf8("payload")));
        Assert.Equal(12, prefix.Length);
        Assert.Equal(prefix, prefix.ToLowerInvariant());
    }

    [Fact]
    public void WorkspacePool_ReturnsTheLargestPoolPerKey()
    {
        var workspace = new BakeWorkspace();
        Assert.Null(workspace.RentPool(7));

        workspace.ReturnPool(7, new byte[16]);
        workspace.ReturnPool(7, new byte[32]);
        Assert.Equal(32, workspace.RentPool(7)!.Length);
        Assert.Null(workspace.RentPool(7));

        workspace.ReturnPool(8, new byte[8]);
        Assert.Equal(8, workspace.RentPool(8)!.Length);
        Assert.Null(workspace.RentPool(9));
    }

    [Fact]
    public void HotLength_ValidatesTheAssetHeader()
    {
        var asset = TimelineBaker.BakeJson(AlphaDoc, Resolver);
        Assert.True(TlbMetadata.HotLength(asset) > 0 && TlbMetadata.HotLength(asset) <= asset.Length);

        Assert.Throws<ArgumentException>(() => TlbMetadata.HotLength(new byte[10]));
        Assert.Throws<ArgumentException>(() => TlbMetadata.HotLength(new byte[64]));
    }

    [Fact]
    public void MetadataViewParse_ValidatesTheTailHeader()
    {
        Assert.Throws<ArgumentException>(() => TlbMetadataView.Parse(new byte[47]));

        var badMagic = new byte[48];
        Assert.Throws<ArgumentException>(() => TlbMetadataView.Parse(badMagic));

        var outOfBounds = new byte[48];
        BinaryPrimitives.WriteUInt32LittleEndian(outOfBounds.AsSpan(0), TlbMetadata.MetadataMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(outOfBounds.AsSpan(4), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(outOfBounds.AsSpan(8), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(outOfBounds.AsSpan(12), uint.MaxValue / 2);
        Assert.Throws<ArgumentException>(() => TlbMetadataView.Parse(outOfBounds));
    }

    [Fact]
    public void Report_ValidatesSectionLayout()
    {
        var asset = TimelineBaker.BakeJson(AlphaDoc, Resolver);

        var badPairOffset = (byte[])asset.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(badPairOffset.AsSpan(28), 0u);
        Assert.Throws<ArgumentException>(() => TlbReport.Report(badPairOffset));

        var badStageTable = (byte[])asset.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(badStageTable.AsSpan(20), uint.MaxValue / 8);
        Assert.Throws<ArgumentException>(() => TlbReport.Report(badStageTable));
    }

    [Fact]
    public void AddAssembly_ExtendsTheResolverSearchScope()
    {
        var resolver = new BakerAssemblyResolver();
        resolver.AddAssembly(typeof(Tlb.AlphaTrack).Assembly);
        resolver.AddAssembly(typeof(Tlb.AlphaTrack).Assembly);

        Assert.Equal(TimelineBaker.BakeJson(AlphaDoc, Resolver), TimelineBaker.BakeJson(AlphaDoc, resolver));
    }

    [Fact]
    public void Resolver_ConstructorAndAddAssembly_AbsorbANotYetLoadedAssembly()
    {
        var numbersDll = FindRepositoryFile(Path.Combine("benchmarks", "Numbers", "bin", "Release", "net10.0", "Numbers.dll"));
        if (numbersDll is null)
            return;

        var fromPaths = new BakerAssemblyResolver([numbersDll]);
        var assembly = System.Reflection.Assembly.LoadFrom(numbersDll);
        Assert.Contains(assembly, fromPaths.ReferencedAssemblies);
        var laneTrack = assembly.GetType("NumbersBench.LaneTrack");
        Assert.NotNull(laneTrack);
        Assert.Equal(laneTrack, fromPaths.ResolveType("NumbersBench", "LaneTrack", "Numbers", "track 0"));

        var fresh = new BakerAssemblyResolver();
        fresh.AddAssembly(assembly);
        Assert.Equal(laneTrack, fresh.ResolveType("NumbersBench", "LaneTrack", null, "track 0"));
    }

    private static string? FindRepositoryFile(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "tl.slnx")))
            directory = directory.Parent;
        if (directory is null)
            return null;
        var candidate = Path.Combine(directory.FullName, relative);
        return File.Exists(candidate) ? candidate : null;
    }

    [Fact]
    public void BatchBake_RejectsInvalidUtf8WithLocatedOffset()
    {
        var invalid = new byte[] { 0x7B, 0xFF, 0x7D };
        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJsonBatch([invalid]));
        Assert.Contains("invalid UTF-8", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("byte 1", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BatchBake_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(TimelineBaker.BakeJsonBatch([], Resolver));
    }
}
