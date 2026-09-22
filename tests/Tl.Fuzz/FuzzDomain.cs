
using FuzzDomain;
namespace Tl.Fuzz;

public readonly record struct FuzzClip(float Value);

public readonly record struct FuzzTrack(float Scale) : IBlend<FuzzClip>
{
    public void Blend(in FuzzClip first, in FuzzClip second, float factor, out FuzzClip result)
        => result = new FuzzClip(first.Value + (second.Value - first.Value) * factor);
}

public static unsafe class FuzzPairs
{
    static bool _installed;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;
        PairRuntime<FuzzTrack, FuzzClip>.Consume(&Execute, &BindFloat);
        PairRuntime<FuzzJsonTrack, FuzzJsonClip>.Consume(&ExecuteJson, &BindFloat);
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

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        FuzzClip scratch = default;
        var frame = TickFrame.ToFrame<FuzzTrack, FuzzClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Value * frame.Track.Scale;
    }

    static void ExecuteJson(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        FuzzJsonClip scratch = default;
        var frame = TickFrame.ToFrame<FuzzJsonTrack, FuzzJsonClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Value * frame.Track.Scale;
    }
}

public interface IDurationShape
{
    static abstract ushort Duration { get; }
}

public interface ILoopShape
{
    static abstract bool Looping { get; }
}

public static class FuzzFx
{
    public static float Effect(ushort position) => position % 251 * 0.25f - 8f;

    public static float Inverse(ushort tick) => -(FuzzFx.Effect(tick) * 0.5f);
}

public readonly struct LaneOf<TD, TL> : ITimelineLane<LaneOf<TD, TL>>
    where TD : IDurationShape
    where TL : ILoopShape
{
    public static ushort Duration => TD.Duration;

    public static bool Looping => TL.Looping;

    public static float Effect(ushort position) => FuzzFx.Effect(position);

    public static float InverseEffect(ushort position) => FuzzFx.Inverse(position);
}
