using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

[CollectionDefinition("GlideWorld", DisableParallelization = true)]
public sealed class GlideWorldCollection;

[Collection("GlideWorld")]
public sealed unsafe class GlideWorldTests
{
    struct Clip { public int Value; }
    struct Track : IBlend<Clip>
    {
        public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
    }

    static byte[] Library(int duration = 1024, int count = 8)
    {
        var tlb = new DomainBaker().Track<Track, Clip>(default).Clip(0, 0, (uint)duration, new Clip { Value = 1 }).Looping().Bake();
        var parts = new LibraryPart[count];
        for (var i = 0; i < count; i++) parts[i] = new() { Name = "Run" + i.ToString("D4"), Tlb = tlb };
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static void Same(PlaybackTable expected, PlaybackTable actual, int entity)
    {
        Assert.Equal(expected.CountOf(entity), actual.CountOf(entity));
        var a = expected.SlotsOf(entity);
        var b = actual.SlotsOf(entity);
        var count = expected.IsSpilled(entity) ? expected.CountOf(entity) : expected.SlotsPerEntity;
        for (var i = 0; i < count; i++) Assert.Equal(((ulong*)a)[i], ((ulong*)b)[i]);
        Assert.Equal(expected.Playing[entity >> 6], actual.Playing[entity >> 6]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(255)]
    public void DenseAndPartialWordsActuallyGlideAndMatchEverySlotByte(int limit)
    {
        using var world = new TimelineWorld(Library());
        using var old = new PlaybackTable(129);
        using var fast = new PlaybackTable(129, 4, limit);
        for (var row = 0; row < 129; row++)
            for (ushort timeline = 1; timeline <= 4; timeline++)
            {
                old.Start(row, timeline, (ushort)(row + timeline));
                fast.Start(row, timeline, (ushort)(row + timeline));
            }
        world.ApplyRequests(old);
        world.ApplyRequests(fast);
        world.Advance(old);
        world.Advance(fast);
        Assert.Equal((byte)limit, fast.Horizons[0]);
        world.Advance(old);
        world.Advance(fast);
        Assert.Equal((byte)(limit - 1), fast.Horizons[0]);
        Assert.Equal((byte)(limit - 1), fast.Horizons[128]);
        for (var tick = 0; tick < 2200; tick++)
        {
            world.Advance(old, (tick / 97 & 1) == 0);
            world.Advance(fast, (tick / 97 & 1) == 0);
            for (var row = 0; row < 129; row++) Same(old, fast, row);
        }
        Assert.Equal(limit, fast.HorizonLimit);
    }

    [Fact]
    public void DenseShortcutDoesNotAdvanceAnUnspilledDeadTail()
    {
        using var world = new TimelineWorld(Library());
        using var old = new PlaybackTable(64);
        using var fast = new PlaybackTable(64, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            for (var row = 0; row < 64; row++)
                for (ushort timeline = 1; timeline <= 4; timeline++) table.Start(row, timeline, 10);
            world.ApplyRequests(table);
            for (var row = 0; row < 64; row++) table.Start(row, 5, 10);
            world.ApplyRequests(table);
            for (var row = 0; row < 64; row++) table.Stop(row, 4);
            world.ApplyRequests(table);
            for (var row = 0; row < 64; row++) table.Stop(row, 5);
            world.ApplyRequests(table);
        }
        Assert.Equal(3, fast.CountOf(0));
        Assert.False(fast.IsSpilled(0));
        Assert.Equal((ushort)4, fast.SlotsOf(0)[3].Timeline);
        var tail = ((ulong*)fast.SlotsOf(0))[3];
        for (var i = 0; i < 100; i++)
        {
            world.Advance(old);
            world.Advance(fast);
            for (var row = 0; row < 64; row++) Same(old, fast, row);
        }
        Assert.Equal(tail, ((ulong*)fast.SlotsOf(0))[3]);
        Assert.True(fast.Horizons[0] > 0);
    }

    [Fact]
    public void MutablePlaybackViewEscapeDisablesGlideForRetainedPointers()
    {
        using var world = new TimelineWorld(Library(31));
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        old.Start(0, 1, 5);
        fast.Start(0, 1, 5);
        world.ApplyRequests(old);
        world.ApplyRequests(fast);
        world.Advance(old);
        world.Advance(fast);
        Assert.True(fast.Horizons[0] > 0);
        var oldView = old.View;
        var retained = fast.View;
        Assert.Equal(0, fast.HorizonLimit);
        oldView.SlotOf(0, 0)->Tick = 30;
        retained.SlotOf(0, 0)->Tick = 30;
        for (var i = 0; i < 40; i++)
        {
            world.Advance(old);
            world.Advance(fast);
            Same(old, fast, 0);
        }
        oldView.SlotOf(0, 0)->Rate = 32;
        retained.SlotOf(0, 0)->Rate = 32;
        world.Advance(old);
        world.Advance(fast);
        Same(old, fast, 0);
    }

    [Fact]
    public void MutableLibraryViewEscapeCannotRetainACachedDuration()
    {
        using var world = new TimelineWorld(Library(31));
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        old.Start(0, 1, 5);
        fast.Start(0, 1, 5);
        world.ApplyRequests(old);
        world.ApplyRequests(fast);
        world.Advance(old);
        world.Advance(fast);
        Assert.True(fast.Horizons[0] > 0);
        var view = world.View;
        view.Timelines[0].Duration = 7;
        for (var i = 0; i < 40; i++)
        {
            world.Advance(old);
            world.Advance(fast);
            Same(old, fast, 0);
        }
    }

    [Fact]
    public void WorldIdentityAndReloadInvalidateEvenWhenGenerationsMatch()
    {
        var longBytes = Library(1024);
        var shortBytes = Library(3);
        using var longWorld = new TimelineWorld(longBytes);
        using var shortWorld = new TimelineWorld(shortBytes);
        Assert.Equal(longWorld._generation, shortWorld._generation);
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        old.Start(0, 1, 1);
        fast.Start(0, 1, 1);
        longWorld.ApplyRequests(old);
        longWorld.ApplyRequests(fast);
        for (var i = 0; i < 100; i++)
        {
            var world = i % 7 == 0 ? shortWorld : longWorld;
            if (i == 40) longWorld.Reload(shortBytes);
            world.Advance(old);
            world.Advance(fast);
            Same(old, fast, 0);
        }
    }

    [Fact]
    public void BorrowedLibraryPointersNeverAcquireAHorizon()
    {
        var timeline = new LibraryTimeline { Id = 1, Duration = 1000, Loop = 1 };
        using var world = new TimelineWorld();
        world.AdoptView(new LibraryView { Timelines = &timeline, TimelineCount = 1 });
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        old.Start(0, 1, 5);
        fast.Start(0, 1, 5);
        world.ApplyRequests(old);
        world.ApplyRequests(fast);
        world.Advance(old);
        world.Advance(fast);
        Assert.Equal((byte)0, fast.Horizons[0]);
        timeline.Duration = 7;
        world.Advance(old);
        world.Advance(fast);
        Same(old, fast, 0);
    }

    [Fact]
    public void ReturningToACachedWorldAfterAnUncachedWorldRebuildsTheHorizon()
    {
        using var cached = new TimelineWorld(Library(31));
        using var uncached = new TimelineWorld(Library(3));
        _ = uncached.View;
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        old.Start(0, 1, 1);
        fast.Start(0, 1, 1);
        cached.ApplyRequests(old);
        cached.ApplyRequests(fast);
        foreach (var world in new[] { cached, uncached, cached, uncached, cached })
        {
            world.Advance(old);
            world.Advance(fast);
            Same(old, fast, 0);
        }
    }

    [Fact]
    public void FailedRequestsInvalidateBeforeReturningControlToTheCaller()
    {
        using var world = new TimelineWorld(Library(31));
        using var fast = new PlaybackTable(1, 4, 255);
        fast.Start(0, 1, 5);
        world.ApplyRequests(fast);
        world.Advance(fast);
        Assert.True(fast.Horizons[0] > 0);
        fast.Start(0, 500);
        Assert.Throws<ArgumentException>(() => world.ApplyRequests(fast));
        Assert.Equal((byte)0, fast.Horizons[0]);
    }

#if TL_CHECKED
    [Fact]
    public void CheckedVisitsIncludeDenseAndPartialGlides()
    {
        using var world = new TimelineWorld(Library());
        using var fast = new PlaybackTable(129, 4, 255);
        for (var row = 0; row < 129; row++)
            for (ushort timeline = 1; timeline <= 4; timeline++) fast.Start(row, timeline, 10);
        world.ApplyRequests(fast);
        world.Advance(fast);
        Checked.ResetVisited();
        world.Advance(fast);
        Assert.Equal(129, Checked.VisitedEntities);
    }
#endif

    [Fact]
    public void OptInCacheHasOneBytePerEntityAndOneFixedHeader()
    {
        using var old = new PlaybackTable(1001);
        using var fast = new PlaybackTable(1001, 4, 255);
        Assert.Equal(1001 + 64, fast.NativeBytes - old.NativeBytes);
        _ = fast.View;
        Assert.Equal(0, fast.HorizonLimit);
        Assert.Equal(1001 + 64, fast.NativeBytes - old.NativeBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackTable(1, 4, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackTable(1, 4, 256));
    }

    [Fact]
    public void TenThousandRequestStreamsMatchAtEveryCacheLimitWithoutEscapingViews()
    {
        using var world = new TimelineWorld(Library(31));
        var tables = new[] { new PlaybackTable(65), new PlaybackTable(65, 4, 1), new PlaybackTable(65, 4, 2), new PlaybackTable(65, 4, 7), new PlaybackTable(65, 4, 255) };
        var random = new Random(0x534C4944);
        var hits = 0;
        try
        {
            for (var stream = 0; stream < 10000; stream++)
            {
                var entity = stream % 65;
                foreach (var table in tables)
                {
                    table.Reset(entity);
                    for (ushort timeline = 1; timeline <= 6; timeline++) table.Start(entity, timeline, (ushort)(stream % 17));
                }
                for (var tick = 0; tick < 24; tick++)
                {
                    var op = random.Next(20);
                    var timeline = (ushort)(1 + random.Next(8));
                    var at = (ushort)random.Next(50);
                    var forward = (stream / 3 + tick / 11 & 1) == 0;
                    foreach (var table in tables)
                    {
                        switch (op)
                        {
                            case 0: table.Start(entity, timeline, at); break;
                            case 1: table.Stop(entity, timeline); break;
                            case 2: table.Pause(entity, timeline); break;
                            case 3: table.Resume(entity, timeline); break;
                            case 4: table.Seek(entity, timeline, at); break;
                            case 5: table.Reverse(entity, timeline, (at & 1) != 0); break;
                            case 6: table.SetSpeed(entity, timeline, at); break;
                            case 7: table.Reset(entity); break;
                        }
                        world.ApplyRequests(table);
                        world.Advance(table, forward);
                    }
                    for (var i = 1; i < tables.Length; i++)
                    {
                        Same(tables[0], tables[i], entity);
                        if (tables[i].Horizons[entity] > 0) hits++;
                    }
                }
                for (var row = 0; row < 65; row++)
                    for (var i = 1; i < tables.Length; i++) Same(tables[0], tables[i], row);
            }
            Assert.True(hits > 10000);
            Assert.Equal(new[] { 0, 1, 2, 7, 255 }, tables.Select(table => table.HorizonLimit).ToArray());
        }
        finally
        {
            foreach (var table in tables) table.Dispose();
        }
    }

    [Fact]
    public void WarmGlideAndBoundaryRebuildsAllocateZeroProcessWideBytes()
    {
        using var world = new TimelineWorld(Library(31));
        using var fast = new PlaybackTable(129, 4, 255);
        for (var row = 0; row < 129; row++)
            for (ushort timeline = 1; timeline <= 4; timeline++) fast.Start(row, timeline, (ushort)(row % 30));
        world.ApplyRequests(fast);
        for (var i = 0; i < 4096; i++) world.Advance(fast);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1200; i++) world.Advance(fast);
        var allocated = GC.GetTotalAllocatedBytes(true) - before;
        Assert.Equal(0, allocated);
        Assert.Equal(255, fast.HorizonLimit);
    }

    [Fact]
    public void IndependentTablesCanAdvanceOnSeparateThreadsInOneImmutableWorld()
    {
        using var world = new TimelineWorld(Library(300));
        using var first = new PlaybackTable(65, 4, 255);
        using var second = new PlaybackTable(65, 4, 255);
        foreach (var table in new[] { first, second })
        {
            for (var row = 0; row < 65; row++)
                for (ushort timeline = 1; timeline <= 4; timeline++) table.Start(row, timeline, 10);
            world.ApplyRequests(table);
        }
        Exception? failure = null;
        Thread Worker(PlaybackTable table) => new(() =>
        {
            try { for (var i = 0; i < 1000; i++) world.Advance(table); }
            catch (Exception error) { Interlocked.CompareExchange(ref failure, error, null); }
        });
        var a = Worker(first);
        var b = Worker(second);
        a.Start();
        b.Start();
        a.Join();
        b.Join();
        Assert.Null(failure);
        for (var row = 0; row < 65; row++) Same(first, second, row);
        Assert.True(first.Horizons[0] > 0);
        Assert.True(second.Horizons[0] > 0);
    }
}
