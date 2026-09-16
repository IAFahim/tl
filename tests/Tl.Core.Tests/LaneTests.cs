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

public readonly record struct ReverseCycleClip(float Amount);

public readonly record struct ReverseCycleTrack(float Scale) : IBlend<ReverseCycleClip>
{
    public void Blend(in ReverseCycleClip first, in ReverseCycleClip second, float factor, out ReverseCycleClip result)
        => result = new ReverseCycleClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct ReverseFoldClip(float Amount);

public readonly record struct ReverseFoldTrack(float Scale) : IBlend<ReverseFoldClip>
{
    public void Blend(in ReverseFoldClip first, in ReverseFoldClip second, float factor, out ReverseFoldClip result)
        => result = new ReverseFoldClip(first.Amount + (second.Amount - first.Amount) * factor);
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

    static byte[] EmptyBake() => new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Bake();

    static byte[] SecondFiniteBake() => new Baker()
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
        using var empty = TimelineAsset.Load(EmptyBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var emptyId = timelines.Add(empty);
        Assert.Equal(0, (int)emptyId);

        var ids = new ushort[] { emptyId, emptyId };
        var positions = new ushort[] { 0, 5 };
        var effects = new float[2];
        var cycles = new long[2];
        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);
        Assert.Equal(new ushort[] { 0, 5 }, positions);
        Assert.Equal(new float[] { 0f, 0f }, effects);
        Assert.Equal(new long[] { 0L, 0L }, cycles);
    }

    [Fact]
    public void SetRejectsRowColumnMismatch()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0 }).Seek(new ushort[] { 0, 1 }, true).Apply(new float[2], new long[2]));
    }

    [Fact]
    public void SetRejectsEffectsColumnMismatch()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0, 0 }).Seek(new ushort[] { 0, 1 }, true).Apply(new float[3], new long[2]));
    }

    [Fact]
    public void SetRejectsCycleColumnMismatch()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        Assert.Throws<ArgumentException>(() =>
            timelines.Gather(new ushort[] { 0, 0 }).Seek(new ushort[] { 0, 1 }, true).Apply(new float[2], new long[3]));
    }

    [Fact]
    public unsafe void SetRejectsIdsOverlappingEffects()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[12];
        var ids = buffer.AsSpan(0, 4);
        var positions = new ushort[4];
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(2, 8));
        var cycles = new long[4];
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsIdsOverlappingCycles()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[20];
        var ids = buffer.AsSpan(0, 4);
        var positions = new ushort[4];
        var effects = new float[4];
        var cycles = MemoryMarshal.Cast<ushort, long>(buffer.AsSpan(0, 16));
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsPositionsOverlappingEffects()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[10];
        var ids = new ushort[] { 0, 0, 0, 0 };
        var positions = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var cycles = new long[4];
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsPositionsOverlappingCycles()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[20];
        var ids = new ushort[] { 0, 0, 0, 0 };
        var positions = buffer.AsSpan(0, 4);
        var effects = new float[4];
        var cycles = MemoryMarshal.Cast<ushort, long>(buffer.AsSpan(0, 16));
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SetRejectsEffectsOverlappingCycles()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var buffer = new ushort[20];
        var ids = new ushort[] { 0, 0, 0, 0 };
        var positions = new ushort[4];
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(0, 8));
        var cycles = MemoryMarshal.Cast<ushort, long>(buffer.AsSpan(2, 16));
        var threw = false;
        try { timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SetAppliesNothingWhenGatherIsEmpty()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);
        timelines.Gather(Array.Empty<ushort>()).Seek(Array.Empty<ushort>(), true).Apply(Array.Empty<float>(), Array.Empty<long>());
    }

    [Fact]
    public void SetDisposeToleratesEmptyAndRepeatedDispose()
    {
        var empty = new TimelineSet<LaneTrack, LaneClip>();
        empty.Dispose();
        empty.Dispose();

        using var looping = TimelineAsset.Load(LoopingBake());
        var populated = new TimelineSet<LaneTrack, LaneClip>();
        populated.Add(looping);
        populated.Dispose();
        populated.Dispose();
    }

    [Fact]
    public void SetSplitsRowsAcrossChunks()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);

        const int Rows = 5000;
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
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
        Assert.Equal(laneCycles, cycles);
    }

    [Fact]
    public void SetAppliesUniformRunsForward()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var ids = new ushort[64];
        Array.Fill(ids, wideId);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);

        var positions = UniformRunPositions();
        var cycles = new long[positions.Length];
        for (var i = 0; i < cycles.Length; i++)
            cycles[i] = i + 1;
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneCycles = (long[])cycles.Clone();
        var laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
        Assert.Equal(laneCycles, cycles);

        positions = UniformRunPositions();
        var bareEffects = new float[positions.Length];
        lanePositions = (ushort[])positions.Clone();
        laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, Span<long>.Empty);
        timelines.Gather(ids).Seek(positions, true).Apply(bareEffects, Span<long>.Empty);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, bareEffects);

        var singles = new ushort[16];
        for (var i = 0; i < singles.Length; i++)
            singles[i] = (ushort)(i % 11);
        var singleIds = new ushort[16];
        Array.Fill(singleIds, wideId);
        var singleEffects = new float[16];
        var singleCycles = new long[16];
        var singleLanePositions = (ushort[])singles.Clone();
        var singleLaneEffects = new float[16];
        var singleLaneCycles = new long[16];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(singleLanePositions, true).Apply(singleLaneEffects, singleLaneCycles);
        timelines.Gather(singleIds).Seek(singles, true).Apply(singleEffects, singleCycles);
        Assert.Equal(singleLanePositions, singles);
        Assert.Equal(singleLaneEffects, singleEffects);
        Assert.Equal(singleLaneCycles, singleCycles);
    }

    [Fact]
    public void SetAppliesUniformRunsBackward()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var ids = new ushort[70];
        Array.Fill(ids, wideId);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);

        var positions = UniformBackwardRunPositions();
        var cycles = new long[positions.Length];
        for (var i = 0; i < cycles.Length; i++)
            cycles[i] = i + 2;
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneCycles = (long[])cycles.Clone();
        var laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, false).Apply(laneEffects, laneCycles);
        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
        Assert.Equal(laneCycles, cycles);

        positions = UniformBackwardRunPositions();
        var bareEffects = new float[positions.Length];
        lanePositions = (ushort[])positions.Clone();
        laneEffects = new float[positions.Length];
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, false).Apply(laneEffects, Span<long>.Empty);
        timelines.Gather(ids).Seek(positions, false).Apply(bareEffects, Span<long>.Empty);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, bareEffects);
    }

    [Fact]
    public void SetAppliesUniformWrapRuns()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
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
        var cycles = new long[positions.Length];
        var effects = new float[positions.Length];
        var lanePositions = (ushort[])positions.Clone();
        var laneCycles = (long[])cycles.Clone();
        var laneEffects = new float[positions.Length];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
        Assert.Equal(laneCycles, cycles);

        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, false).Apply(laneEffects, laneCycles);
        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);
        Assert.Equal(lanePositions, positions);
        Assert.Equal(laneEffects, effects);
        Assert.Equal(laneCycles, cycles);
    }

    [Fact]
    public void SetAppliesMixedColumnsWithCycles()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var second = TimelineAsset.Load(SecondFiniteBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var secondId = timelines.Add(second);

        var ids = new ushort[] { wideId, secondId, wideId, secondId, wideId, secondId };
        var positions = new ushort[] { 2, 3, 9, 4, 10, 5 };
        var cycles = new long[] { 5, 6, 7, 8, 9, 10 };
        var effects = new float[6];

        var widePositions = new ushort[] { 2, 9, 10 };
        var wideEffects = new float[3];
        var wideCycles = new long[] { 5, 7, 9 };
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, true).Apply(wideEffects, wideCycles);

        var secondPositions = new ushort[] { 3, 4, 5 };
        var secondEffects = new float[3];
        var secondCycles = new long[] { 6, 8, 10 };
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(secondPositions, true).Apply(secondEffects, secondCycles);

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        Assert.Equal(new ushort[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new float[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);
        Assert.Equal(new long[] { wideCycles[0], secondCycles[0], wideCycles[1], secondCycles[1], wideCycles[2], secondCycles[2] }, cycles);

        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, false).Apply(wideEffects, wideCycles);
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(secondPositions, false).Apply(secondEffects, secondCycles);
        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);

        Assert.Equal(new ushort[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new float[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);
        Assert.Equal(new long[] { wideCycles[0], secondCycles[0], wideCycles[1], secondCycles[1], wideCycles[2], secondCycles[2] }, cycles);
    }

    [Fact]
    public void SetAppliesMixedColumnsWithoutCycles()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var second = TimelineAsset.Load(SecondFiniteBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var wideId = timelines.Add(wide);
        var secondId = timelines.Add(second);

        var ids = new ushort[] { wideId, secondId, wideId, secondId, wideId, secondId };
        var positions = new ushort[] { 2, 3, 9, 4, 10, 5 };
        var effects = new float[6];

        var widePositions = new ushort[] { 2, 9, 10 };
        var wideEffects = new float[3];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, true).Apply(wideEffects, Span<long>.Empty);

        var secondPositions = new ushort[] { 3, 4, 5 };
        var secondEffects = new float[3];
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(secondPositions, true).Apply(secondEffects, Span<long>.Empty);

        timelines.Gather(ids).Seek(positions, true).Apply(effects, Span<long>.Empty);

        Assert.Equal(new ushort[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new float[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);

        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, false).Apply(wideEffects, Span<long>.Empty);
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(secondPositions, false).Apply(secondEffects, Span<long>.Empty);
        timelines.Gather(ids).Seek(positions, false).Apply(effects, Span<long>.Empty);

        Assert.Equal(new ushort[] { widePositions[0], secondPositions[0], widePositions[1], secondPositions[1], widePositions[2], secondPositions[2] }, positions);
        Assert.Equal(new float[] { wideEffects[0], secondEffects[0], wideEffects[1], secondEffects[1], wideEffects[2], secondEffects[2] }, effects);
    }

    [Fact]
    public void SetAppliesMixedBackwardBoundaryPositions()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new ushort[] { loopingId, wideId, loopingId, wideId, loopingId, wideId, loopingId, wideId };
        var positions = new ushort[] { 6, 9, 7, 10, 0, 12, 3, 1 };
        var cycles = new long[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var effects = new float[8];

        var loopPositions = new ushort[] { 6, 7, 0, 3 };
        var loopEffects = new float[4];
        var loopCycles = new long[] { 1, 3, 5, 7 };
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(loopPositions, false).Apply(loopEffects, loopCycles);

        var widePositions = new ushort[] { 9, 10, 12, 1 };
        var wideEffects = new float[4];
        var wideCycles = new long[] { 2, 4, 6, 8 };
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, false).Apply(wideEffects, wideCycles);

        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);

        Assert.Equal(new ushort[] { loopPositions[0], widePositions[0], loopPositions[1], widePositions[1], loopPositions[2], widePositions[2], loopPositions[3], widePositions[3] }, positions);
        Assert.Equal(new float[] { loopEffects[0], wideEffects[0], loopEffects[1], wideEffects[1], loopEffects[2], wideEffects[2], loopEffects[3], wideEffects[3] }, effects);
        Assert.Equal(new long[] { loopCycles[0], wideCycles[0], loopCycles[1], wideCycles[1], loopCycles[2], wideCycles[2], loopCycles[3], wideCycles[3] }, cycles);
    }

    [Fact]
    public void SetAppliesMixedRunsForward()
    {
        using var wide = TimelineAsset.Load(WideBake());
        using var second = TimelineAsset.Load(SecondFiniteBake());
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
        var cycles = new long[64];
        var effects = new float[64];

        var wideRunPositions = new ushort[40];
        Array.Fill(wideRunPositions, (ushort)3);
        var wideRunEffects = new float[40];
        var wideRunCycles = new long[40];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(wideRunPositions, true).Apply(wideRunEffects, wideRunCycles);

        var secondRunPositions = new ushort[8];
        Array.Fill(secondRunPositions, (ushort)4);
        var secondRunEffects = new float[8];
        var secondRunCycles = new long[8];
        BakedLane<LaneTrack, LaneClip>.Bind(second);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(secondRunPositions, true).Apply(secondRunEffects, secondRunCycles);

        var tailPositions = new ushort[16];
        Array.Fill(tailPositions, (ushort)5, 0, 8);
        Array.Fill(tailPositions, (ushort)6, 8, 8);
        var tailEffects = new float[16];
        var tailCycles = new long[16];
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(tailPositions, true).Apply(tailEffects, tailCycles);

        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);

        for (var i = 0; i < 40; i++)
        {
            Assert.Equal(wideRunPositions[i], positions[i]);
            Assert.Equal(wideRunEffects[i], effects[i]);
            Assert.Equal(wideRunCycles[i], cycles[i]);
        }
        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(secondRunPositions[i], positions[40 + i]);
            Assert.Equal(secondRunEffects[i], effects[40 + i]);
            Assert.Equal(secondRunCycles[i], cycles[40 + i]);
        }
        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(tailPositions[i], positions[48 + i]);
            Assert.Equal(tailEffects[i], effects[48 + i]);
            Assert.Equal(tailCycles[i], cycles[48 + i]);
        }

        var barePositions = new ushort[64];
        Array.Fill(barePositions, (ushort)3, 0, 40);
        Array.Fill(barePositions, (ushort)4, 40, 8);
        Array.Fill(barePositions, (ushort)5, 48, 8);
        Array.Fill(barePositions, (ushort)6, 56, 8);
        var bareEffects = new float[64];
        timelines.Gather(ids).Seek(barePositions, true).Apply(bareEffects, Span<long>.Empty);
        Assert.Equal(positions, barePositions);
        Assert.Equal(effects, bareEffects);
    }

    [Fact]
    public void SetAppliesMixedBackwardRunBoundaries()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var wide = TimelineAsset.Load(WideBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        var loopingId = timelines.Add(looping);
        var wideId = timelines.Add(wide);

        var ids = new ushort[] { loopingId, loopingId, wideId, wideId, loopingId, loopingId, wideId, wideId, loopingId, loopingId, wideId, wideId, wideId, wideId, loopingId, loopingId };
        var positions = new ushort[] { 0, 0, 0, 0, 7, 7, 10, 10, 6, 6, 9, 9, 4, 4, 3, 3 };
        var cycles = new long[16];
        var effects = new float[16];

        var loopIndexes = new[] { 0, 1, 4, 5, 8, 9, 14, 15 };
        var wideIndexes = new[] { 2, 3, 6, 7, 10, 11, 12, 13 };
        var loopPositions = new ushort[8];
        var loopCycles = new long[8];
        for (var i = 0; i < 8; i++)
        {
            loopPositions[i] = positions[loopIndexes[i]];
            loopCycles[i] = i + 3;
            cycles[loopIndexes[i]] = loopCycles[i];
        }
        var widePositions = new ushort[8];
        var wideCycles = new long[8];
        for (var i = 0; i < 8; i++)
        {
            widePositions[i] = positions[wideIndexes[i]];
            wideCycles[i] = i + 11;
            cycles[wideIndexes[i]] = wideCycles[i];
        }
        var loopEffects = new float[8];
        var wideEffects = new float[8];
        BakedLane<LaneTrack, LaneClip>.Bind(looping);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(loopPositions, false).Apply(loopEffects, loopCycles);
        BakedLane<LaneTrack, LaneClip>.Bind(wide);
        Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(widePositions, false).Apply(wideEffects, wideCycles);

        timelines.Gather(ids).Seek(positions, false).Apply(effects, cycles);

        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(loopPositions[i], positions[loopIndexes[i]]);
            Assert.Equal(loopEffects[i], effects[loopIndexes[i]]);
            Assert.Equal(loopCycles[i], cycles[loopIndexes[i]]);
            Assert.Equal(widePositions[i], positions[wideIndexes[i]]);
            Assert.Equal(wideEffects[i], effects[wideIndexes[i]]);
            Assert.Equal(wideCycles[i], cycles[wideIndexes[i]]);
        }
    }

    [Fact]
    public void SetRejectsUnboundIdInsideVectorWindow()
    {
        using var looping = TimelineAsset.Load(LoopingBake());
        using var timelines = new TimelineSet<LaneTrack, LaneClip>();
        timelines.Add(looping);

        var ids = new ushort[20];
        ids[15] = 999;
        var error = Assert.Throws<ArgumentException>(() =>
            timelines.Gather(ids).Seek(new ushort[20], true).Apply(new float[20], new long[20]));
        Assert.Contains("not bound", error.Message);
    }

    [Fact]
    public void SetRejectsAddBeyondCapacity()
    {
        var fill = new Baker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 1, new LaneClip(1))
            .Bake();
        var last = new Baker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 4, new LaneClip(8))
            .Clip(0, 3, 6, new LaneClip(4))
            .Looping()
            .Bake();
        var timelines = new TimelineSet<LaneTrack, LaneClip>();
        for (var i = 0; i < 65535; i++)
        {
            using var asset = TimelineAsset.Load(fill);
            Assert.Equal(i, (int)timelines.Add(asset));
        }
        using var lastAsset = TimelineAsset.Load(last);
        Assert.Equal(65535, (int)timelines.Add(lastAsset));

        var ids = new ushort[17];
        ids[16] = 65535;
        var positions = new ushort[17];
        var effects = new float[17];
        var cycles = new long[17];
        timelines.Gather(ids).Seek(positions, true).Apply(effects, cycles);
        Assert.Equal(1, (int)positions[16]);
        BakedLane<LaneTrack, LaneClip>.Bind(lastAsset);
        Assert.Equal(BakedLane<LaneTrack, LaneClip>.Effect(0), effects[16]);

        var lastIds = new ushort[16];
        Array.Fill(lastIds, (ushort)65535);
        var lastPositions = new ushort[16];
        var lastEffects = new float[16];
        var lastCycles = new long[16];
        timelines.Gather(lastIds).Seek(lastPositions, true).Apply(lastEffects, lastCycles);
        Assert.Equal(1, (int)lastPositions[0]);

        using (var overflow = TimelineAsset.Load(fill))
            Assert.Throws<InvalidOperationException>(() => timelines.Add(overflow));
        timelines.Dispose();
    }

    [Fact]
    public void SeekRejectsPositionsOverlappingCycles()
    {
        var buffer = new ushort[20];
        var positions = buffer.AsSpan(0, 4);
        var effects = new float[4];
        var cycles = MemoryMarshal.Cast<ushort, long>(buffer.AsSpan(0, 16));
        var threw = false;
        try { Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public unsafe void SeekRejectsEffectsOverlappingCycles()
    {
        var buffer = new ushort[20];
        var positions = new ushort[] { 0, 1, 2, 3 };
        var effects = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(0, 8));
        var cycles = MemoryMarshal.Cast<ushort, long>(buffer.AsSpan(2, 16));
        var threw = false;
        try { Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void BakedLaneRejectsDisposedAsset()
    {
        var asset = TimelineAsset.Load(LoopingBake());
        asset.Dispose();
        Assert.Throws<ArgumentException>(() => BakedLane<LaneTrack, LaneClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsOversizedDuration()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
            .Clip(0, 0, 70000, new LaneClip(5))
            .Bake());
        Assert.Throws<ArgumentException>(() => BakedLane<LaneTrack, LaneClip>.Bind(asset));
    }
}

public unsafe class LaneBackwardPurityFaultTests
{
    static LaneBackwardPurityFaultTests()
    {
        PairRuntime<ReverseCycleTrack, ReverseCycleClip>.Consume(&ExecuteReverseCycle, &BindFloat);
        PairRuntime<ReverseFoldTrack, ReverseFoldClip>.Consume(&ExecuteReverseFold, &BindFloat);
    }

    static void ExecuteReverseCycle(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += (flags & FrameFlags.Reverse) != 0 ? cycle : 0f;

    static void ExecuteReverseFold(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        if ((flags & FrameFlags.Reverse) != 0)
            ((float*)columns[0])[row] *= 2f;
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    [Fact]
    public void BakedLaneRejectsBackwardCycleFoldingConsumer()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<ReverseCycleTrack, ReverseCycleClip>(new ReverseCycleTrack(1f))
            .Clip(0, 0, 6, new ReverseCycleClip(5f))
            .Looping()
            .Bake());
        Assert.Throws<ArgumentException>(() => BakedLane<ReverseCycleTrack, ReverseCycleClip>.Bind(asset));
    }

    [Fact]
    public void BakedLaneRejectsBackwardSeedFoldingConsumer()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<ReverseFoldTrack, ReverseFoldClip>(new ReverseFoldTrack(1f))
            .Clip(0, 0, 6, new ReverseFoldClip(5f))
            .Looping()
            .Bake());
        Assert.Throws<ArgumentException>(() => BakedLane<ReverseFoldTrack, ReverseFoldClip>.Bind(asset));
    }
}
