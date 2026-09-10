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
