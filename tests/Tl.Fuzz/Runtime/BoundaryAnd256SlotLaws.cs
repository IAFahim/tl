using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class BoundaryAnd256SlotLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    static GeneratedLibrary Build256SlotLibrary(ref FuzzRandom rng, bool includeKinds, ulong altSeedOffset = 0)
    {
        const int totalCount = 264;
        var names = new string[totalCount];
        for (var i = 0; i < totalCount; i++) names[i] = $"b4_{i + 1:D3}";
        var channels = new[] { "ch_rep", "ch_alt" };
        var manifest = LibraryBake.Manifest(names, channels, 60);

        var r = new FuzzRandom((((ulong)rng.NextUInt() << 32) | rng.NextUInt()) ^ altSeedOffset);
        var tls = new TypedTimeline[totalCount];
        for (var i = 0; i < totalCount; i++)
        {
            uint dur;
            string? ch = null;
            var loops = false;
            var hold = false;
            string? next = null;

            if (i < 254)
            {
                dur = (uint)(6 + r.NextInt(20));
                loops = (i & 3) == 1;
                hold = (i & 3) == 2;
            }
            else if (i == 254)
            {
                dur = 3;
                next = names[260];
            }
            else if (i == 255)
            {
                dur = 12;
                loops = true;
            }
            else if (i is 256 or 257)
            {
                dur = 16;
                ch = channels[0];
                loops = true;
            }
            else if (i is 258 or 259)
            {
                dur = 14;
                ch = channels[1];
                loops = true;
            }
            else
            {
                dur = (uint)(4 + r.NextInt(12));
                loops = true;
            }

            var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch, Loops = loops, Hold = hold, Next = next };
            if (includeKinds)
            {
                var tr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
                tr.Clips.Add(new TypedClip
                {
                    Type = typeof(KCombineClip),
                    Start = 0,
                    End = dur,
                    Data = LibraryGen.BytesOf(new KCombineClip { Delta = (i & 7) + 1, StartPulse = 2, EndPulse = 3 }),
                });
                tl.Tracks.Add(tr);
            }
            else
            {
                var tr = new TypedTrack { Name = "pl", Type = typeof(FuzzPlainTrack), Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 }) };
                tr.Clips.Add(new TypedClip
                {
                    Type = typeof(FuzzPlainClip),
                    Start = 0,
                    End = dur,
                    Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = (i & 7) + 1, Tag = i, TriggerTimeline = 0 }),
                });
                tl.Tracks.Add(tr);
            }
            tls[i] = tl;
        }

        return LibraryGen.AssembleTyped(tls, channels, manifest, includeKinds ? AllKinds : null);
    }

    static byte[] SnapshotEntitySlots(PlaybackTable table, int entity)
    {
        var count = table.CountOf(entity);
        var ptr = table.SlotsOf(entity);
        var bytes = new byte[4 + count * sizeof(Playback)];
        MemoryMarshal.Write(bytes.AsSpan(0, 4), in count);
        if (count > 0)
            new ReadOnlySpan<byte>(ptr, count * sizeof(Playback)).CopyTo(bytes.AsSpan(4));
        return bytes;
    }

    static Playback* FindByTimeline(PlaybackTable table, int entity, ushort timeline)
    {
        var count = table.CountOf(entity);
        var ptr = table.SlotsOf(entity);
        for (var i = 0; i < count; i++)
            if (ptr[i].Timeline == timeline)
                return ptr + i;
        return null;
    }

    static int CountLiveSlots(PlaybackTable table, int entity)
    {
        var count = table.CountOf(entity);
        var ptr = table.SlotsOf(entity);
        var live = 0;
        for (var i = 0; i < count; i++)
            if (ptr[i].Timeline != 0 && (ptr[i].Flags & PlaybackFlags.Stopping) == 0)
                live++;
        return live;
    }

    [Fact]
    public void FullRow255And256_StartChannelReplaceNextHandoverRebindAndReset_PlainVsKindStore()
    {
        FuzzRunner.Run("B4-Full256SlotRow", 0x42340001UL, 10, (rng, iter) =>
        {
            var libP1 = Build256SlotLibrary(ref rng, includeKinds: false, altSeedOffset: 0);
            var libP2 = Build256SlotLibrary(ref rng, includeKinds: false, altSeedOffset: 0x99UL);
            var libK1 = Build256SlotLibrary(ref rng, includeKinds: true, altSeedOffset: 0);
            var libK2 = Build256SlotLibrary(ref rng, includeKinds: true, altSeedOffset: 0x99UL);

            const int entities = 3;
            using var wP = new TimelineWorld(libP1.Bytes);
            using var pP = new PlaybackTable(entities, 4);
            using var cuesP = new Events<FuzzCue>(1024);

            using var wK = new TimelineWorld(libK1.Bytes);
            using var pK = new PlaybackTable(entities, 4);
            using var ks = new KindStore(wK, pK);

            var motion = new PlainMotion[entities];
            var stats = new PlainStats[entities];
            var plainIn = new PlainInput[entities];
            for (var e = 0; e < entities; e++) plainIn[e] = new PlainInput { Mult = 1 };

            var kComb = new KindIntCol[entities];
            var kEach = new KindEachCol[entities];
            var kIdle = new KindIdleCol[entities];
            var kFlt = new KindFloatCol[entities];
            var kIn = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) kIn[e] = new KindInputCol { Factor = 1 };

            void StepAll(bool fwd = true)
            {
                wP.ApplyRequests(pP);
                new PlainFuzzSystem { Motion = motion, Stats = stats, Input = plainIn, Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(wP, pP, fwd);
                wP.Advance(pP, fwd);
                cuesP.Drain();

                wK.Tick(pK, fwd);
                new KindFuzzSystem { CombineCol = kComb, EachCol = kEach, IdleCol = kIdle, FloatCol = kFlt, Input = kIn }.Run(wK, pK, fwd);
            }

            pP.Start(1, 5);
            pK.Start(1, 5);
            for (var t = 1; t <= 254; t++)
            {
                pP.Start(0, (ushort)t);
                pK.Start(0, (ushort)t);
            }
            pP.Start(0, 257);
            pK.Start(0, 257);
            StepAll();

            Assert.Equal(255, pP.CountOf(0));
            Assert.Equal(255, pK.CountOf(0));

            pP.Start(0, 258);
            pK.Start(0, 258);
            StepAll();
            StepAll();
            Assert.Equal(255, CountLiveSlots(pP, 0));
            Assert.Equal(255, CountLiveSlots(pK, 0));
            Assert.Equal(255, pP.CountOf(0));
            Assert.True(FindByTimeline(pP, 0, 258) != null);
            Assert.True(FindByTimeline(pK, 0, 258) != null);
            Assert.True(FindByTimeline(pP, 0, 257) == null);
            Assert.True(FindByTimeline(pK, 0, 257) == null);

            pP.Start(0, 255);
            pK.Start(0, 255);
            StepAll();
            Assert.Equal(256, CountLiveSlots(pP, 0));
            Assert.Equal(256, CountLiveSlots(pK, 0));
            Assert.Equal(256, pP.CountOf(0));
            Assert.Equal(256, pK.CountOf(0));

            var bystanderBeforeP = SnapshotEntitySlots(pP, 1);
            var bystanderBeforeK = SnapshotEntitySlots(pK, 1);
            var rowBeforeRefuseP = SnapshotEntitySlots(pP, 0);
            var rowBeforeRefuseK = SnapshotEntitySlots(pK, 0);

            pP.Start(0, 259);
            pK.Start(0, 259);
            wP.ApplyRequests(pP);
            wK.ApplyRequests(pK);

            var errRefP = FuzzRunner.CompareBytes(rowBeforeRefuseP, SnapshotEntitySlots(pP, 0), "Plain full start refuse");
            var errRefK = FuzzRunner.CompareBytes(rowBeforeRefuseK, SnapshotEntitySlots(pK, 0), "Kind full start refuse");
            if (errRefP != null || errRefK != null)
                FuzzRunner.Fail("B4-Full256SlotRow", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42340001UL, iter, errRefP ?? errRefK!);
            Assert.Null(FuzzRunner.CompareBytes(bystanderBeforeP, SnapshotEntitySlots(pP, 1), "Plain bystander before step"));
            Assert.Null(FuzzRunner.CompareBytes(bystanderBeforeK, SnapshotEntitySlots(pK, 1), "Kind bystander before step"));

            pP.Start(0, 257);
            pK.Start(0, 257);
            StepAll();

            var oldSlotKind = FindByTimeline(pK, 0, 258);
            var newSlotKind = FindByTimeline(pK, 0, 257);
            if (oldSlotKind == null || (oldSlotKind->Flags & PlaybackFlags.Stopping) != 0 || newSlotKind != null)
                FuzzRunner.Fail("B4-Full256SlotRow", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42340001UL, iter,
                    "KindStore did not refuse channel replace at 256 slots cleanly (#631)");
            Assert.Equal(256, pK.CountOf(0));
            Assert.True(FindByTimeline(pP, 0, 257) != null);

            Assert.Equal(1, pP.CountOf(1));
            Assert.Equal(1, pK.CountOf(1));
            Assert.Equal((ushort)5, pP.SlotsOf(1)[0].Timeline);
            Assert.Equal((ushort)5, pK.SlotsOf(1)[0].Timeline);
            Assert.Equal((ushort)(bystanderBeforeP[6] + 1), pP.SlotsOf(1)[0].Tick);
            Assert.Equal((ushort)(bystanderBeforeK[6] + 1), pK.SlotsOf(1)[0].Tick);

            for (var s = 0; s < 4; s++)
                StepAll();

            Assert.True(FindByTimeline(pK, 0, 255) == null);
            Assert.True(FindByTimeline(pK, 0, 261) != null);
            Assert.True(FindByTimeline(pP, 0, 255) == null);
            Assert.True(FindByTimeline(pP, 0, 261) != null);
            Assert.True(pP.CountOf(0) <= 256);
            Assert.True(pK.CountOf(0) <= 256);

            wP.Reload(libP1.Bytes);
            wK.Reload(libK1.Bytes);
            StepAll();
            wP.Reload(libP2.Bytes);
            wK.Reload(libK2.Bytes);
            StepAll();
            Assert.True(pP.CountOf(0) <= 256);
            Assert.True(pK.CountOf(0) <= 256);

            pP.Reset(0);
            pK.Reset(0);
            StepAll();
            Assert.Equal(0, pP.CountOf(0));
            Assert.Equal(0, pK.CountOf(0));

            pP.Start(0, 256);
            pK.Start(0, 256);
            StepAll();
            Assert.Equal(1, pP.CountOf(0));
            Assert.Equal(1, pK.CountOf(0));
            Assert.True(FindByTimeline(pP, 0, 256) != null);
            Assert.True(FindByTimeline(pK, 0, 256) != null);

            if (iter < 4 && rng.NextBool())
            {
                pP.Start(2, 1);
                pK.Start(2, 1);
                StepAll();
                Assert.True(pP.IsPlaying(2, 1));
                Assert.True(pK.IsPlaying(2, 1));
            }
        });
    }

    [Fact]
    public void EntityCapacity1And100000_EdgeSizesOperateCorrectlyAndMatchWorkers()
    {
        FuzzRunner.Run("B4-CapacityEdges", 0x42340002UL, 3, (rng, iter) =>
        {
            var lib = Build256SlotLibrary(ref rng, includeKinds: true, altSeedOffset: 0x33UL);
            var libPlain = Build256SlotLibrary(ref rng, includeKinds: false, altSeedOffset: 0x33UL);

            using (var w1 = new TimelineWorld(lib.Bytes))
            using (var p1 = new PlaybackTable(1, 2))
            using (var k1 = new KindStore(w1, p1))
            {
                var c = new KindIntCol[1];
                var e = new KindEachCol[1];
                var i = new KindIdleCol[1];
                var f = new KindFloatCol[1];
                var inp = new[] { new KindInputCol { Factor = 2 } };
                var pw = k1.Pulses<int>(typeof(KCombineInt).FullName!);

                for (var s = 0; s < 20; s++)
                {
                    var tl = (ushort)(1 + rng.NextInt(12));
                    if (s % 5 == 4) p1.Reset(0);
                    else if (s % 2 == 0) p1.Start(0, tl);
                    else p1.SetSpeed(0, tl, (ushort)(8 + rng.NextInt(48)));
                    pw.To(0, s + 1);
                    w1.Tick(p1, s % 4 != 3);
                    new KindFuzzSystem { CombineCol = c, EachCol = e, IdleCol = i, FloatCol = f, Input = inp }.Run(w1, p1, s % 4 != 3);
                    if (s == 10) w1.Reload(lib.Bytes);
                }
            }

            const int big = 100_000;
            int[] probeEntities = [0, 1, 63, 64, 65, 127, 128, 4095, 4096, 65535, 65536, 99998, 99999];

            using (var wBig = new TimelineWorld(libPlain.Bytes))
            using (var pBig = new PlaybackTable(big, 2))
            using (var cues = new Events<FuzzCue>(1 << 12))
            {
                var mot = new PlainMotion[big];
                var sta = new PlainStats[big];
                var inp = new PlainInput[big];
                for (var idx = 0; idx < probeEntities.Length; idx++)
                {
                    var ent = probeEntities[idx];
                    inp[ent] = new PlainInput { Mult = 1 + (idx & 3) };
                    pBig.Start(ent, (ushort)(1 + idx));
                    if ((idx & 1) == 0) pBig.SetSpeed(ent, (ushort)(1 + idx), 32);
                }
                wBig.ApplyRequests(pBig);
                new PlainFuzzSystem { Motion = mot, Stats = sta, Input = inp, Requests = wBig.Requests(pBig), Cues = cues.Writer }.Run(wBig, pBig, true);
                wBig.Advance(pBig, true);
                cues.Drain();
                wBig.ApplyRequests(pBig);
                new PlainFuzzSystem { Motion = mot, Stats = sta, Input = inp, Requests = wBig.Requests(pBig), Cues = cues.Writer }.Run(wBig, pBig, true);
                wBig.Advance(pBig, true);
                cues.Drain();

                for (var idx = 0; idx < probeEntities.Length; idx++)
                    Assert.NotEqual(0, mot[probeEntities[idx]].X);
            }

            (byte[] Comb, byte[] Each) RunBigKind(int workerCount)
            {
                using var w = new TimelineWorld(lib.Bytes);
                using var p = new PlaybackTable(big, 2);
                using var k = new KindStore(w, p);
                using var pool = workerCount > 0 ? new TlWorkers(workerCount) : null;
                var c = new KindIntCol[big];
                var eCol = new KindEachCol[big];
                var idle = new KindIdleCol[big];
                var flt = new KindFloatCol[big];
                var inp = new KindInputCol[big];
                var pw = k.Pulses<int>(typeof(KCombineInt).FullName!);
                for (var idx = 0; idx < probeEntities.Length; idx++)
                {
                    var ent = probeEntities[idx];
                    inp[ent] = new KindInputCol { Factor = 1 + (idx & 3) };
                    p.Start(ent, (ushort)(1 + idx));
                    if ((idx & 1) == 0) p.SetSpeed(ent, (ushort)(1 + idx), 32);
                    pw.To(ent, 10 + idx);
                }
                for (var step = 0; step < 3; step++)
                {
                    w.Tick(p, true);
                    var sys = new KindFuzzSystem { CombineCol = c, EachCol = eCol, IdleCol = idle, FloatCol = flt, Input = inp };
                    if (pool == null) sys.Run(w, p, true);
                    else sys.Run(w, p, true, pool);
                }
                var combSnap = new byte[probeEntities.Length * 4];
                var eachSnap = new byte[probeEntities.Length * 4];
                for (var idx = 0; idx < probeEntities.Length; idx++)
                {
                    var ent = probeEntities[idx];
                    MemoryMarshal.Write(combSnap.AsSpan(idx * 4, 4), in c[ent].Sum);
                    MemoryMarshal.Write(eachSnap.AsSpan(idx * 4, 4), in eCol[ent].Sum);
                }
                return (combSnap, eachSnap);
            }

            var seq = RunBigKind(0);
            var par = RunBigKind(4);
            var d1 = FuzzRunner.CompareBytes(seq.Comb, par.Comb, "100k Comb");
            var d2 = FuzzRunner.CompareBytes(seq.Each, par.Each, "100k Each");
            if (d1 != null || d2 != null)
                FuzzRunner.Fail("B4-CapacityEdges", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42340002UL, iter, d1 ?? d2!);
        });
    }

    sealed class Domain64Context : AssemblyLoadContext
    {
        public Domain64Context() : base("B4_64Kinds", isCollectible: true) { }
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    static readonly Lazy<(byte[] Pe, byte[] LibBytes)> Domain64 = new(Build64KindFixture, LazyThreadSafetyMode.ExecutionAndPublication);

    static (byte[] Pe, byte[] LibBytes) Build64KindFixture()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Tl;");
        sb.AppendLine("namespace Dom64;");
        for (var i = 0; i < 64; i++)
        {
            sb.AppendLine($"public struct O{i:D2} {{ public int V; }}");
            sb.AppendLine($"public struct C{i:D2} {{ public int D; }}");
            sb.AppendLine($"public readonly struct T{i:D2} : IBlend<C{i:D2}> {{ public void Blend(in C{i:D2} a, in C{i:D2} b, float t, out C{i:D2} r) => r = a; }}");
            sb.AppendLine($"[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)] public struct K{i:D2} {{ public static void Combine(ref int t, in int n) => t += n; public static void Apply(in int t, ref O{i:D2} c) => c.V += t; }}");
        }
        for (var sIdx = 0; sIdx < 3; sIdx++)
        {
            var start = sIdx * 24;
            var end = Math.Min(64, start + 24);
            sb.Append($"[TimelineSystem] public ref partial struct Sys{sIdx} {{ ");
            for (var i = start; i < end; i++) sb.Append($"public Span<O{i:D2}> C{i:D2}; ");
            sb.AppendLine("}");
            for (var i = start; i < end; i++)
                sb.AppendLine($"[TimelineSystemMember(typeof(Sys{sIdx}))] public readonly struct Con{i:D2} : ITrack<T{i:D2}, C{i:D2}> {{ [Feeds(typeof(K{i:D2}))] public static int E(in Frame<T{i:D2}, C{i:D2}> f) => f.Direction * f.Clip.D; }}");
        }
        sb.AppendLine("public static class Host64 {");
        sb.AppendLine("    public static (int KindCount, int LastVal, int Bit0Val, int HighPulseVal, long Dropped) Run(byte[] lib, bool usePartialReach) {");
        sb.AppendLine("        var names = new string[64]; for (var i = 0; i < 64; i++) names[i] = $\"Dom64.K{i:D2}\";");
        sb.AppendLine("        var reach = usePartialReach ? new[] { (1ul << 0) | (1ul << 63), ~0ul } : null;");
        sb.AppendLine("        using var w = new TimelineWorld(lib); using var p = new PlaybackTable(2, 4); using var ks = usePartialReach ? new KindStore(w, p, names, reach) : new KindStore(w, p);");
        sb.AppendLine("        if (ks.KindCount != 64) throw new InvalidOperationException($\"expected 64 kinds got {ks.KindCount}\");");
        for (var i = 0; i < 64; i++) sb.Append($"        var o{i:D2} = new O{i:D2}[2]; ");
        sb.AppendLine();
        sb.AppendLine("        var q0 = ks.Pulses<int>(\"Dom64.K00\"); var q63 = ks.Pulses<int>(\"Dom64.K63\"); var q32 = ks.Pulses<int>(\"Dom64.K32\");");
        sb.AppendLine("        p.Start(0, 2); p.Start(1, 1);");
        sb.AppendLine("        q0.To(0, 100); q63.To(0, 700); q32.To(0, 55); q63.To(1, 300);");
        sb.AppendLine("        for (var s = 0; s < 3; s++) {");
        sb.AppendLine("            w.Tick(p, true);");
        for (var sIdx = 0; sIdx < 3; sIdx++)
        {
            var start = sIdx * 24;
            var end = Math.Min(64, start + 24);
            sb.Append($"            new Sys{sIdx} {{ ");
            for (var i = start; i < end; i++) sb.Append($"C{i:D2} = o{i:D2}, ");
            sb.AppendLine("}.Run(w, p, true);");
        }
        sb.AppendLine("        }");
        sb.AppendLine("        w.Reload(lib);");
        sb.AppendLine("        w.Tick(p, true);");
        for (var sIdx = 0; sIdx < 3; sIdx++)
        {
            var start = sIdx * 24;
            var end = Math.Min(64, start + 24);
            sb.Append($"        new Sys{sIdx} {{ ");
            for (var i = start; i < end; i++) sb.Append($"C{i:D2} = o{i:D2}, ");
            sb.AppendLine("}.Run(w, p, true);");
        }
        sb.AppendLine("        return (ks.KindCount, o63[1].V, o00[0].V, o63[0].V, ks.DroppedPulses);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var refs = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            string loc;
            try { loc = asm.Location; }
            catch { continue; }
            if (loc.Length > 0 && seen.Add(loc)) refs.Add(MetadataReference.CreateFromFile(loc));
        }
        foreach (var t in new[] { typeof(object), typeof(TimelineWorld), typeof(ITrack<,>), typeof(Unsafe), typeof(MemoryMarshal) })
        {
            var loc = t.Assembly.Location;
            if (loc.Length > 0 && seen.Add(loc)) refs.Add(MetadataReference.CreateFromFile(loc));
        }

        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var comp = CSharpCompilation.Create(
            "B4_Dom64",
            [CSharpSyntaxTree.ParseText(sb.ToString(), parse, "Dom64.cs")],
            refs.ToImmutable(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        var gen = new TimelineIncrementalGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([gen], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out var outComp, out _);
        using var peStream = new MemoryStream();
        var emit = outComp.Emit(peStream);
        if (!emit.Success)
            throw new InvalidOperationException("64-kind compile failed: " + string.Join("; ", emit.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        var pe = peStream.ToArray();

        var ctx = new Domain64Context();
        try
        {
            var loaded = ctx.LoadFromStream(new MemoryStream(pe));
            var names = new[] { "tl_all64", "tl_high63" };
            var channels = new[] { "ch" };
            var man = LibraryBake.Manifest(names, channels, 60);
            var tlAll = new TypedTimeline { Name = names[0], Duration = 10, Loops = true };
            for (var i = 0; i < 64; i++)
            {
                var trT = loaded.GetType($"Dom64.T{i:D2}", true)!;
                var clT = loaded.GetType($"Dom64.C{i:D2}", true)!;
                var tr = new TypedTrack { Name = $"tr{i:D2}", Type = trT, Data = new byte[1] };
                tr.Clips.Add(new TypedClip { Type = clT, Start = 0, End = 10, Data = BitConverter.GetBytes(i + 1) });
                tlAll.Tracks.Add(tr);
            }
            var tlEdge = new TypedTimeline { Name = names[1], Duration = 10, Loops = true };
            foreach (var kIdx in new[] { 0, 63 })
            {
                var trT = loaded.GetType($"Dom64.T{kIdx:D2}", true)!;
                var clT = loaded.GetType($"Dom64.C{kIdx:D2}", true)!;
                var tr = new TypedTrack { Name = $"edge{kIdx:D2}", Type = trT, Data = new byte[1] };
                tr.Clips.Add(new TypedClip { Type = clT, Start = 0, End = 10, Data = BitConverter.GetBytes(kIdx + 1) });
                tlEdge.Tracks.Add(tr);
            }
            var kinds = LibraryBake.Kinds([loaded]);
            var p0 = LibraryBake.BakeTyped(tlAll, man);
            var p1 = LibraryBake.BakeTyped(tlEdge, man);
            var s = LibrarySettings.Default;
            s.ChannelCount = 1;
            var libBytes = LibraryAssembler.Assemble(
                [
                    new LibraryPart { Name = names[0], Tlb = p0, Kinds = LibraryBake.KindPayload(names[0], p0, kinds), Next = 0 },
                    new LibraryPart { Name = names[1], Tlb = p1, Kinds = LibraryBake.KindPayload(names[1], p1, kinds), Next = 0 },
                ],
                60,
                s);
            return (pe, libBytes);
        }
        finally
        {
            ctx.Unload();
        }
    }

    [Fact]
    public void SixtyFourKinds_FullBitMaskAndBit63_WorkAcrossSpansPulsesReachAndReload()
    {
        var (pe, libBytes) = Domain64.Value;
        var ctx = new Domain64Context();
        try
        {
            var asm = ctx.LoadFromStream(new MemoryStream(pe));
            var run = asm.GetType("Dom64.Host64", true)!.GetMethod("Run")!;
            var full = ((int KindCount, int LastVal, int Bit0Val, int HighPulseVal, long Dropped))run.Invoke(null, [libBytes, false])!;
            Assert.Equal(64, full.KindCount);
            Assert.Equal(0L, full.Dropped);
            Assert.True(full.LastVal > 300);

            var partial = ((int KindCount, int LastVal, int Bit0Val, int HighPulseVal, long Dropped))run.Invoke(null, [libBytes, true])!;
            Assert.Equal(64, partial.KindCount);
            Assert.Equal(1L, partial.Dropped);
            Assert.True(partial.HighPulseVal >= 700);
            Assert.True(partial.Bit0Val >= 100);
        }
        finally
        {
            ctx.Unload();
        }
    }

    [Fact]
    public void Duration1And65535_LastTickLoopReverseHoldAndSuperSampling_MatchLaws()
    {
        FuzzRunner.Run("B4-Duration1And65535", 0x42340003UL, 10, (rng, iter) =>
        {
            var names = new[] { "b4_0_d1_loop", "b4_1_d1_hold", "b4_2_dmax_loop", "b4_3_dmax_plain" };
            var channels = new[] { "ch" };
            var man = LibraryBake.Manifest(names, channels, 60);

            var t0 = new TypedTimeline { Name = names[0], Duration = 1, Loops = true };
            var tr0 = new TypedTrack { Name = "k0", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            tr0.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 1, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 3, StartPulse = 5, EndPulse = 7 }) });
            t0.Tracks.Add(tr0);

            var t1 = new TypedTimeline { Name = names[1], Duration = 1, Hold = true };
            var tr1 = new TypedTrack { Name = "k1", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            tr1.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 1, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 4, StartPulse = 10, EndPulse = 20 }) });
            t1.Tracks.Add(tr1);

            const uint dMax = 65_535;
            var t2 = new TypedTimeline { Name = names[2], Duration = dMax, Loops = true };
            var tr2 = new TypedTrack { Name = "k2", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            tr2.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = dMax - 8, End = dMax, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 2, StartPulse = 11, EndPulse = 13 }) });
            tr2.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 6, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 5, StartPulse = 17, EndPulse = 19 }) });
            t2.Tracks.Add(tr2);
            t2.Markers.Add(new TypedMarker { Type = typeof(KPulseMarker), Tick = dMax - 1, Data = LibraryGen.BytesOf(new KPulseMarker { Delta = 23 }) });

            var t3 = new TypedTimeline { Name = names[3], Duration = dMax, Loops = true };
            var tr3 = new TypedTrack { Name = "p3", Type = typeof(KIdleTrack), Data = LibraryGen.BytesOf(new KIdleTrack { Bias = 0 }) };
            tr3.Clips.Add(new TypedClip { Type = typeof(KIdleClip), Start = dMax - 2, End = dMax, Data = LibraryGen.BytesOf(new KIdleClip { Code = 999 }) });
            t3.Tracks.Add(tr3);

            var lib = LibraryGen.AssembleTyped([t0, t1, t2, t3], channels, man, AllKinds);

            using (var w1x = new TimelineWorld(lib.Bytes))
            using (var p1x = new PlaybackTable(2, 4))
            using (var k1x = new KindStore(w1x, p1x))
            using (var wRx = new TimelineWorld(lib.Bytes))
            using (var pRx = new PlaybackTable(2, 4))
            using (var kRx = new KindStore(wRx, pRx))
            {
                var mult = 2 + rng.NextInt(3);
                var rate = (ushort)(mult * 16);
                p1x.Start(0, 3, (ushort)(dMax - 4));
                pRx.Start(0, 3, (ushort)(dMax - 4));
                p1x.Start(1, 1, 0);
                pRx.Start(1, 1, 0);

                var c1 = new KindIntCol[2]; var e1 = new KindEachCol[2]; var i1 = new KindIdleCol[2]; var f1 = new KindFloatCol[2];
                var cR = new KindIntCol[2]; var eR = new KindEachCol[2]; var iR = new KindIdleCol[2]; var fR = new KindFloatCol[2];
                var inp = new[] { new KindInputCol { Factor = 1 }, new KindInputCol { Factor = 1 } };

                w1x.Tick(p1x, true);
                new KindFuzzSystem { CombineCol = c1, EachCol = e1, IdleCol = i1, FloatCol = f1, Input = inp }.Run(w1x, p1x, true);
                wRx.Tick(pRx, true);
                new KindFuzzSystem { CombineCol = cR, EachCol = eR, IdleCol = iR, FloatCol = fR, Input = inp }.Run(wRx, pRx, true);

                pRx.SetSpeed(0, 3, rate);
                pRx.SetSpeed(1, 1, rate);

                var fwd = rng.NextBool();
                var fastSteps = 6;
                for (var s = 0; s < fastSteps; s++)
                {
                    for (var sub = 0; sub < mult; sub++)
                    {
                        w1x.Tick(p1x, fwd);
                        new KindFuzzSystem { CombineCol = c1, EachCol = e1, IdleCol = i1, FloatCol = f1, Input = inp }.Run(w1x, p1x, fwd);
                    }
                    wRx.Tick(pRx, fwd);
                    new KindFuzzSystem { CombineCol = cR, EachCol = eR, IdleCol = iR, FloatCol = fR, Input = inp }.Run(wRx, pRx, fwd);

                    if (c1[0].Sum != cR[0].Sum || c1[1].Sum != cR[1].Sum)
                        FuzzRunner.Fail("B4-Duration1And65535", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42340003UL, iter,
                            $"step {s} fwd={fwd} rate={rate} 1x=({c1[0].Sum},{c1[1].Sum}) rx=({cR[0].Sum},{cR[1].Sum})");
                    Assert.Equal(p1x.SlotsOf(0)[0].Tick, pRx.SlotsOf(0)[0].Tick);
                    Assert.Equal(p1x.SlotsOf(1)[0].Tick, pRx.SlotsOf(1)[0].Tick);
                }
            }

            using (var wH = new TimelineWorld(lib.Bytes))
            using (var pH = new PlaybackTable(2, 4))
            using (var kH = new KindStore(wH, pH))
            {
                pH.Start(0, 2, 0);
                pH.Start(1, 4, (ushort)(dMax - 1));
                var c = new KindIntCol[2]; var eCol = new KindEachCol[2]; var idle = new KindIdleCol[2]; var flt = new KindFloatCol[2];
                var inp = new[] { new KindInputCol { Factor = 1 }, new KindInputCol { Factor = 1 } };
                for (var s = 0; s < 4; s++)
                {
                    wH.Tick(pH, true);
                    new KindFuzzSystem { CombineCol = c, EachCol = eCol, IdleCol = idle, FloatCol = flt, Input = inp }.Run(wH, pH, true);
                    if (s == 0) Assert.Equal(999, idle[1].Value);
                }
                Assert.Equal(0, pH.SlotsOf(0)[0].Tick);
                Assert.Equal(-777, idle[1].Value);
                Assert.Equal((ushort)2, pH.SlotsOf(1)[0].Tick);
            }
        });
    }

    [Fact(Skip = "#645")]
    public void EntityAt256Slots_WithRatedTimelines_StepsBothPlainAndKindWithoutFreeze()
    {
        FuzzRunner.Run("B4-256SlotRated", 0x42340004UL, 1, (rng, iter) =>
        {
            const int n = 256;
            var names = new string[n];
            for (var i = 0; i < n; i++) names[i] = $"r256_{i + 1:D3}";
            var manifest = LibraryBake.Manifest(names, [], 60);
            var plainTls = new TypedTimeline[n];
            var kindTls = new TypedTimeline[n];
            for (var i = 0; i < n; i++)
            {
                var pt = new TypedTimeline { Name = names[i], Duration = 16, Loops = true };
                var ptr = new TypedTrack { Name = "pl", Type = typeof(FuzzPlainTrack), Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 }) };
                ptr.Clips.Add(new TypedClip { Type = typeof(FuzzPlainClip), Start = 0, End = 16, Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = 1, Tag = 1 }) });
                pt.Tracks.Add(ptr);
                plainTls[i] = pt;

                var kt = new TypedTimeline { Name = names[i], Duration = 16, Loops = true };
                var ktr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
                ktr.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 16, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 1 }) });
                kt.Tracks.Add(ktr);
                kindTls[i] = kt;
            }
            var libP = LibraryGen.AssembleTyped(plainTls, [], manifest, null);
            var libK = LibraryGen.AssembleTyped(kindTls, [], manifest, AllKinds);
            using var wP = new TimelineWorld(libP.Bytes);
            using var wK = new TimelineWorld(libK.Bytes);
            using var pP = new PlaybackTable(1, n);
            using var pK = new PlaybackTable(1, n);
            using var ks = new KindStore(wK, pK);

            for (ushort tl = 1; tl <= n; tl++)
            {
                pP.Start(0, tl);
                pK.Start(0, tl);
            }
            var rate = (ushort)(32 + rng.NextInt(224));
            pP.SetSpeed(0, 1, rate);
            pK.SetSpeed(0, 1, rate);
            wP.ApplyRequests(pP);
            wP.Advance(pP, true);
            wK.Tick(pK, true);

            Assert.Equal(pP.SlotsOf(0)[0].Tick, pK.SlotsOf(0)[0].Tick);
        });
    }
}
