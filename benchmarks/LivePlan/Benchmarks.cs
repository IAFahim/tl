using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

namespace LivePlan;

public sealed class LivePlanConfig : ManualConfig
{
    public LivePlanConfig()
    {
        AddJob(Job.Default.WithWarmupCount(8).WithIterationCount(16).WithIterationTime(TimeInterval.FromMilliseconds(200)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public enum PlanShape
{
    ColumnOne,
    ColumnRuns8,
    ColumnAlternating8,
    ColumnRuns8Waves,
    ColumnAlternating8Waves,
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(LivePlanConfig))]
public class LivePlanBenchmarks
{
    const int Rows = 100_000;

    ushort[] _positions = null!;
    ushort[] _handles = null!;
    float[] _fold = null!;
    int[] _effects = null!;

    public long Sink;

    [Params(PlanShape.ColumnOne, PlanShape.ColumnRuns8, PlanShape.ColumnAlternating8, PlanShape.ColumnRuns8Waves, PlanShape.ColumnAlternating8Waves)]
    public PlanShape Shape;

    [GlobalSetup]
    public void Setup()
    {
        var bank = Host.BindBank();
        _positions = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            _positions[i] = Waves ? (ushort)(i / 100 % Host.Duration) : (ushort)(i % Host.Duration);
        _handles = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            _handles[i] = Shape switch
            {
                PlanShape.ColumnOne => bank[0],
                PlanShape.ColumnRuns8 or PlanShape.ColumnRuns8Waves => bank[i * Host.Variants / Rows],
                _ => bank[i % Host.Variants],
            };
        _fold = new float[Rows];
        _effects = new int[Rows];
    }

    bool Waves => Shape is PlanShape.ColumnRuns8Waves or PlanShape.ColumnAlternating8Waves;

    [Benchmark]
    public void Apply()
    {
        var columns = new ColumnSet();
        columns.Add(_fold);
        columns.Add(_effects);
        Timeline<BenchTrack, BenchClip>.Apply(_handles, _positions, true, in columns);
        Sink += _effects[0];
    }
}
