using System.Runtime.CompilerServices;
using Tl;

namespace LiveHost;

public readonly struct LiveClip(float value)
{
    public readonly float Value = value;
}

public readonly struct LiveTrack(float scale) : IBlend<LiveClip>
{
    public readonly float Scale = scale;

    public void Blend(in LiveClip first, in LiveClip second, float factor, out LiveClip result)
        => result = new LiveClip(first.Value + (second.Value - first.Value) * factor);
}

public readonly struct ForeignClip(float amount)
{
    public readonly float Amount = amount;
}

public readonly struct ForeignTrack(float scale) : IBlend<ForeignClip>
{
    public readonly float Scale = scale;

    public void Blend(in ForeignClip first, in ForeignClip second, float factor, out ForeignClip result)
        => result = new ForeignClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class LiveHostInstall
{
    [ModuleInitializer]
    internal static void Install() => PairRuntime<LiveTrack, LiveClip>.Consume(&Execute, &Bind);

    static void Bind(ulong* keys, int keyCount, byte* table)
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
        LiveClip scratch = default;
        var current = TickFrame.ToFrame<LiveTrack, LiveClip>(slot, pair, tick, flags, ref scratch);
        var health = (float*)columns[0];
        if (health != null)
            health[row] += current.Clip.Value * current.Track.Scale;
    }
}
