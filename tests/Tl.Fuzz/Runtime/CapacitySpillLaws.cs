using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class CapacitySpillLaws
{
    static readonly string[] FuzzKindNames =
    [
        typeof(KCombineInt).FullName!,
        typeof(KEachInt).FullName!,
        typeof(KIdleInt).FullName!,
        typeof(KCombineFloat).FullName!,
    ];

    static (GeneratedLibrary Small, GeneratedLibrary Large) BuildPairedKindLibraries(ref FuzzRandom rng, int timelineCount, int channelCount, int maxDuration)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++)
            names[i] = $"tl_{i + 1:D2}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++)
            channels[i] = $"ch_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var timelines = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(4 + rng.NextInt(maxDuration));
            var ch = i >= 6 && rng.NextBool() ? null : (channelCount > 0 && rng.NextInt(3) == 0 ? channels[rng.NextInt(channelCount)] : null);
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = ch,
            };
            var mode = rng.NextInt(4);
            if (mode == 1) tl.Loops = true;
            else if (mode == 2) tl.Hold = true;
            else if (mode == 3) tl.Next = names[rng.NextInt(timelineCount)];

            var ct = new TypedTrack
            {
                Name = "kc",
                Type = typeof(KCombineTrack),
                Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(3) }),
            };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
            {
                ct.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = s,
                    End = e,
                    Data = LibraryGen.BytesOf(new KCombineClip
                    {
                        Delta = rng.NextInt(11) - 5,
                        StartPulse = rng.NextInt(5) - 2,
                        EndPulse = rng.NextInt(5) - 2,
                    }),
                });
            }
            tl.Tracks.Add(ct);

            if (rng.NextBool())
            {
                var et = new TypedTrack
                {
                    Name = "ke",
                    Type = typeof(KEachTrack),
                    Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(2) }),
                };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    et.Clips.Add(new TypedClip
                    {
                        Type = typeof(KEachClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KEachClip
                        {
                            Delta = rng.NextInt(7) - 3,
                            StartPulse = rng.NextInt(5) - 2,
                            EndPulse = rng.NextInt(5) - 2,
                        }),
                    });
                }
                tl.Tracks.Add(et);
            }

            if (rng.NextBool())
            {
                var it = new TypedTrack
                {
                    Name = "ki",
                    Type = typeof(KIdleTrack),
                    Data = LibraryGen.BytesOf(new KIdleTrack { Bias = rng.NextInt(5) - 2 }),
                };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                {
                    it.Clips.Add(new TypedClip
                    {
                        Type = typeof(KIdleClip),
                        Start = s,
                        End = e,
                        Data = LibraryGen.BytesOf(new KIdleClip { Code = 1 + rng.NextInt(300) }),
                    });
                }
                tl.Tracks.Add(it);
            }

            if (rng.NextInt(3) == 0)
            {
                tl.Markers.Add(new TypedMarker
                {
                    Type = typeof(KPulseMarker),
                    Tick = (uint)rng.NextInt((int)dur),
                    Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 1 + rng.NextInt(9) }),
                });
            }

            timelines[i] = tl;
        }

        var smallOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
        var largeOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
        for (var k = 0; k < FuzzKindNames.Length; k++)
        {
            smallOv[FuzzKindNames[k]] = new KindOverride(inline: 1, gamePulses: 1, maxInlineBytes: 8);
            largeOv[FuzzKindNames[k]] = new KindOverride(inline: 260, gamePulses: 64, maxInlineBytes: 2048);
        }

        var smallKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], smallOv);
        var largeKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], largeOv);
        var smallLib = LibraryGen.AssembleTyped(timelines, channels, manifest, smallKinds, 8, 1, smallOv);
        var largeLib = LibraryGen.AssembleTyped(timelines, channels, manifest, largeKinds, 2048, 1, largeOv);
        return (smallLib, largeLib);
    }

    static bool HasColdOverflow(KindStore store)
    {
        for (var k = 0; k < store.KindCount; k++)
            if (store.GetStats(k).Overflows > 0)
                return true;
        return false;
    }

    [Fact]
    public void SpillAndGrow_SmallInlineMatchesLargeNoSpillOnKindAndPlainWorlds()
    {
        FuzzRunner.Run("T6-CapacityAndSpill", 0x54360001UL, 18, (rng, iter) =>
        {
            var entities = 96 + rng.NextInt(420);
            var boss = rng.NextInt(entities);
            var timelines = 12 + rng.NextInt(16);
            var channels = 2 + rng.NextInt(3);

            var (smallKindLib, largeKindLib) = BuildPairedKindLibraries(ref rng, timelines, channels, 40);
            var observedSlotSpill = false;
            var observedColdOverflow = false;
            var observedBossUnspill = false;

            using (var wSmall = new TimelineWorld(smallKindLib.Bytes))
            using (var wLarge = new TimelineWorld(largeKindLib.Bytes))
            using (var pSmall = new PlaybackTable(entities, 1))
            using (var pLarge = new PlaybackTable(entities, 256))
            using (var ksSmall = new KindStore(wSmall, pSmall))
            using (var ksLarge = new KindStore(wLarge, pLarge))
            {
                var pulSmall = ksSmall.Pulses<int>(typeof(KCombineInt).FullName!);
                var pulLarge = ksLarge.Pulses<int>(typeof(KCombineInt).FullName!);

                var cSmall = new KindIntCol[entities];
                var cLarge = new KindIntCol[entities];
                var eSmall = new KindEachCol[entities];
                var eLarge = new KindEachCol[entities];
                var iSmall = new KindIdleCol[entities];
                var iLarge = new KindIdleCol[entities];
                var fSmall = new KindFloatCol[entities];
                var fLarge = new KindFloatCol[entities];
                var inp = new KindInputCol[entities];
                for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 + (e & 1) };

                var steps = 28;
                for (var step = 0; step < steps; step++)
                {
                    var burst = step == 0 || step == 14 ? 24 + rng.NextInt(40) : 6 + rng.NextInt(14);
                    for (var b = 0; b < burst; b++)
                    {
                        var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, smallKindLib.Durations);
                        op.Entity = boss;
                        if (step < 3 && op.Op is RequestActionKind.Reset or RequestActionKind.StopAll)
                            op.Op = RequestActionKind.Start;
                        LibraryGen.ApplyOp(pSmall, in op);
                        LibraryGen.ApplyOp(pLarge, in op);
                    }

                    if (step == 10 || step == 22)
                    {
                        pSmall.Reset(boss);
                        pLarge.Reset(boss);
                    }

                    var gamePulseBursts = 2 + rng.NextInt(5);
                    for (var g = 0; g < gamePulseBursts; g++)
                    {
                        var val = rng.NextInt(17) - 8;
                        pulSmall.To(boss, val);
                        pulLarge.To(boss, val);
                    }

                    var smallReqCount = Math.Max(4, entities / 6);
                    for (var r = 0; r < smallReqCount; r++)
                    {
                        var e = rng.NextInt(entities);
                        if (e == boss) continue;
                        var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, smallKindLib.Durations);
                        op.Entity = e;
                        LibraryGen.ApplyOp(pSmall, in op);
                        LibraryGen.ApplyOp(pLarge, in op);
                    }

                    var forward = step % 6 != 4;
                    wSmall.Tick(pSmall, forward);
                    wLarge.Tick(pLarge, forward);

                    new KindFuzzSystem { CombineCol = cSmall, EachCol = eSmall, IdleCol = iSmall, FloatCol = fSmall, Input = inp }.Run(wSmall, pSmall, forward);
                    new KindFuzzSystem { CombineCol = cLarge, EachCol = eLarge, IdleCol = iLarge, FloatCol = fLarge, Input = inp }.Run(wLarge, pLarge, forward);

                    if (pSmall.IsSpilled(boss)) observedSlotSpill = true;
                    if (step == 10 || step == 22)
                    {
                        if (!pSmall.IsSpilled(boss)) observedBossUnspill = true;
                    }
                    if (HasColdOverflow(ksSmall)) observedColdOverflow = true;

                    Assert.False(pLarge.IsSpilled(boss));

                    var pbErr = FuzzRunner.ComparePlaybacks(pSmall, pLarge);
                    if (pbErr != null)
                        FuzzRunner.Fail("T6-CapacityAndSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x54360001UL, iter, $"Kind step {step}: {pbErr}");

                    var err = FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cSmall), MemoryMarshal.AsBytes<KindIntCol>(cLarge), $"step {step} CombineCol")
                        ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(eSmall), MemoryMarshal.AsBytes<KindEachCol>(eLarge), $"step {step} EachCol")
                        ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(iSmall), MemoryMarshal.AsBytes<KindIdleCol>(iLarge), $"step {step} IdleCol")
                        ?? FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(fSmall), MemoryMarshal.AsBytes<KindFloatCol>(fLarge), $"step {step} FloatCol");
                    if (err != null)
                        FuzzRunner.Fail("T6-CapacityAndSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x54360001UL, iter, err);

                    var hSmall = TlStateHash.Begin();
                    fixed (KindIntCol* p = cSmall) hSmall.Bytes((byte*)p, cSmall.Length * sizeof(KindIntCol));
                    fixed (KindEachCol* p = eSmall) hSmall.Bytes((byte*)p, eSmall.Length * sizeof(KindEachCol));
                    fixed (KindIdleCol* p = iSmall) hSmall.Bytes((byte*)p, iSmall.Length * sizeof(KindIdleCol));
                    fixed (KindFloatCol* p = fSmall) hSmall.Bytes((byte*)p, fSmall.Length * sizeof(KindFloatCol));
                    FuzzRunner.MixPlaybacks(ref hSmall, pSmall);

                    var hLarge = TlStateHash.Begin();
                    fixed (KindIntCol* p = cLarge) hLarge.Bytes((byte*)p, cLarge.Length * sizeof(KindIntCol));
                    fixed (KindEachCol* p = eLarge) hLarge.Bytes((byte*)p, eLarge.Length * sizeof(KindEachCol));
                    fixed (KindIdleCol* p = iLarge) hLarge.Bytes((byte*)p, iLarge.Length * sizeof(KindIdleCol));
                    fixed (KindFloatCol* p = fLarge) hLarge.Bytes((byte*)p, fLarge.Length * sizeof(KindFloatCol));
                    FuzzRunner.MixPlaybacks(ref hLarge, pLarge);

                    Assert.Equal(hLarge.Value, hSmall.Value);
                }
            }

            Assert.True(observedSlotSpill);
            Assert.True(observedColdOverflow);
            Assert.True(observedBossUnspill);

            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 40);
            using var pwSmall = new TimelineWorld(plainLib.Bytes);
            using var pwLarge = new TimelineWorld(plainLib.Bytes);
            using var ppSmall = new PlaybackTable(entities, 1);
            using var ppLarge = new PlaybackTable(entities, 256);
            using var cuesSmall = new Events<FuzzCue>(16384);
            using var cuesLarge = new Events<FuzzCue>(16384);

            var mSmall = new PlainMotion[entities];
            var mLarge = new PlainMotion[entities];
            var sSmall = new PlainStats[entities];
            var sLarge = new PlainStats[entities];
            var pInp = new PlainInput[entities];
            for (var e = 0; e < entities; e++) pInp[e] = new PlainInput { Mult = 1 + (e & 1) };

            for (var step = 0; step < 20; step++)
            {
                var burst = step == 0 ? 32 : 8 + rng.NextInt(12);
                for (var b = 0; b < burst; b++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, plainLib.Durations);
                    op.Entity = boss;
                    LibraryGen.ApplyOp(ppSmall, in op);
                    LibraryGen.ApplyOp(ppLarge, in op);
                }
                if (step == 11)
                {
                    ppSmall.Reset(boss);
                    ppLarge.Reset(boss);
                }
                for (var r = 0; r < Math.Max(4, entities / 6); r++)
                {
                    var e = rng.NextInt(entities);
                    if (e == boss) continue;
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, plainLib.Durations);
                    op.Entity = e;
                    LibraryGen.ApplyOp(ppSmall, in op);
                    LibraryGen.ApplyOp(ppLarge, in op);
                }

                var forward = step % 5 != 3;
                pwSmall.ApplyRequests(ppSmall);
                pwLarge.ApplyRequests(ppLarge);

                new PlainFuzzSystem { Motion = mSmall, Stats = sSmall, Input = pInp, Requests = pwSmall.Requests(ppSmall), Cues = cuesSmall.Writer }.Run(pwSmall, ppSmall, forward);
                new PlainFuzzSystem { Motion = mLarge, Stats = sLarge, Input = pInp, Requests = pwLarge.Requests(ppLarge), Cues = cuesLarge.Writer }.Run(pwLarge, ppLarge, forward);

                pwSmall.Advance(ppSmall, forward);
                pwLarge.Advance(ppLarge, forward);

                var pbErr = FuzzRunner.ComparePlaybacks(ppSmall, ppLarge);
                Assert.True(pbErr == null, pbErr);

                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mSmall), MemoryMarshal.AsBytes<PlainMotion>(mLarge), $"plain step {step} Motion") is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(sSmall), MemoryMarshal.AsBytes<PlainStats>(sLarge), $"plain step {step} Stats") is null);

                var evSmall = cuesSmall.Drain();
                var evLarge = cuesLarge.Drain();
                Assert.Equal(evLarge.Length, evSmall.Length);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes(evSmall), MemoryMarshal.AsBytes(evLarge), $"plain step {step} Events") is null);
            }
        });
    }

    [Fact]
    public void BossEntity256SlotsAndUnspillShrink_MatchesNoSpillAcrossResetAndRegrow()
    {
        FuzzRunner.Run("T6-Boss256GrowResetRegrow", 0x54360002UL, 8, (rng, _) =>
        {
            const int timelines = 264;
            const int entities = 140;
            var boss = rng.NextInt(entities);

            var names = new string[timelines];
            for (var i = 0; i < timelines; i++) names[i] = $"btl_{i + 1:D3}";
            var channels = new[] { "c1" };
            var manifest = LibraryBake.Manifest(names, channels, 60);
            var typed = new TypedTimeline[timelines];
            for (var i = 0; i < timelines; i++)
            {
                var tl = new TypedTimeline
                {
                    Name = names[i],
                    Duration = 30,
                    Loops = (i & 1) == 0,
                    Hold = (i & 3) == 1,
                };
                var tr = new TypedTrack
                {
                    Name = "kc",
                    Type = typeof(KCombineTrack),
                    Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }),
                };
                tr.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = 0,
                    End = 30,
                    Data = LibraryGen.BytesOf(new KCombineClip { Delta = (i % 7) - 3, StartPulse = 1, EndPulse = -1 }),
                });
                tl.Tracks.Add(tr);
                typed[i] = tl;
            }

            var smallOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
            {
                [typeof(KCombineInt).FullName!] = new KindOverride(inline: 2, gamePulses: 2, maxInlineBytes: 16),
            };
            var largeOv = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
            {
                [typeof(KCombineInt).FullName!] = new KindOverride(inline: 260, gamePulses: 32, maxInlineBytes: 4096),
            };

            var smallKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], smallOv);
            var largeKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], largeOv);
            var smallLib = LibraryGen.AssembleTyped(typed, channels, manifest, smallKinds, 16, 1, smallOv);
            var largeLib = LibraryGen.AssembleTyped(typed, channels, manifest, largeKinds, 4096, 1, largeOv);

            using var wSmall = new TimelineWorld(smallLib.Bytes);
            using var wLarge = new TimelineWorld(largeLib.Bytes);
            using var pSmall = new PlaybackTable(entities, 2);
            using var pLarge = new PlaybackTable(entities, 256);
            using var ksSmall = new KindStore(wSmall, pSmall);
            using var ksLarge = new KindStore(wLarge, pLarge);

            var cSmall = new KindIntCol[entities];
            var cLarge = new KindIntCol[entities];
            var eSmall = new KindEachCol[entities];
            var eLarge = new KindEachCol[entities];
            var iSmall = new KindIdleCol[entities];
            var iLarge = new KindIdleCol[entities];
            var fSmall = new KindFloatCol[entities];
            var fLarge = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

            for (ushort tl = 1; tl <= 256; tl++)
            {
                pSmall.Start(boss, tl);
                pLarge.Start(boss, tl);
            }
            for (var e = 0; e < entities; e++)
            {
                if (e == boss || rng.NextInt(3) != 0) continue;
                var tl = (ushort)(1 + rng.NextInt(256));
                pSmall.Start(e, tl);
                pLarge.Start(e, tl);
            }

            wSmall.Tick(pSmall, true);
            wLarge.Tick(pLarge, true);
            new KindFuzzSystem { CombineCol = cSmall, EachCol = eSmall, IdleCol = iSmall, FloatCol = fSmall, Input = inp }.Run(wSmall, pSmall, true);
            new KindFuzzSystem { CombineCol = cLarge, EachCol = eLarge, IdleCol = iLarge, FloatCol = fLarge, Input = inp }.Run(wLarge, pLarge, true);

            Assert.Equal(256, pSmall.CountOf(boss));
            Assert.True(pSmall.IsSpilled(boss));
            Assert.False(pLarge.IsSpilled(boss));
            Assert.True(FuzzRunner.ComparePlaybacks(pSmall, pLarge) is null);
            Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cSmall), MemoryMarshal.AsBytes<KindIntCol>(cLarge), "256 CombineCol") is null);

            for (ushort tl = 3; tl <= 256; tl++)
            {
                pSmall.Stop(boss, tl);
                pLarge.Stop(boss, tl);
            }
            for (var t = 0; t < 3; t++)
            {
                wSmall.Tick(pSmall, true);
                wLarge.Tick(pLarge, true);
                new KindFuzzSystem { CombineCol = cSmall, EachCol = eSmall, IdleCol = iSmall, FloatCol = fSmall, Input = inp }.Run(wSmall, pSmall, true);
                new KindFuzzSystem { CombineCol = cLarge, EachCol = eLarge, IdleCol = iLarge, FloatCol = fLarge, Input = inp }.Run(wLarge, pLarge, true);
                Assert.True(FuzzRunner.ComparePlaybacks(pSmall, pLarge) is null);
                Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cSmall), MemoryMarshal.AsBytes<KindIntCol>(cLarge), "shrink CombineCol") is null);
            }
            var activeAfterStop = 0;
            for (var s = 0; s < pSmall.CountOf(boss); s++)
                if (pSmall.SlotsOf(boss)[s].Timeline != 0)
                    activeAfterStop++;
            Assert.Equal(2, activeAfterStop);

            for (ushort tl = 5; tl <= 140; tl++)
            {
                pSmall.Start(boss, tl);
                pLarge.Start(boss, tl);
            }
            wSmall.Tick(pSmall, true);
            wLarge.Tick(pLarge, true);
            new KindFuzzSystem { CombineCol = cSmall, EachCol = eSmall, IdleCol = iSmall, FloatCol = fSmall, Input = inp }.Run(wSmall, pSmall, true);
            new KindFuzzSystem { CombineCol = cLarge, EachCol = eLarge, IdleCol = iLarge, FloatCol = fLarge, Input = inp }.Run(wLarge, pLarge, true);
            Assert.True(FuzzRunner.ComparePlaybacks(pSmall, pLarge) is null);
            Assert.True(FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cSmall), MemoryMarshal.AsBytes<KindIntCol>(cLarge), "regrow CombineCol") is null);

            pSmall.Reset(boss);
            pLarge.Reset(boss);
            wSmall.Tick(pSmall, true);
            wLarge.Tick(pLarge, true);
            Assert.False(pSmall.IsSpilled(boss));
            Assert.True(FuzzRunner.ComparePlaybacks(pSmall, pLarge) is null);
        });
    }
}
