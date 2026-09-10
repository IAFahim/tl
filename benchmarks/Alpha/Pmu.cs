internal static class Pmu
{
    private const int WarmupPasses = 1024;
    private const int MeasuredPasses = 32768;

    public static void Run(string scenario)
    {
        switch (scenario)
        {
            case "sum-direct":
                RunSum(static benchmark => benchmark.DirectScalar());
                return;
            case "sum-public":
                RunSum(static benchmark => benchmark.PublicScalar());
                return;
            case "combat-direct":
                RunCombat(static benchmark => benchmark.DirectScalar());
                return;
            case "combat-public":
                RunCombat(static benchmark => benchmark.PublicScalar());
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static void RunSum(Func<SumBenchmarks, SumReceipt> operation)
    {
        var benchmark = new SumBenchmarks { Pattern = TickPattern.Sequential };
        benchmark.Setup();
        for (var pass = 0; pass < WarmupPasses; pass++)
            _ = operation(benchmark);
        Console.WriteLine($"ready {MeasuredPasses * (long)SumBenchmarks.Operations}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (var pass = 0; pass < MeasuredPasses; pass++)
            receipt += operation(benchmark).SumBits;
        Console.WriteLine($"{receipt} {MeasuredPasses * (long)SumBenchmarks.Operations}");
        Console.ReadLine();
    }

    private static void RunCombat(Func<CombatBenchmarks, CombatReceipt> operation)
    {
        var benchmark = new CombatBenchmarks { Pattern = TickPattern.Sequential };
        benchmark.Setup();
        for (var pass = 0; pass < WarmupPasses; pass++)
            _ = operation(benchmark);
        Console.WriteLine($"ready {MeasuredPasses * (long)CombatBenchmarks.Operations}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (var pass = 0; pass < MeasuredPasses; pass++)
            receipt += operation(benchmark).TickSum;
        Console.WriteLine($"{receipt} {MeasuredPasses * (long)CombatBenchmarks.Operations}");
        Console.ReadLine();
    }
}
