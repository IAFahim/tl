using System.Security.Cryptography;
using Tl;

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

        foreach (var shape in Enum.GetValues<TimelineShape>())
        {
            foreach (var pattern in Enum.GetValues<TickPattern>())
            {
                var benchmark = new ShapeCase(shape, pattern);
                var direct = benchmark.Direct();
                var query = benchmark.Generated();
                ScalarCatalogQueryBenchmarks.Require(direct, query, $"shape/{shape}/{pattern}");
                Console.WriteLine($"shape/{shape}/{pattern}: direct={direct} query={query}");
            }

            var allocationCase = new ShapeCase(shape, TickPattern.Forward);
            for (var pass = 0; pass < 16; pass++)
                _ = allocationCase.Generated();
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 128; pass++)
                _ = allocationCase.Generated();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated != 0)
                throw new InvalidOperationException($"Warm generated {shape} shape allocated {allocated} B.");
            Console.WriteLine($"allocation: 128 x {ShapeCase.Operations} {shape} generated ticks retained {allocated} B");
        }

        foreach (var pattern in Enum.GetValues<TickPattern>())
        {
            var component = new ComponentCase(pattern);
            var direct = component.Direct();
            var query = component.Generated();
            ScalarCatalogQueryBenchmarks.Require(direct, query, $"component/{pattern}");
            Console.WriteLine($"component/{pattern}: direct={direct} query={query}");
        }

        var componentAllocation = new ComponentCase(TickPattern.Forward);
        for (var pass = 0; pass < 16; pass++)
            _ = componentAllocation.Generated();
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 128; pass++)
            _ = componentAllocation.Generated();
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
            throw new InvalidOperationException($"Warm generated component shape allocated {allocated} B.");
        Console.WriteLine($"allocation: 128 x {ComponentCase.Operations} component generated ticks retained {allocated} B");

        var mixed = new MixedAssetCase();
        var mixedDirect = mixed.Direct();
        var mixedQuery = mixed.Generated();
        ScalarCatalogQueryBenchmarks.Require(mixedDirect, mixedQuery, "mixed-assets");
        Console.WriteLine($"mixed-assets: direct={mixedDirect} query={mixedQuery}");
        for (var pass = 0; pass < 8; pass++)
            _ = mixed.Generated();
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 32; pass++)
            _ = mixed.Generated();
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
            throw new InvalidOperationException($"Warm generated mixed-asset shape allocated {allocated} B.");
        Console.WriteLine($"allocation: 32 x {MixedAssetCase.Frames} x {MixedAssetCase.Rows} mixed-asset generated ticks retained {allocated} B");

        foreach (var shape in new[]
                 {
                     TimelineShape.OneTrack,
                     TimelineShape.ThreeTracks,
                     TimelineShape.Blend,
                     TimelineShape.SixteenTracks,
                     TimelineShape.TwoHundredFiftySixTracks,
                 })
        {
            foreach (var pattern in Enum.GetValues<TickPattern>())
            {
                var alpha = new ShapeCase(shape, pattern);
                using var facade = new DataAuthoredCase(shape, pattern);
                var direct = alpha.Direct();
                var generated = alpha.Generated();
                var dataAuthored = facade.DataAuthored();
                ScalarCatalogQueryBenchmarks.Require(direct, generated, $"data-authored/{shape}/generated/{pattern}");
                ScalarCatalogQueryBenchmarks.Require(direct, dataAuthored, $"data-authored/{shape}/facade/{pattern}");
                Console.WriteLine($"data-authored/{shape}/{pattern}: direct={direct} generated={generated} facade={dataAuthored}");

                if (shape == TimelineShape.OneTrack)
                {
                    const string kernelHash = "65a912cd2c3f9565072beed5ef78a2bdc3f5ba25bb58c59e8f325ef0b36d96bf";
                    var baked = DataAuthoredCase.Bake(TimelineShape.OneTrack, DataAuthoredMode.Standard);
                    var actualHash = Convert.ToHexString(SHA256.HashData(baked)).ToLowerInvariant();
                    if (actualHash != kernelHash)
                        throw new InvalidOperationException($"OneTrack baked bytes hash {actualHash} does not match the committed OneTrackKernel.g.cs hash {kernelHash}.");
                    var interpreterBytes = (byte[])baked.Clone();
                    interpreterBytes[40] = 0xA5;
                    using var interpreterCase = new DataAuthoredCase(TimelineShape.OneTrack, pattern, DataAuthoredMode.Standard, interpreterBytes);
                    var interpreter = interpreterCase.DataAuthored();
                    ScalarCatalogQueryBenchmarks.Require(direct, interpreter, $"data-authored/{shape}/interpreter/{pattern}");
                    Console.WriteLine($"kernel-lane/{shape}/{pattern}: facade-is-hash-bound-kernel={dataAuthored} interpreter={interpreter}");
                }
            }

            using var allocationCase = new DataAuthoredCase(shape, TickPattern.Forward);
            for (var pass = 0; pass < 16; pass++)
                _ = allocationCase.DataAuthored();
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 128; pass++)
                _ = allocationCase.DataAuthored();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated != 0)
                throw new InvalidOperationException($"Warm data-authored {shape} facade allocated {allocated} B.");
            Console.WriteLine($"allocation: 128 x {DataAuthoredCase.Operations} {shape} data-authored facade ticks retained {allocated} B");
        }
    }
}
