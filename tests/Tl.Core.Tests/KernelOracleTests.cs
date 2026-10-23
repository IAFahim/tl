using Xunit;
using Xunit.Sdk;

namespace Tl.Core.Tests;

public class KernelOracleTests
{
    const int DomainRows = 65536;
    const int ShapeRows = 4096;
    const int BroadcastSpan = 8;
    const int RowStride = 97;
    const int ShapeDomain = 0;
    const int ShapeUniform = 1;
    const int ShapeWaves = 2;
    const int ShapeEdges = 3;
    const ushort NextSentinel = 0xABCD;

    static readonly bool[] Directions = [true, false];

    static int LegalMax(int duration)
    {
#if TL_CHECKED
        return duration;
#else
        return 65535;
#endif
    }

    static int Shaped(int shape, int row, int duration)
    {
        var raw = shape switch
        {
            ShapeUniform => 5,
            ShapeWaves => row / 100,
            ShapeEdges => (row % 8) switch
            {
                0 => 0,
                1 => 1,
                2 => duration - 1,
                3 => duration,
                4 => duration + 1,
                5 => 65535,
                6 => 7,
                _ => duration / 2,
            },
            _ => row,
        };
        return Math.Min(Math.Max(raw, 0), LegalMax(duration));
    }

    static float SeedEffect(int row) => (row % 17) * 0.25f - 2f;

    static int Bits(float value) => BitConverter.SingleToInt32Bits(value);

    static float FromBits(int bits) => BitConverter.Int32BitsToSingle(bits);

    static int ExpectedBits(int seedBits, OracleFixture fixture, ushort position, bool forward)
    {
        var step = fixture.Step(forward, position);
        return step.Crossed ? Bits(FromBits(seedBits) + fixture.Effect(position, forward)) : seedBits;
    }

    static void SeedBitsColumn(int[] bits, int count)
    {
        for (var i = 0; i < count; i++) bits[i] = Bits(SeedEffect(i));
    }

    static void FillSentinels(ushort[] column, int count)
    {
        for (var i = 0; i < count; i++) column[i] = NextSentinel;
    }

    static void FillSeedEffects(float[] effects, int[] seedBits, int count)
    {
        for (var i = 0; i < count; i++) effects[i] = FromBits(seedBits[i]);
    }

    static void FillSentinelSlice(ushort[] column, int start, int length)
    {
        for (var i = start; i < start + length; i++) column[i] = NextSentinel;
    }

    static void FillSeedSlice(float[] effects, int[] seedBits, int start, int length)
    {
        for (var i = start; i < start + length; i++) effects[i] = FromBits(seedBits[i]);
    }

    static void Restore(ushort[] target, ushort[] source)
        => source.AsSpan(0, target.Length).CopyTo(target.AsSpan(0, target.Length));

    [Fact]
    public void OracleStepEqualsTimelineMovementAdvanceOnEverySweepInput()
    {
        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        foreach (var forward in Directions)
        {
            var duration = (ushort)durationValue;
            for (var position = 0; position <= ushort.MaxValue; position++)
            {
                var p = (ushort)position;
                var oracle = PlaybackOracle.Step(duration, looping, forward, p);
                var crossed = TimelineMovement.Advance(duration, looping, !forward, p, out var next, out var tick, out var flags);
                if (crossed != oracle.Crossed || next != oracle.Next || tick != oracle.Tick || flags != oracle.Flags)
                    throw new XunitException(
                        $"duration {duration} looping {looping} forward {forward} position {p}: " +
                        $"runtime ({crossed}, {next}, {tick}, {flags}) != oracle ({oracle.Crossed}, {oracle.Next}, {oracle.Tick}, {oracle.Flags}).");
                if (oracle.Crossed) continue;
                var chained = PlaybackOracle.Step(duration, looping, forward, oracle.Next);
                if (chained.Crossed || chained.Next != p)
                    throw new XunitException($"duration {duration} looping {looping} forward {forward}: rest position {p} is not a fixed point.");
            }
        }
    }

