using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tl;
using Tl.Gen.Tlb;
using NumbersBench;

var rows = Parsed(args, "--rows", 1_000_000);
var rounds = Parsed(args, "--rounds", 3);
var reps = Parsed(args, "--reps", 5);
var core = Parsed(args, "--core", 2);
var corpusDirectory = ValueOf(args, "--corpus") ?? Path.Combine(AppContext.BaseDirectory, "corpus");
var outPath = ValueOf(args, "--out");
var only = ValueOf(args, "--only")?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

if (core >= 0 && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1L << core);

Host.LoadAssets();

var scenarios = new List<Scenario>
{
    new("sync", "whole crowd on one timeline (a raid jumping in sync)", Playback(static _ => Host.Gold, static _ => (ushort)5)),
    new("groups", "100 timelines, crowds of 10,000 each (per-ability groups)", Playback(static i => Host.Variants[(i / 10_000) % Host.Timelines], static i => (ushort)(i % Host.Duration))),
    new("own-clock", "one looping timeline, every character on its own clock", Playback(static _ => Host.Gold, static i => (ushort)(i % Host.Duration))),
    new("finite", "one-shot finite timeline, staggered clocks", Playback(static _ => Host.Finite, static i => (ushort)(i % (Host.Duration / 2)))),
    new("handwritten", "hand-written loop for comparison (`effects += 1`)", null),
    new("squads", "small squads: 16 timelines × 16 characters", Playback(static i => Host.Variants[(i / 16) % 16], static i => (ushort)(i % Host.Duration))),
    new("worst", "worst case: unsorted rows, a different timeline each", Playback(static i => Host.Variants[i % Host.Timelines], static i => (ushort)(i % Host.Duration))),
};

if (only is not null)
    scenarios.RemoveAll(scenario => !only.Contains(scenario.Id));

foreach (var scenario in scenarios)
{
    scenario.Positions = new ushort[rows];
    scenario.Effects = Seeds.Effects(rows);
    if (scenario.Run is null)
    {
        scenario.Ids = [];
        continue;
    }
    var run = scenario.Run.Value;
    var ids = new ushort[rows];
    for (var i = 0; i < rows; i++)
    {
        ids[i] = run.Ids(i);
        scenario.Positions[i] = run.Positions(i);
    }
    scenario.Ids = ids;
}

foreach (var scenario in scenarios)
    for (var warm = 0; warm < 3; warm++)
        scenario.Step();

var checksum = 0ul;
foreach (var scenario in scenarios)
{
    var allocated = GC.GetTotalAllocatedBytes(precise: true);
    scenario.Step();
    scenario.Allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
    if (scenario.Allocated != 0)
        throw new InvalidOperationException($"scenario '{scenario.Id}' allocated {scenario.Allocated} B on a warm frame.");
}

for (var round = 0; round < rounds; round++)
{
    for (var rep = 0; rep < reps; rep++)
        foreach (var scenario in scenarios)
        {
            var start = Stopwatch.GetTimestamp();
            scenario.Step();
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < scenario.BestMs) scenario.BestMs = ms;
        }
    Console.WriteLine($"round {round} done");
}

foreach (var scenario in scenarios)
{
    if (scenario.Run is null) continue;
    var positions = (ushort[])scenario.Positions.Clone();
    var effects = (float[])scenario.Effects.Clone();
    for (var tick = 0; tick < 100; tick++) scenario.Step();
    for (var tick = 0; tick < 100; tick++) scenario.StepBack();
    if (!scenario.Positions.AsSpan().SequenceEqual(positions) || !scenario.Effects.AsSpan().SequenceEqual(effects))
        throw new InvalidOperationException(
            $"scenario '{scenario.Id}' did not rewind bit-exactly: positions {(scenario.Positions.AsSpan().SequenceEqual(positions) ? "ok" : "differ")}, effects {(scenario.Effects.AsSpan().SequenceEqual(effects) ? "ok" : "differ")}.");
    checksum = checksum * 31 + (ulong)scenario.Positions[0] + (ulong)scenario.Effects[0];
}

var corpusPath = only is null ? Corpus.Generate(corpusDirectory) : "";
var corpusBytes = 0.0;
var resolver = new BakerAssemblyResolver();
var baked = Array.Empty<byte>();
var bakeMs = double.MaxValue;
var loadMs = double.MaxValue;
if (only is null)
{
    corpusBytes = (double)File.ReadAllBytes(corpusPath).LongLength / (1024 * 1024);
    baked = TimelineBakerFast.BakeJsonUtf8(File.ReadAllBytes(corpusPath), resolver);
    for (var warm = 0; warm < 2; warm++)
    {
        _ = TimelineBaker.BakeJson(File.ReadAllBytes(corpusPath), resolver);
        _ = TimelineAsset.Load(baked);
    }
    for (var round = 0; round < rounds; round++)
    {
        for (var rep = 0; rep < reps; rep++)
        {
            var bytes = File.ReadAllBytes(corpusPath);
            var start = Stopwatch.GetTimestamp();
            _ = TimelineBaker.BakeJson(bytes, resolver);
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < bakeMs) bakeMs = ms;
            start = Stopwatch.GetTimestamp();
            _ = TimelineAsset.Load(baked);
            ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < loadMs) loadMs = ms;
        }
        Console.WriteLine($"bake round {round} done");
    }
}

