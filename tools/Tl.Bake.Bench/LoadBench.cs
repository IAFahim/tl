using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Cysharp.Collections;
using Tl;

namespace Tl.Bake.Bench;

internal static class LoadBench
{
    private const int PosixFadvDontNeed = 4;

    [DllImport("libc", SetLastError = true)]
    private static extern int posix_fadvise(int fd, long offset, long len, int advice);

    internal static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: loadbench <file.tlb> [--rounds 3] [--reps 5] [--core -1] [--mode all|managed|native|mmap] [--cold] [--dedup]");
            return 1;
        }
        var path = args[1];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Error: file '{path}' does not exist.");
            return 1;
        }
        var rounds = ParsedValueOf(args, "--rounds", 3);
        var reps = ParsedValueOf(args, "--reps", 5);
        var core = ParsedValueOf(args, "--core", -1);
        var mode = ValueOf(args, "--mode") ?? "all";
        var cold = HasFlag(args, "--cold");
        var dedup = HasFlag(args, "--dedup");

        if (core >= 0 && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1L << core);
        var load1 = File.ReadLines("/proc/loadavg").First().Split(' ')[0];
        var length = new FileInfo(path).Length;
        if (length > int.MaxValue)
        {
            Console.Error.WriteLine("Error: loadbench staging spans are limited to int.MaxValue bytes.");
            return 1;
        }

        var modes = mode == "all"
            ? new[] { "managed", "native", "mmap" }
            : mode.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Console.WriteLine($"loadbench {path}: {length} B, modes [{string.Join(' ', modes)}], rounds {rounds}, reps {reps}, core {core}, cache {(cold ? "cold (fadvise DONTNEED per leg)" : "warm")}, semantics {(dedup ? "dedup-hit" : "ingest (dispose per rep)")}");
        Console.WriteLine($"host {Environment.ProcessorCount} cores, loadavg1 {load1}, dotnet {Environment.Version}, gc {(System.Runtime.GCSettings.IsServerGC ? "server" : "workstation")}");

        var expected = ReceiptOf(File.ReadAllBytes(path));
        var indices = new Dictionary<string, ushort>();
        foreach (var name in modes)
        {
            using var asset = TimelineAsset.Of(LoadVia(name, path, (int)length, out var hash));
            indices[name] = asset.Index;
            if (hash != expected)
            {
                Console.Error.WriteLine($"Error: staging receipt mismatch for mode '{name}': {hash} != {expected}.");
                return 1;
            }
        }
        if (indices.Values.Distinct().Count() != 1)
        {
            Console.Error.WriteLine($"Error: intern indices diverged across modes: {string.Join(' ', indices.Select(kv => $"{kv.Key}={kv.Value}"))}.");
            return 1;
        }
        Console.WriteLine($"receipt: sha256 {expected}, intern index {indices.Values.First()} equal across modes");

        var timings = modes.ToDictionary(m => m, _ => new List<double>());
        var allocations = modes.ToDictionary(m => m, _ => new List<double>());
        for (var round = 0; round < rounds; round++)
        {
            for (var rep = 0; rep < reps; rep++)
            {
                foreach (var name in modes)
                {
                    if (cold)
                        DropCache(path);
                    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    TimedLeg(name, path, dedup);
                    var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    timings[name].Add(ms);
                    allocations[name].Add((GC.GetAllocatedBytesForCurrentThread() - allocatedBefore) / 1024.0);
                }
            }
            Console.WriteLine($"round {round} done");
        }

        Console.WriteLine();
        Console.WriteLine("| staging | best ms | p50 ms | managed KB/rep |");
        Console.WriteLine("|---|---:|---:|---:|");
        foreach (var name in modes)
        {
            var values = timings[name];
            values.Sort();
            var alloc = allocations[name].Average();
            Console.WriteLine($"| {name} | {values[0]:0.00} | {values[values.Count / 2]:0.00} | {alloc:0.0} |");
        }
        return 0;
    }

    private static void TimedLeg(string name, string path, bool dedup)
    {
        if (dedup)
        {
            _ = TimelineAsset.Load(StagingSpan(name, path, out var release));
            release();
            return;
        }
        using var asset = TimelineAsset.Of(TimelineAsset.Load(StagingSpan(name, path, out var releaser)));
        releaser();
    }

    private static ushort LoadVia(string name, string path, int length, out string hash)
    {
        var span = StagingSpan(name, path, out var release);
        try
        {
            hash = Convert.ToHexString(SHA256.HashData(span)).ToLowerInvariant();
            return TimelineAsset.Load(span);
        }
        finally
        {
            release();
        }
    }

    private static unsafe ReadOnlySpan<byte> StagingSpan(string name, string path, out Action release)
    {
        switch (name)
        {
            case "managed":
            {
                var bytes = File.ReadAllBytes(path);
                release = () => { };
                return bytes;
            }
            case "native":
            {
                using var source = File.OpenRead(path);
                var buffer = new NativeMemoryArray<byte>(source.Length, skipZeroClear: true);
                var span = buffer.TryGetFullSpan(out var full) ? full : throw new InvalidOperationException("staging buffer exceeds span addressable length.");
                var read = 0;
                while (read < span.Length)
                {
                    var n = source.Read(span[read..]);
                    if (n == 0) throw new EndOfStreamException($"short read on '{path}' at {read}.");
                    read += n;
                }
                release = buffer.Dispose;
                return span;
            }
            case "mmap":
            {
                var file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                var accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                release = () =>
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    accessor.Dispose();
                    file.Dispose();
                };
                return new ReadOnlySpan<byte>(ptr, (int)new FileInfo(path).Length);
            }
            default:
                throw new InvalidOperationException($"unknown staging mode '{name}'.");
        }
    }

    private static void DropCache(string path)
    {
        using var handle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _ = posix_fadvise((int)handle.SafeFileHandle.DangerousGetHandle(), 0, 0, PosixFadvDontNeed);
    }

    private static string ReceiptOf(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string? ValueOf(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }

    private static int ParsedValueOf(string[] args, string name, int fallback)
    {
        var raw = ValueOf(args, name);
        return raw != null && int.TryParse(raw, out var value) ? value : fallback;
    }

    private static bool HasFlag(string[] args, string flag)
    {
        foreach (var arg in args)
            if (arg == flag)
                return true;
        return false;
    }
}
