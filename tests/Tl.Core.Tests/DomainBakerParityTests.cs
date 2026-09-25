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
        PairRuntime<BakerParityTrack, BakerParityClip>.Consume(&ExecuteParity, &BindFloat);
        PairRuntime<BakerOtherTrack, BakerOtherClip>.Consume(&ExecuteOther, &BindFloat);
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
    [InlineData(0, 200, "891ba4ad5619cc16407ef5b442f1f4d6e52ebeaf9ba86b58d2c593db5af15952")]
    [InlineData(1, 240, "f50a5c1dc68b42287928c578d3548758e708bf9c6037433854d23fa38378a732")]
    [InlineData(2, 224, "533e9574cf2ce37ba6617b59457c8b440f2f37d79b2faab96761bf8f1e6d4642")]
    [InlineData(3, 304, "25d6a1263abf2b86d940384962c81de245fa73d835ef2ff45492b4be7ed645e7")]
    [InlineData(4, 376, "744bc37aef0157aebf8fc91d9825ae8ae31e3c9b5cc4807e8ef897c43de2d9c0")]
    [InlineData(5, 256, "2ad49141cbcd1c9590a84571715b6de82740b4b5f015500820c4bcc1450bae52")]
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
                    Assert.Equal(9f, BakedLane<BakerParityTrack, BakerParityClip>.Effect(0));
                    BakedLane<BakerOtherTrack, BakerOtherClip>.Bind(reloaded);
                    Assert.Equal(9f, BakedLane<BakerOtherTrack, BakerOtherClip>.Effect(0));
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
        3 => 9f,
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
