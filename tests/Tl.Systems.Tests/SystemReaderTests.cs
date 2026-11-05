using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class SystemReaderTests
{
    internal static (IReadOnlyList<SystemDecl> Systems, IReadOnlyList<SystemAssignment> Assignments, IReadOnlyList<DeclarationDiagnostic> Diagnostics) Read(string sources)
    {
        var tree = CSharpSyntaxTree.ParseText(sources, new CSharpParseOptions(LanguageVersion.Latest), "Case.cs");
        var compilation = CSharpCompilation.Create("TlSystemsReaderCases", [tree], EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        return SystemReader.Read(compilation);
    }

    internal static string Code(IReadOnlyList<DeclarationDiagnostic> diagnostics, string code)
    {
        var match = diagnostics.FirstOrDefault(diagnostic => diagnostic.Code == code);
        Assert.True(match is not null, $"Expected {code}; got:\n{string.Join("\n", diagnostics.Select(diagnostic => diagnostic.ToString()))}");
        return match.Message;
    }

    const string Prelude = """
        using Tl;
        public struct T1 { public float V; public T1(float v) { V = v; } }
        public struct C1 { public float W; public C1(float w) { W = w; } }
        public struct ColA { public float V; public ColA(float v) { V = v; } }
        public struct ColB { public float V; public ColB(float v) { V = v; } }
        """;

    [Fact]
    public void UnknownColumnTypeIsTlgen90()
    {
        var read = Read(Prelude + """
        public struct Orphan { public int X; }
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, in Orphan o) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN90");
    }

    [Fact]
    public void AmbiguousFitWithoutMemberIsTlgen91()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        [TimelineSystem]
        public ref partial struct S2 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN91");
    }

    [Fact]
    public void FoldFeedOnEventMethodIsTlgen92()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void Fold(in Frame<T1, C1> frame, out float lift) { lift = 0; }
            public static void OnClipStart(in Frame<T1, C1> frame, in float lift) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN92");
    }

    [Fact]
    public void ManagedParameterIsTlgen93()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, string name) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN93");
    }

    [Fact]
    public void RequestsByInIsTlgen94()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; public Requests R; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, in Requests r) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN94");
    }

    [Fact]
    public void ActivePlusBatchIsTlgen95()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void Fold(in Frame<T1, C1> frame, out float lift) { lift = 0; }
            public static void ExecuteActive(in Frame<T1, C1> frame, in float lift, ref ColA a) { }
            public static void ExecuteBatch(in Frame<T1, C1> frame, global::System.ReadOnlySpan<BatchItem<float>> items, global::System.Span<ColA> a) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN95");
    }

    [Fact]
    public void BatchElementMismatchIsTlgen96()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void Fold(in Frame<T1, C1> frame, out float lift) { lift = 0; }
            public static void ExecuteBatch(in Frame<T1, C1> frame, global::System.ReadOnlySpan<BatchItem<int>> items, global::System.Span<ColA> a) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN96");
    }

    [Fact]
    public void MarkerMethodWithoutMarkerInterfaceIsTlgen97()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a) { }
            public static void OnMarker(in MarkerFrame<C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN97");
    }

    [Fact]
    public void FitsNothingWarnsTlgen98()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        [TimelineSystem]
        public ref partial struct S2 { public global::System.Span<ColB> B; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a, ref ColB b) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN98");
        Assert.DoesNotContain(read.Diagnostics, static diagnostic => diagnostic.Code != "TLGEN98");
    }

    [Fact]
    public void NonPartialSystemIsTlgen99()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref struct S1 { public global::System.Span<ColA> A; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN99");
    }

    [Fact]
    public void ManagedSystemFieldIsTlgen99()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; public string Name; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Code(read.Diagnostics, "TLGEN99");
    }

    [Fact]
    public void DuplicateSystemColumnsAreTlgen99()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; public global::System.Span<ColA> B; }
        """);
        Code(read.Diagnostics, "TLGEN99");
    }

    [Fact]
    public void UnknownMemberSystemIsTlgen99()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; }
        [TimelineSystemMember(typeof(S1))]
        public readonly struct C : ITrack<T1, C1>
        {
            public static void ExecuteActive(in Frame<T1, C1> frame, ref ColA a) { }
            public static void OnClipEnd(in Frame<T1, C1> frame) { }
        }
        """);
        Assert.DoesNotContain(read.Diagnostics, static diagnostic => diagnostic.Code == "TLGEN99");
    }

    [Fact]
    public void CleanPairAndSystemProducesAssignment()
    {
        var read = Read(Prelude + """
        [TimelineSystem]
        public ref partial struct S1 { public global::System.Span<ColA> A; public Requests R; }
        public readonly struct C : ITrack<T1, C1>
        {
            public static void Fold(in Frame<T1, C1> frame, out float lift) { lift = 0; }
            public static void ExecuteActive(in Frame<T1, C1> frame, in float lift, ref ColA a, ref Requests r) { }
        }
        """);
        Assert.Empty(read.Diagnostics);
        var assignment = Assert.Single(read.Assignments);
        var consumer = Assert.Single(assignment.Consumers);
        Assert.Equal("global::C", consumer.ConsumerTypeName);
        Assert.Equal(2, consumer.Methods.Count);
    }
}
