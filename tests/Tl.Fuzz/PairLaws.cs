using Xunit;
using Xunit.Sdk;

namespace Tl.Fuzz;

public unsafe class PairLaws
{
    static PairLaws() => FuzzPairs.Install();
    [Fact]
    public void FoldMatchesTheClipModelForEveryPosition()
    {
        var random = FuzzRandom.FromSeeds(0x101, 0x303);
        for (var trial = 0; trial < 96; trial++)
        {
            var model = FuzzAssetGen.Next(random, out _, out _, out _, out _);
            FoldMatchesModel(model, $"trial {trial}");
        }
        FoldMatchesModel(FuzzAssetGen.OverlapPair(8, true, 0.5f), "overlap looping");
        FoldMatchesModel(FuzzAssetGen.OverlapPair(9, false, 1.25f), "overlap finite");
        FoldMatchesModel(FuzzAssetGen.OverlapPair(1, true, 0.25f), "single tick loop");
        FoldMatchesModel(FuzzAssetGen.OverlapPair(2, false, 0.25f), "two ticks finite");
    }

    [Fact]
    public void PerEntityApplyEqualsTheFoldAndTheSpanForm()
    {
        var random = FuzzRandom.FromSeeds(0x102, 0x303);
        for (var trial = 0; trial < 48; trial++)
        {
            var model = FuzzAssetGen.Next(random, out _, out _, out _, out _);
            using var asset = Load(model);
            foreach (var forward in new[] { true, false })
            {
                foreach (var position in SamplePositions(random, model.Duration, 24))
                {
                    var initial = Initial(random);

                    float perEntity = initial;
                    Timeline<FuzzTrack, FuzzClip>.Apply(asset.Index, position, forward, ref perEntity);

                    var spanEffects = new[] { initial };
                    var spanNext = new ushort[1];
                    Timeline<FuzzTrack, FuzzClip>.Apply(asset.Index, new[] { position }, spanNext, forward, spanEffects);

                    var expected = initial + FoldDelta(model, forward, position);
                    if (perEntity != expected)
                        throw new XunitException($"per-entity apply d={model.Duration} loop={model.Looping} forward={forward} pos={position}: {perEntity} != {expected}");
                    if (spanEffects[0] != expected)
                        throw new XunitException($"span apply d={model.Duration} loop={model.Looping} forward={forward} pos={position}: {spanEffects[0]} != {expected}");
                    var expectedNext = forward ? Movement.Forward(model.Duration, model.Looping, position) : Movement.Backward(model.Duration, model.Looping, position);
                    if (spanNext[0] != expectedNext)
                        throw new XunitException($"span apply next d={model.Duration} pos={position}: {spanNext[0]} != {expectedNext}");
                }
            }
        }
    }

    [Fact]
    public void SharedClockBroadcastEqualsThePerRowPath()
    {
        var random = FuzzRandom.FromSeeds(0x103, 0x303);
        for (var trial = 0; trial < 48; trial++)
        {
            var model = FuzzAssetGen.Next(random, out _, out _, out _, out _);
            using var asset = Load(model);
            foreach (var forward in new[] { true, false })
            {
                var position = SamplePositions(random, model.Duration, 1)[0];
                var rows = 1 + random.NextInt(70);

                var initial = new float[rows];
                for (var i = 0; i < rows; i++) initial[i] = Initial(random);

                var broadcast = (float[])initial.Clone();
                Timeline<FuzzTrack, FuzzClip>.Apply(asset.Index, position, forward, broadcast);

                var perRowPositions = new ushort[rows];
                Array.Fill(perRowPositions, position);
                var perRow = (float[])initial.Clone();
                Timeline<FuzzTrack, FuzzClip>.Apply(asset.Index, perRowPositions, forward, perRow);

                var delta = FoldDelta(model, forward, position);
                for (var i = 0; i < rows; i++)
                {
                    var expected = initial[i] + delta;
                    if (perRow[i] != expected || broadcast[i] != expected)
                        throw new XunitException($"broadcast vs per-row diverged d={model.Duration} forward={forward} row={i}: {perRow[i]} / {broadcast[i]} != {expected}");
                }
            }
        }
    }

    [Fact]
    public void ApplyNeverWritesTheClock()
    {
        var random = FuzzRandom.FromSeeds(0x104, 0x303);
        for (var trial = 0; trial < 32; trial++)
        {
            var model = FuzzAssetGen.Next(random, out _, out _, out _, out _);
            using var asset = Load(model);
            foreach (var forward in new[] { true, false })
            {
                var positions = SamplePositions(random, model.Duration, 40);
                var clock = (ushort[])positions.Clone();
                var effects = new float[positions.Length];
                Timeline<FuzzTrack, FuzzClip>.Apply(asset.Index, positions, forward, effects);
                for (var i = 0; i < positions.Length; i++)
                    if (clock[i] != positions[i])
                        throw new XunitException($"Apply moved the clock d={model.Duration} row={i}");
            }
        }
    }