    [Fact]
    public void EveryCrossedStepIsReversibleInBothDirections()
    {
        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        {
            var duration = (ushort)durationValue;
            for (var position = 0; position <= ushort.MaxValue; position++)
            {
                var p = (ushort)position;
                var forward = PlaybackOracle.Step(duration, looping, true, p);
                if (forward.Crossed)
                {
                    var backward = PlaybackOracle.Step(duration, looping, false, forward.Next);
                    if (!backward.Crossed || backward.Next != p || backward.Tick != forward.Tick || backward.Flags != PlaybackOracle.InvertDirection(forward.Flags))
                        throw new XunitException(
                            $"duration {duration} looping {looping} position {p}: forward step ({forward.Next}, {forward.Tick}, {forward.Flags}) " +
                            $"does not invert to ({p}, {forward.Tick}, {PlaybackOracle.InvertDirection(forward.Flags)}); got ({backward.Crossed}, {backward.Next}, {backward.Tick}, {backward.Flags}).");
                }
                var back = PlaybackOracle.Step(duration, looping, false, p);
                if (!back.Crossed) continue;
                var again = PlaybackOracle.Step(duration, looping, true, back.Next);
                if (!again.Crossed || again.Next != p || again.Tick != back.Tick || again.Flags != PlaybackOracle.InvertDirection(back.Flags))
                    throw new XunitException(
                        $"duration {duration} looping {looping} position {p}: backward step ({back.Next}, {back.Tick}, {back.Flags}) " +
                        $"does not invert to ({p}, {back.Tick}, {PlaybackOracle.InvertDirection(back.Flags)}); got ({again.Crossed}, {again.Next}, {again.Tick}, {again.Flags}).");
            }
        }
    }

    [Fact]
    public void MovementTiersMatchTheOracleAcrossThePositionDomain()
    {
        var uniformPositions = new ushort[DomainRows];
        var uniformPristine = new ushort[DomainRows];
        var positions = new ushort[DomainRows];
        var pristine = new ushort[DomainRows];
        var runPositions = new ushort[DomainRows];
        var runPristine = new ushort[DomainRows];
        var uniformIds = new ushort[DomainRows];
        var alternatingIds = new ushort[DomainRows];
        var runIds = new ushort[DomainRows];
        var actual = new ushort[DomainRows];

        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        foreach (var forward in Directions)
        {
            var duration = (ushort)durationValue;
            var fixture = OracleFixtures.Standard(duration, looping, 2f);
            var companionDuration = (ushort)Math.Min(65535, Math.Max(1, duration * 2 / 3 + 5));
            var companion = OracleFixtures.Standard(companionDuration, !looping, 3f);
            for (var i = 0; i < DomainRows; i++)
            {
                var rowFixture = (i & 1) == 0 ? fixture : companion;
                var runFixture = (i / 3000) % 2 == 0 ? fixture : companion;
                uniformPositions[i] = (ushort)Math.Min(i, LegalMax(duration));
                positions[i] = (ushort)Math.Min(i, LegalMax(rowFixture.Duration));
                runPositions[i] = (ushort)Math.Min(i, LegalMax(runFixture.Duration));
                uniformIds[i] = fixture.Index;
                alternatingIds[i] = rowFixture.Index;
                runIds[i] = runFixture.Index;
            }
            uniformPositions.AsSpan().CopyTo(uniformPristine);
            positions.AsSpan().CopyTo(pristine);
            runPositions.AsSpan().CopyTo(runPristine);

            Timeline.Advance(fixture.Index, uniformPositions, actual, forward);
            AssertColumnUntouched(uniformPristine, uniformPositions, "advance/index-span");
            AssertUniformMovement(fixture, uniformPristine, forward, actual, "advance/index-span");

            Timeline.Advance(fixture.Index, uniformPositions, forward);
            AssertUniformMovement(fixture, uniformPristine, forward, uniformPositions, "advance/index-inplace");
            Restore(uniformPositions, uniformPristine);

            Timeline.Advance(uniformIds, uniformPositions, actual, forward);
            AssertColumnUntouched(uniformPristine, uniformPositions, "advance/ids-uniform");
            AssertUniformMovement(fixture, uniformPristine, forward, actual, "advance/ids-uniform");

            Timeline.Advance(uniformIds, uniformPositions, forward);
            AssertUniformMovement(fixture, uniformPristine, forward, uniformPositions, "advance/ids-alias");
            Restore(uniformPositions, uniformPristine);

            Timeline.Advance(alternatingIds, positions, actual, forward);
            AssertColumnUntouched(pristine, positions, "advance/ids-alternating");
            AssertMixedMovement(fixture, companion, fixture.Index, companion.Index, alternatingIds, pristine, forward, actual, "advance/ids-alternating");

            Timeline.Advance(runIds, runPositions, actual, forward);
            AssertMixedMovement(fixture, companion, fixture.Index, companion.Index, runIds, runPristine, forward, actual, "advance/ids-runs");

            var rows = new int[1 + (DomainRows - 1) / RowStride];
            for (var i = 0; i < rows.Length; i++) rows[i] = i * RowStride;
            Timeline<OracleTrack, OracleClip>.Advance<ushort, ushort>(rows, alternatingIds, positions, forward);
            AssertRowsMovement(fixture, companion, fixture.Index, companion.Index, alternatingIds, pristine, rows, forward, positions, "advance/rows");
            Restore(positions, pristine);

            for (var p = 0; p <= LegalMax(duration); p++)
            {
                ushort clock = (ushort)p;
                Timeline<OracleTrack, OracleClip>.Advance(fixture.Index, ref clock, forward);
                var expectedClock = fixture.Next((ushort)p, forward);
                if (clock != expectedClock)
                    throw new XunitException($"advance/shared-clock: duration {duration} looping {looping} forward {forward} position {p}: clock {clock} != oracle {expectedClock}.");
            }

            using var set = new TimelineSet<OracleTrack, OracleClip>();
            ushort firstId;
            ushort secondId;
            using (var firstAsset = TimelineAsset.LoadAsset(fixture.Bytes)) firstId = set.Add(firstAsset);
            using (var secondAsset = TimelineAsset.LoadAsset(companion.Bytes)) secondId = set.Add(secondAsset);
            var setIds = new ushort[DomainRows];
            for (var i = 0; i < DomainRows; i++)
            {
                setIds[i] = (i / 700) % 2 == 0 ? firstId : secondId;
                positions[i] = (ushort)Math.Min(i, LegalMax(setIds[i] == firstId ? fixture.Duration : companion.Duration));
            }
            positions.AsSpan().CopyTo(pristine);
            set.Advance(setIds, positions, actual, forward);
            AssertMixedMovement(fixture, companion, firstId, secondId, setIds, pristine, forward, actual, "advance/set-mixed");
        }
    }

