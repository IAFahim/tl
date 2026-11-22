using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class DenseOracleTests
{
    const int Rewinds = 0, Controls = 1, DenseSteps = 2, OffDenseSteps = 3, Reloads = 4, GamePulses = 5, Spilled = 6, SkippedTally = 7;

    const string DenseHostSource = """
        using System;
        using Tl;
        using DenseGame;

        public static unsafe class DenseHost
        {
            const int Timelines = 6;

            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            sealed class Arm : IDisposable
            {
                public readonly TimelineWorld World;
                public readonly PlaybackTable Players;
                public readonly KindStore Kinds;
                public readonly Body[] Bodies;

                public Arm(byte[] library, int rows, int slots)
                {
                    World = new TimelineWorld(library);
                    Players = new PlaybackTable(rows, slots);
                    Kinds = new KindStore(World, Players);
                    Bodies = new Body[rows];
                }

                public void Step(bool forward, TlWorkers? workers, bool tally)
                {
                    World.Tick(Players, forward);
                    if (workers == null)
                    {
                        new MoveSystem { Body = Bodies }.Run(World, Players, forward);
                        new PushSystem { Body = Bodies }.Run(World, Players, forward);
                        if (tally) new TallySystem { Body = Bodies }.Run(World, Players, forward);
                        new LaneSystem { Body = Bodies }.Run(World, Players, forward);
                        return;
                    }
                    new MoveSystem { Body = Bodies }.Run(World, Players, forward, workers);
                    new PushSystem { Body = Bodies }.Run(World, Players, forward, workers);
                    if (tally) new TallySystem { Body = Bodies }.Run(World, Players, forward, workers);
                    new LaneSystem { Body = Bodies }.Run(World, Players, forward, workers);
                }

                public void Dispose()
                {
                    Kinds.Dispose();
                    Players.Dispose();
                    World.Dispose();
                }
            }

            static void Control(PlaybackTable players, int entity, uint roll)
            {
                var timeline = (ushort)(1 + (roll >> 8) % Timelines);
                var at = (ushort)((roll >> 16) % 40);
                switch (roll & 31)
                {
                    case 6: case 7: case 8: players.Pause(entity, timeline); return;
                    case 9: case 10: case 11: players.Resume(entity, timeline); return;
                    case 12: case 13: case 14: players.Reverse(entity, timeline, (at & 1) != 0); return;
                    case 15: case 16: players.SetSpeed(entity, timeline, 32); return;
                    case 17: players.SetSpeed(entity, timeline, 8); return;
                    case 18: case 19: players.SetSpeed(entity, timeline, 16); return;
                    case 20: case 21: case 22: players.Seek(entity, timeline, at); return;
                    case 23: case 24: case 25: players.Stop(entity, timeline); return;
                    case 26: players.StopAll(entity); return;
                    case 27: players.Reset(entity); return;
                    default: players.Start(entity, timeline, at); return;
                }
            }

            static string Differs(Arm a, Arm b, int step)
            {
                for (var e = 0; e < a.Bodies.Length; e++)
                {
                    var x = a.Bodies[e];
                    var y = b.Bodies[e];
                    var px = (byte*)&x;
                    var py = (byte*)&y;
                    for (var i = 0; i < sizeof(Body); i++)
                        if (px[i] != py[i]) return $"step {step} entity {e}: body byte {i} dense {px[i]} explicit {py[i]}";
                    var va = a.Players.View;
                    var vb = b.Players.View;
                    if (va.CountOf(e) != vb.CountOf(e)) return $"step {step} entity {e}: dense count {va.CountOf(e)}, explicit {vb.CountOf(e)}";
                    for (var i = 0; i < va.CountOf(e); i++)
                    {
                        var sa = va.RunOf(e)[i];
                        var sb = vb.RunOf(e)[i];
                        if (*(ulong*)&sa != *(ulong*)&sb) return $"step {step} entity {e} slot {i}: dense timeline {sa.Timeline} tick {sa.Tick} flags {sa.Flags} rate {sa.Rate} phase {sa.Phase}, explicit timeline {sb.Timeline} tick {sb.Tick} flags {sb.Flags} rate {sb.Rate} phase {sb.Phase}";
                    }
                    if (a.Players.IsPlaying(e, 1) != b.Players.IsPlaying(e, 1)) return $"step {step} entity {e}: IsPlaying differs";
                }
                var pa = a.Players.Playing;
                var pb = b.Players.Playing;
                for (var w = 0; w < a.Players.PlayingWords; w++)
                    if (pa[w] != pb[w]) return $"step {step}: playing word {w} dense {pa[w]:X16} explicit {pb[w]:X16}";
                if (a.Kinds.DroppedPulses != b.Kinds.DroppedPulses || a.Players.DroppedStarts != b.Players.DroppedStarts) return $"step {step}: dropped counts differ";
                return "";
            }

            public static string Run(byte[] first, byte[] second, int rows, int slots, int steps, int calm, int workerCount, Action<KindStore> off, long[] stats)
            {
                using var a = new Arm(first, rows, slots);
                using var b = new Arm(first, rows, slots);
                off(b.Kinds);
                using var workers = workerCount > 0 ? new TlWorkers(workerCount) : null;
                var rng = 0xC0FFEE12u;
                for (var e = 0; e < rows; e++)
                    for (var k = 1 + (int)(Next(ref rng) % (uint)slots); k > 0; k--)
                    {
                        var timeline = (ushort)(1 + Next(ref rng) % Timelines);
                        var at = (ushort)(Next(ref rng) % 40);
                        a.Players.Start(e, timeline, at);
                        b.Players.Start(e, timeline, at);
                    }
                var forward = true;
                for (var step = 0; step < steps; step++)
                {
                    if (step % 97 == 41 || Next(ref rng) % 211 == 0)
                    {
                        forward = !forward;
                        stats[0]++;
                    }
                    if (step == steps / 2)
                    {
                        a.World.Reload(second);
                        b.World.Reload(second);
                        stats[4]++;
                    }
                    for (var e = 0; e < rows; e++)
                    {
                        if (Next(ref rng) % (uint)calm != 0) continue;
                        var roll = Next(ref rng);
                        Control(a.Players, e, roll);
                        Control(b.Players, e, roll);
                        stats[1]++;
                    }
                    if (step % 11 == 5)
                        for (var e = step % 4; e < rows; e += 4)
                        {
                            a.Kinds.Pulses<int>("DenseGame.Count").To(e, 3 + e % 7);
                            b.Kinds.Pulses<int>("DenseGame.Count").To(e, 3 + e % 7);
                            if (step % 33 != 5) continue;
                            a.Kinds.Pulses<int>("DenseGame.Hits").To(e, 1 + e % 3);
                            b.Kinds.Pulses<int>("DenseGame.Hits").To(e, 1 + e % 3);
                            stats[5]++;
                        }
                    var tally = step % 13 != 7 && step % 29 != 11;
                    if (!tally) stats[7]++;
                    a.Step(forward, workers, tally);
                    b.Step(forward, null, tally);
                    for (var e = 0; e < rows; e++)
                    {
                        if (a.Players.SystemView.IsDense(e)) stats[2]++;
                        if (b.Players.SystemView.IsDense(e)) stats[3]++;
                        if (a.Players.CountOf(e) > slots) stats[6]++;
                    }
                    var differs = Differs(a, b, step);
                    if (differs.Length != 0) return differs;
                }
                return "";
            }
        }
        """;

    sealed class Built
    {
        public Assembly Host = null!;
        public byte[] First = null!;
        public byte[] Second = null!;
    }

    static readonly CSharpParseOptions Parse = new(LanguageVersion.Latest);
    static readonly Lazy<Built> Game = new(Build);

    static byte[] Compile(string name, string source, IEnumerable<MetadataReference> references, bool generate)
    {
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, Parse, name + ".cs")], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Enable));
        if (generate)
        {
            var emitted = SystemGeneration.Emit(compilation, name.Replace('.', '_'));
            Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message)));
            foreach (var artifact in emitted.Artifacts) compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, Parse, artifact.RelativePath));
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    static Built Build()
    {
        var references = EmissionTests.References().ToList();
        var domain = Compile("Tl.Dense.DenseGame", EmissionTests.ReadResource("Tl.Systems.Tests.Dense.DenseGame.cs"), references, true);
        var host = Compile("DenseHost", DenseHostSource, references.Append(MetadataReference.CreateFromImage(domain)), false);
        var context = new AssemblyLoadContext("DenseGame");
        var loaded = context.LoadFromStream(new MemoryStream(domain));
        return new Built { Host = context.LoadFromStream(new MemoryStream(host)), First = Library(loaded, false), Second = Library(loaded, true) };
    }

    static byte[] Library(Assembly domain, bool second)
    {
        var shift = second ? 1u : 0u;
        var bump = second ? 2 : 0;
        TypedTrack Track(string track, string clip, IEnumerable<object[]> clips)
        {
            var typed = new TypedTrack { Type = domain.GetType("DenseGame." + track, true)!, Data = TestDomain.Pack(1f) };
            foreach (var c in clips) typed.Clips.Add(new TypedClip { Type = domain.GetType("DenseGame." + clip, true)!, Start = (uint)c[0], End = (uint)c[1], Data = TestDomain.Pack(c[2..]) });
            return typed;
        }
        TypedTrack Step(params (uint Start, uint End, int Speed, int Priority, float Rise)[] clips) => Track("StepTrack", "StepClip", clips.Select(c => new object[] { c.Start, c.End, c.Speed + bump, c.Priority, c.Rise }));
        TypedTrack Push(params (uint Start, uint End, int Gain)[] clips) => Track("PushTrack", "PushClip", clips.Select(c => new object[] { c.Start, c.End, c.Gain + bump }));
        TypedTrack Tally(params (uint Start, uint End, int Count, int Hit)[] clips) => Track("TallyTrack", "TallyClip", clips.Select(c => new object[] { c.Start, c.End, c.Count, c.Hit + bump }));
        TypedTrack Lane(params (uint Start, uint End, float F, int I, int Hi, int Lo, int Bits, int Value, int Priority, float X, int Y)[] clips) => Track("LaneTrack", "LaneClip", clips.Select(c => new object[] { c.Start, c.End, c.F, c.I, c.Hi, c.Lo, c.Bits, c.Value + bump, c.Priority, c.X, c.Y }));
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "a-loop", Duration = 20 + shift, Loops = true, Tracks = { Step((0, 9, 3, 1, 1.5f), (6, 15, 5, 2, 2.25f)), Step((4, 18, 2, 1, 0.7f)), Push((2, 12, 4), (12, 19, 6)), Tally((1, 6, 2, 3), (10, 17, 4, 5)), Lane((0, 8, -0f, 2, int.MaxValue, 7, 0, 10, 1, 0f, 1), (5, 14, 0f, -1, -4, int.MinValue, 1, 20, 1, -0f, 2), (12, 20, 1.5f, 3, int.MinValue, int.MaxValue, 6, 30, 2, 1.25f, -1)), Lane((2, 10, -0f, 0, 3, 3, 4, 40, 1, -0f, 0)) } },
            new() { Name = "b-head", Duration = 9, Next = "c-tail", Tracks = { Step((0, 9, 1, 0, 0.4f)), Push((3, 8, 2)), Tally((2, 5, 1, 2)), Lane((0, 9, -0f, 1, 9, -9, 2, 5, 0, -0f, 3)) } },
            new() { Name = "c-tail", Duration = 13, Hold = true, Tracks = { Step((5, 11, 7, 2, 1.1f)), Push((0, 13, 3)), Tally((4, 9, 3, 1)), Lane((2, 11, 0f, 4, -7, 7, 8, 6, 3, 0.5f, 1)) } },
            new() { Name = "d-once", Duration = 7, Tracks = { Step((0, 7, 4, 1, 0.9f)), Tally((0, 3, 2, 4)), Lane((0, 7, -2.25f, 2, 1, 1, 0, 8, 1, -0f, 0)) } },
            new() { Name = "e-spin", Duration = 5, Loops = true, Tracks = { Step((0, 5, 2, 2, 0.3f)), Push((1, 4, 1)), Tally((2, 4, 1, 1)), Lane((0, 5, -0f, 1, int.MaxValue, int.MinValue, 16, 9, 1, 0f, 1)) } },
            new() { Name = "f-long", Duration = 31 + 2 * shift, Loops = true, Tracks = { Step((3, 20, 6, 1, 1.25f), (15, 28, 2, 1, 0.5f)), Push((0, 31, 2)), Tally((5, 12, 2, 2), (20, 26, 3, 3)), Lane((3, 20, 0.75f, -2, 11, -11, 3, 12, 2, -0.5f, 2), (15, 28, -0f, 5, int.MinValue, int.MaxValue, 0, 13, 2, 0f, -3)), Lane((10, 25, 0f, 1, 2, -2, 1, 14, 2, -0f, 1)) } },
        };
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), [], 60);
        var kinds = LibraryBake.Kinds([domain]);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static long[] Play(int rows, int slots, int steps, int calm, int workers)
    {
        var game = Game.Value;
        var stats = new long[8];
        var run = game.Host.GetType("DenseHost", true)!.GetMethod("Run")!;
        var differs = (string)run.Invoke(null, [game.First, game.Second, rows, slots, steps, calm, workers, (Action<KindStore>)(static store => store.DenseEnabled = false), stats])!;
        Assert.True(differs.Length == 0, "the dense world left the explicit one at " + differs);
        Assert.Equal(0, stats[OffDenseSteps]);
        Assert.True(stats[Rewinds] > 0 && stats[Reloads] == 1 && stats[GamePulses] > 0 && stats[SkippedTally] > 0, "the script skipped a world rewind, the reload, the game pulses or a step without the tally system.");
        return stats;
    }

    const string WrongSource = """
        using System;
        using Tl;

        namespace WrongGame
        {
        public struct Seen { public int A; public int B; public int C; }
        public struct Pri { public int Value; public byte Priority; }
        public struct WrongClip { public int V; public int P; }
        public struct WrongTrack : IBlend<WrongClip> { public float Scale; public void Blend(in WrongClip first, in WrongClip second, float factor, out WrongClip result) => result = first; }

        [TimelineKind(Combine = KindCombine.Sum)]
        public struct SumIsMax { public static void Combine(ref int total, in int next) => total = Math.Max(total, next); public static void Apply(in int total, ref Seen seen) => seen.A += total; }

        [TimelineKind(Combine = KindCombine.Pick, Priority = nameof(Pri.Priority))]
        public struct FirstWins { public static void Combine(ref Pri total, in Pri next) { if (next.Priority > total.Priority) total = next; } public static void Apply(in Pri total, ref Seen seen) => seen.B += total.Value; }

        [TimelineKind(Combine = KindCombine.Or)]
        public struct OrIsSum { public static void Combine(ref int total, in int next) => total += next; public static void Apply(in int total, ref Seen seen) => seen.C += total; }

        [TimelineSystemMember(typeof(SumSystem))]
        public struct ToSum : ITrack<WrongTrack, WrongClip> { [Feeds(typeof(SumIsMax))] public static int V(in Frame<WrongTrack, WrongClip> f) => f.Clip.V; }

        [TimelineSystemMember(typeof(PickSystem))]
        public struct ToPick : ITrack<WrongTrack, WrongClip> { [Feeds(typeof(FirstWins))] public static Pri V(in Frame<WrongTrack, WrongClip> f) => new Pri { Value = f.Clip.V, Priority = (byte)f.Clip.P }; }

        [TimelineSystemMember(typeof(OrSystem))]
        public struct ToOr : ITrack<WrongTrack, WrongClip> { [Feeds(typeof(OrIsSum))] public static int V(in Frame<WrongTrack, WrongClip> f) => f.Clip.V; }

        [TimelineSystem] public ref partial struct SumSystem { public Span<Seen> Seen; }
        [TimelineSystem] public ref partial struct PickSystem { public Span<Seen> Seen; }
        [TimelineSystem] public ref partial struct OrSystem { public Span<Seen> Seen; }
        }
        """;

    const string WrongHostSource = """
        using System;
        using Tl;
        using WrongGame;

        public static class WrongHost
        {
            public static string Run(byte[] library, int system)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(4, 2);
                using var kinds = new KindStore(world, players);
                players.Start(0, 1, 0);
                var seen = new Seen[4];
                world.Tick(players, true);
                try
                {
                    if (system == 0) new SumSystem { Seen = seen }.Run(world, players, true);
                    else if (system == 1) new PickSystem { Seen = seen }.Run(world, players, true);
                    else new OrSystem { Seen = seen }.Run(world, players, true);
                }
                catch (InvalidOperationException caught)
                {
                    return caught.Message;
                }
                return "";
            }
        }
        """;

    [Fact]
    public void DeclaredCombineThatDisagreesWithTheKindsCombineThrowsWhenTheTableIsBuilt()
    {
        var references = EmissionTests.References().ToList();
        var domain = Compile("Tl.Dense.WrongGame", WrongSource, references, true);
        var host = Compile("WrongHost", WrongHostSource, references.Append(MetadataReference.CreateFromImage(domain)), false);
        var context = new AssemblyLoadContext("WrongGame");
        var loaded = context.LoadFromStream(new MemoryStream(domain));
        var run = context.LoadFromStream(new MemoryStream(host)).GetType("WrongHost", true)!.GetMethod("Run")!;
        var track = new TypedTrack { Type = loaded.GetType("WrongGame.WrongTrack", true)!, Data = TestDomain.Pack(1f) };
        foreach (var (start, end, value, priority) in new[] { (0u, 10u, 3, 1), (5u, 15u, -4, 1), (12u, 20u, 5, 1) })
            track.Clips.Add(new TypedClip { Type = loaded.GetType("WrongGame.WrongClip", true)!, Start = start, End = end, Data = TestDomain.Pack(value, priority) });
        var timeline = new TypedTimeline { Name = "wrong", Duration = 20, Loops = true, Tracks = { track } };
        var manifest = LibraryBake.Manifest(["wrong"], [], 60);
        var tlb = LibraryBake.BakeTyped(timeline, manifest);
        var library = LibraryAssembler.Assemble([new LibraryPart { Name = "wrong", Tlb = tlb, Kinds = LibraryBake.KindPayload("wrong", tlb, LibraryBake.Kinds([loaded])) }], 60, LibrarySettings.Default);
        foreach (var (system, kind, combine) in new[] { (0, "SumIsMax", "Sum"), (1, "FirstWins", "Pick"), (2, "OrIsSum", "Or") })
            Assert.Equal($"Tl: kind 'WrongGame.{kind}' declares Combine = {combine}, but WrongGame.{kind}.Combine gives a different result on values from the library; fix Combine or the declaration.", (string)run.Invoke(null, [library, system])!);
    }

    [Theory]
    [InlineData(48, 3, 1200, 8, 0)]
    [InlineData(64, 4, 1500, 48, 0)]
    [InlineData(256, 5, 400, 24, 3)]
    [InlineData(40, 24, 300, 16, 0)]
    public void DenseAndExplicitWorldsMatchEverySlotAndColumnEveryStepThroughEveryControl(int rows, int slots, int steps, int calm, int workers)
    {
        var stats = Play(rows, slots, steps, calm, workers);
        Assert.True(stats[DenseSteps] * 10 > (long)rows * steps, $"only {stats[DenseSteps]} of {(long)rows * steps} entity-steps ran dense.");
        Assert.True(stats[Controls] > rows, "the script sent too few controls.");
        Assert.True(stats[Spilled] > 0 || slots > 16, "no entity spilled past its inline slots.");
    }
}
