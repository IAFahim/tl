using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct CapClip(float Amount);

public readonly record struct CapTrack(float Scale) : IBlend<CapClip>
{
    public void Blend(in CapClip first, in CapClip second, float factor, out CapClip result)
        => result = new CapClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class CapPairs
{
    internal static volatile int OwnedResults = 10;

    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<CapTrack, CapClip>.Consume(&FoldCap, &KeysCap, "tl\0FoldCap");

    static int KeysCap(ulong* keys, byte* meta)
    {
        if (keys == null) return OwnedResults;
        for (var j = 0; j < OwnedResults; j++)
        {
            keys[j] = TypeKey<float>.Value + (ulong)j;
            meta[j] = 4 | 0x10;
        }
        return OwnedResults;
    }

    static void FoldCap(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        for (var j = 0; j < OwnedResults; j++) ((float*)columns[j])[row] = j;
    }

}

public unsafe class ConsumerResultCapTests
{
    [Fact]
    public void RejectsConsumerDeclaringMoreThanTenOwnedResults()
    {
        var index = TimelineAsset.Load(new DomainBaker()
            .Track<CapTrack, CapClip>(new CapTrack(2f))
            .Clip(0, 0u, 4u, new CapClip(1.5f))
            .Bake());
        var entry = PairTable.HeadOf(PairRuntime<CapTrack, CapClip>.Key);
        Assert.True(entry >= 0);
        var before = PairTable.ConsumerAt[entry];
        CapPairs.OwnedResults = 11;
        try
        {
            var thrown = Assert.Throws<InvalidOperationException>(() => Timeline<CapTrack, CapClip>.View(index));
            Assert.Equal("consumer 0 declares more than 10 owned results.", thrown.Message);
        }
        finally
        {
            CapPairs.OwnedResults = 10;
        }
        var after = PairTable.ConsumerAt[entry];
        Assert.True(before.Next == after.Next && before.Offset == after.Offset && before.Feed == after.Feed && before.Ordinal == after.Ordinal && before.NameOffset == after.NameOffset && before.PairKey == after.PairKey && before.DispatchOnly == after.DispatchOnly);
        Assert.True(SameTail(&before, PairTable.ConsumerAt + entry));
        Timeline<CapTrack, CapClip>.View(index);
    }

    static bool SameTail(PairTable.Consumer* expected, PairTable.Consumer* actual)
        => (void*)expected->Execute == (void*)actual->Execute && (void*)expected->Bind == (void*)actual->Bind && (void*)expected->Keys == (void*)actual->Keys && (void*)expected->Diag == (void*)actual->Diag && expected->DispatchOnly == actual->DispatchOnly;
}
