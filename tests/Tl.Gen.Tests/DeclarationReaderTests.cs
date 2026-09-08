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
        Assert.Contains("s_works1", kernel);
        Assert.Contains("CompiledWorkSlot.Single", kernel);

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
        Assert.Equal(0, pair.BlendOrdinal);

        var single = slots[0].Single();
        Assert.Equal(EmittedWorkSlot.Single, single.Second);
        Assert.Equal(0u, single.EnterF);
        Assert.Equal(7u, single.EnterB);
    }
}
