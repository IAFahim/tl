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

    [Params(ShapeKind.LaneUniform, ShapeKind.PairOne, ShapeKind.PairRuns8, ShapeKind.PairAlternating8)]
    public ShapeKind Shape;

    [GlobalSetup]
    public void Setup()
    {
        Host.BindLane();
        var bank = Host.BindBank();
        var positions = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            positions[i] = (ushort)(i % Host.Duration);
        var handles = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            handles[i] = Shape switch
            {
                ShapeKind.PairOne => bank[0],
                ShapeKind.PairRuns8 => bank[i * Host.Variants / Rows],
                _ => bank[i % Host.Variants],
            };
        _positions = positions;
        _handles = handles;
        _effects = Seeds.Effects(Rows);
    }

    [Benchmark]
    public void Advance()
    {
        if (Shape == ShapeKind.LaneUniform)
        {
            Timeline<BakedLane<LaneTrack, LaneClip>>.Advance(_positions, true, _effects);
            Sink += _positions[0];
            return;
        }
        Timeline<LaneTrack, LaneClip>.Advance(_handles, _positions, true, _effects);
        Sink += _positions[0];
    }
}
