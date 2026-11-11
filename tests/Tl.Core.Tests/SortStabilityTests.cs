using Tl;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class SortStabilityTests
{
    public static readonly int[] Sizes = { 0, 1, 2, 19, 20, 21, 64, 1000, 5000, 100000 };
    public static readonly string[] Shapes = { "equal", "sorted", "reverse", "duplicates", "random", "negative", "nearlysorted" };

    public static TheoryData<int, string> Cases()
    {
        var data = new TheoryData<int, string>();
        foreach (var size in Sizes)
            foreach (var shape in Shapes)
                data.Add(size, shape);
        return data;
    }

    static (int Entity, int Slot)[] Keys(int size, string shape)
    {
        var random = new Random(size * 31 + shape.Length);
        var keys = new (int, int)[size];
        for (var i = 0; i < size; i++)
        {
            keys[i] = shape switch
            {
                "equal" => (7, 3),
                "sorted" => (i / 3, i % 3),
                "reverse" => (size - i, size - i),
                "duplicates" => (random.Next(8), random.Next(4)),
                "negative" => (random.Next(-50, 50), random.Next(-5, 5)),
                "nearlysorted" => (i / 4 + (i % 50 == 0 ? random.Next(-3, 4) : 0), 0),
                _ => (random.Next(), random.Next(int.MinValue, int.MaxValue)),
            };
        }
        return keys;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RequestSortsMatchStableReference(int size, string shape)
    {
        var keys = Keys(size, shape);
        var queue = RequestQueue.Create();
        try
        {
            for (var i = 0; i < size; i++) RequestQueue.Add(queue, keys[i].Entity, keys[i].Slot, (ushort)(i & 0xFFFF), (ushort)(i >> 16), RequestOp.Start);
            var input = new RequestRecord[size];
            for (var i = 0; i < size; i++) input[i] = queue->Item(i);
            var expected = input.OrderBy(r => r.Entity).ThenBy(r => r.Slot).ToArray();
            var viaPublic = (RequestRecord*)System.Runtime.InteropServices.NativeMemory.AlignedAlloc((nuint)Math.Max(size, 1) * (nuint)sizeof(RequestRecord), 8u);
            try
            {
                for (var i = 0; i < size; i++) viaPublic[i] = input[i];
                RequestQueue.Sort(viaPublic, size);
                for (var i = 0; i < size; i++) Assert.True(Same(expected[i], viaPublic[i]), "Sort at " + i);
                var host = new RequestRecord[Math.Max(size, 1)];
                var hostScratch = new RequestRecord[Math.Max(size, 1)];
                for (var i = 0; i < size; i++) host[i] = input[i];
                fixed (RequestRecord* hostPtr = host)
                fixed (RequestRecord* scratchPtr = hostScratch)
                {
                    RequestQueue.Sort(hostPtr, size, scratchPtr);
                    for (var i = 0; i < size; i++) Assert.True(Same(expected[i], hostPtr[i]), "Sort with scratch at " + i);
                }
            }
            finally
            {
                System.Runtime.InteropServices.NativeMemory.AlignedFree(viaPublic);
            }
            RequestQueue.SortStable(queue);
            Assert.Equal(size, queue->Count);
            for (var i = 0; i < size; i++) Assert.True(Same(expected[i], queue->Item(i)), "SortStable at " + i);
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    static bool Same(in RequestRecord x, in RequestRecord y) =>
        x.Entity == y.Entity && x.Slot == y.Slot && x.Timeline == y.Timeline && x.At == y.At && x.Op == y.Op && x.Sequence == y.Sequence;

    [Theory]
    [MemberData(nameof(Cases))]
    public void EventsDrainMatchesStableReference(int size, string shape)
    {
        var keys = Keys(size, shape);
        using var events = new Events<int>(size);
        for (var i = 0; i < size; i++)
        {
            events.Bind(keys[i].Entity, keys[i].Slot);
            events.Raise(i);
        }
        var input = new EventRecord<int>[size];
        for (var i = 0; i < size; i++) input[i] = events.Item(i);
        var expected = input.OrderBy(r => r.Entity.Index).ThenBy(r => r.Slot).ToArray();
        var drained = events.Drain();
        Assert.Equal(size, drained.Length);
        Assert.Equal(0, events.Count);
        for (var i = 0; i < size; i++)
        {
            Assert.True(
                expected[i].Entity.Index == drained[i].Entity.Index && expected[i].Slot == drained[i].Slot
                    && expected[i].Sequence == drained[i].Sequence && expected[i].Value == drained[i].Value,
                "Drain at " + i);
        }
    }

    [Fact]
    public void EventsDrainAllocatesNoManagedBytes()
    {
        const int size = 5000;
        var keys = Keys(size, "random");
        using var events = new Events<int>(size);
        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < size; i++)
            {
                events.Bind(keys[i].Entity, keys[i].Slot);
                events.Raise(i);
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            var drained = events.Drain();
            var after = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(size, drained.Length);
            if (round > 0) Assert.Equal(0L, after - before);
            for (var i = 1; i < size; i++)
                Assert.True(drained[i - 1].Entity.Index < drained[i].Entity.Index
                    || (drained[i - 1].Entity.Index == drained[i].Entity.Index && drained[i - 1].Slot <= drained[i].Slot));
        }
    }

    [Fact]
    public void RequestSortStableAllocatesNoManagedBytes()
    {
        const int size = 5000;
        var keys = Keys(size, "random");
        var queue = RequestQueue.Create();
        try
        {
            for (var round = 0; round < 3; round++)
            {
                RequestQueue.Clear(queue);
                for (var i = 0; i < size; i++) RequestQueue.Add(queue, keys[i].Entity, keys[i].Slot, 1, 0, RequestOp.Start);
                var before = GC.GetAllocatedBytesForCurrentThread();
                RequestQueue.SortStable(queue);
                var after = GC.GetAllocatedBytesForCurrentThread();
                if (round > 0) Assert.Equal(0L, after - before);
                for (var i = 1; i < size; i++)
                    Assert.True(queue->Item(i - 1).Entity < queue->Item(i).Entity
                        || (queue->Item(i - 1).Entity == queue->Item(i).Entity && queue->Item(i - 1).Slot <= queue->Item(i).Slot));
            }
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }
}
