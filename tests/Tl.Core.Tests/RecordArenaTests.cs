using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct ArenaClip(float Amount);

public readonly record struct ArenaTrack(float Scale) : IBlend<ArenaClip>
{
    public void Blend(in ArenaClip first, in ArenaClip second, float factor, out ArenaClip result)
        => result = new ArenaClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class ArenaPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<ArenaTrack, ArenaClip>.Consume(&ExecuteScale, &BindFloat);
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
        ArenaClip scratch = default;
        var frame = TickFrame.ToFrame<ArenaTrack, ArenaClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Scale * frame.Clip.Amount;
    }
}

public unsafe class RecordArenaTests
{
    static byte[] Bake(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static ushort[] BakeSet(TimelineSet<RoutingTrack, RoutingClip> set, params (ushort Duration, bool Looping, float Scale)[] assets)
    {
        var ids = new ushort[assets.Length];
        for (var i = 0; i < assets.Length; i++)
        {
            using var asset = TimelineAsset.LoadAsset(Bake(assets[i].Duration, assets[i].Looping, assets[i].Scale));
            ids[i] = set.Add(asset);
        }
        return ids;
    }

    static void AssertSegmentBytes(TimelineSet<RoutingTrack, RoutingClip> set, ushort id)
    {
        var view = set.View(id);
        var bytes = (int)(view.TableTicks * sizeof(LaneMovementRecord));
        Assert.True(new ReadOnlySpan<byte>(set._arenaForward + set._arenaBases[id], bytes)
            .SequenceEqual(new ReadOnlySpan<byte>(view.ForwardRecords, bytes)), $"forward arena segment of id {id} copies the slot records bit-exactly");
        Assert.True(new ReadOnlySpan<byte>(set._arenaBackward + set._arenaBases[id], bytes)
            .SequenceEqual(new ReadOnlySpan<byte>(view.BackwardRecords, bytes)), $"backward arena segment of id {id} copies the slot records bit-exactly");
    }

    [Fact]
    public void FoldAppendsBitExactSegmentsForEveryBoundId()
    {
        using var set = new TimelineSet<RoutingTrack, RoutingClip>();
        var ids = BakeSet(set,
            (6, true, 1.5f),
            (16, true, 1.5f),
            (1024, true, 2f),
            (16, false, 2f),
            (1024, false, 2f),
            (1, false, 1f),
            (0, true, 1f));
        Assert.True(set.ArenaBytes > 0, "the fold appends arena bytes");
        for (ushort i = 0; i < ids.Length; i++)
            AssertSegmentBytes(set, i);
    }

    [Fact]
    public void SharedContentBindsGetByteIdenticalSegments()
    {
        using var set = new TimelineSet<RoutingTrack, RoutingClip>();
        using var original = TimelineAsset.LoadAsset(Bake(16, true, 1.5f));
        using var duplicate = TimelineAsset.LoadAsset(Bake(16, true, 1.5f));
        var first = set.Add(original);
        var second = set.Add(duplicate);
        Assert.Equal(1, set.BlockCount);
        Assert.Equal(1, set.SharedHits);
        AssertSegmentBytes(set, first);
        AssertSegmentBytes(set, second);
        Assert.Equal(0u, set._arenaBases[first]);
        var ticks = set.View(first).TableTicks;
        Assert.Equal(ticks, set._arenaBases[second]);
        var bytes = (int)(ticks * sizeof(LaneMovementRecord));
        Assert.True(new ReadOnlySpan<byte>(set._arenaForward + set._arenaBases[first], bytes)
            .SequenceEqual(new ReadOnlySpan<byte>(set._arenaForward + set._arenaBases[second], bytes)), "shared binds duplicate byte-identical segments");
        Assert.True(new ReadOnlySpan<byte>(set._arenaBackward + set._arenaBases[first], bytes)
            .SequenceEqual(new ReadOnlySpan<byte>(set._arenaBackward + set._arenaBases[second], bytes)), "shared binds duplicate byte-identical backward segments");
    }

    [Fact]
    public void GrowthPreservesPublishedSegmentsAtStableOffsets()
    {
        using var set = new TimelineSet<RoutingTrack, RoutingClip>();
        var staleIds = BakeSet(set, Enumerable.Repeat((ushort)8, 100).Select((_, i) => ((ushort)8, i % 2 == 0, 1f + i)).ToArray());
        var staleForward = set._arenaForward;
        var staleBackward = set._arenaBackward;
        var staleBases = set._arenaBases;
        var staleCapacity = set._arenaCapacity;

        var grownIds = BakeSet(set, Enumerable.Repeat((ushort)8, 400).Select((_, i) => ((ushort)8, i % 2 == 0, 2f + i)).ToArray());
        Assert.True(set._arenaCapacity > staleCapacity, "the appends doubled the arena");

        foreach (var id in staleIds)
        {
            var view = set.View(id);
            var bytes = (int)(view.TableTicks * sizeof(LaneMovementRecord));
            Assert.True(new ReadOnlySpan<byte>(staleForward + staleBases[id], bytes)
                .SequenceEqual(new ReadOnlySpan<byte>(view.ForwardRecords, bytes)), $"stale arena block keeps id {id} bytes at its published offset");
            Assert.True(new ReadOnlySpan<byte>(staleBackward + staleBases[id], bytes)
                .SequenceEqual(new ReadOnlySpan<byte>(view.BackwardRecords, bytes)), $"stale backward block keeps id {id} bytes at its published offset");
            Assert.Equal(staleBases[id], set._arenaBases[id]);
        }
        foreach (var id in grownIds)
            AssertSegmentBytes(set, id);
    }

    [Fact]
    public void DisposeFreesEveryArenaByte()
    {
        var set = new TimelineSet<RoutingTrack, RoutingClip>();
        BakeSet(set, (16, true, 1.5f), (1024, false, 2f));
        var arena = set.ArenaBytes;
        Assert.True(arena > 0, "the folded bank owns arena bytes");
        Assert.Equal(set.RetainedBytes, set.HeaderBytes + set.TableBytes + set.DirectoryBytes + arena);
        set.Dispose();
        Assert.Equal(0, set.RetainedBytes);
        Assert.Equal(0, set.ArenaBytes);
    }

    public sealed class ArenaId
    {
        public required ushort Index;
        public required ushort Duration;
    }

    static byte[] BakeArena(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<ArenaTrack, ArenaClip>(new ArenaTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new ArenaClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new ArenaClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static (ushort[] Ids, ushort[] Positions, Dictionary<ushort, ArenaId> Assets) BakeCrowd(int variants, int rows, bool staggeredBeyondMin)
    {
        var assets = new Dictionary<ushort, ArenaId>();
        var indices = new ushort[variants];
        ushort minDuration = ushort.MaxValue;
        for (var v = 0; v < variants; v++)
        {
            var duration = (ushort)(300 + v * 7);
            var index = TimelineAsset.Load(BakeArena(duration, looping: true, 1f + v * 0.25f));
            indices[v] = index;
            assets[index] = new ArenaId { Index = index, Duration = duration };
            minDuration = Math.Min(minDuration, duration);
        }
        var ids = new ushort[rows];
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            ids[i] = indices[i % variants];
#if TL_CHECKED
            positions[i] = staggeredBeyondMin ? (ushort)(i % (minDuration + 1)) : (ushort)(i % (minDuration - 1));
#else
            positions[i] = staggeredBeyondMin ? (ushort)(i % 1024) : (ushort)(i % (minDuration - 1));
#endif
        }
        return (ids, positions, assets);
    }

    static void PerRowComposition(Dictionary<ushort, ArenaId> assets, ushort[] ids, ushort[] positions, bool forward, float[] effects)
    {
        for (var i = 0; i < ids.Length; i++)
        {
            var duration = assets[ids[i]].Duration;
            var p = positions[i];
            if (forward)
            {
                if (p < duration)
                {
                    ref var r = ref Timeline<ArenaTrack, ArenaClip>.View(ids[i]).ForwardRecords[p];
                    effects[i] += r.Effect;
                    positions[i] = r.Next;
                }
            }
            else
            {
                if (p <= duration)
                {
                    ref var r = ref Timeline<ArenaTrack, ArenaClip>.View(ids[i]).BackwardRecords[p];
                    if (r.Next != LaneMovementRecord.Skipped)
                    {
                        effects[i] += r.Effect;
                        positions[i] = r.Next;
                    }
                }
            }
        }
    }

    static float[] ArenaSeed(int rows)
    {
        var fx = new float[rows];
        for (var i = 0; i < rows; i++) fx[i] = (i % 23) * 0.25f - 3f;
        return fx;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CrowdRowsMatchPerRowComposition(bool forward)
    {
        const int rows = 4096;
        foreach (var beyond in new[] { false, true })
        {
            var (ids, positions, assets) = BakeCrowd(20, rows, beyond);

            var oraclePositions = (ushort[])positions.Clone();
            var oracleFx = ArenaSeed(rows);
            PerRowComposition(assets, ids, oraclePositions, forward, oracleFx);

            var crowdFx = ArenaSeed(rows);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, forward, crowdFx);
            Assert.Equal(oracleFx, crowdFx);

            var fusedPositions = (ushort[])positions.Clone();
            var fusedFx = ArenaSeed(rows);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, fusedPositions, fusedPositions, forward, fusedFx);
            Assert.Equal(oraclePositions, fusedPositions);
            Assert.Equal(oracleFx, fusedFx);

            var nextPositions = (ushort[])positions.Clone();
            var next = new ushort[rows];
            var nextFx = ArenaSeed(rows);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, nextPositions, next, forward, nextFx);
            Assert.Equal(oraclePositions, next);
            Assert.Equal(oracleFx, nextFx);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CrowdRewindReturnsToSeed(bool staggeredBeyondMin)
    {
        const int rows = 4096;
        var (ids, seedPositions, _) = BakeCrowd(20, rows, staggeredBeyondMin);
        var seedFx = ArenaSeed(rows);
        var positions = (ushort[])seedPositions.Clone();
        var fx = (float[])seedFx.Clone();
        for (var frame = 0; frame < 8; frame++)
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, positions, true, fx);
        Assert.NotEqual(seedPositions, positions);
        for (var frame = 0; frame < 8; frame++)
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, positions, false, fx);
        Assert.Equal(seedPositions, positions);
        Assert.Equal(seedFx, fx);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MaximumDurationEdgesMatchComposition(bool forward)
    {
        var index = TimelineAsset.Load(BakeArena(ushort.MaxValue, looping: false, 2f));
        var duration = Timeline<ArenaTrack, ArenaClip>.View(index).Duration;
        Assert.Equal(ushort.MaxValue, duration);
        ushort[] positions = [0, 1, (ushort)(duration - 2), (ushort)(duration - 1), duration, 1];
        var ids = Enumerable.Repeat(index, positions.Length).ToArray();
        var assets = new Dictionary<ushort, ArenaId> { [index] = new ArenaId { Index = index, Duration = duration } };

        var oraclePositions = (ushort[])positions.Clone();
        var oracleFx = ArenaSeed(positions.Length);
        PerRowComposition(assets, ids, oraclePositions, forward, oracleFx);

        var crowdFx = ArenaSeed(positions.Length);
        Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, forward, crowdFx);
        Assert.Equal(oracleFx, crowdFx);

        var fusedPositions = (ushort[])positions.Clone();
        var fusedFx = ArenaSeed(positions.Length);
        Timeline<ArenaTrack, ArenaClip>.Apply(ids, fusedPositions, fusedPositions, forward, fusedFx);
        Assert.Equal(oraclePositions, fusedPositions);
        Assert.Equal(oracleFx, fusedFx);
    }

    [Fact]
    public void RewindFramesAllocateZeroOnEveryArenaRoute()
    {
        const int rows = 4096;
        var (ids, positions, _) = BakeCrowd(20, rows, staggeredBeyondMin: true);
        var next = new ushort[rows];
        var fx = ArenaSeed(rows);
        var soloIds = new ushort[rows];
        Array.Fill(soloIds, ids[0]);
        for (var warm = 0; warm < 60; warm++)
        {
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, next, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(soloIds, positions, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids[0], positions, next, true, fx);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var rep = 0; rep < 50; rep++)
        {
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, next, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids, positions, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(soloIds, positions, true, fx);
            Timeline<ArenaTrack, ArenaClip>.Apply(ids[0], positions, next, true, fx);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }
}
