using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct FoldJumpClip(float Height);

public readonly record struct FoldJumpTrack(float Scale) : IBlend<FoldJumpClip>
{
    public void Blend(in FoldJumpClip first, in FoldJumpClip second, float factor, out FoldJumpClip result)
        => result = new FoldJumpClip(first.Height + (second.Height - first.Height) * factor);
}

public readonly record struct FoldSoundClip(float Loudness);

public readonly record struct FoldSoundTrack(float Gain) : IBlend<FoldSoundClip>
{
    public void Blend(in FoldSoundClip first, in FoldSoundClip second, float factor, out FoldSoundClip result)
        => result = new FoldSoundClip(first.Loudness + (second.Loudness - first.Loudness) * factor);
}

public readonly record struct FoldChimeClip(float Beep);

public readonly record struct FoldChimeTrack(float Gain) : IBlend<FoldChimeClip>
{
    public void Blend(in FoldChimeClip first, in FoldChimeClip second, float factor, out FoldChimeClip result)
        => result = new FoldChimeClip(first.Beep + (second.Beep - first.Beep) * factor);
}

internal static unsafe class FoldIsolationPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<FoldJumpTrack, FoldJumpClip>.Consume(&ExecuteJump, &BindFloat);
        PairRuntime<FoldSoundTrack, FoldSoundClip>.Consume(&ExecuteSound, &BindFloat);
        PairRuntime<FoldChimeTrack, FoldChimeClip>.Consume(&ExecuteChime, &BindFloat, TickPurity.WindowConstant);
    }

    internal static int JumpFolds;
    internal static int SoundFolds;
    internal static int ChimeFolds;

    internal static void Reset()
    {
        JumpFolds = 0;
        SoundFolds = 0;
        ChimeFolds = 0;
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

    private static void ExecuteJump(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        JumpFolds++;
        FoldJumpClip scratch = default;
        var frame = TickFrame.ToFrame<FoldJumpTrack, FoldJumpClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        var column = (float*)columns[0];
        if (column != null) column[row] += sign * frame.Track.Scale * frame.Clip.Height;
    }

    private static void ExecuteSound(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        SoundFolds++;
        FoldSoundClip scratch = default;
        var frame = TickFrame.ToFrame<FoldSoundTrack, FoldSoundClip>(slot, pair, tick, flags, ref scratch);
        var column = (float*)columns[0];
        if (column != null) column[row] += frame.Track.Gain * frame.Clip.Loudness;
    }

    private static void ExecuteChime(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ChimeFolds++;
        FoldChimeClip scratch = default;
        var frame = TickFrame.ToFrame<FoldChimeTrack, FoldChimeClip>(slot, pair, tick, flags, ref scratch);
        var column = (float*)columns[0];
        if (column != null) column[row] += frame.Track.Gain * frame.Clip.Beep;
    }
}

public unsafe class PairIsolatedFoldTests
{
    static byte[] JumpOnlyBake(float scale) => new Baker()
        .Track<FoldJumpTrack, FoldJumpClip>(new FoldJumpTrack(scale))
        .Clip(0, 0, 4, new FoldJumpClip(3))
        .Clip(0, 4, 8, new FoldJumpClip(5))
        .Bake();

    static byte[] LoopingJumpOnlyBake(float scale) => new Baker()
        .Track<FoldJumpTrack, FoldJumpClip>(new FoldJumpTrack(scale))
        .Clip(0, 0, 4, new FoldJumpClip(3))
        .Clip(0, 4, 8, new FoldJumpClip(5))
        .Looping()
        .Bake();

    static byte[] TwoPairBake(float jumpScale, float soundLoudness) => new Baker()
        .Track<FoldJumpTrack, FoldJumpClip>(new FoldJumpTrack(jumpScale))
        .Track<FoldSoundTrack, FoldSoundClip>(new FoldSoundTrack(1f))
        .Clip(0, 0, 4, new FoldJumpClip(3))
        .Clip(0, 4, 8, new FoldJumpClip(5))
        .Clip(1, 1, 3, new FoldSoundClip(soundLoudness))
        .Clip(1, 5, 8, new FoldSoundClip(soundLoudness * 2))
        .Bake();

