using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Xunit;

namespace Tl.Core.Tests;

internal static class BlendLog
{
    internal static readonly List<int> Amounts = [];
}

internal static class KernelBakers
{
    internal static byte[] Finite() => new Baker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(9))
        .Clip(0, 0, 1, new AlphaClip(11))
        .Clip(0, 2, 3, new AlphaClip(22))
        .Bake();

    internal static byte[] Looping() => new Baker()
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(8))
        .Clip(0, 0, 2, new AlphaClip(3))
        .Looping()
        .Bake();

    internal static byte[] AbaMirrored() => new Baker()
        .Track<PhiTrack, PhiClip>(new PhiTrack(1))
        .Track<AlphaTrack, AlphaClip>(new AlphaTrack(4))
        .Track<PhiTrack, PhiClip>(new PhiTrack(2))
        .Clip(0, 0, 1, new PhiClip(10))
        .Clip(1, 0, 1, new AlphaClip(7))
        .Clip(2, 0, 1, new PhiClip(20))
        .Bake();

    internal static byte[] BlendSpanThree() => new Baker()
        .Track<BlendTrack, BlendClip>(new BlendTrack(1f))
        .Clip(0, 0, 4, new BlendClip(0f))
        .Clip(0, 2, 6, new BlendClip(10f))
        .Bake();

    internal static byte[] BlendSpanOne() => new Baker()
        .Track<BlendTrack, BlendClip>(new BlendTrack(2f))
        .Clip(0, 0, 3, new BlendClip(0f))
        .Clip(0, 2, 4, new BlendClip(8f))
        .Bake();

    internal static byte[] Consumerless() => new Baker()
        .Track<GammaTrack, GammaClip>(new GammaTrack(3))
        .Clip(0, 0, 2, new GammaClip(5))
        .Bake();

    internal static byte[] Empty() => new Baker().Bake();

    internal static byte[] SpyPair() => new Baker()
        .Track<GammaTrack, GammaClip>(new GammaTrack(9))
        .Clip(0, 0, 1, new GammaClip(7))
        .Bake();

    internal static byte[] InterpreterCopy(byte[] baked)
    {
        var copy = (byte[])baked.Clone();
        copy[40] = 0xA5;
        return copy;
    }
}

public unsafe class KernelTests
{
    static KernelTests()
    {
        PairRuntime<BlendTrack, BlendClip>.Consume(&BlendExecute, &NoBind);
    }

    private static void NoBind(ulong* keys, int keyCount, byte* table)
    {
    }