    [Fact]
    public void ApplyTiersMatchTheOracleAcrossThePositionDomain()
    {
        var positions = new ushort[DomainRows];
        var pristine = new ushort[DomainRows];
        var uniformIds = new ushort[DomainRows];
        var alternatingIds = new ushort[DomainRows];
        var next = new ushort[DomainRows];
        var effects = new float[DomainRows];
        var seedBits = new int[DomainRows];
        var shapePositions = new ushort[ShapeRows];
        var shapePristine = new ushort[ShapeRows];
        var shapeIds = new ushort[ShapeRows];
        var shapeNext = new ushort[ShapeRows];
        var shapeEffects = new float[ShapeRows];
        var shapeSeedBits = new int[ShapeRows];
        Span<float> broadcast = stackalloc float[BroadcastSpan];

        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        foreach (var forward in Directions)
        {
            var duration = (ushort)durationValue;
            var fixture = OracleFixtures.Standard(duration, looping, 2f);
            var twin = OracleFixtures.Standard(duration, looping, 3f);
            SeedBitsColumn(seedBits, DomainRows);
            for (var i = 0; i < DomainRows; i++)
            {
                positions[i] = (ushort)Math.Min(i, LegalMax(duration));
                uniformIds[i] = fixture.Index;
                alternatingIds[i] = (i & 1) == 0 ? fixture.Index : twin.Index;
            }
            positions.AsSpan().CopyTo(pristine);

            FillSentinels(next, DomainRows);
            FillSeedEffects(effects, seedBits, DomainRows);
            Timeline<OracleTrack, OracleClip>.Apply(fixture.Index, positions, next, forward, effects);
            AssertColumnUntouched(pristine, positions, "apply/index-slot");
            AssertUniformApply(fixture, pristine, seedBits, forward, next, effects, "apply/index-slot");

            FillSentinels(next, DomainRows);
            FillSeedEffects(effects, seedBits, DomainRows);
            Timeline<OracleTrack, OracleClip>.Apply(uniformIds, positions, next, forward, effects);
            AssertColumnUntouched(pristine, positions, "apply/crowd-uniform");
            AssertUniformApply(fixture, pristine, seedBits, forward, next, effects, "apply/crowd-uniform");

            FillSentinels(next, DomainRows);
            FillSeedEffects(effects, seedBits, DomainRows);
            Timeline<OracleTrack, OracleClip>.Apply(alternatingIds, positions, next, forward, effects);
            AssertColumnUntouched(pristine, positions, "apply/crowd-mixed");
            AssertMixedApply(fixture, twin, fixture.Index, twin.Index, alternatingIds, pristine, seedBits, forward, next, effects, "apply/crowd-mixed");

            FillSeedEffects(effects, seedBits, DomainRows);
            Timeline<OracleTrack, OracleClip>.Apply(uniformIds, positions, forward, effects);
            AssertColumnUntouched(pristine, positions, "apply/crowd-readonly");
            AssertUniformEffects(fixture, pristine, seedBits, forward, effects, "apply/crowd-readonly");

            FillSeedEffects(effects, seedBits, DomainRows);
            Timeline<OracleTrack, OracleClip>.Apply(uniformIds, positions, positions, forward, effects);
            AssertUniformApply(fixture, pristine, seedBits, forward, positions, effects, "apply/crowd-alias");

            for (var p = 0; p <= LegalMax(duration); p++)
            {
                for (var i = 0; i < BroadcastSpan; i++) broadcast[i] = SeedEffect(i);
                Timeline<OracleTrack, OracleClip>.Apply(fixture.Index, (ushort)p, forward, broadcast);
                for (var i = 0; i < BroadcastSpan; i++)
                {
                    var expected = ExpectedBits(Bits(SeedEffect(i)), fixture, (ushort)p, forward);
                    if (Bits(broadcast[i]) != expected)
                        throw new XunitException($"apply/shared-clock: duration {duration} looping {looping} forward {forward} position {p}: effect bits {Bits(broadcast[i])} != oracle {expected}.");
                }
            }

            for (var p = 0; p <= LegalMax(duration); p++)
            {
                var index = fixture.Index;
                var position = (ushort)p;
                var effect = SeedEffect(p % 17);
                Timeline<OracleTrack, OracleClip>.Apply<ushort, ushort, float>(in index, in position, forward, ref effect);
                var expected = ExpectedBits(Bits(SeedEffect(p % 17)), fixture, position, forward);
                if (Bits(effect) != expected)
                    throw new XunitException($"apply/per-entity: duration {duration} looping {looping} forward {forward} position {p}: effect bits {Bits(effect)} != oracle {expected}.");
            }

            for (var shape = ShapeUniform; shape <= ShapeEdges; shape++)
            {
                SeedBitsColumn(shapeSeedBits, ShapeRows);
                for (var i = 0; i < ShapeRows; i++)
                {
                    shapePositions[i] = (ushort)Shaped(shape, i, duration);
                    shapeIds[i] = (i & 1) == 0 ? fixture.Index : twin.Index;
                }
                shapePositions.AsSpan().CopyTo(shapePristine);

                FillSentinels(shapeNext, ShapeRows);
                FillSeedEffects(shapeEffects, shapeSeedBits, ShapeRows);
                Timeline<OracleTrack, OracleClip>.Apply(fixture.Index, shapePositions, shapeNext, forward, shapeEffects);
                AssertColumnUntouched(shapePristine, shapePositions, $"apply/index-slot/shape{shape}");
                AssertUniformApply(fixture, shapePristine, shapeSeedBits, forward, shapeNext, shapeEffects, $"apply/index-slot/shape{shape}");

                FillSentinels(shapeNext, ShapeRows);
                FillSeedEffects(shapeEffects, shapeSeedBits, ShapeRows);
                Timeline<OracleTrack, OracleClip>.Apply(shapeIds, shapePositions, shapeNext, forward, shapeEffects);
                AssertColumnUntouched(shapePristine, shapePositions, $"apply/crowd-mixed/shape{shape}");
                AssertMixedApply(fixture, twin, fixture.Index, twin.Index, shapeIds, shapePristine, shapeSeedBits, forward, shapeNext, shapeEffects, $"apply/crowd-mixed/shape{shape}");
            }

            SeedBitsColumn(shapeSeedBits, ShapeRows);
            for (var i = 0; i < ShapeRows; i++)
            {
                shapePositions[i] = (ushort)Shaped(ShapeEdges, i, duration);
                shapeIds[i] = (i & 1) == 0 ? fixture.Index : twin.Index;
            }
            shapePositions.AsSpan().CopyTo(shapePristine);
            var sparseRows = new int[1 + (ShapeRows - 1) / RowStride];
            for (var i = 0; i < sparseRows.Length; i++) sparseRows[i] = i * RowStride;
            FillSeedEffects(shapeEffects, shapeSeedBits, ShapeRows);
            Timeline<OracleTrack, OracleClip>.Apply<ushort, ushort, float>(sparseRows, shapeIds, shapePositions, forward, shapeEffects);
            AssertColumnUntouched(shapePristine, shapePositions, "apply/rows");
            AssertRowsApply(fixture, twin, fixture.Index, twin.Index, shapeIds, shapePristine, sparseRows, shapeSeedBits, forward, shapeEffects, "apply/rows");
        }
    }

