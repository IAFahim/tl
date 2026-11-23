using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class RequestTrimTests
{
    struct Clip { public int Value; }
    struct Track : IBlend<Clip>
    {
        public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
    }

    [Fact]
    public void ARequestHeavyWorldKeepsItsQueueAndAQuietStepReleasesIt()
    {
        var tlb = new DomainBaker().Track<Track, Clip>(default).Clip(0, 0, 64, new Clip { Value = 1 }).Looping().Bake();
        using var world = new TimelineWorld(LibraryAssembler.Assemble([new LibraryPart { Name = "Run", Tlb = tlb }], 60, LibrarySettings.Default));
        using var players = new PlaybackTable(8192);
        for (var step = 0; step < 3; step++)
        {
            for (var e = 0; e < 8192; e++) players.Start(e, 1);
            world.ApplyRequests(players);
            Assert.True(RequestQueue.CapacityOf(players.Pending) >= 8192);
        }
        world.ApplyRequests(players);
        Assert.Equal(0, RequestQueue.CapacityOf(players.Pending));
        Assert.True(players.IsPlaying(8191, 1));
    }
}
