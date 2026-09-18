using System.IO;
using Tl;

namespace Fresh;

public static class Program
{
    public static void Main()
    {
        using var asset = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
        BakedLane<DamageTrack, DamageClip>.Bind(asset);
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        var health = new Health { Value = 100f };
        Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(effects);
        Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(effects);
        health.Value -= effects[0];
        System.Console.WriteLine($"flawless: health={health.Value} position={positions[0]}");
    }
}
