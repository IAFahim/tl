using Game;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class LoaderRunTests
{
    internal static string LoaderLibraryDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        var library = Path.Combine(dir, "tests", "Tl.Systems.Tests", "LoaderLibrary");
        Assert.True(File.Exists(Path.Combine(library, "game.tlib")), "Loader library golden missing; bake it with: tlb library tests/Tl.Systems.Tests/LoaderLibrary --assembly <Tl.Systems.Tests.dll> --out game.tlib.");
        return library;
    }

    void Step(TimelineWorld world, PlaybackTable players, Motion[] motion, JumpPower[] power, Stats[] stats, InputState[] input, Anim[] anim, Foot[] feet, Events<Cue> cues, bool forward = true)
    {
        world.ApplyRequests(players);
        new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Anim = anim, Feet = feet, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players, forward);
        world.Advance(players, forward);
    }

    [Fact]
    public void LoaderBuiltLibraryRunsFoldLanesFramesEndsAndMarkers()
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(LoaderLibraryDir(), "game.tlib")));
        Assert.Equal(1, world.View.TimelineCount);
        using var players = new PlaybackTable(4);
        var motion = new Motion[4];
        var power = new JumpPower[4];
        var stats = new Stats[4];
        var input = new InputState[4];
        var anim = new Anim[4];
        var feet = new Foot[4];
        using var cues = new Events<Cue>(64);
        for (var i = 0; i < 4; i++) power[i] = new JumpPower(1f);
        players.Start(0, 1);
        for (var f = 0; f < 4; f++) Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(8f, motion[0].Y);
        var codes = new List<int>();
        for (var f = 0; f < 4; f++)
        {
            Step(world, players, motion, power, stats, input, anim, feet, cues);
            foreach (var record in cues.Drain()) codes.Add(record.Value.Code);
        }
        Assert.Equal(0f, motion[0].Y);
        Assert.Contains(1003, codes);
        for (var f = 0; f < 8; f++) Step(world, players, motion, power, stats, input, anim, feet, cues, false);
        Assert.Equal(0f, motion[0].Y);
        Assert.Equal(0, players.SlotsOf(0)[0].Tick);
    }
}
