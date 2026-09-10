using Tl.Compiler;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class JobTimelinePlanAdapterTests
{
    [Fact]
    public void CreatesValidatedPlanWithIndexAlignedBindings()
    {
        var state = new TimelineSlot("state", "global::Game.State", SlotMode.Input);
        var hook = new JobDefinition("global::Game.Hook", [state]);
        var firstJob = new JobDefinition("global::Game.FirstJob", [state with { Mode = SlotMode.Reference }]);
        var secondJob = new JobDefinition("global::Game.SecondJob", [new("pose", "global::Game.Pose", SlotMode.Reference)]);
        var timeline = new JobTimeline(
            "Asset",
            "Game",
            true,
            [],
            [
                new(0, "global::Game.Track", "global::Game.Clip", "new global::Game.Track(7)", firstJob),
                new(1, "global::Game.Track", "global::Game.Clip", "new global::Game.Track(7)", secondJob),
            ],
            [
                new(0, "global::Game.Clip", "new global::Game.Clip(11)", 0, 4),
                new(1, "global::Game.Clip", "new global::Game.Clip(11)", 0, 4),
            ],
            [hook],
            [hook]);

        var bound = JobTimelinePlanAdapter.Create(timeline);
        var repeated = JobTimelinePlanAdapter.Create(timeline);

        Assert.Equal("timeline:0", bound.Plan.Identity);
        Assert.True(bound.Plan.Loops);
        Assert.Equal(4u, bound.Plan.Duration);
        Assert.Equal(["operation:0", "operation:1", "operation:2"], bound.Plan.Operations.Select(static operation => operation.Id.Value).ToArray());
        Assert.Equal([hook, firstJob, secondJob], bound.OperationBindings.ToArray());
        Assert.Equal(
            [
                new CSharpPayloadBinding("global::Game.Track", "new global::Game.Track(7)"),
                new CSharpPayloadBinding("global::Game.Track", "new global::Game.Track(7)"),
                new CSharpPayloadBinding("global::Game.Clip", "new global::Game.Clip(11)"),
                new CSharpPayloadBinding("global::Game.Clip", "new global::Game.Clip(11)"),
            ],
            bound.PayloadBindings.ToArray());
        Assert.Equal([0u, 0u, 1u, 1u], bound.Plan.PayloadStorageIndices.ToArray());
        Assert.Equal([new OrderedHookPlan(new("operation:0"), HookPhase.Before), new(new("operation:0"), HookPhase.After)], bound.Plan.Hooks.ToArray());
        Assert.Equal(4, Assert.Single(bound.Plan.Regions).OccurrenceCount);
        Assert.Collection(
            bound.Plan.Occurrences,
            occurrence => Assert.Equal(OrderedOccurrenceFlags.BeforeHook, occurrence.Flags),
            occurrence => Assert.Equal((1, (byte)0, 0u, 2u), (occurrence.OperationIndex, occurrence.TrackIndex, occurrence.TrackPayloadIndex, occurrence.FirstPayloadIndex)),
            occurrence => Assert.Equal((2, (byte)1, 1u, 3u), (occurrence.OperationIndex, occurrence.TrackIndex, occurrence.TrackPayloadIndex, occurrence.FirstPayloadIndex)),
            occurrence => Assert.Equal(OrderedOccurrenceFlags.AfterHook, occurrence.Flags));
        Assert.Equal(SlotAccess.ReadWrite, Assert.Single(bound.Plan.Slots, static slot => slot.Role == "state").Access);
        Assert.All(bound.Plan.Operations, static operation => Assert.StartsWith("operation:", operation.Id.Value));
        Assert.All(bound.Plan.Operations.SelectMany(static operation => operation.Slots), static slot => Assert.StartsWith("type:", slot.Type.Value));
        Assert.All(bound.Plan.Payloads, static payload => Assert.StartsWith("type:", payload.Type.Value));
        for (var index = 0; index < bound.Plan.Payloads.Length; index++)
            Assert.Equal(bound.Plan.Payloads[index].Bytes.ToArray(), repeated.Plan.Payloads[index].Bytes.ToArray());
        Assert.Equal(bound.Plan.Regions.ToArray(), repeated.Plan.Regions.ToArray());
        Assert.Equal(bound.Plan.Occurrences.ToArray(), repeated.Plan.Occurrences.ToArray());
    }

    [Fact]
    public void KeepsTypeAndExpressionDistinctInPayloadEquality()
    {
        var job = new JobDefinition("global::Game.Job", []);
        var timeline = new JobTimeline(
            "Asset",
            "Game",
            false,
            [],
            [
                new(0, "global::Game.First", "global::Game.Clip", "default", job),
                new(1, "global::Game.Second", "global::Game.Clip", "default", job),
                new(2, "global::Game.First", "global::Game.Clip", "new global::Game.First()", job),
            ],
            [
                new(0, "global::Game.Clip", "default", 0, 1),
                new(1, "global::Game.Clip", "default", 0, 1),
                new(2, "global::Game.Clip", "default", 0, 1),
            ],
            [],
            []);

        var plan = JobTimelinePlanAdapter.Create(timeline).Plan;

        Assert.Equal([0u, 1u, 2u, 3u, 3u, 3u], plan.PayloadStorageIndices.ToArray());
        Assert.Equal(4, plan.UniquePayloads.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void RejectsTrackIndexBeforeNarrowing(int index)
    {
        var timeline = new JobTimeline(
            "Asset",
            "Game",
            false,
            [],
            [new(index, "global::Game.Track", "global::Game.Clip", "default", new("global::Game.Job", []))],
            [],
            [],
            []);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => JobTimelinePlanAdapter.Create(timeline));

        Assert.Contains(index.ToString(System.Globalization.CultureInfo.InvariantCulture), error.Message);
    }
}
