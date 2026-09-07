using Xunit;

namespace Tl.Core.Tests;

public class AllocationTests
{
    public readonly record struct FastClip(float V);
    public readonly struct FastTrack : IBlend<FastClip>
    {
        public void Blend(in FastClip first, in FastClip second, float t, out FastClip result)
            => result = new FastClip(first.V + (second.V - first.V) * t);
    }

    public struct ZeroAllocConsumer :
        IForward<FastTrack, FastClip, ZeroAllocConsumer>,
        IBackward<FastTrack, FastClip, ZeroAllocConsumer>
    {
        public float Accumulator;

        public void Forward(ref ZeroAllocConsumer data, in Tracks<FastTrack, FastClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Accumulator += work.Clip.V;
        }

        public void Backward(ref ZeroAllocConsumer data, in Tracks<FastTrack, FastClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Accumulator -= work.Clip.V;
        }
    }

    [Fact]
    public void WarmPlaybackAllocatesZeroBytes()
    {
        var id = Timeline<FastTrack, FastClip>.Build(b =>
        {
            var t = b.Track(new FastTrack());
            b.Clip(in t, new FastClip(1.5f), 0, 50);
            b.Clip(in t, new FastClip(3.5f), 25, 75);
        });

        // Warm up and bind
        Timeline<FastTrack, FastClip>.Bind<ZeroAllocConsumer>(id);
        var consumer = new ZeroAllocConsumer();
        var pb = Timeline.Start(id);
        var cursor = default(Cursor);

        // Warm up both paths
        pb = Timeline.Forward(id, in pb, ref cursor, ref consumer, 5u, 10u);
        Span<FastClip> scratch = stackalloc FastClip[2];
        pb = Timeline<FastTrack, FastClip>.Forward(id, in pb, ref consumer, scratch, 15u, 20u);

        // Measure single tick
        long beforeSingle = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline.Forward(id, in pb, ref consumer, 21u);
        long afterSingle = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterSingle - beforeSingle);

        // Measure batch ticks
        long beforeBatch = GC.GetAllocatedBytesForCurrentThread();
        ReadOnlySpan<uint> batch = [22u, 23u, 24u, 25u];
        pb = Timeline.Forward(id, in pb, ref consumer, batch);
        long afterBatch = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterBatch - beforeBatch);

        // Measure cursor-primed ticks
        long beforeCursor = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline.Forward(id, in pb, ref cursor, ref consumer, 26u);
        long afterCursor = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterCursor - beforeCursor);

        // Measure scratch overload
        long beforeScratch = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline<FastTrack, FastClip>.Forward(id, in pb, ref consumer, scratch, 30u);
        long afterScratch = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterScratch - beforeScratch);

        Timeline.Destroy(id);
    }
}
