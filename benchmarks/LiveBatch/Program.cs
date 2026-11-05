using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using LiveBatch;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var counterShape = ValueOf(args, "--counters");
if (counterShape is not null)
    return Counters.Run(
        counterShape,
        int.TryParse(ValueOf(args, "--counters-rows"), out var counterRows) ? counterRows : 100_000,
        int.TryParse(ValueOf(args, "--counters-iters"), out var counterIters) ? counterIters : 40_000,
        int.TryParse(ValueOf(args, "--counters-delay"), out var counterDelay) ? counterDelay : 6_000);

var failures = Parity.Run();
if (failures != 0) return 1;
if (args.Contains("--verify")) return 0;

var artifactsIndex = Array.IndexOf(args, "--artifacts");
var artifacts = artifactsIndex >= 0 && artifactsIndex + 1 < args.Length
    ? args[artifactsIndex + 1]
    : $"results/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var filterIndex = Array.IndexOf(args, "--filter");
var filter = filterIndex >= 0 && filterIndex + 1 < args.Length ? args[filterIndex + 1] : "*";

Console.WriteLine($"host: {Environment.OSVersion.VersionString}, {RuntimeInformation.FrameworkDescription}, procs {Environment.ProcessorCount}");
Console.WriteLine($"benchmark artifacts: {Path.GetFullPath(artifacts)}");

BenchmarkSwitcher.FromAssembly(typeof(LiveBatchBenchmarks).Assembly).Run(["--filter", filter, "--artifacts", Path.GetFullPath(artifacts)]);
return 0;

static string? ValueOf(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
