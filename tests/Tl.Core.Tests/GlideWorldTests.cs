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

    static byte[] Library(int duration = 1024, int count = 8, bool loop = true)
    {
        var baker = new DomainBaker().Track<Track, Clip>(default).Clip(0, 0, (uint)duration, new Clip { Value = 1 });
        var tlb = (loop ? baker.Looping() : baker).Bake();
        var parts = new LibraryPart[count];
        for (var i = 0; i < count; i++) parts[i] = new() { Name = "Run" + i.ToString("D4"), Tlb = tlb };
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static ulong Word(PlaybackTable table, int entity, int slot)
    {
        var playback = table.SlotsOf(entity)[slot];
        if (table.Frozen && slot < table.CountOf(entity)) LazyTicks.Shift(&playback, 1, table.Clock, table.Forward);
        return *(ulong*)&playback;
    }

    static void Same(PlaybackTable expected, PlaybackTable actual, int entity)
    {
        Assert.Equal(expected.CountOf(entity), actual.CountOf(entity));
        Assert.Equal(expected.IsSpilled(entity), actual.IsSpilled(entity));
        var count = expected.IsSpilled(entity) ? expected.CountOf(entity) : expected.SlotsPerEntity;
        for (var i = 0; i < count; i++) Assert.Equal(Word(expected, entity, i), Word(actual, entity, i));
        Assert.Equal(expected.PlayingBits[entity >> 6], actual.PlayingBits[entity >> 6]);
    }

    static int Wait(PlaybackTable table, int entity) => (ushort)(table.Next[entity] - table.Clock);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(255)]
    public void DenseAndPartialWordsStayUntouchedBetweenBoundariesAndMatchEverySlotByte(int limit)
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
        Assert.True(fast.Frozen);
        Assert.Equal(1019, Wait(fast, 0));
        Assert.Equal(891, Wait(fast, 128));
        var stored = ((ulong*)fast.SlotsOf(128))[3];
        world.Advance(old);
        world.Advance(fast);
        Assert.Equal(1018, Wait(fast, 0));
        Assert.Equal(890, Wait(fast, 128));
        Assert.Equal(stored, ((ulong*)fast.SlotsOf(128))[3]);
        Assert.Equal((ushort)134, old.SlotsOf(128)[3].Tick);
        for (var row = 0; row < 129; row++) Same(old, fast, row);
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
        Assert.True(fast.Frozen);
        Assert.Equal(914, Wait(fast, 0));
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
        Assert.True(fast.Frozen);
        Assert.Equal(25, Wait(fast, 0));
        var oldView = old.View;
        var retained = fast.View;
        Assert.Equal(0, fast.HorizonLimit);
        Assert.False(fast.Frozen);
        Assert.Equal((ushort)6, retained.SlotOf(0, 0)->Tick);
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
        Assert.True(fast.Frozen);
        Assert.Equal(25, Wait(fast, 0));
        var view = world.View;
        view.Timelines[0].Duration = 7;
        for (var i = 0; i < 40; i++)
        {
            world.Advance(old);
            world.Advance(fast);
            Assert.False(fast.Frozen);
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
    public void BorrowedLibraryPointersNeverFreezeATable()
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
        Assert.False(fast.Frozen);
        timeline.Duration = 7;
        world.Advance(old);
        world.Advance(fast);
        Same(old, fast, 0);
    }

    [Fact]
    public void ReturningToALazyWorldAfterAnEagerWorldFreezesAgain()
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
            Assert.Equal(world == cached, fast.Frozen);
            Same(old, fast, 0);
        }
    }

    [Fact]
    public void RejectedRequestsLeaveTheLazyStateExactBeforeReturningControlToTheCaller()
    {
        using var world = new TimelineWorld(Library(31));
        using var old = new PlaybackTable(1);
        using var fast = new PlaybackTable(1, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            table.Start(0, 1, 5);
            world.ApplyRequests(table);
            world.Advance(table);
            table.Start(0, 2, 9);
            table.Start(0, 500);
            Assert.Throws<ArgumentException>(() => world.ApplyRequests(table));
        }
        Assert.True(fast.Frozen);
        Assert.Equal(25, Wait(fast, 0));
        Assert.Equal(1, fast.CountOf(0));
        Assert.Equal((ushort)6, old.SlotsOf(0)[0].Tick);
        Same(old, fast, 0);
        for (var i = 0; i < 70; i++)
        {
            world.Advance(old);
            world.Advance(fast);
            Same(old, fast, 0);
        }
        Assert.True(fast.Frozen);
    }

    [Fact]
    public void UnknownTimelineThrowsWithTheSameMessageAndTheSameHalfSteppedStateAsEager()
    {
        using var big = new TimelineWorld(Library(31));
        using var small = new TimelineWorld(Library(31, 2));
        using var old = new PlaybackTable(3);
        using var fast = new PlaybackTable(3, 4, 255);
        var messages = new string[2];
        var index = 0;
        foreach (var table in new[] { old, fast })
        {
            table.Start(0, 1, 3);
            table.Start(0, 2, 4);
            table.Start(1, 1, 3);
            table.Start(1, 5, 4);
            table.Start(1, 2, 5);
            table.Start(2, 1, 3);
            big.ApplyRequests(table);
            for (var i = 0; i < 3; i++) big.Advance(table);
            messages[index++] = Assert.Throws<ArgumentException>(() => small.Advance(table)).Message;
        }
        Assert.Equal(messages[0], messages[1]);
        Assert.False(fast.Frozen);
        Assert.Equal((ushort)7, fast.SlotsOf(0)[0].Tick);
        Assert.Equal((ushort)8, fast.SlotsOf(0)[1].Tick);
        Assert.Equal((ushort)7, fast.SlotsOf(1)[0].Tick);
        Assert.Equal((ushort)7, fast.SlotsOf(1)[1].Tick);
        Assert.Equal((ushort)8, fast.SlotsOf(1)[2].Tick);
        Assert.Equal((ushort)6, fast.SlotsOf(2)[0].Tick);
        for (var row = 0; row < 3; row++) Same(old, fast, row);
        for (var i = 0; i < 70; i++)
        {
            big.Advance(old);
            big.Advance(fast);
            Assert.True(fast.Frozen);
            for (var row = 0; row < 3; row++) Same(old, fast, row);
        }
    }

    [Fact]
    public void OneShotsEndingInsideASpilledRunCarryTheRemainingDuesBackInline()
    {
        using var world = new TimelineWorld(Library(40, 8, false));
        using var old = new PlaybackTable(2, 2);
        using var fast = new PlaybackTable(2, 2, 255);
        foreach (var table in new[] { old, fast })
        {
            table.Start(0, 1, 30);
            table.Start(0, 2, 2);
            world.ApplyRequests(table);
            world.Advance(table);
            table.Start(0, 3, 20);
            table.Start(0, 4, 36);
            table.Start(0, 5, 33);
            table.Seek(0, 2, 10);
            table.Start(1, 6, 1);
            world.ApplyRequests(table);
        }
        Assert.True(fast.Frozen);
        Assert.True(fast.IsSpilled(0));
        Assert.Equal(5, fast.CountOf(0));
        var unspilled = -1;
        for (var tick = 0; tick < 60; tick++)
        {
            world.Advance(old);
            world.Advance(fast);
            if (unspilled < 0 && !fast.IsSpilled(0)) unspilled = tick;
            for (var row = 0; row < 2; row++) Same(old, fast, row);
        }
        Assert.Equal(19, unspilled);
        Assert.Equal(0, fast.CountOf(0));
        Assert.Equal(0ul, fast.PlayingBits[0]);
        Assert.True(fast.Frozen);
    }

    [Fact]
    public void ATickThatFillsTheHitBufferToItsLastLaneLandsEveryBoundaryOnce()
    {
        using var world = new TimelineWorld(Library(32, 256));
        using var old = new PlaybackTable(72, 4);
        using var fast = new PlaybackTable(72, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            for (var row = 0; row < 63; row++) table.Start(row, 1);
            for (ushort timeline = 1; timeline <= 256; timeline++)
            {
                table.Start(63, timeline);
                table.Start(71, timeline);
            }
            world.ApplyRequests(table);
        }
        Assert.Equal(PlaybackTable.MaxSlots, fast.CountOf(63));
        for (var tick = 0; tick < 100; tick++)
        {
            world.Advance(old);
            world.Advance(fast);
            for (var row = 0; row < 72; row++) Same(old, fast, row);
        }
        Assert.True(fast.Frozen);
        Assert.Equal(28, Wait(fast, 63));
    }

    [Fact]
    public void ClockWrapPausedSlotsAndDirectionFlipsStayExactForTwoHundredThousandTicks()
    {
        using var world = new TimelineWorld(Library(1000));
        using var old = new PlaybackTable(4);
        using var fast = new PlaybackTable(4, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            table.Start(0, 1, 5);
            table.Start(1, 1, 7);
            table.Start(1, 2, 400);
            table.Start(2, 3, 9);
            table.Start(3, 4, 11);
            world.ApplyRequests(table);
            table.Pause(1, 2);
            table.Reverse(2, 3, true);
            table.Pause(3, 4);
            world.ApplyRequests(table);
        }
        for (var tick = 0; tick < 200000; tick++)
        {
            if (tick == 100000)
                foreach (var table in new[] { old, fast })
                {
                    table.Resume(1, 2);
                    table.Resume(3, 4);
                    world.ApplyRequests(table);
                }
            world.Advance(old, tick < 150000);
            world.Advance(fast, tick < 150000);
            Assert.True(fast.Frozen);
            for (var row = 0; row < 4; row++) Same(old, fast, row);
        }
        Assert.Equal(255, fast.HorizonLimit);
    }

