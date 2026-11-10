using System.Globalization;
using System.Text;
using Film;
using Tl;

namespace Film;

public static unsafe class Host
{
    // Runs timeline number `timeline` (1 = first by name) for `ticks` ticks and returns what is on screen at every tick.
    public static OnScreen[] Run(byte[] library, int timeline, int ticks)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var screen = new OnScreen[1];
        var perTick = new OnScreen[ticks];

        var scenes = timeline == 2 ? TutorialScenes : ReleaseScenes;
        var captions = timeline == 2 ? TutorialCaptions : ReleaseCaptions;
        var footages = timeline == 2 ? TutorialFootages : ReleaseFootages;
        var reveals = timeline == 2 ? TutorialReveals : ReleaseReveals;
        var sweeps = timeline == 2 ? TutorialSweeps : ReleaseSweeps;
        var voices = timeline == 2 ? TutorialVoices : ReleaseVoices;
        var beats = timeline == 2 ? TutorialBeats : ReleaseBeats;

        players.Start(0, (ushort)timeline);
        for (var tick = 0; tick < ticks; tick++)
        {
            world.Tick(players, true);
            var at = players.View.SlotOf(0, 0)->Tick;
            ref var current = ref screen[0];
            Scenes(at, scenes, ref current);
            Captions(at, captions, ref current);
            Footage(at, footages, ref current);
            Reveals(at, reveals, ref current);
            Sweeps(at, sweeps, ref current);
            Voices(at, voices, ref current);
            Beats(at, beats, ref current);
            perTick[tick] = current;
        }
        return perTick;
    }

    static void Scenes(ushort tick, (ushort Start, ushort End, int Scene)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick >= c.End) continue;
            var frame = new TlFrame<SceneClip>(new Frame<TlNoSettings<SceneClip>, SceneClip>(default, new SceneClip { Scene = c.Scene }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var scene = SceneTrack.WhichScene(in frame);
            ShowScene.Apply(in scene, ref screen);
            var phase = SceneTrack.HowFarThroughTheScene(in frame);
            ScenePhase.Apply(in phase, ref screen);
            return;
        }
    }

    static void Captions(ushort tick, (ushort Start, ushort End, int Line)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick >= c.End) continue;
            var frame = new TlFrame<CaptionClip>(new Frame<TlNoSettings<CaptionClip>, CaptionClip>(default, new CaptionClip { Line = c.Line }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var line = CaptionTrack.WhichLine(in frame);
            ShowCaption.Apply(in line, ref screen);
            return;
        }
    }

    static void Footage(ushort tick, (ushort Start, ushort End, int Footage)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick >= c.End) continue;
            var frame = new TlFrame<FootageClip>(new Frame<TlNoSettings<FootageClip>, FootageClip>(default, new FootageClip { Footage = c.Footage }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var footage = FootageTrack.WhichFootage(in frame);
            ShowFootage.Apply(in footage, ref screen);
            var within = FootageTrack.TickInTheClip(in frame);
            ShowFootageTick.Apply(in within, ref screen);
            return;
        }
    }

    static void Reveals(ushort tick, (ushort Start, ushort End, uint Mask)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick >= c.End) continue;
            var frame = new TlFrame<RevealClip>(new Frame<TlNoSettings<RevealClip>, RevealClip>(default, new RevealClip { Mask = c.Mask }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var mask = RevealTrack.WhichElements(in frame);
            ShowElements.Apply(in mask, ref screen);
            return;
        }
    }

    static void Sweeps(ushort tick, (ushort Start, ushort End, float From, float To)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick >= c.End) continue;
            var frame = new TlFrame<SweepClip>(new Frame<TlNoSettings<SweepClip>, SweepClip>(default, new SweepClip { From = c.From, To = c.To }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var value = SweepTrack.Glide(in frame);
            MoveSweep.Apply(in value, ref screen);
            return;
        }
    }

    static void Voices(ushort tick, (ushort Start, ushort End, int Line)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick != c.Start) continue;
            var frame = new TlFrame<VoiceClip>(new Frame<TlNoSettings<VoiceClip>, VoiceClip>(default, new VoiceClip { Line = c.Line }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var line = VoiceTrack.LineThatStartsNow(in frame);
            StartVoice.Apply(in line, ref screen);
        }
    }

    static void Beats(ushort tick, (ushort Start, ushort End, int Weight)[] clips, ref OnScreen screen)
    {
        for (var i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (tick < c.Start) return;
            if (tick != c.Start) continue;
            var frame = new TlFrame<BeatClip>(new Frame<TlNoSettings<BeatClip>, BeatClip>(default, new BeatClip { Weight = c.Weight }, tick, (ushort)(c.End - c.Start), (ushort)(tick - c.Start), 0, FrameFlags.None));
            var weight = BeatTrack.FlashWeight(in frame);
            Flash.Apply(in weight, ref screen);
        }
    }

    static readonly (ushort Start, ushort End, int Scene)[] ReleaseScenes =
    [
        (0, 328, 1),
        (328, 776, 2),
        (776, 1149, 3),
        (1149, 1598, 4),
        (1598, 1979, 5),
        (1979, 2439, 6),
        (2439, 2878, 7),
        (2878, 3272, 8),
        (3272, 3735, 9),
        (3735, 4198, 10),
        (4198, 4622, 11),
        (4622, 4997, 12),
        (4997, 5296, 13),
    ];

    static readonly (ushort Start, ushort End, int Line)[] ReleaseCaptions =
    [
        (0, 23, 0),
        (23, 52, 1),
        (52, 188, 2),
        (188, 243, 3),
        (243, 328, 4),
        (328, 351, 0),
        (351, 439, 5),
        (439, 504, 6),
        (504, 576, 7),
        (576, 644, 8),
        (644, 698, 9),
        (698, 776, 10),
        (776, 797, 0),
        (797, 842, 11),
        (842, 890, 12),
        (890, 1016, 13),
        (1016, 1091, 14),
        (1091, 1149, 15),
        (1149, 1172, 0),
        (1172, 1197, 16),
        (1197, 1274, 17),
        (1274, 1343, 18),
        (1343, 1462, 19),
        (1462, 1507, 20),
        (1507, 1598, 21),
        (1598, 1621, 0),
        (1621, 1695, 22),
        (1695, 1763, 23),
        (1763, 1979, 24),
        (1979, 2002, 0),
        (2002, 2035, 25),
        (2035, 2171, 26),
        (2171, 2206, 27),
        (2206, 2347, 28),
        (2347, 2439, 29),
        (2439, 2462, 0),
        (2462, 2494, 30),
        (2494, 2643, 31),
        (2643, 2728, 32),
        (2728, 2878, 33),
        (2878, 2902, 0),
        (2902, 2930, 34),
        (2930, 2992, 35),
        (2992, 3163, 36),
        (3163, 3272, 37),
        (3272, 3295, 0),
        (3295, 3319, 38),
        (3319, 3488, 39),
        (3488, 3638, 40),
        (3638, 3735, 41),
        (3735, 3758, 0),
        (3758, 3783, 42),
        (3783, 3827, 43),
        (3827, 3997, 44),
        (3997, 4198, 45),
        (4198, 4219, 0),
        (4219, 4257, 46),
        (4257, 4411, 47),
        (4411, 4622, 48),
        (4622, 4646, 0),
        (4646, 4714, 49),
        (4714, 4874, 50),
        (4874, 4913, 51),
        (4913, 4948, 52),
        (4948, 4997, 53),
        (4997, 5021, 0),
        (5021, 5048, 54),
        (5048, 5140, 55),
        (5140, 5296, 56),
    ];

    static readonly (ushort Start, ushort End, int Footage)[] ReleaseFootages =
    [
        (0, 2454, 0),
        (2454, 2857, 1),
        (2857, 2893, 0),
        (2893, 3251, 2),
        (3251, 3287, 0),
        (3287, 3714, 3),
        (3714, 3750, 0),
        (3750, 4177, 4),
        (4177, 4213, 0),
        (4213, 4601, 5),
        (4601, 5326, 0),
    ];

    static readonly (ushort Start, ushort End, uint Mask)[] ReleaseReveals =
    [
        (0, 328, 3u),
        (328, 439, 0u),
        (439, 504, 1u),
        (504, 576, 3u),
        (576, 644, 7u),
        (644, 698, 15u),
        (698, 776, 31u),
        (776, 797, 0u),
        (797, 842, 0u),
        (842, 890, 1u),
        (890, 1016, 2u),
        (1016, 1091, 12u),
        (1091, 1149, 15u),
        (1149, 1598, 0u),
        (1598, 1979, 0u),
        (1979, 2439, 0u),
        (2439, 2494, 0u),
        (2494, 2878, 1u),
        (2878, 2992, 0u),
        (2992, 3272, 1u),
        (3272, 3488, 0u),
        (3488, 3735, 1u),
        (3735, 3827, 0u),
        (3827, 4198, 1u),
        (4198, 4411, 0u),
        (4411, 4622, 1u),
        (4622, 4646, 0u),
        (4646, 4667, 1u),
        (4667, 4874, 3u),
        (4874, 4880, 7u),
        (4880, 4886, 15u),
        (4886, 4892, 31u),
        (4892, 4913, 63u),
        (4913, 4997, 127u),
        (4997, 5021, 3u),
        (5021, 5048, 7u),
        (5048, 5140, 15u),
        (5140, 5296, 63u),
    ];

    static readonly (ushort Start, ushort End, float From, float To)[] ReleaseSweeps =
    [
        (1149, 1172, 0f, 0f),
        (1197, 1259, 0f, 30f),
        (1274, 1329, 30f, 90f),
        (1343, 1438, 90f, 119f),
        (1462, 1498, 119f, 150f),
        (1598, 1621, 0f, 0f),
        (1695, 1749, 0f, 250f),
        (1763, 1936, 250f, 600f),
        (1979, 2002, 0f, 0f),
        (2035, 2144, 0f, 450f),
        (2171, 2199, 450f, 600f),
        (2206, 2319, 600f, 900f),
        (2347, 2421, 900f, 1500f),
    ];

    static readonly (ushort Start, ushort End, int Line)[] ReleaseVoices =
    [
        (15, 307, 1),
        (343, 755, 2),
        (791, 1128, 3),
        (1164, 1577, 4),
        (1613, 1958, 5),
        (1994, 2418, 6),
        (2454, 2857, 7),
        (2893, 3251, 8),
        (3287, 3714, 9),
        (3750, 4177, 10),
        (4213, 4601, 11),
        (4637, 4976, 12),
        (5012, 5275, 13),
    ];

    static readonly (ushort Start, ushort End, int Weight)[] ReleaseBeats =
    [
        (0, 2, 1),
        (23, 25, 1),
        (52, 54, 1),
        (188, 190, 1),
        (243, 245, 1),
        (328, 330, 1),
        (351, 353, 1),
        (439, 441, 1),
        (504, 506, 1),
        (576, 578, 1),
        (644, 646, 1),
        (698, 700, 1),
        (776, 778, 1),
        (797, 799, 1),
        (842, 844, 1),
        (890, 892, 1),
        (1016, 1018, 1),
        (1091, 1093, 1),
        (1149, 1151, 1),
        (1172, 1174, 1),
        (1197, 1199, 1),
        (1274, 1276, 1),
        (1343, 1345, 1),
        (1462, 1464, 1),
        (1507, 1509, 1),
        (1598, 1600, 1),
        (1621, 1623, 1),
        (1695, 1697, 1),
        (1763, 1765, 1),
        (1979, 1981, 1),
        (2002, 2004, 1),
        (2035, 2037, 1),
        (2171, 2173, 1),
        (2206, 2208, 1),
        (2347, 2349, 1),
        (2439, 2441, 1),
        (2462, 2464, 1),
        (2494, 2496, 1),
        (2643, 2645, 1),
        (2728, 2730, 1),
        (2878, 2880, 1),
        (2902, 2904, 1),
        (2930, 2932, 1),
        (2992, 2994, 1),
        (3163, 3165, 1),
        (3272, 3274, 1),
        (3295, 3297, 1),
        (3319, 3321, 1),
        (3488, 3490, 1),
        (3638, 3640, 1),
        (3735, 3737, 1),
        (3758, 3760, 1),
        (3783, 3785, 1),
        (3827, 3829, 1),
        (3997, 3999, 1),
        (4198, 4200, 1),
        (4219, 4221, 1),
        (4257, 4259, 1),
        (4411, 4413, 1),
        (4622, 4624, 1),
        (4646, 4648, 1),
        (4714, 4716, 1),
        (4874, 4876, 1),
        (4913, 4915, 1),
        (4948, 4950, 1),
        (4997, 4999, 1),
        (5021, 5023, 1),
        (5048, 5050, 1),
        (5140, 5142, 1),
    ];

    static readonly (ushort Start, ushort End, int Scene)[] TutorialScenes =
    [
        (0, 267, 1),
        (267, 700, 2),
        (700, 1075, 3),
        (1075, 1684, 4),
        (1684, 2069, 5),
        (2069, 2490, 6),
        (2490, 2805, 7),
        (2805, 3144, 8),
    ];

    static readonly (ushort Start, ushort End, int Line)[] TutorialCaptions =
    [
        (0, 24, 0),
        (24, 64, 1),
        (64, 151, 2),
        (151, 267, 3),
        (267, 291, 0),
        (291, 317, 4),
        (317, 373, 5),
        (373, 512, 6),
        (512, 700, 7),
        (700, 723, 0),
        (723, 751, 8),
        (751, 794, 9),
        (794, 949, 10),
        (949, 1009, 11),
        (1009, 1075, 12),
        (1075, 1099, 0),
        (1099, 1128, 13),
        (1128, 1191, 14),
        (1191, 1237, 15),
        (1237, 1306, 16),
        (1306, 1350, 17),
        (1350, 1491, 18),
        (1491, 1583, 19),
        (1583, 1684, 20),
        (1684, 1707, 0),
        (1707, 1736, 21),
        (1736, 1775, 22),
        (1775, 1897, 23),
        (1897, 2069, 24),
        (2069, 2093, 0),
        (2093, 2125, 25),
        (2125, 2153, 26),
        (2153, 2227, 27),
        (2227, 2289, 28),
        (2289, 2407, 29),
        (2407, 2490, 30),
        (2490, 2513, 0),
        (2513, 2551, 31),
        (2551, 2628, 32),
        (2628, 2805, 33),
        (2805, 2828, 0),
        (2828, 2866, 34),
        (2866, 2954, 35),
        (2954, 3144, 36),
    ];

    static readonly (ushort Start, ushort End, int Footage)[] TutorialFootages =
    [
        (0, 3174, 0),
    ];

    static readonly (ushort Start, ushort End, uint Mask)[] TutorialReveals =
    [
        (0, 267, 3u),
        (267, 373, 0u),
        (373, 512, 1u),
        (512, 571, 3u),
        (571, 700, 7u),
        (700, 794, 0u),
        (794, 842, 1u),
        (842, 949, 3u),
        (949, 1075, 7u),
        (1075, 1099, 0u),
        (1099, 1128, 0u),
        (1128, 1191, 1u),
        (1191, 1237, 2u),
        (1237, 1306, 4u),
        (1306, 1350, 8u),
        (1350, 1491, 8u),
        (1491, 1583, 8u),
        (1583, 1684, 8u),
        (1684, 1775, 0u),
        (1775, 1897, 1u),
        (1897, 1951, 3u),
        (1951, 2069, 7u),
        (2069, 2490, 0u),
        (2490, 2628, 0u),
        (2628, 2650, 1u),
        (2650, 2672, 3u),
        (2672, 2694, 7u),
        (2694, 2716, 15u),
        (2716, 2805, 31u),
        (2805, 2828, 3u),
        (2828, 2866, 7u),
        (2866, 2954, 15u),
        (2954, 3144, 63u),
    ];

    static readonly (ushort Start, ushort End, float From, float To)[] TutorialSweeps =
    [
        (2069, 2093, 0f, 0f),
        (2153, 2212, 0f, 30f),
        (2227, 2277, 30f, 90f),
        (2289, 2383, 90f, 119f),
        (2407, 2473, 119f, 150f),
    ];

    static readonly (ushort Start, ushort End, int Line)[] TutorialVoices =
    [
        (15, 246, 1),
        (282, 679, 2),
        (715, 1054, 3),
        (1090, 1663, 4),
        (1699, 2048, 5),
        (2084, 2469, 6),
        (2505, 2784, 7),
        (2820, 3123, 8),
    ];

    static readonly (ushort Start, ushort End, int Weight)[] TutorialBeats =
    [
        (0, 2, 1),
        (24, 26, 1),
        (64, 66, 1),
        (151, 153, 1),
        (267, 269, 1),
        (291, 293, 1),
        (317, 319, 1),
        (373, 375, 1),
        (512, 514, 1),
        (700, 702, 1),
        (723, 725, 1),
        (751, 753, 1),
        (794, 796, 1),
        (949, 951, 1),
        (1009, 1011, 1),
        (1075, 1077, 1),
        (1099, 1101, 1),
        (1128, 1130, 1),
        (1191, 1193, 1),
        (1237, 1239, 1),
        (1306, 1308, 1),
        (1350, 1352, 1),
        (1491, 1493, 1),
        (1583, 1585, 1),
        (1684, 1686, 1),
        (1707, 1709, 1),
        (1736, 1738, 1),
        (1775, 1777, 1),
        (1897, 1899, 1),
        (2069, 2071, 1),
        (2093, 2095, 1),
        (2125, 2127, 1),
        (2153, 2155, 1),
        (2227, 2229, 1),
        (2289, 2291, 1),
        (2407, 2409, 1),
        (2490, 2492, 1),
        (2513, 2515, 1),
        (2551, 2553, 1),
        (2628, 2630, 1),
        (2805, 2807, 1),
        (2828, 2830, 1),
        (2866, 2868, 1),
        (2954, 2956, 1),
    ];

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
        Column(json, "footageTick", perTick, s => s.FootageTick.ToString(CultureInfo.InvariantCulture));
        Column(json, "reveal", perTick, s => s.Reveal.ToString(CultureInfo.InvariantCulture));
        Column(json, "sweep", perTick, s => s.Sweep.ToString("0.###", CultureInfo.InvariantCulture));
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
