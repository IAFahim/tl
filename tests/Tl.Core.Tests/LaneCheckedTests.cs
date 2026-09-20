#if TL_CHECKED
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public partial class LaneTests
{
    [Fact]
    public void SeekRejectsMismatchedColumns()
    {
        var positions = new ushort[4];
        var effects = new float[3];
        Assert.Throws<ArgumentException>(() => Timeline<LawLane>.Apply(positions, true, effects));
    }

    [Fact]
    public unsafe void SeekRejectsOverlappingColumns()
    {
        var buffer = new ushort[10];
        var positions = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var threw = false;
        try { Timeline<LawLane>.Apply(positions, true, effects); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SetThrowsAfterDispose()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        var lane = timelines.Gather(new ushort[] { 0 });
        timelines.Dispose();

        Assert.Throws<ObjectDisposedException>(() => timelines.Gather(new ushort[] { 0 }));
        var threw = false;
        try { lane.Seek(new ushort[] { 0 }, true).Apply(new float[1]); }
        catch (ObjectDisposedException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsOverlappingColumns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[10];
        var ids = buffer.AsSpan(0, 4);
        var positions = buffer.AsSpan(1, 4);
        var effects = new float[4];
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SetRejectsRowColumnMismatch()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0 }).Seek(new ushort[] { 0, 1 }, true).Apply(new float[2]));
    }

    [Fact]
    public void SetRejectsEffectsColumnMismatch()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0, 0 }).Seek(new ushort[] { 0, 1 }, true).Apply(new float[3]));
    }

    [Fact]
    public unsafe void SetRejectsIdsOverlappingEffects()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[12];
        var ids = buffer.AsSpan(0, 4);
        var positions = new ushort[4];
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(2, 8));
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsPositionsOverlappingEffects()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[10];
        var ids = new ushort[] { 0, 0, 0, 0 };
        var positions = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SetDisposeToleratesEmptyAndRepeatedDispose()
    {
        var empty = new TimelineSet<LaneTrack, LaneClip>();
        empty.Dispose();
        empty.Dispose();
        Assert.Throws<ObjectDisposedException>(() => empty.Gather(Array.Empty<ushort>()));

        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var populated = new TimelineSet<LaneTrack, LaneClip>();
        populated.Add(looping);
        populated.Dispose();
        populated.Dispose();
        Assert.Throws<ObjectDisposedException>(() => populated.Gather(new ushort[] { 0 }));
    }
}
#endif
