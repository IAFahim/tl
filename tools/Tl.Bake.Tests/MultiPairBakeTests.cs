using System;
using Tl;
using Tl.Bake;
using Tl.Core.Tests;
using Xunit;

namespace Tl.Bake.Tests;

public class MultiPairBakeTests
{
    private const string DualJson = """
    {
      "duration": 8,
      "loop": false,
      "tracks": [
        {
          "trackType": "Tl.Core.Tests.DualTrack, Tl.Core.Tests",
          "clipType": "Tl.Core.Tests.DualAlphaClip, Tl.Core.Tests",
          "track": { "Code": 1 },
          "clips": [
            { "start": 0, "end": 6, "payload": { "Value": 10 } },
            { "start": 2, "end": 8, "payload": { "Value": 30 } }
          ]
        },
        {
          "trackType": "Tl.Core.Tests.EchoTrack, Tl.Core.Tests",
          "clipType": "Tl.Core.Tests.EchoClip, Tl.Core.Tests",
          "track": { "Code": 5 },
          "clips": [ { "start": 0, "end": 4, "payload": { "Value": 7 } } ]
        },
        {
          "trackType": "Tl.Core.Tests.DualTrack, Tl.Core.Tests",
          "clipType": "Tl.Core.Tests.DualBetaClip, Tl.Core.Tests",
          "track": { "Code": 2 },
          "clips": [
            { "start": 1, "end": 7, "payload": { "Amount": 1.5 } },
            { "start": 3, "end": 8, "payload": { "Amount": 2.5 } }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void AmbiguousWithoutClipType_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 8,
          "tracks": [
            {
              "trackType": "{{typeof(DualTrack).AssemblyQualifiedName}}",
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 6, "payload": { "Value": 10 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("ambiguous clip type — declare clipType", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongClipType_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 8,
          "tracks": [
            {
              "trackType": "{{typeof(DualTrack).AssemblyQualifiedName}}",
              "clipType": "{{typeof(EchoClip).AssemblyQualifiedName}}",
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 6, "payload": { "Value": 10 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("clipType mismatch", ex.Message, StringComparison.Ordinal);
        Assert.Contains("does not name a Tl.IBlend<TClip> pairing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MismatchedClipTypeOnSingleBlendTrack_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 8,
          "tracks": [
            {
              "trackType": "{{typeof(AlphaTrack).AssemblyQualifiedName}}",
              "clipType": "{{typeof(DualBetaClip).AssemblyQualifiedName}}",
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 6, "payload": { "Value": 10 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("clipType mismatch", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongTypedClipType_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 8,
          "tracks": [
            {
              "trackType": "{{typeof(AlphaTrack).AssemblyQualifiedName}}",
              "clipType": 5,
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 6, "payload": { "Value": 10 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("wrong-typed value: 'clipType' must be a string", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchingClipTypeOnSingleBlendTrack_Bakes()
    {
        var json = $$"""
        {
          "duration": 8,
          "tracks": [
            {
              "trackType": "{{typeof(AlphaTrack).AssemblyQualifiedName}}",
              "clipType": "{{typeof(AlphaClip).AssemblyQualifiedName}}",
              "track": { "Code": 3 },
              "clips": [ { "start": 0, "end": 8, "payload": { "Value": 9 } } ]
            }
          ]
        }
        """;

        var bytes = TimelineBaker.BakeJson(json);
        using var asset = TimelineAsset.Load(bytes);
        var component = new TimelineComponent(asset.Reference);
        AlphaClip clip = default;
        AlphaTrack track = default;
        foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
        {
            track = frame.Track;
            clip = frame.Clip;
        }
        Assert.Equal(3, track.Code);
        Assert.Equal(9, clip.Value);
    }

    [Fact]
    public void DualPairJsonBakesByteIdenticalToCodeBakerAndDeterministically()
    {
        var first = TimelineBaker.BakeJson(DualJson);
        var second = TimelineBaker.BakeJson(DualJson);

        Assert.Equal(first, second);
        Assert.Equal(MultiPairTests.DualFixture(), first);
    }

    [Fact]
    public unsafe void DualPairJsonRoundTripMatchesOracle()
    {
        var bytes = TimelineBaker.BakeJson(DualJson);
        using var asset = TimelineAsset.Load(bytes);
        var rows = new[] { new TimelineComponent(asset.Reference) };

        _ = MultiPairTests.Recording;
        MultiPairTests.Records.Clear();
        Timeline.Rows(rows).Tick(100u, 8);

        Assert.Equal(MultiPairTests.ForwardOracle(100u), MultiPairTests.Records);
        Assert.Equal(8u, rows[0].Position);
        Assert.Equal(0L, rows[0].Cycle);
    }
}
