using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Tl.TestSupport;

using Xunit;

namespace Tl.Core.Tests;

public enum RetypeSbyteKind : sbyte { A }

public enum RetypeByteKind : byte { A }

public struct RetypeSbyteClip { public RetypeSbyteKind Kind; }

public struct RetypeByteClip { public RetypeByteKind Kind; }

public unsafe struct FixedEightClip { public fixed byte Buffer[8]; public byte Tail; }

public unsafe struct FixedNineClip { public fixed byte Buffer[9]; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackOneTrack { public byte A; public byte B; public int C; public int D; }

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct PackTwoTrack { public byte A; public byte B; public int C; public int D; }

public readonly record struct SkewClip(int Value);

public readonly record struct SkewTrack(int Code) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct SkewClipRetyped(float Value);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct LayoutOrderTrack(int Code, float Scale) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct LayoutSwappedTrack(float Scale, int Code) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

public unsafe class BindLayoutAgreementTests
{
    static BindLayoutAgreementTests()
    {
        PairRuntime<SkewTrack, SkewClip>.Consume(&ExecuteCode, &BindFloat);
        PairRuntime<SkewTrack, SkewClip>.VerifyLayout(BakeFingerprint.FoldOnly(typeof(SkewTrack), typeof(SkewClip)));
    }

    private static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    private static void ExecuteCode(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        SkewClip scratch = default;
        var frame = TickFrame.ToFrame<SkewTrack, SkewClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Track.Code * frame.Clip.Value;
    }

    [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "pins loaded assets so intern indices stay live for the test run; content intentionally unread")]
    static readonly List<TimelineAsset> KeepAlive = [];

    static byte[] Bake() => new DomainBaker { FingerprintOf = BakeFingerprint.Of }
        .Track<SkewTrack, SkewClip>(new SkewTrack(2))
        .Clip(0, 0, 4, new SkewClip(5))
        .Bake();

    static TimelineAsset Load(byte[] baked)
    {
        var asset = TimelineAsset.LoadAsset(baked);
        KeepAlive.Add(asset);
        return asset;
    }

    static void Apply(TimelineAsset asset)
    {
        var positions = new ushort[1];
        var next = new ushort[1];
        var effects = new float[1];
        Timeline<SkewTrack, SkewClip>.Apply(asset.Index, positions, next, true, effects);
    }

    static void CorruptPairLayout(byte[] baked)
    {
        var pairOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(28));
        var current = BinaryPrimitives.ReadUInt64LittleEndian(baked.AsSpan(pairOffset + 40));
        BinaryPrimitives.WriteUInt64LittleEndian(baked.AsSpan(pairOffset + 40), current ^ 0x5555AAAA5555AAAAul);
    }

    [Fact]
    public void FoldRejectsBakedPairWithDivergentLayout()
    {
        var baked = Bake();
        CorruptPairLayout(baked);
        var asset = Load(baked);

        var failure = Assert.Throws<ArgumentException>(() => Apply(asset));

        Assert.Contains("different field layout", failure.Message, StringComparison.Ordinal);
        Assert.Contains("SkewTrack", failure.Message, StringComparison.Ordinal);
        Assert.Contains("SkewClip", failure.Message, StringComparison.Ordinal);
        Assert.Contains("rebake", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FoldAcceptsLegacyAssetWithoutRecordedLayout()
    {
        var baked = Bake();
        var pairOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(28));
        BinaryPrimitives.WriteUInt64LittleEndian(baked.AsSpan(pairOffset + 40), 0ul);
        var asset = Load(baked);

        Apply(asset);
    }

    [Fact]
    public void LoaderRejectsVersionThreeBytesWithRebakeRepair()
    {
        var baked = Bake();
        BinaryPrimitives.WriteUInt32LittleEndian(baked.AsSpan(4), 3u);

        var failure = Assert.Throws<ArgumentException>(() => Load(baked));

        Assert.Contains("TLB magic or version invalid", failure.Message, StringComparison.Ordinal);
        Assert.Contains("rebake", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FoldAcceptsCurrentLayoutAndApplies()
    {
        var asset = Load(Bake());
        var positions = new ushort[1];
        var next = new ushort[1];
        var effects = new float[1];

        Timeline<SkewTrack, SkewClip>.Apply(asset.Index, positions, next, true, effects);

        Assert.Equal(10f, effects[0]);
    }

    [Fact]
    public void FingerprintSeparatesReorderAndRetyping()
    {
        var reference = BakeFingerprint.Of(typeof(LayoutOrderTrack), typeof(SkewClip));
        Assert.NotEqual(reference, BakeFingerprint.Of(typeof(LayoutSwappedTrack), typeof(SkewClip)));
        Assert.NotEqual(reference, BakeFingerprint.Of(typeof(LayoutOrderTrack), typeof(SkewClipRetyped)));
    }

    [Fact]
    public void DigestSeparatesLayoutEditsInvisibleToNameFolding()
    {
        Assert.NotEqual(
            PairLayoutDigest.Of(typeof(SkewTrack), typeof(RetypeSbyteClip)),
            PairLayoutDigest.Of(typeof(SkewTrack), typeof(RetypeByteClip)));
        Assert.NotEqual(
            PairLayoutDigest.Of(typeof(SkewTrack), typeof(FixedEightClip)),
            PairLayoutDigest.Of(typeof(SkewTrack), typeof(FixedNineClip)));
        Assert.NotEqual(
            PairLayoutDigest.Of(typeof(PackOneTrack), typeof(SkewClip)),
            PairLayoutDigest.Of(typeof(PackTwoTrack), typeof(SkewClip)));
    }

    static byte[] BakeAgainstPreEditLayout(Type staleTrack, Type staleClip) => new DomainBaker
        {
            FingerprintOf = (track, clip) => BakeFingerprint.FoldOnly(track, clip) ^ PairLayoutDigest.Of(staleTrack, staleClip)
        }
        .Track<SkewTrack, SkewClip>(new SkewTrack(2))
        .Clip(0, 0, 4, new SkewClip(5))
        .Bake();

    [Theory]
    [InlineData(typeof(RetypeSbyteClip))]
    [InlineData(typeof(FixedEightClip))]
    public void BindRejectsAssetsBakedAgainstAnEditedClipLayout(Type staleClip)
    {
        var asset = Load(BakeAgainstPreEditLayout(typeof(SkewTrack), staleClip));

        var failure = Assert.Throws<ArgumentException>(() => Apply(asset));

        Assert.Contains("different field layout", failure.Message, StringComparison.Ordinal);
        Assert.Contains("rebake", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(PackOneTrack))]
    public void BindRejectsAssetsBakedAgainstAnEditedTrackLayout(Type staleTrack)
    {
        var asset = Load(BakeAgainstPreEditLayout(staleTrack, typeof(SkewClip)));

        var failure = Assert.Throws<ArgumentException>(() => Apply(asset));

        Assert.Contains("different field layout", failure.Message, StringComparison.Ordinal);
        Assert.Contains("rebake", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindAcceptsAssetsBakedAgainstTheLiveLayoutDigest()
    {
        var asset = Load(BakeAgainstPreEditLayout(typeof(SkewTrack), typeof(SkewClip)));
        var positions = new ushort[1];
        var next = new ushort[1];
        var effects = new float[1];

        Timeline<SkewTrack, SkewClip>.Apply(asset.Index, positions, next, true, effects);

        Assert.Equal(10f, effects[0]);
    }
}
