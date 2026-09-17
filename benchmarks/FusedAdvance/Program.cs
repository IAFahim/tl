using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using Tl.FusedAdvanceProbe;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var parityOnly = args.Contains("--parity");
var artifactsIndex = Array.IndexOf(args, "--artifacts");
var artifacts = artifactsIndex >= 0 && artifactsIndex + 1 < args.Length
    ? args[artifactsIndex + 1]
    : $"results/{DateTime.UtcNow:yyyyMMdd-HHmmss}";

var failures = Parity.Run();
if (failures != 0) return 1;
if (parityOnly) return 0;

Console.WriteLine($"host: {Environment.OSVersion.VersionString}, {RuntimeInformation.FrameworkDescription}, procs {Environment.ProcessorCount}");
Console.WriteLine($"benchmark artifacts: {Path.GetFullPath(artifacts)}");
BenchmarkSwitcher.FromAssembly(typeof(AdvanceBenchmarks).Assembly).Run(["--filter", "*", "--artifacts", Path.GetFullPath(artifacts)]);
return 0;
