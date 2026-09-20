using System.Runtime.InteropServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public class SmallSpanRecordTests
{
    public static TheoryData<ushort, bool, bool> Durations => new()
    {
        { 10, true, true },
        { 10, true, false },
        { 10, false, true },
        { 10, false, false },
        { 6, true, true },
        { 6, true, false },
        { 96, true, true },
        { 96, true, false },
    };

    [Theory]
    [MemberData(nameof(Durations))]
    public void SmallSpansMatchCrowdPathAcrossLengths(ushort duration, bool looping, bool forward)
    {
        var index = TimelineAsset.Load(Bake(duration, looping, 2f));
        var ids = new ushort[SmallSpanProbe.MaxRows];
        Array.Fill(ids, index);
        foreach (var rows in SmallSpanProbe.Lengths)
            foreach (var schedule in SmallSpanProbe.Schedules(rows, duration))
                AssertFrameParity(ids, index, schedule, forward);
    }

    [Theory]
    [MemberData(nameof(Durations))]
    public void SmallSpanWalksRewindBitExactlyThroughBothRoutings(ushort duration, bool looping, bool forward)
    {
        var index = TimelineAsset.Load(Bake(duration, looping, 2f));
        var ids = new ushort[SmallSpanProbe.MaxRows];
        Array.Fill(ids, index);
        foreach (var rows in SmallSpanProbe.Lengths)
            foreach (var schedule in SmallSpanProbe.Schedules(rows, duration))
                AssertRewindParity(ids, index, schedule, forward);
    }

    [Fact]
    public void SingleRowLaneOverloadMatchesCrowdPath()
    {
        const ushort duration = 10;
        var index = TimelineAsset.Load(Bake(duration, looping: true, 2f));
        foreach (var position in new ushort[] { 0, 5, 9, 10, 11 })
        foreach (var forward in new[] { true, false })
        {
            var lanePos = new TestPosition { Value = position };
            var laneFx = new TestEffect { Value = 1.5f };
            Lane<RoutingTrack, RoutingClip>.Apply(index, ref lanePos, forward, ref laneFx);
            var laneAdvanced = lanePos;
            Lane<RoutingTrack, RoutingClip>.Step(index, ref laneAdvanced, forward);

            var typedId = new TestIndex { Value = index };
            var typedPos = new TestPosition { Value = position };
            var typedFx = new TestEffect { Value = 1.5f };
            Lane<RoutingTrack, RoutingClip>.Apply(typedId, ref typedPos, forward, ref typedFx);
            var typedAdvanced = typedPos;
            Lane<RoutingTrack, RoutingClip>.Step(typedId, ref typedAdvanced, forward);

            var spanIds = new TestIndex[] { new(index) };
            var spanPos = new TestPosition[] { new(position) };
            var spanFx = new TestEffect[] { new() { Value = 1.5f } };
            Lane<RoutingTrack, RoutingClip>.Apply((ReadOnlySpan<TestIndex>)spanIds, spanPos, forward, (Span<TestEffect>)spanFx);
            Lane<RoutingTrack, RoutingClip>.Step((ReadOnlySpan<TestIndex>)spanIds, spanPos, forward);

            var shortIds = new ushort[] { index };
            var shortPos = new ushort[] { position };
            var shortNext = new ushort[1];
            var shortEffects = new float[] { 1.5f };
            Timeline<RoutingTrack, RoutingClip>.Apply((ReadOnlySpan<ushort>)shortIds, shortPos, shortNext, forward, shortEffects);

            Assert.Equal(typedFx.Value, laneFx.Value);
            Assert.Equal(spanFx[0].Value, laneFx.Value);
            Assert.Equal(shortEffects[0], laneFx.Value);
            Assert.Equal(typedPos.Value, lanePos.Value);
            Assert.Equal(typedAdvanced.Value, laneAdvanced.Value);
            Assert.Equal(shortNext[0], laneAdvanced.Value);
            Assert.Equal(spanPos[0].Value, laneAdvanced.Value);
        }
    }

    [Fact]
    public void SingleRowLaneStepMatchesCrowdPath()
    {
        const ushort duration = 10;
        var index = TimelineAsset.Load(Bake(duration, looping: true, 2f));
        foreach (var position in new ushort[] { 0, 5, 9, 10, 11 })
        foreach (var forward in new[] { true, false })
        {
            var lanePos = new TestPosition { Value = position };
            Lane<RoutingTrack, RoutingClip>.Step(index, ref lanePos, forward);

            var typedId = new TestIndex { Value = index };
            var typedPos = new TestPosition { Value = position };
            Lane<RoutingTrack, RoutingClip>.Step(typedId, ref typedPos, forward);

            var spanIds = new TestIndex[] { new(index) };
            var spanPos = new TestPosition[] { new(position) };
            Lane<RoutingTrack, RoutingClip>.Step((ReadOnlySpan<TestIndex>)spanIds, spanPos, forward);

            var shortIds = new ushort[] { index };
            var shortPos = new ushort[] { position };
            Timeline.Step((ReadOnlySpan<ushort>)shortIds, shortPos, forward);

            Assert.Equal(typedPos.Value, lanePos.Value);
            Assert.Equal(spanPos[0].Value, lanePos.Value);
            Assert.Equal(shortPos[0], lanePos.Value);
        }
    }

    [Fact]
    public void UnfoldedIndexResolvesColdThenTakesRecordPath()
    {
        var index = TimelineAsset.Load(Bake(10, looping: true, 2f));
        var positions = new ushort[] { 9 };
        var next = new ushort[1];
        var effects = new float[] { 1.5f };
        Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, true, effects);
        Assert.Equal((ushort)0, next[0]);
        Assert.Equal((ushort)10, Timeline<RoutingTrack, RoutingClip>.View(index).Duration);
    }

    [Fact]
    public void CrowdSkippedRowsWritePositionIntoDistinctNext()
    {
        var index = TimelineAsset.Load(Bake(10, looping: false, 2f));
        var ids = new ushort[4];
        Array.Fill(ids, index);
        var positions = new ushort[] { 5, 12, 11, 3 };
        var next = new ushort[4];
        var effects = new float[4];
        Timeline<RoutingTrack, RoutingClip>.Apply((ReadOnlySpan<ushort>)ids.AsSpan(0, 4), positions, next, true, effects);
        Assert.Equal(new ushort[] { 6, 12, 11, 4 }, next);

        Array.Fill(ids, index);
        positions = [5, 12, 11, 3];
        next = new ushort[4];
        effects = new float[4];
        Timeline<RoutingTrack, RoutingClip>.Apply((ReadOnlySpan<ushort>)ids.AsSpan(0, 4), positions, next, false, effects);
        Assert.Equal(new ushort[] { 4, 12, 11, 2 }, next);
    }

    [Fact]
    public void RecordSkippedRowsWritePositionIntoDistinctNext()
    {
        var index = TimelineAsset.Load(Bake(10, looping: false, 2f));
        var positions = new ushort[] { 5, 12, 11, 3 };
        var next = new ushort[4];
        var effects = new float[4];
        Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, true, effects);
        Assert.Equal(new ushort[] { 6, 12, 11, 4 }, next);
    }

    [Fact]
    public void SmallSpanFramesAllocateZero()
    {
        var index = TimelineAsset.Load(Bake(96, looping: true, 2f));
        var positions = new ushort[15];
        for (var i = 0; i < positions.Length; i++) positions[i] = (ushort)(i % 96);
        var next = new ushort[15];
        var effects = new float[15];
        var single = new ushort[] { 4 };
        var singleNext = new ushort[1];
        var singleFx = new float[1];
        for (var warm = 0; warm < 8; warm++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, true, effects);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, single, true, singleFx);
            Timeline.Step(index, single, singleNext, true);
            Timeline.Step(index, positions, next, true);
        }
        var before = GC.GetTotalAllocatedBytes(precise: true);
        Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, true, effects);
        Timeline<RoutingTrack, RoutingClip>.Apply(index, single, true, singleFx);
        Timeline.Step(index, single, singleNext, true);
        Timeline.Step(index, positions, next, true);
        Assert.Equal(0, GC.GetTotalAllocatedBytes(precise: true) - before);
    }

    [Fact]
    public void SmallSpanRejectsBadColumnsLikeTheCrowdPath()
    {
        var index = TimelineAsset.Load(Bake(10, looping: true, 2f));
        var buffer = new byte[16];
        Assert.Throws<ArgumentException>(() =>
        {
            var positions = MemoryMarshal.Cast<byte, ushort>(buffer.AsSpan(0, 8));
            var effects = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, 16));
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, true, effects);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var positions = MemoryMarshal.Cast<byte, ushort>(buffer.AsSpan(0, 8));
            var effects = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, 16));
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, new ushort[4], true, effects);
        });
        var clean = new ushort[] { 1, 2, 3, 4 };
        Assert.Throws<ArgumentException>(
            () => Timeline<RoutingTrack, RoutingClip>.Apply(index, clean, new ushort[3], true, new float[3]));
    }

    static void AssertFrameParity(ushort[] ids, ushort index, ushort[] schedule, bool forward)
    {
        var rows = schedule.Length;

        var crowdPos = (ushort[])schedule.Clone();
        var crowdNext = new ushort[rows];
        var crowdFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, rows), crowdPos, crowdNext, forward, crowdFx);

        var smallPos = (ushort[])schedule.Clone();
        var smallNext = new ushort[rows];
        var smallFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(index, smallPos, smallNext, forward, smallFx);

        Assert.Equal(crowdPos, smallPos);
        Assert.Equal(crowdNext, smallNext);
        Assert.Equal(crowdFx, smallFx);

        var clockPos = (ushort[])schedule.Clone();
        var clockFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, rows), clockPos, clockPos, forward, clockFx);
        var twinPos = (ushort[])schedule.Clone();
        var twinFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(index, twinPos, twinPos, forward, twinFx);
        Assert.Equal(clockPos, twinPos);
        Assert.Equal(clockFx, twinFx);

        var barePos = (ushort[])schedule.Clone();
        var bareFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, rows), barePos, forward, bareFx);
        var lonePos = (ushort[])schedule.Clone();
        var loneFx = SeedEffects(rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(index, lonePos, forward, loneFx);
        Assert.Equal(barePos, lonePos);
        Assert.Equal(bareFx, loneFx);
        Assert.Equal(schedule, lonePos);

        var crowdStepPos = (ushort[])schedule.Clone();
        var crowdStepNext = new ushort[rows];
        Timeline.Step(ids.AsSpan(0, rows), crowdStepPos, crowdStepNext, forward);
        var smallStepPos = (ushort[])schedule.Clone();
        var smallStepNext = new ushort[rows];
        Timeline.Step(index, smallStepPos, smallStepNext, forward);
        Assert.Equal(crowdStepPos, smallStepPos);
        Assert.Equal(crowdStepNext, smallStepNext);
    }

    static void AssertRewindParity(ushort[] ids, ushort index, ushort[] schedule, bool forward)
    {
        var rows = schedule.Length;
        var crowdPos = (ushort[])schedule.Clone();
        var crowdNext = new ushort[rows];
        var crowdFx = SeedEffects(rows);
        var smallPos = (ushort[])schedule.Clone();
        var smallNext = new ushort[rows];
        var smallFx = SeedEffects(rows);
        for (var frame = 0; frame < 4; frame++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, rows), crowdPos, crowdNext, forward, crowdFx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, smallPos, smallNext, forward, smallFx);
            crowdNext.CopyTo(crowdPos);
            smallNext.CopyTo(smallPos);
        }
        Assert.Equal(crowdPos, smallPos);
        Assert.Equal(crowdNext, smallNext);
        Assert.Equal(crowdFx, smallFx);
        var back = !forward;
        for (var frame = 0; frame < 4; frame++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, rows), crowdPos, crowdNext, back, crowdFx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, smallPos, smallNext, back, smallFx);
            crowdNext.CopyTo(crowdPos);
            smallNext.CopyTo(smallPos);
        }
        Assert.Equal(crowdPos, smallPos);
        Assert.Equal(crowdNext, smallNext);
        Assert.Equal(crowdFx, smallFx);
    }

    static float[] SeedEffects(int rows)
    {
        var effects = new float[rows];
        for (var i = 0; i < rows; i++) effects[i] = (i % 17) * 0.25f - 2f;
        return effects;
    }

    static byte[] Bake(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), (uint)duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }
}

