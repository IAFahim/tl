using Tl.Fuzz.Runtime;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed class B5KindStoreRebuildPartialCommitTests
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    [Fact]
    public void KindStoreRebuild_ThrowMidEntityLoop_CommitsGenerationAndSkipsRemainingEntitiesAfterReset()
    {
        var bigNames = new[] { "t1", "t2", "t3" };
        var smallNames = new[] { "t1", "t2" };
        var channels = new[] { "ch" };
        var bigManifest = LibraryBake.Manifest(bigNames, channels, 60);
        var smallManifest = LibraryBake.Manifest(smallNames, channels, 60);

        TypedTimeline MakeKind(string name, uint dur)
        {
            var tl = new TypedTimeline { Name = name, Duration = dur, Loops = true };
            var tr = new TypedTrack
            {
                Name = "kc",
                Type = typeof(KCombineTrack),
                Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }),
            };
            tr.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = 0,
                End = dur,
                Data = LibraryGen.BytesOf(new KCombineClip { Delta = 5, StartPulse = 0, EndPulse = 0 }),
            });
            tl.Tracks.Add(tr);
            return tl;
        }

        var bigBytes = LibraryGen.AssembleTyped(
            [MakeKind(bigNames[0], 20), MakeKind(bigNames[1], 20), MakeKind(bigNames[2], 20)],
            channels,
            bigManifest,
            AllKinds).Bytes;
        var smallBytes = LibraryGen.AssembleTyped(
            [MakeKind(smallNames[0], 4), MakeKind(smallNames[1], 4)],
            channels,
            smallManifest,
            AllKinds).Bytes;

        using var world = new TimelineWorld(bigBytes);
        using var players = new PlaybackTable(3, 2);
        using var store = new KindStore(world, players);
        using var worldCtrl = new TimelineWorld(bigBytes);
        using var playersCtrl = new PlaybackTable(3, 2);
        using var storeCtrl = new KindStore(worldCtrl, playersCtrl);

        var c = new KindIntCol[3];
        var eCol = new KindEachCol[3];
        var iCol = new KindIdleCol[3];
        var fCol = new KindFloatCol[3];
        var cCtrl = new KindIntCol[3];
        var eCtrl = new KindEachCol[3];
        var iCtrl = new KindIdleCol[3];
        var fCtrl = new KindFloatCol[3];
        var inp = new[]
        {
            new KindInputCol { Factor = 1 },
            new KindInputCol { Factor = 1 },
            new KindInputCol { Factor = 1 },
        };

        players.Start(0, 1);
        players.Start(1, 3);
        players.Start(2, 1);
        playersCtrl.Start(0, 1);
        playersCtrl.Start(1, 3);
        playersCtrl.Start(2, 1);

        for (var s = 0; s < 3; s++)
        {
            world.Tick(players, true);
            worldCtrl.Tick(playersCtrl, true);
            new KindFuzzSystem { CombineCol = c, EachCol = eCol, IdleCol = iCol, FloatCol = fCol, Input = inp }.Run(world, players, true);
            new KindFuzzSystem { CombineCol = cCtrl, EachCol = eCtrl, IdleCol = iCtrl, FloatCol = fCtrl, Input = inp }.Run(worldCtrl, playersCtrl, true);
        }

        world.Reload(smallBytes);
        Assert.Throws<ArgumentException>(() => world.Tick(players, true));

        players.Reset(1);
        world.Tick(players, true);

        playersCtrl.Reset(1);
        worldCtrl.ApplyRequests(playersCtrl);
        worldCtrl.Reload(smallBytes);
        worldCtrl.Tick(playersCtrl, true);
        new KindFuzzSystem { CombineCol = c, EachCol = eCol, IdleCol = iCol, FloatCol = fCol, Input = inp }.Run(world, players, true);
        new KindFuzzSystem { CombineCol = cCtrl, EachCol = eCtrl, IdleCol = iCtrl, FloatCol = fCtrl, Input = inp }.Run(worldCtrl, playersCtrl, true);

        Assert.Equal(cCtrl[0].Sum, c[0].Sum);
        Assert.Equal(cCtrl[2].Sum, c[2].Sum);
    }
}
