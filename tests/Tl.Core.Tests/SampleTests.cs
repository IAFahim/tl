using System;
using System.Runtime.CompilerServices;
using Xunit;
using Tl.TestSupport;

namespace Tl.Core.Tests;

public partial struct RampState
{
    public float Rate;
}

public readonly record struct RampClip(float Rate);

public readonly partial struct RampTrack : IBlend<RampClip>, ITrack<RampTrack, RampClip>
{
    public readonly float Scale;

    public RampTrack(float scale) => Scale = scale;

    public void Blend(in RampClip first, in RampClip second, float factor, out RampClip result)
        => result = new RampClip(first.Rate + (second.Rate - first.Rate) * factor);
}

public sealed unsafe class SampleTests
{
    private static bool _called;
    private static float _rate;
    private static float _scale;
    private static ushort _tick;
    private static ushort _length;
    private static ushort _within;
    private static FrameFlags _flags;

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<RampTrack, RampClip>.ConsumeDispatch(&Row, &Keys, &Diag);
    }

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        var scratch = default(RampClip);
        var frame = TickFrame.ToFrame<RampTrack, RampClip>(slot, pair, tick, flags, ref scratch);
        _rate = frame.Clip.Rate;
        _scale = frame.Track.Scale;
        _tick = frame.TimelineTick;
        _length = frame.ClipLength;
        _within = frame.WithinClip;
        _flags = frame.Flags;
        _called = true;
    }

    private static int Keys(ulong* keys, byte* meta)
    {
        keys[0] = TypeKey<RampState>.Value;
        meta[0] = 4 | 32;
        return 1;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<RampTrack, RampClip> requires a RampState column.");

    private static byte[] RampBake() => new DomainBaker()
        .Track<RampTrack, RampClip>(new RampTrack(2f))
        .Clip(0, 0, 4, new RampClip(1f))
        .Clip(0, 6, 10, new RampClip(3f))
        .Bake();

    [Fact]
    public void Sample_is_bit_identical_to_the_apply_path_at_every_tick()
    {
        using var asset = TimelineAsset.LoadAsset(RampBake());
        var index = asset.Index;
        var state = new RampState[1];
        var ids = new ushort[1];
        var clocks = new ushort[1];
        ids[0] = index;
        var scratch = default(RampClip);

        for (ushort tick = 0; tick < 10; tick++)
        {
            _called = false;
            clocks[0] = tick;
            var columns = new ColumnSet();
            columns.Add<RampState>(state);
            Timeline<RampTrack, RampClip>.Apply<ushort, ushort>(ids, clocks, true, in columns);

            var sampled = Timeline<RampTrack, RampClip>.TrySample(index, tick, ref scratch, out var frame, true);
            Assert.Equal(_called, sampled);
            if (!sampled)
                continue;
            Assert.Equal(BitConverter.SingleToInt32Bits(_rate), BitConverter.SingleToInt32Bits(frame.Clip.Rate));
            Assert.Equal(BitConverter.SingleToInt32Bits(_scale), BitConverter.SingleToInt32Bits(frame.Track.Scale));
            Assert.Equal(_tick, frame.TimelineTick);
            Assert.Equal(_length, frame.ClipLength);
            Assert.Equal(_within, frame.WithinClip);
            Assert.Equal(_flags, frame.Flags);
            Assert.Equal(BitConverter.SingleToInt32Bits(_rate), BitConverter.SingleToInt32Bits(scratch.Rate));
        }
    }

    [Fact]
    public void SampleClip_matches_the_frame_and_reports_the_gap()
    {
        using var asset = TimelineAsset.LoadAsset(RampBake());
        var index = asset.Index;

        Assert.Equal(1f, Timeline<RampTrack, RampClip>.SampleClip(index, 0, true).Rate);
        Assert.Equal(3f, Timeline<RampTrack, RampClip>.SampleClip(index, 7, true).Rate);
        var scratch = default(RampClip);
        Assert.False(Timeline<RampTrack, RampClip>.TrySample(index, 4, ref scratch, out _, true));
        Assert.Throws<ArgumentException>(() => Timeline<RampTrack, RampClip>.SampleClip(index, 5, true));
    }

    [Fact]
    public void Sample_follows_the_public_advance_clock()
    {
        using var asset = TimelineAsset.LoadAsset(RampBake());
        var index = asset.Index;
        var clock = (ushort)0;
        var sampled = new System.Collections.Generic.HashSet<int>();
        var scratch = default(RampClip);

        for (var step = 0; step < 24; step++)
        {
            if (Timeline<RampTrack, RampClip>.TrySample(index, clock, ref scratch, out _, true))
                sampled.Add(clock);
            Timeline<RampTrack, RampClip>.Advance(index, ref clock, true);
        }

        Assert.Equal(new[] { 0, 1, 2, 3, 6, 7, 8, 9 }, sampled.Order());
    }

    [Fact]
    public void Sample_allocates_nothing_in_a_warm_loop()
    {
        using var asset = TimelineAsset.LoadAsset(RampBake());
        var index = asset.Index;

        _ = Timeline<RampTrack, RampClip>.SampleClip(index, 2, true);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 2_000; frame++)
        {
            _ = Timeline<RampTrack, RampClip>.SampleClip(index, 2, true);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
