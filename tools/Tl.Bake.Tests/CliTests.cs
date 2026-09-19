using System;
using System.IO;
using Tl.Gen.Tlb;
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
            Assert.True(TlbMetadata.HasMetadata(bytes));
            Assert.Equal(TimelineBaker.BakeJson(json), bytes);
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
        var error = new StringWriter();
        var original = Console.Error;
        Console.SetError(error);
        try
        {
            var exitCode = Tl.Bake.Program.Main([]);
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.Equal(
            string.Join("\n",
                "Usage: tlb <input.json> <output.tlb> [--assembly <path>]... [--cache <dir>]",
                "       tlb --strip <input.tlb> <output.tlb>",
                "       tlb --report <input.tlb>",
                "       tlb --json --assembly <path>...",
                "       tlb --watch <input.json> <output.tlb> [--assembly <path>]... [--debounce <ms>]",
                ""),
            error.ToString().Replace("\r\n", "\n"));
    }
}
