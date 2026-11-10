using HelloAbility;
using Tl;

namespace HelloAbility;

public static class Host
{
    public const int Steps = 150;

    public static (List<(int Step, bool CanDoubleJump)> Windowed, bool NeverPlayed) Run(byte[] library)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(2);
        using var kinds = new KindStore(world, players);
        var abilities = new Abilities[2];
        var windowedPlayer = 0;
        var playerWhoNeverPlays = 1;
        var changes = new List<(int, bool)>();

        players.Start(windowedPlayer, 1);
        var last = false;
        for (var step = 0; step < Steps; step++)
        {
            world.Tick(players, true);
            new Tl.Generated.TlAuto_HelloAbility { C0_Abilities = abilities }.Run(world, players, true);
            if (abilities[windowedPlayer].CanDoubleJump != last)
            {
                last = abilities[windowedPlayer].CanDoubleJump;
                changes.Add((step, last));
            }
        }
        return (changes, abilities[playerWhoNeverPlays].CanDoubleJump);
    }

    public static int Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "HelloAbility.tlib");
        var (changes, neverPlayed) = Run(File.ReadAllBytes(path));
        Console.WriteLine("step  can double jump");
        Console.WriteLine("   0  False");
        foreach (var (step, canDoubleJump) in changes)
            Console.WriteLine($"{step,4}  {canDoubleJump}");
        Console.WriteLine($"a player who never plays the timeline: {neverPlayed}");
        return 0;
    }
}
