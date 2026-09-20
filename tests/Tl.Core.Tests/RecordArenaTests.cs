using System.Runtime.InteropServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class RecordArenaTests
{
    static byte[] Bake(ushort duration, bool looping, float scale)
    {
        var baker = new Baker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(scale))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), (uint)duration, new RoutingClip(-0.5f));
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

    static unsafe void AssertSegmentBytes(TimelineSet<RoutingTrack, RoutingClip> set, ushort id)
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
        var staleIds = BakeSet(set, Enumerable.Repeat((ushort)8, 100).Select((d, i) => ((ushort)8, i % 2 == 0, 1f + i)).ToArray());
        var staleForward = set._arenaForward;
        var staleBackward = set._arenaBackward;
        var staleBases = set._arenaBases;
        var staleCapacity = set._arenaCapacity;

        var grownIds = BakeSet(set, Enumerable.Repeat((ushort)8, 400).Select((d, i) => ((ushort)8, i % 2 == 0, 2f + i)).ToArray());
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
}
