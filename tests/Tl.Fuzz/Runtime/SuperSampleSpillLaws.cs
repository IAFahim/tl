using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class SuperSampleSpillLaws
{
    static readonly string[] FuzzKindNames =
    [
        typeof(KCombineInt).FullName!,
        typeof(KEachInt).FullName!,
        typeof(KIdleInt).FullName!,
        typeof(KCombineFloat).FullName!,
    ];

    static (GeneratedLibrary Small, GeneratedLibrary Large) BuildRichKindPair(ref FuzzRandom rng, int timelines, int channels, int minDur, int maxDur)
    {
        var names = new string[timelines];
        for (var i = 0; i < timelines; i++) names[i] = $"ss_{i + 1:D2}";
        var chNames = new string[channels];
        for (var i = 0; i < channels; i++) chNames[i] = $"sch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, chNames, 60);
        var list = new TypedTimeline[timelines];

        for (var i = 0; i < timelines; i++)
        {
            var dur = (uint)(minDur + rng.NextInt(Math.Max(1, maxDur - minDur + 1)));
            var ch = i >= timelines / 2 ? chNames[rng.NextInt(channels)] : null;
            var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch };
            var mode = rng.NextInt(4);
            if (mode == 1) tl.Loops = true;
            else if (mode == 2) tl.Hold = true;
            else if (mode == 3) tl.Next = names[rng.NextInt(timelines)];

            var kc = new TypedTrack
            {
                Name = "kc",
                Type = typeof(KCombineTrack),
                Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(4) }),
            };
            kc.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = 1,
                End = dur,
                Data = LibraryGen.BytesOf(new KCombineClip
                {
                    Delta = (rng.NextBool() ? 1 : -1) * (1 + rng.NextInt(11)),
                    StartPulse = rng.NextInt(9) - 4,
                    EndPulse = rng.NextInt(9) - 4,
                }),
            });
            tl.Tracks.Add(kc);

            if (rng.NextInt(3) != 0)
            {
                var ke = new TypedTrack
                {
                    Name = "ke",
                    Type = typeof(KEachTrack),
                    Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(3) }),
                };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    ke.Clips.Add(new TypedClip
                    {
                        Type = typeof(KEachClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KEachClip
                        {
                            Delta = rng.NextInt(9) - 4,
                            StartPulse = rng.NextInt(5) - 2,
                            EndPulse = rng.NextInt(5) - 2,
                        }),
                    });
                }
                tl.Tracks.Add(ke);
            }

            if (rng.NextBool())
            {
                var ki = new TypedTrack
                {
                    Name = "ki",
                    Type = typeof(KIdleTrack),
                    Data = LibraryGen.BytesOf(new KIdleTrack { Bias = rng.NextInt(5) - 2 }),
                };
                ki.Clips.Add(new TypedClip
                {
                    Type = typeof(KIdleClip),
                    Start = 0,
                    End = dur,
                    Data = LibraryGen.BytesOf(new KIdleClip { Code = 1 + rng.NextInt(400) }),
                });
                tl.Tracks.Add(ki);
            }

            if (rng.NextInt(2) == 0 && dur > 2)
            {
                tl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = 1 + (uint)rng.NextInt((int)dur - 1),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 1 + rng.NextInt(15) }),
                });
            }

            list[i] = tl;
        }

        var smallOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
        var largeOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
        for (var k = 0; k < FuzzKindNames.Length; k++)
        {
            smallOv[FuzzKindNames[k]] = new KindOverride(inline: 2, gamePulses: 2, maxInlineBytes: 16);
            largeOv[FuzzKindNames[k]] = new KindOverride(inline: 260, gamePulses: 64, maxInlineBytes: 2048);
        }

        var smallKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], smallOv);
        var largeKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], largeOv);
        return (
            LibraryGen.AssembleTyped(list, chNames, manifest, smallKinds, 16, 1, smallOv),
            LibraryGen.AssembleTyped(list, chNames, manifest, largeKinds, 2048, 1, largeOv));
    }

    static ulong HashState(KindIntCol[] c, KindEachCol[] e, KindIdleCol[] i, KindFloatCol[] f, PlaybackTable p)
    {
        var h = TlStateHash.Begin();
        fixed (KindIntCol* pc = c) h.Bytes((byte*)pc, c.Length * sizeof(KindIntCol));
        fixed (KindEachCol* pe = e) h.Bytes((byte*)pe, e.Length * sizeof(KindEachCol));
        fixed (KindIdleCol* pi = i) h.Bytes((byte*)pi, i.Length * sizeof(KindIdleCol));
        fixed (KindFloatCol* pf = f) h.Bytes((byte*)pf, f.Length * sizeof(KindFloatCol));
        FuzzRunner.MixPlaybacks(ref h, p);
        return h.Value;
    }

    [Fact]
    public void SuperSamplingUnderSpill_SmallInlineMatchesLargeNoSpillEveryTick()
    {
        FuzzRunner.Run("B1-SuperSampleSpill", 0x42310001UL, 28, (rng, iter) =>
        {
            var smallInline = 1 + rng.NextInt(8);
            var entities = 6 + rng.NextInt(18);
            var timelines = 14 + rng.NextInt(10);
            var channels = 2 + rng.NextInt(3);
            var (smallLib, largeLib) = BuildRichKindPair(ref rng, timelines, channels, 24, 96);

            using var wS = new TimelineWorld(smallLib.Bytes);
            using var wL = new TimelineWorld(largeLib.Bytes);
            using var pS = new PlaybackTable(entities, smallInline);
            using var pL = new PlaybackTable(entities, 256);
            using var ksS = new KindStore(wS, pS);
            using var ksL = new KindStore(wL, pL);

            var cS = new KindIntCol[entities];
            var cL = new KindIntCol[entities];
            var eS = new KindEachCol[entities];
            var eL = new KindEachCol[entities];
            var iS = new KindIdleCol[entities];
            var iL = new KindIdleCol[entities];
            var fS = new KindFloatCol[entities];
            var fL = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 + (e & 1) };

            var pulS = ksS.Pulses<int>(typeof(KCombineInt).FullName!);
            var pulL = ksL.Pulses<int>(typeof(KCombineInt).FullName!);

            var sawSpill = false;
            var sawUnspill = false;

            for (var step = 0; step < 26; step++)
            {
                for (var e = 0; e < entities; e++)
                {
                    if (step % 5 == 0)
                    {
                        var targetSlots = Math.Max(1, smallInline - 2 + (e + step) % 5);
                        pS.Reset(e);
                        pL.Reset(e);
                        for (var s = 0; s < targetSlots; s++)
                        {
                            var tl = (ushort)(1 + (s + rng.NextInt(timelines / 2)) % (timelines / 2));
                            var rate = (ushort)(17 + rng.NextInt(239));
                            pS.Start(e, tl, 1);
                            pL.Start(e, tl, 1);
                            pS.SetSpeed(e, tl, rate);
                            pL.SetSpeed(e, tl, rate);
                        }
                    }
                    else
                    {
                        var ops = 1 + rng.NextInt(5);
                        for (var r = 0; r < ops; r++)
                        {
                            var opKind = rng.NextInt(5);
                            var tl = (ushort)(1 + rng.NextInt(timelines));
                            if (opKind <= 1)
                            {
                                var rate = (ushort)(17 + rng.NextInt(239));
                                var at = (ushort)rng.NextInt(4);
                                pS.Start(e, tl, at);
                                pL.Start(e, tl, at);
                                pS.SetSpeed(e, tl, rate);
                                pL.SetSpeed(e, tl, rate);
                            }
                            else if (opKind == 2)
                            {
                                pS.Stop(e, tl);
                                pL.Stop(e, tl);
                            }
                            else if (opKind == 3)
                            {
                                var rate = (ushort)(17 + rng.NextInt(239));
                                pS.SetSpeed(e, tl, rate);
                                pL.SetSpeed(e, tl, rate);
                            }
                            else if (rng.NextInt(4) == 0)
                            {
                                pS.Reset(e);
                                pL.Reset(e);
                            }
                        }
                    }

                    if (rng.NextBool())
                    {
                        var val = rng.NextInt(15) - 7;
                        pulS.To(e, val);
                        pulL.To(e, val);
                    }
                }

                var wasSpilled = new bool[entities];
                for (var e = 0; e < entities; e++) wasSpilled[e] = pS.IsSpilled(e);

                var forward = step % 7 != 5;
                wS.Tick(pS, forward);
                wL.Tick(pL, forward);

                for (var e = 0; e < entities; e++)
                {
                    if (pS.IsSpilled(e)) sawSpill = true;
                    if (wasSpilled[e] && !pS.IsSpilled(e)) sawUnspill = true;
                }

                new KindFuzzSystem { CombineCol = cS, EachCol = eS, IdleCol = iS, FloatCol = fS, Input = inp }.Run(wS, pS, forward);
                new KindFuzzSystem { CombineCol = cL, EachCol = eL, IdleCol = iL, FloatCol = fL, Input = inp }.Run(wL, pL, forward);

                var pbErr = FuzzRunner.ComparePlaybacks(pS, pL);
                if (pbErr != null)
                    FuzzRunner.Fail("B1-SuperSampleSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42310001UL, iter, $"step {step} smallInline={smallInline}: {pbErr}");

                var colErr = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cS), MemoryMarshal.AsBytes<KindIntCol>(cL), $"step {step} CombineCol")
                    ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(eS), MemoryMarshal.AsBytes<KindEachCol>(eL), $"step {step} EachCol")
                    ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(iS), MemoryMarshal.AsBytes<KindIdleCol>(iL), $"step {step} IdleCol")
                    ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(fS), MemoryMarshal.AsBytes<KindFloatCol>(fL), $"step {step} FloatCol");
                if (colErr != null)
                    FuzzRunner.Fail("B1-SuperSampleSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42310001UL, iter, $"smallInline={smallInline} {colErr}");

                Assert.Equal(HashState(cL, eL, iL, fL, pL), HashState(cS, eS, iS, fS, pS));
            }

            Assert.True(sawSpill || smallInline >= 8);
            Assert.True(sawUnspill || smallInline >= 8);
        });
    }

    [Fact]
    public void SuperSamplingUnderSpill_SpeedLawMatchesOneXAcrossSpillGrowAndUnspill()
    {
        FuzzRunner.Run("B1-SuperSampleSpillSpeedLaw", 0x42310002UL, 24, (rng, iter) =>
        {
            var smallInline = 1 + rng.NextInt(8);
            const int spacers = 6;
            var totalTimelines = 1 + spacers;
            var names = new string[totalTimelines];
            for (var i = 0; i < totalTimelines; i++) names[i] = $"sp_{i}";
            var ch = new[] { "ch_main" };
            var manifest = LibraryBake.Manifest(names, ch, 60);
            var list = new TypedTimeline[totalTimelines];

            var mainDur = (uint)(60 + rng.NextInt(120));
            var mainTl = new TypedTimeline { Name = names[0], Duration = mainDur, Loops = rng.NextBool() };
            var kc = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(3) }) };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, mainDur, false))
            {
                kc.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KCombineClip
                    {
                        Delta = (rng.NextBool() ? 1 : -1) * (1 + rng.NextInt(9)),
                        StartPulse = rng.NextInt(5) - 2,
                        EndPulse = rng.NextInt(5) - 2,
                    }),
                });
            }
            mainTl.Tracks.Add(kc);
            if (mainDur > 4)
            {
                mainTl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = 2 + (uint)rng.NextInt((int)mainDur - 3),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 1 + rng.NextInt(9) }),
                });
            }
            list[0] = mainTl;

            for (var i = 1; i < totalTimelines; i++)
            {
                var st = new TypedTimeline { Name = names[i], Duration = 160, Loops = true };
                var ki = new TypedTrack { Name = "ki", Type = typeof(KIdleTrack), Data = LibraryGen.BytesOf(new KIdleTrack { Bias = i }) };
                ki.Clips.Add(new TypedClip { Type = typeof(KIdleClip), Start = 0, End = 160, Data = LibraryGen.BytesOf(new KIdleClip { Code = i * 10 }) });
                st.Tracks.Add(ki);
                list[i] = st;
            }

            var ov = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
            for (var k = 0; k < FuzzKindNames.Length; k++)
                ov[FuzzKindNames[k]] = new KindOverride(inline: 2, gamePulses: 2, maxInlineBytes: 16);
            var kinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], ov);
            var lib = LibraryGen.AssembleTyped(list, ch, manifest, kinds, 16, 1, ov);

            using var wS = new TimelineWorld(lib.Bytes);
            using var w1 = new TimelineWorld(lib.Bytes);
            using var pS = new PlaybackTable(1, smallInline);
            using var p1 = new PlaybackTable(1, 8);
            using var ksS = new KindStore(wS, pS);
            using var ks1 = new KindStore(w1, p1);

            var cS = new KindIntCol[1];
            var c1 = new KindIntCol[1];
            var eS = new KindEachCol[1];
            var e1 = new KindEachCol[1];
            var iS = new KindIdleCol[1];
            var i1 = new KindIdleCol[1];
            var fS = new KindFloatCol[1];
            var f1 = new KindFloatCol[1];
            var inp = new[] { new KindInputCol { Factor = 1 + rng.NextInt(3) } };

            var mainFirst = rng.NextBool();
            var spacerCount = Math.Min(spacers, Math.Max(1, smallInline + rng.NextInt(4)));
            var rate = (ushort)(17 + rng.NextInt(239));

            if (mainFirst)
            {
                pS.Start(0, 1);
                pS.SetSpeed(0, 1, rate);
            }
            for (var sp = 0; sp < spacerCount; sp++)
            {
                var tlId = (ushort)(2 + sp);
                pS.Start(0, tlId);
                pS.SetSpeed(0, tlId, (ushort)(17 + rng.NextInt(239)));
            }
            if (!mainFirst)
            {
                pS.Start(0, 1);
                pS.SetSpeed(0, 1, rate);
            }

            p1.Start(0, 1);

            for (var step = 0; step < 16; step++)
            {
                if (step == 6)
                {
                    rate = (ushort)(17 + rng.NextInt(239));
                    pS.SetSpeed(0, 1, rate);
                    for (var sp = 0; sp < spacerCount; sp++)
                        pS.SetSpeed(0, (ushort)(2 + sp), (ushort)(17 + rng.NextInt(239)));
                }
                if (step == 10)
                {
                    for (var sp = 0; sp < spacerCount; sp++)
                        pS.Stop(0, (ushort)(2 + sp));
                }
                if (step == 13)
                {
                    for (var sp = 0; sp < spacerCount; sp++)
                    {
                        var tlId = (ushort)(2 + sp);
                        pS.Start(0, tlId);
                        pS.SetSpeed(0, tlId, (ushort)(17 + rng.NextInt(239)));
                    }
                }

                wS.ApplyRequests(pS);
                w1.ApplyRequests(p1);

                var mainIdx = -1;
                var slots = pS.SlotsOf(0);
                for (var s = 0; s < pS.CountOf(0); s++)
                {
                    if (slots[s].Timeline == 1)
                    {
                        mainIdx = s;
                        break;
                    }
                }
                if (mainIdx < 0) break;

                var forward = step % 6 != 4;
                var before = slots[mainIdx];
                var moves = PlaybackRules.Moves(before.Flags, forward);
                var crossings = moves ? PlaybackRules.Crossings(in before) : 1;

                wS.Tick(pS, forward);
                new KindFuzzSystem { CombineCol = cS, EachCol = eS, IdleCol = iS, FloatCol = fS, Input = inp }.Run(wS, pS, forward);

                for (var sub = 0; sub < crossings; sub++)
                {
                    w1.Tick(p1, forward);
                    new KindFuzzSystem { CombineCol = c1, EachCol = e1, IdleCol = i1, FloatCol = f1, Input = inp }.Run(w1, p1, forward);
                }

                if (c1[0].Sum != cS[0].Sum)
                    FuzzRunner.Fail("B1-SuperSampleSpillSpeedLaw", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42310002UL, iter,
                        $"step {step} smallInline={smallInline} spacerCount={spacerCount} mainFirst={mainFirst} rate={rate}: CombineSum {cS[0].Sum} != 1x {c1[0].Sum}");
            }
        });
    }
}
