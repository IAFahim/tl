using System.Diagnostics;

namespace PhaseClock;

public static class Probe
{
    public static int Run()
    {
        var cases = Cases.All();
        var c = cases[0];
        var acc = new float[Data.N + 100];
        for (var f = 0; f < 8000; f++) Kernels.ScalarPhase(acc, 37, c.Row, c.Phase, c.Lane, f, c.Duration, 1);
        var sw = Stopwatch.StartNew();
        for (var f = 0; f < 40000; f++) Kernels.ScalarPhase(acc, 37, c.Row, c.Phase, c.Lane, f, c.Duration, 1);
        sw.Stop();
        Console.WriteLine($"direct ScalarPhase uniform: {sw.Elapsed.TotalSeconds * 1e9 / 40000 / Data.N:F3} ns/entity sink={Checker.Hash(acc)}");
        var sw2 = Stopwatch.StartNew();
        for (var f = 0; f < 40000; f++) Kernels.BroadcastDense(acc, 37, 6f, Data.N);
        sw2.Stop();
        Console.WriteLine($"direct BroadcastDense: {sw2.Elapsed.TotalSeconds * 1e9 / 40000 / Data.N:F3} ns/entity");
        var c2 = cases[2];
        for (var f = 0; f < 8000; f++) Kernels.ScalarPhase(acc, 37, c2.Row, c2.Phase, c2.Lane, f, c2.Duration, 1);
        var sw3 = Stopwatch.StartNew();
        for (var f = 0; f < 40000; f++) Kernels.ScalarPhase(acc, 37, c2.Row, c2.Phase, c2.Lane, f, c2.Duration, 1);
        sw3.Stop();
        Console.WriteLine($"direct ScalarPhase mixed40: {sw3.Elapsed.TotalSeconds * 1e9 / 40000 / Data.N:F3} ns/entity");
        var sw4 = Stopwatch.StartNew();
        for (var f = 0; f < 40000; f++) Kernels.RunBroadcast(acc, 37, c2.Row, c2.Runs!, c2.Lane, f, c2.Duration, 1);
        sw4.Stop();
        Console.WriteLine($"direct RunBroadcast mixed40: {sw4.Elapsed.TotalSeconds * 1e9 / 40000 / Data.N:F3} ns/entity");
        return 0;
    }
}
