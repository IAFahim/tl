using FsCheck.Fluent;
using Xunit;

namespace Tl.Fuzz.Laws;

using static FuzzLaws;

public static class Movement
{
    public static ushort Forward(ushort duration, bool looping, ushort position)
        => position < duration ? looping && position + 1 == duration ? (ushort)0 : (ushort)(position + 1) : position;

    public static ushort Backward(ushort duration, bool looping, ushort position)
        => looping
            ? position < duration ? position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1) : position
            : position > 0 && position <= duration ? (ushort)(position - 1) : position;
}

public class MovementLaws
{
    static readonly (ushort Duration, bool Looping)[] Shapes =
    [
        (0, false), (1, false), (1, true), (2, true), (3, false), (8, true),
        (15, false), (16, true), (17, false), (255, true), (1024, false), (65535, true),
    ];

    static readonly ushort[] Durations = [0, 1, 2, 8, 15, 16, 17, 255, 1024, 65535];

    sealed class ShapeTable : IDisposable
    {
        public readonly ushort[] Ids = new ushort[Shapes.Length];
        public readonly ushort[] DurationById = new ushort[65536];
        public readonly bool[] LoopingById = new bool[65536];
        readonly List<TimelineAsset> _assets = [];

        public static ShapeTable Load()
        {
            var table = new ShapeTable();
            for (var i = 0; i < Shapes.Length; i++)
            {
                var (duration, looping) = Shapes[i];
                var bytes = FuzzBake.Bake(duration, looping, 0.25f, duration == 0 ? [] : [new FuzzClipSpec(0, duration, 1f)]);
                var asset = TimelineAsset.Of(TimelineAsset.Load(bytes));
                table._assets.Add(asset);
                table.Ids[i] = asset.Index;
                table.DurationById[asset.Index] = duration;
                table.LoopingById[asset.Index] = looping;
            }
            return table;
        }

        public void Dispose()
        {
            foreach (var asset in _assets) asset.Dispose();
        }
    }

    static ushort[] SamplePositions(FuzzRandom random, int count, ushort duration)
    {
        var positions = new ushort[count];
        for (var i = 0; i < count; i++)
            positions[i] = random.Pick(
            [
                (ushort)0, (ushort)1, (ushort)Math.Max(0, duration - 1), duration,
                (ushort)Math.Min(65535, duration + 1), (ushort)Math.Min(65535, duration + 2),
                (ushort)65534, (ushort)65535, (ushort)random.NextInt(Math.Min(65536, duration + 3)),
            ]);
        return positions;
    }

    [Fact]
    public void AllTiersMatchTheMovementOracle()
    {
        using var table = ShapeTable.Load();
        Verify(nameof(AllTiersMatchTheMovementOracle), Prop.ForAll(ArbLength(70), count =>
        {
            var random = FuzzRandom.FromSeeds((ulong)count, 0xA1);
            var ids = new ushort[count];
            var positions = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                var shapeIndex = random.NextInt(Shapes.Length);
                ids[i] = random.NextBool() ? table.Ids[shapeIndex] : (ushort)random.NextInt(65536);
                positions[i] = SamplePositions(random, 1, table.DurationById[ids[i]])[0];
            }
            var forward = random.NextBool();

            var next = new ushort[count];
            Timeline.Advance(ids, positions, next, forward);

            var inPlace = (ushort[])positions.Clone();
            Timeline.Advance(ids, inPlace, forward);

            for (var i = 0; i < count; i++)
            {
                var id = ids[i];
                var expected = forward ? Movement.Forward(table.DurationById[id], table.LoopingById[id], positions[i]) : Movement.Backward(table.DurationById[id], table.LoopingById[id], positions[i]);
                if (next[i] != expected || inPlace[i] != expected) return false;

                var one = new[] { positions[i] };
                var oneNext = new ushort[1];
                Timeline.Advance(id, one, oneNext, forward);
                if (oneNext[0] != expected) return false;

                var inPlaceOne = new[] { positions[i] };
                Timeline.Advance(id, inPlaceOne, forward);
                if (inPlaceOne[0] != expected) return false;
            }

            var recordPositions = new ushort[Math.Min(count, 9)];
            Array.Copy(positions, recordPositions, recordPositions.Length);
            var recordNext = new ushort[recordPositions.Length];
            Timeline.Advance(ids[0], recordPositions, recordNext, forward);
            for (var i = 0; i < recordPositions.Length; i++)
            {
                var expected = forward ? Movement.Forward(table.DurationById[ids[0]], table.LoopingById[ids[0]], recordPositions[i]) : Movement.Backward(table.DurationById[ids[0]], table.LoopingById[ids[0]], recordPositions[i]);
                if (recordNext[i] != expected) return false;
            }
            return true;
        }));
    }

    [Fact]
    public void AdvanceThenRewindIsIdentityExceptFiniteCompletion()
    {
        Verify(nameof(AdvanceThenRewindIsIdentityExceptFiniteCompletion), Prop.ForAll(ArbUShort(0, 65535), ArbBool(), (positionSeed, looping) =>
        {
            var random = FuzzRandom.FromSeeds(positionSeed, 0xB2);
            var duration = random.Pick(Durations);
            var position = SamplePositions(random, 1, duration)[0];
            var rewound = Movement.Backward(duration, looping, Movement.Forward(duration, looping, position));
            return looping || position != duration ? rewound == position : true;
        }));
    }

    [Fact]
    public void RewindThenAdvanceIsIdentity()
    {
        Verify(nameof(RewindThenAdvanceIsIdentity), Prop.ForAll(ArbUShort(0, 65535), ArbBool(), (positionSeed, looping) =>
        {
            var random = FuzzRandom.FromSeeds(positionSeed, 0xC3);
            var duration = random.Pick(Durations);
            var position = SamplePositions(random, 1, duration)[0];
            var advanced = Movement.Forward(duration, looping, Movement.Backward(duration, looping, position));
            return advanced == position;
        }));
    }
}
