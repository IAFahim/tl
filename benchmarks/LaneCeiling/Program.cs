using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tl.LaneCeilingProbe;

var rows = Parsed(args, "--rows", 1_000_000);
var reps = Parsed(args, "--reps", 15);
var core = Parsed(args, "--core", 2);
var rounds = Parsed(args, "--rounds", 3);
var outPath = ValueOf(args, "--out");
var parityOnly = args.Contains("--parity");
var timingOnly = args.Contains("--time");

if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1L << core);

var failures = 0;
if (!timingOnly)
{
    failures = Parity.Run();
    if (failures != 0) { Console.WriteLine("parity failed; timing skipped"); return 1; }
}
if (parityOnly) return 0;

var results = Timing.Run(rows, reps, rounds);

Console.WriteLine();
Console.WriteLine($"host {Environment.ProcessorCount} cores, {RuntimeInformation.ProcessArchitecture}, dotnet {Environment.Version}, pinned core {core}, {rows:N0} rows, best-of {rounds}x{reps}");
Console.WriteLine();
Console.WriteLine("| lane | shape | kernel | ns/row |");
Console.WriteLine("|---|---|---|---:|");
foreach (var r in results)
    Console.WriteLine($"| {r.Lane} | {r.Shape} | {r.Kernel} | {r.Ns:0.000} |");

if (outPath is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    File.WriteAllText(outPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
}
return 0;

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

internal sealed record Row(string Lane, string Shape, string Kernel, double Ms, double Ns, long Allocated);

internal static unsafe class Timing
{
    internal static List<Row> Run(int rows, int reps, int rounds)
    {
        var results = new List<Row>();
        var durations = new (ushort Duration, bool Looping, bool Forward, string Label)[]
        {
            (6, true, true, "loop6/fwd"),
            (16, true, true, "loop16/fwd"),
            (32, true, true, "loop32/fwd"),
            (1024, true, true, "loop1024/fwd"),
            (1024, true, false, "loop1024/bwd"),
            (1024, false, true, "finite1024/fwd"),
            (1024, false, false, "finite1024/bwd"),
        };

        var shapes = new (string Name, Func<ushort, int, ushort[]> Build)[]
        {
            ("staggered", static (d, n) => Map(n, i => (ushort)(i % d))),
            ("uniform", static (d, n) => Map(n, _ => 5)),
            ("waves100", static (d, n) => Map(n, i => (ushort)(i / 100 % d))),
        };

        foreach (var (duration, looping, forward, label) in durations)
        {
            var eff = Tables.BakeForward(duration, looping);
            var byp = Tables.BakeBackwardByPosition(duration, looping);
            var effPad = Pad(eff, duration);
            var bypPad = Pad(byp, duration);
            var rec = Tables.BakeRecords(eff, byp, duration, looping, forward);
            var table = forward ? effPad : bypPad;

            foreach (var (shapeName, build) in shapes)
            {
                var seed = build(duration, rows);
                var pos = new ushort[rows];
                var seedFx = Fx(rows);
                var fx = (float[])seedFx.Clone();

                foreach (var arm in Parity.Arms)
                {
                    if (!arm.Applies(duration, looping, forward)) continue;
                    var kernel = forward ? arm.Forward : arm.Backward;
                    var best = double.MaxValue;
                    for (var round = 0; round < rounds; round++)
                    for (var rep = 0; rep < reps; rep++)
                    {
                        Array.Copy(seed, pos, rows);
                        Array.Copy(seedFx, fx, rows);
                        var t = Stopwatch.GetTimestamp();
                        fixed (ushort* pp = pos)
                        fixed (float* fp = fx)
                            kernel(table, rec, duration, looping, pp, fp, rows);
                        var ms = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
                        if (ms < best) best = ms;
                    }
                    results.Add(new Row(label, shapeName, arm.Name, best, best * 1_000_000.0 / rows, 0));
                }

                {
                    var t0 = Stopwatch.GetTimestamp();
                    for (var i = 0; i < rows; i++) fx[i] += 1f;
                    var ms = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
                    results.Add(new Row(label, shapeName, "streamadd-floor", ms, ms * 1_000_000.0 / rows, 0));
                }
            }

            NativeMemory.AlignedFree(eff); NativeMemory.AlignedFree(byp);
            NativeMemory.AlignedFree(effPad); NativeMemory.AlignedFree(bypPad);
            NativeMemory.AlignedFree(rec);
        }
        return results;
    }

    static ushort[] Map(int n, Func<int, int> f)
    {
        var a = new ushort[n];
        for (var i = 0; i < n; i++) a[i] = (ushort)f(i);
        return a;
    }

    static float[] Fx(int n)
    {
        var fx = new float[n];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < n; i++)
        {
            state ^= state << 13; state ^= state >> 7; state ^= state << 17;
            fx[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return fx;
    }

    static float* Pad(float* source, ushort duration)
    {
        var size = Math.Max(64, duration + 1);
        var padded = Tables.AllocFloats(size);
        Buffer.MemoryCopy(source, padded, (long)size * sizeof(float), (long)(duration + 1) * sizeof(float));
        for (var i = duration + 1; i < size; i++) padded[i] = 0f;
        return padded;
    }
}
