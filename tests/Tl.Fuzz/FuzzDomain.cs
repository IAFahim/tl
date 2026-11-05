namespace Tl.Fuzz;

public readonly record struct FuzzClip(float Value);

public readonly record struct FuzzTrack(float Scale) : Tl.IBlend<FuzzClip>
{
    public void Blend(in FuzzClip first, in FuzzClip second, float factor, out FuzzClip result)
        => result = new FuzzClip(first.Value + (second.Value - first.Value) * factor);
}
