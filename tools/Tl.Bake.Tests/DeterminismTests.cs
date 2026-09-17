using System;
using System.Security.Cryptography;
using System.Text;
using Tl;
using Tl.Core.Tests;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class DeterminismTests
{
    public const string OwnerSampleJson = """
    {
      "name": "boss_phase_one",
      "duration": 64,
      "loop": true,
      "tracks": [
        {
          "name": "main_damage",
          "namespace": "Tlb",
          "type": "DualTrack",
          "data": { "Code": 2 },
          "clips": [
            { "name": "opening_hit", "namespace": "Tlb", "type": "DualAlphaClip", "start": 0,  "end": 12, "data": { "Value": 5 } },
            { "name": "mid_stun",    "namespace": "Tlb", "type": "DualBetaClip",  "start": 20, "end": 26, "data": { "Amount": 2 } },
            { "name": "heavy_hit",   "namespace": "Tlb", "type": "DualAlphaClip", "start": 40, "end": 52, "data": { "Value": 9 } }
          ]
        },
        {
          "name": "armor_buff",
          "namespace": "Tlb",
          "type": "BlendTrack",
          "data": { "Scale": 0.5 },
          "clips": [
            { "name": "ramp_up", "namespace": "Tlb", "type": "BlendClip", "start": 8, "end": 30, "data": { "Amount": 3 } }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void SameJsonBakedTwice_ProducesIdenticalSha256()
    {
        var bytes1 = TimelineBaker.BakeJson(OwnerSampleJson);
        var bytes2 = TimelineBaker.BakeJson(OwnerSampleJson);

        var sha1 = Convert.ToHexString(SHA256.HashData(bytes1));
        var sha2 = Convert.ToHexString(SHA256.HashData(bytes2));

        Assert.Equal(sha1, sha2);
        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void MetadataStrippedBakeEqualsCodeBaker()
    {
        var codeBytes = new Baker()
            .Track<Tlb.AlphaTrack, Tlb.AlphaClip>(new Tlb.AlphaTrack(1))
            .Track<Tlb.BlendTrack, Tlb.BlendClip>(new Tlb.BlendTrack(2.5f))
            .Clip(0, 0, 10, new Tlb.AlphaClip(7))
            .Clip(1, 0, 6, new Tlb.BlendClip(1f))
            .Clip(1, 4, 10, new Tlb.BlendClip(5f))
            .Bake();

        var json = """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 1 },
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": 7 } }
              ]
            },
            {
              "namespace": "Tlb",
              "type": "BlendTrack",
              "data": { "Scale": 2.5 },
              "clips": [
                { "namespace": "Tlb", "type": "BlendClip", "start": 0, "end": 6, "data": { "Amount": 1.0 } },
                { "namespace": "Tlb", "type": "BlendClip", "start": 4, "end": 10, "data": { "Amount": 5.0 } }
              ]
            }
          ]
        }
        """;

        var jsonBytes = TimelineBaker.BakeJson(json);
        Assert.True(TlbMetadata.HasMetadata(jsonBytes));
        Assert.False(TlbMetadata.HasMetadata(codeBytes));
        Assert.Equal(codeBytes, TlbMetadata.Strip(jsonBytes));
    }

    [Fact]
    public void StripIsIdempotent()
    {
        var full = TimelineBaker.BakeJson(OwnerSampleJson);
        var once = TlbMetadata.Strip(full);
        var twice = TlbMetadata.Strip(once);

        Assert.Equal(once, twice);
        Assert.False(TlbMetadata.HasMetadata(once));
        Assert.Equal(0u, BitConverter.ToUInt32(once, 40));
        Assert.Equal((uint)once.Length, BitConverter.ToUInt32(once, 44));
        using var asset = TimelineAsset.Load(once);
    }

    [Fact]
    public void ExecutableRoundTrip_MatchesOracle()
    {
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);

        Assert.Equal(Recording.OracleFrames(), Recording.FramesOf(bytes));
    }

    [Fact]
    public void FullAndStrippedBytesProduceIdenticalFrames()
    {
        var full = TimelineBaker.BakeJson(Recording.OracleJson);
        var stripped = TlbMetadata.Strip(full);
        var frames = Recording.FramesOf(full);

        Assert.NotEqual(full, stripped);
        Assert.Equal(frames, Recording.FramesOf(stripped));
        Assert.Equal(Recording.OracleFrames(), frames);
    }
    [Fact]
    public void CliStripProducesLoadableStrippedCopy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_strip_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "test.json");
            var fullTlb = Path.Combine(tempDir, "full.tlb");
            var strippedTlb = Path.Combine(tempDir, "stripped.tlb");
            File.WriteAllText(jsonPath, Recording.OracleJson);
            var asmPath = typeof(Tlb.AlphaTrack).Assembly.Location;

            Assert.Equal(0, Tl.Bake.Program.Main([jsonPath, fullTlb, "--assembly", asmPath]));
            Assert.Equal(0, Tl.Bake.Program.Main(["--strip", fullTlb, strippedTlb]));

            var full = File.ReadAllBytes(fullTlb);
            var stripped = File.ReadAllBytes(strippedTlb);
            Assert.True(TlbMetadata.HasMetadata(full));
            Assert.False(TlbMetadata.HasMetadata(stripped));
            Assert.Equal(TlbMetadata.Strip(full), stripped);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null &&
               !(Directory.Exists(Path.Combine(directory.FullName, "src", "Tl.Core")) &&
                 Directory.Exists(Path.Combine(directory.FullName, "tests", "Tl.Core.Tests"))))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("repository root not found");
    }
}
