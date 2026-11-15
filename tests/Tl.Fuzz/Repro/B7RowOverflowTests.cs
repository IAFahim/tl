using Tl.Fuzz.Runtime;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class B7RowOverflowTests
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    [Fact]
    public void PulseFlood_32769EntriesWithInline4_Accepts32768FailsAt32769Claiming65535()
    {
        var names = new[] { "pf_1" };
        var manifest = LibraryBake.Manifest(names, [], 60);
        var overrides = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
        {
            [typeof(KEachInt).FullName!] = new KindOverride(inline: 4, gamePulses: 0, maxInlineBytes: 64),
        };
        var tl = new TypedTimeline { Name = "pf_1", Duration = 4, Loops = true };
        var tr = new TypedTrack { Name = "ke", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }) };
        tr.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = 0, End = 4, Data = LibraryGen.BytesOf(new KEachClip { Delta = 0 }) });
        tl.Tracks.Add(tr);

        var kinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], overrides);
        var lib = LibraryGen.AssembleTyped([tl], [], manifest, kinds, 64, 1, overrides);

        using var world = new TimelineWorld(lib.Bytes);
        using var players = new PlaybackTable(1, 2);
        using var store = new KindStore(world, players);
        var pulses = store.Pulses<int>(typeof(KEachInt).FullName!);

        players.Start(0, 1);
        for (var i = 0; i < 32766; i++) pulses.To(0, 1);
        world.Tick(players, true);
        var eachK = world.View.Kinds.IndexOf(typeof(KEachInt).FullName!);
        Assert.Equal((ushort)32768, store.Table(eachK)->Length[0]);

        for (var i = 0; i < 32768; i++) pulses.To(0, 1);
        var ex = Record.Exception(() => world.Tick(players, true));
        Assert.Null(ex);
    }

    [Fact]
    public void SuperSampling_WarmTickUnder65535Entries_ThrowsRowOverflowAtHalfLimit()
    {
        const int timelineCount = 8;
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"ss_{i + 1:D3}";
        var manifest = LibraryBake.Manifest(names, [], 60);

        var typed = new TypedTimeline[timelineCount];
        for (var i = 0; i < timelineCount; i++)
        {
            var tl = new TypedTimeline { Name = names[i], Duration = 4, Loops = true };
            for (var trk = 0; trk < 175; trk++)
            {
                var tr = new TypedTrack { Name = $"ke{trk}", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }) };
                tr.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = 0, End = 4, Data = LibraryGen.BytesOf(new KEachClip { Delta = 1, StartPulse = 1, EndPulse = 2 }) });
                tl.Tracks.Add(tr);
            }
            typed[i] = tl;
        }

        var overrides = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
        {
            [typeof(KEachInt).FullName!] = new KindOverride(inline: 4, gamePulses: 0, maxInlineBytes: 64),
        };
        var kinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], overrides);
        var lib = LibraryGen.AssembleTyped(typed, [], manifest, kinds, 64, 1, overrides);
        using var world = new TimelineWorld(lib.Bytes);
        using var players = new PlaybackTable(1, timelineCount);
        using var store = new KindStore(world, players);
        var combineCol = new KindIntCol[1];
        var eachCol = new KindEachCol[1];
        var idleCol = new KindIdleCol[1];
        var floatCol = new KindFloatCol[1];
        var inputCol = new[] { new KindInputCol { Factor = 1 } };

        for (ushort tl = 1; tl <= timelineCount; tl++)
        {
            players.Start(0, tl);
            players.SetSpeed(0, tl, 255);
        }

        world.Tick(players, true);
        new KindFuzzSystem { CombineCol = combineCol, EachCol = eachCol, IdleCol = idleCol, FloatCol = floatCol, Input = inputCol }.Run(world, players, true);
        var ex = Record.Exception(() => world.Tick(players, true));
        Assert.Null(ex);
    }

    [Fact]
    public void MergePulses_AfterRowOverflowThrow_DoesNotWedgeSubsequentValidTick()
    {
        var names = new[] { "pf_1" };
        var manifest = LibraryBake.Manifest(names, [], 60);
        var tl = new TypedTimeline { Name = "pf_1", Duration = 4, Loops = true };
        var tr = new TypedTrack { Name = "ke", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }) };
        tr.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = 0, End = 4, Data = LibraryGen.BytesOf(new KEachClip { Delta = 0 }) });
        tl.Tracks.Add(tr);

        var lib = LibraryGen.AssembleTyped([tl], [], manifest, AllKinds);

        using var world = new TimelineWorld(lib.Bytes);
        using var players = new PlaybackTable(1, 2);
        using var store = new KindStore(world, players);
        var pulses = store.Pulses<int>(typeof(KEachInt).FullName!);

        players.Start(0, 1);
        for (var i = 0; i < 70000; i++) pulses.To(0, 1);

        Assert.Throws<InvalidOperationException>(() => world.Tick(players, true));

        var nextValid = Record.Exception(() => world.Tick(players, true));
        Assert.Null(nextValid);
    }
}
