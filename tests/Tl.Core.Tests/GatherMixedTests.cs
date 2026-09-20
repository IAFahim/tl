using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct GatherClip(float Amount);

public readonly record struct GatherTrack(float Scale) : IBlend<GatherClip>
{
    public void Blend(in GatherClip first, in GatherClip second, float factor, out GatherClip result)
        => result = new GatherClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class GatherPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<GatherTrack, GatherClip>.Consume(&ExecuteScale, &BindFloat);
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

    static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        GatherClip scratch = default;
        var frame = TickFrame.ToFrame<GatherTrack, GatherClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Scale * frame.Clip.Amount;
    }
}

public unsafe class GatherMixedTests
{
    static byte[] Bake(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<GatherTrack, GatherClip>(new GatherTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new GatherClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new GatherClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static ushort[] BakeSet(TimelineSet<GatherTrack, GatherClip> set, params (ushort Duration, bool Looping, float Scale)[] assets)
    {
        var ids = new ushort[assets.Length];
        for (var i = 0; i < assets.Length; i++)
        {
            using var asset = TimelineAsset.LoadAsset(Bake(assets[i].Duration, assets[i].Looping, assets[i].Scale));
            ids[i] = set.Add(asset);
        }
        return ids;
    }

    static float[] Seed(int rows)
    {
        var effects = new float[rows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < rows; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = MathF.Round((float)((state >> 11) / 9007199254740992d) * 128f - 64f) / 4f;
        }
        return effects;
    }

    static void RecordWalk(SlotView slot, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        var hasNext = !next.IsEmpty;
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            if (forward)
            {
                ref var r = ref slot.ForwardRecords[position];
                effects[i] += r.Effect;
                if (hasNext) next[i] = r.Next;
            }
            else
            {
                ref var r = ref slot.BackwardRecords[position];
                if (r.Next != LaneMovementRecord.Skipped)
                {
                    effects[i] += r.Effect;
                    if (hasNext) next[i] = r.Next;
                }
                else if (hasNext) next[i] = position;
            }
        }
    }

    static void AssertCrowdBitExact((ushort Duration, bool Looping, float Scale)[] assets, int rows, bool forward, bool inPlace, bool hasNext, int frames)
    {
        using var set = new TimelineSet<GatherTrack, GatherClip>();
        var bound = BakeSet(set, assets);
        var minDuration = assets.Min(a => a.Duration);
        var idColumn = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            idColumn[i] = bound[i % bound.Length];
            positions[i] = (ushort)((i * 7 + 3) % minDuration);
        }
        for (var frame = 0; frame < frames; frame++)
        {
            var fx = Seed(rows);
            var expectedFx = (float[])fx.Clone();
            var expectedPositions = (ushort[])positions.Clone();
            var expectedNext = inPlace ? expectedPositions : new ushort[rows];
            var nextColumn = hasNext && !inPlace ? new ushort[rows] : positions;
            for (var i = 0; i < rows; i++)
            {
                var slot = set.View(idColumn[i]);
                RecordWalk(slot, expectedPositions.AsSpan(i, 1), hasNext ? expectedNext.AsSpan(i, 1) : default, forward, expectedFx.AsSpan(i, 1));
            }
            if (hasNext)
                set.Apply(idColumn, positions, nextColumn, forward, fx);
            else
                set.Apply(idColumn, positions, forward, fx);
            Assert.True(expectedPositions.AsSpan().SequenceEqual(positions), $"positions diverged (rows={rows}, forward={forward}, inPlace={inPlace}, hasNext={hasNext}, frame={frame})");
            if (hasNext)
                Assert.True(expectedNext.AsSpan().SequenceEqual(nextColumn), $"next diverged (rows={rows}, forward={forward}, inPlace={inPlace}, frame={frame})");
            Assert.True(((ReadOnlySpan<float>)expectedFx).SequenceEqual(fx), $"effects diverged (rows={rows}, forward={forward}, inPlace={inPlace}, hasNext={hasNext}, frame={frame})");
            if (!hasNext)
                break;
            positions = nextColumn;
        }
    }

    public static TheoryData<int, bool, bool, bool> CrowdCases
    {
        get
        {
            var data = new TheoryData<int, bool, bool, bool>();
            foreach (var rows in new[] { 1, 2, 7, 15, 16, 17, 33, 4097 })
                foreach (var forward in new[] { true, false })
                    foreach (var inPlace in new[] { true, false })
                        foreach (var hasNext in new[] { true, false })
                            data.Add(rows, forward, inPlace, hasNext);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(CrowdCases))]
    public void WideCrowdMatchesPerRowRecordWalk(int rows, bool forward, bool inPlace, bool hasNext)
    {
        var assets = new (ushort Duration, bool Looping, float Scale)[100];
        for (var i = 0; i < assets.Length; i++)
            assets[i] = (1024, true, 1f + i * 0.25f);
        AssertCrowdBitExact(assets, rows, forward, inPlace, hasNext, frames: 1);
    }

    [Theory]
    [MemberData(nameof(CrowdCases))]
    public void ShortLoopingCrowdMatchesPerRowRecordWalk(int rows, bool forward, bool inPlace, bool hasNext)
    {
        var assets = new (ushort Duration, bool Looping, float Scale)[4];
        for (var i = 0; i < assets.Length; i++)
            assets[i] = (9, true, 1f + i);
        AssertCrowdBitExact(assets, rows, forward, inPlace, hasNext, frames: 1);
    }

