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

        foreach (var (track, clip) in view.PairTypes)
        {
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

        Assert.Contains(view.Labels, l => l is { TrackEntry: 0, ClipIndex: -1, Name: "combat" });
        Assert.Contains(view.Labels, l => l is { TrackEntry: 0, ClipIndex: 0, Name: "a1" });
        Assert.Contains(view.Labels, l => l is { TrackEntry: 0, ClipIndex: 1, Name: "b1" });
        Assert.Contains(view.Labels, l => l is { TrackEntry: 0, ClipIndex: 2, Name: "a2" });
        Assert.Contains(view.Labels, l => l is { TrackEntry: 1, ClipIndex: -1, Name: "echo_lane" });
        Assert.Contains(view.Labels, l => l is { TrackEntry: 1, ClipIndex: 0, Name: "e1" });
    }

}
