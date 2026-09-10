using System.Runtime.CompilerServices;
using Tl;

if (args is ["--capacity"])
{
    RunCapacityReceipt();
    return 0;
}

if (args is ["--module-capacity"])
{
    RunModuleCapacityReceipt();
    return 0;
}

var owner = new Owner
{
    Pose = new Pose(10f, 20f),
    Health = new Health(100f),
    NextPose = new Pose(10f, 20f),
    NextHealth = new Health(100f),
};
var animation = new AnimationSettings(0.5f, false);
var damage = new DamageSettings(2f);
var trace = default(Trace);
var playback = Combat.Start(uint.MaxValue);
var data = new Combat.Data(
    ref playback,
    in animation,
    in owner.Health,
    in owner.Pose,
    in damage,
    ref owner.NextHealth,
    ref owner.NextPose,
    ref trace);

Require(playback.Position == 0L && playback.GameTick == uint.MaxValue);
Require(Timeline.IsValid(Combat.Id));
Require(!Timeline.IsValid(60_000));
Require(!Timeline.TryStart(60_000, 0u, out var absentPlayback));
Require(absentPlayback == default);
RequireThrows<ArgumentOutOfRangeException>(() => Timeline.Duration(60_000));
RequireThrows<ArgumentOutOfRangeException>(() => Timeline.IsLooping(60_000));
var unchanged = playback;
Require(Combat.TrySeek(ref data, 0));
Require(playback == unchanged && trace == default);
Require(Combat.TrySeek(ref data, 1));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(trace == new Trace(1, 1, 0, 0));
Require(playback.Position == 1L && playback.GameTick == 0u);

Require(Combat.TrySeek(ref data, 16));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(owner.NextHealth == new Health(90f));
Require(trace.Calls == 26 && trace.Starts == 3);

Require(Combat.TrySeek(ref data, 47));
Require(playback.Position == 64L && playback.GameTick == 63u);
Require(owner.NextPose == new Pose(13f, 21.5f));
Require(owner.NextHealth == new Health(100f));
var rejectedPose = owner.NextPose;
var rejectedHealth = owner.NextHealth;
var rejectedTrace = trace;
unchanged = playback;
Require(!Combat.TrySeek(ref data, 1));
Require(!Combat.TrySeek(ref data, int.MinValue));
Require(playback == unchanged);
Require(owner.NextPose == rejectedPose && owner.NextHealth == rejectedHealth && trace == rejectedTrace);

var backwardCalls = trace.Calls;
Require(Combat.TrySeek(ref data, -17));
Require(playback.Position == 47L && playback.GameTick == 46u);
Require(owner.NextPose == new Pose(7f, 18.5f));
Require(owner.NextHealth == new Health(110f));
Require(trace.Calls == backwardCalls + 11);

Require(Combat.TryStop(in playback, out var stopped));
playback = stopped;
Require(stopped.Has(PlaybackFlags.Stopped));
Require(Combat.TryStop(in stopped, out var stoppedAgain));
Require(stoppedAgain == stopped);
rejectedTrace = trace;
Require(!Combat.TrySeek(ref data, -1));
Require(playback == stopped && trace == rejectedTrace);
var emptyData = default(Combat.Data);
Require(!Combat.TrySeek(ref emptyData, 0));

var gcSettings = new AnimationSettings(1f, true);
var gcPlayback = Combat.Start(0u);
var gcData = new Combat.Data(
    ref gcPlayback,
    in gcSettings,
    in owner.Health,
    in owner.Pose,
    in damage,
    ref owner.NextHealth,
    ref owner.NextPose,
    ref trace);
Require(Combat.TrySeek(ref gcData, 3));
Require(owner.NextPose == new Pose(12f, 21f));

Require(Unsafe.SizeOf<Playback>() == 16);
Require(Unsafe.SizeOf<Playback<Combat>>() == 16);
Require(Timeline.Duration(Combat.Id) == 64u);
Require(!Timeline.IsLooping(Combat.Id));
Require(Combat.TrackCount == 2 && Combat.ClipCount == 4 && Combat.RegionCount == 7);
Require(Combat.StaticDataBytes > 0);
Require(Timeline.RegistryRetainedBytes > 0);

var allocationPlayback = Cycle.Start(0u);
var allocationData = new Cycle.Data(
    ref allocationPlayback,
    in animation,
    in owner.Pose,
    ref owner.NextPose,
    ref trace);
