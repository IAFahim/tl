using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Pulse;
using System.Diagnostics;
using Tl;

namespace Tl.FusionExperiment;

public sealed class FusionConfig : ManualConfig
{
    public FusionConfig()
    {
        AddJob(Job.Default.WithId("JitCore4").WithWarmupCount(16).WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public enum TickPattern { Sequential, Random, Repeated }

[Config(typeof(FusionConfig))]
public class FusionBenchmarks
{
    public const int Operations = 65536;
    private uint[] _ticks = null!;
    private ushort _timeline;
    private readonly PulseInput _input = new(100f);

    [Params(TickPattern.Sequential, TickPattern.Random, TickPattern.Repeated)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var allowed = File.ReadLines("/proc/self/status")
            .Single(static line => line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal))
            .Split(':', 2)[1].Trim();
        if (allowed != "4")
            throw new InvalidOperationException($"Benchmark requires CPU 4, got {allowed}.");
        _timeline = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, SumConsumer>(_timeline);
        _ticks = new uint[Operations];
        uint random = 0xA312AFD5;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = Pattern switch
            {
                TickPattern.Sequential => (uint)i,
                TickPattern.Random => random % 2400u,
                TickPattern.Repeated => 321u,
                _ => throw new ArgumentOutOfRangeException(nameof(Pattern))
            };
        }
        var expected = InterpreterSingle();
        SumReceipt[] actual = [CompiledSingle(), FusedSingle(), InterpreterBatch8(), CompiledBatch8(), FusedBatch8()];
        foreach (var receipt in actual)
            if (receipt != expected)
                throw new InvalidOperationException($"{Pattern}: {receipt} != {expected}");
        Console.WriteLine($"{Pattern}: {expected}");
        var warmupStart = Stopwatch.GetTimestamp();
        ulong warmupReceipt = 0;
        while (Stopwatch.GetElapsedTime(warmupStart).TotalSeconds < 5)
        {
            warmupReceipt += (uint)InterpreterSingle().SumBits;
            warmupReceipt += (uint)CompiledSingle().SumBits;
            warmupReceipt += (uint)FusedSingle().SumBits;
            warmupReceipt += (uint)InterpreterBatch8().SumBits;
            warmupReceipt += (uint)CompiledBatch8().SumBits;
            warmupReceipt += (uint)FusedBatch8().SumBits;
        }
        Console.WriteLine($"Preparation receipt: {warmupReceipt}");
    }

    [GlobalCleanup]
    public void Cleanup() => Timeline.Destroy(_timeline);

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public SumReceipt InterpreterSingle()
    {
        var result = default(SumConsumer);
        var playback = Timeline.Start(_timeline);
        foreach (var tick in _ticks)
            playback = Timeline.Forward(_timeline, in playback, in _input, ref result, tick);
        return SumReceipt.Capture(in playback, result.Sum);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt CompiledSingle()
    {
        var result = default(SumConsumer);
        var playback = CompiledPulse.Start();
        foreach (var tick in _ticks)
            playback = CompiledPulse.Forward(in playback, in _input, ref result, tick);
        return SumReceipt.Capture(in playback, result.Sum);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt FusedSingle()
    {
        float sum = 0f;
        var playback = FusedPulse.Start();
        foreach (var tick in _ticks)
            playback = FusedPulse.Forward(in playback, ref sum, tick);
        return SumReceipt.Capture(in playback, sum);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt InterpreterBatch8()
    {
        var result = default(SumConsumer);
        var playback = Timeline.Start(_timeline);
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = Timeline.Forward(_timeline, in playback, in _input, ref result, _ticks.AsSpan(i, 8));
        return SumReceipt.Capture(in playback, result.Sum);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt CompiledBatch8()
    {
        var result = default(SumConsumer);
        var playback = CompiledPulse.Start();
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = CompiledPulse.Forward(in playback, in _input, ref result, _ticks.AsSpan(i, 8));
        return SumReceipt.Capture(in playback, result.Sum);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt FusedBatch8()
    {
        float sum = 0f;
        var playback = FusedPulse.Start();
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = FusedPulse.Forward(in playback, ref sum, _ticks.AsSpan(i, 8));
        return SumReceipt.Capture(in playback, sum);
    }
}
