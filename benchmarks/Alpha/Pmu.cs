internal static class Pmu
{
    private const int WarmupPasses = 1024;
    private const int MeasuredPasses = 32768;

    public static void Run(string scenario)
    {
        if (scenario.StartsWith("matrix-", StringComparison.Ordinal))
        {
            RunMatrix(scenario);
            return;
        }

        switch (scenario)
        {
            case "sum-direct":
                RunSum(static benchmark => benchmark.DirectScalar());
                return;
            case "sum-typed":
                RunSum(static benchmark => benchmark.TypedScalar());
                return;
            case "sum-dynamic":
                RunSum(static benchmark => benchmark.DynamicScalar());
                return;
            case "combat-direct":
                RunCombat(static benchmark => benchmark.DirectScalar());
                return;
            case "combat-typed":
                RunCombat(static benchmark => benchmark.TypedScalar());
                return;
            case "combat-dynamic":
                RunCombat(static benchmark => benchmark.DynamicScalar());
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static void RunSum(Func<SumBenchmarks, SumReceipt> operation)
    {
        var benchmark = new SumBenchmarks { Pattern = SeekPattern.Forward };
        benchmark.Setup();
        for (var pass = 0; pass < WarmupPasses; pass++)
            _ = operation(benchmark);
        Console.WriteLine($"ready {MeasuredPasses * (long)SumBenchmarks.Operations}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (var pass = 0; pass < MeasuredPasses; pass++)
            receipt = Fold(receipt, operation(benchmark));
        Console.WriteLine($"{receipt} {MeasuredPasses * (long)SumBenchmarks.Operations}");
        Console.ReadLine();
    }

    private static void RunCombat(Func<CombatBenchmarks, CombatReceipt> operation)
    {
        var benchmark = new CombatBenchmarks { Pattern = SeekPattern.Forward };
        benchmark.Setup();
        for (var pass = 0; pass < WarmupPasses; pass++)
            _ = operation(benchmark);
        Console.WriteLine($"ready {MeasuredPasses * (long)CombatBenchmarks.Operations}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (var pass = 0; pass < MeasuredPasses; pass++)
            receipt = Fold(receipt, operation(benchmark));
        Console.WriteLine($"{receipt} {MeasuredPasses * (long)CombatBenchmarks.Operations}");
        Console.ReadLine();
    }

    private static void RunMatrix(string scenario)
    {
        var separator = scenario.IndexOf('-', "matrix-".Length);
        if (separator < 0 || !Enum.TryParse<SeekCase>(scenario[(separator + 1)..], out var seekCase))
            throw new ArgumentOutOfRangeException(nameof(scenario));

        var facade = scenario["matrix-".Length..separator];
        var benchmark = new SignedSeekBenchmarks { Case = seekCase };
        benchmark.Setup();
        Func<SignedSeekBenchmarks, SumReceipt> operation = facade switch
        {
            "direct" => static value => value.Direct(),
            "typed" => static value => value.Typed(),
            "dynamic" => static value => value.Dynamic(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        var framesPerPass = SignedSeekBenchmarks.Calls * (long)benchmark.FramesPerCall;
        var warmupPasses = Math.Max(1L, 4_194_304L / framesPerPass);
        var measuredPasses = Math.Max(1L, 268_435_456L / framesPerPass);
        for (long pass = 0; pass < warmupPasses; pass++)
            _ = operation(benchmark);
        var measuredFrames = measuredPasses * framesPerPass;
        Console.WriteLine($"ready {measuredFrames}");
        if (Console.ReadLine() != "go")
            throw new InvalidOperationException();
        long receipt = 0;
        for (long pass = 0; pass < measuredPasses; pass++)
            receipt = Fold(receipt, operation(benchmark));
        Console.WriteLine($"{receipt} {measuredFrames}");
        Console.ReadLine();
    }

    private static long Fold(long value, SumReceipt receipt)
    {
        value = unchecked(value * 397L + receipt.Position);
        value = unchecked(value * 397L + receipt.GameTick);
        value = unchecked(value * 397L + (byte)receipt.Flags);
        value = unchecked(value * 397L + receipt.SumBits);
        return unchecked(value * 397L + receipt.Successes);
    }

    private static long Fold(long value, CombatReceipt receipt)
    {
        value = unchecked(value * 397L + receipt.Position);
        value = unchecked(value * 397L + receipt.GameTick);
        value = unchecked(value * 397L + (byte)receipt.Flags);
        value = unchecked(value * 397L + receipt.PoseXBits);
        value = unchecked(value * 397L + receipt.PoseYBits);
        value = unchecked(value * 397L + receipt.HealthBits);
        value = unchecked(value * 397L + receipt.TickSum);
        value = unchecked(value * 397L + receipt.Calls);
        value = unchecked(value * 397L + receipt.Starts);
        value = unchecked(value * 397L + receipt.Interior);
        value = unchecked(value * 397L + receipt.Ends);
        return unchecked(value * 397L + receipt.Successes);
    }
}
