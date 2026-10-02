using System.Collections.Generic;
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
        for (var i = first; i < first + count; i++) columns[consumers[i].Offset] = (void*)(long)(1000 + i - first);
        return columns;
    }

    [Fact]
    public void InstallsGrowPastTheInitialSixtyFourConsumerRows()
    {
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++)
            PairTable.Install(0x9000_0000_0000_0000UL + (ulong)(uint)(i + 1), null, null, null, null, false);
        Assert.True(PairTable.ConsumerCount >= before + 70);
    }

    [Fact]
    public void DistinctPairsGrowPastFiveHundredTwelve()
    {
        const ulong seed = 0xC000_0000_0000_0000UL;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 600; i++) PairTable.Install(seed + (ulong)(uint)i, null, null, null, null, false);
        Assert.Equal(before + 600, PairTable.ConsumerCount);
        for (var i = 0; i < 600; i++) Assert.True(PairTable.HeadOf(seed + (ulong)(uint)i) >= 0);
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
        for (var i = 0; i < 70; i++) PairTable.Install(key, &Tag, null, null, null, false);
        var columns = (void**)TaggedColumns(before, 70);
        Order.Clear();
        PairTable.RunChain(PairTable.HeadOf(key), true, null, null, 0, 0, columns, 0, null, null);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1000 + k, Order[k]);
        Order.Clear();
        PairTable.RunChain(PairTable.HeadOf(key), false, null, null, 0, 0, columns, 0, null, null);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1069 - k, Order[k]);
        NativeMemory.AlignedFree(columns);
    }

    [Fact]
    public void RunDispatchReversesChainsLongerThanSixtyFourEntries()
    {
        const ulong key = 0xA200_0000_0000_0001UL;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++) PairTable.Install(key, &Tag, null, null, null, false, true);
        var columns = (void**)TaggedColumns(before, 70);
        Order.Clear();
        PairTable.RunDispatch(PairTable.HeadOf(key), true, null, null, 0, 0, 0, columns);
        Assert.Equal(70, Order.Count);
        for (var k = 0; k < 70; k++) Assert.Equal(1000 + k, Order[k]);
        NativeMemory.AlignedFree(columns);
    }

    readonly record struct WindowCrowdClip(int Value);

    readonly record struct WindowCrowdTrack(int Code) : IBlend<WindowCrowdClip>
    {
        public void Blend(in WindowCrowdClip first, in WindowCrowdClip second, float factor, out WindowCrowdClip result)
            => result = first;
    }

    [Fact]
    public void ExecuteWindowReverseFeedsEveryConsumerOfAChainsLongerThanSixtyFour()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<WindowCrowdTrack, WindowCrowdClip>(new WindowCrowdTrack(1))
            .Clip(0, 0, 1, new WindowCrowdClip(3))
            .Bake());
        var key = PairRuntime<WindowCrowdTrack, WindowCrowdClip>.Key;
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 65; i++) PairTable.Install(key, null, null, null, null, false);

        Span<int> chains = stackalloc int[1];
        asset.Reference.Resolve(chains);

        var columns = (void**)NativeMemory.AlignedAlloc((nuint)(sizeof(void*) * PairTable.PointerBound), 8);
        var cells = (float*)NativeMemory.Alloc(65 * sizeof(float));
        var consumers = PairTable.ConsumerAt;
        for (var i = 0; i < 65; i++)
        {
            cells[i] = 0f;
            columns[consumers[before + i].Offset] = cells + i;
        }
        var stepCached = (byte*)NativeMemory.Alloc(16);
        var stepCacheBase = (int*)NativeMemory.Alloc(16 * sizeof(int));
        var cacheValues = (float*)NativeMemory.Alloc(128 * sizeof(float));
        for (var i = 0; i < 16; i++) { stepCached[i] = 1; stepCacheBase[i] = 0; }
        for (var i = 0; i < 128; i++) cacheValues[i] = 1f;

        asset.Reference.ExecuteWindow(true, 0, 0, 0, chains, columns, stepCached, stepCacheBase, cacheValues);

        for (var i = 0; i < 65; i++) Assert.Equal(1f, cells[i]);
        NativeMemory.AlignedFree(columns);
        NativeMemory.Free(cells);
        NativeMemory.Free(stepCached);
        NativeMemory.Free(stepCacheBase);
        NativeMemory.Free(cacheValues);
    }
}
