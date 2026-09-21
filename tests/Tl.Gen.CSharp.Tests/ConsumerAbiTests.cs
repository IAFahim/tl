using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class ConsumerAbiTests
{
    private const string FiveParameterSource = """
        using Tl;
        namespace Domain;
        public readonly record struct Clip(float Amount);
        public readonly record struct Track(float Multiplier) : IBlend<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
        }
        public struct Mass { public float Value; }
        public readonly struct OversizedJob : ITrack<Track, Clip>
        {
            public static void OnActive(in Frame<Track, Clip> frame, in Mass first, in Mass second, ref Mass third, ref Mass fourth, in Mass fifth) { }
        }
        """;

    private const string FourParameterSource = """
        using Tl;
        namespace Domain;
        public readonly record struct QuadClip(float Amount);
        public readonly record struct QuadTrack(float Multiplier) : IBlend<QuadClip>
        {
            public void Blend(in QuadClip first, in QuadClip second, float factor, out QuadClip result) => result = first;
        }
        public struct Alpha { public float Value; }
        public struct Beta { public float Value; }
        public struct Gamma { public float Value; }
        public struct Delta { public float Value; }
        public readonly struct QuadJob : ITrack<QuadTrack, QuadClip>
        {
            public static void OnActive(in Frame<QuadTrack, QuadClip> frame, in Alpha first, in Beta second, ref Gamma third, ref Delta fourth) { }
        }
        """;

    private const string FourParameterBinding = """"
        internal static unsafe class TlConsumerBinding
        {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Install()
        {
        global::Tl.PairRuntime<global::Domain.QuadTrack, global::Domain.QuadClip>.ConsumeDispatch(&OnActive_QuadJob, &LiveKeys_QuadJob, &Diag_QuadJob);
        }
        private static void OnActive_QuadJob(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)
        {
        global::Domain.QuadClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.QuadTrack, global::Domain.QuadClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);
        var @first = (global::Domain.Alpha*)__tlColumns[0];
        var @second = (global::Domain.Beta*)__tlColumns[1];
        var @third = (global::Domain.Gamma*)__tlColumns[2];
        var @fourth = (global::Domain.Delta*)__tlColumns[3];
        global::Domain.QuadJob.OnActive(in __tlTyped, in @first[__tlRow], in @second[__tlRow], ref @third[__tlRow], ref @fourth[__tlRow]);
        }
        private static int LiveKeys_QuadJob(ulong* __tlKeys, byte* __tlMeta)
        {
        if (__tlKeys != null) { __tlKeys[0] = global::Tl.TypeKey<global::Domain.Alpha>.Value; __tlMeta[0] = 4; }
        if (__tlKeys != null) { __tlKeys[1] = global::Tl.TypeKey<global::Domain.Beta>.Value; __tlMeta[1] = 4; }
        if (__tlKeys != null) { __tlKeys[2] = global::Tl.TypeKey<global::Domain.Gamma>.Value; __tlMeta[2] = 36; }
        if (__tlKeys != null) { __tlKeys[3] = global::Tl.TypeKey<global::Domain.Delta>.Value; __tlMeta[3] = 36; }
        return 4;
        }
        private static void Diag_QuadJob(ulong __tlKey, long __tlSlot)
        {
        if (__tlSlot == 0) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type Domain.Alpha (first); none was passed.");
        if (__tlSlot == 1) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type Domain.Beta (second); none was passed.");
        if (__tlSlot == 2) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type Domain.Gamma (third); none was passed.");
        if (__tlSlot == 3) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type Domain.Delta (fourth); none was passed.");
        throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires caller columns that were not passed.");
        }
        private static int FindKey(ulong* k, int c, ulong v) { for (var i = 0; i < c; i++) if (k[i] == v) return i; return -1; }
        }

        """";

    [Fact]
    public void RejectsFiveParameterJobWithDuplicateTypesAndEmitsNothing()
    {
        var (sources, diagnostics) = ConsumerBindingTests.GenerateWithDiagnostics(FiveParameterSource);
        Assert.Empty(sources);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN68", diagnostic.Id);
        Assert.Contains("declares 5 gameplay parameters", diagnostic.GetMessage());
        Assert.Contains("the consumer ABI reserves 4 pointer slots per registered consumer", diagnostic.GetMessage());
        Assert.Contains("declare at most 4 gameplay parameters", diagnostic.GetMessage());
    }

    [Fact]
    public void ExcessParameterDiagnosticUsesTheFifthParameterSpan()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(FiveParameterSource, options, "Domain.cs");
        var compilation = CSharpCompilation.Create("ConsumerAbiSpan" + Guid.NewGuid().ToString("N"),
            [tree], ConsumerBindingTests.ReferencePaths().Select(static path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        var parameter = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>()
            .Single(static parameter => parameter.Identifier.ValueText == "fifth");
        var diagnostic = Assert.Single(result.Diagnostics, static candidate => candidate.Id == "TLGEN68");
        Assert.Equal(parameter.Span, diagnostic.Location.SourceSpan);
        Assert.Equal("Domain.cs", diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void FourParameterJobGeneratesTheCommittedBindingByteIdentically()
    {
        var (sources, diagnostics) = ConsumerBindingTests.GenerateWithDiagnostics(FourParameterSource);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Equal(FourParameterBinding, binding);
    }

    [Fact]
    public void EmittedBindThrowsBeforeAnyWriteWhenSlotCountExceedsTheAbi()
    {
        var slots = new TimelineSlot[]
        {
            new("first", "int", SlotMode.Input),
            new("second", "int", SlotMode.Input),
            new("third", "int", SlotMode.Reference),
            new("fourth", "int", SlotMode.Reference),
            new("fifth", "int", SlotMode.Input),
        };
        var binding = JobEmitter.Consumers([new JobConsumer("Track", "Clip", new JobDefinition("Job", slots))]);

        Assert.Contains("5 gameplay parameters exceed the 4-slot consumer ABI; regenerate the binding with a matching Tl generator.", binding);
        var bindStart = binding.IndexOf("private static void Bind_Job(", StringComparison.Ordinal);
        var throwIndex = binding.IndexOf("throw new global::System.InvalidOperationException(", bindStart, StringComparison.Ordinal);
        var writeIndex = binding.IndexOf("__tlIndices[", bindStart, StringComparison.Ordinal);
        Assert.True(throwIndex >= 0);
        Assert.True(writeIndex < 0 || throwIndex < writeIndex);
    }
}
