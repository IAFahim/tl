using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct MovementHoleClip(int Value);

public readonly record struct MovementHoleTrack(int Code) : IBlend<MovementHoleClip>
{
    public void Blend(in MovementHoleClip first, in MovementHoleClip second, float factor, out MovementHoleClip result) => result = first;
}

public unsafe class MovementPairBankTests
{
    static MovementPairBankTests()
    {
        PairRuntime<MovementHoleTrack, MovementHoleClip>.Consume(&ExecuteCode, &BindFloat);
    }

    private static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    private static void ExecuteCode(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        MovementHoleClip scratch = default;
        var frame = TickFrame.ToFrame<MovementHoleTrack, MovementHoleClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Track.Code * frame.Clip.Value;
    }

    static readonly List<TimelineAsset> KeepAlive = new();

    static TimelineAsset Load(byte[] baked)
    {
        var asset = TimelineAsset.LoadAsset(baked);
        KeepAlive.Add(asset);
        return asset;
    }

    static byte[] WrapBake(float amount) => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(amount))
        .Looping()
        .Bake();

    static ushort InDomain(int position) =>
#if TL_CHECKED
        (ushort)Math.Min(position, 4);
#else
        (ushort)position;
#endif

    static byte[] HolePairBake() => new DomainBaker()
        .Track<MovementHoleTrack, MovementHoleClip>(new MovementHoleTrack(5))
        .Clip(0, 0, 4, new MovementHoleClip(7))
        .Bake();

    static byte[] ForeignBake() => new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(1f))
        .Clip(0, 0, 2, new LaneClip(1))
        .Bake();

    [Fact]
    public void ApplyLargeSpanResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(3f));
        var positions = Enumerable.Range(0, 32).Select(i => InDomain(i * 5 % 9)).ToArray();
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, positions, next, true, effects);

        var expectedNext = new ushort[positions.Length];
        var expectedEffects = new float[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            if (positions[i] < 4)
            {
                expectedEffects[i] = 6f;
                expectedNext[i] = positions[i] == 3 ? (ushort)0 : (ushort)(positions[i] + 1);
            }
            else expectedNext[i] = positions[i];
        }
        Assert.Equal(expectedNext, next);
        Assert.Equal(expectedEffects, effects);
    }

    [Fact]
    public void ApplyLargeSpanWithoutNextResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(9.5f));
        var positions = Enumerable.Range(0, 24).Select(i => InDomain(i * 7 % 8)).ToArray();
        var effects = new float[positions.Length];

        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, positions, true, effects);

        for (var i = 0; i < positions.Length; i++)
            Assert.Equal(positions[i] < 4 ? 19f : 0f, effects[i]);
    }

    [Fact]
    public void ApplySingletonResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(3.5f));

        var forward = new float[1];
        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, 2, true, forward);
        Assert.Equal(7f, forward[0]);

        var skipped = new float[1];
        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, InDomain(5), true, skipped);
        Assert.Equal(0f, skipped[0]);

        var backward = new float[1];
        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, 2, false, backward);
        Assert.Equal(-7f, backward[0]);

        var backwardEnd = new float[1];
        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, 4, false, backwardEnd);
        Assert.Equal(0f, backwardEnd[0]);
    }

    [Fact]
    public void AdvanceSingletonResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(4.5f));
        ushort position = 0;

        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, true);
        Assert.Equal(1, position);
        position = 3;
        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, true);
        Assert.Equal(0, position);
#if !TL_CHECKED
        position = 9;
        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, true);
        Assert.Equal(9, (int)position);
