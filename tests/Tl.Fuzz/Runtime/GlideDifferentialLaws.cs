using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class GlideDifferentialLaws
{
    static int PickEntityCount(ref FuzzRandom rng)
    {
        return rng.NextInt(10) switch
        {
            0 => 1 + rng.NextInt(16),
            1 => 63 + rng.NextInt(3),
            2 => 64 * (1 + rng.NextInt(8)),
            3 => 512 + rng.NextInt(512),
            4 => 1500 + rng.NextInt(3501),
            _ => 1 + rng.NextInt(1000),
        };
    }

    static byte PickHorizonLimit(ref FuzzRandom rng)
    {
        return rng.NextInt(5) switch
        {
            0 => 1,
            1 => 2,
            2 => 255,
            _ => (byte)(1 + rng.NextInt(255)),
        };
    }

    static uint[] BuildCumulative(uint[] durations)
    {
        var cum = new uint[durations.Length];
        uint sum = 0;
        for (var i = 0; i < durations.Length; i++)
        {
            sum += durations[i];
            cum[i] = sum;
        }
        return cum;
    }

    static void RemapDurations(ref FuzzRequestOp op, uint[] durations)
    {
        if (op.Timeline == 0 || durations.Length == 0) return;
        op.Timeline = (ushort)(1 + ((op.Timeline - 1) % durations.Length));
        if (op.Op is RequestActionKind.Start or RequestActionKind.Seek or RequestActionKind.SeekChannel)
        {
            var dur = (int)durations[op.Timeline - 1];
            if (op.Arg > dur + 1) op.Arg = (ushort)(op.Arg % (dur + 2));
        }
    }

    [Fact]
    public void HorizonGlide_MatchesPlainRules_OnRandomLibrariesAndScripts()
    {
        FuzzRunner.Run("T1-PlainGlide", 0x54310001UL, 24, (rng, iter) =>
        {
            var entities = PickEntityCount(ref rng);
            var slotsPerEntity = 1 + rng.NextInt(40);
            var limit = PickHorizonLimit(ref rng);
            var timelines = 1 + rng.NextInt(6);
            var channels = 1 + rng.NextInt(4);
            var lib1 = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 300, true, true, true);
            var rebindAt = rng.NextInt(3) == 0 ? 5 + rng.NextInt(40) : -1;
            var lib2 = rebindAt >= 0 ? LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 300, true, true, true) : null;
            var dropViewAt = rng.NextInt(3) == 0 ? 4 + rng.NextInt(45) : -1;
            var steps = 20 + rng.NextInt(80);

            using var world0 = new TimelineWorld(lib1.Bytes);
            using var worldG = new TimelineWorld(lib1.Bytes);
            using var players0 = new PlaybackTable(entities, slotsPerEntity, 0);
            using var playersG = new PlaybackTable(entities, slotsPerEntity, limit);
            using var cues0 = new Events<FuzzCue>(1 << 16);
            using var cuesG = new Events<FuzzCue>(1 << 16);

            var motion0 = new PlainMotion[entities];
            var motionG = new PlainMotion[entities];
            var stats0 = new PlainStats[entities];
            var statsG = new PlainStats[entities];
            var input = new PlainInput[entities];
            for (var e = 0; e < entities; e++)
                input[e] = new PlainInput { Mult = 1 + rng.NextInt(3) };

            var curLib = lib1;
            var initBursts = Math.Min(entities * 2, 1 + rng.NextInt(entities + 32));
            for (var b = 0; b < initBursts; b++)
            {
                var e = rng.NextInt(entities);
                var tl = (ushort)(1 + rng.NextInt(curLib.TimelineCount));
                var at = (ushort)rng.NextInt((int)curLib.Durations[tl - 1]);
                players0.Start(e, tl, at);
                playersG.Start(e, tl, at);
            }

            for (var step = 0; step < steps; step++)
            {
                if (step == rebindAt && lib2 != null)
                {
                    world0.Reload(lib2.Bytes);
                    worldG.Reload(lib2.Bytes);
                    curLib = lib2;
                }
                if (step == dropViewAt)
                    _ = playersG.View;

                var forward = rng.NextInt(5) != 0;
                var opsCount = 1 + rng.NextInt(Math.Min(entities, 64));
                for (var k = 0; k < opsCount; k++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, curLib.TimelineCount, curLib.ChannelCount, curLib.Durations);
                    RemapDurations(ref op, curLib.Durations);
                    LibraryGen.ApplyOp(players0, in op);
                    LibraryGen.ApplyOp(playersG, in op);
                }

                world0.ApplyRequests(players0);
                worldG.ApplyRequests(playersG);

                var sys0 = new PlainFuzzSystem
                {
                    Motion = motion0,
                    Stats = stats0,
                    Input = input,
                    Requests = world0.Requests(players0),
                    Cues = cues0.Writer,
                };
                var sysG = new PlainFuzzSystem
                {
                    Motion = motionG,
                    Stats = statsG,
                    Input = input,
                    Requests = worldG.Requests(playersG),
                    Cues = cuesG.Writer,
                };

                sys0.Run(world0, players0, forward);
                sysG.Run(worldG, playersG, forward);

                world0.Advance(players0, forward);
                worldG.Advance(playersG, forward);

                var err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(motion0), MemoryMarshal.AsBytes<PlainMotion>(motionG), $"step {step} PlainMotion");
                Assert.True(err == null, err);

                err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(stats0), MemoryMarshal.AsBytes<PlainStats>(statsG), $"step {step} PlainStats");
                Assert.True(err == null, err);

                var ev0 = cues0.Drain();
                var evG = cuesG.Drain();
                err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes(ev0), MemoryMarshal.AsBytes(evG), $"step {step} FuzzCue events");
                Assert.True(err == null, err);
                Assert.Equal(cues0.Dropped, cuesG.Dropped);

                err = FuzzRunner.ComparePlaybacks(players0, playersG);
                Assert.True(err == null, $"step {step}: {err}");

                var h0 = TlStateHash.Begin();
                fixed (PlainMotion* pm0 = motion0) h0.Bytes((byte*)pm0, motion0.Length * sizeof(PlainMotion));
                fixed (PlainStats* ps0 = stats0) h0.Bytes((byte*)ps0, stats0.Length * sizeof(PlainStats));
                FuzzRunner.MixPlaybacks(ref h0, players0);

                var hG = TlStateHash.Begin();
                fixed (PlainMotion* pmG = motionG) hG.Bytes((byte*)pmG, motionG.Length * sizeof(PlainMotion));
                fixed (PlainStats* psG = statsG) hG.Bytes((byte*)psG, statsG.Length * sizeof(PlainStats));
                FuzzRunner.MixPlaybacks(ref hG, playersG);

                Assert.Equal(h0.Value, hG.Value);
            }
        });
    }

    [Fact]
    public void LazyAdvance_MatchesEagerRules_WithoutEverLeavingTheLazyPath()
    {
        var skipped = 0L;
        var found = 0;
        FuzzRunner.Run("T1-LazyAdvance", 0x54310004UL, 96, (rng, iter) =>
        {
            var entities = rng.NextInt(3) == 0 ? 1 + rng.NextInt(6) : PickEntityCount(ref rng);
            var slotsPerEntity = 1 + rng.NextInt(6);
            var timelines = 1 + rng.NextInt(6);
            var channels = 1 + rng.NextInt(12);
            var lib1 = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 300, true, true, true);
            var lib2 = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 300, true, true, true);
            var reloadAt = rng.NextInt(3) == 0 ? 5 + rng.NextInt(60) : -1;
            var steps = 60 + rng.NextInt(600);
            var busy = 1 + rng.NextInt(12);

            using var world0 = new TimelineWorld(lib1.Bytes);
            using var worldL = new TimelineWorld(lib1.Bytes);
            using var guest = new TimelineWorld(lib1.Bytes);
            using var players0 = new PlaybackTable(entities, slotsPerEntity, 0);
            using var playersL = new PlaybackTable(entities, slotsPerEntity, PickHorizonLimit(ref rng));

            var curLib = lib1;
            var forward = true;
            for (var step = 0; step < steps; step++)
            {
                if (step == reloadAt)
                {
                    world0.Reload(lib2.Bytes);
                    worldL.Reload(lib2.Bytes);
                    guest.Reload(lib2.Bytes);
                    curLib = lib2;
                }
                if (rng.NextInt(24) == 0) forward = !forward;
                if (step == 0 || rng.NextInt(busy) == 0)
                {
                    var opsCount = 1 + rng.NextInt(Math.Min(entities, 64));
                    for (var k = 0; k < opsCount; k++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, curLib.TimelineCount, curLib.ChannelCount, curLib.Durations);
                        RemapDurations(ref op, curLib.Durations);
                        LibraryGen.ApplyOp(players0, in op);
                        LibraryGen.ApplyOp(playersL, in op);
                    }
                    if (rng.NextInt(12) == 0)
                    {
                        var target = rng.NextInt(entities);
                        var unknown = (ushort)(curLib.TimelineCount + 1 + rng.NextInt(9));
                        players0.Start(target, unknown);
                        playersL.Start(target, unknown);
                        Assert.Throws<ArgumentException>(() => world0.ApplyRequests(players0));
                        Assert.Throws<ArgumentException>(() => worldL.ApplyRequests(playersL));
                    }
                    else
                    {
                        world0.ApplyRequests(players0);
                        worldL.ApplyRequests(playersL);
                    }
                }

                world0.Advance(players0, forward);
                (rng.NextInt(40) == 0 ? guest : worldL).Advance(playersL, forward);
                Assert.True(playersL.Frozen, $"step {step}: the lazy table thawed");

                var err = FuzzRunner.ComparePlaybacks(players0, playersL);
                Assert.True(err == null, $"step {step}: {err}");

                var probe = rng.NextInt(entities);
                var channel = (byte)(1 + rng.NextInt(curLib.ChannelCount));
                var found0 = players0.Find(probe, channel, out var slot0);
                var foundL = playersL.Find(probe, channel, out var slotL);
                Assert.Equal(found0, foundL);
                Assert.Equal(*(ulong*)&slot0, *(ulong*)&slotL);
                if (foundL) found++;

                for (var e = 0; e < entities; e++)
                    if ((playersL.PlayingBits[e >> 6] >> (e & 63) & 1) != 0 && (ushort)(playersL.Next[e] - playersL.Clock) > 1) skipped++;
            }
            Assert.NotEqual(0, playersL.HorizonLimit);
        });
        Assert.True(skipped > 100000, $"lazy tables skipped only {skipped} entity ticks");
        Assert.True(found > 100, $"Find decoded only {found} lazy slots");
    }

    [Fact]
    public void HorizonGlide_MatchesKindRules_OnKindStoreAndCalmOracle()
    {
        FuzzRunner.Run("T1-KindGlide", 0x54310002UL, 24, (rng, _) =>
        {
            var entities = Math.Min(1500, PickEntityCount(ref rng));
            var slotsPerEntity = 1 + rng.NextInt(24);
            var limit = PickHorizonLimit(ref rng);
            var timelines = 1 + rng.NextInt(6);
            var channels = 1 + rng.NextInt(4);
            var lib1 = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 200, true, true, true, true);
            var rebindAt = rng.NextInt(3) == 0 ? 5 + rng.NextInt(30) : -1;
            var lib2 = rebindAt >= 0 ? LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 200, true, true, true, true) : null;
            var steps = 20 + rng.NextInt(60);

            using var world0 = new TimelineWorld(lib1.Bytes);
            using var worldG = new TimelineWorld(lib1.Bytes);
            using var players0 = new PlaybackTable(entities, slotsPerEntity, 0);
            using var playersG = new PlaybackTable(entities, slotsPerEntity, limit);
            using var store0 = new KindStore(world0, players0);
            using var storeG = new KindStore(worldG, playersG);

            var combine0 = new KindIntCol[entities];
            var combineG = new KindIntCol[entities];
            var each0 = new KindEachCol[entities];
            var eachG = new KindEachCol[entities];
            var idle0 = new KindIdleCol[entities];
            var idleG = new KindIdleCol[entities];
            var flt0 = new KindFloatCol[entities];
            var fltG = new KindFloatCol[entities];
            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++)
                input[e] = new KindInputCol { Factor = 1 + rng.NextInt(3) };

            var curLib = lib1;
            for (var step = 0; step < steps; step++)
            {
                if (step == rebindAt && lib2 != null)
                {
                    world0.Reload(lib2.Bytes);
                    worldG.Reload(lib2.Bytes);
                    curLib = lib2;
                }

                var forward = rng.NextInt(5) != 0;
                var opsCount = 1 + rng.NextInt(Math.Min(entities, 48));
                for (var k = 0; k < opsCount; k++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, curLib.TimelineCount, curLib.ChannelCount, curLib.Durations);
                    RemapDurations(ref op, curLib.Durations);
                    LibraryGen.ApplyOp(players0, in op);
                    LibraryGen.ApplyOp(playersG, in op);
                }

                var pulses0 = store0.Pulses<int>("Tl.Fuzz.RuntimeDomain.KCombineInt");
                var pulsesG = storeG.Pulses<int>("Tl.Fuzz.RuntimeDomain.KCombineInt");
                var pCount = rng.NextInt(12);
                for (var p = 0; p < pCount; p++)
                {
                    var e = rng.NextInt(entities);
                    var v = rng.NextInt(31) - 15;
                    pulses0.To(e, v);
                    pulsesG.To(e, v);
                }

                world0.Tick(players0, forward);
                worldG.Tick(playersG, forward);

                new KindFuzzSystem { CombineCol = combine0, EachCol = each0, IdleCol = idle0, FloatCol = flt0, Input = input }.Run(world0, players0, forward);
                new KindFuzzSystem { CombineCol = combineG, EachCol = eachG, IdleCol = idleG, FloatCol = fltG, Input = input }.Run(worldG, playersG, forward);

                var err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(combine0), MemoryMarshal.AsBytes<KindIntCol>(combineG), $"step {step} KindIntCol");
                Assert.True(err == null, err);
                err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(each0), MemoryMarshal.AsBytes<KindEachCol>(eachG), $"step {step} KindEachCol");
                Assert.True(err == null, err);
                err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(idle0), MemoryMarshal.AsBytes<KindIdleCol>(idleG), $"step {step} KindIdleCol");
                Assert.True(err == null, err);
                err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(flt0), MemoryMarshal.AsBytes<KindFloatCol>(fltG), $"step {step} KindFloatCol");
                Assert.True(err == null, err);
                Assert.Equal(store0.DroppedPulses, storeG.DroppedPulses);

                err = FuzzRunner.ComparePlaybacks(players0, playersG);
                Assert.True(err == null, $"step {step}: {err}");
            }
        });
    }

    struct OracleKindRows : IKindRows
    {
        public int Events;

        public KindEntry* Append(int row, LibraryFeeder* feeder)
        {
            Events++;
            return null;
        }

        public void RemoveSpan(int row, int kind, byte slot, ushort timeline, ushort step) => Events++;
        public void Purge(int row, byte slot, ushort timeline) => Events++;
        public Playback* Reserve(int row, int used, int length)
        {
            Events++;
            return null;
        }
        public void Sampled(int row, int samples) { }
        public void Seal() { }
    }

    [Fact]
    public void HorizonOfKindRun_MatchesMultiSlotKindRulesAndVaryingLimits()
    {
        FuzzRunner.Run("T1-OfKindRunOracle", 0x54310003UL, 40, (rng, _) =>
        {
            var timelines = 1 + rng.NextInt(6);
            var channels = 1 + rng.NextInt(4);
            var lib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 180, true, true, false, true);
            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(1);
            using var store = new KindStore(world, players);
            var tables = (KindTables)typeof(KindStore).GetField("_t", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(store)!;
            var calmLen = Math.Max(1, KindCalm.Length(in tables));
            var fwd = (byte*)NativeMemory.AllocZeroed((nuint)calmLen);
            var bwd = (byte*)NativeMemory.AllocZeroed((nuint)calmLen);
            var start = (int*)NativeMemory.AllocZeroed((nuint)Math.Max(1, tables.TimelineCount) * 4);
            var slots = (Playback*)NativeMemory.AllocZeroed(48 * (nuint)sizeof(Playback));
            var expected = (Playback*)NativeMemory.AllocZeroed(48 * (nuint)sizeof(Playback));
            try
            {
                var calm = KindCalm.Build(in tables, fwd, bwd, start);
                var roundSlots = 1 + rng.NextInt(40);
                for (var r = 0; r < 64; r++)
                {
                    for (var i = 0; i < roundSlots; i++)
                    {
                        var empty = rng.NextInt(6) == 0;
                        if (empty)
                        {
                            slots[i] = default;
                        }
                        else
                        {
                            var tl = (ushort)(1 + rng.NextInt(tables.TimelineCount));
                            var dur = (int)tables.Timelines[tl - 1].Duration;
                            var tick = (ushort)rng.NextInt(Math.Max(1, dur));
                            var flags = (PlaybackFlags)rng.NextInt(256);
                            var rate = rng.NextInt(3) == 0 ? (byte)0 : rng.NextBool() ? (byte)16 : (byte)(1 + rng.NextInt(64));
                            slots[i] = new Playback
                            {
                                Timeline = tl,
                                Tick = tick,
                                Channel = (byte)(i + 1),
                                Flags = flags,
                                Rate = rate,
                                Phase = (byte)(rate == 0 ? 0 : rng.NextInt(16)),
                            };
                        }
                        expected[i] = slots[i];
                    }

                    var limit = 1 + rng.NextInt(255);
                    var stepDir = rng.NextBool() ? 1 : -1;
                    var worldRev = SlotWord.WorldReverse(stepDir > 0);
                    var horizon = Horizon.OfKindRun(slots, roundSlots, in tables, in calm, worldRev, limit);
                    var rows = new OracleKindRows();
                    for (var step = 0; step < horizon; step++)
                    {
                        var kept = KindRules.TickEntity(in tables, expected, roundSlots, ref rows, 0, stepDir);
                        GlideKernel.RunScalar((ulong*)slots, roundSlots, worldRev);
                        Assert.True(kept);
                        Assert.Equal(0, rows.Events);
                        for (var s = 0; s < roundSlots; s++)
                            Assert.Equal(((ulong*)expected)[s], ((ulong*)slots)[s]);
                    }
                }
            }
            finally
            {
                NativeMemory.Free(fwd);
                NativeMemory.Free(bwd);
                NativeMemory.Free(start);
                NativeMemory.Free(slots);
                NativeMemory.Free(expected);
            }
        });
    }
}
