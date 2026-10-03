using System.Reflection;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public class SlotViewAbiTests
{
    [Fact]
    public void BakedViewsCarryAbiVersionTwoAndNoStoredRecords()
    {
        using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<SnapshotTrack, SnapshotClip>(new SnapshotTrack(1))
            .Clip(0, 0u, 4u, new SnapshotClip(1f, 0f))
            .Bake()));
        var view = Timeline<SnapshotTrack, SnapshotClip>.View(asset);
        Assert.Equal(SlotView.AbiVersionV2, view.AbiVersion);
        Assert.Equal(2, view.AbiVersion);
    }

    [Fact]
    public void TheV1RecordSurfaceIsGoneSoV1ConsumersFailToCompile()
    {
        Assert.Null(typeof(SlotView).GetField("ForwardRecords", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(SlotView).GetField("BackwardRecords", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(SlotView).GetField("RecordBytes", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(SlotView).GetField(nameof(SlotView.LaneKeys), BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal((ushort)2, SlotView.AbiVersionV2);
    }
}