    [Fact]
    public void TypedAdvanceRewindIsIdentityExceptFiniteCompletion()
    {
        var random = FuzzRandom.FromSeeds(0x105, 0x303);
        for (var trial = 0; trial < 48; trial++)
        {
            var model = FuzzAssetGen.Next(random, out _, out _, out _, out _);
            using var asset = Load(model);
            foreach (var position in SamplePositions(random, model.Duration, 24))
            {
                var advanced = position;
                Timeline<FuzzTrack, FuzzClip>.Advance(asset.Index, ref advanced, true);
                var rewound = advanced;
                Timeline<FuzzTrack, FuzzClip>.Advance(asset.Index, ref rewound, false);
                if ((model.Looping || position != model.Duration) && rewound != position)
                    throw new XunitException($"typed rewind(advance({position}))={rewound} d={model.Duration} loop={model.Looping}");
            }
        }
    }

    [Fact]
    public void BakeTwiceLoadsAndFoldsIdentically()
    {
        var random = FuzzRandom.FromSeeds(0x106, 0x303);
        for (var trial = 0; trial < 32; trial++)
        {
            var model = FuzzAssetGen.Next(random, out var duration, out var looping, out var scale, out var clips);
            var first = FuzzBake.Bake(duration, looping, scale, clips);
            var second = FuzzBake.Bake(duration, looping, scale, clips);
            if (!first.AsSpan().SequenceEqual(second))
                throw new XunitException($"bake is not deterministic d={duration} trial {trial}");
            using var asset = TimelineAsset.Of(TimelineAsset.Load(first));
            var view = Timeline<FuzzTrack, FuzzClip>.View(asset.Index);
            for (ushort tick = 0; tick <= duration; tick++)
                if (LaneEncoding.Value(&view, 0, true, tick) != model.Forward(tick))
                    throw new XunitException($"rebaked fold diverged at tick {tick} d={duration}");
        }
    }

    static void FoldMatchesModel(FuzzModel model, string context)
    {
        using var asset = Load(model);
        var view = Timeline<FuzzTrack, FuzzClip>.View(asset.Index);
        var duration = model.Duration;
        if (view.Duration != duration || (view.Looping != 0) != model.Looping)
            throw new XunitException($"view metadata diverged {context}: d={view.Duration} loop={view.Looping}");
        if (view.AbiVersion != SlotView.AbiVersionV3 || view.Absent != 0)
            throw new XunitException($"view ABI diverged {context}");
        for (ushort position = 0; position <= duration; position++)
        {
            if (position < duration && LaneMovement.ForwardNext(position, duration, model.Looping) != Movement.Forward(duration, model.Looping, position))
                throw new XunitException($"derived forward movement {context} pos={position}: {LaneMovement.ForwardNext(position, duration, model.Looping)}");
            var expectedEffect = position < duration ? model.Forward(position) : 0f;
            if (LaneEncoding.Value(&view, 0, true, position) != expectedEffect)
                throw new XunitException($"forward fold {context} pos={position}: {LaneEncoding.Value(&view, 0, true, position)} != {expectedEffect}");

            var backwardExpected = BackwardRecord(model, position);
            var derivedBackward = LaneMovement.BackwardPlayable(position, duration, model.Looping)
                ? LaneMovement.BackwardNext(position, duration)
                : SlotView.Skipped;
            if (derivedBackward != backwardExpected.Next)
                throw new XunitException($"derived backward movement {context} pos={position}: {derivedBackward}");
            var backwardTick = (ushort)(position == 0 ? duration - 1 : position - 1);
            var derivedByPosition = LaneEncoding.Value(&view, 0, false, backwardTick);
            if (derivedByPosition != model.Backward(backwardTick))
                throw new XunitException($"backward by-position fold {context} pos={position}: {derivedByPosition} != {model.Backward(backwardTick)}");
            if (LaneEncoding.Value(&view, 0, false, position) != model.Backward(position))
                throw new XunitException($"backward fold {context} pos={position}: {LaneEncoding.Value(&view, 0, false, position)} != {model.Backward(position)}");
        }
    }

    static (float Effect, ushort Next) BackwardRecord(FuzzModel model, ushort position)
    {
        var duration = model.Duration;
        var effect = model.BackwardMoveFrom(position);
        if (position < duration)
        {
            if (position == 0) return model.Looping ? (effect, (ushort)(duration - 1)) : (effect, SlotView.Skipped);
            return (effect, (ushort)(position - 1));
        }
        return model.Looping || duration == 0 ? (effect, SlotView.Skipped) : (effect, (ushort)(duration - 1));
    }

    static float FoldDelta(FuzzModel model, bool forward, ushort position)
        => forward ? position < model.Duration ? model.Forward(position) : 0f : model.BackwardMoveFrom(position);

    static TimelineAsset Load(FuzzModel model)
    {
        var bytes = FuzzBake.Bake(model.Duration, model.Looping, model.Scale, model.Clips);
        return TimelineAsset.Of(TimelineAsset.Load(bytes));
    }

    static ushort[] SamplePositions(FuzzRandom random, ushort duration, int count)
    {
        var positions = new ushort[count];
        for (var i = 0; i < count; i++)
            positions[i] = duration <= 20 && random.NextBool()
                ? (ushort)random.NextInt(Math.Min(65536, duration + 4))
                : random.Pick(
                [
                    (ushort)0, (ushort)1, (ushort)Math.Max(0, duration - 1), duration,
                    (ushort)Math.Min(65535, duration + 1), (ushort)random.NextInt(Math.Min(65536, duration + 4)),
                ]);
        return positions;
    }

    static float Initial(FuzzRandom random) => (random.NextInt(512) - 256) * 0.25f;
}
