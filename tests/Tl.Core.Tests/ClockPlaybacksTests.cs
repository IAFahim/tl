using Xunit;

namespace Tl.Core.Tests;

public sealed class ClockPlaybacksTests
{
    [Fact]
    public void ForwardThenReverseRestoresRandomPlaybackAwayFromBoundaries()
    {
        var rng = new Random(20260520);
        for (var i = 0; i < 500; i++)
        {
            ushort duration = (ushort)rng.Next(3, 1000);
            ushort phase = (ushort)rng.Next(1, duration - 1);
            bool looping = rng.Next(2) == 0;
            var playback = new Clock.Playback { Phase = phase, Duration = duration, Looping = looping };
            var (forward, forwardEdge) = Clock.Step(playback, Direction.Forward);
            Assert.Equal(Edge.None, forwardEdge);
            var (back, backEdge) = Clock.Step(forward, Direction.Reverse);
            Assert.Equal(Edge.None, backEdge);
            Assert.Equal(phase, back.Phase);
            Assert.Equal(duration, back.Duration);
            Assert.Equal(looping, back.Looping);
            var (backward, backwardEdge) = Clock.Step(playback, Direction.Reverse);
            Assert.Equal(Edge.None, backwardEdge);
            var (again, againEdge) = Clock.Step(backward, Direction.Forward);
            Assert.Equal(Edge.None, againEdge);
            Assert.Equal(phase, again.Phase);
        }
    }

    [Fact]
    public void ForwardLoopWrapsFromLastFrame()
    {
        var (next, edge) = Clock.Step(new Clock.Playback { Phase = 9, Duration = 10, Looping = true }, Direction.Forward);
        Assert.Equal(Edge.Wrapped, edge);
        Assert.Equal(0, next.Phase);
    }

    [Fact]
    public void ReverseLoopWrapsFromFirstFrame()
    {
        var (next, edge) = Clock.Step(new Clock.Playback { Phase = 0, Duration = 10, Looping = true }, Direction.Reverse);
        Assert.Equal(Edge.Wrapped, edge);
        Assert.Equal(9, next.Phase);
    }

    [Fact]
    public void NonLoopingForwardEndsAtLastFrame()
    {
        var (next, edge) = Clock.Step(new Clock.Playback { Phase = 9, Duration = 10, Looping = false }, Direction.Forward);
        Assert.Equal(Edge.Ended, edge);
        Assert.Equal(9, next.Phase);
    }

    [Fact]
    public void NonLoopingReverseStartsAtFirstFrame()
    {
        var (next, edge) = Clock.Step(new Clock.Playback { Phase = 0, Duration = 10, Looping = false }, Direction.Reverse);
        Assert.Equal(Edge.Started, edge);
        Assert.Equal(0, next.Phase);
    }

    [Fact]
    public void ZeroDurationIsTotalAndReportsEndedOrStarted()
    {
        var (forward, forwardEdge) = Clock.Step(new Clock.Playback { Phase = 5, Duration = 0, Looping = true }, Direction.Forward);
        Assert.Equal(Edge.Ended, forwardEdge);
        Assert.Equal(0, forward.Phase);
        var (backward, backwardEdge) = Clock.Step(new Clock.Playback { Phase = 5, Duration = 0, Looping = true }, Direction.Reverse);
        Assert.Equal(Edge.Started, backwardEdge);
        Assert.Equal(0, backward.Phase);
    }

    [Fact]
    public void BatchAddsKeepPlainCohortSortedByPhaseThenRow()
    {
        var playbacks = new Playbacks();
        var timeline = new TimelineId(7);
        var batch1 = new[]
        {
            new PlainPlayback { Phase = 5, Birth = 0, Duration = 64, Looping = true, Row = new Row(2) },
            new PlainPlayback { Phase = 1, Birth = 0, Duration = 64, Looping = true, Row = new Row(9) },
            new PlainPlayback { Phase = 5, Birth = 0, Duration = 64, Looping = false, Row = new Row(1) },
        };
        var batch2 = new[]
        {
            new PlainPlayback { Phase = 3, Birth = 0, Duration = 64, Looping = true, Row = new Row(0) },
            new PlainPlayback { Phase = 1, Birth = 0, Duration = 64, Looping = true, Row = new Row(3) },
        };
        playbacks.AddCohort(timeline, Direction.Forward, batch1);
        playbacks.AddCohort(timeline, Direction.Forward, batch2);
        Assert.Equal(5, playbacks.CountOf(timeline, Direction.Forward));
        var previousPhase = -1;
        var previousRow = int.MinValue;
        for (var i = 0; i < 5; i++)
        {
            Assert.True(playbacks.TryEntryAt(timeline, Direction.Forward, i, out var entry));
            Assert.True(entry.Phase > previousPhase || (entry.Phase == previousPhase && entry.Row.Value >= previousRow));
            previousPhase = entry.Phase;
            previousRow = entry.Row.Value;
        }
    }

