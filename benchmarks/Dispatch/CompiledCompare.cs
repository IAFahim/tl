using BenchmarkDotNet.Attributes;
using Pulse;
using Tl;

namespace Tl.CompiledBench;

// The gain receipt: interpreter vs compiled kernel on the SAME authored
// timeline and consumer (samples/Compiled — Authoring.Author feeds the
// runtime Timeline.Build registration here, and fed the Tl.Gen compile
// reader that emitted CompiledPulse at build time). Single-tick
// consumption is the headline; the batch arms show per-call amortization.
// Reuses the project's Config (Jit + NoTiering jobs, Median column,
// MemoryDiagnoser) from Benchmarks.cs.
[Config(typeof(Tl.Hooks.Config))]
public class CompiledVsInterpreter
{
    private const int SingleOps = 1024;
    private const int BatchSize = 8;
    private const int BatchOps = 75; // 600 ticks per invoke: exactly one sweep, no wraps inside a span

    private ushort _id;
    private PulseInput _input;
    private PulseResult _result;
    private SumConsumer _sum;
    private Playback _playback;
    private uint _tick;
    private uint[] _batchTicks = null!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new PulseInput(Seed: 100f);
        _id = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author).InMemory();
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(_id);
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, SumConsumer>(_id);
        _playback = Timeline.Start(_id);
        _batchTicks = new uint[BatchOps * BatchSize];
        for (var i = 0; i < _batchTicks.Length; i++)
            _batchTicks[i] = (uint)i;
    }

    // The timeline loops, so an endless forward tick stream would accrue
    // Cycles past the ushort capacity (both engines throw identically).
    // Each sweep restarts the playback instead — one Start per 600 ticks,
    // amortized identically on both legs.
    private uint NextTick(bool interpreter)
    {
        if (_tick == CompiledPulse.Duration)
        {
            _tick = 0;
            _playback = interpreter ? Timeline.Start(_id) : CompiledPulse.Start();
        }

        return _tick++;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = SingleOps)]
    public Playback InterpreterSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, NextTick(interpreter: true));
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public Playback CompiledSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = CompiledPulse.Forward(in _playback, in _input, ref _result, NextTick(interpreter: false));
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public Playback InterpreterBatch8()
    {
        for (var i = 0; i < BatchOps; i++)
        {
            if (i == 0)
                _playback = Timeline.Start(_id);
            _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
        }

        return _playback;
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public Playback CompiledBatch8()
    {
        for (var i = 0; i < BatchOps; i++)
        {
            if (i == 0)
                _playback = CompiledPulse.Start();
            _playback = CompiledPulse.Forward(in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
        }

        return _playback;
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public Playback SumInterpreterSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = Timeline.Forward(_id, in _playback, in _input, ref _sum, NextTick(interpreter: true));
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public Playback SumCompiledSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = CompiledPulse.Forward(in _playback, in _input, ref _sum, NextTick(interpreter: false));
        return _playback;
    }
}

// The light consumer: the sandbox-baseline shape (one accumulation per
// work, no state-machine accounting) the v0.3 ~16-18 ns/tick interpreter
// receipts were measured against. Implemented over both hook pairs so the
// A/B pins the ENGINES with the consumer call retained but the consumer
// body minimal.
public struct SumConsumer :
    IForward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    IBackward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    Tl.Compiled.ICompiledForward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    Tl.Compiled.ICompiledBackward<PulseTrack, PulseClip, PulseInput, SumConsumer>
{
    public float Sum;

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Amount;
    }

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Amount;
    }

    public void Forward(in Tl.Compiled.CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Amount;
    }

    public void Backward(in Tl.Compiled.CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Amount;
    }
}
