using System.Globalization;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Tl;

namespace Tl.ValuePoolFormat;

[MemoryDiagnoser]
public class QueryScan
{
    [Params(100_000)]
    public int Rows;

    TimelineComponent _component;
    float[] _effects = new float[16];

    [GlobalSetup]
    public void Setup() => _component = new TimelineComponent(Host.DualBlendAsset.Reference);

    [Benchmark(Baseline = true)]
    public void DualAlpha()
    {
        var component = _component;
        var effects = _effects;
        for (var i = 0; i < Rows; i++)
        {
            component.Position = (ushort)(i & 63);
            foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualAlphaClip>(in component))
                effects[i & 15] += frame.Clip.Value * frame.Track.Code;
        }
        Consume(effects);
    }

    [Benchmark]
    public void BlendWindow()
    {
        var component = _component;
        var effects = _effects;
        for (var i = 0; i < Rows; i++)
        {
            component.Position = (ushort)(i % 12);
            foreach (var frame in Timeline.Query<Tlb.BlendTrack, Tlb.BlendClip>(in component))
                effects[i & 15] += frame.Clip.Amount * frame.Track.Scale;
        }
        Consume(effects);
    }

    [Benchmark]
    public void MixedProgram()
    {
        var component = _component;
        var effects = _effects;
        for (var i = 0; i < Rows; i++)
        {
            component.Position = (ushort)(i & 63);
            foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualAlphaClip>(in component))
                effects[i & 15] += frame.Clip.Value;
            foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualBetaClip>(in component))
                effects[(i + 1) & 15] += frame.Clip.Amount * frame.Track.Code;
            foreach (var frame in Timeline.Query<Tlb.BlendTrack, Tlb.BlendClip>(in component))
                effects[(i + 2) & 15] += frame.Clip.Amount;
        }
        Consume(effects);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(float[] effects)
    {
        for (var i = 0; i < effects.Length; i++)
            Sink.Value += effects[i];
        for (var i = 0; i < effects.Length; i++)
            effects[i] = 0f;
    }
}

[MemoryDiagnoser]
public class LaneApply
{
    [Params(100_000)]
    public int Rows;

    ushort[] _uniform = null!;
    ushort[] _waves = null!;
    ushort[] _staggered = null!;
    float[] _uniformEffects = null!;
    float[] _wavesEffects = null!;
    float[] _staggeredEffects = null!;

    [GlobalSetup]
    public void Setup()
    {
        BakedLane<LaneBench.LaneTrack, LaneBench.LaneClip>.Bind(Host.LaneAsset);
        _uniform = Constant(Rows, 5);
        _waves = Seeds(Rows, 100);
        _staggered = Seeds(Rows, 1);
        _uniformEffects = Effects(Rows);
        _wavesEffects = Effects(Rows);
        _staggeredEffects = Effects(Rows);
    }

    [Benchmark]
    public void UniformForward()
    {
        var positions = _uniform;
        var effects = _uniformEffects;
        Timeline<BakedLane<LaneBench.LaneTrack, LaneBench.LaneClip>>.Advance(positions, true, effects);
        Consume(effects);
    }

    [Benchmark]
    public void WavesForward()
    {
        var positions = _waves;
        var effects = _wavesEffects;
        Timeline<BakedLane<LaneBench.LaneTrack, LaneBench.LaneClip>>.Advance(positions, true, effects);
        Consume(effects);
    }

    [Benchmark]
    public void StaggeredForward()
    {
        var positions = _staggered;
        var effects = _staggeredEffects;
        Timeline<BakedLane<LaneBench.LaneTrack, LaneBench.LaneClip>>.Advance(positions, true, effects);
        Consume(effects);
    }

    [Benchmark]
    public void UniformBackward()
    {
        var positions = _uniform;
        var effects = _uniformEffects;
        Timeline<BakedLane<LaneBench.LaneTrack, LaneBench.LaneClip>>.Advance(positions, false, effects);
        Consume(effects);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(float[] effects)
    {
        var total = 0f;
        for (var i = 0; i < effects.Length; i += 4096)
            total += effects[i];
        Sink.Value += total;
    }

    internal static ushort[] Seeds(int rows, int stride)
    {
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = (ushort)(i / stride % 1024);
        return positions;
    }

    internal static ushort[] Constant(int rows, ushort position)
    {
        var positions = new ushort[rows];
        Array.Fill(positions, position);
        return positions;
    }

    internal static float[] Effects(int rows)
    {
        var effects = new float[rows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < rows; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return effects;
    }
}

public static class Sink
{
    public static float Value;
}

public static class Strings
{
    public static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
