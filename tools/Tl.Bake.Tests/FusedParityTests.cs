using System.Security.Cryptography;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class FusedParityTests
{
    private const string G = "FusedBake";

    public static IEnumerable<object[]> Cases() => CaseList().Select(c => new object[] { c.Name, c.Json });

    [Theory]
    [MemberData(nameof(Cases))]
    public void FusedPathMatchesLegacyOracle(string name, string json)
    {
        var oldResult = Run(json, j => TimelineBaker.BakeJsonLegacy(j));
        var newResult = Run(json, j => TimelineBaker.BakeJson(j));
        Assert.True(oldResult == newResult,
            $"""
             {name}: fused path diverged from the legacy oracle.
             json: {Compact(json)}
             old:  {oldResult}
             new:  {newResult}
             """);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FusedBytePathMatchesLegacyOracle(string name, string json)
    {
        var oldResult = Run(json, j => TimelineBaker.BakeJsonLegacy(j));
        var newResult = Run(json, j => TimelineBaker.BakeJson(Encoding.UTF8.GetBytes(j)));
        Assert.True(oldResult == newResult,
            $"""
             {name}: fused byte path diverged from the legacy oracle.
             json: {Compact(json)}
             old:  {oldResult}
             new:  {newResult}
             """);
    }

    [Fact]
    public void ByteEntryRejectsInvalidUtf8WithDiagnostic()
    {
        ReadOnlySpan<byte> prefix = "{\"duration\":10,\"loop\":false,\"tracks\":[],\"name\":\"bad "u8;
        ReadOnlySpan<byte> suffix = " byte\"}"u8;
        var bytes = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(bytes);
        bytes[prefix.Length] = 0x80;
        suffix.CopyTo(bytes.AsSpan(prefix.Length + 1));

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(bytes));
        Assert.Contains($"invalid UTF-8 in authoring JSON at byte {prefix.Length}", ex.Message, StringComparison.Ordinal);
        Assert.Contains("must be valid UTF-8", ex.Message, StringComparison.Ordinal);

        var laundered = TimelineBaker.BakeJson(Encoding.UTF8.GetString(bytes));
        var replacement = TimelineBaker.BakeJson("{\"duration\":10,\"loop\":false,\"tracks\":[],\"name\":\"bad \uFFFD byte\"}"u8.ToArray());
        Assert.Equal(Convert.ToHexString(laundered), Convert.ToHexString(replacement));
    }

    [Fact]
    public void FatClipBakesByteIdentical()
    {
        var data = string.Join(",", Enumerable.Range(0, 64).Select(i => $"\"f{i}\":1.5"));
        var json = $"{{\"duration\":10,\"loop\":false,\"tracks\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatTrack\",\"clips\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatClip\",\"start\":1,\"end\":9,\"data\":{{{data}}}}}]}}]}}";
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void FatClipSpansApplierFlush()
    {
        var data = string.Join(",", Enumerable.Range(0, 64).Select(i => $"\"f{i}\":{i + 1}.25"));
        var json = $"{{\"duration\":10,\"loop\":false,\"tracks\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatTrack\",\"clips\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatClip\",\"start\":1,\"end\":9,\"data\":{{{data}}}}}]}}]}}";
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void FatClipTwoOverlapByteIdentical()
    {
        var data = string.Join(",", Enumerable.Range(0, 64).Select(i => $"\"f{i}\":2.5"));
        var json = $"{{\"duration\":100,\"loop\":false,\"tracks\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatTrack\",\"clips\":[{{\"namespace\":\"{G}\",\"type\":\"GaFatClip\",\"start\":10,\"end\":30,\"data\":{{{data}}}}},{{\"namespace\":\"{G}\",\"type\":\"GaFatClip\",\"start\":20,\"end\":40,\"data\":{{{data}}}}}]}}]}}";
        Assert.Equal(TimelineBaker.BakeJsonLegacy(json), TimelineBaker.BakeJson(json));
    }

    private static string Run(string json, Func<string, byte[]> bake)
    {
        try
        {
            return "OK|" + Convert.ToHexString(SHA256.HashData(bake(json)));
        }
        catch (Exception ex)
        {
            return "RE|" + ex.GetType().Name + "|" + ex.Message;
        }
    }

    private static string Compact(string json) => json.Length > 400 ? json[..400] + "..." : json;

    private static string Data(int count, string prefix = "f", string value = "1.5")
    {
        var sb = new StringBuilder();
        sb.Append('{');
        for (var i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"\"{prefix}{i}\":{value}");
        }
        sb.Append('}');
        return sb.ToString();
    }

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

    private static string Doc(string rootProps, string tracks) => "{" + rootProps + ",\"tracks\":[" + tracks + "]}";

    private static IEnumerable<(string Name, string Json)> CaseList()
    {
        var clip0 = ClipJ("GaClip0", Data(4));
        var track0 = TrackJ("GaTrack0", clip0);

        yield return ("valid minimal", Doc("\"duration\":0,\"loop\":false", ""));
        yield return ("valid basic", Doc("\"name\":\"root\",\"duration\":10,\"loop\":true", track0));
        yield return ("valid empty track duration 0", Doc("\"duration\":0,\"loop\":false", TrackJ("GaTrack0", "")));
        yield return ("valid escaped root name", Doc("\"na\\u006De\":\"escaped\",\"duration\":10,\"loop\":false", ""));
        yield return ("valid escaped ns/type", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"\\u0046usedBake\",\"type\":\"GaClip0\",\"clips\":[]}"));
        yield return ("valid escaped dup name only", Doc("\"na\\u006De\":\"escaped\",\"duration\":10,\"loop\":false", track0));
        yield return ("valid unicode label", Doc("\"name\":\"\u8f68\u9053-\\u00e9\",\"duration\":10,\"loop\":false", track0));
        yield return ("valid max duration", Doc("\"duration\":65500,\"loop\":true", track0));
        yield return ("valid unsorted clips", Doc("\"duration\":100,\"loop\":false", TrackJ("GaTrack0",
            ClipJ("GaClip0", Data(1), "start:50;end:60") + "," + ClipJ("GaClip0", Data(1), "start:5;end:20"))));
        yield return ("valid two overlap", Doc("\"duration\":100,\"loop\":false", TrackJ("GaTrack0",
            ClipJ("GaClip0", Data(1), "start:10;end:30") + "," + ClipJ("GaClip0", Data(1), "start:20;end:40"))));
        yield return ("valid assembly hint", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(2), "assembly:GameAsm"))));
        yield return ("valid two pairs", Doc("\"duration\":10,\"loop\":false", track0 + "," + TrackJ("GaTrack1", ClipJ("GaClip1", Data(3)))));
        yield return ("valid primitive fields", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack",
            ClipJ("GaPrimClip", "{\"B\":true,\"Bt\":255,\"Sb\":-128,\"Sh\":-32768,\"Us\":65535,\"I\":2147483647,\"Ui\":4294967295,\"L\":9223372036854775807,\"Ul\":18446744073709551615,\"F\":3.4028235e38,\"D\":1.7976931348623157e308}"))));
        yield return ("valid float extremes", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0",
            ClipJ("GaClip0", "{\"f0\":3.4028235e38,\"f1\":-3.4028235e38,\"f2\":1.4e-45,\"f3\":-0.0,\"f4\":0.1}"))));
        yield return ("valid double extremes", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack",
            ClipJ("GaPrimClip", "{\"B\":false,\"Bt\":0,\"Sb\":0,\"Sh\":0,\"Us\":0,\"I\":0,\"Ui\":0,\"L\":0,\"Ul\":0,\"F\":0,\"D\":1e-300}"))));
        yield return ("valid data prop first in clip", Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":[{\"data\":{\"f0\":7.5},\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":2,\"end\":8}]}"));
        yield return ("valid clips before type in track", Doc("\"duration\":10,\"loop\":false",
            "{\"clips\":[{\"data\":{\"f0\":7.5},\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":2,\"end\":8}],\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\"}"));
        yield return ("valid assembly after clips in track", Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":[{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":1,\"end\":9,\"data\":{\"f0\":1.25}}],\"assembly\":\"" + AssemblyName + "\"}"));
        yield return ("valid clip assembly after data", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0",
            "{\"namespace\":\""+G+"\",\"type\":\"GaClip0\",\"start\":1,\"end\":9,\"data\":{" + Data(2)[1..^1] + "},\"assembly\":\"" + AssemblyName + "\"}")));
        yield return ("valid wrong track assembly rejected equally", Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"assembly\":\"NotThisAssembly\",\"clips\":[{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":1,\"end\":9,\"data\":{\"f0\":1.25}}]}"));
        yield return ("valid global namespace", Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"\",\"type\":\"GaGlobalTrack\",\"clips\":[{\"namespace\":\"\",\"type\":\"GaGlobalClip\",\"start\":1,\"end\":9,\"data\":{\"Amount\":2.5}}]}"));
        yield return ("valid padded doc", "\n\n  " + Doc("\"duration\":10,\"loop\":false", "") + "  \n");
        yield return ("valid track data after clips", Doc("\"duration\":10,\"loop\":false",
            "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":[{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":1,\"end\":9,\"data\":{\"f0\":1.25}}],\"data\":{\"Scale\":2.0,\"Code\":1}}"));

        yield return ("reject root array", "[]");
        yield return ("reject root string", "\"x\"");
        yield return ("reject root number", "5");
        yield return ("reject root true", "true");
        yield return ("reject root null", "null");
        yield return ("reject empty", "");
        yield return ("reject whitespace", "   ");
        yield return ("reject missing everything", "{}");
        yield return ("reject missing tracks", "{\"duration\":10}");
        yield return ("reject missing duration", "{\"loop\":false,\"tracks\":[]}");
        yield return ("reject duration string", Doc("\"duration\":\"12\",\"loop\":false", ""));
        yield return ("reject duration float", Doc("\"duration\":1.5,\"loop\":false", ""));
        yield return ("reject duration negative", Doc("\"duration\":-3,\"loop\":false", ""));
        yield return ("reject duration bool", Doc("\"duration\":true,\"loop\":false", ""));
        yield return ("reject duration null", Doc("\"duration\":null,\"loop\":false", ""));
        yield return ("reject duration object", Doc("\"duration\":{},\"loop\":false", ""));
        yield return ("reject duration 65536", Doc("\"duration\":65536,\"loop\":false", ""));
        yield return ("reject duration 4294967296", Doc("\"duration\":4294967296,\"loop\":false", ""));
        yield return ("reject loop number", Doc("\"loop\":1,\"duration\":1", ""));
        yield return ("reject loop string", Doc("\"loop\":\"true\",\"duration\":1", ""));
        yield return ("reject tracks object", "{\"tracks\":{},\"duration\":1}");
        yield return ("reject name number", Doc("\"name\":5,\"duration\":1", ""));
        yield return ("reject name object", Doc("\"name\":{\"a\":1},\"duration\":1", ""));
        yield return ("reject root removed trackType", Doc("\"trackType\":\"X\",\"duration\":1", ""));
        yield return ("reject root removed clipType", Doc("\"clipType\":\"X\",\"duration\":1", ""));
        yield return ("reject root removed track", Doc("\"track\":\"X\",\"duration\":1", ""));
        yield return ("reject root removed payload", Doc("\"payload\":\"X\",\"duration\":1", ""));
        yield return ("reject root renamed loops", Doc("\"loops\":true,\"duration\":1", ""));
        yield return ("reject root renamed looping", Doc("\"looping\":true,\"duration\":1", ""));
        yield return ("reject root unknown", Doc("\"xyz\":1,\"duration\":1", ""));

        yield return ("reject track non-object", Doc("\"duration\":10,\"loop\":false", "5"));
        yield return ("reject track missing namespace", Doc("\"duration\":10,\"loop\":false", "{\"type\":\"GaTrack0\",\"clips\":[]}"));
        yield return ("reject track missing type", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"" + G + "\",\"clips\":[]}"));
        yield return ("reject track missing clips", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\"}"));
        yield return ("reject track clips object", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":{}}"));
        yield return ("reject track name number", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "", "name:5")));
        yield return ("reject track assembly number", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "", "assembly:5")));
        yield return ("reject track unknown prop", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "", "bogus:1")));
        yield return ("reject track removed prop", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "", "trackType:X")));
        yield return ("reject track renamed prop", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "", "loops:true")));
        yield return ("reject track missing ns with dup beats", Doc("\"duration\":10,\"loop\":false", "{\"type\":\"GaTrack0\",\"type\":\"GaTrack0\",\"clips\":[]}"));

        yield return ("reject clip non-object", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "5")));
        yield return ("reject clip missing namespace", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"type\":\"GaClip0\",\"start\":1,\"end\":9}")));
        yield return ("reject clip missing type", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"namespace\":\"" + G + "\",\"start\":1,\"end\":9}")));
        yield return ("reject clip missing start", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "end:9", withRange: false))));
        yield return ("reject clip missing end", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "start:1", withRange: false))));
        yield return ("reject clip start string", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":\"1\",\"end\":9}")));
        yield return ("reject clip start negative", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":-1,\"end\":9}")));
        yield return ("reject clip start float", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":1.5,\"end\":9}")));
        yield return ("reject clip unknown prop", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "bogus:1"))));
        yield return ("reject clip start equal end", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "", "start:5;end:5", withRange: false))));
        yield return ("reject clip start over end", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "", "start:9;end:5", withRange: false))));
        yield return ("reject clip beyond duration", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "", "start:5;end:50", withRange: false))));
        yield return ("reject empty track duration > 0", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "")));
        yield return ("reject too many tracks", Doc("\"duration\":0,\"loop\":false", string.Join(",", Enumerable.Repeat(TrackJ("GaTrack0", ""), 257))));

        yield return ("reject dup root", Doc("\"duration\":1,\"duration\":2", ""));
        yield return ("reject dup track type", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"type\":\"GaTrack0\",\"clips\":[]}"));
        yield return ("reject dup clip start", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"namespace\":\"" + G + "\",\"type\":\"GaClip0\",\"start\":1,\"start\":2,\"end\":9}")));
        yield return ("reject dup data field", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":1,\"f0\":2}"))));
        yield return ("reject dup escaped plain", Doc("\"na\\u006De\":1,\"name\":2,\"duration\":1", ""));
        yield return ("reject dup inside wrong-typed tracks", "{\"tracks\":{\"a\":1,\"a\":2},\"duration\":1}");
        yield return ("reject dup beats syntax error", "{\"duration\":1,\"duration\":1,,}");
        yield return ("reject syntax beats dup", "{\"duration\":1,,\"duration\":1}");
        yield return ("reject dup beats semantic", "{\"duration\":\"bad\",\"loop\":true,\"loop\":false,\"tracks\":[]}");
        yield return ("reject nested data dup", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":1,\"nested\":{\"a\":1,\"a\":2}}"))));
        yield return ("reject dup in unknown-type clip data", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("NoSuchType", "{\"f0\":1,\"f0\":2}"))));

        yield return ("reject truncated", "{\"duration\":");
        yield return ("reject bare property", "{duration:1}");
        yield return ("reject control char in string", "{\"name\":\"ab\",\"duration\":1,\"tracks\":[]}");
        yield return ("reject bad escape", "{\"name\":\"a\\qb\",\"duration\":1,\"tracks\":[]}");
        yield return ("reject lone surrogate high", "{\"name\":\"\\ud800\",\"duration\":1,\"tracks\":[]}");
        yield return ("reject lone surrogate low", "{\"name\":\"\\udc00\",\"duration\":1,\"tracks\":[]}");
        yield return ("reject trailing comma object", "{\"duration\":1,}");
        yield return ("reject trailing comma tracks", "{\"duration\":1,\"tracks\":[],}");
        yield return ("reject trailing comma in tracks", "{\"duration\":10,\"tracks\":[{\"namespace\":\"" + G + "\",\"type\":\"GaTrack0\",\"clips\":[]},]}");
        yield return ("reject comment", "{/*x*/\"duration\":1,\"tracks\":[]}");
        yield return ("reject single quotes", "{'duration':1}");
        yield return ("reject NaN", "{\"duration\":NaN,\"tracks\":[]}");
        yield return ("reject Infinity", "{\"duration\":Infinity,\"tracks\":[]}");
        yield return ("reject leading zero", "{\"duration\":01,\"tracks\":[]}");
        yield return ("reject plus sign", "{\"duration\":+1,\"tracks\":[]}");
        yield return ("reject hex", "{\"duration\":0x10,\"tracks\":[]}");
        yield return ("reject unclosed object", "{\"duration\":1,\"tracks\":[]");
        yield return ("reject unclosed array", "{\"duration\":1,\"tracks\":[");
        yield return ("reject depth 70", Doc("\"duration\":10", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":" + new string('[', 70) + new string(']', 70) + "}"))));
        yield return ("reject depth 59 wrong-typed", Doc("\"duration\":10", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":" + new string('[', 59) + new string(']', 59) + "}"))));
        yield return ("reject trailing second root", "{\"duration\":1,\"tracks\":[]}{}");
        yield return ("reject trailing scalar", "{\"duration\":1,\"tracks\":[]} 5");

        yield return ("reject unknown type", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("NoSuchType", Data(1)))));
        yield return ("reject dotted type", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("Ga.Clip", Data(1)))));
        yield return ("reject dotted namespace clip", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "", withRange: true, ns: "A.B"))));
        yield return ("reject empty type", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("", Data(1)))));
        yield return ("reject wrong assembly", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", Data(1), "assembly:System.Private.CoreLib"))));
        yield return ("reject missing IBlend", Doc("\"duration\":10,\"loop\":false", TrackJ("GaBareTrack", ClipJ("GaClip0", Data(1)))));
        yield return ("reject not blendable", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip1", Data(1)))));
        yield return ("reject class track", Doc("\"duration\":10,\"loop\":false", TrackJ("GaClassTrack", ClipJ("GaClip0", Data(1)))));

        yield return ("reject unknown data field", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"nosuch\":1}"))));
        yield return ("reject float got string", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":\"x\"}"))));
        yield return ("reject float got bool", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":true}"))));
        yield return ("reject float got null", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":null}"))));
        yield return ("reject float got object", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":{}}"))));
        yield return ("reject float got array", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":[]}"))));
        yield return ("reject float overflow", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"f0\":1e39}"))));
        yield return ("reject bool got number", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", "{\"B\":5}"))));
        yield return ("reject byte overflow", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", "{\"Bt\":256}"))));
        yield return ("reject byte negative", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", "{\"Bt\":-1}"))));
        yield return ("reject int overflow", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", "{\"I\":2147483648}"))));
        yield return ("reject uint negative", Doc("\"duration\":10,\"loop\":false", TrackJ("GaPrimTrack", ClipJ("GaPrimClip", "{\"Ui\":-1}"))));
        yield return ("reject char field", Doc("\"duration\":10,\"loop\":false", TrackJ("GaStrTrack", ClipJ("GaStrClip", "{\"Code\":\"x\"}"))));
        yield return ("reject data non-object", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "5"))));

        yield return ("reject overlapping same start", Doc("\"duration\":100,\"loop\":false", TrackJ("GaTrack0",
            ClipJ("GaClip0", Data(1), "start:10;end:30") + "," + ClipJ("GaClip0", Data(1), "start:10;end:20"))));
        yield return ("reject triple overlap", Doc("\"duration\":100,\"loop\":false", TrackJ("GaTrack0",
            ClipJ("GaClip0", Data(1), "start:10;end:30") + "," + ClipJ("GaClip0", Data(1), "start:15;end:40") + "," + ClipJ("GaClip0", Data(1), "start:20;end:50"))));

        yield return ("order missing duration beats track error", "{\"loop\":false,\"tracks\":[{\"bogus\":1}]}");
        yield return ("order wrong duration beats track error", "{\"duration\":\"x\",\"tracks\":[{\"bogus\":1}]}");
        yield return ("order track0 error beats track1 missing ns", Doc("\"duration\":10,\"loop\":false", "{\"bogus\":1},{\"type\":\"GaTrack1\",\"clips\":[]}"));
        yield return ("order clip error beats track1 missing ns", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", "{\"bogus\":1}") + ",{\"type\":\"GaTrack1\",\"clips\":[]}"));
        yield return ("order track post beats clip error", Doc("\"duration\":10,\"loop\":false", "{\"clips\":[{\"bogus\":1}]}"));
        yield return ("order missing track type beats clip resolve", Doc("\"duration\":10,\"loop\":false", "{\"namespace\":\"" + G + "\",\"clips\":[{\"namespace\":\"" + G + "\",\"type\":\"NoSuchType\",\"start\":1,\"end\":9}]}"));
        yield return ("order parse error beats populate error", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"nosuch\":1}")) + "," + TrackJ("GaTrack0", "")));
        yield return ("order populate clip0 beats resolve clip1", Doc("\"duration\":10,\"loop\":false", TrackJ("GaTrack0", ClipJ("GaClip0", "{\"nosuch\":1}") + "," + ClipJ("NoSuchType", Data(1)))));
    }

    private static string AssemblyName =>
        typeof(FusedParityTests).Assembly.GetName().Name
        ?? throw new InvalidOperationException("test assembly name unavailable");
}
