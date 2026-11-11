using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class DerivedKindTests
{
    private const string Column = """
        using System;
        using Tl;
        namespace Domain;
        [TlState]
        public partial struct Teleported { public float Forward; public float Side; }
        """;

    private const string FeedsSum = """
        using System;
        using Tl;
        namespace Domain;
        [TlState] public partial struct Teleported { public float Forward; public float Side; }
        [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public float Side; }
        public struct TeleportTrack : ITlTrack<TeleportClip>
        {
            [Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)]
            public static Teleported Jump(in TlFrame<TeleportClip> frame) => new() { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };
        }
        [TimelineSystem] public ref partial struct Game { public Span<Teleported> Columns; }
        """;

    private const string FeedsSet = """
        using System;
        using Tl;
        namespace Domain;
        [TlState] public partial struct Teleported { public float Forward; public float Side; }
        [TlClip("Hello/Set")] public struct SetClip { public float Distance; }
        public struct SetTrack : ITlTrack<SetClip>
        {
            [Pulse(typeof(Set<Teleported>), On = ClipEvent.Start)]
            public static Teleported Jump(in TlFrame<SetClip> frame) => new() { Forward = frame.Clip.Distance, Side = 0f };
        }
        [TimelineSystem] public ref partial struct Game { public Span<Teleported> Columns; }
        """;

    private const string FeedsWhile = """
        using System;
        using Tl;
        namespace Domain;
        [TlState] public partial struct Ready { public bool Allowed; public float Floor; }
        [TlClip("Hello/Ability")] public struct AbilityClip { public float Duration; }
        public struct AbilityTrack : ITlTrack<AbilityClip>
        {
            [Feeds(typeof(While<Ready>))]
            public static Ready Allow(in TlFrame<AbilityClip> frame) => new() { Allowed = true, Floor = frame.Clip.Duration };
        }
        [TimelineSystem] public ref partial struct Game { public Span<Ready> Columns; }
        """;

    private static (IReadOnlyList<CompileArtifact> Artifacts, IReadOnlyList<DeclarationDiagnostic> Diagnostics) Emit(string source)
    {
        var compilation = Compilation(source);
        return SystemGeneration.Emit(compilation, "Derived");
    }

    private static string[] ErrorCodes(IReadOnlyList<DeclarationDiagnostic> diagnostics)
        => diagnostics.Where(d => d.Code != "TLGEN98").Select(d => d.Code).ToArray();

    [Fact]
    public void SumSynthesizesFieldWiseKind()
    {
        var (artifacts, diagnostics) = Emit(FeedsSum);
        var errors = ErrorCodes(diagnostics);
        Assert.Empty(errors);
        var derived = Assert.Single(artifacts, a => a.RelativePath == "TlDerivedKindsDerived.g.cs");
        Assert.Contains("public struct Sum_", derived.Content);
        Assert.Contains("Teleported", derived.Content);
        Assert.Contains("total.Forward += next.Forward;", derived.Content);
        Assert.Contains("total.Side += next.Side;", derived.Content);
        Assert.Contains("KindEntries.Both", derived.Content);
        Assert.DoesNotContain("Idle(", derived.Content);
    }

    [Fact]
    public void SetSynthesizesAssignmentKind()
    {
        var (_, diagnostics) = Emit(FeedsSet);
        Assert.Empty(ErrorCodes(diagnostics));
        var derived = Assert.Single(Artifacts(FeedsSet), a => a.RelativePath == "TlDerivedKindsDerived.g.cs");
        Assert.Contains("total = next;", derived.Content);
        Assert.Contains("column = total;", derived.Content);
    }

    [Fact]
    public void WhileSynthesizesOrMaxIdleKind()
    {
        var (_, diagnostics) = Emit(FeedsWhile);
        Assert.Empty(ErrorCodes(diagnostics));
        var derived = Assert.Single(Artifacts(FeedsWhile), a => a.RelativePath == "TlDerivedKindsDerived.g.cs");
        Assert.Contains("total.Allowed |= next.Allowed;", derived.Content);
        Assert.Contains("System.Math.Max", derived.Content);
        Assert.Contains("KindEntries.Spans", derived.Content);
        Assert.Contains("public static void Idle(" , derived.Content);
        Assert.Contains("column = default;", derived.Content);
    }

    private static IReadOnlyList<CompileArtifact> Artifacts(string source) => Emit(source).Artifacts;

    [Fact]
    public void HandWrittenKindHasNoDerivedArtifact()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TimelineKind]
            public struct TeleportKind
            {
                public static void Combine(ref float total, in float next) => total += next;
                public static void Apply(in float total, ref float column) => column += total;
            }
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Direction * frame.Clip.Distance;
            }
            """;
        var (artifacts, _) = Emit(source);
        Assert.DoesNotContain(artifacts, a => a.RelativePath.Contains("Derived"));
    }

    [Fact]
    public void DerivedTypesAttachToOneSystemOnly()
    {
        var (artifacts, diagnostics) = Emit(FeedsSum);
        Assert.Empty(ErrorCodes(diagnostics));
        var derived = Assert.Single(artifacts, a => a.RelativePath == "TlDerivedKindsDerived.g.cs");
        var parsed = CSharpSyntaxTree.ParseText(derived.Content, CSharpParseOptions.Default, derived.RelativePath);
        Assert.DoesNotContain(parsed.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void SumOnNonNumericFieldIsGeneratorError()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TlState] public partial struct Named { public bool Flag; public float Forward; }
            [TlClip("Hello/Name")] public struct NameClip { public float Distance; }
            public struct NameTrack : ITlTrack<NameClip>
            {
                [Pulse(typeof(Sum<Named>), On = ClipEvent.Start)]
                public static Named Jump(in TlFrame<NameClip> frame) => new() { Forward = frame.Clip.Distance, Flag = true };
            }
            [TimelineSystem] public ref partial struct Game { public Span<Named> Columns; }
            """;
        var (_, diagnostics) = Emit(source);
        var hit = Assert.Single(diagnostics, d => d.Code == "TLGEN110");
        Assert.Contains("Flag", hit.Message);
        Assert.Contains("not numeric", hit.Message);
    }

    [Fact]
    public void WhileOnStructFieldIsGeneratorError()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            public struct Inner { public int X; }
            [TlState] public partial struct Ready { public Inner Allowed; }
            [TlClip("Hello/Ability")] public struct AbilityClip { public float Duration; }
            public struct AbilityTrack : ITlTrack<AbilityClip>
            {
                [Feeds(typeof(While<Ready>))]
                public static Ready Allow(in TlFrame<AbilityClip> frame) => new();
            }
            [TimelineSystem] public ref partial struct Game { public Span<Ready> Columns; }
            """;
        var (_, diagnostics) = Emit(source);
        var hit = Assert.Single(diagnostics, d => d.Code == "TLGEN111");
        Assert.Contains("Allowed", hit.Message);
    }

    [Fact]
    public void ColumnWrittenByDerivedAndAnotherKindIsGeneratorError()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TlState] public partial struct Ready { public bool Allowed; public float Floor; }
            [TimelineKind]
            public struct ReadyKind
            {
                public static void Combine(ref bool total, in bool next) => total |= next;
                public static void Apply(in bool total, ref Ready column) => column.Allowed = total;
            }
            [TlClip("Hello/Two")] public struct TwoClip { public float Duration; }
            public struct TwoTrack : ITlTrack<TwoClip>
            {
                [Feeds(typeof(While<Ready>))]
                public static Ready Allow(in TlFrame<TwoClip> frame) => new() { Allowed = true };
            }
            public struct TwoFeed : ITlTrack<TwoClip>
            {
                [Feeds(typeof(ReadyKind))]
                public static bool Feed(in TlFrame<TwoClip> frame) => true;
            }
            [TimelineSystem] public ref partial struct Game { public Span<Ready> Columns; }
            """;
        var (_, diagnostics) = Emit(source);
        var hit = Assert.Single(diagnostics, d => d.Code == "TLGEN112");
        Assert.Contains("written by both", hit.Message);
    }

    [Fact]
    public void HandWrittenTwoWritersWithoutDerivedStayAllowed()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TlState] public partial struct Ready { public bool Allowed; }
            [TimelineKind]
            public struct ReadyKind
            {
                public static void Combine(ref bool total, in bool next) => total |= next;
                public static void Apply(in bool total, ref Ready column) => column.Allowed = total;
            }
            [TlClip("Hello/Two")] public struct TwoClip { public float Duration; }
            public struct TwoTrack : ITlTrack<TwoClip>
            {
                [Feeds(typeof(ReadyKind))]
                public static bool Allow(in TlFrame<TwoClip> frame) => true;
            }
            [TimelineSystem] public ref partial struct Game { public Span<Ready> Columns; }
            """;
        var (_, diagnostics) = Emit(source);
        Assert.DoesNotContain(diagnostics, d => d.Code == "TLGEN112");
    }

    [Fact]
    public void UnconnectedClipFieldWarnsTLGEN113()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TlClip("Hello/Silent")] public struct SilentClip { public float Distance; public float SideSwing; }
            public struct SilentTrack : ITlTrack<SilentClip>
            {
                [Feeds(typeof(float))]
                public static float Jump(in TlFrame<SilentClip> frame) => frame.Direction * frame.Clip.Distance;
            }
            [TimelineSystem] public ref partial struct Game { }
            """;
        var (_, diagnostics) = Emit(source);
        var hit = Assert.Single(diagnostics, d => d.Code == "TLGEN113");
        Assert.Contains("SideSwing", hit.Message);
    }

    [Fact]
    public void UnconnectedStateFieldWarnsTLGEN114()
    {
        var source = Column + """
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Feeds(typeof(float))]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Direction * frame.Clip.Distance;
            }
            [TimelineSystem] public ref partial struct Game { public Span<Teleported> Columns; }
            """;
        var (_, diagnostics) = Emit(source);
        var hits = diagnostics.Where(d => d.Code == "TLGEN114").ToArray();
        Assert.NotEmpty(hits);
        Assert.Contains(hits, h => h.Message.Contains("Forward") || h.Message.Contains("Side"));
    }

    [Fact]
    public void ConnectedFieldsDoNotWarn()
    {
        var (_, diagnostics) = Emit(FeedsSum);
        Assert.DoesNotContain(diagnostics, d => d.Code is "TLGEN113" or "TLGEN114");
    }

    [Fact]
    public void SumMarkerTypesArePublicStructs()
    {
        Assert.True(typeof(Sum<>).IsValueType);
        Assert.True(typeof(Set<>).IsValueType);
        Assert.True(typeof(While<>).IsValueType);
    }

    [Fact]
    public void GeneratedBindingForDerivedFeedsUnchanged()
    {
        var (artifacts, _) = Emit(FeedsSum);
        var systems = artifacts.Single(a => a.RelativePath == "TlSystemsDerived.g.cs");
        Assert.Contains("Tl.Sum`1", systems.Content);
        Assert.DoesNotContain("TimelineKind", systems.Content);
    }

    private static CSharpCompilation Compilation(string source)
    {
        var references = ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
        return CSharpCompilation.Create(
            "DerivedKindTests",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();
}
