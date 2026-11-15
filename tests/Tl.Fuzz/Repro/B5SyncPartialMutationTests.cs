using Tl.Fuzz.Runtime;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class B5SyncPartialMutationTests
{
    [Fact]
    public void SyncPlaybacks_MissingTimelineOnLaterEntity_DoesNotMutateEarlierEntities()
    {
        var bigNames = new[] { "t1", "t2", "t3" };
        var smallNames = new[] { "t1", "t2" };
        var channels = new[] { "ch" };
        var bigManifest = LibraryBake.Manifest(bigNames, channels, 60);
        var smallManifest = LibraryBake.Manifest(smallNames, channels, 60);

        TypedTimeline MakePlain(string name, uint dur)
        {
            var tl = new TypedTimeline { Name = name, Duration = dur, Hold = true };
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
                End = dur,
                Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = 1, Tag = 0, TriggerTimeline = 0 }),
            });
            tl.Tracks.Add(tr);
            return tl;
        }

        var bigBytes = LibraryGen.AssembleTyped(
            [MakePlain(bigNames[0], 20), MakePlain(bigNames[1], 20), MakePlain(bigNames[2], 20)],
            channels,
            bigManifest,
            null).Bytes;
        var smallBytes = LibraryGen.AssembleTyped(
            [MakePlain(smallNames[0], 3), MakePlain(smallNames[1], 3)],
            channels,
            smallManifest,
            null).Bytes;

        using var world = new TimelineWorld(bigBytes);
        using var players = new PlaybackTable(2, 2);
        players.Start(0, 1);
        players.Start(1, 3);
        world.ApplyRequests(players);
        for (var i = 0; i < 10; i++)
            world.Advance(players, true);

        Assert.Equal((ushort)10, players.View.SlotOf(0, 0)->Tick);

        world.Reload(smallBytes);
        Assert.Throws<ArgumentException>(() => world.Advance(players, true));

        world.Reload(bigBytes);
        world.ApplyRequests(players);
        Assert.Equal((ushort)10, players.View.SlotOf(0, 0)->Tick);
    }
}
