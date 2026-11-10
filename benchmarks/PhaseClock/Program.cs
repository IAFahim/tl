using PhaseClock;

Console.WriteLine($"Vector HW accel={System.Numerics.Vector.IsHardwareAccelerated} floatCount={System.Numerics.Vector<float>.Count}");
if (args.Contains("--probe")) return Probe.Run();
if (args.Contains("--large-verify")) return LargeBench.Run(true);
if (args.Contains("--large")) return LargeBench.Run(false);
if (args.Contains("--worldbench")) return WorldBench.RunWorld();

if (args.Contains("--verify") || !args.Contains("--bench"))
{
    var cases = Cases.All();
    var failed = Checker.Run(cases);
    Console.WriteLine(failed == 0 ? "VERIFY PASS" : $"VERIFY FAIL {failed}");
    if (failed != 0) return 1;
}

if (args.Contains("--bench") || args.Length == 0)
{
    var cases = Cases.All();
    Bench.Run(cases);
}
return 0;
