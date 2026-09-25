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

    const string NestedAutoJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "type": "HostedTrack",
          "data": { "Code": 1 },
          "clips": [ { "type": "HostedClip", "start": 0, "end": 10, "data": { "Value": 5 } } ]
        }
      ]
    }
    """;

    const string EmptyNamespaceJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "namespace": "",
          "type": "GaGlobalTrack",
          "data": { "Scale": 1.5 },
          "clips": [ { "namespace": "", "type": "GaGlobalClip", "start": 0, "end": 10, "data": { "Amount": 2 } } ]
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

    static void WriteMetadatalessPe(string path)
    {
        const int peOffset = 0x80;
        const int pe32PlusMagic = 0x20b;
        const int amd64 = 0x8664;
        const int optionalHeaderSize = 0xf0;
        var image = new byte[0x400];
        image[0] = (byte)'M';
        image[1] = (byte)'Z';
        BitConverter.TryWriteBytes(image.AsSpan(0x3c), peOffset);
        image[peOffset] = (byte)'P';
        image[peOffset + 1] = (byte)'E';
        BitConverter.TryWriteBytes(image.AsSpan(peOffset + 4), (ushort)amd64);
        BitConverter.TryWriteBytes(image.AsSpan(peOffset + 20), (ushort)optionalHeaderSize);
        BitConverter.TryWriteBytes(image.AsSpan(peOffset + 24), (ushort)pe32PlusMagic);
        File.WriteAllBytes(path, image);
    }

    void CopyFixtureDll(string directory)
    {
        Directory.CreateDirectory(directory);
        File.Copy(typeof(Tlb.AlphaTrack).Assembly.Location, Path.Combine(directory, "Tl.Bake.Tests.dll"));
        File.Copy(typeof(IBlend<>).Assembly.Location, Path.Combine(directory, Path.GetFileName(typeof(IBlend<>).Assembly.Location)));
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

    [Fact]
    public void NestedTrackType_AutoDiscovers_AndBakes()
    {
        File.WriteAllText(Path.Combine(_root, "nested.json"), NestedAutoJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        var result = Run("nested.json", "nested.tlb", "--auto");
        Assert.Equal(0, result.Exit);
        Assert.Contains("assembly:", result.Stdout);
        Assert.True(File.Exists(Path.Combine(_root, "nested.tlb")));
    }

    [Fact]
    public void EmptyExplicitNamespace_NeverMatchesGlobalTypes_InDiscovery()
    {
        File.WriteAllText(Path.Combine(_root, "empty.json"), EmptyNamespaceJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);
        var result = Run("empty.json", "empty.tlb");
        Assert.NotEqual(0, result.Exit);
        Assert.Contains("No dll under", result.Stderr);
        Assert.False(File.Exists(Path.Combine(_root, "empty.tlb")));
    }

    [Fact]
    public void MetadatalessAndGarbageDlls_AreSkipped_NotFatal()
    {
        File.WriteAllText(Path.Combine(_root, "game.json"), GameJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        WriteMetadatalessPe(Path.Combine(_root, "bin", "Release", "net10.0", "native.dll"));
        File.WriteAllBytes(Path.Combine(_root, "bin", "Release", "net10.0", "garbage.dll"), new byte[8192]);
        Directory.SetCurrentDirectory(_root);
        var result = Run("game.json");
        Assert.Equal(0, result.Exit);
        Assert.Contains("assembly:", result.Stdout);
        Assert.True(File.Exists(Path.Combine(_root, "game.tlb")));
    }
}
