using BenchmarkDotNet.Running;
using System.Diagnostics;
using Tl.FusionExperiment;

if (args is ["--verify"])
{
    Verification.Run();
    return;
}

if (args is ["--asm"])
{
    Verification.Run();
    var benchmark = new FusionBenchmarks { Pattern = TickPattern.Random };
    benchmark.Setup();
    for (var i = 0; i < 20000; i++)
    {
        _ = benchmark.FusedSingle();
        _ = benchmark.FusedBatch8();
    }
    Console.WriteLine(benchmark.FusedSingle());
    benchmark.Cleanup();
    return;
}

if (args is ["--counters", var engine, var pattern])
{
    var benchmark = new FusionBenchmarks { Pattern = Enum.Parse<TickPattern>(pattern) };
    benchmark.Setup();
    Func<SumReceipt> operation = engine switch
    {
        "InterpreterSingle" => benchmark.InterpreterSingle,
        "CompiledSingle" => benchmark.CompiledSingle,
        "FusedSingle" => benchmark.FusedSingle,
        "InterpreterBatch8" => benchmark.InterpreterBatch8,
        "CompiledBatch8" => benchmark.CompiledBatch8,
        "FusedBatch8" => benchmark.FusedBatch8,
        _ => throw new ArgumentException("Unknown benchmark method.", nameof(engine))
    };
    var start = Stopwatch.GetTimestamp();
    ulong checksum = 0;
    while (Stopwatch.GetElapsedTime(start).TotalSeconds < 2)
        checksum += (uint)operation().SumBits;
    const int invocations = 8192;
    Console.WriteLine($"ready {invocations * FusionBenchmarks.Operations}");
    if (Console.ReadLine() != "go")
        throw new InvalidOperationException("Expected counter start signal.");
    for (var i = 0; i < invocations; i++)
    {
        var receipt = operation();
        checksum += (uint)receipt.SumBits + receipt.Playback.Tick + receipt.Playback.Cycles + (uint)receipt.Playback.Flags;
    }
    Console.WriteLine($"done {checksum}");
    if (Console.ReadLine() != "stop")
        throw new InvalidOperationException("Expected counter stop signal.");
    benchmark.Cleanup();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
