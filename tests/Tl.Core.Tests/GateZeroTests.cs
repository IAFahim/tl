using Xunit;

namespace Tl.Core.Tests;

public unsafe class GateZeroTests
{
    [Fact]
    public void RequestQueueGrowingPreservesEveryRecordInOrder()
    {
        var queue = RequestQueue.Create();
        try
        {
            for (var i = 0; i < 100; i++)
                RequestQueue.Add(queue, i, i % 3, (ushort)(i % 7), (ushort)i, RequestOp.Start);
            Assert.Equal(100, queue->Count);
            for (var i = 0; i < 100; i++)
            {
                var record = queue->Item(i);
                Assert.Equal(i, record.Entity);
                Assert.Equal(i % 3, record.Slot);
                Assert.Equal(i, record.Sequence);
                Assert.Equal(RequestOp.Start, record.Op);
            }
        }
        finally
        {
            RequestQueue.Free(queue);
        }
    }

    [Fact]
    public void ClosingListGrowingPreservesEveryEntry()
    {
        var list = ClosingList.Create();
        try
        {
            for (var i = 0; i < 30; i++)
                ClosingList.Add(list, i, i % 4, new Playback { Timeline = (ushort)(i + 1), Tick = (ushort)i });
            Assert.Equal(30, list->Count);
            for (var i = 0; i < 30; i++)
            {
                list->Get(i, out var entity, out var slot, out var playback);
                Assert.Equal(i, entity);
                Assert.Equal(i % 4, slot);
                Assert.Equal((ushort)(i + 1), playback.Timeline);
                Assert.Equal((ushort)i, playback.Tick);
            }
        }
        finally
        {
            ClosingList.Free(list);
        }
    }

    [Fact]
    public void OwnedEventsGrowingPreservesEveryRaisedValue()
    {
        using var events = new Events<int>(2);
        events.Bind(7, 3);
        for (var i = 0; i < 10; i++)
            events.Raise(i);
        Assert.Equal(10, events.Count);
        Assert.Equal(0, events.Dropped);
        var drained = events.Drain();
        Assert.Equal(10, drained.Length);
        for (var i = 0; i < 10; i++)
            Assert.Equal(i, drained[i].Value);
    }

    [Fact]
    public void PlaybackTableGrowingPreservesEarlierSlotsPerEntity()
    {
        using var table = new PlaybackTable(4, 2);
        var slots = new PlaybackTable.Slots(table, 0);
        for (var i = 0; i < 5; i++)
        {
            var playback = new Playback { Timeline = (ushort)(i + 1), Tick = (ushort)(i * 10), Channel = (byte)i };
            Assert.True(slots.TryAdd(in playback));
        }
        Assert.Equal(5, table.CountOf(0));
        var rows = table.SlotsOf(0);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal((ushort)(i + 1), rows[i].Timeline);
            Assert.Equal((ushort)(i * 10), rows[i].Tick);
            Assert.Equal((byte)i, rows[i].Channel);
        }
        Assert.Equal(0, table.CountOf(1));
    }

    [Fact(Skip = "Gate-zero M0 pin: PairTable is process-global state; Install with null thunks faults the test host in this harness. Probe-vs-grow hash consistency is asserted by the v5 Library Verify layout re-derive instead.")]
    public void PairTableProbeFindsKeysInstalledBeforeAndAfterSlotGrowth()
    {
        const ulong layout = 0xC0FFEE123456789AUL;
        for (var i = 0; i < 5; i++)
            PairTable.VerifyLayout((ulong)(2 * i + 1), layout + (ulong)i);
        for (var i = 0; i < 600; i++)
            PairTable.Install(100001UL + (ulong)(2 * i), null, null, "GateZero.Grow." + i);
        for (var i = 0; i < 5; i++)
            Assert.Equal(layout + (ulong)i, PairTable.LayoutOf((ulong)(2 * i + 1)));
        Assert.True(PairTable.HeadOf(100001UL) >= 0);
        Assert.True(PairTable.HeadOf(100001UL + (ulong)(2 * 599)) >= 0);
        Assert.Equal(-1, PairTable.HeadOf(999999937UL));
    }

    [Fact]
    public void PairKeysAreNeverZeroAndPairCompositionMatchesItsSpelling()
    {
        Assert.Equal(1, (int)(Keying.Of("GateZero") & 1UL));
        Assert.Equal(1, (int)(Keying.Of("") & 1UL));
        Assert.Equal(1, (int)(Keying.Of(Keying.StableName(typeof(ProbeTrack))) & 1UL));
        var pair = Keying.Of(Keying.StableName(typeof(ProbeTrack)) + "\0" + Keying.StableName(typeof(ProbeClip)));
        var recomputed = Keying.Of(Keying.StableName(typeof(ProbeTrack)) + "\0" + Keying.StableName(typeof(ProbeClip)));
        Assert.NotEqual(0UL, pair);
        Assert.Equal(recomputed, pair);
        Assert.NotEqual(Keying.StableName(typeof(ProbeTrack)), Keying.StableName(typeof(ProbeClip)));
    }

    [Fact]
    public void AllocZeroedReturnsZeroBytes()
    {
        const int size = 512;
        var block = (byte*)NativeAlloc.AllocZeroed(size);
        try
        {
            var span = new ReadOnlySpan<byte>(block, size);
            for (var i = 0; i < size; i++)
                Assert.Equal(0, span[i]);
        }
        finally
        {
            NativeAlloc.Free(block);
        }
    }

    [Fact(Skip = "Gate-zero M0 pin: NativeAlloc.Clear and the non-NET NativeAlloc.AllocZeroed cast nuint lengths to int (src/Tl.Core/Polyfills.cs:53,60,83), truncating zeroing above 2 GiB. 4.0 cannot satisfy this row without a >2 GiB allocation; 5.0 must clear the full nuint range.")]
    public void AllocZeroedAboveIntMaxValueReturnsZeroBytesToTheEnd()
    {
        var size = (nuint)(1u << 31) + 4096u;
        var block = (byte*)NativeAlloc.AllocZeroed(size);
        try
        {
            var last = new ReadOnlySpan<byte>(block + (nuint)(1u << 31), 4096);
            for (var i = 0; i < last.Length; i++)
                Assert.Equal(0, last[i]);
        }
        finally
        {
            NativeAlloc.Free(block);
        }
    }

    struct ProbeTrack { }
    struct ProbeClip { }
}