    [Theory]
    [MemberData(nameof(CrowdCases))]
    public void FiniteCrowdMatchesPerRowRecordWalk(int rows, bool forward, bool inPlace, bool hasNext)
    {
        var assets = new (ushort Duration, bool Looping, float Scale)[4];
        for (var i = 0; i < assets.Length; i++)
            assets[i] = (33, false, 1f + i);
        AssertCrowdBitExact(assets, rows, forward, inPlace, hasNext, frames: 1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MixedDurationCrowdStaysBitExactAcrossAdvanceFrames(bool forward)
    {
        AssertCrowdBitExact(new (ushort, bool, float)[]
        {
            (16, true, 1.5f),
            (1024, true, 2f),
            (33, false, 1f),
        }, 4099, forward, inPlace: true, hasNext: true, frames: 4);
    }

    [Fact]
    public void FiniteOriginBackwardRowsKeepSkippedSemanticsAcrossFrames()
    {
        AssertCrowdBitExact(new (ushort, bool, float)[]
        {
            (64, false, 1.5f),
            (64, false, 2.5f),
        }, 130, forward: false, inPlace: true, hasNext: true, frames: 4);
    }

    [Fact]
    public void HoleySetKeepsBitExactResultsForBoundColumns()
    {
        using var set = new TimelineSet<GatherTrack, GatherClip>();
        var bound = BakeSet(set, (16, true, 1.5f), (16, true, 2f), (16, true, 3f));
        using var gap = TimelineAsset.LoadAsset(Bake(16, true, 9f));
        var pending = set.AddAt(200, MeasuredLanes.Measure(gap));
        Assert.Equal((ushort)200, pending);
        Assert.True(set.Holes > 0, "the gap between id 3 and id 200 leaves holes");
        var rows = 4097;
        var idColumn = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            idColumn[i] = bound[i % bound.Length];
            positions[i] = (ushort)(i % 16);
        }
        var fx = Seed(rows);
        var expectedFx = (float[])fx.Clone();
        var expectedPositions = (ushort[])positions.Clone();
        for (var i = 0; i < rows; i++)
        {
            var slot = set.View(idColumn[i]);
            RecordWalk(slot, expectedPositions.AsSpan(i, 1), expectedPositions.AsSpan(i, 1), forward: true, expectedFx.AsSpan(i, 1));
        }
        set.Apply(idColumn, positions, positions, true, fx);
        Assert.True(expectedPositions.AsSpan().SequenceEqual(positions), "positions diverged on a holey set");
        Assert.True(((ReadOnlySpan<float>)expectedFx).SequenceEqual(fx), "effects diverged on a holey set");
    }

    [Fact]
    public void PendingIdOnAGatedChunkFailsLoudlyInsteadOfApplyingIdZeroRecords()
    {
        using var set = new TimelineSet<GatherTrack, GatherClip>();
        var bound = BakeSet(set, (16, true, 1.5f), (16, true, 2f), (16, true, 3f));
        using var gap = TimelineAsset.LoadAsset(Bake(16, true, 9f));
        set.AddAt(200, MeasuredLanes.Measure(gap));
        Assert.True(set.Holes > 0, "ids 3..199 stay pending below the AddAt gap");
        Assert.True(set.IsPending(5), "id 5 is a pending id inside the bound");
        var rows = 64;
        var idColumn = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            idColumn[i] = bound[i % bound.Length];
            positions[i] = (ushort)(i % 16);
        }
        idColumn[63] = 5;
        positions[63] = 7;
        var fx = Seed(rows);
        var thrown = Assert.ThrowsAny<Exception>(() => set.Apply(idColumn, positions, positions, true, fx));
        Assert.True(thrown is NullReferenceException or AccessViolationException,
            $"the pending row must fail loudly, got {thrown.GetType().Name}: {thrown.Message}");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PendingIdRowsHoldOnTheMixedFallbackRoute(bool forward)
    {
        using var set = new TimelineSet<GatherTrack, GatherClip>();
        var bound = BakeSet(set, (16, true, 1.5f), (16, true, 2f), (16, true, 3f));
        using var gap = TimelineAsset.LoadAsset(Bake(16, true, 9f));
        set.AddAt(200, MeasuredLanes.Measure(gap));
        Assert.True(set.Holes > 0, "ids 3..199 stay pending below the AddAt gap");
        var rows = 64;
        var idColumn = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            idColumn[i] = bound[i % bound.Length];
            positions[i] = (ushort)(i % 16);
        }
        idColumn[63] = 5;
        positions[63] = 300;
        var fx = Seed(rows);
        var fxBefore = (float[])fx.Clone();
        var positionsBefore = (ushort[])positions.Clone();
        var expectedFx = (float[])fx.Clone();
        var expectedPositions = (ushort[])positions.Clone();
        var expectedNext = new ushort[rows];
        var actualNext = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            if (idColumn[i] == 5)
                continue;
            var slot = set.View(idColumn[i]);
            RecordWalk(slot, expectedPositions.AsSpan(i, 1), expectedNext.AsSpan(i, 1), forward, expectedFx.AsSpan(i, 1));
        }
        set.Apply(idColumn, positions, actualNext, forward, fx);
        Assert.Equal(positionsBefore[63], positions[63]);
        Assert.Equal(fxBefore[63], fx[63]);
        Assert.Equal(positionsBefore[63], actualNext[63]);
        Assert.True(expectedPositions.AsSpan(0, 63).SequenceEqual(positions.AsSpan(0, 63)), "bound-row positions match the oracle");
        Assert.True(((ReadOnlySpan<float>)expectedFx.AsSpan(0, 63)).SequenceEqual(fx.AsSpan(0, 63)), "bound-row effects match the oracle");
        Assert.True(expectedNext.AsSpan(0, 63).SequenceEqual(actualNext.AsSpan(0, 63)), "bound-row next column matches the oracle");
    }
}
