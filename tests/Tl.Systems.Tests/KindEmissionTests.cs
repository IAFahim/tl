using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class KindEmissionTests
{
    internal static readonly string KindSource = EmissionTests.ReadResource("Tl.Systems.Tests.KindDomain.KindDomain.cs");

    internal static CSharpCompilation Compile(string source, string assemblyName = "Tl.Systems.Tests")
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "Kinds.cs");
        return CSharpCompilation.Create(assemblyName, [tree], EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    [Fact]
    public void KindDomainEmitsWithoutDiagnosticsAndMatchesTheSnapshot()
    {
        var emitted = SystemGeneration.Emit(Compile(KindSource), "Kinds");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(emitted.Artifacts);
        var actual = artifact.Content.Replace("\r\n", "\n");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1")
        {
            var root = AppContext.BaseDirectory;
            for (var i = 0; i < 8 && !File.Exists(Path.Combine(root, "Tl.Systems.Tests.csproj")); i++) root = Path.GetDirectoryName(root)!;
            File.WriteAllText(Path.Combine(root, "Generated", "TlSystemsKinds.g.cs"), actual);
        }
        var expected = EmissionTests.ReadResource("Tl.Systems.Tests.Generated.TlSystemsKinds.g.cs").Replace("\r\n", "\n");
        Assert.True(actual == expected, $"Snapshot differs at first offset {EmissionTests.FirstDifference(actual, expected)}.");
    }

    [Fact]
    public void IdleDomainEmitsWithoutDiagnosticsAndMatchesTheSnapshot()
    {
        var emitted = SystemGeneration.Emit(Compile(EmissionTests.ReadResource("Tl.Systems.Tests.IdleDomain.IdleDomain.cs")), "Idle");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(emitted.Artifacts);
        var actual = artifact.Content.Replace("\r\n", "\n");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1")
        {
            var root = AppContext.BaseDirectory;
            for (var i = 0; i < 8 && !File.Exists(Path.Combine(root, "Tl.Systems.Tests.csproj")); i++) root = Path.GetDirectoryName(root)!;
            File.WriteAllText(Path.Combine(root, "Generated", "TlSystemsIdle.g.cs"), actual);
        }
        var expected = EmissionTests.ReadResource("Tl.Systems.Tests.Generated.TlSystemsIdle.g.cs").Replace("\r\n", "\n");
        Assert.True(actual == expected, $"Snapshot differs at first offset {EmissionTests.FirstDifference(actual, expected)}.");
    }

    const string Prelude = """
        using System;
        using Tl;
        namespace T
        {
        public struct M { public float X; }
        public struct Cl { public float V; }
        public struct Tr : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
        public struct Mk { public int D; }
        [TimelineSystem] public ref partial struct S { public Span<M> M; }
        """;

    static string[] Codes(string body)
    {
        var compilation = Compile(Prelude + body + "\n}\n");
        IncrementalParityTests.AssertParity(compilation);
        var diagnostics = SystemReader.Read(compilation).Diagnostics;
        return diagnostics.Select(static d => d.Code + ": " + d.Message).ToArray();
    }

    static void Rejects(string body, string code, string part)
    {
        var found = Codes(body);
        Assert.True(found.Any(f => f.StartsWith(code + ":", StringComparison.Ordinal) && f.Contains(part)), $"expected {code} containing '{part}', got:\n{string.Join("\n", found)}");
    }

    [Fact]
    public void ACleanKindAndFeederProduceNoDiagnostics()
    {
        Assert.Empty(Codes("""
            [TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }
            """));
    }

    [Fact]
    public void KindDeclarationsFailWithTheKindAndTheRepairNamed()
    {
        Rejects("[TimelineKind] public struct K { }", "TLGEN102", "needs public static void Apply(in <value> total");
        Rejects("[TimelineKind] public struct K { public static void Apply(in float t, ref M m) { } }", "TLGEN102", "add public static void Combine(ref float total, in float next)");
        Rejects("[TimelineKind] public struct K { public static void Combine(ref float t, in int n) { } public static void Apply(in float t, ref M m) { } }", "TLGEN102", "Combine(ref float total");
        Rejects("[TimelineKind(Mode = KindMode.Each)] public struct K { public static void Apply(in float t, ref Events<int> e) { } }", "TLGEN102", "takes Events");
        Rejects("[TimelineKind(Mode = KindMode.Each)] public struct K { public static void Apply(in float t, ref M m) { } } [TimelineKind(Mode = KindMode.Each)] public struct K2 { public static void Apply(in float t, ref M m) { } } [TimelineSystem] public ref partial struct S3 { public Span<M> M; }", "TLGEN105", "fits 2 systems");
        Rejects("public struct Unseen { public int Q; } [TimelineKind(Mode = KindMode.Each)] public struct K { public static void Apply(in float t, ref Unseen u) { } }", "TLGEN105", "fits no system");
    }

    [Fact]
    public void AKindMayDeclareIdleOnTheColumnsItsSystemProvides()
    {
        const string feeder = "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }";
        const string kind = "[TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; public static void Idle(ref M m) => m.X = 0; }";
        Assert.Empty(Codes(kind + feeder));
        var emitted = SystemGeneration.Emit(Compile(Prelude + kind + feeder + "\n}\n"), "Idle");
        var all = string.Join("\n", emitted.Artifacts.Select(static a => a.Content));
        Assert.Contains("global::T.K.Idle(ref __c0[__e]);", all);
        Assert.Contains("~__kt->Active[__kw] & __kt->Enabled[__kw]", all);
        var plain = SystemGeneration.Emit(Compile(Prelude + kind.Replace(" public static void Idle(ref M m) => m.X = 0;", "") + feeder + "\n}\n"), "Idle");
        Assert.DoesNotContain(".Idle(", string.Join("\n", plain.Artifacts.Select(static a => a.Content)));
    }

    [Fact]
    public void ATlKindIdleColumnJoinsTheAutoSystem()
    {
        const string source = """
            using Tl;
            namespace N
            {
            [TlClip("N/Line")] public struct LineClip { public int Line; }
            public struct LineTrack : ITlTrack<LineClip> { [Feeds(typeof(Show))] public static int Which(in TlFrame<LineClip> f) => f.Clip.Line; }
            [TlKind] public struct Show
            {
                public static void Combine(ref int total, in int next) => total = next;
                public static void Apply(in int line, ref Screen screen) => screen.Caption = line;
                public static void Idle(ref Screen screen) => screen.Caption = 0;
            }
            [TlState] public partial struct Screen { public int Caption; }
            }
            """;
        var emitted = SystemGeneration.Emit(Compile(source), "Auto");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.ToString())));
        Assert.Contains("global::N.Show.Idle(ref __c0[__e]);", string.Join("\n", emitted.Artifacts.Select(static a => a.Content)));
    }

    [Fact]
    public void IdleDeclarationsFailWithTheKindAndTheRepairNamed()
    {
        const string body = "public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t;";
        Rejects("[TimelineKind(Accepts = KindEntries.Pulses)] public struct K { " + body + " public static void Idle(ref M m) { } }", "TLGEN102", "declares Idle");
        Rejects("[TimelineKind(Group = \"G\")] public struct K { " + body + " public static void Idle(ref M m) { } }", "TLGEN102", "no Group");
        Rejects("[TimelineKind] public struct K { " + body + " public static void Idle(ref Events<int> e) { } }", "TLGEN102", "Idle takes Events");
        Rejects("public struct Unseen { public int Q; } [TimelineKind] public struct K { " + body + " public static void Idle(ref Unseen u) { } }", "TLGEN105", "fits no system");
    }

    [Fact]
    public void FeederDeclarationsFailWithTheConsumerMethodAndTheRepairNamed()
    {
        const string kind = "[TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }";
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static int E(in Frame<Tr, Cl> f) => 1; }", "TLGEN104", "returns 'int', but kind 'K' takes 'float'");
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(M))] public static float E(in Frame<Tr, Cl> f) => 1; }", "TLGEN103", "not a [TimelineKind] struct");
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Pulse(typeof(K), On = ClipEvent.Start)] public static float E(in Frame<Tr, Cl> f) => 1; }", "TLGEN106", "accepts spans only");
        Rejects("[TimelineKind(Accepts = KindEntries.Pulses)] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }" + "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => 1; }", "TLGEN106", "accepts pulses only; use [Pulse]");
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Pulse(typeof(K), On = ClipEvent.Marker)] public static float E(in Frame<Tr, Cl> f) => 1; }", "TLGEN101", "On = Marker needs a consumer that implements IMarker<T>");
        Rejects(kind + "public struct C : IMarker<Mk> { [Feeds(typeof(K))] public static float E(in MarkerFrame<Mk> f) => 1; }", "TLGEN101", "needs a consumer that implements ITrack");
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f, ref Requests r) => 1; }", "TLGEN100", "takes Requests");
        Rejects(kind + "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static void E(in Frame<Tr, Cl> f) { } }", "TLGEN100", "must be a non-generic static method that returns the kind value");
        Rejects(kind + "public struct H { public int V; } [TimelineSystem] public ref partial struct S2 { public Span<H> H; } public struct A : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f, in M m) => 1; } public struct Tr2 : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; } public struct B : ITrack<Tr2, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr2, Cl> f, in H h) => 1; }", "TLGEN105", "fed by consumers in 2 systems");
    }
}
