using System.Runtime.InteropServices;

namespace Tl.ValuePooling;

[StructLayout(LayoutKind.Sequential)]
public struct FloatTrack
{
    public float Multiplier;
    public float Bias;

    public FloatTrack(float multiplier, float bias)
    {
        Multiplier = multiplier;
        Bias = bias;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FloatClip
{
    public float Amount;

    public FloatClip(float amount) => Amount = amount;
}

[StructLayout(LayoutKind.Sequential)]
public struct InlineSlot
{
    public FloatTrack Track;
    public FloatClip First, Second;
    public uint WindowStart, WindowEnd, FactorStart, FactorSpan;
    public byte TrackIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct PooledByteSlot
{
    public uint WindowStart, WindowEnd, FactorStart, FactorSpan;
    public byte MultiplierIndex, BiasIndex, FirstIndex, SecondIndex;
    public byte TrackIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct PooledUshortSlot
{
    public uint WindowStart, WindowEnd, FactorStart, FactorSpan;
    public ushort MultiplierIndex, BiasIndex, FirstIndex, SecondIndex;
    public byte TrackIndex;
}

public static class Layout
{
    public const int MaxBytePool = 256;

    public static int SizeOf<T>() where T : struct => Marshal.SizeOf<T>();

    public static int SlotsPerLine<T>() where T : struct
    {
        var size = Marshal.SizeOf<T>();
        return 64 / size;
    }

    public static double LineDensity<T>() where T : struct
    {
        var size = Marshal.SizeOf<T>();
        return 64.0 / size;
    }
}
