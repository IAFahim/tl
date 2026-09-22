using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct BatchRowsClip(float Amount);

public readonly record struct BatchRowsTrack(float Scale) : IBlend<BatchRowsClip>
{
    public void Blend(in BatchRowsClip first, in BatchRowsClip second, float factor, out BatchRowsClip result)
        => result = new BatchRowsClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class BatchRowsPairs
{
    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<BatchRowsTrack, BatchRowsClip>.Consume(&ExecuteScale, &BindFloat);

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        BatchRowsClip scratch = default;
        var frame = TickFrame.ToFrame<BatchRowsTrack, BatchRowsClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }
}

public class BatchedRowsTests
{
    const ushort Duration = 4;

    static byte[] LoopingBake() => new DomainBaker()
        .Track<BatchRowsTrack, BatchRowsClip>(new BatchRowsTrack(1f))
        .Clip(0, 0, Duration, new BatchRowsClip(3f))
        .Looping()
        .Bake();

    static byte[] FiniteBake() => new DomainBaker()
        .Track<BatchRowsTrack, BatchRowsClip>(new BatchRowsTrack(1f))
        .Clip(0, 0, Duration, new BatchRowsClip(3f))
        .Bake();

    static byte[] LoopingBakeTwo() => new DomainBaker()
        .Track<BatchRowsTrack, BatchRowsClip>(new BatchRowsTrack(1f))
        .Clip(0, 0, Duration, new BatchRowsClip(2f))
        .Looping()
        .Bake();

    public static TheoryData<int> Counts => new() { 1, 2, 8, 64, 1000 };

    [Theory]
    [MemberData(nameof(Counts))]
    public void BatchMatchesSequentialPerEntityCalls(int count)
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var second = TimelineAsset.LoadAsset(LoopingBakeTwo());
        using var finite = TimelineAsset.LoadAsset(FiniteBake());
        ushort[] single = SingleIdColumn(count, looping.Index);
        ushort[] mixed = MixedIdColumn(count, looping.Index, second.Index);
        ushort[] clamped = SingleIdColumn(count, finite.Index);

        foreach (var ids in new[] { single, mixed, clamped })
        foreach (var forward in new[] { true, false })
        {
            var positions = BoundaryPositions(count);
            var effects = SeededEffects(count);
            var rows = ScatteredRows(count);

            var expected = Sequential(ids, positions, effects, rows, forward);

            var batchPositions = (ushort[])positions.Clone();
            var batchEffects = (float[])effects.Clone();
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, batchPositions, forward, batchEffects);
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, batchPositions, forward);

            Assert.Equal(expected.Positions, batchPositions);
            Assert.Equal(expected.Effects, batchEffects);
        }
    }

    [Fact]
    public void SingleRowBatchFoldsAndMovesExactlyLikeThePerEntityLaw()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var finite = TimelineAsset.LoadAsset(FiniteBake());
        var rows = new[] { 0 };

        ushort[] loopingPosition = [ 3 ];
        float[] loopingEffect = [ 1.5f ];
        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, new[] { looping.Index }, loopingPosition, true, loopingEffect);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, new[] { looping.Index }, loopingPosition, true);
        Assert.Equal(0, loopingPosition[0]);
        Assert.Equal(4.5f, loopingEffect[0]);

        ushort[] finitePosition = [ 3 ];
        float[] finiteEffect = [ 1.5f ];
        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, new[] { finite.Index }, finitePosition, true, finiteEffect);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, new[] { finite.Index }, finitePosition, true);
        Assert.Equal(Duration, (int)finitePosition[0]);
        Assert.Equal(4.5f, finiteEffect[0]);
    }

    [Fact]
    public void BoundaryPositionsFoldExactlyLikeThePerEntityOverload()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var ids = SingleIdColumn(6, looping.Index);
        var rows = new[] { 0, 1, 2, 3, 4, 5 };
#if TL_CHECKED
        ushort[] positions = [ 0, 3, 4, 2, 1, 4 ];
#else
        ushort[] positions = [ 0, 3, 4, 5, 1, 7 ];
#endif
        float[] effects = [ 0f, 0f, 0f, 0f, 0f, 0f ];

        var expected = Sequential(ids, positions, effects, rows, forward: true);
        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);

        Assert.Equal(expected.Positions, positions);
        Assert.Equal(expected.Effects, effects);
