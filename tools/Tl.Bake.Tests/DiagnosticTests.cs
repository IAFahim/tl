using System;
using Tl.Gen.Tlb;
using Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class DiagnosticTests
{
    [Fact]
    public void UnknownType_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "NonExistentTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown/unresolvable type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DottedNamespace_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb.Sub",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("dotted name rejected", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DottedTypeName_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "Outer.One",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("dotted name rejected", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrackNotUnmanaged_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "ManagedTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("track or clip not unmanaged", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClipNotUnmanaged_ThrowsDiagnostic()
    {
        var ex = Assert.Throws<BakeDiagnosticException>(() =>
            BakerAssemblyResolver.ValidateUnmanaged(typeof(Tlb.AlphaTrack), typeof(ManagedClip)));
        Assert.Contains("track or clip not unmanaged", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingIBlend_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "NoBlendTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("missing IBlend<>", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClipTypeNotBlendableByTrack_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "DualBetaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("clip type not blendable by track", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(typeof(Tlb.DualBetaClip).FullName!, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousBareName_RequiresAssembly()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "Inner",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("ambiguous type", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("declare 'assembly'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongAssembly_ThrowsDiagnosticWithCandidates()
    {
        var json = $$"""
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "assembly": "NotTheRightAssembly",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("does not contain", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(typeof(Tlb.AlphaTrack).Assembly.GetName().Name!, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovedTrackTypeProperty_ThrowsMigrationDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Tlb.AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("removed property 'trackType'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("schema v1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovedClipTypeProperty_ThrowsMigrationDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "clipType": "Tlb.AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("removed property 'clipType'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovedPayloadProperty_ThrowsMigrationDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "payload": { "Value": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("removed property 'payload'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenamedLoopsProperty_ThrowsMigrationDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "loops": true,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("renamed property 'loops'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFieldNameInTrackData_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "NonExistentField": 123 },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown field name in track/payload", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownFieldNameInClipData_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 1 },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "UnknownField": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown field name in track/payload", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WrongTypedValueInTrackData_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": "not_an_int" },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("wrong-typed value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WrongTypedValueInClipData_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 1 },
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": "not_an_int" } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("wrong-typed value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MoreThanTwoOverlappingSameTypeClips_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 15 },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 5, "end": 18 },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 10, "end": 20 }
              ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("overlapping clips on one track", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SameStartSameTypeClips_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 5, "end": 15 },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 5, "end": 18 }
              ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("overlapping clips on one track", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DifferentClipTypesMayOverlapFreely()
    {
        var json = """
        {
          "duration": 12,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "DualTrack",
              "data": { "Code": 4 },
              "clips": [
                { "namespace": "Tlb", "type": "DualAlphaClip", "start": 0, "end": 10, "data": { "Value": 1 } },
                { "namespace": "Tlb", "type": "DualBetaClip", "start": 5, "end": 12, "data": { "Amount": 2 } }
              ]
            }
          ]
        }
        """;

        var bytes = TimelineBaker.BakeJson(json);
        using var asset = TimelineAsset.Load(bytes);
        var rows = new[] { new Tl.TimelineComponent(asset.Reference) };
        Tl.Timeline.Rows(rows).Tick(0u, 5);
        Tlb.DualTrack track = default;
        Tlb.DualAlphaClip alpha = default;
        Tlb.DualBetaClip beta = default;
        foreach (var frame in Tl.Timeline.Query<Tlb.DualTrack, Tlb.DualAlphaClip>(in rows[0]))
        {
            track = frame.Track;
            alpha = frame.Clip;
        }
        Assert.Equal(4, track.Code);
        Assert.Equal(1, alpha.Value);
        foreach (var frame in Tl.Timeline.Query<Tlb.DualTrack, Tlb.DualBetaClip>(in rows[0]))
        {
            beta = frame.Clip;
        }
        Assert.Equal(2f, beta.Amount);
    }

    [Fact]
    public void StartGreaterOrEqualEnd_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 10, "end": 5 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("start >= end", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClipsOutsideDuration_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 25 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("clips outside [0, duration]", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateField_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "duration": 30,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("duplicate field", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingClipNamespace_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "DualTrack",
              "clips": [ { "type": "DualAlphaClip", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("never inherited from the track", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyTracksWithDuration_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "clips": []
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("empty tracks with duration > 0 (stages must cover duration)", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
