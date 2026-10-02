using System.Buffers.Binary;
using System.Runtime.InteropServices;

using Tl.TestSupport;

using Xunit;

namespace Tl.Core.Tests;

public class AssetGuardTests
{
    [Fact]
    public void ColumnLengthFailureReportsBothCounts()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Fail.ColumnLength(4, 3)).Message);

    [Fact]
    public void ColumnOverlapFailureNamesTheRule()
        => Assert.Equal("Lane columns must not overlap.", Assert.Throws<ArgumentException>(Fail.ColumnOverlap).Message);

    [Fact]
    public void DisposedFailureNamesTheSet()
        => Assert.Equal("TimelineSet", Assert.Throws<ObjectDisposedException>(Fail.Disposed).ObjectName);

    [Fact]
    public void ChunkApplyRejectsIdLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Timeline<SkewTrack, SkewClip>.ApplyChunk(new ushort[3], new ushort[4], true, new float[4], new float[4])).Message);

    [Fact]
    public void LoaderRejectsAssetsDeclaringMoreThan256Pairs()
    {
        var baked = new DomainBaker { FingerprintOf = BakeFingerprint.Of }
            .Track<SkewTrack, SkewClip>(new SkewTrack(2))
            .Clip(0, 0, 4, new SkewClip(5))
            .Bake();
        BinaryPrimitives.WriteUInt32LittleEndian(baked.AsSpan(24), 257u);

        var failure = Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(baked));

        Assert.Contains("more than 256 pairs", failure.Message, StringComparison.Ordinal);
    }

#if TL_CHECKED
    [Fact]
    public void LiveGuardAcceptsALiveAsset() => Checked.Live(false);

    [Fact]
    public void LiveGuardRejectsADisposedAsset()
        => Assert.Equal("TimelineSet", Assert.Throws<ObjectDisposedException>(() => Checked.Live(true)).ObjectName);

    [Fact]
    public void PairColumnGuardRejectsEffectLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Columns(new ushort[4], new float[3])).Message);

    [Fact]
    public void PairColumnGuardRejectsOverlappingColumns()
    {
        var buffer = new ushort[10];
        var threw = false;
        try { Checked.Columns(buffer.AsSpan(0, 4), MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8))); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void PairColumnGuardAcceptsDistinctColumns()
        => Checked.Columns(new ushort[4], new float[4]);

    [Fact]
    public void TripleColumnGuardAcceptsDistinctColumns()
        => Checked.Columns(new ushort[4], new ushort[4], new float[4]);

    [Fact]
    public void TripleColumnGuardReturnsEarlyOnEmptyNext()
        => Checked.Columns(new ushort[4], Span<ushort>.Empty, new float[4]);

    [Fact]
    public void TripleColumnGuardSkipsOverlapForMatchingNext()
    {
        var positions = new ushort[4];
        Checked.Columns(positions, positions, new float[4]);
    }

    [Fact]
    public void TripleColumnGuardRejectsNextLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Columns(new ushort[4], new ushort[3], new float[4])).Message);

    [Fact]
    public void TripleColumnGuardRejectsEffectsOverlappingNext()
    {
        var buffer = new ushort[12];
        var next = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(2, 8));
        var threw = false;
        try { Checked.Columns(new ushort[4], next, effects); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void TripleColumnGuardRejectsPositionsOverlappingNext()
    {
        var buffer = new ushort[8];
        var positions = buffer.AsSpan(0, 4);
        var next = buffer.AsSpan(2, 4);
        var threw = false;
        try { Checked.Columns(positions, next, new float[4]); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void QuadColumnGuardAcceptsDistinctColumns()
        => Checked.Columns(new ushort[4], new ushort[4], new ushort[4], new float[4]);

    [Fact]
    public void QuadColumnGuardRejectsIdLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Columns(new ushort[3], new ushort[4], new ushort[4], new float[4])).Message);

    [Fact]
    public void QuadColumnGuardRejectsIdsOverlappingPositions()
    {
        var buffer = new ushort[8];
        var ids = buffer.AsSpan(0, 4);
        var positions = buffer.AsSpan(0, 4);
        var threw = false;
        try { Checked.Columns(ids, positions, new ushort[4], new float[4]); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void QuadColumnGuardRejectsIdsOverlappingEffects()
    {
        var buffer = new ushort[8];
        var ids = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(0, 8));
        var threw = false;
        try { Checked.Columns(ids, new ushort[4], new ushort[4], effects); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void QuadColumnGuardRejectsIdsOverlappingNext()
    {
        var buffer = new ushort[8];
        var ids = buffer.AsSpan(0, 4);
        var next = buffer.AsSpan(2, 4);
        var threw = false;
        try { Checked.Columns(ids, new ushort[4], next, new float[4]); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void WidthColumnGuardAcceptsMatchingColumns()
        => Checked.Columns(new ushort[4], new ushort[4], new ushort[4]);

    [Fact]
    public void WidthColumnGuardRejectsIdLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Columns(new ushort[3], new ushort[4], new ushort[4])).Message);

    [Fact]
    public void WidthColumnGuardRejectsNextLengthMismatch()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Columns(new ushort[4], new ushort[4], new ushort[3])).Message);

    [Fact]
    public void LengthGuardRejectsMismatchedColumns()
        => Assert.Equal(
            "Column length 3 must equal position count 4.",
            Assert.Throws<ArgumentException>(() => Checked.Length(new ushort[4], new ushort[3])).Message);

    [Fact]
    public void LengthGuardAcceptsMatchingColumns()
        => Checked.Length(new ushort[4], new ushort[4]);

    [Fact]
    public void WidthColumnGuardAcceptsExactInPlaceAdvance()
    {
        var buffer = new ushort[4];
        Checked.Columns(new ushort[4], buffer, buffer);
    }

    [Fact]
    public void WidthColumnGuardRejectsPartiallyOverlappingAdvance()
    {
        var buffer = new ushort[8];
        var positions = buffer.AsSpan(0, 4);
        var next = buffer.AsSpan(2, 4);
        var threw = false;
        try { Checked.Columns(new ushort[4], positions, next); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void WidthColumnGuardRejectsIdsOverlappingNext()
    {
        var buffer = new ushort[8];
        var ids = buffer.AsSpan(0, 4);
        var next = buffer.AsSpan(2, 4);
        var threw = false;
        try { Checked.Columns(ids, new ushort[4], next); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }
#endif
}
