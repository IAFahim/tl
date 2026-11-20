using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class UnreachableStartTests
{
    static byte[] Kinds() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "kinds.tlib"));

    static ushort FirstTimelineWithKinds(TimelineWorld world)
    {
        var section = world.View.Kinds;
        for (ushort timeline = 1; timeline <= section.TimelineCount; timeline++)
            if (section.TimelineOf(timeline)->KindMask != 0)
                return timeline;
        return 0;
    }

    static string[] KindNames(TimelineWorld world)
    {
        var section = world.View.Kinds;
        var names = new string[section.KindCount];
        for (var kind = 0; kind < names.Length; kind++) names[kind] = section.NameOf(kind);
        return names;
    }

    [Fact]
    public void AStartTheEntityCannotHoldIsCountedAndDroppedOrThrowsInCheckedBuilds()
    {
        using var world = new TimelineWorld(Kinds());
        using var players = new PlaybackTable(2);
        var timeline = FirstTimelineWithKinds(world);
        Assert.NotEqual(0, timeline);
        using var kinds = new KindStore(world, players, KindNames(world), [~0ul, 0ul]);
        players.Start(0, timeline);
        players.Start(1, timeline);
#if TL_CHECKED
        var thrown = Assert.Throws<ArgumentException>(() => world.Tick(players));
        Assert.Contains("cannot reach kind", thrown.Message);
        Assert.Equal(0, players.CountOf(0));
#else
        world.Tick(players);
        Assert.Equal(1L, players.DroppedStarts);
        Assert.True(players.IsPlaying(0, timeline));
        Assert.False(players.IsPlaying(1, timeline));
        Assert.Equal(0, players.CountOf(1));
        players.Start(1, timeline);
        world.Tick(players);
        Assert.Equal(2L, players.DroppedStarts);
#endif
    }

    [Fact]
    public void AReachableStartCountsNothing()
    {
        using var world = new TimelineWorld(Kinds());
        using var players = new PlaybackTable(2);
        using var kinds = new KindStore(world, players);
        var timeline = FirstTimelineWithKinds(world);
        players.Start(0, timeline);
        players.Start(1, timeline);
        world.Tick(players);
        Assert.Equal(0L, players.DroppedStarts);
        Assert.True(players.IsPlaying(0, timeline));
        Assert.True(players.IsPlaying(1, timeline));
    }
}
