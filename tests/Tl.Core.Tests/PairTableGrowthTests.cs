using System.Runtime.InteropServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class PairTableGrowthTests
{
    static readonly List<int> Order = new();

    static void Tag(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => Order.Add((int)(long)columns[0]);

    static void* TaggedColumns(int first, int count)
    {
        var columns = (void**)NativeMemory.AlignedAlloc((nuint)(sizeof(void*) * PairTable.PointerBound), 8);
        var consumers = PairTable.ConsumerAt;
        for (var i = first; i < first + count; i++) columns[consumers[i].Offset] = (void*)(1000 + i - first);
        return columns;
    }

    [Fact]
    public void InstallsGrowPastTheInitialSixtyFourConsumerRows()
    {
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++)
            PairTable.Install(0x9000_0000_0000_0000UL + (ulong)(i + 1), null, null, $"tl\0Growth{i}");
        Assert.True(PairTable.ConsumerCount >= before + 70);
    }

    [Fact]
    public void DistinctPairsGrowPastFiveHundredTwelve()
    {
        const ulong seed = 0xC000_0000_0000_0000UL;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 600; i++) PairTable.Install(seed + (ulong)i, null, null, $"tl\0Growth{i}");
        Assert.Equal(before + 600, PairTable.ConsumerCount);
        for (var i = 0; i < 600; i++) Assert.True(PairTable.HeadOf(seed + (ulong)i) >= 0);
        Assert.Equal(-1, PairTable.HeadOf(0xDDDD_0000_0000_0001UL));
        Assert.Equal(0UL, PairTable.LayoutOf(seed + 42));
        PairTable.VerifyLayout(seed + 42, 0x1234_5678_9ABC_DEF0UL);
        Assert.Equal(0x1234_5678_9ABC_DEF0UL, PairTable.LayoutOf(seed + 42));
    }

    [Fact]
    public void RunChainReversesChainsLongerThanSixtyFourEntries()
    {
        const ulong key = 0xA100_0000_0000_0001UL;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++) PairTable.Install(key, &Tag, null, $"tl\0Tag{i}");
        var columns = (void**)TaggedColumns(before, 70);
        Order.Clear();
        PairTable.RunChain(PairTable.HeadOf(key), true, null, null, 0, 0, columns, 0);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1000 + k, Order[k]);
        Order.Clear();
        PairTable.RunChain(PairTable.HeadOf(key), false, null, null, 0, 0, columns, 0);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1069 - k, Order[k]);
        NativeMemory.AlignedFree(columns);
    }

    [Fact]
    public void RunDispatchReversesChainsLongerThanSixtyFourEntries()
    {
        const ulong key = 0xA200_0000_0000_0001UL;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++) PairTable.Install(key, &Tag, null, $"tl\0Tag{i}", true);
        var columns = (void**)TaggedColumns(before, 70);
        Order.Clear();
        PairTable.RunDispatch(PairTable.HeadOf(key), true, null, null, 0, 0, 0, columns);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1000 + k, Order[k]);
        NativeMemory.AlignedFree(columns);
    }

}
