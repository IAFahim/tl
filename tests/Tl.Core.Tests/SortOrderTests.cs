using Tl;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class SortOrderTests
{
    static bool RequestBefore(in RequestRecord left, in RequestRecord right)
    {
        if (left.Entity != right.Entity) return left.Entity < right.Entity;
        if (left.Slot != right.Slot) return left.Slot < right.Slot;
        return left.Sequence < right.Sequence;
    }

    static void InsertionSort(RequestRecord[] items)
    {
        for (var i = 1; i < items.Length; i++)
        {
            var item = items[i];
            var j = i - 1;
            while (j >= 0 && RequestBefore(in item, in items[j])) { items[j + 1] = items[j]; j--; }
            items[j + 1] = item;
        }
    }

    static void AssertSameRequest(RequestRecord expected, RequestRecord actual)
    {
        Assert.Equal(expected.Entity, actual.Entity);
        Assert.Equal(expected.Slot, actual.Slot);
        Assert.Equal(expected.Timeline, actual.Timeline);
        Assert.Equal(expected.At, actual.At);
        Assert.Equal(expected.Op, actual.Op);
        Assert.Equal(expected.Sequence, actual.Sequence);
    }

    static RequestRecord[] Snapshot(RequestQueue* queue)
    {
        var snapshot = new RequestRecord[queue->Count];
        for (var i = 0; i < snapshot.Length; i++) snapshot[i] = queue->Item(i);
        return snapshot;
    }

    [Fact]
    public void RequestSortMatchesInsertionSortForDescending15000()
    {
        var queue = RequestQueue.Create();
        try
        {
            for (var e = 0; e < 100; e++)
                for (var k = 0; k < 150; k++)
                    RequestQueue.Add(queue, 99 - e, -1, (ushort)(1 + k), 0, RequestOp.Start);
            Assert.Equal(15_000, queue->Count);
            var expected = Snapshot(queue);
            InsertionSort(expected);
            RequestQueue.SortStable(queue);
            Assert.Equal(15_000, queue->Count);
            for (var i = 0; i < expected.Length; i++) AssertSameRequest(expected[i], queue->Item(i));
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    [Fact]
    public void RequestSortMatchesInsertionSortForShuffledWithNegativeSlots()
    {
        var queue = RequestQueue.Create();
        try
        {
            for (var i = 0; i < 5_000; i++)
                RequestQueue.Add(queue, i % 97, (i * 13) % 31 - 5, (ushort)(1 + (i % 61)), 0, RequestOp.Start);
            Assert.Equal(5_000, queue->Count);
            var expected = Snapshot(queue);
            InsertionSort(expected);
            RequestQueue.SortStable(queue);
            Assert.Equal(5_000, queue->Count);
            for (var i = 0; i < expected.Length; i++) AssertSameRequest(expected[i], queue->Item(i));
            for (var i = 0; i < expected.Length; i++) Assert.Equal(i, queue->Item(i).Sequence == expected[i].Sequence ? i : -1);
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    [Fact]
    public void RequestSortIsStableForEqualEntityAndSlot()
    {
        var queue = RequestQueue.Create();
        try
        {
            for (var i = 0; i < 2_000; i++)
                RequestQueue.Add(queue, i % 3, 2, (ushort)(i % 7), 0, RequestOp.Start);
            Assert.Equal(2_000, queue->Count);
            var expected = Snapshot(queue);
            InsertionSort(expected);
            RequestQueue.SortStable(queue);
            Assert.Equal(2_000, queue->Count);
            for (var i = 0; i < expected.Length; i++) AssertSameRequest(expected[i], queue->Item(i));
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    [Fact]
    public void RequestSortLeavesSortedQueueUntouched()
    {
        var queue = RequestQueue.Create();
        try
        {
            for (var e = 0; e < 50; e++)
                for (var k = 0; k < 20; k++)
                    RequestQueue.Add(queue, e, k, (ushort)(1 + k), 0, RequestOp.Start);
            var before = Snapshot(queue);
            RequestQueue.SortStable(queue);
            Assert.Equal(before.Length, queue->Count);
            for (var i = 0; i < before.Length; i++) AssertSameRequest(before[i], queue->Item(i));
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    static bool EventBefore(in EventRecord<int> left, in EventRecord<int> right)
    {
        if (left.Entity.Index != right.Entity.Index) return left.Entity.Index < right.Entity.Index;
        if (left.Slot != right.Slot) return left.Slot < right.Slot;
        return left.Sequence < right.Sequence;
    }

    static void InsertionSortEvents(EventRecord<int>[] items)
    {
        for (var i = 1; i < items.Length; i++)
        {
            var item = items[i];
            var j = i - 1;
            while (j >= 0 && EventBefore(in item, in items[j])) { items[j + 1] = items[j]; j--; }
            items[j + 1] = item;
        }
    }

    [Fact]
    public void EventsDrainMatchesInsertionSortForDescending()
    {
        using var events = new Events<int>(15_000);
        for (var e = 0; e < 100; e++)
            for (var k = 0; k < 150; k++)
            {
                events.Bind(99 - e, k);
                events.Raise(k);
            }
        Assert.Equal(15_000, events.Count);
        var expected = new EventRecord<int>[events.Count];
        for (var i = 0; i < expected.Length; i++) expected[i] = events.Item(i);
        InsertionSortEvents(expected);
        var drained = events.Drain();
        Assert.Equal(15_000, drained.Length);
        Assert.Equal(0, events.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Entity.Index, drained[i].Entity.Index);
            Assert.Equal(expected[i].Slot, drained[i].Slot);
            Assert.Equal(expected[i].Sequence, drained[i].Sequence);
            Assert.Equal(expected[i].Value, drained[i].Value);
        }
    }

    [Fact]
    public void EventsDrainIsStableForEqualEntityAndSlot()
    {
        using var events = new Events<int>(2_000);
        for (var i = 0; i < 2_000; i++)
        {
            events.Bind(i % 3, 2);
            events.Raise(i);
        }
        Assert.Equal(2_000, events.Count);
        var expected = new EventRecord<int>[events.Count];
        for (var i = 0; i < expected.Length; i++) expected[i] = events.Item(i);
        InsertionSortEvents(expected);
        var drained = events.Drain();
        Assert.Equal(2_000, drained.Length);
        Assert.Equal(0, events.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Entity.Index, drained[i].Entity.Index);
            Assert.Equal(expected[i].Slot, drained[i].Slot);
            Assert.Equal(expected[i].Sequence, drained[i].Sequence);
            Assert.Equal(expected[i].Value, drained[i].Value);
        }
    }

    [Fact]
    public void EventsDrainLeavesSortedEventsUntouched()
    {
        using var events = new Events<int>(1_000);
        for (var e = 0; e < 10; e++)
            for (var k = 0; k < 100; k++)
            {
                events.Bind(e, k);
                events.Raise(k);
            }
        Assert.Equal(1_000, events.Count);
        var before = new EventRecord<int>[events.Count];
        for (var i = 0; i < before.Length; i++) before[i] = events.Item(i);
        var drained = events.Drain();
        Assert.Equal(1_000, drained.Length);
        for (var i = 0; i < before.Length; i++)
        {
            Assert.Equal(before[i].Entity.Index, drained[i].Entity.Index);
            Assert.Equal(before[i].Slot, drained[i].Slot);
            Assert.Equal(before[i].Sequence, drained[i].Sequence);
            Assert.Equal(before[i].Value, drained[i].Value);
        }
    }

    [Fact]
    public void LibraryEventsAreSortedByTickThenKindThenStepThenMarker()
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "kinds.tlib")));
        var view = world.View;
        var checkedTimelines = 0;
        var checkedEvents = 0;
        for (var i = 0; i < view.TimelineCount; i++)
        {
            var tl = view.Timelines + i;
            var events = LibraryViewReader.Events(tl);
            for (var j = 1; j < events.Length; j++)
            {
                var a = events[j - 1];
                var b = events[j];
                var notAfter = a.Tick < b.Tick
                    || (a.Tick == b.Tick && (a.Kind < b.Kind
                        || (a.Kind == b.Kind && (a.StepIndex < b.StepIndex
                            || (a.StepIndex == b.StepIndex && a.Marker <= b.Marker)))));
                Assert.True(notAfter);
            }
            checkedTimelines++;
            checkedEvents += events.Length;
        }
        Assert.True(checkedTimelines > 0);
        Assert.True(checkedEvents > 0);
    }
}
