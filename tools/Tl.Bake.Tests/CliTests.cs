using System;
using System.IO;
using Xunit;

namespace Tl.Bake.Tests;

public class CliTests
{
    [Fact]
    public void Cli_WithValidInput_ReturnsZeroAndCreatesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_test_" + Guid.NewGuid().ToString("N"));
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
                  "namespace": "Tlb",
                  "type": "AlphaTrack",
                  "data": { "Code": 1 },
                  "clips": [
                    { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": 5 } }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);

            var asmPath = typeof(Tlb.AlphaTrack).Assembly.Location;
            var exitCode = Tl.Bake.Program.Main([jsonPath, tlbPath, "--assembly", asmPath]);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tlbPath));
            var bytes = File.ReadAllBytes(tlbPath);
            Assert.True(bytes.Length >= 48);
            Assert.True(Tl.Gen.Tlb.TlbMetadata.HasMetadata(bytes));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Cli_WithDiagnosticError_ReturnsNonZero()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_test_" + Guid.NewGuid().ToString("N"));
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
                  "namespace": "Nope",
                  "type": "MissingTrack",
                  "clips": [ { "namespace": "Nope", "type": "MissingClip", "start": 0, "end": 10 } ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);

            var exitCode = Tl.Bake.Program.Main([jsonPath, tlbPath]);
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
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var previous = Directory.GetCurrentDirectory();
        var stderr = new StringWriter();
        var original = Console.Error;
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetError(stderr);
            var exitCode = Tl.Bake.Program.Main([]);
            Assert.NotEqual(0, exitCode);
            Assert.Contains("No input given", stderr.ToString());
        }
        finally
        {
            Console.SetError(original);
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(tempDir, true);
        }
    }
}
