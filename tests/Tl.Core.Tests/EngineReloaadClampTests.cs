using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public class EngineReloaadClampTests
{
    const ushort Duration = 8;

    static byte[] Bake(bool looping)
    {
        var baker = new DomainBaker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, Duration, new LaneClip(8));
        return looping ? baker.Looping().Bake() : baker.Bake();
    }

    [Fact]
    public void FiniteReloaadClampsTheSharedClockWhereTheLoopingBakeWrapped()
    {
        var positions = new ushort[1];
        var effects = new float[1];
        using var looping = TimelineAsset.LoadAsset(Bake(looping: true));
        using var loopingSet = new TimelineSet<LaneTrack, LaneClip>();
        var loopingIds = new ushort[] { loopingSet.Add(looping) };
        for (var step = 0; step < 8; step++)
            EngineStep(looping, loopingSet, loopingIds, positions, effects, forward: true, looping: true);
        Assert.Equal(0, positions[0]);
        for (var step = 0; step < 7; step++)
            EngineStep(looping, loopingSet, loopingIds, positions, effects, forward: true, looping: true);
        Assert.Equal(Duration - 1, positions[0]);
        var snapshot = positions[0];
        EngineStep(looping, loopingSet, loopingIds, positions, effects, forward: true, looping: true);
        Assert.Equal(0, positions[0]);
        EngineStep(looping, loopingSet, loopingIds, positions, effects, forward: false, looping: true);
        Assert.Equal(snapshot, positions[0]);

        var finite = TimelineAsset.LoadAsset(Bake(looping: false));
        try
        {
            using var finiteSet = new TimelineSet<LaneTrack, LaneClip>();
            var finiteIds = new ushort[] { finiteSet.Add(finite) };
            Assert.Equal(Duration - 1, positions[0]);
            EngineStep(finite, finiteSet, finiteIds, positions, effects, forward: true, looping: false);
            Assert.Equal(Duration, positions[0]);
            var parked = effects[0];
            EngineStep(finite, finiteSet, finiteIds, positions, effects, forward: true, looping: false);
            Assert.Equal(Duration, positions[0]);
            Assert.Equal(parked, effects[0]);
            for (var step = 0; step < Duration; step++)
                EngineStep(finite, finiteSet, finiteIds, positions, effects, forward: false, looping: false);
            Assert.Equal(0, positions[0]);
            EngineStep(finite, finiteSet, finiteIds, positions, effects, forward: false, looping: false);
            Assert.Equal(0, positions[0]);
        }
        finally
        {
            finite.Dispose();
        }
    }

    [Fact]
    public void LoopingReloaadStillWrapsAfterAFinitePhase()
    {
        var positions = new ushort[Duration];
        var effects = new float[Duration];
        var finite = TimelineAsset.LoadAsset(Bake(looping: false));
        try
        {
            using var finiteSet = new TimelineSet<LaneTrack, LaneClip>();
            var finiteIds = new ushort[Duration];
            Array.Fill(finiteIds, finiteSet.Add(finite));
            for (var step = 0; step <= Duration; step++)
                EngineStep(finite, finiteSet, finiteIds, positions, effects, forward: true, looping: false);
            Assert.Equal(Duration, positions[Duration - 1]);
        }
        finally
        {
            finite.Dispose();
        }
        using var looping = TimelineAsset.LoadAsset(Bake(looping: true));
        using var loopingSet = new TimelineSet<LaneTrack, LaneClip>();
        var loopingIds = new ushort[Duration];
        Array.Fill(loopingIds, loopingSet.Add(looping));
        Array.Clear(positions);
        for (var step = 0; step < Duration; step++)
            EngineStep(looping, loopingSet, loopingIds, positions, effects, forward: true, looping: true);
        for (var row = 0; row < Duration; row++)
            Assert.Equal(0, positions[row]);
    }

    static void EngineStep(
        TimelineAsset asset,
        TimelineSet<LaneTrack, LaneClip> set,
        ushort[] ids,
        ushort[] positions,
        float[] effects,
        bool forward,
        bool looping)
    {
        var before = (ushort[])positions.Clone();
        set.Gather(ids).Seek(positions, forward).Apply(effects);
        Timeline.Step(asset, positions, forward);
        for (var row = 0; row < positions.Length; row++)
        {
            var selected = TimelineMovement.Select(new TimelineState(1, before[row]), Duration, looping, !forward, out var next, out _, out _);
            Assert.Equal(selected ? next.Position : before[row], positions[row]);
        }
    }
}
