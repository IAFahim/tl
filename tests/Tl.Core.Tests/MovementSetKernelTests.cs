using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public unsafe class MovementSetKernelTests
{
    static byte[] WrapBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(3f))
        .Looping()
        .Bake();

    static byte[] StepBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(2f))
        .Looping()
        .Bake();

    static byte[] MixedLoopingBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(3f))
        .Looping()
        .Bake();

    static byte[] MixedFiniteBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(4f))
        .Bake();

    [Fact]
    public void AddAtTracksHolesAndSkipsRebind()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var second = TimelineAsset.LoadAsset(StepBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        _ = set.Add(asset);
        Assert.Equal(0, set.Holes);

        using var measured = MeasuredLanes.Measure(second);
        var bound = set.AddAt(2, measured);
        Assert.Equal(2, bound);
        Assert.Equal(1, set.Holes);
        Assert.True(set.IsPending(1));
        Assert.False(set.IsAbsent(1));

        var generation = set.View(2).Generation;
        Assert.Equal(2, set.AddAt(2, measured));
        Assert.Equal(generation, set.View(2).Generation);
    }

    [Fact]
    public void GateContentionMakesWaitersSpinAndYield()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var second = TimelineAsset.LoadAsset(StepBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        set.Add(asset);
        using var measured = MeasuredLanes.Measure(second);
        set.AddAt(2, measured);
        Assert.True(set.IsPending(1));

        var holder = new Thread(() =>
        {
            while (Interlocked.CompareExchange(ref set._gate, 1, 0) != 0)
                Thread.Yield();
            Thread.Sleep(100);
            Volatile.Write(ref set._gate, 0);
        });
        holder.Start();
        SpinWait.SpinUntil(() => Volatile.Read(ref set._gate) == 1);

        set.MarkAbsent(1);
        holder.Join();

        Assert.True(set.IsAbsent(1));
    }

    [Fact]
    public void AdvanceCopiesRowsForUnboundAndUnresolvedIds()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var second = TimelineAsset.LoadAsset(StepBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        var id = set.Add(asset);
        using var measured = MeasuredLanes.Measure(second);
        set.AddAt(2, measured);

        var ids = new ushort[] { id, 1, 2, 7 };
        var positions = new ushort[] { 0, 3, 2, 5 };
        var next = new ushort[positions.Length];

        set.Advance(ids, positions, next, true);
        Assert.Equal(new ushort[] { 1, 3, 3, 5 }, next);

        set.Advance(ids, positions, next, false);
        Assert.Equal(new ushort[] { 3, 3, 1, 5 }, next);
    }

    [Fact]
    public void AdvancePerRowFallbackCopiesOverBoundRows()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        var id = set.Add(asset);

        var ids = new ushort[20];
        Array.Fill(ids, id);
        ids[9] = 200;
        var positions = new ushort[20];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i % 4);
        var next = new ushort[positions.Length];

        set.Advance(ids, positions, next, true);

        for (var i = 0; i < positions.Length; i++)
        {
            if (i == 9)
                Assert.Equal(1, next[i]);
            else
                Assert.Equal((positions[i] + 1) % 4, next[i]);
        }
    }

    [Fact]
    public void MixedApplySkipsUnboundRowsForward()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var second = TimelineAsset.LoadAsset(StepBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        var id = set.Add(asset);
        using var measured = MeasuredLanes.Measure(second);
        set.AddAt(2, measured);

        var ids = new ushort[] { id, 1, 2, 1, id, 1 };
        var positions = new ushort[] { 0, 4, 0, 4, 0, 4 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        set.Apply(ids, positions, next, true, effects);

        Assert.Equal(new ushort[] { 1, 4, 1, 4, 1, 4 }, next);
        Assert.Equal([ 6f, 0f, 4f, 0f, 6f, 0f ], effects);
    }

    [Fact]
    public void MixedApplySkipsUnboundRowsBackward()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        using var second = TimelineAsset.LoadAsset(StepBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        var id = set.Add(asset);
        using var measured = MeasuredLanes.Measure(second);
        set.AddAt(2, measured);

        var ids = new ushort[] { id, 1, 2, 1, id, 1 };
        var positions = new ushort[] { 2, 4, 2, 4, 2, 4 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        set.Apply(ids, positions, next, false, effects);

        Assert.Equal(new ushort[] { 1, 4, 1, 4, 1, 4 }, next);
        Assert.Equal([ -6f, 0f, -4f, 0f, -6f, 0f ], effects);
    }

    [Fact]
    public void FastMixedBackwardLeavesSkippedFiniteOriginRowsBitExact()
    {
        using var looping = TimelineAsset.LoadAsset(MixedLoopingBake());
        using var finite = TimelineAsset.LoadAsset(MixedFiniteBake());
        using var set = new TimelineSet<MovementWrapTrack, MovementWrapClip>();
        var loopingId = set.Add(looping);
        var finiteId = set.Add(finite);

        ushort[] ids = [ loopingId, finiteId, loopingId, finiteId ];
        var positions = new ushort[] { 1, 0, 1, 0 };
        var next = new ushort[positions.Length];
        float[] effects = [ 0.5f, 1.5f, 2.5f, 3.5f ];

        set.Apply(ids, positions, next, false, effects);

        Assert.Equal(new ushort[] { 0, 0, 0, 0 }, next);
        Assert.Equal([ -5.5f, 1.5f, -3.5f, 3.5f ], effects);
    }

    [Fact]
    public void MeasureWindowConstantCrowdUsesHeapScratch()
    {
        var baker = new DomainBaker();
        for (var s = 1; s <= 16; s++)
            baker.Track<ConstTrack, ConstClip>(new ConstTrack(s));
        for (var s = 0; s < 16; s++)
            baker.Clip(s, 0, 4, new ConstClip(1f));

        using var asset = TimelineAsset.LoadAsset(baker.Bake());
        using var measured = MeasuredLanes.Measure(asset);

        Assert.Equal(4, measured.Duration);
        for (var tick = 0; tick < 4; tick++)
        {
            Assert.Equal(192f, measured.Forward[tick]);
            Assert.Equal(192f, measured.Backward[tick]);
        }
    }
}
