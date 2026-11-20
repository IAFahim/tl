using Xunit;

namespace Tl.Core.Tests;

public sealed class EventRecordsTests
{
    static void RaiseFrom(ref Events<int> events, int entity, int slot, int value)
    {
        events.Bind(entity, slot);
        events.Raise(value);
    }

    [Fact]
    public void EachEntitySeesOnlyItsOwnEventsInSlotThenRaiseOrder()
    {
        using var events = new Events<int>(8);
        var writer = events.Writer;
        RaiseFrom(ref writer, 3, 0, 30);
        RaiseFrom(ref writer, 1, 2, 12);
        RaiseFrom(ref writer, 1, 0, 10);
        RaiseFrom(ref writer, 0, 1, 1);
        RaiseFrom(ref writer, 1, 0, 11);
        var drained = events.Drain();
        Assert.Equal(5, drained.Length);
        var one = drained.Of(1);
        Assert.Equal(3, one.Length);
        Assert.Equal(new[] { 10, 11, 12 }, new[] { one[0].Value, one[1].Value, one[2].Value });
        Assert.Equal(new[] { 0, 0, 2 }, new[] { one[0].Slot, one[1].Slot, one[2].Slot });
        Assert.Equal(1, drained.Of(0).Length);
        Assert.Equal(1, drained.Of(0)[0].Value);
        Assert.Equal(0, drained.Of(2).Length);
        Assert.Equal(30, drained.Of(3)[0].Value);
        Assert.Equal(0, drained.Of(4).Length);
        Assert.Equal(0, drained.Of(-1).Length);
    }

    [Fact]
    public void ReadingByEntityAllocatesNothing()
    {
        using var events = new Events<int>(64);
        var writer = events.Writer;
        for (var i = 0; i < 64; i++) RaiseFrom(ref writer, i % 7, i % 3, i);
        var drained = events.Drain();
        var total = 0;
        for (var e = 0; e < 7; e++) total += drained.Of(e).Length;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var round = 0; round < 1000; round++)
            for (var e = 0; e < 7; e++)
                total += drained.Of(e).Length;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0L, allocated);
        Assert.Equal(64 * 1001, total);
    }
}
