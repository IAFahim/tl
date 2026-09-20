using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct HandleClip(float Amount);

public readonly record struct HandleTrack(float Scale) : IBlend<HandleClip>
{
    public void Blend(in HandleClip first, in HandleClip second, float factor, out HandleClip result)
        => result = new HandleClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct IdleClip(float Amount);

public readonly record struct IdleTrack(float Scale) : IBlend<IdleClip>
{
    public void Blend(in IdleClip first, in IdleClip second, float factor, out IdleClip result)
        => result = first;
}

internal static unsafe class HandlePairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<HandleTrack, HandleClip>.Consume(&ExecuteHandleScale, &BindFloat);
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

    private static void ExecuteHandleScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        HandleClip scratch = default;
        var frame = TickFrame.ToFrame<HandleTrack, HandleClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Scale * frame.Clip.Amount;
    }
}

public class PairHandleLaneTests
{
    const int Rows = 512;
    const int Assets = 3;

    static byte[] VariantBake(int variant)
        => variant switch
        {
            0 => new Baker()
                .Track<HandleTrack, HandleClip>(new HandleTrack(1f))
                .Clip(0, 0, 4, new HandleClip(8))
                .Looping()
                .Bake(),
            1 => new Baker()
                .Track<HandleTrack, HandleClip>(new HandleTrack(2f))
                .Clip(0, 1, 6, new HandleClip(4))
                .Bake(),
            _ => new Baker()
                .Track<HandleTrack, HandleClip>(new HandleTrack(3f))
                .Clip(0, 2, 5, new HandleClip(2))
                .Clip(0, 4, 9, new HandleClip(6))
                .Looping()
                .Bake(),
        };

    static ushort[] BindVariants()
    {
        var handles = new ushort[Assets];
        for (var variant = 0; variant < Assets; variant++)
        {
            using var asset = TimelineAsset.LoadAsset(VariantBake(variant));
            handles[variant] = asset.Index;
            Timeline<HandleTrack, HandleClip>.Apply(asset.Index, Span<ushort>.Empty, true, Span<float>.Empty); Timeline.Step(asset.Index, Span<ushort>.Empty, true);
        }
        return handles;
    }

    static ushort[] UniformHandles(ushort[] handles, int pattern)
    {
        var rows = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            rows[i] = handles[(i + pattern) % Assets];
        return rows;
    }

    [Fact]
    public void LoadReturnsDenseIndicesForFreshContent()
    {
        var first = TimelineAsset.Load(FreshBake(0));
        var second = TimelineAsset.Load(FreshBake(1));
        var third = TimelineAsset.Load(FreshBake(2));
        Assert.Equal(first + 1, second);
        Assert.Equal(second + 1, third);
    }

    static byte[] FreshBake(int seed)
        => new Baker()
            .Track<HandleTrack, HandleClip>(new HandleTrack(100f + seed))
            .Clip(0, 0, (uint)(4 + seed), new HandleClip(1f))
            .Bake();

    [Fact]
    public void IndexIsStableAcrossRepeatedLoadsAndDistinctAcrossContent()
    {
        using var asset = TimelineAsset.LoadAsset(VariantBake(2));
        var first = asset.Index;
        using var again = TimelineAsset.LoadAsset(VariantBake(2));
        Assert.Equal(first, again.Index);
        using var other = TimelineAsset.LoadAsset(VariantBake(0));
        Assert.NotEqual(first, other.Index);
    }

