using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct SegmentClip(float Amount);

public readonly record struct SegmentTrack(float Scale) : IBlend<SegmentClip>
{
    public void Blend(in SegmentClip first, in SegmentClip second, float factor, out SegmentClip result)
        => result = new SegmentClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct SegmentWideClip(float Amount);

public readonly record struct SegmentWideTrack(float Scale) : IBlend<SegmentWideClip>
{
    public void Blend(in SegmentWideClip first, in SegmentWideClip second, float factor, out SegmentWideClip result)
        => result = new SegmentWideClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class SegmentPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<SegmentTrack, SegmentClip>.Consume(&FoldTwo, &RangeTwo, &KeysTwo);
        PairRuntime<SegmentWideTrack, SegmentWideClip>.Consume(&FoldWideA, &RangeWide, &KeysWideA);
        PairRuntime<SegmentWideTrack, SegmentWideClip>.Consume(&FoldWideB, &RangeWide, &KeysWideB);
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

    static int KeysTwo(ulong* keys, byte* meta)
    {
        Owned(keys, meta, 2);
        return 2;
    }

    static int KeysWideA(ulong* keys, byte* meta)
    {
        Owned(keys, meta, 6);
        return 6;
    }

    static int KeysWideB(ulong* keys, byte* meta)
    {
        Owned(keys, meta, 6);
        return 6;
    }

    static void FoldTwo(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ((float*)columns[0])[row] = tick >> 6;
        ((float*)columns[1])[row] = tick % 512 + 1;
    }

    static void RangeTwo(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int rowStart, int rowCount) { }

    static void FoldWideA(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ((float*)columns[0])[row] = tick >> 3;
        ((float*)columns[1])[row] = tick >> 4;
        ((float*)columns[2])[row] = tick >> 5;
        ((float*)columns[3])[row] = tick >> 6;
        ((float*)columns[4])[row] = tick >> 7;
        ((float*)columns[5])[row] = tick >> 8;
    }

    static void FoldWideB(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ((float*)columns[0])[row] = (tick >> 3) + 100;
        ((float*)columns[1])[row] = (tick >> 4) + 200;
        ((float*)columns[2])[row] = (tick >> 5) + 300;
        ((float*)columns[3])[row] = (tick >> 6) + 400;
        ((float*)columns[4])[row] = (tick >> 7) + 500;
        ((float*)columns[5])[row] = (tick >> 8) + 600;
    }

    static void RangeWide(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int rowStart, int rowCount) { }
}

public unsafe class SegmentedMultiLaneTests
{
    static byte[] Bake<TTrack, TClip>(ushort duration, TTrack track, TClip clip)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        var baker = new Baker()
            .Track<TTrack, TClip>(track)
            .Clip(0, 0u, duration, clip);
        return baker.Bake();
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(4095)]
    [InlineData(65535)]
    public void WideBankBakes(ushort duration)
    {
        using var asset = TimelineAsset.LoadAsset(Bake(duration, new SegmentWideTrack(1f), new SegmentWideClip(2.5f)));
        using var set = new TimelineSet<SegmentWideTrack, SegmentWideClip>();
        var index = set.Add(asset);
        var view = set.View(index);
        Assert.Equal(12u, view.ResultCount);
        Assert.True(view.Forward == null);
        Assert.True(view.Backward == null);
        Assert.True(view.Directory != null);
        float[][] expected =
        [
            Table(duration, t => t >> 3),
            Table(duration, t => t >> 4),
            Table(duration, t => t >> 5),
            Table(duration, t => t >> 6),
            Table(duration, t => t >> 7),
            Table(duration, t => t >> 8),
            Table(duration, t => (t >> 3) + 100),
            Table(duration, t => (t >> 4) + 200),
            Table(duration, t => (t >> 5) + 300),
            Table(duration, t => (t >> 6) + 400),
            Table(duration, t => (t >> 7) + 500),
            Table(duration, t => (t >> 8) + 600),
        ];
        var matched = new bool[12];
        for (var l = 0; l < 12; l++)
        {
            var candidates = 0;
            var pick = -1;
            for (var e = 0; e < 12; e++)
            {
                if (matched[e]) continue;
                var equal = true;
                for (var t = 0; t < duration; t++)
                {
                    if (LaneEncoding.Bits(LaneEncoding.Value(&view, (nuint)l, true, t)) != LaneEncoding.Bits(expected[e][t])
                        || LaneEncoding.Bits(LaneEncoding.Value(&view, (nuint)l, false, t)) != LaneEncoding.Bits(expected[e][t]))
                    {
                        equal = false;
                        break;
                    }
                }
                if (!equal) continue;
                candidates++;
                pick = e;
            }
            Assert.Equal(1, candidates);
            matched[pick] = true;
        }
    }

    static float[] Table(int duration, Func<int, float> lane)
    {
        var values = new float[duration];
        for (var t = 0; t < duration; t++)
            values[t] = lane(t);
        return values;
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(4095)]
    public void TwoLaneSegmentedBankDecodesMeasuredValues(ushort duration)
    {
        using var asset = TimelineAsset.LoadAsset(Bake(duration, new SegmentTrack(1f), new SegmentClip(1.5f)));
        using var set = new TimelineSet<SegmentTrack, SegmentClip>();
        var index = set.Add(asset);
        var view = set.View(index);
        Assert.Equal(2u, view.ResultCount);
        var forward = new float[2][];
        var backward = new float[2][];
        for (var l = 0; l < 2; l++)
        {
            forward[l] = new float[duration];
            backward[l] = new float[duration];
            for (var t = 0; t < duration; t++)
            {
                forward[l][t] = LaneEncoding.Value(&view, (nuint)l, true, t);
                backward[l][t] = LaneEncoding.Value(&view, (nuint)l, false, t);
            }
        }
        for (var t = 0; t < duration; t++)
        {
            Assert.Equal(t >> 6, (int)forward[0][t]);
            Assert.Equal(t % 512 + 1, (int)forward[1][t]);
            Assert.Equal(t >> 6, (int)backward[0][t]);
            Assert.Equal(t % 512 + 1, (int)backward[1][t]);
        }
    }
}
