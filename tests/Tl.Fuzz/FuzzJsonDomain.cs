using Tl;

namespace FuzzDomain
{
    public readonly record struct FuzzJsonClip(float Value);

    public readonly record struct FuzzJsonTrack(float Scale) : IBlend<FuzzJsonClip>
    {
        public void Blend(in FuzzJsonClip first, in FuzzJsonClip second, float factor, out FuzzJsonClip result)
            => result = new FuzzJsonClip(first.Value + (second.Value - first.Value) * factor);
    }
}
