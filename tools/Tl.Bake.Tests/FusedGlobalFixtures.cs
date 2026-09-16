using Tl;

public readonly record struct GaGlobalTrack(float Scale) : IBlend<GaGlobalClip>
{
    public void Blend(in GaGlobalClip first, in GaGlobalClip second, float factor, out GaGlobalClip result)
        => result = new GaGlobalClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public struct GaGlobalClip
{
    public GaGlobalClip(float amount) => Amount = amount;
    public float Amount;
}
