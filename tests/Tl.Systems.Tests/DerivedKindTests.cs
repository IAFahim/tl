using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class DerivedKindTests
{
    internal const string Head = """
        using System;
        using Tl;
        namespace Domain;
        [TlState] public partial struct Teleported { public float Forward; public float Side; }
        [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public float Side; }
        """;

    internal static string Track(string kind)
        => "public struct TeleportTrack : ITlTrack<TeleportClip>\n{\n[Pulse(typeof(" + kind + "), On = ClipEvent.Start)]\n"
            + "public static Teleported Jump(in TlFrame<TeleportClip> frame) => new() { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };\n}\n";

    static (IReadOnlyList<CompileArtifact> Artifacts, IReadOnlyList<Tl.Gen.CSharp.Analysis.DeclarationDiagnostic> Diagnostics) Emit(string source)
        => SystemGeneration.Emit(KindEmissionTests.Compile(source), "Derived");

    static string[] Codes(string source) => Emit(source).Diagnostics.Select(static d => d.Code).ToArray();

    static string Derived(string source)
    {
        var emitted = Emit(source);
        Assert.Empty(emitted.Diagnostics);
        var artifact = Assert.Single(emitted.Artifacts, static a => a.RelativePath == "TlDerivedKindsDerived.g.cs");
        Assert.DoesNotContain(CSharpSyntaxTree.ParseText(artifact.Content).GetDiagnostics(), static d => d.Severity == DiagnosticSeverity.Error);
        return artifact.Content;
    }

    [Fact]
    public void SumAddsEveryFieldOfTheColumnTypeAndAppliesItWithoutAnIdle()
    {
        var text = Derived(Head + Track("Sum<Teleported>"));
        Assert.Contains("total.Forward += next.Forward;", text);
        Assert.Contains("total.Side += next.Side;", text);
        Assert.Contains("column.Forward += total.Forward;", text);
        Assert.DoesNotContain("Idle(", text);
        var systems = Emit(Head + Track("Sum<Teleported>")).Artifacts.Single(static a => a.RelativePath == "TlSystemsDerived.g.cs").Content;
        Assert.Contains("\"Tl.Derived.Sum_Domain_Teleported\"", systems);
        Assert.Contains("Tl.Derived.Sum_Domain_Teleported.Apply(in __total, ref __c0[__e]);", systems);
    }

    [Fact]
    public void SetKeepsTheLastEntryAndWhileIsAnOrOrMaxOfSpansWithADefaultIdle()
    {
        var set = Derived(Head + Track("Set<Teleported>"));
        Assert.Contains("total = next;", set);
        Assert.Contains("column = total;", set);
        const string flags = """
            using System;
            using Tl;
            namespace Domain;
            [TlState] public partial struct Ready { public bool Allowed; public int Charges; public float Floor; }
            [TlClip("Hello/Ability")] public struct AbilityClip { public float Duration; }
            public struct AbilityTrack : ITlTrack<AbilityClip>
            {
                [Feeds(typeof(While<Ready>))]
                public static Ready Allow(in TlFrame<AbilityClip> frame) => new() { Allowed = true, Charges = 1, Floor = frame.Clip.Duration };
            }
            """;
        var text = Derived(flags);
        Assert.Contains("total.Allowed |= next.Allowed;", text);
        Assert.Contains("total.Charges |= next.Charges;", text);
        Assert.Contains("total.Floor = global::System.Math.Max(total.Floor, next.Floor);", text);
        Assert.Contains("Idle(ref global::Domain.Ready column)", text);
        Assert.Contains("column = default;", text);
    }

    [Fact]
    public void AHandWrittenKindProducesNoDerivedSource()
    {
        const string source = """
            using System;
            using Tl;
            namespace Domain;
            [TlState] public partial struct Teleported { public float Forward; public float Side; }
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; }
            [TlKind(Accepts = KindEntries.Both)] public struct TeleportKind
            {
                public static void Combine(ref float total, in float next) => total += next;
                public static void Apply(in float total, ref Teleported column) { column.Forward += total; column.Side += total; }
            }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Clip.Distance;
            }
            """;
        var emitted = Emit(source);
        Assert.Empty(emitted.Diagnostics);
        Assert.DoesNotContain(emitted.Artifacts, static a => a.RelativePath.Contains("DerivedKinds"));
    }

    [Fact]
    public void SumOverAFieldThatIsNotNumericNamesTheField()
    {
        var emitted = Emit(Head.Replace("public float Forward;", "public bool Flag; public float Forward;") + Track("Sum<Teleported>"));
        var error = Assert.Single(emitted.Diagnostics, static d => d.Code == "TLGEN110");
        Assert.Contains("'Flag'", error.Message);
        Assert.Contains("not numeric", error.Message);
        Assert.Empty(emitted.Artifacts);
    }

    [Fact]
    public void WhileOverAFieldItCannotCombineNamesTheField()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public struct Inner { public int X; }
            [TlState] public partial struct Ready { public Inner Allowed; }
            [TlClip("Hello/Ability")] public struct AbilityClip { public float Duration; }
            public struct AbilityTrack : ITlTrack<AbilityClip>
            {
                [Feeds(typeof(While<Ready>))]
                public static Ready Allow(in TlFrame<AbilityClip> frame) => new() { Allowed = new Inner { X = 1 } };
            }
            """;
        var error = Assert.Single(Emit(source).Diagnostics, static d => d.Code == "TLGEN111");
        Assert.Contains("'Allowed'", error.Message);
    }

    [Fact]
    public void ASetOrWhileColumnWithASecondWriterNamesBothWriters()
    {
        const string source = """
            using Tl;
            namespace Domain;
            [TlState] public partial struct Ready { public bool Allowed; }
            [TlKind] public struct ReadyKind
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
            public struct OtherTrack : ITlTrack<TwoClip>
            {
                [Feeds(typeof(ReadyKind))]
                public static bool Feed(in TlFrame<TwoClip> frame) => frame.Clip.Duration > 0;
            }
            """;
        var error = Assert.Single(Emit(source).Diagnostics, static d => d.Code == "TLGEN112");
        Assert.Contains("written by both", error.Message);
        Assert.Contains("While<global::Domain.Ready>", error.Message);
        Assert.Contains("ReadyKind", error.Message);
        Assert.DoesNotContain("TLGEN112", Codes(source.Replace("While<Ready>", "Sum<Ready>")));
    }

    [Fact]
    public void ASilentClipFieldWarnsAndTheWorkingVersionDoesNot()
    {
        const string silent = """
            using Tl;
            namespace Domain;
            [TlState] public partial struct Teleported { public float Offset; public float SideSwing; }
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public float SideSwing; }
            [TlKind(Mode = KindMode.Each, Accepts = KindEntries.Pulses)] public struct TeleportKind
            {
                public static void Apply(in float move, ref Teleported player) { player.Offset += move; player.SideSwing += 0; }
            }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Direction * frame.Clip.Distance;
            }
            """;
        var emitted = Emit(silent);
        var warning = Assert.Single(emitted.Diagnostics, static d => d.Code == "TLGEN113");
        Assert.Contains("'SideSwing'", warning.Message);
        Assert.Contains("TeleportClip", warning.Message);
        Assert.Contains(emitted.Artifacts, static a => a.RelativePath == "TlSystemsDerived.g.cs");
        Assert.DoesNotContain("TLGEN113", Codes(silent.Replace("frame.Direction * frame.Clip.Distance;", "frame.Direction * (frame.Clip.Distance + frame.Clip.SideSwing);")));
    }

    [Fact]
    public void AClipPassedWholeToAHelperIsNotReportedAsUnread()
    {
        const string source = """
            using Tl;
            namespace Domain;
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public float SideSwing; }
            [TlState] public partial struct Place { public float X; }
            [TlKind(Mode = KindMode.Each, Accepts = KindEntries.Pulses)] public struct TeleportKind { public static void Apply(in float move, ref Place place) { place.X += move; } }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => Reach(frame.Clip);
                static float Reach(in TeleportClip clip) => clip.Distance + clip.SideSwing;
            }
            """;
        Assert.DoesNotContain("TLGEN113", Codes(source));
    }

    [Fact]
    public void AStateFieldNothingMentionsWarns()
    {
        const string source = """
            using Tl;
            namespace Domain;
            [TlState] public partial struct Teleported { public float Offset; public float Unused; }
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; }
            [TlKind(Mode = KindMode.Each, Accepts = KindEntries.Pulses)] public struct TeleportKind { public static void Apply(in float move, ref Teleported player) { player.Offset += move; } }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Clip.Distance;
            }
            """;
        var warning = Assert.Single(Emit(source).Diagnostics, static d => d.Code == "TLGEN114");
        Assert.Contains("'Unused'", warning.Message);
        Assert.DoesNotContain("TLGEN114", Codes(source.Replace("player.Offset += move;", "player.Offset += move; player.Unused = 0;")));
    }

    [Fact]
    public void ANewColumnFieldSharingTheNameOfANewClipFieldStillWarns()
    {
        const string source = """
            using Tl;
            namespace Domain;
            [TlState] public partial struct Teleported { public float Offset; public float SideSwing; }
            [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public float SideSwing; }
            [TlKind(Mode = KindMode.Each, Accepts = KindEntries.Pulses)] public struct TeleportKind { public static void Apply(in float move, ref Teleported player) { player.Offset += move; } }
            public struct TeleportTrack : ITlTrack<TeleportClip>
            {
                [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
                public static float Jump(in TlFrame<TeleportClip> frame) => frame.Direction * frame.Clip.Distance;
            }
            """;
        var codes = Codes(source);
        Assert.Contains("TLGEN113", codes);
        Assert.Contains("TLGEN114", codes);
    }

    [Fact]
    public void TheDerivedMarkersAreStructsInTheTlNamespace()
    {
        Assert.True(typeof(Sum<>).IsValueType && typeof(Set<>).IsValueType && typeof(While<>).IsValueType);
        Assert.Equal("Tl", typeof(Sum<int>).Namespace);
    }
}
