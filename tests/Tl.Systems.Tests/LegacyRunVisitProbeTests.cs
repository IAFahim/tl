#if TL_CHECKED
using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class LegacyRunVisitProbeTests
{
    [Fact]
    public void LegacyRunVisitsOnlyPlayingEntities()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        using var world = new TimelineWorld(File.ReadAllBytes(System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "unity", "game.tlib")));
        using var players = new PlaybackTable(1000000);
        var movement = new Movement
        {
            Motion = new Motion[1000000],
            Legacy = new LegacyMotion[1000000],
            WalkStat = new WalkStat[1000000],
            JumpPower = new JumpPower[1000000],
            Stamina = new Stamina[1000000],
            Heat = new Heat[1000000],
        };
        for (var e = 0; e < 10; e++) players.Start(e, 1);
        world.ApplyRequests(players);
        Checked.ResetVisited();
        movement.Run(world, players, true);
        Assert.Equal(10, Checked.VisitedEntities);
    }
}
#endif
