using Tl;
using Tl.TestSupport;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class LoaderTests
{
    internal struct LdTrack : IBlend<LdClip>
    {
        public float Scale;
        public LdTrack(float scale) { Scale = scale; }
        public void Blend(in LdClip first, in LdClip second, float factor, out LdClip result)
        {
            result = new LdClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
    }

    internal struct LdClip
    {
        public float Amount;
        public LdClip(float amount) { Amount = amount; }
    }

    static byte[] Looping20() => new DomainBaker().Track<LdTrack, LdClip>(new LdTrack(1f)).Clip(0, 0u, 20u, new LdClip(2f)).Looping().Bake();
    static byte[] Finite10() => new DomainBaker().Track<LdTrack, LdClip>(new LdTrack(1f)).Clip(0, 0u, 10u, new LdClip(2f)).Bake();

    [Fact]
    public void LibraryLoadsManifestIdsAndAdvances()
    {
        var library = TlibBuilder.Pack([(Looping20(), "a", 0u), (Finite10(), "b", 0u)]);
        using var world = new TimelineWorld(library);
        Assert.Equal(2, world.View.TimelineCount);
        using var players = new PlaybackTable(4);
        players.Start(0, 1);
        world.ApplyRequests(players);
        for (var f = 0; f < 25; f++) world.Advance(players);
        Assert.True(players.IsPlaying(0, 1));
        Assert.Equal(5, players.SlotsOf(0)[0].Tick);
        players.Start(1, 2);
        world.ApplyRequests(players);
        for (var f = 0; f < 10; f++) world.Advance(players);
        Assert.False(players.IsPlaying(1, 2));
        Assert.Equal(0, players.CountOf(1));
    }

    [Fact]
    public void ChannelReplaceWorksThroughRealLoader()
    {
        var library = TlibBuilder.Pack([(TlibBuilder.PatchV6Meta(Looping20(), 0, 7, 0), "a", 7u), (TlibBuilder.PatchV6Meta(Finite10(), 0, 7, 0), "b", 7u)]);
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(4);
        players.Start(0, 1);
        world.ApplyRequests(players);
        players.Start(0, 2);
        world.ApplyRequests(players);
        Assert.False(players.IsPlaying(0, 1));
        Assert.True(players.IsPlaying(0, 2));
        Assert.Equal(1, players.CountOf(0));
    }

    [Fact]
    public void NextHandoverWorksThroughRealLoader()
    {
        var library = TlibBuilder.Pack([(TlibBuilder.PatchV6Meta(Finite10(), 2, 0, 0), "a", 0u), (Looping20(), "b", 0u)]);
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(4);
        players.Start(0, 1);
        world.ApplyRequests(players);
        for (var f = 0; f < 10; f++) world.Advance(players);
        Assert.True(players.IsPlaying(0, 2));
        Assert.False(players.IsPlaying(0, 1));
        var playback = players.SlotsOf(0)[0];
        Assert.Equal(2, playback.Timeline);
        Assert.Equal(0, playback.Tick);
        Assert.True((playback.Flags & PlaybackFlags.StartPending) != 0);
    }

    [Fact]
    public void HoldEndsThroughRealLoader()
    {
        var library = TlibBuilder.Pack([(TlibBuilder.PatchV6Meta(Finite10(), 0, 0, 1), "a", 0u)]);
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(4);
        players.Start(0, 1);
        world.ApplyRequests(players);
        for (var f = 0; f < 10; f++) world.Advance(players);
        Assert.True(players.IsPlaying(0, 1));
        var playback = players.SlotsOf(0)[0];
        Assert.True((playback.Flags & PlaybackFlags.Ended) != 0);
        Assert.Equal(9, playback.Tick);
    }
}
