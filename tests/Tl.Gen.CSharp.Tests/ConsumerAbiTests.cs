using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class ConsumerAbiTests
{
    private const string NineParameterSource = """
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
            public static void OnActive(in Frame<Track, Clip> frame, in Mass a1, in Mass a2, in Mass a3, in Mass a4, in Mass a5, in Mass a6, in Mass a7, in Mass a8, in Mass a9, in Mass a10, in Mass a11, in Mass a12, in Mass a13, in Mass a14, in Mass a15, in Mass a16, ref Mass r1, ref Mass r2, ref Mass r3, ref Mass r4, ref Mass r5, ref Mass r6, ref Mass r7, ref Mass r8, ref Mass r9, ref Mass r10, ref Mass r11, ref Mass r12, ref Mass r13, ref Mass r14, ref Mass last) { }
        }
        """;

    private const string FourParameterSource = """
        using Tl;
        namespace Domain;
        public readonly record struct QuadClip(float Amount, int Ticks);
        public readonly record struct QuadTrack(float Multiplier) : IBlend<QuadClip>
        {
            public void Blend(in QuadClip first, in QuadClip second, float factor, out QuadClip result) => result = first;
        }
        public readonly struct QuadJob : ITrack<QuadTrack, QuadClip>
        {
            public static void OnMemo(in Frame<QuadTrack, QuadClip> frame, out float arc, out int ticks)
            {
                arc = frame.Clip.Amount * frame.Track.Multiplier;
                ticks = frame.Clip.Ticks;
            }
            public static void OnActive(in float arc, in int ticks, ref float y, in int multiplier) => y += arc * ticks * multiplier;
        }
        """;

    private const string FourParameterBinding = """"
        internal static unsafe class TlConsumerBinding
        {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Install()
        {
        global::Tl.PairRuntime<global::Domain.QuadTrack, global::Domain.QuadClip>.Consume(&OnMemo_QuadJob, &OnMemoRange_QuadJob, &Keys_QuadJob);
        global::Tl.PairRuntime<global::Domain.QuadTrack, global::Domain.QuadClip>.ConsumeDispatch(&OnActive_QuadJob, &LiveKeys_QuadJob, &Diag_QuadJob);
        }
        private static void OnMemo_QuadJob(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)
        {
        global::Domain.QuadClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.QuadTrack, global::Domain.QuadClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);
        var @arc = (float*)__tlColumns[0];
        var @ticks = (int*)__tlColumns[1];
        global::Domain.QuadJob.OnMemo(in __tlTyped, out @arc[__tlRow], out @ticks[__tlRow]);
        }
        private static void OnMemoRange_QuadJob(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRowStart, int __tlRowCount)
        {
        global::Domain.QuadClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.QuadTrack, global::Domain.QuadClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);
        var @arc = (float*)__tlColumns[0];
        var @ticks = (int*)__tlColumns[1];
        for (var __tlRow = __tlRowStart; __tlRow < __tlRowStart + __tlRowCount; __tlRow++)
        global::Domain.QuadJob.OnMemo(in __tlTyped, out @arc[__tlRow], out @ticks[__tlRow]);
        }
        private static int Keys_QuadJob(ulong* __tlKeys, byte* __tlMeta)
        {
        if (__tlKeys != null) { __tlKeys[0] = global::Tl.TypeKey<float>.Value; __tlMeta[0] = 20; }
        if (__tlKeys != null) { __tlKeys[1] = global::Tl.TypeKey<int>.Value; __tlMeta[1] = 20; }
        return 2;
        }
        private static void OnActive_QuadJob(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)
        {
        var @arc = *(float*)__tlColumns[0];
        var @ticks = *(int*)__tlColumns[1];
        var @y = (float*)__tlColumns[2];
        var @multiplier = (int*)__tlColumns[3];
        global::Domain.QuadJob.OnActive(in @arc, in @ticks, ref @y[__tlRow], in @multiplier[__tlRow]);
        }
        private static int LiveKeys_QuadJob(ulong* __tlKeys, byte* __tlMeta)
        {
        if (__tlKeys != null) { __tlKeys[0] = global::Tl.TypeKey<float>.Value; __tlMeta[0] = 68; }
        if (__tlKeys != null) { __tlKeys[1] = global::Tl.TypeKey<int>.Value; __tlMeta[1] = 68; }
        if (__tlKeys != null) { __tlKeys[2] = global::Tl.TypeKey<float>.Value; __tlMeta[2] = 36; }
        if (__tlKeys != null) { __tlKeys[3] = global::Tl.TypeKey<int>.Value; __tlMeta[3] = 4; }
        return 4;
        }
        private static void Diag_QuadJob(ulong __tlKey, long __tlSlot)
        {
        if (__tlSlot == 0) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type float (arc); none was passed.");
        if (__tlSlot == 1) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type int (ticks); none was passed.");
        if (__tlSlot == 2) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type float (y); none was passed.");
        if (__tlSlot == 3) throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires a column of type int (multiplier); none was passed.");
        throw new global::System.ArgumentException("Timeline<Domain.QuadTrack, Domain.QuadClip> consumer 'Domain.QuadJob' OnActive requires caller columns that were not passed.");
        }
        private static int FindKey(ulong* k, int c, ulong v) { for (var i = 0; i < c; i++) if (k[i] == v) return i; return -1; }
        }

        """";

    [Fact]
    public void RejectsThirtyOneParameterJobWithDuplicateTypesAndEmitsNothing()
    {
        var (sources, diagnostics) = ConsumerBindingTests.GenerateWithDiagnostics(NineParameterSource);
        Assert.Empty(sources);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN68", diagnostic.Id);
        Assert.Contains("declares 31 gameplay parameters", diagnostic.GetMessage());
        Assert.Contains("the consumer ABI holds 30 gameplay columns per registered consumer (memo feeds included)", diagnostic.GetMessage());
        Assert.Contains("declare at most 30.", diagnostic.GetMessage());
    }

    [Fact]
    public void ExcessParameterDiagnosticUsesTheLastParameterSpan()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(NineParameterSource, options, "Domain.cs");
        var compilation = CSharpCompilation.Create("ConsumerAbiSpan" + Guid.NewGuid().ToString("N"),
            [tree], ConsumerBindingTests.ReferencePaths().Select(static path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        var parameter = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>()
            .Single(static parameter => parameter.Identifier.ValueText == "last");
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
        var slots = new TimelineSlot[41];
        for (var i = 0; i < slots.Length; i++) slots[i] = new($"slot{i}", "int", SlotMode.Input);
        var binding = JobEmitter.Consumers([new JobConsumer("Track", "Clip", new JobDefinition("Job", slots))]);

        Assert.Contains("41 gameplay parameters exceed the 40-slot consumer ABI; regenerate the binding with a matching Tl generator.", binding);
        var bindStart = binding.IndexOf("private static void Bind_Job(", StringComparison.Ordinal);
        var throwIndex = binding.IndexOf("throw new global::System.InvalidOperationException(", bindStart, StringComparison.Ordinal);
        var writeIndex = binding.IndexOf("__tlIndices[", bindStart, StringComparison.Ordinal);
        Assert.True(throwIndex >= 0);
        Assert.True(writeIndex < 0 || throwIndex < writeIndex);
    }
}
