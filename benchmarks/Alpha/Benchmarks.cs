using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
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
        WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(40));
    }
}

[Config(typeof(AlphaConfig))]
public class SumBenchmarks
{
    public const int Operations = 65536;
    private readonly int[] _deltas = new int[Operations];
    private ushort _id;

    [Params(SeekPattern.Forward, SeekPattern.Alternating)]
    public SeekPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _id = SumTimeline.Id;
        FillDeltas(_deltas, Pattern);
        var expected = DirectScalar();
        Require(expected, TypedScalar(), nameof(TypedScalar));
        Require(expected, DynamicScalar(), nameof(DynamicScalar));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public SumReceipt DirectScalar()
    {
        long position = 0;
        uint gameTick = 0;
        var sum = 0f;
        foreach (var delta in _deltas)
            Direct.Sum(delta, ref position, ref gameTick, ref sum);
        return SumReceipt.Capture(position, gameTick, PlaybackFlags.Started, sum, Operations);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt TypedScalar()
    {
        var sum = 0f;
        var playback = SumTimeline.Start(0u);
        var data = new SumTimeline.Data(ref playback, ref sum);
        var successes = 0;
        foreach (var delta in _deltas)
            successes += SumTimeline.TrySeek(ref data, delta) ? 1 : 0;
        return SumReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, sum, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public SumReceipt DynamicScalar()
    {
        var sum = 0f;
        Timeline.TryStart(_id, 0u, out var playback);
        var data = new SumTimeline.DynamicData(ref playback, ref sum);
        var successes = 0;
        foreach (var delta in _deltas)
            successes += Timeline.TrySeek(_id, ref data, delta) ? 1 : 0;
        return SumReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, sum, successes);
    }

    internal static void FillDeltas(int[] deltas, SeekPattern pattern)
    {
        uint random = 0xA312AFD5;
        for (var index = 0; index < deltas.Length; index++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            deltas[index] = pattern == SeekPattern.Forward || (random & 1u) == 0u ? 1 : -1;
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
    private readonly int[] _deltas = new int[Operations];
    private readonly Pose _currentPose = new(10f, 20f);
    private readonly AnimationSettings _animationSettings = new(0.5f);
    private readonly Health _currentHealth = new(100f);
    private readonly DamageSettings _damageSettings = new(2f);
    private ushort _id;

    [Params(SeekPattern.Forward, SeekPattern.Alternating)]
    public SeekPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _id = CombatTimeline.Id;
        SumBenchmarks.FillDeltas(_deltas, Pattern);
        var expected = DirectScalar();
        SumBenchmarks.Require(expected, TypedScalar(), nameof(TypedScalar));
        SumBenchmarks.Require(expected, DynamicScalar(), nameof(DynamicScalar));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public CombatReceipt DirectScalar()
    {
        long position = 0;
        uint gameTick = 0;
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        foreach (var delta in _deltas)
            Direct.Combat(
                delta,
                ref position,
                ref gameTick,
                in _currentPose,
                in _animationSettings,
                in _currentHealth,
                in _damageSettings,
                ref pose,
                ref trace,
                ref health);
        return CombatReceipt.Capture(position, gameTick, PlaybackFlags.Started, in pose, in health, in trace, Operations);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt TypedScalar()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        var playback = CombatTimeline.Start(0u);
        var data = new CombatTimeline.Data(
            ref playback,
            in _animationSettings,
            in _currentHealth,
            in _currentPose,
            in _damageSettings,
            ref health,
            ref pose,
            ref trace);
        var successes = 0;
        foreach (var delta in _deltas)
            successes += CombatTimeline.TrySeek(ref data, delta) ? 1 : 0;
        return CombatReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, in pose, in health, in trace, successes);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public CombatReceipt DynamicScalar()
    {
        var pose = _currentPose;
        var health = _currentHealth;
        var trace = default(Trace);
        Timeline.TryStart(_id, 0u, out var playback);
        var data = new CombatTimeline.DynamicData(
            ref playback,
            in _animationSettings,
            in _currentHealth,
            in _currentPose,
            in _damageSettings,
            ref health,
            ref pose,
            ref trace);
        var successes = 0;
        foreach (var delta in _deltas)
            successes += Timeline.TrySeek(_id, ref data, delta) ? 1 : 0;
        return CombatReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, in pose, in health, in trace, successes);
    }
}
