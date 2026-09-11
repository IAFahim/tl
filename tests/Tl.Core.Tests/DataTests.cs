using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct AlphaClip(int Value);

public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = factor < 0.5f ? first : second;
}

public readonly record struct BetaClip(int Value);

public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
}

public readonly record struct GammaClip(int Value);

public readonly record struct GammaTrack(int Code) : IBlend<GammaClip>
{
    public void Blend(in GammaClip first, in GammaClip second, float factor, out GammaClip result) => result = first;
}

public readonly record struct BlendClip(float Amount);

public readonly record struct BlendTrack(float Scale) : IBlend<BlendClip>
{
    public void Blend(in BlendClip first, in BlendClip second, float factor, out BlendClip result)
        => result = new BlendClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct PhiClip(int Value);

public readonly record struct PhiTrack(int Code) : IBlend<PhiClip>
{
    public void Blend(in PhiClip first, in PhiClip second, float factor, out PhiClip result) => result = first;
}

public readonly record struct DeltaClip(int Value);

public readonly record struct DeltaTrack(int Code) : IBlend<DeltaClip>
{
    public void Blend(in DeltaClip first, in DeltaClip second, float factor, out DeltaClip result) => result = first;
}

public struct Health
{
    public float Value;
}

public struct Resistance
{
    public float Scale;
}

public struct Marker
{
    public int Tag;
}

public struct RowAlias
{
    public long A, B, C;
}

public readonly record struct Record(char Kind, int Code, float Value, uint Tick, uint Game, long Cycle, FrameFlags Flags);

public unsafe class DataTests
{
    internal static readonly List<Record> Records = [];
    internal static bool MarkerRequired;

    static DataTests()
    {
        PairRuntime<AlphaTrack, AlphaClip>.Consume(&AlphaExecute, &NoBind);
        PairRuntime<BetaTrack, BetaClip>.Consume(&BetaExecute, &NoBind);
        PairRuntime<PhiTrack, PhiClip>.Consume(&PhiExecute, &NoBind);
        PairRuntime<PhiTrack, PhiClip>.Consume(&PhiExecuteSecond, &NoBind);
        PairRuntime<DeltaTrack, DeltaClip>.Consume(&DeltaExecute, &DeltaBind);
    }

    private static void NoBind(in TimelineQuery columns)
    {
    }

    private static void AlphaExecute(in TickFrame frame, in TimelineQuery columns, int row)
    {
        AlphaClip scratch = default;
        var current = frame.ToFrame<AlphaTrack, AlphaClip>(ref scratch);
        Records.Add(new Record('A', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
        var health = columns.Find(TypeKey<Health>.Value);
        if (health >= 0)
            columns.Span<Health>(health)[row].Value += current.Clip.Value * current.Track.Code;
    }

    private static void BetaExecute(in TickFrame frame, in TimelineQuery columns, int row) => throw new InvalidOperationException("Beta consumer failed.");

    private static void PhiExecute(in TickFrame frame, in TimelineQuery columns, int row)
    {
        PhiClip scratch = default;
        var current = frame.ToFrame<PhiTrack, PhiClip>(ref scratch);
        Records.Add(new Record('1', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void PhiExecuteSecond(in TickFrame frame, in TimelineQuery columns, int row)
    {
        PhiClip scratch = default;
        var current = frame.ToFrame<PhiTrack, PhiClip>(ref scratch);
        Records.Add(new Record('2', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void DeltaExecute(in TickFrame frame, in TimelineQuery columns, int row)
    {
    }

    private static void DeltaBind(in TimelineQuery columns)
    {
        if (MarkerRequired && columns.Find(TypeKey<Marker>.Value) < 0)
            throw new ArgumentException("Delta requires the marker column.");
    }

    private static byte[] FiniteBake() => new Baker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
        .Clip(0, 0, 1, new AlphaClip(11))
        .Clip(0, 2, 3, new AlphaClip(22))
        .Bake();

    [Fact]
    public void LoadRejectsCorruptBlocks()
    {
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(FiniteBake()[..^1]));

        var magic = FiniteBake();
        magic[0] = (byte)'X';
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(magic));

        var version = FiniteBake();
        version[4] = 2;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(version));

        var size = FiniteBake();
        size[44] = 7;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(size));

        var misaligned = FiniteBake();
        misaligned[28] += 4;
        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(misaligned));

        var unsorted = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<GammaTrack, GammaClip>(new GammaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(1))
            .Clip(1, 0, 1, new GammaClip(1))
            .Bake();
        var pairOffset = BitConverter.ToUInt32(unsorted, 28);
        var first = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset));
        var second = BinaryPrimitives.ReadUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 16));
        if (first < second)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset), second);
            BinaryPrimitives.WriteUInt64LittleEndian(unsorted.AsSpan((int)pairOffset + 16), first);
        }

        Assert.Throws<ArgumentException>(() => TimelineAsset.Load(unsorted));
    }

    [Fact]
    public void DisposeFreesExactlyOnceAndIsIdempotent()
    {
        var asset = TimelineAsset.Load(FiniteBake());
        var component = new TimelineComponent(asset.Reference);
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext() && component.Position > 0);
        asset.Dispose();
        asset.Dispose();
    }

    [Fact]
    public void QueryYieldsSingleClipFramesWithWindowFlags()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 1, 3, new AlphaClip(7))
            .Bake());
        var component = new TimelineComponent(asset.Reference);
        component.Position = 3;
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext());

        foreach (var position in new[] { 1u, 2u })
        {
            component.Position = position;
            var frames = new List<(int Code, int Value, uint Tick, uint Game, ushort Track, FrameFlags Flags)>();
            foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
                frames.Add((frame.Track.Code, frame.Clip.Value, frame.TimelineTick, frame.GameTick, frame.TrackIndex, frame.Flags));
            var single = Assert.Single(frames);
            Assert.Equal((5, 7, position, 0u, (ushort)0), (single.Code, single.Value, single.Tick, single.Game, single.Track));
            Assert.True((single.Flags & FrameFlags.ClipStart) != 0 == (position == 1u));
            Assert.True((single.Flags & FrameFlags.ClipEnd) != 0 == (position == 2u));
        }

        Assert.Equal(2u, component.Position);
        Assert.Equal(0, component.Cycle);
    }

    [Fact]
    public void QueryYieldsBlendedFramesMatchingHandComputedFactor()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 4, new BlendClip(0f))
            .Clip(0, 2, 6, new BlendClip(10f))
            .Bake());
        var component = new TimelineComponent(asset.Reference);

        component.Position = 2;
        Assert.Equal(0f + (10f - 0f) * ((2u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 3;
        Assert.Equal(0f + (10f - 0f) * ((3u - 2u) / (float)(2u - 1u)), SingleBlendAmount(in component));

        component.Position = 4;
        Assert.Equal(10f, SingleBlendAmount(in component));

        using var single = TimelineAsset.Load(new Baker()
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Clip(0, 0, 3, new BlendClip(0f))
            .Clip(0, 2, 4, new BlendClip(8f))
            .Bake());
        var one = new TimelineComponent(single.Reference) { Position = 2 };
        Assert.Equal(0f + (8f - 0f) * 0.5f, SingleBlendAmount(in one));
    }

    private static float SingleBlendAmount(in TimelineComponent component)
    {
        foreach (var frame in Timeline.Query<BlendTrack, BlendClip>(in component))
            return frame.Clip.Amount;
        throw new InvalidOperationException("Expected one blended frame.");
    }

    [Fact]
    public void QueryPreservesAuthoredTrackOrderAndSkipsEmptyStages()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(2))
            .Clip(0, 0, 1, new AlphaClip(10))
            .Clip(1, 0, 1, new BlendClip(4f))
            .Clip(2, 0, 1, new AlphaClip(20))
            .Bake());
        var component = new TimelineComponent(asset.Reference);

        var codes = new List<int>();
        foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
            codes.Add(frame.Track.Code);
        Assert.Equal([1, 2], codes);
        Assert.Equal(0u, component.Position);

        component.Position = 1;
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in component).MoveNext());

        Assert.False(Timeline.Query<GammaTrack, GammaClip>(in component).MoveNext());
        var empty = default(TimelineComponent);
        Assert.False(Timeline.Query<AlphaTrack, AlphaClip>(in empty).MoveNext());
    }

    [Fact]
    public void TickMatchesTheMovementOracleForwardAndBackward()
    {
        using var asset = TimelineAsset.Load(FiniteBake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var query = Timeline.Rows(rows).Read(new Resistance[1]).Write(health);
        var windows = new Dictionary<uint, (int Code, float Value, uint Start, uint End)>
        {
            [0u] = (5, 11f, 0u, 1u),
            [2u] = (5, 22f, 2u, 3u),
        };

        Records.Clear();
        var forward = Oracle(3, false, 5, rows[0], 100u, windows);
        query.Tick(100u, 5);
        Assert.Equal(forward, Records);
        Assert.Equal(3u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
        Assert.Equal(11f * 5 + 22f * 5, health[0].Value);

        Records.Clear();
        query.Tick(200u, int.MaxValue);
        Assert.Empty(Records);
        Assert.Equal(3u, rows[0].Position);

        query.Tick(200u, 0);
        Assert.Equal(3u, rows[0].Position);

        Records.Clear();
        var backward = Oracle(3, false, -5, rows[0], 103u, windows);
        query.Tick(103u, -5);
        Assert.Equal(backward, Records);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
    }

    [Fact]
    public void TickWrapsGameTickAndCyclesLikeTheLoopOracle()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(7))
            .Clip(0, 0, 2, new AlphaClip(3))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var windows = new Dictionary<uint, (int Code, float Value, uint Start, uint End)>
        {
            [0u] = (7, 3f, 0u, 2u),
            [1u] = (7, 3f, 0u, 2u),
        };

        Records.Clear();
        var forward = Oracle(2, true, 3, rows[0], 10u, windows);
        Timeline.Rows(rows).Read(new Resistance[1]).Tick(10u, 3);
        Assert.Equal(forward, Records);
        Assert.Equal(1u, rows[0].Position);
        Assert.Equal(1, rows[0].Cycle);

        Records.Clear();
        var backward = Oracle(2, true, -3, rows[0], 13u, windows);
        Timeline.Rows(rows).Read(new Resistance[1]).Tick(13u, -3);
        Assert.Equal(backward, Records);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
    }

    private static List<Record> Oracle(uint duration, bool loops, int delta, TimelineComponent component, uint gameTick, Dictionary<uint, (int Code, float Value, uint Start, uint End)> windows)
    {
        var expected = new List<Record>();
        var position = component.Position;
        var cycle = component.Cycle;
        var remaining = (long)delta;
        var reverse = remaining < 0;
        while (remaining != 0 && TimelineMovement.Select(new TimelineState(1, position, cycle), duration, loops, reverse, out var next, out var tick, out var frameCycle, out var flags))
        {
            if (reverse)
                gameTick = unchecked(gameTick - 1);
            if (windows.TryGetValue(tick, out var window))
            {
                var work = flags;
                if (tick == window.Start)
                    work |= FrameFlags.ClipStart;
                if (tick == window.End - 1)
                    work |= FrameFlags.ClipEnd;
                expected.Add(new Record('A', window.Code, window.Value, tick, gameTick, frameCycle, work));
            }

            position = next.Position;
            cycle = next.Cycle;
            if (!reverse)
                gameTick = unchecked(gameTick + 1);
            remaining += reverse ? 1 : -1;
        }

        return expected;
    }

    [Fact]
    public void TickExecutesAuthoredOrderAndLifoConsumers()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<PhiTrack, PhiClip>(new PhiTrack(1))
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(4))
            .Track<PhiTrack, PhiClip>(new PhiTrack(2))
            .Clip(0, 0, 1, new PhiClip(10))
            .Clip(1, 0, 1, new AlphaClip(7))
            .Clip(2, 0, 1, new PhiClip(20))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };

        Records.Clear();
        Timeline.Rows(rows).Tick(50u, 1);
        Assert.Equal(
        [
            ('2', 1, 10f, 0u, 50u, 0L),
            ('1', 1, 10f, 0u, 50u, 0L),
            ('A', 4, 7f, 0u, 50u, 0L),
            ('2', 2, 20f, 0u, 50u, 0L),
            ('1', 2, 20f, 0u, 50u, 0L),
        ], Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        Assert.Equal(1u, rows[0].Position);
    }

    [Fact]
    public void ConsumerExceptionPropagatesLeavingPrefixAndNoCommit()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(3))
            .Track<BetaTrack, BetaClip>(new BetaTrack(1))
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(4))
            .Clip(0, 0, 1, new AlphaClip(5))
            .Clip(1, 0, 1, new BetaClip(9))
            .Clip(2, 0, 1, new AlphaClip(6))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];

        Records.Clear();
        Assert.Throws<InvalidOperationException>(() => Timeline.Rows(rows).Write(health).Tick(30u, 2));
        var prefix = Assert.Single(Records);
        Assert.Equal(('A', 3, 5f, 0u, 30u, 0L), (prefix.Kind, prefix.Code, prefix.Value, prefix.Tick, prefix.Game, prefix.Cycle));
        Assert.True((prefix.Flags & FrameFlags.ClipStart) != 0);
        Assert.Equal(15f, health[0].Value);
        Assert.Equal(0u, rows[0].Position);
    }

    [Fact]
    public void ZeroConsumerPairStillAdvancesTiming()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<GammaTrack, GammaClip>(new GammaTrack(1))
            .Clip(0, 0, 2, new GammaClip(5))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };

        Records.Clear();
        Timeline.Rows(rows).Tick(1u, 2);
        Assert.Empty(Records);
        Assert.Equal(2u, rows[0].Position);
    }

    [Fact]
    public void RowsOfDifferentAssetsClampIndependently()
    {
        using var shortAsset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(2))
            .Bake());
        using var longAsset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(2))
            .Clip(0, 0, 3, new AlphaClip(3))
            .Bake());
        var rows = new[]
        {
            new TimelineComponent(shortAsset.Reference),
            new TimelineComponent(longAsset.Reference),
        };

        Records.Clear();
        Timeline.Rows(rows).Read(new Resistance[2]).Tick(7u, 3);
        Assert.Equal(1u, rows[0].Position);
        Assert.Equal(3u, rows[1].Position);
        Assert.Equal(
        [
            (1, 2f, 0u, 7u),
            (2, 3f, 0u, 7u),
            (2, 3f, 1u, 8u),
            (2, 3f, 2u, 9u),
        ], Records.Select(record => (record.Code, record.Value, record.Tick, record.Game)).ToList());
    }

    [Fact]
    public void DefaultComponentsAndEmptyRowsAreTotalNoOps()
    {
        var rows = new[] { new TimelineComponent(), new TimelineComponent() };
        Records.Clear();
        Timeline.Rows(rows).Tick(uint.MaxValue, int.MinValue);
        Assert.Empty(Records);
        Assert.Equal(0u, rows[0].Position);

        Timeline.Rows([]).Tick(5u, 5);
        Assert.True(Timeline.Rows([]).Find(TypeKey<Health>.Value) < 0);
    }

    [Fact]
    public void TickBindsRelevantConsumerColumnsBeforeEffects()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(3))
            .Track<DeltaTrack, DeltaClip>(new DeltaTrack(1))
            .Clip(0, 0, 1, new AlphaClip(5))
            .Clip(1, 0, 1, new DeltaClip(4))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };

        MarkerRequired = true;
        try
        {
            Records.Clear();
            Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(new Resistance[1]).Tick(9u, 1));
            Assert.Empty(Records);
            Assert.Equal(0u, rows[0].Position);

            using var foreign = TimelineAsset.Load(FiniteBake());
            var foreignRows = new[] { new TimelineComponent(foreign.Reference) };
            Timeline.Rows(foreignRows).Read(new Resistance[1]).Tick(9u, 1);
            Assert.Single(Records);
            Assert.Equal(1u, foreignRows[0].Position);

            Timeline.Rows(rows).Read(new Resistance[1]).Read(new Marker[1]).Tick(9u, 1);
            Assert.Equal(2, Records.Count);
            Assert.Equal(1u, rows[0].Position);
        }
        finally
        {
            MarkerRequired = false;
        }
    }

    [Fact]
    public void ColumnValidationRejectsBadQueriesAtConstruction()
    {
        var rows = new[] { new TimelineComponent(), new TimelineComponent() };
        Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(new Resistance[1]));
        Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(new Resistance[2]).Read(new Resistance[2]));

        var buffer = new float[4];
        Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(buffer.AsSpan(0, 2)).Write(buffer.AsSpan(1, 2)));

        Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(MemoryMarshal.Cast<TimelineComponent, RowAlias>(rows)));

        var columns = Timeline.Rows(rows).Read(new Resistance[2]).Write(new Health[2]).Write(new Marker[2]).Write(new DeltaClip[2]);
        Assert.Equal(3, columns.Find(TypeKey<DeltaClip>.Value));
        Assert.Throws<ArgumentException>(() => Timeline.Rows(rows).Read(new Resistance[2]).Write(new Health[2]).Write(new Marker[2]).Write(new DeltaClip[2]).Write(new PhiClip[2]));
    }

    [Fact]
    public void ColumnFindAndSpanExposeRowsThroughTheConsumerSeam()
    {
        using var asset = TimelineAsset.Load(FiniteBake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var resistance = new[] { new Resistance { Scale = 2f } };
        var health = new Health[1];
        var query = Timeline.Rows(rows).Read(resistance).Write(health);

        Assert.Equal(0, query.Find(TypeKey<Resistance>.Value));
        Assert.Equal(1, query.Find(TypeKey<Health>.Value));
        Assert.Equal(-1, query.Find(TypeKey<Marker>.Value));
        Assert.Equal(2f, query.Span<Resistance>(0)[0].Scale);
        query.Tick(100u, 1);
        Assert.Equal(11f * 5f, health[0].Value);
        Assert.Equal(1u, rows[0].Position);
    }

    [Fact]
    public void WarmTickAllocatesNoManagedMemory()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<GammaTrack, GammaClip>(new GammaTrack(1))
            .Clip(0, 0, 1, new GammaClip(1))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);

        for (var index = 0; index < 1_000; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
    }
}
