using BenchmarkDotNet.Running;
using Tl.ConsumerFusion;

if (args is ["--verify"])
{
    Verification.Run();
    return;
}

if (args is ["--asm"])
{
    Verification.Run();
    Inspect<SumConsumer>();
    Inspect<StateConsumer>();
    Inspect<EffectConsumer>();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

static void Inspect<TConsumer>() where TConsumer : unmanaged, IConsumer<TConsumer>
{
    var benchmark = new ConsumerBenchmarks<TConsumer> { Pattern = TickPattern.Random };
    benchmark.Setup();
    ulong receipt = 0;
    for (var i = 0; i < 1000; i++)
    {
        receipt += (uint)benchmark.FusedSingle().SumBits;
        receipt += (uint)benchmark.FusedBatch8().SumBits;
    }
    benchmark.Cleanup();
    Console.WriteLine($"{typeof(TConsumer).Name} disassembly receipt: {receipt}");
}
