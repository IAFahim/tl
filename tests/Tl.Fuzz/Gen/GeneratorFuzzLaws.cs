using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Fuzz.Runtime;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Gen;

public sealed unsafe class GeneratorFuzzLaws
{
    static readonly ImmutableArray<MetadataReference> BaseReferences = BuildReferences();
    static readonly CSharpParseOptions ParseOpts = new(LanguageVersion.Latest);

    static ImmutableArray<MetadataReference> BuildReferences()
    {
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
        return refs.ToImmutable();
    }

    static ulong NextSeed(ref FuzzRandom rng) => ((ulong)rng.NextUInt() << 32) | rng.NextUInt();

    sealed class GenDomainContext : AssemblyLoadContext
    {
        public GenDomainContext(string name) : base(name, isCollectible: true) { }
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    static (byte[] Pe, ImmutableArray<Diagnostic> GenDiags, ImmutableArray<Diagnostic> CompileErrors) CompileWithGenerator(string asmName, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, ParseOpts, asmName + ".cs");
        var compilation = CSharpCompilation.Create(
            asmName,
            [tree],
            BaseReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));

        var generator = new TimelineIncrementalGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], parseOptions: ParseOpts);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var driverDiags);

        var genErrors = driver.GetRunResult().Diagnostics.Concat(driverDiags).ToImmutableArray();
        using var pe = new MemoryStream();
        var emit = outputCompilation.Emit(pe);
        var compileErrors = emit.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        return (emit.Success ? pe.ToArray() : [], genErrors, compileErrors);
    }

    enum ValueShape { B1, B2, B4, B8, B16 }

    struct KindSpec
    {
        public int Index;
        public ValueShape Shape;
        public bool Each;
        public int Accepts;
        public int GroupId;
        public bool Idle;
        public bool FoldsPulses;
        public int DerivedMode;
        public int Order;
    }

    static string ValueTypeName(ValueShape s) => s switch
    {
        ValueShape.B1 => "Val1",
        ValueShape.B2 => "Val2",
        ValueShape.B4 => "Val4",
        ValueShape.B8 => "Val8",
        _ => "Val16",
    };

    static string ValueHelpers() => """
        public struct Val1 { public byte A; }
        public struct Val2 { public short A; }
        public struct Val4 { public int A; }
        public struct Val8 { public long A; }
        public struct Val16 { public long A; public long B; }
        """;

    static void AppendAddExpr(StringBuilder sb, ValueShape s, string lhs, string rhs)
    {
        switch (s)
        {
            case ValueShape.B1: sb.Append($"{lhs}.A = (byte)unchecked({lhs}.A + {rhs}.A);"); break;
            case ValueShape.B2: sb.Append($"{lhs}.A = (short)unchecked({lhs}.A + {rhs}.A);"); break;
            case ValueShape.B4: sb.Append($"{lhs}.A = unchecked({lhs}.A + {rhs}.A);"); break;
            case ValueShape.B8: sb.Append($"{lhs}.A = unchecked({lhs}.A + {rhs}.A);"); break;
            case ValueShape.B16: sb.Append($"{lhs}.A = unchecked({lhs}.A + {rhs}.A); {lhs}.B = unchecked({lhs}.B + {rhs}.B);"); break;
        }
    }

    static void AppendMakeExpr(StringBuilder sb, ValueShape s, string aExpr, string bExpr)
    {
        switch (s)
        {
            case ValueShape.B1: sb.Append($"new Val1 {{ A = (byte)({aExpr}) }}"); break;
            case ValueShape.B2: sb.Append($"new Val2 {{ A = (short)({aExpr}) }}"); break;
            case ValueShape.B4: sb.Append($"new Val4 {{ A = (int)({aExpr}) }}"); break;
            case ValueShape.B8: sb.Append($"new Val8 {{ A = (long)({aExpr}) }}"); break;
            case ValueShape.B16: sb.Append($"new Val16 {{ A = (long)({aExpr}), B = (long)({bExpr}) }}"); break;
        }
    }

    static string ColumnStateType(ValueShape s, int colIdx) => $"public struct Col_{colIdx} {{ public {ValueTypeName(s)} V; }}";

    static string GenerateRandomValidDomain(
        ref FuzzRandom rng,
        string ns,
        bool allowGroups,
        bool forceGroup,
        bool? forceFoldPulses,
        int minKinds,
        int maxKinds,
        int minCols,
        int maxCols,
        out KindSpec[] kinds,
        out int trackCount,
        out bool hasMarker,
        out int[] trackKinds)
    {
        var kindCount = minKinds + rng.NextInt(maxKinds - minKinds + 1);
        var groupCount = forceGroup ? Math.Min(kindCount >= 2 ? 1 : 0, 1) : (allowGroups && kindCount >= 2 && rng.NextBool() ? 1 + rng.NextInt(Math.Min(2, kindCount / 2)) : 0);
        var grpSizes = new int[groupCount];
        var nextGrpKind = 0;
        for (var g = 0; g < groupCount; g++)
        {
            var remaining = kindCount - nextGrpKind - (groupCount - 1 - g) * 2;
            var sz = Math.Min(6, Math.Max(2, 2 + rng.NextInt(Math.Max(1, Math.Min(5, remaining - 1)))));
            grpSizes[g] = sz;
            nextGrpKind += sz;
        }

        kinds = new KindSpec[kindCount];
        var kAssign = 0;
        for (var g = 0; g < groupCount; g++)
        {
            for (var m = 0; m < grpSizes[g] && kAssign < kindCount; m++, kAssign++)
            {
                var each = forceFoldPulses.HasValue ? false : rng.NextInt(3) == 0;
                var acc = rng.NextInt(3);
                kinds[kAssign] = new KindSpec
                {
                    Index = kAssign,
                    Shape = (ValueShape)rng.NextInt(5),
                    Each = each,
                    Accepts = acc,
                    GroupId = g + 1,
                    Idle = false,
                    FoldsPulses = forceFoldPulses ?? (!each && acc != 0 && rng.NextBool()),
                    Order = rng.NextInt(7) - 3,
                };
            }
        }
        for (; kAssign < kindCount; kAssign++)
        {
            var each = forceFoldPulses.HasValue ? false : rng.NextInt(3) == 0;
            var acc = rng.NextInt(3);
            kinds[kAssign] = new KindSpec
            {
                Index = kAssign,
                Shape = (ValueShape)rng.NextInt(5),
                Each = each,
                Accepts = acc,
                GroupId = 0,
                Idle = acc == 0 && rng.NextBool(),
                FoldsPulses = forceFoldPulses ?? (!each && acc != 0 && rng.NextBool()),
                Order = rng.NextInt(7) - 3,
            };
        }

        var extraCols = Math.Max(0, minCols - kindCount);
        var totalCols = Math.Min(30, Math.Max(kindCount, Math.Min(maxCols, kindCount + extraCols)));

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Tl;");
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine(ValueHelpers());
        for (var c = 0; c < totalCols; c++)
        {
            var sh = c < kindCount ? kinds[c].Shape : ValueShape.B4;
            sb.AppendLine(ColumnStateType(sh, c));
        }

        sb.AppendLine("public struct DynClip { public int A; public int B; }");
        sb.AppendLine("public struct DynTrack : IBlend<DynClip> { public int Mult; public void Blend(in DynClip first, in DynClip second, float factor, out DynClip result) => result = first; }");
        sb.AppendLine("public struct DynMarker { public int D; }");
        sb.AppendLine("public struct PlainClip { public int P; }");
        sb.AppendLine("public readonly struct PlainTrack : IBlend<PlainClip> { public int S { get; init; } public void Blend(in PlainClip first, in PlainClip second, float factor, out PlainClip result) => result = first; }");

        sb.AppendLine("[TimelineSystem]");
        sb.AppendLine("public ref partial struct DynSystem");
        sb.AppendLine("{");
        for (var c = 0; c < totalCols; c++)
            sb.AppendLine($"    public Span<Col_{c}> C{c};");
        sb.AppendLine("}");

        for (var k = 0; k < kindCount; k++)
        {
            ref readonly var ks = ref kinds[k];
            var modeStr = ks.Each ? "KindMode.Each" : "KindMode.Combine";
            var accStr = ks.Accepts == 0 ? "KindEntries.Spans" : ks.Accepts == 1 ? "KindEntries.Pulses" : "KindEntries.Both";
            var grpStr = ks.GroupId > 0 ? $", Group = \"G{ks.GroupId}\"" : "";
            var foldStr = ks.FoldsPulses ? ", FoldsPulses = true" : "";
            sb.AppendLine($"[TimelineKind(Accepts = {accStr}, Mode = {modeStr}, Order = {ks.Order}{grpStr}{foldStr})]");
            sb.AppendLine($"public struct Kind_{k}");
            sb.AppendLine("{");
            if (!ks.Each)
            {
                sb.Append($"    public static void Combine(ref {ValueTypeName(ks.Shape)} total, in {ValueTypeName(ks.Shape)} next) {{ ");
                AppendAddExpr(sb, ks.Shape, "total", "next");
                sb.AppendLine(" }");
            }
            sb.Append($"    public static void Apply(in {ValueTypeName(ks.Shape)} total, ref Col_{k} col) {{ ");
            AppendAddExpr(sb, ks.Shape, "col.V", "total");
            sb.AppendLine(" }");
            if (ks.Idle)
            {
                sb.AppendLine($"    public static void Idle(ref Col_{k} col) {{ col.V = default; }}");
            }
            sb.AppendLine("}");
        }

        trackCount = Math.Max(1, Math.Min(4, 1 + rng.NextInt(4)));
        trackKinds = new int[trackCount];
        var coveredKinds = 0;
        var extraFeedConsumer = new List<int>();
        for (var t = 0; t < trackCount; t++)
        {
            sb.AppendLine($"public readonly struct Tr_{t} : IBlend<DynClip> {{ public int Salt {{ get; init; }} public void Blend(in DynClip first, in DynClip second, float factor, out DynClip result) => result = first; }}");
            sb.AppendLine($"[TimelineSystemMember(typeof(DynSystem))]");
            sb.AppendLine($"public readonly struct Con_{t} : ITrack<Tr_{t}, DynClip>");
            sb.AppendLine("{");
            var fedHere = 0;
            for (var k = t; k < kindCount; k += trackCount)
            {
                trackKinds[t] |= 1 << k;
                var sh = kinds[k].Shape;
                if (kinds[k].Accepts is 0 or 2)
                {
                    sb.Append($"    [Feeds(typeof(Kind_{k}))] public static {ValueTypeName(sh)} Span_{k}(in Frame<Tr_{t}, DynClip> f) => ");
                    AppendMakeExpr(sb, sh, $"f.Direction * (f.Clip.A + f.Track.Salt + {k + 1})", $"f.Direction * (f.Clip.B - {k})");
                    sb.AppendLine(";");
                }
                if (kinds[k].Accepts is 1 or 2)
                {
                    sb.Append($"    [Pulse(typeof(Kind_{k}), On = ClipEvent.Start)] public static {ValueTypeName(sh)} PStart_{k}(in Frame<Tr_{t}, DynClip> f) => ");
                    AppendMakeExpr(sb, sh, $"f.Direction * (f.Clip.B + {k + 3})", $"f.Clip.A + 7");
                    sb.AppendLine(";");
                    sb.Append($"    [Pulse(typeof(Kind_{k}), On = ClipEvent.End)] public static {ValueTypeName(sh)} PEnd_{k}(in Frame<Tr_{t}, DynClip> f) => ");
                    AppendMakeExpr(sb, sh, $"f.Direction * (f.Clip.A - {k + 2})", $"f.Clip.B + 5");
                    sb.AppendLine(";");
                }
                fedHere++;
                coveredKinds |= 1 << k;
            }
            if (fedHere == 0)
            {
                sb.AppendLine($"    public static void ExecuteActive(in Frame<Tr_{t}, DynClip> f, ref Col_0 c0) {{ c0.V.A += 1; }}");
            }
            sb.AppendLine("}");
            extraFeedConsumer.Add(t);
        }

        hasMarker = false;
        for (var k = 0; k < kindCount; k++)
        {
            if (kinds[k].Accepts is 1 or 2)
            {
                hasMarker = true;
                break;
            }
        }

        if (hasMarker)
        {
            sb.AppendLine("[TimelineSystemMember(typeof(DynSystem))]");
            sb.AppendLine("public readonly struct MarkerCon : IMarker<DynMarker>");
            sb.AppendLine("{");
            for (var k = 0; k < kindCount; k++)
            {
                if (kinds[k].Accepts is 1 or 2)
                {
                    var sh = kinds[k].Shape;
                    sb.Append($"    [Pulse(typeof(Kind_{k}), On = ClipEvent.Marker)] public static {ValueTypeName(sh)} Mk_{k}(in MarkerFrame<DynMarker> f) => ");
                    AppendMakeExpr(sb, sh, $"f.Direction * (f.Marker.D + {k + 1})", $"f.Marker.D * 3 + {k}");
                    sb.AppendLine(";");
                }
            }
            sb.AppendLine("}");
        }

        if (totalCols > kindCount)
        {
            var sigCols = new List<string>();
            for (var c = 0; c < totalCols; c++) sigCols.Add($"ref Col_{c} p{c}");
            sb.AppendLine("[TimelineSystemMember(typeof(DynSystem))]");
            sb.AppendLine("public readonly struct PlainBigCon : ITrack<PlainTrack, PlainClip>");
            sb.AppendLine("{");
            sb.Append($"    public static void ExecuteActive(in Frame<PlainTrack, PlainClip> f, {string.Join(", ", sigCols)}) {{ ");
            for (var c = kindCount; c < totalCols; c++) sb.Append($"p{c}.V.A += f.Direction * (f.Clip.P + {c}); ");
            sb.AppendLine("}");
            sb.AppendLine("}");
        }

        var firstPulseKind = -1;
        for (var k = 0; k < kindCount; k++)
        {
            if (kinds[k].Accepts is 1 or 2)
            {
                firstPulseKind = k;
                break;
            }
        }
        sb.AppendLine("public static class DynHost");
        sb.AppendLine("{");
        sb.AppendLine("    public static byte[] Execute(byte[] library, int timelines, int entities, int steps, int kindCount, int pulseKind, int pulseEvery, int pulseVal, bool useReverse)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var world = new TimelineWorld(library);");
        sb.AppendLine("        using var players = new PlaybackTable(entities, 4);");
        sb.AppendLine("        using var ks = new KindStore(world, players);");
        for (var c = 0; c < totalCols; c++)
            sb.AppendLine($"        var c{c} = new Col_{c}[entities];");
        sb.AppendLine("        var snap = new List<byte>();");
        sb.AppendLine("        var rng = 0x13572468u;");
        sb.AppendLine("        for (var s = 0; s < steps; s++)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (var e = 0; e < entities; e++)");
        sb.AppendLine("            {");
        sb.AppendLine("                rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;");
        sb.AppendLine("                var op = rng % 5;");
        sb.AppendLine("                var tl = (ushort)(1 + (rng >> 8) % Math.Max(1, timelines));");
        sb.AppendLine("                if (op <= 1) players.Start(e, tl, (ushort)(rng & 3));");
        sb.AppendLine("                else if (op == 2) players.SetSpeed(e, tl, (ushort)(16 + (rng >> 12) % 64));");
        sb.AppendLine("                else if (op == 3 && (s % 4 == 0)) players.Stop(e, tl);");
        sb.AppendLine("            }");
        if (firstPulseKind >= 0)
        {
            var vt = ValueTypeName(kinds[firstPulseKind].Shape);
            var castPrefix = kinds[firstPulseKind].Shape == ValueShape.B1 ? "(byte)" : kinds[firstPulseKind].Shape == ValueShape.B2 ? "(short)" : "";
            sb.AppendLine($"            if (pulseEvery > 0 && s % pulseEvery == 0)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var q = ks.Pulses<{vt}>(typeof(Kind_{firstPulseKind}).FullName!);");
            sb.AppendLine($"                for (var e = 0; e < entities; e++) q.To(e, new {vt} {{ A = {castPrefix}(s + e + pulseVal) }});");
            sb.AppendLine("            }");
        }
        sb.AppendLine("            var fwd = !useReverse || (s % 7 != 4);");
        sb.AppendLine("            world.Tick(players, fwd);");
        sb.Append("            new DynSystem { ");
        for (var c = 0; c < totalCols; c++) sb.Append($"C{c} = c{c}{(c + 1 < totalCols ? ", " : "")}");
        sb.AppendLine(" }.Run(world, players, fwd);");
        for (var c = 0; c < totalCols; c++)
            sb.AppendLine($"            snap.AddRange(MemoryMarshal.AsBytes<Col_{c}>(c{c}).ToArray());");
        sb.AppendLine("        }");
        sb.AppendLine("        return snap.ToArray();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    static byte[] BakeGeneratedLibrary(Assembly asm, string ns, int trackCount, bool hasMarker, ref FuzzRandom rng, int timelines, int duration, bool hasPlain30)
    {
        var names = new string[timelines];
        for (var i = 0; i < timelines; i++) names[i] = $"gen_{i + 1:D2}";
        var channels = new[] { "gch_0", "gch_1" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var clipType = asm.GetType($"{ns}.DynClip", true)!;
        var markerType = hasMarker ? asm.GetType($"{ns}.DynMarker", true)! : null;
        var plainTrackType = hasPlain30 ? asm.GetType($"{ns}.PlainTrack", true)! : null;
        var plainClipType = hasPlain30 ? asm.GetType($"{ns}.PlainClip", true)! : null;

        var list = new TypedTimeline[timelines];
        for (var i = 0; i < timelines; i++)
        {
            var dur = (uint)(Math.Max(6, duration / 2 + rng.NextInt(Math.Max(1, duration / 2))));
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = rng.NextBool() ? channels[rng.NextInt(channels.Length)] : null,
                Loops = rng.NextInt(3) == 0,
            };
            for (var t = 0; t < trackCount; t++)
            {
                var trType = asm.GetType($"{ns}.Tr_{t}", true)!;
                var tr = new TypedTrack
                {
                    Name = $"t_{t}",
                    Type = trType,
                    Data = BitConverter.GetBytes(1 + rng.NextInt(5)),
                };
                var a0 = 1 + rng.NextInt(9);
                var b0 = rng.NextInt(9) - 4;
                var cBytes = new byte[8];
                MemoryMarshal.Write(cBytes.AsSpan(0, 4), in a0);
                MemoryMarshal.Write(cBytes.AsSpan(4, 4), in b0);
                var end1 = Math.Max(2u, Math.Min(dur, 2u + (uint)rng.NextInt((int)Math.Max(1, dur - 2))));
                tr.Clips.Add(new TypedClip { Type = clipType, Start = 1, End = end1, Data = cBytes });
                if (end1 + 2 < dur)
                {
                    var s2 = end1 >= 3 ? end1 - 1 : end1;
                    var e2 = Math.Min(dur, s2 + 2 + (uint)rng.NextInt((int)(dur - s2 - 1)));
                    var a1 = -2 - rng.NextInt(7);
                    var b1 = 1 + rng.NextInt(7);
                    var cBytes2 = new byte[8];
                    MemoryMarshal.Write(cBytes2.AsSpan(0, 4), in a1);
                    MemoryMarshal.Write(cBytes2.AsSpan(4, 4), in b1);
                    tr.Clips.Add(new TypedClip { Type = clipType, Start = s2, End = e2, Data = cBytes2 });
                }
                tl.Tracks.Add(tr);
            }
            if (plainTrackType != null && plainClipType != null)
            {
                var ptr = new TypedTrack { Name = "plain", Type = plainTrackType, Data = BitConverter.GetBytes(1) };
                ptr.Clips.Add(new TypedClip { Type = plainClipType, Start = 0, End = dur, Data = BitConverter.GetBytes(1 + rng.NextInt(3)) });
                tl.Tracks.Add(ptr);
            }
            if (markerType != null && dur > 3)
            {
                var mc = 1 + rng.NextInt(3);
                for (var m = 0; m < mc; m++)
                {
                    tl.Markers.Add(new TypedMarker
                    {
                        Type = markerType,
                        Tick = 1 + (uint)rng.NextInt((int)dur - 2),
                        Data = BitConverter.GetBytes(1 + rng.NextInt(11)),
                    });
                }
            }
            list[i] = tl;
        }

        var kinds = LibraryBake.Kinds([asm]);
        var parts = new List<LibraryPart>(timelines);
        for (var i = 0; i < timelines; i++)
        {
            var tlb = LibraryBake.BakeTyped(list[i], manifest);
            parts.Add(new LibraryPart
            {
                Name = list[i].Name!,
                Tlb = tlb,
                Kinds = LibraryBake.KindPayload(list[i].Name!, tlb, kinds),
                Next = (ushort)LibraryAssembler.NextOf(tlb),
            });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = channels.Length;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }

    [Fact]
    public void ValidRandomDomains_CompileZeroErrorsBakeAndRunDeterministic()
    {
        FuzzRunner.Run("B2-ValidGenDomains", 0x42320001UL, 4, (rng, iter) =>
        {
            var source = GenerateRandomValidDomain(
                ref rng, "GenValid", true, false, null, 1, 8, 1, 8,
                out var kinds, out var trackCount, out var hasMarker, out _);

            var (pe, genDiags, compileErrors) = CompileWithGenerator($"GenValid_{iter}", source);
            var genErrs = genDiags.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (genErrs.Count > 0 || compileErrors.Length > 0 || pe.Length == 0)
            {
                FuzzRunner.Fail("B2-ValidGenDomains", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320001UL, iter,
                    $"compile failed: gen={string.Join("; ", genErrs)} compile={string.Join("; ", compileErrors)}");
            }

            var alc = new GenDomainContext($"GenValidCtx_{iter}");
            try
            {
                var asm = alc.LoadFromStream(new MemoryStream(pe));
                var bakeRng = new FuzzRandom(NextSeed(ref rng));
                var lib = BakeGeneratedLibrary(asm, "GenValid", trackCount, hasMarker, ref bakeRng, 6, 32, false);
                var host = asm.GetType("GenValid.DynHost", true)!;
                var exec = host.GetMethod("Execute")!;
                var r1 = (byte[])exec.Invoke(null, [lib, 6, 4, 24, kinds.Length, 0, 3, 2, true])!;
                var r2 = (byte[])exec.Invoke(null, [lib, 6, 4, 24, kinds.Length, 0, 3, 2, true])!;
                var diff = FuzzRunner.CompareBytes(r1, r2, "replay");
                if (diff != null)
                    FuzzRunner.Fail("B2-ValidGenDomains", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320001UL, iter, diff);
            }
            finally
            {
                alc.Unload();
            }
        });
    }

    [Fact]
    public void GroupedDomains_MatchUngroupedTwinsEveryTick()
    {
        FuzzRunner.Run("B2-GroupedGenVsUngrouped", 0x42320002UL, 4, (rng, iter) =>
        {
            var memberCount = 2 + rng.NextInt(5);
            var shapes = new ValueShape[memberCount];
            var each = new bool[memberCount];
            var accepts = new int[memberCount];
            for (var k = 0; k < memberCount; k++)
            {
                shapes[k] = (ValueShape)rng.NextInt(5);
                each[k] = rng.NextInt(3) == 0;
                accepts[k] = rng.NextInt(3);
            }
            accepts[0] = 2;
            accepts[1] = 2;

            static string EmitPairDomain(ValueShape[] sh, bool[] isEach, int[] acc, bool grouped)
            {
                var sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Runtime.InteropServices;");
                sb.AppendLine("using Tl;");
                sb.AppendLine("namespace GenGrp;");
                sb.AppendLine(ValueHelpers());
                for (var k = 0; k < sh.Length; k++)
                    sb.AppendLine(ColumnStateType(sh[k], k));
                sb.AppendLine("public struct GClip { public int A; public int B; }");
                sb.AppendLine("public readonly struct GTrack : IBlend<GClip> { public int Salt { get; init; } public void Blend(in GClip a, in GClip b, float t, out GClip r) => r = a; }");
                sb.AppendLine("public struct GMarker { public int D; }");
                sb.AppendLine("[TimelineSystem] public ref partial struct GrpSys {");
                for (var k = 0; k < sh.Length; k++) sb.AppendLine($"    public Span<Col_{k}> C{k};");
                sb.AppendLine("}");

                var grpProp = grouped ? ", Group = \"FuzzMix\"" : "";
                for (var k = 0; k < sh.Length; k++)
                {
                    var m = isEach[k] ? "KindMode.Each" : "KindMode.Combine";
                    var a = acc[k] == 0 ? "KindEntries.Spans" : acc[k] == 1 ? "KindEntries.Pulses" : "KindEntries.Both";
                    sb.AppendLine($"[TimelineKind(Accepts = {a}, Mode = {m}{grpProp})] public struct K_{k} {{");
                    if (!isEach[k])
                    {
                        sb.Append($"    public static void Combine(ref {ValueTypeName(sh[k])} t, in {ValueTypeName(sh[k])} n) {{ ");
                        AppendAddExpr(sb, sh[k], "t", "n");
                        sb.AppendLine(" }");
                    }
                    sb.Append($"    public static void Apply(in {ValueTypeName(sh[k])} t, ref Col_{k} c) {{ ");
                    AppendAddExpr(sb, sh[k], "c.V", "t");
                    sb.AppendLine(" }");
                    sb.AppendLine("}");

                    sb.AppendLine($"[TimelineSystemMember(typeof(GrpSys))] public readonly struct C_{k} : ITrack<GTrack, GClip>");
                    sb.AppendLine("{");
                    if (acc[k] is 0 or 2)
                    {
                        sb.Append($"    [Feeds(typeof(K_{k}))] public static {ValueTypeName(sh[k])} S(in Frame<GTrack, GClip> f) => ");
                        AppendMakeExpr(sb, sh[k], $"f.Direction * (f.Clip.A + {k + 1})", $"f.Direction * (f.Clip.B - {k})");
                        sb.AppendLine(";");
                    }
                    if (acc[k] is 1 or 2)
                    {
                        sb.Append($"    [Pulse(typeof(K_{k}), On = ClipEvent.Start)] public static {ValueTypeName(sh[k])} P0(in Frame<GTrack, GClip> f) => ");
                        AppendMakeExpr(sb, sh[k], $"f.Direction * (f.Clip.B + {k + 2})", $"f.Clip.A + 3");
                        sb.AppendLine(";");
                        sb.Append($"    [Pulse(typeof(K_{k}), On = ClipEvent.End)] public static {ValueTypeName(sh[k])} P1(in Frame<GTrack, GClip> f) => ");
                        AppendMakeExpr(sb, sh[k], $"f.Direction * (f.Clip.A + {k + 5})", $"f.Clip.B + 9");
                        sb.AppendLine(";");
                    }
                    sb.AppendLine("}");
                }

                sb.AppendLine("[TimelineSystemMember(typeof(GrpSys))] public readonly struct MkCon : IMarker<GMarker>");
                sb.AppendLine("{");
                for (var k = 0; k < sh.Length; k++)
                {
                    if (acc[k] is 1 or 2)
                    {
                        sb.Append($"    [Pulse(typeof(K_{k}), On = ClipEvent.Marker)] public static {ValueTypeName(sh[k])} M_{k}(in MarkerFrame<GMarker> f) => ");
                        AppendMakeExpr(sb, sh[k], $"f.Direction * (f.Marker.D + {k + 1})", $"f.Marker.D + {k * 7}");
                        sb.AppendLine(";");
                    }
                }
                sb.AppendLine("}");

                sb.AppendLine("public static class GrpHost {");
                sb.AppendLine("    static void PushPulse(KindStore ks, int k, ValueType shapeDummy, int e, int val) { }");
                sb.AppendLine("    public static byte[] Play(byte[] lib, int timelines, int entities, int steps, int kindCount, uint pulseSeed)");
                sb.AppendLine("    {");
                sb.AppendLine("        using var w = new TimelineWorld(lib);");
                sb.AppendLine("        using var p = new PlaybackTable(entities, 4);");
                sb.AppendLine("        using var ks = new KindStore(w, p);");
                for (var k = 0; k < sh.Length; k++) sb.AppendLine($"        var col{k} = new Col_{k}[entities];");
                sb.AppendLine("        var list = new List<byte>();");
                sb.AppendLine("        var r = 0x55512345u ^ pulseSeed;");
                sb.AppendLine("        for (var step = 0; step < steps; step++) {");
                sb.AppendLine("            for (var e = 0; e < entities; e++) {");
                sb.AppendLine("                r ^= r << 13; r ^= r >> 17; r ^= r << 5;");
                sb.AppendLine("                var tl = (ushort)(1 + (r >> 8) % Math.Max(1, timelines));");
                sb.AppendLine("                if ((r & 3) <= 1) p.Start(e, tl, (ushort)(r & 3));");
                sb.AppendLine("                else if ((r & 3) == 2) p.SetSpeed(e, tl, (ushort)(16 + (r >> 12) % 48));");
                sb.AppendLine("                else if ((step % 5 == 0)) p.Stop(e, tl);");
                sb.AppendLine("            }");
                for (var k = 0; k < sh.Length; k++)
                {
                    if (acc[k] is 1 or 2)
                    {
                        var vt = ValueTypeName(sh[k]);
                        sb.AppendLine($"            if ((step & 1) == 0) {{ var q{k} = ks.Pulses<{vt}>(typeof(K_{k}).FullName!); for (var e = 0; e < entities; e++) q{k}.To(e, new {vt} {{ A = {(sh[k] == ValueShape.B1 ? "(byte)" : sh[k] == ValueShape.B2 ? "(short)" : "")}(step + e + {k + 1}) }}); }}");
                    }
                }
                sb.AppendLine("            var fwd = step % 6 != 4;");
                sb.AppendLine("            w.Tick(p, fwd);");
                sb.Append("            new GrpSys { ");
                for (var k = 0; k < sh.Length; k++) sb.Append($"C{k} = col{k}{(k + 1 < sh.Length ? ", " : "")}");
                sb.AppendLine(" }.Run(w, p, fwd);");
                for (var k = 0; k < sh.Length; k++)
                    sb.AppendLine($"            list.AddRange(MemoryMarshal.AsBytes<Col_{k}>(col{k}).ToArray());");
                sb.AppendLine("        }");
                sb.AppendLine("        return list.ToArray();");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                return sb.ToString();
            }

            var srcGrp = EmitPairDomain(shapes, each, accepts, true);
            var srcUng = EmitPairDomain(shapes, each, accepts, false);
            var (peG, gdG, ceG) = CompileWithGenerator($"GenGrp_{iter}", srcGrp);
            var (peU, gdU, ceU) = CompileWithGenerator($"GenUngrp_{iter}", srcUng);
            if (ceG.Length > 0 || ceU.Length > 0 || gdG.Any(static d => d.Severity == DiagnosticSeverity.Error) || gdU.Any(static d => d.Severity == DiagnosticSeverity.Error))
            {
                FuzzRunner.Fail("B2-GroupedGenVsUngrouped", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320002UL, iter,
                    $"compile err G={string.Join(",", ceG)} U={string.Join(",", ceU)}");
            }

            var tlCount = 4 + rng.NextInt(3);
            var bakeSeed = NextSeed(ref rng);
            static byte[] BuildGrpLib(Assembly asm, int tlCount, ulong bSeed)
            {
                var r = new FuzzRandom(bSeed);
                var names = Enumerable.Range(1, tlCount).Select(static i => $"gt_{i:D2}").ToArray();
                var ch = new[] { "c0", "c1" };
                var man = LibraryBake.Manifest(names, ch, 60);
                var trType = asm.GetType("GenGrp.GTrack", true)!;
                var clType = asm.GetType("GenGrp.GClip", true)!;
                var mkType = asm.GetType("GenGrp.GMarker", true)!;
                var tls = new TypedTimeline[tlCount];
                for (var i = 0; i < tlCount; i++)
                {
                    var dur = (uint)(16 + r.NextInt(24));
                    var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch[r.NextInt(2)], Loops = r.NextBool() };
                    var tr = new TypedTrack { Name = "g", Type = trType, Data = BitConverter.GetBytes(1 + r.NextInt(3)) };
                    var cb = new byte[8];
                    MemoryMarshal.Write(cb.AsSpan(0, 4), 1 + r.NextInt(7));
                    MemoryMarshal.Write(cb.AsSpan(4, 4), r.NextInt(7) - 3);
                    tr.Clips.Add(new TypedClip { Type = clType, Start = 1, End = dur - 1, Data = cb });
                    tl.Tracks.Add(tr);
                    tl.Markers.Add(new TypedMarker { Type = mkType, Tick = 2, Data = BitConverter.GetBytes(3 + r.NextInt(5)) });
                    tls[i] = tl;
                }
                var kinds = LibraryBake.Kinds([asm]);
                var parts = tls.Select(tl =>
                {
                    var tlb = LibraryBake.BakeTyped(tl, man);
                    return new LibraryPart { Name = tl.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(tl.Name!, tlb, kinds), Next = 0 };
                }).ToList();
                var s = LibrarySettings.Default;
                s.ChannelCount = 2;
                return LibraryAssembler.Assemble(parts, 60, s);
            }

            var alcG = new GenDomainContext($"G_{iter}");
            var alcU = new GenDomainContext($"U_{iter}");
            try
            {
                var asmG = alcG.LoadFromStream(new MemoryStream(peG));
                var asmU = alcU.LoadFromStream(new MemoryStream(peU));
                var libG = BuildGrpLib(asmG, tlCount, bakeSeed);
                var libU = BuildGrpLib(asmU, tlCount, bakeSeed);
                var pulseSeed = (uint)NextSeed(ref rng);
                var outG = (byte[])asmG.GetType("GenGrp.GrpHost", true)!.GetMethod("Play")!.Invoke(null, [libG, tlCount, 4, 24, memberCount, pulseSeed])!;
                var outU = (byte[])asmU.GetType("GenGrp.GrpHost", true)!.GetMethod("Play")!.Invoke(null, [libU, tlCount, 4, 24, memberCount, pulseSeed])!;
                var diff = FuzzRunner.CompareBytes(outG, outU, "grouped vs ungrouped");
                if (diff != null)
                    FuzzRunner.Fail("B2-GroupedGenVsUngrouped", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320002UL, iter, diff);
            }
            finally
            {
                alcG.Unload();
                alcU.Unload();
            }
        });
    }

    [Fact]
    public void FoldedPulsesDomains_MatchUnfoldedTwinsEveryTick()
    {
        FuzzRunner.Run("B2-FoldedGenVsUnfolded", 0x42320003UL, 4, (rng, iter) =>
        {
            var shape = (ValueShape)rng.NextInt(5);
            var vt = ValueTypeName(shape);

            string BuildFoldSrc(bool fold)
            {
                var sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Runtime.InteropServices;");
                sb.AppendLine("using Tl;");
                sb.AppendLine("namespace GenFold;");
                sb.AppendLine(ValueHelpers());
                sb.AppendLine(ColumnStateType(shape, 0));
                sb.AppendLine("public struct FClip { public int A; public int B; }");
                sb.AppendLine("public readonly struct FTrack : IBlend<FClip> { public int S { get; init; } public void Blend(in FClip a, in FClip b, float t, out FClip r) => r = a; }");
                sb.AppendLine("public struct FMarker { public int D; }");
                sb.AppendLine("[TimelineSystem] public ref partial struct FoldSys { public Span<Col_0> C0; }");
                sb.AppendLine($"[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, FoldsPulses = {(fold ? "true" : "false")})] public struct FKind {{");
                sb.Append($"    public static void Combine(ref {vt} t, in {vt} n) {{ ");
                AppendAddExpr(sb, shape, "t", "n");
                sb.AppendLine(" }");
                sb.Append($"    public static void Apply(in {vt} t, ref Col_0 c) {{ ");
                AppendAddExpr(sb, shape, "c.V", "t");
                sb.AppendLine(" }");
                sb.AppendLine("}");
                sb.AppendLine("[TimelineSystemMember(typeof(FoldSys))] public readonly struct FCon : ITrack<FTrack, FClip> {");
                sb.Append($"    [Feeds(typeof(FKind))] public static {vt} Sp(in Frame<FTrack, FClip> f) => ");
                AppendMakeExpr(sb, shape, "f.Direction * f.Clip.A", "f.Direction * f.Clip.B");
                sb.AppendLine(";");
                sb.Append($"    [Pulse(typeof(FKind), On = ClipEvent.Start)] public static {vt} Ps(in Frame<FTrack, FClip> f) => ");
                AppendMakeExpr(sb, shape, "f.Direction * (f.Clip.B + 2)", "f.Clip.A + 1");
                sb.AppendLine(";");
                sb.Append($"    [Pulse(typeof(FKind), On = ClipEvent.End)] public static {vt} Pe(in Frame<FTrack, FClip> f) => ");
                AppendMakeExpr(sb, shape, "f.Direction * (f.Clip.A + 3)", "f.Clip.B + 5");
                sb.AppendLine(";");
                sb.AppendLine("}");
                sb.AppendLine("[TimelineSystemMember(typeof(FoldSys))] public readonly struct FMk : IMarker<FMarker> {");
                sb.Append($"    [Pulse(typeof(FKind), On = ClipEvent.Marker)] public static {vt} Pm(in MarkerFrame<FMarker> f) => ");
                AppendMakeExpr(sb, shape, "f.Direction * f.Marker.D", "f.Marker.D + 2");
                sb.AppendLine(";");
                sb.AppendLine("}");
                sb.AppendLine("public static class FoldHost {");
                sb.AppendLine("    public static byte[] Play(byte[] lib, int timelines, int entities, int steps, uint seed) {");
                sb.AppendLine("        using var w = new TimelineWorld(lib); using var p = new PlaybackTable(entities, 4); using var ks = new KindStore(w, p);");
                sb.AppendLine("        var c0 = new Col_0[entities]; var outList = new List<byte>(); var q = ks.Pulses<" + vt + ">(typeof(FKind).FullName!);");
                sb.AppendLine("        var r = 0x77119933u ^ seed;");
                sb.AppendLine("        for (var s = 0; s < steps; s++) {");
                sb.AppendLine("            for (var e = 0; e < entities; e++) {");
                sb.AppendLine("                r ^= r << 13; r ^= r >> 17; r ^= r << 5;");
                sb.AppendLine("                var tl = (ushort)(1 + (r >> 8) % Math.Max(1, timelines));");
                sb.AppendLine("                if ((r & 3) == 0) p.Start(e, tl, (ushort)(r & 3));");
                sb.AppendLine("                else if ((r & 3) == 1) p.SetSpeed(e, tl, (ushort)(16 + (r >> 10) % 48));");
                sb.AppendLine("                else if ((r & 3) == 2 && s % 4 == 0) p.Stop(e, tl);");
                sb.AppendLine($"                if ((r & 4) == 0) q.To(e, new {vt} {{ A = {(shape == ValueShape.B1 ? "(byte)" : shape == ValueShape.B2 ? "(short)" : "(int)")}((r >> 4) & 7) }});");
                sb.AppendLine("            }");
                sb.AppendLine("            var fwd = s % 6 != 4;");
                sb.AppendLine("            w.Tick(p, fwd); new FoldSys { C0 = c0 }.Run(w, p, fwd);");
                sb.AppendLine("            outList.AddRange(MemoryMarshal.AsBytes<Col_0>(c0).ToArray());");
                sb.AppendLine("        }");
                sb.AppendLine("        return outList.ToArray();");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                return sb.ToString();
            }

            var (peF, gdF, ceF) = CompileWithGenerator($"GF_{iter}", BuildFoldSrc(true));
            var (peU, gdU, ceU) = CompileWithGenerator($"GU_{iter}", BuildFoldSrc(false));
            if (ceF.Length > 0 || ceU.Length > 0)
                FuzzRunner.Fail("B2-FoldedGenVsUnfolded", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320003UL, iter,
                    $"compile err F={string.Join(",", ceF)} U={string.Join(",", ceU)}");

            var tlCount = 3 + rng.NextInt(3);
            var bSeed = NextSeed(ref rng);
            static byte[] BuildLib(Assembly asm, int tlCount, ulong bs)
            {
                var r = new FuzzRandom(bs);
                var names = Enumerable.Range(1, tlCount).Select(static i => $"ft_{i:D2}").ToArray();
                var ch = new[] { "c0", "c1" };
                var man = LibraryBake.Manifest(names, ch, 60);
                var trT = asm.GetType("GenFold.FTrack", true)!;
                var clT = asm.GetType("GenFold.FClip", true)!;
                var mkT = asm.GetType("GenFold.FMarker", true)!;
                var tls = new TypedTimeline[tlCount];
                for (var i = 0; i < tlCount; i++)
                {
                    var dur = (uint)(12 + r.NextInt(20));
                    var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch[r.NextInt(2)], Loops = true };
                    var tr = new TypedTrack { Name = "f", Type = trT, Data = BitConverter.GetBytes(1) };
                    var cb = new byte[8];
                    MemoryMarshal.Write(cb.AsSpan(0, 4), 1 + r.NextInt(6));
                    MemoryMarshal.Write(cb.AsSpan(4, 4), 2 + r.NextInt(5));
                    tr.Clips.Add(new TypedClip { Type = clT, Start = 1, End = dur - 1, Data = cb });
                    tl.Tracks.Add(tr);
                    tl.Markers.Add(new TypedMarker { Type = mkT, Tick = 3, Data = BitConverter.GetBytes(5) });
                    tls[i] = tl;
                }
                var kinds = LibraryBake.Kinds([asm]);
                var parts = tls.Select(tl =>
                {
                    var tlb = LibraryBake.BakeTyped(tl, man);
                    return new LibraryPart { Name = tl.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(tl.Name!, tlb, kinds), Next = 0 };
                }).ToList();
                var s = LibrarySettings.Default;
                s.ChannelCount = 2;
                return LibraryAssembler.Assemble(parts, 60, s);
            }

            var alcF = new GenDomainContext($"FF_{iter}");
            var alcU = new GenDomainContext($"FU_{iter}");
            try
            {
                var asmF = alcF.LoadFromStream(new MemoryStream(peF));
                var asmU = alcU.LoadFromStream(new MemoryStream(peU));
                var pSeed = (uint)NextSeed(ref rng);
                var resF = (byte[])asmF.GetType("GenFold.FoldHost", true)!.GetMethod("Play")!.Invoke(null, [BuildLib(asmF, tlCount, bSeed), tlCount, 4, 24, pSeed])!;
                var resU = (byte[])asmU.GetType("GenFold.FoldHost", true)!.GetMethod("Play")!.Invoke(null, [BuildLib(asmU, tlCount, bSeed), tlCount, 4, 24, pSeed])!;
                var diff = FuzzRunner.CompareBytes(resF, resU, "folded vs unfolded");
                if (diff != null)
                    FuzzRunner.Fail("B2-FoldedGenVsUnfolded", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320003UL, iter, diff);
            }
            finally
            {
                alcF.Unload();
                alcU.Unload();
            }
        });
    }

    static string BuildDerivedOrHandSource(bool hasFloat, bool hasBool, bool derived, bool useITlTrack)
    {
        var sumRef = derived ? "Sum<SState>" : "HandSum";
        var setRef = derived ? "Set<TState>" : "HandSet";
        var whileRef = derived ? "While<WState>" : "HandWhile";
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Tl;");
        sb.AppendLine("namespace GenDer;");
        sb.AppendLine("public struct SState { public int X; " + (hasFloat ? "public float F; " : "public long L; ") + "}");
        sb.AppendLine("public struct TState { public int A; public short B; }");
        sb.AppendLine("public struct WState { " + (hasBool ? "public bool On; " : "public byte B; ") + "public int M; }");
        if (useITlTrack) sb.AppendLine("[TlClip(\"GenDer/Clip\")]");
        sb.AppendLine("public struct DClip { public int A; public int B; }");
        sb.AppendLine("public readonly struct DTrack : IBlend<DClip> { public int S { get; init; } public void Blend(in DClip a, in DClip b, float t, out DClip r) => r = a; }");
        sb.AppendLine("[TimelineSystem] public ref partial struct DerSys { public Span<SState> CS; public Span<TState> CT; public Span<WState> CW; }");
        if (!derived)
        {
            sb.AppendLine("[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)] public struct HandSum {");
            sb.AppendLine("    public static void Combine(ref SState t, in SState n) { t.X += n.X; " + (hasFloat ? "t.F += n.F;" : "t.L += n.L;") + " }");
            sb.AppendLine("    public static void Apply(in SState t, ref SState c) { c.X += t.X; " + (hasFloat ? "c.F += t.F;" : "c.L += t.L;") + " }");
            sb.AppendLine("}");
            sb.AppendLine("[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)] public struct HandSet {");
            sb.AppendLine("    public static void Combine(ref TState t, in TState n) => t = n;");
            sb.AppendLine("    public static void Apply(in TState t, ref TState c) => c = t;");
            sb.AppendLine("}");
            sb.AppendLine("[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine)] public struct HandWhile {");
            sb.AppendLine("    public static void Combine(ref WState t, in WState n) { " + (hasBool ? "t.On |= n.On;" : "t.B |= n.B;") + " t.M |= n.M; }");
            sb.AppendLine("    public static void Apply(in WState t, ref WState c) => c = t;");
            sb.AppendLine("    public static void Idle(ref WState c) => c = default;");
            sb.AppendLine("}");
        }
        var iface = useITlTrack ? "ITlTrack<DClip>" : "ITrack<DTrack, DClip>";
        var frm = useITlTrack ? "in TlFrame<DClip> f" : "in Frame<DTrack, DClip> f";
        sb.AppendLine($"[TimelineSystemMember(typeof(DerSys))] public readonly struct DCon : {iface} {{");
        sb.AppendLine($"    [Feeds(typeof({sumRef}))] public static SState S1({frm}) => new() {{ X = f.Direction * f.Clip.A, {(hasFloat ? "F = f.Clip.B * 0.5f" : "L = f.Clip.B")} }};");
        sb.AppendLine($"    [Pulse(typeof({sumRef}), On = ClipEvent.Start)] public static SState P1({frm}) => new() {{ X = f.Clip.A + 2, {(hasFloat ? "F = 1.25f" : "L = 5")} }};");
        sb.AppendLine($"    [Feeds(typeof({setRef}))] public static TState S2({frm}) => new() {{ A = f.Clip.A + 1, B = (short)f.Clip.B }};");
        sb.AppendLine($"    [Pulse(typeof({setRef}), On = ClipEvent.End)] public static TState P2({frm}) => new() {{ A = -f.Clip.A, B = 7 }};");
        sb.AppendLine($"    [Feeds(typeof({whileRef}))] public static WState S3({frm}) => new() {{ {(hasBool ? "On = true" : "B = (byte)(f.Clip.A & 15)")}, M = Math.Abs(f.Clip.A) }};");
        sb.AppendLine("}");
        sb.AppendLine("public static class DerHost {");
        sb.AppendLine("    public static byte[] Play(byte[] lib, int timelines, int entities, int steps, uint seed) {");
        sb.AppendLine("        using var w = new TimelineWorld(lib); using var p = new PlaybackTable(entities, 4); using var ks = new KindStore(w, p);");
        sb.AppendLine("        var cs = new SState[entities]; var ct = new TState[entities]; var cw = new WState[entities];");
        sb.AppendLine("        var list = new List<byte>(); var r = 0x33995511u ^ seed;");
        sb.AppendLine("        for (var s = 0; s < steps; s++) {");
        sb.AppendLine("            for (var e = 0; e < entities; e++) {");
        sb.AppendLine("                r ^= r << 13; r ^= r >> 17; r ^= r << 5;");
        sb.AppendLine("                var tl = (ushort)(1 + (r >> 8) % Math.Max(1, timelines));");
        sb.AppendLine("                if ((r & 3) == 0) p.Start(e, tl, (ushort)(r & 3));");
        sb.AppendLine("                else if ((r & 3) == 1) p.SetSpeed(e, tl, (ushort)(16 + (r >> 12) % 32));");
        sb.AppendLine("                else if ((r & 3) == 2 && s % 4 == 0) p.Stop(e, tl);");
        sb.AppendLine("            }");
        sb.AppendLine("            var fwd = s % 6 != 4;");
        sb.AppendLine("            w.Tick(p, fwd); new DerSys { CS = cs, CT = ct, CW = cw }.Run(w, p, fwd);");
        sb.AppendLine("            list.AddRange(MemoryMarshal.AsBytes<SState>(cs).ToArray());");
        sb.AppendLine("            list.AddRange(MemoryMarshal.AsBytes<TState>(ct).ToArray());");
        sb.AppendLine("            list.AddRange(MemoryMarshal.AsBytes<WState>(cw).ToArray());");
        sb.AppendLine("        }");
        sb.AppendLine("        return list.ToArray();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static byte[] BuildDerivedOrHandLib(Assembly asm, ulong bs, bool useITlTrack)
    {
        var r = new FuzzRandom(bs);
        var names = new[] { "dt_1", "dt_2", "dt_3" };
        var ch = new[] { "c0", "c1" };
        var man = LibraryBake.Manifest(names, ch, 60);
        var clT = asm.GetType("GenDer.DClip", true)!;
        var trT = useITlTrack ? typeof(TlNoSettings<>).MakeGenericType(clT) : asm.GetType("GenDer.DTrack", true)!;
        var trData = useITlTrack ? new byte[1] : new byte[4];
        var tls = new TypedTimeline[3];
        for (var i = 0; i < 3; i++)
        {
            var dur = (uint)(12 + r.NextInt(16));
            var tl = new TypedTimeline { Name = names[i], Duration = dur, Channel = ch[r.NextInt(2)], Loops = r.NextBool() };
            var tr = new TypedTrack { Name = "d", Type = trT, Data = trData };
            var cb = new byte[8];
            MemoryMarshal.Write(cb.AsSpan(0, 4), 2 + r.NextInt(5));
            MemoryMarshal.Write(cb.AsSpan(4, 4), 1 + r.NextInt(4));
            tr.Clips.Add(new TypedClip { Type = clT, Start = 1, End = dur - 1, Data = cb });
            tl.Tracks.Add(tr);
            tls[i] = tl;
        }
        var kinds = LibraryBake.Kinds([asm]);
        var parts = tls.Select(tl =>
        {
            var tlb = LibraryBake.BakeTyped(tl, man);
            return new LibraryPart { Name = tl.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(tl.Name!, tlb, kinds), Next = 0 };
        }).ToList();
        var s = LibrarySettings.Default;
        s.ChannelCount = 2;
        return LibraryAssembler.Assemble(parts, 60, s);
    }

    static void RunDerivedVsHand(string target, ulong defaultSeed, int baseCases, bool useITlTrack)
    {
        FuzzRunner.Run(target, defaultSeed, baseCases, (rng, iter) =>
        {
            var hasFloat = rng.NextBool();
            var hasBool = rng.NextBool();
            var (peD, _, ceD) = CompileWithGenerator($"GD_{iter}", BuildDerivedOrHandSource(hasFloat, hasBool, true, useITlTrack));
            var (peH, _, ceH) = CompileWithGenerator($"GH_{iter}", BuildDerivedOrHandSource(hasFloat, hasBool, false, useITlTrack));
            if (ceD.Length > 0 || ceH.Length > 0)
                FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter,
                    $"compile err D={string.Join(",", ceD)} H={string.Join(",", ceH)}");

            var bSeed = NextSeed(ref rng);
            var alcD = new GenDomainContext($"DD_{iter}");
            var alcH = new GenDomainContext($"DH_{iter}");
            try
            {
                var asmD = alcD.LoadFromStream(new MemoryStream(peD));
                var asmH = alcH.LoadFromStream(new MemoryStream(peH));
                var pSeed = (uint)NextSeed(ref rng);
                var outD = (byte[])asmD.GetType("GenDer.DerHost", true)!.GetMethod("Play")!.Invoke(null, [BuildDerivedOrHandLib(asmD, bSeed, useITlTrack), 3, 4, 24, pSeed])!;
                var outH = (byte[])asmH.GetType("GenDer.DerHost", true)!.GetMethod("Play")!.Invoke(null, [BuildDerivedOrHandLib(asmH, bSeed, useITlTrack), 3, 4, 24, pSeed])!;
                var diff = FuzzRunner.CompareBytes(outD, outH, "derived vs hand");
                if (diff != null)
                    FuzzRunner.Fail(target, FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : defaultSeed, iter, diff);
            }
            finally
            {
                alcD.Unload();
                alcH.Unload();
            }
        });
    }

    [Fact]
    public void DerivedKindsDomains_MatchHandWrittenTwinsEveryTick()
        => RunDerivedVsHand("B2-DerivedVsHand", 0x42320004UL, 4, useITlTrack: true);

    [Fact(Skip = "#637")]
    public void DerivedKinds_ITrackWithTimelineSystem_EmitsThroughIncrementalGenerator()
        => RunDerivedVsHand("B2-DerivedITrack", 0x42320007UL, 4, useITlTrack: false);

    [Fact]
    public void ThirtyColumnsAndMultiConsumer_CompileZeroErrorsAndRun()
    {
        FuzzRunner.Run("B2-Max30Columns", 0x42320005UL, 2, (rng, iter) =>
        {
            var src = GenerateRandomValidDomain(
                ref rng, "GenMaxCols", false, false, null, 2, 4, 30, 30,
                out var kinds, out var trackCount, out var hasMarker, out _);

            var (pe, gd, ce) = CompileWithGenerator($"Gen30Cols_{iter}", src);
            if (gd.Any(static d => d.Severity == DiagnosticSeverity.Error) || ce.Length > 0 || pe.Length == 0)
                FuzzRunner.Fail("B2-Max30Columns", FuzzRunner.HasExplicitSeed ? FuzzRunner.ExplicitSeed : 0x42320005UL, iter,
                    $"failed: gen={string.Join(",", gd)} comp={string.Join(",", ce)}");

            var alc = new GenDomainContext($"G30_{iter}");
            try
            {
                var asm = alc.LoadFromStream(new MemoryStream(pe));
                var bakeRng = new FuzzRandom(NextSeed(ref rng));
                var lib = BakeGeneratedLibrary(asm, "GenMaxCols", trackCount, hasMarker, ref bakeRng, 3, 18, true);
                var exec = asm.GetType("GenMaxCols.DynHost", true)!.GetMethod("Execute")!;
                var r1 = (byte[])exec.Invoke(null, [lib, 3, 3, 12, kinds.Length, 0, 2, 1, true])!;
                var r2 = (byte[])exec.Invoke(null, [lib, 3, 3, 12, kinds.Length, 0, 2, 1, true])!;
                Assert.True(r1.AsSpan().SequenceEqual(r2));
            }
            finally
            {
                alc.Unload();
            }
        });
    }

    [Fact]
    public void InvalidDomains_ReportExpectedTlGenDiagnosticsNeverCrashOrSilentCompile()
    {
        static string Wrap(string body) => """
            using System;
            using Tl;
            namespace BadDom;
            public struct Cl { public float V; }
            public readonly struct Tr : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
            public struct AnchorSt { public int X; }
            [TimelineKind] public struct AnchorKind { public static void Combine(ref int t, in int n) => t += n; public static void Apply(in int t, ref AnchorSt a) => a.X += t; }
            """ + body;

        static string WrapNoAnchor(string body) => """
            using System;
            using Tl;
            namespace BadDom;
            public struct Cl { public float V; }
            public readonly struct Tr : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
            """ + body;

        static void AssertDiags(string name, string source, string expectedCode)
        {
            var (_, genDiags, _) = CompileWithGenerator(name, source);
            Assert.True(
                genDiags.Any(d => d.Id == expectedCode && d.Severity == DiagnosticSeverity.Error),
                $"expected {expectedCode}, got: {string.Join("; ", genDiags.Select(d => d.Id + ":" + d.GetMessage()))}");
        }

        AssertDiags("DupColConsumer", Wrap("""
            public struct M { public int X; }
            [TimelineSystem] public ref partial struct S { public Span<AnchorSt> A; public Span<M> M; }
            [TimelineSystemMember(typeof(S))]
            public struct C : ITrack<Tr, Cl> { public static void ExecuteActive(in Frame<Tr, Cl> f, ref M a, ref M b) { } public static void OnClipStart(in Frame<Tr, Cl> f) { } }
            """), "TLGEN90");

        var cols31Decl = string.Join("\n", Enumerable.Range(0, 31).Select(static i => $"public struct M{i} {{ public int V; }}"));
        var cols31Fields = string.Join("\n", Enumerable.Range(0, 31).Select(static i => $"public Span<M{i}> C{i};"));
        var cols31Params = string.Join(", ", Enumerable.Range(0, 31).Select(static i => $"ref M{i} p{i}"));
        AssertDiags("Cols31", Wrap(cols31Decl + "\n[TimelineSystem] public ref partial struct S {\npublic Span<AnchorSt> Anchor;\n" + cols31Fields + "\n}\npublic struct C : ITrack<Tr, Cl> { public static void ExecuteActive(in Frame<Tr, Cl> f, " + cols31Params + ") { } public static void OnClipStart(in Frame<Tr, Cl> f) { } }"), "TLGEN90");

        var fold11Params = string.Join(", ", Enumerable.Range(0, 11).Select(static i => $"out int r{i}"));
        var fold11Assigns = string.Join(" ", Enumerable.Range(0, 11).Select(static i => $"r{i} = {i};"));
        AssertDiags("Folds11", Wrap("[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; }\npublic struct C : ITrack<Tr, Cl> { public static void Fold(in Frame<Tr, Cl> f, " + fold11Params + ") { " + fold11Assigns + " } public static void ExecuteActive(in Frame<Tr, Cl> f) { } public static void OnClipStart(in Frame<Tr, Cl> f) { } }"), "TLGEN96");

        AssertDiags("BadGroupIdle", WrapNoAnchor("""
            public struct M { public int X; }
            [TimelineSystem] public ref partial struct S { public Span<M> M; }
            [TimelineKind(Group = "BadG")]
            public struct GK { public static void Combine(ref int t, in int n) => t += n; public static void Apply(in int t, ref M m) => m.X += t; public static void Idle(ref M m) => m.X = 0; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(GK))] public static int E(in Frame<Tr, Cl> f) => 1; }
            """), "TLGEN102");

        AssertDiags("BadGroupMissingCombine", WrapNoAnchor("""
            public struct M { public int X; }
            [TimelineSystem] public ref partial struct S { public Span<M> M; }
            [TimelineKind(Group = "BadG")]
            public struct GK { public static void Apply(in int t, ref M m) => m.X += t; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(GK))] public static int E(in Frame<Tr, Cl> f) => 1; }
            """), "TLGEN102");

        AssertDiags("DuplicateColumnSystem", Wrap("""
            public struct M { public int X; }
            [TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> A; public Span<M> B; }
            """), "TLGEN99");

        AssertDiags("DerivedBoolSum", WrapNoAnchor("""
            [TlState] public partial struct BadSum { public bool B; }
            [TlClip("Bad/S")] public struct BadClip { public int X; }
            public struct C : ITlTrack<BadClip> { [Feeds(typeof(Sum<BadSum>))] public static BadSum E(in TlFrame<BadClip> f) => default; }
            """), "TLGEN110");

        AssertDiags("TwoWritersToSet", WrapNoAnchor("""
            [TlState] public partial struct St { public int V; }
            [TlClip("Bad/St")] public struct BadClip { public int X; }
            public struct K : ITlTrack<BadClip> { [Feeds(typeof(Set<St>))] public static St E(in TlFrame<BadClip> f) => default; }
            public readonly struct Tr2 : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
            public struct W2 : ITrack<Tr2, Cl> { public static void ExecuteActive(in Frame<Tr2, Cl> f, ref St c) => c.V++; public static void OnClipStart(in Frame<Tr2, Cl> f) { } }
            """), "TLGEN112");

        FuzzRunner.Run("B2-InvalidMutations", 0x42320006UL, 6, (rng, iter) =>
        {
            var mut = rng.NextInt(5);
            var snippet = mut switch
            {
                0 => Wrap("public struct M { public int X; }\n[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> M; }\n[TimelineKind(Group = \"G\")] public struct BadK { public static void Apply(in int v, ref M m) => m.X += v; }\npublic struct C : ITrack<Tr, Cl> { [Feeds(typeof(BadK))] public static int E(in Frame<Tr, Cl> f) => 1; }"),
                1 => Wrap("public struct M { public int X; }\n[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> M; }\n[TimelineKind(Accepts = KindEntries.Pulses)] public struct PK { public static void Combine(ref int t, in int n) => t += n; public static void Apply(in int v, ref M m) => m.X += v; }\npublic struct C : ITrack<Tr, Cl> { [Feeds(typeof(PK))] public static int E(in Frame<Tr, Cl> f) => 1; }"),
                2 => Wrap("public struct M { public int X; }\n[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> M; }\n[TimelineKind] public struct K { public static void Combine(ref int t, in int n) => t += n; public static void Apply(in int v, ref M m) => m.X += v; }\npublic struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => 1f; }"),
                3 => Wrap("public struct M { public int X; }\n[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> M; }\npublic struct K : ITrack<Tr, Cl> { [Feeds(typeof(M))] public static int E(in Frame<Tr, Cl> f) => 1; }"),
                _ => Wrap("public struct M { public int X; }\n[TimelineSystem] public ref partial struct S { public Span<AnchorSt> Anchor; public Span<M> M; }\npublic struct C : ITrack<Tr, Cl> { public static void ExecuteActive(in Frame<Tr, Cl> f, ref M a, ref M b) { } public static void OnClipStart(in Frame<Tr, Cl> f) { } }"),
            };
            var (_, genDiags, _) = CompileWithGenerator($"BadMut_{iter}_{mut}", snippet);
            Assert.Contains(genDiags, static d => d.Id.StartsWith("TLGEN", StringComparison.Ordinal) && d.Severity == DiagnosticSeverity.Error);
        });
    }
}
