internal static class Pmu
{
    private const long WarmupFrames = 4_194_304L;
    private const long MeasuredFrames = 268_435_456L;

    internal static void Run(string scenario)
    {
        switch (scenario)
        {
            case "scalar-direct":
            {
                var benchmark = new ScalarCatalogQueryBenchmarks { Pattern = TickPattern.Forward };
                benchmark.Setup();
                Measure(ScalarCatalogQueryBenchmarks.Operations, benchmark.DirectScalar);
                return;
            }
            case "scalar-query":
            {
                var benchmark = new ScalarCatalogQueryBenchmarks { Pattern = TickPattern.Forward };
                benchmark.Setup();
                Measure(ScalarCatalogQueryBenchmarks.Operations, benchmark.GeneratedQueryScalar);
                return;
            }
            case "batch-direct":
            {
                var benchmark = new BatchCatalogQueryBenchmarks { Rows = 10_000 };
                benchmark.Setup();
                Measure(BatchCatalogQueryBenchmarks.Frames * (long)benchmark.Rows, benchmark.DirectBatch);
                return;
            }
            case "batch-query":
            {
                var benchmark = new BatchCatalogQueryBenchmarks { Rows = 10_000 };
                benchmark.Setup();
                Measure(BatchCatalogQueryBenchmarks.Frames * (long)benchmark.Rows, benchmark.GeneratedQueryBatch);
                return;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static void Measure(long framesPerPass, Func<BenchmarkReceipt> operation)
    {
        var warmupPasses = Math.Max(1L, WarmupFrames / framesPerPass);
        var measuredPasses = Math.Max(1L, MeasuredFrames / framesPerPass);
        for (long pass = 0; pass < warmupPasses; pass++)
            _ = operation();
        var totalFrames = measuredPasses * framesPerPass;
        Console.WriteLine($"ready {totalFrames}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (long pass = 0; pass < measuredPasses; pass++)
            receipt = Fold(receipt, operation());
        Console.WriteLine($"{receipt} {totalFrames}");
        _ = Console.ReadLine();
    }

    private static long Fold(long value, BenchmarkReceipt receipt)
    {
        value = unchecked(value * 397L + receipt.StateHash);
        value = unchecked(value * 397L + receipt.ValueHash);
        value = unchecked(value * 397L + receipt.OrderHash);
        value = unchecked(value * 397L + receipt.GameTickHash);
        return unchecked(value * 397L + receipt.Calls);
    }
}
