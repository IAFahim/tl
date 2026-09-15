using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct ImpureClip(float Amount);

public readonly record struct ImpureTrack(float Scale) : IBlend<ImpureClip>
{
    public void Blend(in ImpureClip first, in ImpureClip second, float factor, out ImpureClip result) => result = first;
}

public readonly record struct OrderClip(float Amount);

public readonly record struct OrderTrack(float Scale) : IBlend<OrderClip>
{
    public void Blend(in OrderClip first, in OrderClip second, float factor, out OrderClip result) => result = first;
}

public readonly record struct CycleClip(float Amount);

public readonly record struct CycleTrack(float Scale) : IBlend<CycleClip>
{
    public void Blend(in CycleClip first, in CycleClip second, float factor, out CycleClip result) => result = first;
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
        PairRuntime<CycleTrack, CycleClip>.Consume(&ExecuteCycle, &BindFloat);
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

    private static void ExecuteScale(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ExecuteConstant(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -7f : 7f;

    private static void ExecuteImpure(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] *= 2f;

    private static void ExecuteOrphan(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        OrphanClip scratch = default;
        var frame = TickFrame.ToFrame<OrphanTrack, OrphanClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Code * frame.Clip.Value;
    }

    private static void ExecuteOrderA(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -16777216f : 16777216f;

    private static void ExecuteOrderB(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -1f : 1f;

    private static void ExecuteOrderC(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? -1f : 1f;

    private static void ExecuteCycle(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += cycle;
}

public class LaneTests
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

    static byte[] ImpureBake() => new Baker()
        .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
        .Clip(0, 0, 6, new ImpureClip(5))
        .Looping()
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

    [Fact]
    public void SeekMatchesMovementLawLooping()
    {
        for (var position = 0u; position <= 7u; position++)
        for (var seed = 0L; seed <= 1L; seed++)
        for (var forward = 0; forward < 2; forward++)
        {
            var positions = new ushort[] { (ushort)position };
            var effects = new float[1];
            var cycles = new[] { seed };
            Timeline<LawLane>.Seek(positions, forward == 0).Apply(effects, cycles);
            var moved = TimelineMovement.Select(new TimelineState(1, position, seed), LawLane.Duration, LawLane.Looping, forward != 0, out var next, out _, out _, out _);
            if (moved)
            {
                Assert.Equal(next.Position, (uint)positions[0]);
                Assert.Equal(next.Cycle, cycles[0]);
            }
            else
            {
                Assert.Equal(position, (uint)positions[0]);
                Assert.Equal(seed, cycles[0]);
            }
        }
    }

    [Fact]
    public void SeekMatchesMovementLawFinite()
    {
        for (var position = 0u; position <= 7u; position++)
        for (var forward = 0; forward < 2; forward++)
        {
            var positions = new ushort[] { (ushort)position };
            var effects = new float[1];
            var cycles = new long[] { position + 5 };
            Timeline<LawFiniteLane>.Seek(positions, forward == 0).Apply(effects, cycles);
            var moved = TimelineMovement.Select(new TimelineState(1, position, 0), LawFiniteLane.Duration, LawFiniteLane.Looping, forward != 0, out var next, out _, out _, out _);
            if (moved)
            {
                Assert.Equal(next.Position, (uint)positions[0]);
                Assert.Equal(0L, cycles[0]);
            }
            else
            {
                Assert.Equal(position, (uint)positions[0]);
                Assert.Equal(position + 5, cycles[0]);
            }
        }
    }

    [Fact]
    public void FiniteLaneAcceptsEmptyCycleColumn()
    {
        var positions = new ushort[] { 0, 3, 5 };
        var effects = new float[3];
        var cycles = new long[] { 9, 8, 7 };
        Timeline<LawFiniteLane>.Seek(positions, true).Apply(effects, Span<long>.Empty);
        Assert.Equal(new ushort[] { 1, 4, 6 }, positions);
        Assert.Equal(new float[] { 1, 4, 6 }, effects);
        Assert.Equal(new long[] { 9, 8, 7 }, cycles);
    }

    [Fact]
    public void LoopingLaneRejectsEmptyCycleColumn()
    {
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        Assert.Throws<ArgumentException>(() => Timeline<LawLane>.Seek(positions, true).Apply(effects, Span<long>.Empty));
    }

    [Fact]
    public void HandLaneAppliesFoldedEffects()
    {
        var positions = new ushort[] { 0, 0, 2, 2, 2, 5 };
        var effects = new float[positions.Length];
        var cycles = new long[positions.Length];

        Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(0f, effects[0]);
        Assert.Equal(0f, effects[1]);
        Assert.Equal(2f, effects[2]);
        Assert.Equal(2f, effects[3]);
        Assert.Equal(2f, effects[4]);
        Assert.Equal(5f, effects[5]);
        Assert.Equal(0L, cycles[0]);
        Assert.Equal(1, (int)positions[0]);
        Assert.Equal(3, (int)positions[2]);
        Assert.Equal(0, (int)positions[5]);
        Assert.Equal(1L, cycles[5]);
    }

    static readonly float[] LoopingEffects = [15f, 26f, 26f, 24f, 22f, 22f];

    static void Simulate(ushort[] positions, long[] cycles, float[] effects, float[] table, bool forward, int calls, ushort duration, bool looping)
    {
        for (var call = 0; call < calls; call++)
            for (var i = 0; i < positions.Length; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, positions[i], cycles[i]), duration, looping, !forward, out var next, out var tick, out _, out _))
                    continue;
                effects[i] += forward ? table[tick] : -table[tick];
                positions[i] = (ushort)next.Position;
                cycles[i] = next.Cycle;
            }
    }

    [Fact]
    public void BakedLaneTablesMatchAuthoredOracle()
    {
        using var asset = TimelineAsset.Load(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, (int)BakedLane<LaneTrack, LaneClip>.Duration);
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
        const int Rows = 257;
        const int Ticks = 80;
        using var asset = TimelineAsset.Load(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var lanePositions = new ushort[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        var oraclePositions = new ushort[Rows];
        var oracleEffects = new float[Rows];
        var oracleCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            lanePositions[i] = oraclePositions[i] = (ushort)(i * 7 % 6);
            laneCycles[i] = oracleCycles[i] = i % 3 - 1;
        }
        var initialEffects = (float[])laneEffects.Clone();
        var initialPositions = (ushort[])lanePositions.Clone();
        var initialCycles = (long[])laneCycles.Clone();

        for (var tick = 0; tick < Ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
            Simulate(oraclePositions, oracleCycles, oracleEffects, LoopingEffects, true, 1, 6, true);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleCycles, laneCycles);
            Assert.Equal(oracleEffects, laneEffects);
        }

        for (var tick = 0; tick < Ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, false).Apply(laneEffects, laneCycles);
            Simulate(oraclePositions, oracleCycles, oracleEffects, LoopingEffects, false, 1, 6, true);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleCycles, laneCycles);
            Assert.Equal(oracleEffects, laneEffects);
        }

        Assert.Equal(initialPositions, lanePositions);
        Assert.Equal(initialCycles, laneCycles);
        Assert.Equal(initialEffects, laneEffects);
    }

    [Fact]
    public void BakedLaneCatchUpMatchesOracleDelta()
    {
        const int Rows = 64;
        using var asset = TimelineAsset.Load(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var lanePositions = new ushort[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        var oraclePositions = new ushort[Rows];
        var oracleEffects = new float[Rows];
        var oracleCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            lanePositions[i] = oraclePositions[i] = (ushort)(i % 6);
            laneCycles[i] = oracleCycles[i] = i / 6;
        }

        for (var call = 0; call < 3; call++)
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
        Simulate(oraclePositions, oracleCycles, oracleEffects, LoopingEffects, true, 3, 6, true);

        Assert.Equal(oraclePositions, lanePositions);
        Assert.Equal(oracleCycles, laneCycles);
        Assert.Equal(oracleEffects, laneEffects);
    }

    [Fact]
    public void BakedLaneFiniteClampMatchesOracle()
    {
        const int Rows = 16;
        const int Ticks = 10;
        using var asset = TimelineAsset.Load(FiniteBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, (int)BakedLane<LaneTrack, LaneClip>.Duration);
        Assert.False(BakedLane<LaneTrack, LaneClip>.Looping);
        var finiteEffects = new float[6];
        for (var tick = 0; tick < 6; tick++)
        {
            finiteEffects[tick] = BakedLane<LaneTrack, LaneClip>.Effect((ushort)tick);
            Assert.Equal(17f, finiteEffects[tick]);
        }

        var lanePositions = new ushort[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        var oraclePositions = new ushort[Rows];
        var oracleEffects = new float[Rows];
        var oracleCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            lanePositions[i] = oraclePositions[i] = (ushort)(i % 8);
            laneCycles[i] = oracleCycles[i] = i + 1;
        }

        for (var tick = 0; tick < Ticks; tick++)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
            Simulate(oraclePositions, oracleCycles, oracleEffects, finiteEffects, true, 1, 6, false);
            Assert.Equal(oraclePositions, lanePositions);
            Assert.Equal(oracleCycles, laneCycles);
            Assert.Equal(oracleEffects, laneEffects);
        }
        Assert.Equal(0L, laneCycles[0]);
        Assert.Equal(7L, laneCycles[6]);
    }

    [Fact]
    public void BakedLaneFoldsEveryPairOfTheAsset()
    {
        using var asset = TimelineAsset.Load(DualPairBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(6, (int)BakedLane<LaneTrack, LaneClip>.Duration);
        for (var tick = 0; tick < 6; tick++)
        {
            var orphan = tick >= 2 && tick < 5 ? 5 * 3f : 0f;
            Assert.Equal(LoopingEffects[tick] + orphan, BakedLane<LaneTrack, LaneClip>.Effect((ushort)tick));
        }
    }

    [Fact]
    public void BakedLanePropagatesConsumerFaults()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<BetaTrack, BetaClip>(new BetaTrack(1))
            .Clip(0, 0, 6, new BetaClip(5))
            .Looping()
            .Bake());
        Assert.Throws<InvalidOperationException>(() => BakedLane<BetaTrack, BetaClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsImpureConsumer()
    {
        using var asset = TimelineAsset.Load(ImpureBake());
        Assert.Throws<ArgumentException>(() => BakedLane<ImpureTrack, ImpureClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsCycleFoldingConsumer()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<CycleTrack, CycleClip>(new CycleTrack(1f))
            .Clip(0, 0, 6, new CycleClip(5f))
            .Looping()
            .Bake());
        Assert.Throws<ArgumentException>(() => BakedLane<CycleTrack, CycleClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneFoldsConsumersInRegisteredOrder()
    {
        using var asset = TimelineAsset.Load(new Baker()
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
        using var asset = TimelineAsset.Load(new Baker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
            .Clip(0, 0, 1, new LaneClip(8))
            .Looping()
            .Bake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        Assert.Equal(1, (int)BakedLane<LaneTrack, LaneClip>.Duration);
        Assert.Equal(23f, BakedLane<LaneTrack, LaneClip>.Effect(0));
        Assert.Equal(-23f, BakedLane<LaneTrack, LaneClip>.InverseEffect(0));

        var positions = new ushort[] { 0 };
        var effects = new float[1];
        var cycles = new long[] { 4 };
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(positions, true).Apply(effects, cycles);
        Assert.Equal(0, (int)positions[0]);
        Assert.Equal(5L, cycles[0]);
        Assert.Equal(23f, effects[0]);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(positions, false).Apply(effects, cycles);
        Assert.Equal(0, (int)positions[0]);
        Assert.Equal(4L, cycles[0]);
        Assert.Equal(0f, effects[0]);
    }

    [Fact]
    public void BakedLaneRejectsAssetWithoutPair()
    {
        using var asset = TimelineAsset.Load(FiniteBake());
        Assert.Throws<ArgumentException>(() => BakedLane<ImpureTrack, ImpureClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsPairWithoutConsumer()
    {
        using var asset = TimelineAsset.Load(GammaBake());
        Assert.Throws<ArgumentException>(() => BakedLane<GammaTrack, GammaClip>.Bind(asset));
    }

    [Fact]
    public void SeekWarmPathAllocatesNothing()
    {
        var positions = new ushort[256];
        var effects = new float[256];
        var cycles = new long[256];

        using (var asset = TimelineAsset.Load(LoopingBake()))
            BakedLane<LaneTrack, LaneClip>.Bind(asset);

        for (var i = 0; i < 100; i++)
        {
            Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(positions, false).Apply(effects, cycles);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++)
        {
            Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(positions, false).Apply(effects, cycles);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void SeekRejectsMismatchedColumns()
    {
        var positions = new ushort[4];
        var effects = new float[3];
        var cycles = new long[4];
        Assert.Throws<ArgumentException>(() => Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles));

        effects = new float[4];
        cycles = new long[5];
        Assert.Throws<ArgumentException>(() => Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles));
    }

    [Fact]
    public unsafe void SeekRejectsOverlappingColumns()
    {
        var buffer = new ushort[10];
        var positions = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var cycles = new long[4];
        var threw = false;
        try { Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SeekTreatsEmptyAndZeroDurationAsNoOp()
    {
        var positions = Array.Empty<ushort>();
        Timeline<LawLane>.Seek(positions, true).Apply(Array.Empty<float>(), Array.Empty<long>());

        var effects = new float[1];
        var cycles = new long[1];
        var zero = new ushort[] { 3 };
        var zeroDuration = zero;
        Timeline<ZeroLane>.Seek(zeroDuration, true).Apply(effects, cycles);
        Assert.Equal(3, (int)zeroDuration[0]);
        Assert.Equal(0f, effects[0]);
    }

    static byte[] ConstantBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 0, 6, new LaneClip(10))
        .Looping()
        .Bake();

    static byte[] WideBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 2, 9, new LaneClip(4))
        .Bake();

    [Fact]
    public void SetAppliesEachTimelineIndependently()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var constant = TimelineAsset.Load(ConstantBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var constantId = timelines.Add(constant);
        Assert.Equal(0, (int)loopingId);
        Assert.Equal(1, (int)constantId);

        var ids = new ushort[] { loopingId, constantId, loopingId };
        var positions = new ushort[] { 0, 0, 1 };
        var effects = new float[3];
        var cycles = new long[3];

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(LoopingEffects[0], effects[0]);
        Assert.Equal(17f, effects[1]);
        Assert.Equal(LoopingEffects[1], effects[2]);
        Assert.Equal(new ushort[] { 1, 1, 2 }, positions);
        Assert.Equal(new long[] { 0, 0, 0 }, cycles);
    }

    [Fact]
    public void SetMatchesStaticLaneBitExact()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var constant = TimelineAsset.Load(ConstantBake());
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var constantId = timelines.Add(constant);
        var wideId = timelines.Add(wide);

        const int Rows = 600;
        var ids = new ushort[Rows];
        var positions = new ushort[Rows];
        var cycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
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
            cycles[i] = i % 3 - 1;
        }
        var effects = new float[Rows];

        var loopRows = new List<int>();
        var constantRows = new List<int>();
        var wideRows = new List<int>();
        for (var i = 0; i < Rows; i++)
        {
            if (ids[i] == loopingId) loopRows.Add(i);
            else if (ids[i] == constantId) constantRows.Add(i);
            else wideRows.Add(i);
        }

        var loopPositions = new ushort[loopRows.Count];
        var loopEffects = new float[loopRows.Count];
        var loopCycles = new long[loopRows.Count];
        var constantPositions = new ushort[constantRows.Count];
        var constantEffects = new float[constantRows.Count];
        var constantCycles = new long[constantRows.Count];
        var widePositions = new ushort[wideRows.Count];
        var wideEffects = new float[wideRows.Count];
        var wideCycles = new long[wideRows.Count];
        for (var j = 0; j < loopRows.Count; j++) { loopPositions[j] = positions[loopRows[j]]; loopCycles[j] = cycles[loopRows[j]]; }
        for (var j = 0; j < constantRows.Count; j++) { constantPositions[j] = positions[constantRows[j]]; constantCycles[j] = cycles[constantRows[j]]; }
        for (var j = 0; j < wideRows.Count; j++) { widePositions[j] = positions[wideRows[j]]; wideCycles[j] = cycles[wideRows[j]]; }

        for (var frame = 0; frame < 80; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(effects, cycles);
            BakedLane<LaneTrack, LaneClip>.Bind(looping);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(loopPositions, forward).Apply(loopEffects, loopCycles);
            BakedLane<LaneTrack, LaneClip>.Bind(constant);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(constantPositions, forward).Apply(constantEffects, constantCycles);
            BakedLane<LaneTrack, LaneClip>.Bind(wide);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, forward).Apply(wideEffects, wideCycles);
            for (var j = 0; j < loopRows.Count; j++)
            {
                var row = loopRows[j];
                Assert.Equal(loopPositions[j], positions[row]);
                Assert.Equal(loopEffects[j], effects[row]);
                Assert.Equal(loopCycles[j], cycles[row]);
            }
            for (var j = 0; j < constantRows.Count; j++)
            {
                var row = constantRows[j];
                Assert.Equal(constantPositions[j], positions[row]);
                Assert.Equal(constantEffects[j], effects[row]);
                Assert.Equal(constantCycles[j], cycles[row]);
            }
            for (var j = 0; j < wideRows.Count; j++)
            {
                var row = wideRows[j];
                Assert.Equal(widePositions[j], positions[row]);
                Assert.Equal(wideEffects[j], effects[row]);
                Assert.Equal(wideCycles[j], cycles[row]);
            }
        }
    }

    [Fact]
    public void SetHandlesSmallCrowds()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new ushort[] { loopingId, wideId, loopingId, wideId, loopingId };
        var positions = new ushort[] { 4, 12, 5, 8, 0 };
        var effects = new float[5];
        var cycles = new long[] { 2, 1, 0, 3, 1 };

        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        var loopPositions = new ushort[] { 4, 5, 0 };
        var loopEffects = new float[3];
        var loopCycles = new long[] { 2, 0, 1 };
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(loopPositions, true).Apply(loopEffects, loopCycles);

        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        var widePositions = new ushort[] { 12, 8 };
        var wideEffects = new float[2];
        var wideCycles = new long[] { 1, 3 };
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, true).Apply(wideEffects, wideCycles);

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(loopEffects[0], effects[0]);
        Assert.Equal(wideEffects[0], effects[1]);
        Assert.Equal(loopEffects[1], effects[2]);
        Assert.Equal(wideEffects[1], effects[3]);
        Assert.Equal(loopEffects[2], effects[4]);
        Assert.Equal(new[] { loopPositions[0], loopPositions[1], loopPositions[2] }, new[] { positions[0], positions[2], positions[4] });
        Assert.Equal(new[] { widePositions[0], widePositions[1] }, new[] { positions[1], positions[3] });
        Assert.Equal(new[] { loopCycles[0], loopCycles[1], loopCycles[2] }, new[] { cycles[0], cycles[2], cycles[4] });
        Assert.Equal(new[] { wideCycles[0], wideCycles[1] }, new[] { cycles[1], cycles[3] });
    }

    [Fact]
    public void SetGatherModeMatchesStaticLane()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        const int Rows = 300;
        var ids = new ushort[Rows];
        Array.Fill(ids, loopingId);
        var positions = new ushort[Rows];
        var cycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            positions[i] = (ushort)(i % 6);
            cycles[i] = i % 3 - 1;
        }
        var effects = new float[Rows];

        var lanePositions = (ushort[])positions.Clone();
        var laneEffects = new float[Rows];
        var laneCycles = (long[])cycles.Clone();

        for (var frame = 0; frame < 80; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(effects, cycles);
            BakedLane<LaneTrack, LaneClip>.Bind(looping);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, forward).Apply(laneEffects, laneCycles);
            Assert.Equal(lanePositions, positions);
            Assert.Equal(laneEffects, effects);
            Assert.Equal(laneCycles, cycles);
        }
    }

    [Fact]
    public void SetGatherLeavesSkippedRowEffectsBitExact()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        var ids = new ushort[33];
        Array.Fill(ids, loopingId);
        var positions = new ushort[33];
        var effects = new float[33];
        var cycles = new long[33];
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = (ushort)(i * 5 % 6);
            cycles[i] = -7;
        }
        positions[16] = 7;
        positions[17] = 6;
        effects[16] = MemoryMarshal.Read<float>(stackalloc byte[4] { 0x01, 0x00, 0x80, 0x7F });
        effects[17] = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(7, (int)positions[16]);
        Assert.Equal(6, (int)positions[17]);
        Assert.Equal(-7, cycles[16]);
        Assert.Equal(-7, cycles[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));

        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);

        Assert.Equal(7, (int)positions[16]);
        Assert.Equal(6, (int)positions[17]);
        Assert.Equal(-7, cycles[16]);
        Assert.Equal(-7, cycles[17]);
        Assert.Equal(0x7F800001u, BitConverter.SingleToUInt32Bits(effects[16]));
        Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(effects[17]));
    }

    [Fact]
    public void SetRejectsUnboundId()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var error = Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0, 1 }).Seek(new ushort[2], true).Apply(new float[2], new long[2]));
        Assert.Contains("not bound", error.Message);
    }

    [Fact]
    public void SetAcceptsEmptyCycleColumnWhenAllTimelinesAreFinite()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);

        var positions = new ushort[] { 2, 7 };
        var effects = new float[2];
        var cycles = new long[] { 9, 8 };

        timelines.Gather(new ushort[] { wideId, wideId }).Seek(positions, true).Apply(effects, Span<long>.Empty);

        Assert.Equal(new ushort[] { 3, 8 }, positions);
        Assert.Equal(new float[] { 11f, 11f }, effects);
        Assert.Equal(new long[] { 9, 8 }, cycles);
    }

    [Fact]
    public void SetRejectsEmptyCycleColumnWhenAnyTimelineLoops()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        timelines.Add(wide);

        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 1, 1 }).Seek(new ushort[] { 2, 3 }, true).Apply(new float[2], Span<long>.Empty));
    }

    [Fact]
    public void SetThrowsAfterDispose()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        var lane = timelines.Gather(new ushort[] { 0 });
        timelines.Dispose();

        Assert.Throws<ObjectDisposedException>(() => timelines.Gather(new ushort[] { 0 }));
        var threw = false;
        try { lane.Seek(new ushort[] { 0 }, true).Apply(new float[1], new long[1]); }
        catch (ObjectDisposedException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsOverlappingColumns()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[10];
        var ids = buffer.AsSpan(0, 4);
        var positions = buffer.AsSpan(1, 4);
        var effects = new float[4];
        var cycles = new long[4];
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
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
        var cycles = new long[256];
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        for (var i = 0; i < 100; i++)
        {
            timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
            timelines.Gather(ids).Seek(staggered, true).Apply(effects, cycles);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++)
        {
            timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
            timelines.Gather(ids).Seek(staggered, true).Apply(effects, cycles);
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
}
