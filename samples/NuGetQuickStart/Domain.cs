using Tl;

namespace Fresh;

public partial struct Health { public float Value; }
public readonly struct DamageClip { public readonly float Amount; public DamageClip(float amount) => Amount = amount; }
public readonly struct DamageTrack : IBlend<DamageClip>
{
    public readonly float Multiplier;
    public DamageTrack(float multiplier) => Multiplier = multiplier;
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
}
public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
{
    public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float effect)
        => effect += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}
