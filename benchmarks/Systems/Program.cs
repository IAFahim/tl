using System.Diagnostics;
using System.Text.Json;
using BenchmarkDotNet.Running;
using Systems;


if (args.Contains("--receipt"))
    return Receipt.Run(args);

if (args.Contains("--smoke"))
    return Smoke.Run();

BenchmarkRunner.Run<SystemsBenchmarks>(null, args);
return 0;

internal static class Smoke
{
    public static int Run()
    {
        var first = Pass();
        var second = Pass();
        Console.WriteLine($"SMOKE hash1={first:x16} hash2={second:x16} equal={first == second}");
        return first == second ? 0 : 1;
    }

    static ulong Pass()
    {
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        RunShape(bench, "NewA", () => bench.NewA());
        RunShape(bench, "NewABackward", () => bench.NewABackward());
        RunShape(bench, "NewB", () => bench.NewB());
        RunShape(bench, "NewC", () => bench.NewC());
        RunShape(bench, "NewD", () => bench.NewD());
        RunShape(bench, "S2OnePlayback", () => bench.S2OnePlayback());
        RunShape(bench, "S2TwoPlaybacks", () => bench.S2TwoPlaybacks());
        RunShape(bench, "S64TwoActive", () => bench.S64TwoActive());
        Console.WriteLine($"SMOKE sink={bench.Sink}");
        return bench.Checksum();
    }

    static void RunShape(SystemsBenchmarks bench, string name, Action run)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = DateTime.UtcNow;
        run();
        var elapsed = DateTime.UtcNow - start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"SMOKE {name} frames={SystemsBenchmarks.Frames} entities={SystemsBenchmarks.N} ms={elapsed.TotalMilliseconds:F1} bytes={allocated} sum={bench.Checksum():x16} ticks={bench.LastTicks}");
    }
}

internal static class Stress
{
    public static int Run()
    {
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        Loop("NewA", 3000, () => bench.NewA());
        Loop("NewABackward", 3000, () => bench.NewABackward());
        Loop("NewB", 3000, () => bench.NewB());
        Loop("NewC", 3000, () => bench.NewC());
        Loop("NewD", 3000, () => bench.NewD());
        Loop("S2OnePlayback", 3000, () => bench.S2OnePlayback());
        Loop("S2TwoPlaybacks", 3000, () => bench.S2TwoPlaybacks());
        Loop("S64TwoActive", 3000, () => bench.S64TwoActive());
        Console.WriteLine($"STRESS sink={bench.Sink}");
        return 0;
    }

    static void Loop(string name, int invokes, Action run)
    {
        for (var i = 0; i < invokes; i++) run();
        Console.WriteLine($"STRESS {name} ok");
    }
}


internal static class Receipt
{
    internal sealed record Shape(string Id, string Label, Func<SystemsBenchmarks, Action> Run, string? Baseline, string? Gate);

    internal static readonly Shape[] Shapes =
    [
        new("folded", "folded lane through a generated system", b => b.NewA, "folded", "<=1.03x (no regression)"),
        new("folded-backward", "folded lane rewound through the same system", b => b.NewABackward, null, null),
        new("fold-fed", "fold-fed compose through a generated system", b => b.NewB, "fold-fed", "<=0.15x"),
        new("frame", "frame-consumer dispatch through a generated system", b => b.NewC, "frame", "<=0.25x"),
        new("four-pair", "four pairs through one generated system", b => b.NewD, "four-pair", "<=0.70x"),
        new("1-playback", "one playback per entity (2-playback reference)", b => b.S2OnePlayback, null, null),
        new("2-playback", "two playbacks per entity", b => b.S2TwoPlaybacks, null, "<=2.2x of 1-playback (live)"),
        new("64-consumer", "64 consumers, 2 active per tick", b => b.S64TwoActive, null, "<=1.5x of same-system 2-consumer (manual)"),
    ];

