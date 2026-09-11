using System.Runtime.CompilerServices;
using Tl;

if (args is ["--capacity"])
{
    RunBatchReceipt();
    return 0;
}

if (args is ["--module-capacity"])
{
    RunSchemaReceipt();
    return 0;
}

Require(args.Length == 0);
RunBehaviorReceipt();
RunLoopReceipt();
RunSchemaReceipt();
RunBatchReceipt();
RunAllocationReceipt();
Console.WriteLine($"memory: state={Unsafe.SizeOf<ReceiptCatalog.State>()} B static={Combo.StaticDataBytes + Short.StaticDataBytes + Cycle.StaticDataBytes + CycleOne.StaticDataBytes + AliasingTimeline.StaticDataBytes} B");
return 0;

static void RunBehaviorReceipt()
{
    var states = new[]
    {
        new ReceiptCatalog.State(ReceiptCatalog.Asset.Combo),
        new ReceiptCatalog.State(ReceiptCatalog.Asset.Short),
        new ReceiptCatalog.State(ReceiptCatalog.Asset.None),
    };
    var receipts = new Receipt[states.Length];
    var query = new ReceiptCatalog.Query().MainRows(states, receipts);

    query.Tick(100u, 0);
    Require(states[0].Position == 0u && receipts[0] == default);
    default(ReceiptCatalog.MainRowsQuery).Tick(0u, int.MinValue);

    query.Tick(200_000u, int.MaxValue);
    Require(states[0].Position == 3u);
    Require(states[1].Position == 1u);
    Require(states[2].Position == 0u);
    Require(receipts[0].FrameOrder == 81_239);
    Require(receipts[0].Calls == 15);
    Require(receipts[0].Value == 74);
    Require(receipts[0].Before == 3 && receipts[0].After == 3);
    Require(receipts[0].LastGameTick == 200_002u && receipts[0].LastTimelineTick == 2u);
    Require(receipts[0].LastCycle == 0);
    Require(receipts[0].Flags.HasFlag(FrameFlags.ClipStart));
    Require(receipts[0].Flags.HasFlag(FrameFlags.ClipEnd));
    Require(receipts[0].Flags.HasFlag(FrameFlags.TimelineStart));
    Require(receipts[0].Flags.HasFlag(FrameFlags.TimelineEnd));
    Require(receipts[0].Flags.HasFlag(FrameFlags.CompletedAfter));
    Require(receipts[1].FrameOrder == 4 && receipts[1].Calls == 1 && receipts[1].Value == 7);
    Require(receipts[1].LastGameTick == 200_000u && receipts[1].LastTimelineTick == 0u);
    Require(receipts[2] == default);

    query.Tick(200_003u, int.MinValue);
    Require(states[0].Position == 0u);
    Require(states[1].Position == 0u);
    Require(states[2].Position == 0u);
    Require(receipts[0].FrameOrder == 93_218);
    Require(receipts[0].Calls == 30);
    Require(receipts[0].Value == 0);
    Require(receipts[0].Before == 0 && receipts[0].After == 0);
    Require(receipts[0].LastGameTick == 200_000u && receipts[0].LastTimelineTick == 0u);
    Require(receipts[0].Flags.HasFlag(FrameFlags.Reverse));
    Require(receipts[0].Flags.HasFlag(FrameFlags.CompletedBefore));
    Require(receipts[1].FrameOrder == 44 && receipts[1].Calls == 2 && receipts[1].Value == 0);
    Require(receipts[1].LastGameTick == 200_002u);

    Console.WriteLine($"behavior: A-B-A={receipts[0].FrameOrder} calls={receipts[0].Calls} value={receipts[0].Value} position={states[0].Position} gameTick={receipts[0].LastGameTick}");
}

static void RunLoopReceipt()
{
    var states = new[]
    {
        new ReceiptCatalog.State(ReceiptCatalog.Asset.Cycle, 0u, long.MaxValue),
        new ReceiptCatalog.State(ReceiptCatalog.Asset.CycleOne, 0u, long.MaxValue),
    };
    var receipts = new Receipt[states.Length];
    var query = new ReceiptCatalog.Query().LoopRows(states, receipts);

    query.Tick(10u, 3);
    Require(states[0].Position == 1u && states[0].Cycle == long.MinValue);
    Require(receipts[0].Calls == 3 && receipts[0].Value == 6);
    Require(receipts[0].LastGameTick == 12u && receipts[0].LastTimelineTick == 0u);
    Require(receipts[0].LastCycle == long.MinValue);
    Require(receipts[0].Flags.HasFlag(FrameFlags.Looping));
    Require(states[1].Position == 0u && states[1].Cycle == long.MinValue + 2);
    Require(receipts[1].Calls == 3 && receipts[1].Value == 3);

    query.Tick(13u, -3);
    Require(states[0].Position == 0u && states[0].Cycle == long.MaxValue);
    Require(receipts[0].Calls == 6 && receipts[0].Value == 0);
    Require(receipts[0].LastGameTick == 10u && receipts[0].LastTimelineTick == 0u);
    Require(receipts[0].LastCycle == long.MaxValue);
    Require(states[1].Position == 0u && states[1].Cycle == long.MaxValue);
    Require(receipts[1].Calls == 6 && receipts[1].Value == 0);

    Console.WriteLine($"loops: cycle={states[0].Cycle} position={states[0].Position} last={receipts[0].LastTimelineTick}/{receipts[0].LastGameTick}");
}

