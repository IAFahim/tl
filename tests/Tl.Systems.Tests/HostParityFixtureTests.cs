using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class HostParityFixtureTests
{
    const string PackName = "Tl.Unity.ParityPack";
    const string GameName = "Tl.Unity.ParityGame";
    const int Rows = 4;
    const int Ticks = 600;

    const string HostSource = """
        using System;
        using System.Runtime.InteropServices;
        using Tl;

        public static class ParityHost
        {
            public static string[] Play(byte[] library, string script, int rows)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var screen = new ParityPack.Screen[rows];
                var stage = new ParityGame.Stage[rows];
                for (var e = 0; e < rows; e++) stage[e].Offset = e * 100;
                var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var result = new string[lines.Length];
                for (var t = 0; t < lines.Length; t++)
                {
                    var parts = lines[t].Trim().Split(';');
                    var forward = parts[0] == "1";
                    if (parts.Length > 1 && parts[1].Length > 0)
                        foreach (var command in parts[1].Split(','))
                        {
                            var c = command.Split(':');
                            var row = int.Parse(c[0]);
                            if (c[1] == "S") players.Start(row, ushort.Parse(c[2]));
                            else if (c[1] == "P") players.Stop(row, ushort.Parse(c[2]));
                            else players.StopAll(row);
                        }
                    world.Tick(players, forward);
                    new Tl.Generated.TlAuto_Tl_Unity_ParityGame { C0_Stage = stage }.Run(world, players, forward);
                    new Tl.Generated.TlAuto_Tl_Unity_ParityPack { C0_Screen = screen }.Run(world, players, forward);
                    result[t] = Convert.ToHexString(MemoryMarshal.AsBytes<ParityPack.Screen>(screen));
                }
                return result;
            }
        }
        """;

    static string Dir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        return Path.Combine(dir, "tests", "Tl.Systems.Tests", "Parity");
    }

    static bool Update => Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1";

    static string Script()
    {
        var text = new StringBuilder();
        var rng = 0x51ED270Bu;
        uint Next()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return rng;
        }
        for (var t = 0; t < Ticks; t++)
        {
            var forward = !(t >= 200 && t < 240 || t >= 400 && t < 420);
            var commands = new List<string>();
            for (var e = 0; e < Rows; e++)
            {
                var r = Next();
                var timeline = 1 + (r >> 8) % 5;
                switch (r & 7)
                {
                    case 0:
                    case 1:
                        commands.Add($"{e}:S:{timeline}");
                        break;
                    case 2:
                        commands.Add($"{e}:P:{timeline}");
                        break;
                    case 3:
                        if (((r >> 8) & 15) == 0) commands.Add($"{e}:A:0");
                        break;
                }
            }
            text.Append(forward ? '1' : '0').Append(';').Append(string.Join(',', commands)).Append('\n');
        }
        return text.ToString();
    }

    static byte[] Ints(params int[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), values[i]);
        return bytes;
    }

    static byte[] Library(Assembly pack, Assembly game)
    {
        var mood = pack.GetType("ParityPack.MoodClip", true)!;
        var line = game.GetType("ParityGame.LineClip", true)!;
        var reel = game.GetType("ParityGame.ReelClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "a_line", Duration = 30, Tracks = { TestDomain.Track(line, (3, 10, Ints(7)), (14, 20, Ints(9))) } },
            new() { Name = "b_mood", Duration = 30, Tracks = { TestDomain.Track(mood, (2, 12, Ints(5)), (18, 26, Ints(6))) } },
            new() { Name = "c_reel", Duration = 30, Tracks = { TestDomain.Track(reel, (5, 15, Ints(3))) } },
            new() { Name = "d_all", Duration = 40, Tracks = { TestDomain.Track(line, (4, 24, Ints(21))), TestDomain.Track(mood, (10, 30, Ints(8))), TestDomain.Track(reel, (0, 12, Ints(4))) } },
            new() { Name = "e_loop", Duration = 16, Loops = true, Tracks = { TestDomain.Track(line, (0, 8, Ints(11))) } },
        };
        return TestDomain.Library(timelines, pack, game);
    }

    sealed class Built
    {
        public Assembly Pack = null!;
        public Assembly Game = null!;
        public Assembly Host = null!;
        public string PackSource = "";
        public string GameSource = "";
    }

    static Built Build()
    {
        var references = EmissionTests.References().ToList();
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var packImage = TestDomain.Compile(PackName, EmissionTests.ReadResource("Tl.Systems.Tests.Parity.ParityPack.cs"), references, PackName.Replace('.', '_'), out var packGenerated, out var packWarnings);
        var packReference = MetadataReference.CreateFromImage(packImage);
        var gameImage = TestDomain.Compile(GameName, EmissionTests.ReadResource("Tl.Systems.Tests.Parity.ParityGame.cs"), references.Append(packReference).ToList(), GameName.Replace('.', '_'), out var gameGenerated, out var gameWarnings);
        Assert.Empty(packWarnings);
        Assert.Empty(gameWarnings);
        var hostImage = TestDomain.Compile("ParityHost", HostSource, references.Append(packReference).Append(MetadataReference.CreateFromImage(gameImage)).ToList(), "", out _, out _);
        var context = new AssemblyLoadContext("Parity", false);
        return new Built
        {
            Pack = context.LoadFromStream(new MemoryStream(packImage)),
            Game = context.LoadFromStream(new MemoryStream(gameImage)),
            Host = context.LoadFromStream(new MemoryStream(hostImage)),
            PackSource = packGenerated,
            GameSource = gameGenerated,
        };
    }

    static readonly Lazy<Built> Assemblies = new(Build);

    static string[] Play(Built built, byte[] library, string script)
        => (string[])built.Host.GetType("ParityHost", true)!.GetMethod("Play")!.Invoke(null, [library, script, Rows])!;

    [Fact]
    public void TheIdleKindStaysInThePackAndIsFedByATrackInTheGameAssembly()
    {
        var built = Assemblies.Value;
        Assert.Contains("global::ParityPack.Caption.Idle(", built.PackSource);
        Assert.Contains("global::ParityPack.Mood.Idle(", built.PackSource);
        Assert.DoesNotContain("Idle(", built.GameSource);
        Assert.Contains("global::ParityGame.LineTrack.Which(", built.GameSource);
        Assert.DoesNotContain("global::ParityPack.Caption.Apply(", built.GameSource);
    }

    [Fact]
    public void LibraryScriptAndPerTickColumnBytesMatchTheCommittedFixtureTheOtherHostRuns()
    {
        var built = Assemblies.Value;
        var library = Library(built.Pack, built.Game);
        var script = Script();
        var bytes = Play(built, library, script);
        var expected = string.Join('\n', bytes) + "\n";
        if (Update)
        {
            File.WriteAllBytes(Path.Combine(Dir(), "parity.tlib"), library);
            File.WriteAllText(Path.Combine(Dir(), "parity.script"), script);
            File.WriteAllText(Path.Combine(Dir(), "parity.expected"), expected);
        }
        Assert.Equal(File.ReadAllBytes(Path.Combine(Dir(), "parity.tlib")), library);
        Assert.Equal(File.ReadAllText(Path.Combine(Dir(), "parity.script")).Replace("\r\n", "\n"), script);
        var golden = File.ReadAllText(Path.Combine(Dir(), "parity.expected")).Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(Ticks, golden.Length);
        for (var tick = 0; tick < Ticks; tick++) Assert.True(golden[tick] == bytes[tick], $"column bytes differ from the fixture at tick {tick}");
    }

    [Fact]
    public void TheFixtureExercisesIdleAcrossAssembliesAndTheRewindThroughAnAssigningIdle()
    {
        var built = Assemblies.Value;
        var bytes = Play(built, Library(built.Pack, built.Game), File.ReadAllText(Path.Combine(Dir(), "parity.script")));
        int Field(int tick, int row, int column) => BitConverter.ToInt32(Convert.FromHexString(bytes[tick]), (row * 3 + column) * 4);
        var captionIdle = 0;
        var moodIdle = 0;
        var captionLive = 0;
        var moodLive = 0;
        var reelHolds = 0;
        var rewoundIdle = 0;
        for (var tick = 1; tick < Ticks; tick++)
        {
            var rewind = tick is >= 200 and < 240 or >= 400 and < 420;
            for (var row = 0; row < Rows; row++)
            {
                if (Field(tick, row, 0) == -1) { captionIdle++; if (rewind) rewoundIdle++; }
                else captionLive++;
                if (Field(tick, row, 1) == 0) moodIdle++;
                else moodLive++;
                if (Field(tick, row, 2) != 0 && Field(tick - 1, row, 2) == Field(tick, row, 2)) reelHolds++;
            }
        }
        Assert.True(captionIdle > 100 && captionLive > 100, $"Caption (Idle -1, fed from the game assembly): idle {captionIdle}, live {captionLive}");
        Assert.True(moodIdle > 100 && moodLive > 100, $"Mood (Idle 0, same assembly): idle {moodIdle}, live {moodLive}");
        Assert.True(reelHolds > 100, $"Reel has no Idle and keeps its last value: {reelHolds} held ticks");
        Assert.True(rewoundIdle > 20, $"Idle values assigned while the timeline runs backwards: {rewoundIdle}");
    }
}