    private static void BlendExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        BlendClip scratch = default;
        var current = TickFrame.ToFrame<BlendTrack, BlendClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        BlendLog.Amounts.Add(BitConverter.SingleToInt32Bits(current.Clip.Amount));
    }

    internal static unsafe class SpyKernel
    {
        internal static long Calls;
        internal static int RowCount;
        internal static uint GameTick;
        internal static int Delta;

        internal static void Reset() => Calls = RowCount = 0;

        internal static void Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
        {
            Calls++;
            RowCount = rowCount;
            GameTick = gameTick;
            Delta = delta;
        }
    }

    [Fact]
    public void InterpreterCopyDiffersOnlyInTheReservedHeaderWord()
    {
        var baked = KernelBakers.Finite();
        var copy = KernelBakers.InterpreterCopy(baked);
        Assert.Equal(baked.Length, copy.Length);
        var differing = new List<int>();
        for (var i = 0; i < baked.Length; i++)
            if (baked[i] != copy[i])
                differing.Add(i);
        Assert.Equal(new[] { 40 }, differing);
        Assert.Equal(0xA5, copy[40]);
    }

    [Fact]
    public void KernelParityFiniteForwardBackwardAndClamp()
    {
        using var kernelAsset = TimelineAsset.Load(KernelBakers.Finite());
        using var interpreterAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.Finite()));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference) };
        var kernelHealth = new Health[1];
        var interpreterHealth = new Health[1];

        var kernelQuery = Timeline.Rows(kernelRows).Read(new Resistance[1]).Write(kernelHealth);
        var interpreterQuery = Timeline.Rows(interpreterRows).Read(new Resistance[1]).Write(interpreterHealth);

        var finiteBound = TimelineKernels.Bound;
        DataTests.Records.Clear();
        kernelQuery.Tick(100u, 5);
        Assert.Equal(finiteBound + 1, TimelineKernels.Bound);
        var kernelForward = DataTests.Records.ToList();
        Assert.Equal(2, kernelForward.Count);
        Assert.Equal(3u, kernelRows[0].Position);
        Assert.Equal(11f * 9 + 22f * 9, kernelHealth[0].Value);

        DataTests.Records.Clear();
        interpreterQuery.Tick(100u, 5);
        Assert.Equal(finiteBound + 1, TimelineKernels.Bound);
        Assert.Equal(kernelForward, DataTests.Records);
        Assert.Equal(kernelHealth[0].Value, interpreterHealth[0].Value);

        DataTests.Records.Clear();
        kernelQuery.Tick(200u, int.MaxValue);
        Assert.Empty(DataTests.Records);
        Assert.Equal(3u, kernelRows[0].Position);

        DataTests.Records.Clear();
        interpreterQuery.Tick(200u, int.MaxValue);
        Assert.Empty(DataTests.Records);
        Assert.Equal(3u, interpreterRows[0].Position);

        kernelQuery.Tick(200u, 0);
        interpreterQuery.Tick(200u, 0);
        Assert.Equal(3u, kernelRows[0].Position);
        Assert.Equal(3u, interpreterRows[0].Position);

        DataTests.Records.Clear();
        kernelQuery.Tick(103u, -5);
        Assert.Equal(finiteBound + 1, TimelineKernels.Bound);
        var kernelBackward = DataTests.Records.ToList();
        Assert.Equal(2, kernelBackward.Count);
        Assert.Equal(0u, kernelRows[0].Position);
        Assert.Equal(0, kernelRows[0].Cycle);

        DataTests.Records.Clear();
        interpreterQuery.Tick(103u, -5);
        Assert.Equal(kernelBackward, DataTests.Records);
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(kernelRows[0].Cycle, interpreterRows[0].Cycle);
    }

    [Fact]
    public void KernelParityLoopingWrapAndCycles()
    {
        using var kernelAsset = TimelineAsset.Load(KernelBakers.Looping());
        using var interpreterAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.Looping()));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference) };
        var kernelQuery = Timeline.Rows(kernelRows).Read(new Resistance[1]);
        var interpreterQuery = Timeline.Rows(interpreterRows).Read(new Resistance[1]);

        var loopBound = TimelineKernels.Bound;
        DataTests.Records.Clear();
        kernelQuery.Tick(10u, 3);
        Assert.Equal(loopBound + 1, TimelineKernels.Bound);
        var kernelForward = DataTests.Records.ToList();
        Assert.Equal(1u, kernelRows[0].Position);
        Assert.Equal(1, kernelRows[0].Cycle);

        DataTests.Records.Clear();
        interpreterQuery.Tick(10u, 3);
        Assert.Equal(kernelForward, DataTests.Records);
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(kernelRows[0].Cycle, interpreterRows[0].Cycle);

        DataTests.Records.Clear();
        kernelQuery.Tick(13u, -3);
        var kernelBackward = DataTests.Records.ToList();
        Assert.Equal(0u, kernelRows[0].Position);
        Assert.Equal(0, kernelRows[0].Cycle);

        DataTests.Records.Clear();
        interpreterQuery.Tick(13u, -3);
        Assert.Equal(kernelBackward, DataTests.Records);
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(kernelRows[0].Cycle, interpreterRows[0].Cycle);

        DataTests.Records.Clear();
        kernelQuery.Tick(2u, -3);
        var kernelWrap = DataTests.Records.ToList();
        Assert.Equal(3, kernelWrap.Count);
        Assert.Contains(kernelWrap, record => record.Game == 4_294_967_295u);
        Assert.Equal(1u, kernelRows[0].Position);
        Assert.Equal(-2, kernelRows[0].Cycle);

        DataTests.Records.Clear();
        interpreterQuery.Tick(2u, -3);
        Assert.Equal(kernelWrap, DataTests.Records);
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(kernelRows[0].Cycle, interpreterRows[0].Cycle);
    }

    [Fact]
    public void KernelParityAuthoredOrderAndMirroredConsumers()
    {
        using var kernelAsset = TimelineAsset.Load(KernelBakers.AbaMirrored());
        using var interpreterAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.AbaMirrored()));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference) };

        DataTests.Records.Clear();
        Timeline.Rows(kernelRows).Tick(50u, 1);
        Assert.True(kernelRows.Length == 1);
        var kernelForward = DataTests.Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList();
        Assert.Equal(
        [
            ('2', 1, 10f, 0u, 50u, 0L),
            ('1', 1, 10f, 0u, 50u, 0L),
            ('A', 4, 7f, 0u, 50u, 0L),
            ('2', 2, 20f, 0u, 50u, 0L),
            ('1', 2, 20f, 0u, 50u, 0L),
        ], kernelForward);
        Assert.Equal(1u, kernelRows[0].Position);

        DataTests.Records.Clear();
        Timeline.Rows(interpreterRows).Tick(50u, 1);
        Assert.Equal(kernelForward, DataTests.Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        Assert.Equal(1u, interpreterRows[0].Position);

        DataTests.Records.Clear();
        Timeline.Rows(kernelRows).Tick(51u, -1);
        var kernelBackward = DataTests.Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList();
        Assert.Equal(
        [
            ('1', 2, 20f, 0u, 50u, 0L),
            ('2', 2, 20f, 0u, 50u, 0L),
            ('A', 4, 7f, 0u, 50u, 0L),
            ('1', 1, 10f, 0u, 50u, 0L),
            ('2', 1, 10f, 0u, 50u, 0L),
        ], kernelBackward);
        Assert.Equal(0u, kernelRows[0].Position);

        DataTests.Records.Clear();
        Timeline.Rows(interpreterRows).Tick(51u, -1);
        Assert.Equal(kernelBackward, DataTests.Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        Assert.Equal(0u, interpreterRows[0].Position);
    }

    [Fact]
    public void KernelParityBlendOnceIncludingSpanOne()
    {
        using var kernelThree = TimelineAsset.Load(KernelBakers.BlendSpanThree());
        using var interpreterThree = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.BlendSpanThree()));
        var kernelRows = new[] { new TimelineComponent(kernelThree.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterThree.Reference) };

        BlendLog.Amounts.Clear();
        Timeline.Rows(kernelRows).Tick(800u, 6);
        var kernelAmounts = BlendLog.Amounts.ToArray();
        Assert.Equal(6, kernelAmounts.Length);
        Assert.Equal(6u, kernelRows[0].Position);
        Assert.Equal(BitConverter.SingleToInt32Bits(0f), kernelAmounts[0]);
        Assert.Equal(BitConverter.SingleToInt32Bits(0f), kernelAmounts[2]);
        Assert.Equal(BitConverter.SingleToInt32Bits(10f), kernelAmounts[3]);
        Assert.Equal(BitConverter.SingleToInt32Bits(10f), kernelAmounts[5]);

        BlendLog.Amounts.Clear();
        Timeline.Rows(interpreterRows).Tick(800u, 6);
        Assert.Equal(kernelAmounts, BlendLog.Amounts);
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);

        using var kernelOne = TimelineAsset.Load(KernelBakers.BlendSpanOne());
        using var interpreterOne = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.BlendSpanOne()));
        var kernelOneRows = new[] { new TimelineComponent(kernelOne.Reference) };
        var interpreterOneRows = new[] { new TimelineComponent(interpreterOne.Reference) };

        BlendLog.Amounts.Clear();
        Timeline.Rows(kernelOneRows).Tick(900u, 3);
        var kernelOneAmounts = BlendLog.Amounts.ToArray();
        Assert.Equal(3, kernelOneAmounts.Length);
        Assert.Equal(3u, kernelOneRows[0].Position);
        Assert.Equal(BitConverter.SingleToInt32Bits(4f), kernelOneAmounts[2]);

        BlendLog.Amounts.Clear();
        Timeline.Rows(interpreterOneRows).Tick(900u, 3);
        Assert.Equal(kernelOneAmounts, BlendLog.Amounts);
        Assert.Equal(kernelOneRows[0].Position, interpreterOneRows[0].Position);
    }

    [Fact]
    public void KernelParityZeroConsumerPairAndEmptyAsset()
    {
        using var kernelAsset = TimelineAsset.Load(KernelBakers.Consumerless());
        using var interpreterAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.Consumerless()));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference) };

        DataTests.Records.Clear();
        Timeline.Rows(kernelRows).Tick(1u, 5);
        Assert.Empty(DataTests.Records);
        Assert.Equal(2u, kernelRows[0].Position);

        Timeline.Rows(interpreterRows).Tick(1u, 5);
        Assert.Empty(DataTests.Records);
        Assert.Equal(2u, interpreterRows[0].Position);

        Timeline.Rows(kernelRows).Tick(9u, -2);
        Assert.Equal(0u, kernelRows[0].Position);

        using var kernelEmpty = TimelineAsset.Load(KernelBakers.Empty());
        using var interpreterEmpty = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.Empty()));
        var kernelEmptyRows = new[] { new TimelineComponent(kernelEmpty.Reference) };
        var interpreterEmptyRows = new[] { new TimelineComponent(interpreterEmpty.Reference) };
        Timeline.Rows(kernelEmptyRows).Tick(uint.MaxValue, int.MinValue);
        Timeline.Rows(interpreterEmptyRows).Tick(uint.MaxValue, int.MinValue);
        Assert.Equal(0u, kernelEmptyRows[0].Position);
        Assert.Equal(0u, interpreterEmptyRows[0].Position);
        Assert.Empty(DataTests.Records);
    }

    [Fact]
    public void KernelParityUniformMultiRowsTwoPassCommit()
    {
        using var kernelAsset = TimelineAsset.Load(KernelBakers.Finite());
        using var interpreterAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(KernelBakers.Finite()));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference), new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference), new TimelineComponent(interpreterAsset.Reference) };
        var kernelHealth = new Health[2];
        var interpreterHealth = new Health[2];

        var kernelQuery = Timeline.Rows(kernelRows).Write(kernelHealth);
        var interpreterQuery = Timeline.Rows(interpreterRows).Write(interpreterHealth);

        var multiBound = TimelineKernels.Bound;
        DataTests.Records.Clear();
        kernelQuery.Tick(100u, 3);
        Assert.Equal(multiBound + 1, TimelineKernels.Bound);
        var kernelForward = DataTests.Records.ToList();
        Assert.Equal(4, kernelForward.Count);
        Assert.Equal(3u, kernelRows[0].Position);
        Assert.Equal(3u, kernelRows[1].Position);
        Assert.Equal(11f * 9 + 22f * 9, kernelHealth[0].Value);
        Assert.Equal(kernelHealth[0].Value, kernelHealth[1].Value);

        DataTests.Records.Clear();
        interpreterQuery.Tick(100u, 3);
        Assert.Equal(multiBound + 1, TimelineKernels.Bound);
        Assert.Equal(kernelForward, DataTests.Records);
        Assert.Equal(kernelHealth, interpreterHealth);

        DataTests.Records.Clear();
        kernelQuery.Tick(103u, -3);
        var kernelBackward = DataTests.Records.ToList();
        Assert.Equal(4, kernelBackward.Count);
        Assert.Equal(0u, kernelRows[0].Position);
        Assert.Equal(0u, kernelRows[1].Position);

        DataTests.Records.Clear();
        interpreterQuery.Tick(103u, -3);
        Assert.Equal(kernelBackward, DataTests.Records);
        Assert.Equal(kernelHealth, interpreterHealth);
    }

    [Fact]
    public void MixedAssetRowsUseTheInterpreter()
    {
        using var finite = TimelineAsset.Load(KernelBakers.Finite());
        using var looping = TimelineAsset.Load(KernelBakers.Looping());
        var rows = new[]
        {
            new TimelineComponent(finite.Reference),
            new TimelineComponent(looping.Reference),
        };

        var query = Timeline.Rows(rows).Read(new Resistance[2]);
        var mixedBound = TimelineKernels.Bound;
        DataTests.Records.Clear();
        query.Tick(7u, 3);
        Assert.Equal(mixedBound, TimelineKernels.Bound);
        Assert.Equal(3u, rows[0].Position);
        Assert.Equal(1u, rows[1].Position);
        Assert.Equal(1, rows[1].Cycle);
        Assert.Equal(
        [
            ('A', 9, 11f, 0u, 7u),
            ('A', 8, 3f, 0u, 7u),
            ('A', 8, 3f, 1u, 8u),
            ('A', 9, 22f, 2u, 9u),
            ('A', 8, 3f, 0u, 9u),
        ], DataTests.Records.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game)).ToList());
    }

    [Fact]
    public void SpyKernelRegistrationDispatchesOnExactBytes()
    {
        var baked = KernelBakers.SpyPair();
        var hash = SHA256.HashData(baked);
        var before = TimelineKernels.Bound;
        SpyKernel.Reset();
        TimelineKernels.Register(
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(0)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(24)),
            &SpyKernel.Tick);

        using var asset = TimelineAsset.Load(baked);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);
        query.Tick(77u, 1);
        Assert.Equal(before + 1, TimelineKernels.Bound);
        Assert.Equal(0, SpyKernel.Calls);
        Assert.Equal(1u, rows[0].Position);
        query.Tick(76u, 1);
        Assert.Equal(1, SpyKernel.Calls);
        Assert.Equal(1, SpyKernel.RowCount);
        Assert.Equal(76u, SpyKernel.GameTick);
        Assert.Equal(1, SpyKernel.Delta);
        Assert.Equal(1u, rows[0].Position);
        Assert.Equal(before + 1, TimelineKernels.Bound);

        query.Tick(75u, -1);
        Assert.Equal(2, SpyKernel.Calls);
        Assert.Equal(75u, SpyKernel.GameTick);
        Assert.Equal(-1, SpyKernel.Delta);

        using var fallbackAsset = TimelineAsset.Load(KernelBakers.InterpreterCopy(baked));
        var fallbackRows = new[] { new TimelineComponent(fallbackAsset.Reference) };
        var fallbackQuery = Timeline.Rows(fallbackRows);
        fallbackQuery.Tick(5u, 1);
        fallbackQuery.Tick(5u, 1);
        Assert.Equal(2, SpyKernel.Calls);
        Assert.Equal(1u, fallbackRows[0].Position);
        Assert.Equal(before + 1, TimelineKernels.Bound);
    }

    [Fact]
    public void KernelWarmTickAllocatesNoManagedMemory()
    {
        using var asset = TimelineAsset.Load(KernelBakers.Consumerless());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var query = Timeline.Rows(rows);

        var allocBound = TimelineKernels.Bound;
        for (var index = 0; index < 1_000; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
        Assert.Equal(allocBound + 1, TimelineKernels.Bound);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
    }

    [Fact]
    public void CompactingGcBetweenKernelTicksKeepsDispatchWritingCurrentColumns()
    {
        using var asset = TimelineAsset.Load(KernelBakers.Looping());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var query = Timeline.Rows(rows).Read(new Resistance[1]).Write(health);
        var compactionBound = TimelineKernels.Bound;
        query.Tick(1u, 1);
        Assert.Equal(compactionBound + 1, TimelineKernels.Bound);
        var first = health[0].Value;
        Assert.Equal(3f * 8, first);

        var address = (nint)Unsafe.AsPointer(ref health[0]);
        var moved = false;
        for (var attempt = 0; attempt < 20 && !moved; attempt++)
        {
            var junk = new byte[64 * 1024];
            junk[0] = 1;
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            var current = (nint)Unsafe.AsPointer(ref health[0]);
            moved = current != address;
            address = current;
        }
        Assert.True(moved);

        query.Tick(2u, 1);
        Assert.Equal(first * 2, health[0].Value);
    }
}
