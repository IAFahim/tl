using System;
using System.Buffers.Binary;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class ValuePoolTests
{
    [Fact]
    public void SlotsReferenceOrdinalSortedPoolsByUshortIndex()
    {
        var json = """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 7 },
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 5, "data": { "Value": 30 } },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 5, "end": 10, "data": { "Value": 10 } }
              ]
            }
          ]
        }
        """;

        var bytes = TimelineBaker.BakeJson(json);
        var pairCount = BitConverter.ToUInt32(bytes, 24);
        Assert.Equal(1u, pairCount);
        var pairOffset = BitConverter.ToUInt32(bytes, 28);
        var frameOffset = BitConverter.ToUInt32(bytes, 40);

        var clipPoolAddress = (int)(pairOffset + BitConverter.ToUInt32(bytes, (int)pairOffset + 24));
        Assert.Equal(2u, BitConverter.ToUInt32(bytes, (int)pairOffset + 28));
        Assert.Equal(4u, BitConverter.ToUInt32(bytes, (int)pairOffset + 32));
        Assert.Equal(10u, BitConverter.ToUInt32(bytes, clipPoolAddress));
        Assert.Equal(30u, BitConverter.ToUInt32(bytes, clipPoolAddress + 4));

        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan((int)frameOffset + 2)));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan((int)frameOffset + 2 + 32)));
    }

    [Fact]
    public void ClipPoolBeyondUshortCapacityIsRejectedNamingTypeAndCount()
    {
        var clips = new StringBuilder();
        clips.Append("{\"duration\":65535,\"loop\":false,\"tracks\":[");
        clips.Append("{\"namespace\":\"Tlb\",\"type\":\"AlphaTrack\",\"data\":{\"Code\":1},\"clips\":[");
        for (var i = 0; i < 65535; i++)
        {
            clips.Append("{\"namespace\":\"Tlb\",\"type\":\"AlphaClip\",\"start\":").Append(i)
                .Append(",\"end\":").Append(i + 1)
                .Append(",\"data\":{\"Value\":").Append(i).Append("}}");
            if (i < 65534)
                clips.Append(',');
        }
        clips.Append("]},");
        clips.Append("{\"namespace\":\"Tlb\",\"type\":\"AlphaTrack\",\"data\":{\"Code\":2},\"clips\":[");
        clips.Append("{\"namespace\":\"Tlb\",\"type\":\"AlphaClip\",\"start\":0,\"end\":1,\"data\":{\"Value\":999999}}");
        clips.Append("]}]}");

        var failure = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(clips.ToString()));
        Assert.Contains("65536 unique values", failure.Message);
        Assert.Contains("Tlb.AlphaClip", failure.Message);
    }
}
