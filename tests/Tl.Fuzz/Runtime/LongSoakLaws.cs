using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class LongSoakLaws
{
    static ulong HashPlainStep(ulong rollingCueHash, PlainMotion[] motion, PlainStats[] stats, PlaybackTable players)
    {
        var h = TlStateHash.Begin();
        h.Lane(rollingCueHash);
        fixed (PlainMotion* mp = motion) h.Bytes((byte*)mp, motion.Length * sizeof(PlainMotion));
        fixed (PlainStats* sp = stats) h.Bytes((byte*)sp, stats.Length * sizeof(PlainStats));
        FuzzRunner.MixPlaybacks(ref h, players);
        return h.Value;
    }

    static ulong HashKindStep(KindIntCol[] comb, KindEachCol[] each, KindIdleCol[] idle, KindFloatCol[] flt, PlaybackTable players, long dropped)
    {
        var h = TlStateHash.Begin();
        fixed (KindIntCol* cp = comb) h.Bytes((byte*)cp, comb.Length * sizeof(KindIntCol));
        fixed (KindEachCol* ep = each) h.Bytes((byte*)ep, each.Length * sizeof(KindEachCol));
        fixed (KindIdleCol* ip = idle) h.Bytes((byte*)ip, idle.Length * sizeof(KindIdleCol));
        fixed (KindFloatCol* fp = flt) h.Bytes((byte*)fp, flt.Length * sizeof(KindFloatCol));
        h.Int((int)dropped);
        h.Int((int)(dropped >> 32));
        FuzzRunner.MixPlaybacks(ref h, players);
        return h.Value;
    }

    static ulong FoldCues(ulong current, ReadOnlySpan<EventRecord<FuzzCue>> items)
    {
        var h = new TlStateHash { Value = current };
        h.Int(items.Length);
        for (var i = 0; i < items.Length; i++)
        {
            var c = items[i].Value;
            h.Int(items[i].Entity.Index);
            h.Int(items[i].Slot);
            h.Int(c.Tick);
            h.Int(c.Tag);
            h.Int(c.Direction);
        }
        return h.Value;
    }

    [Fact]
    public void PlainSoak_20kTo200kTicks_CheckpointsHashEvery1000TicksAgainstZeroHorizon()
    {
        FuzzRunner.Run("B7-PlainLongSoak", 0x42370001UL, 1, (rng, iter) =>
        {
            const int entities = 16;
            const int slotsPerEntity = 6;
            const int timelines = 4;
            const int channels = 2;
            var limit = (byte)(1 + rng.NextInt(255));
            var steps = 20_000 + rng.NextInt(180_001);
            var reloadStep = steps / 2;
            var maxDur1 = rng.NextBool() ? 65000 : 120;
            var lib1 = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, maxDur1, true, true, true);
            var lib2 = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 240, true, true, true);

            using var world0 = new TimelineWorld(lib1.Bytes);
            using var worldG = new TimelineWorld(lib1.Bytes);
            using var players0 = new PlaybackTable(entities, slotsPerEntity, 0);
            using var playersG = new PlaybackTable(entities, slotsPerEntity, limit);
            using var cues0 = new Events<FuzzCue>(4096);
            using var cuesG = new Events<FuzzCue>(4096);

            var motion0 = new PlainMotion[entities];
            var motionG = new PlainMotion[entities];
            var stats0 = new PlainStats[entities];
            var statsG = new PlainStats[entities];
            var input = new PlainInput[entities];
            for (var e = 0; e < entities; e++) input[e] = new PlainInput { Mult = 1 + (e & 3) };

            var curDurations = lib1.Durations;
            var curTls = lib1.TimelineCount;
            var sys0 = new PlainFuzzSystem { Motion = motion0, Stats = stats0, Input = input, Requests = world0.Requests(players0), Cues = cues0.Writer };
            var sysG = new PlainFuzzSystem { Motion = motionG, Stats = statsG, Input = input, Requests = worldG.Requests(playersG), Cues = cuesG.Writer };

            for (var e = 0; e < entities; e++)
            {
                var tl = (ushort)(1 + (e % curTls));
                var startTick = curDurations[tl - 1] > 10 ? (ushort)(curDurations[tl - 1] - 5) : (ushort)0;
                players0.Start(e, tl, startTick);
                playersG.Start(e, tl, startTick);
            }

            var cueHash0 = TlStateHash.Seed;
            var cueHashG = TlStateHash.Seed;

            for (var step = 0; step < steps; step++)
            {
                if (step == reloadStep)
                {
                    world0.Reload(lib2.Bytes);
                    worldG.Reload(lib2.Bytes);
                    curDurations = lib2.Durations;
                    curTls = lib2.TimelineCount;
                    sys0 = new PlainFuzzSystem { Motion = motion0, Stats = stats0, Input = input, Requests = world0.Requests(players0), Cues = cues0.Writer };
                    sysG = new PlainFuzzSystem { Motion = motionG, Stats = statsG, Input = input, Requests = worldG.Requests(playersG), Cues = cuesG.Writer };
                }

                if ((step & 7) == 0)
                {
                    var nOps = 1 + rng.NextInt(4);
                    for (var i = 0; i < nOps; i++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, curTls, channels, curDurations);
                        if (op.Op == RequestActionKind.SetSpeed || op.Op == RequestActionKind.SetChannelSpeed)
                            op.Arg = (ushort)(rng.NextInt(6) switch { 0 => 1, 1 => 7, 2 => 16, 3 => 33, 4 => 64, _ => 128 });
                        LibraryGen.ApplyOp(players0, in op);
                        LibraryGen.ApplyOp(playersG, in op);
                    }
                }

                var fwd = (step & 15) != 15;
                world0.ApplyRequests(players0);
                worldG.ApplyRequests(playersG);
                sys0.Run(world0, players0, fwd);
                sysG.Run(worldG, playersG, fwd);
                world0.Advance(players0, fwd);
                worldG.Advance(playersG, fwd);
                cueHash0 = FoldCues(cueHash0, cues0.Drain());
                cueHashG = FoldCues(cueHashG, cuesG.Drain());

                if (((step + 1) % 1000 == 0) || step == steps - 1)
                {
                    var h0 = HashPlainStep(cueHash0, motion0, stats0, players0);
                    var hG = HashPlainStep(cueHashG, motionG, statsG, playersG);
                    if (h0 != hG)
                    {
                        var err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(motion0), MemoryMarshal.AsBytes<PlainMotion>(motionG), $"step={step} motion")
                            ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(stats0), MemoryMarshal.AsBytes<PlainStats>(statsG), $"step={step} stats")
                            ?? FuzzRunner.ComparePlaybacks(players0, playersG)
                            ?? $"step={step} hash0={h0:X16} != hashG={hG:X16} (cueHash0={cueHash0:X16}, cueHashG={cueHashG:X16})";
                        FuzzRunner.Fail("B7-PlainLongSoak", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42370001UL, iter, err);
                    }
                }
            }
        });
    }

    [Fact]
    public void KindAndPulseSoak_20kTo200kTicks_CheckpointsHashEvery1000TicksAgainstZeroHorizon()
    {
        FuzzRunner.Run("B7-KindLongSoak", 0x42370002UL, 1, (rng, iter) =>
        {
            const int entities = 16;
            const int slotsPerEntity = 4;
            const int timelines = 4;
            const int channels = 2;
            var limit = (byte)(1 + rng.NextInt(255));
            var steps = 20_000 + rng.NextInt(180_001);
            var reloadStep = steps / 2;
            var lib1 = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 180, true, true, true, true);
            var lib2 = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 240, true, true, true, true);

            using var world0 = new TimelineWorld(lib1.Bytes);
            using var worldG = new TimelineWorld(lib1.Bytes);
            using var players0 = new PlaybackTable(entities, slotsPerEntity, 0);
            using var playersG = new PlaybackTable(entities, slotsPerEntity, limit);
            using var store0 = new KindStore(world0, players0);
            using var storeG = new KindStore(worldG, playersG);

            var comb0 = new KindIntCol[entities];
            var combG = new KindIntCol[entities];
            var each0 = new KindEachCol[entities];
            var eachG = new KindEachCol[entities];
            var idle0 = new KindIdleCol[entities];
            var idleG = new KindIdleCol[entities];
            var flt0 = new KindFloatCol[entities];
            var fltG = new KindFloatCol[entities];
            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) input[e] = new KindInputCol { Factor = 1 + (e & 1) };

            var pComb0 = store0.Pulses<int>(typeof(KCombineInt).FullName!);
            var pCombG = storeG.Pulses<int>(typeof(KCombineInt).FullName!);
            var pEach0 = store0.Pulses<int>(typeof(KEachInt).FullName!);
            var pEachG = storeG.Pulses<int>(typeof(KEachInt).FullName!);

            var curDurations = lib1.Durations;
            var curTls = lib1.TimelineCount;
            var sys0 = new KindFuzzSystem { CombineCol = comb0, EachCol = each0, IdleCol = idle0, FloatCol = flt0, Input = input };
            var sysG = new KindFuzzSystem { CombineCol = combG, EachCol = eachG, IdleCol = idleG, FloatCol = fltG, Input = input };

            for (var e = 0; e < entities; e++)
            {
                var tl = (ushort)(1 + (e % curTls));
                players0.Start(e, tl, (ushort)(e & 3));
                playersG.Start(e, tl, (ushort)(e & 3));
            }

            for (var step = 0; step < steps; step++)
            {
                if (step == reloadStep)
                {
                    world0.Reload(lib2.Bytes);
                    worldG.Reload(lib2.Bytes);
                    curDurations = lib2.Durations;
                    curTls = lib2.TimelineCount;
                }

                if ((step & 7) == 0)
                {
                    var nOps = 1 + rng.NextInt(4);
                    for (var i = 0; i < nOps; i++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, curTls, channels, curDurations);
                        if (op.Op == RequestActionKind.SetSpeed || op.Op == RequestActionKind.SetChannelSpeed)
                            op.Arg = (ushort)(rng.NextInt(6) switch { 0 => 1, 1 => 7, 2 => 16, 3 => 33, 4 => 64, _ => 128 });
                        LibraryGen.ApplyOp(players0, in op);
                        LibraryGen.ApplyOp(playersG, in op);
                    }
                    var pe = rng.NextInt(entities);
                    var pv = rng.NextInt(19) - 9;
                    pComb0.To(pe, pv);
                    pCombG.To(pe, pv);
                    if ((step & 16) != 0)
                    {
                        pEach0.To(pe, pv);
                        pEachG.To(pe, pv);
                    }
                }

                var fwd = (step & 31) != 31;
                world0.Tick(players0, fwd);
                worldG.Tick(playersG, fwd);
                sys0.Run(world0, players0, fwd);
                sysG.Run(worldG, playersG, fwd);

                if (((step + 1) % 1000 == 0) || step == steps - 1)
                {
                    var h0 = HashKindStep(comb0, each0, idle0, flt0, players0, store0.DroppedPulses);
                    var hG = HashKindStep(combG, eachG, idleG, fltG, playersG, storeG.DroppedPulses);
                    if (h0 != hG || store0.DroppedPulses != storeG.DroppedPulses)
                    {
                        var err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(comb0), MemoryMarshal.AsBytes<KindIntCol>(combG), $"step={step} comb")
                            ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(each0), MemoryMarshal.AsBytes<KindEachCol>(eachG), $"step={step} each")
                            ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(idle0), MemoryMarshal.AsBytes<KindIdleCol>(idleG), $"step={step} idle")
                            ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(flt0), MemoryMarshal.AsBytes<KindFloatCol>(fltG), $"step={step} flt")
                            ?? FuzzRunner.ComparePlaybacks(players0, playersG)
                            ?? $"step={step} hash0={h0:X16} != hashG={hG:X16} dropped={store0.DroppedPulses}!={storeG.DroppedPulses}";
                        FuzzRunner.Fail("B7-KindLongSoak", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42370002UL, iter, err);
                    }
                }
            }
        });
    }

    [Fact(Skip = "#643")]
    public void SuperSamplingWarmTickUnder65535Entries_DoesNotFailAtHalfRowLimit()
    {
        FuzzRunner.Run("B7-WarmRowOverflowHalfLimit", 0x42370003UL, 1, (rng, iter) =>
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
            world.Tick(players, true);
        });
    }
}
