using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public class TotalMovementTests
{
    public readonly record struct JobClip(int Value);

    public readonly struct JobTrack : IBlend<JobClip>
    {
        public void Blend(in JobClip first, in JobClip second, float factor, out JobClip result)
            => result = factor < 0.5f ? first : second;
    }

    private static byte[] FiniteFixture() => new DomainBaker()
        .Track<JobTrack, JobClip>(default)
        .Clip(0, 0u, 3u, new JobClip(1))
        .Bake();

    private static byte[] LoopingFixture() => new DomainBaker()
        .Track<JobTrack, JobClip>(default)
        .Clip(0, 0u, 2u, new JobClip(1))
        .Looping()
        .Bake();
    public static TheoryData<uint, ushort, ushort, bool, bool, ushort, ushort, FrameFlags> FiniteCases => new()
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
        { 65_536, 65_534, 65_535, false, true, 65_535, 65_534, FrameFlags.TimelineEnd | FrameFlags.CompletedAfter },
        { 65_536, 65_535, 65_535, true, true, 65_534, 65_534, FrameFlags.Reverse | FrameFlags.TimelineEnd | FrameFlags.CompletedBefore },
    };

    public static TheoryData<ushort, ushort, bool, ushort, ushort, FrameFlags> LoopCases => new()
    {
        { 0, 4, false, 1, 0, FrameFlags.Looping | FrameFlags.TimelineStart },
        { 3, 4, false, 0, 3, FrameFlags.Looping | FrameFlags.TimelineEnd },
        { 0, 4, true, 3, 3, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineEnd },
        { 3, 4, true, 2, 2, FrameFlags.Looping | FrameFlags.Reverse },
        { 1, 4, true, 0, 0, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart },
        { 0, 1, false, 0, 0, FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.TimelineEnd },
        { 0, 1, true, 0, 0, FrameFlags.Looping | FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.TimelineEnd },
        { 65_534, 65_535, false, 0, 65_534, FrameFlags.Looping | FrameFlags.TimelineEnd },
    };

    [Theory]
    [MemberData(nameof(FiniteCases))]
    public void FiniteSelectionMatchesBoundaryOracle(
        uint asset,
        ushort position,
        ushort duration,
        bool reverse,
        bool selected,
        ushort nextPosition,
        ushort tick,
        FrameFlags flags)
    {
        var state = new TimelineState(asset, position);

        var result = TimelineMovement.Select(
            in state,
            duration,
            false,
            reverse,
            out var next,
            out var actualTick,
            out var actualFlags);

        Assert.Equal(selected, result);
        Assert.Equal(asset, next.Asset);
        Assert.Equal(nextPosition, next.Position);
        Assert.Equal(tick, actualTick);
        Assert.Equal(flags, actualFlags);
    }

    [Theory]
    [MemberData(nameof(LoopCases))]
    public void LoopSelectionMatchesTheWrapOracle(
        ushort position,
        ushort duration,
        bool reverse,
        ushort nextPosition,
        ushort tick,
        FrameFlags flags)
    {
        var state = new TimelineState(65_536, position);

        Assert.True(TimelineMovement.Select(
            in state,
            duration,
            true,
            reverse,
            out var next,
            out var actualTick,
            out var actualFlags));

        Assert.Equal(65_536u, next.Asset);
        Assert.Equal(nextPosition, next.Position);
        Assert.Equal(tick, actualTick);
        Assert.Equal(flags, actualFlags);
        Assert.Equal(tick == duration - 1, flags.HasFlag(FrameFlags.TimelineEnd));
        Assert.Equal(reverse, flags.HasFlag(FrameFlags.Reverse));
    }

    [Fact]
    public void EmptyAndInvalidLoopStatesAreInactive()
    {
        AssertInactive(new TimelineState(0, 3), 4, true, false);
        AssertInactive(new TimelineState(1, 3), 0, true, true);
        AssertInactive(new TimelineState(1, 4), 4, true, false);
        AssertInactive(new TimelineState(1, ushort.MaxValue), 4, true, true);
    }

    [Fact]
    public void FiniteReplayClampsExtremeDeltasAndVisitsGaps()
    {
        Span<ushort> ticks = stackalloc ushort[8];
        var state = new TimelineState(1);

        var unchanged = state;
        Assert.Equal(0, Replay(ref state, 5, false, 0, ticks));
        AssertState(unchanged, state);

        var count = Replay(ref state, 5, false, int.MaxValue, ticks);
        Assert.Equal(5, count);
        Assert.Equal(new ushort[] { 0, 1, 2, 3, 4 }, ticks[..count].ToArray());
        Assert.Equal(5, (int)state.Position);

        count = Replay(ref state, 5, false, int.MinValue, ticks);
        Assert.Equal(5, count);
        Assert.Equal(new ushort[] { 4, 3, 2, 1, 0 }, ticks[..count].ToArray());
        Assert.Equal(0, (int)state.Position);

        var empty = default(TimelineState);
        count = Replay(ref empty, ushort.MaxValue, false, int.MinValue, ticks);
        Assert.Equal(0, count);
    }

    [Fact]
    public void RouteDomainIncludesEmptyAndSixtyFiveThousandFiveHundredThirtySixAssets()
    {
        var empty = default(TimelineState);
        AssertInactive(empty, 1, false, false);

        for (uint asset = 1; asset <= 65_536; asset++)
        {
            var state = new TimelineState(asset);
            Assert.True(TimelineMovement.Select(in state, 1, false, false, out var next, out _, out _));
            Assert.Equal(asset, next.Asset);
        }
    }

    [Fact]
    public void CompletionOfOneRowDoesNotStopLiveRows()
    {
        var states = new[] { new TimelineState(1), new TimelineState(2) };
        var durations = new ushort[] { 1, 3 };
        var calls = new int[2];

        for (var pass = 0; pass < 3; pass++)
        {
            for (var row = 0; row < states.Length; row++)
            {
                if (!TimelineMovement.Select(in states[row], durations[row], false, false, out var next, out _, out _))
                    continue;
                calls[row]++;
                states[row] = next;
            }
        }

        Assert.Equal(new[] { 1, 3 }, calls);
        Assert.Equal(1, (int)states[0].Position);
        Assert.Equal(3, (int)states[1].Position);
    }
    [Fact]
    public void FiniteClampingDoesNotClaimInverseMovement()
    {
        Span<ushort> ticks = stackalloc ushort[4];
        var state = new TimelineState(1, 1);

        Assert.Equal(2, Replay(ref state, 3, false, 10, ticks));
        Assert.Equal(3, Replay(ref state, 3, false, -10, ticks));
        Assert.Equal(0, (int)state.Position);
    }
    [Fact]
    public void LoopReplayCrossesMultipleWrapsAndImmediateReverseRestoresPosition()
    {
        Span<ushort> ticks = stackalloc ushort[8];
        var state = new TimelineState(1, 0);

        Assert.Equal(5, Replay(ref state, 2, true, 5, ticks));
        Assert.Equal(new ushort[] { 0, 1, 0, 1, 0 }, ticks[..5].ToArray());
        Assert.Equal(1, (int)state.Position);

        Assert.Equal(5, Replay(ref state, 2, true, -5, ticks));
        Assert.Equal(new ushort[] { 0, 1, 0, 1, 0 }, ticks[..5].ToArray());
        Assert.Equal(0, (int)state.Position);

        state = new TimelineState(1, 1);
        Assert.True(TimelineMovement.Select(in state, 2, true, false, out var wrapped, out var forwardTick, out var forwardFlags));
        Assert.True(TimelineMovement.Select(in wrapped, 2, true, true, out var restored, out var reverseTick, out var reverseFlags));
        Assert.Equal(1, (int)forwardTick);
        Assert.Equal(1, (int)reverseTick);
        Assert.True(forwardFlags.HasFlag(FrameFlags.TimelineEnd));
        Assert.True(reverseFlags.HasFlag(FrameFlags.TimelineEnd));
        Assert.Equal(state, restored);
    }
    [Fact]
    public void WarmSelectionAllocatesNoManagedMemory()
    {
        var state = new TimelineState(1);
        uint receipt = 0;
        for (var index = 0; index < 32; index++)
        {
            Assert.True(TimelineMovement.Select(in state, 3, true, false, out var next, out var tick, out var flags));
            state = next;
            receipt ^= tick | (uint)flags;
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
        {
            if (!TimelineMovement.Select(in state, 3, true, false, out var next, out var tick, out var flags))
                throw new InvalidOperationException();
            state = next;
            receipt ^= tick | (uint)flags;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(uint.MaxValue, receipt);
    }

    [Fact]
    public void SelectionAndCommitAreSeparatedAndIdempotent()
    {
        var committed = new TimelineState(73);

        Assert.True(TimelineMovement.Select(in committed, 2, false, false, out var pending, out var tick, out _));
        Assert.Equal(0, (int)committed.Position);
        Assert.Equal(0, (int)tick);

        Assert.True(TimelineMovement.Select(in committed, 2, false, false, out var repeated, out var repeatedTick, out _));
        AssertState(pending, repeated);
        Assert.Equal(tick, repeatedTick);

        committed = pending;
        Assert.Equal(1, (int)committed.Position);
    }

    [Fact]
    public void DataAuthoredFramesExposeTheFrozenShape()
    {
        using var asset = TimelineAsset.LoadAsset(FiniteFixture());
        var component = new TimelineComponent(asset.Reference);

        var queried = Timeline.Query<JobTrack, JobClip>(in component);
        Assert.True(queried.MoveNext());
        Assert.Equal((ushort)0, queried.Current.TimelineTick);
        Assert.Equal((ushort)0, queried.Current.TrackIndex);
        Assert.Equal((ushort)0, component.Position);

        var track = new JobTrack();
        var clip = new JobClip(19);
        var borrowed = new Frame<JobTrack, JobClip>(in track, in clip, 3, 4, 2, 1, FrameFlags.Reverse);
        Assert.Equal(19, borrowed.Clip.Value);

        Assert.Equal(8, Unsafe.SizeOf<TimelineState>());
        Assert.Equal(0, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Asset)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Position)).ToInt32());

        var frameType = typeof(Frame<JobTrack, JobClip>);
        Assert.True(frameType.IsByRefLike);
        Assert.Equal(typeof(IBlend<JobClip>), typeof(JobTrack).GetInterfaces().Single());

        var stateType = typeof(TimelineState);
        Assert.True(stateType.IsDefined(typeof(IsReadOnlyAttribute), false));
        Assert.Equal(LayoutKind.Sequential, stateType.StructLayoutAttribute!.Value);
        Assert.All(stateType.GetFields(), static field => Assert.True(field.IsInitOnly));

        var parameters = typeof(TimelineMovement).GetMethod(nameof(TimelineMovement.Select))!.GetParameters();
        Assert.Equal(7, parameters.Length);
        Assert.True(parameters[0].IsIn);
        Assert.Equal(typeof(TimelineState).MakeByRefType(), parameters[0].ParameterType);
        Assert.Equal(
            new[]
            {
                typeof(TimelineState).MakeByRefType(),
                typeof(ushort),
                typeof(bool),
                typeof(bool),
                typeof(TimelineState).MakeByRefType(),
                typeof(ushort).MakeByRefType(),
                typeof(FrameFlags).MakeByRefType(),
            },
            parameters.Select(static parameter => parameter.ParameterType));
        Assert.All(parameters[4..], static parameter => Assert.True(parameter.IsOut));
    }

    [Fact]
    public void WrapTicksCarryTheReconstructionFlags()
    {
        var looping = new TimelineState(1, 3);
        Assert.True(TimelineMovement.Select(in looping, 4, true, false, out _, out var forwardTick, out var forwardFlags));
        Assert.Equal((ushort)3, forwardTick);
        Assert.True(forwardFlags.HasFlag(FrameFlags.TimelineEnd));
        Assert.False(forwardFlags.HasFlag(FrameFlags.Reverse));

        var loopingStart = new TimelineState(1, 0);
        Assert.True(TimelineMovement.Select(in loopingStart, 4, true, true, out _, out var reverseTick, out var reverseFlags));
        Assert.Equal((ushort)3, reverseTick);
        Assert.True(reverseFlags.HasFlag(FrameFlags.TimelineEnd));
        Assert.True(reverseFlags.HasFlag(FrameFlags.Reverse));

        var finiteEnd = new TimelineState(1, 2);
        Assert.True(TimelineMovement.Select(in finiteEnd, 3, false, false, out _, out var completedTick, out var completedFlags));
        Assert.True(completedFlags.HasFlag(FrameFlags.CompletedAfter));

        var finitePastEnd = new TimelineState(1, 3);
        Assert.True(TimelineMovement.Select(in finitePastEnd, 3, false, true, out _, out var unwoundTick, out var unwoundFlags));
        Assert.True(unwoundFlags.HasFlag(FrameFlags.CompletedBefore));
        Assert.Equal((ushort)2, unwoundTick);
    }

    private static int Replay(
        ref TimelineState state,
        ushort duration,
        bool looping,
        int delta,
        Span<ushort> ticks)
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
            out _))
        {
            ticks[count] = tick;
            count++;
            state = next;
            remaining += reverse ? 1 : -1;
        }

        return count;
    }

    private static void AssertInactive(TimelineState state, ushort duration, bool looping, bool reverse)
    {
        Assert.False(TimelineMovement.Select(
            in state,
            duration,
            looping,
            reverse,
            out var next,
            out var tick,
            out var flags));
        AssertState(state, next);
        Assert.Equal(0, (int)tick);
        Assert.Equal(FrameFlags.None, flags);
    }

    private static void AssertState(in TimelineState expected, in TimelineState actual)
    {
        Assert.Equal(expected.Asset, actual.Asset);
        Assert.Equal(expected.Position, actual.Position);
    }
}