var fingerprint = Fingerprint.Of();
var receipt = new Receipt(
    fingerprint,
    rows, rounds, reps,
    [.. scenarios.Select(scenario => new ScenarioReceipt(scenario.Id, scenario.Label, scenario.BestMs, scenario.BestMs * 1_000_000.0 / rows, scenario.Allocated))],
    new BakeReceipt(corpusBytes, Path.GetFileName(corpusPath), bakeMs, loadMs),
    checksum,
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
    Environment.Version.ToString());

Console.WriteLine();
Console.WriteLine($"fingerprint/cpu: {fingerprint.Cpu}");
Console.WriteLine($"fingerprint/cores: {fingerprint.Cores}");
Console.WriteLine($"fingerprint/dotnet: {fingerprint.Dotnet}");
foreach (var scenario in scenarios)
    Console.WriteLine($"scenario/{scenario.Id}: {scenario.BestMs.ToString("0.00", CultureInfo.InvariantCulture)} ms, {(scenario.BestMs * 1_000_000.0 / rows).ToString("0.00", CultureInfo.InvariantCulture)} ns/char, {scenario.Allocated} B");
Console.WriteLine($"bake/corpus-mb: {corpusBytes.ToString("0.0#", CultureInfo.InvariantCulture)}");
Console.WriteLine($"bake/total-ms: {bakeMs.ToString("0.0#", CultureInfo.InvariantCulture)}");
Console.WriteLine($"load/ms: {loadMs.ToString("0.0#", CultureInfo.InvariantCulture)}");
Console.WriteLine($"sink/checksum: {checksum}");

if (outPath is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    File.WriteAllText(outPath, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
}

return 0;

static (Func<int, ushort> Ids, Func<int, ushort> Positions)? Playback(Func<int, ushort> ids, Func<int, ushort> positions) =>
    (ids, positions);

static int Parsed(string[] args, string name, int fallback)
{
    var raw = ValueOf(args, name);
    return raw is not null && int.TryParse(raw, out var value) ? value : fallback;
}

static string? ValueOf(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == name)
            return args[i + 1];
    return null;
}

internal sealed class Scenario(string id, string label, (Func<int, ushort> Ids, Func<int, ushort> Positions)? run)
{
    public string Id = id;
    public string Label = label;
    public (Func<int, ushort> Ids, Func<int, ushort> Positions)? Run = run;
    public ushort[] Ids = [];
    public ushort[] Positions = [];
    public float[] Effects = [];
    public double BestMs = double.MaxValue;
    public long Allocated;

    public void Step()
    {
        if (Run is null)
        {
            for (var i = 0; i < Effects.Length; i++)
                Effects[i] += 1f;
            return;
        }
        Timeline<LaneTrack, LaneClip>.Apply(Ids, Positions, Positions, true, Effects);
    }

    public void StepBack()
    {
        if (Run is null)
        {
            for (var i = 0; i < Effects.Length; i++)
                Effects[i] -= 1f;
            return;
        }
        Timeline<LaneTrack, LaneClip>.Apply(Ids, Positions, Positions, false, Effects);
    }
}

internal sealed record Fingerprint(string Cpu, int Cores, string Dotnet, string Os)
{
    public static Fingerprint Of()
    {
        var cpu = "unknown";
        if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo"))
            foreach (var line in File.ReadLines("/proc/cpuinfo"))
                if (line.StartsWith("model name", StringComparison.Ordinal))
                {
                    cpu = line.Split(':', 2)[1].Trim();
                    break;
                }
        if (OperatingSystem.IsWindows())
            cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? cpu;
        return new(cpu, Environment.ProcessorCount, Environment.Version.ToString(), Environment.OSVersion.ToString());
    }
}

internal sealed record ScenarioReceipt(string Id, string Label, double Ms, double Ns, long Allocated);

internal sealed record BakeReceipt(double CorpusMb, string CorpusFile, double BakeMs, double LoadMs);

internal sealed record Receipt(
    Fingerprint Fingerprint,
    int Rows,
    int Rounds,
    int Reps,
    ScenarioReceipt[] Scenarios,
    BakeReceipt Bake,
    ulong Checksum,
    string GeneratedUtc,
    string Dotnet);

internal static class Seeds
{
    public static float[] Effects(int rows)
    {
        var effects = new float[rows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < rows; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = MathF.Round((float)((state >> 11) / 9007199254740992d) * 128f - 64f) / 4f;
        }
        return effects;
    }
}
