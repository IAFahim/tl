using BenchmarkDotNet.Attributes;
using Pulse;
using Tl;

namespace Tl.CompiledBench;

[Config(typeof(Tl.Hooks.Config))]
public class CompiledVsInterpreter
{
    private const int SingleOps = 1024;
    private const int BatchSize = 8;
    private const int BatchOps = 75;

    private ushort _id;
    private PulseInput _input;
    private uint[] _singleTicks = null!;
    private uint[] _batchTicks = null!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new PulseInput(Seed: 100f);
        _id = Timeline<PulseTrack, PulseClip>.Build(PulseTimeline.Define).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(_id);
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, SumConsumer>(_id);
        _singleTicks = new uint[SingleOps];
        for (var i = 0; i < _singleTicks.Length; i++)
            _singleTicks[i] = (uint)i % PulseTimeline.Duration;
        _batchTicks = new uint[BatchOps * BatchSize];
        for (var i = 0; i < _batchTicks.Length; i++)
            _batchTicks[i] = (uint)i;

        RequireEqual(InterpreterSingleTick(), CompiledSingleTick(), "full scalar");
        RequireEqual(InterpreterBatch8(), CompiledBatch8(), "full batch");
        RequireEqual(SumInterpreterSingleTick(), SumCompiledSingleTick(), "sum scalar");
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = SingleOps)]
    public BenchmarkReceipt InterpreterSingleTick()
    {
        var result = new PulseResult();
        var playback = Timeline.Start(_id);
        foreach (var tick in _singleTicks)
            playback = Timeline.Forward(_id, in playback, in _input, ref result, tick);
        return BenchmarkReceipt.Capture(in playback, in result);
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public BenchmarkReceipt CompiledSingleTick()
    {
        var result = new PulseResult();
        var playback = PulseTimeline.Start();
        foreach (var tick in _singleTicks)
            playback = PulseTimeline.Forward(in playback, in _input, ref result, tick);
        return BenchmarkReceipt.Capture(in playback, in result);
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public BenchmarkReceipt InterpreterBatch8()
    {
        var result = new PulseResult();
        var playback = Timeline.Start(_id);
        for (var i = 0; i < BatchOps; i++)
            playback = Timeline.Forward(_id, in playback, in _input, ref result, _batchTicks.AsSpan(i * BatchSize, BatchSize));

        return BenchmarkReceipt.Capture(in playback, in result);
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public BenchmarkReceipt CompiledBatch8()
    {
        var result = new PulseResult();
        var playback = PulseTimeline.Start();
        for (var i = 0; i < BatchOps; i++)
            playback = PulseTimeline.Forward(in playback, in _input, ref result, _batchTicks.AsSpan(i * BatchSize, BatchSize));

        return BenchmarkReceipt.Capture(in playback, in result);
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public BenchmarkReceipt SumInterpreterSingleTick()
    {
        var result = new SumConsumer();
        var playback = Timeline.Start(_id);
        foreach (var tick in _singleTicks)
            playback = Timeline.Forward(_id, in playback, in _input, ref result, tick);
        return BenchmarkReceipt.Capture(in playback, result.Sum);
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public BenchmarkReceipt SumCompiledSingleTick()
    {
        var result = new SumConsumer();
        var playback = PulseTimeline.Start();
        foreach (var tick in _singleTicks)
            playback = PulseTimeline.Forward(in playback, in _input, ref result, tick);
        return BenchmarkReceipt.Capture(in playback, result.Sum);
    }

    private static void RequireEqual(BenchmarkReceipt expected, BenchmarkReceipt actual, string name)
    {
        if (expected != actual)
            throw new InvalidOperationException($"{name}: interpreter {expected}; compiled {actual}.");
    }
}

public readonly record struct BenchmarkReceipt(
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
    public static BenchmarkReceipt Capture(in Playback playback, in PulseResult result) => new(
        playback.Tick,
        playback.Cycles,
        playback.Flags,
        BitConverter.SingleToInt32Bits(result.Sum),
        result.Ticks,
        result.Count,
        result.Enters,
        result.Stays,
        result.Exits);

    public static BenchmarkReceipt Capture(in Playback playback, float sum) => new(
        playback.Tick,
        playback.Cycles,
        playback.Flags,
        BitConverter.SingleToInt32Bits(sum),
        0,
        0,
        0,
        0,
        0);
}

public struct SumConsumer :
    ITrack<PulseTrack, PulseClip, PulseInput, SumConsumer>
{
    public float Sum;

    public static void Forward(int ordinal, int count, ushort index,
        in PulseTrack track, in PulseClip clip, ClipState state,
        in uint tick, in PulseInput input, ref SumConsumer result)
        => result.Sum += clip.Amount;

    public static void Backward(int ordinal, int count, ushort index,
        in PulseTrack track, in PulseClip clip, ClipState state,
        in uint tick, in PulseInput input, ref SumConsumer result)
        => result.Sum -= clip.Amount;
}
