using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class EventSpeedTests
{
    const string Name = "Speedy";

    const string DomainSource = """
        using Tl;

        namespace Speedy
        {
            [TlState]
            public partial struct Chime
            {
                public int Count;
            }

            [TlClip("Speedy/Chime")]
            public struct ChimeClip
            {
                public int Rings;
            }

            public struct Ding { public int Rings; }
            public struct Dong { public int Rings; }
            public struct Hum { public int Rings; }

            [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
            public struct ChimeKind
            {
                public static void Apply(in int rings, ref Chime chime) => chime.Count += rings;
            }

            public struct ChimeTrack : ITlTrack<ChimeClip>
            {
                [Pulse(typeof(ChimeKind), On = ClipEvent.Start)]
                public static int Ring(in TlFrame<ChimeClip> frame) => frame.Direction * frame.Clip.Rings;

                public static void OnClipStart(in TlFrame<ChimeClip> frame, ref Events<Ding> dings) => dings.Raise(new Ding { Rings = frame.Clip.Rings });

                public static void OnClipEnd(in TlFrame<ChimeClip> frame, ref Events<Dong> dongs) => dongs.Raise(new Dong { Rings = frame.Clip.Rings });

                public static void ExecuteActive(in TlFrame<ChimeClip> frame, ref Events<Hum> hums) => hums.Raise(new Hum { Rings = frame.Clip.Rings });
            }
        }
        """;

    const string HostSource = """
        using System.Linq;
        using Tl;

        public static class SpeedyHost
        {
            public static string[] Play(byte[] library, int rate, int steps, bool kinds)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(1);
                using var store = kinds ? new KindStore(world, players) : null;
                using var dings = new Events<Speedy.Ding>(8);
                using var dongs = new Events<Speedy.Dong>(8);
                using var hums = new Events<Speedy.Hum>(8);
                var chime = new Speedy.Chime[1];
                var trace = new string[steps];
                players.Start(0, 1);
                for (var step = 0; step < steps; step++)
                {
                    if (step == 1 && rate != 16) players.SetSpeed(0, 1, (ushort)rate);
                    if (kinds) world.Tick(players);
                    else
                    {
                        world.ApplyRequests(players);
                        world.Advance(players);
                    }
                    new Tl.Generated.TlAuto_Speedy { C0_Chime = chime, E0_Ding = dings.Writer, E1_Dong = dongs.Writer, E2_Hum = hums.Writer }.Run(world, players);
                    var d = string.Join(",", dings.Drain().ToArray().Select(static r => r.Value.Rings).OrderBy(static x => x));
                    var e = string.Join(",", dongs.Drain().ToArray().Select(static r => r.Value.Rings).OrderBy(static x => x));
                    var h = string.Join(",", hums.Drain().ToArray().Select(static r => r.Value.Rings).OrderBy(static x => x));
                    trace[step] = "start " + d + " end " + e + " active " + h + " chime " + chime[0].Count;
                }
                return trace;
            }
        }
        """;

    sealed class Built
    {
        public Assembly Domain = null!;
        public Assembly Host = null!;
    }

    static Built Build()
    {
        var references = EmissionTests.References().ToList();
        var image = TestDomain.Compile(Name, DomainSource, references, Name, out _, out var warnings);
        Assert.Empty(warnings);
        var host = TestDomain.Compile("SpeedyHost", HostSource, references.Append(MetadataReference.CreateFromImage(image)).ToList(), "", out _, out _);
        var context = new AssemblyLoadContext(Name, false);
        return new Built { Domain = context.LoadFromStream(new MemoryStream(image)), Host = context.LoadFromStream(new MemoryStream(host)) };
    }

    static readonly Lazy<Built> Assemblies = new(Build);

    static readonly (uint Start, uint End)[] Clips = [(5, 7), (7, 8), (8, 11), (12, 13), (13, 16)];

    static string[] Play(int rate, int steps, bool kinds = true)
    {
        var built = Assemblies.Value;
        var chime = built.Domain.GetType("Speedy.ChimeClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "chimes", Duration = 20, Tracks = { TestDomain.Track(chime, Clips.Select(static c => (c.Start, c.End, TestDomain.Pack((int)c.Start))).ToArray()) } },
        };
        return (string[])built.Host.GetType("SpeedyHost", true)!.GetMethod("Play")!.Invoke(null, [TestDomain.Library(timelines, built.Domain), rate, steps, kinds])!;
    }

    static string[] Trace(int rate)
    {
        var steps = Play(rate, 24);
        var trace = new List<string>();
        var before = "chime 0";
        foreach (var (step, line) in steps.Select(static (line, step) => (step, line)))
        {
            var chime = line[line.IndexOf("chime", StringComparison.Ordinal)..];
            if (!line.StartsWith("start  end  active  ", StringComparison.Ordinal) || chime != before)
                trace.Add(step + ": " + line);
            before = chime;
        }
        return [.. trace];
    }

    internal static readonly Dictionary<int, string[]> Pinned = new()
    {
        [8] =
        [
            "10: start 5 end  active 5 chime 5",
            "12: start  end 5 active 5 chime 5",
            "14: start 7 end 7 active 7 chime 12",
            "16: start 8 end  active 8 chime 20",
            "18: start  end  active 8 chime 20",
            "20: start  end 8 active 8 chime 20"
        ],
        [16] =
        [
            "5: start 5 end  active 5 chime 5",
            "6: start  end 5 active 5 chime 5",
            "7: start 7 end 7 active 7 chime 12",
            "8: start 8 end  active 8 chime 20",
            "9: start  end  active 8 chime 20",
            "10: start  end 8 active 8 chime 20",
            "12: start 12 end 12 active 12 chime 32",
            "13: start 13 end  active 13 chime 45",
            "14: start  end  active 13 chime 45",
            "15: start  end 13 active 13 chime 45"
        ],
        [24] =
        [
            "4: start 5 end 5 active 5,5 chime 5",
            "5: start 7 end 7 active 7 chime 12",
            "6: start 8 end  active 8,8 chime 20",
            "7: start  end 8 active 8 chime 20",
            "8: start 12 end 12 active 12 chime 32",
            "9: start 13 end  active 13 chime 45",
            "10: start  end 13 active 13,13 chime 45"
        ],
        [32] =
        [
            "3: start 5 end 5 active 5,5 chime 5",
            "4: start 7,8 end 7 active 7,8 chime 20",
            "5: start  end 8 active 8,8 chime 20",
            "6: start 12 end 12 active 12 chime 32",
            "7: start 13 end  active 13,13 chime 45",
            "8: start  end 13 active 13 chime 45"
        ],
        [40] =
        [
            "2: start 5 end  active 5 chime 5",
            "3: start 7 end 5,7 active 5,7 chime 12",
            "4: start 8 end 8 active 8,8,8 chime 20",
            "5: start 12 end 12 active 12 chime 32",
            "6: start 13 end 13 active 13,13,13 chime 45"
        ],
        [48] =
        [
            "2: start 5 end 5 active 5,5 chime 5",
            "3: start 7,8 end 7 active 7,8,8 chime 20",
            "4: start 12 end 8,12 active 8,12 chime 32",
            "5: start 13 end 13 active 13,13,13 chime 45"
        ],
        [64] =
        [
            "2: start 5,7,8 end 5,7 active 5,5,7,8 chime 20",
            "3: start 12 end 8,12 active 8,8,12 chime 32",
            "4: start 13 end 13 active 13,13,13 chime 45"
        ],
    };

    [Fact]
    public void ClipEventsAtEachSpeedMatchThePinnedTraceTheUnityHostAlsoChecks()
    {
        foreach (var (rate, expected) in Pinned)
            Assert.Equal(expected, Trace(rate));
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(16, true)]
    [InlineData(24, true)]
    [InlineData(32, true)]
    [InlineData(40, true)]
    [InlineData(48, true)]
    [InlineData(64, true)]
    [InlineData(8, false)]
    [InlineData(16, false)]
    [InlineData(24, false)]
    [InlineData(32, false)]
    [InlineData(48, false)]
    [InlineData(64, false)]
    public void EveryClipStartEndAndActiveTickIsRaisedOnceAtEachSpeed(int rate, bool kinds)
    {
        var steps = Play(rate, 48, kinds);
        var starts = new List<int>();
        var ends = new List<int>();
        var active = new List<int>();
        foreach (var line in steps)
        {
            var parts = line.Split(' ');
            starts.AddRange(Numbers(parts[1]));
            ends.AddRange(Numbers(parts[3]));
            active.AddRange(Numbers(parts[5]));
        }
        var rings = Clips.Select(static c => (int)c.Start).ToArray();
        Assert.Equal(rings, starts.Order());
        Assert.Equal(rings, ends.Order());
        Assert.Equal(Clips.SelectMany(static c => Enumerable.Repeat((int)c.Start, (int)(c.End - c.Start))), active.Order());
        Assert.EndsWith(kinds ? "chime 45" : "chime 0", steps[^1], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(48)]
    [InlineData(64)]
    public void EachClipStartIsRaisedOnTheStepItsKindPulseLands(int rate)
    {
        var before = 0;
        foreach (var line in Play(rate, 48))
        {
            var parts = line.Split(' ');
            var chime = int.Parse(parts[^1]);
            Assert.Equal(chime - before, Numbers(parts[1]).Sum());
            before = chime;
        }
    }

    static IEnumerable<int> Numbers(string list) => list.Length == 0 ? [] : list.Split(',').Select(int.Parse);
}