#if TL_CHECKED
    [Fact]
    public void CheckedVisitsCountOnlyTheEntitiesThatReachABoundary()
    {
        using var world = new TimelineWorld(Library());
        using var old = new PlaybackTable(129);
        using var fast = new PlaybackTable(129, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            for (var row = 0; row < 129; row++)
                for (ushort timeline = 1; timeline <= 4; timeline++) table.Start(row, timeline, 10);
            world.ApplyRequests(table);
            world.Advance(table);
        }
        Checked.ResetVisited();
        world.Advance(fast);
        Assert.Equal(0, Checked.VisitedEntities);
        for (var i = 0; i < 1023; i++) world.Advance(fast);
        Assert.Equal(129, Checked.VisitedEntities);
        Checked.ResetVisited();
        for (var i = 0; i < 1024; i++) world.Advance(old);
        Assert.Equal(129 * 1024, Checked.VisitedEntities);
        for (var row = 0; row < 129; row++) Same(old, fast, row);
    }
#endif

    [Fact]
    public void OptInLazyStateHasTwoBytesPerInlineSlotAndPerEntityLane()
    {
        using var old = new PlaybackTable(1001);
        using var fast = new PlaybackTable(1001, 4, 255);
        Assert.Equal(1001 * 5 * 2, fast.NativeBytes - old.NativeBytes);
        _ = fast.View;
        Assert.Equal(0, fast.HorizonLimit);
        Assert.Equal(1001 * 5 * 2, fast.NativeBytes - old.NativeBytes);
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
                        if (tables[i].Frozen && (tables[i].PlayingBits[entity >> 6] >> (entity & 63) & 1) != 0 && Wait(tables[i], entity) > 1) hits++;
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
    public void WarmGlideAndBoundaryRebuildsAllocateZeroBytes()
    {
        using var world = new TimelineWorld(Library(31));
        using var fast = new PlaybackTable(129, 4, 255);
        for (var row = 0; row < 129; row++)
            for (ushort timeline = 1; timeline <= 4; timeline++) fast.Start(row, timeline, (ushort)(row % 30));
        world.ApplyRequests(fast);
        for (var i = 0; i < 4096; i++) world.Advance(fast);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1200; i++) world.Advance(fast);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
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
        Assert.True(first.Frozen);
        Assert.True(second.Frozen);
        Assert.Equal(Wait(first, 0), Wait(second, 0));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(600, true)]
    public void ATableWhoseSlotsAreDueMostTicksStepsEagerlyUntilItsNextProbeAndStaysExact(int duration, bool stays)
    {
        using var world = new TimelineWorld(Library(duration));
        using var old = new PlaybackTable(64);
        using var fast = new PlaybackTable(64, 4, 255);
        foreach (var table in new[] { old, fast })
        {
            for (var row = 0; row < 64; row++)
                for (ushort timeline = 1; timeline <= 2; timeline++) table.Start(row, timeline, (ushort)((row + timeline) % duration));
            world.ApplyRequests(table);
        }
        var eager = 0;
        for (var tick = 0; tick < 400; tick++)
        {
            world.Advance(old);
            world.Advance(fast);
            if (!fast.Frozen) { Assert.True(fast.Resting); eager++; }
            for (var row = 0; row < 64; row++) Same(old, fast, row);
        }
        if (stays) Assert.Equal(0, eager);
        else Assert.InRange(eager, 300, 399);
        world.Advance(fast);
        if (!stays)
        {
            for (var tick = 0; tick < 4096 && !fast.Frozen; tick++) world.Advance(fast);
            Assert.True(fast.Frozen);
        }
    }
}