#if TL_CHECKED
        Assert.Equal([ 1, 0, 4, 3, 2, 4 ], positions);
        Assert.Equal([ 3f, 3f, 0f, 3f, 3f, 0f ], effects);
#else
        Assert.Equal([ 1, 0, 4, 5, 2, 7 ], positions);
        Assert.Equal([ 3f, 3f, 0f, 0f, 3f, 0f ], effects);
#endif
    }

    [Fact]
    public void DuplicateRowsFoldPerOccurrenceWithoutAdvancingPositions()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var ids = SingleIdColumn(4, looping.Index);
        var rows = new[] { 3, 3, 0 };

        ushort[] positions = [ 2, 0, 1, 3 ];
        float[] effects = [ 0f, 10f, 20f, 1.5f ];
        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
        Assert.Equal([ 2, 0, 1, 3 ], positions);
        Assert.Equal([ 3f, 10f, 20f, 7.5f ], effects);

        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);
        Assert.Equal([ 3, 0, 1, 1 ], positions);
    }

    [Fact]
    public void DuplicateRowsMatchSequentialPerEntityCallsInBothDirections()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var finite = TimelineAsset.LoadAsset(FiniteBake());
        ushort[] ids = [ looping.Index, looping.Index, finite.Index, looping.Index, finite.Index, looping.Index ];
        var rows = new[] { 3, 3, 2, 3, 5, 5 };
#if TL_CHECKED
        ushort[] positions = [ 3, 1, 4, 2, 4, 0 ];
#else
        ushort[] positions = [ 3, 1, 4, 2, 5, 0 ];
