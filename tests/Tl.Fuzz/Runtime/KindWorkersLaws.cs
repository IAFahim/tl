using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class KindWorkersLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);
    static readonly int[] WorkerPalette = [1, 2, 4, 8, 16, 32];

    static GeneratedLibrary BuildCombinedKindAndGroupLibrary(ref FuzzRandom rng, int timelineCount, int channelCount, int maxDuration)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++) names[i] = $"b6_{i + 1:D3}";
        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++) channels[i] = $"b6c_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var timelines = new TypedTimeline[timelineCount];
        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(6 + rng.NextInt(maxDuration));
            var ch = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null;
            var mode = rng.NextInt(4);
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = ch,
                Loops = mode == 1,
                Hold = mode == 2,
                Next = mode == 3 ? names[rng.NextInt(timelineCount)] : null,
            };

            var trC = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(3) }) };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                trC.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = s, End = e, Data = LibraryGen.BytesOf(new KCombineClip { Delta = rng.NextInt(15) - 7, StartPulse = rng.NextInt(9), EndPulse = rng.NextInt(9) }) });
            tl.Tracks.Add(trC);

            if (rng.NextBool())
            {
                var trE = new TypedTrack { Name = "ke", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(2) }) };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                    trE.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = s, End = e, Data = LibraryGen.BytesOf(new KEachClip { Delta = rng.NextInt(13) - 6 }) });
                tl.Tracks.Add(trE);
            }

            if (rng.NextBool())
            {
                var trI = new TypedTrack { Name = "ki", Type = typeof(KIdleTrack), Data = LibraryGen.BytesOf(new KIdleTrack { Bias = rng.NextInt(5) - 2 }) };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                    trI.Clips.Add(new TypedClip { Type = typeof(KIdleClip), Start = s, End = e, Data = LibraryGen.BytesOf(new KIdleClip { Code = 10 + rng.NextInt(50) }) });
                tl.Tracks.Add(trI);
            }

            if (rng.NextBool())
            {
                var trF = new TypedTrack { Name = "kf", Type = typeof(KFloatTrack), Data = LibraryGen.BytesOf(new KFloatTrack { Scale = 0.5f + rng.NextInt(4) * 0.5f }) };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                    trF.Clips.Add(new TypedClip { Type = typeof(KFloatClip), Start = s, End = e, Data = LibraryGen.BytesOf(new KFloatClip { Delta = (rng.NextInt(11) - 5) * 0.25f }) });
                tl.Tracks.Add(trF);
            }

            if (rng.NextBool())
            {
                var trFp = new TypedTrack { Name = "fp", Type = typeof(FoldPulseTrack), Data = new byte[1] };
                foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                    trFp.Clips.Add(new TypedClip { Type = typeof(FoldPulseClip), Start = s, End = e, Data = LibraryGen.BytesOf(new FoldPulseClip { SpanVal = rng.NextInt(11) - 5, PulseVal = rng.NextInt(17) - 8 }) });
                tl.Tracks.Add(trFp);
            }

            var trGa = new TypedTrack { Name = "ga", Type = typeof(B3CombTrackA), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                trGa.Clips.Add(new TypedClip { Type = typeof(B3CombClipA), Start = s, End = e, Data = LibraryGen.BytesOf(new B3CombClipA { SpanVal = rng.NextInt(11) - 5, PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trGa);

            var trGb = new TypedTrack { Name = "gb", Type = typeof(B3CombTrackB), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                trGb.Clips.Add(new TypedClip { Type = typeof(B3CombClipB), Start = s, End = e, Data = LibraryGen.BytesOf(new B3CombClipB { SpanVal = rng.NextInt(11) - 5, PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trGb);

            var trGc = new TypedTrack { Name = "gc", Type = typeof(B3CombTrackC), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                trGc.Clips.Add(new TypedClip { Type = typeof(B3CombClipC), Start = s, End = e, Data = LibraryGen.BytesOf(new B3CombClipC { PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trGc);

            var trSa = new TypedTrack { Name = "sa", Type = typeof(B3SplitTrackA), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                trSa.Clips.Add(new TypedClip { Type = typeof(B3SplitClipA), Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipA { SpanVal = rng.NextInt(11) - 5, PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trSa);

            var trSb = new TypedTrack { Name = "sb", Type = typeof(B3SplitTrackB), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, true))
                trSb.Clips.Add(new TypedClip { Type = typeof(B3SplitClipB), Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipB { SpanVal = rng.NextInt(11) - 5, PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trSb);

            var trSc = new TypedTrack { Name = "sc", Type = typeof(B3SplitTrackC), Data = new byte[1] };
            foreach (var (s, e) in LibraryGen.GenerateTrackSpans(ref rng, dur, false))
                trSc.Clips.Add(new TypedClip { Type = typeof(B3SplitClipC), Start = s, End = e, Data = LibraryGen.BytesOf(new B3SplitClipC { PulseVal = rng.NextInt(15) - 7 }) });
            tl.Tracks.Add(trSc);

            var markers = 1 + rng.NextInt(Math.Min(4, (int)dur));
            for (var m = 0; m < markers; m++)
                tl.Markers.Add(new TypedMarker { Type = typeof(KPulseMarker), Tick = (uint)rng.NextInt((int)dur), Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 1 + rng.NextInt(13) }) });

            timelines[i] = tl;
        }

        return LibraryGen.AssembleTyped(timelines, channels, manifest, AllKinds);
    }

    sealed class WorkersRunState : IDisposable
    {
        public readonly TimelineWorld World;
        public readonly PlaybackTable Players;
        public readonly KindStore Store;
        public readonly KindIntCol[] Comb;
        public readonly KindEachCol[] Each;
        public readonly KindIdleCol[] Idle;
        public readonly KindFloatCol[] Flt;
        public readonly FoldPulseOut[] Folded;
        public readonly B3CombOutA[] CombA;
        public readonly B3CombOutB[] CombB;
        public readonly B3CombOutC[] CombC;
        public readonly B3SplitOutA[] SplitA;
        public readonly B3SplitOutB[] SplitB;
        public readonly B3SplitOutC[] SplitC;
        public readonly KindInputCol[] Input;
        public readonly KindPulses<int> CombPulses;
        public readonly KindPulses<int, IntAddFold> FoldPulses;
        public readonly KindPulses<int> CombAPulses;
        public readonly KindPulses<int> CombBPulses;
        public readonly KindPulses<int> CombCPulses;
        public readonly KindPulses<int> SplitAPulses;
        public readonly KindPulses<int> SplitBPulses;
        public readonly KindPulses<int> SplitCPulses;

        public WorkersRunState(byte[] libBytes, int entities, int slotsPerEntity, KindInputCol[] input)
        {
            World = new TimelineWorld(libBytes);
            Players = new PlaybackTable(entities, slotsPerEntity);
            Store = new KindStore(World, Players);
            Comb = new KindIntCol[entities];
            Each = new KindEachCol[entities];
            Idle = new KindIdleCol[entities];
            Flt = new KindFloatCol[entities];
            Folded = new FoldPulseOut[entities];
            CombA = new B3CombOutA[entities];
            CombB = new B3CombOutB[entities];
            CombC = new B3CombOutC[entities];
            SplitA = new B3SplitOutA[entities];
            SplitB = new B3SplitOutB[entities];
            SplitC = new B3SplitOutC[entities];
            Input = input;
            CombPulses = Store.Pulses<int>(typeof(KCombineInt).FullName!);
            FoldPulses = Store.Pulses<int, IntAddFold>(typeof(FoldedPulseKind).FullName!);
            CombAPulses = Store.Pulses<int>(typeof(B3CombKindA).FullName!);
            CombBPulses = Store.Pulses<int>(typeof(B3CombKindB).FullName!);
            CombCPulses = Store.Pulses<int>(typeof(B3CombKindC).FullName!);
            SplitAPulses = Store.Pulses<int>(typeof(B3SplitKindA).FullName!);
            SplitBPulses = Store.Pulses<int>(typeof(B3SplitKindB).FullName!);
            SplitCPulses = Store.Pulses<int>(typeof(B3SplitKindC).FullName!);
        }

        public void Step(bool fwd, TlWorkers? pool)
        {
            World.Tick(Players, fwd);
            var s0 = new KindFuzzSystem { CombineCol = Comb, EachCol = Each, IdleCol = Idle, FloatCol = Flt, Input = Input };
            var s1 = new FoldedPulseSystem { Out = Folded };
            var s2 = new B3CombSys { A = CombA, B = CombB, C = CombC };
            var s3 = new B3SplitSysA { A = SplitA };
            var s4 = new B3SplitSysB { B = SplitB };
            var s5 = new B3SplitSysC { C = SplitC };
            if (pool == null)
            {
                s0.Run(World, Players, fwd);
                s1.Run(World, Players, fwd);
                s2.Run(World, Players, fwd);
                s3.Run(World, Players, fwd);
                s4.Run(World, Players, fwd);
                s5.Run(World, Players, fwd);
            }
            else
            {
                s0.Run(World, Players, fwd, pool);
                s1.Run(World, Players, fwd, pool);
                s2.Run(World, Players, fwd, pool);
                s3.Run(World, Players, fwd, pool);
                s4.Run(World, Players, fwd, pool);
                s5.Run(World, Players, fwd, pool);
            }
        }

        public ulong StepHash()
        {
            var h = TlStateHash.Begin();
            fixed (KindIntCol* p0 = Comb) h.Bytes((byte*)p0, Comb.Length * sizeof(KindIntCol));
            fixed (KindEachCol* p1 = Each) h.Bytes((byte*)p1, Each.Length * sizeof(KindEachCol));
            fixed (KindIdleCol* p2 = Idle) h.Bytes((byte*)p2, Idle.Length * sizeof(KindIdleCol));
            fixed (KindFloatCol* p3 = Flt) h.Bytes((byte*)p3, Flt.Length * sizeof(KindFloatCol));
            fixed (FoldPulseOut* p4 = Folded) h.Bytes((byte*)p4, Folded.Length * sizeof(FoldPulseOut));
            fixed (B3CombOutA* p5 = CombA) h.Bytes((byte*)p5, CombA.Length * sizeof(B3CombOutA));
            fixed (B3CombOutB* p6 = CombB) h.Bytes((byte*)p6, CombB.Length * sizeof(B3CombOutB));
            fixed (B3CombOutC* p7 = CombC) h.Bytes((byte*)p7, CombC.Length * sizeof(B3CombOutC));
            fixed (B3SplitOutA* p8 = SplitA) h.Bytes((byte*)p8, SplitA.Length * sizeof(B3SplitOutA));
            fixed (B3SplitOutB* p9 = SplitB) h.Bytes((byte*)p9, SplitB.Length * sizeof(B3SplitOutB));
            fixed (B3SplitOutC* p10 = SplitC) h.Bytes((byte*)p10, SplitC.Length * sizeof(B3SplitOutC));
            h.Int((int)Store.DroppedPulses);
            FuzzRunner.MixPlaybacks(ref h, Players);
            return h.Value;
        }

        public string? CompareTo(WorkersRunState other, string label)
            => FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(Comb), MemoryMarshal.AsBytes<KindIntCol>(other.Comb), $"{label} Comb") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(Each), MemoryMarshal.AsBytes<KindEachCol>(other.Each), $"{label} Each") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(Idle), MemoryMarshal.AsBytes<KindIdleCol>(other.Idle), $"{label} Idle") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(Flt), MemoryMarshal.AsBytes<KindFloatCol>(other.Flt), $"{label} Flt") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<FoldPulseOut>(Folded), MemoryMarshal.AsBytes<FoldPulseOut>(other.Folded), $"{label} Folded") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3CombOutA>(CombA), MemoryMarshal.AsBytes<B3CombOutA>(other.CombA), $"{label} CombA") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3CombOutB>(CombB), MemoryMarshal.AsBytes<B3CombOutB>(other.CombB), $"{label} CombB") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3CombOutC>(CombC), MemoryMarshal.AsBytes<B3CombOutC>(other.CombC), $"{label} CombC") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3SplitOutA>(SplitA), MemoryMarshal.AsBytes<B3SplitOutA>(other.SplitA), $"{label} SplitA") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3SplitOutB>(SplitB), MemoryMarshal.AsBytes<B3SplitOutB>(other.SplitB), $"{label} SplitB") ??
               FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<B3SplitOutC>(SplitC), MemoryMarshal.AsBytes<B3SplitOutC>(other.SplitC), $"{label} SplitC") ??
               FuzzRunner.ComparePlaybacks(Players, other.Players) ??
               (Store.DroppedPulses != other.Store.DroppedPulses ? $"{label} Dropped {Store.DroppedPulses} != {other.Store.DroppedPulses}" : null);

        public void Dispose()
        {
            Store.Dispose();
            Players.Dispose();
            World.Dispose();
        }
    }

    [Fact]
    public void KindAndGroupedWorkers_From1To32WithGamePulsesAndReload_MatchSequentialEveryTick()
    {
        FuzzRunner.Run("B6-KindWorkersParity", 0x42360001UL, 10, (rng, iter) =>
        {
            var entities = 96 + rng.NextInt(256);
            var slotsPerEntity = 2 + rng.NextInt(6);
            var timelines = 3 + rng.NextInt(4);
            var channels = 1 + rng.NextInt(3);
            var steps = 20 + rng.NextInt(20);
            var reloadStep = steps / 2;

            var lib1 = BuildCombinedKindAndGroupLibrary(ref rng, timelines, channels, 24);
            var lib2 = BuildCombinedKindAndGroupLibrary(ref rng, timelines, channels, 24);

            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) input[e] = new KindInputCol { Factor = 1 + rng.NextInt(3) };

            var ops = new List<FuzzRequestOp>();
            var opsPerStep = new int[steps];
            var pulses = new List<(int Entity, int Kind, int Value)>();
            var pulsesPerStep = new int[steps];
            var forwards = new bool[steps];

            for (var s = 0; s < steps; s++)
            {
                forwards[s] = rng.NextInt(5) != 0;
                var nOps = s == 0 ? Math.Min(entities, 140) : 1 + rng.NextInt(28);
                opsPerStep[s] = nOps;
                for (var i = 0; i < nOps; i++)
                    ops.Add(LibraryGen.NextRequest(ref rng, entities, timelines, channels, lib1.Durations));

                var nP = rng.NextInt(32);
                pulsesPerStep[s] = nP;
                for (var i = 0; i < nP; i++)
                    pulses.Add((rng.NextInt(entities), rng.NextInt(8), rng.NextInt(21) - 10));
            }

            var workerChoices = new int[4];
            workerChoices[0] = WorkerPalette[iter % WorkerPalette.Length];
            workerChoices[1] = WorkerPalette[(iter + 3) % WorkerPalette.Length];
            workerChoices[2] = 1 + rng.NextInt(32);
            workerChoices[3] = 32;

            for (var wi = 0; wi < workerChoices.Length; wi++)
            {
                var wCount = workerChoices[wi];
                using var seqState = new WorkersRunState(lib1.Bytes, entities, slotsPerEntity, input);
                using var parState = new WorkersRunState(lib1.Bytes, entities, slotsPerEntity, input);
                using var pool = new TlWorkers(wCount);

                var opIdx = 0;
                var pIdx = 0;
                for (var s = 0; s < steps; s++)
                {
                    if (s == reloadStep)
                    {
                        seqState.World.Reload(lib2.Bytes);
                        parState.World.Reload(lib2.Bytes);
                    }
                    for (var i = 0; i < opsPerStep[s]; i++)
                    {
                        var op = ops[opIdx++];
                        LibraryGen.ApplyOp(seqState.Players, in op);
                        LibraryGen.ApplyOp(parState.Players, in op);
                    }
                    for (var i = 0; i < pulsesPerStep[s]; i++)
                    {
                        var p = pulses[pIdx++];
                        switch (p.Kind)
                        {
                            case 0: seqState.CombPulses.To(p.Entity, p.Value); parState.CombPulses.To(p.Entity, p.Value); break;
                            case 1: seqState.FoldPulses.To(p.Entity, p.Value); parState.FoldPulses.To(p.Entity, p.Value); break;
                            case 2: seqState.CombAPulses.To(p.Entity, p.Value); parState.CombAPulses.To(p.Entity, p.Value); break;
                            case 3: seqState.CombBPulses.To(p.Entity, p.Value); parState.CombBPulses.To(p.Entity, p.Value); break;
                            case 4: seqState.CombCPulses.To(p.Entity, p.Value); parState.CombCPulses.To(p.Entity, p.Value); break;
                            case 5: seqState.SplitAPulses.To(p.Entity, p.Value); parState.SplitAPulses.To(p.Entity, p.Value); break;
                            case 6: seqState.SplitBPulses.To(p.Entity, p.Value); parState.SplitBPulses.To(p.Entity, p.Value); break;
                            default: seqState.SplitCPulses.To(p.Entity, p.Value); parState.SplitCPulses.To(p.Entity, p.Value); break;
                        }
                    }

                    seqState.Step(forwards[s], null);
                    parState.Step(forwards[s], pool);

                    if (seqState.StepHash() != parState.StepHash())
                        FuzzRunner.Fail("B6-KindWorkersParity", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42360001UL, iter, $"w={wCount} step={s} hash mismatch");
                    var err = seqState.CompareTo(parState, $"w={wCount} step={s}");
                    if (err != null)
                        FuzzRunner.Fail("B6-KindWorkersParity", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42360001UL, iter, err);
                }
            }
        });
    }

    [Fact(Skip = "#642")]
    public void KindAndGroupedWorkers_WarmTicksAndPooledRuns_AllocateZeroBytesAfterWarmup()
    {
        FuzzRunner.Run("B6-KindWorkersZeroAlloc", 0x42360002UL, 6, (rng, iter) =>
        {
            const int entities = 128;
            const int timelines = 4;
            const int channels = 2;
            var workerCount = WorkerPalette[iter % WorkerPalette.Length];
            var lib = BuildCombinedKindAndGroupLibrary(ref rng, timelines, channels, 30);
            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) input[e] = new KindInputCol { Factor = 1 };

            using var state = new WorkersRunState(lib.Bytes, entities, 8, input);
            using var pool = new TlWorkers(workerCount);

            for (var warm = 0; warm < 40; warm++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + warm) % timelines));
                    state.Players.Start(e, tl, (ushort)(warm & 3));
                    state.Players.SetSpeed(e, tl, (ushort)(16 + (warm & 1) * 16));
                    if ((warm & 3) == 0)
                    {
                        state.CombPulses.To(e, warm);
                        state.FoldPulses.To(e, warm);
                        state.CombAPulses.To(e, warm);
                        state.CombBPulses.To(e, warm);
                        state.CombCPulses.To(e, warm);
                    }
                    if ((warm & 7) == 7) state.Players.Reset(e);
                }
                state.Step((warm & 3) != 3, pool);
            }

            for (var e = 0; e < entities; e++) state.Players.Reset(e);
            state.World.Tick(state.Players, true);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 32; step++)
            {
                for (var e = 0; e < entities; e++)
                {
                    var tl = (ushort)(1 + ((e + step) % timelines));
                    state.Players.Start(e, tl, (ushort)(step & 3));
                    state.Players.Seek(e, tl, (ushort)(step + 2));
                    state.Players.Pause(e, tl);
                    state.Players.Resume(e, tl);
                    state.Players.Reverse(e, tl, (step & 1) != 0);
                    state.Players.SetSpeed(e, tl, (ushort)(16 + (step & 1) * 16));
                    state.CombPulses.To(e, (step & 7) - 3);
                    state.FoldPulses.To(e, (step & 7) - 3);
                    state.CombAPulses.To(e, (step & 3) - 1);
                    state.CombBPulses.To(e, (step & 3) - 1);
                    state.CombCPulses.To(e, (step & 3) - 1);
                    if ((step & 7) == 7) state.Players.Reset(e);
                }
                state.Step((step & 3) != 3, pool);
            }
            var alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0L, alloc);
        });
    }
}
