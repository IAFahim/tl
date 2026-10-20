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
    public void IndexStaysLiveUntilEveryViewAcquisitionReleases()
    {
        var bytes = Bytes(4f, 6);
        var index = TimelineAsset.Load(bytes);
        Assert.Equal(index, TimelineAsset.Load(bytes));
        using var survivor = TimelineAsset.Of(index);
        Assert.NotEqual(0, survivor.Reference.Address);
        TimelineAsset.Of(index).Dispose();
        Assert.NotEqual(0, survivor.Reference.Address);
        survivor.Dispose();
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(index));
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
    public void ReviveOfValidatedContentAvoidsPerStepDelegateGarbage()
    {
        var baker = new DomainBaker().Track<HandleTrack, HandleClip>(new HandleTrack(2f));
        for (var i = 0; i < 1_200; i++)
            baker.Clip(0, (uint)i, (uint)i + 1, new HandleClip(1f));
        var bytes = baker.Bake();
        TimelineAsset.Of(TimelineAsset.Load(bytes)).Dispose();
        var before = GC.GetAllocatedBytesForCurrentThread();
        using var revived = TimelineAsset.Of(TimelineAsset.Load(bytes));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 32 * 1024);
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
        TimelineTable.Release(index, generation);
        var before = TimelineTable.OverReleases;
        TimelineTable.Release(index, generation);
        Assert.Equal(before + 1, TimelineTable.OverReleases);
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(index));
    }

    [Fact]
    public void FirstPublishedIndexIsNeverZero()
    {
        Assert.NotEqual(0, TimelineAsset.Load(Bytes(10f, 4)));
    }

    [Fact]
    public void LoadRejectsInvalidBytesBeforeInterning()
    {
        var before = TimelineTable.AllocCount;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(Bytes(1f, 8)[..^1]));
        Assert.Equal(before, TimelineTable.AllocCount);
    }
}
