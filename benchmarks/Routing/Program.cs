using BenchmarkDotNet.Running;

if (args is ["--verify"])
{
    foreach (var scale in Enum.GetValues<RoutingScale>())
    {
        var benchmark = new RoutingBenchmarks { Scale = scale };
        benchmark.Setup();
        Console.WriteLine($"{scale}: {benchmark.Dynamic()}");
    }

    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
