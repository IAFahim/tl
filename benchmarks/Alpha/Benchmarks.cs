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
    private readonly BenchmarkCatalog.State[] _queryStates = new BenchmarkCatalog.State[1];
    private readonly Accumulator[] _queryAccumulators = new Accumulator[1];

    [Params(TickPattern.Forward, TickPattern.Alternating)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        TickPatterns.Fill(_deltas, Pattern);
        Require(DirectScalar(), GeneratedQueryScalar(), nameof(GeneratedQueryScalar));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public BenchmarkReceipt DirectScalar()
    {
        _directStates[0] = default;
        _directAccumulators[0] = default;
        for (var index = 0; index < _deltas.Length; index++)
            Direct.Tick((uint)index, _deltas[index], ref _directStates[0], ref _directAccumulators[0]);
        return Direct.Capture(_directStates, _directAccumulators);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public BenchmarkReceipt GeneratedQueryScalar()
    {
        _queryStates[0] = new BenchmarkCatalog.State(BenchmarkCatalog.Asset.MixedTimeline);
        _queryAccumulators[0] = default;
        var query = new BenchmarkCatalog.Query().BenchmarkRows(_queryStates, _queryAccumulators);
        if (Pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
        return Direct.Capture(_queryStates, _queryAccumulators);
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
    private BenchmarkCatalog.State[] _queryStates = null!;
    private Accumulator[] _queryAccumulators = null!;

    [Params(1, 32, 10_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _directStates = new ReferenceState[Rows];
        _directAccumulators = new Accumulator[Rows];
        _queryStates = new BenchmarkCatalog.State[Rows];
        _queryAccumulators = new Accumulator[Rows];
        ScalarCatalogQueryBenchmarks.Require(DirectBatch(), GeneratedQueryBatch(), nameof(GeneratedQueryBatch));
    }

    [Benchmark(Baseline = true)]
    public BenchmarkReceipt DirectBatch()
    {
        Array.Clear(_directStates);
        Array.Clear(_directAccumulators);
        for (var tick = 0; tick < Frames; tick++)
            for (var row = 0; row < _directStates.Length; row++)
                Direct.Tick((uint)tick, 1, ref _directStates[row], ref _directAccumulators[row]);
        return Direct.Capture(_directStates, _directAccumulators);
    }

    [Benchmark]
    public BenchmarkReceipt GeneratedQueryBatch()
    {
        Array.Fill(_queryStates, new BenchmarkCatalog.State(BenchmarkCatalog.Asset.MixedTimeline));
        Array.Clear(_queryAccumulators);
        var query = new BenchmarkCatalog.Query().BenchmarkRows(_queryStates, _queryAccumulators);
        query.Tick(0u, Frames);
        return Direct.Capture(_queryStates, _queryAccumulators);
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
    private readonly ShapeCatalog.State[] _queryStates = new ShapeCatalog.State[1];
    private readonly Accumulator[] _queryAccumulators = new Accumulator[1];
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

    internal BenchmarkReceipt Generated()
    {
        _queryStates[0] = new ShapeCatalog.State(Asset(Shape));
        _queryAccumulators[0] = default;
        switch (Shape)
        {
            case TimelineShape.OneTrack:
            {
                var query = new ShapeCatalog.Query().OneTrackRows(_queryStates, _queryAccumulators);
                Run(query);
                break;
            }
            case TimelineShape.ThreeTracks:
            {
                var query = new ShapeCatalog.Query().ThreeTrackRows(_queryStates, _queryAccumulators);
                Run(query);
                break;
            }
            case TimelineShape.SixteenTracks:
            {
                var query = new ShapeCatalog.Query().SixteenTrackRows(_queryStates, _queryAccumulators);
                Run(query);
                break;
            }
            case TimelineShape.Gap:
            {
                var query = new ShapeCatalog.Query().GapRows(_queryStates, _queryAccumulators);
                Run(query);
                break;
            }
            case TimelineShape.Blend:
            {
                var query = new ShapeCatalog.Query().BlendRows(_queryStates, _queryAccumulators);
                Run(query);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(Shape));
        }
        return global::Direct.Capture(_queryStates, _queryAccumulators);
    }

    private void Run(ShapeCatalog.OneTrackRowsQuery query)
    {
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
    }

    private void Run(ShapeCatalog.ThreeTrackRowsQuery query)
    {
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
    }

    private void Run(ShapeCatalog.SixteenTrackRowsQuery query)
    {
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
    }

    private void Run(ShapeCatalog.GapRowsQuery query)
    {
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
    }

    private void Run(ShapeCatalog.BlendRowsQuery query)
    {
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
    }

    private static ShapeCatalog.Asset Asset(TimelineShape shape)
        => shape switch
        {
            TimelineShape.OneTrack => ShapeCatalog.Asset.OneTrackTimeline,
            TimelineShape.ThreeTracks => ShapeCatalog.Asset.MixedTimeline,
            TimelineShape.SixteenTracks => ShapeCatalog.Asset.SixteenTrackTimeline,
            TimelineShape.Gap => ShapeCatalog.Asset.GapTimeline,
            TimelineShape.Blend => ShapeCatalog.Asset.BlendTimeline,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
}

internal sealed class ComponentCase
{
    internal const int Operations = ShapeCase.Operations;
    private readonly int[] _deltas = new int[Operations];
    private readonly ReferenceState[] _directStates = new ReferenceState[1];
    private readonly Accumulator[] _directAccumulators = new Accumulator[1];
    private readonly ShapeCatalog.State[] _queryStates = new ShapeCatalog.State[1];
    private readonly Accumulator[] _queryAccumulators = new Accumulator[1];
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

    internal BenchmarkReceipt Generated()
    {
        _queryStates[0] = new ShapeCatalog.State(ShapeCatalog.Asset.ComponentTimeline);
        _queryAccumulators[0] = default;
        var query = new ShapeCatalog.Query().ComponentRows(_queryStates, _first, _second, _third, _queryAccumulators);
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
        return global::Direct.Capture(_queryStates, _queryAccumulators);
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
        TimelineShape.Gap,
        TimelineShape.Blend)]
    public TimelineShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _benchmark = new ShapeCase(Shape, TickPattern.Forward);
        ScalarCatalogQueryBenchmarks.Require(DirectShape(), GeneratedShape(), nameof(GeneratedShape));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ShapeCase.Operations)]
    public BenchmarkReceipt DirectShape() => _benchmark.Direct();

    [Benchmark(OperationsPerInvoke = ShapeCase.Operations)]
    public BenchmarkReceipt GeneratedShape() => _benchmark.Generated();
}

[Config(typeof(AlphaConfig))]
public class ComponentCatalogQueryBenchmarks
{
    private ComponentCase _benchmark = null!;

    [GlobalSetup]
    public void Setup()
    {
        _benchmark = new ComponentCase(TickPattern.Forward);
        ScalarCatalogQueryBenchmarks.Require(DirectComponent(), GeneratedComponent(), nameof(GeneratedComponent));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ComponentCase.Operations)]
    public BenchmarkReceipt DirectComponent() => _benchmark.Direct();

    [Benchmark(OperationsPerInvoke = ComponentCase.Operations)]
    public BenchmarkReceipt GeneratedComponent() => _benchmark.Generated();
}

[Config(typeof(AlphaConfig))]
public class MixedAssetCatalogQueryBenchmarks
{
    private MixedAssetCase _benchmark = null!;

    [GlobalSetup]
    public void Setup()
    {
        _benchmark = new MixedAssetCase();
        ScalarCatalogQueryBenchmarks.Require(DirectMixedAssets(), GeneratedMixedAssets(), nameof(GeneratedMixedAssets));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = MixedAssetCase.Rows * MixedAssetCase.Frames)]
    public BenchmarkReceipt DirectMixedAssets() => _benchmark.Direct();

    [Benchmark(OperationsPerInvoke = MixedAssetCase.Rows * MixedAssetCase.Frames)]
    public BenchmarkReceipt GeneratedMixedAssets() => _benchmark.Generated();
}

internal sealed class MixedAssetCase
{
    internal const int Rows = 256;
    internal const int Frames = 64;
    private readonly TimelineShape[] _shapes = new TimelineShape[Rows];
    private readonly ReferenceState[] _directStates = new ReferenceState[Rows];
    private readonly Accumulator[] _directAccumulators = new Accumulator[Rows];
    private readonly MixedShapeCatalog.State[] _queryStates = new MixedShapeCatalog.State[Rows];
    private readonly Accumulator[] _queryAccumulators = new Accumulator[Rows];

    internal MixedAssetCase()
    {
        var shapes = Enum.GetValues<TimelineShape>();
        for (var row = 0; row < Rows; row++)
            _shapes[row] = shapes[row % shapes.Length];
    }

    internal BenchmarkReceipt Direct()
    {
        Array.Clear(_directStates);
        Array.Clear(_directAccumulators);
        for (var tick = 0; tick < Frames; tick++)
            for (var row = 0; row < Rows; row++)
                DirectShapes.Tick(_shapes[row], (uint)tick, 1, ref _directStates[row], ref _directAccumulators[row]);
        return global::Direct.Capture(_directStates, _directAccumulators);
    }

    internal BenchmarkReceipt Generated()
    {
        for (var row = 0; row < Rows; row++)
            _queryStates[row] = new MixedShapeCatalog.State(Asset(_shapes[row]));
        Array.Clear(_queryAccumulators);
        var query = new MixedShapeCatalog.Query().MixedShapeRows(_queryStates, _queryAccumulators);
        query.Tick(0u, Frames);
        return global::Direct.Capture(_queryStates, _queryAccumulators);
    }

    private static MixedShapeCatalog.Asset Asset(TimelineShape shape)
        => shape switch
        {
            TimelineShape.OneTrack => MixedShapeCatalog.Asset.OneTrackTimeline,
            TimelineShape.ThreeTracks => MixedShapeCatalog.Asset.MixedTimeline,
            TimelineShape.SixteenTracks => MixedShapeCatalog.Asset.SixteenTrackTimeline,
            TimelineShape.Gap => MixedShapeCatalog.Asset.GapTimeline,
            TimelineShape.Blend => MixedShapeCatalog.Asset.BlendTimeline,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
}
