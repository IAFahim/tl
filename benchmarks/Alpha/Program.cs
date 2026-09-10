using BenchmarkDotNet.Running;

if (args is ["--pmu", var scenario])
{
    Pmu.Run(scenario);
    return;
}

if (args is ["--verify"])
{
    Verification.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
