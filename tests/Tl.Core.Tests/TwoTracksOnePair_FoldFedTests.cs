using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct TwoTrackFoldClip(float Amount);

public readonly record struct TwoTrackFoldTrack(float Scale) : IBlend<TwoTrackFoldClip>
{
    public void Blend(in TwoTrackFoldClip first, in TwoTrackFoldClip second, float factor, out TwoTrackFoldClip result)
        => result = new TwoTrackFoldClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class TwoTrackFoldConsumers
{
    internal static int RowRuns;

    [ModuleInitializer]
    internal static void Install()
    {
        var fold = PairRuntime<TwoTrackFoldTrack, TwoTrackFoldClip>.Consume(&Fold, &FoldKeys, "tl\0TwoTrackFold");
        PairRuntime<TwoTrackFoldTrack, TwoTrackFoldClip>.ConsumeDispatch(&Row, &RowKeys, &Diag, "tl\0TwoTrackRow", fold);
    }

    private static int FoldKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x10; }
        return 1;
    }

    private static void Fold(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        TwoTrackFoldClip scratch = default;
        var frame = TickFrame.ToFrame<TwoTrackFoldTrack, TwoTrackFoldClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] = sign * frame.Track.Scale * frame.Clip.Amount;
    }

    private static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x40;
            keys[1] = TypeKey<float>.Value; meta[1] = 4;
        }
        return 2;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<TwoTrackFoldTrack, TwoTrackFoldClip> requires its named fold feed.");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        RowRuns++;
        ((float*)columns[1])[row] += *(float*)columns[0];
    }
}

public sealed unsafe class TwoTracksOnePair_FoldFed_RecordsCurrentBehavior
{
    static byte[] Bake() => new DomainBaker()
        .Track<TwoTrackFoldTrack, TwoTrackFoldClip>(new TwoTrackFoldTrack(2f))
        .Clip(0, 0, 6, new TwoTrackFoldClip(3f))
        .Track<TwoTrackFoldTrack, TwoTrackFoldClip>(new TwoTrackFoldTrack(10f))
        .Clip(1, 2, 8, new TwoTrackFoldClip(5f))
        .Bake();

    static float LaneAt(ushort index, ushort position, bool forward)
    {
        var effects = new float[1];
        Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.Apply([index], [position], forward, effects);
        return effects[0];
    }

    static (float Effect, int Runs) ComposeAt(ushort index, ushort position, bool forward)
    {
        TwoTrackFoldConsumers.RowRuns = 0;
        var effects = new float[1];
        var columns = new ColumnSet();
        columns.Add(effects);
        Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.Apply([index], [position], forward, in columns);
        return (effects[0], TwoTrackFoldConsumers.RowRuns);
    }

    [Fact]
    public void FoldLaneKeepsOneValuePerTick()
    {
        using var asset = TimelineAsset.LoadAsset(Bake());
        Assert.Equal(8u, Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.View(asset.Index).Duration);
        float[] forward = [6, 6, 50, 50, 50, 50, 50, 50, 0];
        float[] backward = [0, -6, -6, -6, -6, -6, -6, -50, -50];
        for (var position = 0; position <= 8; position++)
        {
            Assert.Equal(forward[position], LaneAt(asset.Index, (ushort)position, true));
            Assert.Equal(backward[position], LaneAt(asset.Index, (ushort)position, false));
        }
    }

    [Fact]
    public void ComposeRunsOncePerActiveStepWithTheSharedLane()
    {
        using var asset = TimelineAsset.LoadAsset(Bake());
        float[] forward = [6, 6, 100, 100, 100, 100, 50, 50, 0];
        float[] backward = [0, -6, -6, -12, -12, -12, -12, -50, -50];
        int[] forwardRuns = [1, 1, 2, 2, 2, 2, 1, 1, 0];
        int[] backwardRuns = [0, 1, 1, 2, 2, 2, 2, 1, 1];
        for (var position = 0; position <= 8; position++)
        {
            var ahead = ComposeAt(asset.Index, (ushort)position, true);
            Assert.Equal(forward[position], ahead.Effect);
            Assert.Equal(forwardRuns[position], ahead.Runs);
            var behind = ComposeAt(asset.Index, (ushort)position, false);
            Assert.Equal(backward[position], behind.Effect);
            Assert.Equal(backwardRuns[position], behind.Runs);
        }
    }

    [Fact]
    public void OverlapDoublesTheSharedLaneInsteadOfComposingEachTrack()
    {
        using var asset = TimelineAsset.LoadAsset(Bake());
        foreach (var position in new ushort[] { 2, 3, 4, 5 })
        {
            var lane = LaneAt(asset.Index, position, true);
            var shared = ComposeAt(asset.Index, position, true);
            Assert.Equal(50f, lane);
            Assert.Equal(2, shared.Runs);
            Assert.Equal(2 * lane, shared.Effect);
        }
        foreach (var position in new ushort[] { 3, 4, 5, 6 })
        {
            var lane = LaneAt(asset.Index, position, false);
            var shared = ComposeAt(asset.Index, position, false);
            Assert.Equal(-6f, lane);
            Assert.Equal(2, shared.Runs);
            Assert.Equal(2 * lane, shared.Effect);
        }
    }

    [Fact]
    public void TrySampleReturnsOnlyTheFirstMatchingStep()
    {
        using var asset = TimelineAsset.LoadAsset(Bake());
        foreach (var position in new ushort[] { 7, 8 })
        {
            var scratch = default(TwoTrackFoldClip);
            Assert.True(Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.TrySample(asset.Index, position, ref scratch, out var frame, false));
            Assert.Equal(10f, frame.Track.Scale);
            Assert.Equal(5f, frame.Clip.Amount);
        }
        foreach (var position in new ushort[] { 0, 1, 2, 3, 4, 5 })
        {
            var scratch = default(TwoTrackFoldClip);
            Assert.True(Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.TrySample(asset.Index, position, ref scratch, out var frame, true));
            Assert.Equal(2f, frame.Track.Scale);
            Assert.Equal(3f, frame.Clip.Amount);
        }
        foreach (var position in new ushort[] { 6, 7 })
        {
            var scratch = default(TwoTrackFoldClip);
            Assert.True(Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.TrySample(asset.Index, position, ref scratch, out var frame, true));
            Assert.Equal(10f, frame.Track.Scale);
            Assert.Equal(5f, frame.Clip.Amount);
        }
        foreach (var position in new ushort[] { 1, 2, 3, 4, 5, 6 })
        {
            var scratch = default(TwoTrackFoldClip);
            Assert.True(Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.TrySample(asset.Index, position, ref scratch, out var frame, false));
            Assert.Equal(2f, frame.Track.Scale);
            Assert.Equal(3f, frame.Clip.Amount);
        }
        foreach (var position in new ushort[] { 0, 8 })
        {
            var scratch = default(TwoTrackFoldClip);
            Assert.False(Timeline<TwoTrackFoldTrack, TwoTrackFoldClip>.TrySample(asset.Index, position, ref scratch, out _, position == 8));
        }
    }
}