for (var index = 0; index < 128; index++)
    Require(Cycle.TrySeek(ref allocationData, (index & 1) == 0 ? 1 : -1));
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
const int allocationCalls = 1_048_576;
for (var index = 0; index < allocationCalls; index++)
    Require(Cycle.TrySeek(ref allocationData, (index & 1) == 0 ? 1 : -1));
Require(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore);
var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

var emptyPlayback = Other.Start(123u);
var otherData = new Other.Data(ref emptyPlayback);
Require(Other.TrySeek(ref otherData, 0));
Require(emptyPlayback.Position == 0L && emptyPlayback.GameTick == 123u);
Require(!Other.TrySeek(ref otherData, 1));

var compatibleHealth = owner.NextHealth;
var compatibleCalls = trace.Calls;
Require(Timeline.TryStart(Cycle.Id, 0u, out var compatiblePlayback));
var compatibleData = new Combat.DynamicData(
    ref compatiblePlayback,
    in animation,
    in owner.Health,
    in owner.Pose,
    in damage,
    ref owner.NextHealth,
    ref owner.NextPose,
    ref trace);
Require(Timeline.TrySeek(Cycle.Id, ref compatibleData, 1));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(owner.NextHealth == compatibleHealth);
Require(trace.Calls == compatibleCalls + 1);

Require(Timeline.TryGetCompiledRoute(Combat.Id, out var combatRoute));
var forgedCombat = Timeline.RegisterCompiled(Combat.Duration + 1u, !Combat.Loops, combatRoute);
Require(Timeline.TryStart(forgedCombat, 0u, out var forgedPlayback));
var forgedPose = owner.NextPose;
var forgedHealth = owner.NextHealth;
var forgedTrace = trace;
var forgedData = new Combat.DynamicData(
    ref forgedPlayback,
    in animation,
    in owner.Health,
    in owner.Pose,
    in damage,
    ref owner.NextHealth,
    ref owner.NextPose,
    ref trace);
Require(!Timeline.TrySeek(forgedCombat, ref forgedData, 1));
Require(owner.NextPose == forgedPose && owner.NextHealth == forgedHealth && trace == forgedTrace);

var hookReceipt = default(HookReceipt);
var timestamp = new DateTime(2026, 1, 1);
var hookPlayback = HookTimeline.Start(0u);
var hookData = new HookTimeline.Data(ref hookPlayback, in timestamp, ref hookReceipt);
Require(HookTimeline.TrySeek(ref hookData, 0));
Require(hookPlayback.Position == 0L && hookReceipt == default);
Require(HookTimeline.TrySeek(ref hookData, 1));
Require(hookReceipt == new HookReceipt(1, 0, 1));
Require(HookTimeline.TrySeek(ref hookData, 2));
Require(hookReceipt == new HookReceipt(3, 1, 3));

Require(Timeline.TryStart(Combat.Id, 0u, out var incompatiblePlayback));
var incompatibleData = new Cycle.DynamicData(
    ref incompatiblePlayback,
    in animation,
    in owner.Pose,
    ref owner.NextPose,
    ref trace);
var incompatiblePose = owner.NextPose;
var incompatibleTrace = trace;
Require(!Timeline.TrySeek(Combat.Id, ref incompatibleData, 1));
Require(incompatiblePlayback.Position == 0L && owner.NextPose == incompatiblePose && trace == incompatibleTrace);
var cyclePlayback = Cycle.Start(200_000u);
var cycleData = new Cycle.Data(
    ref cyclePlayback,
    in animation,
    in owner.Pose,
    ref owner.NextPose,
    ref trace);
Require(Cycle.TrySeek(ref cycleData, 129));
Require(cyclePlayback.Position == 129L && cyclePlayback.GameTick == 200_129u);
Require(Timeline.IsLooping(Cycle.Id) && Timeline.Duration(Cycle.Id) == 64u);

Require(Timeline.TryStart(Combat.Id, 17u, out var dynamicPlayback));
var dynamicData = new Combat.DynamicData(
    ref dynamicPlayback,
    in animation,
    in owner.Health,
    in owner.Pose,
    in damage,
    ref owner.NextHealth,
    ref owner.NextPose,
    ref trace);
