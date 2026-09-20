using System.Runtime.CompilerServices;
using Xunit;


namespace Tl.Core.Tests;

public readonly record struct RoutingClip(float Amount);

public readonly record struct RoutingTrack(float Scale) : IBlend<RoutingClip>
{
    public void Blend(in RoutingClip first, in RoutingClip second, float factor, out RoutingClip result)
        => result = new RoutingClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class RoutingPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<RoutingTrack, RoutingClip>.Consume(&ExecuteScale, &BindFloat);
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

    private static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        RoutingClip scratch = default;
        var frame = TickFrame.ToFrame<RoutingTrack, RoutingClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Scale * frame.Clip.Amount;
    }
}

public class UniformRunRoutingTests
{
    const int Rows = 5000;

    static byte[] Bake(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static ushort[] Uniform(int rows) => Map(rows, _ => 5);

    static ushort[] Waves(int rows, ushort duration) => Map(rows, i => (ushort)(i / 100 % duration));

    static ushort[] Staggered(int rows, ushort duration) => Map(rows, i => (ushort)(i % duration));

    static ushort[] Alternating(int rows) => Map(rows, i => (ushort)(i & 1));

    static ushort[] StaggeredHead(int rows, ushort duration) => Map(rows, i => i < 200 ? (ushort)(i % duration) : (ushort)5);

    static ushort[] UniformHead(int rows, ushort duration) => Map(rows, i => i < 200 ? (ushort)5 : (ushort)(i % duration));

    static ushort[] UniformSkips(int rows, ushort duration) => Map(rows, i => i % 512 == 5 ? (ushort)(duration + 3) : (ushort)5);

    static ushort[] Map(int rows, Func<int, ushort> shape)
    {
        var values = new ushort[rows];
        for (var i = 0; i < rows; i++) values[i] = shape(i);
        return values;
    }

    static ushort[][] Shapes(ushort duration)
        =>
        [
            Uniform(Rows),
            Waves(Rows, duration),
            Staggered(Rows, duration),
            Alternating(Rows),
            StaggeredHead(Rows, duration),
            UniformHead(Rows, duration),
            UniformSkips(Rows, duration),
        ];

    static float[] SeedEffects(int rows)
    {
        var effects = new float[rows];
        for (var i = 0; i < rows; i++) effects[i] = (i % 17) * 0.25f - 2f;
        return effects;
    }

    [Theory]
    [InlineData(96, true)]
    [InlineData(96, false)]
    [InlineData(6, true)]
    [InlineData(6, false)]
    public void CrowdFramesMatchPerRowScalarOnEveryShape(ushort duration, bool looping)
    {
        var index = TimelineAsset.Load(Bake(duration, looping, 2f));
        var ids = new ushort[Rows];
        Array.Fill(ids, index);
        foreach (var shape in Shapes(duration))
            AssertFrameParity(ids, shape);
    }

    [Theory]
    [InlineData(96, true)]
    [InlineData(96, false)]
    [InlineData(6, true)]
    [InlineData(6, false)]
    public void SplitIdBlocksMatchPerRowScalarOnEveryShape(ushort duration, bool looping)
    {
        var first = TimelineAsset.Load(Bake(duration, looping, 2f));
        var second = TimelineAsset.Load(Bake(duration, looping, 3f));
        Assert.NotEqual(first, second);
        foreach (var shape in Shapes(duration))
        {
            var ids = new ushort[Rows];
            Array.Fill(ids, first, 0, 3000);
            Array.Fill(ids, second, 3000, Rows - 3000);
            AssertFrameParity(ids, shape);
        }
    }

    [Fact]
    public void RewindReturnsPositionsBitExactlyThroughBothRoutings()
    {
        var index = TimelineAsset.Load(Bake(96, true, 2f));
        var ids = new ushort[Rows];
        Array.Fill(ids, index);
        foreach (var shape in Shapes(96))
        {
            var positions = (ushort[])shape.Clone();
            var effects = SeedEffects(Rows);
            for (var frame = 0; frame < 16; frame++)
                Timeline<RoutingTrack, RoutingClip>.Apply(ids, positions, positions, true, effects);
            Assert.NotEqual(shape, positions);
            for (var frame = 0; frame < 16; frame++)
                Timeline<RoutingTrack, RoutingClip>.Apply(ids, positions, positions, false, effects);
            Assert.Equal(shape, positions);
        }
    }

    [Fact]
    public void WarmCrowdFrameAllocatesZeroOnEveryShape()
    {
        var index = TimelineAsset.Load(Bake(96, true, 2f));
        var ids = new ushort[Rows];
        Array.Fill(ids, index);
        foreach (var shape in Shapes(96))
        {
            var positions = (ushort[])shape.Clone();
            var effects = SeedEffects(Rows);
            for (var warm = 0; warm < 4; warm++)
                Timeline<RoutingTrack, RoutingClip>.Apply(ids, positions, positions, true, effects);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Timeline<RoutingTrack, RoutingClip>.Apply(ids, positions, positions, true, effects);
            Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
        }
    }

    static void AssertFrameParity(ushort[] ids, ushort[] seed)
    {
        var batchPositions = (ushort[])seed.Clone();
        var batchEffects = SeedEffects(Rows);
        Timeline<RoutingTrack, RoutingClip>.Apply(ids, batchPositions, batchPositions, true, batchEffects);
        var forward = OracleFrame(ids, seed, SeedEffects(Rows), forward: true);
        Assert.Equal(forward.Positions, batchPositions);
        Assert.Equal(forward.Effects, batchEffects);

        Timeline<RoutingTrack, RoutingClip>.Apply(ids, batchPositions, batchPositions, false, batchEffects);
        var backward = OracleFrame(ids, forward.Positions, forward.Effects, forward: false);
        Assert.Equal(backward.Positions, batchPositions);
        Assert.Equal(backward.Effects, batchEffects);
    }

    static (ushort[] Positions, float[] Effects) OracleFrame(ushort[] ids, ushort[] seed, float[] effectsSeed, bool forward)
    {
        var positions = new ushort[seed.Length];
        var effects = effectsSeed;
        for (var i = 0; i < seed.Length; i++)
        {
            var position = seed[i];
            Timeline<RoutingTrack, RoutingClip>.Apply(ids[i], new ReadOnlySpan<ushort>(in position), new Span<ushort>(ref position), forward, new Span<float>(ref effects[i]));
            positions[i] = position;
        }
        return (positions, effects);
    }
}
