using Tl;

namespace Tlb;

public readonly struct AlphaClip(int value)
{
    public readonly int Value = value;
}

public readonly struct AutoClip(int value)
{
    public readonly int Value = value;
}

public readonly struct AutoTrack(int code) : IBlend<AutoClip>
{
    public readonly int Code = code;

    public void Blend(in AutoClip first, in AutoClip second, float factor, out AutoClip result) => result = first;
}

public readonly struct AlphaTrack(int code) : IBlend<AlphaClip>
{
    public readonly int Code = code;

    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
}

public readonly struct BlendClip(float amount)
{
    public readonly float Amount = amount;
}

public readonly struct BlendTrack(float scale) : IBlend<BlendClip>
{
    public readonly float Scale = scale;

    public void Blend(in BlendClip first, in BlendClip second, float factor, out BlendClip result)
        => result = new BlendClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct DualAlphaClip(int value)
{
    public readonly int Value = value;
}

public readonly struct DualBetaClip(float amount)
{
    public readonly float Amount = amount;
}

public readonly struct DualTrack(int code) : IBlend<DualAlphaClip>, IBlend<DualBetaClip>
{
    public readonly int Code = code;

    public void Blend(in DualAlphaClip first, in DualAlphaClip second, float factor, out DualAlphaClip result)
        => result = new DualAlphaClip(first.Value + (int)((second.Value - first.Value) * factor));

    public void Blend(in DualBetaClip first, in DualBetaClip second, float factor, out DualBetaClip result)
        => result = new DualBetaClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct EchoClip(int value)
{
    public readonly int Value = value;
}

public readonly struct EchoTrack(int code) : IBlend<EchoClip>
{
    public readonly int Code = code;

    public void Blend(in EchoClip first, in EchoClip second, float factor, out EchoClip result) => result = first;
}

public static class OuterOne
{
    public struct Inner(int value)
    {
        public int Value = value;
    }
}

public static class OuterTwo
{
    public struct Inner(int value)
    {
        public int Value = value;
    }
}

public struct ManagedTrack(string name) : IBlend<AlphaClip>
{
    public string Name = name;

    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
}

public struct ManagedClip
{
    public string Str;
}

public struct NoBlendTrack(int code)
{
    public int Code = code;
}

public static class FixtureHost
{
    public readonly struct HostedClip(float value)
    {
        public readonly float Value = value;
    }

    public readonly struct HostedTrack(int code) : IBlend<HostedClip>
    {
        public readonly int Code = code;

        public void Blend(in HostedClip first, in HostedClip second, float factor, out HostedClip result)
            => result = new HostedClip(first.Value + (second.Value - first.Value) * factor);
    }
}
