using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed class KindLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    static (GeneratedLibrary Folded, GeneratedLibrary Unfolded) BuildFoldedPair(
        ref FuzzRandom rng,
        int timelineCount,
        int channelCount,
        int maxDuration,
        int foldedGamePulses,
        int unfoldedGamePulses,
        int maxInlineBytes)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"fp_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"fch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var foldedList = new TypedTimeline[timelineCount];
        var unfoldedList = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(2 + rng.NextInt(maxDuration));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            var next = mode == 3 ? names[rng.NextInt(timelineCount)] : null;

            var ftl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };
            var utl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };

            var tracks = 1 + rng.NextInt(2);
            for (var t = 0; t < tracks; t++)
            {
                var ftr = new TypedTrack { Name = $"ft_{t}", Type = typeof(FoldPulseTrack), Data = LibraryGen.BytesOf(new FoldPulseTrack()) };
                var utr = new TypedTrack { Name = $"ft_{t}", Type = typeof(UnfoldPulseTrack), Data = LibraryGen.BytesOf(new UnfoldPulseTrack()) };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var sv = rng.NextInt(15) - 7;
                    var pv = rng.NextInt(21) - 10;
                    ftr.Clips.Add(new TypedClip { Type = typeof(FoldPulseClip), Start = s, End = e, Data = LibraryGen.BytesOf(new FoldPulseClip { SpanVal = sv, PulseVal = pv }) });
                    utr.Clips.Add(new TypedClip { Type = typeof(UnfoldPulseClip), Start = s, End = e, Data = LibraryGen.BytesOf(new UnfoldPulseClip { SpanVal = sv, PulseVal = pv }) });
                }
                ftl.Tracks.Add(ftr);
                utl.Tracks.Add(utr);
            }

            foldedList[i] = ftl;
            unfoldedList[i] = utl;
        }

        var foldOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
        {
            [typeof(FoldedPulseKind).FullName!] = new KindOverride(inline: 2, gamePulses: foldedGamePulses, maxInlineBytes: maxInlineBytes),
        };
        var unfoldOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
        {
            [typeof(UnfoldedPulseKind).FullName!] = new KindOverride(inline: 4, gamePulses: unfoldedGamePulses, maxInlineBytes: Math.Max(maxInlineBytes, 256)),
        };

        var foldKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], foldOv);
        var unfoldKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], unfoldOv);
        var fLib = LibraryGen.AssembleTyped(foldedList, channels, manifest, foldKinds, maxInlineBytes, 1, foldOv);
        var uLib = LibraryGen.AssembleTyped(unfoldedList, channels, manifest, unfoldKinds, Math.Max(maxInlineBytes, 256), 1, unfoldOv);
        return (fLib, uLib);
    }

    static (GeneratedLibrary Derived, GeneratedLibrary Hand) BuildDerivedPair(ref FuzzRandom rng, int timelineCount, int channelCount, int maxDuration)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"dr_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"dch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var dList = new TypedTimeline[timelineCount];
        var hList = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(2 + rng.NextInt(maxDuration));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            var next = mode == 3 ? names[rng.NextInt(timelineCount)] : null;

            var dtl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };
            var htl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };

            var calm = i > 0 && rng.NextInt(5) == 0;
            if (!calm)
            {
                var dSum = new TypedTrack { Name = "sum", Type = typeof(TlNoSettings<DiffSumClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<DiffSumClip>>()] };
                var hSum = new TypedTrack { Name = "sum", Type = typeof(TlNoSettings<HandSumClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<HandSumClip>>()] };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var a = rng.NextInt(13) - 6;
                    var b = (rng.NextInt(9) - 4) * 0.25f;
                    var pa = rng.NextInt(9) - 4;
                    var pb = (rng.NextInt(5) - 2) * 0.5f;
                    dSum.Clips.Add(new TypedClip { Type = typeof(DiffSumClip), Start = s, End = e, Data = LibraryGen.BytesOf(new DiffSumClip { A = a, B = b, PulseA = pa, PulseB = pb }) });
                    hSum.Clips.Add(new TypedClip { Type = typeof(HandSumClip), Start = s, End = e, Data = LibraryGen.BytesOf(new HandSumClip { A = a, B = b, PulseA = pa, PulseB = pb }) });
                }
                dtl.Tracks.Add(dSum);
                htl.Tracks.Add(hSum);

                if (rng.NextBool())
                {
                    var dSet = new TypedTrack { Name = "set", Type = typeof(TlNoSettings<DiffSetClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<DiffSetClip>>()] };
                    var hSet = new TypedTrack { Name = "set", Type = typeof(TlNoSettings<HandSetClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<HandSetClip>>()] };
                    foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                    {
                        var m = 1 + rng.NextInt(100);
                        var ang = rng.NextInt(360) * 0.5f;
                        var sm = 1000 + rng.NextInt(50);
                        var sang = rng.NextInt(100) * 0.25f;
                        dSet.Clips.Add(new TypedClip { Type = typeof(DiffSetClip), Start = s, End = e, Data = LibraryGen.BytesOf(new DiffSetClip { Mode = m, Angle = ang, SnapMode = sm, SnapAngle = sang }) });
                        hSet.Clips.Add(new TypedClip { Type = typeof(HandSetClip), Start = s, End = e, Data = LibraryGen.BytesOf(new HandSetClip { Mode = m, Angle = ang, SnapMode = sm, SnapAngle = sang }) });
                    }
                    dtl.Tracks.Add(dSet);
                    htl.Tracks.Add(hSet);
                }

                if (rng.NextBool())
                {
                    var dWhile = new TypedTrack { Name = "while", Type = typeof(TlNoSettings<DiffWhileClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<DiffWhileClip>>()] };
                    var hWhile = new TypedTrack { Name = "while", Type = typeof(TlNoSettings<HandWhileClip>), Data = new byte[Unsafe.SizeOf<TlNoSettings<HandWhileClip>>()] };
                    foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                    {
                        var lvl = 1 << rng.NextInt(10);
                        var pwr = rng.NextInt(100) * 0.5f;
                        dWhile.Clips.Add(new TypedClip { Type = typeof(DiffWhileClip), Start = s, End = e, Data = LibraryGen.BytesOf(new DiffWhileClip { Level = lvl, Power = pwr }) });
                        hWhile.Clips.Add(new TypedClip { Type = typeof(HandWhileClip), Start = s, End = e, Data = LibraryGen.BytesOf(new HandWhileClip { Level = lvl, Power = pwr }) });
                    }
                    dtl.Tracks.Add(dWhile);
                    htl.Tracks.Add(hWhile);
                }
            }

            dList[i] = dtl;
            hList[i] = htl;
        }

        var dLib = LibraryGen.AssembleTyped(dList, channels, manifest, AllKinds);
        var hLib = LibraryGen.AssembleTyped(hList, channels, manifest, AllKinds);
        return (dLib, hLib);
    }

    static (GeneratedLibrary Grouped, GeneratedLibrary Ungrouped) BuildGroupPair(ref FuzzRandom rng, int timelineCount, int channelCount, int maxDuration)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"gr_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"gch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var gList = new TypedTimeline[timelineCount];
        var uList = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(2 + rng.NextInt(maxDuration));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            var next = mode == 3 ? names[rng.NextInt(timelineCount)] : null;

            var gtl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };
            var utl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = mode == 1, Hold = mode == 2, Next = next };

            var calm = i > 0 && rng.NextInt(5) == 0;
            if (!calm)
            {
                var ga = new TypedTrack { Name = "ga", Type = typeof(SpanGrpTrackA), Data = LibraryGen.BytesOf(new SpanGrpTrackA()) };
                var ua = new TypedTrack { Name = "ga", Type = typeof(SpanUngrpTrackA), Data = LibraryGen.BytesOf(new SpanUngrpTrackA()) };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    var d = rng.NextInt(15) - 7;
                    ga.Clips.Add(new TypedClip { Type = typeof(SpanGrpClipA), Start = s, End = e, Data = LibraryGen.BytesOf(new SpanGrpClipA { Delta = d }) });
                    ua.Clips.Add(new TypedClip { Type = typeof(SpanUngrpClipA), Start = s, End = e, Data = LibraryGen.BytesOf(new SpanUngrpClipA { Delta = d }) });
                }
                gtl.Tracks.Add(ga);
                utl.Tracks.Add(ua);

                if (rng.NextBool())
                {
                    var gb = new TypedTrack { Name = "gb", Type = typeof(SpanGrpTrackB), Data = LibraryGen.BytesOf(new SpanGrpTrackB()) };
                    var ub = new TypedTrack { Name = "gb", Type = typeof(SpanUngrpTrackB), Data = LibraryGen.BytesOf(new SpanUngrpTrackB()) };
                    foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                    {
                        var d = 1 + rng.NextInt(500);
                        gb.Clips.Add(new TypedClip { Type = typeof(SpanGrpClipB), Start = s, End = e, Data = LibraryGen.BytesOf(new SpanGrpClipB { Delta = d }) });
                        ub.Clips.Add(new TypedClip { Type = typeof(SpanUngrpClipB), Start = s, End = e, Data = LibraryGen.BytesOf(new SpanUngrpClipB { Delta = d }) });
                    }
                    gtl.Tracks.Add(gb);
                    utl.Tracks.Add(ub);
                }
            }

            gList[i] = gtl;
            uList[i] = utl;
        }

        var gLib = LibraryGen.AssembleTyped(gList, channels, manifest, AllKinds);
        var uLib = LibraryGen.AssembleTyped(uList, channels, manifest, AllKinds);
        return (gLib, uLib);
    }

    [Fact]
    public void FoldedPulses_MatchesUnfoldedPulsesAcrossInlineColdAndGamePulses()
    {
        FuzzRunner.Run("T7-FoldedVsUnfoldedPulses", 0x54370001UL, 32, (rng, _) =>
        {
            var entities = 2 + rng.NextInt(24);
            var timelines = 2 + rng.NextInt(5);
            var channels = 1 + rng.NextInt(3);
            var foldedGameCap = 1 + rng.NextInt(4);
            var maxInlineBytes = rng.NextBool() ? 8 : 64;
            var (fLib, uLib) = BuildFoldedPair(ref rng, timelines, channels, 50, foldedGameCap, 512, maxInlineBytes);

            using var wf = new TimelineWorld(fLib.Bytes);
            using var wu = new TimelineWorld(uLib.Bytes);
            using var pf = new PlaybackTable(entities, 8);
            using var pu = new PlaybackTable(entities, 8);
            using var ksf = new KindStore(wf, pf);
            using var ksu = new KindStore(wu, pu);

            var fPulse = ksf.Pulses<int, IntAddFold>(typeof(FoldedPulseKind).FullName!);
            var uPulse = ksu.Pulses<int>(typeof(UnfoldedPulseKind).FullName!);

            var fOut = new FoldPulseOut[entities];
            var uOut = new UnfoldPulseOut[entities];
            var sysF = new FoldedPulseSystem { Out = fOut };
            var sysU = new UnfoldedPulseSystem { Out = uOut };

            for (var step = 0; step < 30; step++)
            {
                var reqs = 1 + rng.NextInt(entities * 2);
                for (var r = 0; r < reqs; r++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, fLib.Durations);
                    LibraryGen.ApplyOp(pf, in op);
                    LibraryGen.ApplyOp(pu, in op);
                }

                var pulseBursts = 1 + rng.NextInt(entities * 6);
                for (var p = 0; p < pulseBursts; p++)
                {
                    var e = rng.NextInt(entities);
                    var val = rng.NextInt(31) - 15;
                    fPulse.To(e, val);
                    uPulse.To(e, val);
                }

                var forward = step % 6 != 4;
                wf.Tick(pf, forward);
                wu.Tick(pu, forward);
                sysF.Run(wf, pf, forward);
                sysU.Run(wu, pu, forward);

                Assert.Equal(0, ksf.DroppedPulses);
                Assert.Equal(0, ksu.DroppedPulses);
                Assert.True(FuzzRunner.ComparePlaybacks(pf, pu) is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<FoldPulseOut>(fOut), MemoryMarshal.AsBytes<UnfoldPulseOut>(uOut), $"step {step} FoldedOut") is null);
            }
        });
    }

    [Fact]
    public void DerivedKinds_SumSetWhile_MatchesHandWrittenEquivalentsEveryTick()
    {
        FuzzRunner.Run("T7-DerivedVsHandWritten", 0x54370002UL, 32, (rng, _) =>
        {
            var entities = 2 + rng.NextInt(32);
            var timelines = 2 + rng.NextInt(5);
            var channels = 1 + rng.NextInt(3);
            var (dLib, hLib) = BuildDerivedPair(ref rng, timelines, channels, 60);

            using var wd = new TimelineWorld(dLib.Bytes);
            using var wh = new TimelineWorld(hLib.Bytes);
            using var pd = new PlaybackTable(entities, 8);
            using var ph = new PlaybackTable(entities, 8);
            using var ksd = new KindStore(wd, pd);
            using var ksh = new KindStore(wh, ph);

            var dSum = new FuzzSumState[entities];
            var hSum = new HandSumState[entities];
            var dSet = new FuzzSetState[entities];
            var hSet = new HandSetState[entities];
            var dWhile = new FuzzWhileState[entities];
            var hWhile = new HandWhileState[entities];

            for (var step = 0; step < 30; step++)
            {
                var reqs = 1 + rng.NextInt(entities * 2);
                for (var r = 0; r < reqs; r++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, dLib.Durations);
                    LibraryGen.ApplyOp(pd, in op);
                    LibraryGen.ApplyOp(ph, in op);
                }

                var forward = step % 7 != 5;
                wd.Tick(pd, forward);
                wh.Tick(ph, forward);

                new DerivedDiffSystem { Sum = dSum, Set = dSet, While = dWhile }.Run(wd, pd, forward);
                new HandDiffSystem { Sum = hSum, Set = hSet, While = hWhile }.Run(wh, ph, forward);

                Assert.True(FuzzRunner.ComparePlaybacks(pd, ph) is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<FuzzSumState>(dSum), MemoryMarshal.AsBytes<HandSumState>(hSum), $"step {step} Sum") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<FuzzSetState>(dSet), MemoryMarshal.AsBytes<HandSetState>(hSet), $"step {step} Set") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<FuzzWhileState>(dWhile), MemoryMarshal.AsBytes<HandWhileState>(hWhile), $"step {step} While") is null);
            }
        });
    }

    [Fact]
    public void KindGroups_GroupedMatchesUngroupedKindsEveryTick()
    {
        FuzzRunner.Run("T7-GroupedVsUngrouped", 0x54370003UL, 32, (rng, iter) =>
        {
            var entities = 2 + rng.NextInt(32);
            var timelines = 2 + rng.NextInt(5);
            var channels = 1 + rng.NextInt(3);
            var (gLib, uLib) = BuildGroupPair(ref rng, timelines, channels, 60);

            using var wg = new TimelineWorld(gLib.Bytes);
            using var wu = new TimelineWorld(uLib.Bytes);
            using var pg = new PlaybackTable(entities, 8);
            using var pu = new PlaybackTable(entities, 8);
            using var ksg = new KindStore(wg, pg);
            using var ksu = new KindStore(wu, pu);

            var gpA = ksg.Pulses<int>(typeof(GrpSpanKindA).FullName!);
            var gpB = ksg.Pulses<int>(typeof(GrpSpanKindB).FullName!);
            var upA = ksu.Pulses<int>(typeof(UngrpSpanKindA).FullName!);
            var upB = ksu.Pulses<int>(typeof(UngrpSpanKindB).FullName!);

            var gA = new SpanGrpOutA[entities];
            var uA = new SpanUngrpOutA[entities];
            var gB = new SpanGrpOutB[entities];
            var uB = new SpanUngrpOutB[entities];

            for (var step = 0; step < 30; step++)
            {
                var reqs = 1 + rng.NextInt(entities * 2);
                for (var r = 0; r < reqs; r++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, gLib.Durations);
                    LibraryGen.ApplyOp(pg, in op);
                    LibraryGen.ApplyOp(pu, in op);
                }

                var pulses = rng.NextInt(entities * 2);
                for (var g = 0; g < pulses; g++)
                {
                    var e = rng.NextInt(entities);
                    var v = rng.NextInt(41) - 20;
                    if (rng.NextBool()) { gpA.To(e, v); upA.To(e, v); }
                    else { gpB.To(e, v); upB.To(e, v); }
                }

                var forward = step % 6 != 4;
                wg.Tick(pg, forward);
                wu.Tick(pu, forward);

                new GroupedSpanFuzzSystem { A = gA, B = gB }.Run(wg, pg, forward);
                new UngroupedSpanFuzzSystem { A = uA, B = uB }.Run(wu, pu, forward);

                var pbErr = FuzzRunner.ComparePlaybacks(pg, pu);
                Assert.True(pbErr == null, pbErr);
                for (var e = 0; e < entities; e++)
                {
                    if (gA[e].V != uA[e].V || gB[e].V != uB[e].V)
                        FuzzRunner.Fail("T7-GroupedVsUngrouped", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x54370003UL, iter,
                            $"step {step} entity {e}: gA={gA[e].V} uA={uA[e].V} gB={gB[e].V} uB={uB[e].V}");
                }
            }
        });
    }

    [Fact]
    public void IdleAndReset_ClearsAndRestoresIdleSentinelWhenNoSpansAreActive()
    {
        FuzzRunner.Run("T7-IdleAndReset", 0x54370004UL, 24, (rng, _) =>
        {
            const int entities = 8;
            var names = new[] { "it_calm", "it_hold", "it_once" };
            var channels = new[] { "ch1" };
            var manifest = LibraryBake.Manifest(names, channels, 60);

            var t0 = new TypedTimeline { Name = "it_calm", Duration = 5 };
            var tr0 = new TypedTrack { Name = "c0", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            tr0.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 5, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 1 }) });
            t0.Tracks.Add(tr0);

            var t1 = new TypedTimeline { Name = "it_hold", Duration = 4, Hold = true };
            var tr1 = new TypedTrack { Name = "i1", Type = typeof(KIdleTrack), Data = LibraryGen.BytesOf(new KIdleTrack { Bias = 2 }) };
            tr1.Clips.Add(new TypedClip { Type = typeof(KIdleClip), Start = 1, End = 3, Data = LibraryGen.BytesOf(new KIdleClip { Code = 200 }) });
            t1.Tracks.Add(tr1);

            var t2 = new TypedTimeline { Name = "it_once", Duration = 6, Channel = "ch1" };
            var tr2 = new TypedTrack { Name = "i2", Type = typeof(KIdleTrack), Data = LibraryGen.BytesOf(new KIdleTrack { Bias = 5 }) };
            tr2.Clips.Add(new TypedClip { Type = typeof(KIdleClip), Start = 0, End = 6, Data = LibraryGen.BytesOf(new KIdleClip { Code = 100 }) });
            t2.Tracks.Add(tr2);

            var lib = LibraryGen.AssembleTyped([t0, t1, t2], channels, manifest, AllKinds);

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(entities, 8);
            using var store = new KindStore(world, players);

            var cCol = new KindIntCol[entities];
            var eCol = new KindEachCol[entities];
            var iCol = new KindIdleCol[entities];
            var fCol = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

            var sys = new KindFuzzSystem { CombineCol = cCol, EachCol = eCol, IdleCol = iCol, FloatCol = fCol, Input = inp };

            for (var step = 0; step < 20; step++)
            {
                for (var e = 0; e < entities; e++)
                    iCol[e].Value = 999999;

                for (var e = 0; e < entities; e++)
                {
                    var act = rng.NextInt(6);
                    if (act == 0) players.Start(e, (ushort)(1 + rng.NextInt(3)));
                    else if (act == 1) players.Stop(e, 3);
                    else if (act == 2) players.StopAll(e);
                    else if (act == 3) players.Reset(e);
                    else if (act == 4) players.Seek(e, 2, (ushort)rng.NextInt(6));
                }

                world.Tick(players, true);
                sys.Run(world, players, true);

                for (var e = 0; e < entities; e++)
                {
                    unsafe
                    {
                        var count = players.CountOf(e);
                        var slots = players.SlotsOf(e);
                        var hasOnce = false;
                        var hasHold = false;
                        for (var s = 0; s < count; s++)
                        {
                            var sl = slots[s];
                            if ((sl.Flags & (PlaybackFlags.Ended | PlaybackFlags.Stopping)) != 0) continue;
                            if (sl.Timeline == 3) hasOnce = true;
                            else if (sl.Timeline == 2 && sl.Tick >= 1 && sl.Tick < 3) hasHold = true;
                        }
                        if (hasOnce && hasHold)
                            Assert.True(iCol[e].Value is 105 or 202);
                        else if (hasOnce)
                            Assert.Equal(105, iCol[e].Value);
                        else if (hasHold)
                            Assert.Equal(202, iCol[e].Value);
                        else
                            Assert.Equal(-777, iCol[e].Value);
                    }
                }
            }
        });
    }
}
