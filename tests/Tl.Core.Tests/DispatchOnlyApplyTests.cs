using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct DispatchJumpClip(float Height);
public readonly record struct DispatchJumpTrack(float Scale) : IBlend<DispatchJumpClip>
{
    public void Blend(in DispatchJumpClip first, in DispatchJumpClip second, float factor, out DispatchJumpClip result)
        => result = new DispatchJumpClip(first.Height + (second.Height - first.Height) * factor);
}
public readonly record struct DispatchSoundClip(ushort Code);
public readonly record struct DispatchSoundTrack(float Gain) : IBlend<DispatchSoundClip>
{
    public void Blend(in DispatchSoundClip first, in DispatchSoundClip second, float factor, out DispatchSoundClip result)
        => result = new DispatchSoundClip(first.Code);
}
public readonly record struct DispatchBellClip(float Tone);
public readonly record struct DispatchBellTrack(float Gain) : IBlend<DispatchBellClip>
{
    public void Blend(in DispatchBellClip first, in DispatchBellClip second, float factor, out DispatchBellClip result)
        => result = new DispatchBellClip(first.Tone + (second.Tone - first.Tone) * factor);
}
public readonly record struct DispatchDualClip(float V);
public readonly record struct DispatchDualTrack(float Scale) : IBlend<DispatchDualClip>
{
    public void Blend(in DispatchDualClip first, in DispatchDualClip second, float factor, out DispatchDualClip result)
        => result = new DispatchDualClip(first.V + (second.V - first.V) * factor);
}

internal static unsafe class DispatchLog
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<DispatchSoundTrack, DispatchSoundClip>.ConsumeDispatch(&OnSound);
        PairRuntime<DispatchDualTrack, DispatchDualClip>.ConsumeDispatch(&OnDual);
        PairRuntime<DispatchDualTrack, DispatchDualClip>.Consume(&OnDualWrite, &BindFloat);
        PairRuntime<DispatchJumpTrack, DispatchJumpClip>.Consume(&OnJump, &BindFloat);
        PairRuntime<DispatchBellTrack, DispatchBellClip>.Consume(&OnBell, &BindFloat);
    }

    internal static readonly (ushort Tick, ushort Code, bool Backward)[] Hits = new (ushort, ushort, bool)[4096];
    internal static int HitCount;
    internal static int JumpMeasures;
    internal static int DualWrites;

    internal static void Reset()
    {
        HitCount = 0;
        JumpMeasures = 0;
        DualWrites = 0;
    }

    internal static ushort[] Ticks()
    {
        var ticks = new ushort[HitCount];
        for (var i = 0; i < HitCount; i++) ticks[i] = Hits[i].Tick;
        return ticks;
    }

    internal static (ushort Tick, ushort Code, bool Backward)[] Trail()
    {
        var trail = new (ushort Tick, ushort Code, bool Backward)[HitCount];
        Array.Copy(Hits, trail, HitCount);
        return trail;
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

    private static void OnSound(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        DispatchSoundClip scratch = default;
        var frame = TickFrame.ToFrame<DispatchSoundTrack, DispatchSoundClip>(slot, pair, tick, flags, ref scratch);
        if (HitCount < Hits.Length) Hits[HitCount++] = (frame.TimelineTick, frame.Clip.Code, frame.IsBackward);
    }

    private static void OnDual(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        DispatchDualClip scratch = default;
        var frame = TickFrame.ToFrame<DispatchDualTrack, DispatchDualClip>(slot, pair, tick, flags, ref scratch);
        if (HitCount < Hits.Length) Hits[HitCount++] = (frame.TimelineTick, (ushort)(frame.Clip.V * 10), frame.IsBackward);
    }

    private static void OnDualWrite(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        DualWrites++;
        DispatchDualClip scratch = default;
        var frame = TickFrame.ToFrame<DispatchDualTrack, DispatchDualClip>(slot, pair, tick, flags, ref scratch);
        var column = (float*)columns[0];
        if (column != null) column[row] += frame.Track.Scale * frame.Clip.V;
    }

    private static void OnJump(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        JumpMeasures++;
        DispatchJumpClip scratch = default;
        var frame = TickFrame.ToFrame<DispatchJumpTrack, DispatchJumpClip>(slot, pair, tick, flags, ref scratch);
        var column = (float*)columns[0];
        if (column != null) column[row] += frame.Track.Scale * frame.Clip.Height;
    }

    private static void OnBell(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        DispatchBellClip scratch = default;
        var frame = TickFrame.ToFrame<DispatchBellTrack, DispatchBellClip>(slot, pair, tick, flags, ref scratch);
        var column = (float*)columns[0];
        if (column != null) column[row] += frame.Track.Gain * frame.Clip.Tone;
    }
}

