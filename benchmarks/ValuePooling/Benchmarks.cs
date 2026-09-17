using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;

namespace Tl.ValuePooling;

public static class RunSettings
{
    public static string ArtifactsPath = Path.Combine(AppContext.BaseDirectory, "bdn");
}

public sealed class ProtoConfig : ManualConfig
{
    public ProtoConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(8)
            .WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250))
            .WithId("InProcess"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
        ArtifactsPath = RunSettings.ArtifactsPath;
    }
}

[Config(typeof(ProtoConfig))]
public class QueryScanSmall
{
    public const int Rows = 100_000;

    [Params(10, 256)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.ScanInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledByte() => _fixture.ScanPooledByte();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.ScanPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class QueryScanLarge
{
    public const int Rows = 1_000_000;

    [Params(10, 256)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.ScanInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledByte() => _fixture.ScanPooledByte();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.ScanPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class QueryScanWideSmall
{
    public const int Rows = 100_000;

    [Params(1000)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.ScanInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.ScanPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class QueryScanWideLarge
{
    public const int Rows = 1_000_000;

    [Params(1000)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.ScanInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.ScanPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class IndirectScanSmall
{
    public const int Rows = 100_000;

    [Params(10, 256)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.IndirectInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledByte() => _fixture.IndirectPooledByte();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.IndirectPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class IndirectScanLarge
{
    public const int Rows = 1_000_000;

    [Params(10, 256)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.IndirectInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledByte() => _fixture.IndirectPooledByte();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.IndirectPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class IndirectScanWideSmall
{
    public const int Rows = 100_000;

    [Params(1000)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.IndirectInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.IndirectPooledUshort();
}

[Config(typeof(ProtoConfig))]
public class IndirectScanWideLarge
{
    public const int Rows = 1_000_000;

    [Params(1000)]
    public int Pool;

    private Fixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = Fixture.Create(Rows, Pool);
        Verification.Assert(_fixture);
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public float Inline() => _fixture.IndirectInline();

    [Benchmark(OperationsPerInvoke = Rows)]
    public float PooledUshort() => _fixture.IndirectPooledUshort();
}
