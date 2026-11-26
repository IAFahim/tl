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

public sealed unsafe class ExceptionRecoveryLaws
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    sealed class MismatchCtx : AssemblyLoadContext
    {
        public MismatchCtx() : base("B5_Mismatch", isCollectible: true) { }
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    static readonly Lazy<byte[]> WrongKindSizeLib = new(BuildWrongSizeLibrary, LazyThreadSafetyMode.ExecutionAndPublication);

    static byte[] BuildWrongSizeLibrary()
    {
        const string code = """
using System;
using Tl;
namespace Tl.Fuzz.RuntimeDomain;
public struct B5Out { public long V; }
public struct B5Clip { public long D; }
public readonly struct B5Track : IBlend<B5Clip> { public void Blend(in B5Clip a, in B5Clip b, float t, out B5Clip r) => r = a; }
[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct KCombineInt
{
    public static void Combine(ref long total, in long next) => total += next;
    public static void Apply(in long total, ref B5Out col) => col.V += total;
}
[TimelineSystem]
public ref partial struct B5Sys { public Span<B5Out> Col; }
[TimelineSystemMember(typeof(B5Sys))]
public readonly struct B5Consumer : ITrack<B5Track, B5Clip>
{
    [Feeds(typeof(KCombineInt))]
    public static long Eval(in Frame<B5Track, B5Clip> f) => f.Direction * f.Clip.D;
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
            if (loc.Length > 0 && seen.Add(loc)) refs.Add(MetadataReference.CreateFromFile(loc));
        }
        foreach (var t in new[] { typeof(object), typeof(TimelineWorld), typeof(ITrack<,>), typeof(Unsafe), typeof(MemoryMarshal) })
        {
            var loc = t.Assembly.Location;
            if (loc.Length > 0 && seen.Add(loc)) refs.Add(MetadataReference.CreateFromFile(loc));
        }
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var comp = CSharpCompilation.Create("B5_WrongSize", [CSharpSyntaxTree.ParseText(code, parse, "B5.cs")], refs.ToImmutable(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        var gen = new TimelineIncrementalGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create([gen], parseOptions: parse).RunGeneratorsAndUpdateCompilation(comp, out var outComp, out _);
        using var pe = new MemoryStream();
        var emit = outComp.Emit(pe);
        if (!emit.Success)
            throw new InvalidOperationException("B5 wrong-size compile failed: " + string.Join("; ", emit.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));

        var ctx = new MismatchCtx();
        try
        {
            var asm = ctx.LoadFromStream(new MemoryStream(pe.ToArray()));
            var names = new[] { "ktl_0001", "ktl_0002", "ktl_0003", "ktl_0004" };
            var channels = new[] { "kch_0001" };
            var man = LibraryBake.Manifest(names, channels, 60);
            var trT = asm.GetType("Tl.Fuzz.RuntimeDomain.B5Track", true)!;
            var clT = asm.GetType("Tl.Fuzz.RuntimeDomain.B5Clip", true)!;
            var tls = new TypedTimeline[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var tl = new TypedTimeline { Name = names[i], Duration = 16, Loops = true };
                var tr = new TypedTrack { Name = "t", Type = trT, Data = new byte[1] };
                tr.Clips.Add(new TypedClip { Type = clT, Start = 0, End = 16, Data = BitConverter.GetBytes((long)(i + 1)) });
                tl.Tracks.Add(tr);
                tls[i] = tl;
            }
            var kinds = LibraryBake.Kinds([asm]);
            return LibraryGen.AssembleTyped(tls, channels, man, kinds).Bytes;
        }
        finally
        {
            ctx.Unload();
        }
    }

    static string[] BuildReachableNames()
        => [typeof(KCombineInt).FullName!, typeof(KEachInt).FullName!, typeof(KIdleInt).FullName!, typeof(KCombineFloat).FullName!];

    static byte[] CorruptLibrary(byte[] source, int mode)
    {
        switch (mode % 3)
        {
            case 0:
                return source.AsSpan(0, 48).ToArray();
            case 1:
            {
                var bad = source.ToArray();
                bad[0] ^= 0xFF;
                bad[1] ^= 0xAA;
                return bad;
            }
            default:
            {
                var bad = source.ToArray();
                bad[^3] ^= 0x55;
                if (bad.Length > 120) bad[112] ^= 0x33;
                return bad;
            }
        }
    }

    static (byte[] Big, byte[] Small, uint[] BigDurations) BuildBigAndSmallPair(ref FuzzRandom rng, bool includeKinds)
    {
        var namesBig = new[] { "b5_tl1", "b5_tl2", "b5_tl3", "b5_tl4" };
        var namesSmall = new[] { "b5_tl1", "b5_tl2" };
        var channels = new[] { "b5_ch1", "b5_ch2" };
        var manBig = LibraryBake.Manifest(namesBig, channels, 60);
        var manSmall = LibraryBake.Manifest(namesSmall, channels, 60);

        var bigTls = new TypedTimeline[4];
        var durs = new uint[4];
        for (var i = 0; i < 4; i++)
        {
            var dur = (uint)(20 + rng.NextInt(15));
            durs[i] = dur;
            var tl = new TypedTimeline { Name = namesBig[i], Duration = dur, Loops = true, Channel = i == 1 ? channels[0] : null };
            if (includeKinds)
            {
                var tr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
                tr.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new KCombineClip { Delta = i + 2, StartPulse = 3, EndPulse = 5 }) });
                tl.Tracks.Add(tr);
                if (i == 3)
                {
                    var et = new TypedTrack { Name = "ke", Type = typeof(KEachTrack), Data = LibraryGen.BytesOf(new KEachTrack { Scale = 1 }) };
                    et.Clips.Add(new TypedClip { Type = typeof(KEachClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new KEachClip { Delta = 7 }) });
                    tl.Tracks.Add(et);
                }
            }
            else
            {
                var tr = new TypedTrack { Name = "pl", Type = typeof(FuzzPlainTrack), Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 }) };
                tr.Clips.Add(new TypedClip { Type = typeof(FuzzPlainClip), Start = 0, End = dur, Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = i + 2, Tag = i, TriggerTimeline = 0 }) });
                tl.Tracks.Add(tr);
            }
            bigTls[i] = tl;
        }

        var smallTls = new TypedTimeline[2];
        for (var i = 0; i < 2; i++)
        {
            const uint shortDur = 3;
            var tl = new TypedTimeline { Name = namesSmall[i], Duration = shortDur, Hold = true };
            if (includeKinds)
            {
                var tr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 2 }) };
                tr.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = shortDur, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 1, StartPulse = 1, EndPulse = 1 }) });
                tl.Tracks.Add(tr);
            }
            else
            {
                var tr = new TypedTrack { Name = "pl", Type = typeof(FuzzPlainTrack), Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 2 }) };
                tr.Clips.Add(new TypedClip { Type = typeof(FuzzPlainClip), Start = 0, End = shortDur, Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = 1, Tag = i, TriggerTimeline = 0 }) });
                tl.Tracks.Add(tr);
            }
            smallTls[i] = tl;
        }

        var bigLib = LibraryGen.AssembleTyped(bigTls, channels, manBig, includeKinds ? AllKinds : null);
        var smallLib = LibraryGen.AssembleTyped(smallTls, channels, manSmall, includeKinds ? AllKinds : null);
        return (bigLib.Bytes, smallLib.Bytes, durs);
    }

    [Fact]
    public void ApplyRequestsAndTick_InvalidEntityTimelineAndReach_LeaveStateIdenticalToControl()
    {
        FuzzRunner.Run("B5-ApplyTickInvalid", 0x42350001UL, 20, (rng, iter) =>
        {
            var entities = 6 + rng.NextInt(10);
            var timelines = 4;
            var channels = 2;
            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 40, true, true, true);
            var kindLib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 40, true, true, true, true);

            var names = BuildReachableNames();
            var reach = new ulong[entities];
            for (var e = 0; e < entities; e++)
                reach[e] = e == 0 ? (1ul << 0) | (1ul << 2) | (1ul << 3) : 0xFul;

            using var wPA = new TimelineWorld(plainLib.Bytes);
            using var wPC = new TimelineWorld(plainLib.Bytes);
            using var pPA = new PlaybackTable(entities, 4);
            using var pPC = new PlaybackTable(entities, 4);
            using var cPA = new Events<FuzzCue>(2048);
            using var cPC = new Events<FuzzCue>(2048);

            var mA = new PlainMotion[entities];
            var mC = new PlainMotion[entities];
            var sA = new PlainStats[entities];
            var sC = new PlainStats[entities];
            var pIn = new PlainInput[entities];
            for (var e = 0; e < entities; e++) pIn[e] = new PlainInput { Mult = 1 + (e & 1) };

            using var wKA = new TimelineWorld(kindLib.Bytes);
            using var wKC = new TimelineWorld(kindLib.Bytes);
            using var pKA = new PlaybackTable(entities, 4);
            using var pKC = new PlaybackTable(entities, 4);
            using var kA = new KindStore(wKA, pKA, names, reach);
            using var kC = new KindStore(wKC, pKC, names, reach);

            var cA = new KindIntCol[entities];
            var cC = new KindIntCol[entities];
            var eA = new KindEachCol[entities];
            var eC = new KindEachCol[entities];
            var iA = new KindIdleCol[entities];
            var iC = new KindIdleCol[entities];
            var fA = new KindFloatCol[entities];
            var fC = new KindFloatCol[entities];
            var kIn = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) kIn[e] = new KindInputCol { Factor = 1 + (e & 1) };

            var pulseA = kA.Pulses<int>(names[0]);
            var pulseC = kC.Pulses<int>(names[0]);

            var sec = wKA.View.Kinds;
            var tls = wKA.View.Timelines;
            var eachBit = kA.IndexOf(names[1]);
            bool UsesEach(ushort timeline)
            {
                var at = timeline - 1;
                for (var hop = 0; hop <= timelines; hop++)
                {
                    if ((sec.Timelines[at].KindMask & (1ul << eachBit)) != 0) return true;
                    if (tls[at].Next == 0) break;
                    at = tls[at].Next - 1;
                }
                return false;
            }

            ushort tlWithEach = 0;
            for (ushort t = 1; t <= timelines; t++)
            {
                if (UsesEach(t))
                {
                    tlWithEach = t;
                    break;
                }
            }

            for (var step = 0; step < 18; step++)
            {
                if ((step & 1) == 0)
                {
                    wPA.ApplyRequests(pPA);
                    wPC.ApplyRequests(pPC);
                }

                var batch = 1 + rng.NextInt(10);
                var savedPlain = new FuzzRequestOp[batch];
                var savedKind = new FuzzRequestOp[batch];
                for (var b = 0; b < batch; b++)
                {
                    var op = LibraryGen.NextRequest(ref rng, entities, timelines, channels, plainLib.Durations);
                    savedPlain[b] = op;
                    LibraryGen.ApplyOp(pPA, in op);
                    LibraryGen.ApplyOp(pPC, in op);

                    var kop = LibraryGen.NextRequest(ref rng, entities, timelines, channels, kindLib.Durations);
                    if (kop.Entity == 0 && kop.Op == RequestActionKind.Start && UsesEach(kop.Timeline))
                        kop.Entity = 1;
                    savedKind[b] = kop;
                    LibraryGen.ApplyOp(pKA, in kop);
                    LibraryGen.ApplyOp(pKC, in kop);
                }

                if ((step & 1) == 0)
                {
                    var badMode = step % 6;
                    if (badMode == 0)
                        pPA.Start(-1 - rng.NextInt(4), 1);
                    else if (badMode == 2)
                        pPA.Start(entities + rng.NextInt(5), 1);
                    else
                        wPA.Requests(pPA).For(rng.NextInt(entities), 0).Start((ushort)(badMode == 4 ? ushort.MaxValue : timelines + 1 + rng.NextInt(10)));

                    pPA.Start(rng.NextInt(entities), 1);
                    Assert.ThrowsAny<ArgumentException>(() => wPA.ApplyRequests(pPA));

                    if (badMode == 0)
                        pKA.Start(-1 - rng.NextInt(4), 1);
                    else if (badMode == 2 && tlWithEach != 0)
                    {
#if TL_CHECKED
                        pKA.Start(0, tlWithEach);
#else
                        pKA.Start(entities + rng.NextInt(5), 1);
#endif
                    }
                    else
                        wKA.Requests(pKA).For(1 + rng.NextInt(entities - 1), 0).Start((ushort)(badMode == 4 ? ushort.MaxValue : timelines + 2));

                    pKA.Start(1, 1);
                    Assert.ThrowsAny<ArgumentException>(() =>
                    {
                        if (rng.NextBool()) wKA.Tick(pKA, true);
                        else wKA.ApplyRequests(pKA);
                    });

                    for (var r = 0; r < batch; r++)
                    {
                        LibraryGen.ApplyOp(pPA, in savedPlain[r]);
                        LibraryGen.ApplyOp(pKA, in savedKind[r]);
                    }
                    wPA.ApplyRequests(pPA);
                    wKA.ApplyRequests(pKA);

                    wPC.ApplyRequests(pPC);
                    wKC.ApplyRequests(pKC);

                    Assert.True(FuzzRunner.ComparePlaybacks(pPA, pPC) is null);
                    Assert.True(FuzzRunner.ComparePlaybacks(pKA, pKC) is null);
#if !TL_CHECKED
                    if (badMode == 2 && tlWithEach != 0)
                    {
                        var dropped = pKA.DroppedStarts;
                        pKA.Start(0, tlWithEach);
                        pKC.Start(0, tlWithEach);
                        wKA.ApplyRequests(pKA);
                        wKC.ApplyRequests(pKC);
                        Assert.Equal(dropped + 1, pKA.DroppedStarts);
                        Assert.Equal(pKA.DroppedStarts, pKC.DroppedStarts);
                        Assert.True(FuzzRunner.ComparePlaybacks(pKA, pKC) is null);
                    }
#endif
                }

                for (var e = 0; e < entities; e++)
                {
                    if ((step + e) % 3 == 0)
                    {
                        var v = rng.NextInt(13) - 6;
                        pulseA.To(e, v);
                        pulseC.To(e, v);
                    }
                }

                var fwd = step % 5 != 4;
                wPA.ApplyRequests(pPA);
                wPC.ApplyRequests(pPC);
                new PlainFuzzSystem { Motion = mA, Stats = sA, Input = pIn, Requests = wPA.Requests(pPA), Cues = cPA.Writer }.Run(wPA, pPA, fwd);
                new PlainFuzzSystem { Motion = mC, Stats = sC, Input = pIn, Requests = wPC.Requests(pPC), Cues = cPC.Writer }.Run(wPC, pPC, fwd);
                wPA.Advance(pPA, fwd);
                wPC.Advance(pPC, fwd);
                cPA.Drain();
                cPC.Drain();

                wKA.Tick(pKA, fwd);
                wKC.Tick(pKC, fwd);
                new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = kIn }.Run(wKA, pKA, fwd);
                new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = kIn }.Run(wKC, pKC, fwd);

                var err =
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mA), MemoryMarshal.AsBytes<PlainMotion>(mC), $"step {step} Motion") ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainStats>(sA), MemoryMarshal.AsBytes<PlainStats>(sC), $"step {step} Stats") ??
                    FuzzRunner.ComparePlaybacks(pPA, pPC) ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cA), MemoryMarshal.AsBytes<KindIntCol>(cC), $"step {step} KComb") ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindEachCol>(eA), MemoryMarshal.AsBytes<KindEachCol>(eC), $"step {step} KEach") ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIdleCol>(iA), MemoryMarshal.AsBytes<KindIdleCol>(iC), $"step {step} KIdle") ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindFloatCol>(fA), MemoryMarshal.AsBytes<KindFloatCol>(fC), $"step {step} KFloat") ??
                    FuzzRunner.ComparePlaybacks(pKA, pKC);
                if (err != null)
                    FuzzRunner.Fail("B5-ApplyTickInvalid", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350001UL, iter, err);
                Assert.Equal(kC.DroppedPulses, kA.DroppedPulses);
            }
        });
    }

    [Fact]
    public void KindStoreApiAndSystemRun_ArgumentAndModeThrows_LeaveStateIdenticalToControl()
    {
        FuzzRunner.Run("B5-ApiAndRunThrows", 0x42350002UL, 20, (rng, iter) =>
        {
            const int entities = 8;
            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, 4, 2, 30, true, true, true);
            var kindLib = LibraryGen.BuildKindLibrary(ref rng, 4, 2, 30, true, true, true, true);

            using var wP = new TimelineWorld(plainLib.Bytes);
            using var pP = new PlaybackTable(entities, 4);
            using var cuesP = new Events<FuzzCue>(1024);

            Assert.Throws<ArgumentNullException>(() => wP.ApplyRequests(null!));
            Assert.Throws<ArgumentNullException>(() => wP.Tick(null!));
            Assert.Throws<ArgumentNullException>(() => wP.Advance(null!));
            Assert.Throws<ArgumentNullException>(() => wP.Requests(null!));
            Assert.Throws<InvalidOperationException>(() => wP.Tick(pP, true));

            using var wPA = new TimelineWorld(plainLib.Bytes);
            using var pPA = new PlaybackTable(entities, 4);
            using var cuesPA = new Events<FuzzCue>(1024);

            using var wK = new TimelineWorld(kindLib.Bytes);
            using var wKC = new TimelineWorld(kindLib.Bytes);
            using var pK = new PlaybackTable(entities, 4);
            using var pKC = new PlaybackTable(entities, 4);

            Assert.Throws<ArgumentNullException>(() => new KindStore(null!, pK));
            Assert.Throws<ArgumentNullException>(() => new KindStore(wK, null!));
            Assert.Throws<ArgumentException>(() => new KindStore(wK, pK, new string[65], new ulong[entities]));
            Assert.Throws<ArgumentException>(() => new KindStore(wK, pK, ["a"], null));
            Assert.Throws<ArgumentException>(() => new KindStore(wK, pK, ["a"], new ulong[entities - 1]));

            using var ks = new KindStore(wK, pK);
            using var ksC = new KindStore(wKC, pKC);

            Assert.Throws<InvalidOperationException>(() => wK.Advance(pK, true));
            Assert.Throws<ArgumentException>(() => ks.Pulses<int>("Dom.MissingKind"));
            Assert.Throws<ArgumentException>(() => ks.Pulses<float>(typeof(KCombineFloat).FullName!));
            Assert.Throws<ArgumentException>(() => ks.Pulses<long>(typeof(KCombineInt).FullName!));
            Assert.Throws<ArgumentException>(() => ks.Pulses<int, IntAddFold>(typeof(KEachInt).FullName!));

            var combPulses = ks.Pulses<int>(typeof(KCombineInt).FullName!);
            var combPulsesC = ksC.Pulses<int>(typeof(KCombineInt).FullName!);
            Assert.Throws<ArgumentOutOfRangeException>(() => combPulses.To(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => combPulses.To(entities, 5));

            var mA = new PlainMotion[entities];
            var mC = new PlainMotion[entities];
            var sA = new PlainStats[entities];
            var sC = new PlainStats[entities];
            var pIn = new PlainInput[entities];
            for (var e = 0; e < entities; e++) pIn[e] = new PlainInput { Mult = 2 };

            var cA = new KindIntCol[entities];
            var cC = new KindIntCol[entities];
            var eA = new KindEachCol[entities];
            var eC = new KindEachCol[entities];
            var iA = new KindIdleCol[entities];
            var iC = new KindIdleCol[entities];
            var fA = new KindFloatCol[entities];
            var fC = new KindFloatCol[entities];
            var kIn = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) kIn[e] = new KindInputCol { Factor = 2 };

            for (var step = 0; step < 12; step++)
            {
                var ent = rng.NextInt(entities);
                var tl = (ushort)(1 + rng.NextInt(4));
                pP.Start(ent, tl);
                pPA.Start(ent, tl);
                pK.Start(ent, tl);
                pKC.Start(ent, tl);
                combPulses.To(ent, step + 1);
                combPulsesC.To(ent, step + 1);

                wP.ApplyRequests(pP);
                wPA.ApplyRequests(pPA);
                wK.Tick(pK, true);
                wKC.Tick(pKC, true);

                Assert.Throws<ArgumentNullException>(() => new PlainFuzzSystem { Motion = mA, Stats = sA, Input = pIn, Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(null!, pP, true));
                Assert.Throws<ArgumentNullException>(() => new PlainFuzzSystem { Motion = mA, Stats = sA, Input = pIn, Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(wP, null!, true));
                Assert.Throws<ArgumentException>(() => new PlainFuzzSystem { Motion = mA, Stats = new PlainStats[entities - 1], Input = pIn, Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(wP, pP, true));
                Assert.Throws<ArgumentException>(() => new PlainFuzzSystem { Motion = new PlainMotion[entities + 1], Stats = new PlainStats[entities + 1], Input = new PlainInput[entities + 1], Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(wP, pP, true));

                Assert.Throws<ArgumentNullException>(() => new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = kIn }.Run(null!, pK, true));
                Assert.Throws<ArgumentNullException>(() => new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = kIn }.Run(wK, null!, true));
                Assert.Throws<ArgumentException>(() => new KindFuzzSystem { CombineCol = cA, EachCol = new KindEachCol[entities - 1], IdleCol = iA, FloatCol = fA, Input = kIn }.Run(wK, pK, true));
                Assert.Throws<ArgumentException>(() => new KindFuzzSystem { CombineCol = new KindIntCol[entities + 1], EachCol = new KindEachCol[entities + 1], IdleCol = new KindIdleCol[entities + 1], FloatCol = new KindFloatCol[entities + 1], Input = new KindInputCol[entities + 1] }.Run(wK, pK, true));

                new PlainFuzzSystem { Motion = mA, Stats = sA, Input = pIn, Requests = wP.Requests(pP), Cues = cuesP.Writer }.Run(wP, pP, true);
                new PlainFuzzSystem { Motion = mC, Stats = sC, Input = pIn, Requests = wPA.Requests(pPA), Cues = cuesPA.Writer }.Run(wPA, pPA, true);
                wP.Advance(pP, true);
                wPA.Advance(pPA, true);
                cuesP.Drain();
                cuesPA.Drain();

                new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = kIn }.Run(wK, pK, true);
                new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = kIn }.Run(wKC, pKC, true);

                var err =
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mA), MemoryMarshal.AsBytes<PlainMotion>(mC), "Plain Motion") ??
                    FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cA), MemoryMarshal.AsBytes<KindIntCol>(cC), "Kind Comb") ??
                    FuzzRunner.ComparePlaybacks(pP, pPA) ??
                    FuzzRunner.ComparePlaybacks(pK, pKC);
                if (err != null)
                    FuzzRunner.Fail("B5-ApiAndRunThrows", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350002UL, iter, err);
                Assert.Equal(ksC.DroppedPulses, ks.DroppedPulses);
            }
        });
    }

    [Fact]
    public void Reload_CorruptLibraryBytes_LeavesPreviousValidWorldUsable()
    {
        FuzzRunner.Run("B5-ReloadCorrupt", 0x42350003UL, 12, (rng, iter) =>
        {
            const int entities = 6;
            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, 4, 2, 30, true, true, true);
            var kindLib = LibraryGen.BuildKindLibrary(ref rng, 4, 2, 30, true, true, true, true);

            using var wPA = new TimelineWorld(plainLib.Bytes);
            using var wPC = new TimelineWorld(plainLib.Bytes);
            using var pPA = new PlaybackTable(entities, 4);
            using var pPC = new PlaybackTable(entities, 4);
            using var cPA = new Events<FuzzCue>(1024);
            using var cPC = new Events<FuzzCue>(1024);

            using var wKA = new TimelineWorld(kindLib.Bytes);
            using var wKC = new TimelineWorld(kindLib.Bytes);
            using var pKA = new PlaybackTable(entities, 4);
            using var pKC = new PlaybackTable(entities, 4);
            using var kA = new KindStore(wKA, pKA);
            using var kC = new KindStore(wKC, pKC);

            var mA = new PlainMotion[entities];
            var mC = new PlainMotion[entities];
            var sA = new PlainStats[entities];
            var sC = new PlainStats[entities];
            var pIn = new PlainInput[entities];
            var cA = new KindIntCol[entities];
            var cC = new KindIntCol[entities];
            var eA = new KindEachCol[entities];
            var eC = new KindEachCol[entities];
            var iA = new KindIdleCol[entities];
            var iC = new KindIdleCol[entities];
            var fA = new KindFloatCol[entities];
            var fC = new KindFloatCol[entities];
            var kIn = new KindInputCol[entities];
            for (var e = 0; e < entities; e++)
            {
                pIn[e] = new PlainInput { Mult = 1 };
                kIn[e] = new KindInputCol { Factor = 1 };
                pPA.Start(e, (ushort)(1 + (e & 3)));
                pPC.Start(e, (ushort)(1 + (e & 3)));
                pKA.Start(e, (ushort)(1 + (e & 3)));
                pKC.Start(e, (ushort)(1 + (e & 3)));
            }

            void StepPair()
            {
                wPA.ApplyRequests(pPA);
                wPC.ApplyRequests(pPC);
                new PlainFuzzSystem { Motion = mA, Stats = sA, Input = pIn, Requests = wPA.Requests(pPA), Cues = cPA.Writer }.Run(wPA, pPA, true);
                new PlainFuzzSystem { Motion = mC, Stats = sC, Input = pIn, Requests = wPC.Requests(pPC), Cues = cPC.Writer }.Run(wPC, pPC, true);
                wPA.Advance(pPA, true);
                wPC.Advance(pPC, true);
                cPA.Drain();
                cPC.Drain();

                wKA.Tick(pKA, true);
                wKC.Tick(pKC, true);
                new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = kIn }.Run(wKA, pKA, true);
                new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = kIn }.Run(wKC, pKC, true);
            }

            StepPair();
            StepPair();

            Assert.Throws<ArgumentException>(() => wPA.Reload(CorruptLibrary(plainLib.Bytes, iter)));
            Assert.Throws<ArgumentException>(() => wKA.Reload(CorruptLibrary(kindLib.Bytes, iter)));

            StepPair();
            StepPair();

            var err =
                FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mA), MemoryMarshal.AsBytes<PlainMotion>(mC), "Plain Motion after corrupt reload") ??
                FuzzRunner.ComparePlaybacks(pPA, pPC) ??
                FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cA), MemoryMarshal.AsBytes<KindIntCol>(cC), "Kind Comb after corrupt reload") ??
                FuzzRunner.ComparePlaybacks(pKA, pKC);
            if (err != null)
                FuzzRunner.Fail("B5-ReloadCorrupt", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350003UL, iter, err);
        });
    }

    [Fact]
    public void Reload_PlainSyncMissingTimeline_DoesNotMutatePrecedingEntitiesBeforeThrow()
    {
        FuzzRunner.Run("B5-PlainSyncPartial", 0x42350004UL, 12, (rng, iter) =>
        {
            const int entities = 4;
            var (bigPlain, smallPlain, _) = BuildBigAndSmallPair(ref rng, includeKinds: false);

            using var wPA = new TimelineWorld(bigPlain);
            using var wPC = new TimelineWorld(bigPlain);
            using var pPA = new PlaybackTable(entities, 4);
            using var pPC = new PlaybackTable(entities, 4);
            using var cPA = new Events<FuzzCue>(512);
            using var cPC = new Events<FuzzCue>(512);

            var mA = new PlainMotion[entities];
            var mC = new PlainMotion[entities];
            var sA = new PlainStats[entities];
            var sC = new PlainStats[entities];
            var inp = new PlainInput[entities];
            for (var e = 0; e < entities; e++) inp[e] = new PlainInput { Mult = 1 };

            pPA.Start(0, 1); pPA.Start(1, 3); pPA.Start(2, 1);
            pPC.Start(0, 1); pPC.Start(1, 3); pPC.Start(2, 1);
            for (var s = 0; s < 10; s++)
            {
                wPA.ApplyRequests(pPA); wPC.ApplyRequests(pPC);
                new PlainFuzzSystem { Motion = mA, Stats = sA, Input = inp, Requests = wPA.Requests(pPA), Cues = cPA.Writer }.Run(wPA, pPA, true);
                new PlainFuzzSystem { Motion = mC, Stats = sC, Input = inp, Requests = wPC.Requests(pPC), Cues = cPC.Writer }.Run(wPC, pPC, true);
                wPA.Advance(pPA, true); wPC.Advance(pPC, true);
                cPA.Drain(); cPC.Drain();
            }

            wPA.Reload(smallPlain);
            Assert.Throws<ArgumentException>(() => wPA.Advance(pPA, true));

            wPA.Reload(bigPlain);
            wPC.Reload(bigPlain);
            wPA.ApplyRequests(pPA); wPC.ApplyRequests(pPC);
            new PlainFuzzSystem { Motion = mA, Stats = sA, Input = inp, Requests = wPA.Requests(pPA), Cues = cPA.Writer }.Run(wPA, pPA, true);
            new PlainFuzzSystem { Motion = mC, Stats = sC, Input = inp, Requests = wPC.Requests(pPC), Cues = cPC.Writer }.Run(wPC, pPC, true);
            wPA.Advance(pPA, true); wPC.Advance(pPC, true);
            cPA.Drain(); cPC.Drain();

            var pErr =
                FuzzRunner.ComparePlaybacks(pPA, pPC) ??
                FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<PlainMotion>(mA), MemoryMarshal.AsBytes<PlainMotion>(mC), "Plain Motion after aborted small reload");
            if (pErr != null)
                FuzzRunner.Fail("B5-PlainSyncPartial", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350004UL, iter, pErr);
        });
    }

    [Fact]
    public void Reload_KindStoreSyncMissingTimelineOrReach_DoesNotSkipRevalidationOrCorruptEntities()
    {
        FuzzRunner.Run("B5-KindSyncPartial", 0x42350006UL, 12, (rng, iter) =>
        {
            const int entities = 4;
            var (bigKind, smallKind, _) = BuildBigAndSmallPair(ref rng, includeKinds: true);

            using var wKA = new TimelineWorld(bigKind);
            using var wKC = new TimelineWorld(bigKind);
            using var pKA = new PlaybackTable(entities, 4);
            using var pKC = new PlaybackTable(entities, 4);
            using var kA = new KindStore(wKA, pKA);
            using var kC = new KindStore(wKC, pKC);

            var cA = new KindIntCol[entities];
            var cC = new KindIntCol[entities];
            var eA = new KindEachCol[entities];
            var eC = new KindEachCol[entities];
            var iA = new KindIdleCol[entities];
            var iC = new KindIdleCol[entities];
            var fA = new KindFloatCol[entities];
            var fC = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++) inp[e] = new KindInputCol { Factor = 1 };

            pKA.Start(0, 1); pKA.Start(1, 3); pKA.Start(2, 1);
            pKC.Start(0, 1); pKC.Start(1, 3); pKC.Start(2, 1);
            for (var s = 0; s < 10; s++)
            {
                wKA.Tick(pKA, true); wKC.Tick(pKC, true);
                new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(wKA, pKA, true);
                new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = inp }.Run(wKC, pKC, true);
            }

            wKA.Reload(smallKind);
            Assert.Throws<ArgumentException>(() => wKA.Tick(pKA, true));
            Assert.Throws<ArgumentException>(() => wKA.Tick(pKA, true));

            foreach (var (world, players) in new[] { (wKA, pKA), (wKC, pKC) })
            {
                world.Reload(bigKind);
                players.Reset(1);
                world.Tick(players, true);
                world.Reload(smallKind);
                world.Tick(players, true);
            }
            new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(wKA, pKA, true);
            new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = inp }.Run(wKC, pKC, true);

            var kErr =
                FuzzRunner.ComparePlaybacks(pKA, pKC) ??
                FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cA), MemoryMarshal.AsBytes<KindIntCol>(cC), "Kind Comb after reset-and-continue small reload");
            if (kErr != null)
                FuzzRunner.Fail("B5-KindSyncPartial", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350006UL, iter, kErr);
        });
    }

    [Fact]
    public void KindStoreJoin_SizeMismatchOnReload_ThrowsCleanlyAndRecoversWhenValidLibraryReloaded()
    {
        var wrongBytes = WrongKindSizeLib.Value;
        FuzzRunner.Run("B5-JoinMismatch", 0x42350005UL, 10, (rng, iter) =>
        {
            const int entities = 4;
            var goodLib = LibraryGen.BuildKindLibrary(ref rng, 4, 2, 24, true, true, true, true);
            using var wA = new TimelineWorld(goodLib.Bytes);
            using var wC = new TimelineWorld(goodLib.Bytes);
            using var pA = new PlaybackTable(entities, 4);
            using var pC = new PlaybackTable(entities, 4);
            using var kA = new KindStore(wA, pA);
            using var kC = new KindStore(wC, pC);

            var cA = new KindIntCol[entities];
            var cC = new KindIntCol[entities];
            var eA = new KindEachCol[entities];
            var eC = new KindEachCol[entities];
            var iA = new KindIdleCol[entities];
            var iC = new KindIdleCol[entities];
            var fA = new KindFloatCol[entities];
            var fC = new KindFloatCol[entities];
            var inp = new KindInputCol[entities];
            for (var e = 0; e < entities; e++)
            {
                inp[e] = new KindInputCol { Factor = 1 };
                pA.Start(e, (ushort)(1 + (e & 3)));
                pC.Start(e, (ushort)(1 + (e & 3)));
            }

            wA.Tick(pA, true);
            wC.Tick(pC, true);
            new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(wA, pA, true);
            new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = inp }.Run(wC, pC, true);

            wA.Reload(wrongBytes);
            wA.ApplyRequests(pA);
            Assert.Throws<InvalidOperationException>(() =>
                new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(wA, pA, true));

            wA.Reload(goodLib.Bytes);
            wC.Reload(goodLib.Bytes);
            wA.Tick(pA, true);
            wC.Tick(pC, true);
            new KindFuzzSystem { CombineCol = cA, EachCol = eA, IdleCol = iA, FloatCol = fA, Input = inp }.Run(wA, pA, true);
            new KindFuzzSystem { CombineCol = cC, EachCol = eC, IdleCol = iC, FloatCol = fC, Input = inp }.Run(wC, pC, true);

            var err =
                FuzzRunner.ComparePlaybacks(pA, pC) ??
                FuzzRunner.CompareBytes(MemoryMarshal.AsBytes<KindIntCol>(cA), MemoryMarshal.AsBytes<KindIntCol>(cC), "Kind Comb after Join mismatch");
            if (err != null)
                FuzzRunner.Fail("B5-JoinMismatch", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42350005UL, iter, err);
        });
    }
}
