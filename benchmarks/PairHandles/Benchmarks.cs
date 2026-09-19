using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

namespace Tl.PairHandlesProbe;

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
    {
        var gold = Host.SlotGoldLane();
        var bank = Host.BindBank();
        var positions = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            positions[i] = HasWaves()
                ? (ushort)(i / 100 % Host.Duration)
                : (ushort)(i % Host.Duration);
        var handles = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            handles[i] = Shape switch
            {
                ShapeKind.LaneUniform => gold,
                ShapeKind.PairOne => bank[0],
                ShapeKind.PairRuns8 or ShapeKind.PairRuns8Waves => bank[i * Host.Variants / Rows],
                ShapeKind.PairBlocks8Waves => bank[i / 100 % Host.Variants],
                _ => bank[i % Host.Variants],
            };
        _positions = positions;
        _handles = handles;
        _effects = Seeds.Effects(Rows);
    }

    bool HasWaves()
        => Shape is ShapeKind.PairRuns8Waves or ShapeKind.PairBlocks8Waves or ShapeKind.PairAlternating8Waves;

    [Benchmark]
    public void Advance()
    {
        Timeline<LaneTrack, LaneClip>.Apply(_handles, _positions, true, _effects); Timeline.Step(_handles, _positions, true);
        Sink += _positions[0];
    }
}