    [Fact]
    public unsafe void BankLaneValuesEqualTheDirectFoldAtEveryTickAndDirection()
    {
        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        foreach (var shape in new[] { OracleShape.Standard, OracleShape.Gap, OracleShape.Blend })
        {
            if (shape != OracleShape.Standard && durationValue < 8) continue;
            var fixture = OracleFixtures.Get((ushort)durationValue, looping, shape, 2f);
            var duration = fixture.Duration;
            var forwardFold = new float[duration];
            var backwardFold = new float[duration];
            for (var p = 0; p <= duration; p++)
            {
                var position = (ushort)p;
                var forwardStep = fixture.Step(true, position);
                if (forwardStep.Crossed) forwardFold[forwardStep.Tick] = fixture.Effect(position, true);
                else if (fixture.Sampled(position, true))
                    throw new XunitException($"memo: duration {duration} looping {looping} forward sampled a frame at rest position {position}.");
                var backwardStep = fixture.Step(false, position);
                if (backwardStep.Crossed) backwardFold[backwardStep.Tick] = fixture.Effect(position, false);
                else if (fixture.Sampled(position, false))
                    throw new XunitException($"memo: duration {duration} looping {looping} backward sampled a frame at rest position {position}.");
            }
            var view = fixture.View;
            if (shape == OracleShape.Standard && durationValue == 4096 && (view.Forward != null || view.Backward != null))
                throw new XunitException("memo: the 4096-tick standard fixture should exercise the segmented encoding, not the flat tables.");
            for (var tick = 0; tick < duration; tick++)
            {
                var bankForward = Bits(LaneEncoding.Value(&view, 0, true, tick));
                if (bankForward != Bits(forwardFold[tick]))
                    throw new XunitException($"memo: duration {duration} looping {looping} {shape} forward tick {tick}: bank bits {bankForward} != fold bits {Bits(forwardFold[tick])}.");
                var bankBackward = Bits(LaneEncoding.Value(&view, 0, false, tick));
                if (bankBackward != Bits(backwardFold[tick]))
                    throw new XunitException($"memo: duration {duration} looping {looping} {shape} backward tick {tick}: bank bits {bankBackward} != fold bits {Bits(backwardFold[tick])}.");
            }
        }
    }

