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

public readonly record struct OrphanClip(int Value);

public readonly record struct OrphanTrack(int Code) : IBlend<OrphanClip>
{
    public void Blend(in OrphanClip first, in OrphanClip second, float factor, out OrphanClip result) => result = first;
}

public readonly struct LawLane : ITimelineLane<LawLane>
{
    public static uint Duration => 6;
    public static bool Looping => true;
    public static float Effect(uint position) => position;
    public static float InverseEffect(uint position) => -position;
}

public readonly struct LawFiniteLane : ITimelineLane<LawFiniteLane>
{
    public static uint Duration => 6;
    public static bool Looping => false;
    public static float Effect(uint position) => position + 1;
    public static float InverseEffect(uint position) => -(position + 1);
}

internal static unsafe class LanePairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteScale, &BindFloat);
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteConstant, &BindFloat);
        PairRuntime<ImpureTrack, ImpureClip>.Consume(&ExecuteImpure, &BindFloat);
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

    static byte[] OrphanBake() => new Baker()
        .Track<OrphanTrack, OrphanClip>(new OrphanTrack(1))
        .Clip(0, 0, 6, new OrphanClip(5))
        .Looping()
        .Bake();

    [Fact]
    public void SeekMatchesMovementLawLooping()
    {
        for (var position = 0u; position <= 7u; position++)
        for (var seed = 0L; seed <= 1L; seed++)
        for (var forward = 0; forward < 2; forward++)
        {
            var positions = new[] { position };
            var effects = new float[1];
            var cycles = new[] { seed };
            Timeline<LawLane>.Seek(positions, forward == 0).Apply(effects, cycles);
            var moved = TimelineMovement.Select(new TimelineState(1, position, seed), LawLane.Duration, LawLane.Looping, forward != 0, out var next, out _, out _, out _);
            if (moved)
            {
                Assert.Equal(next.Position, positions[0]);
                Assert.Equal(next.Cycle, cycles[0]);
            }
            else
            {
                Assert.Equal(position, positions[0]);
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
            var positions = new[] { position };
            var effects = new float[1];
            var cycles = new long[1];
            Timeline<LawFiniteLane>.Seek(positions, forward == 0).Apply(effects, cycles);
            var moved = TimelineMovement.Select(new TimelineState(1, position, 0), LawFiniteLane.Duration, LawFiniteLane.Looping, forward != 0, out var next, out _, out _, out _);
            if (moved)
            {
                Assert.Equal(next.Position, positions[0]);
                Assert.Equal(0L, cycles[0]);
            }
            else
            {
                Assert.Equal(position, positions[0]);
                Assert.Equal(0L, cycles[0]);
            }
        }
    }

    [Fact]
    public void HandLaneAppliesFoldedEffects()
    {
        var positions = new uint[] { 0, 0, 2, 2, 2, 5 };
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
        Assert.Equal(1u, positions[0]);
        Assert.Equal(3u, positions[2]);
        Assert.Equal(0u, positions[5]);
        Assert.Equal(1L, cycles[5]);
    }

    [Fact]
    public void BakedLaneMatchesFacadeForwardAndBackward()
    {
        const int Rows = 257;
        const int Ticks = 80;
        using var asset = TimelineAsset.Load(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var facadeRows = new TimelineComponent[Rows];
        var facadeColumn = new float[Rows];
        var lanePositions = new uint[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            facadeRows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i * 7 % 6), Cycle = i % 3 - 1 };
            lanePositions[i] = (uint)(i * 7 % 6);
            laneCycles[i] = i % 3 - 1;
        }
        var initialEffects = new float[Rows];
        var initialPositions = (uint[])lanePositions.Clone();
        var initialCycles = (long[])laneCycles.Clone();

        var query = Timeline.Rows(facadeRows).Write(facadeColumn);
        for (var tick = 0u; tick < Ticks; tick++)
        {
            query.Tick(tick);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
            for (var i = 0; i < Rows; i++)
            {
                Assert.Equal(facadeRows[i].Position, lanePositions[i]);
                Assert.Equal(facadeRows[i].Cycle, laneCycles[i]);
                Assert.Equal(facadeColumn[i], laneEffects[i]);
            }
        }

        for (var tick = 0u; tick < Ticks; tick++)
        {
            query.Tick(Ticks - tick, -1);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, false).Apply(laneEffects, laneCycles);
            for (var i = 0; i < Rows; i++)
            {
                Assert.Equal(facadeRows[i].Position, lanePositions[i]);
                Assert.Equal(facadeRows[i].Cycle, laneCycles[i]);
                Assert.Equal(facadeColumn[i], laneEffects[i]);
            }
        }

        Assert.Equal(initialPositions, lanePositions);
        Assert.Equal(initialCycles, laneCycles);
        Assert.Equal(initialEffects, laneEffects);
    }

    [Fact]
    public void BakedLaneCatchUpMatchesFacadeDelta()
    {
        const int Rows = 64;
        using var asset = TimelineAsset.Load(LoopingBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var facadeRows = new TimelineComponent[Rows];
        var facadeColumn = new float[Rows];
        var lanePositions = new uint[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            facadeRows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % 6), Cycle = i / 6 };
            lanePositions[i] = (uint)(i % 6);
            laneCycles[i] = i / 6;
        }

        Timeline.Rows(facadeRows).Write(facadeColumn).Tick(0u, 3);
        for (var call = 0; call < 3; call++)
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);

        for (var i = 0; i < Rows; i++)
        {
            Assert.Equal(facadeRows[i].Position, lanePositions[i]);
            Assert.Equal(facadeRows[i].Cycle, laneCycles[i]);
            Assert.Equal(facadeColumn[i], laneEffects[i]);
        }
    }

    [Fact]
    public void BakedLaneFiniteClampMatchesFacade()
    {
        const int Rows = 16;
        using var asset = TimelineAsset.Load(FiniteBake());
        BakedLane<LaneTrack, LaneClip>.Bind(asset);

        var facadeRows = new TimelineComponent[Rows];
        var facadeColumn = new float[Rows];
        var lanePositions = new uint[Rows];
        var laneEffects = new float[Rows];
        var laneCycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
        {
            facadeRows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % 8) };
            lanePositions[i] = (uint)(i % 8);
        }

        var query = Timeline.Rows(facadeRows).Write(facadeColumn);
        for (var tick = 0u; tick < 10u; tick++)
        {
            query.Tick(tick);
            Timeline<BakedLane<LaneTrack, LaneClip>>.Seek(lanePositions, true).Apply(laneEffects, laneCycles);
        }

        for (var i = 0; i < Rows; i++)
        {
            Assert.Equal(facadeRows[i].Position, lanePositions[i]);
            Assert.Equal(facadeRows[i].Cycle, laneCycles[i]);
            Assert.Equal(facadeColumn[i], laneEffects[i]);
        }
    }

    [Fact]
    public void BakedLaneRejectsImpureConsumer()
    {
        using var asset = TimelineAsset.Load(ImpureBake());
        Assert.Throws<ArgumentException>(() => BakedLane<ImpureTrack, ImpureClip>.Bind(asset));
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
        using var asset = TimelineAsset.Load(OrphanBake());
        Assert.Throws<ArgumentException>(() => BakedLane<OrphanTrack, OrphanClip>.Bind(asset));
    }

    [Fact]
    public void SeekWarmPathAllocatesNothing()
    {
        var positions = new uint[256];
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
        var positions = new uint[4];
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
        var buffer = new uint[10];
        var positions = buffer.AsSpan(0, 4);
        var effects = MemoryMarshal.Cast<uint, float>(buffer.AsSpan(1, 8));
        var cycles = new long[4];
        var threw = false;
        try { Timeline<LawLane>.Seek(positions, true).Apply(effects, cycles); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void SeekTreatsEmptyAndZeroDurationAsNoOp()
    {
        var positions = Array.Empty<uint>();
        Timeline<LawLane>.Seek(positions, true).Apply(Array.Empty<float>(), Array.Empty<long>());

        var effects = new float[1];
        var cycles = new long[1];
        var zero = new uint[] { 3 };
        var zeroDuration = zero;
        Timeline<ZeroLane>.Seek(zeroDuration, true).Apply(effects, cycles);
        Assert.Equal(3u, zeroDuration[0]);
        Assert.Equal(0f, effects[0]);
    }

    readonly struct ZeroLane : ITimelineLane<ZeroLane>
    {
        public static uint Duration => 0;
        public static bool Looping => true;
        public static float Effect(uint position) => 1f;
        public static float InverseEffect(uint position) => -1f;
    }
}
