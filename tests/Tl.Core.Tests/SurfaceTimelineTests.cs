using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct SurfaceClip(float Value);

public readonly record struct SurfaceTrack(float Scale) : IBlend<SurfaceClip>
{
    public void Blend(in SurfaceClip first, in SurfaceClip second, float factor, out SurfaceClip result)
        => result = factor < 0.5f ? first : second;
}

public unsafe class SurfaceTimelineTests
{
    static int _dispatchBakes;
    static int _dispatchInt;
    static long _dispatchLong;
    static float _dispatchFloat;
    static double _dispatchDouble;

    static byte[] SingleBake() => new DomainBaker()
        .Track<SurfaceTrack, SurfaceClip>(new SurfaceTrack(1f))
        .Clip(0, 0, 6, new SurfaceClip(42f))
        .Bake();

    static byte[] WindowBake() => new DomainBaker()
        .Track<SurfaceTrack, SurfaceClip>(new SurfaceTrack(1f))
        .Clip(0, 5, 10, new SurfaceClip(42f))
        .Bake();

    static byte[] FiveStageBake()
    {
        var baker = new DomainBaker()
            .Track<SurfaceTrack, SurfaceClip>(new SurfaceTrack(1f));
        for (uint i = 0; i < 5; i++)
            baker.Clip(0, i, i + 1, new SurfaceClip(i));
        return baker.Bake();
    }

    [Fact]
    public void AssetAdvanceViewForwardsAndBackwards()
    {
        using var asset = TimelineAsset.LoadAsset(SingleBake());
        var next = new ushort[3];
        Timeline.Advance(asset, new ushort[] { 0, 1, 5 }, next, true);
        Assert.Equal(new ushort[] { 1, 2, 6 }, next);
        Timeline.Advance(asset, new ushort[] { 0, 1, 6 }, next, false);
        Assert.Equal(new ushort[] { 0, 0, 5 }, next);
    }

    [Fact]
    public void AssetAdvanceViewRejectsNullAsset()
    {
        Assert.Throws<ArgumentNullException>(() => Timeline.Advance((TimelineAsset)null!, new ushort[1], new ushort[1], true));
    }

    [Fact]
    public void TickFrameWrapsARowForTypedFrameReading()
    {
        using var view = TimelineAsset.LoadAsset(WindowBake());
        var p = (byte*)view.Reference.Address;
        var header = *(NativeHeader*)p;
        var pairs = (NativePair*)(p + header.PairOffset);
        var stages = view.Reference.Stages;
        NativeStage chosen = default;
        for (var i = 0; i < (int)header.StageCount; i++)
            if (stages[i].ProgramCount > 0) { chosen = stages[i]; break; }
        var step = *(NativeStep*)(p + chosen.ProgramOffset);

        var tickFrame = new TickFrame((SlotRow*)(p + step.Slot), (byte*)(pairs + step.Pair), 6, FrameFlags.None);
        var scratch = default(SurfaceClip);
        var frame = tickFrame.ToFrame<SurfaceTrack, SurfaceClip>(ref scratch);

        Assert.Equal(42f, frame.Clip.Value);
        Assert.Equal(6, frame.TimelineTick);
        Assert.Equal(5, frame.ClipLength);
        Assert.Equal(1, frame.WithinClip);
        Assert.Equal(0, frame.TrackIndex);
        Assert.False(frame.Has(FrameFlags.ClipStart));
        Assert.False(frame.Has(FrameFlags.ClipEnd));
    }

    [Fact]
    public void QueryPastTheFinalStageYieldsNoFrames()
    {
        using var view = TimelineAsset.LoadAsset(FiveStageBake());
        var component = new TimelineComponent(view.Reference) { Position = 5 };
        var seen = 0;
        foreach (var frame in Timeline.Query<SurfaceTrack, SurfaceClip>(component))
            seen++;
        Assert.Equal(0, seen);
    }

    [Fact]
    public void QueryInsideAStageYieldsTheTypedFrame()
    {
        using var view = TimelineAsset.LoadAsset(FiveStageBake());
        var component = new TimelineComponent(view.Reference) { Position = 2 };
        var seen = 0;
        foreach (var frame in Timeline.Query<SurfaceTrack, SurfaceClip>(component))
        {
            seen++;
            Assert.Equal(2f, frame.Clip.Value);
            Assert.Equal(1, frame.ClipLength);
            Assert.Equal(0, frame.WithinClip);
        }
        Assert.Equal(1, seen);
    }

    static void FourArgumentSink(byte** arguments)
    {
        _dispatchInt = *(int*)arguments[0];
        _dispatchLong = *(long*)arguments[1];
        _dispatchFloat = *(float*)arguments[2];
        _dispatchDouble = *(double*)arguments[3];
        _dispatchBakes++;
    }

    [Fact]
    public void BakeDispatchBindsFourTypedArgumentsByTypeKey()
    {
        BakeRuntime<SurfaceTrack, SurfaceClip>.Bake(
            &FourArgumentSink,
            TypeKey<int>.Value, TypeKey<long>.Value, TypeKey<float>.Value, TypeKey<double>.Value);
        using var view = TimelineAsset.LoadAsset(SingleBake());
        var before = _dispatchBakes;

        Timeline.Bake(view.Index, 41, 7L, 2.5f, 1.25);

        Assert.Equal(before + 1, _dispatchBakes);
        Assert.Equal(41, _dispatchInt);
        Assert.Equal(7L, _dispatchLong);
        Assert.Equal(2.5f, _dispatchFloat);
        Assert.Equal(1.25, _dispatchDouble);
    }
}
