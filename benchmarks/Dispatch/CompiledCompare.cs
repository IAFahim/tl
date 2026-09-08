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
    private const int BatchOps = 128;
    private const int BatchSize = 8;

    private ushort _id;
    private PulseInput _input;
    private PulseResult _result;
    private Playback _playback;
    private uint _tick;
    private uint[] _batchTicks = null!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new PulseInput(Seed: 100f);
        _id = Timeline<PulseTrack, PulseClip>.Build(Pulse.Authoring.Author);
        Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(_id);
        _playback = Timeline.Start(_id);
        _batchTicks = new uint[BatchOps * BatchSize];
        for (var i = 0; i < _batchTicks.Length; i++)
            _batchTicks[i] = (uint)(i % CompiledPulse.Duration);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = SingleOps)]
    public Playback InterpreterSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, NextTick());
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = SingleOps)]
    public Playback CompiledSingleTick()
    {
        for (var i = 0; i < SingleOps; i++)
            _playback = CompiledPulse.Forward(in _playback, in _input, ref _result, NextTick());
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public Playback InterpreterBatch8()
    {
        for (var i = 0; i < BatchOps; i++)
            _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
        return _playback;
    }

    [Benchmark(OperationsPerInvoke = BatchOps * BatchSize)]
    public Playback CompiledBatch8()
    {
        for (var i = 0; i < BatchOps; i++)
            _playback = CompiledPulse.Forward(in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
        return _playback;
    }

    private uint NextTick()
    {
        _tick = (_tick + 1) % CompiledPulse.Duration;
        return _tick;
    }
}
