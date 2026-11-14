using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class RequestRobustnessLaws
{
    static readonly RequestOp[] DistinctSafeOps =
    [
        RequestOp.Start,
        RequestOp.Stop,
        RequestOp.Pause,
        RequestOp.Resume,
        RequestOp.Seek,
        RequestOp.Reverse,
        RequestOp.Speed,
    ];

    static void IssueRandomValidEntityFlood(ref FuzzRandom rng, PlaybackTable players, int entity, int timelineCount, int channelCount, uint[] durations)
    {
        var count = 1 + rng.NextInt(32);
        for (var i = 0; i < count; i++)
        {
            var kind = rng.NextInt(18);
            var tl = (ushort)(1 + rng.NextInt(timelineCount));
            var badChan = (byte)(channelCount + 1 + rng.NextInt(4));
            var chan = rng.NextInt(5) == 0 ? (byte)0 : rng.NextInt(4) == 0 ? badChan : (byte)(1 + rng.NextInt(channelCount));
            var pastEnd = (ushort)(durations[tl - 1] + 5 + rng.NextInt(500));

            switch (kind)
            {
                case 0:
                    players.Start(entity, tl, rng.NextBool() ? pastEnd : (ushort)rng.NextInt((int)durations[tl - 1]));
                    break;
                case 1:
                    players.Stop(entity, tl);
                    break;
                case 2:
                    players.Stop(entity, (ushort)(timelineCount + 50));
                    break;
                case 3:
                    players.StopChannel(entity, chan);
                    break;
                case 4:
                    players.StopAll(entity);
                    break;
                case 5:
                    players.Pause(entity, tl);
                    break;
                case 6:
                    players.Pause(entity, (ushort)(timelineCount + 10));
                    break;
                case 7:
                    players.Resume(entity, tl);
                    break;
                case 8:
                    players.Seek(entity, tl, pastEnd);
                    break;
                case 9:
                    players.Reverse(entity, tl, rng.NextBool());
                    break;
                case 10:
                    players.PauseChannel(entity, chan);
                    break;
                case 11:
                    players.ResumeChannel(entity, chan);
                    break;
                case 12:
                    players.SeekChannel(entity, chan, pastEnd);
                    break;
                case 13:
                    players.ReverseChannel(entity, chan, rng.NextBool());
                    break;
                case 14:
                    players.SetSpeed(entity, tl, (ushort)rng.NextInt(256));
                    break;
                case 15:
                    players.SetChannelSpeed(entity, chan, (ushort)rng.NextInt(256));
                    break;
                case 16:
                    if (rng.NextInt(4) == 0) players.Reset(entity);
                    break;
                default:
                    players.Stop(entity, 0);
                    break;
            }
        }
    }

    [Fact]
    public void RequestFlood_NeverCorruptsNeighborsAndHandles256SlotOverflowOnValidEntities()
    {
        FuzzRunner.Run("T5-RequestFloodAndIsolation", 0x54350001UL, 30, (rng, _) =>
        {
            var entities = 6 + rng.NextInt(24);
            var target = rng.NextInt(entities);
            var timelines = 2 + rng.NextInt(4);
            var channels = 2 + rng.NextInt(3);
            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 80, false, true, false);
            var kindLib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 80, true, true, false, true);

            using (var wA = new TimelineWorld(plainLib.Bytes))
            using (var wB = new TimelineWorld(plainLib.Bytes))
            using (var pA = new PlaybackTable(entities, 4))
            using (var pB = new PlaybackTable(entities, 4))
            using (var cA = new Events<FuzzCue>(4096))
            using (var cB = new Events<FuzzCue>(4096))
            {
                var mA = new PlainMotion[entities];
                var mB = new PlainMotion[entities];
                var sA = new PlainStats[entities];
                var sB = new PlainStats[entities];
                var inp = new PlainInput[entities];
                for (var e = 0; e < entities; e++) inp[e] = new PlainInput { Mult = 1 + rng.NextInt(3) };

                for (var step = 0; step < 25; step++)
                {
                    for (var e = 0; e < entities; e++)
                    {
                        if (e == target) continue;
                        if (rng.NextBool())
                        {
                            var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, plainLib.Durations);
                            op.Entity = e;
                            LibraryGen.ApplyOp(pA, in op);
                            LibraryGen.ApplyOp(pB, in op);
                        }
                    }

                    if (step == 2)
                    {
                        for (var f = 0; f < 270; f++)
                            pA.Start(target, (ushort)(1 + (f % timelines)));
                    }
                    else
                    {
                        IssueRandomValidEntityFlood(ref rng, pA, target, timelines, channels, plainLib.Durations);
                    }

                    wA.ApplyRequests(pA);
                    wB.ApplyRequests(pB);

                    new PlainFuzzSystem { Motion = mA, Stats = sA, Input = inp, Requests = wA.Requests(pA), Cues = cA.Writer }.Run(wA, pA, true);
                    new PlainFuzzSystem { Motion = mB, Stats = sB, Input = inp, Requests = wB.Requests(pB), Cues = cB.Writer }.Run(wB, pB, true);

                    wA.Advance(pA, true);
                    wB.Advance(pB, true);
                    cA.Drain();
                    cB.Drain();

                    Assert.True(pA.CountOf(target) <= PlaybackTable.MaxSlots);
                    for (var e = 0; e < entities; e++)
                    {
                        if (e == target) continue;
                        Assert.Equal(pB.CountOf(e), pA.CountOf(e));
                        Assert.Equal(mB[e].X, mA[e].X);
                        Assert.Equal(sB[e].Starts, sA[e].Starts);
                        Assert.Equal(sB[e].Ends, sA[e].Ends);
                        Assert.Equal(sB[e].Markers, sA[e].Markers);
                    }
                }
            }

            using (var kwA = new TimelineWorld(kindLib.Bytes))
            using (var kwB = new TimelineWorld(kindLib.Bytes))
            using (var kpA = new PlaybackTable(entities, 4))
            using (var kpB = new PlaybackTable(entities, 4))
            using (var ksA = new KindStore(kwA, kpA))
            using (var ksB = new KindStore(kwB, kpB))
            {
                var cA = new KindIntCol[entities];
                var cB = new KindIntCol[entities];
                var eA = new KindEachCol[entities];
                var eB = new KindEachCol[entities];
                var iA = new KindIdleCol[entities];
                var iB = new KindIdleCol[entities];
                var fA = new KindFloatCol[entities];
                var fB = new KindFloatCol[entities];
                var inp = new KindInputCol[entities];
                for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

                for (var step = 0; step < 20; step++)
                {
                    for (var e = 0; e < entities; e++)
                    {
                        if (e == target) continue;
                        if (rng.NextBool())
                        {
                            var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, kindLib.Durations);
                            op.Entity = e;
                            LibraryGen.ApplyOp(kpA, in op);
                            LibraryGen.ApplyOp(kpB, in op);
                        }
                    }

                    if (step == 1)
                    {
                        for (var f = 0; f < 270; f++)
                            kpA.Start(target, (ushort)(1 + (f % timelines)));
                    }
                    else
                    {
                        IssueRandomValidEntityFlood(ref rng, kpA, target, timelines, channels, kindLib.Durations);
                    }

                    kwA.Tick(kpA, true);
                    kwB.Tick(kpB, true);

                    new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(kwA, kpA, true);
                    new KindFuzzSystem { CombineCol = cB, EachCol = eB, IdleCol = iB, FloatCol = fB, Input = inp }.Run(kwB, kpB, true);

                    Assert.True(kpA.CountOf(target) <= PlaybackTable.MaxSlots);
                    for (var e = 0; e < entities; e++)
                    {
                        if (e == target) continue;
                        Assert.Equal(kpB.CountOf(e), kpA.CountOf(e));
                        Assert.Equal(cB[e].Sum, cA[e].Sum);
                        Assert.Equal(eB[e].Sum, eA[e].Sum);
                        Assert.Equal(iB[e].Value, iA[e].Value);
                    }
                }
            }
        });
    }

    [Fact]
    public void RequestFlood_WarmPathAllocatesZeroBytesAfterWarmup()
    {
        FuzzRunner.Run("T5-ZeroWarmAlloc", 0x54350002UL, 6, (rng, _) =>
        {
            const int entities = 32;
            const int timelines = 4;
            const int channels = 3;
            var lib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 60, false, true, true);

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(entities, 16);
            using var cues = new Events<FuzzCue>(4096);
            var motion = new PlainMotion[entities];
            var stats = new PlainStats[entities];
            var input = new PlainInput[entities];
            for (var e = 0; e < entities; e++) input[e] = new PlainInput { Mult = 1 };

            for (var warm = 0; warm < 200; warm++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + warm) % timelines));
                    players.Start(e, tl, (ushort)(warm & 3));
                    players.Seek(e, tl, (ushort)(warm + 100));
                    players.Pause(e, tl);
                    players.Resume(e, tl);
                    players.Reverse(e, tl, (warm & 1) != 0);
                    players.SetSpeed(e, tl, (ushort)(8 + (warm & 3) * 8));
                    players.Stop(e, (ushort)(timelines + 9));
                    if ((warm & 7) == 7) players.Reset(e);
                }
                world.ApplyRequests(players);
                new PlainFuzzSystem { Motion = motion, Stats = stats, Input = input, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players, true);
                world.Advance(players, true);
                cues.Drain();
            }

            var reqs = world.Requests(players);
            var sys = new PlainFuzzSystem { Motion = motion, Stats = stats, Input = input, Requests = reqs, Cues = cues.Writer };
            for (var e = 0; e < entities; e++) players.Reset(e);
            world.ApplyRequests(players);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 32; step++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + step) % timelines));
                    players.Start(e, tl, (ushort)(step & 3));
                    players.Seek(e, tl, (ushort)(step + 100));
                    players.Pause(e, tl);
                    players.Resume(e, tl);
                    players.Reverse(e, tl, (step & 1) != 0);
                    players.SetSpeed(e, tl, (ushort)(8 + (step & 3) * 8));
                    players.Stop(e, (ushort)(timelines + 9));
                    if ((step & 7) == 7) players.Reset(e);
                }
                world.ApplyRequests(players);
                sys.Run(world, players, true);
                world.Advance(players, true);
                cues.Drain();
            }
            var alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0L, alloc);
        });
    }

    [Fact]
    public void RequestFlood_KindStoreAndPulsesWarmPathAllocatesZeroBytesAfterWarmup()
    {
        FuzzRunner.Run("T5-KindZeroWarmAlloc", 0x54350004UL, 6, (rng, _) =>
        {
            const int entities = 32;
            const int timelines = 4;
            const int channels = 3;
            var ov = new Dictionary<string, KindOverride>
            {
                [typeof(KCombineInt).FullName!] = new KindOverride(inline: 16, gamePulses: 8, maxInlineBytes: 64),
                [typeof(KEachInt).FullName!] = new KindOverride(inline: 16, gamePulses: 8, maxInlineBytes: 64),
                [typeof(KIdleInt).FullName!] = new KindOverride(inline: 16, gamePulses: 8, maxInlineBytes: 64),
            };
            var lib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 60, true, true, false, true, ov);

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(entities, 16);
            using var store = new KindStore(world, players);
            var pulses = store.Pulses<int>(typeof(KCombineInt).FullName!);

            var combineCol = new KindIntCol[entities];
            var eachCol = new KindEachCol[entities];
            var idleCol = new KindIdleCol[entities];
            var floatCol = new KindFloatCol[entities];
            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) input[e] = new KindInputCol { Factor = 1 };

            var sys = new KindFuzzSystem { CombineCol = combineCol, EachCol = eachCol, IdleCol = idleCol, FloatCol = floatCol, Input = input };

            for (var warm = 0; warm < 200; warm++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + warm) % timelines));
                    players.Start(e, tl, (ushort)(warm & 3));
                    players.Seek(e, tl, (ushort)(warm + 100));
                    players.Pause(e, tl);
                    players.Resume(e, tl);
                    players.Reverse(e, tl, (warm & 1) != 0);
                    players.SetSpeed(e, tl, (ushort)(8 + (warm & 3) * 8));
                    players.Stop(e, (ushort)(timelines + 9));
                    pulses.To(e, (warm & 3) - 1);
                    if ((warm & 7) == 7) players.Reset(e);
                }
                world.Tick(players, true);
                sys.Run(world, players, true);
            }

            for (var e = 0; e < entities; e++) players.Reset(e);
            world.Tick(players, true);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 32; step++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + step) % timelines));
                    players.Start(e, tl, (ushort)(step & 3));
                    players.Seek(e, tl, (ushort)(step + 100));
                    players.Pause(e, tl);
                    players.Resume(e, tl);
                    players.Reverse(e, tl, (step & 1) != 0);
                    players.SetSpeed(e, tl, (ushort)(8 + (step & 3) * 8));
                    players.Stop(e, (ushort)(timelines + 9));
                    pulses.To(e, (step & 3) - 1);
                    if ((step & 7) == 7) players.Reset(e);
                }
                world.Tick(players, true);
                sys.Run(world, players, true);
            }
            var alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0L, alloc);
        });
    }

    [Fact]
    public void RequestQueue_DistinctEntitySlotKeysGiveIdenticalResultInAnyArrivalOrder()
    {
        FuzzRunner.Run("T5-DistinctKeyPermutation", 0x54350003UL, 40, (rng, _) =>
        {
            var entities = 2 + rng.NextInt(16);
            var timelines = 2 + rng.NextInt(4);
            var channels = 2 + rng.NextInt(3);
            var lib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 80, false, true, false);

            var records = new List<(int Entity, int Slot, ushort Timeline, ushort At, RequestOp Op)>();
            for (var e = 0; e < entities; e++)
            {
                var keys = 1 + rng.NextInt(6);
                for (var s = 0; s < keys; s++)
                {
                    var op = DistinctSafeOps[rng.NextInt(DistinctSafeOps.Length)];
                    var tl = (ushort)(1 + rng.NextInt(timelines));
                    var at = op switch
                    {
                        RequestOp.Start or RequestOp.Seek => (ushort)rng.NextInt((int)lib.Durations[tl - 1]),
                        RequestOp.Reverse => (ushort)(rng.NextBool() ? 1 : 0),
                        RequestOp.Speed => LibraryGen.PickRate(ref rng),
                        _ => (ushort)0,
                    };
                    records.Add((e, s, tl, at, op));
                }
            }

            var permuted = records.ToArray();
            for (var i = permuted.Length - 1; i > 0; i--)
            {
                var j = rng.NextInt(i + 1);
                (permuted[i], permuted[j]) = (permuted[j], permuted[i]);
            }

            using var wA = new TimelineWorld(lib.Bytes);
            using var wB = new TimelineWorld(lib.Bytes);
            using var pA = new PlaybackTable(entities, 8);
            using var pB = new PlaybackTable(entities, 8);
            using var cA = new Events<FuzzCue>(4096);
            using var cB = new Events<FuzzCue>(4096);

            for (var i = 0; i < records.Count; i++)
            {
                var r = records[i];
                RequestQueue.Add(pA.Pending, r.Entity, r.Slot, r.Timeline, r.At, r.Op);
            }
            for (var i = 0; i < permuted.Length; i++)
            {
                var r = permuted[i];
                RequestQueue.Add(pB.Pending, r.Entity, r.Slot, r.Timeline, r.At, r.Op);
            }

            wA.ApplyRequests(pA);
            wB.ApplyRequests(pB);

            var mA = new PlainMotion[entities];
            var mB = new PlainMotion[entities];
            var sA = new PlainStats[entities];
            var sB = new PlainStats[entities];
            var inp = new PlainInput[entities];
            for (var e = 0; e < entities; e++) inp[e] = new PlainInput { Mult = 1 };

            for (var step = 0; step < 8; step++)
            {
                new PlainFuzzSystem { Motion = mA, Stats = sA, Input = inp, Requests = wA.Requests(pA), Cues = cA.Writer }.Run(wA, pA, true);
                new PlainFuzzSystem { Motion = mB, Stats = sB, Input = inp, Requests = wB.Requests(pB), Cues = cB.Writer }.Run(wB, pB, true);
                wA.Advance(pA, true);
                wB.Advance(pB, true);
                cA.Drain();
                cB.Drain();
            }

            Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mA), MemoryMarshal.AsBytes<PlainMotion>(mB), "Motion") is null);
            Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(sA), MemoryMarshal.AsBytes<PlainStats>(sB), "Stats") is null);
            var pbErr = FuzzRunner.ComparePlaybacks(pA, pB);
            Assert.True(pbErr == null, pbErr);
        });
    }
}