Require(Timeline.TrySeek(Combat.Id, ref dynamicData, 1));
Require(dynamicPlayback.Position == 1L && dynamicPlayback.GameTick == 18u);
var dynamicBefore = dynamicPlayback;
rejectedPose = owner.NextPose;
rejectedHealth = owner.NextHealth;
rejectedTrace = trace;
Require(!Timeline.TrySeek(Other.Id, ref dynamicData, 1));
Require(!Timeline.TrySeek(60_000, ref dynamicData, 1));
Require(dynamicPlayback == dynamicBefore);
Require(owner.NextPose == rejectedPose && owner.NextHealth == rejectedHealth && trace == rejectedTrace);
var emptyDynamicData = default(Combat.DynamicData);
Require(!Timeline.TrySeek(Combat.Id, ref emptyDynamicData, 0));

ReadDefinition<Other>();
RunRegistryReceipt();
DataAliasingReceipt.Run();

Console.WriteLine($"behavior: pose={owner.NextPose} health={owner.NextHealth} trace={trace}");
Console.WriteLine($"lifecycle: position={playback.Position} gameTick={playback.GameTick} flags={playback.Flags} size={Unsafe.SizeOf<Playback<Combat>>()}");
Console.WriteLine($"allocation: {allocationCalls} scalar calls retained {allocatedAfter - allocatedBefore} B");
Console.WriteLine($"memory: combat static data={Combat.StaticDataBytes} B registry={Timeline.RegistryRetainedBytes} B");
return 0;

static void Require(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
{
    if (!condition)
        throw new InvalidOperationException(expression);
}

static void RequireThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static void ReadDefinition<TTimeline>() where TTimeline : unmanaged, ITimeline
    => TTimeline.Define(default);

static void RunRegistryReceipt()
{
    const int count = 512;
    var ids = new ushort[count];
    Parallel.For(0, count, index =>
    {
        var duration = (uint)(index * 2 + 1);
        var loops = (index & 1) != 0;
        var id = Timeline.RegisterCompiled(duration, loops, new CompiledRoute(0xFF, (byte)index));
        Require(Timeline.IsValid(id));
        Require(Timeline.Duration(id) == duration);
        Require(Timeline.IsLooping(id) == loops);
        ids[index] = id;
    });

    var ordered = ids.Order().ToArray();
    Require(ordered.Distinct().Count() == count);

    var lazy = new ushort[count];
    Parallel.For(0, count, index => lazy[index] = LazyRegistration.Id);
    Require(lazy.All(id => id == lazy[0]));
    Require(Timeline.Duration(lazy[0]) == uint.MaxValue);
    Require(Timeline.IsLooping(lazy[0]));

    var routed = Timeline.RegisterCompiled(uint.MaxValue, true, new CompiledRoute(0xA5, 0x5A));
    Require(Timeline.TryGetCompiledRoute(routed, out var route));
    Require(route == new CompiledRoute(0xA5, 0x5A));

    const int moduleCount = 64;
    var modules = new byte[moduleCount];
    Parallel.For(0, moduleCount, index => modules[index] = Timeline.RegisterModule());
    Require(modules.Distinct().Count() == moduleCount);

    _ = Timeline.RegisterCompiled(1, false, new CompiledRoute(0xFF, 0xFE));
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    _ = Timeline.RegisterCompiled(2, true, new CompiledRoute(0xFF, 0xFD));
    Require(GC.GetAllocatedBytesForCurrentThread() == allocated);

    Console.WriteLine($"registry: {count} parallel unique IDs; {moduleCount} parallel unique modules; lazy={lazy[0]} route={route.Module:X2}/{route.Ordinal:X2}");
}

static void RunCapacityReceipt()
{
    for (var expected = 0; expected <= ushort.MaxValue; expected++)
    {
        var duration = unchecked((uint)expected * 65_537u);
        var loops = (expected & 1) != 0;
        var route = new CompiledRoute((byte)(expected >> 8), (byte)expected);
        var id = Timeline.RegisterCompiled(duration, loops, route);
        Require(id == (ushort)expected);
        Require(Timeline.Duration(id) == duration);
        Require(Timeline.IsLooping(id) == loops);
        Require(Timeline.TryGetCompiledRoute(id, out var actualRoute));
        Require(actualRoute == route);
    }

    Require(Timeline.IsValid(ushort.MaxValue));
    var rejected = false;
    try
    {
        _ = Timeline.RegisterCompiled(1, false, default);
    }
    catch (InvalidOperationException)
    {
        rejected = true;
    }
    Require(rejected);
    Console.WriteLine($"capacity: {ushort.MaxValue + 1} live IDs, max={ushort.MaxValue}, overflow rejected");
}

static void RunModuleCapacityReceipt()
{
    var first = Timeline.RegisterModule();
    for (var expected = first + 1; expected <= byte.MaxValue; expected++)
        Require(Timeline.RegisterModule() == (byte)expected);

    var rejected = false;
    try
    {
        _ = Timeline.RegisterModule();
    }
    catch (InvalidOperationException)
    {
        rejected = true;
    }
    Require(rejected);
    Console.WriteLine($"module capacity: first available={first}, max={byte.MaxValue}, overflow rejected");
}

static class LazyRegistration
{
    internal static readonly ushort Id = Timeline.RegisterCompiled(uint.MaxValue, true, new CompiledRoute(0xFF, 0xFF));
}

public sealed class Owner
{
    public Pose Pose;
    public Health Health;
    public Pose NextPose;
    public Health NextHealth;
}

public readonly record struct Pose(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct AnimationSettings(float Weight, bool Collect);
public readonly record struct DamageSettings(float Multiplier);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct DamageClip(float Amount);
public readonly record struct HookClip(byte Value);
public readonly record struct HookReceipt(int Before, int Track, int After);
public readonly record struct Trace(int Calls, int Starts, int Interior, int Ends)
{
    public Trace Add(FrameFlags flags)
    {
        var boundary = flags & (FrameFlags.ClipStart | FrameFlags.ClipEnd);
        return new(
            Calls + 1,
            Starts + ((flags & FrameFlags.ClipStart) != 0 ? 1 : 0),
            Interior + (boundary == 0 ? 1 : 0),
            Ends + ((flags & FrameFlags.ClipEnd) != 0 ? 1 : 0));
    }
}

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Seek(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        Collect(in animationSettings);
        nextPose = new(
            currentPose.X + frame.Direction * frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Direction * frame.Clip.Y * animationSettings.Weight);
        trace = trace.Add(frame.Flags);
    }

    private static void Collect(in AnimationSettings settings)
    {
        if (!settings.Collect)
            return;
        for (var index = 0; index < 64; index++)
            _ = new byte[1024];
        GC.Collect(2, GCCollectionMode.Forced, true, true);
    }
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Seek(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value - frame.Direction * frame.Clip.Amount * damageSettings.Multiplier);
        trace = trace.Add(frame.Flags);
    }
}

