using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class TotalMovementTests
{
    public readonly record struct JobClip(int Value);

    public readonly struct JobTrack : IBlend<JobClip>
    {
        public void Blend(in JobClip first, in JobClip second, float factor, out JobClip result)
            => result = factor < 0.5f ? first : second;
    }

    public readonly struct Job : ITimelineJob<JobTrack, JobClip>;

    public readonly struct Hook : IHook
    {
        public static void Execute(in TimelineFrame frame, in int input, ref int output)
            => output += frame.Direction * input;
    }

    public readonly struct JobTimeline : ITimeline
    {
        public static void Define(scoped Builder builder)
        {
            var track = builder.Track(new JobTrack()).Use<Job>();
            builder.Clip(track, new JobClip(1), 0, 1);
        }
    }

    public readonly struct Rows;

    public readonly struct Catalog : ITimelineCatalog
    {
        public static void Define(scoped CatalogBuilder builder)
            => builder.Schema<Rows>().Asset<JobTimeline>();
    }

    public static TheoryData<uint, uint, uint, bool, bool, uint, uint, FrameFlags> FiniteCases => new()
    {
        { 0, 7, 3, false, false, 7, 0, FrameFlags.None },
        { 1, 0, 0, false, false, 0, 0, FrameFlags.None },
        { 1, 4, 3, false, false, 4, 0, FrameFlags.None },
        { 1, 0, 3, false, true, 1, 0, FrameFlags.TimelineStart },
        { 1, 1, 3, false, true, 2, 1, FrameFlags.None },
        { 1, 2, 3, false, true, 3, 2, FrameFlags.TimelineEnd | FrameFlags.CompletedAfter },
        { 1, 3, 3, false, false, 3, 0, FrameFlags.None },
        { 1, 0, 3, true, false, 0, 0, FrameFlags.None },
        { 1, 1, 3, true, true, 0, 0, FrameFlags.Reverse | FrameFlags.TimelineStart },
        { 1, 2, 3, true, true, 1, 1, FrameFlags.Reverse },
        { 1, 3, 3, true, true, 2, 2, FrameFlags.Reverse | FrameFlags.TimelineEnd | FrameFlags.CompletedBefore },
        { 1, 4, 3, true, false, 4, 0, FrameFlags.None },
        { 1, 0, 1, false, true, 1, 0, FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.CompletedAfter },
        { 1, 1, 1, true, true, 0, 0, FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.CompletedBefore | FrameFlags.Reverse },
        { 65_536, uint.MaxValue - 1, uint.MaxValue, false, true, uint.MaxValue, uint.MaxValue - 1, FrameFlags.TimelineEnd | FrameFlags.CompletedAfter },
    };

    public static TheoryData<uint, uint, long, bool, uint, long, uint, long, FrameFlags> LoopCases => new()
    {
        { 0, 4, 0, false, 1, 0, 0, 0, FrameFlags.Looping | FrameFlags.TimelineStart },
        { 3, 4, 7, false, 0, 8, 3, 7, FrameFlags.Looping | FrameFlags.TimelineEnd },
        { 3, 4, long.MaxValue, false, 0, long.MinValue, 3, long.MaxValue, FrameFlags.Looping | FrameFlags.TimelineEnd },
        { 0, 4, 0, true, 3, -1, 3, -1, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineEnd },
        { 3, 4, -2, true, 2, -2, 2, -2, FrameFlags.Looping | FrameFlags.Reverse },
        { 1, 4, 9, true, 0, 9, 0, 9, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart },
        { 0, 4, long.MinValue, true, 3, long.MaxValue, 3, long.MaxValue, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineEnd },
        { 0, 1, 4, false, 0, 5, 0, 4, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.TimelineEnd },
        { 0, 1, 4, true, 0, 3, 0, 3, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.TimelineEnd },
        { uint.MaxValue - 1, uint.MaxValue, 11, false, 0, 12, uint.MaxValue - 1, 11, FrameFlags.Looping | FrameFlags.TimelineEnd },
    };

    [Theory]
    [MemberData(nameof(FiniteCases))]
    public void FiniteSelectionMatchesBoundaryOracle(
        uint asset,
        uint position,
        uint duration,
        bool reverse,
        bool selected,
        uint nextPosition,
        uint tick,
        FrameFlags flags)
    {
        var state = new TimelineState(asset, position, 17);

        var result = TimelineMovement.Select(
            in state,
            duration,
            false,
            reverse,
            out var next,
            out var actualTick,
            out var cycle,
            out var actualFlags);

        Assert.Equal(selected, result);
        Assert.Equal(asset, next.Asset);
        Assert.Equal(nextPosition, next.Position);
        Assert.Equal(selected ? 0 : 17, next.Cycle);
        Assert.Equal(tick, actualTick);
        Assert.Equal(0, cycle);
        Assert.Equal(flags, actualFlags);
    }

    [Theory]
    [MemberData(nameof(LoopCases))]
    public void LoopSelectionMatchesCycleCoordinateOracle(
        uint position,
        uint duration,
        long initialCycle,
        bool reverse,
        uint nextPosition,
        long nextCycle,
        uint tick,
        long frameCycle,
        FrameFlags flags)
    {
        var state = new TimelineState(65_536, position, initialCycle);

        Assert.True(TimelineMovement.Select(
            in state,
            duration,
            true,
            reverse,
            out var next,
            out var actualTick,
            out var actualCycle,
            out var actualFlags));

        Assert.Equal(65_536u, next.Asset);
        Assert.Equal(nextPosition, next.Position);
        Assert.Equal(nextCycle, next.Cycle);
        Assert.Equal(tick, actualTick);
        Assert.Equal(frameCycle, actualCycle);
        Assert.Equal(flags, actualFlags);
    }

    [Fact]
    public void EmptyAndInvalidLoopStatesAreInactive()
    {
        AssertInactive(new TimelineState(0, 3, 9), 4, true, false);
        AssertInactive(new TimelineState(1, 3, 9), 0, true, true);
        AssertInactive(new TimelineState(1, 4, 9), 4, true, false);
        AssertInactive(new TimelineState(1, uint.MaxValue, 9), 4, true, true);
    }

    [Fact]
    public void FiniteReplayClampsExtremeDeltasAndVisitsGaps()
    {
        Span<uint> ticks = stackalloc uint[8];
        Span<uint> gameTicks = stackalloc uint[8];
        var state = new TimelineState(1);
        var gameTick = uint.MaxValue;

        var unchanged = state;
        Assert.Equal(0, Replay(ref state, 5, false, 0, ref gameTick, ticks, gameTicks));
        AssertState(unchanged, state);
        Assert.Equal(uint.MaxValue, gameTick);

        var count = Replay(ref state, 5, false, int.MaxValue, ref gameTick, ticks, gameTicks);
        Assert.Equal(5, count);
        Assert.Equal(new uint[] { 0, 1, 2, 3, 4 }, ticks[..count].ToArray());
        Assert.Equal(new uint[] { uint.MaxValue, 0, 1, 2, 3 }, gameTicks[..count].ToArray());
        Assert.Equal(5u, state.Position);
        Assert.Equal(4u, gameTick);

        count = Replay(ref state, 5, false, int.MinValue, ref gameTick, ticks, gameTicks);
        Assert.Equal(5, count);
        Assert.Equal(new uint[] { 4, 3, 2, 1, 0 }, ticks[..count].ToArray());
        Assert.Equal(new uint[] { 3, 2, 1, 0, uint.MaxValue }, gameTicks[..count].ToArray());
        Assert.Equal(0u, state.Position);
        Assert.Equal(uint.MaxValue, gameTick);

        var empty = default(TimelineState);
        count = Replay(ref empty, uint.MaxValue, false, int.MinValue, ref gameTick, ticks, gameTicks);
        Assert.Equal(0, count);
        Assert.Equal(uint.MaxValue, gameTick);
    }

    [Fact]
    public void CatalogRouteDomainIncludesEmptyAndSixtyFiveThousandFiveHundredThirtySixAssets()
    {
        var empty = default(TimelineState);
        AssertInactive(empty, 1, false, false);

        for (uint asset = 1; asset <= 65_536; asset++)
        {
            var state = new TimelineState(asset);
            Assert.True(TimelineMovement.Select(in state, 1, false, false, out var next, out _, out _, out _));
            Assert.Equal(asset, next.Asset);
        }
    }

    [Fact]
    public void CompletionOfOneRowDoesNotStopLiveRows()
    {
        var states = new[] { new TimelineState(1), new TimelineState(2) };
        var durations = new uint[] { 1, 3 };
        var calls = new int[2];

        for (var pass = 0; pass < 3; pass++)
        {
            for (var row = 0; row < states.Length; row++)
            {
                if (!TimelineMovement.Select(in states[row], durations[row], false, false, out var next, out _, out _, out _))
                    continue;
                calls[row]++;
                states[row] = next;
            }
        }

        Assert.Equal(new[] { 1, 3 }, calls);
        Assert.Equal(1u, states[0].Position);
        Assert.Equal(3u, states[1].Position);
    }

    [Fact]
    public void FiniteClampingDoesNotClaimInverseMovement()
    {
        Span<uint> ticks = stackalloc uint[4];
        Span<uint> gameTicks = stackalloc uint[4];
        var state = new TimelineState(1, 1);
        var gameTick = 1u;

        Assert.Equal(2, Replay(ref state, 3, false, 10, ref gameTick, ticks, gameTicks));
        Assert.Equal(3, Replay(ref state, 3, false, -10, ref gameTick, ticks, gameTicks));
        Assert.Equal(0u, state.Position);
    }

    [Fact]
    public void LoopReplayCrossesMultipleCyclesAndImmediateReverseRestoresWrap()
    {
        Span<uint> ticks = stackalloc uint[8];
        Span<uint> gameTicks = stackalloc uint[8];
        var state = new TimelineState(1, 0, -1);
        var gameTick = 10u;

        Assert.Equal(5, Replay(ref state, 2, true, 5, ref gameTick, ticks, gameTicks));
        Assert.Equal(new uint[] { 0, 1, 0, 1, 0 }, ticks[..5].ToArray());
        Assert.Equal(1u, state.Position);
        Assert.Equal(1, state.Cycle);

        Assert.Equal(5, Replay(ref state, 2, true, -5, ref gameTick, ticks, gameTicks));
        Assert.Equal(new uint[] { 0, 1, 0, 1, 0 }, ticks[..5].ToArray());
        Assert.Equal(0u, state.Position);
        Assert.Equal(-1, state.Cycle);

        state = new TimelineState(1, 1, long.MaxValue);
        Assert.True(TimelineMovement.Select(in state, 2, true, false, out var wrapped, out var forwardTick, out var forwardCycle, out _));
        Assert.True(TimelineMovement.Select(in wrapped, 2, true, true, out var restored, out var reverseTick, out var reverseCycle, out _));
        Assert.Equal(1u, forwardTick);
        Assert.Equal(long.MaxValue, forwardCycle);
        Assert.Equal(forwardTick, reverseTick);
        Assert.Equal(forwardCycle, reverseCycle);
        AssertState(state, restored);
    }

    [Fact]
    public void WarmSelectionAllocatesNoManagedMemory()
    {
        var state = new TimelineState(1);
        uint receipt = 0;
        for (var index = 0; index < 32; index++)
        {
            Assert.True(TimelineMovement.Select(in state, 3, true, false, out var next, out var tick, out _, out var flags));
            state = next;
            receipt ^= tick | (uint)flags;
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
        {
            if (!TimelineMovement.Select(in state, 3, true, false, out var next, out var tick, out var cycle, out var flags))
                throw new InvalidOperationException();
            state = next;
            receipt ^= tick | (uint)flags | (uint)cycle;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(uint.MaxValue, receipt);
    }

    [Fact]
    public void SelectionAndCommitAreSeparatedAndIdempotent()
    {
        var committed = new TimelineState(73);

        Assert.True(TimelineMovement.Select(in committed, 2, false, false, out var pending, out var tick, out _, out _));
        Assert.Equal(0u, committed.Position);
        Assert.Equal(0u, tick);

        Assert.True(TimelineMovement.Select(in committed, 2, false, false, out var repeated, out var repeatedTick, out _, out _));
        AssertState(pending, repeated);
        Assert.Equal(tick, repeatedTick);

        committed = pending;
        committed = pending;
        Assert.Equal(1u, committed.Position);
    }

    [Fact]
    public void DeclarationAndHookFramesExposeTheFrozenShape()
    {
        JobTimeline.Define(default);
        Catalog.Define(default);

        var track = new JobTrack();
        var clip = new JobClip(19);
        var borrowed = new Frame<JobTrack, JobClip>(in track, in clip, 4, 3, -2, 1, FrameFlags.Reverse);
        Assert.Equal(19, borrowed.Clip.Value);

        var hook = new TimelineFrame(4, 3, -2, FrameFlags.TimelineEnd | FrameFlags.Reverse);
        Assert.Equal(4u, hook.GameTick);
        Assert.Equal(3u, hook.TimelineTick);
        Assert.Equal(-2, hook.Cycle);
        Assert.Equal(-1, hook.Direction);
        Assert.True(hook.Has(FrameFlags.TimelineEnd | FrameFlags.Reverse));
        var hookResult = 0;
        var hookInput = 7;
        Hook.Execute(in hook, in hookInput, ref hookResult);
        Assert.Equal(-7, hookResult);

        Assert.Equal(16, Unsafe.SizeOf<TimelineState>());
        Assert.Equal(24, Unsafe.SizeOf<TimelineFrame>());
        Assert.Equal(0, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Asset)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Position)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Cycle)).ToInt32());
        Assert.Equal(0, Marshal.OffsetOf<TimelineFrame>("<GameTick>k__BackingField").ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<TimelineFrame>("<TimelineTick>k__BackingField").ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<TimelineFrame>("<Cycle>k__BackingField").ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<TimelineFrame>("<Flags>k__BackingField").ToInt32());

        var frameType = typeof(Frame<JobTrack, JobClip>);
        Assert.True(frameType.IsByRefLike);
        Assert.Equal(typeof(IBlend<JobClip>), typeof(JobTrack).GetInterfaces().Single());
        Assert.DoesNotContain(typeof(ITrack<JobClip>), typeof(JobTrack).GetInterfaces());

        var asset = typeof(SchemaBuilder<Rows>).GetMethod(nameof(SchemaBuilder<Rows>.Asset))!;
        Assert.Equal(typeof(SchemaBuilder<Rows>), asset.ReturnType);
        Assert.Equal(typeof(TrackRef<JobTrack, Job>), typeof(TrackRef<JobTrack>).GetMethod(nameof(TrackRef<JobTrack>.Use))!.MakeGenericMethod(typeof(Job)).ReturnType);

        var stateType = typeof(TimelineState);
        Assert.True(stateType.IsDefined(typeof(IsReadOnlyAttribute), false));
        Assert.Equal(LayoutKind.Sequential, stateType.StructLayoutAttribute!.Value);
        Assert.All(stateType.GetFields(), static field => Assert.True(field.IsInitOnly));

        var hookType = typeof(TimelineFrame);
        Assert.True(hookType.IsDefined(typeof(IsReadOnlyAttribute), false));
        Assert.Equal(LayoutKind.Sequential, hookType.StructLayoutAttribute!.Value);
        var hookConstructor = Assert.Single(hookType.GetConstructors());
        Assert.Equal(
            new[] { typeof(uint), typeof(uint), typeof(long), typeof(FrameFlags) },
            hookConstructor.GetParameters().Select(static parameter => parameter.ParameterType));

        var parameters = typeof(TimelineMovement).GetMethod(nameof(TimelineMovement.Select))!.GetParameters();
        Assert.Equal(8, parameters.Length);
        Assert.True(parameters[0].IsIn);
        Assert.Equal(typeof(TimelineState).MakeByRefType(), parameters[0].ParameterType);
        Assert.Equal(
            new[]
            {
                typeof(TimelineState).MakeByRefType(),
                typeof(uint),
                typeof(bool),
                typeof(bool),
                typeof(TimelineState).MakeByRefType(),
                typeof(uint).MakeByRefType(),
                typeof(long).MakeByRefType(),
                typeof(FrameFlags).MakeByRefType(),
            },
            parameters.Select(static parameter => parameter.ParameterType));
        Assert.All(parameters[4..], static parameter => Assert.True(parameter.IsOut));
    }

    private static int Replay(
        ref TimelineState state,
        uint duration,
        bool looping,
        int delta,
        ref uint gameTick,
        Span<uint> ticks,
        Span<uint> gameTicks)
    {
        var remaining = (long)delta;
        var reverse = remaining < 0;
        var count = 0;
        while (remaining != 0 && TimelineMovement.Select(
            in state,
            duration,
            looping,
            reverse,
            out var next,
            out var tick,
            out var cycle,
            out var flags))
        {
            if (reverse)
                gameTick = unchecked(gameTick - 1u);
            var frame = new TimelineFrame(gameTick, tick, cycle, flags);
            ticks[count] = frame.TimelineTick;
            gameTicks[count] = frame.GameTick;
            count++;
            state = next;
            if (!reverse)
                gameTick = unchecked(gameTick + 1u);
            remaining += reverse ? 1 : -1;
        }

        return count;
    }

    private static void AssertInactive(TimelineState state, uint duration, bool looping, bool reverse)
    {
        Assert.False(TimelineMovement.Select(
            in state,
            duration,
            looping,
            reverse,
            out var next,
            out var tick,
            out var cycle,
            out var flags));
        AssertState(state, next);
        Assert.Equal(0u, tick);
        Assert.Equal(0, cycle);
        Assert.Equal(FrameFlags.None, flags);
    }

    private static void AssertState(in TimelineState expected, in TimelineState actual)
    {
        Assert.Equal(expected.Asset, actual.Asset);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Cycle, actual.Cycle);
    }
}
