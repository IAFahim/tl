using Xunit;

namespace Tl.Compiler.Tests;

public sealed class ValidatedTimelinePlanTests
{
    private static readonly OperationId First = new("first");
    private static readonly OperationId Second = new("second");

    [Fact]
    public void ValidationUsesTheAuthoredRuleOrderAndExactDiagnostics()
    {
        AssertInvalid(
            new(" ", 1, false, [new TrackPlan(0, 0, default)], []),
            "Timeline identity cannot be empty.");

        var tracks = Enumerable.Range(0, ushort.MaxValue + 1)
            .Select(static index => new TrackPlan((ushort)index, (uint)index, First))
            .Append(new TrackPlan(0, 0, default));
        AssertInvalid(
            new("capacity", 1, false, tracks, []),
            "A timeline may contain at most 65,536 tracks.");

        AssertInvalid(
            new("duplicate", 1, false, [new TrackPlan(4, 0, First), new TrackPlan(4, 0, default)], []),
            "Track index 4 is duplicated.");
        AssertInvalid(
            new("operation", 1, false, [new TrackPlan(4, 0, default)], []),
            "Track index 4 has no operation.");
        AssertInvalid(
            new("missing", 1, false, [new TrackPlan(4, 0, First)], [new ClipPlan(3, 0, 8, 2)]),
            "Clip track index 3 does not exist.");
        AssertInvalid(
            new("window", 1, false, [new TrackPlan(4, 0, First)], [new ClipPlan(4, 0, 8, 2)]),
            "Clip [8, 2) is empty or reversed.");
        AssertInvalid(
            new(
                "overlap",
                1,
                false,
                [new TrackPlan(4, 0, First)],
                [new ClipPlan(4, 1, 0, 5), new ClipPlan(4, 2, 1, 4), new ClipPlan(4, 3, 2, 3)]),
            "Track index 4 has more than two overlapping clips.");
    }

    [Fact]
    public void RegionSchedulePreservesTrackOrderClipTiesAndHalfOpenWindows()
    {
        var plan = new TimelinePlan(
            "schedule",
            7,
            true,
            [new TrackPlan(9, 900, First), new TrackPlan(2, 200, Second)],
            [
                new ClipPlan(2, 210, 0, 2),
                new ClipPlan(9, 901, 2, 5),
                new ClipPlan(9, 902, 2, 4),
                new ClipPlan(2, 211, 1, 3),
                new ClipPlan(9, 900, 0, 2)
            ]);

        var validated = plan.Validate();

        Assert.Equal(TimelinePlan.CurrentFormatVersion, plan.FormatVersion);
        Assert.Equal(plan.FormatVersion, validated.FormatVersion);
        Assert.Equal("schedule", validated.Identity);
        Assert.Equal((ushort)7, validated.RuntimeId);
        Assert.True(validated.Loops);
        Assert.Equal(5u, validated.Duration);
        Assert.Equal([9, 2], validated.Tracks.Select(static track => (int)track.Index));
        Assert.Equal(
            [
                "[0,1):9=900;2=210",
                "[1,2):9=900;2=210,211@1+1",
                "[2,3):9=901,902@2+2;2=211",
                "[3,4):9=901,902@2+2",
                "[4,5):9=901"
            ],
            validated.Regions.Select(Describe));
    }

    [Fact]
    public void UnsupportedFormatIsRejectedBeforeSemanticValidation()
    {
        var plan = new TimelinePlan(" ", 1, false, [], [], formatVersion: 2);

        var exception = Assert.Throws<NotSupportedException>(plan.Validate);

        Assert.Equal("Timeline plan format 2 is not supported. Expected 1.", exception.Message);
    }

    [Fact]
    public void TouchingWindowsRemainValidAndEmptyTimelinesHaveNoRegions()
    {
        var touching = new TimelinePlan(
            "touching",
            1,
            false,
            [new TrackPlan(0, 0, First)],
            [new ClipPlan(0, 1, 0, 2), new ClipPlan(0, 2, 1, 3), new ClipPlan(0, 3, 2, 4)]).Validate();
        var empty = new TimelinePlan("empty", 2, false, [], []).Validate();

        var boundary = touching.Regions.Single(static region => region.Start == 2).Works[0];
        Assert.Equal(2u, boundary.First.Payload);
        Assert.Equal(3u, boundary.Second?.Payload);
        Assert.Empty(empty.Regions);
        Assert.Equal(0u, empty.Duration);
    }

    private static string Describe(RegionPlan region)
        => $"[{region.Start},{region.End}):" + string.Join(
            ";",
            region.Works.Select(static work => work.Second is { } second
                ? $"{work.Track.Index}={work.First.Payload},{second.Payload}@{work.FactorStart}+{work.FactorLength}"
                : $"{work.Track.Index}={work.First.Payload}"));

    private static void AssertInvalid(TimelinePlan plan, string diagnostic)
    {
        var exception = Assert.Throws<ArgumentException>(plan.Validate);
        Assert.Equal(diagnostic + " (Parameter 'plan')", exception.Message);
        Assert.Equal("plan", exception.ParamName);
    }
}
