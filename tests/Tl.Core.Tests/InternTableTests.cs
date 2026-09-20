using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public class InternTableTests
{
    static byte[] Bytes(float scale, uint end)
        => new DomainBaker()
            .Track<HandleTrack, HandleClip>(new HandleTrack(scale))
            .Clip(0, 0, end, new HandleClip(1f))
            .Bake();

    [Fact]
    public void SameBytesAcrossLoadsShareOneIndexAndOneBlock()
    {
        var bytes = Bytes(3f, 8);
        var beforeAlloc = TimelineTable.AllocCount;
        var first = TimelineAsset.Load(bytes);
        var second = TimelineAsset.Load(bytes.AsSpan().ToArray());
        Assert.Equal(first, second);
        Assert.Equal(1, TimelineTable.AllocCount - beforeAlloc);
    }

    [Fact]
    public void DistinctBytesGetDistinctIndices()
    {
        var first = TimelineAsset.Load(Bytes(1f, 8));
        var second = TimelineAsset.Load(Bytes(2f, 8));
        var third = TimelineAsset.Load(Bytes(1f, 9));
        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void IndexStaysLiveUntilEveryLoadAcquisitionReleases()
    {
        var bytes = Bytes(4f, 6);
        var first = TimelineAsset.Load(bytes);
        var second = TimelineAsset.Load(bytes);
        Assert.Equal(first, second);
        TimelineAsset.Of(first).Dispose();
        using var survivor = TimelineAsset.Of(second);
        Assert.NotEqual(0, survivor.Reference.Address);
        TimelineAsset.Of(second).Dispose();
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(first));
        Assert.Throws<ArgumentException>(() => survivor.Reference);
    }

    [Fact]
    public void ViewDisposeReleasesExactlyOneAcquisition()
    {
        var index = TimelineAsset.Load(Bytes(5f, 6));
        var view = TimelineAsset.Of(index);
        view.Dispose();
        view.Dispose();
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(index));
    }

    [Fact]
    public void ReloadAfterFullReleaseRevivesTheSameIndex()
    {
        var bytes = Bytes(6f, 7);
        var first = TimelineAsset.Load(bytes);
        TimelineAsset.Of(first).Dispose();
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(first));
        var second = TimelineAsset.Load(bytes);
        Assert.Equal(first, second);
        using var revived = TimelineAsset.Of(second);
        Assert.NotEqual(0, revived.Reference.Address);
    }

    [Fact]
    public void DeadBlocksDrainAndBalanceUnderTheNextLoad()
    {
        var beforeAlloc = TimelineTable.AllocCount;
        var beforeFree = TimelineTable.FreeCount;
        var first = TimelineAsset.Load(Bytes(7f, 5));
        TimelineAsset.Of(first).Dispose();
        Assert.True(TimelineTable.GraveyardBlocks > 0);
        var second = TimelineAsset.Load(Bytes(8f, 5));
        TimelineAsset.Of(second).Dispose();
        TimelineTable.Drain();
        Assert.Equal(TimelineTable.AllocCount - beforeAlloc, TimelineTable.FreeCount - beforeFree);
        Assert.Equal(2, TimelineTable.AllocCount - beforeAlloc);
    }

    [Fact]
    public void OverReleaseIsCountedLoudAndKeepsTheEntryDead()
    {
        var index = TimelineAsset.Load(Bytes(9f, 4));
        TimelineTable.Pin(index, out var generation);
        TimelineAsset.Of(index).Dispose();
        var before = TimelineTable.OverReleases;
        TimelineTable.Release(index, generation);
        Assert.Equal(before + 1, TimelineTable.OverReleases);
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(index));
    }

    [Fact]
    public void LoadRejectsInvalidBytesBeforeInterning()
    {
        var before = TimelineTable.AllocCount;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(Bytes(1f, 8)[..^1]));
        Assert.Equal(before, TimelineTable.AllocCount);
    }
}
