global using Baker = Tl.TestSupport.DomainBaker;

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

internal static unsafe class BakerParityPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<BakerParityTrack, BakerParityClip>.Consume(&ExecuteParity, &BindFloat, "tl\0ExecuteParity");
        PairRuntime<BakerOtherTrack, BakerOtherClip>.Consume(&ExecuteOther, &BindFloat, "tl\0ExecuteOther");
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    static void ExecuteParity(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        BakerParityClip scratch = default;
        var frame = TickFrame.ToFrame<BakerParityTrack, BakerParityClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    static void ExecuteOther(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        BakerOtherClip scratch = default;
        var frame = TickFrame.ToFrame<BakerOtherTrack, BakerOtherClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }
}

public class DomainBakerParityTests
{
    [Theory]
    [InlineData(0, 200, "8e2ec34507a47397ab93fdef7a63ef28157340ff4fcdcd444183477e2eb41391")]
    [InlineData(1, 240, "a4404f1f012dd53b58b47147326919fefabfeb1d608ffca0a3343e6ee71dafde")]
    [InlineData(2, 224, "3e32d1222e3660974699416ce8ee7d83c6b799efa4a8fbd9bd560537cd7ce521")]
    [InlineData(3, 304, "cd4d5e7531d2b7633d605bb686dbc6e5f2425f5d3026bc15e6798546d7c4d656")]
    [InlineData(4, 376, "f96f14b5cd77de42cd09b2c34af85e242d108e17a43bedf0741eb17541227080")]
    [InlineData(5, 256, "817bb3aa68deb832d068a1dd8b9fa17d82a0824a30726fbd4af2061c38b41046")]
    public void BakesCanonicalBytes(int fixture, int length, string sha256)
    {
        var bytes = BakeFixture(fixture);
        Assert.Equal(length, bytes.Length);
        Assert.Equal(sha256, Hash(bytes));
    }

    [Fact]
    public void BakedFixturesLoadIntoTheRuntime()
    {
        for (var fixture = 0; fixture < 6; fixture++)
        {
            var bytes = BakeFixture(fixture);
            ushort first;
            using (var asset = TimelineAsset.Of(TimelineAsset.Load(bytes)))
                first = asset.Index;
            using var reloaded = TimelineAsset.Of(TimelineAsset.Load(bytes));
            Assert.Equal(first, reloaded.Index);

            BakedLane<BakerParityTrack, BakerParityClip>.Bind(reloaded);
            Assert.Equal(64, BakedLane<BakerParityTrack, BakerParityClip>.Duration);
            Assert.Equal(fixture is 1 or 3 or 5, BakedLane<BakerParityTrack, BakerParityClip>.Looping);
            Assert.Equal(ParityEffectAtZero(fixture), BakedLane<BakerParityTrack, BakerParityClip>.Effect(0));
            switch (fixture)
            {
                case 1:
                    Assert.Equal(6f, BakedLane<BakerParityTrack, BakerParityClip>.Effect(40));
                    break;
                case 3:
                    Assert.Equal(1f, BakedLane<BakerParityTrack, BakerParityClip>.Effect(0));
                    BakedLane<BakerOtherTrack, BakerOtherClip>.Bind(reloaded);
                    Assert.Equal(8f, BakedLane<BakerOtherTrack, BakerOtherClip>.Effect(0));
                    break;
                case 4:
                    Assert.Equal(2f, BakedLane<BakerParityTrack, BakerParityClip>.Effect(48));
                    BakedLane<BakerOtherTrack, BakerOtherClip>.Bind(reloaded);
                    Assert.Equal(1f, BakedLane<BakerOtherTrack, BakerOtherClip>.Effect(16));
                    break;
                case 5:
                    Assert.Equal(4f, BakedLane<BakerParityTrack, BakerParityClip>.Effect(48));
                    break;
            }
        }
    }

    static float ParityEffectAtZero(int fixture) => fixture switch
    {
        1 => 2f,
        2 => 7f,
        3 => 1f,
        4 => 1f,
        5 => 2f,
        _ => 1f,
    };

    private static string Hash(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static byte[] BakeFixture(int fixture)
    {
        var baker = new DomainBaker();
        switch (fixture)
        {
            case 0:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(1f))
                     .Clip(0, 0u, 64u, new BakerParityClip(1f));
                break;
            case 1:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(2f))
                     .Clip(0, 0u, 32u, new BakerParityClip(1f))
                     .Clip(0, 32u, 64u, new BakerParityClip(3f))
                     .Looping();
                break;
            case 2:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(1f))
                     .Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(3f))
                     .Clip(0, 0u, 64u, new BakerParityClip(1f))
                     .Clip(1, 0u, 64u, new BakerParityClip(2f));
                break;
            case 3:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(1f))
                     .Track<BakerOtherTrack, BakerOtherClip>(new BakerOtherTrack(2f))
                     .Clip(0, 0u, 64u, new BakerParityClip(1f))
                     .Clip(1, 0u, 64u, new BakerOtherClip(4f))
                     .Looping();
                break;
            case 4:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(1f))
                     .Track<BakerOtherTrack, BakerOtherClip>(new BakerOtherTrack(1f))
                     .Clip(0, 0u, 16u, new BakerParityClip(1f))
                     .Clip(0, 48u, 64u, new BakerParityClip(2f))
                     .Clip(1, 16u, 48u, new BakerOtherClip(1f));
                break;
            case 5:
                baker.Track<BakerParityTrack, BakerParityClip>(new BakerParityTrack(2f))
                     .Clip(0, 0u, 16u, new BakerParityClip(1f))
                     .Clip(0, 48u, 64u, new BakerParityClip(2f))
                     .Looping();
                break;
        }
        return baker.Bake();
    }
}

public readonly record struct BakerParityClip(float Amount);

public readonly record struct BakerParityTrack(float Scale) : IBlend<BakerParityClip>
{
    public void Blend(in BakerParityClip first, in BakerParityClip second, float factor, out BakerParityClip result)
        => result = new BakerParityClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct BakerOtherClip(float Amount);

public readonly record struct BakerOtherTrack(float Scale) : IBlend<BakerOtherClip>
{
    public void Blend(in BakerOtherClip first, in BakerOtherClip second, float factor, out BakerOtherClip result)
        => result = new BakerOtherClip(first.Amount + (second.Amount - first.Amount) * factor);
}
