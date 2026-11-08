using D10;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class TwoTrackD10Tests
{
    internal static string TwoTrackLibraryDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        var library = Path.Combine(dir, "tests", "Tl.Systems.Tests", "TwoTrackLibrary");
        Assert.True(File.Exists(Path.Combine(library, "game.tlib")), "TwoTrack library golden missing; bake it with: tl library tests/Tl.Systems.Tests/TwoTrackLibrary --assembly <Tl.Systems.Tests.dll> --out game.tlib.");
        return library;
    }

    static (float Effect, int Runs) AtTick(ushort position, bool forward)
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(TwoTrackLibraryDir(), "game.tlib")));
        Assert.Equal(1, world.View.TimelineCount);
        using var players = new PlaybackTable(4);
        var total = new float[4];
        var runs = new int[4];
        players.Start(0, 1);
        for (var i = 0; i < position; i++)
        {
            world.ApplyRequests(players);
            new TwoTrack { Total = total, Runs = runs }.Run(world, players, true);
            world.Advance(players, true);
        }
        total[0] = 0f;
        runs[0] = 0;
        world.ApplyRequests(players);
        new TwoTrack { Total = total, Runs = runs }.Run(world, players, forward);
        return (total[0], runs[0]);
    }

    [Fact]
    public void ComposeRunsOncePerActiveStepWithItsOwnTrackValuesForward()
    {
        // D10: overlap ticks run twice (once per step) with per-step values
        // (6 + 50), not twice the shared lane (100).
        float[] effects = [6, 6, 56, 56, 56, 56, 50, 50];
        int[] runs = [1, 1, 2, 2, 2, 2, 1, 1];
        for (var position = 0; position <= 7; position++)
        {
            var (effect, runCount) = AtTick((ushort)position, true);
            Assert.Equal(effects[position], effect);
            Assert.Equal(runs[position], runCount);
        }
    }

    [Fact]
    public void ComposeRunsOncePerActiveStepWithItsOwnTrackValuesBackward()
    {
        float[] effects = [-6, -6, -56, -56, -56, -56, -50, -50];
        int[] runs = [1, 1, 2, 2, 2, 2, 1, 1];
        for (var position = 0; position <= 7; position++)
        {
            var (effect, runCount) = AtTick((ushort)position, false);
            Assert.Equal(effects[position], effect);
            Assert.Equal(runs[position], runCount);
        }
    }
}
