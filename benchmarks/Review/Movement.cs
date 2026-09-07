using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using Perfolizer.Horology;

namespace Tl.Review;

public sealed class Config : ManualConfig
{
    public Config()
    {
        AddJob(Job.Default.WithWarmupCount(16).WithIterationCount(12).WithIterationTime(TimeInterval.FromMilliseconds(250)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public readonly record struct Receipt(float Sum, ulong Flags, int Count);

[Config(typeof(Config))]
public class Movement
{
    private const int Operations = 65536;
    [Params(16, 512)] public int Clips { get; set; }
    [Params(false, true)] public bool Sequential { get; set; }
    private uint[] _ticks = null!;
    private Tl.Before.Timeline<BeforeTrack, BeforeClip, BeforeData> _before = null!;
    private ushort _after;

    [GlobalSetup]
    public void Setup()
    {
        _ticks = new uint[Operations];
        uint random = 0xB19F4C27;
        uint duration = (uint)Clips * 4 - 1;
        for (int i = 0; i < Operations; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = (Sequential ? (uint)i : random) % duration;
        }
        _before = new();
        _before.AddTrack(new BeforeTrack());
        for (uint i = 0; i < Clips; i++)
            _before.AddClip(0, new BeforeClip(i + 1), i * 4, i * 4 + 3);
        _before.Build();
        _after = Tl.Hooks.Timeline<AfterTrack, AfterClip, AfterData>.Build(b =>
        {
            var track = b.Track(new AfterTrack());
            for (uint i = 0; i < (uint)Clips; i++)
                b.Clip(track, new AfterClip(i + 1), i * 4, i * 4 + 3);
        });
        var expected = BeforeSingle();
        if (AfterSingle() != expected || BeforeBatch() != expected || AfterBatch() != expected)
            throw new InvalidOperationException($"Movement receipts differ for {Clips}, {Sequential}.");
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt BeforeSingle()
    {
        var data = new BeforeData();
        var state = Tl.Before.Playback.Start();
        foreach (uint tick in _ticks)
            state = _before.Forward(in state, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt BeforeBatch()
    {
        var data = new BeforeData();
        var state = Tl.Before.Playback.Start();
        for (int i = 0; i < _ticks.Length; i += 8)
            state = _before.Forward(in state, ref data, _ticks.AsSpan(i, 8));
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt AfterSingle()
    {
        var data = new AfterData();
        var state = Tl.Hooks.Playback.Start();
        foreach (uint tick in _ticks)
            state = Tl.Hooks.Timeline<AfterTrack, AfterClip, AfterData>.Forward(_after, in state, ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt AfterBatch()
    {
        var data = new AfterData();
        var state = Tl.Hooks.Playback.Start();
        for (int i = 0; i < _ticks.Length; i += 8)
            state = Tl.Hooks.Timeline<AfterTrack, AfterClip, AfterData>.Forward(_after, in state, ref data, _ticks.AsSpan(i, 8));
        return data.Result;
    }
}

public readonly record struct BeforeClip(float Value);
public readonly struct BeforeTrack : Tl.Before.IBlend<BeforeClip>
{
    public void Blend(in BeforeClip a, in BeforeClip b, float factor, out BeforeClip result)
        => result = new(a.Value * (1f - factor) + b.Value * factor);
}
public struct BeforeData : Tl.Before.IForwardTracks<BeforeTrack, BeforeClip, BeforeData>, Tl.Before.IBackwardTracks<BeforeTrack, BeforeClip, BeforeData>
{
    public float Sum;
    public ulong Flags;
    public int Count;
    public readonly Receipt Result => new(Sum, Flags, Count);
    public void Forward(uint tick, in Tl.Before.Tracks<BeforeTrack, BeforeClip> tracks, ref BeforeData data)
    {
        foreach (var item in tracks) data.Sum += item.Clip.Value;
        data.Flags += (uint)tracks.Status;
        data.Count++;
    }
    public void Backward(uint tick, in Tl.Before.Tracks<BeforeTrack, BeforeClip> tracks, ref BeforeData data)
        => Forward(tick, in tracks, ref data);
}

public readonly record struct AfterClip(float Value);
public readonly struct AfterTrack : Tl.Hooks.IBlend<AfterClip>
{
    public void Blend(in AfterClip a, in AfterClip b, float factor, out AfterClip result)
        => result = new(a.Value * (1f - factor) + b.Value * factor);
}
public struct AfterData : Tl.Hooks.IForwardTracks<AfterTrack, AfterClip, AfterData>, Tl.Hooks.IBackwardTracks<AfterTrack, AfterClip, AfterData>
{
    public float Sum;
    public ulong Flags;
    public int Count;
    public readonly Receipt Result => new(Sum, Flags, Count);
    public void Forward(uint tick, in Tl.Hooks.Tracks<AfterTrack, AfterClip> tracks, ref AfterData data)
    {
        foreach (var item in tracks) data.Sum += item.Clip.Value;
        data.Flags += (uint)tracks.Status;
        data.Count++;
    }
    public void Backward(uint tick, in Tl.Hooks.Tracks<AfterTrack, AfterClip> tracks, ref AfterData data)
        => Forward(tick, in tracks, ref data);
}
