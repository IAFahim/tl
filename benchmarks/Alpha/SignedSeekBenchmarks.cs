using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using Tl;

public enum SeekCase
{
    LiteralPositiveOne,
    LiteralNegativeOne,
    RuntimePositiveOne,
    RuntimeNegativeOne,
    AlternatingOne,
    LiteralPositiveFive,
    LiteralNegativeFive,
    RuntimePositiveFive,
    RuntimeNegativeFive,
    RepeatedPositiveOneFive,
    RepeatedNegativeOneFive,
    LiteralPositiveSixtyFour,
    RuntimePositiveSixtyFour,
}

internal interface ISeekCase<TSelf> where TSelf : struct, ISeekCase<TSelf>
{
    static abstract int FramesPerCall { get; }
    static abstract int Delta(int index);
    static abstract bool Typed(ref SumTimeline.Data data, int index);
    static abstract bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index);
}

internal static class RuntimeDeltas
{
    internal static int PositiveOne = 1;
    internal static int NegativeOne = -1;
    internal static int PositiveFive = 5;
    internal static int NegativeFive = -5;
    internal static int PositiveSixtyFour = 64;
    internal static readonly int[] Alternating = CreateAlternating();

    private static int[] CreateAlternating()
    {
        var deltas = new int[SignedSeekBenchmarks.Calls];
        SumBenchmarks.FillDeltas(deltas, SeekPattern.Alternating);
        return deltas;
    }
}

internal readonly struct LiteralPositiveOne : ISeekCase<LiteralPositiveOne>
{
    public static int FramesPerCall => 1;
    public static int Delta(int index) => 1;
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, 1);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, 1);
}

internal readonly struct LiteralNegativeOne : ISeekCase<LiteralNegativeOne>
{
    public static int FramesPerCall => 1;
    public static int Delta(int index) => -1;
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, -1);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, -1);
}

internal readonly struct RuntimePositiveOne : ISeekCase<RuntimePositiveOne>
{
    public static int FramesPerCall => 1;
    public static int Delta(int index) => Volatile.Read(ref RuntimeDeltas.PositiveOne);
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, Volatile.Read(ref RuntimeDeltas.PositiveOne));
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, Volatile.Read(ref RuntimeDeltas.PositiveOne));
}

internal readonly struct RuntimeNegativeOne : ISeekCase<RuntimeNegativeOne>
{
    public static int FramesPerCall => 1;
    public static int Delta(int index) => Volatile.Read(ref RuntimeDeltas.NegativeOne);
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, Volatile.Read(ref RuntimeDeltas.NegativeOne));
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, Volatile.Read(ref RuntimeDeltas.NegativeOne));
}

internal readonly struct AlternatingOne : ISeekCase<AlternatingOne>
{
    public static int FramesPerCall => 1;
    public static int Delta(int index) => RuntimeDeltas.Alternating[index];
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, RuntimeDeltas.Alternating[index]);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, RuntimeDeltas.Alternating[index]);
}

internal readonly struct LiteralPositiveFive : ISeekCase<LiteralPositiveFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => 5;
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, 5);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, 5);
}

internal readonly struct LiteralNegativeFive : ISeekCase<LiteralNegativeFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => -5;
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, -5);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, -5);
}

internal readonly struct RuntimePositiveFive : ISeekCase<RuntimePositiveFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => Volatile.Read(ref RuntimeDeltas.PositiveFive);
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, Volatile.Read(ref RuntimeDeltas.PositiveFive));
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, Volatile.Read(ref RuntimeDeltas.PositiveFive));
}

internal readonly struct RuntimeNegativeFive : ISeekCase<RuntimeNegativeFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => Volatile.Read(ref RuntimeDeltas.NegativeFive);
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, Volatile.Read(ref RuntimeDeltas.NegativeFive));
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, Volatile.Read(ref RuntimeDeltas.NegativeFive));
}

internal readonly struct RepeatedPositiveOneFive : ISeekCase<RepeatedPositiveOneFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => 5;
    public static bool Typed(ref SumTimeline.Data data, int index)
        => SumTimeline.TrySeek(ref data, 1)
            & SumTimeline.TrySeek(ref data, 1)
            & SumTimeline.TrySeek(ref data, 1)
            & SumTimeline.TrySeek(ref data, 1)
            & SumTimeline.TrySeek(ref data, 1);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index)
        => Timeline.TrySeek(id, ref data, 1)
            & Timeline.TrySeek(id, ref data, 1)
            & Timeline.TrySeek(id, ref data, 1)
            & Timeline.TrySeek(id, ref data, 1)
            & Timeline.TrySeek(id, ref data, 1);
}