internal static class SmallSpanProbe
{
    internal const int MaxRows = 64;

    internal static readonly int[] Lengths = [.. Enumerable.Range(1, MaxRows)];

    internal static ushort[][] Schedules(int rows, ushort duration)
    {
        var choices = new ushort[] { 0, (ushort)(duration / 2), (ushort)(duration - 1), duration, (ushort)(duration + 1) };
        var schedules = new ushort[][] { Uniform(rows, choices), Runs(rows, choices, 0x51ED2701ul + (uint)(rows * 31 + duration)), Mixed(rows, choices, 0x9E3779B9ul ^ (uint)(rows * 40503 + duration * 7)) };
        return schedules;
    }

    static ushort[] Uniform(int rows, ushort[] choices)
    {
        var schedule = new ushort[rows];
        for (var i = 0; i < rows; i++) schedule[i] = choices[i % choices.Length];
        return schedule;
    }

    static ushort[] Runs(int rows, ushort[] choices, ulong seed)
    {
        var schedule = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            seed = Next(ref seed);
            schedule[i] = i > 0 && (seed & 3) == 0 ? schedule[i - 1] : choices[(uint)seed % choices.Length];
        }
        return schedule;
    }

    static ushort[] Mixed(int rows, ushort[] choices, ulong seed)
    {
        var schedule = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            seed = Next(ref seed);
            schedule[i] = choices[(uint)(seed >> 32) % choices.Length];
        }
        return schedule;
    }

    static ulong Next(ref ulong state)
    {
        state ^= state << 13;
        state ^= state >> 7;
        state ^= state << 17;
        return state;
    }
}