    [Fact]
    public void RemoveDeletesOnlyTheMatchingRow()
    {
        var playbacks = new Playbacks();
        var timeline = new TimelineId(8);
        var batch = new[]
        {
            new PlainPlayback { Phase = 1, Duration = 64, Looping = true, Row = new Row(0) },
            new PlainPlayback { Phase = 2, Duration = 64, Looping = true, Row = new Row(1) },
        };
        playbacks.AddCohort(timeline, Direction.Reverse, batch);
        Assert.True(playbacks.Remove(timeline, Direction.Reverse, new Row(0)));
        Assert.False(playbacks.Remove(timeline, Direction.Reverse, new Row(0)));
        Assert.False(playbacks.Remove(timeline, Direction.Reverse, new Row(99)));
        Assert.Equal(1, playbacks.CountOf(timeline, Direction.Reverse));
        Assert.True(playbacks.TryEntryAt(timeline, Direction.Reverse, 0, out var entry));
        Assert.Equal(new Row(1), entry.Row);
    }

    [Fact]
    public void NonLoopingPlainEndsAtClockEdgeAndStepperKeepsSurvivors()
    {
        var playbacks = new Playbacks();
        var timeline = new TimelineId(3);
        var entries = new[]
        {
            new PlainPlayback { Phase = 9, Birth = 20, Duration = 10, Looping = false, Row = new Row(0) },
            new PlainPlayback { Phase = 4, Birth = 20, Duration = 10, Looping = false, Row = new Row(1) },
        };
        playbacks.AddCohort(timeline, Direction.Forward, entries);
        Assert.Equal(1, playbacks.Advance(timeline, Direction.Forward));
        Assert.Equal(1, playbacks.CountOf(timeline, Direction.Forward));
        Assert.True(playbacks.TryEntryAt(timeline, Direction.Forward, 0, out var remaining));
        Assert.Equal(new Row(1), remaining.Row);
        Assert.Equal(5, remaining.Phase);
    }

    [Fact]
    public void LoopingPlainWrapsAndSurvivesAdvance()
    {
        var playbacks = new Playbacks();
        var timeline = new TimelineId(3);
        var entries = new[]
        {
            new PlainPlayback { Phase = 9, Birth = 0, Duration = 10, Looping = true, Row = new Row(0) },
        };
        playbacks.AddCohort(timeline, Direction.Forward, entries);
        Assert.Equal(0, playbacks.Advance(timeline, Direction.Forward));
        Assert.True(playbacks.TryEntryAt(timeline, Direction.Forward, 0, out var wrapped));
        Assert.Equal(0, wrapped.Phase);
    }

    [Fact]
    public void SoloAdvanceStepsRatedPlaybackOneTickPerCall()
    {
        var playbacks = new Playbacks();
        var timeline = new TimelineId(4);
        var entries = new[]
        {
            new SoloPlayback { Tick = 0, Duration = 16, Flags = 1, Rate = 16, Carry = 0, Row = new Row(0) },
        };
        playbacks.AddSoloCohort(timeline, Direction.Forward, entries);
        Assert.Equal(0, playbacks.AdvanceSolo(timeline, Direction.Forward));
        Assert.True(playbacks.TrySoloEntryAt(timeline, Direction.Forward, 0, out var solo));
        Assert.Equal(1, solo.Tick);
        Assert.Equal(0, solo.Carry);
    }

    [Fact]
    public void RadixSortIsStableForEqualKeysAndOrdersByRowThenSlot()
    {
        var items = new[]
        {
            new Request5 { Row = new Row(2), Slot = 1, Op = RequestOp.Start, Timeline = new TimelineId(1), At = 10 },
            new Request5 { Row = new Row(1), Slot = 5, Op = RequestOp.Stop, Timeline = new TimelineId(1), At = 11 },
            new Request5 { Row = new Row(2), Slot = 1, Op = RequestOp.Pause, Timeline = new TimelineId(1), At = 12 },
            new Request5 { Row = new Row(1), Slot = 5, Op = RequestOp.Resume, Timeline = new TimelineId(1), At = 13 },
            new Request5 { Row = new Row(0), Slot = 0, Op = RequestOp.Seek, Timeline = new TimelineId(2), At = 14 },
            new Request5 { Row = new Row(2), Slot = 1, Op = RequestOp.Reverse, Timeline = new TimelineId(1), At = 15 },
        };
        Requests5.StableRadixSort(items);
        ushort[] expectedAt = [14, 11, 13, 10, 12, 15];
        for (var i = 0; i < items.Length; i++)
        {
            Assert.Equal(expectedAt[i], items[i].At);
            if (i > 0)
            {
                var previous = items[i - 1];
                var current = items[i];
                Assert.True(previous.Row.Value < current.Row.Value ||
                    (previous.Row.Value == current.Row.Value && previous.Slot <= current.Slot));
            }
        }
    }

    [Fact]
    public void InvalidRequestsAreCountedNotThrown()
    {
        var requests = new Requests5();
        requests.Add(new Row(0), 0, RequestOp.Start, new TimelineId(1), 0);
        requests.Add(new Row(3), 0, RequestOp.Start, new TimelineId(1), 1);
        requests.Add(new Row(-1), 0, RequestOp.Stop, new TimelineId(1), 2);
        requests.Add(new Row(1), 0, RequestOp.Stop, TimelineId.None, 3);
        requests.Add(new Row(1), 0, RequestOp.Stop, new TimelineId(99), 4);
        var seen = 0;
        var outcome = requests.Apply(2, timeline => timeline.Value == 1, _ => seen++);
        Assert.Equal(1, outcome.Applied);
        Assert.Equal(2, outcome.InvalidRow);
        Assert.Equal(2, outcome.InvalidTimeline);
        Assert.Equal(5, outcome.Total);
        Assert.Equal(1, seen);
    }
}
