using System.Threading;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct FeedLinkClip(float Height);
public readonly record struct FeedLinkTrack(float Scale) : IBlend<FeedLinkClip>
{
    public void Blend(in FeedLinkClip first, in FeedLinkClip second, float factor, out FeedLinkClip result)
        => result = new FeedLinkClip(first.Height + (second.Height - first.Height) * factor);
}

public unsafe class PairTableFeedLinkTests
{
    static void FoldA(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row) { }
    static void FoldB(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row) { }
    static void FoldRange(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int rowStart, int rowCount) { }
    static void ExecuteA(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row) { }
    static void ExecuteFresh(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row) { }

    static int FoldKeysA(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 0x10;
        }
        return 1;
    }

    static int FoldKeysB(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 0x10;
        }
        return 1;
    }

    static int LiveKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 0x40;
        }
        return 1;
    }

    static void Diag(ulong key, long slot) { }

    static int EntryOf(delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> execute)
    {
        var target = (nint)execute;
        var consumers = PairTable.ConsumerAt;
        for (var i = PairTable.ConsumerCount - 1; i >= 0; i--)
            if ((nint)consumers[i].Execute == target) return i;
        return -1;
    }

    [Fact]
    public void ExecuteActiveLinksToTheLastFoldInstalledByItsOwnThreadNotThePairHead()
    {
        var readyA = new ManualResetEventSlim(false);
        var releasedA = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            PairRuntime<FeedLinkTrack, FeedLinkClip>.Consume(&FoldA, &FoldRange, &FoldKeysA);
            readyA.Set();
            releasedA.Wait();
            PairRuntime<FeedLinkTrack, FeedLinkClip>.ConsumeDispatch(&ExecuteA, &LiveKeys, &Diag);
        });
        thread.Start();
        readyA.Wait();
        PairRuntime<FeedLinkTrack, FeedLinkClip>.Consume(&FoldB, &FoldRange, &FoldKeysB);
        releasedA.Set();
        thread.Join();

        var consumers = PairTable.ConsumerAt;
        var execute = EntryOf(&ExecuteA);
        Assert.True(execute >= 0, "ExecuteActive entry was not installed.");
        var feed = consumers[execute].Feed;
        Assert.True(feed >= 0, "ExecuteActive stayed unlinked despite a same-thread Fold.");
        Assert.True(feed < PairTable.ConsumerCount, "Feed points past the consumer table.");
        var expected = (nint)(delegate*<ulong*, byte*, int>)&FoldKeysA;
        Assert.True((nint)consumers[feed].Keys == expected, "ExecuteActive linked the pair head instead of its own thread's Fold.");
    }

    [Fact]
    public void ExecuteActiveInstalledWithoutAFoldOnItsThreadStaysUnlinked()
    {
        var done = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            PairRuntime<FeedLinkTrack, FeedLinkClip>.ConsumeDispatch(&ExecuteFresh, &LiveKeys, &Diag);
            done.Set();
        });
        thread.Start();
        done.Wait();
        thread.Join();

        var execute = EntryOf(&ExecuteFresh);
        Assert.True(execute >= 0, "fresh ExecuteActive entry was not installed.");
        Assert.Equal(-1, PairTable.ConsumerAt[execute].Feed);
    }
}