    [Fact]
    public void BatchLengthsAndMisalignedStartsMatchTheOracle()
    {
        const int pad = 64;
        var durations = new[] { 0, 1, 2, 7, 8, 9, 16, 33, 64, 255, 2048, 2049, 4096 };
        var lengths = new int[41 + 19];
        for (var i = 0; i <= 40; i++) lengths[i] = i;
        var boundary = new[] { 47, 48, 49, 63, 64, 65, 127, 128, 255, 256, 511, 512, 1023, 1024, 2047, 2048, 2049, 4095, 4096 };
        for (var i = 0; i < boundary.Length; i++) lengths[41 + i] = boundary[i];
        var starts = new[] { 0, 1, 3, 13 };
        var columns = 4096 + pad;
        var positions = new ushort[columns];
        var uniformIds = new ushort[columns];
        var alternatingIds = new ushort[columns];
        var next = new ushort[columns];
        var effects = new float[columns];
        var seedBits = new int[columns];

        foreach (var durationValue in durations)
        foreach (var looping in Directions)
        foreach (var forward in Directions)
        foreach (var shape in new[] { ShapeDomain, ShapeEdges })
        {
            var duration = (ushort)durationValue;
            var fixture = OracleFixtures.Standard(duration, looping, 2f);
            var twin = OracleFixtures.Standard(duration, looping, 3f);
            SeedBitsColumn(seedBits, columns);
            for (var i = 0; i < columns; i++)
            {
                positions[i] = (ushort)Shaped(shape, i, duration);
                uniformIds[i] = fixture.Index;
                alternatingIds[i] = (i & 1) == 0 ? fixture.Index : twin.Index;
            }

            foreach (var start in starts)
            foreach (var length in lengths)
            {
                FillSentinelSlice(next, start, length);
                FillSeedSlice(effects, seedBits, start, length);
                Timeline<OracleTrack, OracleClip>.Apply(fixture.Index, positions.AsSpan(start, length), next.AsSpan(start, length), forward, effects.AsSpan(start, length));
                AssertSliceApply(fixture, positions, seedBits, start, length, forward, next, effects, "matrix/apply-slot");

                FillSentinelSlice(next, start, length);
                FillSeedSlice(effects, seedBits, start, length);
                Timeline<OracleTrack, OracleClip>.Apply(alternatingIds.AsSpan(start, length), positions.AsSpan(start, length), next.AsSpan(start, length), forward, effects.AsSpan(start, length));
                AssertSliceMixedApply(fixture, twin, fixture.Index, twin.Index, alternatingIds, positions, seedBits, start, length, forward, next, effects, "matrix/apply-mixed");

                FillSentinelSlice(next, start, length);
                Timeline.Advance(fixture.Index, positions.AsSpan(start, length), next.AsSpan(start, length), forward);
                AssertSliceMovement(fixture, positions, start, length, forward, next, "matrix/advance-index");

                FillSentinelSlice(next, start, length);
                Timeline.Advance(alternatingIds.AsSpan(start, length), positions.AsSpan(start, length), next.AsSpan(start, length), forward);
                AssertSliceMixedMovement(fixture, twin, fixture.Index, twin.Index, alternatingIds, positions, start, length, forward, next, "matrix/advance-mixed");
            }
        }
    }