public unsafe class DispatchOnlyApplyTests
{
    internal static byte[] TwoPairBake() => new Baker()
        .Track<DispatchJumpTrack, DispatchJumpClip>(new DispatchJumpTrack(2f))
        .Track<DispatchSoundTrack, DispatchSoundClip>(new DispatchSoundTrack(1f))
        .Clip(0, 0, 4, new DispatchJumpClip(3))
        .Clip(0, 4, 8, new DispatchJumpClip(5))
        .Clip(1, 1, 3, new DispatchSoundClip(11))
        .Clip(1, 5, 8, new DispatchSoundClip(22))
        .Bake();

    static byte[] JumpOnlyBake() => new Baker()
        .Track<DispatchJumpTrack, DispatchJumpClip>(new DispatchJumpTrack(2f))
        .Clip(0, 0, 4, new DispatchJumpClip(3))
        .Clip(0, 4, 8, new DispatchJumpClip(5))
        .Bake();

    static byte[] DualShapeBake() => new Baker()
        .Track<DispatchDualTrack, DispatchDualClip>(new DispatchDualTrack(2f))
        .Clip(0, 2, 6, new DispatchDualClip(1f))
        .Bake();

    static byte[] LoopingSoundBake() => new Baker()
        .Track<DispatchSoundTrack, DispatchSoundClip>(new DispatchSoundTrack(1f))
        .Clip(0, 0, 4, new DispatchSoundClip(11))
        .Clip(0, 4, 8, new DispatchSoundClip(22))
        .Looping()
        .Bake();

    [Fact]
    public void DispatchApplyFiresOnlyTheWindowsCoveringEachRowPosition()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        ushort[] indices = [asset.Index, asset.Index, asset.Index, asset.Index, asset.Index, asset.Index];
        ushort[] positions = [0, 1, 2, 3, 5, 7];
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, positions, true);
        Assert.Equal(
            [(ushort)1, (ushort)2, (ushort)5, (ushort)7],
            DispatchLog.Ticks());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.False(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void DispatchApplyBackwardFiresTheRewoundIntoWindowWithReverseFlag()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        ushort[] indices = [asset.Index, asset.Index];
        ushort[] positions = [3, 8];
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, positions, false);
        Assert.Equal(
            [(ushort)2, (ushort)7],
            DispatchLog.Ticks());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.True(DispatchLog.Hits[i].Backward);
        
    }

    [Fact]
    public void DispatchOnlyConsumerNeverFiresAtFoldOrMeasure()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        var effects = new float[4];
        var positions = new ushort[] { 1, 2, 5, 7 };
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, positions, true, effects);
        Assert.Equal([0f, 0f, 0f, 0f], effects);
        Assert.Equal(0, DispatchLog.HitCount);
    }

    [Fact]
    public void MeasuredTableOfAPairIsUnaffectedByDispatchOnlyConsumers()
    {
        DispatchLog.Reset();
        using var dual = TimelineAsset.LoadAsset(TwoPairBake());
        using var single = TimelineAsset.LoadAsset(JumpOnlyBake());
        var positions = new ushort[] { 0, 1, 3, 4, 5, 7 };
        var dualFx = new float[positions.Length];
        var singleFx = new float[positions.Length];
        Timeline<DispatchJumpTrack, DispatchJumpClip>.Apply(dual.Index, positions, true, dualFx);
        Timeline<DispatchJumpTrack, DispatchJumpClip>.Apply(single.Index, positions, true, singleFx);
        Assert.Equal(singleFx, dualFx);
        Assert.Equal([6f, 6f, 6f, 10f, 10f, 10f], dualFx);
    }

    [Fact]
    public void DispatchAndOutputConsumersCoexistOnOnePair()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(DualShapeBake());
        var positions = new ushort[] { 2, 4 };
        var effects = new float[positions.Length];
        Timeline<DispatchDualTrack, DispatchDualClip>.Apply(asset.Index, positions, true, effects);
        Assert.Equal([2f, 2f], effects);
        Assert.True(DispatchLog.DualWrites > 0);
        Assert.Equal(0, DispatchLog.HitCount);
        var beforeWrites = DispatchLog.DualWrites;
        Timeline<DispatchDualTrack, DispatchDualClip>.Apply(asset.Index, positions, true);
        Assert.Equal(
            [(ushort)2, (ushort)4],
            DispatchLog.Ticks());
        Assert.Equal(beforeWrites, DispatchLog.DualWrites);
    }

    [Fact]
    public void ZeroAdvanceCallsRefireOnlyTheCurrentWindow()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        var positions = new ushort[] { 1, 1 };
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, positions, true);
        Assert.Equal([(ushort)1, (ushort)1], DispatchLog.Ticks());
    }

    [Fact]
    public void AbsentPairAndOutOfDomainPositionsFireNothing()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(JumpOnlyBake());
        var positions = new ushort[] { 1, 2, 5 };
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, positions, true);
        using var sound = TimelineAsset.LoadAsset(TwoPairBake());