#endif
        var effects = SeededEffects(6);

        foreach (var forward in new[] { true, false })
        {
            var expected = Sequential(ids, positions, effects, rows, forward);

            var batchPositions = (ushort[])positions.Clone();
            var batchEffects = (float[])effects.Clone();
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, batchPositions, forward, batchEffects);
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, batchPositions, forward);

            Assert.Equal(expected.Positions, batchPositions);
            Assert.Equal(expected.Effects, batchEffects);
        }
    }

    [Fact]
    public void EmptyBatchChangesNothing()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var ids = SingleIdColumn(2, looping.Index);
        var rows = Array.Empty<int>();
        ushort[] positions = [ 2, 3 ];
        float[] effects = [ 1f, 2f ];

        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);

        Assert.Equal([ 2, 3 ], positions);
        Assert.Equal([ 1f, 2f ], effects);
    }

    [Fact]
    public void BatchWalksRewindBitExactly()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var finite = TimelineAsset.LoadAsset(FiniteBake());
        var rows = ScatteredRows(64);

        var ids = SingleIdColumn(64, looping.Index);
        var positions = BoundaryPositions(64);
        var effects = SeededEffects(64);
        var originalPositions = (ushort[])positions.Clone();
        var originalEffects = (float[])effects.Clone();

        Walk(rows, ids, positions, effects, 40);
        Rewind(rows, ids, positions, effects, 40);

        Assert.Equal(originalPositions, positions);
        Assert.Equal(originalEffects, effects);

        Array.Fill(ids, finite.Index);
        Array.Clear(positions);
        Array.Clear(effects);

        Walk(rows, ids, positions, effects, Duration);
        Assert.Equal(new ushort[64].Select(_ => Duration), positions);
        Assert.Equal(new float[64].Select(_ => Duration * 3f), effects);

        Rewind(rows, ids, positions, effects, Duration);
        Assert.Equal(new ushort[64], positions);
        Assert.Equal(new float[64], effects);
    }

    static void Walk(int[] rows, ushort[] ids, ushort[] positions, float[] effects, int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);
        }
    }

    static void Rewind(int[] rows, ushort[] ids, ushort[] positions, float[] effects, int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, false, effects);
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, false);
        }
    }

    [Fact]
    public void FirstUseResolvesUnboundIdsInsideTheBatch()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var ids = SingleIdColumn(3, looping.Index);
        var rows = new[] { 0, 1, 2 };
        ushort[] positions = [ 1, 2, 3 ];
        float[] effects = [ 0f, 0f, 0f ];

        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);

        Assert.Equal([ 2, 3, 0 ], positions);
        Assert.Equal([ 3f, 3f, 3f ], effects);
    }

    [Fact]
    public void HostStructColumnsMatchUshortColumns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var rows = new[] { 2, 0, 1 };
        TestIndex[] ids = [ new(looping.Index), new(looping.Index), new(looping.Index) ];
        TestPosition[] positions = [ new(0), new(3), new(2) ];
        var effects = new TestEffect[] { new() { Value = 0.5f }, new() { Value = 1f }, new() { Value = 2f } };

        Timeline<BatchRowsTrack, BatchRowsClip>.Apply(rows, ids, positions, true, effects);
        Timeline<BatchRowsTrack, BatchRowsClip>.Advance(rows, ids, positions, true);

        Assert.Equal([ 1, 0, 3 ], positions.Select(p => p.Value));
        Assert.Equal([ 3.5f, 4f, 5f ], effects.Select(e => e.Value));
    }

    [Fact]
    public void WrongColumnSizesAreRejected()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        int[] ids = [ looping.Index ];
        Assert.Throws<ArgumentException>(() =>
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(new[] { 0 }, ids, new ushort[1], true, new float[1]));
        Assert.Throws<ArgumentException>(() =>
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(new[] { 0 }, ids, new int[1], true));
    }

    [Fact]
    public void NonLazySetRejectsUnboundIdsWithTheLocatedRow()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0, 1], [0, 1], [0, 0], true, [0f, 0f]));
        Assert.Throws<ArgumentException>(() => set.AdvanceRows([1], [0, 1], [0, 0], true));
    }

    [Fact]
    public void NonLazySetAppliesBoundIdsAcrossScatteredRows()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        var id = set.Add(looping);
        var rows = new[] { 2, 0, 1 };
        ushort[] ids = [ id, id, id ];
        ushort[] positions = [ 0, 3, 2 ];
        float[] effects = [ 0f, 0f, 0f ];

        set.ApplyRows(rows, ids, positions, true, effects);
        set.AdvanceRows(rows, ids, positions, true);

        Assert.Equal([ 1, 0, 3 ], positions);
        Assert.Equal([ 3f, 3f, 3f ], effects);
    }

    static (ushort[] Positions, float[] Effects) Sequential(ushort[] ids, ushort[] positions, float[] effects, int[] rows, bool forward)
    {
        var pos = (ushort[])positions.Clone();
        var fx = (float[])effects.Clone();
        foreach (var row in rows)
        {
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply(ids[row], pos[row], forward, ref fx[row]);
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance(ids[row], ref pos[row], forward);
        }
        return (pos, fx);
    }

    static ushort[] SingleIdColumn(int count, ushort id)
    {
        var ids = new ushort[count];
        Array.Fill(ids, id);
        return ids;
    }

    static ushort[] MixedIdColumn(int count, ushort first, ushort second)
    {
        var ids = new ushort[count];
        for (var i = 0; i < count; i++)
            ids[i] = i % 2 == 0 ? first : second;
        return ids;
    }

    static ushort[] BoundaryPositions(int count)
    {
        ushort[] boundaries = [0, 1, 3, 4, 5, 7, 2, 6];
        var positions = new ushort[count];
        for (var i = 0; i < count; i++)
        {
            var boundary = boundaries[i % boundaries.Length];
#if TL_CHECKED
            if (boundary > Duration) boundary = Duration;
#endif
            positions[i] = boundary;
        }
        return positions;
    }

    static float[] SeededEffects(int count)
    {
        var effects = new float[count];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < count; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = MathF.Round((float)((state >> 11) / 9007199254740992d) * 128f - 64f) / 4f;
        }
        return effects;
    }

    static int[] ScatteredRows(int count)
    {
        var rows = new int[count];
        for (var i = 0; i < count; i++) rows[i] = i;
        var state = 0x9E3779B97F4A7C15ul;
        for (var i = count - 1; i > 0; i--)
        {
            state = state * 6364136223846793005ul + 1442695040888963407ul;
            var j = (int)((state >> 33) % (ulong)(i + 1));
            (rows[i], rows[j]) = (rows[j], rows[i]);
        }
        return rows;
    }
}

#if TL_CHECKED
public class BatchedRowsCheckedTests
{
    const ushort Duration = 4;

    static byte[] LoopingBake() => new DomainBaker()
        .Track<BatchRowsTrack, BatchRowsClip>(new BatchRowsTrack(1f))
        .Clip(0, 0, Duration, new BatchRowsClip(3f))
        .Looping()
        .Bake();

