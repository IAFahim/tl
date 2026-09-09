using BenchmarkDotNet.Running;

if (args is ["--verify"])
{
    Verification.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
