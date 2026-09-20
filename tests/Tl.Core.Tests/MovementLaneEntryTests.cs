using System.Runtime.CompilerServices;
using Xunit;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct MovementWrapClip(float Amount);

public readonly record struct MovementWrapTrack(float Scale) : IBlend<MovementWrapClip>
{
    public void Blend(in MovementWrapClip first, in MovementWrapClip second, float factor, out MovementWrapClip result)
        => result = new MovementWrapClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct MovementWrapLane : ITimelineLane<MovementWrapLane>
{
    public static ushort Duration => 4;
    public static bool Looping => true;
    public static float Effect(ushort position) => position;
    public static float InverseEffect(ushort position) => -position;
}

public readonly struct MovementClampLane : ITimelineLane<MovementClampLane>
{
    public static ushort Duration => 4;
    public static bool Looping => false;
    public static float Effect(ushort position) => position + 0.5f;
    public static float InverseEffect(ushort position) => -(position + 0.5f);
}

public readonly struct MovementZeroLane : ITimelineLane<MovementZeroLane>
{
    public static ushort Duration => 0;
    public static bool Looping => true;
    public static float Effect(ushort position) => 1f;
    public static float InverseEffect(ushort position) => -1f;
}

internal static unsafe class MovementLanePairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<MovementWrapTrack, MovementWrapClip>.Consume(&ExecuteScale, &BindFloat);
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

    private static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        MovementWrapClip scratch = default;
        var frame = TickFrame.ToFrame<MovementWrapTrack, MovementWrapClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }
}

