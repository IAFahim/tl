using System;
using System.IO;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class CliRun : IDisposable
{
    private readonly string _root;

    public CliRun()
    {
        _root = Path.Combine(Path.GetTempPath(), "tlbake_cache_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        JsonPath = Path.Combine(_root, "input.json");
        TlbPath = Path.Combine(_root, "out.tlb");
        CacheDir = Path.Combine(_root, "cache");
    }

    public string JsonPath { get; }
    public string TlbPath { get; }
    public string CacheDir { get; }

    public void WriteJson(string json) => File.WriteAllText(JsonPath, json);

    public string AssemblyPath(string name)
    {
        var path = Path.Combine(_root, name);
        File.Copy(typeof(Tlb.AlphaTrack).Assembly.Location, path, true);
        return path;
    }

    public (int Exit, string StdOut) Run(params string[] args)
    {
        var original = Console.Out;
        using var capture = new StringWriter();
        try
        {
            Console.SetOut(capture);
            var exit = Tl.Bake.Program.Main(args);
            return (exit, capture.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    public byte[] KeyBytes(string assemblyPath) =>
        BakeCacheKey.Compute(File.ReadAllBytes(JsonPath), [File.ReadAllBytes(assemblyPath)], strip: false);

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}

public class CacheCliTests
{
    private const string SimpleJson = """
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

    [Fact]
    public void FirstRunMisses_SecondIdenticalRunHits_WithoutRewritingBytes()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var asm = run.AssemblyPath("consumer.dll");

        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, first.Exit);
        var key = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.Contains($"cache: miss {key}", first.StdOut);

        var tlbBytes = File.ReadAllBytes(run.TlbPath);
        var cachedTlb = Path.Combine(run.CacheDir, Convert.ToHexString(run.KeyBytes(asm)) + ".tlb");
        Assert.Equal(tlbBytes, File.ReadAllBytes(cachedTlb));

        var preservedTlb = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(run.TlbPath, preservedTlb);

        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, second.Exit);
        Assert.Contains($"cache: hit {key}", second.StdOut);
        Assert.DoesNotContain("cache: miss", second.StdOut);

        Assert.Equal(tlbBytes, File.ReadAllBytes(run.TlbPath));
        Assert.Equal(preservedTlb, File.GetLastWriteTimeUtc(run.TlbPath));
    }

    [Fact]
    public void HitWithoutKernelDestination_StillRewritesMissingOutputs()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var asm = run.AssemblyPath("consumer.dll");
        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, first.Exit);

        File.Delete(run.TlbPath);
        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, second.Exit);
        var key = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.Contains($"cache: hit {key}", second.StdOut);
        Assert.Equal(TimelineBaker.BakeJson(SimpleJson), File.ReadAllBytes(run.TlbPath));
    }

    [Fact]
    public void ChangedInputJson_Misses()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var asm = run.AssemblyPath("consumer.dll");
        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, first.Exit);
        var firstKey = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.Contains($"cache: miss {firstKey}", first.StdOut);

        run.WriteJson(SimpleJson.Replace("\"duration\": 10", "\"duration\": 12"));
        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, second.Exit);
        var secondKey = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.NotEqual(firstKey, secondKey);
        Assert.Contains($"cache: miss {secondKey}", second.StdOut);
    }

    [Fact]
    public void ChangedAssemblyContent_Misses()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var asm = run.AssemblyPath("consumer.dll");
        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, first.Exit);
        var firstKey = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.Contains($"cache: miss {firstKey}", first.StdOut);

        File.WriteAllBytes(asm, [.. File.ReadAllBytes(asm), (byte)' ']);
        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.Equal(0, second.Exit);
        var secondKey = BakeCacheKey.Prefix(run.KeyBytes(asm));
        Assert.NotEqual(firstKey, secondKey);
        Assert.Contains($"cache: miss {secondKey}", second.StdOut);
    }

    [Fact]
    public void SameAssemblyContent_UnderDifferentPath_Hits()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", run.AssemblyPath("one.dll"), "--cache", run.CacheDir);
        Assert.Equal(0, first.Exit);
        Assert.Contains("cache: miss", first.StdOut);

        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", run.AssemblyPath("two.dll"), "--cache", run.CacheDir);
        Assert.Equal(0, second.Exit);
        Assert.Contains("cache: hit", second.StdOut);
    }

    [Fact]
    public void Cache_NeverMasksABakeFailure()
    {
        using var run = new CliRun();
        run.WriteJson("""
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
        """);
        var asm = run.AssemblyPath("consumer.dll");

        var first = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.NotEqual(0, first.Exit);
        Assert.DoesNotContain("cache:", first.StdOut);
        Assert.False(File.Exists(run.TlbPath));
        Assert.False(Directory.Exists(run.CacheDir) && Directory.EnumerateFiles(run.CacheDir).Any());

        var second = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm, "--cache", run.CacheDir);
        Assert.NotEqual(0, second.Exit);
        Assert.DoesNotContain("cache:", second.StdOut);
    }

    [Fact]
    public void WithoutCacheFlag_NoCacheLinesArePrinted()
    {
        using var run = new CliRun();
        run.WriteJson(SimpleJson);
        var asm = run.AssemblyPath("consumer.dll");
        var result = run.Run(run.JsonPath, run.TlbPath, "--assembly", asm);
        Assert.Equal(0, result.Exit);
        Assert.DoesNotContain("cache:", result.StdOut);
        Assert.True(File.Exists(run.TlbPath));
    }
}