    static byte[] ThreePairBake(float jumpScale, float soundLoudness, float chimeBeep) => new Baker()
        .Track<FoldJumpTrack, FoldJumpClip>(new FoldJumpTrack(jumpScale))
        .Track<FoldSoundTrack, FoldSoundClip>(new FoldSoundTrack(1f))
        .Track<FoldChimeTrack, FoldChimeClip>(new FoldChimeTrack(2f))
        .Clip(0, 0, 4, new FoldJumpClip(3))
        .Clip(0, 4, 8, new FoldJumpClip(5))
        .Clip(1, 1, 3, new FoldSoundClip(soundLoudness))
        .Clip(1, 5, 8, new FoldSoundClip(soundLoudness * 2))
        .Clip(2, 2, 6, new FoldChimeClip(chimeBeep))
        .Clip(2, 6, 8, new FoldChimeClip(chimeBeep * 2))
        .Bake();

    static byte[] ChimeOnlyBake(float chimeBeep) => new Baker()
        .Track<FoldChimeTrack, FoldChimeClip>(new FoldChimeTrack(2f))
        .Clip(0, 2, 6, new FoldChimeClip(chimeBeep))
        .Clip(0, 6, 8, new FoldChimeClip(chimeBeep * 2))
        .Bake();

    [Fact]
    public void FoldingEachPairOfAMultiPairAssetRunsOnlyThatPairsConsumers()
    {
        FoldIsolationPairs.Reset();
        using var lower = TimelineAsset.LoadAsset(ThreePairBake(2f, 7f, 1.25f));
        using var higher = TimelineAsset.LoadAsset(TwoPairBake(3f, 9f));

        Timeline<FoldJumpTrack, FoldJumpClip>.Apply(higher.Index, Span<ushort>.Empty, true, Span<float>.Empty);
        Assert.True(FoldIsolationPairs.JumpFolds > 0);
        Assert.Equal(0, FoldIsolationPairs.SoundFolds);
        Assert.Equal(0, FoldIsolationPairs.ChimeFolds);

        var jumpFoldsAfterPairAFold = FoldIsolationPairs.JumpFolds;
        Timeline<FoldSoundTrack, FoldSoundClip>.View(lower.Index);
        Assert.True(FoldIsolationPairs.SoundFolds > 0);
        Assert.Equal(jumpFoldsAfterPairAFold, FoldIsolationPairs.JumpFolds);
        Assert.Equal(0, FoldIsolationPairs.ChimeFolds);

        var soundFoldsAfterPairBFold = FoldIsolationPairs.SoundFolds;
        Timeline<FoldChimeTrack, FoldChimeClip>.View(lower.Index);
        Assert.True(FoldIsolationPairs.ChimeFolds > 0);
        Assert.Equal(jumpFoldsAfterPairAFold, FoldIsolationPairs.JumpFolds);
        Assert.Equal(soundFoldsAfterPairBFold, FoldIsolationPairs.SoundFolds);

        Timeline<FoldJumpTrack, FoldJumpClip>.Apply(higher.Index, Span<ushort>.Empty, true, Span<float>.Empty);
        Assert.Equal(jumpFoldsAfterPairAFold, FoldIsolationPairs.JumpFolds);
        Assert.Equal(soundFoldsAfterPairBFold, FoldIsolationPairs.SoundFolds);
        Assert.True(FoldIsolationPairs.ChimeFolds > 0);
    }

    [Fact]
    public void PairATablesExcludeColumnWritesFromOtherPairs()
    {
        FoldIsolationPairs.Reset();
        using var dual = TimelineAsset.LoadAsset(TwoPairBake(4f, 11f));
        using var single = TimelineAsset.LoadAsset(JumpOnlyBake(4f));

        var dualView = Timeline<FoldJumpTrack, FoldJumpClip>.View(dual.Index);
        var singleView = Timeline<FoldJumpTrack, FoldJumpClip>.View(single.Index);

        Assert.True(SameTables(&dualView, &singleView));
        Assert.Equal(12f, dualView.Forward[0]);
        Assert.Equal(20f, dualView.Forward[4]);
        Assert.Equal(0, FoldIsolationPairs.SoundFolds);
    }

