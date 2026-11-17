using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed class GroupStressLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    static readonly int[][] Permutations =
    [
        [0, 1, 2],
        [0, 2, 1],
        [1, 0, 2],
        [1, 2, 0],
        [2, 0, 1],
        [2, 1, 0],
    ];

    static readonly string[] SplitKindNames =
    [
        typeof(B3SplitKindA).FullName!,
        typeof(B3SplitKindB).FullName!,
        typeof(B3SplitKindC).FullName!,
    ];

    static readonly string[] CombKindNames =
    [
        typeof(B3CombKindA).FullName!,
        typeof(B3CombKindB).FullName!,
        typeof(B3CombKindC).FullName!,
    ];

    static readonly string[] UngrpKindNames =
    [
        typeof(B3UngrpKindA).FullName!,
        typeof(B3UngrpKindB).FullName!,
        typeof(B3UngrpKindC).FullName!,
    ];

    static (GeneratedLibrary Split, GeneratedLibrary Comb, GeneratedLibrary Ungrp) BuildVariantTriplet(
        ref FuzzRandom rng,
        int[] timelineMasks,
        int channelCount,
        int maxDuration)
    {
        var seed = ((ulong)rng.NextUInt() << 32) | rng.NextUInt();
        return (
            BuildSingleVariantLibrary(new FuzzRandom(seed), timelineMasks, channelCount, maxDuration, 0),
            BuildSingleVariantLibrary(new FuzzRandom(seed), timelineMasks, channelCount, maxDuration, 1),
            BuildSingleVariantLibrary(new FuzzRandom(seed), timelineMasks, channelCount, maxDuration, 2));
    }

    static GeneratedLibrary BuildSingleVariantLibrary(
        FuzzRandom rng,
        int[] timelineMasks,
        int channelCount,
        int maxDuration,
        int variant)
    {
        var timelineCount = timelineMasks.Length;
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"b3_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"b3ch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var (trA, clA, trB, clB, trC, clC) = variant switch
        {
            0 => (typeof(B3SplitTrackA), typeof(B3SplitClipA), typeof(B3SplitTrackB), typeof(B3SplitClipB), typeof(B3SplitTrackC), typeof(B3SplitClipC)),
            1 => (typeof(B3CombTrackA), typeof(B3CombClipA), typeof(B3CombTrackB), typeof(B3CombClipB), typeof(B3CombTrackC), typeof(B3CombClipC)),
            _ => (typeof(B3UngrpTrackA), typeof(B3UngrpClipA), typeof(B3UngrpTrackB), typeof(B3UngrpClipB), typeof(B3UngrpTrackC), typeof(B3UngrpClipC)),
        };

        var timelines = new TypedTimeline[timelineCount];
        for (var i = 0; i < timelineCount; i++)
        {
            var mask = timelineMasks[i];
            var dur = (uint)(4 + rng.NextInt(maxDuration));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            string? next = null;
            if (mode == 3)
            {
                var candidates = new List<int>();
                for (var j = 0; j < timelineCount; j++)
                    if ((timelineMasks[j] & ~mask) == 0)
                        candidates.Add(j);
                if (candidates.Count > 0)
                    next = names[candidates[rng.NextInt(candidates.Count)]];
            }

            var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };

            if ((mask & 1) != 0)
            {
                var tr = new TypedTrack { Name = "ta", Type = trA, Data = new byte[1] };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var sv = rng.NextInt(15) - 7;
                    var pv = rng.NextInt(21) - 10;
                    tr.Clips.Add(new TypedClip { Type = clA, Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipA { SpanVal = sv, PulseVal = pv }) });
                }
                tl.Tracks.Add(tr);
            }

            if ((mask & 2) != 0)
            {
                var tr = new TypedTrack { Name = "tb", Type = trB, Data = new byte[1] };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var sv = rng.NextInt(15) - 7;
                    var pv = rng.NextInt(21) - 10;
                    tr.Clips.Add(new TypedClip { Type = clB, Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipB { SpanVal = sv, PulseVal = pv }) });
                }
                tl.Tracks.Add(tr);
            }

            if ((mask & 4) != 0)
            {
                var tr = new TypedTrack { Name = "tc", Type = trC, Data = new byte[1] };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var pv = rng.NextInt(21) - 10;
                    tr.Clips.Add(new TypedClip { Type = clC, Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipC { PulseVal = pv }) });
                }
                tl.Tracks.Add(tr);
            }

            timelines[i] = tl;
        }

        return LibraryGen.AssembleTyped(timelines, channels, manifest, AllKinds);
    }

    static bool TryReachableRequest(
        ref FuzzRandom rng,
        int entities,
        int[] timelineMasks,
        ulong[] reach,
        int channels,
        uint[] durations,
        out FuzzRequestOp op)
    {
        op = LibraryGen.NextRequest(ref rng, entities, timelineMasks.Length, channels, durations);
        if (op.Op == RequestActionKind.Start)
        {
            var eMask = reach[op.Entity];
            var allowed = new List<int>();
            for (var i = 0; i < timelineMasks.Length; i++)
                if (((ulong)timelineMasks[i] & ~eMask) == 0)
                    allowed.Add(i + 1);
            if (allowed.Count == 0)
                return false;
            op.Timeline = (ushort)allowed[rng.NextInt(allowed.Count)];
            var d = (int)durations[op.Timeline - 1];
            op.Arg = (ushort)rng.NextInt(Math.Max(1, d + 2));
        }
        return true;
    }

    static void RunGroupScenario(string target, ulong defaultSeed, int baseCases, bool allowDoubleSpanRun)
    {
        FuzzRunner.Run(target, defaultSeed, baseCases, (rng, iter) =>
        {
            var timelines = 3 + rng.NextInt(4);
            var timelineMasks = new int[timelines];
            timelineMasks[0] = 1;
            timelineMasks[1] = 2;
            timelineMasks[2] = 7;
            for (var i = 3; i < timelines; i++)
                timelineMasks[i] = 1 + rng.NextInt(7);

            var channels = 1 + rng.NextInt(2);
            var entities = 8 + rng.NextInt(16);
            var slots = 2 + rng.NextInt(6);
            var steps = 25 + rng.NextInt(30);
            var rebindStep = 8 + rng.NextInt(10);

            var lib1 = BuildVariantTriplet(ref rng, timelineMasks, channels, 18);
            var lib2 = BuildVariantTriplet(ref rng, timelineMasks, channels, 18);

            var usePartialReach = rng.NextBool();
            var reach = new ulong[entities];
            for (var e = 0; e < entities; e++)
                reach[e] = usePartialReach ? (ulong)(rng.NextInt(8)) : 7UL;

            using var wS = new TimelineWorld(lib1.Split.Bytes);
            using var pS = new PlaybackTable(entities, slots);
            using var kS = usePartialReach ? new KindStore(wS, pS, SplitKindNames, reach) : new KindStore(wS, pS);

            using var wC = new TimelineWorld(lib1.Comb.Bytes);
            using var pC = new PlaybackTable(entities, slots);
            using var kC = usePartialReach ? new KindStore(wC, pC, CombKindNames, reach) : new KindStore(wC, pC);

            using var wU = new TimelineWorld(lib1.Ungrp.Bytes);
            using var pU = new PlaybackTable(entities, slots);
            using var kU = usePartialReach ? new KindStore(wU, pU, UngrpKindNames, reach) : new KindStore(wU, pU);

            var qSA = kS.Pulses<int>(SplitKindNames[0]);
            var qSB = kS.Pulses<int, IntAddFold>(SplitKindNames[1]);
            var qSC = kS.Pulses<int>(SplitKindNames[2]);
            var qCA = kC.Pulses<int>(CombKindNames[0]);
            var qCB = kC.Pulses<int, IntAddFold>(CombKindNames[1]);
            var qCC = kC.Pulses<int>(CombKindNames[2]);
            var qUA = kU.Pulses<int>(UngrpKindNames[0]);
            var qUB = kU.Pulses<int, IntAddFold>(UngrpKindNames[1]);
            var qUC = kU.Pulses<int>(UngrpKindNames[2]);

            var sA = new B3SplitOutA[entities];
            var sB = new B3SplitOutB[entities];
            var sC = new B3SplitOutC[entities];
            var cA = new B3CombOutA[entities];
            var cB = new B3CombOutB[entities];
            var cC = new B3CombOutC[entities];
            var uA = new B3UngrpOutA[entities];
            var uB = new B3UngrpOutB[entities];
            var uC = new B3UngrpOutC[entities];

            var activeDurations = lib1.Split.Durations;
            for (var step = 0; step < steps; step++)
            {
                if (step == rebindStep)
                {
                    var sameBytes = rng.NextBool();
                    var next = sameBytes ? lib1 : lib2;
                    activeDurations = next.Split.Durations;
                    wS.Reload(next.Split.Bytes);
                    wC.Reload(next.Comb.Bytes);
                    wU.Reload(next.Ungrp.Bytes);
                }

                var opCount = rng.NextInt(entities + 2);
                for (var k = 0; k < opCount; k++)
                {
                    if (!TryReachableRequest(ref rng, entities, timelineMasks, reach, channels, activeDurations, out var op))
                        continue;
                    LibraryGen.ApplyOp(pS, in op);
                    LibraryGen.ApplyOp(pC, in op);
                    LibraryGen.ApplyOp(pU, in op);
                }

                for (var e = 0; e < entities; e++)
                {
                    var vA = rng.NextInt(31) - 15;
                    var vB = rng.NextInt(31) - 15;
                    var vC = rng.NextInt(31) - 15;
                    qSA.To(e, vA); qSB.To(e, vB); qSC.To(e, vC);
                    qCA.To(e, vA); qCB.To(e, vB); qCC.To(e, vC);
                    qUA.To(e, vA); qUB.To(e, vB); qUC.To(e, vC);
                    if (rng.NextBool())
                    {
                        var vA2 = rng.NextInt(19) - 9;
                        var vB2 = rng.NextInt(19) - 9;
                        var vC2 = rng.NextInt(19) - 9;
                        qSA.To(e, vA2); qSB.To(e, vB2); qSC.To(e, vC2);
                        qCA.To(e, vA2); qCB.To(e, vB2); qCC.To(e, vC2);
                        qUA.To(e, vA2); qUB.To(e, vB2); qUC.To(e, vC2);
                    }
                }

                var fwd = rng.NextInt(5) != 0;
                wS.Tick(pS, fwd);
                wC.Tick(pC, fwd);
                wU.Tick(pU, fwd);

                var order = Permutations[rng.NextInt(Permutations.Length)];
                var extraMember = allowDoubleSpanRun ? rng.NextInt(6) : (rng.NextInt(3) == 0 ? 2 : -1);
                var sysSA = new B3SplitSysA { A = sA };
                var sysSB = new B3SplitSysB { B = sB };
                var sysSC = new B3SplitSysC { C = sC };
                var sysUA = new B3UngrpSysA { A = uA };
                var sysUB = new B3UngrpSysB { B = uB };
                var sysUC = new B3UngrpSysC { C = uC };

                for (var idx = 0; idx < 3; idx++)
                {
                    switch (order[idx])
                    {
                        case 0:
                            sysSA.Run(wS, pS, fwd);
                            sysUA.Run(wU, pU, fwd);
                            if (extraMember == 0)
                            {
                                sysSA.Run(wS, pS, fwd);
                                sysUA.Run(wU, pU, fwd);
                            }
                            break;
                        case 1:
                            sysSB.Run(wS, pS, fwd);
                            sysUB.Run(wU, pU, fwd);
                            if (extraMember == 1)
                            {
                                sysSB.Run(wS, pS, fwd);
                                sysUB.Run(wU, pU, fwd);
                            }
                            break;
                        default:
                            sysSC.Run(wS, pS, fwd);
                            sysUC.Run(wU, pU, fwd);
                            if (extraMember == 2)
                            {
                                var beforeS = sC.ToArray();
                                var beforeU = uC.ToArray();
                                sysSC.Run(wS, pS, fwd);
                                sysUC.Run(wU, pU, fwd);
                                for (var e = 0; e < entities; e++)
                                {
                                    if (sC[e].V != beforeS[e].V || uC[e].V != beforeU[e].V)
                                        FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter,
                                            $"step {step} double-run pulse-only mutated e={e} sC:{beforeS[e].V}->{sC[e].V} uC:{beforeU[e].V}->{uC[e].V}");
                                }
                            }
                            break;
                    }
                }

                new B3CombSys { A = cA, B = cB, C = cC }.Run(wC, pC, fwd);

                for (var e = 0; e < entities; e++)
                {
                    if (sA[e].V != uA[e].V || sB[e].V != uB[e].V || sC[e].V != uC[e].V)
                        FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter,
                            $"step {step} split vs ungrouped e={e}: s=({sA[e].V},{sB[e].V},{sC[e].V}) u=({uA[e].V},{uB[e].V},{uC[e].V})");
                    if (!allowDoubleSpanRun && (cA[e].V != uA[e].V || cB[e].V != uB[e].V || cC[e].V != uC[e].V))
                        FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter,
                            $"step {step} combined vs ungrouped e={e}: c=({cA[e].V},{cB[e].V},{cC[e].V}) u=({uA[e].V},{uB[e].V},{uC[e].V})");
                }
                if (kS.DroppedPulses != kU.DroppedPulses || kC.DroppedPulses != kU.DroppedPulses)
                    FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter,
                        $"step {step} dropped pulses mismatch: S={kS.DroppedPulses} C={kC.DroppedPulses} U={kU.DroppedPulses}");
            }
        });
    }

    [Fact]
    public void SplitSystems_VaryingOrderDoubleRunPulseMemberPartialReachPulsesAndRebind_MatchUngroupedAndCombinedEveryTick()
        => RunGroupScenario("B3-GroupSplitVsCombined", 0x42330001UL, 16, allowDoubleSpanRun: false);

    [Fact]
    public void SplitSystems_DoubleRunSpanOrPulseMemberInAnyOrder_MatchesUngroupedTwinEveryTick()
        => RunGroupScenario("B3-GroupDoubleRunSpan", 0x42330002UL, 12, allowDoubleSpanRun: true);

    [Fact]
    public void SingleRun_FastGroupMemberSuperSamplesWhileSlowMemberAtFractionSpeed_MatchesUngroupedTwin()
    {
        FuzzRunner.Run("B3-GroupMixedSpeedSample", 0x42330003UL, 4, (rng, iter) =>
        {
            var masks = new[] { 1, 2 };
            var trip = BuildVariantTriplet(ref rng, masks, 2, 12);

            using var wS = new TimelineWorld(trip.Split.Bytes);
            using var pS = new PlaybackTable(1, 4);
            using var kS = new KindStore(wS, pS);

            using var wC = new TimelineWorld(trip.Comb.Bytes);
            using var pC = new PlaybackTable(1, 4);
            using var kC = new KindStore(wC, pC);

            using var wU = new TimelineWorld(trip.Ungrp.Bytes);
            using var pU = new PlaybackTable(1, 4);
            using var kU = new KindStore(wU, pU);

            void Apply(Action<PlaybackTable> f) { f(pS); f(pC); f(pU); }

            Apply(p => { p.Start(0, 1); p.SetSpeed(0, 1, 32); p.Start(0, 2); p.SetSpeed(0, 2, 12); });

            var sA = new B3SplitOutA[1]; var sB = new B3SplitOutB[1]; var sC = new B3SplitOutC[1];
            var cA = new B3CombOutA[1]; var cB = new B3CombOutB[1]; var cC = new B3CombOutC[1];
            var uA = new B3UngrpOutA[1]; var uB = new B3UngrpOutB[1]; var uC = new B3UngrpOutC[1];

            for (var step = 0; step < 12; step++)
            {
                wS.Tick(pS, true);
                wC.Tick(pC, true);
                wU.Tick(pU, true);

                new B3SplitSysA { A = sA }.Run(wS, pS, true);
                new B3SplitSysB { B = sB }.Run(wS, pS, true);
                new B3SplitSysC { C = sC }.Run(wS, pS, true);
                new B3CombSys { A = cA, B = cB, C = cC }.Run(wC, pC, true);
                new B3UngrpSysA { A = uA }.Run(wU, pU, true);
                new B3UngrpSysB { B = uB }.Run(wU, pU, true);
                new B3UngrpSysC { C = uC }.Run(wU, pU, true);

                if (sA[0].V != uA[0].V || sB[0].V != uB[0].V || cA[0].V != uA[0].V || cB[0].V != uB[0].V)
                    FuzzRunner.Fail("B3-GroupMixedSpeedSample", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42330003UL, iter,
                        $"step {step}: s=({sA[0].V},{sB[0].V}) c=({cA[0].V},{cB[0].V}) u=({uA[0].V},{uB[0].V})");
            }
        });
    }
}
