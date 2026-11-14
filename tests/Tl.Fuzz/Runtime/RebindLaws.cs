using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class RebindLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    static (GeneratedLibrary Base, GeneratedLibrary Modified, uint OldDur, uint NewDur) BuildSingleTimelineModifiedPair(
        ref FuzzRandom rng,
        int timelineCount,
        int channelCount,
        int targetIndex,
        bool forcePulseFreeTriggers)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"rb_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"rch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var baseList = new TypedTimeline[timelineCount];
        var modList = new TypedTimeline[timelineCount];
        var unchangedPool = Enumerable.Range(0, timelineCount).Where(i => i != targetIndex).ToArray();

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(6 + rng.NextInt(40));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            string? next = null;
            if (mode == 3 && i != targetIndex && unchangedPool.Length > 0)
                next = names[unchangedPool[rng.NextInt(unchangedPool.Length)]];

            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = ch,
                Loops = mode == 1,
                Hold = mode == 2,
                Next = next,
            };

            var ctr = new TypedTrack
            {
                Name = "kc",
                Type = typeof(KCombineTrack),
                Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(3) }),
            };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
            {
                ctr.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KCombineClip
                    {
                        Delta = 1 + rng.NextInt(7),
                        StartPulse = 100 + rng.NextInt(20),
                        EndPulse = 200 + rng.NextInt(20),
                    }),
                });
            }
            tl.Tracks.Add(ctr);

            var etr = new TypedTrack
            {
                Name = "ke",
                Type = typeof(KEachTrack),
                Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(2) }),
            };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
            {
                etr.Clips.Add(new TypedClip
                {
                    Type = typeof(KEachClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KEachClip
                    {
                        Delta = 1 + rng.NextInt(5),
                        StartPulse = 50 + rng.NextInt(10),
                        EndPulse = 80 + rng.NextInt(10),
                    }),
                });
            }
            tl.Tracks.Add(etr);

            var itr = new TypedTrack
            {
                Name = "ki",
                Type = typeof(KIdleTrack),
                Data = LibraryGen.BytesOf(new KIdleTrack { Bias = i + 1 }),
            };
            itr.Clips.Add(new TypedClip
            {
                Type = typeof(KIdleClip),
                Start = 0,
                End = dur,
                Data = LibraryGen.BytesOf(new KIdleClip { Code = 300 + i }),
            });
            tl.Tracks.Add(itr);

            if (!forcePulseFreeTriggers && rng.NextBool())
            {
                tl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = (uint)rng.NextInt((int)dur),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 500 + rng.NextInt(30) }),
                });
            }
            else if (forcePulseFreeTriggers)
            {
                tl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = (uint)rng.NextInt((int)dur),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 500 + rng.NextInt(30) }),
                });
            }

            baseList[i] = tl;
            modList[i] = tl;
        }

        var oldTl = baseList[targetIndex];
        var oldDur = oldTl.Duration;
        var newDur = rng.NextBool()
            ? (uint)Math.Max(2, (int)oldDur / 2)
            : oldDur + (uint)(4 + rng.NextInt(20));

        var modMode = rng.NextInt(3);
        var modTl = new TypedTimeline
        {
            Name = oldTl.Name,
            Duration = newDur,
            Channel = oldTl.Channel,
            Loops = modMode == 1,
            Hold = modMode == 2,
            Next = null,
        };
        var modCtr = new TypedTrack
        {
            Name = "kc",
            Type = typeof(KCombineTrack),
            Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 2 + rng.NextInt(4) }),
        };
        modCtr.Clips.Add(new TypedClip
        {
            Type = typeof(KCombineClip),
            Start = 0,
            End = newDur,
            Data = LibraryGen.BytesOf(new KCombineClip { Delta = 13, StartPulse = 777, EndPulse = 888 }),
        });
        modTl.Tracks.Add(modCtr);

        var modItr = new TypedTrack
        {
            Name = "ki",
            Type = typeof(KIdleTrack),
            Data = LibraryGen.BytesOf(new KIdleTrack { Bias = 99 }),
        };
        modItr.Clips.Add(new TypedClip
        {
            Type = typeof(KIdleClip),
            Start = 0,
            End = newDur,
            Data = LibraryGen.BytesOf(new KIdleClip { Code = 901 }),
        });
        modTl.Tracks.Add(modItr);
        modList[targetIndex] = modTl;

        var baseLib = LibraryGen.AssembleTyped(baseList, channels, manifest, AllKinds);
        var modLib = LibraryGen.AssembleTyped(modList, channels, manifest, AllKinds);
        return (baseLib, modLib, oldDur, newDur);
    }

    [Fact]
    public void Reload_SameBytesMidRun_PreservesPlaybackStateAndReplaysZeroPulses()
    {
        FuzzRunner.Run("T8-SameBytesRebind", 0x54380001UL, 30, (rng, _) =>
        {
            const int entities = 16;
            const int timelines = 4;
            const int channels = 2;

            var names = new string[timelines];
            for (var i = 0; i < timelines; i++) names[i] = $"sb_{i + 1:D2}";
            var chanNames = new[] { "c1", "c2" };
            var manifest = LibraryBake.Manifest(names, chanNames, 60);
            var typed = new TypedTimeline[timelines];
            var spanOnlyTyped = new TypedTimeline[timelines];
            var durations = new uint[timelines];

            for (var i = 0; i < timelines; i++)
            {
                var dur = (uint)(8 + rng.NextInt(30));
                durations[i] = dur;
                var mode = rng.NextInt(3);
                var ch = i < 2 ? chanNames[i] : null;
                var tl = new TypedTimeline
                {
                    Name = names[i],
                    Duration = dur,
                    Channel = ch,
                    Loops = mode == 1,
                    Hold = mode == 2,
                };
                var stl = new TypedTimeline
                {
                    Name = names[i],
                    Duration = dur,
                    Channel = ch,
                    Loops = mode == 1,
                    Hold = mode == 2,
                };
                var scale = 1 + rng.NextInt(3);
                var ctr = new TypedTrack
                {
                    Name = "kc",
                    Type = typeof(KCombineTrack),
                    Data = LibraryGen.BytesOf(new KCombineTrack { Scale = scale }),
                };
                var sctr = new TypedTrack
                {
                    Name = "kc",
                    Type = typeof(KCombineTrack),
                    Data = LibraryGen.BytesOf(new KCombineTrack { Scale = scale }),
                };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var delta = 1 + rng.NextInt(5);
                    ctr.Clips.Add(new TypedClip
                    {
                        Type = typeof(KCombineClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KCombineClip
                        {
                            Delta = delta,
                            StartPulse = 1000 + rng.NextInt(100),
                            EndPulse = 2000 + rng.NextInt(100),
                        }),
                    });
                    sctr.Clips.Add(new TypedClip
                    {
                        Type = typeof(KCombineClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KCombineClip
                        {
                            Delta = delta,
                            StartPulse = 0,
                            EndPulse = 0,
                        }),
                    });
                }
                tl.Tracks.Add(ctr);
                stl.Tracks.Add(sctr);

                var etr = new TypedTrack
                {
                    Name = "ke",
                    Type = typeof(KEachTrack),
                    Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }),
                };
                var setr = new TypedTrack
                {
                    Name = "ke",
                    Type = typeof(KEachTrack),
                    Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }),
                };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var delta = 1 + rng.NextInt(4);
                    etr.Clips.Add(new TypedClip
                    {
                        Type = typeof(KEachClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KEachClip
                        {
                            Delta = delta,
                            StartPulse = 3000 + rng.NextInt(50),
                            EndPulse = 4000 + rng.NextInt(50),
                        }),
                    });
                    setr.Clips.Add(new TypedClip
                    {
                        Type = typeof(KEachClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KEachClip
                        {
                            Delta = delta,
                            StartPulse = 0,
                            EndPulse = 0,
                        }),
                    });
                }
                tl.Tracks.Add(etr);
                stl.Tracks.Add(setr);

                for (uint tick = 0; tick < dur; tick += 3)
                {
                    tl.Markers.Add(new TypedMarker
                    {
                        Type = typeof(KPulseMarker),
                        Tick = tick,
                        Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 5000 + (int)tick }),
                    });
                }
                typed[i] = tl;
                spanOnlyTyped[i] = stl;
            }

            var kindLib = LibraryGen.AssembleTyped(typed, chanNames, manifest, AllKinds);
            var spanOnlyLib = LibraryGen.AssembleTyped(spanOnlyTyped, chanNames, manifest, AllKinds);

            using (var world = new TimelineWorld(kindLib.Bytes))
            using (var spanWorld = new TimelineWorld(spanOnlyLib.Bytes))
            using (var players = new PlaybackTable(entities, 8))
            using (var spanPlayers = new PlaybackTable(entities, 8))
            using (var store = new KindStore(world, players))
            using (var spanStore = new KindStore(spanWorld, spanPlayers))
            {
                var cCol = new KindIntCol[entities];
                var eCol = new KindEachCol[entities];
                var iCol = new KindIdleCol[entities];
                var fCol = new KindFloatCol[entities];
                var scCol = new KindIntCol[entities];
                var seCol = new KindEachCol[entities];
                var siCol = new KindIdleCol[entities];
                var sfCol = new KindFloatCol[entities];
                var inp = new KindInputCol[entities];
                for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 + (e & 1) };
                var sys = new KindFuzzSystem { CombineCol = cCol, EachCol = eCol, IdleCol = iCol, FloatCol = fCol, Input = inp };
                var ssys = new KindFuzzSystem { CombineCol = scCol, EachCol = seCol, IdleCol = siCol, FloatCol = sfCol, Input = inp };

                var warmup = 2 + rng.NextInt(8);
                for (var step = 0; step < warmup; step++)
                {
                    for (var r = 0; r < entities * 2; r++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, durations);
                        LibraryGen.ApplyOp(players, in op);
                        LibraryGen.ApplyOp(spanPlayers, in op);
                    }
                    world.Tick(players, true);
                    spanWorld.Tick(spanPlayers, true);
                    sys.Run(world, players, true);
                    ssys.Run(spanWorld, spanPlayers, true);
                }

                var snapCounts = new int[entities];
                var snapSlots = new Playback[entities][];
                for (var e = 0; e < entities; e++)
                {
                    snapCounts[e] = players.CountOf(e);
                    snapSlots[e] = new Playback[snapCounts[e]];
                    for (var s = 0; s < snapCounts[e]; s++)
                        snapSlots[e][s] = players.SlotsOf(e)[s];
                }

                Array.Clear(cCol);
                Array.Clear(eCol);
                Array.Clear(scCol);
                Array.Clear(seCol);

                world.Reload(kindLib.Bytes);
                spanWorld.Reload(spanOnlyLib.Bytes);
                world.Tick(players, true);
                spanWorld.Tick(spanPlayers, true);
                sys.Run(world, players, true);
                ssys.Run(spanWorld, spanPlayers, true);

                Assert.True(FuzzRunner.ComparePlaybacks(players, spanPlayers) is null);

                for (var e = 0; e < entities; e++)
                {
                    var countNow = players.CountOf(e);
                    var slotsNow = players.SlotsOf(e);
                    var hasFastSlot = false;
                    for (var s = 0; s < Math.Min(snapCounts[e], countNow); s++)
                    {
                        var before = snapSlots[e][s];
                        if (before.Timeline == 0 || (before.Flags & PlaybackFlags.Stopping) != 0) continue;
                        var after = slotsNow[s];
                        if ((before.Flags & (PlaybackFlags.Ended | PlaybackFlags.Paused | PlaybackFlags.Stopping)) == 0
                            && (before.Rate > 16 || (before.Flags & PlaybackFlags.StartPending) != 0))
                        {
                            hasFastSlot = true;
                            continue;
                        }
                        Assert.Equal(before.Timeline, after.Timeline);
                        Assert.Equal(before.Channel, after.Channel);
                        Assert.Equal(before.Rate, after.Rate);
                        const PlaybackFlags preserved = PlaybackFlags.Paused | PlaybackFlags.Ended | PlaybackFlags.Reverse;
                        Assert.Equal(before.Flags & preserved, after.Flags & preserved);
                        if (before.Rate == 0 || before.Rate == 16 || (before.Flags & (PlaybackFlags.Paused | PlaybackFlags.Ended)) != 0)
                            Assert.Equal(before.Tick, after.Tick);
                    }

                    if (!hasFastSlot)
                    {
                        Assert.Equal(scCol[e].Sum, cCol[e].Sum);
                        Assert.Equal(seCol[e].Sum, eCol[e].Sum);
                    }
                }
            }

            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 36, includeTriggers: false, includeMarkers: true, includeBatch: false);
            using var wBase = new TimelineWorld(plainLib.Bytes);
            using var wGlide = new TimelineWorld(plainLib.Bytes);
            using var pBase = new PlaybackTable(entities, 8, horizonLimit: 0);
            using var pGlide = new PlaybackTable(entities, 8, horizonLimit: (byte)(1 + rng.NextInt(64)));
            using var cBase = new Events<FuzzCue>(4096);
            using var cGlide = new Events<FuzzCue>(4096);

            var mBase = new PlainMotion[entities];
            var mGlide = new PlainMotion[entities];
            var sBase = new PlainStats[entities];
            var sGlide = new PlainStats[entities];
            var pInp = new PlainInput[entities];
            for (var e = 0; e < entities; e++) pInp[e] = new PlainInput { Mult = 1 };

            var plainWarmup = 2 + rng.NextInt(8);
            for (var step = 0; step <= plainWarmup + 6; step++)
            {
                if (step == plainWarmup)
                {
                    wBase.Reload(plainLib.Bytes);
                    wGlide.Reload(plainLib.Bytes);
                }
                else
                {
                    for (var r = 0; r < entities * 2; r++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, plainLib.Durations);
                        LibraryGen.ApplyOp(pBase, in op);
                        LibraryGen.ApplyOp(pGlide, in op);
                    }
                }

                var preStarts = new int[entities];
                var preEnds = new int[entities];
                var preMarkers = new int[entities];
                var entityMayFireOnReload = new bool[entities];
                var anyMayFireOnReload = false;
                if (step == plainWarmup)
                {
                    for (var e = 0; e < entities; e++)
                    {
                        preStarts[e] = sBase[e].Starts;
                        preEnds[e] = sBase[e].Ends;
                        preMarkers[e] = sBase[e].Markers;
                        var sc = pBase.CountOf(e);
                        var ss = pBase.SlotsOf(e);
                        for (var si = 0; si < sc; si++)
                        {
                            var sl = ss[si];
                            if (sl.Timeline == 0 || (sl.Flags & (PlaybackFlags.Ended | PlaybackFlags.Paused | PlaybackFlags.Stopping)) != 0)
                                continue;
                            if (sl.Rate > 16 || (sl.Flags & PlaybackFlags.StartPending) != 0)
                            {
                                entityMayFireOnReload[e] = true;
                                anyMayFireOnReload = true;
                                break;
                            }
                        }
                    }
                }

                wBase.ApplyRequests(pBase);
                wGlide.ApplyRequests(pGlide);

                new PlainFuzzSystem { Motion = mBase, Stats = sBase, Input = pInp, Requests = wBase.Requests(pBase), Cues = cBase.Writer }.Run(wBase, pBase, true);
                new PlainFuzzSystem { Motion = mGlide, Stats = sGlide, Input = pInp, Requests = wGlide.Requests(pGlide), Cues = cGlide.Writer }.Run(wGlide, pGlide, true);

                wBase.Advance(pBase, true);
                wGlide.Advance(pGlide, true);

                var evBase = cBase.Drain();
                var evGlide = cGlide.Drain();

                if (step == plainWarmup)
                {
                    for (var e = 0; e < entities; e++)
                    {
                        if (!entityMayFireOnReload[e])
                        {
                            Assert.Equal(preStarts[e], sBase[e].Starts);
                            Assert.Equal(preEnds[e], sBase[e].Ends);
                            Assert.Equal(preMarkers[e], sBase[e].Markers);
                        }
                    }
                    if (!anyMayFireOnReload)
                    {
                        for (var ev = 0; ev < evBase.Length; ev++)
                            Assert.True(evBase[ev].Value.Tag < 1000);
                    }
                }

                Assert.True(FuzzRunner.ComparePlaybacks(pBase, pGlide) is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mBase), MemoryMarshal.AsBytes<PlainMotion>(mGlide), "PlainMotion") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(sBase), MemoryMarshal.AsBytes<PlainStats>(sGlide), "PlainStats") is null);
                Assert.Equal(evBase.Length, evGlide.Length);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes(evBase), MemoryMarshal.AsBytes(evGlide), "PlainEvents") is null);
            }
        });
    }

    [Fact]
    public void Reload_OneTimelineChanged_OnlyChangedTimelinePlaybacksAndEntitiesChange()
    {
        FuzzRunner.Run("T8-OneTimelineChanged", 0x54380002UL, 30, (rng, _) =>
        {
            const int entities = 24;
            const int split = 12;
            const int timelines = 5;
            const int channels = 2;
            var targetIdx = rng.NextInt(timelines);
            var targetTl = (ushort)(targetIdx + 1);

            var (baseLib, modLib, oldDur, newDur) = BuildSingleTimelineModifiedPair(ref rng, timelines, channels, targetIdx, false);
            var unchangedTls = Enumerable.Range(1, timelines).Where(t => t != targetTl).Select(t => (ushort)t).ToArray();

            using var wCtrl = new TimelineWorld(baseLib.Bytes);
            using var wMod = new TimelineWorld(baseLib.Bytes);
            using var pCtrl = new PlaybackTable(entities, 8);
            using var pMod = new PlaybackTable(entities, 8);
            using var ksCtrl = new KindStore(wCtrl, pCtrl);
            using var ksMod = new KindStore(wMod, pMod);

            var cCtrl = new KindIntCol[entities];
            var cMod = new KindIntCol[entities];
            var eCtrl = new KindEachCol[entities];
            var eMod = new KindEachCol[entities];
            var iCtrl = new KindIdleCol[entities];
            var iMod = new KindIdleCol[entities];
            var fCtrl = new KindFloatCol[entities];
            var fMod = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

            var preSteps = 2 + rng.NextInt(10);
            for (var step = 0; step < preSteps + 12; step++)
            {
                if (step == preSteps)
                {
                    wCtrl.Reload(baseLib.Bytes);
                    wMod.Reload(modLib.Bytes);
                }

                for (var e = 0; e < entities; e++)
                {
                    if (rng.NextInt(3) == 0) continue;
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, baseLib.Durations);
                    op.Entity = e;
                    if (e < split)
                    {
                        op.Timeline = unchangedTls[rng.NextInt(unchangedTls.Length)];
                        var d = baseLib.Durations[op.Timeline - 1];
                        if (op.Op is RequestActionKind.Start or RequestActionKind.Seek or RequestActionKind.SeekChannel)
                            op.Arg = (ushort)rng.NextInt((int)d + 2);
                    }
                    else if (step < preSteps && rng.NextBool())
                    {
                        op.Timeline = targetTl;
                        if (op.Op is RequestActionKind.Start or RequestActionKind.Seek)
                            op.Arg = (ushort)rng.NextInt((int)oldDur + 1);
                    }
                    else if (step >= preSteps)
                    {
                        var durArr = op.Timeline == targetTl ? modLib.Durations : baseLib.Durations;
                        if (op.Op is RequestActionKind.Start or RequestActionKind.Seek or RequestActionKind.SeekChannel)
                            op.Arg = (ushort)rng.NextInt((int)durArr[op.Timeline - 1] + 2);
                    }
                    LibraryGen.ApplyOp(pCtrl, in op);
                    LibraryGen.ApplyOp(pMod, in op);
                }

                var forward = step % 6 != 4;
                wCtrl.Tick(pCtrl, forward);
                wMod.Tick(pMod, forward);

                new KindFuzzSystem { CombineCol = cCtrl, EachCol = eCtrl, IdleCol = iCtrl, FloatCol = fCtrl, Input = inp }.Run(wCtrl, pCtrl, forward);
                new KindFuzzSystem { CombineCol = cMod, EachCol = eMod, IdleCol = iMod, FloatCol = fMod, Input = inp }.Run(wMod, pMod, forward);

                for (var e = 0; e < split; e++)
                {
                    var cntA = pCtrl.CountOf(e);
                    var cntB = pMod.CountOf(e);
                    Assert.Equal(cntA, cntB);
                    var sa = pCtrl.SlotsOf(e);
                    var sb = pMod.SlotsOf(e);
                    for (var s = 0; s < cntA; s++)
                    {
                        Assert.Equal(sa[s].Timeline, sb[s].Timeline);
                        Assert.Equal(sa[s].Channel, sb[s].Channel);
                        Assert.Equal(sa[s].Tick, sb[s].Tick);
                        Assert.Equal(sa[s].Phase, sb[s].Phase);
                        Assert.Equal(sa[s].Rate, sb[s].Rate);
                        Assert.Equal(sa[s].Flags, sb[s].Flags);
                    }
                    Assert.Equal(cCtrl[e].Sum, cMod[e].Sum);
                    Assert.Equal(eCtrl[e].Sum, eMod[e].Sum);
                    Assert.Equal(iCtrl[e].Value, iMod[e].Value);
                }

                for (var e = split; e < entities; e++)
                {
                    var cnt = pMod.CountOf(e);
                    var sm = pMod.SlotsOf(e);
                    for (var s = 0; s < cnt; s++)
                    {
                        if (sm[s].Timeline == targetTl)
                            Assert.True(sm[s].Tick < (step >= preSteps ? newDur : oldDur));
                    }
                }
            }
        });
    }

    [Fact]
    public void Reload_PausedEndedAndPendingRequests_KeepValidBoundsAndHorizonParity()
    {
        FuzzRunner.Run("T8-RebindClampingAndHorizon", 0x54380003UL, 24, (rng, _) =>
        {
            const int entities = 16;
            const int timelines = 4;
            const int channels = 2;
            var targetIdx = rng.NextInt(timelines);
            var targetTl = (ushort)(targetIdx + 1);

            var (baseLib, modLib, oldDur, newDur) = BuildSingleTimelineModifiedPair(ref rng, timelines, channels, targetIdx, true);

            using var wPlain0 = new TimelineWorld(baseLib.Bytes);
            using var wPlainH = new TimelineWorld(baseLib.Bytes);
            using var pPlain0 = new PlaybackTable(entities, 8, horizonLimit: 0);
            using var pPlainH = new PlaybackTable(entities, 8, horizonLimit: (byte)(1 + rng.NextInt(40)));
            using var c0 = new Events<FuzzCue>(4096);
            using var ch = new Events<FuzzCue>(4096);

            var m0 = new PlainMotion[entities];
            var mh = new PlainMotion[entities];
            var s0 = new PlainStats[entities];
            var sh = new PlainStats[entities];
            var inp = new PlainInput[entities];
            for (var e = 0; e < entities; e++) inp[e] = new PlainInput { Mult = 1 };

            for (var e = 0; e < entities; e++)
            {
                var startTick = (ushort)Math.Min((int)oldDur - 1, Math.Max(0, (int)oldDur - 1 - (e % 4)));
                pPlain0.Start(e, targetTl, startTick);
                pPlainH.Start(e, targetTl, startTick);
                pPlain0.SetSpeed(e, targetTl, (ushort)(8 + (e & 3) * 8));
                pPlainH.SetSpeed(e, targetTl, (ushort)(8 + (e & 3) * 8));
                if ((e & 1) == 1)
                {
                    pPlain0.Pause(e, targetTl);
                    pPlainH.Pause(e, targetTl);
                }
                if ((e & 2) == 2)
                {
                    pPlain0.Reverse(e, targetTl, true);
                    pPlainH.Reverse(e, targetTl, true);
                }
            }

            for (var step = 0; step < 12; step++)
            {
                if (step == 3)
                {
                    pPlain0.Start(0, targetTl, 0);
                    pPlainH.Start(0, targetTl, 0);
                    wPlain0.Reload(modLib.Bytes);
                    wPlainH.Reload(modLib.Bytes);
                }
                else if (step == 7)
                {
                    wPlain0.Reload(baseLib.Bytes);
                    wPlainH.Reload(baseLib.Bytes);
                }

                var forward = step % 4 != 2;
                wPlain0.ApplyRequests(pPlain0);
                wPlainH.ApplyRequests(pPlainH);

                new PlainFuzzSystem { Motion = m0, Stats = s0, Input = inp, Requests = wPlain0.Requests(pPlain0), Cues = c0.Writer }.Run(wPlain0, pPlain0, forward);
                new PlainFuzzSystem { Motion = mh, Stats = sh, Input = inp, Requests = wPlainH.Requests(pPlainH), Cues = ch.Writer }.Run(wPlainH, pPlainH, forward);

                wPlain0.Advance(pPlain0, forward);
                wPlainH.Advance(pPlainH, forward);

                var ev0 = c0.Drain();
                var evH = ch.Drain();

                Assert.True(FuzzRunner.ComparePlaybacks(pPlain0, pPlainH) is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(m0), MemoryMarshal.AsBytes<PlainMotion>(mh), "Rebind Motion") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(s0), MemoryMarshal.AsBytes<PlainStats>(sh), "Rebind Stats") is null);
                Assert.Equal(ev0.Length, evH.Length);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes(ev0), MemoryMarshal.AsBytes(evH), "Rebind Events") is null);

                var curDur = step >= 3 && step < 7 ? newDur : oldDur;
                for (var e = 0; e < entities; e++)
                {
                    var cnt = pPlain0.CountOf(e);
                    var sl = pPlain0.SlotsOf(e);
                    for (var s = 0; s < cnt; s++)
                        if (sl[s].Timeline == targetTl)
                            Assert.True(sl[s].Tick < curDur);
                }
            }
        });
    }
}
