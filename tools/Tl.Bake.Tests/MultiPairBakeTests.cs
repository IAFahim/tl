using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class MultiPairBakeTests
{
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
            hotKeys.Add(BitConverter.ToUInt64(bytes, (int)pairOffset + 48 * i));
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

        using var asset = TimelineAsset.LoadAsset(bytes);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var codes = new List<int>();
        foreach (var frame in Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in rows[0]))
            codes.Add(frame.Track.Code);
        rows[0].Position = 4;
        foreach (var frame in Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in rows[0]))
            codes.Add(frame.Track.Code);
        Assert.Equal([1, 2], codes);
    }
}
