using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class AutoNamespaceTests
{
    private static readonly BakerAssemblyResolver Resolver =
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    private const string ExplicitJson = """
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

    private const string AbsentJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "type": "AlphaTrack",
          "data": { "Code": 1 },
          "clips": [ { "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": 5 } } ]
        }
      ]
    }
    """;

    private const string GlobalJson = """
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

    [Fact]
    public void AbsentNamespaces_AutoBake_IsByteIdenticalToExplicitBake()
    {
        var explicitBytes = TimelineBaker.BakeJson(ExplicitJson, Resolver);
        var autoBytes = TimelineBaker.BakeJson(AbsentJson, Resolver, autoNamespace: true);

        Assert.Equal(explicitBytes, autoBytes);
    }

    [Fact]
    public void AbsentNamespaces_WithoutAuto_KeepsTheNeedsNamespaceDiagnostic()
    {
        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(AbsentJson, Resolver));

        Assert.Contains("missing required property: track 0 needs 'namespace'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousTrackBareName_ListsEveryCandidateAndTheRepair()
    {
        const string json = """
        {
          "duration": 10,
          "tracks": [
            { "type": "TwinTrack", "clips": [ { "namespace": "TwinA", "type": "TwinClip", "start": 0, "end": 10, "data": { "Value": 3 } } ] }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver, autoNamespace: true));

        Assert.Contains("ambiguous bare name: 'TwinTrack' matches 2 loaded types for track 0", ex.Message, StringComparison.Ordinal);
        Assert.Contains("(TwinA, TwinTrack) in Tl.Bake.Tests", ex.Message, StringComparison.Ordinal);
        Assert.Contains("(TwinB, TwinTrack) in Tl.Bake.Tests", ex.Message, StringComparison.Ordinal);
        Assert.Contains("write the 'namespace' explicitly", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousClipBareName_ListsEveryCandidateAndTheRepair()
    {
        const string json = """
        {
          "duration": 10,
          "tracks": [
            { "namespace": "TwinA", "type": "TwinTrack", "clips": [ { "type": "TwinClip", "start": 0, "end": 10, "data": { "Value": 3 } } ] }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver, autoNamespace: true));

        Assert.Contains("ambiguous bare name: 'TwinClip' matches 2 loaded types for clip 0 on track 0", ex.Message, StringComparison.Ordinal);
        Assert.Contains("(TwinA, TwinClip) in Tl.Bake.Tests", ex.Message, StringComparison.Ordinal);
        Assert.Contains("(TwinB, TwinClip) in Tl.Bake.Tests", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownBareName_StatesTheAutoRule()
    {
        const string json = """
        {
          "duration": 10,
          "tracks": [
            { "type": "GhostTrack", "clips": [ { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10 } ] }
          ]
        }
        """;

        var ex = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(json, Resolver, autoNamespace: true));

        Assert.Contains("unknown bare name: no loaded type named 'GhostTrack' for track 0", ex.Message, StringComparison.Ordinal);
        Assert.Contains("--auto fills a missing 'namespace' only when exactly one loaded type carries that bare name", ex.Message, StringComparison.Ordinal);
        Assert.Contains("write the 'namespace' explicitly", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitNamespace_AutoBake_MatchesBakeWithoutAuto()
    {
        Assert.Equal(
            TimelineBaker.BakeJson(ExplicitJson, Resolver),
            TimelineBaker.BakeJson(ExplicitJson, Resolver, autoNamespace: true));
    }

    [Fact]
    public void ExplicitEmptyNamespace_KeepsGlobalMeaningUnderAuto()
    {
        Assert.Equal(
            TimelineBaker.BakeJson(GlobalJson, Resolver),
            TimelineBaker.BakeJson(GlobalJson, Resolver, autoNamespace: true));
    }

    [Fact]
    public void LateExplicitNamespace_WinsOverInference_RegardlessOfOrder()
    {
        const string late = """
        {
          "duration": 10,
          "tracks": [
            {
              "type": "TwinTrack",
              "clips": [ { "namespace": "TwinA", "type": "TwinClip", "start": 0, "end": 10, "data": { "Value": 3 } } ],
              "namespace": "TwinA"
            }
          ]
        }
        """;
        const string first = """
        {
          "duration": 10,
          "tracks": [
            {
              "namespace": "TwinA",
              "type": "TwinTrack",
              "clips": [ { "namespace": "TwinA", "type": "TwinClip", "start": 0, "end": 10, "data": { "Value": 3 } } ]
            }
          ]
        }
        """;

        Assert.Equal(
            TimelineBaker.BakeJson(first, Resolver, autoNamespace: true),
            TimelineBaker.BakeJson(late, Resolver, autoNamespace: true));
    }
}

public class AutoLazyDiscoveryTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "tlb_lazy_auto_" + Guid.NewGuid().ToString("N"));
    readonly string _previous = Directory.GetCurrentDirectory();
    readonly TextWriter _out = Console.Out;
    readonly TextWriter _error = Console.Error;

    public AutoLazyDiscoveryTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        Console.SetOut(_out);
        Console.SetError(_error);
        Directory.SetCurrentDirectory(_previous);
        Directory.Delete(_root, recursive: true);
    }

    const string AutoJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "type": "AutoTrack",
          "data": { "Code": 1 },
          "clips": [ { "type": "AutoClip", "start": 0, "end": 10, "data": { "Value": 5 } } ]
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

    void Equal(int expected, int actual, string stderr) =>
        Assert.True(expected == actual, $"exit {actual} (expected {expected}): {stderr}");

    void CopyFixtureDll(string directory)
    {
        Directory.CreateDirectory(directory);
        File.Copy(typeof(Tlb.AlphaTrack).Assembly.Location, Path.Combine(directory, "Tl.Bake.Tests.dll"));
        File.Copy(typeof(Tl.IBlend<>).Assembly.Location, Path.Combine(directory, Path.GetFileName(typeof(Tl.IBlend<>).Assembly.Location)));
    }

    [Fact]
    public void AbsentNamespaces_AutoDiscoveryFindsTheDllByBareName_AndCaches()
    {
        File.WriteAllText(Path.Combine(_root, "auto.json"), AutoJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);

        var first = Run("auto.json", "--auto");
        Equal(0, first.Exit, first.Stderr);
        Assert.Contains("assembly:", first.Stdout);
        Assert.True(File.Exists(Path.Combine(_root, "auto.tlb")));

        var second = Run("auto.json", "--auto");
        Equal(0, second.Exit, second.Stderr);
        Assert.Contains("cached in tlb.db", second.Stdout);
    }

    [Fact]
    public void AbsentNamespaces_WithoutAuto_DiscoveryReportsTheAutoPairs()
    {
        File.WriteAllText(Path.Combine(_root, "auto.json"), AutoJson);
        CopyFixtureDll(Path.Combine(_root, "bin", "Release", "net10.0"));
        Directory.SetCurrentDirectory(_root);

        var result = Run("auto.json");
        Assert.NotEqual(0, result.Exit);
        Assert.Contains("No dll under", result.Stderr);
        Assert.Contains("<auto>.AutoTrack", result.Stderr);
        Assert.Contains("<auto>.AutoClip", result.Stderr);
    }
}

public class AutoCliTests
{
    private static readonly string AssemblyPath = typeof(Tlb.AlphaTrack).Assembly.Location;

    private const string AutoJson = """
    {
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "type": "AutoTrack",
          "data": { "Code": 1 },
          "clips": [ { "type": "AutoClip", "start": 0, "end": 10, "data": { "Value": 5 } } ]
        }
      ]
    }
    """;

    [Fact]
    public void Cli_AutoFlagAnywhere_BakesAbsentNamespaceJson()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_auto_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "auto.json");
            var tlbPath = Path.Combine(tempDir, "auto.tlb");
            File.WriteAllText(jsonPath, AutoJson);

            var stderr = new StringWriter();
            var previousError = Console.Error;
            Console.SetError(stderr);
            int exitCode;
            try
            {
                exitCode = Tl.Bake.Program.Main(["--auto", jsonPath, tlbPath, "--assembly", AssemblyPath]);
            }
            finally
            {
                Console.SetError(previousError);
            }
            Assert.True(exitCode == 0, $"exit {exitCode}: {stderr}");
            Assert.True(File.Exists(tlbPath));
            var expected = TimelineBaker.BakeJson(
                AutoJson,
                BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]),
                autoNamespace: true);
            Assert.Equal(expected, File.ReadAllBytes(tlbPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Watch_AutoFlag_BakesAbsentNamespaceJson()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_auto_watch_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var sink = new LineSink();
        try
        {
            var inputPath = Path.Combine(tempDir, "auto.json");
            var outputPath = Path.Combine(tempDir, "auto.tlb");
            File.WriteAllText(inputPath, AutoJson);

            var exitCode = Tl.Bake.WatchMode.Run(
                ["--watch", inputPath, outputPath, "--assembly", AssemblyPath, "--auto", "--debounce", "5"],
                sink,
                cancelled: () => true);

            Assert.True(exitCode == 0, $"exit {exitCode}: {string.Join(" | ", sink.Lines)}");
            Assert.True(
                sink.Kinds().SequenceEqual(new[] { "ready", "rebuild" }),
                $"events: {string.Join(" | ", sink.Lines)}");
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private sealed class LineSink : TextWriter
    {
        private readonly ConcurrentQueue<string> _lines = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (value != null)
                _lines.Enqueue(value);
        }

        public override void Write(char value)
        {
        }

        public override void Flush()
        {
        }

        public IReadOnlyList<string> Lines => _lines.ToArray();

        public List<string> Kinds() => _lines
            .Select(line => System.Text.Json.JsonDocument.Parse(line).RootElement.GetProperty("event").GetString()!)
            .ToList();
    }
}
