using Xunit;

namespace Tl.Core.Tests;

public class MeasuredLanesTests
{
    static byte[] LoopingBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 4, new LaneClip(8))
        .Clip(0, 3, 6, new LaneClip(4))
        .Clip(1, 1, 6, new LaneClip(2))
        .Looping()
        .Bake();

    static byte[] FiniteBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 6, new LaneClip(5))
        .Bake();

    static byte[] FiniteVariantBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(9f))
        .Clip(0, 0, 6, new LaneClip(5))
        .Bake();

    static byte[] DualPairBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Track<OrphanTrack, OrphanClip>(new OrphanTrack(5))
        .Clip(0, 0, 4, new LaneClip(8))
        .Clip(0, 3, 6, new LaneClip(4))
        .Clip(1, 1, 6, new LaneClip(2))
        .Clip(2, 2, 5, new OrphanClip(3))
        .Looping()
        .Bake();

    static byte[] GammaBake() => new Baker()
        .Track<GammaTrack, GammaClip>(new GammaTrack(1))
        .Clip(0, 0, 6, new GammaClip(5))
        .Looping()
        .Bake();

    static byte[] ImpureBake() => new Baker()
        .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
        .Clip(0, 0, 6, new ImpureClip(5))
        .Looping()
        .Bake();

    static byte[] CycleBake() => new Baker()
        .Track<CycleTrack, CycleClip>(new CycleTrack(1f))
        .Clip(0, 0, 6, new CycleClip(5))
        .Looping()
        .Bake();

    static byte[] EmptyBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Bake();

    [Fact]
    public void SharedMeasureReproducesAddBindBitExact()
    {
        var looping = TimelineAsset.Load(LoopingBake());
        var finite = TimelineAsset.Load(FiniteBake());
        try
        {
            using var directLooping = new TimelineSet<LaneTrack, LaneClip>();
            using var sharedLooping = new TimelineSet<LaneTrack, LaneClip>();
            var directLoopingId = directLooping.Add(looping);
            using var loopingLanes = MeasuredLanes.Measure(looping);
            Assert.Equal(6, (int)loopingLanes.Duration);
            Assert.True(loopingLanes.Looping);
            var sharedLoopingId = sharedLooping.Add(looping, loopingLanes);
            AssertSamePlayback(directLooping, directLoopingId, sharedLooping, sharedLoopingId, 6, true);

            using var directFinite = new TimelineSet<LaneTrack, LaneClip>();
            using var sharedFinite = new TimelineSet<LaneTrack, LaneClip>();
            var directFiniteId = directFinite.Add(finite);
            using var finiteLanes = MeasuredLanes.Measure(finite);
            Assert.Equal(6, (int)finiteLanes.Duration);
            Assert.False(finiteLanes.Looping);
            var sharedFiniteId = sharedFinite.Add(finite, finiteLanes);
            AssertSamePlayback(directFinite, directFiniteId, sharedFinite, sharedFiniteId, 6, false);
        }
        finally
        {
            looping.Dispose();
            finite.Dispose();
        }
    }

    [Fact]
    public void SharedMeasureServesIndependentSets()
    {
        var asset = TimelineAsset.Load(LoopingBake());
        try
        {
            using var first = new TimelineSet<LaneTrack, LaneClip>();
            using var second = new TimelineSet<LaneTrack, LaneClip>();
            var firstId = first.Add(asset);
            using var measured = MeasuredLanes.Measure(asset);
            var secondId = second.Add(asset, measured);
            var thirdId = second.Add(asset, measured);
            Assert.Equal(0, (int)secondId);
            Assert.Equal(1, (int)thirdId);
            AssertSamePlayback(first, firstId, second, secondId, 6, true);
            AssertSamePlayback(first, firstId, second, thirdId, 6, true);
        }
        finally
        {
            asset.Dispose();
        }
    }

    [Fact]
    public void SharedMeasureServesEveryPairOfTheAsset()
    {
        var asset = TimelineAsset.Load(DualPairBake());
        try
        {
            using var laneDirectSet = new TimelineSet<LaneTrack, LaneClip>();
            using var orphanDirectSet = new TimelineSet<OrphanTrack, OrphanClip>();
            var laneDirect = laneDirectSet.Add(asset);
            var orphanDirect = orphanDirectSet.Add(asset);
            using var measured = MeasuredLanes.Measure(asset);
            using var laneSharedSet = new TimelineSet<LaneTrack, LaneClip>();
            using var orphanSharedSet = new TimelineSet<OrphanTrack, OrphanClip>();
            var laneShared = laneSharedSet.Add(asset, measured);
            var orphanShared = orphanSharedSet.Add(asset, measured);
            AssertSamePlayback(laneDirectSet, laneDirect, laneSharedSet, laneShared, 6, true);
            AssertSamePlayback(orphanDirectSet, orphanDirect, orphanSharedSet, orphanShared, 6, true);
        }
        finally
        {
            asset.Dispose();
        }
    }

    [Fact]
    public void ForeignMeasuredLanesAreRejected()
    {
        var alpha = TimelineAsset.Load(FiniteBake());
        var beta = TimelineAsset.Load(FiniteVariantBake());
        try
        {
            using var measured = MeasuredLanes.Measure(alpha);
            using var set = new TimelineSet<LaneTrack, LaneClip>();
            Assert.Throws<ArgumentException>(() => set.Add(beta, measured));
        }
        finally
        {
            alpha.Dispose();
            beta.Dispose();
        }
    }

    [Fact]
    public void SharedAddKeepsPairValidation()
    {
        var dual = TimelineAsset.Load(DualPairBake());
        var gamma = TimelineAsset.Load(GammaBake());
        try
        {
            using var measured = MeasuredLanes.Measure(gamma);
            using var laneSet = new TimelineSet<LaneTrack, LaneClip>();
            Assert.Throws<ArgumentException>(() => laneSet.Add(gamma, measured));
            using var gammaSet = new TimelineSet<GammaTrack, GammaClip>();
            Assert.Throws<ArgumentException>(() => gammaSet.Add(gamma));
            using var gammaLanes = MeasuredLanes.Measure(gamma);
            Assert.Throws<ArgumentException>(() => gammaSet.Add(gamma, gammaLanes));
        }
        finally
        {
            dual.Dispose();
            gamma.Dispose();
        }
    }

    [Fact]
    public void DisposedMeasuredLanesAreRejected()
    {
        var asset = TimelineAsset.Load(FiniteBake());
        try
        {
            var measured = MeasuredLanes.Measure(asset);
            measured.Dispose();
            using var set = new TimelineSet<LaneTrack, LaneClip>();
            Assert.Throws<ObjectDisposedException>(() => set.Add(asset, measured));
        }
        finally
        {
            asset.Dispose();
        }
    }

    [Fact]
    public void UnloadedAssetsAreRejected()
    {
        var asset = TimelineAsset.Load(FiniteBake());
        asset.Dispose();
        Assert.Throws<ArgumentException>(() => MeasuredLanes.Measure(asset));
    }

    [Fact]
    public void ImpureConsumersAreStillRejected()
    {
        var asset = TimelineAsset.Load(ImpureBake());
        try
        {
            using var set = new TimelineSet<ImpureTrack, ImpureClip>();
            Assert.Throws<ArgumentException>(() => set.Add(asset));
        }
        finally
        {
            asset.Dispose();
        }
    }

    [Fact]
    public void CycleDependentConsumersAreStillRejected()
    {
        var asset = TimelineAsset.Load(CycleBake());
        try
        {
            using var set = new TimelineSet<CycleTrack, CycleClip>();
            Assert.Throws<ArgumentException>(() => set.Add(asset));
        }
        finally
        {
            asset.Dispose();
        }
    }

    [Fact]
    public void EmptyAssetsBindThroughSharedMeasure()
    {
        var asset = TimelineAsset.Load(EmptyBake());
        try
        {
            using var direct = new TimelineSet<LaneTrack, LaneClip>();
            using var shared = new TimelineSet<LaneTrack, LaneClip>();
            var directId = direct.Add(asset);
            using var measured = MeasuredLanes.Measure(asset);
            Assert.Equal(0, (int)measured.Duration);
            var sharedId = shared.Add(asset, measured);
            AssertSamePlayback(direct, directId, shared, sharedId, 0, false);
        }
        finally
        {
            asset.Dispose();
        }
    }

    static void AssertSamePlayback<TTrack, TClip>(
        TimelineSet<TTrack, TClip> first,
        ushort firstId,
        TimelineSet<TTrack, TClip> second,
        ushort secondId,
        ushort duration,
        bool looping)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        for (var start = 0; start <= duration; start++)
        {
            foreach (var forward in new[] { true, false })
            {
                var left = Run(first, firstId, (ushort)start, duration + 2, forward);
                var right = Run(second, secondId, (ushort)start, duration + 2, forward);
                Assert.Equal(left.Positions, right.Positions);
                Assert.Equal(left.Effects, right.Effects);
                Assert.Equal(left.Cycles, right.Cycles);
            }
        }
    }

    static (ushort[] Positions, float[] Effects, long[] Cycles) Run<TTrack, TClip>(
        TimelineSet<TTrack, TClip> set, ushort id, ushort start, int steps, bool forward)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        var ids = new ushort[] { id };
        var positions = new ushort[] { start };
        var effects = new float[1];
        var cycles = new long[1];
        var seenPositions = new ushort[steps];
        var seenEffects = new float[steps];
        var seenCycles = new long[steps];
        for (var step = 0; step < steps; step++)
        {
            set.Gather(ids).Seek(positions, forward).Apply(effects, cycles);
            seenPositions[step] = positions[0];
            seenEffects[step] = effects[0];
            seenCycles[step] = cycles[0];
        }
        return (seenPositions, seenEffects, seenCycles);
    }
}
