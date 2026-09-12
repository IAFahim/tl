using Xunit;

namespace Tl.Core.Tests;

public readonly record struct DualAlphaClip(int Value);

public readonly record struct DualBetaClip(float Amount);

public readonly record struct EchoClip(int Value);

public readonly record struct DualTrack(int Code) : IBlend<DualAlphaClip>, IBlend<DualBetaClip>
{
    public void Blend(in DualAlphaClip first, in DualAlphaClip second, float factor, out DualAlphaClip result)
        => result = factor < 0.5f ? first : second;

    public void Blend(in DualBetaClip first, in DualBetaClip second, float factor, out DualBetaClip result)
        => result = new DualBetaClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct EchoTrack(int Code) : IBlend<EchoClip>
{
    public void Blend(in EchoClip first, in EchoClip second, float factor, out EchoClip result) => result = first;
}

public readonly record struct PairRecord(char Pair, int Code, float Value, uint Tick, uint Game, int Track, long Cycle, FrameFlags Flags);

public unsafe class MultiPairTests
{
    internal static readonly List<PairRecord> Records = [];
    internal static bool Recording = true;

    static MultiPairTests()
    {
        PairRuntime<DualTrack, DualAlphaClip>.Consume(&DualAlphaExecute, &NoBind);
        PairRuntime<DualTrack, DualBetaClip>.Consume(&DualBetaExecute, &NoBind);
        PairRuntime<EchoTrack, EchoClip>.Consume(&EchoExecute, &NoBind);
    }

    internal static byte[] DualFixture() => new Baker()
        .Track<DualTrack, DualAlphaClip>(new DualTrack(1))
        .Track<EchoTrack, EchoClip>(new EchoTrack(5))
        .Track<DualTrack, DualBetaClip>(new DualTrack(2))
        .Clip(0, 0, 6, new DualAlphaClip(10))
        .Clip(0, 2, 8, new DualAlphaClip(30))
        .Clip(1, 0, 4, new EchoClip(7))
        .Clip(2, 1, 7, new DualBetaClip(1.5f))
        .Clip(2, 3, 8, new DualBetaClip(2.5f))
        .Bake();

    internal static List<PairRecord> ForwardOracle(uint gameTick) =>
    [
        new('A', 1, 10f, 0u, gameTick, 0, 0L, FrameFlags.TimelineStart | FrameFlags.ClipStart),
        new('E', 5, 7f, 0u, gameTick, 1, 0L, FrameFlags.TimelineStart | FrameFlags.ClipStart),
        new('A', 1, 10f, 1u, gameTick + 1u, 0, 0L, FrameFlags.None),
        new('E', 5, 7f, 1u, gameTick + 1u, 1, 0L, FrameFlags.None),
        new('B', 2, 1.5f, 1u, gameTick + 1u, 2, 0L, FrameFlags.ClipStart),
        new('A', 1, 10f, 2u, gameTick + 2u, 0, 0L, FrameFlags.None),
        new('E', 5, 7f, 2u, gameTick + 2u, 1, 0L, FrameFlags.None),
        new('B', 2, 1.5f, 2u, gameTick + 2u, 2, 0L, FrameFlags.None),
        new('A', 1, 10f, 3u, gameTick + 3u, 0, 0L, FrameFlags.None),
        new('E', 5, 7f, 3u, gameTick + 3u, 1, 0L, FrameFlags.ClipEnd),
        new('B', 2, 1.5f, 3u, gameTick + 3u, 2, 0L, FrameFlags.None),
        new('A', 1, 30f, 4u, gameTick + 4u, 0, 0L, FrameFlags.None),
        new('B', 2, 1.5f + (2.5f - 1.5f) * (1f / 3f), 4u, gameTick + 4u, 2, 0L, FrameFlags.None),
        new('A', 1, 30f, 5u, gameTick + 5u, 0, 0L, FrameFlags.None),
        new('B', 2, 1.5f + (2.5f - 1.5f) * (2f / 3f), 5u, gameTick + 5u, 2, 0L, FrameFlags.None),
        new('A', 1, 30f, 6u, gameTick + 6u, 0, 0L, FrameFlags.None),
        new('B', 2, 2.5f, 6u, gameTick + 6u, 2, 0L, FrameFlags.None),
        new('A', 1, 30f, 7u, gameTick + 7u, 0, 0L, FrameFlags.ClipEnd | FrameFlags.TimelineEnd | FrameFlags.CompletedAfter),
        new('B', 2, 2.5f, 7u, gameTick + 7u, 2, 0L, FrameFlags.ClipEnd | FrameFlags.TimelineEnd | FrameFlags.CompletedAfter),
    ];

    private static void NoBind(ulong* keys, int keyCount, byte* table)
    {
    }

    private static void DualAlphaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        if (!Recording) return;
        DualAlphaClip scratch = default;
        var frame = TickFrame.ToFrame<DualTrack, DualAlphaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new PairRecord('A', frame.Track.Code, frame.Clip.Value, tick, gameTick, frame.TrackIndex, cycle, frame.Flags));
    }

    private static void DualBetaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        if (!Recording) return;
        DualBetaClip scratch = default;
        var frame = TickFrame.ToFrame<DualTrack, DualBetaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new PairRecord('B', frame.Track.Code, frame.Clip.Amount, tick, gameTick, frame.TrackIndex, cycle, frame.Flags));
    }

    private static void EchoExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        if (!Recording) return;
        EchoClip scratch = default;
        var frame = TickFrame.ToFrame<EchoTrack, EchoClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new PairRecord('E', frame.Track.Code, frame.Clip.Value, tick, gameTick, frame.TrackIndex, cycle, frame.Flags));
    }

    [Fact]
    public void DualPairTimelineExecutesAuthoredOrderAndMirrorsBackward()
    {
        using var asset = TimelineAsset.Load(DualFixture());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);

        Records.Clear();
        query.Tick(100u, 8);
        Assert.Equal(ForwardOracle(100u), Records);
        Assert.Equal(8u, rows[0].Position);
        Assert.Equal(0L, rows[0].Cycle);

        Records.Clear();
        query.Tick(108u, -8);
        Assert.Equal(
        [
            new PairRecord('B', 2, 2.5f, 7u, 107u, 2, 0L, FrameFlags.Reverse | FrameFlags.ClipEnd | FrameFlags.TimelineEnd | FrameFlags.CompletedBefore),
            new PairRecord('A', 1, 30f, 7u, 107u, 0, 0L, FrameFlags.Reverse | FrameFlags.ClipEnd | FrameFlags.TimelineEnd | FrameFlags.CompletedBefore),
            new PairRecord('B', 2, 2.5f, 6u, 106u, 2, 0L, FrameFlags.Reverse),
            new PairRecord('A', 1, 30f, 6u, 106u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('B', 2, 1.5f + (2.5f - 1.5f) * (2f / 3f), 5u, 105u, 2, 0L, FrameFlags.Reverse),
            new PairRecord('A', 1, 30f, 5u, 105u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('B', 2, 1.5f + (2.5f - 1.5f) * (1f / 3f), 4u, 104u, 2, 0L, FrameFlags.Reverse),
            new PairRecord('A', 1, 30f, 4u, 104u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('B', 2, 1.5f, 3u, 103u, 2, 0L, FrameFlags.Reverse),
            new PairRecord('E', 5, 7f, 3u, 103u, 1, 0L, FrameFlags.Reverse | FrameFlags.ClipEnd),
            new PairRecord('A', 1, 10f, 3u, 103u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('B', 2, 1.5f, 2u, 102u, 2, 0L, FrameFlags.Reverse),
            new PairRecord('E', 5, 7f, 2u, 102u, 1, 0L, FrameFlags.Reverse),
            new PairRecord('A', 1, 10f, 2u, 102u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('B', 2, 1.5f, 1u, 101u, 2, 0L, FrameFlags.Reverse | FrameFlags.ClipStart),
            new PairRecord('E', 5, 7f, 1u, 101u, 1, 0L, FrameFlags.Reverse),
            new PairRecord('A', 1, 10f, 1u, 101u, 0, 0L, FrameFlags.Reverse),
            new PairRecord('E', 5, 7f, 0u, 100u, 1, 0L, FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('A', 1, 10f, 0u, 100u, 0, 0L, FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
        ], Records);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0L, rows[0].Cycle);
    }

    [Fact]
    public void DualPairLoopingAssetWrapsPositionAndCycle()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<DualTrack, DualAlphaClip>(new DualTrack(1))
            .Track<DualTrack, DualBetaClip>(new DualTrack(2))
            .Clip(0, 0, 2, new DualAlphaClip(4))
            .Clip(1, 0, 2, new DualBetaClip(2.5f))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);

        Records.Clear();
        query.Tick(10u, 3);
        Assert.Equal(
        [
            new PairRecord('A', 1, 4f, 0u, 10u, 0, 0L, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('B', 2, 2.5f, 0u, 10u, 1, 0L, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('A', 1, 4f, 1u, 11u, 0, 0L, FrameFlags.Looping | FrameFlags.TimelineEnd | FrameFlags.ClipEnd),
            new PairRecord('B', 2, 2.5f, 1u, 11u, 1, 0L, FrameFlags.Looping | FrameFlags.TimelineEnd | FrameFlags.ClipEnd),
            new PairRecord('A', 1, 4f, 0u, 12u, 0, 1L, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('B', 2, 2.5f, 0u, 12u, 1, 1L, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.ClipStart),
        ], Records);
        Assert.Equal(1u, rows[0].Position);
        Assert.Equal(1L, rows[0].Cycle);

        Records.Clear();
        query.Tick(13u, -3);
        Assert.Equal(
        [
            new PairRecord('B', 2, 2.5f, 0u, 12u, 1, 1L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('A', 1, 4f, 0u, 12u, 0, 1L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('B', 2, 2.5f, 1u, 11u, 1, 0L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineEnd | FrameFlags.ClipEnd),
            new PairRecord('A', 1, 4f, 1u, 11u, 0, 0L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineEnd | FrameFlags.ClipEnd),
            new PairRecord('B', 2, 2.5f, 0u, 10u, 1, 0L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
            new PairRecord('A', 1, 4f, 0u, 10u, 0, 0L, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.ClipStart),
        ], Records);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0L, rows[0].Cycle);
    }

    [Fact]
    public void WarmDualPairTickAllocatesNoManagedMemory()
    {
        using var asset = TimelineAsset.Load(DualFixture());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);

        for (var index = 0; index < 1_000; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);

        Recording = false;
        try
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100_000; index++)
                query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0, allocated);
        }
        finally
        {
            Recording = true;
        }

        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0L, rows[0].Cycle);
    }

    [Fact]
    public void DualPairFrameQueriesSeeOnlyTheirOwnPair()
    {
        using var asset = TimelineAsset.Load(DualFixture());
        var component = new TimelineComponent(asset.Reference) { Position = 4 };

        var alphas = new List<(int Code, float Value, int Track)>();
        foreach (var frame in Timeline.Query<DualTrack, DualAlphaClip>(in component))
            alphas.Add((frame.Track.Code, frame.Clip.Value, frame.TrackIndex));
        var single = Assert.Single(alphas);
        Assert.Equal((1, 30f, 0), (single.Code, single.Value, single.Track));

        var betas = new List<(int Code, float Amount, int Track)>();
        foreach (var frame in Timeline.Query<DualTrack, DualBetaClip>(in component))
            betas.Add((frame.Track.Code, frame.Clip.Amount, frame.TrackIndex));
        var beta = Assert.Single(betas);
        Assert.Equal((2, 1.5f + (2.5f - 1.5f) * (1f / 3f), 2), (beta.Code, beta.Amount, beta.Track));

        Assert.False(Timeline.Query<EchoTrack, EchoClip>(in component).MoveNext());
    }
}
