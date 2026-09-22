using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class SharedClockCrowdTests
{
    static byte[] LoopingBake() => new Baker()
        .Track<HandleTrack, HandleClip>(new HandleTrack(1f))
        .Clip(0, 0, 4, new HandleClip(8))
        .Looping()
        .Bake();

    static byte[] FiniteBake() => new Baker()
        .Track<HandleTrack, HandleClip>(new HandleTrack(2f))
        .Clip(0, 1, 6, new HandleClip(4))
        .Bake();

    static byte[] WideLoopingBake() => new Baker()
        .Track<HandleTrack, HandleClip>(new HandleTrack(3f))
        .Clip(0, 2, 5, new HandleClip(2))
        .Clip(0, 4, 9, new HandleClip(6))
        .Looping()
        .Bake();

    static (byte[] Bake, ushort Duration, bool Looping)[] Variants() =>
    [
        (LoopingBake(), 4, true),
        (FiniteBake(), 6, false),
        (WideLoopingBake(), 9, true),
    ];

    static ushort[] Positions(ushort duration) =>
    [
        0,
        1,
        (ushort)(duration / 2),
        (ushort)(duration - 1),
        duration,
#if !TL_CHECKED
        (ushort)(duration + 1),
        (ushort)(duration + 40),
        ushort.MaxValue,
#endif
    ];

    static readonly int[] RowCounts = [3, 15, 16, 4096, 65536];

    static float[] Seed(int rows, int salt)
    {
        var effects = new float[rows];
        for (var i = 0; i < rows; i++)
            effects[i] = ((i * 7 + salt) % 13 - 6) * 0.25f;
        return effects;
    }

    static void Bind(ushort index)
    {
        Timeline<HandleTrack, HandleClip>.Apply(index, Span<ushort>.Empty, true, Span<float>.Empty);
        Timeline.Advance(index, Span<ushort>.Empty, true);
    }

    [Fact]
    public void SharedClockApplyMatchesPerRowPathBitExact()
    {
        foreach (var (bake, duration, looping) in Variants())
            {
                using var asset = TimelineAsset.LoadAsset(bake);
            var index = asset.Index;
            Bind(index);
            foreach (var forward in new[] { true, false })
                foreach (var rows in RowCounts)
                    foreach (var position in Positions(duration))
                    {
                        var uniform = new ushort[rows];
                        Array.Fill(uniform, position);
                        var perRow = Seed(rows, position);
                        var shared = (float[])perRow.Clone();
                        Timeline<HandleTrack, HandleClip>.Apply(index, uniform, forward, perRow);
                        Timeline<HandleTrack, HandleClip>.Apply(index, position, forward, shared);
                        Assert.True(
                            MemoryMarshal.AsBytes(perRow.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(shared.AsSpan())),
                            $"effects differ: duration {duration}, looping {looping}, forward {forward}, rows {rows}, position {position}");
                    }
            }
    }

    [Fact]
    public void SharedClockStepMatchesPerRowStepFromEveryStart()
    {
        foreach (var (bake, duration, _) in Variants())
            {
                using var asset = TimelineAsset.LoadAsset(bake);
            var index = asset.Index;
            Bind(index);
            var maxStart =
#if TL_CHECKED
                duration;
#else
                duration + 2;
#endif
            for (var start = 0; start <= maxStart; start++)
                foreach (var forward in new[] { true, false })
                {
                    var uniform = new ushort[37];
                    Array.Fill(uniform, (ushort)start);
                    Timeline.Advance(index, uniform, forward);
                    var clock = (ushort)start;
                    Timeline<HandleTrack, HandleClip>.Advance(index, ref clock, forward);
                    var allMatch = true;
                    foreach (var uniformTick in uniform)
                        allMatch &= uniformTick == clock;
                    Assert.True(
                        allMatch,
                        $"clock diverged: duration {duration}, start {start}, forward {forward}, per-row {uniform[0]}, shared {clock}");
                }
            }
    }

    [Fact]
    public void SharedClockFrameLoopMatchesPerRowColumns()
    {
        foreach (var (bake, duration, looping) in Variants())
            {
                using var asset = TimelineAsset.LoadAsset(bake);
            var index = asset.Index;
            Bind(index);
            foreach (var rows in new[] { 15, 16, 4096 })
            {
                var uniform = new ushort[rows];
                Array.Fill(uniform, (ushort)(duration / 2));
                var perRowEffects = Seed(rows, 3);
                var sharedEffects = (float[])perRowEffects.Clone();
                var clock = (ushort)(duration / 2);
                for (var step = 0; step < 80; step++)
                {
                    var forward = step % 4 != 3;
                    Timeline<HandleTrack, HandleClip>.Apply(index, uniform, forward, perRowEffects);
                    Timeline.Advance(index, uniform, forward);
                    Timeline<HandleTrack, HandleClip>.Apply(index, clock, forward, sharedEffects);
                    Timeline<HandleTrack, HandleClip>.Advance(index, ref clock, forward);
                    var allMatch = true;
                    foreach (var uniformTick in uniform)
                        allMatch &= uniformTick == clock;
                    Assert.True(
                        allMatch,
                        $"clock diverged at step {step}: duration {duration}, looping {looping}, rows {rows}");
                    Assert.True(
                        MemoryMarshal.AsBytes(perRowEffects.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(sharedEffects.AsSpan())),
                        $"effects diverged at step {step}: duration {duration}, looping {looping}, forward {forward}, rows {rows}");
                }
            }
            }
    }

    [Fact]
    public void SkippedSharedClockLeavesEffectsUntouched()
    {
        foreach (var (bake, duration, looping) in Variants())
            {
                using var asset = TimelineAsset.LoadAsset(bake);
            var index = asset.Index;
            Bind(index);
            foreach (var forward in new[] { true, false })
                foreach (var position in Positions(duration))
                {
                    if (!IsSkipped(index, position, forward)) continue;
                    var effects = Seed(64, position);
                    var pristine = (float[])effects.Clone();
                    Timeline<HandleTrack, HandleClip>.Apply(index, position, forward, effects);
                    Assert.True(
                        MemoryMarshal.AsBytes(effects.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(pristine.AsSpan())),
                        $"skipped position moved effects: duration {duration}, looping {looping}, forward {forward}, position {position}");
                }
            }
    }

    static unsafe bool IsSkipped(ushort index, ushort position, bool forward)
    {
        var view = Timeline<HandleTrack, HandleClip>.View(index);
        if (forward) return position >= view.Duration;
        if (position > view.Duration) return true;
        return view.BackwardRecords[position].Next == LaneMovementRecord.Skipped;
    }

    [Fact]
    public void FirstTypedUseResolvesThroughSharedClockApply()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var effects = new float[64];
        Timeline<HandleTrack, HandleClip>.Apply(asset.Index, 0, true, effects);
        foreach (var effect in effects)
            Assert.Equal(8f, effect);
    }

    [Fact]
    public void WarmSharedClockFrameAllocatesZero()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        var index = asset.Index;
        Bind(index);
        var effects = new float[256];
        var clock = (ushort)2;
        long allocated;
        for (var attempt = 0; ; attempt++)
        {
            for (var pass = 0; pass < 1_000; pass++)
            {
                Timeline<HandleTrack, HandleClip>.Apply(index, clock, true, effects);
                Timeline<HandleTrack, HandleClip>.Advance(index, ref clock, true);
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 100_000; pass++)
            {
                Timeline<HandleTrack, HandleClip>.Apply(index, clock, true, effects);
                Timeline<HandleTrack, HandleClip>.Advance(index, ref clock, true);
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0 || attempt >= 8) break;
        }
        Assert.Equal(0, allocated);
    }
}
