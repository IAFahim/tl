using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct RestQuietClip(float Amount);

public readonly record struct RestQuietTrack(float Scale) : IBlend<RestQuietClip>
{
    public void Blend(in RestQuietClip first, in RestQuietClip second, float factor, out RestQuietClip result)
        => result = new RestQuietClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct RestWideClip(float Amount);

public readonly record struct RestWideTrack(float Scale) : IBlend<RestWideClip>
{
    public void Blend(in RestWideClip first, in RestWideClip second, float factor, out RestWideClip result)
        => result = new RestWideClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct RestLiveClip(float Amount);

public readonly record struct RestLiveTrack(float Scale) : IBlend<RestLiveClip>
{
    public void Blend(in RestLiveClip first, in RestLiveClip second, float factor, out RestLiveClip result)
        => result = new RestLiveClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class DeadIndexRestPairs
{
    internal static int LiveDispatches;

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<RestWideTrack, RestWideClip>.Consume(&FoldWide, &WideKeys, "tl\0FoldWide");
        PairRuntime<RestLiveTrack, RestLiveClip>.ConsumeDispatch(&OnLive, &LiveKeys, &LiveDiag, "tl\0RestLiveRow");
    }

    static int WideKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            keys[1] = TypeKey<int>.Value;
            meta[0] = 4 | 0x10;
            meta[1] = 4 | 0x10;
        }
        return 2;
    }

    static void FoldWide(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ((float*)columns[0])[row] = 1f + tick;
        ((int*)columns[1])[row] = 2 + tick;
    }

    static int LiveKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4;
        }
        return 1;
    }

    static void LiveDiag(ulong key, long slot)
        => throw new ArgumentException("Timeline<RestLiveTrack, RestLiveClip> requires a float column.");

    static void OnLive(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LiveDispatches++;
        ((float*)columns[0])[row] += 1f;
    }
}

public class DeadIndexRestTests
{
    static byte[] QuietBake() => new DomainBaker()
        .Track<RestQuietTrack, RestQuietClip>(new RestQuietTrack(2f))
        .Clip(0, 0u, 6u, new RestQuietClip(3f))
        .Bake();

    static byte[] WideBake() => new DomainBaker()
        .Track<RestWideTrack, RestWideClip>(new RestWideTrack(2f))
        .Clip(0, 0u, 6u, new RestWideClip(3f))
        .Bake();

    static byte[] LiveBake() => new DomainBaker()
        .Track<RestLiveTrack, RestLiveClip>(new RestLiveTrack(1f))
        .Clip(0, 0u, 6u, new RestLiveClip(1f))
        .Bake();

    static byte[] SoundBake() => new DomainBaker()
        .Track<DispatchSoundTrack, DispatchSoundClip>(new DispatchSoundTrack(1f))
        .Clip(0, 0u, 6u, new DispatchSoundClip(7))
        .Bake();