internal readonly struct RepeatedNegativeOneFive : ISeekCase<RepeatedNegativeOneFive>
{
    public static int FramesPerCall => 5;
    public static int Delta(int index) => -5;
    public static bool Typed(ref SumTimeline.Data data, int index)
        => SumTimeline.TrySeek(ref data, -1)
            & SumTimeline.TrySeek(ref data, -1)
            & SumTimeline.TrySeek(ref data, -1)
            & SumTimeline.TrySeek(ref data, -1)
            & SumTimeline.TrySeek(ref data, -1);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index)
        => Timeline.TrySeek(id, ref data, -1)
            & Timeline.TrySeek(id, ref data, -1)
            & Timeline.TrySeek(id, ref data, -1)
            & Timeline.TrySeek(id, ref data, -1)
            & Timeline.TrySeek(id, ref data, -1);
}

internal readonly struct LiteralPositiveSixtyFour : ISeekCase<LiteralPositiveSixtyFour>
{
    public static int FramesPerCall => 64;
    public static int Delta(int index) => 64;
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, 64);
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, 64);
}

internal readonly struct RuntimePositiveSixtyFour : ISeekCase<RuntimePositiveSixtyFour>
{
    public static int FramesPerCall => 64;
    public static int Delta(int index) => Volatile.Read(ref RuntimeDeltas.PositiveSixtyFour);
    public static bool Typed(ref SumTimeline.Data data, int index) => SumTimeline.TrySeek(ref data, Volatile.Read(ref RuntimeDeltas.PositiveSixtyFour));
    public static bool Dynamic(ushort id, ref SumTimeline.DynamicData data, int index) => Timeline.TrySeek(id, ref data, Volatile.Read(ref RuntimeDeltas.PositiveSixtyFour));
}

[Config(typeof(AlphaConfig))]
public class SignedSeekBenchmarks
{
    public const int Calls = 4096;
    private ushort _id;

    [ParamsAllValues]
    public SeekCase Case { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _id = SumTimeline.Id;
        var expected = Direct();
        SumBenchmarks.Require(expected, Typed(), nameof(Typed));
        SumBenchmarks.Require(expected, Dynamic(), nameof(Dynamic));
    }

    public int FramesPerCall => Case switch
    {
        SeekCase.LiteralPositiveOne => LiteralPositiveOne.FramesPerCall,
        SeekCase.LiteralNegativeOne => LiteralNegativeOne.FramesPerCall,
        SeekCase.RuntimePositiveOne => RuntimePositiveOne.FramesPerCall,
        SeekCase.RuntimeNegativeOne => RuntimeNegativeOne.FramesPerCall,
        SeekCase.AlternatingOne => AlternatingOne.FramesPerCall,
        SeekCase.LiteralPositiveFive => LiteralPositiveFive.FramesPerCall,
        SeekCase.LiteralNegativeFive => LiteralNegativeFive.FramesPerCall,
        SeekCase.RuntimePositiveFive => RuntimePositiveFive.FramesPerCall,
        SeekCase.RuntimeNegativeFive => RuntimeNegativeFive.FramesPerCall,
        SeekCase.RepeatedPositiveOneFive => RepeatedPositiveOneFive.FramesPerCall,
        SeekCase.RepeatedNegativeOneFive => RepeatedNegativeOneFive.FramesPerCall,
        SeekCase.LiteralPositiveSixtyFour => LiteralPositiveSixtyFour.FramesPerCall,
        SeekCase.RuntimePositiveSixtyFour => RuntimePositiveSixtyFour.FramesPerCall,
        _ => throw new ArgumentOutOfRangeException(nameof(Case)),
    };

    [Benchmark(Baseline = true, OperationsPerInvoke = Calls)]
    public SumReceipt Direct() => SelectDirect(Case);

    [Benchmark(OperationsPerInvoke = Calls)]
    public SumReceipt Typed() => SelectTyped(Case);

    [Benchmark(OperationsPerInvoke = Calls)]
    public SumReceipt Dynamic() => SelectDynamic(Case);

    private static SumReceipt SelectDirect(SeekCase seekCase) => seekCase switch
    {
        SeekCase.LiteralPositiveOne => RunDirect<LiteralPositiveOne>(),
        SeekCase.LiteralNegativeOne => RunDirect<LiteralNegativeOne>(),
        SeekCase.RuntimePositiveOne => RunDirect<RuntimePositiveOne>(),
        SeekCase.RuntimeNegativeOne => RunDirect<RuntimeNegativeOne>(),
        SeekCase.AlternatingOne => RunDirect<AlternatingOne>(),
        SeekCase.LiteralPositiveFive => RunDirect<LiteralPositiveFive>(),
        SeekCase.LiteralNegativeFive => RunDirect<LiteralNegativeFive>(),
        SeekCase.RuntimePositiveFive => RunDirect<RuntimePositiveFive>(),
        SeekCase.RuntimeNegativeFive => RunDirect<RuntimeNegativeFive>(),
        SeekCase.RepeatedPositiveOneFive => RunDirect<RepeatedPositiveOneFive>(),
        SeekCase.RepeatedNegativeOneFive => RunDirect<RepeatedNegativeOneFive>(),
        SeekCase.LiteralPositiveSixtyFour => RunDirect<LiteralPositiveSixtyFour>(),
        SeekCase.RuntimePositiveSixtyFour => RunDirect<RuntimePositiveSixtyFour>(),
        _ => throw new ArgumentOutOfRangeException(nameof(seekCase)),
    };

