using System.Security.Cryptography;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class SimdScanTests
{
    private const string G = "FusedBake";

    private static string Doc(string rootProps, string tracks) => "{" + rootProps + ",\"tracks\":[" + tracks + "]}";

    private static string ClipJ(string type, string data, string extra = "", bool withRange = true, string ns = G)
    {
        var sb = new StringBuilder();
        sb.Append("{\"namespace\":\"").Append(ns).Append("\",\"type\":\"").Append(type).Append('"');
        foreach (var part in extra.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var sep = part.IndexOf(':');
            sb.Append(",\"").Append(part[..sep]).Append("\":").Append(part[(sep + 1)..]);
        }
        if (withRange)
            sb.Append(",\"start\":1,\"end\":9");
        if (data.Length > 0)
            sb.Append(",\"data\":").Append(data);
        sb.Append('}');
        return sb.ToString();
    }

    private static string TrackJ(string type, string clips, string extra = "", string ns = G)
    {
        var sb = new StringBuilder();
        sb.Append("{\"namespace\":\"").Append(ns).Append("\",\"type\":\"").Append(type).Append('"');
        foreach (var part in extra.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var sep = part.IndexOf(':');
            sb.Append(",\"").Append(part[..sep]).Append("\":").Append(part[(sep + 1)..]);
        }
        sb.Append(",\"clips\":[").Append(clips).Append("]}");
        return sb.ToString();
    }

    private static string Data(int count, string prefix = "f", string value = "1.5")
    {
        var sb = new StringBuilder();
        sb.Append('{');
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append($"\"{prefix}{i}\":{value}");
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    private static bool TakesFastPath(string json) =>
        TimelineBakerSimd.TryParseFast(Utf8(json), new BakerAssemblyResolver(), out _);

    private static string Bake(string json) => Convert.ToHexString(SHA256.HashData(TimelineBaker.BakeJson(json)));

    public static IEnumerable<object[]> InvalidCases() =>
        FusedParityTests.CaseList()
            .Where(c => IsRejected(c.Json))
            .Select(c => new object[] { c.Name, c.Json });

    private static bool IsRejected(string json)
    {
        try
        {
            TimelineBaker.BakeJsonLegacy(json);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void RejectedDocumentsProduceIdenticalReceipts(string name, string json)
    {
        _ = name;
        var legacy = ReceiptOf(() => TimelineBaker.BakeJsonLegacy(json));
        var wired = ReceiptOf(() => TimelineBaker.BakeJson(json));
        Assert.Equal(legacy, wired);
        if (TimelineBakerSimd.TryParseFast(Utf8(json), new BakerAssemblyResolver(), out var doc))
        {
            var fast = ReceiptOf(() => TimelineBakerFastCore.BakeFast(doc, new BakerAssemblyResolver()));
            Assert.Equal(legacy, fast);
        }
    }

    private static string ReceiptOf(Func<byte[]> bake)
    {
        try
        {
            return "OK|" + Convert.ToHexString(SHA256.HashData(bake()));
        }
        catch (Exception ex)
        {
            return "RE|" + ex.GetType().Name + "|" + ex.Message;
        }
    }

    [Fact]
    public void CanonicalCorpusShapeTakesFastPathAndBakesIdentically()
    {
        var data = Data(60, "f", "-123.456");
        var json = Doc("\"name\":\"root\",\"duration\":100,\"loop\":true",
            TrackJ("GaFatTrack", ClipJ("GaFatClip", data, "start:1;end:50", withRange: false) + "," + ClipJ("GaFatClip", Data(4), "start:60;end:99", withRange: false)));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void WhitespaceBetweenTokensTakesFastPath()
    {
        var json = "\r\n  {  \"name\" :\t\"root\" , \"duration\" : 10 ,\n \"loop\" : false , \"tracks\" : [ { \"namespace\" : \"FusedBake\" , \"type\" : \"GaTrack0\" , \"clips\" : [ { \"namespace\":\"FusedBake\",\"type\":\"GaClip0\",\"start\":1,\"end\":9,\"data\":{ \"f0\" : 1.5 , \"f1\" : -0.25 } } ] } ] }  \n";
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void StructuralCharactersInsideStringsStayInStrings()
    {
        var json = Doc("\"name\":\"a,b:c[d]e{f}g\",\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "name:\"x,y\"")));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void StringsSpanningManyBlocksTakesFastPath()
    {
        var longName = new string('x', 500) + ",:" + new string('y', 500);
        var json = Doc($"\"name\":\"{longName}\",\"duration\":10,\"loop\":false", "");
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void MultibyteUtf8InStringsTakesFastPath()
    {
        var json = Doc("\"name\":" + "\"\u8f68\u9053-\u00e9-\U0001F600\",\"duration\":10,\"loop\":false",
            TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "name:\"\u30d1\u30c6\u30a3\u30af\u30eb\"")));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void NumberShapesOnTheFastPathBakeIdentically()
    {
        var data = "{\"f0\":0,\"f1\":-0.0,\"f2\":0.1,\"f3\":1999.999,\"f4\":1e3,\"f5\":-2.5e-2,\"f6\":1234.5678,\"f7\":199,\"f8\":7.0}";
        var json = Doc("\"duration\":10,\"loop\":false", TrackJ("GaFatTrack", ClipJ("GaFatClip", data)));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void IntegerFieldShapesOnTheFastPathBakeIdentically()
    {
        var data = "{\"B\":true,\"Bt\":255,\"Sb\":-128,\"Sh\":-32768,\"Us\":65535,\"I\":2147483647,\"Ui\":4294967295,\"L\":9223372036854775807,\"Ul\":18446744073709551615,\"F\":1.5,\"D\":2.5}";
        var json = Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", data)));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void EmptyDataObjectTakesFastPath()
    {
        var json = Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{}")));
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void TrackDataBeforeClipsTakesFastPath()
    {
        var json = Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"data\":{\"Scale\":2.5,\"Code\":7},\"clips\":[" + ClipJ("GaClip0", Data(2)) + "]}");
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void MinimumDocumentBakesThroughFastPath()
    {
        var json = "{\"duration\":0,\"loop\":false,\"tracks\":[]}";
        Assert.True(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void HardFloatsFallBackAndBakeIdentically()
    {
        var data = "{\"f0\":3.4028235e38,\"f1\":1.4e-45}";
        var json = Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", data)));
        Assert.False(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void AssemblyPropertyFallsBackAndBakesIdentically()
    {
        var json = Doc("\"duration\":10,\"loop\":false",
            TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "assembly:\"" + typeof(SimdScanTests).Assembly.GetName().Name + "\"")));
        Assert.False(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void DataBeforeTypeFallsBackAndBakesIdentically()
    {
        var json = Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":[{\"data\":{\"f0\":7.5},\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":2,\"end\":8}]}");
        Assert.False(TakesFastPath(json));
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }
}