    [Fact]
    public void BatchPartitioningNeverChangesARow()
    {
        const int rows = 4096;
        foreach (var durationValue in new[] { 9, 64, 2048 })
        foreach (var looping in Directions)
        foreach (var forward in Directions)
        {
            var duration = (ushort)durationValue;
            var fixture = OracleFixtures.Standard(duration, looping, 2f);
            var twin = OracleFixtures.Standard(duration, looping, 3f);
            var positions = new ushort[rows];
            var ids = new ushort[rows];
            var seedBits = new int[rows];
            SeedBitsColumn(seedBits, rows);
            for (var i = 0; i < rows; i++)
            {
                positions[i] = (ushort)Shaped(ShapeWaves, i, duration);
                ids[i] = (i & 1) == 0 ? fixture.Index : twin.Index;
            }

            var referenceNext = new ushort[rows];
            var referenceEffects = new float[rows];
            FillSeedEffects(referenceEffects, seedBits, rows);
            Timeline<OracleTrack, OracleClip>.Apply(ids, positions, referenceNext, forward, referenceEffects);

            var partitionedNext = new ushort[rows];
            var partitionedEffects = new float[rows];
            FillSeedEffects(partitionedEffects, seedBits, rows);
            int[] cuts = [0, 1, 17, 154, 1154, rows];
            for (var c = 0; c + 1 < cuts.Length; c++)
                Timeline<OracleTrack, OracleClip>.Apply(
                    ids.AsSpan(cuts[c], cuts[c + 1] - cuts[c]),
                    positions.AsSpan(cuts[c], cuts[c + 1] - cuts[c]),
                    partitionedNext.AsSpan(cuts[c], cuts[c + 1] - cuts[c]),
                    forward,
                    partitionedEffects.AsSpan(cuts[c], cuts[c + 1] - cuts[c]));
            Assert.Equal(referenceNext, partitionedNext);
            AssertBitExact(referenceEffects, partitionedEffects, "partition/cuts");

            var singleRow = new int[1];
            var singleEffects = new float[rows];
            for (var i = 0; i < rows; i++)
            {
                FillSeedEffects(singleEffects, seedBits, rows);
                singleRow[0] = i;
                Timeline<OracleTrack, OracleClip>.Apply<ushort, ushort, float>(singleRow, ids, positions, forward, singleEffects);
                if (Bits(singleEffects[i]) != Bits(referenceEffects[i]))
                    throw new XunitException($"partition/single-row: duration {duration} looping {looping} forward {forward} row {i}: effect bits {Bits(singleEffects[i])} != whole-batch {Bits(referenceEffects[i])}.");
            }
        }
    }

