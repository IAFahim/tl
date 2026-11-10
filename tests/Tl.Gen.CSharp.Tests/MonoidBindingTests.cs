using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class MonoidBindingTests
{
    const string Prelude = """
        using System;
        using Tl;
        namespace T
        {
        public struct Cl { public float V; }
        public struct Tr : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
        [TimelineSystem] public ref partial struct S { public Span<float> F; }
        """;

    static CSharpCompilation Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "Kinds.cs");
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        return CSharpCompilation.Create("MonoidBindingTests", [tree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    static string Emit(string body)
    {
        var emitted = SystemGeneration.Emit(Compile(Prelude + body + "\n}\n"), "Proof");
        var errors = string.Join("\n", emitted.Diagnostics.Select(static d => d.ToString()));
        Assert.True(emitted.Diagnostics.Count == 0, errors);
        var artifact = Assert.Single(emitted.Artifacts);
        return artifact.Content.Replace("\r\n", "\n");
    }

    [Fact]
    public void MonoidKindEmitsKindTableCombineBinder()
    {
        var generated = Emit("""
            [TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref float c) => c = c + t; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }
            """);
        Assert.Contains("internal static float TlKind_0_Combine(float __total, float __v)", generated);
        Assert.Contains("global::T.K.Combine(ref __total, in __v);", generated);
        Assert.Contains("internal sealed class TlKindBinder_0 : global::Tl.Kind<float>", generated);
        Assert.Contains("public override int Id => 1;", generated);
        Assert.Contains("internal static void TlKind_0_ApplyAffine(in float __value, ref float __column)", generated);
        Assert.Contains("if (!world.TryKindTable<float>(1, out _)) world.RegisterKind<float>(new TlKindBinder_0());", generated);
        Assert.Contains("var __table = world.KindTable<float>(1);", generated);
        Assert.Contains("var __acc = world.Accumulator;", generated);
        Assert.Contains("__table.Add(__r, __acc[__r]);", generated);
        Assert.Contains("__table.ApplyAll(__c0);", generated);
        Assert.Contains("__table.Reset();", generated);
    }

    [Fact]
    public void OpaqueKindKeepsDirectCallsWithoutBinder()
    {
        var generated = Emit("""
            [TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t = t - n; public static void Apply(in float t, ref float c) => c = t; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }
            """);
        Assert.DoesNotContain("TlKind_0_Combine", generated);
        Assert.DoesNotContain("TlKindBinder_0", generated);
        Assert.Contains("float __v = __acc[__e];", generated);
        Assert.Contains("float __total = __v;", generated);
        Assert.Contains("global::T.K.Apply(in __total, ref __c0[__e]);", generated);
    }

    [Fact]
    public void MonoidEmissionIsDeterministic()
    {
        var body = """
            [TimelineKind] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref float c) => c = c + t; }
            public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }
            """;
        Assert.Equal(Emit(body), Emit(body));
    }
}
