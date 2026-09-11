internal static class Pmu
{
    private const long WarmupFrames = 4_194_304L;
    private const long MeasuredFrames = 268_435_456L;
    private const long WideWarmupFrames = 8_192L;
    private const long WideMeasuredFrames = 262_144L;

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
            case "shape-one-direct":
                MeasureShape(TimelineShape.OneTrack, false);
                return;
            case "shape-one-query":
                MeasureShape(TimelineShape.OneTrack, true);
                return;
            case "shape-three-direct":
                MeasureShape(TimelineShape.ThreeTracks, false);
                return;
            case "shape-three-query":
                MeasureShape(TimelineShape.ThreeTracks, true);
                return;
            case "shape-sixteen-direct":
                MeasureShape(TimelineShape.SixteenTracks, false);
                return;
            case "shape-sixteen-query":
                MeasureShape(TimelineShape.SixteenTracks, true);
                return;
            case "shape-256-direct":
                MeasureShape(TimelineShape.TwoHundredFiftySixTracks, false);
                return;
            case "shape-256-query":
                MeasureShape(TimelineShape.TwoHundredFiftySixTracks, true);
                return;
            case "shape-gap-direct":
                MeasureShape(TimelineShape.Gap, false);
                return;
            case "shape-gap-query":
                MeasureShape(TimelineShape.Gap, true);
                return;
            case "shape-blend-direct":
                MeasureShape(TimelineShape.Blend, false);
                return;
            case "shape-blend-query":
                MeasureShape(TimelineShape.Blend, true);
                return;
            case "component-direct":
            {
                var benchmark = new ComponentCase(TickPattern.Forward);
                Measure(ComponentCase.Operations, benchmark.Direct);
                return;
            }
            case "component-query":
            {
                var benchmark = new ComponentCase(TickPattern.Forward);
                Measure(ComponentCase.Operations, benchmark.Generated);
                return;
            }
            case "mixed-assets-direct":
            {
                var benchmark = new MixedAssetCase();
                Measure(MixedAssetCase.Rows * MixedAssetCase.Frames, benchmark.Direct);
                return;
            }
            case "mixed-assets-query":
            {
                var benchmark = new MixedAssetCase();
                Measure(MixedAssetCase.Rows * MixedAssetCase.Frames, benchmark.Generated);
                return;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static void MeasureShape(TimelineShape shape, bool generated)
    {
        var benchmark = new ShapeCase(shape, TickPattern.Forward);
        Func<BenchmarkReceipt> operation = generated ? benchmark.Generated : benchmark.Direct;
        if (shape == TimelineShape.TwoHundredFiftySixTracks)
            Measure(ShapeCase.Operations, operation, WideWarmupFrames, WideMeasuredFrames);
        else
            Measure(ShapeCase.Operations, operation, WarmupFrames, MeasuredFrames);
    }

    private static void Measure(
        long framesPerPass,
        Func<BenchmarkReceipt> operation,
        long warmupFrames = WarmupFrames,
        long measuredFrames = MeasuredFrames)
    {
        var warmupPasses = Math.Max(1L, warmupFrames / framesPerPass);
        var measuredPasses = Math.Max(1L, measuredFrames / framesPerPass);
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
