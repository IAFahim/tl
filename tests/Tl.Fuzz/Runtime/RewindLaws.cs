using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class RewindLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(RewindLaws).Assembly]);

    static GeneratedLibrary BuildReversibleLibrary(
        ref FuzzRandom rng,
        int timelineCount,
        int channelCount,
        bool includePulses,
        bool includeFloat,
        bool includeControlFlags)
    {
        var names = new string[timelineCount];
        var channels = new string[channelCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"rev_{i + 1}";
        for (var i = 0; i < channelCount; i++) channels[i] = $"rch_{i + 1}";

        var manifest = LibraryBake.Manifest(names, channels, 60);
        var typed = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(24 + rng.NextInt(120));
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = channelCount > 0 ? channels[rng.NextInt(channelCount)] : null,
            };

            if (includeControlFlags)
            {
                var mode = rng.NextInt(4);
                if (mode == 1) tl.Loops = true;
                else if (mode == 2) tl.Hold = true;
                else if (mode == 3) tl.Next = names[rng.NextInt(timelineCount)];
            }

            var ctr = new TypedTrack
            {
                Name = "kc",
                Type = typeof(KCombineTrack),
                Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(3) }),
            };
            var etr = new TypedTrack
            {
                Name = "ke",
                Type = typeof(KEachTrack),
                Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(3) }),
            };
            var ftr = includeFloat
                ? new TypedTrack { Name = "kf", Type = typeof(KFloatTrack), Data = LibraryGen.BytesOf(new KFloatTrack { Scale = 0.5f }) }
                : null;

            uint cursor = 2;
            var clips = 1 + rng.NextInt(4);
            for (var c = 0; c < clips && cursor + 3 < dur; c++)
            {
                var s = cursor + (uint)rng.NextInt(3);
                if (s + 2 >= dur) break;
                var e = Math.Min(dur - 2, s + 2 + (uint)rng.NextInt((int)Math.Min(16, dur - s - 2)));
                var sp = includePulses ? rng.NextInt(7) - 3 : 0;
                var ep = includePulses ? rng.NextInt(7) - 3 : 0;

                ctr.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KCombineClip { Delta = rng.NextInt(13) - 6, StartPulse = sp, EndPulse = ep }),
                });
                etr.Clips.Add(new TypedClip
                {
                    Type = typeof(KEachClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KEachClip { Delta = rng.NextInt(9) - 4, StartPulse = sp, EndPulse = ep }),
                });
                if (ftr != null)
                {
                    ftr.Clips.Add(new TypedClip
                    {
                        Type = typeof(KFloatClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KFloatClip { Delta = (rng.NextInt(9) - 4) * 0.1f }),
                    });
                }
                cursor = e + 1;
            }

            tl.Tracks.Add(ctr);
            tl.Tracks.Add(etr);
            if (ftr != null) tl.Tracks.Add(ftr);

            if (includePulses && rng.NextBool())
            {
                tl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = 3 + (uint)rng.NextInt((int)dur - 6),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = rng.NextInt(11) - 5 }),
                });
            }

            typed[i] = tl;
        }

        return LibraryGen.AssembleTyped(typed, channels, manifest, AllKinds);
    }

    static GeneratedLibrary BuildPlainRewindLibrary(ref FuzzRandom rng, int timelineCount, int channelCount, bool loops)
    {
        var names = new string[timelineCount];
        var channels = new string[channelCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"prev_{i + 1}";
        for (var i = 0; i < channelCount; i++) channels[i] = $"pch_{i + 1}";
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var typed = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(24 + rng.NextInt(100));
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Loops = loops,
                Channel = channels[rng.NextInt(channelCount)],
            };
            var tr = new TypedTrack
            {
                Name = "plain",
                Type = typeof(FuzzPlainTrack),
                Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 + rng.NextInt(4) }),
            };
            uint cursor = 2;
            var clips = 1 + rng.NextInt(4);
            for (var c = 0; c < clips && cursor + 3 < dur; c++)
            {
                var s = cursor + (uint)rng.NextInt(3);
                if (s + 2 >= dur) break;
                var e = Math.Min(dur - 2, s + 2 + (uint)rng.NextInt((int)Math.Min(16, dur - s - 2)));
                tr.Clips.Add(new TypedClip
                {
                    Type = typeof(FuzzPlainClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = rng.NextInt(11) - 5, Tag = rng.NextInt(16), TriggerTimeline = 0 }),
                });
                cursor = e + 1;
            }
            tl.Tracks.Add(tr);
            typed[i] = tl;
        }

        return LibraryGen.AssembleTyped(typed, channels, manifest, null);
    }

    [Fact]
    public void TiedToTicks_RewindingExactCrossedTicksRestoresIntegerColumnsOnKindAndPlain()
    {
        FuzzRunner.Run("T3-ExactRewindTicks", 0x54330001UL, 40, (rng, _) =>
        {
            var entities = 1 + rng.NextInt(32);
            var timelines = 1 + rng.NextInt(4);
            var channels = 1 + rng.NextInt(3);
            var kindLib = BuildReversibleLibrary(ref rng, timelines, channels, includePulses: false, includeFloat: false, includeControlFlags: false);
            var plainLib = BuildPlainRewindLibrary(ref rng, timelines, channels, loops: false);

            var startTicks = new ushort[entities];
            var tls = new ushort[entities];
            var pStartTicks = new ushort[entities];
            var pTls = new ushort[entities];
            var rates = new ushort[entities];
            var minSafeSteps = 100;

            for (var e = 0; e < entities; e++)
            {
                var tl = (ushort)(1 + rng.NextInt(timelines));
                var dur = (int)kindLib.Durations[tl - 1];
                var st = (ushort)(1 + rng.NextInt(Math.Max(1, dur / 3)));
                var avail = Math.Max(1, (dur - 2 - st) / 2);
                minSafeSteps = Math.Min(minSafeSteps, avail);
                tls[e] = tl;
                startTicks[e] = st;

                var ptl = (ushort)(1 + rng.NextInt(timelines));
                var pdur = (int)plainLib.Durations[ptl - 1];
                var pst = (ushort)(1 + rng.NextInt(Math.Max(1, pdur / 3)));
                var pavail = Math.Max(1, (pdur - 2 - pst) / 2);
                minSafeSteps = Math.Min(minSafeSteps, pavail);
                pTls[e] = ptl;
                pStartTicks[e] = pst;

                rates[e] = rng.NextBool() ? (ushort)16 : (ushort)32;
            }

            var n = 1 + rng.NextInt(Math.Max(1, Math.Min(12, minSafeSteps / 2)));

            using (var world = new TimelineWorld(kindLib.Bytes))
            using (var players = new PlaybackTable(entities, 4))
            using (var store = new KindStore(world, players))
            {
                var combine = new KindIntCol[entities];
                var each = new KindEachCol[entities];
                var idle = new KindIdleCol[entities];
                var flt = new KindFloatCol[entities];
                var input = new KindInputCol[entities];
                for (var e = 0; e < entities; e++)
                {
                    input[e] = new KindInputCol { Factor = 1 + rng.NextInt(3) };
                    players.Start(e, tls[e], startTicks[e]);
                    players.SetSpeed(e, tls[e], rates[e]);
                }

                for (var s = 0; s < n; s++)
                {
                    world.Tick(players, true);
                    new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, true);
                }

                for (var e = 0; e < entities; e++)
                {
                    var endTick = players.SlotsOf(e)[0].Tick;
                    players.Seek(e, tls[e], endTick);
                }

                for (var s = 0; s < n; s++)
                {
                    world.Tick(players, false);
                    new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, false);
                }

                for (var e = 0; e < entities; e++)
                {
                    if (combine[e].Sum != 0 || each[e].Sum != 0 || players.SlotsOf(e)[0].Tick != startTicks[e])
                        throw new InvalidOperationException($"Kind rewind mismatch entity={e} n={n} rate={rates[e]} tl={tls[e]} dur={kindLib.Durations[tls[e]-1]} startTick={startTicks[e]} endTick={players.SlotsOf(e)[0].Tick} flags={players.SlotsOf(e)[0].Flags} combine={combine[e].Sum} each={each[e].Sum}");
                }
            }

            using (var pWorld = new TimelineWorld(plainLib.Bytes))
            using (var pPlayers = new PlaybackTable(entities, 4))
            using (var cues = new Events<FuzzCue>(4096))
            {
                var motion = new PlainMotion[entities];
                var stats = new PlainStats[entities];
                var input = new PlainInput[entities];
                var totalSteps = Math.Max(2, n);
                for (var e = 0; e < entities; e++)
                {
                    input[e] = new PlainInput { Mult = 1 + rng.NextInt(3) };
                    pPlayers.Start(e, pTls[e], pStartTicks[e]);
                }

                for (var s = 0; s < totalSteps; s++)
                {
                    pWorld.ApplyRequests(pPlayers);
                    pWorld.Advance(pPlayers, true);
                    new PlainFuzzSystem { Motion = motion, Stats = stats, Input = input, Requests = pWorld.Requests(pPlayers), Cues = cues.Writer }.Run(pWorld, pPlayers, true);
                    cues.Drain();
                }

                for (var s = 0; s < totalSteps; s++)
                {
                    pWorld.ApplyRequests(pPlayers);
                    new PlainFuzzSystem { Motion = motion, Stats = stats, Input = input, Requests = pWorld.Requests(pPlayers), Cues = cues.Writer }.Run(pWorld, pPlayers, false);
                    pWorld.Advance(pPlayers, false);
                    cues.Drain();
                }

                for (var e = 0; e < entities; e++)
                {
                    if (motion[e].X != 0 || pPlayers.SlotsOf(e)[0].Tick != pStartTicks[e])
                        throw new InvalidOperationException($"Plain symmetric rewind mismatch entity={e} start={pStartTicks[e]} end={pPlayers.SlotsOf(e)[0].Tick} motion={motion[e].X}");
                }
            }
        });
    }

    [Fact]
    public void HeldPlaybacksStayParkedAndPausedDoNotMoveOnWorldRewind()
    {
        FuzzRunner.Run("T3-RewindBoundaries", 0x54330002UL, 30, (rng, _) =>
        {
            var dur = 6 + rng.NextInt(20);
            var names = new[] { "held_tl", "loop_tl" };
            var ch = new[] { "main" };
            var manifest = LibraryBake.Manifest(names, ch, 60);

            var heldTl = new TypedTimeline { Name = "held_tl", Duration = (uint)dur, Hold = true, Channel = "main" };
            var ctr1 = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            ctr1.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = 1,
                End = (uint)dur,
                Data = LibraryGen.BytesOf(new KCombineClip { Delta = 3, StartPulse = 0, EndPulse = 0 }),
            });
            heldTl.Tracks.Add(ctr1);

            var loopTl = new TypedTimeline { Name = "loop_tl", Duration = (uint)dur, Loops = true, Channel = "main" };
            var ctr2 = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            ctr2.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = 0,
                End = (uint)dur,
                Data = LibraryGen.BytesOf(new KCombineClip { Delta = 2, StartPulse = 0, EndPulse = 0 }),
            });
            loopTl.Tracks.Add(ctr2);

            var lib = LibraryGen.AssembleTyped([heldTl, loopTl], ch, manifest, AllKinds);

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(3, 4);
            using var store = new KindStore(world, players);
            var combine = new KindIntCol[3];
            var each = new KindEachCol[3];
            var idle = new KindIdleCol[3];
            var flt = new KindFloatCol[3];
            var input = new[] { new KindInputCol { Factor = 1 }, new KindInputCol { Factor = 1 }, new KindInputCol { Factor = 1 } };

            players.Start(0, 1);
            players.Start(1, 2);
            players.Start(2, 2);

            var stepsForward = dur + 2 + rng.NextInt(10);
            for (var s = 0; s < stepsForward; s++)
            {
                if (s == 2) players.Pause(2, 2);
                world.Tick(players, true);
                new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, true);
            }

            var heldEndVal = combine[0].Sum;
            var heldTickAfterFwd = players.SlotsOf(0)[0].Tick;
            var pausedTick = players.SlotsOf(2)[0].Tick;
            var pausedVal = combine[2].Sum;
            Assert.NotEqual(0, (int)(players.SlotsOf(0)[0].Flags & PlaybackFlags.Ended));

            var backSteps = 3 + rng.NextInt(10);
            for (var s = 0; s < backSteps; s++)
            {
                world.Tick(players, false);
                new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, false);

                Assert.Equal(heldTickAfterFwd, players.SlotsOf(0)[0].Tick);
                Assert.NotEqual(0, (int)(players.SlotsOf(0)[0].Flags & PlaybackFlags.Ended));
                Assert.Equal(heldEndVal, combine[0].Sum);

                Assert.Equal(pausedTick, players.SlotsOf(2)[0].Tick);
                Assert.Equal(pausedVal, combine[2].Sum);
            }

            Assert.True(players.IsPlaying(1, 2));
        });
    }

    [Fact]
    public void PulseStartAndEndReverseSignSymmetricallyAcrossCrossedClipBoundaries()
    {
        FuzzRunner.Run("T3-PulseRewindLaw", 0x54330003UL, 30, (rng, _) =>
        {
            var dur = 30u;
            var names = new[] { "pulse_tl" };
            var ch = new[] { "main" };
            var manifest = LibraryBake.Manifest(names, ch, 60);
            var tl = new TypedTimeline { Name = "pulse_tl", Duration = dur, Channel = "main" };

            var s1 = 4u + (uint)rng.NextInt(5);
            var e1 = s1 + 4u + (uint)rng.NextInt(6);
            var ctr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            var sp = 1 + rng.NextInt(11);
            var ep = 1 + rng.NextInt(11);
            ctr.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = s1,
                End = e1,
                Data = LibraryGen.BytesOf(new KCombineClip { Delta = 2 + rng.NextInt(5), StartPulse = sp, EndPulse = ep }),
            });
            tl.Tracks.Add(ctr);

            var lib = LibraryGen.AssembleTyped([tl], ch, manifest, AllKinds);

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(1, 4);
            using var store = new KindStore(world, players);
            var combine = new KindIntCol[1];
            var each = new KindEachCol[1];
            var idle = new KindIdleCol[1];
            var flt = new KindFloatCol[1];
            var input = new[] { new KindInputCol { Factor = 1 } };

            players.Start(0, 1, 1);
            var n = (int)(e1 + 2);
            for (var s = 0; s < n; s++)
            {
                world.Tick(players, true);
                new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, true);
            }

            var endTick = players.SlotsOf(0)[0].Tick;
            players.Seek(0, 1, (ushort)(endTick + 1));
            for (var s = 0; s < n; s++)
            {
                world.Tick(players, false);
                new KindFuzzSystem { CombineCol = combine, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input }.Run(world, players, false);
            }

            Assert.Equal(0, combine[0].Sum);
        });
    }
}