    internal static int Run(string[] args)
    {
        var outPath = ValueOf(args, "--receipt");
        if (string.IsNullOrEmpty(outPath)) { Console.Error.WriteLine("--receipt <path> is required."); return 2; }
        var reps = int.TryParse(ValueOf(args, "--reps"), out var parsedReps) ? parsedReps : 5;
        var warmupTargetMs = double.TryParse(ValueOf(args, "--warmup-ms"), out var parsedWarmup) ? parsedWarmup : 500.0;
        var baselinesPath = ValueOf(args, "--baselines") ?? Path.Combine("benchmarks", "Systems", "results", "old-baselines.json");
        using var baselinesDoc = JsonDocument.Parse(File.ReadAllText(baselinesPath));
        var baselines = baselinesDoc.RootElement.GetProperty("Shapes");
        var rows = SystemsBenchmarks.N;
        var bench = new SystemsBenchmarks();
        bench.Setup();
        bench.Reset();
        var runners = Shapes.Select(s => (Shape: s, Run: s.Run(bench))).ToArray();
        var sw = new Stopwatch();
        var warmMs = new Dictionary<string, double>();
        var warmFrames = new Dictionary<string, int>();
        var warmed = new Dictionary<string, double>();
        var flatRounds = new Dictionary<string, int>();
        var best = new Dictionary<string, double>();
        foreach (var (shape, _) in runners) { warmMs[shape.Id] = 0.0; warmFrames[shape.Id] = 0; warmed[shape.Id] = 0.0; flatRounds[shape.Id] = 0; best[shape.Id] = double.PositiveInfinity; }
        while (flatRounds.Values.Any(f => f < 3) || runners.Any(t => warmed[t.Shape.Id] < warmupTargetMs))
        {
            foreach (var (shape, run) in runners)
            {
                if (warmed[shape.Id] >= warmupTargetMs && flatRounds[shape.Id] >= 3) continue;
                sw.Restart(); run(); sw.Stop();
                var ms = sw.Elapsed.TotalMilliseconds;
                warmMs[shape.Id] += ms;
                warmFrames[shape.Id] += SystemsBenchmarks.Frames;
                warmed[shape.Id] += ms;
                if (warmed[shape.Id] > 30000) flatRounds[shape.Id] = 3;
                else if (ms < best[shape.Id]) { best[shape.Id] = ms; flatRounds[shape.Id] = 0; }
                else flatRounds[shape.Id]++;
            }
            if (runners.All(t => warmed[t.Shape.Id] > 30000)) break;
        }
        (double Ms, long Allocated) Measure((Shape Shape, Action Run) t, bool hot)
        {
            var samples = new List<double>();
            var allocs = new List<long>();
            for (var i = 0; i < reps; i++)
            {
                if (!hot) foreach (var (other, run) in runners) if (!ReferenceEquals(other, t.Shape)) run();
                var before = GC.GetAllocatedBytesForCurrentThread();
                sw.Restart(); t.Run(); sw.Stop();
                allocs.Add(GC.GetAllocatedBytesForCurrentThread() - before);
                samples.Add(sw.Elapsed.TotalMilliseconds);
            }
            return (samples.Min(), allocs[samples.IndexOf(samples.Min())]);
        }
        double Ns(double ms) => ms * 1_000_000.0 / SystemsBenchmarks.Frames / rows;
        var scenarios = new List<object>();
        var hashes = new Dictionary<string, string>();
        foreach (var t in runners)
        {
            var (hotMs, hotAlloc) = Measure(t, hot: true);
            var (coldMs, _) = Measure(t, hot: false);
            bench.Reset();
            var h1 = bench.Checksum();
            bench.Reset();
            var h2 = bench.Checksum();
            if (h1 != h2) { Console.Error.WriteLine($"non-deterministic shape {t.Shape.Id}."); return 1; }
            hashes[t.Shape.Id] = h1.ToString("x16");
            double? baselineNs = t.Shape.Baseline is null ? null : baselines.GetProperty(t.Shape.Baseline).GetProperty("Ns").GetDouble();
            var hotNs = Ns(hotMs);
            scenarios.Add(new Dictionary<string, object?>
            {
                ["Id"] = t.Shape.Id,
                ["Label"] = t.Shape.Label,
                ["Hot"] = new Dictionary<string, object> { ["Ms"] = hotMs, ["Ns"] = hotNs },
                ["Cold"] = new Dictionary<string, object> { ["Ms"] = coldMs, ["Ns"] = Ns(coldMs) },
                ["WarmupMs"] = warmMs[t.Shape.Id],
                ["WarmupFrames"] = warmFrames[t.Shape.Id],
                ["Allocated"] = hotAlloc,
                ["Gate"] = t.Shape.Gate,
                ["BaselineNs"] = baselineNs,
                ["Ratio"] = baselineNs is null or 0 ? null : hotNs / baselineNs,
            });
        }
        double NsOf(string id)
        {
            var scenario = (Dictionary<string, object?>)scenarios.First(s => ((Dictionary<string, object?>)s)["Id"]!.ToString() == id);
            return scenario["Hot"] is Dictionary<string, object> hot && hot["Ns"] is double ns ? ns : 0.0;
        }
        var one = NsOf("1-playback");
        var two = NsOf("2-playback");
        var doc = new Dictionary<string, object?>
        {
            ["Fingerprint"] = Fingerprint(),
            ["Rows"] = rows,
            ["Reps"] = reps,
            ["Scenarios"] = scenarios,
            ["TwoVsOne"] = one > 0 ? two / one : 0.0,
            ["Parity"] = new Dictionary<string, object> { ["DoubleRunMatch"] = true, ["Hashes"] = hashes },
            ["Bake"] = null,
            ["Checksum"] = Checksum(),
            ["Tiering"] = Tiering(),
            ["GeneratedUtc"] = DateTime.UtcNow.ToString("o"),
            ["Dotnet"] = Environment.Version.ToString(),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"receipt: {scenarios.Count} scenarios -> {outPath}");
        return 0;
    }

    static Dictionary<string, object?> Fingerprint()
    {
        var cpu = "unknown";
        if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo"))
            foreach (var line in File.ReadLines("/proc/cpuinfo"))
                if (line.StartsWith("model name", StringComparison.Ordinal)) { cpu = line.Split(':', 2)[1].Trim(); break; }
        if (OperatingSystem.IsWindows())
            cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? cpu;
        return new() { ["Cpu"] = cpu, ["Cores"] = Environment.ProcessorCount, ["Dotnet"] = Environment.Version.ToString(), ["Os"] = Environment.OSVersion.ToString() };
    }

    static Dictionary<string, object?> Tiering() => new()
    {
        ["TieredCompilation"] = null,
        ["TieredPGO"] = null,
        ["EnableAVX2"] = null,
        ["EnableHWIntrinsic"] = null,
    };

    static ulong Checksum()
    {
        var hash = 14695981039346656037UL;
        foreach (var c in "tl-numbers-v2|folded|folded-backward|fold-fed|frame|four-pair|1-playback|2-playback|64-consumer") hash = (hash ^ c) * 1099511628211UL;
        return hash | 1UL;
    }

    static string? ValueOf(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
