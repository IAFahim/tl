using Game;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class LoaderRunTests
{
    internal static string LoaderLibraryDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        var library = Path.Combine(dir, "tests", "Tl.Systems.Tests", "LoaderLibrary");
        Assert.True(File.Exists(Path.Combine(library, "game.tlib")), "Loader library golden missing; bake it with: tl library tests/Tl.Systems.Tests/LoaderLibrary --assembly <Tl.Systems.Tests.dll> --out game.tlib.");
        return library;
    }

    void Step(TimelineWorld world, PlaybackTable players, Motion[] motion, JumpPower[] power, Stats[] stats, InputState[] input, Anim[] anim, Foot[] feet, Events<Cue> cues, bool forward = true)
    {
        world.ApplyRequests(players);
        new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Anim = anim, Feet = feet, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players, forward);
        world.Advance(players, forward);
    }

    [Fact]
    public void AShorterLiveLibraryDoesNotReplayMarkersAtTheReplacementPosition()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(ArcTrack).Assembly]);
        byte[] Library(int duration, int marker)
        {
            var json = $$$"""
                {"name":"mini","duration":{{{duration}}},"loop":true,"tracks":[
                {"namespace":"Game","type":"ArcTrack","data":{"Scale":1},"clips":[
                {"namespace":"Game","type":"ArcClip","start":0,"end":{{{duration}}},"data":{"Velocity":2}}]}],
                "markers":[{"namespace":"Game","type":"FootstepMarker","tick":0,"data":{"Step":{{{marker}}}}}]}
                """;
            return LibraryAssembler.Assemble([new LibraryPart { Name = "mini", Tlb = TimelineBaker.BakeJson(json, resolver) }], 60, LibrarySettings.Default);
        }
        using var world = new TimelineWorld(Library(8, 1));
        using var players = new PlaybackTable(1);
        using var cues = new Events<Cue>(16);
        var motion = new Motion[1];
        var power = new[] { new JumpPower(1f) };
        var stats = new Stats[1];
        var input = new InputState[1];
        var anim = new Anim[1];
        var feet = new Foot[1];
        players.Start(0, 1);
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(1001, cues.Drain()[0].Value.Code);
        Assert.Equal(1, players.View.SlotOf(0, 0)->Tick);
        world.Reload(Library(1, 2));
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(0, players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(0, cues.Count);
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(1002, cues.Drain()[0].Value.Code);
    }

    [Fact]
    public void AHeldPlaybackRewindsWhenReversedAndPlaysForwardAgain()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(ArcTrack).Assembly]);
        var json = """
            {"name":"held","duration":4,"hold":true,"tracks":[
            {"namespace":"Game","type":"ArcTrack","data":{"Scale":1},"clips":[
            {"namespace":"Game","type":"ArcClip","start":0,"end":4,"data":{"Velocity":2}}]}]}
            """;
        using var world = new TimelineWorld(LibraryAssembler.Assemble([new LibraryPart { Name = "held", Tlb = TimelineBaker.BakeJson(json, resolver) }], 60, LibrarySettings.Default));
        using var players = new PlaybackTable(1);
        using var cues = new Events<Cue>(16);
        var motion = new Motion[1];
        var power = new[] { new JumpPower(1f) };
        var stats = new Stats[1];
        var input = new InputState[1];
        var anim = new Anim[1];
        var feet = new Foot[1];
        players.Start(0, 1);
        for (var f = 0; f < 6; f++) Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(8f, motion[0].Y);
        Assert.Equal(PlaybackFlags.Ended, players.View.SlotOf(0, 0)->Flags);
        Assert.Equal(3, players.View.SlotOf(0, 0)->Tick);
        for (var f = 0; f < 3; f++) Step(world, players, motion, power, stats, input, anim, feet, cues, false);
        Assert.Equal(8f, motion[0].Y);
        players.Reverse(0, 1, true);
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(6f, motion[0].Y);
        Assert.Equal(2, players.View.SlotOf(0, 0)->Tick);
        for (var f = 0; f < 4; f++) Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(0f, motion[0].Y);
        Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Reverse, players.View.SlotOf(0, 0)->Flags);
        Assert.Equal(0, players.View.SlotOf(0, 0)->Tick);
        players.Reverse(0, 1, false);
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(2f, motion[0].Y);
        Assert.Equal(1, players.View.SlotOf(0, 0)->Tick);
        players.Seek(0, 1, 3);
        Step(world, players, motion, power, stats, input, anim, feet, cues);
        Assert.Equal(4f, motion[0].Y);
        Assert.Equal(3, players.View.SlotOf(0, 0)->Tick);
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
