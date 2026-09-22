using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct RegistryClip(float Value);

public readonly record struct RegistryTrack(float Scale) : IBlend<RegistryClip>
{
    public void Blend(in RegistryClip first, in RegistryClip second, float factor, out RegistryClip result)
        => result = first;
}

public unsafe struct RegistryFatClip
{
    public fixed float Values[32768];
}

public readonly record struct RegistryFatTrack(float Scale) : IBlend<RegistryFatClip>
{
    public void Blend(in RegistryFatClip first, in RegistryFatClip second, float factor, out RegistryFatClip result)
        => result = first;
}

public unsafe class InternRegistryTests
{
    static byte[] Bake(float scale, uint end) => new DomainBaker()
        .Track<RegistryTrack, RegistryClip>(new RegistryTrack(scale))
        .Clip(0, 0, end, new RegistryClip(1f))
        .Bake();

    [Fact]
    public void PublishAcquireAndDisposeTrackEveryCounter()
    {
        TimelineTable.Drain();
        var bytes = Bake(31f, 8);
        var padded = (bytes.Length + 63) & ~63;
        var liveBefore = TimelineTable.LiveEntries;
        var distinctBefore = TimelineTable.DistinctContents;
        var publishesBefore = TimelineTable.Publishes;
        var allocBytesBefore = TimelineTable.AllocBytes;
        var deathsBefore = TimelineTable.Deaths;
        var releasesBefore = TimelineTable.Releases;
        var graveyardBlocksBefore = TimelineTable.GraveyardBlocks;
        var graveyardBytesBefore = TimelineTable.GraveyardBytes;
        var freeBytesBefore = TimelineTable.FreeBytes;
        var peakBefore = TimelineTable.PeakLiveBytes;

        var index = TimelineAsset.Load(bytes);
        Assert.Equal(liveBefore + 1, TimelineTable.LiveEntries);
        Assert.Equal(distinctBefore + 1, TimelineTable.DistinctContents);
        Assert.Equal(publishesBefore + 1, TimelineTable.Publishes);
        Assert.Equal(allocBytesBefore + padded, TimelineTable.AllocBytes);
        var liveBytes = TimelineTable.AllocBytes - TimelineTable.FreeBytes - TimelineTable.GraveyardBytes;
        Assert.True(TimelineTable.PeakLiveBytes >= liveBytes);
        Assert.True(TimelineTable.PeakLiveBytes >= peakBefore);

        using (TimelineAsset.Of(index))
        {
            Assert.True(TimelineTable.IsLive(index));
        }
        Assert.Equal(liveBefore, TimelineTable.LiveEntries);
        Assert.Equal(deathsBefore + 1, TimelineTable.Deaths);
        Assert.Equal(releasesBefore + 1, TimelineTable.Releases);
        Assert.Equal(graveyardBlocksBefore + 1, TimelineTable.GraveyardBlocks);
        Assert.Equal(graveyardBytesBefore + bytes.Length, TimelineTable.GraveyardBytes);

        TimelineTable.Drain();
        Assert.Equal(freeBytesBefore + bytes.Length, TimelineTable.FreeBytes);
        Assert.Equal(0, TimelineTable.GraveyardBlocks);
        Assert.Equal(0, TimelineTable.GraveyardBytes);
    }

    [Fact]
    public void ReloadAfterDisposeRevivesAndCountsTheRevival()
    {
        var bytes = Bake(32f, 7);
        var revivalsBefore = TimelineTable.Revivals;
        var publishesBefore = TimelineTable.Publishes;

        var first = TimelineAsset.Load(bytes);
        TimelineAsset.Of(first).Dispose();
        var second = TimelineAsset.Load(bytes);

        Assert.Equal(first, second);
        Assert.Equal(revivalsBefore + 1, TimelineTable.Revivals);
        Assert.Equal(publishesBefore + 2, TimelineTable.Publishes);
        Assert.True(TimelineTable.IsLive(first));

        TimelineAsset.Of(second).Dispose();
        TimelineTable.Drain();
    }

    [Fact]
    public void OverReleaseOfAnUnpublishedIndexIsRejected()
    {
        var before = TimelineTable.OverReleases;
        TimelineTable.Release(ushort.MaxValue, 0);
        Assert.Equal(before + 1, TimelineTable.OverReleases);
    }

    static byte[] FatBake()
    {
        var baker = new DomainBaker()
            .Track<RegistryFatTrack, RegistryFatClip>(new RegistryFatTrack(1f));
        for (uint i = 0; i < 384; i++)
        {
            var clip = default(RegistryFatClip);
            clip.Values[0] = i;
            baker.Clip(0, i, i + 1, clip);
        }
        return baker.Bake();
    }

    [Fact]
    public void GateWaitersYieldWhenAPublishHoldsTheTable()
    {
        var bytes = FatBake();
        var failures = 0;
        using var stop = new CancellationTokenSource();
        var threads = new Thread[8];
        for (var i = 0; i < threads.Length; i++)
            threads[i] = new Thread(() =>
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                        _ = TimelineTable.IsLive(0);
                }
                catch
                {
                    Interlocked.Increment(ref failures);
                }
            })
            { IsBackground = true };
        foreach (var thread in threads)
            thread.Start();
        try
        {
            var poolOffset = BitConverter.ToUInt32(bytes, 36);
            for (var i = 0; i < 4; i++)
            {
                bytes[poolOffset + 64] = (byte)i;
                var index = TimelineAsset.Load(bytes);
                Assert.True(TimelineTable.IsLive(index));
                TimelineAsset.Of(index).Dispose();
            }
        }
        finally
        {
            stop.Cancel();
            foreach (var thread in threads)
                thread.Join();
        }
        Assert.Equal(0, failures);
        TimelineTable.Drain();
    }
}
