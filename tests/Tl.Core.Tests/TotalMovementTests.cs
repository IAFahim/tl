using System.Reflection;
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
        { 65_536, uint.MaxValue - 1, uint.MaxValue, false, true, uint.MaxValue, uint.MaxValue - 1, FrameFlags.TimelineEnd | FrameFlags.CompletedAfter },
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

        Assert.Equal(16, Unsafe.SizeOf<TimelineState>());
        Assert.Equal(24, Unsafe.SizeOf<TimelineFrame>());
        Assert.Equal(0, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Asset)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Position)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<TimelineState>(nameof(TimelineState.Cycle)).ToInt32());

        var frameType = typeof(Frame<JobTrack, JobClip>);
        Assert.True(frameType.IsByRefLike);
        Assert.Equal(typeof(IBlend<JobClip>), typeof(JobTrack).GetInterfaces().Single());
        Assert.DoesNotContain(typeof(ITrack<JobClip>), typeof(JobTrack).GetInterfaces());

        var asset = typeof(SchemaBuilder<Rows>).GetMethod(nameof(SchemaBuilder<Rows>.Asset))!;
        Assert.Equal(typeof(SchemaBuilder<Rows>), asset.ReturnType);
        Assert.Equal(typeof(TrackRef<JobTrack, Job>), typeof(TrackRef<JobTrack>).GetMethod(nameof(TrackRef<JobTrack>.Use))!.MakeGenericMethod(typeof(Job)).ReturnType);
    }

    private static void AssertState(in TimelineState expected, in TimelineState actual)
    {
        Assert.Equal(expected.Asset, actual.Asset);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Cycle, actual.Cycle);
    }
}
