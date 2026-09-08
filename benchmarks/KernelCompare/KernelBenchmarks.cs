using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Pulse;
using Tl;

public sealed class KernelConfig : ManualConfig
{
    public KernelConfig()
    {
        AddJob(Job.Default
            .WithId("JitCore4")
            .WithAffinity(new IntPtr(1 << 4))
            .WithWarmupCount(12)
            .WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(KernelConfig))]
public class KernelBenchmarks
{
    private const int Ops = 4096;
    private const int BatchSize = 8;
    private readonly PulseInput _input = new(100f);
    private uint[] _sequential = null!;
    private uint[] _random = null!;
    private uint[] _same = null!;

    [GlobalSetup]
    public void Setup()
    {
        var allowed = File.ReadLines("/proc/self/status").Single(static line => line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal)).Split(':', 2)[1].Trim();
        Console.WriteLine($"Cpus_allowed_list: {allowed}");
        if (allowed != "4")
            throw new InvalidOperationException($"Expected CPU 4, got {allowed}.");

        _sequential = new uint[Ops];
        _random = new uint[Ops];
        _same = new uint[Ops];
        var state = 0xA312AFD5u;
        for (var i = 0; i < Ops; i++)
        {
            _sequential[i] = (uint)i;
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            _random[i] = state % 2400u;
            _same[i] = 321u;
        }
    }

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt SequentialSingle() => RunSingle(_sequential);

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt RandomSingle() => RunSingle(_random);

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt SameTickSingle() => RunSingle(_same);

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt SequentialBatch8() => RunBatch(_sequential);

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt RandomBatch8() => RunBatch(_random);

    [Benchmark(OperationsPerInvoke = Ops)]
    public KernelReceipt SameTickBatch8() => RunBatch(_same);

    public static void Verify()
    {
        var benchmark = new KernelBenchmarks();
        benchmark.Setup();
        var id = Timeline<PulseTrack, PulseClip>.Build(Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(id);
        benchmark.Verify(id, nameof(SequentialSingle), benchmark._sequential);
        benchmark.Verify(id, nameof(RandomSingle), benchmark._random);
        benchmark.Verify(id, nameof(SameTickSingle), benchmark._same);
    }

    private void Verify(ushort id, string name, uint[] ticks)
    {
        var compiledSingle = RunSingle(ticks);
        var interpreterSingle = RunInterpreterSingle(id, ticks);
        var compiledBatch = RunBatch(ticks);
        var interpreterBatch = RunInterpreterBatch(id, ticks);
        if (compiledSingle != interpreterSingle || compiledBatch != interpreterBatch || compiledSingle != compiledBatch)
            throw new InvalidOperationException($"{name}: {compiledSingle} / {interpreterSingle} / {compiledBatch} / {interpreterBatch}");
        Console.WriteLine($"{name}: {compiledSingle}");
    }

    private KernelReceipt RunSingle(uint[] ticks)
    {
        var result = new PulseResult();
        var playback = CompiledPulse.Start();
        foreach (var tick in ticks)
            playback = CompiledPulse.Forward(in playback, in _input, ref result, tick);
        return KernelReceipt.Capture(in playback, in result);
    }

    private KernelReceipt RunBatch(uint[] ticks)
    {
        var result = new PulseResult();
        var playback = CompiledPulse.Start();
        for (var i = 0; i < ticks.Length; i += BatchSize)
            playback = CompiledPulse.Forward(in playback, in _input, ref result, ticks.AsSpan(i, BatchSize));
        return KernelReceipt.Capture(in playback, in result);
    }

    private KernelReceipt RunInterpreterSingle(ushort id, uint[] ticks)
    {
        var result = new PulseResult();
        var playback = Timeline.Start(id);
        foreach (var tick in ticks)
            playback = Timeline.Forward(id, in playback, in _input, ref result, tick);
        return KernelReceipt.Capture(in playback, in result);
    }

    private KernelReceipt RunInterpreterBatch(ushort id, uint[] ticks)
    {
        var result = new PulseResult();
        var playback = Timeline.Start(id);
        for (var i = 0; i < ticks.Length; i += BatchSize)
            playback = Timeline.Forward(id, in playback, in _input, ref result, ticks.AsSpan(i, BatchSize));
        return KernelReceipt.Capture(in playback, in result);
    }
}

public readonly record struct KernelReceipt(
    uint Tick,
    ushort Cycles,
    PlaybackFlags Flags,
    int SumBits,
    long Ticks,
    int Count,
    int Enters,
    int Stays,
    int Exits)
{
    public static KernelReceipt Capture(in Playback playback, in PulseResult result)
        => new(
            playback.Tick,
            playback.Cycles,
            playback.Flags,
            BitConverter.SingleToInt32Bits(result.Sum),
            result.Ticks,
            result.Count,
            result.Enters,
            result.Stays,
            result.Exits);
}
