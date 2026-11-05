using BenchmarkDotNet.Running;
using Kinds;

if (args.Contains("--oracle"))
{
    var steps = args.Contains("--quick") ? 1000 : 10000;
    var failed = 0;
    foreach (var seed in new[] { 1, 2, 3, 4, 5 }) failed |= Oracle.Run(seed, steps, 64, true);
    return failed;
}

if (args.Contains("--smoke"))
    return Smoke.Run();

if (args.Length == 2 && args[0] == "--profile")
    return Smoke.Profile(args[1]);

BenchmarkRunner.Run<KindBenchmarks>(null, args);
return 0;
