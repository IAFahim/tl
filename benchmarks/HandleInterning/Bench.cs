using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace HandleInterning;

public sealed class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddJob(Job.Default
            .WithId("steady")
            .WithWarmupCount(8)
            .WithIterationCount(10)
            .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(BenchConfig))]
public class InternBench
{
    public const int Ops = 2048;

    InternTable _table = null!;
    byte[] _bytes = null!;
    ContentKey _key;
    Asset _keeper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _table = new InternTable(4096);
        _bytes = Rng.Payload(7, 4096);
        _key = ContentKey.Of(_bytes);
        _keeper = _table.Load(_bytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _keeper.Dispose();
        _table.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
    public long FreshLoad()
    {
        long sink = 0;
        for (var i = 0; i < Ops; i++)
        {
            using var a = Asset.Fresh(_bytes);
            sink += a.FirstByte + a.Length;
        }
        return sink;
    }

    [Benchmark(OperationsPerInvoke = Ops)]
    public long InternedHit()
    {
        long sink = 0;
        for (var i = 0; i < Ops; i++)
        {
            using var a = _table.Load(_bytes);
            sink += a.FirstByte + a.Length;
        }
        return sink;
    }

    [Benchmark(OperationsPerInvoke = Ops)]
    public long InternedProbeOnly()
    {
        long sink = 0;
        for (var i = 0; i < Ops; i++)
        {
            using var a = _table.Load(_key, _bytes);
            sink += a.FirstByte + a.Length;
        }
        return sink;
    }
}

[Config(typeof(BenchConfig))]
public class GuardBench
{
    public const int Ops = 8192;
    public const ulong Pair = 0xC0FFEEC0FFEEC0FFUL;

    InternTable _table = null!;
    Asset _asset = null!;
    ushort[] _positions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _table = new InternTable(4096);
        _asset = _table.Load(Rng.Payload(11, 4096));
        ushort pos = 0;
        TypedLane.Advance(_asset, Pair, ref pos);
        _positions = new ushort[Ops];
        var state = Rng.Seed ^ 0xDEADBEEFDEADBEEFUL;
        for (var i = 0; i < Ops; i++) _positions[i] = (ushort)(Rng.Next(ref state) % TypedLane.Duration);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _asset.Dispose();
        PairFolds.ResetAll();
        _table.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
    public double NoGuardDirect()
    {
        double sink = 0;
        var positions = _positions;
        for (var i = 0; i < Ops; i++)
        {
            var p = positions[i];
            sink += TypedLane.AdvanceDirect(ref p);
        }
        return sink;
    }

    [Benchmark(OperationsPerInvoke = Ops)]
    public double GuardedHit()
    {
        double sink = 0;
        var positions = _positions;
        var asset = _asset;
        for (var i = 0; i < Ops; i++)
        {
            var p = positions[i];
            sink += TypedLane.Advance(asset, Pair, ref p);
        }
        return sink;
    }

    [Benchmark(OperationsPerInvoke = Ops)]
    public double IndirectResolvePerCall()
    {
        double sink = 0;
        var positions = _positions;
        var asset = _asset;
        for (var i = 0; i < Ops; i++)
        {
            var p = positions[i];
            sink += TypedLane.AdvanceIndirect(asset, Pair, ref p);
        }
        return sink;
    }
}