    [Fact]
    public void WindowConstantPairFoldsStayIsolatedInMixedPrograms()
    {
        FoldIsolationPairs.Reset();
        using var dual = TimelineAsset.LoadAsset(ThreePairBake(5f, 13f, 1.5f));
        using var chimeOnly = TimelineAsset.LoadAsset(ChimeOnlyBake(1.5f));

        var dualChime = Timeline<FoldChimeTrack, FoldChimeClip>.View(dual.Index);
        var onlyChime = Timeline<FoldChimeTrack, FoldChimeClip>.View(chimeOnly.Index);

        Assert.True(SameTables(&dualChime, &onlyChime));
        Assert.Equal(3f, dualChime.Forward[2]);
        Assert.Equal(6f, dualChime.Forward[6]);
        Assert.Equal(0, FoldIsolationPairs.JumpFolds);
        Assert.Equal(0, FoldIsolationPairs.SoundFolds);
        Assert.True(FoldIsolationPairs.ChimeFolds > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SinglePairFoldsReproduceWholeAssetMeasurementBytes(int fixture)
    {
        FoldIsolationPairs.Reset();
        switch (fixture)
        {
            case 0:
            {
                using var asset = TimelineAsset.LoadAsset(JumpOnlyBake(6f));
                var view = Timeline<FoldJumpTrack, FoldJumpClip>.View(asset.Index);
                using var measured = MeasuredLanes.Measure(asset);
                Assert.True(SameTables(&view, measured));
                break;
            }
            case 1:
            {
                using var asset = TimelineAsset.LoadAsset(LoopingJumpOnlyBake(7f));
                var view = Timeline<FoldJumpTrack, FoldJumpClip>.View(asset.Index);
                using var measured = MeasuredLanes.Measure(asset);
                Assert.True(SameTables(&view, measured));
                break;
            }
            default:
            {
                using var asset = TimelineAsset.LoadAsset(ChimeOnlyBake(1f));
                var view = Timeline<FoldChimeTrack, FoldChimeClip>.View(asset.Index);
                using var measured = MeasuredLanes.Measure(asset);
                Assert.True(SameTables(&view, measured));
                break;
            }
        }
    }

    static bool SameTables(SlotView* left, SlotView* right)
    {
        if (left->Duration != right->Duration || left->Looping != right->Looping || left->TableTicks != right->TableTicks
            || left->Absent != right->Absent || left->RecordBytes != right->RecordBytes || left->AbiVersion != right->AbiVersion)
            return false;
        var floatBytes = checked((int)(left->TableTicks * sizeof(float)));
        var recordBytes = checked((int)(left->TableTicks * sizeof(LaneMovementRecord)));
        return new ReadOnlySpan<byte>(left->Forward, floatBytes).SequenceEqual(new ReadOnlySpan<byte>(right->Forward, floatBytes))
            && new ReadOnlySpan<byte>(left->Backward, floatBytes).SequenceEqual(new ReadOnlySpan<byte>(right->Backward, floatBytes))
            && new ReadOnlySpan<byte>(left->BackwardByPosition, floatBytes).SequenceEqual(new ReadOnlySpan<byte>(right->BackwardByPosition, floatBytes))
            && new ReadOnlySpan<byte>(left->ForwardRecords, recordBytes).SequenceEqual(new ReadOnlySpan<byte>(right->ForwardRecords, recordBytes))
            && new ReadOnlySpan<byte>(left->BackwardRecords, recordBytes).SequenceEqual(new ReadOnlySpan<byte>(right->BackwardRecords, recordBytes));
    }

    static bool SameTables(SlotView* view, MeasuredLanes measured)
    {
        if (view->Duration != measured.Duration || view->Looping != (ushort)(measured.Looping ? 1 : 0))
            return false;
        var floatBytes = checked((int)(view->TableTicks * sizeof(float)));
        return new ReadOnlySpan<byte>(view->Forward, floatBytes).SequenceEqual(new ReadOnlySpan<byte>(measured.Forward, floatBytes))
            && new ReadOnlySpan<byte>(view->Backward, floatBytes).SequenceEqual(new ReadOnlySpan<byte>(measured.Backward, floatBytes));
    }
}
