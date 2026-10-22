using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public record struct StainTrack(float Scale) : IBlend<StainClip>
{
    public float Stain;

    public void Blend(in StainClip first, in StainClip second, float factor, out StainClip result)
    {
        Stain = 42f;
        result = new StainClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public readonly record struct StainClip(float Amount);

internal static unsafe class StainPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<StainTrack, StainClip>.Consume(&FoldStain, &KeysStain);
    }

    static int KeysStain(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 0x10;
        }
        return 1;
    }

    static void FoldStain(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        StainClip scratch = default;
        var frame = TickFrame.ToFrame<StainTrack, StainClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] = frame.Clip.Amount;
    }

}

public unsafe class BlendImmutabilityTests
{
    [Fact]
    public void MutatingBlendLeavesInternedBytesIdentical()
    {
        var bytes = new DomainBaker()
            .Track<StainTrack, StainClip>(new StainTrack(2f))
            .Clip(0, 0u, 10u, new StainClip(1f))
            .Clip(0, 5u, 10u, new StainClip(3f))
            .Bake();
        var index = TimelineAsset.Load(bytes);
        var plain = new float[1];
        Timeline<StainTrack, StainClip>.Apply(index, 0, true, plain);
        Assert.Equal(1f, plain[0]);
        var blended = new float[1];
        Timeline<StainTrack, StainClip>.Apply(index, 7, true, blended);
        Assert.InRange(blended[0], 1.01f, 2.99f);
        Assert.Equal(index, TimelineAsset.Load(bytes));
    }
}
