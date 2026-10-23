using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Tl.TestSupport;

namespace Tl.Core.Tests;

public readonly record struct OracleClip(float Amount);

public readonly record struct OracleTrack(float Scale) : IBlend<OracleClip>
{
    public void Blend(in OracleClip first, in OracleClip second, float factor, out OracleClip result)
        => result = new OracleClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class OracleKernelPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<OracleTrack, OracleClip>.Consume(&ExecuteScale, &BindFloat, "tl\0OracleKernelScale");
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        OracleClip scratch = default;
        var frame = TickFrame.ToFrame<OracleTrack, OracleClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Track.Scale * frame.Clip.Amount;
    }
}

internal enum OracleShape { Standard, Gap, Blend }

internal sealed class OracleFixture
{
    internal readonly byte[] Bytes;
    internal readonly ushort Duration;
    internal readonly bool Looping;
    internal readonly ushort Index;
    internal readonly SlotView View;
    readonly TimelineAsset _lease;
    readonly float[] _forwardByPosition;
    readonly float[] _backwardByPosition;
    readonly bool[] _sampledForward;
    readonly bool[] _sampledBackward;

    internal OracleFixture(ushort duration, bool looping, OracleShape shape, float scale)
    {
        Duration = duration;
        Looping = looping;
        Bytes = Bake(duration, looping, shape, scale);
        Index = TimelineAsset.Load(Bytes);
        _lease = TimelineAsset.Of(Index);
        View = Timeline<OracleTrack, OracleClip>.View(Index);
        var legal = (int)duration + 1;
        _forwardByPosition = new float[legal];
        _backwardByPosition = new float[legal];
        _sampledForward = new bool[legal];
        _sampledBackward = new bool[legal];
        for (var p = 0; p < legal; p++)
        {
            _forwardByPosition[p] = Fold((ushort)p, true, out _sampledForward[p]);
            _backwardByPosition[p] = Fold((ushort)p, false, out _sampledBackward[p]);
        }
    }

    internal OracleStep Step(bool forward, ushort position) => PlaybackOracle.Step(Duration, Looping, forward, position);

    internal ushort Next(ushort position, bool forward)
    {
        var step = Step(forward, position);
        return step.Crossed ? step.Next : position;
    }

    internal float Effect(ushort position, bool forward) => forward ? _forwardByPosition[position] : _backwardByPosition[position];

    internal bool Sampled(ushort position, bool forward) => forward ? _sampledForward[position] : _sampledBackward[position];

    float Fold(ushort position, bool forward, out bool sampled)
    {
        var scratch = default(OracleClip);
        if (!Timeline<OracleTrack, OracleClip>.TrySample(Index, position, ref scratch, out var frame, forward))
        {
            sampled = false;
            return 0f;
        }
        sampled = true;
        return 0f + ((frame.Has(FrameFlags.Reverse) ? -1f : 1f) * frame.Track.Scale * frame.Clip.Amount);
    }

    static byte[] Bake(ushort duration, bool looping, OracleShape shape, float scale)
    {
        var baker = new DomainBaker().Track<OracleTrack, OracleClip>(new OracleTrack(scale));
        if (duration == 1)
            baker.Clip(0, 0u, 1u, new OracleClip(1.25f));
        else if (duration > 1)
            foreach (var clip in Clips(duration, shape))
                baker.Clip(0, clip.Start, clip.End, new OracleClip(clip.Amount));
        if (looping) baker.Looping();
        return baker.Bake();
    }

    static (uint Start, uint End, float Amount)[] Clips(ushort duration, OracleShape shape)
    {
        var d = (uint)duration;
        var mid = Math.Max(1u, d * 6 / 10);
        return shape switch
        {
            OracleShape.Gap => new (uint, uint, float)[] { (0u, d / 4, 1.25f), (d - d / 4, d, -0.5f) },
            OracleShape.Blend => new (uint, uint, float)[] { (0u, d * 3 / 4, 1.25f), (d / 4, d, -0.5f) },
            _ => new (uint, uint, float)[] { (0u, mid, 1.25f), (mid, d, -0.5f) },
        };
    }
}

internal static class OracleFixtures
{
    static readonly ConcurrentDictionary<(int Duration, bool Looping, int Shape, float Scale), OracleFixture> Cache = new();

    internal static OracleFixture Standard(ushort duration, bool looping, float scale = 2f)
        => Get(duration, looping, OracleShape.Standard, scale);

    internal static OracleFixture Get(ushort duration, bool looping, OracleShape shape, float scale)
        => Cache.GetOrAdd((duration, looping, (int)shape, scale),
            key => new OracleFixture((ushort)key.Duration, key.Looping, (OracleShape)key.Shape, key.Scale));
}
