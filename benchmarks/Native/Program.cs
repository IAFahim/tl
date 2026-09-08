using BenchmarkDotNet.Running;
using Tl.Native;

if (args is ["--verify"])
{
    // The receipts check runs in GlobalSetup; running it standalone proves
    // the two engines agree before any measurement session.
    new NativeVsManaged().Setup();
    Console.WriteLine("Managed v0.3 sandbox and native v0.4 receipts agree (single and params-four).");
    return 0;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
return 0;
