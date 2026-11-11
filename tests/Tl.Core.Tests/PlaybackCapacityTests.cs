using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class PlaybackCapacityTests
{
    static byte[] Kinds() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "kinds.tlib"));

    [Fact]
    public void OneBigEntityLeavesEveryOtherEntityInline()
    {
        // Slots are filled directly: the Start pipeline dedupes by timeline
        // and the test libraries hold at most 4 timelines, so a 154-wide
        // entity cannot be reached through Start.
        using var players = new PlaybackTable(40_001);
        var big = new PlaybackTable.Slots(players, 0);
        for (var i = 0; i < 154; i++)
            big.TryAdd(new Playback { Timeline = (ushort)(1 + i), Tick = (ushort)i });
        for (var e = 1; e < 40_001; e++)
        {
            var slots = new PlaybackTable.Slots(players, e);
            for (var i = 0; i < 4; i++)
                slots.TryAdd(new Playback { Timeline = (ushort)(1 + i), Tick = (ushort)i });
        }

        Assert.Equal(154, players.CountOf(0));
        Assert.Equal(4, players.CountOf(1));
        Assert.Equal(4, players.CountOf(40_000));
        Assert.True(players.IsSpilled(0));
        var view = players.View;
        for (var e = 1; e < 40_001; e++)
            Assert.True((view.Spilled[e >> 6] & (1ul << (e & 63))) == 0);
        Assert.Equal(154, view.SlotOf(0, 153)->Timeline);
        Assert.Equal(4, view.SlotOf(40_000, 3)->Timeline);

        // Slots 1,280,032 + counts 160,004 + Spilled 5,008 + one 256-block
        // arena (2,048) = 1,447,092 native bytes (main: 82.09 MB).
        Console.WriteLine($"native bytes: {players.NativeBytes}");
        Assert.True(players.NativeBytes <= 1_600_000, $"native bytes {players.NativeBytes}");
    }

    [Fact]
    public void ReserveMovesOnlyTheReservedEntity()
    {
        using var players = new PlaybackTable(4);
        var before = new Playback*[4];
        for (var e = 0; e < 4; e++)
            before[e] = players.View.RunOf(e);
        var run = players.Reserve(2, 8);
        Assert.Equal((nint)run, (nint)players.View.RunOf(2));
        Assert.NotEqual((nint)before[2], (nint)run);
        Assert.True(players.IsSpilled(2));
        for (var e = 0; e < 4; e++)
        {
            if (e == 2) continue;
            Assert.Equal((nint)before[e], (nint)players.View.RunOf(e));
            Assert.False(players.IsSpilled(e));
        }
        var inline = players.Reserve(2, 4);
        Assert.Equal((nint)before[2], (nint)inline);
        Assert.False(players.IsSpilled(2));
    }

    [Fact]
    public void EntityReturnsInlineWhenItsCountFitsAgain()
    {
        using var players = new PlaybackTable(2, 4);
        var inline = players.View.RunOf(0);
        var slots = new PlaybackTable.Slots(players, 0);
        for (var i = 0; i < 10; i++)
            Assert.True(slots.TryAdd(new Playback { Timeline = 1, Tick = (ushort)i }));
        Assert.True(players.IsSpilled(0));
        for (var i = 9; i >= 4; i--)
            players.RemoveSlot(0, i);
        Assert.Equal(4, players.CountOf(0));
        Assert.False(players.IsSpilled(0));
        Assert.Equal((nint)inline, (nint)players.View.RunOf(0));
        for (var i = 0; i < 4; i++)
            Assert.Equal((ushort)i, players.View.SlotOf(0, i)->Tick);
    }

    [Fact]
    public void RatedSamplingReservesOnlyTheRatedEntity()
    {
        using var world = new TimelineWorld(Kinds());
        using var players = new PlaybackTable(4);
        using var store = new KindStore(world, players);
        for (var e = 0; e < 4; e++)
            for (var tl = 1; tl <= 4; tl++)
                players.Start(e, (ushort)tl);
        world.Tick(players); // begins all playbacks
        var runs = new Playback*[4];
        for (var e = 0; e < 4; e++)
        {
            runs[e] = players.View.RunOf(e);
            Assert.Equal(4, players.CountOf(e));
        }
        // Slot 2 of entity 0 runs at 2x on the last tick of the looping
        // duration-40 timeline, so the rated path reserves count +
        // crossings (4 + 2) for entity 0 only.
        players.View.SlotOf(0, 2)->Rate = 32;
        players.View.SlotOf(0, 2)->Tick = 39;
        world.Tick(players);
        Assert.Equal(4, players.CountOf(0));
        Assert.NotEqual((nint)runs[0], (nint)players.View.RunOf(0));
        Assert.Equal((ushort)3, players.View.SlotOf(0, 2)->Timeline);
        for (var e = 1; e < 4; e++)
        {
            Assert.Equal(4, players.CountOf(e));
            Assert.Equal((nint)runs[e], (nint)players.View.RunOf(e));
        }
    }
}
