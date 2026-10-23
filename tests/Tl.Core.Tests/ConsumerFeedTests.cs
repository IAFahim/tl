using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct FeedHandleClip(float Amount);
public readonly record struct FeedHandleTrack(int Pattern) : IBlend<FeedHandleClip>
{
    public void Blend(in FeedHandleClip a, in FeedHandleClip b, float factor, out FeedHandleClip result) => result = a;
}

public readonly record struct FeedForeignClip(float Amount);
public readonly record struct FeedForeignTrack(int Pattern) : IBlend<FeedForeignClip>
{
    public void Blend(in FeedForeignClip a, in FeedForeignClip b, float factor, out FeedForeignClip result) => result = a;
}

internal static unsafe class FeedHandleConsumers
{
    internal static int AlphaHandle;
    internal static float Observed;

    [ModuleInitializer]
    internal static void Install()
    {
        AlphaHandle = PairRuntime<FeedHandleTrack, FeedHandleClip>.Consume(&FoldAlpha, &KeysAlpha, "tl\0FoldAlpha");
        _ = PairRuntime<FeedHandleTrack, FeedHandleClip>.Consume(&FoldBeta, &KeysBeta, "tl\0FoldBeta");
        PairRuntime<FeedHandleTrack, FeedHandleClip>.ConsumeDispatch(&Row, &RowKeys, &Diag, "tl\0Row", AlphaHandle);
    }

    private static int KeysAlpha(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x10; }
        return 1;
    }

    private static int KeysBeta(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<long>.Value; meta[0] = 8 | 0x10; }
        return 1;
    }

    private static void FoldAlpha(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = 100 + tick;

    private static void FoldBeta(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((long*)columns[0])[row] = 7;

    private static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x40; }
        return 1;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<FeedHandleTrack, FeedHandleClip> requires its named fold feed.");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => Observed = ((float*)columns[0])[row];
}

internal static unsafe class FeedForeignConsumers
{
    [ModuleInitializer]
    internal static void Install() => PairRuntime<FeedForeignTrack, FeedForeignClip>.Consume(&Fold, &Keys, "tl\0Fold");

    private static int Keys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x10; }
        return 1;
    }

    private static void Fold(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = tick;

    internal static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
    }

    internal static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x40; }
        return 1;
    }

    internal static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<FeedForeignTrack, FeedForeignClip> requires its named fold feed.");
}

public sealed unsafe class ConsumerFeedTests
{
    static TimelineAsset Asset(int pattern, ushort duration)
        => TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<FeedHandleTrack, FeedHandleClip>(new FeedHandleTrack(pattern))
            .Clip(0, 0, duration, new FeedHandleClip(1f))
            .Bake()));

    [Fact]
    public void DispatchFeedsTheFoldTheRegistrationNamesNotTheLastInstalled()
    {
        using var asset = Asset(0, 4);
        var ids = new ushort[] { asset.Index };
        var clocks = new ushort[] { 2 };
        var columns = new ColumnSet();
        Timeline<FeedHandleTrack, FeedHandleClip>.Apply(ids, clocks, true, in columns);
        Assert.Equal(102f, FeedHandleConsumers.Observed);
    }

    [Fact]
    public void ViewCarriesTheFrozenLaneMapOfItsFold()
    {
        using var asset = Asset(0, 4);
        var view = Timeline<FeedHandleTrack, FeedHandleClip>.View(asset.Index);
        Assert.Equal(0u, view.Absent);
        var map = (int*)((byte*)view.LaneKeys + (nuint)view.ResultCount * 8);
        Assert.Equal(2, map[0]);
        Assert.Equal(0, map[1]);
        Assert.Equal(1, map[2]);
    }

    [Fact]
    public void LiveFeedStaysCorrectAfterAnotherFoldOfTheSamePair()
    {
        using var first = Asset(0, 4);
        using var second = Asset(1, 8);
        var columns = new ColumnSet();
        var one = new ushort[] { first.Index };
        var two = new ushort[] { second.Index };
        Timeline<FeedHandleTrack, FeedHandleClip>.Apply(one, new ushort[] { 2 }, true, in columns);
        Assert.Equal(102f, FeedHandleConsumers.Observed);
        Timeline<FeedHandleTrack, FeedHandleClip>.Apply(two, new ushort[] { 5 }, true, in columns);
        Assert.Equal(105f, FeedHandleConsumers.Observed);
        Timeline<FeedHandleTrack, FeedHandleClip>.Apply(one, new ushort[] { 3 }, true, in columns);
        Assert.Equal(103f, FeedHandleConsumers.Observed);
    }

    [Fact]
    public void ForeignFeedHandleIsRejectedAtInstall()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PairRuntime<FeedForeignTrack, FeedForeignClip>.ConsumeDispatch(
                &FeedForeignConsumers.Row, &FeedForeignConsumers.RowKeys, &FeedForeignConsumers.Diag, "tl\0ForeignRow", FeedHandleConsumers.AlphaHandle));
    }

    [Fact]
    public void FutureFeedHandleIsRejectedAtInstall()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PairRuntime<FeedForeignTrack, FeedForeignClip>.ConsumeDispatch(
                &FeedForeignConsumers.Row, &FeedForeignConsumers.RowKeys, &FeedForeignConsumers.Diag, "tl\0FutureRow", 99));
    }
}
