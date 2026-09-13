using System.Security.Cryptography;

internal static class Verification
{
    internal static void Run()
    {
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
                var dataAuthored = facade.DataAuthored();
                ScalarCatalogQueryBenchmarks.Require(direct, dataAuthored, $"data-authored/{shape}/facade/{pattern}");
                Console.WriteLine($"data-authored/{shape}/{pattern}: direct={direct} facade={dataAuthored}");

                if (TimelineKernelHashes.Committed.TryGetValue(shape, out var kernelHash))
                {
                    var baked = DataAuthoredCase.Bake(shape, DataAuthoredMode.Standard);
                    var actualHash = Convert.ToHexString(SHA256.HashData(baked)).ToLowerInvariant();
                    if (actualHash != kernelHash)
                        throw new InvalidOperationException($"{shape} baked bytes hash {actualHash} does not match the committed kernel hash {kernelHash}.");
                    var interpreterBytes = (byte[])baked.Clone();
                    interpreterBytes[40] = 0xA5;
                    using var interpreterCase = new DataAuthoredCase(shape, pattern, DataAuthoredMode.Standard, interpreterBytes);
                    var interpreter = interpreterCase.DataAuthored();
                    DataAuthoredCase.AssertKernelBound(facade, true);
                    DataAuthoredCase.AssertKernelBound(interpreterCase, false);
                    ScalarCatalogQueryBenchmarks.Require(direct, interpreter, $"data-authored/{shape}/interpreter/{pattern}");
                    Console.WriteLine($"kernel-lane/{shape}/{pattern}: facade-is-hash-bound-kernel={dataAuthored} interpreter={interpreter}");
                }
            }

            using var allocationCase = new DataAuthoredCase(shape, TickPattern.Forward);
            for (var pass = 0; pass < 16; pass++)
                _ = allocationCase.DataAuthored();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 128; pass++)
                _ = allocationCase.DataAuthored();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated != 0)
                throw new InvalidOperationException($"Warm data-authored {shape} facade allocated {allocated} B.");
            Console.WriteLine($"allocation: 128 x {DataAuthoredCase.Operations} {shape} data-authored facade ticks retained {allocated} B");
        }
    }
}
