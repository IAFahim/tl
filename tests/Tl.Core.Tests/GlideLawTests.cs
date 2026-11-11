using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class GlideLawTests
{
    [Fact]
    public void SlotWordHasTheRequiredEightByteLittleEndianLayout()
    {
        Assert.True(BitConverter.IsLittleEndian);
        Assert.Equal(8, sizeof(Playback));
        var fields = new[] { nameof(Playback.Timeline), nameof(Playback.Tick), nameof(Playback.Channel), nameof(Playback.Flags), nameof(Playback.Rate), nameof(Playback.Phase) };
        var offsets = new[] { 0, 2, 4, 5, 6, 7 };
        for (var i = 0; i < fields.Length; i++) Assert.Equal(offsets[i], Marshal.OffsetOf<Playback>(fields[i]).ToInt32());
        var slot = new Playback { Timeline = 0x3344, Tick = 0x5566, Channel = 0x77, Flags = (PlaybackFlags)0xA5, Rate = 0x0F, Phase = 0xB2 };
        Assert.Equal(0xB20FA57755663344ul, *(ulong*)&slot);
    }

    [Fact]
    public void EveryFlagByteAcrossRatesPhasesDirectionsAndBoundariesObeysTheHorizonLaw()
    {
        var timelines = stackalloc LibraryTimeline[8];
        timelines[0] = new() { Duration = 31 };
        timelines[1] = new() { Duration = 31, Loop = 1 };
        timelines[2] = new() { Duration = 31, Hold = 1 };
        timelines[3] = new() { Duration = 31, Next = 2 };
        timelines[4] = new() { Duration = 0 };
        timelines[5] = new() { Duration = 1, Loop = 1 };
        timelines[6] = new() { Duration = 65535, Loop = 1 };
        timelines[7] = new() { Duration = 65536 };
        var cases = 0;
        var glides = 0;
        var fixedPoints = 0;
        foreach (var forward in new[] { false, true })
            for (ushort id = 1; id <= 8; id++)
                foreach (var tick in new ushort[] { 0, 1, 15, 30, 65534, 65535 })
                    for (var flags = 0; flags < 256; flags++)
                        foreach (var rate in new byte[] { 0, 1, 16, 255 })
                            foreach (var phase in new byte[] { 0, 3, 15, 255 })
                            {
                                var slot = new Playback { Timeline = id, Tick = tick, Channel = 0xA3, Flags = (PlaybackFlags)flags, Rate = rate, Phase = phase };
                                var word = *(ulong*)&slot;
                                if (SlotWord.IsInert(word))
                                {
                                    var fixedPoint = slot;
                                    Assert.Equal(AdvanceResult.Kept, PlaybackRules.Step(ref fixedPoint, timelines, 8, forward));
                                    Assert.Equal(word, *(ulong*)&fixedPoint);
                                    fixedPoints++;
                                }
                                var horizon = Horizon.OfRun(&slot, 1, timelines, 8, SlotWord.WorldReverse(forward), 7);
                                var expected = slot;
                                for (var step = 0; step < horizon; step++)
                                {
                                    Assert.Equal(AdvanceResult.Kept, PlaybackRules.Step(ref expected, timelines, 8, forward));
                                    GlideKernel.RunScalar((ulong*)&slot, 1, SlotWord.WorldReverse(forward));
                                    Assert.Equal(*(ulong*)&expected, *(ulong*)&slot);
                                    glides++;
                                }
                                cases++;
                            }
        Assert.Equal(2 * 8 * 6 * 256 * 4 * 4, cases);
        Assert.True(glides > 1000);
        Assert.True(fixedPoints > 1000);
    }

    [Fact]
    public void RunHorizonStopsAtTheEarliestEdgeAndRejectsUnstableSlots()
    {
        var timeline = new LibraryTimeline { Duration = 31, Loop = 1 };
        var slots = stackalloc Playback[4];
        slots[0] = new() { Timeline = 1, Tick = 5, Phase = 255 };
        slots[1] = new() { Timeline = 1, Tick = 9, Flags = PlaybackFlags.Reverse };
        slots[2] = new() { Timeline = 1, Tick = 15, Flags = PlaybackFlags.Paused, Rate = 255, Phase = 255 };
        slots[3] = new() { Timeline = 0, Tick = 65535, Flags = (PlaybackFlags)255, Rate = 255, Phase = 255 };
        Assert.Equal((byte)9, Horizon.OfRun(slots, 4, &timeline, 1, 0, 255));
        Assert.Equal((byte)1, Horizon.OfRun(slots, 4, &timeline, 1, 0, 1));
        Assert.Equal((byte)5, Horizon.OfRun(slots, 4, &timeline, 1, SlotWord.ReverseBit, 255));
        var expected = new Playback[4];
        new ReadOnlySpan<Playback>(slots, 4).CopyTo(expected);
        for (var step = 0; step < 9; step++)
        {
            for (var i = 0; i < 4; i++)
                if (expected[i].Timeline != 0) PlaybackRules.Step(ref expected[i], &timeline, 1, true);
            GlideKernel.RunScalar((ulong*)slots, 4, 0);
            for (var i = 0; i < 4; i++)
            {
                var value = expected[i];
                Assert.Equal(*(ulong*)&value, ((ulong*)slots)[i]);
            }
        }
        slots[1].Tick = 9;
        Assert.Equal((byte)0, Horizon.OfRun(slots, 4, &timeline, 1, 0, -1));
        Assert.Equal((byte)0, Horizon.OfRun(slots, 4, &timeline, -1, 0, 255));
        Assert.Equal((byte)0, Horizon.OfRun(null, 4, &timeline, 1, 0, 255));
        slots[0].Flags |= PlaybackFlags.StartPending;
        Assert.Equal((byte)0, Horizon.OfRun(slots, 4, &timeline, 1, 0, 255));
        slots[0].Flags = PlaybackFlags.None;
        slots[0].Rate = 1;
        Assert.Equal((byte)0, Horizon.OfRun(slots, 4, &timeline, 1, 0, 255));
        slots[0].Rate = 0;
        slots[0].Tick = 30;
        Assert.Equal((byte)0, Horizon.OfRun(slots, 4, &timeline, 1, 0, 255));
    }

    [Fact]
    public void LongRunsStopAtTheCacheLimitAndTheKernelDoesNotTouchItsTail()
    {
        var timeline = new LibraryTimeline { Duration = 65535, Loop = 1 };
        var slots = stackalloc Playback[259];
        for (var i = 0; i < 259; i++) slots[i] = new() { Timeline = 1, Tick = (ushort)(1000 + i), Flags = (i & 1) == 0 ? PlaybackFlags.None : PlaybackFlags.Reverse, Channel = (byte)i, Phase = (byte)i };
        var first = *(ulong*)slots;
        var last = *(ulong*)(slots + 258);
        Assert.Equal((byte)255, Horizon.OfRun(slots + 1, 257, &timeline, 1, 0, int.MaxValue));
        for (var step = 0; step < 255; step++) GlideKernel.RunScalar((ulong*)(slots + 1), 257, 0);
        Assert.Equal(first, *(ulong*)slots);
        Assert.Equal(last, *(ulong*)(slots + 258));
        for (var i = 1; i < 258; i++) Assert.Equal((ushort)(1000 + i + ((i & 1) == 0 ? 255 : -255)), slots[i].Tick);
    }

    [Fact]
    public void PackedDecrementValidatesEveryByteBeforeWriting()
    {
        var bytes = stackalloc byte[64];
        var before = new byte[64];
        for (var zero = -1; zero < 64; zero++)
        {
            for (var i = 0; i < 64; i++) bytes[i] = (byte)(1 + (i * 37 % 255));
            if (zero >= 0) bytes[zero] = 0;
            new ReadOnlySpan<byte>(bytes, 64).CopyTo(before);
            Assert.Equal(zero < 0, Horizon.Decrement64(bytes));
            for (var i = 0; i < 64; i++) Assert.Equal(zero < 0 ? (byte)(before[i] - 1) : before[i], bytes[i]);
        }
        for (var i = 0; i < 64; i++) bytes[i] = 1;
        Assert.True(Horizon.Decrement64(bytes));
        Assert.True(new ReadOnlySpan<byte>(bytes, 64).SequenceEqual(new byte[64]));
    }
}
