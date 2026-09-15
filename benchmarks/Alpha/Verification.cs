internal static class Verification
{
    internal static void Run()
    {
        foreach (var shape in Enum.GetValues<TimelineShape>())
            foreach (var pattern in Enum.GetValues<TickPattern>())
            {
                using var lane = new LaneCase(shape, pattern);
                var actual = lane.Run();
                var expected = LaneCase.Oracle(shape, pattern);
                if (actual != expected)
                    throw new InvalidOperationException($"lane/{shape}/{pattern}: {actual} != oracle {expected}.");
                Console.WriteLine($"lane/{shape}/{pattern}: {actual} == oracle");
            }

        using var allocationCase = new LaneCase(TimelineShape.SixteenTracks, TickPattern.Forward);
        for (var pass = 0; pass < 16; pass++)
            _ = allocationCase.Run();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 128; pass++)
            _ = allocationCase.Run();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
            throw new InvalidOperationException($"Warm lane run allocated {allocated} B.");
        Console.WriteLine($"allocation: 128 x {LaneCase.Operations} sixteen-track lane runs retained {allocated} B");
    }
}
