using BenchmarkDotNet.Running;
using Tl.Algorithms;

if (args is ["--verify"])
{
    Verification.Run<SmallCode>();
    Verification.Run<MediumCode>();
    Verification.Run<LargeCode>();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
