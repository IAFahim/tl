using System.IO;
using Tl;

namespace Fresh;

public static class Program
{
    public static void Main()
    {
        using var asset = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new[] { new Health { Value = 100f } };
        var query = Timeline.Rows(rows).Write(health);
        query.Tick(200_000u, 2);
        System.Console.WriteLine($"flawless: health={health[0].Value} position={rows[0].Position}");
    }
}
