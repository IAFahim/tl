using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class TierParityTests
{
    public static TheoryData<ushort, bool, bool> Configurations => new()
    {
        { 6, true, true },
        { 6, true, false },
        { 6, false, true },
        { 6, false, false },
        { 16, true, true },
        { 16, true, false },
        { 16, false, true },
        { 16, false, false },
        { 1024, true, true },
        { 1024, true, false },
        { 1024, false, true },
        { 1024, false, false },
    };

    const int Rows = 4199;

    [Theory]
    [MemberData(nameof(Configurations))]
    public void CrowdAndStepPathsMatchRecordWalkBitExactly(ushort duration, bool looping, bool forward)
    {
        var index = TimelineAsset.Load(Bake(duration, looping));
        var slot = Timeline<RoutingTrack, RoutingClip>.View(index);
        Assert.Equal(duration, slot.Duration);
        var ids = new ushort[Rows];
        Array.Fill(ids, index);

        foreach (var positions in Schedules(duration))
        {
            var (expectedFx, expectedNext) = RecordWalk(slot, positions, forward);

            var crowdPos = (ushort[])positions.Clone();
            var crowdNext = new ushort[Rows];
            var crowdFx = Seed();
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), crowdPos, crowdNext, forward, crowdFx);
            Assert.Equal(expectedNext, crowdNext);
            Assert.Equal(expectedFx, crowdFx);

            var slotPos = (ushort[])positions.Clone();
            var slotNext = new ushort[Rows];
            var slotFx = Seed();
            Timeline<RoutingTrack, RoutingClip>.Apply(index, slotPos, slotNext, forward, slotFx);
            Assert.Equal(expectedNext, slotNext);
            Assert.Equal(expectedFx, slotFx);

            var stepPos = (ushort[])positions.Clone();
            var stepNext = new ushort[Rows];
            Timeline.Advance(index, stepPos, stepNext, forward);
            Assert.Equal(expectedNext, stepNext);

            var idsStepPos = (ushort[])positions.Clone();
            var idsStepNext = new ushort[Rows];
            Timeline.Advance(ids.AsSpan(0, Rows), idsStepPos, idsStepNext, forward);
            Assert.Equal(expectedNext, idsStepNext);

            var mixedPos = (ushort[])positions.Clone();
            var mixedFx = Seed();
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), mixedPos, forward, mixedFx);
            Assert.Equal(expectedFx, mixedFx);
            Assert.Equal(positions, mixedPos);
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void RewindFramesStayBitExactOnEveryRouting(ushort duration, bool looping, bool forward)
    {
        var index = TimelineAsset.Load(Bake(duration, looping));
        var slot = Timeline<RoutingTrack, RoutingClip>.View(index);
        var ids = new ushort[Rows];
        Array.Fill(ids, index);
        var positions = Schedules(duration)[3];

        var crowdPos = (ushort[])positions.Clone();
        var crowdNext = new ushort[Rows];
        var crowdFx = Seed();
        var lonePos = (ushort[])positions.Clone();
        var loneNext = new ushort[Rows];
        var loneFx = Seed();
        for (var frame = 0; frame < 6; frame++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), crowdPos, crowdNext, forward, crowdFx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, lonePos, loneNext, forward, loneFx);
            Assert.Equal(crowdPos, lonePos);
            Assert.Equal(crowdNext, loneNext);
            Assert.Equal(crowdFx, loneFx);
            crowdNext.CopyTo(crowdPos);
            loneNext.CopyTo(lonePos);
        }
        var back = !forward;
        for (var frame = 0; frame < 6; frame++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), crowdPos, crowdNext, back, crowdFx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, lonePos, loneNext, back, loneFx);
            Assert.Equal(crowdPos, lonePos);
            Assert.Equal(crowdNext, loneNext);
            Assert.Equal(crowdFx, loneFx);
            crowdNext.CopyTo(crowdPos);
            loneNext.CopyTo(lonePos);
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void WarmCrowdFramesAllocateZero(ushort duration, bool looping, bool forward)
    {
        var index = TimelineAsset.Load(Bake(duration, looping));
        var ids = new ushort[Rows];
        Array.Fill(ids, index);
        var positions = Schedules(duration)[0];
        var next = new ushort[Rows];
        var fx = Seed();
        for (var warm = 0; warm < 60; warm++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), positions, next, forward, fx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, forward, fx);
            Timeline.Advance(ids.AsSpan(0, Rows), positions, next, forward);
            Timeline.Advance(index, positions, next, forward);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var rep = 0; rep < 50; rep++)
        {
            Timeline<RoutingTrack, RoutingClip>.Apply(ids.AsSpan(0, Rows), positions, next, forward, fx);
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, next, forward, fx);
            Timeline.Advance(ids.AsSpan(0, Rows), positions, next, forward);
            Timeline.Advance(index, positions, next, forward);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    static (float[] fx, ushort[] next) RecordWalk(SlotView slot, ushort[] positions, bool forward)
    {
        var duration = slot.Duration;
        var fx = Seed();
        var next = new ushort[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            if (forward)
            {
                if (p < duration)
                {
                    ref var r = ref slot.ForwardRecords[p];
                    fx[i] += r.Effect;
                    next[i] = r.Next;
                    continue;
                }
                next[i] = p;
            }
            else
            {
                if (p <= duration)
                {
                    ref var r = ref slot.BackwardRecords[p];
                    if (r.Next != LaneMovementRecord.Skipped)
                    {
                        fx[i] += r.Effect;
                        next[i] = r.Next;
                        continue;
                    }
                }
                next[i] = p;
            }
        }
        return (fx, next);
    }

    static ushort[][] Schedules(ushort duration)
    {
        var staggered = new ushort[Rows];
        for (var i = 0; i < Rows; i++) staggered[i] = (ushort)(i % duration);
        var uniform = new ushort[Rows];
        Array.Fill(uniform, duration < 5 ? duration : (ushort)5);
        var waves = new ushort[Rows];
        for (var i = 0; i < Rows; i++) waves[i] = (ushort)(i / 100 % duration);
        var edges = new ushort[Rows];
        var edgeValues = new ushort[] { 0, 1, (ushort)(duration - 1), duration, (ushort)Math.Min(65535, duration + 1), 65535, 7, (ushort)(duration / 2) };
        for (var i = 0; i < Rows; i++) edges[i] = edgeValues[i % edgeValues.Length];
        return [staggered, uniform, waves, edges];
    }

    static float[] Seed()
    {
        var fx = new float[Rows];
        for (var i = 0; i < Rows; i++) fx[i] = (i % 17) * 0.25f - 2f;
        return fx;
    }

    static byte[] Bake(ushort duration, bool looping)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(2f))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), (uint)duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }
}
