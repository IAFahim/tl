using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Perfolizer.Horology;

public sealed class AlphaConfig : ManualConfig
{
    public AlphaConfig()
    {
        var job = Job.Default
            .WithWarmupCount(16)
            .WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        if (Environment.GetEnvironmentVariable("TL_ALPHA_IN_PROCESS") == "1")
            job = job.WithToolchain(InProcessNoEmitToolchain.Instance);
        AddJob(job.WithId("Jit"));
        if (Environment.GetEnvironmentVariable("TL_ALPHA_NO_TIERING") == "1")
            AddJob(job.WithId("NoTiering").WithEnvironmentVariable("DOTNET_TieredCompilation", "0"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
        WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(40));
    }
}

[Config(typeof(AlphaConfig))]
public class ScalarCatalogQueryBenchmarks
{
    public const int Operations = 4096;
    private readonly int[] _deltas = new int[Operations];
    private readonly ReferenceState[] _directStates = new ReferenceState[1];
    private readonly Accumulator[] _directAccumulators = new Accumulator[1];

    [Params(TickPattern.Forward, TickPattern.Alternating)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        TickPatterns.Fill(_deltas, Pattern);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public BenchmarkReceipt DirectScalar()
    {
        _directStates[0] = default;
        _directAccumulators[0] = default;
        if (Pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                Direct.TickForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
        else
            for (var index = 0; index < _deltas.Length; index++)
                Direct.Tick((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
        return Direct.Capture(_directStates, _directAccumulators);
    }

    internal static void Require(BenchmarkReceipt expected, BenchmarkReceipt actual, string method)
    {
        if (actual != expected)
            throw new InvalidOperationException($"{method}: {actual} != {expected}");
    }
}

[Config(typeof(AlphaConfig))]
public class BatchCatalogQueryBenchmarks
{
    public const int Frames = 64;
    private ReferenceState[] _directStates = null!;
    private Accumulator[] _directAccumulators = null!;

    [Params(1, 32, 10_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _directStates = new ReferenceState[Rows];
        _directAccumulators = new Accumulator[Rows];
    }

    [Benchmark(Baseline = true)]
    public BenchmarkReceipt DirectBatch()
    {
        Array.Clear(_directStates);
        Array.Clear(_directAccumulators);
        for (var tick = 0; tick < Frames; tick++)
            for (var row = 0; row < _directStates.Length; row++)
                Direct.TickForward((uint)tick, ref _directStates[row], ref _directAccumulators[row]);
        return Direct.Capture(_directStates, _directAccumulators);
    }
}

internal static class TickPatterns
{
    internal static void Fill(Span<int> deltas, TickPattern pattern)
    {
        for (var index = 0; index < deltas.Length; index++)
            deltas[index] = pattern == TickPattern.Forward || (index & 1) == 0 ? 1 : -1;
    }
}

internal sealed class ShapeCase
{
    internal const int Operations = 4096;
    private readonly int[] _deltas = new int[Operations];
    private readonly ReferenceState[] _directStates = new ReferenceState[1];
    private readonly Accumulator[] _directAccumulators = new Accumulator[1];
    private readonly TickPattern _pattern;

    internal ShapeCase(TimelineShape shape, TickPattern pattern)
    {
        Shape = shape;
        _pattern = pattern;
        TickPatterns.Fill(_deltas, pattern);
    }

    internal TimelineShape Shape { get; }

    internal BenchmarkReceipt Direct()
    {
        _directStates[0] = default;
        _directAccumulators[0] = default;
        if (_pattern == TickPattern.Forward)
        {
            switch (Shape)
            {
                case TimelineShape.OneTrack:
                    for (var index = 0; index < _deltas.Length; index++)
                        DirectShapes.TickOneTrackForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                case TimelineShape.ThreeTracks:
                    for (var index = 0; index < _deltas.Length; index++)
                        global::Direct.TickForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                case TimelineShape.SixteenTracks:
                    for (var index = 0; index < _deltas.Length; index++)
                        DirectShapes.TickSixteenTracksForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                case TimelineShape.TwoHundredFiftySixTracks:
                    for (var index = 0; index < _deltas.Length; index++)
                        DirectShapes.TickTwoHundredFiftySixTracksForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                case TimelineShape.Gap:
                    for (var index = 0; index < _deltas.Length; index++)
                        DirectShapes.TickGapForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                case TimelineShape.Blend:
                    for (var index = 0; index < _deltas.Length; index++)
                        DirectShapes.TickBlendForward((uint)index, ref _directStates[0], ref _directAccumulators[0]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Shape));
            }
            return global::Direct.Capture(_directStates, _directAccumulators);
        }

        switch (Shape)
        {
            case TimelineShape.OneTrack:
                for (var index = 0; index < _deltas.Length; index++)
                    DirectShapes.TickOneTrack((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            case TimelineShape.ThreeTracks:
                for (var index = 0; index < _deltas.Length; index++)
                    global::Direct.Tick((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            case TimelineShape.SixteenTracks:
                for (var index = 0; index < _deltas.Length; index++)
                    DirectShapes.TickSixteenTracks((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            case TimelineShape.TwoHundredFiftySixTracks:
                for (var index = 0; index < _deltas.Length; index++)
                    DirectShapes.TickTwoHundredFiftySixTracks((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            case TimelineShape.Gap:
                for (var index = 0; index < _deltas.Length; index++)
                    DirectShapes.TickGap((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            case TimelineShape.Blend:
                for (var index = 0; index < _deltas.Length; index++)
                    DirectShapes.TickBlend((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Shape));
        }
        return global::Direct.Capture(_directStates, _directAccumulators);
    }
}

internal sealed class ComponentCase
{
    internal const int Operations = ShapeCase.Operations;
    private readonly int[] _deltas = new int[Operations];
    private readonly ReferenceState[] _directStates = new ReferenceState[1];
    private readonly Accumulator[] _directAccumulators = new Accumulator[1];
    private readonly FirstInput[] _first = [new(2)];
    private readonly SecondInput[] _second = [new(3)];
    private readonly ThirdInput[] _third = [new(-5)];
    private readonly TickPattern _pattern;

    internal ComponentCase(TickPattern pattern)
    {
        _pattern = pattern;
        TickPatterns.Fill(_deltas, pattern);
    }

    internal BenchmarkReceipt Direct()
    {
        _directStates[0] = default;
        _directAccumulators[0] = default;
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                DirectShapes.TickComponentForward(
                    (uint)index,
                    in _first[0],
                    in _second[0],
                    in _third[0],
                    ref _directStates[0],
                    ref _directAccumulators[0]);
        else
            for (var index = 0; index < _deltas.Length; index++)
                DirectShapes.TickComponent(
                    (uint)index,
                    _deltas[index],
                    in _first[0],
                    in _second[0],
                    in _third[0],
                    ref _directStates[0],
                    ref _directAccumulators[0]);
        return global::Direct.Capture(_directStates, _directAccumulators);
    }
}

[Config(typeof(AlphaConfig))]
public class ShapeCatalogQueryBenchmarks
{
    private ShapeCase _benchmark = null!;

    [Params(
        TimelineShape.OneTrack,
        TimelineShape.ThreeTracks,
        TimelineShape.SixteenTracks,
        TimelineShape.TwoHundredFiftySixTracks,
        TimelineShape.Gap,
        TimelineShape.Blend)]
    public TimelineShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _benchmark = new ShapeCase(Shape, TickPattern.Forward);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ShapeCase.Operations)]
    public BenchmarkReceipt DirectShape() => _benchmark.Direct();
}

internal sealed class MixedAssetCase
{
    internal const int Rows = 256;
    internal const int Frames = 64;
    private readonly TimelineShape[] _shapes = new TimelineShape[Rows];
    private readonly ReferenceState[] _directStates = new ReferenceState[Rows];
    private readonly Accumulator[] _directAccumulators = new Accumulator[Rows];

    internal MixedAssetCase()
    {
        TimelineShape[] shapes =
        [
            TimelineShape.OneTrack,
            TimelineShape.ThreeTracks,
            TimelineShape.SixteenTracks,
            TimelineShape.Gap,
            TimelineShape.Blend,
        ];
        for (var row = 0; row < Rows; row++)
            _shapes[row] = shapes[row % shapes.Length];
    }

    internal BenchmarkReceipt Direct()
    {
        Array.Clear(_directStates);
        Array.Clear(_directAccumulators);
        for (var tick = 0; tick < Frames; tick++)
            for (var row = 0; row < Rows; row++)
                DirectShapes.TickForward(_shapes[row], (uint)tick, ref _directStates[row], ref _directAccumulators[row]);
        return global::Direct.Capture(_directStates, _directAccumulators);
    }
}
