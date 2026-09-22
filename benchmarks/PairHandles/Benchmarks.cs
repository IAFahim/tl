using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

namespace PairHandles;

public sealed class PairHandlesConfig : ManualConfig
{
    public PairHandlesConfig()
    {
        AddJob(Job.Default.WithWarmupCount(8).WithIterationCount(16).WithIterationTime(TimeInterval.FromMilliseconds(200)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public enum ShapeKind
{
    LaneUniform,
    PairOne,
    PairRuns8,
    PairAlternating8,
    PairRuns8Waves,
    PairBlocks8Waves,
    PairAlternating8Waves,
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(PairHandlesConfig))]
public class PairHandleBenchmarks
{
    const int Rows = 100_000;

    ushort[] _positions = null!;
    ushort[] _handles = null!;
    float[] _effects = null!;

    public long Sink;

    [Params(ShapeKind.LaneUniform, ShapeKind.PairOne, ShapeKind.PairRuns8, ShapeKind.PairAlternating8, ShapeKind.PairRuns8Waves, ShapeKind.PairBlocks8Waves, ShapeKind.PairAlternating8Waves)]
    public ShapeKind Shape;

    [GlobalSetup]
    public void Setup()
        => (_positions, _handles, _effects) = Build(Shape, Rows);

    internal static (ushort[] Positions, ushort[] Handles, float[] Effects) Build(ShapeKind shape, int rows)
    {
        var gold = Host.SlotGoldLane();
        var bank = Host.BindBank();
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = HasWaves(shape)
                ? (ushort)(i / 100 % Host.Duration)
                : (ushort)(i % Host.Duration);
        var handles = new ushort[rows];
        for (var i = 0; i < rows; i++)
            handles[i] = shape switch
            {
                ShapeKind.LaneUniform => gold,
                ShapeKind.PairOne => bank[0],
                ShapeKind.PairRuns8 or ShapeKind.PairRuns8Waves => bank[i * Host.Variants / rows],
                ShapeKind.PairBlocks8Waves => bank[i / 100 % Host.Variants],
                _ => bank[i % Host.Variants],
            };
        return (positions, handles, Seeds.Effects(rows));
    }

    internal static bool HasWaves(ShapeKind shape)
        => shape is ShapeKind.PairRuns8Waves or ShapeKind.PairBlocks8Waves or ShapeKind.PairAlternating8Waves;

    [Benchmark]
    public void Advance()
    {
        Timeline<LaneTrack, LaneClip>.Apply(_handles, _positions, true, _effects); Timeline.Advance(_handles, _positions, true);
        Sink += _positions[0];
    }
}
