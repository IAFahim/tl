using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
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
        Assert.Contains("Consume(&OnMemo_Arc, &OnMemoRange_Arc, &Bind_Arc);", binding);
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
        Assert.Contains("Consume(&OnMemo_Both, &OnMemoRange_Both, &Bind_Both);", binding);
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
    [InlineData("public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float a, out bool b) { a = 0f; b = false; }")]
    [InlineData("public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, in int seed, out float a) { a = 0f; }")]
    [InlineData("public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out int a) { a = 0; }")]
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
    public void MemoFedOnActiveInputReportsTlgen79()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Pending : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 0f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, in float arc) { }
            }
            """);

        AssertConsumerRejected(result, "TLGEN79");
    }

    [Fact]
    public void OnActiveLiveColumnReportsTlgen79()
    {
        var result = Read($$"""
            {{Domain}}
            public readonly struct Pending : ITrack<DamageTrack, DamageClip>
            {
                public static void OnMemo(in Frame<DamageTrack, DamageClip> frame, out float arc) => arc = 0f;
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float y) { }
            }
            """);

        AssertConsumerRejected(result, "TLGEN79");
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
