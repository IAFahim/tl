using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct AlphaClip(int Value);

public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = factor < 0.5f ? first : second;
}

public readonly record struct BetaClip(int Value);

public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
}

public readonly record struct GammaClip(int Value);

public readonly record struct GammaTrack(int Code) : IBlend<GammaClip>
{
    public void Blend(in GammaClip first, in GammaClip second, float factor, out GammaClip result) => result = first;
}

public readonly record struct BlendClip(float Amount);

public readonly record struct BlendTrack(float Scale) : IBlend<BlendClip>
{
    public void Blend(in BlendClip first, in BlendClip second, float factor, out BlendClip result)
        => result = new BlendClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct PhiClip(int Value);

public readonly record struct PhiTrack(int Code) : IBlend<PhiClip>
{
    public void Blend(in PhiClip first, in PhiClip second, float factor, out PhiClip result) => result = first;
}

public readonly record struct DeltaClip(int Value);

public readonly record struct DeltaTrack(int Code) : IBlend<DeltaClip>
{
    public void Blend(in DeltaClip first, in DeltaClip second, float factor, out DeltaClip result) => result = first;
}

public struct Health
{
    public float Value;
}

public struct Resistance
{
    public float Scale;
}

public struct Marker
{
    public int Tag;
}

public struct RowAlias
{
    public long A, B, C;
}

public readonly record struct Record(char Kind, int Code, float Value, uint Tick, uint Game, long Cycle, FrameFlags Flags);

public unsafe class DataTests
{
    internal static readonly List<Record> Records = [];
    internal static bool MarkerRequired;

    private static byte[] FiniteBake() => new Baker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
        .Clip(0, 0, 1, new AlphaClip(11))
        .Clip(0, 2, 3, new AlphaClip(22))
        .Bake();

    [Fact]
    public void LoadRejectsCorruptBlocks()
    {
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(FiniteBake()[..^1]));

        var magic = FiniteBake();
        magic[0] = (byte)'X';
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(magic));

        var version = FiniteBake();
        version[4] = 2;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(version));

        var size = FiniteBake();
        size[44] = 7;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(size));

        var misaligned = FiniteBake();
        misaligned[28] += 4;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(misaligned));

        var unsorted = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<GammaTrack, GammaClip>(new GammaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(1))
            .Clip(1, 0, 1, new GammaClip(1))
            .Bake();
        var pairOffset = BitConverter.ToUInt32(unsorted, 28);
        var first = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset));
        var second = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 16));
        if (first < second)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset), second);
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 16), first);
        }

        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(unsorted));
    }

    [Fact]
    public void DisposeFreesExactlyOnceAndIsIdempotent()
    {
        var asset = TimelineAsset.Load(FiniteBake());
        var component = new TimelineComponent(asset.Reference);
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext() && component.Position > 0);
        asset.Dispose();
        asset.Dispose();
    }

    [Fact]
    public void QueryYieldsSingleClipFramesWithWindowFlags()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 1, 3, new AlphaClip(7))
            .Bake());
        var component = new TimelineComponent(asset.Reference);
        component.Position = 3;
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext());

        foreach (var position in new[] { 1u, 2u })
        {
            component.Position = position;
            var frames = new List<(int Code, int Value, uint Tick, uint Game, ushort Track, FrameFlags Flags)>();
            foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
                frames.Add((frame.Track.Code, frame.Clip.Value, frame.TimelineTick, frame.GameTick, frame.TrackIndex, frame.Flags));
            var single = Assert.Single(frames);
            Assert.Equal((5, 7, position, 0u, (ushort)0), (single.Code, single.Value, single.Tick, single.Game, single.Track));
            Assert.True((single.Flags & FrameFlags.ClipStart) != 0 == (position == 1u));
            Assert.True((single.Flags & FrameFlags.ClipEnd) != 0 == (position == 2u));
        }

        Assert.Equal(2u, component.Position);
        Assert.Equal(0, component.Cycle);
    }

    [Fact]
    public void QueryYieldsBlendedFramesMatchingHandComputedFactor()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 4, new BlendClip(0f))
            .Clip(0, 2, 6, new BlendClip(10f))
            .Bake());
        var component = new TimelineComponent(asset.Reference);

        component.Position = 2;
        Assert.Equal(0f + (10f - 0f) * ((2u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 3;
        Assert.Equal(0f + (10f - 0f) * ((3u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 4;
        Assert.Equal(10f, SingleBlendAmount(in component));

        using var single = TimelineAsset.Load(new Baker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 3, new BlendClip(0f))
            .Clip(0, 2, 4, new BlendClip(8f))
            .Bake());
        var one = new TimelineComponent(single.Reference) { Position = 2 };
        Assert.Equal(0f + (8f - 0f) * 0.5f, SingleBlendAmount(in one));
    }

    private static float SingleBlendAmount(in TimelineComponent component)
    {
        foreach (var frame in Timeline.Query<BlendTrack, BlendClip>(in component))
            return frame.Clip.Amount;
        throw new InvalidOperationException("Expected one blended frame.");
    }

    [Fact]
    public void QueryPreservesAuthoredTrackOrderAndSkipsEmptyStages()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(2))
            .Clip(0, 0, 1, new AlphaClip(10))
            .Clip(1, 0, 1, new BlendClip(4f))
            .Clip(2, 0, 1, new AlphaClip(20))
            .Bake());
        var component = new TimelineComponent(asset.Reference);

        var codes = new List<int>();
        foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
            codes.Add(frame.Track.Code);
        Assert.Equal([1, 2], codes);
        Assert.Equal(0u, component.Position);

        component.Position = 1;
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext());

        Assert.False(Timeline.Query<GammaTrack, GammaClip>(in component).MoveNext());
        var empty = default(TimelineComponent);
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in empty).MoveNext());
    }
}
