using Xunit;

namespace Tl.Core.Tests;

public class PlaybackTests
{
    public readonly record struct SampleClip(float Value);

    public readonly struct SampleTrack : IBlend<SampleClip>
    {
        public void Blend(in SampleClip first, in SampleClip second, float t, out SampleClip result)
            => result = new SampleClip(first.Value * (1f - t) + second.Value * t);
    }

    public struct RecordingConsumer :
        IForward<SampleTrack, SampleClip, RecordingConsumer>,
        IBackward<SampleTrack, SampleClip, RecordingConsumer>
    {
        public List<(uint Tick, ClipState State, float Value)> Visits = [];

        public RecordingConsumer() { }

        public void Forward(ref RecordingConsumer data, in Tracks<SampleTrack, SampleClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Visits.Add((tick, work.State, work.Clip.Value));
        }

        public void Backward(ref RecordingConsumer data, in Tracks<SampleTrack, SampleClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Visits.Add((tick, work.State, work.Clip.Value));
        }
    }

    [Fact]
    public void UnstartedPlaybackThrowsInvalidOperationException()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(1f), 0, 10);
        });

        var consumer = new RecordingConsumer();
        var unstarted = default(Playback);

        Assert.Throws<InvalidOperationException>(() => Timeline.Forward(id, in unstarted, ref consumer, 1u));
        Assert.Throws<InvalidOperationException>(() => Timeline.Backward(id, in unstarted, ref consumer, 1u));
        Assert.Throws<InvalidOperationException>(() => Timeline.Stop(id, in unstarted));
        Assert.Empty(consumer.Visits);

        Timeline.Destroy(id);
    }

    [Fact]
    public void StoppedPlaybackThrowsBeforeCallbacks()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(1f), 0, 10);
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id);
        pb = Timeline.Stop(id, in pb);

        Assert.True(pb.Has(PlaybackFlags.Stopped));
        Assert.Throws<InvalidOperationException>(() => Timeline.Forward(id, in pb, ref consumer, 1u));
        Assert.Throws<InvalidOperationException>(() => Timeline.Backward(id, in pb, ref consumer, 1u));
        Assert.Empty(consumer.Visits);

        // Idempotent stop
        var stoppedAgain = Timeline.Stop(id, in pb);
        Assert.True(stoppedAgain.Has(PlaybackFlags.Stopped));

        // New start clears stopped
        var fresh = Timeline.Start(id);
        Assert.False(fresh.Has(PlaybackFlags.Stopped));
        Assert.True(fresh.Has(PlaybackFlags.Started));

        Timeline.Destroy(id);
    }

    [Fact]
    public void EmptyTickSpanPreservesPlaybackWithoutCallbacks()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(1f), 0, 10);
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id, 3u);

        var next = Timeline.Forward(id, in pb, ref consumer, ReadOnlySpan<uint>.Empty);
        Assert.Equal(pb.Tick, next.Tick);
        Assert.Equal(pb.Cycles, next.Cycles);
        Assert.Equal(pb.Flags, next.Flags);
        Assert.Empty(consumer.Visits);

        Timeline.Destroy(id);
    }

    [Fact]
    public void GapsDoNotTriggerCallbacks()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(10f), 5, 10);
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id);

        // Ticks 0, 1, 2 are in the gap before clip start 5
        pb = Timeline.Forward(id, in pb, ref consumer, 0u, 1u, 2u);

        Assert.Empty(consumer.Visits);
        Assert.Equal(2u, pb.Tick);

        // Tick 5 enters the clip
        pb = Timeline.Forward(id, in pb, ref consumer, 5u);
        Assert.Single(consumer.Visits);
        Assert.Equal(5u, consumer.Visits[0].Tick);
        Assert.Equal(ClipState.Enter, consumer.Visits[0].State);

        Timeline.Destroy(id);
    }

    [Fact]
    public void OneTickClipReportsExitOnArrival()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(99f), start: 3, end: 4); // single tick: 3
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id);

        pb = Timeline.Forward(id, in pb, ref consumer, 3u);

        Assert.Single(consumer.Visits);
        Assert.Equal(ClipState.Exit, consumer.Visits[0].State);

        Timeline.Destroy(id);
    }

    [Fact]
    public void SequentialWalkReportsEnterStayExit()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(5f), start: 2, end: 5); // ticks 2, 3, 4
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id, 0);

        pb = Timeline.Forward(id, in pb, ref consumer, 2u, 3u, 4u);

        Assert.Equal(3, consumer.Visits.Count);
        Assert.Equal((2u, ClipState.Enter, 5f), consumer.Visits[0]);
        Assert.Equal((3u, ClipState.Stay, 5f), consumer.Visits[1]);
        Assert.Equal((4u, ClipState.Exit, 5f), consumer.Visits[2]);

        Timeline.Destroy(id);
    }

    [Fact]
    public void OverlapBlendingComputesCorrectFactors()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(0f), 0, 10);
            b.Clip(in t, new SampleClip(100f), 0, 10);
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id);

        // Overlap from 0 to 10 (factorLength = 10)
        // at tick 0: factor = (0 - 0) / 9 = 0 -> value = 0
        // at tick 9: factor = (9 - 0) / 9 = 1 -> value = 100
        pb = Timeline.Forward(id, in pb, ref consumer, 0u, 9u);

        Assert.Equal(2, consumer.Visits.Count);
        Assert.Equal(0f, consumer.Visits[0].Value, precision: 3);
        Assert.Equal(100f, consumer.Visits[1].Value, precision: 3);

        Timeline.Destroy(id);
    }

    [Fact]
    public void CycleCapacityOverflowThrowsArgumentOutOfRangeException()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(1f), 0, 10);
            b.Looping();
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id);

        // A jump of (ushort.MaxValue + 1) * 10 ticks forward exceeds ushort cycle capacity
        uint overflowTick = ((uint)ushort.MaxValue + 2u) * 10u;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Timeline.Forward(id, in pb, ref consumer, overflowTick);
        });

        Timeline.Destroy(id);
    }

    [Fact]
    public void BackwardCycleSubtractionSaturatesAtZero()
    {
        var id = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t = b.Track(new SampleTrack());
            b.Clip(in t, new SampleClip(1f), 0, 10);
            b.Looping();
        });

        var consumer = new RecordingConsumer();
        var pb = Timeline.Start(id, 25u); // Cycles = 0 initially

        // Moving backward from tick 25 to 5
        pb = Timeline.Backward(id, in pb, ref consumer, 5u);
        Assert.Equal(0, pb.Cycles); // saturates at 0

        Timeline.Destroy(id);
    }
}
