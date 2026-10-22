using System.Buffers.Binary;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbRejectionCorpusTests
{
    const string SingleTrack = """
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

    const string Blended = """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "BlendTrack",
              "data": { "Scale": 2 },
              "clips": [
                { "namespace": "Tlb", "type": "BlendClip", "start": 0, "end": 6, "data": { "Amount": 1 } },
                { "namespace": "Tlb", "type": "BlendClip", "start": 5, "end": 10, "data": { "Amount": 2 } }
              ]
            }
          ]
        }
        """;

    const string FiveStages = """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "BlendTrack",
              "data": { "Scale": 2 },
              "clips": [
                { "namespace": "Tlb", "type": "BlendClip", "start": 0, "end": 4, "data": { "Amount": 1 } },
                { "namespace": "Tlb", "type": "BlendClip", "start": 3, "end": 7, "data": { "Amount": 2 } },
                { "namespace": "Tlb", "type": "BlendClip", "start": 6, "end": 10, "data": { "Amount": 3 } }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void RejectsMisalignedValuePoolWithLocatedDiagnostic()
    {
        var bytes = TimelineBaker.BakeJson(SingleTrack);
        var pairOffset = ReadU32(bytes, 28);
        WriteU32(bytes, (int)pairOffset + 12, ReadU32(bytes, (int)pairOffset + 12) + 8);
        Rejects(bytes, "TLB value pools must be 16-byte aligned.");
    }

    [Fact]
    public void RejectsSecondValueIndexWithoutBlendWindow()
    {
        var bytes = TimelineBaker.BakeJson(Blended);
        var row = BlendedRow(bytes);
        WriteU32(bytes, row + 20, 0);
        Rejects(bytes, "TLB second value index requires a blend window.");
    }

    [Fact]
    public void RejectsStageRangeThatRunsBackwards()
    {
        var bytes = TimelineBaker.BakeJson(FiveStages);
        var stageOffset = ReadU32(bytes, 32);
        Assert.Equal(3u, ReadU32(bytes, (int)stageOffset + 16));
        WriteU32(bytes, (int)stageOffset + 16 + 4, 2);
        WriteU32(bytes, (int)stageOffset + 32, 2);
        Rejects(bytes, "TLB stages must partition [0, Duration) into non-empty ranges.");
    }

    [Fact]
    public void RejectsSlotTrackIndexBeyondTrackCount()
    {
        var bytes = TimelineBaker.BakeJson(SingleTrack);
        var row = FirstRow(bytes);
        bytes[row + 6] = 1;
        Rejects(bytes, "TLB slot track index out of bounds.");
    }

    [Fact]
    public void RejectsSlotWindowThatMissesItsStage()
    {
        var bytes = TimelineBaker.BakeJson(SingleTrack);
        var row = FirstRow(bytes);
        WriteU32(bytes, row + 8, 1);
        Rejects(bytes, "TLB slot window must contain the referencing stage.");
    }

    [Fact]
    public void RejectsBlendWindowThatMissesItsStage()
    {
        var bytes = TimelineBaker.BakeJson(Blended);
        var row = BlendedRow(bytes);
        WriteU32(bytes, row + 16, ReadU32(bytes, row + 16) + 1);
        Rejects(bytes, "TLB blend window must contain the referencing stage.");
    }

    static void Rejects(byte[] bytes, string message)
    {
        var failure = Assert.Throws<ArgumentException>(() => TimelineAsset.Load(bytes));
        Assert.Equal(message, failure.Message);
    }

    static int FirstRow(byte[] bytes)
    {
        var frameOffset = (int)ReadU32(bytes, 40);
        Assert.True(frameOffset > 0);
        return frameOffset;
    }

    static int BlendedRow(byte[] bytes)
    {
        var hotLength = (int)ReadU32(bytes, 44);
        for (var at = FirstRow(bytes); at + 24 <= hotLength; at += 24)
            if (ReadU32(bytes, at + 20) != 0) return at;
        throw new Xunit.Sdk.XunitException("fixture bakes without a blended row");
    }

    static uint ReadU32(byte[] bytes, int at) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));

    static void WriteU32(byte[] bytes, int at, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), value);
}
