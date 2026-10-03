using System.Reflection;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public class SlotViewAbiTests
{
    [Fact]
    public void BakedViewsCarryAbiVersionThree()
    {
        using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<SnapshotTrack, SnapshotClip>(new SnapshotTrack(1))
            .Clip(0, 0u, 4u, new SnapshotClip(1f, 0f))
            .Bake()));
        var view = Timeline<SnapshotTrack, SnapshotClip>.View(asset);
        Assert.Equal(SlotView.AbiVersionV3, view.AbiVersion);
        Assert.Equal(3, view.AbiVersion);
    }

    [Fact]
    public void TheStoredRecordAndByPositionSurfacesAreGoneSoOlderConsumersFailToCompile()
    {
        Assert.Null(typeof(SlotView).GetField("ForwardRecords", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(SlotView).GetField("BackwardRecords", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(SlotView).GetField("RecordBytes", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(SlotView).GetField("BackwardByPosition", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(SlotView).GetField(nameof(SlotView.Directory), BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(SlotView).GetField(nameof(SlotView.Segments), BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(SlotView).GetField(nameof(SlotView.Dense), BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(SlotView).GetField(nameof(SlotView.LaneKeys), BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal((ushort)3, SlotView.AbiVersionV3);
    }

    [Fact]
    public void LaneSegmentIsPublicForHostsWithTheThreeRunKinds()
    {
        Assert.Equal((byte)0, LaneSegment.Constant);
        Assert.Equal((byte)1, LaneSegment.Ramp);
        Assert.Equal((byte)2, LaneSegment.Dense);
        Assert.True(typeof(LaneSegment).IsPublic);
    }
}
