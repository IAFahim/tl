using Tl.Fuzz.Runtime;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class B5ReloadCorruptLibTests
{
    [Fact]
    public void Reload_TruncatedLibrary_PreservesPreviouslyLoadedWorld()
    {
        var names = new[] { "r1" };
        var channels = new[] { "ch" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var tl = new TypedTimeline { Name = names[0], Duration = 16, Loops = true };
        var tr = new TypedTrack
        {
            Name = "pl",
            Type = typeof(FuzzPlainTrack),
            Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 }),
        };
        tr.Clips.Add(new TypedClip
        {
            Type = typeof(FuzzPlainClip),
            Start = 0,
            End = 16,
            Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = 2, Tag = 1, TriggerTimeline = 0 }),
        });
        tl.Tracks.Add(tr);
        var validBytes = LibraryGen.AssembleTyped([tl], channels, manifest, null).Bytes;

        using var world = new TimelineWorld(validBytes);
        using var players = new PlaybackTable(1, 2);
        players.Start(0, 1);
        world.ApplyRequests(players);
        world.Advance(players, true);

        Assert.Throws<ArgumentException>(() => world.Reload(validBytes.AsSpan(0, 48)));

        world.ApplyRequests(players);
        world.Advance(players, true);
        Assert.Equal(1, players.CountOf(0));
        Assert.Equal((ushort)2, players.View.SlotOf(0, 0)->Tick);
    }
}
