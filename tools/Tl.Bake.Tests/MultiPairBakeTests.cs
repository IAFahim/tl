using System;
using System.Security.Cryptography;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class MultiPairBakeTests
{
    [Fact]
    public unsafe void InterleavedDerivedGroups_ExecuteInAuthoredClipOrder()
    {
        _ = Recording.Records;
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);
        using var asset = TimelineAsset.Load(bytes);
        var rows = new[] { new TimelineComponent(asset.Reference) };

        Recording.Records.Clear();
        Timeline.Rows(rows).Tick(7u, 8);

        Assert.Equal(Recording.ForwardOracle(7u), Recording.Records);
    }

    [Fact]
    public unsafe void ReverseMovement_MirrorsAuthoredOrder()
    {
        _ = Recording.Records;
        var full = TimelineBaker.BakeJson(Recording.OracleJson);
        var stripped = TlbMetadata.Strip(full);

        Recording.Records.Clear();
        using (var asset = TimelineAsset.Load(full))
        {
            var rows = new[] { new TimelineComponent(asset.Reference) };
            Timeline.Rows(rows).Tick(7u, 8);
        }
        var fullForward = Recording.Records.ToArray();

        Recording.Records.Clear();
        using (var asset = TimelineAsset.Load(full))
        {
            var rows = new[] { new TimelineComponent(asset.Reference) };
            Timeline.Rows(rows).Tick(7u, 8);
            Timeline.Rows(rows).Tick(7u, -8);
        }
        var fullTrace = Recording.Records.ToArray();

        Recording.Records.Clear();
        using (var asset = TimelineAsset.Load(stripped))
        {
            var rows = new[] { new TimelineComponent(asset.Reference) };
            Timeline.Rows(rows).Tick(7u, 8);
            Timeline.Rows(rows).Tick(7u, -8);
        }
        var strippedTrace = Recording.Records.ToArray();

        Assert.Equal(18, fullForward.Length);
        Assert.Equal(Recording.ForwardOracle(7u), fullForward);
        Assert.Equal(fullForward.Length * 2, fullTrace.Length);
        Assert.Equal(fullTrace, strippedTrace);
        Assert.Equal('A', fullTrace[18].Pair);
        Assert.Equal(7u, fullTrace[18].Tick);
        Assert.Equal(30f, fullTrace[18].Value);
        Assert.Equal('A', fullTrace[19].Pair);
        Assert.Equal(6u, fullTrace[19].Tick);
        Assert.Equal('B', fullTrace[20].Pair);
        Assert.Equal(6u, fullTrace[20].Tick);
    }

    [Fact]
    public void MetadataStrippedOracleMatchesForwardOracle()
    {
        _ = Recording.Records;
        var full = TimelineBaker.BakeJson(Recording.OracleJson);
        var stripped = TlbMetadata.Strip(full);
        Assert.NotEqual(full, stripped);
        Assert.True(TlbMetadata.HasMetadata(full));
        Assert.False(TlbMetadata.HasMetadata(stripped));

        Recording.Records.Clear();
        using (var asset = TimelineAsset.Load(stripped))
        {
            var rows = new[] { new TimelineComponent(asset.Reference) };
            Timeline.Rows(rows).Tick(100u, 8);
        }
        Assert.Equal(Recording.ForwardOracle(100u), Recording.Records);
    }

    [Fact]
    public void PairTypeTableIsIndexAlignedWithHotPairs()
    {
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);
        var view = TlbMetadata.Read(bytes);

        var pairCount = BitConverter.ToUInt32(bytes, 24);
        var pairOffset = BitConverter.ToUInt32(bytes, 28);
        Assert.Equal((int)pairCount, view.PairTypes.Count);

        var hotKeys = new List<ulong>();
        for (var i = 0; i < pairCount; i++)
            hotKeys.Add(BitConverter.ToUInt64(bytes, (int)pairOffset + 16 * i));
        Assert.Equal(hotKeys, hotKeys.OrderBy(k => k).ToList());

        for (var i = 0; i < view.PairTypes.Count; i++)
        {
            var (track, clip) = view.PairTypes[i];
            var trackIdentity = view.Types[track];
            var clipIdentity = view.Types[clip];
            Assert.Equal("Tlb", trackIdentity.Namespace);
            Assert.Equal("Tlb", clipIdentity.Namespace);
            Assert.Equal(typeof(Tlb.DualTrack).Assembly.GetName().Name, trackIdentity.Assembly);
            var expectedPair = (Track: trackIdentity.Name, Clip: clipIdentity.Name);
            Assert.True(expectedPair is ("DualTrack", "DualAlphaClip") or ("DualTrack", "DualBetaClip") or ("EchoTrack", "EchoClip"));
        }
    }

    [Fact]
    public void MetadataPoolIsSortedAndDeduplicated()
    {
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);
        var view = TlbMetadata.Read(bytes);

        for (var i = 1; i < view.Strings.Count; i++)
            Assert.True(string.CompareOrdinal(view.Strings[i - 1], view.Strings[i]) < 0);
        Assert.Contains("oracle_asset", view.Strings);
        Assert.Contains("combat", view.Strings);
        Assert.Contains("a1", view.Strings);
    }

    [Fact]
    public void LabelsRecordRootTrackAndClipNames()
    {
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);
        var view = TlbMetadata.Read(bytes);

        var root = Assert.Single(view.Labels, l => l.TrackEntry == -1);
        Assert.Equal("oracle_asset", root.Name);

        Assert.Contains(view.Labels, l => l.TrackEntry == 0 && l.ClipIndex == -1 && l.Name == "combat");
        Assert.Contains(view.Labels, l => l.TrackEntry == 0 && l.ClipIndex == 0 && l.Name == "a1");
        Assert.Contains(view.Labels, l => l.TrackEntry == 0 && l.ClipIndex == 1 && l.Name == "b1");
        Assert.Contains(view.Labels, l => l.TrackEntry == 0 && l.ClipIndex == 2 && l.Name == "a2");
        Assert.Contains(view.Labels, l => l.TrackEntry == 1 && l.ClipIndex == -1 && l.Name == "echo_lane");
        Assert.Contains(view.Labels, l => l.TrackEntry == 1 && l.ClipIndex == 0 && l.Name == "e1");
    }

    [Fact]
    public void BakeIsDeterministicIncludingMetadata()
    {
        var first = TimelineBaker.BakeJson(Recording.OracleJson);
        var second = TimelineBaker.BakeJson(Recording.OracleJson);
        Assert.Equal(first, second);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(first)), Convert.ToHexString(SHA256.HashData(second)));
    }

    [Fact]
    public void SameTrackTypeTwiceWithDifferentData_StaysSeparateEntries()
    {
        var json = """
        {
          "duration": 8,
          "loop": false,
          "tracks": [
            {
              "name": "left",
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 1 },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 4, "data": { "Value": 1 } } ]
            },
            {
              "name": "right",
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 2 },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 4, "end": 8, "data": { "Value": 2 } } ]
            }
          ]
        }
        """;

        var bytes = TimelineBaker.BakeJson(json);
        Assert.Equal(2u, BitConverter.ToUInt32(bytes, 16));

        using var asset = TimelineAsset.Load(bytes);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var codes = new List<int>();
        foreach (var frame in Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in rows[0]))
            codes.Add(frame.Track.Code);
        Timeline.Rows(rows).Tick(0u, 4);
        foreach (var frame in Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in rows[0]))
            codes.Add(frame.Track.Code);
        Assert.Equal([1, 2], codes);
    }
}