    [Fact]
    public void ForwardThenBackwardReplaysEveryRowBitExactly()
    {
        const int rows = 1024;
        foreach (var durationValue in PlaybackOracle.SweepDurations)
        foreach (var looping in Directions)
        foreach (var firstForward in Directions)
        {
            var duration = (ushort)durationValue;
            var frames = Math.Min((int)duration, 32) + 2;
            if (!looping && duration < frames) continue;
            var fixture = OracleFixtures.Standard(duration, looping, 2f);
            var positions = new ushort[rows];
            var ids = new ushort[rows];
            var seedBits = new int[rows];
            SeedBitsColumn(seedBits, rows);
            var startSpan = looping ? duration + 1 : duration - frames + 1;
            for (var i = 0; i < rows; i++)
            {
                positions[i] = looping
                    ? (ushort)(i % (duration + 1))
                    : firstForward
                        ? (ushort)(i % startSpan)
                        : (ushort)(frames + i % startSpan);
                ids[i] = fixture.Index;
            }
            var startPositions = (ushort[])positions.Clone();
            var effects = new float[rows];
            FillSeedEffects(effects, seedBits, rows);

            for (var frame = 0; frame < frames; frame++)
                Timeline<OracleTrack, OracleClip>.Apply(ids, positions, positions, firstForward, effects);
            for (var frame = 0; frame < frames; frame++)
                Timeline<OracleTrack, OracleClip>.Apply(ids, positions, positions, !firstForward, effects);

            Assert.Equal(startPositions, positions);
            for (var i = 0; i < rows; i++)
                if (Bits(effects[i]) != seedBits[i])
                    throw new XunitException($"roundtrip: duration {duration} looping {looping} firstForward {firstForward} row {i}: effect bits {Bits(effects[i])} != {seedBits[i]}.");

            positions = (ushort[])startPositions.Clone();
            for (var frame = 0; frame < frames; frame++)
                Timeline.Advance(ids, positions, firstForward);
            for (var frame = 0; frame < frames; frame++)
                Timeline.Advance(ids, positions, !firstForward);
            Assert.Equal(startPositions, positions);
        }
    }

    static void AssertBitExact(float[] expected, float[] actual, string tier)
    {
        for (var i = 0; i < expected.Length; i++)
            if (Bits(actual[i]) != Bits(expected[i]))
                throw new XunitException($"{tier}: row {i} effect bits {Bits(actual[i])} != {Bits(expected[i])}.");
    }

    static void AssertColumnUntouched(ushort[] pristine, ushort[] column, string tier)
    {
        for (var i = 0; i < pristine.Length; i++)
            if (column[i] != pristine[i])
                throw new XunitException($"{tier}: row {i} position mutated from {pristine[i]} to {column[i]}.");
    }

    static void AssertUniformMovement(OracleFixture fixture, ushort[] seed, bool forward, ushort[] next, string tier)
    {
        for (var i = 0; i < seed.Length; i++)
        {
            var expected = fixture.Next(seed[i], forward);
            if (next[i] != expected)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: next {next[i]} != oracle {expected}.");
        }
    }

