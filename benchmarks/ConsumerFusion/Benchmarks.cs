using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Pulse;

namespace Tl.ConsumerFusion;

public sealed class ConsumerConfig : ManualConfig
{
    public ConsumerConfig()
    {
        AddJob(Job.Default.WithId("JitCore4").WithWarmupCount(16).WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public enum TickPattern { Sequential, Random, Repeated }

[Config(typeof(ConsumerConfig))]
[GenericTypeArguments(typeof(SumConsumer))]
[GenericTypeArguments(typeof(StateConsumer))]
[GenericTypeArguments(typeof(EffectConsumer))]
public class ConsumerBenchmarks<TConsumer> where TConsumer : unmanaged, IConsumer<TConsumer>
{
    public const int Operations = 65536;
    private uint[] _ticks = null!;
    private ConsumerInput[] _inputs = null!;
    private ushort _timeline;

    [Params(TickPattern.Sequential, TickPattern.Random, TickPattern.Repeated)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"Generated source SHA-256: {BuildIdentity.SourceSha256}");
        Console.WriteLine($"Consumer source SHA-256: {BuildIdentity.ConsumerSha256}");
        var allowed = File.ReadLines("/proc/self/status")
            .Single(static line => line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal))
            .Split(':', 2)[1].Trim();
        if (allowed != "4")
            throw new InvalidOperationException($"Benchmark requires CPU 4, got {allowed}.");
        _timeline = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<ConsumerInput, TConsumer>(_timeline);
        _inputs = [new(1.25f, 0.125f), new(-0.75f, -3f), new(0.5f, 5f), new(2f, 0f)];
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
        ConsumerReceipt[] actual = [CompiledSingle(), FusedSingle(), InterpreterBatch8(), CompiledBatch8(), FusedBatch8()];
        foreach (var receipt in actual)
            if (receipt != expected)
                throw new InvalidOperationException($"{typeof(TConsumer).Name}/{Pattern}: {receipt} != {expected}");
        Console.WriteLine($"{typeof(TConsumer).Name}/{Pattern}: {expected}");
        var warmupStart = Stopwatch.GetTimestamp();
        ulong preparationReceipt = 0;
        while (Stopwatch.GetElapsedTime(warmupStart).TotalSeconds < 5)
        {
            preparationReceipt += (uint)InterpreterSingle().SumBits;
            preparationReceipt += (uint)CompiledSingle().SumBits;
            preparationReceipt += (uint)FusedSingle().SumBits;
            preparationReceipt += (uint)InterpreterBatch8().SumBits;
            preparationReceipt += (uint)CompiledBatch8().SumBits;
            preparationReceipt += (uint)FusedBatch8().SumBits;
        }
        Console.WriteLine($"Preparation receipt: {preparationReceipt}");
    }

    [GlobalCleanup]
    public void Cleanup() => Timeline.Destroy(_timeline);

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public ConsumerReceipt InterpreterSingle()
    {
        TConsumer result = default;
        var playback = Timeline.Start(_timeline);
        for (var i = 0; i < _ticks.Length; i++)
            playback = Timeline.Forward(_timeline, in playback, in _inputs[(i >> 3) & 3], ref result, _ticks[i]);
        return result.Capture(in playback);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public ConsumerReceipt CompiledSingle()
    {
        TConsumer result = default;
        var playback = CompiledPulse.Start();
        for (var i = 0; i < _ticks.Length; i++)
            playback = CompiledPulse.Forward(in playback, in _inputs[(i >> 3) & 3], ref result, _ticks[i]);
        return result.Capture(in playback);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public ConsumerReceipt FusedSingle()
    {
        TConsumer result = default;
        var playback = FusedPulse.Start();
        for (var i = 0; i < _ticks.Length; i++)
            playback = FusedPulse.Forward(in playback, in _inputs[(i >> 3) & 3], ref result, _ticks[i]);
        return result.Capture(in playback);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public ConsumerReceipt InterpreterBatch8()
    {
        TConsumer result = default;
        var playback = Timeline.Start(_timeline);
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = Timeline.Forward(_timeline, in playback, in _inputs[(i >> 3) & 3], ref result, _ticks.AsSpan(i, 8));
        return result.Capture(in playback);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public ConsumerReceipt CompiledBatch8()
    {
        TConsumer result = default;
        var playback = CompiledPulse.Start();
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = CompiledPulse.Forward(in playback, in _inputs[(i >> 3) & 3], ref result, _ticks.AsSpan(i, 8));
        return result.Capture(in playback);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public ConsumerReceipt FusedBatch8()
    {
        TConsumer result = default;
        var playback = FusedPulse.Start();
        for (var i = 0; i < _ticks.Length; i += 8)
            playback = FusedPulse.Forward(in playback, in _inputs[(i >> 3) & 3], ref result, _ticks.AsSpan(i, 8));
        return result.Capture(in playback);
    }
}
