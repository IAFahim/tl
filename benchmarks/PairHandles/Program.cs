using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using Tl;
using Tl.PairHandlesProbe;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var parityOnly = args.Contains("--parity");
var artifactsIndex = Array.IndexOf(args, "--artifacts");
var artifacts = artifactsIndex >= 0 && artifactsIndex + 1 < args.Length
    ? args[artifactsIndex + 1]
    : $"results/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var filterIndex = Array.IndexOf(args, "--filter");
var filter = filterIndex >= 0 && filterIndex + 1 < args.Length ? args[filterIndex + 1] : "*";

var failures = Parity.Run();
if (failures != 0) return 1;
if (parityOnly) return 0;

Console.WriteLine($"host: {Environment.OSVersion.VersionString}, {RuntimeInformation.FrameworkDescription}, procs {Environment.ProcessorCount}");
Console.WriteLine($"benchmark artifacts: {Path.GetFullPath(artifacts)}");

BenchmarkSwitcher.FromAssembly(typeof(PairHandleBenchmarks).Assembly).Run(["--filter", "*", "--artifacts", Path.GetFullPath(artifacts)]);
return 0;

static class Parity
{
    public static int Run()
    {
        const int rows = 8192;
        const int steps = 240;
        var bank = Host.BindBank();
        var handles = new ushort[rows];
        var positions = new ushort[rows];
        var effects = new float[rows];
        var oraclePositions = new ushort[rows];
        var oracleEffects = new float[rows];
        for (var i = 0; i < rows; i++)
        {
            handles[i] = bank[(i * 3 + i / 64) % Host.Variants];
            positions[i] = (ushort)(i % Host.Duration);
            oraclePositions[i] = positions[i];
            oracleEffects[i] = effects[i] = (i % 17) * 0.5f;
        }

        var groupedRows = new int[Host.Variants][];
        for (var variant = 0; variant < Host.Variants; variant++)
        {
            var count = 0;
            for (var i = 0; i < rows; i++)
                if (handles[i] == bank[variant])
                    count++;
            groupedRows[variant] = new int[count];
            var write = 0;
            for (var i = 0; i < rows; i++)
                if (handles[i] == bank[variant])
                    groupedRows[variant][write++] = i;
        }
        var groupPositions = new ushort[rows];
        var groupEffects = new float[rows];

        for (var step = 0; step < steps; step++)
        {
            var forward = step % 5 != 4;
            Timeline<LaneTrack, LaneClip>.Advance(handles, positions, forward, effects);
            for (var variant = 0; variant < Host.Variants; variant++)
            {
                var rowsOfVariant = groupedRows[variant];
                var write = 0;
                foreach (var row in rowsOfVariant)
                {
                    groupPositions[write] = oraclePositions[row];
                    groupEffects[write] = oracleEffects[row];
                    write++;
                }
                Timeline<LaneTrack, LaneClip>.Advance(bank[variant], groupPositions.AsSpan(0, write), forward, groupEffects.AsSpan(0, write));
                write = 0;
                foreach (var row in rowsOfVariant)
                {
                    oraclePositions[row] = groupPositions[write];
                    oracleEffects[row] = groupEffects[write];
                    write++;
                }
            }
        }

        var failures = 0;
        for (var i = 0; i < rows; i++)
        {
            if (positions[i] != oraclePositions[i])
            {
                Console.WriteLine($"parity position mismatch at row {i}: pair-typed {positions[i]} vs per-index {oraclePositions[i]}");
                failures++;
            }
            if (effects[i] != oracleEffects[i])
            {
                Console.WriteLine($"parity effect mismatch at row {i}: pair-typed {effects[i]} vs per-index {oracleEffects[i]}");
                failures++;
            }
        }
        Console.WriteLine(failures == 0
            ? $"PAIR-HANDLES PARITY PASS: {rows} rows x {steps} steps over {Host.Variants} variant timelines, forward and backward, bit-exact"
            : $"PAIR-HANDLES PARITY FAIL: {failures} mismatches");
        return failures;
    }
}
