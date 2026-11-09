using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class PlaybackRebindTests
{
    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 4)]
    public void RebindClampsOrWrapsAShorterTimelineAndKeepsRateAndPhase(bool loop, ushort expected)
    {
        var timeline = new LibraryTimeline { Id = 1, Duration = 10, Loop = (byte)(loop ? 1 : 0) };
        var slot = new Playback { Timeline = 1, Tick = 24, Channel = 3, Flags = PlaybackFlags.Reverse | PlaybackFlags.Paused, Rate = 0x7b, Phase = 0xc3 };
        Assert.True(PlaybackRules.Rebind(ref slot, &timeline, 1));
        Assert.Equal(expected, slot.Tick);
        Assert.Equal(0x7b, slot.Rate);
        Assert.Equal(0xc3, slot.Phase);
        Assert.Equal(3, slot.Channel);
        Assert.Equal(PlaybackFlags.Reverse | PlaybackFlags.Paused | PlaybackFlags.ReenterPending, slot.Flags);
    }

    [Fact]
    public void RebindPreservesHeldStateAndKeepsAnUnstartedPlaybackPending()
    {
        var timeline = new LibraryTimeline { Id = 1, Duration = 10, Hold = 1 };
        var held = new Playback { Timeline = 1, Tick = 9, Flags = PlaybackFlags.Ended | PlaybackFlags.Paused, Rate = 0x34, Phase = 0x12 };
        Assert.True(PlaybackRules.Rebind(ref held, &timeline, 1));
        Assert.Equal(PlaybackFlags.Ended | PlaybackFlags.Paused, held.Flags);
        Assert.Equal(0x34, held.Rate);
        Assert.Equal(0x12, held.Phase);
        var unstarted = new Playback { Timeline = 1, Flags = PlaybackFlags.StartPending, Rate = 0x78, Phase = 0x56 };
        Assert.True(PlaybackRules.Rebind(ref unstarted, &timeline, 1));
        Assert.Equal(PlaybackFlags.StartPending, unstarted.Flags);
        Assert.Equal(0x78, unstarted.Rate);
        Assert.Equal(0x56, unstarted.Phase);
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
        players.View.SlotOf(0, 0)->Rate = 16;
        players.View.SlotOf(0, 0)->Phase = 3;
        world.Reload(bytes);
        if (attachStore) world.Tick(players);
        else { world.ApplyRequests(players); world.Advance(players); }
        Assert.Equal(before, players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(16, players.View.SlotOf(0, 0)->Rate);
        Assert.Equal(3, players.View.SlotOf(0, 0)->Phase);
        if (attachStore) world.Tick(players);
        else world.Advance(players);
        Assert.Equal(before + 1, players.View.SlotOf(0, 0)->Tick);
    }
}
