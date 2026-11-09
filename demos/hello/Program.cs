using HelloTl;
using Tl;

namespace HelloTl;

public static class Host
{
    public const int Steps = 150;

    public static List<(int Step, float Offset)> Run(byte[] library)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var player = new Teleported[1];
        var changes = new List<(int, float)>();

        players.Start(0, 1);
        var last = 0f;
        for (var step = 0; step < Steps; step++)
        {
            world.Tick(players, true);
            new Tl.Generated.TlAuto_HelloTl { C0_Teleported = player }.Run(world, players, true);
            if (player[0].Offset != last)
            {
                changes.Add((step, player[0].Offset));
                last = player[0].Offset;
            }
        }
        return changes;
    }

    public static int Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "HelloTl.tlib");
        Console.WriteLine("step  offset");
        foreach (var (step, offset) in Run(File.ReadAllBytes(path)))
            Console.WriteLine($"{step,4}  {offset}");
        return 0;
    }
}
