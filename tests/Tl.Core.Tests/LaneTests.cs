using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using Tl.TestSupport;
using System.Diagnostics.CodeAnalysis;

namespace Tl.Core.Tests;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct ImpureClip(float Amount);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct ImpureTrack(float Scale) : IBlend<ImpureClip>
{
    public void Blend(in ImpureClip first, in ImpureClip second, float factor, out ImpureClip result) => result = first;
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct OrderClip(float Amount);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct OrderTrack(float Scale) : IBlend<OrderClip>
{
    public void Blend(in OrderClip first, in OrderClip second, float factor, out OrderClip result) => result = first;
}

public readonly record struct OrphanClip(int Value);

public readonly record struct OrphanTrack(int Code) : IBlend<OrphanClip>
{
    public void Blend(in OrphanClip first, in OrphanClip second, float factor, out OrphanClip result) => result = first;
}

public readonly struct LawLane : ITimelineLane<LawLane>
{
    public static ushort Duration => 6;
    public static bool Looping => true;
    public static float Effect(ushort position) => position;
    public static float InverseEffect(ushort position) => -position;
}

public readonly struct LawFiniteLane : ITimelineLane<LawFiniteLane>
{
    public static ushort Duration => 6;
    public static bool Looping => false;
    public static float Effect(ushort position) => position + 1;
    public static float InverseEffect(ushort position) => -(position + 1);
}

internal static unsafe class LanePairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteScale, &BindFloat);
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteConstant, &BindFloat);
        PairRuntime<ImpureTrack, ImpureClip>.Consume(&ExecuteImpure, &BindFloat);
        PairRuntime<OrphanTrack, OrphanClip>.Consume(&ExecuteOrphan, &BindFloat);
        PairRuntime<OrderTrack, OrderClip>.Consume(&ExecuteOrderA, &BindFloat);
        PairRuntime<OrderTrack, OrderClip>.Consume(&ExecuteOrderB, &BindFloat);
        PairRuntime<OrderTrack, OrderClip>.Consume(&ExecuteOrderC, &BindFloat);
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
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ExecuteConstant(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -7f : 7f;

    private static void ExecuteImpure(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] *= 2f;

    private static void ExecuteOrphan(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        OrphanClip scratch = default;
        var frame = TickFrame.ToFrame<OrphanTrack, OrphanClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Code * frame.Clip.Value;
    }

    private static void ExecuteOrderA(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -16777216f : 16777216f;

    private static void ExecuteOrderB(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -1f : 1f;

    private static void ExecuteOrderC(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -1f : 1f;
}

public partial class LaneTests
{
    static byte[] LoopingBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 4, new LaneClip(8))
        .Clip(0, 3, 6, new LaneClip(4))
        .Clip(1, 1, 6, new LaneClip(2))
        .Looping()
        .Bake();

    static byte[] FiniteBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 6, new LaneClip(5))
        .Bake();

    static byte[] ImpureBake() => new DomainBaker()
        .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
        .Clip(0, 0, 6, new ImpureClip(5))
        .Looping()
        .Bake();

    static byte[] DualPairBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Track<OrphanTrack, OrphanClip>(new OrphanTrack(5))
        .Clip(0, 0, 4, new LaneClip(8))
        .Clip(0, 3, 6, new LaneClip(4))
        .Clip(1, 1, 6, new LaneClip(2))
        .Clip(2, 2, 5, new OrphanClip(3))
        .Looping()
        .Bake();

    static byte[] GammaBake() => new DomainBaker()
        .Track<GammaTrack, GammaClip>(new GammaTrack(1))
        .Clip(0, 0, 6, new GammaClip(5))
        .Looping()
        .Bake();

    [Fact]
    public void SeekMatchesMovementLawLooping()
    {
        for (var position = 0; position <= 7; position++)
        for (var forward = 0; forward < 2; forward++)
        {
            var positions = new[] { (ushort)position };
            var effects = new float[1];
            Timeline<LawLane>.Apply(positions, forward == 0, effects); Timeline<LawLane>.Advance(positions, forward == 0);
            var moved = TimelineMovement.Select(new TimelineState(1, (ushort)position), LawLane.Duration, LawLane.Looping, forward != 0, out var next, out _, out _);
            if (moved)
                Assert.Equal(next.Position, positions[0]);
            else
                Assert.Equal((ushort)position, positions[0]);
        }
    }

    [Fact]
    public void SeekMatchesMovementLawFinite()
    {
        for (var position = 0; position <= 7; position++)
        for (var forward = 0; forward < 2; forward++)
        {
            var positions = new[] { (ushort)position };
            var effects = new float[1];
            Timeline<LawFiniteLane>.Apply(positions, forward == 0, effects); Timeline<LawFiniteLane>.Advance(positions, forward == 0);
            var moved = TimelineMovement.Select(new TimelineState(1, (ushort)position), LawFiniteLane.Duration, LawFiniteLane.Looping, forward != 0, out var next, out _, out _);
            if (moved)
                Assert.Equal(next.Position, positions[0]);
            else
                Assert.Equal((ushort)position, positions[0]);
        }
    }

    [Fact]
    public void HandLaneAppliesFoldedEffects()
    {
        var positions = new ushort[] { 0, 0, 2, 2, 2, 5 };
        var effects = new float[positions.Length];

        Timeline<LawLane>.Apply(positions, true, effects); Timeline<LawLane>.Advance(positions, true);

        Assert.Equal(0f, effects[0]);
        Assert.Equal(0f, effects[1]);
        Assert.Equal(2f, effects[2]);
        Assert.Equal(2f, effects[3]);
        Assert.Equal(2f, effects[4]);
        Assert.Equal(5f, effects[5]);
        Assert.Equal(1, positions[0]);
        Assert.Equal(3, positions[2]);
        Assert.Equal(0, positions[5]);
    }

    static readonly float[] LoopingEffects = [15f, 26f, 26f, 24f, 22f, 22f];

    static void Simulate(ushort[] positions, float[] effects, float[] table, bool forward, int calls, ushort duration, bool looping)
    {
        for (var call = 0; call < calls; call++)
            for (var i = 0; i < positions.Length; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, positions[i]), duration, looping, !forward, out var next, out var tick, out _))
                    continue;
                effects[i] += forward ? table[tick] : -table[tick];
                positions[i] = next.Position;
            }
    }

    [Fact]
    public void BakedLaneTablesMatchAuthoredOracle()
    {
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, BakedLane<LaneTrack, LaneClip>.Duration);
        Assert.True(BakedLane<LaneTrack, LaneClip>.Looping);
        for (var tick = 0; tick < 6; tick++)
        {
            Assert.Equal(LoopingEffects[tick], BakedLane<LaneTrack, LaneClip>.Effect((ushort)tick));
            Assert.Equal(-LoopingEffects[tick], BakedLane<LaneTrack, LaneClip>.InverseEffect((ushort)tick));
        }
    }

    [Fact]
    public void BakedLaneMatchesOracleForwardAndBackward()
    {
        const int rows = 257;
        const int ticks = 80;
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var lanePositions = new ushort[rows];
        var laneEffects = new float[rows];
        var oraclePositions = new ushort[rows];
        var oracleEffects = new float[rows];
        for (var i = 0; i < rows; i++)
            lanePositions[i] = oraclePositions[i] = (ushort)(i * 7 % 6);
        var initialEffects = (float[])laneEffects.Clone();
        var initialPositions = (ushort[])lanePositions.Clone();

        for (var tick = 0; tick < ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
            Simulate(oraclePositions, oracleEffects, LoopingEffects, true, 1, 6, true);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleEffects, laneEffects);
        }

        for (var tick = 0; tick < ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, false, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, false);
            Simulate(oraclePositions, oracleEffects, LoopingEffects, false, 1, 6, true);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleEffects, laneEffects);
        }

        Assert.Equal(initialPositions, lanePositions);
        Assert.Equal(initialEffects, laneEffects);
    }

    [Fact]
    public void BakedLaneCatchUpMatchesOracleDelta()
    {
        const int rows = 64;
        using var asset = TimelineAsset.LoadAsset(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var lanePositions = new ushort[rows];
        var laneEffects = new float[rows];
        var oraclePositions = new ushort[rows];
        var oracleEffects = new float[rows];
        for (var i = 0; i < rows; i++)
            lanePositions[i] = oraclePositions[i] = (ushort)(i % 6);

        for (var call = 0; call < 3; call++)
            {
                Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects);
                Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
            }
        Simulate(oraclePositions, oracleEffects, LoopingEffects, true, 3, 6, true);

        Assert.Equal(oraclePositions, lanePositions);
        Assert.Equal(oracleEffects, laneEffects);
    }

    [Fact]
    public void BakedLaneFiniteClampMatchesOracle()
    {
        const int rows = 16;
        const int ticks = 10;
        using var asset = TimelineAsset.LoadAsset(FiniteBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, BakedLane<LaneTrack, LaneClip>.Duration);
        Assert.False(BakedLane<LaneTrack, LaneClip>.Looping);
        var finiteEffects = new float[6];
        for (var tick = 0; tick < 6; tick++)
        {
            finiteEffects[tick] = BakedLane<LaneTrack, LaneClip>.Effect((ushort)tick);
            Assert.Equal(17f, finiteEffects[tick]);
        }

        var lanePositions = new ushort[rows];
        var laneEffects = new float[rows];
        var oraclePositions = new ushort[rows];
        var oracleEffects = new float[rows];
        for (var i = 0; i < rows; i++)
            lanePositions[i] = oraclePositions[i] = (ushort)(i % 8);

        for (var tick = 0; tick < ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
            Simulate(oraclePositions, oracleEffects, finiteEffects, true, 1, 6, false);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleEffects, laneEffects);
        }
        Assert.Equal(6, lanePositions[0]);
    }

    [Fact]
    public void BakedLaneFoldsEveryPairOfTheAsset()
    {
        using var asset = TimelineAsset.LoadAsset(DualPairBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, BakedLane<LaneTrack, LaneClip>.Duration);
        for (var tick = 0; tick < 6; tick++)
        {
            var orphan = tick is >= 2 and < 5 ? 5 * 3f : 0f;
            Assert.Equal(LoopingEffects[tick] + orphan, BakedLane<LaneTrack, LaneClip>.Effect((ushort)tick));
        }
    }

    [Fact]
    public void BakedLanePropagatesConsumerFaults()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<BetaTrack, BetaClip>(new BetaTrack(1))
            .Clip(0, 0, 6, new BetaClip(5))
            .Looping()
            .Bake());
        Assert.Throws<InvalidOperationException>(() => BakedLane<BetaTrack, BetaClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneBakesTheSingleBaselineFoldForColumnFoldingConsumers()
    {
        using var asset = TimelineAsset.LoadAsset(ImpureBake());
        BakedLane<ImpureTrack, ImpureClip>.Bind(asset);

        Assert.Equal(0f, BakedLane<ImpureTrack, ImpureClip>.Effect(0));
        Assert.Equal(0f, BakedLane<ImpureTrack, ImpureClip>.InverseEffect(0));
        var positions = new ushort[] { 0, 2, 4 };
        var effects = new float[3];
        Timeline<BakedLane<ImpureTrack, ImpureClip>>.Apply(positions, true, effects); Timeline<BakedLane<ImpureTrack, ImpureClip>>.Advance(positions, true);
        Assert.Equal(new ushort[] { 1, 3, 5 }, positions);
        Assert.Equal(new[] { 0f, 0f, 0f }, effects);
    }

    [Fact]
    public void BakedLaneFoldsConsumersInRegisteredOrder()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<OrderTrack, OrderClip>(new OrderTrack(1f))
            .Clip(0, 0, 2, new OrderClip(1f))
            .Looping()
            .Bake());
        BakedLane<OrderTrack, OrderClip>.Bind(asset);

        Assert.Equal(16777218f, BakedLane<OrderTrack, OrderClip>.Effect(0));
        Assert.Equal(-16777216f, BakedLane<OrderTrack, OrderClip>.InverseEffect(0));
    }

    [Fact]
    public void BakedLaneBindsDurationOneLoopingAsset()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
            .Clip(0, 0, 1, new LaneClip(8))
            .Looping()
            .Bake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(1, BakedLane<LaneTrack, LaneClip>.Duration);
        Assert.Equal(23f, BakedLane<LaneTrack, LaneClip>.Effect(0));
        Assert.Equal(-23f, BakedLane<LaneTrack, LaneClip>.InverseEffect(0));

        var positions = new ushort[] { 0 };
        var effects = new float[1];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(positions, true, effects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(positions, true);
        Assert.Equal(0, positions[0]);
        Assert.Equal(23f, effects[0]);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(positions, false, effects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(positions, false);
        Assert.Equal(0, positions[0]);
        Assert.Equal(0f, effects[0]);
    }

    [Fact]
    public void BakedLaneRejectsAssetWithoutPair()
    {
        using var asset = TimelineAsset.LoadAsset(FiniteBake());
        Assert.Throws<ArgumentException>(() => BakedLane<ImpureTrack, ImpureClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsPairWithoutConsumer()
    {
        using var asset = TimelineAsset.LoadAsset(GammaBake());
        Assert.Throws<ArgumentException>(() => BakedLane<GammaTrack, GammaClip>.Bind(asset));
    }

    [Fact]
    public void SeekWarmPathAllocatesNothing()
    {
        var positions = new ushort[256];
        var effects = new float[256];

        using (var asset = TimelineAsset.LoadAsset(LoopingBake()))
            BakedLane<LaneTrack, LaneClip>.Bind(asset);

        for (var i = 0; i < 100; i++)
        {
            Timeline<LawLane>.Apply(positions, true, effects); Timeline<LawLane>.Advance(positions, true);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(positions, false, effects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(positions, false);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++)
        {
            Timeline<LawLane>.Apply(positions, true, effects); Timeline<LawLane>.Advance(positions, true);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(positions, false, effects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(positions, false);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void SeekTreatsEmptyAndZeroDurationAsNoOp()
    {
        var positions = Array.Empty<ushort>();
        Timeline<LawLane>.Apply(positions, true, Array.Empty<float>()); Timeline<LawLane>.Advance(positions, true);

        var effects = new float[1];
        var zero = new ushort[] { 3 };
        Timeline<ZeroLane>.Apply(zero, true, effects); Timeline<ZeroLane>.Advance(zero, true);
        Assert.Equal(3, zero[0]);
        Assert.Equal(0f, effects[0]);
    }

    static byte[] ConstantBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 0, 6, new LaneClip(10))
        .Looping()
        .Bake();

    static byte[] WideBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 2, 9, new LaneClip(4))
        .Bake();

    [Fact]
    public void SetAppliesEachTimelineIndependently()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var constant = TimelineAsset.LoadAsset(ConstantBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var constantId = timelines.Add(constant);
        Assert.Equal(0, loopingId);
        Assert.Equal(1, constantId);

        var ids = new[] { loopingId, constantId, loopingId };
        var positions = new ushort[] { 0, 0, 1 };
        var effects = new float[3];

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(LoopingEffects[0], effects[0]);
        Assert.Equal(17f, effects[1]);
        Assert.Equal(LoopingEffects[1], effects[2]);
        Assert.Equal(new ushort[] { 1, 1, 2 }, positions);
    }

    [Fact]
    public void SetMatchesStaticLaneBitExact()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var constant = TimelineAsset.LoadAsset(ConstantBake());
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var constantId = timelines.Add(constant);
        var wideId = timelines.Add(wide);

        const int rows = 600;
        var ids = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            if (i < 300)
            {
                ids[i] = loopingId;
                positions[i] = i < 240 ? (ushort)(i * 7 % 11) : i < 282 ? (ushort)3 : (ushort)(i * 3 % 11);
            }
            else if (i < 540)
            {
                ids[i] = (i - 300) / 60 % 2 == 0 ? constantId : wideId;
                positions[i] = (ushort)(i * 5 % 13);
            }
            else
            {
                ids[i] = wideId;
                positions[i] = (ushort)(i * 11 % 13);
            }
        }
        var effects = new float[rows];

        var loopRows = new List<int>();
        var constantRows = new List<int>();
        var wideRows = new List<int>();
        for (var i = 0; i < rows; i++)
        {
            if (ids[i] == loopingId) loopRows.Add(i);
            else if (ids[i] == constantId) constantRows.Add(i);
            else wideRows.Add(i);
        }

        var loopPositions = new ushort[loopRows.Count];
        var loopEffects = new float[loopRows.Count];
        var constantPositions = new ushort[constantRows.Count];
        var constantEffects = new float[constantRows.Count];
        var widePositions = new ushort[wideRows.Count];
        var wideEffects = new float[wideRows.Count];
        for (var j = 0; j < loopRows.Count; j++) loopPositions[j] = positions[loopRows[j]];
        for (var j = 0; j < constantRows.Count; j++) constantPositions[j] = positions[constantRows[j]];
        for (var j = 0; j < wideRows.Count; j++) widePositions[j] = positions[wideRows[j]];

        for (var frame = 0; frame < 80; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(effects); timelines.Advance(ids, positions, forward);
            BakedLane<LaneTrack, LaneClip>.Bind(looping);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(loopPositions, forward, loopEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(loopPositions, forward);
            BakedLane<LaneTrack, LaneClip>.Bind(constant);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(constantPositions, forward, constantEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(constantPositions, forward);
            BakedLane<LaneTrack, LaneClip>.Bind(wide);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, forward, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, forward);
            for (var j = 0; j < loopRows.Count; j++)
            {
                var row = loopRows[j];
                Assert.Equal(loopPositions[j], positions[row]);
                Assert.Equal(loopEffects[j], effects[row]);
            }
            for (var j = 0; j < constantRows.Count; j++)
            {
                var row = constantRows[j];
                Assert.Equal(constantPositions[j], positions[row]);
                Assert.Equal(constantEffects[j], effects[row]);
            }
            for (var j = 0; j < wideRows.Count; j++)
            {
                var row = wideRows[j];
                Assert.Equal(widePositions[j], positions[row]);
                Assert.Equal(wideEffects[j], effects[row]);
            }
        }
    }

    [Fact]
    public void SetHandlesSmallCrowds()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new[] { loopingId, wideId, loopingId, wideId, loopingId };
        var positions = new ushort[] { 4, 12, 5, 8, 0 };
        var effects = new float[5];

        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        var loopPositions = new ushort[] { 4, 5, 0 };
        var loopEffects = new float[3];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(loopPositions, true, loopEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(loopPositions, true);

        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        var widePositions = new ushort[] { 12, 8 };
        var wideEffects = new float[2];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, true, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, true);

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(loopEffects[0], effects[0]);
        Assert.Equal(wideEffects[0], effects[1]);
        Assert.Equal(loopEffects[1], effects[2]);
        Assert.Equal(wideEffects[1], effects[3]);
        Assert.Equal(loopEffects[2], effects[4]);
        Assert.Equal(new[] { loopPositions[0], loopPositions[1], loopPositions[2] }, new[] { positions[0], positions[2], positions[4] });
        Assert.Equal(new[] { widePositions[0], widePositions[1] }, new[] { positions[1], positions[3] });
    }

    [Fact]
    public void SetGatherModeMatchesStaticLane()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        const int rows = 300;
        var ids = new ushort[rows];
        Array.Fill(ids, loopingId);
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = (ushort)(i % 6);
        var effects = new float[rows];

        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[rows];

        for (var frame = 0; frame < 80; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(effects); timelines.Advance(ids, positions, forward);
            BakedLane<LaneTrack, LaneClip>.Bind(looping);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, forward, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, forward);
            Assert.Equal(lanePositions, positions);
            Assert.Equal(laneEffects, effects);
        }
    }

    [Fact]
    public void SetGatherLeavesSkippedRowEffectsBitExact()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        var ids = new ushort[33];
        Array.Fill(ids, loopingId);
        var positions = new ushort[33];
        var effects = new float[33];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i * 5 % 6);
        positions[16] = 7;
        positions[17] = 6;
        effects[16] = MemoryMarshal.Read<float>([ 0x01, 0x00, 0x80, 0x7F ]);
        effects[17] = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(7, positions[16]);
        Assert.Equal(6, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));

        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        Assert.Equal(7, positions[16]);
        Assert.Equal(6, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));
    }

    [Fact]
    public void SetMixedPathLeavesSkippedRowEffectsBitExact()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        timelines.Add(looping);

        var ids = new ushort[33];
        for (var i = 0; i < ids.Length; i++)
            ids[i] = (ushort)(i & 1);
        var positions = new ushort[33];
        var effects = new float[33];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i * 5 % 6);
        positions[16] = 7;
        positions[17] = 6;
        effects[16] = MemoryMarshal.Read<float>([ 0x01, 0x00, 0x80, 0x7F ]);
        effects[17] = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(7, positions[16]);
        Assert.Equal(6, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));

        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        Assert.Equal(7, positions[16]);
        Assert.Equal(6, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));
    }

    [Fact]
    public void SetFiniteGatherLeavesSkippedRowEffectsBitExact()
    {
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);

        var ids = new ushort[33];
        Array.Fill(ids, wideId);
        var positions = new ushort[33];
        var effects = new float[33];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i * 5 % 9);
        positions[16] = 9;
        positions[17] = 9;
        effects[16] = MemoryMarshal.Read<float>([ 0x01, 0x00, 0x80, 0x7F ]);
        effects[17] = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(9, positions[16]);
        Assert.Equal(9, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));

        positions[16] = 10;
        positions[17] = 10;
        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        Assert.Equal(10, positions[16]);
        Assert.Equal(10, positions[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));
    }

    [Fact]
    public void SetRejectsUnboundId()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var error = Assert.Throws<ArgumentException>(() =>
            timelines.Gather([ 0, 1 ]).Seek(new ushort[2], true).Apply(new float[2]));
        Assert.Contains("not bound", error.Message);
    }

    [Fact]
    public void SetWarmPathAllocatesNothing()
    {
        var ids = new ushort[256];
        var positions = new ushort[256];
        var staggered = new ushort[256];
        for (var i = 0; i < staggered.Length; i++)
            staggered[i] = (ushort)(i % 6);
        var effects = new float[256];
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        for (var i = 0; i < 100; i++)
        {
            timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
            timelines.Gather(ids).Seek(staggered, true).Apply(effects); timelines.Advance(ids, staggered, true);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++)
        {
            timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
            timelines.Gather(ids).Seek(staggered, true).Apply(effects); timelines.Advance(ids, staggered, true);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    readonly struct ZeroLane : ITimelineLane<ZeroLane>
    {
        public static ushort Duration => 0;
        public static bool Looping => true;
        public static float Effect(ushort position) => 1f;
        public static float InverseEffect(ushort position) => -1f;
    }

    static byte[] EmptyBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Bake();

    static byte[] SecondFiniteBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(3f))
        .Clip(0, 0, 9, new LaneClip(7))
        .Bake();

    static ushort[] UniformRunPositions()
    {
        var positions = new ushort[64];
        Array.Fill(positions, (ushort)5, 0, 40);
        positions[40] = 1;
        positions[41] = 99;
        positions[42] = 2;
        positions[43] = 10;
        positions[44] = 3;
        positions[45] = 11;
        positions[46] = 4;
        positions[47] = 12;
        for (var i = 48; i < positions.Length; i++)
            positions[i] = (ushort)(i * 5 % 9);
        return positions;
    }

    static ushort[] UniformBackwardRunPositions()
    {
        var positions = new ushort[70];
        positions[0] = 3;
        positions[1] = 99;
        positions[2] = 0;
        Array.Fill(positions, (ushort)5, 3, 40);
        positions[43] = 99;
        positions[44] = 1;
        positions[45] = 2;
        positions[46] = 3;
        positions[47] = 4;
        positions[48] = 6;
        positions[49] = 7;
        positions[50] = 8;
        positions[51] = 10;
        positions[52] = 11;
        positions[53] = 12;
        positions[54] = 9;
        positions[55] = 0;
        positions[56] = 1;
        for (var i = 57; i < positions.Length; i++)
            positions[i] = (ushort)(i * 7 % 11);
        return positions;
    }

    [Fact]
    public void SetAddsZeroDurationTimeline()
    {
        using var empty = TimelineAsset.LoadAsset(EmptyBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var emptyId = timelines.Add(empty);
        Assert.Equal(0, emptyId);

        var ids = new[] { emptyId, emptyId };
        var positions = new ushort[] { 0, 5 };
        var effects = new float[2];
        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);
        Assert.Equal(new ushort[] { 0, 5 }, positions);
        Assert.Equal(new[] { 0f, 0f }, effects);
    }

    [Fact]
    public void SetAppliesNothingWhenGatherIsEmpty()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        timelines.Gather(Array.Empty<ushort>()).Seek(Array.Empty<ushort>(), true).Apply(Array.Empty<float>()); timelines.Advance(Array.Empty<ushort>(), Array.Empty<ushort>(), true);

        var ids = new[] { loopingId };
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
        Assert.Equal(LoopingEffects[0], effects[0]);
        Assert.Equal(1, positions[0]);
    }

    [Fact]
    public void SetSplitsRowsAcrossChunks()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        const int rows = 5000;
        var ids = new ushort[rows];
        Array.Fill(ids, loopingId);
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = (ushort)(i % 6);
        var effects = new float[rows];

        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[rows];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
    }

    [Fact]
    public void SetAppliesUniformRunsForward()
    {
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var ids = new ushort[64];
        Array.Fill(ids, wideId);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);

        var positions = UniformRunPositions();
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);

        positions = UniformRunPositions();
        var bareEffects = new float[positions.Length];
        lanePositions = (ushort[])positions.Clone();
        laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
        timelines.Gather(ids).Seek(positions, true).Apply(bareEffects); timelines.Advance(ids, positions, true);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, bareEffects);

        var singles = new ushort[16];
        for (var i = 0; i < singles.Length; i++)
            singles[i] = (ushort)(i % 11);
        var singleIds = new ushort[16];
        Array.Fill(singleIds, wideId);
        var singleEffects = new float[16];
        var singleLanePositions = (ushort[])singles.Clone();
        var singleLaneEffects = new float[16];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(singleLanePositions, true, singleLaneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(singleLanePositions, true);
        timelines.Gather(singleIds).Seek(singles, true).Apply(singleEffects); timelines.Advance(singleIds, singles, true);
        Assert.Equal(singleLanePositions, singles);
        Assert.Equal(singleLaneEffects, singleEffects);
    }

    [Fact]
    public void SetAppliesUniformRunsBackward()
    {
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var ids = new ushort[70];
        Array.Fill(ids, wideId);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);

        var positions = UniformBackwardRunPositions();
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, false, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, false);
        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);

        positions = UniformBackwardRunPositions();
        var bareEffects = new float[positions.Length];
        lanePositions = (ushort[])positions.Clone();
        laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, false, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, false);
        timelines.Gather(ids).Seek(positions, false).Apply(bareEffects); timelines.Advance(ids, positions, false);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, bareEffects);
    }

    [Fact]
    public void SetAppliesUniformWrapRuns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var ids = new ushort[48];
        Array.Fill(ids, loopingId);

        var positions = new ushort[48];
        Array.Fill(positions, (ushort)5, 0, 40);
        positions[40] = 1;
        positions[41] = 8;
        positions[42] = 2;
        positions[43] = 7;
        positions[44] = 3;
        positions[45] = 6;
        positions[46] = 4;
        positions[47] = 0;
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[positions.Length];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, true, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, true);
        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);

        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(lanePositions, false, laneEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(lanePositions, false);
        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
    }

    [Fact]
    public void SetAppliesMixedColumnsForwardAndBackward()
    {
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var second = TimelineAsset.LoadAsset(SecondFiniteBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var secondId = timelines.Add(second);

        var ids = new[] { wideId, secondId, wideId, secondId, wideId, secondId };
        var positions = new ushort[] { 2, 3, 9, 4, 10, 5 };
        var effects = new float[6];

        var widePositions = new ushort[] { 2, 9, 10 };
        var wideEffects = new float[3];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, true, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, true);

        var secondPositions = new ushort[] { 3, 4, 5 };
        var secondEffects = new float[3];
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(secondPositions, true, secondEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(secondPositions, true);

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        Assert.Equal(new[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);

        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, false, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, false);
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(secondPositions, false, secondEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(secondPositions, false);
        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        Assert.Equal(new[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);
    }

    [Fact]
    public void SetAppliesMixedBackwardBoundaryPositions()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new[] { loopingId, wideId, loopingId, wideId, loopingId, wideId, loopingId, wideId };
        var positions = new ushort[] { 6, 9, 7, 10, 0, 12, 3, 1 };
        var effects = new float[8];

        var loopPositions = new ushort[] { 6, 7, 0, 3 };
        var loopEffects = new float[4];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(loopPositions, false, loopEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(loopPositions, false);

        var widePositions = new ushort[] { 9, 10, 12, 1 };
        var wideEffects = new float[4];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, false, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, false);

        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        Assert.Equal(new[] { loopPositions[0], widePositions[0], loopPositions[1], widePositions[1], loopPositions[2], widePositions[2], loopPositions[3], widePositions[3] }, positions);
        Assert.Equal(new[] { loopEffects[0], wideEffects[0], loopEffects[1], wideEffects[1], loopEffects[2], wideEffects[2], loopEffects[3], wideEffects[3] }, effects);
    }

    [Fact]
    public void SetAppliesMixedRunsForward()
    {
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var second = TimelineAsset.LoadAsset(SecondFiniteBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var secondId = timelines.Add(second);

        var ids = new ushort[64];
        Array.Fill(ids, wideId, 0, 40);
        Array.Fill(ids, secondId, 40, 8);
        Array.Fill(ids, wideId, 48, 16);
        var positions = new ushort[64];
        Array.Fill(positions, (ushort)3, 0, 40);
        Array.Fill(positions, (ushort)4, 40, 8);
        Array.Fill(positions, (ushort)5, 48, 8);
        Array.Fill(positions, (ushort)6, 56, 8);
        var effects = new float[64];

        var wideRunPositions = new ushort[40];
        Array.Fill(wideRunPositions, (ushort)3);
        var wideRunEffects = new float[40];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(wideRunPositions, true, wideRunEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(wideRunPositions, true);

        var secondRunPositions = new ushort[8];
        Array.Fill(secondRunPositions, (ushort)4);
        var secondRunEffects = new float[8];
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(secondRunPositions, true, secondRunEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(secondRunPositions, true);

        var tailPositions = new ushort[16];
        Array.Fill(tailPositions, (ushort)5, 0, 8);
        Array.Fill(tailPositions, (ushort)6, 8, 8);
        var tailEffects = new float[16];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(tailPositions, true, tailEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(tailPositions, true);

        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);

        for (var i = 0; i < 40; i++)
        {
            Assert.Equal(wideRunPositions[i], positions[i]);
            Assert.Equal(wideRunEffects[i], effects[i]);
        }
        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(secondRunPositions[i], positions[40 + i]);
            Assert.Equal(secondRunEffects[i], effects[40 + i]);
        }
        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(tailPositions[i], positions[48 + i]);
            Assert.Equal(tailEffects[i], effects[48 + i]);
        }

        var barePositions = new ushort[64];
        Array.Fill(barePositions, (ushort)3, 0, 40);
        Array.Fill(barePositions, (ushort)4, 40, 8);
        Array.Fill(barePositions, (ushort)5, 48, 8);
        Array.Fill(barePositions, (ushort)6, 56, 8);
        var bareEffects = new float[64];
        timelines.Gather(ids).Seek(barePositions, true).Apply(bareEffects); timelines.Advance(ids, barePositions, true);
        Assert.Equal(positions, barePositions);
        Assert.Equal(effects, bareEffects);
    }

    [Fact]
    public void SetAppliesMixedBackwardRunBoundaries()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var wide = TimelineAsset.LoadAsset(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new[] { loopingId, loopingId, wideId, wideId, loopingId, loopingId, wideId, wideId, loopingId, loopingId, wideId, wideId, wideId, wideId, loopingId, loopingId };
        var positions = new ushort[] { 0, 0, 0, 0, 7, 7, 10, 10, 6, 6, 9, 9, 4, 4, 3, 3 };
        var effects = new float[16];

        var loopIndexes = new[] { 0, 1, 4, 5, 8, 9, 14, 15 };
        var wideIndexes = new[] { 2, 3, 6, 7, 10, 11, 12, 13 };
        var loopPositions = new ushort[8];
        for (var i = 0; i < 8; i++)
            loopPositions[i] = positions[loopIndexes[i]];
        var widePositions = new ushort[8];
        for (var i = 0; i < 8; i++)
            widePositions[i] = positions[wideIndexes[i]];
        var loopEffects = new float[8];
        var wideEffects = new float[8];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(loopPositions, false, loopEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(loopPositions, false);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Apply(widePositions, false, wideEffects); Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(widePositions, false);

        timelines.Gather(ids).Seek(positions, false).Apply(effects); timelines.Advance(ids, positions, false);

        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(loopPositions[i], positions[loopIndexes[i]]);
            Assert.Equal(loopEffects[i], effects[loopIndexes[i]]);
            Assert.Equal(widePositions[i], positions[wideIndexes[i]]);
            Assert.Equal(wideEffects[i], effects[wideIndexes[i]]);
        }
    }

    [Fact]
    public void SetRejectsUnboundIdInsideVectorWindow()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var ids = new ushort[20];
        ids[15] = 999;
        var error = Assert.Throws<ArgumentException>(() =>
            timelines.Gather(ids).Seek(new ushort[20], true).Apply(new float[20]));
        Assert.Contains("not bound", error.Message);
    }

    [Fact]
    public void SetRejectsAddBeyondCapacity()
    {
        var fill = new DomainBaker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 1, new LaneClip(1))
            .Bake();
        var last = new DomainBaker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 4, new LaneClip(8))
            .Clip(0, 3, 6, new LaneClip(4))
            .Looping()
            .Bake();
        var timelines = new TimelineSet<LaneTrack, LaneClip>();
        for (var i = 0; i < 65535; i++)
        {
            using var asset = TimelineAsset.LoadAsset(fill);
            Assert.Equal(i, timelines.Add(asset));
        }
        using var lastAsset = TimelineAsset.LoadAsset(last);
        Assert.Equal(65535, timelines.Add(lastAsset));

        var ids = new ushort[17];
        ids[16] = 65535;
        var positions = new ushort[17];
        var effects = new float[17];
        timelines.Gather(ids).Seek(positions, true).Apply(effects); timelines.Advance(ids, positions, true);
        Assert.Equal(1, positions[16]);
        BakedLane<LaneTrack, LaneClip>.Bind(lastAsset);
        Assert.Equal(BakedLane<LaneTrack, LaneClip>.Effect(0), effects[16]);

        var lastIds = new ushort[16];
        Array.Fill(lastIds, (ushort)65535);
        var lastPositions = new ushort[16];
        var lastEffects = new float[16];
        timelines.Gather(lastIds).Seek(lastPositions, true).Apply(lastEffects); timelines.Advance(lastIds, lastPositions, true);
        Assert.Equal(1, lastPositions[0]);

        using (var overflow = TimelineAsset.LoadAsset(fill))
            Assert.Throws<InvalidOperationException>(() => timelines.Add(overflow));
        timelines.Dispose();
    }

    [Fact]
    public void BakedLaneRejectsDisposedAsset()
    {
        var asset = TimelineAsset.LoadAsset(LoopingBake());
        asset.Dispose();
        Assert.Throws<ArgumentException>(() => BakedLane<LaneTrack, LaneClip>.Bind(asset));
    }

    [Fact]
    public void LoadRejectsOversizedDuration()
    {
        Assert.Throws<ArgumentException>(() => TimelineAsset.LoadAsset(new DomainBaker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 70000, new LaneClip(5))
            .Bake()));
    }
}
