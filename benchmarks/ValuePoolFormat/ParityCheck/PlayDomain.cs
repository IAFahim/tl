using Tl;

namespace Play
{
    public readonly record struct AmountClip(float Amount);

    public readonly record struct ScaleTrack(float Scale) : IBlend<AmountClip>
    {
        public void Blend(in AmountClip first, in AmountClip second, float factor, out AmountClip result)
            => result = new AmountClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}
