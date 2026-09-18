using System.IO;
using Tl;

namespace Fresh;

public static class Program
{
    public static void Main()
    {
        ushort boss = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        var health = new Health { Value = 100f };
        Timeline<DamageTrack, DamageClip>.Advance(boss, positions, true, effects);
        Timeline<DamageTrack, DamageClip>.Advance(boss, positions, true, effects);
        health.Value -= effects[0];
        System.Console.WriteLine($"flawless: health={health.Value} position={positions[0]}");
    }
}
