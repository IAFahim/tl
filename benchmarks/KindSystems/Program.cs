using BenchmarkDotNet.Running;
using KindBench;

if (args.Contains("--smoke")) return Smoke.Run();
if (args.Length == 2 && args[0] == "--profile") return Smoke.Profile(args[1]);
BenchmarkRunner.Run<KindBenchmarks>(null, args);
return 0;