    [Fact]
    public void MovingAClockThrowsNothingForAConsumerlessPair()
    {
        using var asset = TimelineAsset.LoadAsset(QuietBake());
        var index = asset.Index;
        var clock = (ushort)0;

        Timeline<RestQuietTrack, RestQuietClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)1, clock);
        Timeline<RestQuietTrack, RestQuietClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)2, clock);
        Timeline<RestQuietTrack, RestQuietClip>.Advance(index, ref clock, false);
        Assert.Equal((ushort)1, clock);
    }

    [Fact]
    public void TrySampleReadsAFramesForAConsumerlessPair()
    {
        using var asset = TimelineAsset.LoadAsset(QuietBake());
        var scratch = default(RestQuietClip);

        Assert.True(Timeline<RestQuietTrack, RestQuietClip>.TrySample(asset.Index, 2, ref scratch, out var frame));
        Assert.Equal(2f, frame.Track.Scale);
        Assert.Equal(3f, frame.Clip.Amount);
        Assert.Equal((ushort)2, frame.TimelineTick);
        Assert.Equal((ushort)6, frame.ClipLength);
        Assert.Equal((ushort)2, frame.WithinClip);
    }

    [Fact]
    public void DeadIndexAddsNothingAndMovesNothingAcrossEveryFamily()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        var ids = new ushort[] { index, index };
        var positions = new ushort[] { 2, 3 };
        var effects = new float[2];
        var cells = new int[2];
        Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, true, effects);
        Timeline<RestWideTrack, RestWideClip>.Apply<ushort, ushort, int>(ids, positions, true, cells);
        var frozen = (float[])effects.Clone();
        var frozenCells = (int[])cells.Clone();

        asset.Dispose();

        Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, true, effects);
        Assert.Equal(frozen, effects);

        var next = new ushort[2];
        Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, next, true, effects);
        Assert.Equal(frozen, effects);
        Assert.Equal(positions, next);

        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)2, true, effects);
        Assert.Equal(frozen, effects);

        Timeline<RestWideTrack, RestWideClip>.ApplyChunk<ushort, ushort, float, int>(ids, positions, true, effects, cells);
        Assert.Equal(frozen, effects);
        Assert.Equal(frozenCells, cells);

        var rows = new int[] { 0, 1 };
        Timeline<RestWideTrack, RestWideClip>.Apply(rows, ids, positions, true, effects);
        Assert.Equal(frozen, effects);

        var clock = (ushort)2;
        Timeline<RestWideTrack, RestWideClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)2, clock);

        var clocks = new ushort[] { 2, 3 };
        Timeline.Advance(ids, clocks, true);
        Assert.Equal(new ushort[] { 2, 3 }, clocks);

        var rowClocks = new ushort[] { 2, 3 };
        Timeline<RestWideTrack, RestWideClip>.Advance(rows, ids, rowClocks, true);
        Assert.Equal(new ushort[] { 2, 3 }, rowClocks);
    }

    [Fact]
    public void TheUnboundedReAddIsGoneAfterTheFinalDispose()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        var ids = new ushort[16];
        var positions = new ushort[16];
        var effects = new float[16];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = index;
            positions[i] = (ushort)(i % 6);
        }
        Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, true, effects);
        asset.Dispose();
        var frozen = (float[])effects.Clone();

        for (var frame = 0; frame < 100; frame++)
        {
            Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, true, effects);
            Timeline.Advance(ids, positions, true);
        }

        Assert.Equal(frozen, effects);
        for (var i = 0; i < positions.Length; i++)
            Assert.Equal((ushort)(i % 6), positions[i]);
    }

    [Fact]
    public void RevivingIdenticalContentRebindsTheParkedRestView()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        var effects = new float[2];
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)2, true, effects);
        Assert.Equal(new float[] { 3f, 3f }, effects);

        asset.Dispose();
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)2, true, effects);
        Assert.Equal(new float[] { 3f, 3f }, effects);
        var clock = (ushort)2;
        Timeline<RestWideTrack, RestWideClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)2, clock);

        using var revived = TimelineAsset.LoadAsset(WideBake());
        Assert.Equal(index, revived.Index);
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)2, true, effects);
        Assert.Equal(new float[] { 6f, 6f }, effects);
        clock = 2;
        Timeline<RestWideTrack, RestWideClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)3, clock);
    }

    [Fact]
    public void AParkedDeadViewReportsTheAbiItWasBuiltAgainst()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)0, true, new float[1]);
        asset.Dispose();
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)0, true, new float[1]);

        var view = Inspection.Bank<RestWideTrack, RestWideClip>()!.Views[index];

        Assert.Equal(Inspection.FoldState.Folded, view.State);
        Assert.Equal((ushort)0, view.Duration);
        Assert.Equal((int)SlotView.AbiVersionV4, view.AbiVersion);
    }

    [Fact]
    public void DeadIndexDispatchesNothingAndThrowsNothing()
    {
        using var asset = TimelineAsset.LoadAsset(SoundBake());
        var index = asset.Index;
        var ids = new ushort[] { index };
        var positions = new ushort[] { 2 };
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(ids, positions, true);
        Assert.True(DispatchLog.HitCount > 0);

        asset.Dispose();

        var hits = DispatchLog.HitCount;
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(ids, positions, true);
        Assert.Equal(hits, DispatchLog.HitCount);
    }

    [Fact]
    public void DeadIndexLiveColumnsStayQuiet()
    {
        using var asset = TimelineAsset.LoadAsset(LiveBake());
        var index = asset.Index;
        var ids = new ushort[] { index };
        var positions = new ushort[] { 2 };
        var effects = new float[1];
        var columns = new ColumnSet();
        columns.Add(effects.AsSpan());
        DeadIndexRestPairs.LiveDispatches = 0;
        Timeline<RestLiveTrack, RestLiveClip>.Apply(ids, positions, true, in columns);
        Assert.Equal(1, DeadIndexRestPairs.LiveDispatches);
        var played = effects[0];

        asset.Dispose();

        Timeline<RestLiveTrack, RestLiveClip>.Apply(ids, positions, true, in columns);
        Assert.Equal(1, DeadIndexRestPairs.LiveDispatches);
        Assert.Equal(played, effects[0]);
    }

    [Fact]
    public void ViewOnADeadIndexThrowsTheLocatedDiagnostic()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        _ = Timeline<RestWideTrack, RestWideClip>.View(index);

        asset.Dispose();

        var thrown = Assert.Throws<ArgumentException>(() => Timeline<RestWideTrack, RestWideClip>.View(index));
        Assert.Contains($"Timeline index {index} is not loaded", thrown.Message);
    }

    [Fact]
    public void NeverLoadedIdsKeepTheLocatedThrow()
    {
        var thrown = Assert.Throws<ArgumentException>(() =>
            Timeline<RestWideTrack, RestWideClip>.Apply([ushort.MaxValue], [(ushort)0], true, new float[1]));
        Assert.Contains("is not loaded", thrown.Message);
    }

#if TL_CHECKED
    [Fact]
    public void CheckedBuildsAcceptDeadIndicesAtAnyPosition()
    {
        using var asset = TimelineAsset.LoadAsset(WideBake());
        var index = asset.Index;
        Timeline<RestWideTrack, RestWideClip>.Apply(index, (ushort)0, true, new float[1]);
        asset.Dispose();

        var ids = new ushort[] { index, index };
        var positions = new ushort[] { 40, ushort.MaxValue };
        Timeline<RestWideTrack, RestWideClip>.Apply(ids, positions, true, new float[2]);

        var clock = (ushort)40;
        Timeline<RestWideTrack, RestWideClip>.Advance(index, ref clock, true);
        Assert.Equal((ushort)40, clock);
    }
#endif
}
