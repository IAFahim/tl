using System;
using System.Security.Cryptography;
using System.Text;
using Tl;
using Tl.Bake;
using Tl.Core.Tests;
using Xunit;

namespace Tl.Bake.Tests;

public class DeterminismTests
{
    [Fact]
    public void SameJsonBakedTwice_ProducesIdenticalSha256()
    {
        var json = """
        {
          "duration": 60,
          "loop": false,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 10 },
              "clips": [
                { "start": 0, "end": 30, "payload": { "Value": 5 } },
                { "start": 30, "end": 60, "payload": { "Value": 8 } }
              ]
            },
            {
              "trackType": "Tl.Core.Tests.BlendTrack, Tl.Core.Tests",
              "track": { "Scale": 2.0 },
              "clips": [
                { "start": 0, "end": 40, "payload": { "Amount": 1.0 } },
                { "start": 20, "end": 60, "payload": { "Amount": 4.0 } }
              ]
            }
          ]
        }
        """;

        var bytes1 = TimelineBaker.BakeJson(json);
        var bytes2 = TimelineBaker.BakeJson(json);

        var sha1 = Convert.ToHexString(SHA256.HashData(bytes1));
        var sha2 = Convert.ToHexString(SHA256.HashData(bytes2));

        Assert.Equal(sha1, sha2);
        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void JsonEquivalentOfCodeBaker_ProducesByteIdenticalOutput()
    {
        var codeBytes = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Track<BlendTrack, BlendClip>(new BlendTrack(2.5f))
            .Clip(0, 0, 10, new AlphaClip(7))
            .Clip(1, 0, 6, new BlendClip(1f))
            .Clip(1, 4, 10, new BlendClip(5f))
            .Bake();

        var json = """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [
                { "start": 0, "end": 10, "payload": { "Value": 7 } }
              ]
            },
            {
              "trackType": "Tl.Core.Tests.BlendTrack, Tl.Core.Tests",
              "track": { "Scale": 2.5 },
              "clips": [
                { "start": 0, "end": 6, "payload": { "Amount": 1.0 } },
                { "start": 4, "end": 10, "payload": { "Amount": 5.0 } }
              ]
            }
          ]
        }
        """;

        var jsonBytes = TimelineBaker.BakeJson(json);

        Assert.Equal(codeBytes, jsonBytes);
    }

    [Fact]
    public unsafe void ExecutableRoundTrip_MatchesOracle()
    {
        var json = """
        {
          "duration": 4,
          "loop": false,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 3 },
              "clips": [
                { "start": 0, "end": 4, "payload": { "Value": 9 } }
              ]
            }
          ]
        }
        """;

        var bytes = TimelineBaker.BakeJson(json);
        using var asset = TimelineAsset.Load(bytes);
        var rows = new[] { new TimelineComponent(asset.Reference) };

        DataTests.Records.Clear();
        Timeline.Rows(rows).Tick(100u, 1);

        Assert.Equal(1u, rows[0].Position);
        var record = Assert.Single(DataTests.Records);
        Assert.Equal('A', record.Kind);
        Assert.Equal(3, record.Code);
        Assert.Equal(9.0f, record.Value);
        Assert.Equal(0u, record.Tick);
        Assert.Equal(100u, record.Game);
    }
}
