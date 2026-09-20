using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal sealed record WatchedFile(string Input, string Output);

internal sealed class WatchEngine
{
    private readonly IReadOnlyList<WatchedFile> _files;
    private readonly int _debounceMilliseconds;
    private readonly bool _autoNamespace;
    private readonly TextWriter _events;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Dictionary<string, WatchedFile> _byInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _hashes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _pending = new(StringComparer.Ordinal);
    private readonly BakerAssemblyResolver _resolver;

    public WatchEngine(
        IReadOnlyList<WatchedFile> files,
        string[] assemblyPaths,
        int debounceMilliseconds,
        TextWriter events,
        Func<DateTimeOffset>? clock = null,
        bool autoNamespace = false)
    {
        _files = files;
        _debounceMilliseconds = debounceMilliseconds;
        _autoNamespace = autoNamespace;
        _events = events;
        _clock = clock ?? DefaultClock;
        _resolver = new BakerAssemblyResolver(assemblyPaths);
        foreach (var file in files)
            _byInput[Path.GetFullPath(file.Input)] = file;
    }

    private static DateTimeOffset DefaultClock() => DateTimeOffset.UtcNow;

    public void Ready()
    {
        Emit("ready", writer => writer.WriteNumber("inputs", _files.Count));
    }

    public void InitialBake()
    {
        Ready();
        BakeFiles(_files);
    }

    public void OnEvent(string path)
    {
        var full = Path.GetFullPath(path);
        if (!_byInput.ContainsKey(full))
            return;
        _pending[full] = _clock();
    }

    public void OnDeleted(string path)
    {
        var full = Path.GetFullPath(path);
        _pending.Remove(full);
        _hashes.Remove(full);
    }

    public void Pump(DateTimeOffset now)
    {
        var ready = PendingReady(now);
        foreach (var path in ready)
            _pending.Remove(path);
        BakeFiles(ready.Select(path => _byInput[path]));
    }

    private List<string> PendingReady(DateTimeOffset now) =>
        _pending
            .Where(item => (now - item.Value).TotalMilliseconds >= _debounceMilliseconds)
            .Select(item => item.Key)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    private void BakeFiles(IEnumerable<WatchedFile> files)
    {
        var ordered = files.ToList();
        if (ordered.Count == 0)
            return;
        byte[]?[] inputs = new byte[ordered.Count][];
        var hashes = new string?[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
        {
            try
            {
                var bytes = File.ReadAllBytes(ordered[i].Input);
                inputs[i] = bytes;
                hashes[i] = HashOf(bytes);
            }
            catch (IOException)
            {
            }
        }

        var bakeIndexes = new List<int>();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (inputs[i] == null)
                continue;
            var file = ordered[i];
            if (_hashes.TryGetValue(file.Input, out var known) && known == hashes[i])
            {
                var hash = hashes[i]!;
                Emit("skip", writer =>
                {
                    writer.WriteString("input", file.Input);
                    writer.WriteString("hash", hash);
                });
                continue;
            }
            bakeIndexes.Add(i);
        }
        if (bakeIndexes.Count == 0)
            return;

        var outcomes = TimelineBaker.BakeBatch(
            bakeIndexes.Select(i => inputs[i]!).ToList(),
            _resolver,
            _autoNamespace);

        for (var k = 0; k < bakeIndexes.Count; k++)
        {
            var i = bakeIndexes[k];
            var file = ordered[i];
            var outcome = outcomes[k];
            if (outcome.Error != null)
            {
                _hashes[file.Input] = hashes[i]!;
                var message = outcome.Error.Message;
                Emit("diagnostic", writer =>
                {
                    writer.WriteString("input", file.Input);
                    writer.WriteString("message", message);
                });
                continue;
            }
            var writeStart = Stopwatch.GetTimestamp();
            WriteIfChanged(file.Output, outcome.Bytes!);
            _hashes[file.Input] = hashes[i]!;
            var input = file.Input;
            var output = file.Output;
            var hash = hashes[i]!;
            var duration = outcome.Ms + Stopwatch.GetElapsedTime(writeStart).TotalMilliseconds;
            Emit("rebuild", writer =>
            {
                writer.WriteString("input", input);
                writer.WriteString("output", output);
                writer.WriteString("hash", hash);
                writer.WriteNumber("durationMs", duration);
            });
        }
    }

    private static void WriteIfChanged(string outputPath, byte[] bytes)
    {
        if (File.Exists(outputPath) && bytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)))
            return;
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(outputPath, bytes);
    }

    private static string HashOf(byte[] bytes)
    {
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest, 0, 8).ToLowerInvariant();
    }

    private void Emit(string eventName, Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("event", eventName);
            write(writer);
            writer.WriteEndObject();
        }
        _events.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        _events.Flush();
    }
}

