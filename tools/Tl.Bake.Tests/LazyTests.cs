using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tl.Bake.Tests;

public class LazyTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "tlb_lazy_" + Guid.NewGuid().ToString("N"));
    readonly string _previous = Directory.GetCurrentDirectory();
    readonly TextWriter _out = Console.Out;
    readonly TextWriter _error = Console.Error;

    public LazyTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        Console.SetOut(_out);
        Console.SetError(_error);
        Directory.SetCurrentDirectory(_previous);
        Directory.Delete(_root, recursive: true);
    }

    const string GameJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "data": { "Code": 1 },
          "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": 5 } } ]
        }
      ]
    }
    """;

    (string Stdout, string Stderr, int Exit) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        var exit = Program.Main(args);
        return (stdout.ToString(), stderr.ToString(), exit);
    }

    void CopyFixtureDll(string directory)
    {
        Directory.CreateDirectory(directory);
        File.Copy(typeof(Tlb.AlphaTrack).Assembly.Location, Path.Combine(directory, "Tl.Bake.Tests.dll"));
        File.Copy(typeof(Tl.IBlend<>).Assembly.Location, Path.Combine(directory, Path.GetFileName(typeof(Tl.IBlend<>).Assembly.Location)));
    }

    [Fact]
    public void JsonOnly_DisoversAssembly_AndBakes()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        var result = Run("game.json");
        Assert.Equal(0, result.Exit);
        Assert.Contains("assembly:", result.Stdout);
        Assert.True(File.Exists(Path.Combine(_root, "game.tlb")));
        Assert.True(File.Exists(Path.Combine(_root, "tlb.db")));
    }

    [Fact]
    public void NoArgs_SingleJson_Bakes()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        var result = Run();
        Assert.Equal(0, result.Exit);
        Assert.True(File.Exists(Path.Combine(_root, "game.tlb")));
    }

    [Fact]
    public void NoArgs_MultipleJsons_FailsWithCandidates()
    {
        File.WriteAllText(Path.Combine(_root, "a.json"), GameJson);
        File.WriteAllText(Path.Combine(_root, "b.json"), GameJson);
        Directory.SetCurrentDirectory(_root);
        var result = Run();
        Assert.NotEqual(0, result.Exit);
        Assert.Contains("2 *.json files", result.Stderr);
        Assert.Contains("Fix: name one", result.Stderr);
    }

    [Fact]
    public void SecondRun_UsesTlbDbCache()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        Assert.Equal(0, Run("game.json").Exit);
        var second = Run("game.json");
        Assert.Equal(0, second.Exit);
        Assert.Contains("cached in tlb.db", second.Stdout);
    }

    [Fact]
    public void StaleCache_RediscoversMovedAssembly()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        Assert.Equal(0, Run("game.json").Exit);
        Directory.Delete(Path.Combine(_root, "bin"), recursive: true);
        CopyFixtureDll(Path.Combine(_root, "dist", "Release", "net10.0"));
        var rediscovered = Run("game.json");
        Assert.Equal(0, rediscovered.Exit);
        Assert.Contains("found by scanning", rediscovered.Stdout);
        Assert.Contains("dist", rediscovered.Stdout);
    }

    [Fact]
    public void AmbiguousDlls_FailsWithExplicitFix()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        CopyFixtureDll(Path.Combine(_root, "dist", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        var result = Run("game.json");
        Assert.NotEqual(0, result.Exit);
        Assert.Contains("2 dlls define every type", result.Stderr);
        Assert.Contains("Fix: pass one explicitly", result.Stderr);
    }

    [Fact]
    public void NoDllAnywhere_FailsWithTypeList()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        Directory.SetCurrentDirectory(_root);
        var result = Run("game.json");
        Assert.NotEqual(0, result.Exit);
        Assert.Contains("No dll under", result.Stderr);
        Assert.Contains("Tlb.AlphaTrack", result.Stderr);
    }

    [Fact]
    public void ExplicitAssembly_SkipsDiscoveryAndCache()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        var assembly = typeof(Tlb.AlphaTrack).Assembly.Location;
        Directory.SetCurrentDirectory(_root);
        var result = Run("game.json", "game.tlb", "--assembly", assembly);
        Assert.Equal(0, result.Exit);
        Assert.DoesNotContain("assembly:", result.Stdout);
        Assert.False(File.Exists(Path.Combine(_root, "tlb.db")));
        Assert.True(File.Exists(Path.Combine(_root, "game.tlb")));
    }
}
