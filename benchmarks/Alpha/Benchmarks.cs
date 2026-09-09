using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Perfolizer.Horology;
using Tl;

public sealed class AlphaConfig : ManualConfig
{
    public AlphaConfig()
    {
        var job = Job.Default
            .WithWarmupCount(16)
            .WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        if (Environment.GetEnvironmentVariable("TL_ALPHA_IN_PROCESS") == "1")
            job = job.WithToolchain(InProcessNoEmitToolchain.Instance);
        AddJob(job.WithId("Jit"));
        if (Environment.GetEnvironmentVariable("TL_ALPHA_NO_TIERING") == "1")
            AddJob(job.WithId("NoTiering").WithEnvironmentVariable("DOTNET_TieredCompilation", "0"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[Config(typeof(AlphaConfig))]
public class SumBenchmarks
{
    public const int Operations = 65536;
    private const int BatchSize = 8;
    private readonly uint[] _ticks = new uint[Operations];
    private ushort _id;

    [Params(TickPattern.Sequential, TickPattern.Random)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _id = SumTimeline.Id;
        FillTicks(_ticks, Pattern);
        var expected = DirectScalar();
        Require(expected, PublicScalar(), nameof(PublicScalar));
        Require(expected, IndexedScalar(), nameof(IndexedScalar));
        Require(expected, PublicBatch8(), nameof(PublicBatch8));
        Require(expected, IndexedBatch8(), nameof(IndexedBatch8));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public SumReceipt DirectScalar()
    {
        var sum = 0f;
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            playback = Direct.Sum(_id, in playback, tick, ref sum);
            successes++;
        }
        return SumReceipt.Capture(in playback, sum, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt PublicScalar()
    {
        var sum = 0f;
        var input = default(SumTimeline.Input);
        var output = new SumTimeline.Output(sum: ref sum);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            successes += Timeline.TryForward(_id, in playback, tick, in input, ref output, out var next) ? 1 : 0;
            playback = next;
        }
        return SumReceipt.Capture(in playback, sum, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt IndexedScalar()
    {
        var sum = 0f;
        var input = default(SumTimeline.Input);
        var output = new SumTimeline.Output(sum: ref sum);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            successes += Timeline.All[_id].TryForward(in playback, tick, in input, ref output, out var next) ? 1 : 0;
            playback = next;
        }
        return SumReceipt.Capture(in playback, sum, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt PublicBatch8()
    {
        var sum = 0f;
        var input = default(SumTimeline.Input);
        var output = new SumTimeline.Output(sum: ref sum);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        for (var index = 0; index < _ticks.Length; index += BatchSize)
        {
            var ticks = _ticks.AsSpan(index, BatchSize);
            successes += Timeline.TryForward(_id, in playback, ticks, in input, ref output, out var next) ? ticks.Length : 0;
            playback = next;
        }
        return SumReceipt.Capture(in playback, sum, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt IndexedBatch8()
    {
        var sum = 0f;
        var input = default(SumTimeline.Input);
        var output = new SumTimeline.Output(sum: ref sum);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        for (var index = 0; index < _ticks.Length; index += BatchSize)
        {
            var ticks = _ticks.AsSpan(index, BatchSize);
            successes += Timeline.All[_id].TryForward(in playback, ticks, in input, ref output, out var next) ? ticks.Length : 0;
            playback = next;
        }
        return SumReceipt.Capture(in playback, sum, successes);
    }

    internal static void FillTicks(uint[] ticks, TickPattern pattern)
    {
        uint random = 0xA312AFD5;
        for (var index = 0; index < ticks.Length; index++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            ticks[index] = pattern == TickPattern.Sequential
                ? (uint)index & 63u
                : random & 63u;
        }
    }

    internal static void Require<T>(T expected, T actual, string method) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new InvalidOperationException($"{method}: {actual} != {expected}");
    }
}

[Config(typeof(AlphaConfig))]
public class CombatBenchmarks
{
    public const int Operations = 65536;
    private const int BatchSize = 8;
    private readonly uint[] _ticks = new uint[Operations];
    private readonly Pose _currentPose = new(10f, 20f);
    private readonly AnimationSettings _animationSettings = new(0.5f);
    private readonly Health _currentHealth = new(100f);
    private readonly DamageSettings _damageSettings = new(2f);
    private ushort _id;

    [Params(TickPattern.Sequential, TickPattern.Random)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _id = CombatTimeline.Id;
        SumBenchmarks.FillTicks(_ticks, Pattern);
        var expected = DirectScalar();
        SumBenchmarks.Require(expected, PublicScalar(), nameof(PublicScalar));
        SumBenchmarks.Require(expected, IndexedScalar(), nameof(IndexedScalar));
        SumBenchmarks.Require(expected, PublicBatch8(), nameof(PublicBatch8));
        SumBenchmarks.Require(expected, IndexedBatch8(), nameof(IndexedBatch8));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public CombatReceipt DirectScalar()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            playback = Direct.Combat(
                _id,
                in playback,
                tick,
                in _currentPose,
                in _animationSettings,
                in _currentHealth,
                in _damageSettings,
                ref pose,
                ref trace,
                ref health);
            successes++;
        }
        return CombatReceipt.Capture(in playback, in pose, in health, in trace, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt PublicScalar()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        var input = Input();
        var output = new CombatTimeline.Output(nextPose: ref pose, trace: ref trace, nextHealth: ref health);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            successes += Timeline.TryForward(_id, in playback, tick, in input, ref output, out var next) ? 1 : 0;
            playback = next;
        }
        return CombatReceipt.Capture(in playback, in pose, in health, in trace, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt IndexedScalar()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        var input = Input();
        var output = new CombatTimeline.Output(nextPose: ref pose, trace: ref trace, nextHealth: ref health);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        foreach (var tick in _ticks)
        {
            successes += Timeline.All[_id].TryForward(in playback, tick, in input, ref output, out var next) ? 1 : 0;
            playback = next;
        }
        return CombatReceipt.Capture(in playback, in pose, in health, in trace, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt PublicBatch8()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        var input = Input();
        var output = new CombatTimeline.Output(nextPose: ref pose, trace: ref trace, nextHealth: ref health);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        for (var index = 0; index < _ticks.Length; index += BatchSize)
        {
            var ticks = _ticks.AsSpan(index, BatchSize);
            successes += Timeline.TryForward(_id, in playback, ticks, in input, ref output, out var next) ? ticks.Length : 0;
            playback = next;
        }
        return CombatReceipt.Capture(in playback, in pose, in health, in trace, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt IndexedBatch8()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        var input = Input();
        var output = new CombatTimeline.Output(nextPose: ref pose, trace: ref trace, nextHealth: ref health);
        Timeline.TryStart(_id, out var playback);
        var successes = 0;
        for (var index = 0; index < _ticks.Length; index += BatchSize)
        {
            var ticks = _ticks.AsSpan(index, BatchSize);
            successes += Timeline.All[_id].TryForward(in playback, ticks, in input, ref output, out var next) ? ticks.Length : 0;
            playback = next;
        }
        return CombatReceipt.Capture(in playback, in pose, in health, in trace, successes);
    }

    private CombatTimeline.Input Input()
        => new(
            currentPose: in _currentPose,
            animationSettings: in _animationSettings,
            currentHealth: in _currentHealth,
            damageSettings: in _damageSettings);
}
