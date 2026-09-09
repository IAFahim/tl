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
var input = new Combat.Input(
    currentPose: in owner.Pose,
    animationSettings: in animation,
    currentHealth: in owner.Health,
    damageSettings: in damage);
var output = new Combat.Output(
    nextPose: ref owner.NextPose,
    trace: ref trace,
    nextHealth: ref owner.NextHealth);

Require(Timeline.TryStart(Combat.Id, out var playback));
Require(playback.Owner == Combat.Id);
Require(Timeline.IsValid(Combat.Id));
Require(!Timeline.IsValid(60_000));
Require(!Timeline.TryStart(60_000, out var absentPlayback));
Require(absentPlayback == default);
RequireThrows<ArgumentOutOfRangeException>(() => Timeline.Duration(60_000));
RequireThrows<ArgumentOutOfRangeException>(() => Timeline.IsLooping(60_000));
Require(Timeline.TryStart(Combat.Id, 37u, out var positionedPlayback));
Require(positionedPlayback.Tick == 37u && positionedPlayback.Owner == Combat.Id);
Require(Timeline.TryForward(Combat.Id, in playback, 0u, in input, ref output, out playback));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(trace.Calls == 1 && trace.Exits == 0);

Require(Timeline.TryForward(Combat.Id, in playback, 16u, in input, ref output, out playback));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(owner.NextHealth == new Health(90f));
Require(trace.Calls == 3);

var beforeBatch = trace.Calls;
ReadOnlySpan<uint> ticks = [17u, 31u, 47u, 55u, 63u];
Require(Timeline.All[Combat.Id].TryForward(in playback, ticks, in input, ref output, out playback));
Require(trace.Calls == beforeBatch + 8);
Require(playback.Tick == 63u);

var wrongPlayback = playback;
var rejectedPose = owner.NextPose;
var rejectedHealth = owner.NextHealth;
var rejectedTrace = trace;
Require(Timeline.TryStart(Other.Id, out var otherPlayback));
Require(!Timeline.TryForward(Other.Id, in wrongPlayback, 1u, in input, ref output, out var failed));
Require(failed == wrongPlayback);
Require(!Timeline.TryForward(Combat.Id, in otherPlayback, 1u, in input, ref output, out failed));
Require(failed == otherPlayback);
Require(!Timeline.TryForward(60_000, in playback, 1u, in input, ref output, out failed));
Require(failed == playback);
Require(!Timeline.All[60_000].TryForward(in playback, 1u, in input, ref output, out failed));
Require(failed == playback);
Require(owner.NextPose == rejectedPose && owner.NextHealth == rejectedHealth && trace == rejectedTrace);

var unstarted = default(Playback);
Require(!Timeline.TryForward(Combat.Id, in unstarted, 1u, in input, ref output, out failed));
Require(failed == unstarted);
Require(!Timeline.TryStop(Combat.Id, in unstarted, out failed));
Require(failed == unstarted);
Require(!Timeline.TryStop(60_000, in playback, out failed));
Require(failed == playback);

var defaultInput = default(Combat.Input);
Require(!Timeline.TryForward(Combat.Id, in playback, 1u, in defaultInput, ref output, out failed));
Require(owner.NextPose == rejectedPose && owner.NextHealth == rejectedHealth && trace == rejectedTrace);
Require(RejectsDefaultOutput(in playback, in input));

Require(Timeline.TryStop(Combat.Id, in playback, out var stopped));
Require(stopped.Has(PlaybackFlags.Stopped));
Require(Timeline.TryStop(Combat.Id, in stopped, out var stoppedAgain));
Require(stoppedAgain == stopped);
Require(!Timeline.TryForward(Combat.Id, in stopped, 1u, in input, ref output, out failed));
Require(failed == stopped);

var gcSettings = new AnimationSettings(1f, true);
var gcInput = new Combat.Input(
    currentPose: in owner.Pose,
    animationSettings: in gcSettings,
    currentHealth: in owner.Health,
    damageSettings: in damage);
var gcOutput = new Combat.Output(
    nextPose: ref owner.NextPose,
    trace: ref trace,
    nextHealth: ref owner.NextHealth);
