#if TL_CHECKED
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public class PerEntityCheckedTests
{
    static byte[] LoopingBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 0, 4, new LaneClip(8))
        .Looping()
        .Bake();

    [Fact]
    public unsafe void FoldedViewThrowsAfterDispose()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var set = new TimelineSet<LaneTrack, LaneClip>();
        set.Add(looping);
        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.FoldedView(0));
    }

    [Fact]
    public void ViewThrowsAfterDispose()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var set = new TimelineSet<LaneTrack, LaneClip>();
        set.Add(looping);
        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.View(0));
    }

    [Fact]
    public void AddAtThrowsAfterDispose()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var set = new TimelineSet<LaneTrack, LaneClip>();
        using var measured = MeasuredLanes.Measure(looping);
        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.AddAt(0, measured));
    }

    [Fact]
    public void SetAdvanceRejectsMismatchedColumns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<LaneTrack, LaneClip>();
        set.Add(looping);
        Assert.Throws<ArgumentException>(() => set.Advance([ 0 ], new ushort[] { 0, 1 }, new ushort[] { 0 }, true));
        Assert.Throws<ArgumentException>(() => set.Advance([ 0, 0 ], new ushort[] { 0, 1 }, new ushort[] { 0, 1, 2 }, true));
    }

    [Fact]
    public void TimelineAdvanceRejectsMismatchedColumns()
    {
        Assert.Throws<ArgumentException>(() => Timeline.Advance([ 0 ], new ushort[] { 0, 1 }, new ushort[] { 0, 1 }, true));
        Assert.Throws<ArgumentException>(() => Timeline.Advance([ 0, 0 ], new ushort[] { 0, 1 }, new ushort[] { 0 }, true));
    }
}
#endif
