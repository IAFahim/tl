using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct ConstClip(float Amount);

public readonly record struct ConstTrack(float Scale) : IBlend<ConstClip>
{
    public void Blend(in ConstClip first, in ConstClip second, float factor, out ConstClip result)
        => result = new ConstClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct MirrorClip(float Amount);

public readonly record struct MirrorTrack(float Scale) : IBlend<MirrorClip>
{
    public void Blend(in MirrorClip first, in MirrorClip second, float factor, out MirrorClip result)
        => result = new MirrorClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct LyingClip(float Amount);

public readonly record struct LyingTrack(float Scale) : IBlend<LyingClip>
{
    public void Blend(in LyingClip first, in LyingClip second, float factor, out LyingClip result)
        => result = new LyingClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct TickClip(float Amount);

public readonly record struct TickTrack(float Scale) : IBlend<TickClip>
{
    public void Blend(in TickClip first, in TickClip second, float factor, out TickClip result)
        => result = new TickClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct ChainClip(float Amount);

public readonly record struct ChainTrack(float Scale) : IBlend<ChainClip>
{
    public void Blend(in ChainClip first, in ChainClip second, float factor, out ChainClip result)
        => result = new ChainClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct TChainClip(float Amount);

public readonly record struct TChainTrack(float Scale) : IBlend<TChainClip>
{
    public void Blend(in TChainClip first, in TChainClip second, float factor, out TChainClip result)
        => result = new TChainClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class WindowPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<ConstTrack, ConstClip>.Consume(&ConstExecute, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<ConstTrack, ConstClip>.Consume(&ConstOffsetExecute, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<MirrorTrack, MirrorClip>.Consume(&MirrorExecute, &BindFloat);
        PairRuntime<MirrorTrack, MirrorClip>.Consume(&MirrorOffsetExecute, &BindFloat);
        PairRuntime<LyingTrack, LyingClip>.Consume(&LyingExecute, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<TickTrack, TickClip>.Consume(&TickExecute, &BindFloat);
        PairRuntime<ChainTrack, ChainClip>.Consume(&ChainScale, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<ChainTrack, ChainClip>.Consume(&ChainOne, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<ChainTrack, ChainClip>.Consume(&ChainOneB, &BindFloat, TickPurity.WindowConstant);
        PairRuntime<TChainTrack, TChainClip>.Consume(&TChainScale, &BindFloat);
        PairRuntime<TChainTrack, TChainClip>.Consume(&TChainOne, &BindFloat);
        PairRuntime<TChainTrack, TChainClip>.Consume(&TChainOneB, &BindFloat);
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

    private static void ConstExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ConstClip scratch = default;
        var frame = TickFrame.ToFrame<ConstTrack, ConstClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ConstOffsetExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 3.5f;

    private static void MirrorExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        MirrorClip scratch = default;
        var frame = TickFrame.ToFrame<MirrorTrack, MirrorClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void MirrorOffsetExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 3.5f;

    private static void LyingExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LyingClip scratch = default;
        var frame = TickFrame.ToFrame<LyingTrack, LyingClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.TimelineTick * frame.Track.Scale;
    }

    private static void TickExecute(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        TickClip scratch = default;
        var frame = TickFrame.ToFrame<TickTrack, TickClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.TimelineTick * frame.Track.Scale;
    }

    private static void ChainScale(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ChainClip scratch = default;
        var frame = TickFrame.ToFrame<ChainTrack, ChainClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ChainOne(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 1f;

    private static void ChainOneB(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 1f;

    private static void TChainScale(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        TChainClip scratch = default;
        var frame = TickFrame.ToFrame<TChainTrack, TChainClip>(slot, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void TChainOne(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 1f;

    private static void TChainOneB(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] += 1f;
}

public static unsafe class WindowConstantBitwise
{
    internal static bool TablesEqual(MeasuredLanes expected, MeasuredLanes actual)
    {
        var length = (int)(expected.Duration + 1u) * sizeof(float);
        return new ReadOnlySpan<byte>(expected.Forward, length).SequenceEqual(new ReadOnlySpan<byte>(actual.Forward, length))
            && new ReadOnlySpan<byte>(expected.Backward, length).SequenceEqual(new ReadOnlySpan<byte>(actual.Backward, length));
    }

    internal static float TableValue(MeasuredLanes lanes, bool forward, uint tick) => (forward ? lanes.Forward : lanes.Backward)[tick];
}

public class WindowConstantTests
{
    [Fact]
    public void DeclaredWindowConstantMatchesPerTickAcrossDurations()
    {        foreach (var duration in new uint[] { 1, 2, 63, 64, 65 })
        foreach (var looping in new[] { true, false })
        {
            var declared = TimelineAsset.Load(Bake<ConstTrack, ConstClip>(new ConstTrack(2f), duration, looping, (0, duration)));
            var mirror = TimelineAsset.Load(Bake<MirrorTrack, MirrorClip>(new MirrorTrack(2f), duration, looping, (0, duration)));
            try
            {
                using var declaredLanes = MeasuredLanes.Measure(declared);
                using var mirrorLanes = MeasuredLanes.Measure(mirror);
                Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
            }
            finally
            {
                declared.Dispose();
                mirror.Dispose();
            }
        }
    }

    [Fact]
    public void DeclaredWindowConstantMatchesPerTickWithMultipleStages()
    {
        foreach (var looping in new[] { true, false })
        {
            var declared = TimelineAsset.Load(Bake<ConstTrack, ConstClip>(new ConstTrack(1.5f), 100, looping, (0, 1), (1, 64), (64, 65), (65, 100)));
            var mirror = TimelineAsset.Load(Bake<MirrorTrack, MirrorClip>(new MirrorTrack(1.5f), 100, looping, (0, 1), (1, 64), (64, 65), (65, 100)));
            try
            {
                using var declaredLanes = MeasuredLanes.Measure(declared);
                using var mirrorLanes = MeasuredLanes.Measure(mirror);
                Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
            }
            finally
            {
                declared.Dispose();
                mirror.Dispose();
            }
        }
    }

    [Fact]
    public void DeclaredWindowConstantMatchesPerTickAtLargeDuration()
    {
        var declared = TimelineAsset.Load(Bake<ConstTrack, ConstClip>(new ConstTrack(1f), 65535, true, (0, 65535)));
        var mirror = TimelineAsset.Load(Bake<MirrorTrack, MirrorClip>(new MirrorTrack(1f), 65535, true, (0, 65535)));
        try
        {
            using var declaredLanes = MeasuredLanes.Measure(declared);
            using var mirrorLanes = MeasuredLanes.Measure(mirror);
            Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
        }
        finally
        {
            declared.Dispose();
            mirror.Dispose();
        }
    }

    [Fact]
    public void CrossfadeWindowsStayPerTickBitExact()
    {
        foreach (var looping in new[] { true, false })
        {
            var declared = TimelineAsset.Load(Bake<ConstTrack, ConstClip>(new ConstTrack(2f), 12, looping, (0, 8), (4, 12)));
            var mirror = TimelineAsset.Load(Bake<MirrorTrack, MirrorClip>(new MirrorTrack(2f), 12, looping, (0, 8), (4, 12)));
            try
            {
                using var declaredLanes = MeasuredLanes.Measure(declared);
                using var mirrorLanes = MeasuredLanes.Measure(mirror);
                Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
                Assert.NotEqual(
                    WindowConstantBitwise.TableValue(mirrorLanes, true, 6),
                    WindowConstantBitwise.TableValue(mirrorLanes, true, 5));
            }
            finally
            {
                declared.Dispose();
                mirror.Dispose();
            }
        }
    }

    [Fact]
    public void MixedDeclaredAndUndeclaredStagesMatchPerTick()
    {
        foreach (var looping in new[] { true, false })
        {
            var declared = BakeTwo<ConstTrack, ConstClip, MirrorTrack, MirrorClip>(
                new ConstTrack(2f), new MirrorTrack(3f), 40, looping,
                new[] { (0u, 10u), (10u, 40u) }, new[] { (5u, 20u), (20u, 40u) });
            var mirror = BakeTwo<MirrorTrack, MirrorClip, MirrorTrack, MirrorClip>(
                new MirrorTrack(2f), new MirrorTrack(3f), 40, looping,
                new[] { (0u, 10u), (10u, 40u) }, new[] { (5u, 20u), (20u, 40u) });
            var declaredAsset = TimelineAsset.Load(declared);
            var mirrorAsset = TimelineAsset.Load(mirror);
            try
            {
                using var declaredLanes = MeasuredLanes.Measure(declaredAsset);
                using var mirrorLanes = MeasuredLanes.Measure(mirrorAsset);
                Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
            }
            finally
            {
                declaredAsset.Dispose();
                mirrorAsset.Dispose();
            }
        }
    }

    [Fact]
    public void SamePairTwoTracksDeclaredMatchesPerTick()
    {
        foreach (var looping in new[] { true, false })
        {
            var declared = BakeTwo<ConstTrack, ConstClip, ConstTrack, ConstClip>(
                new ConstTrack(2f), new ConstTrack(3f), 40, true,
                new[] { (0u, 10u), (10u, 40u) }, new[] { (5u, 20u), (20u, 40u) });
            var mirror = BakeTwo<MirrorTrack, MirrorClip, MirrorTrack, MirrorClip>(
                new MirrorTrack(2f), new MirrorTrack(3f), 40, true,
                new[] { (0u, 10u), (10u, 40u) }, new[] { (5u, 20u), (20u, 40u) });
            var declaredAsset = TimelineAsset.Load(declared);
            var mirrorAsset = TimelineAsset.Load(mirror);
            try
            {
                using var declaredLanes = MeasuredLanes.Measure(declaredAsset);
                using var mirrorLanes = MeasuredLanes.Measure(mirrorAsset);
                Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
            }
            finally
            {
                declaredAsset.Dispose();
                mirrorAsset.Dispose();
            }
        }
    }

    [Fact]
    public void LyingDeclarationFillsWindowFirstTickDeterministically()
    {
        var lying = TimelineAsset.Load(Bake<LyingTrack, LyingClip>(new LyingTrack(1f), 12, false, (2, 10)));
        var honest = TimelineAsset.Load(Bake<TickTrack, TickClip>(new TickTrack(1f), 12, false, (2, 10)));
        try
        {
            using var lyingLanes = MeasuredLanes.Measure(lying);
            using var honestLanes = MeasuredLanes.Measure(honest);
            for (var tick = 2u; tick < 10u; tick++)
            {
                Assert.Equal(2f, WindowConstantBitwise.TableValue(lyingLanes, true, tick));
                Assert.Equal(2f, WindowConstantBitwise.TableValue(lyingLanes, false, tick));
            }
            Assert.Equal(0f, WindowConstantBitwise.TableValue(lyingLanes, true, 0));
            Assert.NotEqual(WindowConstantBitwise.TableValue(honestLanes, true, 9), WindowConstantBitwise.TableValue(lyingLanes, true, 9));
        }
        finally
        {
            lying.Dispose();
            honest.Dispose();
        }
    }

    [Fact]
    public void DeclaredChainReplayKeepsDirectionalAccumulationOrder()
    {
        var chainBake = new Baker()
            .Track<ChainTrack, ChainClip>(new ChainTrack(1f))
            .Clip(0, 0u, 8u, new ChainClip(16777216f))
            .Looping()
            .Bake();
        var mirrorBake = new Baker()
            .Track<TChainTrack, TChainClip>(new TChainTrack(1f))
            .Clip(0, 0u, 8u, new TChainClip(16777216f))
            .Looping()
            .Bake();
        var chain = TimelineAsset.Load(chainBake);
        var mirror = TimelineAsset.Load(mirrorBake);
        try
        {
            using var chainLanes = MeasuredLanes.Measure(chain);
            using var mirrorLanes = MeasuredLanes.Measure(mirror);
            Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, chainLanes));
            Assert.NotEqual(
                WindowConstantBitwise.TableValue(mirrorLanes, true, 4),
                WindowConstantBitwise.TableValue(mirrorLanes, false, 4));
        }
        finally
        {
            chain.Dispose();
            mirror.Dispose();
        }
    }

    [Fact]
    public void DeclaredWindowConstantMatchesPerTickWithEmptyAsset()
    {
        var declared = Bake<ConstTrack, ConstClip>(new ConstTrack(1f), 0, false);
        var mirror = Bake<MirrorTrack, MirrorClip>(new MirrorTrack(1f), 0, false);
        var declaredAsset = TimelineAsset.Load(declared);
        var mirrorAsset = TimelineAsset.Load(mirror);
        try
        {
            using var declaredLanes = MeasuredLanes.Measure(declaredAsset);
            using var mirrorLanes = MeasuredLanes.Measure(mirrorAsset);
            Assert.True(WindowConstantBitwise.TablesEqual(mirrorLanes, declaredLanes));
        }
        finally
        {
            declaredAsset.Dispose();
            mirrorAsset.Dispose();
        }
    }

    static byte[] Bake<TTrack, TClip>(TTrack track, uint duration, bool looping, params (uint Start, uint End)[] clips)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        var baker = new Baker().Track<TTrack, TClip>(track);
        foreach (var (start, end) in clips)
            baker.Clip(0, start, end, ClipOf<TClip>(start, end));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static byte[] BakeTwo<TTrack0, TClip0, TTrack1, TClip1>(
        TTrack0 track0, TTrack1 track1, uint duration, bool looping,
        (uint Start, uint End)[] clips0, (uint Start, uint End)[] clips1)
        where TTrack0 : unmanaged, IBlend<TClip0>
        where TClip0 : unmanaged
        where TTrack1 : unmanaged, IBlend<TClip1>
        where TClip1 : unmanaged
    {
        var baker = new Baker()
            .Track<TTrack0, TClip0>(track0)
            .Track<TTrack1, TClip1>(track1);
        foreach (var (start, end) in clips0)
            baker.Clip(0, start, end, ClipOf<TClip0>(start, end));
        foreach (var (start, end) in clips1)
            baker.Clip(1, start, end, ClipOf<TClip1>(start, end));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static TClip ClipOf<TClip>(uint start, uint end) where TClip : unmanaged
    {
        if (typeof(TClip) == typeof(ConstClip)) return (TClip)(object)new ConstClip((start + end) * 0.5f);
        if (typeof(TClip) == typeof(MirrorClip)) return (TClip)(object)new MirrorClip((start + end) * 0.5f);
        if (typeof(TClip) == typeof(LyingClip)) return (TClip)(object)new LyingClip((start + end) * 0.5f);
        if (typeof(TClip) == typeof(TickClip)) return (TClip)(object)new TickClip((start + end) * 0.5f);
        throw new NotSupportedException();
    }
}
