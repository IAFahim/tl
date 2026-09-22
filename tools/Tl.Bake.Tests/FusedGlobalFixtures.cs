using Tl;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct GaGlobalTrack(float Scale) : IBlend<GaGlobalClip>
{
    public void Blend(in GaGlobalClip first, in GaGlobalClip second, float factor, out GaGlobalClip result)
        => result = new GaGlobalClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public struct GaGlobalClip(float amount)
{
    public float Amount = amount;
}
