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

    public struct SumResult :
        ITrack<CollisionTrack, PayloadClip, NoInput, SumResult>
    {
        public int TotalX;
        public int TotalY;

        public static void Forward(int ordinal, int count, ushort index,
            in CollisionTrack track, in PayloadClip clip, ClipState state,
            in uint tick, in NoInput input, ref SumResult result)
        {
            result.TotalX += clip.X;
            result.TotalY += clip.Y;
        }

        public static void Backward(int ordinal, int count, ushort index,
            in CollisionTrack track, in PayloadClip clip, ClipState state,
            in uint tick, in NoInput input, ref SumResult result)
        {
            result.TotalX -= clip.X;
            result.TotalY -= clip.Y;
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

        var defaultId = Timeline<CollisionTrack, PayloadClip>.Build(AuthorTimeline).InMemory();
        var dedupId = Timeline<CollisionTrack, PayloadClip>.Build(AuthorTimeline, new TimelineOptions { DedupStorage = true }).InMemory();

        var input = default(NoInput);

        var cDefault = new SumResult();
        var pbDefault = Timeline.Start(defaultId);
        pbDefault = Timeline.Forward(defaultId, in pbDefault, in input, ref cDefault, 0u, 2u, 6u, 11u);

        var cDedup = new SumResult();
        var pbDedup = Timeline.Start(dedupId);
        pbDedup = Timeline.Forward(dedupId, in pbDedup, in input, ref cDedup, 0u, 2u, 6u, 11u);

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
        }, new TimelineOptions { DedupStorage = true }).InMemory();

        var c = new SumResult();
        var input = default(NoInput);
        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, in input, ref c, 2u, 7u);

        Assert.Equal(3, c.TotalX); // 1 + 2
        Assert.Equal(3, c.TotalY); // 2 + 1

        Timeline.Destroy(id);
    }
}
