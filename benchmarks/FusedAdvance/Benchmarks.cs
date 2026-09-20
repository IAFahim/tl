using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

namespace Tl.FusedAdvanceProbe;

public sealed class FusedAdvanceConfig : ManualConfig
{
    public FusedAdvanceConfig()
    {
        AddJob(Job.Default.WithWarmupCount(8).WithIterationCount(16).WithIterationTime(TimeInterval.FromMilliseconds(200)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(FusedAdvanceConfig))]
public class AdvanceBenchmarks
{
    public enum ShapeKind
    {
        LaneUniform,
        LaneWaves,
        LaneStaggered,
        LaneUniformBackward,
        SetWavesOne,
        SetStaggeredMixed,
    }

    const int Rows = 100_000;
    const int ProbeMask = 0xFFFF;

    static readonly ushort[] Bank = Host.BuildIndices(8);

    ushort[] _positions = null!;
    ushort[] _laneIds = null!;
    ushort[] _ids = null!;
    float[] _effects = null!;
    int _probe;

    public long Sink;

    [Params(ShapeKind.LaneUniform, ShapeKind.LaneWaves, ShapeKind.LaneStaggered, ShapeKind.LaneUniformBackward, ShapeKind.SetWavesOne, ShapeKind.SetStaggeredMixed)]
    public ShapeKind Shape;

    [GlobalSetup]
    public void Setup()
    {
        var laneSlot = Host.SlotLane();
        _laneIds = new ushort[Rows];
        Array.Fill(_laneIds, laneSlot);
        var clock = Shape switch
        {
            ShapeKind.LaneUniform or ShapeKind.LaneUniformBackward or ShapeKind.SetWavesOne => Clock.Uniform,
            ShapeKind.LaneWaves => Clock.Waves,
            _ => Clock.Staggered,
        };
        _positions = Seeds.Positions(Rows, clock);
        _effects = Seeds.Effects(Rows);
        if (Shape == ShapeKind.SetStaggeredMixed)
        {
            var dense = Seeds.Ids(Rows, 8);
            _ids = new ushort[Rows];
            for (var i = 0; i < Rows; i++)
                _ids[i] = Bank[dense[i]];
        }
        else
            _ids = new ushort[Rows];
        _probe = 0;
    }

    [Benchmark(Baseline = true)]
    public void TwoCall() => Run(fused: false);

    [Benchmark]
    public void Fused() => Run(fused: true);

    void Run(bool fused)
    {
        var positions = _positions;
        var effects = _effects;
        switch (Shape)
        {
            case ShapeKind.LaneUniform or ShapeKind.LaneWaves or ShapeKind.LaneStaggered:
                if (fused) { Timeline<LaneTrack, LaneClip>.Apply(_laneIds, positions, true, effects); Timeline.Advance(_laneIds, positions, true); }
                else { Timeline<LaneTrack, LaneClip>.Apply(_laneIds, positions, true, effects); Timeline.Advance(_laneIds, positions, true); }
                break;
            case ShapeKind.LaneUniformBackward:
                if (fused) { Timeline<LaneTrack, LaneClip>.Apply(_laneIds, positions, false, effects); Timeline.Advance(_laneIds, positions, false); }
                else { Timeline<LaneTrack, LaneClip>.Apply(_laneIds, positions, false, effects); Timeline.Advance(_laneIds, positions, false); }
                break;
            default:
                if (fused) { Timeline<LaneTrack, LaneClip>.Apply(_ids, positions, true, effects); Timeline.Advance(_ids, positions, true); }
                else { Timeline<LaneTrack, LaneClip>.Apply(_ids, positions, true, effects); Timeline.Advance(_ids, positions, true); }
                break;
        }
        Sink += BitConverter.SingleToInt32Bits(effects[ProbeMask & _probe++]);
    }
}
