internal static class Verification
{
    internal static void Run()
    {
        foreach (var pattern in Enum.GetValues<TickPattern>())
        {
            var benchmark = new ScalarCatalogQueryBenchmarks { Pattern = pattern };
            benchmark.Setup();
            var direct = benchmark.DirectScalar();
            var query = benchmark.GeneratedQueryScalar();
            ScalarCatalogQueryBenchmarks.Require(direct, query, $"scalar/{pattern}");
            Console.WriteLine($"scalar/{pattern}: direct={direct} query={query}");
        }

        foreach (var rows in new[] { 1, 32, 10_000 })
        {
            var benchmark = new BatchCatalogQueryBenchmarks { Rows = rows };
            benchmark.Setup();
            var direct = benchmark.DirectBatch();
            var query = benchmark.GeneratedQueryBatch();
            ScalarCatalogQueryBenchmarks.Require(direct, query, $"batch/{rows}");
            Console.WriteLine($"batch/{rows}: direct={direct} query={query}");
        }

        var allocation = new ScalarCatalogQueryBenchmarks { Pattern = TickPattern.Alternating };
        allocation.Setup();
        for (var pass = 0; pass < 16; pass++)
            _ = allocation.GeneratedQueryScalar();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 128; pass++)
            _ = allocation.GeneratedQueryScalar();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
            throw new InvalidOperationException($"Warm generated query benchmark allocated {allocated} B.");
        Console.WriteLine($"allocation: 128 x {ScalarCatalogQueryBenchmarks.Operations} scalar generated query ticks retained {allocated} B");

        var batchAllocation = new BatchCatalogQueryBenchmarks { Rows = 10_000 };
        batchAllocation.Setup();
        for (var pass = 0; pass < 4; pass++)
            _ = batchAllocation.GeneratedQueryBatch();
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 16; pass++)
            _ = batchAllocation.GeneratedQueryBatch();
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
            throw new InvalidOperationException($"Warm generated batch benchmark allocated {allocated} B.");
        Console.WriteLine($"allocation: 16 x 64 x 10000 batch generated query ticks retained {allocated} B");
    }
}
