using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class PulseFloodAndReloadLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);
    static readonly (Assembly Asm, Type Track, Type Clip) ReloadSubset = BuildReloadSubset();

    sealed class ReloadSubsetContext : AssemblyLoadContext
    {
        public ReloadSubsetContext() : base("B8ReloadSubset", isCollectible: false) { }
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    static (Assembly Asm, Type Track, Type Clip) BuildReloadSubset()
    {
        const string source = """
using Tl;

namespace Tl.Fuzz.RuntimeDomain
{
    public struct B8SubCol
    {
        public int Sum;
    }

    public struct B8SubEachCol
    {
        public long Sum;
    }

    public struct B8SubClip
    {
        public int Delta;
    }

    public struct B8SubTrack : IBlend<B8SubClip>
    {
        public int Scale;

        public void Blend(in B8SubClip first, in B8SubClip second, float factor, out B8SubClip result)
            => result = first;
    }

    [TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
    public struct KCombineInt
    {
        public static void Combine(ref int total, in int next) => total += next;
        public static void Apply(in int total, ref B8SubCol col) => col.Sum += total;
    }

    [TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Each)]
    public struct KEachInt
    {
        public static void Apply(in long delta, ref B8SubEachCol col) => col.Sum += delta;
    }

    public readonly struct B8SubConsumer : ITrack<B8SubTrack, B8SubClip>
    {
        [Feeds(typeof(KCombineInt))]
        public static int Evaluate(in Frame<B8SubTrack, B8SubClip> frame)
            => frame.Clip.Delta * frame.Track.Scale;
    }
}
""";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var refs = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            string loc;
            try { loc = asm.Location; }
            catch { continue; }
            if (loc.Length > 0 && seen.Add(loc))
                refs.Add(MetadataReference.CreateFromFile(loc));
        }
        foreach (var t in new[] { typeof(object), typeof(TimelineWorld), typeof(ITrack<,>), typeof(Unsafe), typeof(MemoryMarshal) })
        {
            var loc = t.Assembly.Location;
            if (loc.Length > 0 && seen.Add(loc))
                refs.Add(MetadataReference.CreateFromFile(loc));
        }

        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "B8ReloadSubset.cs");
        var compilation = CSharpCompilation.Create(
            "Tl.Fuzz.B8ReloadSubset",
            [tree],
            refs.ToImmutable(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var pe = new MemoryStream();
        var emit = compilation.Emit(pe);
        if (!emit.Success)
            throw new InvalidOperationException("B8 subset compile failed: " + string.Join("; ", emit.Diagnostics));

        pe.Position = 0;
        var ctx = new ReloadSubsetContext();
        var asmLoaded = ctx.LoadFromStream(pe);
        return (asmLoaded, asmLoaded.GetType("Tl.Fuzz.RuntimeDomain.B8SubTrack")!, asmLoaded.GetType("Tl.Fuzz.RuntimeDomain.B8SubClip")!);
    }

    static GeneratedLibrary BuildFloodLibrary(ref FuzzRandom rng, int timelines, int channels, int inlineCap, int gamePulsesCap, bool extraOnlyOnFirstTimeline = false)
    {
        var names = new string[timelines];
        for (var i = 0; i < timelines; i++) names[i] = $"b8_{i + 1:D2}";
        var chNames = new string[channels];
        for (var i = 0; i < channels; i++) chNames[i] = $"b8c_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, chNames, 60);

        var overrides = new Dictionary<string, KindOverride>(StringComparer.Ordinal)
        {
            [typeof(KCombineInt).FullName!] = new KindOverride(inline: inlineCap, gamePulses: gamePulsesCap, maxInlineBytes: Math.Max(16, inlineCap * 16)),
            [typeof(KEachInt).FullName!] = new KindOverride(inline: inlineCap, gamePulses: gamePulsesCap, maxInlineBytes: Math.Max(16, inlineCap * 16)),
            [typeof(FoldedPulseKind).FullName!] = new KindOverride(inline: inlineCap, gamePulses: gamePulsesCap, maxInlineBytes: Math.Max(16, inlineCap * 16)),
        };

        var typed = new TypedTimeline[timelines];
        for (var i = 0; i < timelines; i++)
        {
            var dur = (uint)(8 + rng.NextInt(16));
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = channels > 0 ? chNames[i % channels] : null,
                Loops = true,
            };

            var tc = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            tc.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 0, StartPulse = 0, EndPulse = 0 }) });
            tl.Tracks.Add(tc);

            if (!extraOnlyOnFirstTimeline || i == 0)
            {
                var te = new TypedTrack { Name = "ke", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }) };
                te.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new KEachClip { Delta = 0, StartPulse = 0, EndPulse = 0 }) });
                tl.Tracks.Add(te);

                var tf = new TypedTrack { Name = "fp", Type = typeof(FoldPulseTrack), Data = new byte[1] };
                tf.Clips.Add(new TypedClip { Type = typeof(FoldPulseClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new FoldPulseClip { SpanVal = 0, PulseVal = 0 }) });
                tl.Tracks.Add(tf);
            }

            typed[i] = tl;
        }

        var kinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly], overrides);
        return LibraryGen.AssembleTyped(typed, chNames, manifest, kinds, Math.Max(16, inlineCap * 16), 1, overrides);
    }

    static byte[] BuildSubsetReloadLibrary(int timelines, int channels)
    {
        var names = new string[timelines];
        for (var i = 0; i < timelines; i++) names[i] = $"b8_{i + 1:D2}";
        var chNames = new string[channels];
        for (var i = 0; i < channels; i++) chNames[i] = $"b8c_{i + 1:D2}";
        var manifest = LibraryBake.Manifest(names, chNames, 60);

        var clipData = new byte[4];
        var trackData = new byte[4];
        BinaryPrimitivesWriteInt(trackData, 1);

        var typed = new TypedTimeline[timelines];
        var kinds = LibraryBake.Kinds([ReloadSubset.Asm]);
        for (var i = 0; i < timelines; i++)
        {
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = 12,
                Channel = channels > 0 ? chNames[i % channels] : null,
                Loops = true,
            };
            var tr = new TypedTrack { Name = "sub", Type = ReloadSubset.Track, Data = (byte[])trackData.Clone() };
            tr.Clips.Add(new TypedClip { Type = ReloadSubset.Clip, Start = 0, End = 12, Data = (byte[])clipData.Clone() });
            tl.Tracks.Add(tr);
            typed[i] = tl;
        }
        return LibraryGen.AssembleTyped(typed, chNames, manifest, kinds).Bytes;
    }

    static void BinaryPrimitivesWriteInt(byte[] dst, int value)
        => MemoryMarshal.Write(dst, in value);

    [Fact]
    public void PulseFlood_BeyondGamePulsesAndInlineCapacity_PreservesAllDeliveredOrderAndExactDrops()
    {
        FuzzRunner.Run("B8-PulseFloodSpill", 0x42380001UL, 10, (rng, iter) =>
        {
            var entities = 6 + rng.NextInt(12);
            var floodEntity = rng.NextInt(entities);
            var idleEntity = (floodEntity + 1) % entities;
            var neighborEntity = (floodEntity + 2) % entities;
            var unreachableEntity = (floodEntity + 3) % entities;
            var inlineCap = rng.NextInt(5);
            var gamePulsesCap = rng.NextInt(4);
            var lib = BuildFloodLibrary(ref rng, 2, 1, inlineCap, gamePulsesCap, true);

            var combName = typeof(KCombineInt).FullName!;
            var eachName = typeof(KEachInt).FullName!;
            var foldName = typeof(FoldedPulseKind).FullName!;
            var reach = new ulong[entities];
            for (var e = 0; e < entities; e++)
                reach[e] = e == idleEntity ? 0UL : e == unreachableEntity ? 0b001UL : 0b111UL;

            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(entities, 4);
            using var store = new KindStore(world, players, [combName, eachName, foldName], reach);

            var comb = new KindIntCol[entities];
            var each = new KindEachCol[entities];
            var idle = new KindIdleCol[entities];
            var flt = new KindFloatCol[entities];
            var folded = new FoldPulseOut[entities];
            var input = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) input[e] = new KindInputCol { Factor = 1 };

            var pComb = store.Pulses<int>(combName);
            var pEach = store.Pulses<int>(eachName);
            var pFold = store.Pulses<int, IntAddFold>(foldName);

            for (var e = 0; e < entities; e++)
            {
                if (e == unreachableEntity) players.Start(e, 2);
                else if (e != idleEntity) players.Start(e, 1);
            }

            var sysK = new KindFuzzSystem { CombineCol = comb, EachCol = each, IdleCol = idle, FloatCol = flt, Input = input };
            var sysF = new FoldedPulseSystem { Out = folded };

            var eachK = world.View.Kinds.IndexOf(eachName);
            var combK = world.View.Kinds.IndexOf(combName);
            var foldK = world.View.Kinds.IndexOf(foldName);

            var expectedCombTotal = new int[entities];
            var expectedEachTotal = new int[entities];
            var expectedFoldTotal = new int[entities];
            long expectedDrops = 0;
            for (var round = 0; round < 3; round++)
            {
                var floodCount = 40 + rng.NextInt(280);
                var expectedEachSeq = new List<int>(floodCount);
                var expectedCombSum = new int[entities];
                var expectedFoldSum = new int[entities];

                void SendFoldTracked(int entity, int val, bool delivered)
                {
                    pFold.To(entity, val);
                    if (delivered)
                    {
                        expectedFoldSum[entity] += val;
                        expectedFoldTotal[entity] += val;
                    }
                }

                for (var i = 0; i < floodCount; i++)
                {
                    var v = (i & 15) - 7 + (round + 1);
                    pComb.To(floodEntity, v);
                    expectedCombSum[floodEntity] += v;
                    expectedCombTotal[floodEntity] += v;
                    pEach.To(floodEntity, v);
                    expectedEachSeq.Add(v);
                    expectedEachTotal[floodEntity] += v;
                    SendFoldTracked(floodEntity, v, true);
                }

                var nv = 100 + round;
                pComb.To(neighborEntity, nv);
                expectedCombSum[neighborEntity] += nv;
                expectedCombTotal[neighborEntity] += nv;
                pEach.To(neighborEntity, nv);
                expectedEachTotal[neighborEntity] += nv;
                SendFoldTracked(neighborEntity, nv, true);

                var unreachCount = 3 + rng.NextInt(6);
                for (var i = 0; i < unreachCount; i++)
                {
                    pComb.To(idleEntity, 77);
                    pEach.To(unreachableEntity, 88);
                    pEach.To(idleEntity, 99);
                    expectedDrops += 3;
                }
                pFold.To(idleEntity, 55);
                expectedDrops += 1;

                world.Tick(players, true);

                if (store.DroppedPulses != expectedDrops)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} drops={store.DroppedPulses} != {expectedDrops}");

                var eachTbl = store.Table(eachK);
                var floodEachRow = store.RowOf(floodEntity, eachK);
                if (eachTbl == null || floodEachRow < 0)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} missing flood each row");

                var actualLen = eachTbl->Length[floodEachRow];
                var clipEntries = actualLen - expectedEachSeq.Count;
                if (clipEntries is < 1 or > 2)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} rowLen={actualLen} flood={expectedEachSeq.Count}");

                for (var idx = 0; idx < expectedEachSeq.Count; idx++)
                {
                    var got = *(int*)((byte*)eachTbl->At(floodEachRow, clipEntries + idx) + eachTbl->ValueAt);
                    if (got != expectedEachSeq[idx])
                        FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} fifo idx={idx} got={got} != {expectedEachSeq[idx]}");
                }

                var foldTbl = store.Table(foldK);
                var floodFoldRow = store.RowOf(floodEntity, foldK);
                if (foldTbl == null || floodFoldRow < 0 || foldTbl->Length[floodFoldRow] is < 2 or > 3)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} folded burst did not collapse into 1 pulse entry len={(foldTbl == null ? -1 : foldTbl->Length[floodFoldRow])}");

                for (var e = 0; e < entities; e++)
                {
                    if (e == floodEntity || e == neighborEntity)
                    {
                        var rowC = store.RowOf(e, combK);
                        var tblC = store.Table(combK);
                        var rowSum = 0;
                        if (tblC != null && rowC >= 0)
                        {
                            for (var j = 0; j < tblC->Length[rowC]; j++)
                                rowSum += *(int*)((byte*)tblC->At(rowC, j) + tblC->ValueAt);
                        }
                        if (rowSum != expectedCombSum[e])
                            FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} e={e} comb rowSum={rowSum} != {expectedCombSum[e]}");

                        var rowF = store.RowOf(e, foldK);
                        var foldRowSum = 0;
                        if (foldTbl != null && rowF >= 0)
                        {
                            for (var j = 0; j < foldTbl->Length[rowF]; j++)
                                foldRowSum += *(int*)((byte*)foldTbl->At(rowF, j) + foldTbl->ValueAt);
                        }
                        if (foldRowSum != expectedFoldSum[e])
                            FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} e={e} fold rowSum={foldRowSum} != {expectedFoldSum[e]}");
                    }
                }

                if (floodCount > inlineCap + 1 && store.GetStats(eachK).Overflows == 0)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} expected Overflows > 0 for floodCount={floodCount} inline={inlineCap}");

                sysK.Run(world, players, true);
                sysF.Run(world, players, true);

                if (eachTbl->Length[floodEachRow] != 1 || foldTbl->Length[floodFoldRow] != 1)
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} post-run trim left pulses each={eachTbl->Length[floodEachRow]} fold={foldTbl->Length[floodFoldRow]}");

                if (comb[floodEntity].Sum != expectedCombTotal[floodEntity] || each[floodEntity].Sum != expectedEachTotal[floodEntity] || folded[floodEntity].Value != expectedFoldTotal[floodEntity])
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} system sum mismatch comb={comb[floodEntity].Sum}!={expectedCombTotal[floodEntity]} each={each[floodEntity].Sum}!={expectedEachTotal[floodEntity]} fold={folded[floodEntity].Value}!={expectedFoldTotal[floodEntity]}");

                if (comb[neighborEntity].Sum != expectedCombTotal[neighborEntity] || each[neighborEntity].Sum != expectedEachTotal[neighborEntity] || folded[neighborEntity].Value != expectedFoldTotal[neighborEntity])
                    FuzzRunner.Fail("B8-PulseFloodSpill", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380001UL, iter, $"round={round} neighbor system sum mismatch");
            }
        });
    }

    [Fact]
    public void QueuedPulses_AcrossWorldReload_PreserveKeptAndCountDroppedPulses()
    {
        FuzzRunner.Run("B8-QueuedPulsesReload", 0x42380002UL, 8, (rng, iter) =>
        {
            const int entities = 8;
            var lib1 = BuildFloodLibrary(ref rng, 2, 1, 1 + rng.NextInt(3), rng.NextInt(3));
            var lib1Keep = BuildFloodLibrary(ref rng, 2, 1, 1 + rng.NextInt(3), rng.NextInt(3));
            var lib2Subset = BuildSubsetReloadLibrary(2, 1);

            var combName = typeof(KCombineInt).FullName!;
            var eachName = typeof(KEachInt).FullName!;
            var foldName = typeof(FoldedPulseKind).FullName!;

            using (var wA = new TimelineWorld(lib1.Bytes))
            using (var pA = new PlaybackTable(entities, 4))
            using (var sA = new KindStore(wA, pA))
            {
                var cHandle = sA.Pulses<int>(combName);
                var fHandle = sA.Pulses<int, IntAddFold>(foldName);
                for (var e = 0; e < entities; e++) pA.Start(e, (ushort)(1 + (e & 1)));
                wA.Tick(pA, true);

                var expectedKept = new int[entities];
                for (var i = 0; i < 10; i++)
                {
                    var e = rng.NextInt(entities);
                    var v = rng.NextInt(25) - 12;
                    cHandle.To(e, v);
                    fHandle.To(e, v);
                    expectedKept[e] += v;
                }

                wA.Reload(lib1Keep.Bytes);

                for (var i = 0; i < 6; i++)
                {
                    var e = rng.NextInt(entities);
                    var v = rng.NextInt(17) - 8;
                    cHandle.To(e, v);
                    fHandle.To(e, v);
                    expectedKept[e] += v;
                }

                wA.Tick(pA, true);
                Assert.Equal(0L, sA.DroppedPulses);

                var combK = wA.View.Kinds.IndexOf(combName);
                var foldK = wA.View.Kinds.IndexOf(foldName);
                var cTbl = sA.Table(combK);
                var fTbl = sA.Table(foldK);
                for (var e = 0; e < entities; e++)
                {
                    var cr = sA.RowOf(e, combK);
                    var fr = sA.RowOf(e, foldK);
                    var cs = 0;
                    for (var j = 0; j < cTbl->Length[cr]; j++) cs += *(int*)((byte*)cTbl->At(cr, j) + cTbl->ValueAt);
                    var fs = 0;
                    for (var j = 0; j < fTbl->Length[fr]; j++) fs += *(int*)((byte*)fTbl->At(fr, j) + fTbl->ValueAt);
                    if (cs != expectedKept[e] || fs != expectedKept[e])
                        FuzzRunner.Fail("B8-QueuedPulsesReload", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380002UL, iter, $"kept reload mismatch e={e} comb={cs} fold={fs} exp={expectedKept[e]}");
                }
            }

            var subsetReach = new ulong[entities];
            for (var e = 0; e < entities - 1; e++) subsetReach[e] = 0b111UL;

            using (var wB = new TimelineWorld(lib1.Bytes))
            using (var pB = new PlaybackTable(entities, 4))
            using (var sB = new KindStore(wB, pB, [combName, eachName, foldName], subsetReach))
            {
                var cHandle = sB.Pulses<int>(combName);
                var eHandle = sB.Pulses<int>(eachName);
                var fHandle = sB.Pulses<int, IntAddFold>(foldName);

                for (var e = 0; e < entities - 1; e++) pB.Start(e, (ushort)(1 + (e & 1)));
                wB.Tick(pB, true);

                var combExpected = new int[entities];
                var combSendCount = 12;
                for (var i = 0; i < combSendCount; i++)
                {
                    var e = i % (entities - 1);
                    var v = i + 1;
                    cHandle.To(e, v);
                    combExpected[e] += v;
                }
                cHandle.To(entities - 1, 999);

                var eachSendCount = 5 + rng.NextInt(10);
                for (var i = 0; i < eachSendCount; i++)
                    eHandle.To(i % entities, i + 3);

                fHandle.To(0, 10);
                fHandle.To(0, 20);
                fHandle.To(1, 30);
                var expectedFoldQueueEntries = 2;

                wB.Reload(lib2Subset);
                fHandle.To(2, 40);
                cHandle.To(0, 5);
                combExpected[0] += 5;

                wB.Tick(pB, true);

                var expectedFinalDrops = eachSendCount + expectedFoldQueueEntries + 1 + 1;
                if (sB.DroppedPulses != expectedFinalDrops)
                    FuzzRunner.Fail("B8-QueuedPulsesReload", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380002UL, iter, $"after reload+tick drops={sB.DroppedPulses} != {expectedFinalDrops}");

                fHandle.To(3, 50);
                if (sB.DroppedPulses != expectedFinalDrops + 1)
                    FuzzRunner.Fail("B8-QueuedPulsesReload", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380002UL, iter, $"post-rebuild handle drop={sB.DroppedPulses} != {expectedFinalDrops + 1}");

                var combK = wB.View.Kinds.IndexOf(combName);
                var cTbl = sB.Table(combK);
                for (var e = 0; e < entities - 1; e++)
                {
                    var cr = sB.RowOf(e, combK);
                    var cs = 0;
                    for (var j = 0; j < cTbl->Length[cr]; j++) cs += *(int*)((byte*)cTbl->At(cr, j) + cTbl->ValueAt);
                    if (cs != combExpected[e])
                        FuzzRunner.Fail("B8-QueuedPulsesReload", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42380002UL, iter, $"subset kept e={e} comb={cs} != {combExpected[e]}");
                }
            }
        });
    }

    [Fact(Skip = "#643")]
    public void PulseFloodUpTo65535Entries_SmallInlineRowsDoNotFailAtHalfLimit()
    {
        FuzzRunner.Run("B8-HalfLimitOverflow", 0x42380003UL, 1, (rng, iter) =>
        {
            var inline = 2 << rng.NextInt(2);
            var lib = BuildFloodLibrary(ref rng, 1, 0, inline, 0, false);
            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(1, 2);
            using var store = new KindStore(world, players);
            var pulses = store.Pulses<int>(typeof(KCombineInt).FullName!);

            players.Start(0, 1);
            var targetPulses = 32768 + rng.NextInt(100);
            for (var i = 0; i < targetPulses; i++) pulses.To(0, 1);

            world.Tick(players, true);
        });
    }

    [Fact(Skip = "#644")]
    public void PulseFloodOverflow_ClearsFailedQueueSoSubsequentTicksRecover()
    {
        FuzzRunner.Run("B8-QueueWedgeRecovery", 0x42380004UL, 1, (rng, iter) =>
        {
            var lib = BuildFloodLibrary(ref rng, 1, 0, 4, 0, false);
            using var world = new TimelineWorld(lib.Bytes);
            using var players = new PlaybackTable(1, 2);
            using var store = new KindStore(world, players);
            var pulses = store.Pulses<int>(typeof(KCombineInt).FullName!);

            players.Start(0, 1);
            for (var i = 0; i < 70000; i++) pulses.To(0, 1);

            Assert.Throws<InvalidOperationException>(() => world.Tick(players, true));
            var recovered = Record.Exception(() => world.Tick(players, true));
            Assert.Null(recovered);
        });
    }
}
