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
    [Fact]
    public void KernelEmissionIsByteIdenticalForIdenticalBytes()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        Assert.Equal(KernelEmitter.Emit(bytes), KernelEmitter.Emit(bytes));
        Assert.Contains("TimelineKernel_", KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void CliKernelFlagWritesDeterministicSource()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlbake_kernel_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "test.json");
            var firstTlb = Path.Combine(tempDir, "first.tlb");
            var secondTlb = Path.Combine(tempDir, "second.tlb");
            var firstKernel = Path.Combine(tempDir, "first.g.cs");
            var secondKernel = Path.Combine(tempDir, "second.g.cs");
            var json = """
            {
              "duration": 3,
              "loop": false,
              "tracks": [
                {
                  "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
                  "track": { "Code": 9 },
                  "clips": [
                    { "start": 0, "end": 1, "payload": { "Value": 11 } },
                    { "start": 2, "end": 3, "payload": { "Value": 22 } }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);
            var asmPath = typeof(Tl.Core.Tests.AlphaTrack).Assembly.Location;

            Assert.Equal(0, Program.Main([jsonPath, firstTlb, "--assembly", asmPath, "--kernel", firstKernel]));
            Assert.Equal(0, Program.Main([jsonPath, secondTlb, "--assembly", asmPath, "--kernel", secondKernel]));

            Assert.Equal(File.ReadAllBytes(firstTlb), File.ReadAllBytes(secondTlb));
            Assert.Equal(File.ReadAllText(firstKernel), File.ReadAllText(secondKernel));
            Assert.Equal(File.ReadAllText(firstKernel), KernelEmitter.Emit(File.ReadAllBytes(firstTlb)));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [InlineData("FiniteAlphaKernel", "Finite")]
    [InlineData("LoopingAlphaKernel", "Looping")]
    [InlineData("AbaMirroredKernel", "AbaMirrored")]
    [InlineData("BlendSpanThreeKernel", "BlendSpanThree")]
    [InlineData("BlendSpanOneKernel", "BlendSpanOne")]
    [InlineData("ConsumerlessKernel", "Consumerless")]
    [InlineData("EmptyAssetKernel", "Empty")]
    public void CommittedKernelFixturesRegenerateByteIdentically(string fixtureFile, string bakerMethod)
    {
        var bytes = bakerMethod switch
        {
            "Finite" => Tl.Core.Tests.KernelBakers.Finite(),
            "Looping" => Tl.Core.Tests.KernelBakers.Looping(),
            "AbaMirrored" => Tl.Core.Tests.KernelBakers.AbaMirrored(),
            "BlendSpanThree" => Tl.Core.Tests.KernelBakers.BlendSpanThree(),
            "BlendSpanOne" => Tl.Core.Tests.KernelBakers.BlendSpanOne(),
            "Consumerless" => Tl.Core.Tests.KernelBakers.Consumerless(),
            "Empty" => Tl.Core.Tests.KernelBakers.Empty(),
            _ => throw new InvalidOperationException("unknown fixture"),
        };

        var root = RepoRoot();
        var committed = File.ReadAllText(Path.Combine(root, "tests", "Tl.Core.Tests", "KernelFixtures", fixtureFile + ".g.cs"));
        Assert.Equal(committed, KernelEmitter.Emit(bytes));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null &&
               !(Directory.Exists(Path.Combine(directory.FullName, "src", "Tl.Core")) &&
                 Directory.Exists(Path.Combine(directory.FullName, "unity", "com.iafahim.tl"))))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("repository root not found");
    }
}
