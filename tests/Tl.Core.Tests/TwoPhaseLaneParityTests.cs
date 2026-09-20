using Xunit;

namespace Tl.Core.Tests;

public class TwoPhaseLaneParityTests
{
    public static TheoryData<ushort, bool, bool, int> Shapes => new()
    {
        { 6, true, true, 1 },
        { 6, true, false, 15 },
        { 6, false, true, 16 },
        { 6, false, false, 64 },
        { 10, true, true, 15 },
        { 10, true, false, 16 },
        { 10, false, true, 1 },
        { 10, false, false, 64 },
        { 96, true, true, 16 },
        { 96, true, false, 64 },
        { 96, false, true, 15 },
        { 96, false, false, 1 },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void ApplyAllThenOneStepMatchesFusedKernelBitExactly(ushort duration, bool looping, bool forward, int rows)
    {
        var index = TimelineAsset.Load(Bake(duration, looping));
        var ids = new TestIndex[rows];
        Array.Fill(ids, new TestIndex(index));
        var rawIds = new ushort[rows];
        Array.Fill(rawIds, index);

        foreach (var schedule in SmallSpanProbe.Schedules(rows, duration))
        {
            var splitPos = new TestPosition[rows];
            var splitFx = new TestEffect[rows];
            var fusedPos = new ushort[rows];
            var fusedFx = new float[rows];
            for (var i = 0; i < rows; i++)
            {
                splitPos[i] = new TestPosition(schedule[i]);
                splitFx[i] = new TestEffect { Value = (i % 17) * 0.25f - 2f };
                fusedPos[i] = schedule[i];
                fusedFx[i] = (i % 17) * 0.25f - 2f;
            }

            for (var frame = 0; frame < 9; frame++)
                Frame(ids, rawIds, splitPos, splitFx, fusedPos, fusedFx, forward);

            Frame(ids, rawIds, splitPos, splitFx, fusedPos, fusedFx, !forward);
        }
    }

    [Fact]
    public void PerEntityApplyThenStepMatchesFusedKernelBitExactly()
    {
        const ushort duration = 10;
        var index = TimelineAsset.Load(Bake(duration, looping: true));
        var typedId = new TestIndex { Value = index };
        foreach (var position in new ushort[] { 0, 5, 9, 10, 11 })
        foreach (var forward in new[] { true, false })
        {
            var splitPos = new TestPosition { Value = position };
            var splitFx = new TestEffect { Value = 1.5f };
            var fusedPos = new [] { position };
            var fusedFx = new [] { 1.5f };
            for (var frame = 0; frame < 9; frame++)
            {
                Timeline<RoutingTrack, RoutingClip>.Apply(typedId, splitPos, forward, ref splitFx);
                Timeline<RoutingTrack, RoutingClip>.Apply(index, fusedPos, fusedPos, forward, fusedFx);
                Assert.Equal(fusedFx[0], splitFx.Value);
                Timeline<RoutingTrack, RoutingClip>.Advance(typedId, ref splitPos, forward);
                Assert.Equal(fusedPos[0], splitPos.Value);
            }
        }
    }

    static void Frame(TestIndex[] ids, ushort[] rawIds, TestPosition[] splitPos, TestEffect[] splitFx, ushort[] fusedPos, float[] fusedFx, bool forward)
    {
        Timeline<RoutingTrack, RoutingClip>.Apply(ids, splitPos, forward, splitFx);
        Timeline<RoutingTrack, RoutingClip>.Apply(rawIds, fusedPos, fusedPos, forward, fusedFx);
        for (var i = 0; i < splitFx.Length; i++)
            Assert.Equal(fusedFx[i], splitFx[i].Value);
        Timeline<RoutingTrack, RoutingClip>.Advance(ids, splitPos, forward);
        for (var i = 0; i < splitPos.Length; i++)
            Assert.Equal(fusedPos[i], splitPos[i].Value);
    }

    static byte[] Bake(ushort duration, bool looping)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(2f))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }
}
