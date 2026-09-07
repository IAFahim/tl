using Xunit;

namespace Tl.Core.Tests;

public class DedupTests
{
    public readonly record struct PayloadClip(int X, int Y);

    public readonly struct CollisionTrack : IBlend<PayloadClip>
    {
        public void Blend(in PayloadClip first, in PayloadClip second, float t, out PayloadClip result)
            => result = new PayloadClip(first.X + second.X, first.Y + second.Y);
    }

    public struct SumConsumer :
        IForward<CollisionTrack, PayloadClip, SumConsumer>,
        IBackward<CollisionTrack, PayloadClip, SumConsumer>
    {
        public int TotalX;
        public int TotalY;

        public void Forward(ref SumConsumer data, in Tracks<CollisionTrack, PayloadClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
            {
                data.TotalX += work.Clip.X;
                data.TotalY += work.Clip.Y;
            }
        }

        public void Backward(ref SumConsumer data, in Tracks<CollisionTrack, PayloadClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
            {
                data.TotalX -= work.Clip.X;
                data.TotalY -= work.Clip.Y;
            }
        }
    }

    [Fact]
    public void DedupPreservesIdenticalExecutionReceipts()
    {
        void AuthorTimeline(TimelineBuilder<CollisionTrack, PayloadClip> b)
        {
            var t1 = b.Track(new CollisionTrack());
            var t2 = b.Track(new CollisionTrack());

            // Add duplicate clips
            b.Clip(in t1, new PayloadClip(10, 20), 0, 5);
            b.Clip(in t1, new PayloadClip(10, 20), 10, 15);
            b.Clip(in t2, new PayloadClip(10, 20), 0, 5);
            b.Clip(in t2, new PayloadClip(30, 40), 5, 10);
        }

        var defaultId = Timeline<CollisionTrack, PayloadClip>.Build(AuthorTimeline);
        var dedupId = Timeline<CollisionTrack, PayloadClip>.Build(AuthorTimeline, new TimelineOptions { DedupStorage = true });

        var cDefault = new SumConsumer();
        var pbDefault = Timeline.Start(defaultId);
        pbDefault = Timeline.Forward(defaultId, in pbDefault, ref cDefault, 0u, 2u, 6u, 11u);

        var cDedup = new SumConsumer();
        var pbDedup = Timeline.Start(dedupId);
        pbDedup = Timeline.Forward(dedupId, in pbDedup, ref cDedup, 0u, 2u, 6u, 11u);

        Assert.Equal(cDefault.TotalX, cDedup.TotalX);
        Assert.Equal(cDefault.TotalY, cDedup.TotalY);
        Assert.Equal(pbDefault.Tick, pbDedup.Tick);
        Assert.Equal(pbDefault.Flags, pbDedup.Flags);

        Timeline.Destroy(defaultId);
        Timeline.Destroy(dedupId);
    }

    [Fact]
    public void DedupResolvesHashCollisionsViaEquality()
    {
        // Different clips that might have the same hash
        var id = Timeline<CollisionTrack, PayloadClip>.Build(b =>
        {
            var t = b.Track(new CollisionTrack());
            b.Clip(in t, new PayloadClip(1, 2), 0, 5);
            b.Clip(in t, new PayloadClip(2, 1), 5, 10);
        }, new TimelineOptions { DedupStorage = true });

        var c = new SumConsumer();
        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, ref c, 2u, 7u);

        Assert.Equal(3, c.TotalX); // 1 + 2
        Assert.Equal(3, c.TotalY); // 2 + 1

        Timeline.Destroy(id);
    }
}