internal static class WatchMode
{
    public const int DefaultDebounceMilliseconds = 100;

    public static IReadOnlyList<WatchedFile> ResolveFiles(string input, string? output)
    {
        if (File.Exists(input))
        {
            if (output == null)
                throw new WatchUsageException($"Error: Watched input file '{input}' needs an output .tlb path.");
            if (Directory.Exists(output))
                throw new WatchUsageException($"Error: Output '{output}' is a directory; pass a .tlb file path.");
            return [new WatchedFile(Path.GetFullPath(input), Path.GetFullPath(output))];
        }

        if (Directory.Exists(input))
        {
            var outputDirectory = output ?? input;
            if (File.Exists(outputDirectory))
                throw new WatchUsageException($"Error: Watched output '{outputDirectory}' is a file; pass a directory.");
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            var files = Directory.EnumerateFiles(input, "*.json")
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new WatchedFile(
                    Path.GetFullPath(path),
                    Path.GetFullPath(Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(path) + ".tlb"))))
                .ToList();
            if (files.Count == 0)
                throw new WatchUsageException($"Error: No .json inputs found in directory '{input}'.");
            return files;
        }

        throw new WatchUsageException($"Error: Watched input '{input}' does not exist.");
    }

    public static int Run(
        string[] args,
        TextWriter events,
        Func<DateTimeOffset>? clock = null,
        Action<int>? sleep = null,
        Func<bool>? cancelled = null)
    {
        string? input = null;
        string? output = null;
        var debounce = DefaultDebounceMilliseconds;
        var autoNamespace = false;
        var assemblyPaths = new List<string>();

        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--assembly" || args[i] == "-a")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Error: Missing argument for --assembly");
                    return 1;
                }
                assemblyPaths.Add(args[++i]);
            }
            else if (args[i] == "--auto")
            {
                autoNamespace = true;
            }
            else if (args[i] == "--debounce")
            {
                if (i + 1 >= args.Length || !int.TryParse(args[++i], out debounce) || debounce < 0)
                {
                    Console.Error.WriteLine("Error: --debounce needs a non-negative integer millisecond value");
                    return 1;
                }
            }
            else if (input == null)
            {
                input = args[i];
            }
            else if (output == null)
            {
                output = args[i];
            }
            else
            {
                Console.Error.WriteLine($"Error: Unexpected argument '{args[i]}'");
                return 1;
            }
        }

        if (input == null)
        {
            Console.Error.WriteLine("Usage: tlb --watch <input.json> <output.tlb> [--assembly <path>]... [--debounce <ms>] [--auto]");
            Console.Error.WriteLine("       tlb --watch <input-dir> [<output-dir>] [--assembly <path>]... [--debounce <ms>] [--auto]");
            return 1;
        }

        IReadOnlyList<WatchedFile> files;
        try
        {
            files = ResolveFiles(input, output);
        }
        catch (WatchUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        foreach (var path in assemblyPaths)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Error: Referenced assembly file not found: '{path}'");
                return 1;
            }
        }

        var engine = new WatchEngine(files, [.. assemblyPaths], debounce, events, clock, autoNamespace);
        using var watchers = new WatcherSet(files, engine);
        engine.InitialBake();

        var cancelledByKey = false;
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancelledByKey = true;
        };

        var pause = Math.Clamp(debounce / 4, 10, 50);
        var sleepAction = sleep ?? Thread.Sleep;
        while (!cancelledByKey && (cancelled is null || !cancelled()))
        {
            engine.Pump(clock is null ? DateTimeOffset.UtcNow : clock());
            sleepAction(pause);
        }

        return 0;
    }

    private sealed class WatcherSet : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = [];

        public WatcherSet(IReadOnlyList<WatchedFile> files, WatchEngine engine)
        {
            foreach (var directory in files
                         .Select(file => Path.GetDirectoryName(Path.GetFullPath(file.Input))!)
                         .Distinct(StringComparer.Ordinal))
            {
                var watcher = new FileSystemWatcher(directory, "*.json");
                watcher.Created += (_, e) => engine.OnEvent(e.FullPath);
                watcher.Changed += (_, e) => engine.OnEvent(e.FullPath);
                watcher.Renamed += (_, e) => engine.OnEvent(e.FullPath);
                watcher.Deleted += (_, e) => engine.OnDeleted(e.FullPath);
                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }
    }
}

internal sealed class WatchUsageException(string message) : Exception(message);
