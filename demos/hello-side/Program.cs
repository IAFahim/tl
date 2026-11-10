using HelloSide;
using Tl;

namespace HelloSide;

public static class Host
{
    public const int Steps = 150;

    public static List<(int Step, float Offset, float Side)> Run(byte[] library)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var player = new Teleported[1];
        var changes = new List<(int, float, float)>();

        players.Start(0, 1);
        var last = 0f; var lastSide = 0f;
        for (var step = 0; step < Steps; step++)
        {
            world.Tick(players, true);
            new Tl.Generated.TlAuto_HelloSide { C0_Teleported = player }.Run(world, players, true);
            if (player[0].Offset != last || player[0].SideSwing != lastSide)
            {
                changes.Add((step, player[0].Offset, player[0].SideSwing));
                last = player[0].Offset; lastSide = player[0].SideSwing;
            }
        }
        return changes;
    }

    public static int Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "HelloSide.tlib");
        Console.WriteLine("step  offset");
        foreach (var (step, offset, side) in Run(File.ReadAllBytes(path)))
            Console.WriteLine($"{step,4}  {offset}  {side}");
        return 0;
    }
}
