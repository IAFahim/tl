using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

public enum RoutingScale
{
    One = 1,
    Sixteen = 16,
    TwoHundredFiftySix = 256
}

public readonly record struct RoutingReceipt(int Successes, ulong OwnerSum);

[Config(typeof(RoutingConfig))]
[MemoryDiagnoser]
public partial class RoutingBenchmarks
{
    public const int Operations = 65_536;

    private ushort[] _ids = null!;
    private Playback[] _playbacks = null!;
    private OneState _oneState;
    private SixteenState _sixteenState;
    private WideState _wideState;

    [Params(RoutingScale.One, RoutingScale.Sixteen, RoutingScale.TwoHundredFiftySix)]
    public RoutingScale Scale { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _ids = Scale switch
        {
            RoutingScale.One => s_oneIds,
            RoutingScale.Sixteen => s_sixteenIds,
            RoutingScale.TwoHundredFiftySix => s_wideIds,
            _ => throw new InvalidOperationException()
        };
        _playbacks = new Playback[_ids.Length];
        for (var index = 0; index < _ids.Length; index++)
        {
            if (!Timeline.TryStart(_ids[index], 0, out _playbacks[index]))
            {
                throw new InvalidOperationException($"Could not start timeline {_ids[index]}.");
            }
        }

        var receipt = Dynamic();
        if (receipt.Successes != Operations)
        {
            throw new InvalidOperationException($"Expected {Operations} successful seeks, received {receipt.Successes}.");
        }
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public RoutingReceipt Dynamic()
        => Scale switch
        {
            RoutingScale.One => RunOne(),
            RoutingScale.Sixteen => RunSixteen(),
            RoutingScale.TwoHundredFiftySix => RunWide(),
            _ => throw new InvalidOperationException()
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RoutingReceipt RunOne()
    {
        var successes = 0;
        ulong ownerSum = 0;
        for (var operation = 0; operation < Operations; operation++)
        {
            var index = operation % _ids.Length;
            var id = _ids[index];
            ref var playback = ref _playbacks[index];
            var data = new One000.DynamicData(ref playback, ref _oneState);
            successes += Timeline.TrySeek(id, ref data, 0) ? 1 : 0;
            ownerSum += playback.Owner;
        }

        return new RoutingReceipt(successes, ownerSum);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RoutingReceipt RunSixteen()
    {
        var successes = 0;
        ulong ownerSum = 0;
        for (var operation = 0; operation < Operations; operation++)
        {
            var index = operation % _ids.Length;
            var id = _ids[index];
            ref var playback = ref _playbacks[index];
            var data = new Sixteen000.DynamicData(ref playback, ref _sixteenState);
            successes += Timeline.TrySeek(id, ref data, 0) ? 1 : 0;
            ownerSum += playback.Owner;
        }

        return new RoutingReceipt(successes, ownerSum);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RoutingReceipt RunWide()
    {
        var successes = 0;
        ulong ownerSum = 0;
        for (var operation = 0; operation < Operations; operation++)
        {
            var index = operation % _ids.Length;
            var id = _ids[index];
            ref var playback = ref _playbacks[index];
            var data = new Wide000.DynamicData(ref playback, ref _wideState);
            successes += Timeline.TrySeek(id, ref data, 0) ? 1 : 0;
            ownerSum += playback.Owner;
        }

        return new RoutingReceipt(successes, ownerSum);
    }

    private sealed class RoutingConfig : ManualConfig
    {
        public RoutingConfig()
        {
            AddJob(Job.Default
                .WithWarmupCount(16)
                .WithIterationCount(12)
                .WithIterationTime(TimeInterval.FromMilliseconds(250)));
            AddExporter(JsonExporter.Full);
        }
    }
}
