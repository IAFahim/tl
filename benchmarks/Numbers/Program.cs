using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Tl;
using Tl.Gen.Tlb;
using NumbersBench;

var rows = Parsed(args, "--rows", 1_000_000);
var rounds = Parsed(args, "--rounds", 3);
var reps = Parsed(args, "--reps", 5);
var core = Parsed(args, "--core", 2);
var steadyRows = Parsed(args, "--steady-rows", 100_000);
var steady = HasFlag(args, "--steady");
var armId = ValueOf(args, "--steady-arm");
var corpusDirectory = ValueOf(args, "--corpus") ?? Path.Combine(AppContext.BaseDirectory, "corpus");
var outPath = ValueOf(args, "--out");
var only = ValueOf(args, "--only")?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

if (armId is not null)
{
    if (core >= 0 && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
        Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1L << core);
    Host.LoadAssets();
    SteadyArms.Run(armId, rows, rounds, reps, outPath);
    return 0;
}

var armReceipts = new List<ArmReceipt>();
if (steady)
{
    var exe = Environment.ProcessPath ?? throw new InvalidOperationException("cannot resolve the host executable.");
    var assembly = Path.Combine(AppContext.BaseDirectory, "Numbers.dll");
    foreach (var id in SteadyArms.Ids)
    {
        var armOut = Path.GetTempFileName();
        var info = new ProcessStartInfo(exe) { UseShellExecute = false };
        info.ArgumentList.Add(assembly);
        info.ArgumentList.Add("--steady-arm");
        info.ArgumentList.Add(id);
        info.ArgumentList.Add("--rows");
        info.ArgumentList.Add(rows.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add("--rounds");
        info.ArgumentList.Add(rounds.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add("--reps");
        info.ArgumentList.Add(reps.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add("--core");
        info.ArgumentList.Add(core.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add("--out");
        info.ArgumentList.Add(armOut);
        info.EnvironmentVariables[SteadyArms.VariableOf(id)] = "0";
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"cannot launch steady arm '{id}'.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"steady arm '{id}' exited with {process.ExitCode}.");
        armReceipts.Add(JsonSerializer.Deserialize<ArmReceipt>(File.ReadAllText(armOut))!);
        File.Delete(armOut);
    }
}

if (core >= 0 && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()))
    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1L << core);

Host.LoadAssets();

var steadyShapes = steady ? SteadyShapes.Run(steadyRows, rounds, reps) : null;

var scenarios = new List<Scenario>
{
    new("sync", "whole crowd on one timeline (a raid jumping in sync)", Playback(static _ => Host.Gold, static _ => (ushort)5)),
    new("shared-clock", "crowd on one clock: shared-clock Apply + scalar Advance", null, sharedClock: true),
    new("groups", "100 timelines, crowds of 10,000 each (per-ability groups)", Playback(static i => Host.Variants[(i / 10_000) % Host.Timelines], static i => (ushort)(i % Host.Duration))),
    new("own-clock", "one looping timeline, every character on its own clock", Playback(static _ => Host.Gold, static i => (ushort)(i % Host.Duration))),
    new("finite", "one-shot finite timeline, staggered clocks", Playback(static _ => Host.Finite, static i => (ushort)(i % (Host.Duration / 2)))),
    new("handwritten", "hand-written scalar loop (`effects[i] += 1f`)", null),
    new("handwritten-simd", "hand-written SIMD loop (`Vector<float>` add, scalar tail)", null, handVector: true),
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

foreach (var scenario in scenarios)
{
    var allocated = GC.GetTotalAllocatedBytes(precise: true);
    scenario.Step();
    scenario.Allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
    if (scenario.Allocated != 0)
        throw new InvalidOperationException($"scenario '{scenario.Id}' allocated {scenario.Allocated} B on a warm frame.");
}

foreach (var scenario in scenarios)
    for (var frame = 0; frame < rounds * reps; frame++)
        scenario.Step();

var checksum = 0ul;
foreach (var scenario in scenarios)
{
    var positions = (ushort[])scenario.Positions.Clone();
    var effects = (float[])scenario.Effects.Clone();
    var sink = scenario.Sink;
    for (var tick = 0; tick < 100; tick++) scenario.Step();
    for (var tick = 0; tick < 100; tick++) scenario.StepBack();
    if (!scenario.Positions.AsSpan().SequenceEqual(positions) || !scenario.Effects.AsSpan().SequenceEqual(effects) || scenario.Sink != sink)
        throw new InvalidOperationException(
            $"scenario '{scenario.Id}' did not rewind bit-exactly: positions {(scenario.Positions.AsSpan().SequenceEqual(positions) ? "ok" : "differ")}, effects {(scenario.Effects.AsSpan().SequenceEqual(effects) ? "ok" : "differ")}, sink {(scenario.Sink == sink ? "ok" : "differ")}.");
    checksum = checksum * 31 + (ulong)scenario.Sink + (ulong)scenario.Effects[0];
}

foreach (var scenario in scenarios)
{
    (scenario.WarmupMs, scenario.WarmupFrames) = Measure.WarmUp(scenario.Step);
    var hot = double.MaxValue;
    for (var frame = 0; frame < rounds * reps; frame++)
    {
        var start = Stopwatch.GetTimestamp();
        scenario.Step();
        var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (ms < hot) hot = ms;
    }
    scenario.HotMs = hot;
    Console.WriteLine($"hot {scenario.Id} done");
}

foreach (var scenario in scenarios)
    scenario.ColdMs = double.MaxValue;
for (var round = 0; round < rounds; round++)
{
    for (var rep = 0; rep < reps; rep++)
        foreach (var scenario in scenarios)
        {
            var start = Stopwatch.GetTimestamp();
            scenario.Step();
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < scenario.ColdMs) scenario.ColdMs = ms;
        }
    Console.WriteLine($"cold round {round} done");
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
    [.. scenarios.Select(scenario => new ScenarioReceipt(
        scenario.Id,
        scenario.Label,
        new WindowReceipt(scenario.HotMs, scenario.HotMs * 1_000_000.0 / rows),
        new WindowReceipt(scenario.ColdMs, scenario.ColdMs * 1_000_000.0 / rows),
        scenario.WarmupMs,
        scenario.WarmupFrames,
        scenario.Allocated))],
    steady ? new SteadyReceipt(steadyRows, steadyShapes!, [.. armReceipts]) : null,
    only is null ? new BakeReceipt(corpusBytes, Path.GetFileName(corpusPath), bakeMs, loadMs) : null,
    checksum,
    new TieringReceipt(
        Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
        Environment.GetEnvironmentVariable("DOTNET_TieredPGO"),
        Environment.GetEnvironmentVariable("DOTNET_EnableAVX2"),
        Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic")),
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
    Environment.Version.ToString());

Console.WriteLine();
Console.WriteLine($"fingerprint/cpu: {fingerprint.Cpu}");
Console.WriteLine($"fingerprint/cores: {fingerprint.Cores}");
Console.WriteLine($"fingerprint/dotnet: {fingerprint.Dotnet}");
foreach (var scenario in scenarios)
    Console.WriteLine(
        $"scenario/{scenario.Id}: hot {Format(scenario.HotMs)} ms ({Format(scenario.HotMs * 1_000_000.0 / rows)} ns/char), " +
        $"cold {Format(scenario.ColdMs)} ms ({Format(scenario.ColdMs * 1_000_000.0 / rows)} ns/char), " +
        $"{scenario.Allocated} B, warmup {scenario.WarmupMs.ToString("0", CultureInfo.InvariantCulture)} ms / {scenario.WarmupFrames} frames");
if (only is null)
{
    Console.WriteLine($"bake/corpus-mb: {corpusBytes.ToString("0.0#", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"bake/total-ms: {bakeMs.ToString("0.0#", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"load/ms: {loadMs.ToString("0.0#", CultureInfo.InvariantCulture)}");
}
else
{
    Console.WriteLine($"bake: skipped (--only {string.Join(',', only)})");
}
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

static bool HasFlag(string[] args, string name)
{
    foreach (var arg in args)
        if (arg == name)
            return true;
    return false;
}

static string Format(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

internal static class Measure
{
    private const double SteadyWindowMs = 500;
    private const int SteadyFlatFrames = 3;

    public static (double Ms, int Frames) WarmUp(Action run)
    {
        var best = double.MaxValue;
        var flat = 0;
        var frames = 0;
        var wall = Stopwatch.StartNew();
        while (wall.Elapsed.TotalMilliseconds < SteadyWindowMs || flat < SteadyFlatFrames)
        {
            var start = Stopwatch.GetTimestamp();
            run();
            frames++;
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < best)
            {
                best = ms;
                flat = 0;
            }
            else flat++;
        }
        return (wall.Elapsed.TotalMilliseconds, frames);
    }

    public static ShapeReceipt MeasureShape(string id, string label, int count, Action run, int rounds, int reps)
    {
        for (var warm = 0; warm < 5; warm++) run();
        var allocated = GC.GetTotalAllocatedBytes(precise: true);
        run();
        var delta = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        if (delta != 0)
            throw new InvalidOperationException($"shape '{id}' allocated {delta} B on a warm frame.");
        var (warmupMs, warmupFrames) = WarmUp(run);
        var best = double.MaxValue;
        for (var frame = 0; frame < rounds * reps; frame++)
        {
            var start = Stopwatch.GetTimestamp();
            run();
            var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms < best) best = ms;
        }
        return new ShapeReceipt(id, label, best, best * 1_000_000.0 / count, warmupMs, warmupFrames, 0);
    }
}

internal static class SteadyShapes
{
    public static ShapeReceipt[] Run(int entityRows, int rounds, int reps)
    {
        var gold = Host.Gold;
        var pos = new ushort[entityRows];
        var fx = Seeds.Effects(entityRows);
        var records = Records();
        var sparseRows = ScatteredRows(entityRows);
        var denseRows = DenseRows(entityRows);
        var sparseIds = new ushort[entityRows];
        Array.Fill(sparseIds, gold);
        var shapes = new (string Id, string Label, Action Seed, Action Run)[]
        {
            ("per-entity-apply-step", "one entity at a time: 1-row Apply + 1-row Advance", () => Seed(pos), () =>
            {
                for (var i = 0; i < entityRows; i++)
                {
                    Timeline<LaneTrack, LaneClip>.Apply(gold, new ReadOnlySpan<ushort>(in pos[i]), true, new Span<float>(ref fx[i]));
                    Timeline.Advance(gold, new Span<ushort>(ref pos[i]), true);
                }
            }),
            ("per-entity-fused", "one entity at a time: fused 1-row Apply", () => Seed(pos), () =>
            {
                for (var i = 0; i < entityRows; i++)
                    Timeline<LaneTrack, LaneClip>.Apply(gold, new ReadOnlySpan<ushort>(in pos[i]), new Span<ushort>(ref pos[i]), true, new Span<float>(ref fx[i]));
            }),
            ("per-entity-lane", "one entity at a time: Timeline<T,C>.Apply", () => Seed(pos), () =>
            {
                for (var i = 0; i < entityRows; i++)
                    Timeline<LaneTrack, LaneClip>.Apply(gold, pos[i], true, ref fx[i]);
            }),
            ("shared-clock-crowd", "crowd on one clock: shared-clock Apply + scalar Advance", () => Seed(pos), () =>
            {
                var clock = (ushort)(entityRows % Host.Duration);
                Timeline<LaneTrack, LaneClip>.Apply(gold, clock, true, fx);
                Timeline<LaneTrack, LaneClip>.Advance(gold, ref clock, true);
            }),
            ("per-entity-record-floor", "hand record table (floor)", () => Seed(pos), () =>
            {
                for (var i = 0; i < entityRows; i++)
                {
                    ref var r = ref records[pos[i]];
                    fx[i] += r.Effect;
                    pos[i] = r.Next;
                }
            }),
            ("sparse-separate-apply-step", "scattered entities: separate 1-row Apply + Advance through row handles", () => Seed(pos), () =>
            {
                for (var k = 0; k < entityRows; k++)
                {
                    var r = sparseRows[k];
                    Timeline<LaneTrack, LaneClip>.Apply(gold, new ReadOnlySpan<ushort>(in pos[r]), true, new Span<float>(ref fx[r]));
                    Timeline.Advance(gold, new Span<ushort>(ref pos[r]), true);
                }
            }),
            ("sparse-batch-apply-step", "scattered entities: rows-gather batched Apply + Advance", () => Seed(pos), () =>
            {
                Timeline<LaneTrack, LaneClip>.Apply(sparseRows, sparseIds, pos, true, fx);
                Timeline<LaneTrack, LaneClip>.Advance(sparseRows, sparseIds, pos, true);
            }),
            ("sparse-batch-dense-list", "compacted sparse list (dense rows): batched Apply + Advance", () => Seed(pos), () =>
            {
                Timeline<LaneTrack, LaneClip>.Apply(denseRows, sparseIds, pos, true, fx);
                Timeline<LaneTrack, LaneClip>.Advance(denseRows, sparseIds, pos, true);
            }),
            ("sparse-list-crowd-apply-step", "compacted sparse list: crowd Apply + raw Advance (existing surface)", () => Seed(pos), () =>
            {
                Timeline<LaneTrack, LaneClip>.Apply(sparseIds, pos, true, fx);
                Timeline.Advance(sparseIds, pos, true);
            }),
        };
        var receipts = new ShapeReceipt[shapes.Length];
        for (var i = 0; i < shapes.Length; i++)
        {
            fx = Seeds.Effects(entityRows);
            shapes[i].Seed();
            receipts[i] = Measure.MeasureShape(shapes[i].Id, shapes[i].Label, entityRows, shapes[i].Run, rounds, reps);
            Console.WriteLine(
                $"steady/{shapes[i].Id}: {receipts[i].Ms.ToString("0.00", CultureInfo.InvariantCulture)} ms, " +
                $"{receipts[i].Ns.ToString("0.00", CultureInfo.InvariantCulture)} ns/entity, {receipts[i].Allocated} B");
        }
        return receipts;
    }

    static int[] ScatteredRows(int count)
    {
        var rows = new int[count];
        for (var i = 0; i < count; i++) rows[i] = i;
        var state = 0x9E3779B97F4A7C15ul;
        for (var i = count - 1; i > 0; i--)
        {
            state = state * 6364136223846793005ul + 1442695040888963407ul;
            var j = (int)((state >> 33) % (ulong)(i + 1));
            (rows[i], rows[j]) = (rows[j], rows[i]);
        }
        return rows;
    }

    static int[] DenseRows(int count)
    {
        var rows = new int[count];
        for (var i = 0; i < count; i++) rows[i] = i;
        return rows;
    }

    static void Seed(ushort[] pos)
    {
        for (var i = 0; i < pos.Length; i++)
            pos[i] = (ushort)(i % Host.Duration);
    }

    static Rec[] Records()
    {
        var table = new Rec[Host.Duration + 1];
        for (var p = 0; p < Host.Duration; p++)
            table[p] = new Rec { Effect = p < 614 ? 2.5f : -1f, Next = (ushort)(p + 1 == Host.Duration ? 0 : p + 1), Pad = 0 };
        return table;
    }
}

internal struct Rec
{
    public float Effect;
    public ushort Next;
    public ushort Pad;
}

internal static class SteadyArms
{
    public static readonly string[] Ids = ["avx2-off", "hwintrinsic-off"];

    public static string VariableOf(string id) => id switch
    {
        "avx2-off" => "DOTNET_EnableAVX2",
        "hwintrinsic-off" => "DOTNET_EnableHWIntrinsic",
        _ => throw new ArgumentException($"Unknown steady arm '{id}'."),
    };

    public static ArmReceipt Run(string id, int rows, int rounds, int reps, string? outPath)
    {
        var gold = Host.Gold;
        var pos = new ushort[rows];
        var fx = Seeds.Effects(rows);
        var uniform = Uniform(rows);
        var staggered = Staggered(rows);
        var shapes = id switch
        {
            "avx2-off" => new (string Id, string Label, ushort[] Seed, Action Run)[]
            {
                ("index-fused-staggered", "single timeline, staggered clocks", staggered, () => Timeline<LaneTrack, LaneClip>.Apply(gold, pos, pos, true, fx)),
                ("index-fused-uniform", "single timeline, uniform clocks", uniform, () => Timeline<LaneTrack, LaneClip>.Apply(gold, pos, pos, true, fx)),
                ("step-index-uniform", "single timeline, uniform clocks, clock advance only", uniform, () => Timeline.Advance(gold, pos, true)),
            },
            "hwintrinsic-off" => new (string Id, string Label, ushort[] Seed, Action Run)[]
            {
                ("index-fused-staggered", "single timeline, staggered clocks", staggered, () => Timeline<LaneTrack, LaneClip>.Apply(gold, pos, pos, true, fx)),
            },
            _ => throw new ArgumentException($"Unknown steady arm '{id}'."),
        };
        var receipts = new ShapeReceipt[shapes.Length];
        for (var i = 0; i < shapes.Length; i++)
        {
            shapes[i].Seed.CopyTo(pos);
            receipts[i] = Measure.MeasureShape(shapes[i].Id, shapes[i].Label, rows, shapes[i].Run, rounds, reps);
            Console.WriteLine(
                $"arm/{id}/{shapes[i].Id}: {receipts[i].Ms.ToString("0.00", CultureInfo.InvariantCulture)} ms, " +
                $"{receipts[i].Ns.ToString("0.00", CultureInfo.InvariantCulture)} ns/row");
        }
        var arm = new ArmReceipt(id, $"{VariableOf(id)}=0", receipts);
        if (outPath is not null)
            File.WriteAllText(outPath, JsonSerializer.Serialize(arm));
        return arm;
    }

    static ushort[] Uniform(int rows)
    {
        var ids = new ushort[rows];
        Array.Fill(ids, (ushort)5);
        return ids;
    }

    static ushort[] Staggered(int rows)
    {
        var ids = new ushort[rows];
        for (var i = 0; i < rows; i++)
            ids[i] = (ushort)(i % Host.Duration);
        return ids;
    }
}

internal sealed class Scenario(string id, string label, (Func<int, ushort> Ids, Func<int, ushort> Positions)? run, bool sharedClock = false, bool handVector = false)
{
    public string Id = id;
    public string Label = label;
    public (Func<int, ushort> Ids, Func<int, ushort> Positions)? Run = run;
    private bool SharedClock = sharedClock;
    private bool HandVector = handVector;
    private ushort Clock = 5;
    public ushort[] Ids = [];
    public ushort[] Positions = [];
    public float[] Effects = [];
    public double HotMs = double.MaxValue;
    public double ColdMs = double.MaxValue;
    public double WarmupMs;
    public int WarmupFrames;
    public long Allocated;

    public ulong Sink => SharedClock ? Clock : Positions[0];

    public void Step()
    {
        if (SharedClock)
        {
            Timeline<LaneTrack, LaneClip>.Apply(Host.Gold, Clock, true, Effects);
            Timeline<LaneTrack, LaneClip>.Advance(Host.Gold, ref Clock, true);
            return;
        }
        if (Run is null)
        {
            if (HandVector) Domain.AddVector(Effects, 1f);
            else Domain.AddScalar(Effects, 1f);
            return;
        }
        Timeline<LaneTrack, LaneClip>.Apply(Ids, Positions, Positions, true, Effects);
    }

    public void StepBack()
    {
        if (SharedClock)
        {
            Timeline<LaneTrack, LaneClip>.Apply(Host.Gold, Clock, false, Effects);
            Timeline<LaneTrack, LaneClip>.Advance(Host.Gold, ref Clock, false);
            return;
        }
        if (Run is null)
        {
            if (HandVector) Domain.AddVector(Effects, -1f);
            else Domain.AddScalar(Effects, -1f);
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

internal sealed record WindowReceipt(double Ms, double Ns);

internal sealed record ScenarioReceipt(
    string Id,
    string Label,
    WindowReceipt Hot,
    WindowReceipt Cold,
    double WarmupMs,
    int WarmupFrames,
    long Allocated);

internal sealed record ShapeReceipt(string Id, string Label, double Ms, double Ns, double WarmupMs, int WarmupFrames, long Allocated);

internal sealed record ArmReceipt(string Id, string Env, ShapeReceipt[] Shapes);

internal sealed record SteadyReceipt(int Rows, ShapeReceipt[] Shapes, ArmReceipt[] Arms);

internal sealed record TieringReceipt(string? TieredCompilation, string? TieredPGO, string? EnableAVX2, string? EnableHWIntrinsic);

internal sealed record BakeReceipt(double CorpusMb, string CorpusFile, double BakeMs, double LoadMs);

internal sealed record Receipt(
    Fingerprint Fingerprint,
    int Rows,
    int Rounds,
    int Reps,
    ScenarioReceipt[] Scenarios,
    SteadyReceipt? Steady,
    BakeReceipt? Bake,
    ulong Checksum,
    TieringReceipt Tiering,
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
