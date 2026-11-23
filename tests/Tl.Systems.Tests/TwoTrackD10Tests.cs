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

    static (float Effect, int Runs)[] SampleLoop(bool forward)
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(TwoTrackLibraryDir(), "game.tlib")));
        Assert.Equal(1, world.View.TimelineCount);
        using var players = new PlaybackTable(4);
        using var kinds = new KindStore(world, players);
        var total = new float[4];
        var runs = new int[4];
        var system = new TwoTrack { Total = total, Runs = runs };
        var samples = new (float, int)[8];
        players.Start(0, 1);
        if (!forward)
        {
            for (var i = 0; i <= samples.Length; i++)
            {
                world.Tick(players, true);
                system.Run(world, players, true);
            }
        }
        for (var i = 0; i < samples.Length; i++)
        {
            total[0] = 0;
            runs[0] = 0;
            world.Tick(players, forward);
            system.Run(world, players, forward);
            samples[forward ? i : samples.Length - i - 1] = (total[0], runs[0]);
        }
        return samples;
    }

    [Fact]
    public void ComposeRunsOncePerActiveStepWithItsOwnTrackValuesForward()
    {
        // D10: overlap ticks run twice (once per step) with per-step values
        // (6 + 50), not twice the shared lane (100).
        float[] effects = [6, 6, 56, 56, 56, 56, 50, 50];
        int[] runs = [1, 1, 2, 2, 2, 2, 1, 1];
        var samples = SampleLoop(true);
        for (var position = 0; position < samples.Length; position++)
        {
            var (effect, runCount) = samples[position];
            Assert.Equal(effects[position], effect);
            Assert.Equal(runs[position], runCount);
        }
    }

    [Fact]
    public void ComposeRunsOncePerActiveStepWithItsOwnTrackValuesBackward()
    {
        float[] effects = [-6, -6, -56, -56, -56, -56, -50, -50];
        int[] runs = [1, 1, 2, 2, 2, 2, 1, 1];
        var samples = SampleLoop(false);
        Assert.Equal(effects, samples.Select(static sample => sample.Effect).ToArray());
        Assert.Equal(runs, samples.Select(static sample => sample.Runs).ToArray());
    }
}
