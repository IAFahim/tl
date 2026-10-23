using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct GuardClip(float Amount);

public readonly record struct GuardTrack(float Scale) : IBlend<GuardClip>
{
    public void Blend(in GuardClip first, in GuardClip second, float factor, out GuardClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct GuardWideClip(float Amount);

public readonly record struct GuardWideTrack(float Scale) : IBlend<GuardWideClip>
{
    public void Blend(in GuardWideClip first, in GuardWideClip second, float factor, out GuardWideClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class GuardPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<GuardTrack, GuardClip>.Consume(&FoldSolo, &KeysSolo, "tl\0FoldSolo");
        PairRuntime<GuardWideTrack, GuardWideClip>.Consume(&FoldWide, &KeysWide, "tl\0FoldWide");
    }

    static void Owned(ulong* keys, byte* meta, int count)
    {
        if (keys == null) return;
        for (var j = 0; j < count; j++)
        {
            keys[j] = TypeKey<float>.Value;
            meta[j] = 4 | 0x10;
        }
    }

    static int KeysSolo(ulong* keys, byte* meta)
    {
        Owned(keys, meta, 1);
        return 1;
    }

    static int KeysWide(ulong* keys, byte* meta)
    {
        Owned(keys, meta, 3);
        return 3;
    }

    static void FoldSolo(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = tick + 1;

    static void FoldWide(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ((float*)columns[0])[row] = tick + 1;
        ((float*)columns[1])[row] = tick + 2;
        ((float*)columns[2])[row] = tick + 3;
    }

}

public unsafe class BlockGuardTests
{
    const int GuardBytes = 32;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(8)]
    public void SoloPermuteLoadsStayInsideTheZeroedBlockGuard(ushort duration)
        => CheckGuard<GuardTrack, GuardClip>(duration, new GuardTrack(1), new GuardClip(1f));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    public void WidePermuteLoadsStayInsideTheZeroedBlockGuard(ushort duration)
        => CheckGuard<GuardWideTrack, GuardWideClip>(duration, new GuardWideTrack(1), new GuardWideClip(1f));

    [Fact]
    public void TableBytesCountTheGuardPerBlock()
    {
        CheckAccounting<GuardTrack, GuardClip>(new GuardTrack(1), new GuardClip(1f), 1, [0, 3, 7]);
        CheckAccounting<GuardWideTrack, GuardWideClip>(new GuardWideTrack(1), new GuardWideClip(1f), 3, [0, 3, 7]);
    }

    static void CheckGuard<TTrack, TClip>(ushort duration, TTrack track, TClip clip)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        var baker = new DomainBaker().Track<TTrack, TClip>(track);
        if (duration > 0)
            baker = baker.Clip(0, 0u, duration, clip);
        using var asset = TimelineAsset.Of(TimelineAsset.Load(baker.Bake()));
        using var isolated = new TimelineSet<TTrack, TClip>();
        var index = isolated.Add(asset);
        var slot = isolated.FoldedView((ushort)index);
        Assert.True(slot != null);
        Assert.True(slot->Forward != null);
        Assert.True(slot->Backward != null);
        var ticks = (int)slot->TableTicks;
        var lanes = (int)slot->ResultCount;
        Assert.Equal(Math.Max(1, (int)duration) + 1, ticks);
        var guard = (byte*)slot->LaneKeys + lanes * 8;
        for (var l = 0; l < lanes; l++)
        {
            Assert.True((byte*)slot->Forward + ((nuint)l * (nuint)ticks + 8) * 4 <= guard + GuardBytes, $"forward lane {l}");
            Assert.True((byte*)slot->Backward + ((nuint)l * (nuint)ticks + 8) * 4 <= guard + GuardBytes, $"backward lane {l}");
        }
        for (var i = 0; i < GuardBytes; i++)
            Assert.Equal(0, guard[i]);
        var last = lanes - 1;
        var expected = BitConverter.SingleToUInt32Bits(LaneEncoding.Value(slot, (nuint)last, false, 0));
        Assert.Equal(expected, BitConverter.SingleToUInt32Bits(Vector256.Load(slot->Backward + (nuint)last * (nuint)ticks).GetElement(0)));
        Assert.Equal(expected, BitConverter.SingleToUInt32Bits(Vector128.Load(slot->Backward + (nuint)last * (nuint)ticks).GetElement(0)));
        _ = Vector128.Load(slot->Backward + (nuint)last * (nuint)ticks + 4).GetElement(0);
    }

    static void CheckAccounting<TTrack, TClip>(TTrack track, TClip clip, int lanes, ushort[] durations)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        using var isolated = new TimelineSet<TTrack, TClip>();
        var expected = 0L;
        foreach (var duration in durations)
        {
            var baker = new DomainBaker().Track<TTrack, TClip>(track);
            if (duration > 0)
                baker = baker.Clip(0, 0u, duration, clip);
            using var asset = TimelineAsset.Of(TimelineAsset.Load(baker.Bake()));
            isolated.Add(asset);
            expected += 8L * lanes * (Math.Max(1, (int)duration) + 1) + GuardBytes;
        }
        Assert.Equal(durations.Length, isolated.BlockCount);
        Assert.Equal(expected, isolated.TableBytes);
    }
}
