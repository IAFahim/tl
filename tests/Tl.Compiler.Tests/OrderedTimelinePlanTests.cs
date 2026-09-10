using Xunit;

namespace Tl.Compiler.Tests;

public sealed class OrderedTimelinePlanTests
{
    private static readonly OperationId Before = new("before");
    private static readonly OperationId A = new("a");
    private static readonly OperationId B = new("b");
    private static readonly OperationId After = new("after");
    private static readonly TypeId Data = new("data");

    [Fact]
    public void SparseRegionsPreserveHooksAuthoredOrderBlendsGapsAndReverseOrder()
    {
        var plan = new OrderedTimelinePlan(
            "ordered",
            true,
            [Operation(Before), Operation(A), Operation(B), Operation(After)],
            [Payload(1), Payload(2), Payload(3), Payload(10), Payload(11), Payload(12), Payload(13)],
            [new(0, new(1), A), new(1, new(2), B), new(2, new(3), A)],
            [new(0, new(10), 2, 4), new(1, new(11), 2, 4), new(2, new(12), 2, 4), new(2, new(13), 3, 5)],
            [new(Before, HookPhase.Before), new(Before, HookPhase.Before), new(After, HookPhase.After)]).Validate();

        Assert.Equal(5u, plan.Duration);
        Assert.Equal([(0u, 2u), (2u, 3u), (3u, 4u), (4u, 5u)], plan.Regions.Select(static region => (region.Start, region.End)));
        Assert.Equal(["before", "before", "after"], Describe(plan, plan.Regions[0], false));
        Assert.Equal(["before", "before", "a", "b", "a", "after"], Describe(plan, plan.Regions[2], false));
        Assert.Equal(["after", "a", "b", "a", "before", "before"], Describe(plan, plan.Regions[2], true));

        var blend = Slice(plan, plan.Regions[2]).Single(static occurrence => occurrence.TrackIndex == 2);
        Assert.Equal(OrderedOccurrenceFlags.Blend, blend.Flags);
        Assert.Equal(2u, blend.TrackPayloadIndex);
        Assert.Equal(5u, blend.FirstPayloadIndex);
        Assert.Equal(6u, blend.SecondPayloadIndex);
        Assert.Equal(3u, blend.FactorStart);
        Assert.Equal(1u, blend.FactorLength);
        Assert.Equal(4, plan.Regions.Length);
        Assert.Equal("O(entities * operation kinds * stages)", plan.WorstCaseTraversal);
    }

    [Fact]
    public void OpposingSchedulesAndEveryByteTrackIndexMatchTheIndependentOracle()
    {
        var payloads = new[] { Payload(1), Payload(2) };
        var operations = new[] { Operation(A), Operation(B) };
        var ab = new OrderedTimelinePlan(
            "ab",
            false,
            operations,
            payloads,
            [new(0, new(1), A), new(1, new(1), B)],
            [new(0, new(2), 0, 1), new(1, new(2), 0, 1)],
            []).Validate();
        var ba = new OrderedTimelinePlan(
            "ba",
            false,
            operations,
            payloads,
            [new(0, new(1), B), new(1, new(1), A)],
            [new(0, new(2), 0, 1), new(1, new(2), 0, 1)],
            []).Validate();
        var tracks = Enumerable.Range(0, 256)
            .Select(index => new OrderedTrackPlan((byte)index, new(1), (index & 1) == 0 ? A : B))
            .ToArray();
        var clips = Enumerable.Range(0, 256)
            .Select(index => new OrderedClipPlan((byte)index, new(2), 0, 1))
            .ToArray();
        var wide = new OrderedTimelinePlan("wide", false, operations, payloads, tracks, clips, []).Validate();

        Assert.Equal(["a", "b"], Describe(ab, ab.Regions[0], false));
        Assert.Equal(["b", "a"], Describe(ba, ba.Regions[0], false));
        Assert.Equal(["b", "a"], Describe(ab, ab.Regions[0], true));
        Assert.Equal(["a", "b"], Describe(ba, ba.Regions[0], true));
        Assert.Equal((ushort)256, wide.Regions[0].OccurrenceCount);
        Assert.Equal(Enumerable.Range(0, 256).Select(static index => (byte)index), Slice(wide, wide.Regions[0]).Select(static occurrence => occurrence.TrackIndex));
        Assert.Equal(Enumerable.Range(0, 256).Reverse().Select(static index => (byte)index), Slice(wide, wide.Regions[0], true).Select(static occurrence => occurrence.TrackIndex));
    }

    [Fact]
    public void UintDurationUsesBoundaryRegionsAndInvalidInputHasDeterministicDiagnostics()
    {
        var maximum = new OrderedTimelinePlan(
            "maximum",
            false,
            [Operation(A)],
            [Payload(1), Payload(2)],
            [new(0, new(1), A)],
            [new(0, new(2), uint.MaxValue - 1, uint.MaxValue)],
            []).Validate();

        Assert.Equal(uint.MaxValue, maximum.Duration);
        Assert.Equal(2, maximum.Regions.Length);
        Assert.Equal((0u, uint.MaxValue - 1, (ushort)0), (maximum.Regions[0].Start, maximum.Regions[0].End, maximum.Regions[0].OccurrenceCount));
        AssertInvalid(
            new("capacity", false, [Operation(A)], [Payload(1)], Enumerable.Range(0, 257).Select(index => new OrderedTrackPlan((byte)index, new(1), A)), [], []),
            "An ordered timeline may contain at most 256 authored tracks.");
        AssertInvalid(
            new("mapping", false, [Operation(A)], [Payload(1)], [new(0, new(2), A)], [], []),
            "Track index 0 maps unknown payload 2.");
        AssertInvalid(
            new("window", false, [Operation(A)], [Payload(1)], [new(0, new(1), A)], [new(0, new(1), 8, 2)], []),
            "Clip [8, 2) is empty or reversed.");
    }

    private static OrderedOperationPlan Operation(OperationId id) => new(id, []);

    private static PayloadPlan Payload(uint id) => new(new(id), Data, [unchecked((byte)id), 0, 0, 0]);

    private static IEnumerable<OrderedOccurrencePlan> Slice(
        ValidatedOrderedTimelinePlan plan,
        OrderedRegionPlan region,
        bool reverse = false)
    {
        var slice = plan.Occurrences.Skip((int)region.OccurrenceOffset).Take(region.OccurrenceCount);
        return reverse ? slice.Reverse() : slice;
    }

    private static string[] Describe(ValidatedOrderedTimelinePlan plan, OrderedRegionPlan region, bool reverse)
        => Slice(plan, region, reverse).Select(occurrence => plan.Operations[occurrence.OperationIndex].Id.Value).ToArray();

    private static void AssertInvalid(OrderedTimelinePlan plan, string diagnostic)
    {
        var exception = Assert.Throws<ArgumentException>(plan.Validate);
        Assert.Equal(diagnostic + " (Parameter 'plan')", exception.Message);
        Assert.Equal("plan", exception.ParamName);
    }
}
