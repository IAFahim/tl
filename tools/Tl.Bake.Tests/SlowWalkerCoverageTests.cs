using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tl.Bake.Oracle;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class SlowWalkerCoverageTests
{
    private static readonly BakerAssemblyResolver Resolver =
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    private static string Receipt(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static byte[] BakeSlow(string json) => TimelineBaker.BakeJson(json, Resolver, autoNamespace: true);

    private static string FloatData(int count)
    {
        var builder = new StringBuilder("{");
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append("\"f").Append(i).Append("\":1.5");
        }
        return builder.Append('}').ToString();
    }

    [Fact]
    public void LargeDuplicateScope_GrowsAndBakesIdenticallyToOracle()
    {
        var json =
            """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaFatTrack","data":{"Scale":1},"clips":[{"namespace":"FusedBake","type":"GaFatClip","start":0,"end":10,"data":""" +
            FloatData(13) + "}]}]}";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(BakeSlow(json)));
    }

    [Fact]
    public void MoreFloatsThanFlushWindow_FlushesMidObject_AndBakesIdenticallyToOracle()
    {
        var json =
            """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaFatTrack","data":{"Scale":2},"clips":[{"namespace":"FusedBake","type":"GaFatClip","start":0,"end":10,"data":""" +
            FloatData(40) + "}]}]}";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(BakeSlow(json)));
    }

    public static IEnumerable<object[]> WrongShapedClipDataCases()
    {
        yield return ["number", "5", "got Number."];
        yield return ["string", "\"s\"", "got String."];
        yield return ["true", "true", "got True."];
        yield return ["false", "false", "got False."];
        yield return ["null", "null", "got Null."];
        yield return ["array", "[]", "got Array."];
    }

    [Theory]
    [MemberData(nameof(WrongShapedClipDataCases))]
    public void WrongShapedInlineClipData_ReportsTheTokenKind(string name, string data, string expectedSuffix)
    {
        _ = name;
        var json =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":10,"data":""" +
            data + "}]}]}";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("expected object, got", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(expectedSuffix, diagnostic.Message, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> ScalarWrongTypeCases()
    {
        yield return ["sbyte overflow", "{\"Sb\":1000}", "expected sbyte"];
        yield return ["short overflow", "{\"Sh\":70000}", "expected short"];
        yield return ["ushort negative", "{\"Us\":-1}", "expected ushort"];
        yield return ["long fraction", "{\"L\":1.5}", "expected long"];
        yield return ["ulong negative", "{\"Ul\":-1}", "expected ulong"];
        yield return ["double string", "{\"D\":\"x\"}", "expected double"];
    }

    [Theory]
    [MemberData(nameof(ScalarWrongTypeCases))]
    public void WrongTypedScalarFields_ReportsTheExpectedType(string name, string data, string expectedFragment)
    {
        _ = name;
        var json =
            """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaPrimTrack","clips":[{"namespace":"FusedBake","type":"GaPrimClip","start":0,"end":10,"data":""" +
            data + "}]}]}";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("wrong-typed value", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(expectedFragment, diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFieldWithNestedContainers_IsSkippedWithDuplicateScanning_AndReported()
    {
        const string json =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":10,"data":{"zzz":[{"a":[1,{"b":2}]},[2]],"nn":{"in":{"deep":1}},"Value":5}}]}]}""";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("unknown field", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("zzz", diagnostic.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":1,"C\u006Fde":2},"clips":[]}]}""")]
    [InlineData("""{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"C\u006Fde":1,"C\u006Fde":2},"clips":[]}]}""")]
    public void EscapedDuplicateFieldNames_AreRejected(string json)
    {
        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("duplicate field", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapedWellKnownPropertyNames_DecodeAndBakeIdenticallyToOracle()
    {
        const string json =
            """{"\u0064uration":20,"\u0074racks":[{"\u006Eamespace":"Tlb","\u0074ype":"AlphaTrack","clips":[{"\u006Eamespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"\u0056alue":4}}]}]}""";

        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(BakeSlow(json)));
    }

    [Fact]
    public void DeferredPopulation_UnknownNestedField_IsSkippedWithoutScopes_AndReported()
    {
        const string json =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"data":{"zzz":{"a":1}},"namespace":"Tlb","type":"AlphaClip","start":0,"end":10}]}]}""";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("unknown field", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeferredPopulation_EscapedFieldName_DecodesAndBakesIdenticallyToOracle()
    {
        const string json =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"data":{"\u0056alue":7},"namespace":"Tlb","type":"AlphaClip","start":0,"end":10}]}]}""";

        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(BakeSlow(json)));
    }

    [Theory]
    [InlineData("""[{"a":1}]""")]
    [InlineData("42")]
    public void NonObjectRoot_ReportsRootShapeDiagnostic(string json)
    {
        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("Root of timeline document must be a JSON object.", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyDocument_FailsAtTheReader()
    {
        Assert.ThrowsAny<JsonException>(() => TimelineBaker.BakeJson([], Resolver));
    }

    [Fact]
    public void TrackTypeBeforeNamespace_RefreshesResolve_AndBakesIdenticallyToOracle()
    {
        const string json =
            """{"duration":20,"tracks":[{"type":"AlphaTrack","namespace":"Tlb","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":2}}]}]}""";

        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void ClipTypeBeforeNamespace_RefreshesPair_AndBakesIdenticallyToOracle()
    {
        const string json =
            """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"type":"AlphaClip","namespace":"Tlb","start":0,"end":20,"data":{"Value":8}}]}]}""";

        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void TrackNamespaceNotAString_ReportsLocatedDiagnostic()
    {
        const string json = """{"duration":10,"tracks":[{"namespace":5,"type":"AlphaTrack","clips":[]}]}""";
        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => BakeSlow(json));
        Assert.Contains("'namespace' must be a string", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TrackAndClipMetadata_MustBeStrings_AndValidNamesBakeIdenticallyToOracle()
    {
        const string trackBad = """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","assembly":5,"clips":[]}]}""";
        Assert.Contains("'assembly' must be a string", Assert
            .Throws<BakeDiagnosticException>(() => BakeSlow(trackBad)).Message, StringComparison.Ordinal);

        const string clipBad =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","name":"s","start":0,"end":"x"}]}]}""";
        Assert.Contains("'end' must be", Assert
            .Throws<BakeDiagnosticException>(() => BakeSlow(clipBad)).Message, StringComparison.Ordinal);

        const string clipAssemblyBad =
            """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","assembly":7,"start":0,"end":10}]}]}""";
        Assert.Contains("'assembly' must be a string", Assert
            .Throws<BakeDiagnosticException>(() => BakeSlow(clipAssemblyBad)).Message, StringComparison.Ordinal);

        const string valid =
            """{"name":"root","duration":20,"tracks":[{"name":"lane","namespace":"Tlb","type":"AlphaTrack","clips":[{"name":"s1","namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":9}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json: valid, Resolver)),
            Receipt(BakeSlow(valid)));
    }

    [Fact]
    public void ManagedTrackTypeWithData_IsRejectedAsUnmanaged()
    {
        const string json =
            """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaClassTrack","data":{},"clips":[{"namespace":"FusedBake","type":"GaClip0","start":0,"end":10,"data":{"Code":1}}]}]}""";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver));
        Assert.Contains("unmanaged", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagedTrackTypeWithBadData_ReportsTheLocatedPopulateDiagnostic()
    {
        const string json =
            """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaClassTrack","data":{"ghost":1},"clips":[{"namespace":"FusedBake","type":"GaClip0","start":0,"end":10,"data":{"Code":1}}]}]}""";

        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver));
        Assert.Contains("unknown field", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("ghost", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StringOverloadOfFastBake_MatchesThePublicBake()
    {
        const string json =
            """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":4}}]}]}""";

        Assert.Equal(
            Receipt(TimelineBaker.BakeJson(json, Resolver)),
            Receipt(TimelineBakerFast.BakeJson(json, Resolver)));
    }
}
