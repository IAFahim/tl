using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using Tl;
using LivePlan;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

BenchConsumers.Install();
var verifyOnly = args.Contains("--verify");
var artifactsIndex = Array.IndexOf(args, "--artifacts");
var artifacts = artifactsIndex >= 0 && artifactsIndex + 1 < args.Length
    ? args[artifactsIndex + 1]
    : $"results/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var filterIndex = Array.IndexOf(args, "--filter");
var filter = filterIndex >= 0 && filterIndex + 1 < args.Length ? args[filterIndex + 1] : "*";

var failures = Parity.Run();
if (failures != 0) return 1;
if (verifyOnly) return 0;

Console.WriteLine($"host: {Environment.OSVersion.VersionString}, {RuntimeInformation.FrameworkDescription}, procs {Environment.ProcessorCount}");
Console.WriteLine($"benchmark artifacts: {Path.GetFullPath(artifacts)}");

BenchmarkSwitcher.FromAssembly(typeof(LivePlanBenchmarks).Assembly).Run(["--filter", filter, "--artifacts", Path.GetFullPath(artifacts)]);
return 0;

static class Parity
{
    public static int Run()
    {
        const int rows = 8192;
        var bank = Host.BindBank();
        var ids = new ushort[rows];
        var clocks = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            ids[i] = bank[i % Host.Variants];
            clocks[i] = (ushort)(i * 7 % 61);
        }
        var batch = new int[rows];
        var columns = new ColumnSet();
        columns.Add(new float[rows]);
        columns.Add(batch);
        Timeline<BenchTrack, BenchClip>.Apply(ids, clocks, true, in columns);
        var singles = new int[rows];
        for (var i = 0; i < rows; i++)
        {
            var single = new ColumnSet();
            single.Add(new float[1]);
            single.Add(singles.AsSpan(i, 1));
            Timeline<BenchTrack, BenchClip>.Apply(ids.AsSpan(i, 1), clocks.AsSpan(i, 1), true, in single);
        }
        var mismatches = 0;
        for (var i = 0; i < rows; i++)
            if (batch[i] != singles[i]) mismatches++;
        Console.WriteLine($"parity rows {rows}, alternating-vs-singles mismatches {mismatches}");
        return mismatches;
    }
}
