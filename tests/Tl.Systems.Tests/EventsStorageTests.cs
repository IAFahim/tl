using Tl;
using Xunit;

public sealed unsafe class EventsStorageTests
{
    [Fact]
    public void EventsOverCallerStorageNeverGrowAndCountWhatDidNotFit()
    {
        var header = stackalloc byte[Events<int>.HeaderBytes];
        var items = stackalloc EventRecord<int>[2];
        var events = new Events<int>(header, items, 2);
        events.Bind(0, 3);
        events.Raise(10);
        events.Bind(0, 1);
        events.Raise(20);
        events.Raise(30);
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events.Dropped);
        var drained = events.Drain();
        Assert.Equal(2, drained.Length);
        Assert.Equal(1, drained[0].Slot);
        Assert.Equal(20, drained[0].Value);
        Assert.Equal(3, drained[1].Slot);
        Assert.True(drained[0].Sequence > drained[1].Sequence);
        Assert.True(items == (EventRecord<int>*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref drained[0]));
        events.Dispose();
    }

    [Fact]
    public void OwnedEventsStillGrow()
    {
        using var events = new Events<int>(1);
        for (var i = 0; i < 40; i++) events.Raise(i);
        Assert.Equal(40, events.Count);
        Assert.Equal(0, events.Dropped);
    }
}
