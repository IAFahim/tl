using System.Buffers.Binary;
using Xunit;

using Tl.TestSupport;
using System.Diagnostics.CodeAnalysis;

namespace Tl.Core.Tests;

public readonly record struct AlphaClip(int Value);

public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = factor < 0.5f ? first : second;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct BetaClip(int Value);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct GammaClip(int Value);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct GammaTrack(int Code) : IBlend<GammaClip>
{
    public void Blend(in GammaClip first, in GammaClip second, float factor, out GammaClip result) => result = first;
}

public readonly record struct BlendClip(float Amount);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct BlendTrack(float Scale) : IBlend<BlendClip>
{
    public void Blend(in BlendClip first, in BlendClip second, float factor, out BlendClip result)
        => result = new BlendClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public struct Health
{
    public float Value;
}

public class DataTests
{

    private static byte[] FiniteBake() => new DomainBaker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
        .Clip(0, 0, 1, new AlphaClip(11))
        .Clip(0, 2, 3, new AlphaClip(22))
        .Bake();

    [Fact]
    public void LoadRejectsCorruptBlocks()
    {
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(FiniteBake()[..^1]));

        var magic = FiniteBake();
        magic[0] = (byte)'X';
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(magic));

        var version = FiniteBake();
        version[4] = 4;
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(version));

        var size = FiniteBake();
        size[48] = 7;
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(size));

        var misaligned = FiniteBake();
        misaligned[28] += 4;
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(misaligned));

        var unsorted = new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<GammaTrack, GammaClip>(new GammaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(1))
            .Clip(1, 0, 1, new GammaClip(1))
            .Bake();
        var pairOffset = BitConverter.ToUInt32(unsorted, 28);
        var first = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset));
        var second = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 48));
        if (first < second)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset), second);
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 48), first);
        }

        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(unsorted));
    }

    [Fact]
    public void DisposeFreesExactlyOnceAndIsIdempotent()
    {
        var beforeAlloc = TimelineTable.AllocCount;
        var asset = TimelineAsset.LoadAsset(FiniteBake());
        var allocated = TimelineTable.AllocCount - beforeAlloc;
        var beforeFree = TimelineTable.FreeCount;
        var beforeOverReleases = TimelineTable.OverReleases;
        var index = asset.Index;

        asset.Dispose();
        asset.Dispose();

        Assert.Equal(beforeAlloc + allocated, TimelineTable.AllocCount);
        Assert.Equal(beforeOverReleases, TimelineTable.OverReleases);
        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(index));
        TimelineTable.Drain();
        Assert.Equal(allocated, TimelineTable.FreeCount - beforeFree);
    }
    [Fact]
    public void QueryYieldsSingleClipFramesWithWindowFlags()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 1, 3, new AlphaClip(7))
            .Bake());
        var component = new TimelineComponent(asset.Reference) { Position = 3 };
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext());

        foreach (var position in new ushort[] { 1, 2 })
        {
            component.Position = position;
            var frames = new List<(int Code, int Value, ushort Tick, ushort ClipLength, ushort WithinClip, ushort Track, FrameFlags Flags)>();
            foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
                frames.Add((frame.Track.Code, frame.Clip.Value, frame.TimelineTick, frame.ClipLength, frame.WithinClip, frame.TrackIndex, frame.Flags));
            var single = Assert.Single(frames);
            Assert.Equal((5, 7, position, (ushort)2, (ushort)(position - 1), (ushort)0), (single.Code, single.Value, single.Tick, single.ClipLength, single.WithinClip, single.Track));
            Assert.True((single.Flags & FrameFlags.ClipStart) != 0 == (position == 1u));
            Assert.True((single.Flags & FrameFlags.ClipEnd) != 0 == (position == 2u));
        }

        Assert.Equal((ushort)2, component.Position);
    }

    [Fact]
    public void QueryExposesClipWindowsAcrossOverlappingClips()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 4, new BlendClip(0f))
            .Clip(0, 2, 6, new BlendClip(10f))
            .Bake());
        var component = new TimelineComponent(asset.Reference) { Position = 2 };
        var frame = SingleFrame(in component);
        Assert.Equal((ushort)6, frame.ClipLength);
        Assert.Equal((ushort)2, frame.WithinClip);
        Assert.Equal((ushort)2, frame.TimelineTick);
        Assert.False(frame.Has(FrameFlags.ClipStart));

        component.Position = 3;
        frame = SingleFrame(in component);
        Assert.Equal((ushort)6, frame.ClipLength);
        Assert.Equal((ushort)3, frame.WithinClip);
        Assert.Equal((ushort)3, frame.TimelineTick);

        component.Position = 0;
        frame = SingleFrame(in component);
        Assert.Equal((ushort)4, frame.ClipLength);
        Assert.Equal((ushort)0, frame.WithinClip);
        Assert.True(frame.Has(FrameFlags.ClipStart));
    }

    private static Frame<BlendTrack, BlendClip> SingleFrame(in TimelineComponent component)
    {
        foreach (var frame in Timeline.Query<BlendTrack, BlendClip>(in component))
            return frame;
        throw new InvalidOperationException("Expected one frame.");
    }

    [Fact]
    public void QueryYieldsBlendedFramesMatchingHandComputedFactor()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 4, new BlendClip(0f))
            .Clip(0, 2, 6, new BlendClip(10f))
            .Bake());
        var component = new TimelineComponent(asset.Reference) { Position = 2 };
        Assert.Equal(0f + (10f - 0f) * ((2u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 3;
        Assert.Equal(0f + (10f - 0f) * ((3u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 4;
        Assert.Equal(10f, SingleBlendAmount(in component));

        using var single = TimelineAsset.LoadAsset(new DomainBaker()
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
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
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

    [Fact]
    public void LegacyVersionTwoAssetsAreRejected()
    {
        var current = new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Track<BlendTrack, BlendClip>(new BlendTrack(2f))
            .Clip(0, 0, 2, new AlphaClip(11))
            .Clip(0, 2, 4, new AlphaClip(22))
            .Clip(1, 0, 4, new BlendClip(0f))
            .Clip(1, 2, 4, new BlendClip(10f))
            .Bake();
        var legacy = LegacyV2Layout(current);
        Assert.Equal(2u, BitConverter.ToUInt32(legacy, 4));
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(legacy));
    }

    private static byte[] LegacyV2Layout(byte[] current)
    {
        uint Word(int at) => BitConverter.ToUInt32(current, at);
        var frameOffset = (int)Word(40);
        var hotLength = (int)Word(44);
        var rowCount = (hotLength - frameOffset) / 24;
        var legacy = new byte[current.Length + rowCount * 8];
        Array.Copy(current, legacy, frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(4), 2u);
        BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(44), (uint)(hotLength + rowCount * 8));
        BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(48), (uint)(current.Length + rowCount * 8));
        var pairOffset = (int)Word(28);
        var pairCount = (int)Word(24);
        for (var index = 0; index < pairCount; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(pairOffset + 48 * index + 8), 32u);
        var stageOffset = (int)Word(32);
        var stageCount = (int)Word(20);
        for (var stage = 0; stage < stageCount; stage++)
        {
            var programOffset = (int)Word(stageOffset + 16 * stage + 8);
            var programCount = (int)Word(stageOffset + 16 * stage + 12);
            for (var step = 0; step < programCount; step++)
            {
                var slot = (int)Word(programOffset + 8 * step);
                BinaryPrimitives.WriteUInt32LittleEndian(legacy.AsSpan(programOffset + 8 * step), (uint)(frameOffset + (slot - frameOffset) / 24 * 32));
            }
        }
        for (var row = 0; row < rowCount; row++)
        {
            var from = frameOffset + row * 24;
            var at = frameOffset + row * 32;
            Array.Copy(current, from, legacy, at, 24);
            legacy[at + 6] = 0;
            legacy[at + 24] = current[from + 6];
        }
        return legacy;
    }
}
