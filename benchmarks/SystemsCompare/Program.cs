using BenchmarkDotNet.Running;
using SystemsCompare;

if (args.Contains("--verify"))
{
    var report = CompareBenchmarks.VerifyClosedForm();
    Console.WriteLine(report);
    return report.Contains("MISMATCH") ? 1 : 0;
}

if (args.Contains("--smoke"))
    return Smoke.Run();

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
return 0;

internal static class Smoke
{
    public static int Run()
    {
        var first = Pass();
        var second = Pass();
        Console.WriteLine($"SMOKE hash1={first:x16} hash2={second:x16} equal={first == second}");
        return first == second ? 0 : 1;
    }

    static ulong Pass()
    {
        var bench = new CompareBenchmarks();
        bench.Setup();
        RunShape(bench, "NewA", () => bench.NewA());
        RunShape(bench, "NewB", () => bench.NewB());
        RunShape(bench, "NewC", () => bench.NewC());
        RunShape(bench, "NewD", () => bench.NewD());
        Console.WriteLine($"SMOKE sink={bench.Sink}");
        return bench.Checksum();
    }

    static void RunShape(CompareBenchmarks bench, string name, Action run)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = DateTime.UtcNow;
        run();
        var elapsed = DateTime.UtcNow - start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"SMOKE {name} entities={CompareBenchmarks.N} ms={elapsed.TotalMilliseconds:F1} bytes={allocated} sum={bench.Checksum():x16}");
    }
}
