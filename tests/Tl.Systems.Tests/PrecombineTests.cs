using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class PrecombineTests
{
    const int SingleSteps = 100, ManySteps = 240, PeakSteps = 400;

    const string PreHostSource = """
        using System;
        using Tl;
        using PreGame;

        public static unsafe class PreHost
        {
            const ushort Hop = 1, Stand = 2, Walk = 3;
            static readonly string[] Kinds = { "PreGame.Lift", "PreGame.Count", "PreGame.Choice", "PreGame.Shown", "PreGame.Seen" };

            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            static ulong[] Tail(KindStore kinds, ulong[] hashes, int steps, long extra)
            {
                for (var k = 0; k < Kinds.Length; k++) hashes[steps + k] = (ulong)kinds.SpanValueBytes(kinds.IndexOf(Kinds[k]));
                hashes[steps + Kinds.Length] = (ulong)extra;
                return hashes;
            }

            public static ulong[] Single(byte[] first, byte[] second, int steps)
            {
                const int rows = 12;
                using var world = new TimelineWorld(first);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var pre = new Pre[rows];
                var hashes = new ulong[steps + Kinds.Length + 1];
                for (var step = 0; step < steps; step++)
                {
                    if (step == 0)
                    {
                        players.Start(0, Walk);
                        players.Start(1, Walk, 7);
                        players.Start(2, Walk);
                        players.Start(3, Walk);
                        players.Start(4, Walk);
                        players.Start(5, Walk);
                        players.SetSpeed(5, Walk, 32);
                        players.Start(6, Walk);
                        players.SetSpeed(6, Walk, 8);
                        players.Start(7, Hop);
                        players.Start(8, Walk);
                        players.Start(9, Stand);
                        players.Start(10, Walk);
                        players.SetSpeed(10, Walk, 24);
                        players.Start(11, Hop, 5);
                        players.SetSpeed(11, Hop, 40);
                    }
                    if (step == 4) players.Pause(4, Walk);
                    if (step == 5) players.Seek(2, Walk, 15);
                    if (step == 6) players.Reverse(3, Walk, true);
                    if (step == 7) players.Reverse(10, Walk, true);
                    if (step == 9) players.Resume(4, Walk);
                    if (step == 9) players.Stop(8, Walk);
                    if (step == 11) players.Seek(2, Walk, 3);
                    if (step == 12) players.Start(8, Hop);
                    if (step == 16) players.Reverse(3, Walk, false);
                    if (step == 70) world.Reload(second);
                    var forward = !(step >= 40 && step < 52);
                    world.Tick(players, forward);
                    new PreSystem { Pre = pre }.Run(world, players, forward);
                    var h = TlStateHash.Begin();
                    for (var e = 0; e < rows; e++)
                    {
                        h.Column(in pre[e]);
                        h.Slots(players.View.RunOf(e), players.View.CountOf(e));
                    }
                    hashes[step] = h.Value;
                }
                var seen = 0L;
                for (var e = 0; e < rows; e++) seen += pre[e].Seen;
                return Tail(kinds, hashes, steps, seen);
            }

            public static ulong[] Many(byte[] first, byte[] second, int steps)
            {
                const int rows = 16;
                using var world = new TimelineWorld(first);
                using var players = new PlaybackTable(rows, 8);
                using var kinds = new KindStore(world, players);
                var pre = new Pre[rows];
                var hashes = new ulong[steps + Kinds.Length + 1];
                var rng = 0x2545F491u;
                var most = 0;
                for (var step = 0; step < steps; step++)
                {
                    for (var e = 0; e < rows; e++)
                    {
                        var r = Next(ref rng);
                        var timeline = (ushort)(1 + (r >> 8) % 3);
                        switch (r & 15)
                        {
                            case 0:
                            case 1:
                            case 2:
                                players.Start(e, timeline, (ushort)((r >> 12) % 10));
                                break;
                            case 3:
                                players.Stop(e, timeline);
                                break;
                            case 4:
                                players.SetSpeed(e, timeline, (ushort)(8 + 8 * ((r >> 16) % 4)));
                                break;
                            case 5:
                                players.Reverse(e, timeline, ((r >> 16) & 1) != 0);
                                break;
                            case 6:
                                if (((r >> 16) & 7) == 0) players.Seek(e, timeline, (ushort)((r >> 20) % 12));
                                break;
                            case 7:
                                if (((r >> 16) & 7) == 0) players.Pause(e, timeline);
                                break;
                            case 8:
                                players.Resume(e, timeline);
                                break;
                        }
                    }
                    if (step == 150) world.Reload(second);
                    var forward = !(step >= 100 && step < 120);
                    world.Tick(players, forward);
                    new PreSystem { Pre = pre }.Run(world, players, forward);
                    var h = TlStateHash.Begin();
                    for (var e = 0; e < rows; e++)
                    {
                        h.Int(pre[e].Count);
                        h.Float(pre[e].Pick);
                        h.Int(pre[e].PickPriority);
                        h.Int(pre[e].Shown);
                        h.Int(pre[e].Seen);
                        h.Int(pre[e].Applied);
                        h.Slots(players.View.RunOf(e), players.View.CountOf(e));
                        var listed = 0;
                        for (var s = 0; s < players.View.CountOf(e); s++) if (players.View.RunOf(e)[s].Timeline != 0 && KindScore.Listed(players.View.RunOf(e)[s].Flags)) listed++;
                        most = Math.Max(most, listed);
                    }
                    hashes[step] = h.Value;
                }
                return Tail(kinds, hashes, steps, most);
            }
        }
        """;

    const string PeakHostSource = """
        using System;
        using Tl;
        using PeakPack;
        using PeakGame;

        public static unsafe class PeakHost
        {
            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            public static ulong[] Play(byte[] library, int rows, int steps)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var peak = new Peak[rows];
                var gain = new Gain[rows];
                var current = new ushort[rows];
                for (var e = 0; e < rows; e++) gain[e].Scale = 1f + 0.5f * e;
                var rng = 0x6D2B79F5u;
                var hashes = new ulong[steps + 1];
                for (var step = 0; step < steps; step++)
                {
                    for (var e = 0; e < rows; e++)
                    {
                        var r = Next(ref rng);
                        switch (r & 15)
                        {
                            case 0:
                                current[e] = (ushort)(1 + (r >> 8) % 3);
                                players.StopAll(e);
                                players.Start(e, current[e], (ushort)((r >> 12) % 6));
                                break;
                            case 1:
                                if (current[e] != 0) players.Reverse(e, current[e], ((r >> 8) & 1) != 0);
                                break;
                            case 2:
                                if (current[e] != 0 && ((r >> 8) & 3) == 0) players.Seek(e, current[e], (ushort)((r >> 12) % 20));
                                break;
                            case 3:
                                if (current[e] != 0) players.SetSpeed(e, current[e], (ushort)(8 + 8 * ((r >> 8) % 4)));
                                break;
                        }
                    }
                    var forward = !(step >= 120 && step < 150);
                    world.Tick(players, forward);
                    new Tl.Generated.TlAuto_Tl_Precombine_PeakGame { C0_Gain = gain }.Run(world, players, forward);
                    new Tl.Generated.TlAuto_Tl_Precombine_PeakPack { C0_Peak = peak }.Run(world, players, forward);
                    var h = TlStateHash.Begin();
                    for (var e = 0; e < rows; e++)
                    {
                        h.Column(in peak[e]);
                        h.Slots(players.View.RunOf(e), players.View.CountOf(e));
                    }
                    hashes[step] = h.Value;
                }
                hashes[steps] = (ulong)kinds.SpanValueBytes(kinds.IndexOf("PeakPack.PeakKind"));
                return hashes;
            }
        }
        """;

    sealed class Variant
    {
        public Assembly Host = null!;
        public Assembly Domain = null!;
        public Assembly? Game;
        public string Generated = "";
        public List<string> Diagnostics = [];
    }

    sealed class Pair
    {
        public Variant Fast = null!;
        public Variant Plain = null!;
        public byte[] First = null!;
        public byte[] Second = null!;
    }

    static readonly CSharpParseOptions Parse = new(LanguageVersion.Latest);
    static readonly Lazy<Pair> Pre = new(BuildPre);
    static readonly Lazy<Pair> Peak = new(BuildPeak);

    static byte[] Compile(string name, string source, IEnumerable<MetadataReference> references, bool generate, Variant into)
    {
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, Parse, name + ".cs")], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        if (generate)
        {
            var emitted = SystemGeneration.Emit(compilation, name.Replace('.', '_'));
            into.Diagnostics.AddRange(emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message));
            Assert.True(emitted.Diagnostics.All(static d => d.Code == "TLGEN115"), string.Join("\n", into.Diagnostics));
            foreach (var artifact in emitted.Artifacts)
            {
                into.Generated += artifact.Content;
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, Parse, artifact.RelativePath));
            }
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    static string Toggle(string source, bool fast)
    {
        Assert.Contains("Associative = true", source);
        return fast ? source : source.Replace("Associative = true", "Associative = false");
    }

    static Variant PreVariant(bool fast)
    {
        var variant = new Variant();
        var references = EmissionTests.References().ToList();
        var domain = Compile("Tl.Precombine.PreGame", Toggle(EmissionTests.ReadResource("Tl.Systems.Tests.Precombine.PreGame.cs"), fast), references, true, variant);
        var host = Compile("PreHost", PreHostSource, references.Append(MetadataReference.CreateFromImage(domain)), false, variant);
        var context = new AssemblyLoadContext("PreGame" + (fast ? "Fast" : "Plain"));
        variant.Domain = context.LoadFromStream(new MemoryStream(domain));
        variant.Host = context.LoadFromStream(new MemoryStream(host));
        return variant;
    }

    static Variant PeakVariant(bool fast)
    {
        var variant = new Variant();
        var references = EmissionTests.References().ToList();
        var pack = Compile("Tl.Precombine.PeakPack", Toggle(EmissionTests.ReadResource("Tl.Systems.Tests.Precombine.PeakPack.cs"), fast), references, true, variant);
        var packReference = MetadataReference.CreateFromImage(pack);
        var game = Compile("Tl.Precombine.PeakGame", EmissionTests.ReadResource("Tl.Systems.Tests.Precombine.PeakGame.cs"), references.Append(packReference), true, variant);
        var host = Compile("PeakHost", PeakHostSource, references.Append(packReference).Append(MetadataReference.CreateFromImage(game)), false, variant);
        var context = new AssemblyLoadContext("Peak" + (fast ? "Fast" : "Plain"));
        variant.Domain = context.LoadFromStream(new MemoryStream(pack));
        variant.Game = context.LoadFromStream(new MemoryStream(game));
        variant.Host = context.LoadFromStream(new MemoryStream(host));
        return variant;
    }

    static Pair BuildPre()
    {
        var pair = new Pair { Fast = PreVariant(true), Plain = PreVariant(false) };
        pair.First = PreLibrary(pair.Fast.Domain, false);
        pair.Second = PreLibrary(pair.Fast.Domain, true);
        Assert.Equal(pair.First, PreLibrary(pair.Plain.Domain, false));
        Assert.NotEqual(pair.First, pair.Second);
        return pair;
    }

    static Pair BuildPeak()
    {
        var pair = new Pair { Fast = PeakVariant(true), Plain = PeakVariant(false) };
        pair.First = PeakLibrary(pair.Fast.Domain, pair.Fast.Game!);
        Assert.Equal(pair.First, PeakLibrary(pair.Plain.Domain, pair.Plain.Game!));
        return pair;
    }

    static byte[] Assemble(List<TypedTimeline> timelines, params Assembly[] assemblies)
    {
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), [], 60);
        var kinds = LibraryBake.Kinds(assemblies);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static byte[] PreLibrary(Assembly domain, bool second)
    {
        var riseTrack = domain.GetType("PreGame.RiseTrack", true)!;
        var riseClip = domain.GetType("PreGame.RiseClip", true)!;
        var markTrack = domain.GetType("PreGame.MarkTrack", true)!;
        var markClip = domain.GetType("PreGame.MarkClip", true)!;
        var gain = second ? 1.75f : 1f;
        var lift = second ? 2 : 0;
        TypedTrack Rise(float scale, params (uint Start, uint End, float Rise, int Count, float Speed, int Priority)[] clips)
        {
            var track = new TypedTrack { Type = riseTrack, Data = TestDomain.Pack(scale) };
            foreach (var clip in clips)
                track.Clips.Add(new TypedClip { Type = riseClip, Start = clip.Start, End = clip.End, Data = TestDomain.Pack(clip.Rise * gain, clip.Count + lift, clip.Speed + lift, clip.Priority) });
            return track;
        }
        TypedTrack Mark(params (uint Start, uint End, int Line)[] clips)
        {
            var track = new TypedTrack { Type = markTrack, Data = TestDomain.Pack(1f) };
            foreach (var clip in clips) track.Clips.Add(new TypedClip { Type = markClip, Start = clip.Start, End = clip.End, Data = TestDomain.Pack(clip.Line + lift) });
            return track;
        }
        return Assemble(
        [
            new() { Name = "hop", Duration = 12, Next = "walk", Tracks = { Rise(1f, (0, 12, 0.4f, 2, 1f, 0)), Mark((3, 8, 4)) } },
            new() { Name = "stand", Duration = 16, Hold = true, Tracks = { Mark((0, 16, 7)), Rise(1f, (5, 11, 1.1f, 7, 5f, 3)) } },
            new() { Name = "walk", Duration = 24, Loops = true, Tracks = { Rise(1f, (0, 10, 1.5f, 3, 2f, 1), (6, 16, 2.25f, 5, 4f, 2)), Rise(0.5f, (4, 20, 0.7f, 1, 3f, 2)), Mark((2, 9, 3), (12, 22, 5)) } },
        ], domain);
    }

    static byte[] PeakLibrary(Assembly pack, Assembly game)
    {
        var peak = pack.GetType("PeakPack.PeakClip", true)!;
        var dip = game.GetType("PeakGame.DipClip", true)!;
        TypedTrack Track(Type clip, params (uint Start, uint End, float Value)[] clips)
        {
            var track = new TypedTrack { Type = typeof(TlNoSettings<>).MakeGenericType(clip) };
            foreach (var c in clips) track.Clips.Add(new TypedClip { Type = clip, Start = c.Start, End = c.End, Data = TestDomain.Pack(c.Value) });
            return track;
        }
        return Assemble(
        [
            new() { Name = "dip", Duration = 24, Hold = true, Tracks = { Track(dip, (2, 22, 2f)) } },
            new() { Name = "mix", Duration = 40, Loops = true, Tracks = { Track(peak, (0, 14, 0.5f), (20, 34, 1.25f)), Track(dip, (8, 26, 0.75f)) } },
            new() { Name = "peak", Duration = 30, Next = "dip", Tracks = { Track(peak, (5, 25, 3f)) } },
        ], pack, game);
    }

    static ulong[] Play(Variant variant, string method, params object[] arguments)
        => (ulong[])variant.Host.GetType(method == "Play" ? "PeakHost" : "PreHost", true)!.GetMethod(method)!.Invoke(null, arguments)!;

    static void Same(ulong[] plain, ulong[] fast, int steps)
    {
        for (var step = 0; step < steps; step++)
            Assert.True(plain[step] == fast[step], $"the precombined path first differs from the span walk at step {step}.");
    }

    [Fact]
    public void OnePlaybackPerEntityMatchesTheSpanWalkBitForBitThroughEveryControl()
    {
        var pair = Pre.Value;
        var fast = Play(pair.Fast, "Single", pair.First, pair.Second, SingleSteps);
        var plain = Play(pair.Plain, "Single", pair.First, pair.Second, SingleSteps);
        Same(plain, fast, SingleSteps);
        for (var k = 0; k < 4; k++)
        {
            Assert.True(fast[SingleSteps + k] > 0, $"kind {k} did not build precombined values.");
            Assert.Equal(0UL, plain[SingleSteps + k]);
        }
        Assert.Equal(0UL, fast[SingleSteps + 4]);
        Assert.True(fast[SingleSteps + 5] > 0, "the Synthetic-reading feeder never saw a synthetic span.");
    }

    [Fact]
    public void ManyPlaybacksPerEntityMatchExactlyForIntegerAndPickKinds()
    {
        var pair = Pre.Value;
        var fast = Play(pair.Fast, "Many", pair.First, pair.Second, ManySteps);
        var plain = Play(pair.Plain, "Many", pair.First, pair.Second, ManySteps);
        Same(plain, fast, ManySteps);
        Assert.True(fast[ManySteps] > 0 && fast[ManySteps + 2] > 0, "the precombined path was not taken.");
        Assert.True(fast[ManySteps + 5] >= 3, $"the script kept at most {fast[ManySteps + 5]} listed playbacks per entity.");
    }

    [Fact]
    public void AForeignFedKindSkipsCellsWithoutItsOwnSpansAndMatchesTheSpanWalk()
    {
        var pair = Peak.Value;
        var fast = Play(pair.Fast, "Play", pair.First, 6, PeakSteps);
        var plain = Play(pair.Plain, "Play", pair.First, 6, PeakSteps);
        Same(plain, fast, PeakSteps);
        Assert.True(fast[PeakSteps] > 0, "the precombined path was not taken.");
        Assert.Equal(0UL, plain[PeakSteps]);
    }

    [Fact]
    public void OnlyOptedInKindsWithPureSpanFeedersArePrecombined()
    {
        var pre = Pre.Value;
        Assert.Contains("SpanValues<float, KindSpans_", pre.Fast.Generated);
        Assert.DoesNotContain("SpanValues<", pre.Plain.Generated);
        Assert.Empty(pre.Plain.Diagnostics);
        var info = Assert.Single(pre.Fast.Diagnostics);
        Assert.StartsWith("TLGEN115: Kind 'Seen' declares Associative but keeps the per-span path in system 'global::PreGame.PreSystem': feeder 'PreGame.Rise.Spot' reads FrameFlags.Synthetic.", info);
        var peak = Peak.Value;
        Assert.Contains("SpanValues<float, KindSpans_0>", peak.Fast.Generated);
        Assert.Empty(peak.Fast.Diagnostics);
    }

    const string Prelude = """
        using System;
        using Tl;
        namespace T
        {
        public struct M { public float X; }
        public struct Cl { public float V; }
        public struct Tr : IBlend<Cl> { public void Blend(in Cl a, in Cl b, float t, out Cl r) => r = a; }
        [TimelineSystem] public ref partial struct S { public Span<M> M; }
        [TimelineKind(Associative = true)] public struct K { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }
        """;

    static (string[] Codes, string Generated) Route(string body)
    {
        var compilation = KindEmissionTests.Compile(Prelude + body + "\n}\n", "Route");
        IncrementalParityTests.AssertParity(compilation);
        var emitted = SystemGeneration.Emit(compilation, "Route");
        return (emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message).ToArray(), string.Join("\n", emitted.Artifacts.Select(static a => a.Content)));
    }

    [Theory]
    [InlineData("public static float E(in Frame<Tr, Cl> f) => f.Clip.V * f.Direction;", "")]
    [InlineData("public static float E(in Frame<Tr, Cl> f) => f.Has(FrameFlags.Reverse) ? -f.Clip.V : f.Clip.V + f.Tick;", "")]
    [InlineData("public static float E(in Frame<Tr, Cl> f, in M m) => f.Clip.V * m.X;", "takes 'global::T.M'")]
    [InlineData("public static float E(in Frame<Tr, Cl> f) => (f.Flags & FrameFlags.ClipStart) != 0 ? 1f : f.Clip.V;", "reads f.Flags")]
    [InlineData("public static float E(in Frame<Tr, Cl> f) => f.Has(FrameFlags.Interrupted) ? 0f : f.Clip.V;", "reads FrameFlags.Interrupted")]
    [InlineData("public static float E(in Frame<Tr, Cl> f) => H(f); static float H(in Frame<Tr, Cl> g) => g.Clip.V;", "passes 'f' on whole")]
    [InlineData("public static void Fold(in Frame<Tr, Cl> f, out float v) => v = f.Has(FrameFlags.Synthetic) ? 0f : f.Clip.V; [Feeds(typeof(K))] public static float W(in float v) => v;", "takes a fold whose Fold reads FrameFlags.Synthetic")]
    public void APureSpanFeederPrecombinesItsKindAndAnyOtherSaysWhyNot(string feeder, string why)
    {
        var (codes, generated) = Route("public struct C : ITrack<Tr, Cl> { " + (feeder.Contains("[Feeds") ? feeder : "[Feeds(typeof(K))] " + feeder) + " }");
        if (why.Length == 0)
        {
            Assert.Empty(codes);
            Assert.Contains("SpanValues<float, KindSpans_0>", generated);
            return;
        }
        var info = Assert.Single(codes);
        Assert.StartsWith("TLGEN115: Kind 'K' declares Associative but keeps the per-span path in system 'global::T.S': feeder 'T.C.", info);
        Assert.Contains(why, info);
        Assert.DoesNotContain("SpanValues<", generated);
    }

    [Fact]
    public void KindsWithoutTheOptInOrInEachModeKeepTheSpanWalk()
    {
        const string feeder = "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K2))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }";
        var (codes, generated) = Route("[TimelineKind] public struct K2 { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }" + feeder);
        Assert.Empty(codes);
        Assert.DoesNotContain("SpanValues<", generated);
        (codes, generated) = Route("[TimelineKind(Mode = KindMode.Each, Associative = true)] public struct K2 { public static void Apply(in float t, ref M m) => m.X += t; }" + feeder);
        Assert.Contains("Mode is Each", Assert.Single(codes));
        Assert.DoesNotContain("SpanValues<", generated);
    }
}