    private SumReceipt SelectTyped(SeekCase seekCase) => seekCase switch
    {
        SeekCase.LiteralPositiveOne => RunTyped<LiteralPositiveOne>(),
        SeekCase.LiteralNegativeOne => RunTyped<LiteralNegativeOne>(),
        SeekCase.RuntimePositiveOne => RunTyped<RuntimePositiveOne>(),
        SeekCase.RuntimeNegativeOne => RunTyped<RuntimeNegativeOne>(),
        SeekCase.AlternatingOne => RunTyped<AlternatingOne>(),
        SeekCase.LiteralPositiveFive => RunTyped<LiteralPositiveFive>(),
        SeekCase.LiteralNegativeFive => RunTyped<LiteralNegativeFive>(),
        SeekCase.RuntimePositiveFive => RunTyped<RuntimePositiveFive>(),
        SeekCase.RuntimeNegativeFive => RunTyped<RuntimeNegativeFive>(),
        SeekCase.RepeatedPositiveOneFive => RunTyped<RepeatedPositiveOneFive>(),
        SeekCase.RepeatedNegativeOneFive => RunTyped<RepeatedNegativeOneFive>(),
        SeekCase.LiteralPositiveSixtyFour => RunTyped<LiteralPositiveSixtyFour>(),
        SeekCase.RuntimePositiveSixtyFour => RunTyped<RuntimePositiveSixtyFour>(),
        _ => throw new ArgumentOutOfRangeException(nameof(seekCase)),
    };

    private SumReceipt SelectDynamic(SeekCase seekCase) => seekCase switch
    {
        SeekCase.LiteralPositiveOne => RunDynamic<LiteralPositiveOne>(),
        SeekCase.LiteralNegativeOne => RunDynamic<LiteralNegativeOne>(),
        SeekCase.RuntimePositiveOne => RunDynamic<RuntimePositiveOne>(),
        SeekCase.RuntimeNegativeOne => RunDynamic<RuntimeNegativeOne>(),
        SeekCase.AlternatingOne => RunDynamic<AlternatingOne>(),
        SeekCase.LiteralPositiveFive => RunDynamic<LiteralPositiveFive>(),
        SeekCase.LiteralNegativeFive => RunDynamic<LiteralNegativeFive>(),
        SeekCase.RuntimePositiveFive => RunDynamic<RuntimePositiveFive>(),
        SeekCase.RuntimeNegativeFive => RunDynamic<RuntimeNegativeFive>(),
        SeekCase.RepeatedPositiveOneFive => RunDynamic<RepeatedPositiveOneFive>(),
        SeekCase.RepeatedNegativeOneFive => RunDynamic<RepeatedNegativeOneFive>(),
        SeekCase.LiteralPositiveSixtyFour => RunDynamic<LiteralPositiveSixtyFour>(),
        SeekCase.RuntimePositiveSixtyFour => RunDynamic<RuntimePositiveSixtyFour>(),
        _ => throw new ArgumentOutOfRangeException(nameof(seekCase)),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SumReceipt RunDirect<TCase>() where TCase : struct, ISeekCase<TCase>
    {
        long position = 0;
        uint gameTick = 0;
        var sum = 0f;
        for (var index = 0; index < Calls; index++)
            global::Direct.Sum(TCase.Delta(index), ref position, ref gameTick, ref sum);
        return SumReceipt.Capture(position, gameTick, PlaybackFlags.Started, sum, Calls);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SumReceipt RunTyped<TCase>() where TCase : struct, ISeekCase<TCase>
    {
        var sum = 0f;
        var playback = SumTimeline.Start(0u);
        var data = new SumTimeline.Data(ref playback, ref sum);
        var successes = 0;
        for (var index = 0; index < Calls; index++)
            successes += TCase.Typed(ref data, index) ? 1 : 0;
        return SumReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, sum, successes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private SumReceipt RunDynamic<TCase>() where TCase : struct, ISeekCase<TCase>
    {
        var sum = 0f;
        Timeline.TryStart(_id, 0u, out var playback);
        var data = new SumTimeline.DynamicData(ref playback, ref sum);
        var successes = 0;
        for (var index = 0; index < Calls; index++)
            successes += TCase.Dynamic(_id, ref data, index) ? 1 : 0;
        return SumReceipt.Capture(playback.Position, playback.GameTick, playback.Flags, sum, successes);
    }
}