#endif
        position = 4;
        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, false);
        Assert.Equal(4, position);
        position = 2;
        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, false);
        Assert.Equal(1, position);
        position = 0;
        Timeline<MovementWrapTrack, MovementWrapClip>.Advance(asset.Index, ref position, false);
        Assert.Equal(3, position);
    }

    [Fact]
    public void ApplyColumnsLargeSpanResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(5.5f));
        var positions = Enumerable.Range(0, 24).Select(i => InDomain(i * 3 % 8)).ToArray();
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, positions, next, true, effects);

        for (var i = 0; i < positions.Length; i++)
        {
            if (positions[i] < 4)
            {
                Assert.Equal(11f, effects[i]);
                Assert.Equal(positions[i] == 3 ? 0 : positions[i] + 1, next[i]);
            }
            else
            {
                Assert.Equal(0f, effects[i]);
                Assert.Equal(positions[i], next[i]);
            }
        }
    }

    [Fact]
    public void ApplyColumnsSmallSpanResolvesUnfoldedIndex()
    {
        var asset = Load(WrapBake(6.5f));
        var positions = new ushort[] { 1, 4, 3 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, positions, next, false, effects);

        Assert.Equal(new ushort[] { 0, 4, 2 }, next);
        Assert.Equal([ -13f, 0f, -13f ], effects);
    }

    [Fact]
    public void ApplyAssetColumnsOverloadMatchesIndexOverload()
    {
        var asset = Load(WrapBake(7.5f));
        var positions = new ushort[] { 0, 2, 3, InDomain(6) };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, positions, next, true, effects);

        Assert.Equal(new ushort[] { 1, 3, 0, InDomain(6) }, next);
        Assert.Equal([ 15f, 15f, 15f, 0f ], effects);

        Assert.Throws<ArgumentNullException>(() =>
            Timeline<MovementWrapTrack, MovementWrapClip>.Apply((TimelineAsset)null!, positions, true, effects));
    }

    [Fact]
    public void GenericColumnSizeMismatchThrows()
    {
        var indices = new int[3];
        var positions = new ushort[3];
        var error = Assert.Throws<ArgumentException>(() =>
            Timeline<MovementWrapTrack, MovementWrapClip>.Advance(indices, positions, true));
        Assert.Contains("single-field", error.Message);

        int index = 0;
        byte position = 0;
        var byRefError = Assert.Throws<ArgumentException>(() =>
            Timeline<MovementWrapTrack, MovementWrapClip>.Advance(index, ref position, true));
        Assert.Contains("single-field", byRefError.Message);
    }

    [Fact]
    public void ViewReturnsAbsentSlotForForeignPairIndex()
    {
        var foreign = Load(ForeignBake());
        var asset = Load(HolePairBake());
        var effects = new float[1];

        Timeline<MovementHoleTrack, MovementHoleClip>.Apply(asset.Index, [ 1 ], true, effects);
        Assert.Equal(35f, effects[0]);

        var folded = Timeline<MovementHoleTrack, MovementHoleClip>.View(asset.Index);
        Assert.Equal(0, folded.Absent);
        Assert.Equal(4, folded.Duration);
        Assert.Equal(0, folded.Looping);
        Assert.Equal(5u, folded.TableTicks);
        Assert.Equal((ushort)sizeof(LaneMovementRecord), folded.RecordBytes);
        Assert.Equal(SlotView.AbiVersionV1, folded.AbiVersion);
        Assert.True(folded.Generation >= 1ul);

        var absent = Timeline<MovementHoleTrack, MovementHoleClip>.View(foreign.Index);
        Assert.Equal(1, absent.Absent);
        Assert.Equal((ushort)sizeof(LaneMovementRecord), absent.RecordBytes);
        Assert.Equal(SlotView.AbiVersionV1, absent.AbiVersion);
    }

    [Fact]
    public void ViewAssetOverloadForwardsAndRejectsNull()
    {
        var asset = Load(WrapBake(8.5f));
        Timeline<MovementWrapTrack, MovementWrapClip>.Apply(asset.Index, [ 0 ], true, new float[1]);

        var view = Timeline<MovementWrapTrack, MovementWrapClip>.View(asset);
        Assert.Equal(4, view.Duration);

        Assert.Throws<ArgumentNullException>(() => Timeline<MovementWrapTrack, MovementWrapClip>.View(null!));
    }
}
