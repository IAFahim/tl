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

    readonly struct ZeroLane : ITimelineLane<ZeroLane>
    {
        public static ushort Duration => 0;
        public static bool Looping => true;
        public static float Effect(ushort position) => 1f;
        public static float InverseEffect(ushort position) => -1f;
    }
}