Require(Timeline.TryStart(Combat.Id, out var gcPlayback));
Require(Timeline.TryForward(Combat.Id, in gcPlayback, 2u, in gcInput, ref gcOutput, out gcPlayback));
Require(owner.NextPose == new Pose(12f, 21f));

var backwardTrace = trace;
Require(Timeline.TryStart(Combat.Id, 63u, out var backwardPlayback));
Require(Timeline.TryBackward(Combat.Id, in backwardPlayback, 47u, in input, ref output, out backwardPlayback));
Require(backwardPlayback.Tick == 47u && backwardPlayback.Owner == Combat.Id);
Require(owner.NextPose == new Pose(7f, 18.5f));
Require(owner.NextHealth == new Health(110f));
Require(trace.Calls == backwardTrace.Calls + 2);

Require(Unsafe.SizeOf<Playback>() == 12);
Require(Timeline.Duration(Combat.Id) == 64u);
Require(!Timeline.IsLooping(Combat.Id));

Require(Timeline.TryStart(Combat.Id, out var allocationPlayback));
for (uint tick = 0; tick < 64; tick++)
    Require(Timeline.TryForward(Combat.Id, in allocationPlayback, tick, in input, ref output, out allocationPlayback));
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
for (var index = 0; index < 4096; index++)
    Require(Timeline.TryForward(Combat.Id, in allocationPlayback, (uint)index & 63u, in input, ref output, out allocationPlayback));
Require(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore);
var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

var emptyTrace = trace;
Require(Timeline.TryForward(Combat.Id, in gcPlayback, ReadOnlySpan<uint>.Empty, in gcInput, ref gcOutput, out var unchanged));
Require(unchanged == gcPlayback && trace == emptyTrace);

var emptyInput = default(Other.Input);
var emptyOutput = default(Other.Output);
Require(Timeline.TryForward(Other.Id, in otherPlayback, 123u, in emptyInput, ref emptyOutput, out var emptyPlayback));
Require(emptyPlayback.Tick == 123u && emptyPlayback.Has(PlaybackFlags.Completed));

var compatibleHealth = owner.NextHealth;
var compatibleCalls = trace.Calls;
Require(Timeline.TryStart(Cycle.Id, out var compatiblePlayback));
Require(Timeline.TryForward(Cycle.Id, in compatiblePlayback, 4u, in input, ref output, out compatiblePlayback));
Require(owner.NextPose == new Pose(11f, 20.5f));
Require(owner.NextHealth == compatibleHealth);
Require(trace.Calls == compatibleCalls + 1);

Require(Timeline.TryGetCompiledRoute(Combat.Id, out var combatRoute));
var forgedCombat = Timeline.RegisterCompiled(Combat.Duration + 1u, !Combat.Loops, combatRoute);
Require(Timeline.TryStart(forgedCombat, out var forgedPlayback));
var forgedPose = owner.NextPose;
var forgedHealth = owner.NextHealth;
var forgedTrace = trace;
Require(!Timeline.TryForward(forgedCombat, in forgedPlayback, 4u, in input, ref output, out failed));
Require(failed == forgedPlayback);
Require(owner.NextPose == forgedPose && owner.NextHealth == forgedHealth && trace == forgedTrace);

var hookReceipt = default(HookReceipt);
var timestamp = new DateTime(2026, 1, 1);
var hookInput = new HookTimeline.Input(timestamp: in timestamp);
var hookOutput = new HookTimeline.Output(ref hookReceipt);
Require(Timeline.TryStart(HookTimeline.Id, out var hookPlayback));
Require(Timeline.TryForward(HookTimeline.Id, in hookPlayback, ReadOnlySpan<uint>.Empty, in hookInput, ref hookOutput, out var emptyHookPlayback));
Require(emptyHookPlayback == hookPlayback && hookReceipt == default);
Require(Timeline.TryForward(HookTimeline.Id, in hookPlayback, 0u, in hookInput, ref hookOutput, out hookPlayback));
Require(hookReceipt == new HookReceipt(1, 0, 1));
Require(Timeline.TryForward(HookTimeline.Id, in hookPlayback, 2u, in hookInput, ref hookOutput, out hookPlayback));
Require(hookReceipt == new HookReceipt(2, 1, 2));

