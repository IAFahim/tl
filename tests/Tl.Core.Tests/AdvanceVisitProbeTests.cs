#if TL_CHECKED
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class AdvanceVisitProbeTests
{
    [Fact]
    public void AdvanceVisitsOnlyPlayingEntities()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "v1.tlib"));
        using var world = new TimelineWorld(bytes);
        using var players = new PlaybackTable(1000000);
        for (var e = 0; e < 10; e++) players.Start(e, 1);
        world.ApplyRequests(players);
        Checked.ResetVisited();
        world.Advance(players);
        Assert.Equal(10, Checked.VisitedEntities);
    }
}
#endif
