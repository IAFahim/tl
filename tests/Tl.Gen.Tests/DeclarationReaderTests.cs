using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.Tests;

public class DeclarationReaderTests
{
    private const string Shell = """
        using Tl;
        using Tl.Compiled;

        namespace Fix;

        public readonly record struct FClip(float Amount);

        public readonly struct FTrack : IBlend<FClip>
        {
            public void Blend(in FClip first, in FClip second, float t, out FClip result)
                => result = new FClip(first.Amount + (second.Amount - first.Amount) * t);
        }

        public static class Sites
        {
            internal static readonly CompiledTimelineInfo @@VARIABLE@@ = Timeline<FTrack, FClip>.Build(@@AUTHOR@@).Compile();
        }
        """;

    private static (IReadOnlyList<CompiledDeclaration> Declarations, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        ReadShell(string variable, string author)
        => DeclarationReader.Read([("Fix.cs",
            Shell.Replace("@@VARIABLE@@", variable).Replace("@@AUTHOR@@", author))]);

    [Fact]
    public void PartialTimeline_ReadsDefineWithoutCompilePlaceholder()
    {
        const string source = """
            using Tl;

            namespace Fix;

            public readonly record struct FClip(float Amount);

            public readonly struct FTrack : IBlend<FClip>
            {
                public void Blend(in FClip first, in FClip second, float t, out FClip result)
                    => result = new(first.Amount + (second.Amount - first.Amount) * t);
            }

            public readonly partial struct PulseTimeline : ITimeline<FTrack, FClip>
            {
                public static void Define(scoped TimelineBuilder<FTrack, FClip> timeline)
                {
                    var pulse = timeline.Track(new FTrack());
                    timeline.Clip(in pulse, new FClip(7f), 2u, 11u);
                    timeline.Looping();
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("PulseTimeline.cs", source)]);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        Assert.Equal(CompiledDeclarationKind.PartialTimeline, declaration.Kind);
        Assert.Equal("PulseTimeline", declaration.KernelName);
        Assert.Equal("Fix", declaration.Definition.Namespace);
        Assert.Equal("FTrack", declaration.Definition.TrackTypeName);
        Assert.Equal("FClip", declaration.Definition.ClipTypeName);
        Assert.True(declaration.Definition.Loops);
        var clip = Assert.Single(declaration.Definition.Clips);
        Assert.Equal(2u, clip.Start);
        Assert.Equal(11u, clip.End);
        Assert.Equal("new FClip(7f)", clip.PayloadExpression);
    }

    [Fact]
    public void PartialTimeline_PreservesUsingAndAliasContext()
    {
        const string source = """
            using Tl;
            using Domain = Game.Data;

            namespace Game.Data
            {
                public readonly record struct Clip(float Value);
                public readonly struct Track : IBlend<Clip>
                {
                    public void Blend(in Clip first, in Clip second, float t, out Clip result) => result = first;
                }
            }

            namespace Game.Playback
            {
                public readonly partial struct Pulse : ITimeline<Domain.Track, Domain.Clip>
                {
                    public static void Define(scoped TimelineBuilder<Domain.Track, Domain.Clip> timeline)
                    {
                        var track = timeline.Track(new Domain.Track());
                        timeline.Clip(in track, new Domain.Clip(3f), 0u, 4u);
                    }
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("Pulse.cs", source)]);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        Assert.Contains("using Domain = Game.Data;", declaration.Definition.SourceUsings);
        var plan = RegionAnalyzer.Analyze(declaration.Definition);
        var kernel = KernelEmitter.EmitKernel(
            plan,
            WorkSlotMaterializer.ForRegions(plan),
            declaration.KernelName,
            declaration.File,
            declaration.Line,
            declaration.Kind);
        Assert.Contains("using Domain = Game.Data;", kernel);
        Assert.Empty(CSharpSyntaxTree.ParseText(kernel).GetDiagnostics());
    }

    [Fact]
    public void PartialTimeline_PreservesEscapedIdentifier()
    {
        const string source = """
            using Tl;
            namespace Fix;
            public readonly record struct Clip(float Value);
            public readonly struct Track : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float t, out Clip result) => result = first;
            }
            public readonly partial struct @class : ITimeline<Track, Clip>
            {
                public static void Define(scoped TimelineBuilder<Track, Clip> timeline)
                {
                    var track = timeline.Track(new Track());
                    timeline.Clip(in track, new Clip(1f), 0u, 1u);
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("Keyword.cs", source)]);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        Assert.Equal("@class", declaration.KernelName);
        var plan = RegionAnalyzer.Analyze(declaration.Definition);
        var kernel = KernelEmitter.EmitKernel(
            plan,
            WorkSlotMaterializer.ForRegions(plan),
            declaration.KernelName,
            declaration.File,
            declaration.Line,
            declaration.Kind);
        Assert.Contains("public readonly partial struct @class", kernel);
        Assert.Empty(CSharpSyntaxTree.ParseText(kernel).GetDiagnostics());
    }

    [Fact]
    public void PartialTimeline_RejectsConflictingAliasesAcrossParts()
    {
        const string declaration = """
            using Tl;
            using Domain = First.Data;

            namespace First { public sealed class Data; }
            namespace Second { public sealed class Data; }
            namespace Fix
            {
                public readonly record struct Clip(float Value);
                public readonly struct Track : IBlend<Clip>
                {
                    public void Blend(in Clip first, in Clip second, float t, out Clip result) => result = first;
                }
                public readonly partial struct Pulse : ITimeline<Track, Clip> { }
            }
            """;
        const string definition = """
            using Tl;
            using Domain = Second.Data;

            namespace Fix
            {
                public readonly partial struct Pulse
                {
                    public static void Define(scoped TimelineBuilder<Track, Clip> timeline)
                    {
                        var track = timeline.Track(new Track());
                        timeline.Clip(in track, new Clip(1f), 0u, 1u);
                    }
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read(
            [("Declaration.cs", declaration), ("Definition.cs", definition)]);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN13", diagnostic.Code);
        Assert.Equal("Definition.cs", diagnostic.File);
        Assert.Contains("Domain", diagnostic.Message);
    }

    [Fact]
    public void StaticLambda_ReadsTracksClipsAndLoops()
    {
        var (declarations, diagnostics) = ReadShell("Alpha", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                var t1 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0, end: 10);
                b.Clip(in t1, 2f, 5u, 15u);
                b.Looping();
            }
            """);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        Assert.Equal("CompiledAlpha", declaration.KernelName);
        Assert.Equal(2, declaration.TrackCount);
        Assert.Equal(2, declaration.ClipCount);
        Assert.True(declaration.Definition.Loops);
        Assert.Equal("Fix", declaration.Definition.Namespace);
        Assert.Equal("FTrack", declaration.Definition.TrackTypeName);
        Assert.Equal("FClip", declaration.Definition.ClipTypeName);
        Assert.Equal(0u, declaration.Definition.Clips[0].Start);
        Assert.Equal(10u, declaration.Definition.Clips[0].End);
        Assert.Equal("new FClip(1f)", declaration.Definition.Clips[0].PayloadExpression);
        Assert.Equal(5u, declaration.Definition.Clips[1].Start);
        Assert.Equal(15u, declaration.Definition.Clips[1].End);
    }

    [Fact]
    public void StaticMethodGroup_ResolvesAcrossSources()
    {
        var author = """
            public static class Authoring
            {
                public static void Author(scoped TimelineBuilder<FTrack, FClip> b)
                {
                    var t0 = b.Track(new FTrack());
                    b.Clip(in t0, new FClip(3f), start: 2, end: 8);
                }
            }
            """;
        var (declarations, diagnostics) = DeclarationReader.Read(
            [("Fix.cs", Shell.Replace("@@VARIABLE@@", "Beta").Replace("@@AUTHOR@@", "Authoring.Author") + "\n" + author)]);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        Assert.Equal("CompiledBeta", declaration.KernelName);
        var clip = Assert.Single(declaration.Definition.Clips);
        Assert.Equal(2u, clip.Start);
        Assert.Equal(8u, clip.End);
    }

    [Fact]
    public void QualifiedMethodGroupSelectsItsContainingType()
    {
        var authors = """
            public static class OtherAuthoring
            {
                public static void Author(scoped TimelineBuilder<FTrack, FClip> b)
                {
                    var t0 = b.Track(new FTrack());
                    b.Clip(in t0, new FClip(19f), start: 0, end: 19);
                }
            }

            public static class QualifiedAuthoring
            {
                public static void Author(scoped Tl.TimelineBuilder<FTrack, FClip> b)
                {
                    var t0 = b.Track(new FTrack());
                    b.Clip(in t0, new FClip(1f), start: 0, end: 1);
                }
            }
            """;
        var source = Shell
            .Replace("@@VARIABLE@@", "QualifiedAuthor")
            .Replace("@@AUTHOR@@", "QualifiedAuthoring.Author")
            + "\n" + authors;

        var (declarations, diagnostics) = DeclarationReader.Read([("QualifiedAuthor.cs", source)]);

        Assert.Empty(diagnostics);
        var clip = Assert.Single(Assert.Single(declarations).Definition.Clips);
        Assert.Equal(1u, clip.End);
    }

    [Fact]
    public void QualifiedMethodGroupDoesNotFallThroughAfterParameterMismatch()
    {
        var declaration = """
            using Tl;
            using Tl.Compiled;

            namespace Fix
            {
                public readonly record struct FClip(float Amount);

                public readonly struct FTrack : IBlend<FClip>
                {
                    public void Blend(in FClip first, in FClip second, float t, out FClip result) => result = first;
                }

                public static class Sites
                {
                    internal static readonly CompiledTimelineInfo Qualified = Timeline<FTrack, FClip>.Build(First.Authoring.Author).Compile();
                }
            }

            namespace First
            {
                public static class Authoring
                {
                    public static void Author(scoped Tl.TimelineBuilder<global::Fix.FTrack, global::Fix.FClip> b) { }
                }
            }

            namespace Second
            {
                public static class Authoring
                {
                    public static void Author(scoped Tl.TimelineBuilder<Fix.FTrack, Fix.FClip> b)
                    {
                        var t0 = b.Track(new Fix.FTrack());
                        b.Clip(in t0, new Fix.FClip(19f), start: 0, end: 19);
                    }
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("QualifiedMismatch.cs", declaration)]);

        Assert.Empty(declarations);
        Assert.Equal("TLGEN06", Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void BareMethodGroupDoesNotBindOutsideItsContainingType()
    {
        var author = """
            public static class OtherAuthoring
            {
                public static void Author(scoped TimelineBuilder<FTrack, FClip> b)
                {
                    b.Track(new FTrack());
                }
            }
            """;
        var source = Shell
            .Replace("@@VARIABLE@@", "Bare")
            .Replace("@@AUTHOR@@", "Author")
            + "\n" + author;

        var (declarations, diagnostics) = DeclarationReader.Read([("Bare.cs", source)]);

        Assert.Empty(declarations);
        Assert.Equal("TLGEN06", Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void NonStaticLambda_IsRejectedWithInterpreterPointer()
    {
        var (declarations, diagnostics) = ReadShell("Gamma", """
            b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0, end: 10);
            }
            """);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN02", diagnostic.Code);
        Assert.Contains("in-memory interpreter", diagnostic.Message);
    }

    [Fact]
    public void NonLiteralPayload_IsRejected()
    {
        var (declarations, diagnostics) = ReadShell("Delta", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, FClip.Live, start: 0, end: 10);
            }
            """);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN04", diagnostic.Code);
        Assert.Contains("compile-time constants", diagnostic.Message);
    }

    [Fact]
    public void CapturedTrackVarFromOutside_IsRejected()
    {
        var (declarations, diagnostics) = ReadShell("Epsilon", """
            static b =>
            {
                b.Clip(in elsewhere, new FClip(1f), start: 0, end: 10);
            }
            """);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN05", diagnostic.Code);
    }

    [Fact]
    public void UnsupportedStatement_IsRejected()
    {
        var (declarations, diagnostics) = ReadShell("Zeta", """
            static b =>
            {
                if (true) { }
            }
            """);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN03", diagnostic.Code);
    }

    [Fact]
    public void InvalidWindow_FailsTimelineValidation()
    {
        var (declarations, diagnostics) = ReadShell("Eta", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 10, end: 10);
            }
            """);

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN08", diagnostic.Code);
    }

    [Fact]
    public void UnresolvableMethodGroup_IsRejected()
    {
        var (declarations, diagnostics) = ReadShell("Theta", "Missing.Author");

        Assert.Empty(declarations);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN06", diagnostic.Code);
    }

    [Fact]
    public void EmittedKernel_ParsesAndCarriesTheSpecializedFacts()
    {
        var (declarations, diagnostics) = ReadShell("Iota", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                var t1 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0, end: 10);
                b.Clip(in t1, new FClip(2f), start: 5, end: 10);
                b.Clip(in t1, new FClip(3f), start: 5, end: 15);
            }
            """);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);

        var plan = RegionAnalyzer.Analyze(declaration.Definition);
        Assert.Equal(15u, plan.Duration);
        Assert.Equal(2, plan.MaxActiveTracks); // [5,10) has both tracks, one blend

        var slots = WorkSlotMaterializer.ForRegions(plan);
        var kernel = KernelEmitter.EmitKernel(plan, slots, declaration.KernelName, declaration.File, declaration.Line);

        var tree = CSharpSyntaxTree.ParseText(kernel);
        Assert.Empty(tree.GetDiagnostics());
        Assert.Contains("public const uint Duration = 15u;", kernel);
        Assert.Contains("s_track0", kernel);
        Assert.Contains("TResult.Forward", kernel);

        var shared = KernelEmitter.EmitSharedRuntime();
        Assert.Empty(CSharpSyntaxTree.ParseText(shared).GetDiagnostics());
    }

    [Fact]
    public void WorkSlots_MatchTheRuntimeConventions()
    {
        var (declarations, diagnostics) = ReadShell("Kappa", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0, end: 7);
                b.Clip(in t0, new FClip(2f), start: 3, end: 11);
            }
            """);

        Assert.Empty(diagnostics);
        var plan = RegionAnalyzer.Analyze(Assert.Single(declarations).Definition);
        var slots = WorkSlotMaterializer.ForRegions(plan);

        // Regions: [0,3) standalone clip 0; [3,7) the blend pair; [7,11)
        // standalone clip 1; [11,inf) the empty sentinel. The pair carries
        // the OUTER window (0..11: min starts, max ends) as its entry
        // references and the shared blend window (3, length 4), exactly as
        // PlaybackCore.MaterializeWorkSlots computes at runtime.
        Assert.Equal(4, slots.Length);
        Assert.Empty(slots[3]);

        var pair = slots[1].Single(s => s.Second != EmittedWorkSlot.Single);
        Assert.Equal(0u, pair.EnterF);
        Assert.Equal(11u, pair.EnterB);
        Assert.Equal(3u, pair.FactorStart);
        Assert.Equal(4u, pair.FactorLength);
        var single = slots[0].Single();
        Assert.Equal(EmittedWorkSlot.Single, single.Second);
        Assert.Equal(0u, single.EnterF);
        Assert.Equal(7u, single.EnterB);
    }

    [Fact]
    public void UnrelatedCompileInvocation_IsIgnored()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            Expression<Func<int>> expression = () => 1;
            expression.Compile();
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("Other.cs", source)]);

