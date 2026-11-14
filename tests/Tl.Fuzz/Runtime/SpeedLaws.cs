using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class SpeedLaws
{
    static readonly ushort[] TestRates = [0, 1, 2, 4, 5, 8, 12, 16, 20, 24, 32, 40, 48, 64, 96, 128, 255];

    static int EffectiveRate(ushort rate) => rate == 0 ? 16 : Math.Min(255, (int)rate);

    [Fact]
    public void SpeedRate_MatchesExactCrossedTicksOfOneXRunAcrossKindsAndPulses()
    {
        FuzzRunner.Run("T4-SpeedDifferential", 0x54340001UL, 40, (rng, _) =>
        {
            var timelines = 1 + rng.NextInt(5);
            var channels = 1 + rng.NextInt(3);
            var lib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 120, includeEach: true, includeIdle: false, includeFloat: false, includeMarkers: true);
            var initTl = (ushort)(1 + rng.NextInt(timelines));
            var initAt = (ushort)rng.NextInt((int)lib.Durations[initTl - 1]);
            var rate = TestRates[rng.NextInt(TestRates.Length)];
            if (rng.NextBool()) rate = (ushort)(1 + rng.NextInt(255));
            var steps = 10 + rng.NextInt(40);

            using var worldS = new TimelineWorld(lib.Bytes);
            using var world1 = new TimelineWorld(lib.Bytes);
            using var playersS = new PlaybackTable(1, 4);
            using var players1 = new PlaybackTable(1, 4);
            using var storeS = new KindStore(worldS, playersS);
            using var store1 = new KindStore(world1, players1);

            var combS = new KindIntCol[1];
            var comb1 = new KindIntCol[1];
            var eachS = new KindEachCol[1];
            var each1 = new KindEachCol[1];
            var idleS = new KindIdleCol[1];
            var idle1 = new KindIdleCol[1];
            var fltS = new KindFloatCol[1];
            var flt1 = new KindFloatCol[1];
            var input = new[] { new KindInputCol { Factor = 1 + rng.NextInt(3) } };

            playersS.Start(0, initTl, initAt);
            playersS.SetSpeed(0, initTl, rate);
            players1.Start(0, initTl, initAt);

            var gamePulseS = storeS.Pulses<int>("Tl.Fuzz.RuntimeDomain.KCombineInt");
            var gamePulse1 = store1.Pulses<int>("Tl.Fuzz.RuntimeDomain.KCombineInt");

            var trace = new List<string>();
            for (var s = 0; s < steps; s++)
            {
                var forward = rng.NextInt(5) != 0;
                var verbLog = "none";

                if (s > 0 && rng.NextInt(8) == 0)
                {
                    var verb = rng.NextInt(5);
                    if (verb == 0)
                    {
                        verbLog = "Pause";
                        for (ushort t = 1; t <= timelines; t++)
                        {
                            playersS.Pause(0, t);
                            players1.Pause(0, t);
                        }
                    }
                    else if (verb == 1)
                    {
                        verbLog = "Resume";
                        for (ushort t = 1; t <= timelines; t++)
                        {
                            playersS.Resume(0, t);
                            players1.Resume(0, t);
                        }
                    }
                    else if (verb == 2)
                    {
                        var rev = rng.NextBool();
                        verbLog = $"Reverse({rev})";
                        for (ushort t = 1; t <= timelines; t++)
                        {
                            playersS.Reverse(0, t, rev);
                            players1.Reverse(0, t, rev);
                        }
                    }
                    else if (verb == 3)
                    {
                        rate = TestRates[rng.NextInt(TestRates.Length)];
                        verbLog = $"SetSpeed({rate})";
                        for (ushort t = 1; t <= timelines; t++)
                            playersS.SetSpeed(0, t, rate);
                    }
                    else if (verb == 4)
                    {
                        var seekTick = (ushort)rng.NextInt(64);
                        verbLog = $"Seek({seekTick})";
                        for (ushort t = 1; t <= timelines; t++)
                        {
                            playersS.Seek(0, t, seekTick);
                            players1.Seek(0, t, seekTick);
                        }
                    }
                }

                worldS.ApplyRequests(playersS);
                world1.ApplyRequests(players1);
                var beforeS = playersS.CountOf(0) > 0 ? playersS.SlotsOf(0)[0] : default;
                var before1 = players1.CountOf(0) > 0 ? players1.SlotsOf(0)[0] : default;
                var moves = beforeS.Timeline != 0 && PlaybackRules.Moves(beforeS.Flags, forward);
                var crossings = moves ? PlaybackRules.Crossings(in beforeS) : 1;

                if (moves && crossings > 0 && rng.NextBool())
                {
                    var gp = rng.NextInt(21) - 10;
                    gamePulseS.To(0, gp);
                    gamePulse1.To(0, gp);
                }

                worldS.Tick(playersS, forward);
                new KindFuzzSystem { CombineCol = combS, EachCol = eachS, IdleCol = idleS, FloatCol = fltS, Input = input }.Run(worldS, playersS, forward);

                for (var sub = 0; sub < crossings; sub++)
                {
                    world1.Tick(players1, forward);
                    new KindFuzzSystem { CombineCol = comb1, EachCol = each1, IdleCol = idle1, FloatCol = flt1, Input = input }.Run(world1, players1, forward);
                }

                var afterS = playersS.CountOf(0) > 0 ? playersS.SlotsOf(0)[0] : default;
                var after1 = players1.CountOf(0) > 0 ? players1.SlotsOf(0)[0] : default;
                trace.Add($"s={s} fwd={forward} verb={verbLog} moves={moves} cross={crossings} bS=({beforeS.Timeline},{beforeS.Tick},{beforeS.Phase},{beforeS.Rate},{beforeS.Flags}) b1=({before1.Timeline},{before1.Tick},{before1.Flags}) -> aS=({afterS.Timeline},{afterS.Tick},{afterS.Phase},{afterS.Flags}) a1=({after1.Timeline},{after1.Tick},{after1.Flags}) c=({comb1[0].Sum},{combS[0].Sum})");

                Assert.Equal(comb1[0].Sum, combS[0].Sum);
                Assert.Equal(each1[0].Sum, eachS[0].Sum);
                Assert.Equal(players1.CountOf(0), playersS.CountOf(0));
                for (var sl = 0; sl < playersS.CountOf(0); sl++)
                {
                    var a = players1.SlotsOf(0)[sl];
                    var b = playersS.SlotsOf(0)[sl];
                    if (a.Timeline != b.Timeline || a.Tick != b.Tick || a.Channel != b.Channel || a.Flags != b.Flags)
                        throw new InvalidOperationException(string.Join("\n", trace));
                }
            }
        });
    }

    [Fact]
    public void ZeroAndSixteenRatesBothRepresent1xSpeedAndMixedRatesCoexistDeterministically()
    {
        FuzzRunner.Run("T4-ZeroSixteenAndMultiChannel", 0x54340002UL, 30, (rng, _) =>
        {
            var timelines = 2 + rng.NextInt(4);
            var channels = 2 + rng.NextInt(3);
            var lib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 100, true, false, false, true);
            var entities = 8 + rng.NextInt(32);

            using var w0 = new TimelineWorld(lib.Bytes);
            using var w16 = new TimelineWorld(lib.Bytes);
            using var p0 = new PlaybackTable(entities, 8);
            using var p16 = new PlaybackTable(entities, 8);
            using var s0 = new KindStore(w0, p0);
            using var s16 = new KindStore(w16, p16);

            var c0 = new KindIntCol[entities];
            var c16 = new KindIntCol[entities];
            var e0 = new KindEachCol[entities];
            var e16 = new KindEachCol[entities];
            var i0 = new KindIdleCol[entities];
            var i16 = new KindIdleCol[entities];
            var f0 = new KindFloatCol[entities];
            var f16 = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

            for (var step = 0; step < 30; step++)
            {
                var opsCount = rng.NextInt(entities);
                for (var k = 0; k < opsCount; k++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, lib.Durations);
                    LibraryGen.ApplyOp(p0, in op);
                    if (op.Op is RequestActionKind.SetSpeed or RequestActionKind.SetChannelSpeed)
                        op.Arg = op.Arg == 0 ? (ushort)16 : op.Arg == 16 ? (ushort)0 : op.Arg;
                    LibraryGen.ApplyOp(p16, in op);
                }

                var forward = rng.NextInt(5) != 0;
                w0.Tick(p0, forward);
                w16.Tick(p16, forward);
                new KindFuzzSystem { CombineCol = c0, EachCol = e0, IdleCol = i0, FloatCol = f0, Input = inp }.Run(w0, p0, forward);
                new KindFuzzSystem { CombineCol = c16, EachCol = e16, IdleCol = i16, FloatCol = f16, Input = inp }.Run(w16, p16, forward);

                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(c0), MemoryMarshal.AsBytes<KindIntCol>(c16), $"step {step} Combine") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(e0), MemoryMarshal.AsBytes<KindEachCol>(e16), $"step {step} Each") is null);
                Assert.Equal(p0.EntityCapacity, p16.EntityCapacity);
            }
        });
    }
}
