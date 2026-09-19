using System;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class DatalessClipTests
{
    private const string DatalessJson = """
    {
      "duration": 10,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "data": { "Code": 3 },
          "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
        }
      ]
    }
    """;

    private static string WithEmptyClipData => DatalessJson.Replace("\"start\": 0, \"end\": 10 }", "\"start\": 0, \"end\": 10, \"data\": {} }");

    private static string WithWalkerBail => DatalessJson.Replace(
        "\"type\": \"AlphaClip\",",
        "\"type\": \"AlphaClip\", \"assembly\": \"" + typeof(DatalessClipTests).Assembly.GetName().Name + "\",");

    private static string MixedJson => """
    {
      "duration": 10,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "clips": [
            { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 5 },
            { "namespace": "Tlb", "type": "AlphaClip", "start": 5, "end": 10, "data": { "Value": 7 } }
          ]
        }
      ]
    }
    """;

    private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    private static (FastDoc Doc, BakerAssemblyResolver Resolver) ParseSimd(string json)
    {
        var resolver = new BakerAssemblyResolver();
        Assert.True(TimelineBakerSimd.TryParseFast(Utf8(json), resolver, null, out var doc));
        return (doc, resolver);
    }

    [Fact]
    public void DatalessClip_TakesSimdPath_AndBakesDefaultPayload()
    {
        var (doc, resolver) = ParseSimd(DatalessJson);
        var bytes = TimelineBakerFastCore.BakeFast(doc, resolver);
        using var asset = Tl.TimelineAsset.LoadAsset(bytes);
        var row = new Tl.TimelineComponent(asset.Reference);
        row.Position = 5;
        Tlb.AlphaTrack track = default;
        Tlb.AlphaClip clip = default;
        var frames = 0;
        foreach (var frame in Tl.Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in row))
        {
            track = frame.Track;
            clip = frame.Clip;
            frames++;
        }
        Assert.Equal(1, frames);
        Assert.Equal(3, track.Code);
        Assert.Equal(0, clip.Value);
        Assert.Equal(bytes, TimelineBaker.BakeJson(WithEmptyClipData));
    }

    [Fact]
    public void DatalessClip_WalkerPath_BakesIdenticallyToSimdPath()
    {
        var (simdDoc, simdResolver) = ParseSimd(DatalessJson);
        var simdBytes = TimelineBakerFastCore.BakeFast(simdDoc, simdResolver);
        var resolver = new BakerAssemblyResolver();
        var walkerBytes = TimelineBakerFastCore.BakeFast(TimelineBakerFast.ParseFast(Utf8(WithWalkerBail), resolver), resolver);
        Assert.Equal(simdBytes, walkerBytes);
    }

    [Fact]
    public void DatalessClip_MixedWithData_MapsIndependentPayloads()
    {
        var (doc, resolver) = ParseSimd(MixedJson);
        var bytes = TimelineBakerFastCore.BakeFast(doc, resolver);
        using var asset = Tl.TimelineAsset.LoadAsset(bytes);
        var row = new Tl.TimelineComponent(asset.Reference);
        row.Position = 2;
        Tlb.AlphaClip defaultClip = default;
        var defaultFrames = 0;
        foreach (var frame in Tl.Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in row))
        {
            defaultClip = frame.Clip;
            defaultFrames++;
        }
        Assert.Equal(1, defaultFrames);
        Assert.Equal(0, defaultClip.Value);
        row.Position = 7;
        Tlb.AlphaClip authoredClip = default;
        var authoredFrames = 0;
        foreach (var frame in Tl.Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in row))
        {
            authoredClip = frame.Clip;
            authoredFrames++;
        }
        Assert.Equal(1, authoredFrames);
        Assert.Equal(7, authoredClip.Value);
    }

    [Fact]
    public void DatalessClip_PooledWorkspace_MatchesSoloBake()
    {
        var solo = TimelineBaker.BakeJson(DatalessJson);
        var dirty = WithEmptyClipData.Replace("\"data\": {}", "\"data\": { \"Value\": 5 }");
        var batch = TimelineBaker.BakeJsonBatch([Utf8(dirty), Utf8(DatalessJson)]);
        Assert.Equal(solo, batch[1]);
        var repeated = TimelineBaker.BakeJsonBatch([Utf8(DatalessJson)]);
        Assert.Equal(solo, repeated[0]);
    }
}
