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
            var exitCode = Program.Main([jsonPath, tlbPath, "--assembly", asmPath]);
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
    public void Cli_WithDatalessClip_ReturnsZeroAndBakesDefaultPayload()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "dataless.json");
            var tlbPath = Path.Combine(tempDir, "dataless.tlb");

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
                    { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(jsonPath, json);

            var asmPath = typeof(Tlb.AlphaTrack).Assembly.Location;
            var exitCode = Program.Main([jsonPath, tlbPath, "--assembly", asmPath]);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tlbPath));
            var bytes = File.ReadAllBytes(tlbPath);
            Assert.True(TlbMetadata.HasMetadata(bytes));
            Assert.Equal(TimelineBaker.BakeJson(json), bytes);
            Assert.Equal(TimelineBaker.BakeJson(json.Replace("\"end\": 10 }", "\"end\": 10, \"data\": {} }")), bytes);
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
    public void Cli_NoInputInEmptyDirectory_TeachesTheLazyFix()
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
            var exitCode = Program.Main([]);
            Assert.NotEqual(0, exitCode);
            Assert.Contains("No input given and no *.json found", stderr.ToString());
            Assert.Contains("Fix: pass an input, e.g. 'tlb jump.json'.", stderr.ToString());
        }
        finally
        {
            Console.SetError(original);
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Cli_FlagWithoutInput_PrintsUsage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var stderr = new StringWriter();
        var original = Console.Error;
        try
        {
            Console.SetError(stderr);
            var exitCode = Program.Main(["--cache", tempDir]);
            Assert.Equal(1, exitCode);
            Assert.Equal(
                string.Join("\n",
                    "Usage: tlb [input.json [output.tlb]] [--assembly <path>]... [--cache <dir>] [--auto]",
                    "       tlb --strip <input.tlb> <output.tlb>",
                    "       tlb --report <input.tlb>",
                    "       tlb --json --assembly <path>...",
                    "       tlb --watch <input.json> <output.tlb> [--assembly <path>]... [--debounce <ms>] [--auto]",
                    "With only an input, output defaults beside it and the assembly is discovered from the JSON's types.",
                    ""),
                stderr.ToString().Replace("\r\n", "\n"));
        }
        finally
        {
            Console.SetError(original);
            Directory.Delete(tempDir, true);
        }
    }
}