    static void AssertMixedMovement(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] seed, bool forward, ushort[] next, string tier)
    {
        for (var i = 0; i < seed.Length; i++)
        {
            var fixture = ids[i] == firstId ? first : second;
            var expected = fixture.Next(seed[i], forward);
            if (next[i] != expected)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} id {ids[i]} position {seed[i]}: next {next[i]} != oracle {expected}.");
        }
    }

    static void AssertRowsMovement(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] seed, int[] rows, bool forward, ushort[] positions, string tier)
    {
        var touched = new HashSet<int>(rows);
        for (var i = 0; i < seed.Length; i++)
        {
            if (touched.Contains(i)) continue;
            if (positions[i] != seed[i])
                throw new XunitException($"{tier}: untouched row {i} moved from {seed[i]} to {positions[i]}.");
        }
        foreach (var row in rows)
        {
            var fixture = ids[row] == firstId ? first : second;
            var expected = fixture.Next(seed[row], forward);
            if (positions[row] != expected)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {row} position {seed[row]}: next {positions[row]} != oracle {expected}.");
        }
    }

    static void AssertUniformApply(OracleFixture fixture, ushort[] seed, int[] seedBits, bool forward, ushort[] next, float[] effects, string tier)
    {
        for (var i = 0; i < seed.Length; i++)
        {
            var expectedNext = fixture.Next(seed[i], forward);
            if (next[i] != expectedNext)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: next {next[i]} != oracle {expectedNext}.");
            var expectedBits = ExpectedBits(seedBits[i], fixture, seed[i], forward);
            if (Bits(effects[i]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: effect bits {Bits(effects[i])} != oracle {expectedBits}.");
        }
    }

    static void AssertUniformEffects(OracleFixture fixture, ushort[] seed, int[] seedBits, bool forward, float[] effects, string tier)
    {
        for (var i = 0; i < seed.Length; i++)
        {
            var expectedBits = ExpectedBits(seedBits[i], fixture, seed[i], forward);
            if (Bits(effects[i]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: effect bits {Bits(effects[i])} != oracle {expectedBits}.");
        }
    }

    static void AssertMixedApply(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] seed, int[] seedBits, bool forward, ushort[] next, float[] effects, string tier)
    {
        for (var i = 0; i < seed.Length; i++)
        {
            var fixture = ids[i] == firstId ? first : second;
            var expectedNext = fixture.Next(seed[i], forward);
            if (next[i] != expectedNext)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: next {next[i]} != oracle {expectedNext}.");
            var expectedBits = ExpectedBits(seedBits[i], fixture, seed[i], forward);
            if (Bits(effects[i]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {i} position {seed[i]}: effect bits {Bits(effects[i])} != oracle {expectedBits}.");
        }
    }

    static void AssertRowsApply(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] seed, int[] rows, int[] seedBits, bool forward, float[] effects, string tier)
    {
        var touched = new HashSet<int>(rows);
        for (var i = 0; i < seed.Length; i++)
        {
            if (touched.Contains(i)) continue;
            if (Bits(effects[i]) != seedBits[i])
                throw new XunitException($"{tier}: untouched row {i} effect bits {Bits(effects[i])} != seed {seedBits[i]}.");
        }
        foreach (var row in rows)
        {
            var fixture = ids[row] == firstId ? first : second;
            var expectedBits = ExpectedBits(seedBits[row], fixture, seed[row], forward);
            if (Bits(effects[row]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} row {row} position {seed[row]}: effect bits {Bits(effects[row])} != oracle {expectedBits}.");
        }
    }

    static void AssertSliceApply(OracleFixture fixture, ushort[] positions, int[] seedBits, int start, int length, bool forward, ushort[] next, float[] effects, string tier)
    {
        for (var i = start; i < start + length; i++)
        {
            var expectedNext = fixture.Next(positions[i], forward);
            if (next[i] != expectedNext)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: next {next[i]} != oracle {expectedNext}.");
            var expectedBits = ExpectedBits(seedBits[i], fixture, positions[i], forward);
            if (Bits(effects[i]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: effect bits {Bits(effects[i])} != oracle {expectedBits}.");
        }
    }

    static void AssertSliceMixedApply(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] positions, int[] seedBits, int start, int length, bool forward, ushort[] next, float[] effects, string tier)
    {
        for (var i = start; i < start + length; i++)
        {
            var fixture = ids[i] == firstId ? first : second;
            var expectedNext = fixture.Next(positions[i], forward);
            if (next[i] != expectedNext)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: next {next[i]} != oracle {expectedNext}.");
            var expectedBits = ExpectedBits(seedBits[i], fixture, positions[i], forward);
            if (Bits(effects[i]) != expectedBits)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: effect bits {Bits(effects[i])} != oracle {expectedBits}.");
        }
    }

    static void AssertSliceMovement(OracleFixture fixture, ushort[] positions, int start, int length, bool forward, ushort[] next, string tier)
    {
        for (var i = start; i < start + length; i++)
        {
            var expected = fixture.Next(positions[i], forward);
            if (next[i] != expected)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: next {next[i]} != oracle {expected}.");
        }
    }

    static void AssertSliceMixedMovement(OracleFixture first, OracleFixture second, ushort firstId, ushort secondId, ushort[] ids, ushort[] positions, int start, int length, bool forward, ushort[] next, string tier)
    {
        for (var i = start; i < start + length; i++)
        {
            var fixture = ids[i] == firstId ? first : second;
            var expected = fixture.Next(positions[i], forward);
            if (next[i] != expected)
                throw new XunitException($"{tier}: duration {fixture.Duration} looping {fixture.Looping} forward {forward} start {start} length {length} row {i} position {positions[i]}: next {next[i]} != oracle {expected}.");
        }
    }
}
