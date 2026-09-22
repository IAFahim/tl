#if TL_CHECKED
using Xunit;

namespace Tl.Core.Tests;

public partial class LaneTests
{
    [Fact]
    public void AdvanceRejectsPositionsBeyondTheLoadedAssetDuration()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var indices = new ushort[] { asset.Index, asset.Index, asset.Index };
        var positions = new ushort[] { 0, 10, 6 };

        var thrown = Assert.Throws<ArgumentException>(() => Timeline.Advance(indices, positions, true));

        Assert.Contains("Row 1", thrown.Message);
        Assert.Contains($"timeline {asset.Index}", thrown.Message);
        Assert.Contains("position 10", thrown.Message);
        Assert.Contains("duration 6", thrown.Message);
    }

    [Fact]
    public void TypedApplyRejectsAPositionBeyondTheLoadedAssetDuration()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());

        var thrown = Assert.Throws<ArgumentException>(() => Timeline<LaneTrack, LaneClip>.Apply(asset.Index, (ushort)10, true));

        Assert.Contains("position 10", thrown.Message);
        Assert.Contains("duration 6", thrown.Message);
    }

    [Fact]
    public void TypedEffectsApplyRejectsOutOfDomainPositions()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var positions = new ushort[] { 2, 9 };
        var effects = new float[2];

        var thrown = Assert.Throws<ArgumentException>(() => Timeline<LaneTrack, LaneClip>.Apply(asset.Index, positions, true, effects));

        Assert.Contains("Row 1", thrown.Message);
        Assert.Contains("position 9", thrown.Message);
        Assert.Contains("duration 6", thrown.Message);
    }

    [Fact]
    public void TypedAdvanceRejectsAnOutOfDomainPosition()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var position = (ushort)7;

        Assert.Throws<ArgumentException>(() => Timeline<LaneTrack, LaneClip>.Advance(asset.Index, ref position, true));
    }

    [Fact]
    public void RangeApplyRejectsAnOutOfDomainStartButNotAnOutOfDomainTarget()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());

        Assert.Throws<ArgumentException>(() => Timeline<LaneTrack, LaneClip>.Apply(asset.Index, 10, 0, false));
        Timeline<LaneTrack, LaneClip>.Apply(asset.Index, 0, 10, true);
    }

    [Fact]
    public void InDomainAndClampPositionsPassEverySurface()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var indices = new ushort[] { asset.Index, asset.Index, asset.Index };
        var positions = new ushort[] { 0, 3, 6 };
        var effects = new float[3];

        Timeline.Advance(indices, positions, true);
        Timeline.Advance(asset.Index, positions, true);
        Timeline<LaneTrack, LaneClip>.Apply(indices, positions, true);
        Timeline<LaneTrack, LaneClip>.Apply(asset.Index, positions, true, effects);
        Timeline<LaneTrack, LaneClip>.Apply(asset.Index, (ushort)6, true);
        var atEnd = (ushort)6;
        Timeline<LaneTrack, LaneClip>.Advance(asset.Index, ref atEnd, true);
        Timeline<LaneTrack, LaneClip>.Apply(asset.Index, 0, 6, true);

        Assert.Equal((ushort)6, atEnd);
        Assert.Equal(0f, effects[2]);
    }
}
#endif
