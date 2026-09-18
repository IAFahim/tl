using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class BatchParityTests
{
    [Fact]
    public void BatchOutputMatchesPerItemBakes()
    {
        var docs = MixedDocs(96);
        var inputs = docs.Select(Encoding.UTF8.GetBytes).ToArray();
        var batched = TimelineBaker.BakeJsonBatch(inputs);
        Assert.Equal(inputs.Length, batched.Length);
        for (var i = 0; i < inputs.Length; i++)
            Assert.Equal(TimelineBaker.BakeJson(inputs[i]), batched[i]);
    }

    [Fact]
    public void BatchOrderIsInputOrder()
    {
        var docs = MixedDocs(64);
        var inputs = docs.Select(Encoding.UTF8.GetBytes).ToArray();
        for (var round = 0; round < 4; round++)
        {
            var batched = TimelineBaker.BakeJsonBatch(inputs);
            for (var i = 0; i < inputs.Length; i++)
                Assert.Equal(TimelineBaker.BakeJson(inputs[i]), batched[i]);
        }
    }

    [Fact]
    public void BatchSingleItemBakes()
    {
        var inputs = new[] { Encoding.UTF8.GetBytes(Doc("GaTrack0", "GaClip0", 3, 4, 100, Ns)) };
        var batched = TimelineBaker.BakeJsonBatch(inputs);
        Assert.Equal(TimelineBaker.BakeJson(inputs[0]), batched[0]);
    }

    [Fact]
    public void BatchThrowsFirstFailureLikeSerial()
    {
        var failing = "{\"name\":\"d\",\"loop\":true,\"tracks\":[]}";
        var inputs = new[]
        {
            Encoding.UTF8.GetBytes(Doc("GaTrack0", "GaClip0", 2, 2, 100, Ns)),
            Encoding.UTF8.GetBytes(failing),
            Encoding.UTF8.GetBytes(Doc("GaTrack0", "GaClip0", 2, 2, 100, Ns)),
        };
        var batchError = Record.Exception(() => TimelineBaker.BakeJsonBatch(inputs));
        var serialError = Record.Exception(() =>
        {
            _ = TimelineBaker.BakeJson(inputs[0]);
            _ = TimelineBaker.BakeJson(inputs[1]);
            _ = TimelineBaker.BakeJson(inputs[2]);
        });
        Assert.IsType<BakeDiagnosticException>(batchError);
        Assert.NotNull(serialError);
        Assert.Equal(serialError!.GetType(), batchError!.GetType());
        Assert.Equal(serialError.Message, batchError.Message);
    }

    [Fact]
    public void BatchThrowsLowestIndexOnMultipleFailures()
    {
        var first = "{\"name\":\"a\",\"loop\":true,\"tracks\":[]}";
        var second = "{\"name\":\"b\",\"duration\":10,\"loop\":true}";
        var inputs = new[]
        {
            Encoding.UTF8.GetBytes(Doc("GaTrack0", "GaClip0", 2, 2, 100, Ns)),
            Encoding.UTF8.GetBytes(first),
            Encoding.UTF8.GetBytes(Doc("GaTrack0", "GaClip0", 2, 2, 100, Ns)),
            Encoding.UTF8.GetBytes(second),
        };
        var batchError = Record.Exception(() => TimelineBaker.BakeJsonBatch(inputs));
        var expected = Record.Exception(() => _ = TimelineBaker.BakeJson(inputs[1]));
        Assert.NotNull(expected);
        Assert.Equal(expected!.GetType(), batchError!.GetType());
        Assert.Equal(expected.Message, batchError.Message);
    }

    [Fact]
    public void BatchFallbackAndSimdPathsInterleave()
    {
        var docs = new List<string>
        {
            Doc("GaTrack0", "GaClip0", 4, 8, 120, Ns),
            Doc("GaTrack0", "GaClip0", 3, 1, 90, Ns, null, rootName: "\\u0041bc"),
            Doc("GaPrimTrack", "GaPrimClip", 2, 0, 80, Ns, "\"B\":true,\"Bt\":7,\"Sb\":-3,\"Sh\":300,\"Us\":60000,\"I\":-5,\"Ui\":7,\"L\":-9,\"Ul\":11,\"F\":1.5,\"D\":2.5"),
            Doc("AlphaTrack", "AlphaClip", 3, 0, 60, "Tlb", "\"Value\":4"),
            Doc("BlendTrack", "BlendClip", 2, 0, 60, "Tlb", "\"Amount\":0.5"),
            Doc("DualTrack", "DualAlphaClip", 2, 0, 60, "Tlb", "\"Value\":9"),
            Doc("EchoTrack", "EchoClip", 2, 0, 60, "Tlb", "\"Value\":2"),
            Doc("GaFatTrack", "GaFatClip", 2, 6, 70, Ns),
        };
        for (var i = 0; i < 8; i++)
            docs.Add(docs[i % docs.Count]);
        var inputs = docs.Select(Encoding.UTF8.GetBytes).ToArray();
        var batched = TimelineBaker.BakeJsonBatch(inputs);
        for (var i = 0; i < inputs.Length; i++)
            Assert.Equal(TimelineBaker.BakeJson(inputs[i]), batched[i]);
    }

    [Fact]
    public void BatchReusedPoolKeepsMissingFieldsAtDefault()
    {
        var full = Doc("GaTrack0", "GaClip0", 3, 8, 100, Ns);
        var sparse = Doc("GaTrack0", "GaClip0", 3, 1, 100, Ns);
        var inputs = new[] { Encoding.UTF8.GetBytes(full), Encoding.UTF8.GetBytes(sparse), Encoding.UTF8.GetBytes(full) };
        for (var round = 0; round < 3; round++)
        {
            var batched = TimelineBaker.BakeJsonBatch(inputs);
            for (var i = 0; i < inputs.Length; i++)
                Assert.Equal(TimelineBaker.BakeJson(inputs[i]), batched[i]);
        }
    }

    [Fact]
    public void RepeatedClipPayloadsDedupMatchesOracle()
    {
        var sb = new StringBuilder();
        sb.Append("{\"duration\":40,\"loop\":true,\"tracks\":[{\"namespace\":\"").Append(Ns)
          .Append("\",\"type\":\"GaTrack0\",\"clips\":[");
        for (var c = 0; c < 4; c++)
        {
            if (c > 0)
                sb.Append(',');
            sb.Append("{\"namespace\":\"").Append(Ns).Append("\",\"type\":\"GaClip0\",\"start\":").Append(c * 10)
              .Append(",\"end\":").Append((c + 1) * 10).Append(",\"data\":{");
            for (var f = 0; f < 8; f++)
            {
                if (f > 0)
                    sb.Append(',');
                var source = c is 2 or 3 ? c - 2 : c;
                sb.Append("\"f").Append(f).Append("\":").Append(source * 0.25 + 0.5);
            }
            sb.Append("}}");
        }
        sb.Append("]}]}");
        var json = sb.ToString();
        var input = Encoding.UTF8.GetBytes(json);
        var legacy = BakeOracle.BakeJsonLegacy(json);
        Assert.Equal(legacy, TimelineBaker.BakeJson(input));
        var batched = TimelineBaker.BakeJsonBatch([input, input]);
        Assert.Equal(legacy, batched[0]);
        Assert.Equal(legacy, batched[1]);
    }

    [Fact]
    public void BatchEmptyInputYieldsEmptyOutput()
    {
        Assert.Empty(TimelineBaker.BakeJsonBatch([]));
    }

    private const string Ns = "FusedBake";

    private static List<string> MixedDocs(int count)
    {
        var docs = new List<string>();
        string[][] pairs =
        [
            ["GaTrack0", "GaClip0"],
            ["GaTrack0", "GaClip0"],
            ["GaFatTrack", "GaFatClip"],
            ["AlphaTrack", "AlphaClip"],
            ["EchoTrack", "EchoClip"],
            ["BlendTrack", "BlendClip"],
            ["DualTrack", "DualAlphaClip"],
            ["DualTrack", "DualBetaClip"],
        ];
        for (var i = 0; i < count; i++)
        {
            var pair = pairs[i % pairs.Length];
            var ns = pair[0][0] == 'G' ? Ns : "Tlb";
            var floats = pair[1].StartsWith("Ga") ? 2 + i % 6 : 0;
            var data = ns == "Tlb"
                ? pair[1] switch
                {
                    "DualBetaClip" => "\"Amount\":0.25",
                    "AlphaClip" or "EchoClip" or "DualAlphaClip" => "\"Value\":3",
                    _ => "\"Amount\":0.75",
                }
                : null;
            docs.Add(Doc(pair[0], pair[1], 2 + i % 5, floats, 60 + 10 * (i % 7), ns, data, withName: i % 3 == 0));
        }
        return docs;
    }

    private static string Doc(string trackType, string clipType, int clips, int floats, int duration, string ns, string? clipData = null, bool withName = true, string rootName = "d")
    {
        var sb = new StringBuilder();
        sb.Append('{');
        if (withName)
            sb.Append("\"name\":\"").Append(rootName).Append("\",");
        sb.Append("\"duration\":").Append(duration).Append(",\"loop\":true,\"tracks\":[");
        sb.Append("{\"namespace\":\"").Append(ns).Append("\",\"type\":\"").Append(trackType).Append('"');
        sb.Append(",\"clips\":[");
        for (var c = 0; c < clips; c++)
        {
            if (c > 0)
                sb.Append(',');
            sb.Append("{\"namespace\":\"").Append(ns).Append("\",\"type\":\"").Append(clipType).Append('"');
            sb.Append(",\"start\":").Append(c * 10).Append(",\"end\":").Append((c + 1) * 10);
            if (clipData != null)
                sb.Append(",\"data\":{").Append(clipData).Append('}');
            else if (floats > 0)
            {
                sb.Append(",\"data\":{");
                for (var f = 0; f < floats; f++)
                {
                    if (f > 0)
                        sb.Append(',');
                    sb.Append("\"f").Append(f).Append("\":").Append((f + c) * 0.5 + 0.5);
                }
                sb.Append('}');
            }
            sb.Append('}');
        }
        sb.Append("]}]}");
        return sb.ToString();
    }
}
