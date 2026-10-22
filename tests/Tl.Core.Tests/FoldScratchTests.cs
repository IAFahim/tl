using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct ScratchZeroClip(float Amount);

public readonly record struct ScratchZeroTrack(float Scale) : IBlend<ScratchZeroClip>
{
    public void Blend(in ScratchZeroClip first, in ScratchZeroClip second, float factor, out ScratchZeroClip result)
        => result = new ScratchZeroClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class ScratchPairs
{
    internal static volatile bool ThrowOnKeys;

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<ScratchZeroTrack, ScratchZeroClip>.Consume(&FoldZero, &KeysZero);
    }

    static int KeysZero(ulong* keys, byte* meta)
    {
        if (ThrowOnKeys) throw new InvalidOperationException("scratch fold keys failed on purpose");
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 0x10;
        }
        return 1;
    }

    static void FoldZero(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = 1f;

}

public unsafe class FoldScratchTests
{
    [Fact]
    public void ThrowingKeysPropagatesAndTheSameAssetFoldsAfterwards()
    {
        var index = TimelineAsset.Load(new DomainBaker()
            .Track<ScratchZeroTrack, ScratchZeroClip>(new ScratchZeroTrack(2f))
            .Clip(0, 0u, 4u, new ScratchZeroClip(1.5f))
            .Bake());
        ScratchPairs.ThrowOnKeys = true;
        var effects = new float[1];
        try
        {
            var thrown = Assert.Throws<InvalidOperationException>(() => Timeline<ScratchZeroTrack, ScratchZeroClip>.Apply(index, 0, true, effects));
            Assert.Equal("scratch fold keys failed on purpose", thrown.Message);
        }
        finally
        {
            ScratchPairs.ThrowOnKeys = false;
        }
        Timeline<ScratchZeroTrack, ScratchZeroClip>.Apply(index, 1, true, effects);
        Assert.Equal(1f, effects[0]);
    }

    [Fact]
    public void DurationZeroFoldsAllZeroTablesForDistinctContents()
    {
        foreach (var scale in new[] { 1f, 7f })
        {
            var index = TimelineAsset.Load(new DomainBaker()
                .Track<ScratchZeroTrack, ScratchZeroClip>(new ScratchZeroTrack(scale))
                .Bake());
            var effects = new float[1];
            Timeline<ScratchZeroTrack, ScratchZeroClip>.Apply(index, 0, true, effects);
            var copy = Inspection.CopyLanes<ScratchZeroTrack, ScratchZeroClip>(index)!;
            Assert.NotNull(copy);
            Assert.Equal(0, copy.Duration);
            foreach (var lane in copy.Lanes)
            {
                Assert.All(lane.Forward, word => Assert.Equal(0u, word));
                Assert.All(lane.Backward, word => Assert.Equal(0u, word));
            }
        }
    }
}
