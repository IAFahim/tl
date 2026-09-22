using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class TwoMethodShapeTests
{
    private const string Domain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
        }
        """;

    [Fact]
    public void OnMemoWithSingleOutProducesMeasuredConsumer()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Arc : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc)
                    => arc = frame.Clip.Amount * frame.Track.Multiplier;
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.MemoMethod);
        Assert.False(consumer.Job.Dispatch);
        Assert.Equal([new TimelineSlot("arc", "float", SlotMode.Output)], consumer.Job.Slots);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("Consume(&OnMemo_Arc, &OnMemoRange_Arc, &Keys_Arc);", binding);
        Assert.DoesNotContain("Bind_Arc", binding);
        Assert.Contains("global::Domain.Arc.OnMemo(in __tlTyped, out @arc[__tlRow]);", binding);
    }

    [Fact]
    public void OnMemoWithRefResultProducesMeasuredConsumer()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Arc : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, ref float arc)
                    => arc += frame.Clip.Amount;
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.MemoMethod);
        Assert.Equal([new TimelineSlot("arc", "float", SlotMode.Reference)], consumer.Job.Slots);
    }

    [Fact]
    public void OnMemoAndFrameOnlyOnActiveRegisterBothLanes()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Both : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 1f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.MemoMethod);
        Assert.True(consumer.Job.Dispatch);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("Consume(&OnMemo_Both, &OnMemoRange_Both, &Keys_Both);", binding);
        Assert.Contains("ConsumeDispatch(&OnActive_Both);", binding);
        Assert.Contains("global::Domain.Both.OnActive(in __tlTyped);", binding);
    }

    [Fact]
    public void FramelessOnActiveRegistersDispatch()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Bare : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive() { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.Dispatch);
        Assert.False(consumer.Job.LiveFrame);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("global::Domain.Bare.OnActive();", binding);
    }

    [Fact]
    public void LegacyRefFloatOnActiveKeepsMeasuredEmission()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Legacy : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float y) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.False(consumer.Job.MemoMethod);
        Assert.False(consumer.Job.Dispatch);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("Consume(&OnActive_Legacy, &OnActiveRange_Legacy, &Bind_Legacy);", binding);
        Assert.Contains("global::Domain.Legacy.OnActive(in __tlTyped, ref @y[__tlRow]);", binding);
    }

    [Fact]
    public void OnMemoWithoutFrameReportsTlgen75()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(out float arc) => arc = 0f;
            }
            """);

        AssertConsumerRejected(result, "TLGEN75");
    }

    [Theory]
    [InlineData("out string label")]
    [InlineData("in string label")]
    [InlineData("int amount = 3")]
    [InlineData("in int seed")]
    public void OnMemoManagedOrOptionalParameterReportsTlgen76(string parameter)
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, {{parameter}}, out float arc) { arc = 0f; }
            }
            """);

        AssertConsumerRejected(result, "TLGEN76");
    }

    [Fact]
    public void OnMemoWithoutResultReportsTlgen76()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame) { }
            }
            """);

        AssertConsumerRejected(result, "TLGEN76");
    }

    [Fact]
    public void OnActiveOutParameterReportsTlgen78()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 0f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, out float y) => y = 0f;
            }
            """);

        AssertConsumerRejected(result, "TLGEN78");
    }

    [Theory]
    [InlineData("public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out long a) { a = 0; }")]
    [InlineData("public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float a, out float b, out float c, out float d, out float e) { a = b = c = d = e = 0f; }")]
    public void PendingMemoShapesReportTlgen79(string memo)
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Pending : ITrack<DamageTrack, DamageClip>
            {
                {{memo}}
            }
            """);

        AssertConsumerRejected(result, "TLGEN79");
    }

    [Fact]
    public void MultiOutputMemoEmitsKeysThunkAndKeysConsume()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct DualClip(float Amount);
            public readonly record struct DualTrack(float Multiplier) : IBlend<DualClip>
            {
                public void Blend(in DualClip first, in DualClip second, float factor, out DualClip result) => result = first;
            }
            public readonly struct Dual : ITrack<DualTrack, DualClip>
            {
                public static void OnMemo(in Frame<DualTrack, DualClip> frame, out float a, out int b) { a = 0f; b = 0; }
            }
            """;
        var (sources, diagnostics) = ConsumerBindingTests.GenerateWithDiagnostics(source);
        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("Consume(&OnMemo_Dual, &OnMemoRange_Dual, &Keys_Dual);", binding);
        Assert.Contains("private static int Keys_Dual(ulong* __tlKeys, byte* __tlMeta)", binding);
        Assert.Contains("__tlKeys[0] = global::Tl.TypeKey<float>.Value; __tlMeta[0] = 20;", binding);
        Assert.Contains("__tlKeys[1] = global::Tl.TypeKey<int>.Value; __tlMeta[1] = 20;", binding);
        Assert.Contains("global::Domain.Dual.OnMemo(in __tlTyped, out @a[__tlRow], out @b[__tlRow]);", binding);
    }

    [Fact]
    public void MemoFedFramedOnActiveRegistersComposeColumns()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Pending : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 0f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, in float arc) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.Dispatch);
        Assert.Equal([new TimelineSlot("arc", "float", SlotMode.MemoFeed)], consumer.Job.LiveColumns);
    }

    [Fact]
    public void MemoAndRefOnActiveRegistersLiveColumns()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Pending : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 0f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float y) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.Dispatch);
        Assert.Equal([new TimelineSlot("y", "float", SlotMode.Reference)], consumer.Job.LiveColumns);
    }

    [Fact]
    public void ComposeOnActiveBindsMemoFedPrefixAndLiveColumns()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Jump : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = frame.Clip.Amount;
                public static void OnActive(in float arc, ref float y, in int multiplier) => y += arc * multiplier;
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.MemoMethod);
        Assert.True(consumer.Job.Dispatch);
        Assert.False(consumer.Job.LiveFrame);
        Assert.Equal([new TimelineSlot("arc", "float", SlotMode.MemoFeed), new TimelineSlot("y", "float", SlotMode.Reference), new TimelineSlot("multiplier", "int", SlotMode.Input)], consumer.Job.LiveColumns);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("Consume(&OnMemo_Jump, &OnMemoRange_Jump, &Keys_Jump);", binding);
        Assert.Contains("ConsumeDispatch(&OnActive_Jump, &LiveKeys_Jump, &Diag_Jump);", binding);
        Assert.Contains("var @arc = *(float*)__tlColumns[0];", binding);
        Assert.Contains("var @y = (float*)__tlColumns[1];", binding);
        Assert.Contains("var @multiplier = (int*)__tlColumns[2];", binding);
        Assert.Contains("global::Domain.Jump.OnActive(in @arc, ref @y[__tlRow], in @multiplier[__tlRow]);", binding);
        Assert.Contains("__tlKeys[1] = global::Tl.TypeKey<float>.Value; __tlMeta[1] = 36;", binding);
        Assert.Contains("__tlKeys[2] = global::Tl.TypeKey<int>.Value; __tlMeta[2] = 4;", binding);
        Assert.DoesNotContain("Bind_Jump", binding);
    }

    [Fact]
    public void FramedLiveColumnsRegisterWithoutMemo()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Push : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float y, in int multiplier) => y += frame.Clip.Amount * multiplier;
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.False(consumer.Job.MemoMethod);
        Assert.True(consumer.Job.Dispatch);
        Assert.True(consumer.Job.LiveFrame);
        Assert.Empty(consumer.Job.Slots);
        Assert.Equal([new TimelineSlot("y", "float", SlotMode.Reference), new TimelineSlot("multiplier", "int", SlotMode.Input)], consumer.Job.LiveColumns);
        var binding = JobEmitter.Consumers(result.Consumers);
        Assert.Contains("ConsumeDispatch(&OnActive_Push, &LiveKeys_Push, &Diag_Push);", binding);
        Assert.Contains("global::Domain.Push.OnActive(in __tlTyped, ref @y[__tlRow], in @multiplier[__tlRow]);", binding);
    }

    [Fact]
    public void FramelessLiveColumnsRegisterWithoutMemo()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Bare : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(ref float y) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.True(consumer.Job.Dispatch);
        Assert.False(consumer.Job.LiveFrame);
        Assert.Equal([new TimelineSlot("y", "float", SlotMode.Reference)], consumer.Job.LiveColumns);
    }

    [Fact]
    public void ExcessLiveColumnsReportTlgen68()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Oversized : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in float a, in float b, ref float c, in float d, in float e, in float f, in float g, in float h, in float i, in float j, in float k, in float l, in float m, in float n, in float o, in float p, in float q, in float r, in float s, in float t, in float u, in float v, in float w, in float x, in float y, in float z, in float aa, in float ab, in float ac, ref float ad, in float ae) { }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN68", diagnostic.Code);
        Assert.Contains("declares 31 gameplay parameters", diagnostic.Message);
        Assert.Contains("declare at most 30.", diagnostic.Message);
    }

    [Theory]
    [InlineData("out float y")]
    [InlineData("string label")]
    public void MalformedLiveColumnReportsTlgen78(string column)
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(ref float y, {{column}}) { y = 0f; }
            }
            """);

        AssertConsumerRejected(result, "TLGEN78");
    }

    [Fact]
    public void ManagedLiveColumnReportsTlgen78()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Broken : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, in object extra) { }
            }
            """);

        AssertConsumerRejected(result, "TLGEN78");
    }

    [Fact]
    public void DuplicateOnMemoReportsTlgen66()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Dup : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float a) => a = 0f;
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out int b) => b = 0;
            }
            """);

        AssertConsumerRejected(result, "TLGEN66");
    }

    [Fact]
    public void NeitherMethodReportsTlgen66()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Silent : ITrack<DamageTrack, DamageClip> { }
            """);

        AssertConsumerRejected(result, "TLGEN66");
    }

    [Fact]
    public void TwoDistinctLiveInColumnTypesReportTlgen81()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly record struct RatioClip(float Amount);
            public readonly record struct RatioTrack(float Scale) : IBlend<RatioClip>
            {
                public void Blend(in RatioClip first, in RatioClip second, float factor, out RatioClip result) => result = first;
            }
            public struct Mass { public float Value; }
            public readonly struct Mixed : ITrack<RatioTrack, RatioClip>
            {
                public static void OnActive(in Frame<RatioTrack, RatioClip> frame, in Mass mass, in float gain) { }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN81", diagnostic.Code);
        Assert.Contains("multiple distinct live 'in' column types", diagnostic.Message);
        Assert.Contains("(global::Domain.Mass, float)", diagnostic.Message);
        Assert.Contains("(wider surface pending)", diagnostic.Message);
        Assert.Empty(result.Consumers);
    }

    [Fact]
    public void TwoDistinctLiveRefColumnTypesReportTlgen81()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly record struct RatioClip(float Amount);
            public readonly record struct RatioTrack(float Scale) : IBlend<RatioClip>
            {
                public void Blend(in RatioClip first, in RatioClip second, float factor, out RatioClip result) => result = first;
            }
            public struct Health { public float Value; }
            public readonly struct Mixed : ITrack<RatioTrack, RatioClip>
            {
                public static void OnActive(in Frame<RatioTrack, RatioClip> frame, ref float y, ref Health health) { }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TLGEN81", diagnostic.Code);
        Assert.Contains("multiple distinct live 'ref' column types", diagnostic.Message);
        Assert.Contains("one typed caller column", diagnostic.Message);
        Assert.Empty(result.Consumers);
    }

    [Fact]
    public void MemoFedAndRefAndInputAcrossFourSlotsStayLegal()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly record struct RatioClip(float Amount);
            public readonly record struct RatioTrack(float Scale) : IBlend<RatioClip>
            {
                public void Blend(in RatioClip first, in RatioClip second, float factor, out RatioClip result) => result = first;
            }
            public readonly struct Full : ITrack<RatioTrack, RatioClip>
            {
                public static void OnMemo(in Frame<RatioTrack, RatioClip> frame, out float arc, out int ticks) { arc = 0f; ticks = 0; }
                public static void OnActive(in float arc, in int ticks, ref float y, in int multiplier) { }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var consumer = Assert.Single(result.Consumers);
        Assert.Equal(
        [
            new TimelineSlot("arc", "float", SlotMode.MemoFeed),
            new TimelineSlot("ticks", "int", SlotMode.MemoFeed),
            new TimelineSlot("y", "float", SlotMode.Reference),
            new TimelineSlot("multiplier", "int", SlotMode.Input),
        ], consumer.Job.LiveColumns);
    }

    private static void AssertConsumerRejected(JobReadResult result, string code)
    {
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Empty(result.Consumers);
    }

    private static JobReadResult Read(string source)
        => JobReader.Read(Compile(source, "TwoMethodShapeTests" + Guid.NewGuid().ToString("N")));

    private static CSharpCompilation Compile(string source, string assemblyName)
        => CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

    private static IEnumerable<MetadataReference> References()
        => CliFrontDoorTests.ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));
}
