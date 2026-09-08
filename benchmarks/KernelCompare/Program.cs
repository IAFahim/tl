using BenchmarkDotNet.Running;

if (args is ["--verify"])
{
    KernelBenchmarks.Verify();
    return;
}

BenchmarkRunner.Run<KernelBenchmarks>(args: args);
