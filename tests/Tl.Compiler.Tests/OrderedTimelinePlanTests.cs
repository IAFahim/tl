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
    public void ConstructorsRejectMissingSequences()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderedOperationPlan(A, null!));
        Assert.Throws<ArgumentNullException>(() => new PayloadPlan(new(1), Data, null!));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan(null!, false, [], [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan("plan", false, null!, [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan("plan", false, [], null!, [], [], []));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan("plan", false, [], [], null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan("plan", false, [], [], [], null!, []));
        Assert.Throws<ArgumentNullException>(() => new OrderedTimelinePlan("plan", false, [], [], [], [], null!));
    }

    [Fact]
    public void ValidationRejectsEveryInvalidIdentityAndReferenceBoundary()
    {
        AssertInvalid(
            new("plan", false, [], [], [], [], [], formatVersion: 2),
            "Ordered timeline plan format 2 is not supported. Expected 1.");
        AssertInvalid(
            new(" ", false, [], [], [], [], []),
            "Ordered timeline identity cannot be empty.");
        AssertInvalid(
            new("operations", false, Enumerable.Repeat(Operation(A), ushort.MaxValue + 2), [], [], [], []),
            "An ordered timeline may contain at most 65,536 operations.");
        AssertInvalid(
            new("null-operation", false, [null!], [], [], [], []),
            "Operation at index 0 is null.");
        AssertInvalid(
            new("empty-operation", false, [Operation(default)], [], [], [], []),
            "Operation at index 0 has no identity.");
        AssertInvalid(
            new("duplicate-operation", false, [Operation(A), Operation(A)], [], [], [], []),
            "Operation 'a' is duplicated.");
        AssertInvalid(
            new("null-payload", false, [], [null!], [], [], []),
            "Payload at index 0 is null.");
        AssertInvalid(
            Empty(new OrderedOperationPlan(A, [new(" ", Data, SlotAccess.Read)])),
            "Operation 'a' has an empty slot role.");
        AssertInvalid(
            Empty(new OrderedOperationPlan(A, [new("value", new(" "), SlotAccess.Read)])),
            "Operation 'a' slot 'value' has no type identity.");
        AssertInvalid(
            new("duplicate-track", false, [Operation(A)], [Payload(1)], [new(0, new(1), A), new(0, new(1), A)], [], []),
            "Authored track index 0 is duplicated.");
        AssertInvalid(
            new("missing-track", false, [Operation(A)], [Payload(1)], [], [new(0, new(1), 0, 1)], []),
            "Clip track index 0 does not exist.");
        AssertInvalid(
            new("hook-phase", false, [Operation(A)], [], [], [], [new(A, (HookPhase)0)]),
            "Hook operation 'a' has invalid phase 0.");
    }

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

    [Fact]
    public void ExactDedupPreservesPayloadIdentityOrderAndFloatingPointBits()
    {
        var payloads = new[]
        {
            Payload(1, 0, 0, 0, 0),
            Payload(2, 0, 0, 0, 0),
            Payload(3, 0, 0, 0, 128),
            Payload(4, 0, 0, 192, 127),
            Payload(5, 1, 0, 192, 127),
            Payload(6, 0, 0, 192, 127),
        };
        var source = new OrderedTimelinePlan(
            "dedup",
            false,
            [Operation(A), Operation(B)],
            payloads,
            [new(0, new(1), A), new(1, new(1), B)],
            [new(0, new(2), 0, 5), new(1, new(4), 1, 2), new(1, new(4), 3, 4)],
            []);

        var first = source.Validate();
        var second = source.Validate();

        Assert.Equal([1u, 2u, 3u, 4u, 5u, 6u], first.Payloads.Select(static payload => payload.Id.Value));
        Assert.Equal("0,0,1,2,3,2", string.Join(",", first.PayloadStorageIndices));
        Assert.Equal(4, first.UniquePayloads.Length);
        Assert.Equal(16ul, first.PayloadBytes);
        Assert.Equal(5, first.Regions.Length);
        Assert.Equal(2, first.UniqueScheduleCount);
        Assert.Equal(3, first.Occurrences.Length);
        Assert.Equal([0u, 1u, 0u, 1u, 0u], first.Regions.Select(static region => region.OccurrenceOffset));
        Assert.Equal(152ul, first.ScheduleBytes);
        Assert.True(first.Regions.SequenceEqual(second.Regions));
        Assert.True(first.Occurrences.SequenceEqual(second.Occurrences));
        Assert.True(first.PayloadStorageIndices.SequenceEqual(second.PayloadStorageIndices));
        Assert.Equal(3u, first.Occurrences[2].FirstPayloadIndex);
        Assert.NotEqual(first.PayloadStorageIndices[2], first.PayloadStorageIndices[0]);
        Assert.NotEqual(first.PayloadStorageIndices[4], first.PayloadStorageIndices[3]);
    }

    [Fact]
    public void SlotEffectsJoinByRoleAndTypeAndRejectAmbiguousMappings()
    {
        var component = new TypeId("component");
        var pose = new TypeId("pose");
        var plan = new OrderedTimelinePlan(
            "slots",
            false,
            [
                new(A, [new("health", component, SlotAccess.Write), new("bodyPose", pose, SlotAccess.Read)]),
                new(B, [new("health", component, SlotAccess.Read), new("aimPose", pose, SlotAccess.Write)])
            ],
            [],
            [],
            [],
            []).Validate();

        Assert.Equal(
            [("health", SlotAccess.ReadWrite), ("bodyPose", SlotAccess.Read), ("aimPose", SlotAccess.Write)],
            plan.Slots.Select(static slot => (slot.Role, slot.Access)));
        AssertInvalid(
            Empty(new OrderedOperationPlan(A, [new("health", component, SlotAccess.Read), new("health", component, SlotAccess.Write)])),
            "Operation 'a' maps slot role 'health' more than once.");
        AssertInvalid(
            new("conflict", false,
                [new(A, [new("health", component, SlotAccess.Read)]), new(B, [new("health", pose, SlotAccess.Read)])], [], [], [], []),
            "Slot role 'health' maps to both 'component' and 'pose'.");
        AssertInvalid(
            Empty(new OrderedOperationPlan(A, [new("health", component, (SlotAccess)4)])),
            "Operation 'a' slot 'health' has invalid access 4.");
    }

    [Fact]
    public void MappingsConstantEncodingsOverlapAndOccurrenceCapacityFailBeforePlayback()
    {
        AssertInvalid(
            new("operation", false, [Operation(A)], [Payload(1)], [new(0, new(1), B)], [], []),
            "Track index 0 maps unknown operation 'b'.");
        AssertInvalid(
            new("clip-payload", false, [Operation(A)], [Payload(1)], [new(0, new(1), A)], [new(0, new(2), 0, 1)], []),
            "Clip on track index 0 maps unknown payload 2.");
        AssertInvalid(
            new("hook", false, [Operation(A)], [], [], [], [new(B, HookPhase.Before)]),
            "Before hook maps unknown operation 'b'.");
        AssertInvalid(
            new("encoding", false, [Operation(A)], [new(new(1), Data, [])], [], [], []),
            "Payload 1 has an empty constant encoding.");
        AssertInvalid(
            new("type", false, [Operation(A)], [new(new(1), new(" "), [1])], [], [], []),
            "Payload 1 has no type identity.");
        AssertInvalid(
            new("payload", false, [Operation(A)], [Payload(1), Payload(1)], [], [], []),
            "Payload identity 1 is duplicated.");
        AssertInvalid(
            new(
                "overlap",
                false,
                [Operation(A)],
                [Payload(1), Payload(2), Payload(3), Payload(4)],
                [new(0, new(1), A)],
                [new(0, new(2), 0, 5), new(0, new(3), 1, 4), new(0, new(4), 2, 3)],
                []),
            "Authored track index 0 has more than two overlapping clips.");
        AssertInvalid(
            new(
                "occurrences",
                false,
                [Operation(A)],
                [Payload(1), Payload(2)],
                [new(0, new(1), A)],
                [new(0, new(2), 0, 1)],
                Enumerable.Repeat(new OrderedHookPlan(A, HookPhase.Before), ushort.MaxValue)),
            "Region [0, 1) has more than 65,535 operation occurrences.");
    }

    [Fact]
    public void NeutralPlanAssemblyDoesNotReferenceRoslyn()
        => Assert.DoesNotContain(
            typeof(OrderedTimelinePlan).Assembly.GetReferencedAssemblies(),
            static assembly => assembly.Name?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true);

    private static OrderedOperationPlan Operation(OperationId id) => new(id, []);

    private static PayloadPlan Payload(uint id) => new(new(id), Data, [unchecked((byte)id), 0, 0, 0]);

    private static PayloadPlan Payload(uint id, params byte[] bytes) => new(new(id), new("float32"), bytes);

    private static OrderedTimelinePlan Empty(OrderedOperationPlan operation)
        => new("empty", false, [operation], [], [], [], []);

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
