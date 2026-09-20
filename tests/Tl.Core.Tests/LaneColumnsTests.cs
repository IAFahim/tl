using System.Runtime.CompilerServices;
using Tl;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct LaneColumnsClip(float Amount);

public readonly record struct LaneColumnsTrack(float Scale) : IBlend<LaneColumnsClip>
{
    public void Blend(in LaneColumnsClip first, in LaneColumnsClip second, float factor, out LaneColumnsClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct TestIndex(ushort Value);

public record struct TestPosition(ushort Value);

public struct TestEffect { public float Value; }

readonly record struct WrongIndex(int Value);

internal static unsafe class LaneColumnsPairs
{
    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<LaneColumnsTrack, LaneColumnsClip>.Consume(&Execute, &BindFloat);

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneColumnsClip scratch = default;
        var frame = TickFrame.ToFrame<LaneColumnsTrack, LaneColumnsClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }
}

public class LaneColumnsTests
{
    const int N = 8;
    const int Ticks = 13;
    const ushort Duration = 6;

    static ushort BakeJump()
        => TimelineAsset.Load(new DomainBaker()
            .Track<LaneColumnsTrack, LaneColumnsClip>(new LaneColumnsTrack(2f))
            .Clip(0, 0u, 3u, new LaneColumnsClip(1f))
            .Clip(0, 3u, Duration, new LaneColumnsClip(-1f))
            .Bake());

    [Fact]
    public void SpanColumnsMatchRawTimelineApplyAndStep()
    {
        ushort asset = BakeJump();
        var ids = new TestIndex[N];
        var positions = new TestPosition[N];
        var effects = new TestEffect[N];
        for (var i = 0; i < N; i++)
        {
            ids[i] = new TestIndex(asset);
            positions[i] = new TestPosition((ushort)(i % Duration));
        }

        var rawIds = new ushort[N];
        var rawPos = new ushort[N];
        var rawFx = new float[N];
        for (var i = 0; i < N; i++)
        {
            rawIds[i] = asset;
            rawPos[i] = (ushort)(i % Duration);
        }

        for (var tick = 0; tick < Ticks; tick++)
        {
            Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(ids, positions, true, effects);
            Lane<LaneColumnsTrack, LaneColumnsClip>.Advance(ids, positions, true);
            Timeline<LaneColumnsTrack, LaneColumnsClip>.Apply(rawIds, rawPos, true, rawFx);
            Timeline.Advance(rawIds, rawPos, true);
        }

        for (var i = 0; i < N; i++)
        {
            Assert.Equal(rawPos[i], positions[i].Value);
            Assert.Equal(rawFx[i], effects[i].Value);
        }
    }

    [Fact]
    public void PerEntityMatchesSpanColumns()
    {
        ushort asset = BakeJump();
        var index = new TestIndex(asset);
        var position = new TestPosition(0);
        var effect = new TestEffect();

        var spanIds = new TestIndex[] { new(asset) };
        var spanPositions = new TestPosition[] { new(0) };
        var spanEffects = new TestEffect[1];

        for (var tick = 0; tick < Ticks; tick++)
        {
            Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(in index, ref position, true, ref effect);
            Lane<LaneColumnsTrack, LaneColumnsClip>.Advance(in index, ref position, true);
            Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(spanIds, spanPositions, true, spanEffects);
            Lane<LaneColumnsTrack, LaneColumnsClip>.Advance(spanIds, spanPositions, true);
        }

        Assert.Equal(spanPositions[0].Value, position.Value);
        Assert.Equal(spanEffects[0].Value, effect.Value);
    }

    [Fact]
    public void ApplyLeavesPositionsUntouchedUntilStep()
    {
        ushort asset = BakeJump();
        var ids = new TestIndex[N];
        var positions = new TestPosition[N];
        var effects = new TestEffect[N];
        for (var i = 0; i < N; i++)
        {
            ids[i] = new TestIndex(asset);
            positions[i] = new TestPosition((ushort)(i % Duration));
        }

        Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(ids, positions, true, effects);

        for (var i = 0; i < N; i++)
            Assert.Equal((ushort)(i % Duration), positions[i].Value);

        var rawIds = new ushort[N];
        var rawPos = new ushort[N];
        for (var i = 0; i < N; i++)
        {
            rawIds[i] = asset;
            rawPos[i] = (ushort)(i % Duration);
        }

        Lane<LaneColumnsTrack, LaneColumnsClip>.Advance(ids, positions, true);
        Timeline.Advance(rawIds, rawPos, true);

        for (var i = 0; i < N; i++)
            Assert.Equal(rawPos[i], positions[i].Value);
    }

    [Fact]
    public void PerEntityApplyLeavesPositionUntouchedUntilStep()
    {
        ushort asset = BakeJump();
        var index = new TestIndex(asset);
        var position = new TestPosition(3);
        var effect = new TestEffect();

        Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(in index, ref position, true, ref effect);

        Assert.Equal((ushort)3, position.Value);

        Lane<LaneColumnsTrack, LaneColumnsClip>.Advance(in index, ref position, true);

        var rawPos = new ushort[] { 3 };
        Timeline.Advance(new ushort[] { asset }, rawPos, true);
        Assert.Equal(rawPos[0], position.Value);
    }

    [Fact]
    public void WrongColumnSizeThrowsBeforePlayback()
    {
        var ids = new WrongIndex[1];
        var positions = new TestPosition[1];
        var effects = new TestEffect[1];
        Assert.Throws<ArgumentException>(
            () => Lane<LaneColumnsTrack, LaneColumnsClip>.Apply(ids, positions, true, effects));
    }
}