        Assert.Empty(declarations);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("Tl.Timeline<FTrack, FClip>")]
    [InlineData("global::Tl.Timeline<FTrack, FClip>")]
    public void QualifiedTimelineReceiver_IsRecognized(string receiver)
    {
        var source = Shell
            .Replace("@@VARIABLE@@", "Qualified")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }")
            .Replace("Timeline<FTrack, FClip>", receiver);

        var (declarations, diagnostics) = DeclarationReader.Read([("Qualified.cs", source)]);

        Assert.Empty(diagnostics);
        Assert.Single(declarations);
    }

    [Fact]
    public void GlobalTlUsingRecognizesBareTimelineReceiver()
    {
        var source = Shell
            .Replace("using Tl;", "using global::Tl;")
            .Replace("@@VARIABLE@@", "GlobalUsing")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }");

        var (declarations, diagnostics) = DeclarationReader.Read([("GlobalUsing.cs", source)]);

        Assert.Empty(diagnostics);
        Assert.Single(declarations);
    }

    [Fact]
    public void TlChildNamespaceRecognizesBareTimelineReceiver()
    {
        var source = Shell
            .Replace("using Tl;\n", "")
            .Replace("namespace Fix;", "namespace Tl.App;")
            .Replace("@@VARIABLE@@", "ChildNamespace")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }");

        var (declarations, diagnostics) = DeclarationReader.Read([("ChildNamespace.cs", source)]);

        Assert.Empty(diagnostics);
        Assert.Single(declarations);
    }

    [Fact]
    public void UnrelatedNestedTimelineDoesNotHideTlTimelineReceiver()
    {
        var source = Shell
            .Replace("@@VARIABLE@@", "Nested")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }")
            + "\npublic static class OtherContainer { public sealed class Timeline<TTrack, TClip> { } }";

        var (declarations, diagnostics) = DeclarationReader.Read([("Nested.cs", source)]);

        Assert.Empty(diagnostics);
        Assert.Single(declarations);
    }

    [Fact]
    public void OtherQualifiedTimelineReceiver_IsIgnored()
    {
        var source = Shell
            .Replace("@@VARIABLE@@", "Other")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }")
            .Replace("Timeline<FTrack, FClip>", "Other.Timeline<FTrack, FClip>");

        var (declarations, diagnostics) = DeclarationReader.Read([("Other.cs", source)]);

        Assert.Empty(declarations);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void OtherBareTimelineReceiver_IsIgnored()
    {
        var source = """
            using Tl.Compiled;
            using Other;

            namespace Other
            {
                public readonly struct Timeline<TTrack, TClip>
                {
                    public static Timeline<TTrack, TClip> Build<TAuthor>(TAuthor author) => default;
                }

                public static class Extensions
                {
                    public static CompiledTimelineInfo Compile<TTrack, TClip>(this Timeline<TTrack, TClip> timeline) => default;
                }
            }

            namespace Fix
            {
                public static class Sites
                {
                    internal static readonly CompiledTimelineInfo Other = Timeline<int, int>.Build(static () => { }).Compile();
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("Other.cs", source)]);

        Assert.Empty(declarations);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NumericLiteralValues_SupportSeparatorsHexAndBinary()
    {
        var (declarations, diagnostics) = ReadShell("Literals", """
            static b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0b1_0, end: 0xFFFF_FFFFu);
            }
            """);

        Assert.Empty(diagnostics);
        var clip = Assert.Single(Assert.Single(declarations).Definition.Clips);
        Assert.Equal(2u, clip.Start);
        Assert.Equal(uint.MaxValue, clip.End);
    }

    [Theory]
    [InlineData("1L")]
    [InlineData("1UL")]
    [InlineData("4_294_967_296UL")]
    public void NumericLiteralWithoutImplicitUintConversion_IsRejected(string literal)
    {
        var (declarations, diagnostics) = ReadShell("Wide", $$"""
            static b =>
            {
                var t0 = b.Track(new FTrack());
                b.Clip(in t0, new FClip(1f), start: 0, end: {{literal}});
            }
            """);

        Assert.Empty(declarations);
        Assert.Equal("TLGEN10", Assert.Single(diagnostics).Code);
    }

    [Theory]
    [InlineData("System.DateTime.UtcNow.Ticks > 0")]
    [InlineData("1")]
    [InlineData("default")]
    public void DedupStorageArgument_MustBeABooleanLiteral(string argument)
    {
        var (declarations, diagnostics) = ReadShell("Dedup", $$"""
            static b =>
            {
                b.DedupStorage({{argument}});
            }
            """);

        Assert.Empty(declarations);
        Assert.Equal("TLGEN03", Assert.Single(diagnostics).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("false")]
    public void DedupStorageLiteralForms_AreAccepted(string argument)
    {
        var (declarations, diagnostics) = ReadShell("Dedup", $$"""
            static b =>
            {
                b.DedupStorage({{argument}});
            }
            """);

        Assert.Empty(diagnostics);
        Assert.Single(declarations);
    }

    [Fact]
    public void KernelNamesThatDifferOnlyByCaseAreRejected()
    {
        var first = Shell
            .Replace("@@VARIABLE@@", "FOO")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }");
        var second = Shell
            .Replace("@@VARIABLE@@", "Foo")
            .Replace("@@AUTHOR@@", "static b => { b.Track(new FTrack()); }");

        var (declarations, diagnostics) = DeclarationReader.Read([("First.cs", first), ("Second.cs", second)]);

        Assert.Single(declarations);
        Assert.Equal("TLGEN07", Assert.Single(diagnostics).Code);
    }

    [Theory]
    [InlineData("Runtime")]
    [InlineData("RUNTIME")]
    public void SharedRuntimeKernelNameIsReserved(string variable)
    {
        var (declarations, diagnostics) = ReadShell(variable, "static b => { b.Track(new FTrack()); }");

        Assert.Empty(declarations);
        Assert.Equal("TLGEN07", Assert.Single(diagnostics).Code);
    }
}
