using System.Threading;
using Tl;

namespace Play;

public readonly record struct AmountClip(float Amount);

public readonly record struct ScaleTrack(float Scale) : IBlend<AmountClip>
{
    public void Blend(in AmountClip first, in AmountClip second, float factor, out AmountClip result)
        => result = new AmountClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public static unsafe class PlayRuntime
{
    static int _installed;

    public static void Ensure()
    {
        if (Interlocked.Exchange(ref _installed, 1) == 1) return;
        PairRuntime<ScaleTrack, AmountClip>.Consume(&ExecuteScale, &BindFloat);
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
        AmountClip scratch = default;
        var frame = TickFrame.ToFrame<ScaleTrack, AmountClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
    }
}