public class MovementLaneEntryTests
{
    [Fact]
    public void ApplyColumnsFollowsMovementLawForward()
    {
        var positions = new ushort[] { 0, 1, 2, 3, 4, 5, 3, 0 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapLane>.Apply(positions, next, true, effects);

        Assert.Equal(new ushort[] { 1, 2, 3, 0, 4, 5, 0, 1 }, next);
        Assert.Equal(new float[] { 0f, 1f, 2f, 3f, 0f, 0f, 3f, 0f }, effects);
    }

    [Fact]
    public void ApplyColumnsFollowsMovementLawBackward()
    {
        var positions = new ushort[] { 0, 1, 2, 3, 4, 5, 0, 3 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementWrapLane>.Apply(positions, next, false, effects);

        Assert.Equal(new ushort[] { 3, 0, 1, 2, 4, 5, 3, 2 }, next);
        Assert.Equal(new float[] { -3f, 0f, -1f, -2f, 0f, 0f, -3f, -2f }, effects);
    }

    [Fact]
    public void ApplyColumnsClampLaneFinishesAtDuration()
    {
        var positions = new ushort[] { 0, 1, 3, 4, 5 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementClampLane>.Apply(positions, next, false, effects);

        Assert.Equal(new ushort[] { 0, 0, 2, 3, 5 }, next);
        Assert.Equal(new float[] { 0f, -0.5f, -2.5f, -3.5f, 0f }, effects);

        var forwardPositions = new ushort[] { 0, 2, 3, 4 };
        var forwardNext = new ushort[forwardPositions.Length];
        var forwardEffects = new float[forwardPositions.Length];

        Timeline<MovementClampLane>.Apply(forwardPositions, forwardNext, true, forwardEffects);

        Assert.Equal(new ushort[] { 1, 3, 4, 4 }, forwardNext);
        Assert.Equal(new float[] { 0.5f, 2.5f, 3.5f, 0f }, forwardEffects);
    }

    [Fact]
    public void ZeroDurationLaneCopiesColumnsWithoutEffects()
    {
        var positions = new ushort[] { 3, 9 };
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];

        Timeline<MovementZeroLane>.Apply(positions, next, true, effects);
        Assert.Equal(new ushort[] { 3, 9 }, next);
        Assert.Equal(new float[] { 0f, 0f }, effects);

        Timeline<MovementZeroLane>.Advance(positions, next, true);
        Assert.Equal(new ushort[] { 3, 9 }, next);
    }

    [Fact]
    public void EmptySpansAreNoOps()
    {
        Timeline<MovementWrapLane>.Apply(Array.Empty<ushort>(), Array.Empty<ushort>(), true, Array.Empty<float>());
        Timeline<MovementWrapLane>.Advance(Array.Empty<ushort>(), Array.Empty<ushort>(), true);
        Timeline<MovementClampLane>.Apply(Array.Empty<ushort>(), Array.Empty<ushort>(), false, Array.Empty<float>());
        Timeline<MovementClampLane>.Advance(Array.Empty<ushort>(), Array.Empty<ushort>(), false);
    }

    [Fact]
    public void AdvanceColumnsFollowsMovementLawWrap()
    {
        var positions = new ushort[] { 0, 1, 2, 3, 4, 9 };
        var next = new ushort[positions.Length];

        Timeline<MovementWrapLane>.Advance(positions, next, true);
        Assert.Equal(new ushort[] { 1, 2, 3, 0, 4, 9 }, next);

        Timeline<MovementWrapLane>.Advance(positions, next, false);
        Assert.Equal(new ushort[] { 3, 0, 1, 2, 4, 9 }, next);
    }

    [Fact]
    public void AdvanceColumnsFollowsMovementLawClamp()
    {
        var positions = new ushort[] { 0, 1, 2, 3, 4, 9 };
        var next = new ushort[positions.Length];

        Timeline<MovementClampLane>.Advance(positions, next, true);
        Assert.Equal(new ushort[] { 1, 2, 3, 4, 4, 9 }, next);

        Timeline<MovementClampLane>.Advance(positions, next, false);
        Assert.Equal(new ushort[] { 0, 0, 1, 2, 3, 9 }, next);
    }

    static byte[] WrapBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(2f))
        .Clip(0, 0, 4, new MovementWrapClip(3f))
        .Looping()
        .Bake();

    static byte[] FiniteBake() => new DomainBaker()
        .Track<MovementWrapTrack, MovementWrapClip>(new MovementWrapTrack(1f))
        .Clip(0, 0, 9, new MovementWrapClip(1f))
        .Bake();

    static void ApplyOracle(ushort[] positions, ushort[] next, float[] effects, bool forward, ushort duration, bool looping)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (forward)
            {
                if (pos < duration)
                {
                    effects[i] += 6f;
                    next[i] = looping && pos + 1 == duration ? (ushort)0 : (ushort)(pos + 1);
                }
                else next[i] = pos;
            }
            else
            {
                if (looping ? pos < duration : pos > 0 && pos <= duration)
                {
                    var tick = pos == 0 ? (ushort)(duration - 1) : (ushort)(pos - 1);
                    effects[i] += -6f;
                    next[i] = tick;
                }
                else next[i] = pos;
            }
        }
    }

    static void FiniteApplyOracle(ushort[] positions, ushort[] next, float[] effects, bool forward, ushort duration)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (forward)
            {
                if (pos < duration)
                {
                    effects[i] += 1f;
                    next[i] = (ushort)(pos + 1);
                }
                else next[i] = pos;
            }
            else
            {
                if (pos > 0 && pos <= duration)
                {
                    effects[i] += -1f;
                    next[i] = (ushort)(pos - 1);
                }
                else next[i] = pos;
            }
        }
    }

    static ushort[] OracleRows(int count) => Enumerable.Range(0, count).Select(i => (ushort)(i * 7 % 12)).ToArray();

    [Fact]
    public void BakedLaneColumnApplyMatchesRecordsOracle()
    {
        using var asset = TimelineAsset.LoadAsset(WrapBake());
        BakedLane<MovementWrapTrack, MovementWrapClip>.Bind(asset);

        var positions = OracleRows(70);
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];
        var oraclePositions = (ushort[])positions.Clone();
        var oracleNext = new ushort[positions.Length];
        var oracleEffects = new float[positions.Length];
        for (var i = 0; i < effects.Length; i++)
        {
            effects[i] = i * 0.25f;
            oracleEffects[i] = effects[i];
        }

        Timeline<BakedLane<MovementWrapTrack, MovementWrapClip>>.Apply(positions, next, true, effects);
        ApplyOracle(oraclePositions, oracleNext, oracleEffects, true, 4, true);
        Assert.Equal(oraclePositions, positions);
        Assert.Equal(oracleNext, next);
        Assert.Equal(oracleEffects, effects);

        var backwardPositions = OracleRows(37);
        var backwardNext = new ushort[backwardPositions.Length];
        var backwardEffects = new float[backwardPositions.Length];
        var backwardOraclePositions = (ushort[])backwardPositions.Clone();
        var backwardOracleNext = new ushort[backwardPositions.Length];
        var backwardOracleEffects = new float[backwardPositions.Length];

        Timeline<BakedLane<MovementWrapTrack, MovementWrapClip>>.Apply(backwardPositions, backwardNext, false, backwardEffects);
        ApplyOracle(backwardOraclePositions, backwardOracleNext, backwardOracleEffects, false, 4, true);
        Assert.Equal(backwardOraclePositions, backwardPositions);
        Assert.Equal(backwardOracleNext, backwardNext);
        Assert.Equal(backwardOracleEffects, backwardEffects);
    }

    [Fact]
    public void BakedLaneColumnApplyGatherMatchesRecordsOracle()
    {
        using var asset = TimelineAsset.LoadAsset(FiniteBake());
        BakedLane<MovementWrapTrack, MovementWrapClip>.Bind(asset);

        var positions = OracleRows(70);
        var next = new ushort[positions.Length];
        var effects = new float[positions.Length];
        var oraclePositions = (ushort[])positions.Clone();
        var oracleNext = new ushort[positions.Length];
        var oracleEffects = new float[positions.Length];

        Timeline<BakedLane<MovementWrapTrack, MovementWrapClip>>.Apply(positions, next, true, effects);
        FiniteApplyOracle(oraclePositions, oracleNext, oracleEffects, true, 9);
        Assert.Equal(oraclePositions, positions);
        Assert.Equal(oracleNext, next);
        Assert.Equal(oracleEffects, effects);

        var backwardPositions = OracleRows(21);
        var backwardNext = new ushort[backwardPositions.Length];
        var backwardEffects = new float[backwardPositions.Length];
        var backwardOraclePositions = (ushort[])backwardPositions.Clone();
        var backwardOracleNext = new ushort[backwardPositions.Length];
        var backwardOracleEffects = new float[backwardPositions.Length];

        Timeline<BakedLane<MovementWrapTrack, MovementWrapClip>>.Apply(backwardPositions, backwardNext, false, backwardEffects);
        FiniteApplyOracle(backwardOraclePositions, backwardOracleNext, backwardOracleEffects, false, 9);
        Assert.Equal(backwardOraclePositions, backwardPositions);
        Assert.Equal(backwardOracleNext, backwardNext);
        Assert.Equal(backwardOracleEffects, backwardEffects);
    }
}
