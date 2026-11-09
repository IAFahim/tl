using System.Globalization;
using System.Text;
using Film;
using Tl;

namespace Film;

public static class Host
{
    // Runs timeline number `timeline` (1 = first by name) for `ticks` ticks and returns what is on screen at every tick.
    public static OnScreen[] Run(byte[] library, int timeline, int ticks)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var screen = new OnScreen[1];
        var perTick = new OnScreen[ticks];

        players.Start(0, (ushort)timeline);
        for (var tick = 0; tick < ticks; tick++)
        {
            world.Tick(players, true);
            new Tl.Generated.TlAuto_FilmTl { C0_OnScreen = screen }.Run(world, players, true);
            perTick[tick] = screen[0];
        }
        return perTick;
    }

    // One number per tick for each field, as JSON: {"fps":30,"scene":[...],"phase":[...],"caption":[...],"voice":[...],"footage":[...],"flashes":[...]}
    public static string ToJson(OnScreen[] perTick, int fps)
    {
        var json = new StringBuilder();
        json.Append("{\"fps\":").Append(fps).Append(",\"ticks\":").Append(perTick.Length);
        Column(json, "scene", perTick, s => s.Scene.ToString(CultureInfo.InvariantCulture));
        Column(json, "phase", perTick, s => s.ScenePhase.ToString("0.####", CultureInfo.InvariantCulture));
        Column(json, "caption", perTick, s => s.Caption.ToString(CultureInfo.InvariantCulture));
        Column(json, "voice", perTick, s => s.Voice.ToString(CultureInfo.InvariantCulture));
        Column(json, "footage", perTick, s => s.Footage.ToString(CultureInfo.InvariantCulture));
        Column(json, "flashes", perTick, s => s.Flashes.ToString(CultureInfo.InvariantCulture));
        return json.Append('}').ToString();
    }

    static void Column(StringBuilder json, string name, OnScreen[] perTick, Func<OnScreen, string> field)
        => json.Append(",\"").Append(name).Append("\":[").AppendJoin(',', perTick.Select(field)).Append(']');

    // film <library.tlib> <timeline number> <ticks> <out.json>
    public static int Main(string[] args)
    {
        if (args.Length != 4) { Console.Error.WriteLine("Usage: FilmTl <library.tlib> <timeline number> <ticks> <out.json>"); return 1; }
        var perTick = Run(File.ReadAllBytes(args[0]), int.Parse(args[1]), int.Parse(args[2]));
        File.WriteAllText(args[3], ToJson(perTick, 30));
        Console.WriteLine($"{perTick.Length} ticks, last scene {perTick[^1].Scene}, {perTick[^1].Flashes} flashes");
        return 0;
    }
}
