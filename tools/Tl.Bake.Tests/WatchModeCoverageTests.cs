using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class WatchModeCoverageTests
{
    private static readonly string AssemblyPath = typeof(Tlb.JobTrack).Assembly.Location;

    private const string AlphaJson =
        """{"duration":20,"tracks":[{"namespace":"Tlb","type":"AlphaTrack","clips":[{"namespace":"Tlb","type":"AlphaClip","start":0,"end":20,"data":{"Value":4}}]}]}""";

    private static string NewDirectory() =>
        Path.Combine(Path.GetTempPath(), "tlb_watch_cov_" + Guid.NewGuid().ToString("N"));

    private static WatchEngine NewEngine(string input, string output, LineSink sink, int debounce = 25)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(input)!);
        File.WriteAllText(input, AlphaJson);
        return new WatchEngine(
            [new WatchedFile(input, output)],
            [AssemblyPath],
            debounce,
            sink);
    }

    private static string SilencedJson() =>
        AlphaJson.Replace("\"Value\":4", "\"Value\":6");

    [Fact]
    public void EventsForUnknownPaths_AreIgnored()
    {
        var directory = NewDirectory();
        try
        {
            var input = Path.Combine(directory, "alpha.json");
            var sink = new LineSink();
            var engine = NewEngine(input, Path.Combine(directory, "alpha.tlb"), sink);
            engine.InitialBake();
            sink.Clear();

            engine.OnEvent(Path.Combine(directory, "other.json"));
            engine.Pump(DateTimeOffset.UtcNow.AddSeconds(1));

            Assert.Empty(sink.Kinds());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DeletedEvent_ForgetsTheHash_AndRewriteRebakesWithoutRewritingOutput()
    {
        var directory = NewDirectory();
        try
        {
            var input = Path.Combine(directory, "alpha.json");
            var output = Path.Combine(directory, "nested", "alpha.tlb");
            var sink = new LineSink();
            Directory.CreateDirectory(directory);
            File.WriteAllText(input, AlphaJson);
            var engine = new WatchEngine(
                [new WatchedFile(input, output)],
                [AssemblyPath],
                25,
                sink);
            engine.InitialBake();
            var baked = File.ReadAllBytes(output);
            sink.Clear();

            engine.OnDeleted(input);
            engine.OnEvent(input);
            engine.Pump(DateTimeOffset.UtcNow.AddSeconds(1));

            Assert.Equal(["rebuild"], sink.Kinds());
            Assert.Equal(baked, File.ReadAllBytes(output));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Pump_AfterInputDisappears_EmitsNoEvents()
    {
        var directory = NewDirectory();
        try
        {
            var input = Path.Combine(directory, "alpha.json");
            var sink = new LineSink();
            var engine = NewEngine(input, Path.Combine(directory, "alpha.tlb"), sink);
            engine.InitialBake();
            sink.Clear();

            File.Delete(input);
            engine.OnEvent(input);
            engine.Pump(DateTimeOffset.UtcNow.AddSeconds(1));

            Assert.Empty(sink.Kinds());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MultiplePendingInputs_AreBakedInOrdinalOrder()
    {
        var directory = NewDirectory();
        try
        {
            var alpha = Path.Combine(directory, "alpha.json");
            var beta = Path.Combine(directory, "beta.json");
            Directory.CreateDirectory(directory);
            File.WriteAllText(alpha, AlphaJson);
            File.WriteAllText(beta, AlphaJson);
            var sink = new LineSink();
            var engine = new WatchEngine(
                [new WatchedFile(alpha, Path.Combine(directory, "alpha.tlb")),
                 new WatchedFile(beta, Path.Combine(directory, "beta.tlb"))],
                [AssemblyPath],
                25,
                sink);

            engine.OnEvent(alpha);
            engine.OnEvent(beta);
            engine.Pump(DateTimeOffset.UtcNow.AddSeconds(1));

            var rebuiltInputs = sink.JsonLines()
                .Where(document => document.RootElement.GetProperty("event").GetString() == "rebuild")
                .Select(document => Path.GetFileName(document.RootElement.GetProperty("input").GetString()!))
                .ToArray();
            Assert.Equal(["alpha.json", "beta.json"], rebuiltInputs);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ResolveFiles_DirectoryForms()
    {
        var directory = NewDirectory();
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "a.json"), AlphaJson);
            File.WriteAllText(Path.Combine(directory, "b.json"), AlphaJson);
            var outputFile = Path.Combine(directory, "occupied.tlb");
            File.WriteAllText(outputFile, AlphaJson);

            Assert.Throws<WatchUsageException>(() => WatchMode.ResolveFiles(directory, outputFile));

            var nested = Path.Combine(directory, "nested");
            var files = WatchMode.ResolveFiles(directory, nested);
            Assert.Equal(["a.json", "b.json"], files.Select(file => Path.GetFileName(file.Input)).ToArray());
            Assert.All(files, file => Assert.Equal(".tlb", Path.GetExtension(file.Output)));
            Assert.True(Directory.Exists(nested));

            var empty = Path.Combine(directory, "empty");
            Directory.CreateDirectory(empty);
            Assert.Throws<WatchUsageException>(() => WatchMode.ResolveFiles(empty, null));

            Assert.Throws<WatchUsageException>(() => WatchMode.ResolveFiles(
                Path.Combine(directory, "a.json"), directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Run_ArgumentErrors_ExitNonZero()
    {
        var directory = NewDirectory();
        try
        {
            Directory.CreateDirectory(directory);
            var input = Path.Combine(directory, "alpha.json");
            File.WriteAllText(input, AlphaJson);
            var output = Path.Combine(directory, "alpha.tlb");
            var sink = new LineSink();

            Assert.Equal(1, WatchMode.Run(["--watch", input, output, "--assembly"], sink));
            Assert.Equal(1, WatchMode.Run(["--watch", input, output, "extra"], sink));
            Assert.Equal(1, WatchMode.Run(["--watch", input, output, "--debounce", "-5"], sink));
            Assert.Empty(sink.Kinds());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RealWatcher_DeletedAndRecreatedInput_DeletesThenRebuilds()
    {
        var directory = NewDirectory();
        try
        {
            Directory.CreateDirectory(directory);
            var alpha = Path.Combine(directory, "alpha.json");
            File.WriteAllText(alpha, AlphaJson);
            var sink = new LineSink();
            var stop = false;
            using var session = StartSession(directory, sink, () => stop);

            Assert.True(sink.WaitFor(() => sink.Kinds().Count(kind => kind == "ready") >= 1));
            Assert.True(sink.WaitFor(() => sink.JsonLines().Any(document =>
                document.RootElement.GetProperty("event").GetString() == "rebuild" &&
                document.RootElement.GetProperty("input").GetString()!.EndsWith("alpha.json"))));
            sink.Clear();

            File.Delete(alpha);
            Thread.Sleep(250);
            File.WriteAllText(alpha, SilencedJson());
            Assert.True(sink.WaitFor(
                () => sink.JsonLines().Any(document =>
                    document.RootElement.GetProperty("event").GetString() == "rebuild" &&
                    document.RootElement.GetProperty("input").GetString()!.EndsWith("alpha.json")),
                TimeSpan.FromSeconds(30)));
            Assert.Equal(1, sink.JsonLines().Count(document =>
                document.RootElement.GetProperty("event").GetString() == "rebuild"));
            Assert.True(File.Exists(Path.Combine(directory, "alpha.tlb")));

            stop = true;
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private static IDisposable StartSession(string directory, LineSink sink, Func<bool> stopRequested)
    {
        var thread = new Thread(() => WatchMode.Run(
            ["--watch", directory, "--assembly", AssemblyPath, "--debounce", "10"],
            sink,
            cancelled: stopRequested))
        {
            IsBackground = true,
        };
        thread.Start();
        return new ThreadStopper(thread);
    }

    private sealed class ThreadStopper(Thread thread) : IDisposable
    {
        public void Dispose() => thread.Join(TimeSpan.FromSeconds(5));
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

        private IReadOnlyList<string> Lines => _lines.ToArray();

        public void Clear() => _lines.Clear();

        public IReadOnlyList<string> Kinds() => Lines
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("event").GetString()!)
            .ToArray();

        public IReadOnlyList<JsonDocument> JsonLines() => Lines
            .Select(line => JsonDocument.Parse(line))
            .ToArray();

        public bool WaitFor(Func<bool> condition, TimeSpan? timeout = null)
        {
            var elapsed = Stopwatch.StartNew();
            var limit = timeout ?? TimeSpan.FromSeconds(10);
            while (elapsed.Elapsed < limit)
            {
                if (condition())
                    return true;
                Thread.Sleep(10);
            }

            return condition();
        }
    }
}