    [Fact]
    public void ScalarAdvanceMatchesOneSlotCrowdAdvance()
    {
        using var asset = TimelineAsset.LoadAsset(VariantBake(1));
        var slot = asset.Index;
        Timeline<HandleTrack, HandleClip>.Apply(slot, Span<ushort>.Empty, true, Span<float>.Empty); Timeline.Step(slot, Span<ushort>.Empty, true);
        var crowdHandles = new ushort[Rows];
        Array.Fill(crowdHandles, slot);
        var scalarPositions = new ushort[Rows];
        var crowdPositions = new ushort[Rows];
        var scalarEffects = new float[Rows];
        var crowdEffects = new float[Rows];
        for (var i = 0; i < Rows; i++)
        {
            scalarPositions[i] = (ushort)(i % 7);
            crowdPositions[i] = scalarPositions[i];
            scalarEffects[i] = crowdEffects[i] = (i % 5) * 0.5f;
        }
        for (var step = 0; step < 80; step++)
        {
            var forward = step % 4 != 3;
            Timeline<HandleTrack, HandleClip>.Apply(asset, scalarPositions, forward, scalarEffects); Timeline.Step(asset, scalarPositions, forward);
            Timeline<HandleTrack, HandleClip>.Apply(crowdHandles, crowdPositions, forward, crowdEffects); Timeline.Step(crowdHandles, crowdPositions, forward);
        }
        Assert.Equal(crowdPositions, scalarPositions);
        Assert.Equal(crowdEffects, scalarEffects);
    }

    [Fact]
    public void WarmScalarAdvanceAllocatesZero()
    {
        using var asset = TimelineAsset.LoadAsset(VariantBake(0));
        var positions = new ushort[256];
        var effects = new float[256];
        for (var i = 0; i < 256; i++)
            positions[i] = (ushort)(i % 6);

        long allocated;
        for (var attempt = 0; ; attempt++)
        {
            for (var pass = 0; pass < 1_000; pass++)
                { Timeline<HandleTrack, HandleClip>.Apply(asset, positions, true, effects); Timeline.Step(asset, positions, true); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 100_000; pass++)
                { Timeline<HandleTrack, HandleClip>.Apply(asset, positions, true, effects); Timeline.Step(asset, positions, true); }
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0 || attempt >= 8) break;
        }
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void FirstTypedUseOnTimelineWithoutPairThrowsExistingDiagnostic()
    {
        using var asset = TimelineAsset.LoadAsset(new Baker()
            .Track<IdleTrack, IdleClip>(new IdleTrack(1f))
            .Clip(0, 0, 4, new IdleClip(1f))
            .Bake());
        var positions = new ushort[2];
        var effects = new float[2];
        var thrown = Assert.Throws<ArgumentException>(() => Timeline<HandleTrack, HandleClip>.Apply(asset.Index, positions, true, effects));
        Assert.Contains("does not contain the timeline pair", thrown.Message);
    }

    [Fact]
    public void FirstTypedUseWithoutRegisteredConsumerThrowsExistingDiagnostic()
    {
        using var asset = TimelineAsset.LoadAsset(new Baker()
            .Track<IdleTrack, IdleClip>(new IdleTrack(1f))
            .Clip(0, 0, 4, new IdleClip(1f))
            .Bake());
        var positions = new ushort[2];
        var effects = new float[2];
        var thrown = Assert.Throws<ArgumentException>(() => Timeline<IdleTrack, IdleClip>.Apply(asset.Index, positions, true, effects));
        Assert.Contains("No consumer is registered", thrown.Message);
    }

