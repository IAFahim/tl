namespace PhaseClock;

public static class Checker
{
    const int Off = 37;

    public static ulong Hash(float[] acc) => HashSlice(acc, 0, acc.Length);

    public static ulong HashSlice(float[] acc, int off, int count)
    {
        var h = 14695981039346656037ul;
        for (var i = 0; i < count; i++)
        {
            var bits = (uint)BitConverter.SingleToInt32Bits(acc[off + i]);
            h = (h ^ bits) * 1099511628211ul;
        }
        return h;
    }

    public static int Run(List<Case> cases)
    {
        var failed = 0;
        foreach (var c in cases)
        {
            var expected = new float[Data.N + Off + 64];
            var actual = new float[Data.N + Off + 64];
            const int ticks = 97;
            for (var f = 0; f < ticks; f++) c.ScalarTick(expected, Off, c, f);
            for (var f = 0; f < ticks; f++) c.VectorTick(actual, Off, c, f);
            var scalarHash = HashSlice(expected, Off, Data.N);
            var vectorHash = HashSlice(actual, Off, Data.N);
            var equal = scalarHash == vectorHash;
            if (!equal) failed++;
            Console.WriteLine($"{(equal ? "PASS" : "FAIL")} {c.Name} scalar={scalarHash:x16} vector={vectorHash:x16}");
        }
        var probe = new float[Data.N + Off + 64];
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 200; f++) cases[0].VectorTick(probe, Off, cases[0], f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"ALLOC warm BroadcastDense over 200 ticks: {allocated} B");
        if (allocated != 0) failed++;
        Console.WriteLine($"SINK probe[0]={probe[Off]}");
        return failed;
    }
}
