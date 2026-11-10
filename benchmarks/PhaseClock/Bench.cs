using System.Diagnostics;

namespace PhaseClock;

public static class Bench
{
    const int Off = 37;

    public static void Run(List<Case> cases)
    {
        const int warmupTicks = 8000;
        const int rounds = 9;
        const int ticksPerRound = 20000;
        Console.WriteLine("case | kernel | vector ns/entity (min of 9x20000)");
        foreach (var c in cases)
        {
            var acc = new float[Data.N + Off + 64];
            var first = Measure(c, acc, warmupTicks, rounds, ticksPerRound);
            var second = Measure(c, acc, 2000, rounds, ticksPerRound);
            Console.WriteLine($"{c.Name} | {c.Kernel} | {Math.Min(first, second):F3} sink={c.Sink:F0}");
        }
    }

    static double Measure(Case c, float[] acc, int warmup, int rounds, int ticksPerRound)
    {
        for (var f = 0; f < warmup; f++) c.VectorTick(acc, Off, c, f);
        var samples = new double[rounds];
        for (var r = 0; r < rounds; r++)
        {
            Array.Clear(acc, Off, Data.N);
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) c.VectorTick(acc, Off, c, f);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / ticksPerRound / Data.N;
            c.Sink += Checker.HashSlice(acc, Off, Data.N) & 0xff;
        }
        Array.Sort(samples);
        return samples[0];
    }
}