    [Fact]
    public void DisposedSetThrowsOnBatchApplyAndAdvance()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);
        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.ApplyRows([0], [0], [0], true, [0f]));
        Assert.Throws<ObjectDisposedException>(() => set.AdvanceRows([0], [0], [0], true));
    }

    [Fact]
    public void BatchRejectsMismatchedColumnLengths()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0], [0], new ushort[2], true, new float[2]));
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0], [0], new ushort[1], true, new float[2]));
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0], new ushort[2], new ushort[1], true, new float[1]));
        Assert.Throws<ArgumentException>(() => set.AdvanceRows([0], [0], new ushort[2], true));
    }

    [Fact]
    public void BatchRejectsOverlappingColumns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new float[16];
            var positions = MemoryMarshal.Cast<float, ushort>(buffer);
            set.ApplyRows([0], positions.Slice(0, 8), positions.Slice(0, 8), true, buffer.AsSpan(1, 8));
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new float[16];
            var positions = MemoryMarshal.Cast<float, ushort>(buffer);
            set.ApplyRows([0], new ushort[8], positions.Slice(0, 8), true, buffer.AsSpan(2, 8));
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new float[16];
            var positions = MemoryMarshal.Cast<float, ushort>(buffer);
            set.AdvanceRows([0], positions.Slice(0, 8), MemoryMarshal.Cast<float, ushort>(buffer.AsSpan(2, 4)), true);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new float[16];
            var rows = MemoryMarshal.Cast<float, int>(buffer.AsSpan(2, 2));
            var positions = MemoryMarshal.Cast<float, ushort>(buffer);
            set.ApplyRows(rows, positions.Slice(0, 8), positions.Slice(8, 8), true, new float[8]);
        });
    }

    [Fact]
    public void BatchRejectsRowsOverlappingPositionAndEffectColumns()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);

        var positionBuffer = new float[16];
        var rowsOverPositions = MemoryMarshal.Cast<float, int>(positionBuffer.AsSpan(0, 2));
        var positions = MemoryMarshal.Cast<float, ushort>(positionBuffer.AsSpan(1, 2));
        var threw = false;
        try { set.ApplyRows(rowsOverPositions, new ushort[4], positions, true, new float[4]); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);

        var effectBuffer = new float[16];
        var rowsOverEffects = MemoryMarshal.Cast<float, int>(effectBuffer.AsSpan(0, 2));
        threw = false;
        try { set.ApplyRows(rowsOverEffects, new ushort[4], new ushort[4], true, effectBuffer.AsSpan(1, 4)); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);

        var advanceBuffer = new float[16];
        var rowsOverAdvancePositions = MemoryMarshal.Cast<float, int>(advanceBuffer.AsSpan(0, 2));
        var advancePositions = MemoryMarshal.Cast<float, ushort>(advanceBuffer.AsSpan(1, 2));
        threw = false;
        try { set.AdvanceRows(rowsOverAdvancePositions, new ushort[4], advancePositions, true); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);

        var publicBuffer = new float[16];
        var publicRows = MemoryMarshal.Cast<float, int>(publicBuffer.AsSpan(0, 2));
        threw = false;
        try { Timeline<BatchRowsTrack, BatchRowsClip>.Apply(publicRows, new ushort[4], new ushort[4], true, publicBuffer.AsSpan(1, 4)); }
        catch (ArgumentException ex) { threw = ex.Message == "Lane columns must not overlap."; }
        Assert.True(threw);
    }

    [Fact]
    public void BatchRejectsRowsOutsideTheColumnsWithALocatedDiagnostic()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        using var set = new TimelineSet<BatchRowsTrack, BatchRowsClip>();
        set.Add(looping);
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0, 8], new ushort[8], new ushort[8], true, new float[8]));
        Assert.Throws<ArgumentException>(() => set.ApplyRows([0, -1], new ushort[8], new ushort[8], true, new float[8]));
        Assert.Throws<ArgumentException>(() => set.AdvanceRows([9], new ushort[8], new ushort[8], true));
    }

    [Fact]
    public void PublicBatchSurfaceCarriesTheSameCheckedDiagnostics()
    {
        using var looping = TimelineAsset.LoadAsset(LoopingBake());
        var ids = new[] { looping.Index, looping.Index };
        Assert.Throws<ArgumentException>(() =>
            Timeline<BatchRowsTrack, BatchRowsClip>.Apply([0, 5], ids, new ushort[2], true, new float[2]));
        Assert.Throws<ArgumentException>(() =>
            Timeline<BatchRowsTrack, BatchRowsClip>.Advance([5], ids, new ushort[2], true));
    }
}
#endif
