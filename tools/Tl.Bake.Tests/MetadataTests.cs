using System;
using System.Security.Cryptography;
using Tl;
using Tl.Core.Tests;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public unsafe class MetadataTests
{
    private static byte[] BakeUniqueAsset()
    {
        var json = """
        {
          "duration": 4,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 77 },
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 2, "data": { "Value": 11 } },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 2, "end": 4, "data": { "Value": 22 } }
              ]
            }
          ]
        }
        """;
        return TimelineBaker.BakeJson(json);
    }

    [Fact]
    public void MetadataAbsentAssetsStripToIdenticalCopy()
    {
        var codeBytes = new Baker()
            .Track<Tlb.AlphaTrack, Tlb.AlphaClip>(new Tlb.AlphaTrack(3))
            .Clip(0, 0, 4, new Tlb.AlphaClip(9))
            .Bake();

        Assert.False(TlbMetadata.HasMetadata(codeBytes));
        Assert.Equal(codeBytes, TlbMetadata.Strip(codeBytes));
        Assert.Throws<ArgumentException>(() => { TlbMetadata.Read(codeBytes); });
    }

    [Fact]
    public void ReadParsesSortedPoolAndAlignedPairTypes()
    {
        var full = BakeUniqueAsset();
        var view = TlbMetadata.Read(full);

        for (var i = 1; i < view.Strings.Count; i++)
            Assert.True(string.CompareOrdinal(view.Strings[i - 1], view.Strings[i]) < 0);

        var pair = Assert.Single(view.PairTypes);
        var track = view.Types[pair.TrackType];
        var clip = view.Types[pair.ClipType];
        Assert.Equal("AlphaTrack", track.Name);
        Assert.Equal("Tlb", track.Namespace);
        Assert.Equal("AlphaClip", clip.Name);
        Assert.Equal(typeof(Tlb.AlphaTrack).Assembly.GetName().Name, track.Assembly);

        var trackIndex = BitConverter.ToUInt32(full, 16);
        Assert.Equal(1u, trackIndex);
    }
}

