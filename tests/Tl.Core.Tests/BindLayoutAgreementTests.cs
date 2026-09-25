using System.Buffers.Binary;

using Tl.TestSupport;

using Xunit;

namespace Tl.Core.Tests;

public readonly record struct SkewClip(int Value);

public readonly record struct SkewTrack(int Code) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

public readonly record struct SkewClipRetyped(float Value);

public readonly record struct LayoutOrderTrack(int Code, float Scale) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

public readonly record struct LayoutSwappedTrack(float Scale, int Code) : IBlend<SkewClip>
{
    public void Blend(in SkewClip first, in SkewClip second, float factor, out SkewClip result) => result = first;
}

public unsafe class BindLayoutAgreementTests
{
    static BindLayoutAgreementTests()
    {
        PairRuntime<SkewTrack, SkewClip>.Consume(&ExecuteCode, &BindFloat);
        PairRuntime<SkewTrack, SkewClip>.VerifyLayout(BakeFingerprint.Of(typeof(SkewTrack), typeof(SkewClip)));
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
}