#if TL_CHECKED
        Assert.Throws<ArgumentException>(() => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(sound.Index, new ushort[] { 8, 9, 40 }, true));
#else
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(sound.Index, new ushort[] { 8, 9, 40 }, true);
#endif
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(sound.Index, new ushort[] { 0 }, false);
        Assert.Equal(0, DispatchLog.HitCount);
    }

    [Fact]
    public void DispatchApplyAllocatesZeroBytesWarm()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        var positions = new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        var indices = Enumerable.Repeat(asset.Index, positions.Length).ToArray();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, positions, true);
        DispatchLog.Reset();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++)
            Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, positions, true);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal(64 * 5, DispatchLog.HitCount);
    }

    [Fact]
    public void DispatchApplyAcceptsTypedComponentSpans()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        var indices = new[] { new IdCell(asset.Index) };
        var positions = new[] { new PosCell(5) };
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices.AsSpan(), positions.AsSpan(), true);
        Assert.Equal([(ushort)5], DispatchLog.Ticks());
        Assert.Equal(22, DispatchLog.Hits[0].Code);
    }

    [Fact]
    public void ScalarDispatchFiresTheCurrentTick()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)2, true);
        Assert.Equal([(ushort)2], DispatchLog.Ticks());
    }

    [Fact]
    public void RangeApplyFiresEveryCrossedWindowInTickOrderForward()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)2, (ushort)7, true);
        Assert.Equal(
            [(ushort)2, (ushort)5, (ushort)6],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)11, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.False(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void RangeApplyFiresWrappedWindowsInOrderAcrossTheLoop()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(LoopingSoundBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)4, (ushort)1, true);
        Assert.Equal(
            [(ushort)4, (ushort)5, (ushort)6, (ushort)7, (ushort)0],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)22, (ushort)22, (ushort)22, (ushort)22, (ushort)11],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.False(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void RangeApplyBackwardFiresCrossedWindowsInReverseTickOrder()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)7, (ushort)4, false);
        Assert.Equal(
            [(ushort)6, (ushort)5],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.True(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void RangeApplyBackwardWrapsAcrossTheLoopBoundary()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(LoopingSoundBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)0, (ushort)5, false);
        Assert.Equal(
            [(ushort)7, (ushort)6, (ushort)5],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)22, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.True(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void RangeApplyForwardClampsAtDurationAndStops()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)2, (ushort)100, true);
        Assert.Equal(
            [(ushort)2, (ushort)5, (ushort)6, (ushort)7],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)11, (ushort)22, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        DispatchLog.Reset();
#if !TL_CHECKED
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)9, (ushort)3, true);
        Assert.Equal(0, DispatchLog.HitCount);