static void RunSchemaReceipt()
{
    var states = new[]
    {
        new ReceiptCatalog.State(ReceiptCatalog.Asset.Combo),
        new ReceiptCatalog.State(ReceiptCatalog.Asset.Combo),
    };
    var receipts = new Receipt[states.Length];
    var query = new ReceiptCatalog.Query().MainRows(states, receipts);
    states[1] = new ReceiptCatalog.State(ReceiptCatalog.Asset.AliasingTimeline);
    var routeRejected = false;
    try
    {
        query.Tick(7u);
    }
    catch (ArgumentException)
    {
        routeRejected = true;
    }
    Require(routeRejected);
    Require(states[0].Position == 0u && states[1].Position == 0u);
    Require(receipts[0] == default && receipts[1] == default);

    var invalidStates = new[] { new ReceiptCatalog.State(ReceiptCatalog.Asset.AliasingTimeline) };
    var invalidReceipts = new Receipt[1];
    RequireThrowsArgument(() => _ = new ReceiptCatalog.Query().MainRows(invalidStates, invalidReceipts));
    Require(invalidStates[0].Position == 0u && invalidReceipts[0] == default);

    RequireThrowsArgument(() => _ = new ReceiptCatalog.Query().MainRows(states, new Receipt[1]));
    Require(receipts[0] == default && receipts[1] == default);

    var aliasState = new[] { new ReceiptCatalog.State(ReceiptCatalog.Asset.AliasingTimeline) };
    uint[] aliased = [5u];
    var calls = new uint[1];
    RequireThrowsArgument(() => _ = new ReceiptCatalog.Query().AliasRows(aliasState, aliased, aliased, calls));
    Require(aliasState[0].Position == 0u && aliased[0] == 5u && calls[0] == 0u);

    uint[] source = [5u];
    uint[] target = [1u];
    var aliasQuery = new ReceiptCatalog.Query().AliasRows(aliasState, source, target, calls);
    aliasQuery.Tick(20u, 2);
    Require(aliasState[0].Position == 2u && target[0] == 13u && calls[0] == 2u);
    aliasQuery.Tick(22u, -2);
    Require(aliasState[0].Position == 0u && target[0] == 1u && calls[0] == 0u);

    Console.WriteLine("schema: mutable route, membership, length, and alias rejected before effects");
}

static void RunBatchReceipt()
{
    const int count = 10_000;
    var states = new ReceiptCatalog.State[count];
    var receipts = new Receipt[count];
    for (var row = 0; row < count; row++)
        states[row] = new ReceiptCatalog.State((row & 1) == 0 ? ReceiptCatalog.Asset.Combo : ReceiptCatalog.Asset.Short);
    var query = new ReceiptCatalog.Query().MainRows(states, receipts);
    query.Tick(1_000u, int.MaxValue);
    for (var row = 0; row < count; row++)
    {
        if ((row & 1) == 0)
            Require(states[row].Position == 3u && receipts[row].Calls == 15 && receipts[row].Value == 74 && receipts[row].FrameOrder == 81_239);
        else
            Require(states[row].Position == 1u && receipts[row].Calls == 1 && receipts[row].Value == 7 && receipts[row].FrameOrder == 4);
    }

    Console.WriteLine($"batch: {count} mixed rows matched the independent finite oracle");
}

static void RunAllocationReceipt()
{
    var states = new[] { new ReceiptCatalog.State(ReceiptCatalog.Asset.Cycle) };
    var receipts = new Receipt[1];
    var query = new ReceiptCatalog.Query().LoopRows(states, receipts);
    for (var index = 0; index < 256; index++)
        query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);

    var before = GC.GetAllocatedBytesForCurrentThread();
    const int calls = 1_048_576;
    for (var index = 0; index < calls; index++)
        query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

    Require(allocated == 0);
    Require(states[0].Position == 0u && states[0].Cycle == 0);
    Require(receipts[0].Value == 0);
    Console.WriteLine($"allocation: {calls} warm generated query ticks retained {allocated} B");
}

