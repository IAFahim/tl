using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tl.Bake.Oracle;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class SimdWalkerCoverageTests
{
    private static readonly BakerAssemblyResolver Resolver =
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    private const string AlphaTrackJson =
        "{\"namespace\":\"Tlb\",\"type\":\"AlphaTrack\",\"data\":{\"Code\":1},\"clips\":[" +
        "{\"namespace\":\"Tlb\",\"type\":\"AlphaClip\",\"start\":0,\"end\":20,\"data\":{\"Value\":5}}]}";

    private const string BlendTrackJson =
        "{\"namespace\":\"Tlb\",\"type\":\"BlendTrack\",\"data\":{\"Scale\":1.5},\"clips\":[" +
        "{\"namespace\":\"Tlb\",\"type\":\"BlendClip\",\"start\":0,\"end\":20,\"data\":{\"Amount\":2.5}}]}";

    private const string ValidClipJson =
        """{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"data":{"Value":3}}""";

    private static string PaddedTracksDoc(int trackCount, int padBytesPerTrack, string join, string head = "")
    {
        var builder = new StringBuilder("{\"duration\":40,\"loop\":false,\"tracks\":[");
        builder.Append(head);
        var pad = new string(' ', padBytesPerTrack);
        for (var i = 0; i < trackCount; i++)
        {
            if (i > 0)
                builder.Append(join);
            builder.Append(pad);
            builder.Append(i % 2 == 0 ? AlphaTrackJson : BlendTrackJson);
        }
        builder.Append(pad);
        builder.Append("]}");
        return builder.ToString();
    }

    private static byte[] BakeWithPartitions(string json, int degree)
    {
        Environment.SetEnvironmentVariable("TL_BAKE_PARTITIONS", degree.ToString());
        try
        {
            return TimelineBaker.BakeJson(json, Resolver);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TL_BAKE_PARTITIONS", null);
        }
    }

    private static string Receipt(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static bool Avx2Available => Avx2.IsSupported;

    [Fact]
    public void LargeTrackArray_PartitionsAcrossSlices_AndBakesIdentically()
    {
        if (!Avx2Available)
            return;
        var json = PaddedTracksDoc(12, 90 * 1024, ",");
        Assert.True(Encoding.UTF8.GetByteCount(json) > (1 << 20) + 1024);

        var partitioned = BakeWithPartitions(json, 3);
        var sequential = TimelineBaker.BakeJson(json, Resolver);
        var legacy = BakeOracle.BakeJsonLegacy(json, Resolver);

        Assert.Equal(sequential, partitioned);
        Assert.Equal(Receipt(legacy), Receipt(partitioned));
    }

    [Fact]
    public void Partitioned_MissingCommaBetweenTracks_FailsFast()
    {
        if (!Avx2Available)
            return;
        var json = PaddedTracksDoc(10, 110 * 1024, "");
        Assert.ThrowsAny<JsonException>(() => BakeWithPartitions(json, 2));
    }

    [Fact]
    public void Partitioned_LeadingComma_FailsFast()
    {
        if (!Avx2Available)
            return;
        var json = PaddedTracksDoc(10, 110 * 1024, ",", head: " , ");
        Assert.ThrowsAny<JsonException>(() => BakeWithPartitions(json, 2));
    }

    [Fact]
    public void Partitioned_StrayColonAfterLastTrack_FailsFast()
    {
        if (!Avx2Available)
            return;
        var json = PaddedTracksDoc(10, 110 * 1024, ",");
        var colonBetweenLastTrackAndClose = json.Insert(json.Length - 3, " : ");
        Assert.ThrowsAny<JsonException>(() => BakeWithPartitions(colonBetweenLastTrackAndClose, 2));
    }

    [Fact]
    public void UnclosedTracksArray_TrackElementsExhausts_FailsFast()
    {
        if (!Avx2Available)
            return;
        var builder = new StringBuilder("{\"duration\":40,\"loop\":false,\"tracks\":[");
        var pad = new string(' ', 110 * 1024);
        for (var i = 0; i < 9; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append(pad).Append(i % 2 == 0 ? AlphaTrackJson : BlendTrackJson);
        }
        Assert.ThrowsAny<JsonException>(() => BakeWithPartitions(builder.ToString(), 2));
    }

    public static IEnumerable<object[]> DiagnosticCases()
    {
        yield return Case("root duplicate tracks", """{"duration":10,"tracks":[],"tracks":[]}""");
        yield return Case("root duplicate name", """{"name":"a","name":"b","duration":10,"tracks":[]}""");
        yield return Case("root duplicate loop", """{"duration":10,"loop":true,"loop":false,"tracks":[]}""");
        yield return Case("tracks element not an object", """{"duration":10,"tracks":[[]]}""");
        yield return Case("track duplicate namespace", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","namespace":"Tlb","clips":[]}]}""");
        yield return Case("track namespace not a string", """{"duration":10,"tracks":[{"namespace":5,"type":"AlphaTrack","clips":[]}]}""");
        yield return Case("track type not a string", """{"duration":10,"tracks":[{"namespace":"Tlb","type":5,"clips":[]}]}""");
        yield return Case("track duplicate name", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","name":"a","name":"b","clips":[]}]}""");
        yield return Case("track duplicate clips", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[],"clips":[]}]}""");
        yield return Case("track duplicate data", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{},"data":{},"clips":[]}]}""");
        yield return Case("track data not an object", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":5,"clips":[]}]}""");
        yield return Case("track data string value", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":"x"},"clips":[]}]}""");
        yield return Case("track data array value", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":[1]},"clips":[]}]}""");
        yield return Case("track data true literal", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":true},"clips":[]}]}""");
        yield return Case("track data null literal", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":null},"clips":[]}]}""");
        yield return Case("clip type not blendable with track", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"BlendClip","start":0,"end":9}]}]}""");
        yield return Case("clips element not an object", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[[]]}]}""");
        yield return Case("empty clip object", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{}]}]}""");
        yield return Case("clip duplicate namespace", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","namespace":"Tlb","type":"AlphaClip","start":0,"end":9}]}]}""");
        yield return Case("clip type not a string", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":5,"start":0,"end":9}]}]}""");
        yield return Case("clip duplicate name", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","name":"a","name":"b","start":0,"end":9}]}]}""");
        yield return Case("clip name not a string", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","name":5,"start":0,"end":9}]}]}""");
        yield return Case("clip duplicate end", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"end":9}]}]}""");
        yield return Case("clip end not scalar", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":[]}]}]}""");
        yield return Case("clip end out of range", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":-1}]}]}""");
        yield return Case("clip duplicate data", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"data":{"Value":1},"data":{"Value":2}}]}]}""");
        yield return Case("clip data ulong negative", """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaPrimTrack","clips":[{"namespace":"FusedBake","type":"GaPrimClip","start":0,"end":9,"data":{"Ul":-1}}]}]}""");
    }

    private static object[] Case(string name, string json) => [name, json];

    [Theory]
    [MemberData(nameof(DiagnosticCases))]
    public void MisuseDocuments_FailFastWithLocatedDiagnostic(string name, string json)
    {
        _ = name;
        var diagnostic = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }

    public static IEnumerable<object[]> GrammarCases()
    {
        yield return Case("root member missing colon", """{"duration" 10,"tracks":[]}""");
        yield return Case("loop literal wrong token", """{"duration":10,"loop":truex,"tracks":[]}""");
        yield return Case("track object colon first", """{"duration":10,"tracks":[{:1}]}""");
        yield return Case("track trailing comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[],}]}""");
        yield return Case("track separator not comma", """{"duration":10,"tracks":[{"namespace":"Tlb" :,"type":"AlphaTrack","clips":[]}]}""");
        yield return Case("track data invalid literal", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":tru},"clips":[]}]}""");
        yield return Case("clip data colon first", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"data":{:1}}]}]}""");
        yield return Case("clip data trailing comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"data":{"Value":1,}}]}]}""");
        yield return Case("clip data separator not comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,"data":{"Value":1:}}]}]}""");
        yield return Case("loop literal trailing junk", """{"duration":10,"loop":true x,"tracks":[]}""");
        yield return Case("tracks separator not comma", """{"duration":10,"tracks":[""" + AlphaTrackJson + " :]}");
        yield return Case("clips trailing separator", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[""" + ValidClipJson + ",]}]}");
        yield return Case("clips separator not comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[""" + ValidClipJson + " :]}]}");
        yield return Case("clip trailing comma inside object", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":9,}]}]}""");
        yield return Case("clip separator not comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb" :,"type":"AlphaClip","start":0,"end":9}]}]}""");
        yield return Case("track data missing colon", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code" 1},"clips":[]}]}""");
        yield return Case("track data separator not comma", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":1:},"clips":[]}]}""");
        yield return Case("track data trailing comma member", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":1,},"clips":[]}]}""");
        yield return Case("track data colon first", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{:1},"clips":[]}]}""");
        yield return Case("track data leading zero", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":01},"clips":[]}]}""");
        yield return Case("track data bare word", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":x},"clips":[]}]}""");
        yield return Case("track data number trailing junk", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":1 x},"clips":[]}]}""");
        yield return Case("track data missing fraction", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"BlendTrack","data":{"Scale":1.},"clips":[]}]}""");
        yield return Case("track data bare exponent", """{"duration":10,"tracks":[{"namespace":"Tlb","type":"BlendTrack","data":{"Scale":1e},"clips":[]}]}""");
        yield return Case("clip data leading zero", """{"duration":10,"tracks":[{"namespace":"FusedBake","type":"GaPrimTrack","clips":[{"namespace":"FusedBake","type":"GaPrimClip","start":0,"end":9,"data":{"D":01}}]}]}""");
    }

    [Theory]
    [MemberData(nameof(GrammarCases))]
    public void GrammarBrokenDocuments_FailFastAtTheReader(string name, string json)
    {
        _ = name;
        Assert.ThrowsAny<JsonException>(() => TimelineBaker.BakeJson(json, Resolver));
    }

    [Fact]
    public void TrackDataBeforeType_BakesIdenticallyToOracle()
    {
        var json =
            """{"duration":20,"tracks":[{"namespace":"Tlb","data":{"Code":3},"type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":4}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void ClipDataBeforeIdentity_DefersPopulation_AndBakesIdenticallyToOracle()
    {
        var json =
            """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"data":{"Value":6},"namespace":"Tlb","type":"AlphaClip","start":0,"end":20}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void EmptyTrackDataRegion_BakesIdenticallyToOracle()
    {
        var json = """{"duration":10,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","data":{},"clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":10,"data":{"Value":2}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void TrackDataNegativeAndExponentNumbers_BakeIdenticallyToOracle()
    {
        var json =
            """{"duration":40,"loop":false,"tracks":[""" +
            """{"namespace":"Tlb","type":"AlphaTrack","data":{"Code":-7},"clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":-3}}]},""" +
            """{"namespace":"Tlb","type":"BlendTrack","data":{"Scale":-2.5e2},"clips":[{"namespace":"Tlb","type":"BlendClip","start":0,"end":20,"data":{"Amount":1.5e+2}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void NamedClipsAndTracks_BakeIdenticallyToOracle()
    {
        var json =
            """{"name":"root","duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","name":"lane","clips":[{"namespace":"Tlb","type":"AlphaClip","name":"s1","start":0,"end":20,"data":{"Value":9}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void TrackDataSignedExponentNumbers_BakeIdenticallyToOracle()
    {
        var json =
            """{"duration":40,"loop":false,"tracks":[{"namespace":"Tlb","type":"BlendTrack","data":{"Scale":-1.5e+2},"clips":[{"namespace":"Tlb","type":"BlendClip","start":0,"end":20,"data":{"Amount":7e-1}}]}]}""";
        Assert.Equal(
            Receipt(BakeOracle.BakeJsonLegacy(json, Resolver)),
            Receipt(TimelineBaker.BakeJson(json, Resolver)));
    }

    [Fact]
    public void RawControlCharacterInString_FailsTheScan()
    {
        var json = "{\"duration\":10,\"name\":\"a\u0001b\",\"tracks\":[]}";
        Assert.ThrowsAny<JsonException>(() => TimelineBaker.BakeJson(json, Resolver));
    }

    [Fact]
    public void Scan_ReturnsStructuralIndexOrNull()
    {
        var valid = TimelineBakerSimd.Scan(Encoding.UTF8.GetBytes("""{"duration":1,"tracks":[]}"""));
        Assert.NotNull(valid);
        Assert.True(valid.Blocks > 0);

        Assert.Null(TimelineBakerSimd.Scan(Encoding.UTF8.GetBytes("""{"a":"b\\c"}""")));
        Assert.Null(TimelineBakerSimd.Scan([]));
    }
}