public readonly struct HookTrack : ITrack<HookClip>
{
    public void Blend(in HookClip first, in HookClip second, float factor, out HookClip result)
        => result = factor < 0.5f ? first : second;

    public static void Seek(in Frame<HookTrack, HookClip> frame, in DateTime timestamp, ref HookReceipt hookReceipt)
        => hookReceipt = new(
            hookReceipt.Before,
            hookReceipt.Track + frame.Direction * timestamp.Day * frame.Clip.Value,
            hookReceipt.After);
}

public readonly struct BeforeHook : IHook
{
    public static void Forward(ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { Before = hookReceipt.Before + 1 };

    public static void Backward(ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { Before = hookReceipt.Before - 1 };
}

public readonly struct AfterHook : IHook
{
    public static void Forward(ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { After = hookReceipt.After + 1 };

    public static void Backward(ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { After = hookReceipt.After - 1 };
}

public readonly partial struct Combat : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        var damage = builder.Track(new DamageTrack());
        builder.Clip(animation, new AnimationClip(2f, 1f), 0u, 32u);
        builder.Clip(animation, new AnimationClip(6f, 3f), 16u, 48u);
        builder.Clip(damage, new DamageClip(5f), 8u, 56u);
        builder.Clip(damage, new DamageClip(0f), 63u, 64u);
    }
}

public readonly partial struct Other : ITimeline
{
    public static void Define(scoped Builder builder)
    {
    }
}

public readonly partial struct Cycle : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        builder.Clip(animation, new AnimationClip(2f, 1f), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct HookTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        builder.Before<BeforeHook>();
        var hook = builder.Track(new HookTrack());
        builder.Clip(hook, new HookClip(1), 2u, 4u);
        builder.After<AfterHook>();
    }
}