var cycleInput = new Cycle.Input(
    currentPose: in owner.Pose,
    animationSettings: in animation);
var cycleOutput = new Cycle.Output(
    nextPose: ref owner.NextPose,
    trace: ref trace);
Require(Timeline.TryStart(Combat.Id, out var incompatiblePlayback));
var incompatiblePose = owner.NextPose;
var incompatibleTrace = trace;
Require(!Timeline.TryForward(Combat.Id, in incompatiblePlayback, 4u, in cycleInput, ref cycleOutput, out failed));
Require(failed == incompatiblePlayback && owner.NextPose == incompatiblePose && trace == incompatibleTrace);
Require(Timeline.TryStart(Cycle.Id, out var cyclePlayback));
Require(Timeline.TryForward(Cycle.Id, in cyclePlayback, 129u, in cycleInput, ref cycleOutput, out cyclePlayback));
Require(cyclePlayback.Tick == 129u && cyclePlayback.Cycles == 2);
Require(Timeline.IsLooping(Cycle.Id) && Timeline.Duration(Cycle.Id) == 64u);
var cycleTrace = trace;
Require(!Timeline.TryForward(Cycle.Id, in cyclePlayback, 4_194_368u, in cycleInput, ref cycleOutput, out failed));
Require(failed == cyclePlayback && trace == cycleTrace);

ReadDefinition<Other>();
RunRegistryReceipt();

Console.WriteLine($"behavior: pose={owner.NextPose} health={owner.NextHealth} trace={trace}");
Console.WriteLine($"lifecycle: owner={playback.Owner} tick={playback.Tick} flags={playback.Flags} size={Unsafe.SizeOf<Playback>()}");
Console.WriteLine($"allocation: 4096 scalar calls retained {allocatedAfter - allocatedBefore} B");
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

static bool RejectsDefaultOutput(in Playback playback, scoped in Combat.Input input)
{
    var output = default(Combat.Output);
    return !Timeline.TryForward(Combat.Id, in playback, 1u, in input, ref output, out _);
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
    for (var expected = (int)first + 1; expected <= byte.MaxValue; expected++)
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
public readonly record struct Trace(int Calls, int Enters, int Stays, int Exits)
{
    public Trace Add(ClipState state) => state switch
    {
        ClipState.Enter => this with { Calls = Calls + 1, Enters = Enters + 1 },
        ClipState.Stay => this with { Calls = Calls + 1, Stays = Stays + 1 },
        ClipState.Exit => this with { Calls = Calls + 1, Exits = Exits + 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}

public readonly partial struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Forward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        Collect(in animationSettings);
        nextPose = new(
            currentPose.X + frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Clip.Y * animationSettings.Weight);
        trace = trace.Add(frame.State);
    }

    public static void Backward(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose,
        ref Trace trace)
    {
        Collect(in animationSettings);
        nextPose = new(
            currentPose.X - frame.Clip.X * animationSettings.Weight,
            currentPose.Y - frame.Clip.Y * animationSettings.Weight);
        trace = trace.Add(frame.State);
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

public readonly partial struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Forward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value - frame.Clip.Amount * damageSettings.Multiplier);
        trace = trace.Add(frame.State);
    }

    public static void Backward(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth,
        ref Trace trace)
    {
        nextHealth = new(currentHealth.Value + frame.Clip.Amount * damageSettings.Multiplier);
        trace = trace.Add(frame.State);
    }
}

public readonly partial struct HookTrack : ITrack<HookClip>
{
    public void Blend(in HookClip first, in HookClip second, float factor, out HookClip result)
        => result = factor < 0.5f ? first : second;

    public static void Forward(in Frame<HookTrack, HookClip> frame, in DateTime timestamp, ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { Track = hookReceipt.Track + timestamp.Day };

    public static void Backward(in Frame<HookTrack, HookClip> frame, in DateTime timestamp, ref HookReceipt hookReceipt)
        => hookReceipt = hookReceipt with { Track = hookReceipt.Track - timestamp.Day };
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
