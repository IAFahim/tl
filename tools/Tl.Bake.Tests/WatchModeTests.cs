using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tl.Bake.Tests;

public class WatchModeTests
{
    private static readonly string AssemblyPath = typeof(Tlb.JobTrack).Assembly.Location;

    private static string ValidJson =>
        """
        {
          "duration": 10,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "JobTrack",
              "data": { "Multiplier": 2 },
              "clips": [
                { "namespace": "Tlb", "type": "JobClip", "start": 0, "end": 10, "data": { "Amount": 5, "Steps": 1 } }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void InitialBake_EmitsReadyAndRebuildAndWritesOutput()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        Assert.True(scope.OutputExists);
        Assert.Equal(new[] { "ready", "rebuild" }, scope.EventKinds());
    }

    [Fact]
    public void ChangedContent_Rebuilds()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        scope.ClearLines();

        scope.WriteInput(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 4"));
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));

        Assert.Equal(new[] { "rebuild" }, scope.EventKinds());
    }

    [Fact]
    public void ContentIdenticalRewrite_SkipsRebuild()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        scope.ClearLines();

        scope.WriteInput(ValidJson);
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));

        Assert.Equal(new[] { "skip" }, scope.EventKinds());
    }

    [Fact]
    public void DebounceWindow_HoldsEventsUntilQuiet()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        scope.ClearLines();

        scope.WriteInput(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 4"));
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds - 1));
        Assert.Empty(scope.EventKinds());

        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));
        Assert.Equal(new[] { "rebuild" }, scope.EventKinds());
    }

    [Fact]
    public void InvalidJson_EmitsDiagnosticWithLineColumn_AndKeepsPreviousOutput()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        var outputBefore = File.ReadAllBytes(scope.OutputPath);
        scope.ClearLines();

        scope.WriteInput(ValidJson.Replace("\"duration\": 10,", "\"duration\": 10, \"duration\": 11,"));
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));

        Assert.Equal(new[] { "diagnostic" }, scope.EventKinds());
        var message = scope.JsonLines().Single().RootElement.GetProperty("message").GetString()!;
        Assert.Matches(@"^\[\d+:\d+\] .+", message);
        Assert.Equal(outputBefore, File.ReadAllBytes(scope.OutputPath));
    }

    [Fact]
    public void RepeatedEventsForSameFile_CoalesceIntoOneDecision()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        scope.ClearLines();

        scope.WriteInput(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 4"));
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));

        Assert.Equal(new[] { "rebuild" }, scope.EventKinds());
    }

    [Fact]
    public void EventLines_AreVersionedJsonWithStableKeyOrder()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        scope.Engine.InitialBake();
        scope.ClearLines();

        scope.WriteInput(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 4"));
        scope.Engine.OnEvent(scope.InputPath);
        scope.Engine.Pump(scope.Now(WatchScope.DebounceMilliseconds + 1));

        Assert.Single(scope.JsonLines());
        foreach (var document in scope.JsonLines())
        {
            var properties = document.RootElement.EnumerateObject().ToArray();
            Assert.Equal("schemaVersion", properties[0].Name);
            Assert.Equal(1, properties[0].Value.GetInt32());
            Assert.Equal("event", properties[1].Name);
        }
    }

    [Fact]
    public void RealWatcher_RenameOverInput_RebuildsAndSkipsIdenticalRewrite()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        var stop = false;
        using var session = scope.StartBackgroundSession(() => stop);

        Assert.True(scope.WaitFor(() => scope.EventKinds().Contains("ready")));
        Assert.True(scope.WaitFor(() => scope.EventKinds().Contains("rebuild")));
        scope.ClearLines();

        var changedBytes = Encoding.UTF8.GetBytes(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 4"));
        scope.RenameOverInput(changedBytes);
        Assert.True(scope.WaitFor(() => scope.EventKinds().Contains("rebuild"), TimeSpan.FromSeconds(20)));
        Assert.Equal(1, scope.EventKinds().Count(kind => kind == "rebuild"));
        scope.ClearLines();

        scope.RenameOverInput(changedBytes);
        Assert.True(scope.WaitFor(() => scope.EventKinds().Contains("skip"), TimeSpan.FromSeconds(20)));
        Assert.Equal(0, scope.EventKinds().Count(kind => kind == "rebuild"));
        scope.ClearLines();

        var secondBytes = Encoding.UTF8.GetBytes(ValidJson.Replace("\"Multiplier\": 2", "\"Multiplier\": 7"));
        scope.RenameOverInput(secondBytes);
        Assert.True(scope.WaitFor(() => scope.EventKinds().Contains("rebuild"), TimeSpan.FromSeconds(20)));
        Assert.Equal(1, scope.EventKinds().Count(kind => kind == "rebuild"));
        stop = true;
    }

    [Fact]
    public void WatchUsageErrors()
    {
        using var scope = WatchScope.Create();
        scope.WriteInput(ValidJson);
        var sink = new LineSink();
        Assert.NotEqual(0, WatchMode.Run(["--watch"], sink));
        Assert.NotEqual(0, WatchMode.Run(["--watch", scope.InputPath], sink));
        Assert.NotEqual(0, WatchMode.Run(["--watch", scope.MissingPath, scope.OutputPath], sink));
        Assert.NotEqual(0, WatchMode.Run(["--watch", scope.EmptyDirectory, "--debounce", "5"], sink));
        Assert.NotEqual(0, WatchMode.Run(["--watch", scope.InputPath, scope.OutputPath, "--debounce", "abc"], sink));
        Assert.NotEqual(0, WatchMode.Run(["--watch", scope.InputPath, scope.OutputPath, "--assembly", scope.MissingPath], sink));
        Assert.Equal(0, WatchMode.Run(
            ["--watch", scope.InputPath, scope.OutputPath, "--assembly", AssemblyPath, "--debounce", "5"],
            sink,
            cancelled: () => true));
        Assert.Equal(new[] { "ready", "rebuild" }, sink.Kinds());
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

        public void Clear() => _lines.Clear();

        public List<string> Kinds() => Lines
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("event").GetString()!)
            .ToList();
    }

    private sealed class WatchScope : IDisposable
    {
        public const int DebounceMilliseconds = 25;

        private readonly string _directory;
        private readonly LineSink _sink;
        private DateTimeOffset _clock = DateTimeOffset.UtcNow;

        private WatchScope()
        {
            _directory = Path.Combine(Path.GetTempPath(), "tlb_watch_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            EmptyDirectory = Path.Combine(_directory, "empty");
            Directory.CreateDirectory(EmptyDirectory);
            _sink = new LineSink();
            InputPath = Path.Combine(_directory, "alpha.json");
            OutputPath = Path.Combine(_directory, "alpha.tlb");
            MissingPath = Path.Combine(_directory, "missing.json");
            Engine = new WatchEngine(
                [new WatchedFile(InputPath, OutputPath)],
                [AssemblyPath],
                DebounceMilliseconds,
                _sink,
                () => _clock);
        }

        public WatchEngine Engine { get; }
        public string InputPath { get; }
        public string OutputPath { get; }
        public string MissingPath { get; }
        public string EmptyDirectory { get; }

        public static WatchScope Create() => new();

        public bool OutputExists => File.Exists(OutputPath);

        public void WriteInput(string json) => File.WriteAllText(InputPath, json);

        public void RenameOverInput(byte[] bytes)
        {
            var temporary = InputPath + ".tmp";
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, InputPath, overwrite: true);
        }

        public void ClearLines() => _sink.Clear();

        public IReadOnlyList<string> EventKinds() => _sink.Kinds();

        public IReadOnlyList<JsonDocument> JsonLines() => _sink.Lines
            .Select(line => JsonDocument.Parse(line))
            .ToList();

        public DateTimeOffset Now(int offsetMilliseconds) => _clock.AddMilliseconds(offsetMilliseconds);

        public IDisposable StartBackgroundSession(Func<bool> stopRequested)
        {
            ClearLines();
            var thread = new Thread(() => WatchMode.Run(
                ["--watch", InputPath, OutputPath, "--assembly", AssemblyPath, "--debounce", "10"],
                _sink,
                cancelled: () => stopRequested()))
            {
                IsBackground = true,
            };
            thread.Start();
            return new ThreadStopper(thread);
        }

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

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private sealed class ThreadStopper(Thread thread) : IDisposable
        {
            public void Dispose() => thread.Join(TimeSpan.FromSeconds(5));
        }
    }
}
