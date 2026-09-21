using Tl;

namespace Fresh;

public static class Program
{
    public static int Main()
    {
        ushort boss = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        var health = new Health { Value = 100f };
        Timeline<DamageTrack, DamageClip>.Apply(boss, positions, true, effects);
        Timeline.Advance(boss, positions, true);
        Timeline<DamageTrack, DamageClip>.Apply(boss, positions, true, effects);
        Timeline.Advance(boss, positions, true);
        health.Value -= effects[0];
        System.Console.WriteLine($"flawless: health={health.Value} position={positions[0]}");
        if (health.Value != 80f || positions[0] != 2)
            return 1;
        return 0;
    }
}
