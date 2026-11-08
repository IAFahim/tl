using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class PlaybackRebindTests
{
    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 4)]
    public void RebindClampsOrWrapsAShorterTimelineAndKeepsReserved(bool loop, ushort expected)
    {
        var timeline = new LibraryTimeline { Id = 1, Duration = 10, Loop = (byte)(loop ? 1 : 0) };
        var slot = new Playback { Timeline = 1, Tick = 24, Channel = 3, Flags = PlaybackFlags.Reverse | PlaybackFlags.Paused, Reserved = 0xc37b };
        Assert.True(PlaybackRules.Rebind(ref slot, &timeline, 1));
        Assert.Equal(expected, slot.Tick);
        Assert.Equal(0xc37b, slot.Reserved);
        Assert.Equal(3, slot.Channel);
        Assert.Equal(PlaybackFlags.Reverse | PlaybackFlags.Paused | PlaybackFlags.ReenterPending, slot.Flags);
    }

    [Fact]
    public void RebindPreservesHeldStateAndKeepsAnUnstartedPlaybackPending()
    {
        var timeline = new LibraryTimeline { Id = 1, Duration = 10, Hold = 1 };
        var held = new Playback { Timeline = 1, Tick = 9, Flags = PlaybackFlags.Ended | PlaybackFlags.Paused, Reserved = 0x1234 };
        Assert.True(PlaybackRules.Rebind(ref held, &timeline, 1));
        Assert.Equal(PlaybackFlags.Ended | PlaybackFlags.Paused, held.Flags);
        Assert.Equal(0x1234, held.Reserved);
        var unstarted = new Playback { Timeline = 1, Flags = PlaybackFlags.StartPending, Reserved = 0x5678 };
        Assert.True(PlaybackRules.Rebind(ref unstarted, &timeline, 1));
        Assert.Equal(PlaybackFlags.StartPending, unstarted.Flags);
        Assert.Equal(0x5678, unstarted.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AKindsFreeLibraryReloadKeepsTheTickForOneStep(bool attachStore)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "v1.tlib"));
        using var world = new TimelineWorld(bytes);
        using var players = new PlaybackTable(1);
        using var store = attachStore ? new KindStore(world, players) : null;
        players.Start(0, 1);
        if (attachStore) world.Tick(players);
        else world.ApplyRequests(players);
        for (var i = 0; i < 3; i++)
            if (attachStore) world.Tick(players);
            else world.Advance(players);
        var before = players.View.SlotOf(0, 0)->Tick;
        players.View.SlotOf(0, 0)->Reserved = 0xc37b;
        world.Reload(bytes);
        if (attachStore) world.Tick(players);
        else { world.ApplyRequests(players); world.Advance(players); }
        Assert.Equal(before, players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(0xc37b, players.View.SlotOf(0, 0)->Reserved);
        if (attachStore) world.Tick(players);
        else world.Advance(players);
        Assert.Equal(before + 1, players.View.SlotOf(0, 0)->Tick);
    }
}