    [Fact]
    public void VariedHandlesMatchPerAssetUniformLanes()
    {
        var handles = BindVariants();
        for (var pattern = 0; pattern < Assets; pattern++)
        {
            var rowHandles = UniformHandles(handles, pattern);
            var positions = new ushort[Rows];
            var effects = new float[Rows];
            for (var i = 0; i < Rows; i++)
                positions[i] = (ushort)(i % 7);

            var oraclePositions = (ushort[])positions.Clone();
            var oracleEffects = (float[])effects.Clone();
            using var oracle = UniformOracle.Create(rowHandles, handles, oraclePositions, oracleEffects);

            for (var step = 0; step < 80; step++)
            {
                var forward = step % 4 != 3;
                Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positions, forward, effects); Timeline.Step(rowHandles, positions, forward);
                oracle.Advance(forward);
            }

            Assert.Equal(oraclePositions, positions);
            Assert.Equal(oracleEffects, effects);
        }
    }

    sealed class UniformOracle : IDisposable
    {
        readonly ushort[] _rowHandles;
        readonly ushort[] _handles;
        readonly ushort[] _positions;
        readonly float[] _effects;
        readonly TimelineSet<HandleTrack, HandleClip>[] _sets = new TimelineSet<HandleTrack, HandleClip>[Assets];

        UniformOracle(ushort[] rowHandles, ushort[] handles, ushort[] positions, float[] effects)
        {
            _rowHandles = rowHandles;
            _handles = handles;
            _positions = positions;
            _effects = effects;
        }

        public static UniformOracle Create(ushort[] rowHandles, ushort[] handles, ushort[] positions, float[] effects)
        {
            var oracle = new UniformOracle(rowHandles, handles, positions, effects);
            for (var variant = 0; variant < Assets; variant++)
            {
                var set = new TimelineSet<HandleTrack, HandleClip>();
                set.Add(TimelineAsset.LoadAsset(VariantBake(variant)));
                oracle._sets[variant] = set;
            }
            return oracle;
        }

        public void Advance(bool forward)
        {
            for (var variant = 0; variant < Assets; variant++)
            {
                var handle = _handles[variant];
                var rows = 0;
                for (var i = 0; i < _rowHandles.Length; i++)
                    if (_rowHandles[i] == handle)
                        rows++;
                var ids = new ushort[rows];
                var positions = new ushort[rows];
                var effects = new float[rows];
                var write = 0;
                for (var i = 0; i < _rowHandles.Length; i++)
                    if (_rowHandles[i] == handle)
                    {
                        ids[write] = 0;
                        positions[write] = _positions[i];
                        effects[write] = _effects[i];
                        write++;
                    }
                _sets[variant].Apply(ids, positions, forward, effects); _sets[variant].Step(ids, positions, forward);
                write = 0;
                for (var i = 0; i < _rowHandles.Length; i++)
                    if (_rowHandles[i] == handle)
                    {
                        _positions[i] = positions[write];
                        _effects[i] = effects[write];
                        write++;
                    }
            }
        }

        public void Dispose()
        {
            foreach (var set in _sets)
                set?.Dispose();
        }
    }

    static void AdvanceAgainstOracle(ushort[] rowHandles, ushort[] positions, float[] effects, ushort[] oraclePositions, float[] oracleEffects, ushort[] handles)
    {
        using var oracle = UniformOracle.Create(rowHandles, handles, oraclePositions, oracleEffects);
        for (var step = 0; step < 80; step++)
        {
            var forward = step % 4 != 3;
            Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positions, forward, effects); Timeline.Step(rowHandles, positions, forward);
            oracle.Advance(forward);
        }
        Assert.Equal(oraclePositions, positions);
        Assert.Equal(oracleEffects, effects);
    }

    [Fact]
    public void PositionsBelowEveryDurationMatchPerAssetUniformLanes()
    {
        var handles = BindVariants();
        for (var pattern = 0; pattern < Assets; pattern++)
        {
            var rowHandles = UniformHandles(handles, pattern);
            var positions = new ushort[Rows];
            var effects = new float[Rows];
            for (var i = 0; i < Rows; i++)
                positions[i] = (ushort)(i % 3);

            AdvanceAgainstOracle(rowHandles, positions, effects, (ushort[])positions.Clone(), (float[])effects.Clone(), handles);
        }
    }

    [Fact]
    public void BlockRunsMatchPerAssetUniformLanes()
    {
        var handles = BindVariants();
        var rowHandles = new ushort[Rows];
        var positions = new ushort[Rows];
        var effects = new float[Rows];
        for (var i = 0; i < Rows; i++)
        {
            var block = i / 32;
            rowHandles[i] = handles[block % Assets];
            positions[i] = (ushort)(block % 5);
        }

        AdvanceAgainstOracle(rowHandles, positions, effects, (ushort[])positions.Clone(), (float[])effects.Clone(), handles);
    }

    [Fact]
    public void ChunkStraddlingStaggeredRunsMatchPerAssetUniformLanes()
    {
        var handles = BindVariants();
        const int rows = 10_240;
        var rowHandles = new ushort[rows];
        var positions = new ushort[rows];
        var effects = new float[rows];
        for (var i = 0; i < rows; i++)
        {
            rowHandles[i] = handles[i / 5_000 % Assets];
            positions[i] = (ushort)(i % 7);
        }

        AdvanceAgainstOracle(rowHandles, positions, effects, (ushort[])positions.Clone(), (float[])effects.Clone(), handles);
    }

    [Fact]
    public void ChunkStraddlingBlocksMatchPerAssetUniformLanes()
    {
        var handles = BindVariants();
        const int rows = 10_240;
        var rowHandles = new ushort[rows];
        var positions = new ushort[rows];
        var effects = new float[rows];
        for (var i = 0; i < rows; i++)
        {
            rowHandles[i] = handles[i / 100 % Assets];
            positions[i] = (ushort)(i / 100 % 5);
        }

        AdvanceAgainstOracle(rowHandles, positions, effects, (ushort[])positions.Clone(), (float[])effects.Clone(), handles);
    }

    [Fact]
    public void AdvanceMatchesSeekApply()
    {
        var bound = BindVariants();
        var rowHandles = UniformHandles(bound, 1);
        var positionsA = new ushort[Rows];
        var positionsB = new ushort[Rows];
        var effectsA = new float[Rows];
        var effectsB = new float[Rows];
        for (var i = 0; i < Rows; i++)
        {
            positionsA[i] = (ushort)(i % 5);
            positionsB[i] = positionsA[i];
        }
        for (var step = 0; step < 20; step++)
        {
            var forward = step % 3 != 2;
            Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positionsA, forward, effectsA); Timeline.Step(rowHandles, positionsA, forward);
            Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positionsB, forward, effectsB); Timeline.Step(rowHandles, positionsB, forward);
        }
        Assert.Equal(positionsB, positionsA);
        Assert.Equal(effectsB, effectsA);
    }

    [Fact]
    public void LoopingWrapFollowsPerRowDuration()
    {
        var bound = BindVariants();
        var durations = new Dictionary<ushort, ushort>
        {
            [bound[0]] = 4,
            [bound[2]] = 9,
        };
        var rowHandles = new ushort[Rows];
        var positions = new ushort[Rows];
        var effects = new float[Rows];
        var starts = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
        {
            rowHandles[i] = bound[i % 2 == 0 ? 0 : 2];
            starts[i] = (ushort)(i % 3);
            positions[i] = starts[i];
        }
        const int steps = 43;
        for (var step = 0; step < steps; step++)
            { Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positions, true, effects); Timeline.Step(rowHandles, positions, true); }
        for (var i = 0; i < Rows; i++)
            Assert.Equal((ushort)((starts[i] + steps) % durations[rowHandles[i]]), positions[i]);
    }

    [Fact]
    public void FirstTypedUseAgreesWithPriorMeasuredLanes()
    {
        using var asset = TimelineAsset.LoadAsset(VariantBake(0));
        using var measured = MeasuredLanes.Measure(asset);
        var index = asset.Index;
        Timeline<HandleTrack, HandleClip>.Apply(index, Span<ushort>.Empty, true, Span<float>.Empty); Timeline.Step(index, Span<ushort>.Empty, true);
        Assert.Equal(4, (int)measured.Duration);
        Assert.True(measured.Looping);
        var positions = new ushort[64];
        var effects = new float[64];
        for (var i = 0; i < 64; i++)
            positions[i] = (ushort)(i % 4);
        Timeline<HandleTrack, HandleClip>.Apply(index, positions, true, effects); Timeline.Step(index, positions, true);
        for (var i = 0; i < 64; i++)
            Assert.Equal(8f, effects[i]);
    }

    [Fact]
    public void SeekWithUnloadedIndexThrows()
    {
        var positions = new ushort[2];
        var effects = new float[2];
        var thrown = Assert.Throws<ArgumentException>(() =>
            Timeline<IdleTrack, IdleClip>.Apply([ushort.MaxValue, ushort.MaxValue], positions, true, effects));
        Assert.Contains("is not loaded", thrown.Message);
    }

    [Fact]
    public void UnloadedIndexInCrowdKeepsDiagnostic()
    {
        var bound = BindVariants();
        var rowHandles = new ushort[4];
        var positions = new ushort[4];
        var effects = new float[4];
        rowHandles[0] = bound[0];
        rowHandles[1] = bound[0];
        rowHandles[3] = bound[0];
        rowHandles[2] = ushort.MaxValue;
        var thrown = Assert.Throws<ArgumentException>(() =>
            Timeline<HandleTrack, HandleClip>.Apply(rowHandles, positions, true, effects));
        Assert.Contains("is not loaded", thrown.Message);
    }

    [Fact]
    public void IndexAdvanceMatchesPerAssetUniformOracleForwardAndBackward()
    {
        var handles = BindVariants();
        for (var pattern = 0; pattern < Assets; pattern++)
        {
            var rowHandles = UniformHandles(handles, pattern);
            var positions = new ushort[Rows];
            var effects = new float[Rows];
            for (var i = 0; i < Rows; i++)
                positions[i] = (ushort)(i % 7);

            var oraclePositions = (ushort[])positions.Clone();
            var oracleEffects = (float[])effects.Clone();
            using var oracle = UniformOracle.Create(rowHandles, handles, oraclePositions, oracleEffects);

            for (var step = 0; step < 80; step++)
            {
                var forward = step % 4 != 3;
                AdvanceGroupedByIndex(rowHandles, positions, forward, effects);
                oracle.Advance(forward);
            }

            Assert.Equal(oraclePositions, positions);
            Assert.Equal(oracleEffects, effects);
        }
    }

    static void AdvanceGroupedByIndex(ushort[] rowHandles, ushort[] positions, bool forward, float[] effects)
    {
        var distinct = new ushort[Assets];
        var distinctCount = 0;
        for (var i = 0; i < rowHandles.Length; i++)
        {
            var known = false;
            for (var k = 0; k < distinctCount; k++)
                if (distinct[k] == rowHandles[i])
                {
                    known = true;
                    break;
                }
            if (!known) distinct[distinctCount++] = rowHandles[i];
        }
        var groupPositions = new ushort[Rows];
        var groupEffects = new float[Rows];
        for (var k = 0; k < distinctCount; k++)
        {
            var index = distinct[k];
            var write = 0;
            for (var i = 0; i < rowHandles.Length; i++)
                if (rowHandles[i] == index)
                {
                    groupPositions[write] = positions[i];
                    groupEffects[write] = effects[i];
                    write++;
                }
            Timeline<HandleTrack, HandleClip>.Apply(index, groupPositions.AsSpan(0, write), forward, groupEffects.AsSpan(0, write)); Timeline.Step(index, groupPositions.AsSpan(0, write), forward);
            write = 0;
            for (var i = 0; i < rowHandles.Length; i++)
                if (rowHandles[i] == index)
                {
                    positions[i] = groupPositions[write];
                    effects[i] = groupEffects[write];
                    write++;
                }
        }
    }

    [Fact]
    public void WarmIndexAdvanceAllocatesZero()
    {
        var bound = BindVariants();
        var positions = new ushort[256];
        var effects = new float[256];
        for (var i = 0; i < 256; i++)
            positions[i] = (ushort)(i % 6);

        long allocated;
        for (var attempt = 0; ; attempt++)
        {
            for (var pass = 0; pass < 1_000; pass++)
                for (var variant = 0; variant < Assets; variant++)
                    { { Timeline<HandleTrack, HandleClip>.Apply(bound[variant], positions, true, effects); Timeline.Step(bound[variant], positions, true); }; }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 30_000; pass++)
                for (var variant = 0; variant < Assets; variant++)
                    { { Timeline<HandleTrack, HandleClip>.Apply(bound[variant], positions, true, effects); Timeline.Step(bound[variant], positions, true); }; }
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0 || attempt >= 8) break;
        }
        Assert.Equal(0, allocated);
    }
}
