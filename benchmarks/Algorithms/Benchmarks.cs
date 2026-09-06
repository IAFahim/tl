using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace Tl.Algorithms;

public sealed class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddJob(Job.Default
            .WithId("steady")
            .WithWarmupCount(16)
            .WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(BenchConfig))]
public abstract class Sampling<T> where T : struct, IGenerated
{
    public const int Operations = 65536;
    protected Fixture Data = null!;
    protected int[] Ticks = null!;

    protected void Prepare(int step)
    {
        Data = T.Create();
        Ticks = new int[Operations];
        uint random = 0xA61D043B;

        for (var i = 0; i < Ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            Ticks[i] = step == 0 ? (int)(random % (uint)Data.Duration) : (int)((long)i * step % Data.Duration);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt Binary()
    {
        SumSink sink = default;
        foreach (var tick in Ticks)
            Data.Emit(Data.Binary(tick), tick, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt Dense()
    {
        SumSink sink = default;
        foreach (var tick in Ticks)
            Data.Emit(Data.Dense[tick], tick, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt Rank()
    {
        SumSink sink = default;
        foreach (var tick in Ticks)
            Data.Emit(Data.Rank(tick), tick, ref sink);
        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt GeneratedTree()
    {
        SumSink sink = default;
        foreach (var tick in Ticks)
            T.Tree(tick, Data.Payloads, ref sink);
        return sink.Result;
    }
}

[GenericTypeArguments(typeof(SmallCode))]
[GenericTypeArguments(typeof(MediumCode))]
[GenericTypeArguments(typeof(LargeCode))]
public class Playback<T> : Sampling<T> where T : struct, IGenerated
{
    [Params(1, 7)]
    public int Step { get; set; }

    [GlobalSetup]
    public void Setup() => Prepare(Step);

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt Cursor()
    {
        SumSink sink = default;
        var state = 0;
        var previous = -1;

        foreach (var tick in Ticks)
        {
            if (tick < previous)
                state = 0;
            Data.Cursor(tick, ref state, ref sink);
            previous = tick;
        }

        return sink.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt GeneratedState()
    {
        SumSink sink = default;
        var state = 0;
        var previous = -1;

        foreach (var tick in Ticks)
        {
            if (tick < previous)
                state = 0;
            T.State(tick, ref state, Data.Payloads, ref sink);
            previous = tick;
        }

        return sink.Result;
    }
}

[GenericTypeArguments(typeof(SmallCode))]
[GenericTypeArguments(typeof(MediumCode))]
[GenericTypeArguments(typeof(LargeCode))]
public class Seeking<T> : Sampling<T> where T : struct, IGenerated
{
    [GlobalSetup]
    public void Setup() => Prepare(0);
}
