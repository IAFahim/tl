using Xunit;

namespace Tl.Core.Tests;

public unsafe class StateHashTests
{
    [Fact]
    public void BytesHashLittleEndianWordsAndZeroPadTheTail()
    {
        var data = stackalloc byte[] { 1, 2, 3, 4, 5, 6 };
        var bytes = TlStateHash.Begin();
        bytes.Bytes(data, 6);
        var words = TlStateHash.Begin();
        words.Word(0x04030201u);
        words.Word(0x0605u);
        Assert.Equal(words.Value, bytes.Value);
    }

    [Fact]
    public void FloatHashesItsBitsAndFloatLaneSignExtendsThem()
    {
        var a = TlStateHash.Begin();
        a.Float(-1f);
        var b = TlStateHash.Begin();
        b.Word(0xBF800000u);
        Assert.Equal(b.Value, a.Value);
        var c = TlStateHash.Begin();
        c.FloatLane(-1f);
        var d = TlStateHash.Begin();
        d.Lane(0xFFFFFFFFBF800000UL);
        Assert.Equal(d.Value, c.Value);
        Assert.NotEqual(a.Value, c.Value);
    }

    [Fact]
    public void SlotsHashTheCountThenEachEightBytePlayback()
    {
        var slots = stackalloc Playback[1];
        slots[0] = new Playback { Timeline = 2, Tick = 7, Channel = 1, Flags = PlaybackFlags.Reverse };
        var a = TlStateHash.Begin();
        a.Slots(slots, 1);
        var b = TlStateHash.Begin();
        b.Word(1);
        b.Word(2u | 7u << 16);
        b.Word(1u | 1u << 8);
        Assert.Equal(b.Value, a.Value);
        var c = TlStateHash.Begin();
        c.Column(slots[0]);
        Assert.NotEqual(a.Value, c.Value);
    }
}
