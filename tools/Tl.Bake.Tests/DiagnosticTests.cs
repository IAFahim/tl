using System;
using Tl;
using Tl.Bake;
using Tl.Core.Tests;
using Xunit;

namespace Tl.Bake.Tests;

public struct ManagedTrack(string name) : IBlend<AlphaClip>
{
    public string Name = name;
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
}

public struct ManagedClip
{
    public string Str;
}

public struct NoBlendTrack(int code)
{
    public int Code = code;
}

public class DiagnosticTests
{
    [Fact]
    public void UnknownTrackType_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Game.NonExistentTrack, Game",
              "clips": [ { "start": 0, "end": 10, "payload": {} } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown/unresolvable track type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrackNotUnmanaged_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "{{typeof(ManagedTrack).AssemblyQualifiedName}}",
              "clips": [ { "start": 0, "end": 10, "payload": { "Value": 1 } } ]
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
            BakerAssemblyResolver.ValidateUnmanaged(typeof(AlphaTrack), typeof(ManagedClip)));
        Assert.Contains("track or clip not unmanaged", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingIBlend_ThrowsDiagnostic()
    {
        var json = $$"""
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "{{typeof(NoBlendTrack).AssemblyQualifiedName}}",
              "clips": [ { "start": 0, "end": 10, "payload": {} } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("missing IBlend<>", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownFieldNameInTrack_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "NonExistentField": 123 },
              "clips": [ { "start": 0, "end": 10, "payload": { "Value": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown field name in track/payload", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownFieldNameInPayload_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 10, "payload": { "UnknownField": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("unknown field name in track/payload", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WrongTypedValueInTrack_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": "not_an_int" },
              "clips": [ { "start": 0, "end": 10, "payload": { "Value": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("wrong-typed value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WrongTypedValueInPayload_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 10,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [ { "start": 0, "end": 10, "payload": { "Value": "not_an_int" } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("wrong-typed value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OverlappingClipsOnOneTrack_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [
                { "start": 0, "end": 15, "payload": { "Value": 1 } },
                { "start": 5, "end": 18, "payload": { "Value": 2 } },
                { "start": 10, "end": 20, "payload": { "Value": 3 } }
              ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("overlapping clips on one track", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartGreaterOrEqualEnd_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [
                { "start": 10, "end": 5, "payload": { "Value": 1 } }
              ]
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
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "track": { "Code": 1 },
              "clips": [
                { "start": 0, "end": 25, "payload": { "Value": 1 } }
              ]
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
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "clips": [ { "start": 0, "end": 10, "payload": { "Value": 1 } } ]
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("duplicate field", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyTracksWithDuration_ThrowsDiagnostic()
    {
        var json = """
        {
          "duration": 20,
          "tracks": [
            {
              "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
              "clips": []
            }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json));
        Assert.Contains("empty tracks with duration > 0 (stages must cover duration)", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