static void RequireThrowsArgument(Action action)
{
    try
    {
        action();
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException("Expected ArgumentException.");
}

static void Require(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
{
    if (!condition)
        throw new InvalidOperationException(expression);
}

public readonly record struct Receipt(
    long FrameOrder,
    int Calls,
    long Value,
    uint LastGameTick,
    uint LastTimelineTick,
    long LastCycle,
    FrameFlags Flags,
    int Before,
    int After);

public readonly record struct AlphaClip(int Value);
public readonly record struct BetaClip(int Value);
public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = new((int)(first.Value + (second.Value - first.Value) * factor));
}

public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result)
        => result = new((int)(first.Value + (second.Value - first.Value) * factor));
}

public readonly struct AlphaJob : ITimelineJob<AlphaTrack, AlphaClip>
{
    public static void Execute(in Frame<AlphaTrack, AlphaClip> frame, ref Receipt receipt)
        => receipt = receipt with
        {
            FrameOrder = unchecked(receipt.FrameOrder * 10 + frame.Track.Code),
            Calls = receipt.Calls + 1,
            Value = receipt.Value + frame.Direction * frame.Clip.Value,
            LastGameTick = frame.GameTick,
            LastTimelineTick = frame.TimelineTick,
            LastCycle = frame.Cycle,
            Flags = receipt.Flags | frame.Flags,
        };
}

public readonly struct BetaJob : ITimelineJob<BetaTrack, BetaClip>
{
    public static void Execute(in Frame<BetaTrack, BetaClip> frame, ref Receipt receipt)
        => receipt = receipt with
        {
            FrameOrder = unchecked(receipt.FrameOrder * 10 + frame.Track.Code),
            Calls = receipt.Calls + 1,
            Value = receipt.Value + frame.Direction * frame.Clip.Value * 10,
            LastGameTick = frame.GameTick,
            LastTimelineTick = frame.TimelineTick,
            LastCycle = frame.Cycle,
            Flags = receipt.Flags | frame.Flags,
        };
}

public readonly struct BeforeHook : IHook
{
    public static void Execute(in TimelineFrame frame, ref Receipt receipt)
        => receipt = receipt with
        {
            FrameOrder = frame.Direction > 0 ? 8 : unchecked(receipt.FrameOrder * 10 + 8),
            Calls = receipt.Calls + 1,
            LastGameTick = frame.GameTick,
            LastTimelineTick = frame.TimelineTick,
            LastCycle = frame.Cycle,
            Flags = receipt.Flags | frame.Flags,
            Before = receipt.Before + frame.Direction,
        };
}

public readonly struct AfterHook : IHook
{
    public static void Execute(in TimelineFrame frame, ref Receipt receipt)
        => receipt = receipt with
        {
            FrameOrder = frame.Direction < 0 ? 9 : unchecked(receipt.FrameOrder * 10 + 9),
            Calls = receipt.Calls + 1,
            LastGameTick = frame.GameTick,
            LastTimelineTick = frame.TimelineTick,
            LastCycle = frame.Cycle,
            Flags = receipt.Flags | frame.Flags,
            After = receipt.After + frame.Direction,
        };
}

public readonly partial struct Combo : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        builder.Before<BeforeHook>();
        var first = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        var middle = builder.Track(new BetaTrack(2)).Use<BetaJob>();
        var last = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        builder.Clip(first, new AlphaClip(1), 0u, 3u);
        builder.Clip(first, new AlphaClip(5), 1u, 2u);
        builder.Clip(middle, new BetaClip(2), 0u, 3u);
        builder.Clip(last, new AlphaClip(3), 0u, 3u);
        builder.After<AfterHook>();
    }
}

public readonly partial struct Short : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(7), 0u, 1u);
    }
}

public readonly partial struct Cycle : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(6)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(2), 0u, 2u);
        builder.Looping();
    }
}

public readonly partial struct CycleOne : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(7)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 1u);
        builder.Looping();
    }
}

public readonly record struct AliasingClip(uint Value);
public readonly struct AliasingTrack : IBlend<AliasingClip>
{
    public void Blend(in AliasingClip first, in AliasingClip second, float factor, out AliasingClip result)
        => result = first;
}

public readonly struct AliasingJob : ITimelineJob<AliasingTrack, AliasingClip>
{
    public static void Execute(in Frame<AliasingTrack, AliasingClip> frame, in uint source, ref uint target, ref uint aliasCalls)
    {
        target = unchecked((uint)(target + frame.Direction * (source + frame.Clip.Value)));
        aliasCalls = unchecked((uint)(aliasCalls + frame.Direction));
    }
}

public readonly partial struct AliasingTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AliasingTrack()).Use<AliasingJob>();
        builder.Clip(track, new AliasingClip(1u), 0u, 2u);
    }
}

public readonly struct MainRows;
public readonly struct LoopRows;
public readonly struct AliasRows;

public readonly partial struct ReceiptCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<MainRows>().Asset<Combo>().Asset<Short>();
        builder.Schema<LoopRows>().Asset<Cycle>().Asset<CycleOne>();
        builder.Schema<AliasRows>().Asset<AliasingTimeline>();
    }
}
