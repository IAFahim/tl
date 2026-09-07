using BenchmarkDotNet.Running;
using Tl.Review;

if (args is ["--verify"])
{
    foreach (int count in new[] { 16, 512 })
    foreach (bool sequential in new[] { false, true })
        new Movement { Clips = count, Sequential = sequential }.Setup();
    Console.WriteLine("Before/after movement receipts match for both sizes, orders and batch shapes.");
    return;
}
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
