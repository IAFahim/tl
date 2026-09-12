using System;
using System.IO;
using Tl.Bake;
using Xunit;

namespace Tl.Bake.Tests;

public class CliTests
{
    [Fact]
    public void Cli_WithValidInput_ReturnsZeroAndCreatesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlbake_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "test.json");
            var tlbPath = Path.Combine(tempDir, "test.tlb");

            var json = """
            {
              "duration": 10,
              "loop": false,
              "tracks": [
                {
                  "trackType": "Tl.Core.Tests.AlphaTrack, Tl.Core.Tests",
                  "track": { "Code": 1 },
                  "clips": [
                    { "start": 0, "end": 10, "payload": { "Value": 5 } }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);

            var asmPath = typeof(Tl.Core.Tests.AlphaTrack).Assembly.Location;
            var exitCode = Program.Main([jsonPath, tlbPath, "--assembly", asmPath]);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tlbPath));
            var bytes = File.ReadAllBytes(tlbPath);
            Assert.True(bytes.Length >= 48);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Cli_WithDiagnosticError_ReturnsNonZero()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlbake_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "invalid.json");
            var tlbPath = Path.Combine(tempDir, "invalid.tlb");

            var json = """
            {
              "duration": 10,
              "tracks": [
                {
                  "trackType": "NonExistent.Track, NonExistent",
                  "clips": [ { "start": 0, "end": 10, "payload": {} } ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);

            var exitCode = Program.Main([jsonPath, tlbPath]);
            Assert.NotEqual(0, exitCode);
            Assert.False(File.Exists(tlbPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Cli_MissingArguments_ReturnsNonZero()
    {
        var exitCode = Program.Main([]);
        Assert.NotEqual(0, exitCode);
    }
}