#endif
    }

    [Fact]
    public void RangeApplyBackwardFromTheClampFiresTheCompletedWindows()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)8, (ushort)5, false);
        Assert.Equal(
            [(ushort)7, (ushort)6, (ushort)5],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)22, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
        for (var i = 0; i < DispatchLog.HitCount; i++) Assert.True(DispatchLog.Hits[i].Backward);
    }

    [Fact]
    public void RangeApplyZeroDistanceFiresNothing()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)5, (ushort)5, true);
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)5, (ushort)5, false);
        Assert.Equal(0, DispatchLog.HitCount);
    }

    [Fact]
    public void RangeApplyFiresEachWindowOnceWhenTheTargetIsOutsideTheLoopDomain()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(LoopingSoundBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(asset.Index, (ushort)0, (ushort)500, true);
        Assert.Equal(
            [(ushort)0, (ushort)1, (ushort)2, (ushort)3, (ushort)4, (ushort)5, (ushort)6, (ushort)7],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)11, (ushort)11, (ushort)11, (ushort)11, (ushort)22, (ushort)22, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
    }

    [Fact]
    public void RangeApplySharedIndexCrowdFiresEachRowsOwnRange()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(TwoPairBake());
        var index = asset.Index;
        ushort[] indices = [index, index, index, index];
        ushort[] from = [0, 7, 2, 5];
        ushort[] to = [3, 8, 2, 1];
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, from, to, true);
        Assert.Equal(
            [(ushort)1, (ushort)2, (ushort)7, (ushort)5, (ushort)6, (ushort)7],
            DispatchLog.Trail().Select(hit => hit.Tick).ToArray());
        Assert.Equal(
            [(ushort)11, (ushort)11, (ushort)22, (ushort)22, (ushort)22, (ushort)22],
            DispatchLog.Trail().Select(hit => hit.Code).ToArray());
    }

    [Fact]
    public void RangeApplyIsPairIsolated()
    {
        DispatchLog.Reset();
        using var jumpOnly = TimelineAsset.LoadAsset(JumpOnlyBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(jumpOnly.Index, (ushort)0, (ushort)8, true);
        Assert.Equal(0, DispatchLog.HitCount);
        using var both = TimelineAsset.LoadAsset(TwoPairBake());
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(both.Index, (ushort)0, (ushort)8, true);
        Assert.Equal(5, DispatchLog.HitCount);
        DispatchLog.Reset();
        Timeline<DispatchJumpTrack, DispatchJumpClip>.Apply(both.Index, (ushort)0, (ushort)8, true);
        Assert.Equal(0, DispatchLog.HitCount);
    }

    [Fact]
    public void ScalarRangeApplyMatchesTheOneRowSpanForm()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingSoundBake());
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(looping.Index, (ushort)4, (ushort)1, true);
        var scalarForward = DispatchLog.Trail();
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([looping.Index], [(ushort)4], [(ushort)1], true);
        Assert.Equal(scalarForward, DispatchLog.Trail());
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(looping.Index, (ushort)0, (ushort)5, false);
        var scalarBackward = DispatchLog.Trail();
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([looping.Index], [(ushort)0], [(ushort)5], false);
        Assert.Equal(scalarBackward, DispatchLog.Trail());
        Assert.Equal(3, scalarBackward.Length);
    }

    [Fact]
    public void RangeApplyAllocatesZeroBytesWarm()
    {
        DispatchLog.Reset();
        using var asset = TimelineAsset.LoadAsset(LoopingSoundBake());
        var indices = Enumerable.Repeat(asset.Index, 8).ToArray();
        var from = Enumerable.Repeat((ushort)2, 8).ToArray();
        var to = Enumerable.Repeat((ushort)6, 8).ToArray();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, from, to, true);
        DispatchLog.Reset();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++)
            Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(indices, from, to, true);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal(64 * 8 * 4, DispatchLog.HitCount);
    }
}

#if TL_CHECKED
public class DispatchRangeCheckedTests
{
    [Fact]
    public void RangeApplyRejectsMismatchedAndOverlappingColumns()
    {
        using var asset = TimelineAsset.LoadAsset(DispatchOnlyApplyTests.TwoPairBake());
        var index = asset.Index;
        Assert.Throws<ArgumentException>(
            () => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([index, index], [(ushort)1], [(ushort)2, (ushort)3], true));
        Assert.Throws<ArgumentException>(
            () => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([index, index], [(ushort)1, (ushort)2], [(ushort)3], true));
        var shared = new ushort[6];
        Assert.Throws<ArgumentException>(
            () => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([index, index, index], shared.AsSpan(0, 3), shared.AsSpan(2, 3), true));
        Assert.Throws<ArgumentException>(
            () => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply(shared.AsSpan(1, 3), shared.AsSpan(0, 3), shared.AsSpan(3, 3), true));
        Assert.Throws<ArgumentException>(
            () => Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([index, index, index], shared.AsSpan(0, 3), shared.AsSpan(3, 2), true));
        DispatchLog.Reset();
        Timeline<DispatchSoundTrack, DispatchSoundClip>.Apply([index], [(ushort)1], [(ushort)3], true);
        Assert.Equal(2, DispatchLog.HitCount);
    }
}
#endif

public readonly record struct IdCell(ushort Value);
public readonly record struct PosCell(ushort Value);
