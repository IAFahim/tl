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
    const int PrecombinedKinds = 6, FlareBytes = 4, TallyBytes = 5, SeenBytes = 6, GlowBytes = 7, Extra = 8, FireShared = 9, FirePulsesSent = 10;

    const string PreHostSource = """
        using System;
        using Tl;
        using PreGame;

        public static unsafe class PreHost
        {
            const ushort Hop = 1, Stand = 2, Walk = 3;
            static readonly string[] Kinds = { "PreGame.Lift", "PreGame.Count", "PreGame.Choice", "PreGame.Shown", "PreGame.Flare", "PreGame.Tally", "PreGame.Seen", "PreGame.Glow" };

            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            static ulong[] Tail(KindStore kinds, ulong[] hashes, int steps, long extra, long firePulses)
            {
                for (var k = 0; k < Kinds.Length; k++) hashes[steps + k] = (ulong)kinds.SpanValueBytes(kinds.IndexOf(Kinds[k]));
                hashes[steps + Kinds.Length] = (ulong)extra;
                var flare = kinds.Table(kinds.IndexOf("PreGame.Flare"));
                hashes[steps + Kinds.Length + 1] = flare != null && flare->Grouped && flare == kinds.Table(kinds.IndexOf("PreGame.Glow")) ? 1UL : 0UL;
                hashes[steps + Kinds.Length + 2] = (ulong)firePulses;
                return hashes;
            }

            static long FirePulses(KindStore kinds, int step, int rows)
            {
                if (step % 9 != 4) return 0;
                var sent = 0L;
                for (var e = step % 3; e < rows; e += 3)
                {
                    kinds.Pulses<int>("PreGame.Flare").To(e, 2 + e % 4);
                    kinds.Pulses<int>("PreGame.Glow").To(e, 3 - e % 5);
                    sent += 2;
                }
                return sent;
            }

            public static ulong[] Single(byte[] first, byte[] second, int steps)
            {
                const int rows = 12;
                using var world = new TimelineWorld(first);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var pre = new Pre[rows];
                var hashes = new ulong[steps + Kinds.Length + 3];
                var fire = 0L;
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
                    fire += FirePulses(kinds, step, rows);
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
                return Tail(kinds, hashes, steps, seen, fire);
            }

            public static ulong[] Many(byte[] first, byte[] second, int steps, int rows, int workerCount)
            {
                using var world = new TimelineWorld(first);
                using var players = new PlaybackTable(rows, 8);
                using var kinds = new KindStore(world, players);
                using var workers = workerCount > 0 ? new TlWorkers(workerCount) : null;
                var pre = new Pre[rows];
                var hashes = new ulong[steps + Kinds.Length + 3];
                var rng = 0x2545F491u;
                var most = 0;
                var fire = 0L;
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
                    fire += FirePulses(kinds, step, rows);
                    var forward = !(step >= 100 && step < 120);
                    world.Tick(players, forward);
                    if (workers != null) new PreSystem { Pre = pre }.Run(world, players, forward, workers);
                    else new PreSystem { Pre = pre }.Run(world, players, forward);
                    var h = TlStateHash.Begin();
                    for (var e = 0; e < rows; e++)
                    {
                        h.Int(pre[e].Count);
                        h.Float(pre[e].Pick);
                        h.Int(pre[e].PickPriority);
                        h.Int(pre[e].Shown);
                        h.Int(pre[e].Seen);
                        h.Int(pre[e].Applied);
                        h.Int(pre[e].Flare);
                        h.Int(pre[e].Glow);
                        h.Int((int)(pre[e].Tally >> 96));
                        h.Int((int)(pre[e].Tally >> 64));
                        h.Int((int)(pre[e].Tally >> 32));
                        h.Int((int)pre[e].Tally);
                        h.Slots(players.View.RunOf(e), players.View.CountOf(e));
                        var listed = 0;
                        for (var s = 0; s < players.View.CountOf(e); s++) if (players.View.RunOf(e)[s].Timeline != 0 && KindScore.Listed(players.View.RunOf(e)[s].Flags)) listed++;
                        most = Math.Max(most, listed);
                    }
                    hashes[step] = h.Value;
                }
                return Tail(kinds, hashes, steps, most, fire);
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
        for (var k = 0; k < PrecombinedKinds; k++)
        {
            Assert.True(fast[SingleSteps + k] > 0, $"kind {k} did not build precombined values.");
            Assert.Equal(0UL, plain[SingleSteps + k]);
        }
        Assert.Equal(0UL, fast[SingleSteps + SeenBytes]);
        Assert.Equal(0UL, fast[SingleSteps + GlowBytes]);
        Assert.True(fast[SingleSteps + Extra] > 0, "the Synthetic-reading feeder never saw a synthetic span.");
    }

    [Fact]
    public void ManyPlaybacksPerEntityMatchExactlyForIntegerAndPickKinds()
    {
        var pair = Pre.Value;
        var fast = Play(pair.Fast, "Many", pair.First, pair.Second, ManySteps, 16, 0);
        var plain = Play(pair.Plain, "Many", pair.First, pair.Second, ManySteps, 16, 0);
        Same(plain, fast, ManySteps);
        Assert.True(fast[ManySteps] > 0 && fast[ManySteps + 2] > 0, "the precombined path was not taken.");
        Assert.True(fast[ManySteps + Extra] >= 3, $"the script kept at most {fast[ManySteps + Extra]} listed playbacks per entity.");
    }

    [Fact]
    public void WorkersBuildTheValuesOnceAndMatchTheSequentialSpanWalk()
    {
        var pair = Pre.Value;
        var fast = Play(pair.Fast, "Many", pair.First, pair.Second, ManySteps, 640, 4);
        var plain = Play(pair.Plain, "Many", pair.First, pair.Second, ManySteps, 640, 0);
        Same(plain, fast, ManySteps);
        Assert.True(fast[ManySteps] > 0 && fast[ManySteps + 2] > 0, "the precombined path was not taken.");
    }

    [Fact]
    public void AGroupWhoseFirstMemberIsPrecombinedAndSecondIsNotSharesOneTableAndMatchesTheSpanWalk()
    {
        var pair = Pre.Value;
        foreach (var (rows, workers) in new[] { (16, 0), (640, 4) })
        {
            var fast = Play(pair.Fast, "Many", pair.First, pair.Second, ManySteps, rows, workers);
            var plain = Play(pair.Plain, "Many", pair.First, pair.Second, ManySteps, rows, 0);
            Same(plain, fast, ManySteps);
            Assert.True(fast[ManySteps + FlareBytes] > 0, "the grouped Flare kind did not read precombined values.");
            Assert.Equal(0UL, plain[ManySteps + FlareBytes]);
            Assert.Equal(0UL, fast[ManySteps + GlowBytes]);
            Assert.Equal(1UL, fast[ManySteps + FireShared]);
            Assert.Equal(1UL, plain[ManySteps + FireShared]);
            Assert.True(fast[ManySteps + FirePulsesSent] > 0, "no game pulse reached the shared Fire rows.");
        }
    }

    [Fact]
    public void AnInt128KindReadsItsPrecombinedValuesAndMatchesTheSpanWalk()
    {
        var pair = Pre.Value;
        var fast = Play(pair.Fast, "Many", pair.First, pair.Second, ManySteps, 16, 0);
        var plain = Play(pair.Plain, "Many", pair.First, pair.Second, ManySteps, 16, 0);
        Same(plain, fast, ManySteps);
        Assert.True(fast[ManySteps + TallyBytes] > 0, "the Int128 Tally kind did not read precombined values.");
        Assert.Equal(0UL, plain[ManySteps + TallyBytes]);
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
        Assert.Contains("__b.Add<float, KindSpans_", pre.Fast.Generated);
        Assert.Contains("if (!Fused(world, players, __ks", pre.Fast.Generated);
        Assert.DoesNotContain("KindFill", pre.Plain.Generated);
        Assert.Empty(pre.Plain.Diagnostics);
        Assert.Equal(2, pre.Fast.Diagnostics.Count);
        Assert.Contains(pre.Fast.Diagnostics, static d => d.StartsWith("TLGEN115: Kind 'Glow' declares Associative but keeps the per-span path in system 'global::PreGame.PreSystem': feeder 'PreGame.Mark.Shine' reads FrameFlags.Synthetic.", StringComparison.Ordinal));
        Assert.Contains(pre.Fast.Diagnostics, static d => d.StartsWith("TLGEN115: Kind 'Seen' declares Associative but keeps the per-span path in system 'global::PreGame.PreSystem': feeder 'PreGame.Rise.Spot' reads FrameFlags.Synthetic.", StringComparison.Ordinal));
        Assert.Contains("__b.Add<int, KindSpans_", pre.Fast.Generated);
        Assert.Contains("__b.Add<System.Int128, KindSpans_", pre.Fast.Generated);
        var peak = Peak.Value;
        Assert.Contains("__b.Add<float, KindSpans_0>", peak.Fast.Generated);
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
            Assert.Contains("__b.Add<float, KindSpans_0>", generated);
            return;
        }
        var info = Assert.Single(codes);
        Assert.StartsWith("TLGEN115: Kind 'K' declares Associative but keeps the per-span path in system 'global::T.S': feeder 'T.C.", info);
        Assert.Contains(why, info);
        Assert.DoesNotContain("KindFill", generated);
    }

    [Fact]
    public void KindsWithoutTheOptInOrInEachModeKeepTheSpanWalk()
    {
        const string feeder = "public struct C : ITrack<Tr, Cl> { [Feeds(typeof(K2))] public static float E(in Frame<Tr, Cl> f) => f.Clip.V; }";
        var (codes, generated) = Route("[TimelineKind] public struct K2 { public static void Combine(ref float t, in float n) => t += n; public static void Apply(in float t, ref M m) => m.X += t; }" + feeder);
        Assert.Empty(codes);
        Assert.DoesNotContain("KindFill", generated);
        (codes, generated) = Route("[TimelineKind(Mode = KindMode.Each, Associative = true)] public struct K2 { public static void Apply(in float t, ref M m) => m.X += t; }" + feeder);
        Assert.Contains("Mode is Each", Assert.Single(codes));
        Assert.DoesNotContain("KindFill", generated);
    }
}

public sealed class PrecombinePulseTests
{
    const string DomainSource = """
        using System;
        using Tl;
        namespace PulseOracle
        {
        public struct State
        {
            public long CombSum;
            public long CombHistory;
            public long EachLog;
            public int EachCount;
            public long Folded;
            public long FallbackTotal;
            public long SynthFallbackTotal;
        }
        public struct PulseClip { public int Val; }
        public struct NoTrack : IBlend<PulseClip>
        {
            public float Gain;
            public void Blend(in PulseClip a, in PulseClip b, float t, out PulseClip r) => r = a;
        }
        [TimelineKind(Accepts = KindEntries.Pulses)]
        public struct Comb
        {
            public static void Combine(ref long total, in long next) => total = total * 31 + next;
            public static void Apply(in long total, ref State s) { s.CombHistory = s.CombHistory * 131 + total; s.CombSum += total; }
        }
        [TimelineKind(Mode = KindMode.Each, Accepts = KindEntries.Pulses)]
        public struct Each
        {
            public static void Apply(in int total, ref State s) { s.EachLog = s.EachLog * 31 + total; s.EachCount++; }
        }
        [TimelineKind(Accepts = KindEntries.Pulses)]
        public struct Folded
        {
            public static void Combine(ref long total, in long next) => total = total * 17 + next;
            public static void Apply(in long total, ref State s) => s.Folded = s.Folded * 97 + total;
        }
        [TimelineKind(Accepts = KindEntries.Pulses)]
        public struct Fallback
        {
            public static void Combine(ref long total, in long next) => total = total * 13 + next;
            public static void Apply(in long total, ref State s) => s.FallbackTotal = s.FallbackTotal * 41 + total;
        }
        [TimelineKind(Accepts = KindEntries.Pulses)]
        public struct SynthFallback
        {
            public static void Combine(ref long total, in long next) => total = total * 19 + next;
            public static void Apply(in long total, ref State s) => s.SynthFallbackTotal = s.SynthFallbackTotal * 43 + total;
        }
        public struct PulseTrack : ITrack<NoTrack, PulseClip>
        {
            public static void Fold(in Frame<NoTrack, PulseClip> f, out int v) => v = f.Clip.Val * 10 + f.WithinClip;
            [Pulse(typeof(Comb), On = ClipEvent.Start)]
            public static long CombStart(in Frame<NoTrack, PulseClip> f) { __SLOW__ return f.Clip.Val * 100L + f.Tick * 3L + (f.IsBackward ? 1L : 0L); }
            [Pulse(typeof(Comb), On = ClipEvent.End)]
            public static long CombEnd(in Frame<NoTrack, PulseClip> f) { __SLOW__ return f.Clip.Val * 1000L + f.Tick * 5L + (f.IsBackward ? 2L : 0L); }
            [Pulse(typeof(Each), On = ClipEvent.Start)]
            public static int EachStart(in Frame<NoTrack, PulseClip> f) { __SLOW__ return f.Clip.Val * 10 + (f.IsBackward ? 1 : 0); }
            [Pulse(typeof(Each), On = ClipEvent.End)]
            public static int EachEnd(in Frame<NoTrack, PulseClip> f) { __SLOW__ return -(f.Clip.Val * 10 + (f.IsBackward ? 1 : 0)); }
            [Pulse(typeof(Folded), On = ClipEvent.Start)]
            public static long FoldedStart(in Frame<NoTrack, PulseClip> f, in int v) { __SLOW__ return v * 10L + (f.IsBackward ? 2L : 1L); }
            [Pulse(typeof(Folded), On = ClipEvent.End)]
            public static long FoldedEnd(in Frame<NoTrack, PulseClip> f, in int v) { __SLOW__ return v * 20L + (f.IsBackward ? 4L : 3L); }
            [Pulse(typeof(Fallback), On = ClipEvent.Start)]
            public static long FallbackStart(in Frame<NoTrack, PulseClip> f) => f.Has(FrameFlags.Interrupted) ? 999L : f.Clip.Val * 7L;
            [Pulse(typeof(Fallback), On = ClipEvent.End)]
            public static long FallbackEnd(in Frame<NoTrack, PulseClip> f) => f.Has(FrameFlags.Interrupted) ? -777L : f.Clip.Val * 11L;
            [Pulse(typeof(SynthFallback), On = ClipEvent.End)]
            public static long SynthEnd(in Frame<NoTrack, PulseClip> f) => f.Has(FrameFlags.Synthetic) ? 555L : f.Clip.Val * 13L;
        }
        [TimelineSystem]
        public ref partial struct PulseSystem
        {
            public Span<State> States;
        }
        }
        """;

    const string HarnessSource = """
        using System;
        using System.Runtime.InteropServices;
        using PulseOracle;
        using Tl;
        namespace PulseHost
        {
        public sealed unsafe class Harness : IDisposable
        {
            public readonly TimelineWorld World;
            public readonly PlaybackTable Players;
            public readonly KindStore Store;
            public readonly State[] States;
            readonly TlWorkers? _workers;

            public Harness(byte[] library, int entities, int workers = 1)
            {
                World = new TimelineWorld(library);
                Players = new PlaybackTable(entities);
                Store = new KindStore(World, Players);
                States = new State[entities];
                if (workers > 1) _workers = new TlWorkers(workers);
            }

            public void Dispose()
            {
                _workers?.Dispose();
                Players.Dispose();
                World.Dispose();
            }

            public void TickAndRun(bool forward = true)
            {
                World.Tick(Players, forward);
                RunOnly(forward);
            }

            public void RunOnly(bool forward = true)
            {
                var sys = new PulseSystem { States = States };
                if (_workers is not null) sys.Run(World, Players, forward, _workers);
                else sys.Run(World, Players, forward);
            }

            public byte[] Snapshot() => MemoryMarshal.AsBytes<State>(States).ToArray();
            public long PulseBytes(string kind) => Store.PulseValueBytes(Store.IndexOf(kind));
            public void PulseComb(int entity, long value) => Store.Pulses<long>("PulseOracle.Comb").To(entity, value);
            public void PulseEach(int entity, int value) => Store.Pulses<int>("PulseOracle.Each").To(entity, value);
            public void SetEnabled(int entity, string kind, bool on)
            {
                var k = Store.IndexOf(kind);
                Store.Table(k)->SetEnabled(entity, on);
            }
            public long CombSum(int e) => States[e].CombSum;
            public long CombHistory(int e) => States[e].CombHistory;
            public long EachLog(int e) => States[e].EachLog;
            public int EachCount(int e) => States[e].EachCount;
            public long Folded(int e) => States[e].Folded;
        }
        }
        """;

    sealed record PulseVariant(Assembly Domain, Assembly Host, string Generated, IReadOnlyList<string> Diagnostics, string Name)
    {
        public dynamic CreateHarness(byte[] library, int entities, int workers = 1)
            => Activator.CreateInstance(Host.GetType("PulseHost.Harness", true)!, library, entities, workers)!;
    }

    static PulseVariant CompileVariant(bool fast, string tag)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var src = DomainSource.Replace("__SLOW__", fast ? "" : "if (f.Has(FrameFlags.Synthetic)) { } ");
        var compilation = CSharpCompilation.Create("Pulse" + tag, [CSharpSyntaxTree.ParseText(src, parse, "Pulse.cs")], EmissionTests.References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        var emitted = SystemGeneration.Emit(compilation, "Pulse" + tag);
        var diags = emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message).ToArray();
        Assert.Empty(diags);
        var gen = string.Join("\n", emitted.Artifacts.Select(static a => a.Content));
        foreach (var artifact in emitted.Artifacts)
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, parse, artifact.RelativePath));
        using var ms = new MemoryStream();
        var res = compilation.Emit(ms);
        Assert.True(res.Success, string.Join("\n", res.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        var img = ms.ToArray();
        var hostCompilation = CSharpCompilation.Create("PulseHost" + tag, [CSharpSyntaxTree.ParseText(HarnessSource, parse, "Host.cs")],
            EmissionTests.References().Append(MetadataReference.CreateFromImage(img)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        using var hms = new MemoryStream();
        var hres = hostCompilation.Emit(hms);
        Assert.True(hres.Success, string.Join("\n", hres.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        var alc = new AssemblyLoadContext("Pulse" + tag, false);
        return new PulseVariant(alc.LoadFromStream(new MemoryStream(img)), alc.LoadFromStream(new MemoryStream(hms.ToArray())), gen, diags, tag);
    }

    static readonly Lazy<PulseVariant> FastVariant = new(() => CompileVariant(true, "Fast"));
    static readonly Lazy<PulseVariant> SlowVariant = new(() => CompileVariant(false, "Slow"));

    static TypedTrack MakeTrack(Assembly domain, params (uint Start, uint End, int Val)[] clips)
    {
        var trackType = domain.GetType("PulseOracle.NoTrack", true)!;
        var clipType = domain.GetType("PulseOracle.PulseClip", true)!;
        var track = new TypedTrack { Type = trackType, Data = TestDomain.Pack(1f) };
        foreach (var (s, e, v) in clips)
            track.Clips.Add(new TypedClip { Type = clipType, Start = s, End = e, Data = TestDomain.Pack(v) });
        return track;
    }

    static byte[] BuildLibrary(Assembly domain, bool alternate = false)
    {
        var clip = new TypedTimeline
        {
            Name = "clip",
            Duration = 12,
            Tracks =
            {
                MakeTrack(domain, (2, 6, alternate ? 9 : 3)),
                MakeTrack(domain, (5, 10, alternate ? 4 : 7)),
            },
        };
        var loop = new TypedTimeline
        {
            Name = "loop",
            Duration = 6,
            Loops = true,
            Tracks =
            {
                MakeTrack(domain, (0, 3, alternate ? 8 : 2), (3, 6, 5)),
                MakeTrack(domain, (0, 6, 1)),
            },
        };
        var head = new TypedTimeline
        {
            Name = "head",
            Duration = 4,
            Next = "tail",
            Tracks = { MakeTrack(domain, (1, 4, alternate ? 11 : 4)) },
        };
        var tail = new TypedTimeline
        {
            Name = "tail",
            Duration = 5,
            Tracks = { MakeTrack(domain, (0, 4, alternate ? 12 : 6)) },
        };
        var mixedA = new TypedTimeline
        {
            Name = "mixedA",
            Duration = 8,
            Tracks = { MakeTrack(domain, (1, 4, 2), (5, 7, 3)) },
        };
        var mixedB = new TypedTimeline
        {
            Name = "mixedB",
            Duration = 8,
            Tracks = { MakeTrack(domain, (1, 5, 5), (6, 8, 7)) },
        };
        var mixedC = new TypedTimeline
        {
            Name = "mixedC",
            Duration = 8,
            Tracks = { MakeTrack(domain, (1, 6, 9), (2, 7, 11)) },
        };
        var unit = new TypedTimeline
        {
            Name = "unit",
            Duration = 1,
        };
        return TestDomain.Library([clip, head, loop, mixedA, mixedB, mixedC, tail, unit], domain);
    }

    [Fact]
    public void PrecombinedPulsesEmitAndMeasureOnlyPureStartAndEndFeedersWithZeroDiagnostics()
    {
        var fast = FastVariant.Value;
        var slow = SlowVariant.Value;
        Assert.Empty(fast.Diagnostics);
        Assert.Empty(slow.Diagnostics);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(fast.Generated, @"PulseValues<").Count);
        Assert.DoesNotContain("PulseValues<", slow.Generated);
        Assert.Contains("KindPulses_0", fast.Generated);
        Assert.Contains("KindPulses_1", fast.Generated);
        Assert.DoesNotContain("KindPulses_2", fast.Generated);
        Assert.Contains("KindPulses_3", fast.Generated);
        Assert.DoesNotContain("KindPulses_4", fast.Generated);

        using var fh = fast.CreateHarness(BuildLibrary(fast.Domain), 2);
        using var sh = slow.CreateHarness(BuildLibrary(slow.Domain), 2);
        fh.Players.Start(0, 1);
        sh.Players.Start(0, 1);
        fh.TickAndRun(true);
        sh.TickAndRun(true);

        Assert.True(fh.PulseBytes("PulseOracle.Comb") > 0);
        Assert.True(fh.PulseBytes("PulseOracle.Each") > 0);
        Assert.True(fh.PulseBytes("PulseOracle.Folded") > 0);
        Assert.Equal(0L, fh.PulseBytes("PulseOracle.Fallback"));
        Assert.Equal(0L, fh.PulseBytes("PulseOracle.SynthFallback"));
        Assert.Equal(0L, sh.PulseBytes("PulseOracle.Comb"));
        Assert.Equal(0L, sh.PulseBytes("PulseOracle.Each"));
        Assert.Equal(0L, sh.PulseBytes("PulseOracle.Folded"));
    }

    [Fact]
    public void ControlMatrixLoopWrapsHandoverAndReloadMatchSlowOracleAndExactValues()
    {
        var fast = FastVariant.Value;
        var slow = SlowVariant.Value;
        var libFast = BuildLibrary(fast.Domain, false);
        var libSlow = BuildLibrary(slow.Domain, false);
        const int n = 6;
        using var fh = fast.CreateHarness(libFast, n);
        using var sh = slow.CreateHarness(libSlow, n);
        void Both(Action<PlaybackTable> act) { act(fh.Players); act(sh.Players); }

        Both(p => p.Start(0, 1, 0));
        Both(p => p.Start(1, 1, 4));
        Both(p => p.Start(2, 3, 0));
        Both(p => { p.Start(3, 1, 0); p.SetSpeed(3, 1, 32); });
        Both(p => p.Start(4, 2, 0));
        Both(p => { p.Start(5, 1, 9); p.Reverse(5, 1, true); });

        for (var step = 0; step < 30; step++)
        {
            if (step == 3) Both(p => p.Stop(0, 1));
            if (step == 6) Both(p => p.Pause(1, 1));
            if (step == 9) Both(p => p.Resume(1, 1));
            if (step == 11) Both(p => p.Seek(1, 1, 2));
            if (step == 15) Both(p => p.Reverse(2, 3, true));
            if (step == 20)
            {
                fh.World.Reload(BuildLibrary(fast.Domain, true));
                sh.World.Reload(BuildLibrary(slow.Domain, true));
                Both(p => p.Start(0, 1, 1));
            }
            var dir = step is >= 17 and < 20 ? false : true;
            fh.TickAndRun(dir);
            sh.TickAndRun(dir);
            Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
            if (step == 11)
            {
                long hBefore = fh.CombHistory(1);
                Assert.Equal((long)sh.CombHistory(1), hBefore);
            }
        }
        Assert.Equal(6, (int)fh.EachCount(5));
        Assert.True((long)fh.CombSum(2) != 0);
        Assert.True((long)fh.Folded(2) != 0);
        Assert.True(fh.PulseBytes("PulseOracle.Comb") > 0);
    }

    [Fact]
    public void SeekDoesNotPulseAndStartAtTickZeroAndPositiveTickComputeExactOracleValues()
    {
        var fast = FastVariant.Value;
        var slow = SlowVariant.Value;
        using var fh = fast.CreateHarness(BuildLibrary(fast.Domain), 3);
        using var sh = slow.CreateHarness(BuildLibrary(slow.Domain), 3);

        fh.Players.Start(0, 1, 0); sh.Players.Start(0, 1, 0);
        fh.Players.Start(1, 1, 5); sh.Players.Start(1, 1, 5);
        fh.Players.Start(2, 1, 0); sh.Players.Start(2, 1, 0);

        for (var s = 0; s < 3; s++)
        {
            fh.TickAndRun(true);
            sh.TickAndRun(true);
            Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
        }
        Assert.Equal(1, (int)fh.EachCount(0));
        Assert.Equal(30, (int)fh.EachLog(0));
        Assert.Equal(3 * 100L + 2 * 3L, (long)fh.CombSum(0));
        Assert.Equal(3 * 100L + 2 * 3L, (long)fh.CombHistory(0));
        var foldedT2 = (3 * 10 + 0) * 10L + 1L;
        Assert.Equal(foldedT2, (long)fh.Folded(0));

        Assert.Equal(((3025L * 31L + 315L) * 31L + 715L), (long)fh.CombSum(1));

        long prevSum = fh.CombSum(2);
        int prevEach = fh.EachCount(2);
        fh.Players.Seek(2, 1, 4); sh.Players.Seek(2, 1, 4);
        fh.TickAndRun(true);
        sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
        Assert.Equal(prevSum, (long)fh.CombSum(2));
        Assert.Equal(prevEach, (int)fh.EachCount(2));

        fh.TickAndRun(true);
        sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
        Assert.True((int)fh.EachCount(2) > prevEach);
    }

    [Fact]
    public void MixedSameTickBackfillCloseGamePulsesAndDisabledRowMatchSlowOracle()
    {
        var fast = FastVariant.Value;
        var slow = SlowVariant.Value;
        var libF = BuildLibrary(fast.Domain);
        var libS = BuildLibrary(slow.Domain);
        const int n = 7;
        using var fh = fast.CreateHarness(libF, n);
        using var sh = slow.CreateHarness(libS, n);
        void Both(Action<PlaybackTable> act) { act(fh.Players); act(sh.Players); }

        Both(p =>
        {
            p.Start(0, 4, 0); p.Start(0, 8, 0); p.Start(0, 6, 0);
            p.Start(1, 4, 0); p.Start(1, 6, 0); p.SetSpeed(1, 6, 32);
            p.Start(2, 1, 1); p.Start(2, 4, 0);
            p.Start(3, 4, 0); p.Start(3, 6, 0);
            p.Start(5, 4, 0);
        });
        fh.TickAndRun(true); sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());

        Both(p =>
        {
            p.Start(0, 5, 1);
            p.Stop(2, 1);
            p.Start(6, 4, 0); p.Start(6, 6, 0);
        });
        fh.PulseComb(3, 777L); sh.PulseComb(3, 777L);
        fh.PulseEach(3, 88); sh.PulseEach(3, 88);
        fh.PulseComb(4, 999L); sh.PulseComb(4, 999L);
        fh.PulseEach(4, 55); sh.PulseEach(4, 55);
        fh.SetEnabled(5, "PulseOracle.Comb", false);
        fh.SetEnabled(5, "PulseOracle.Each", false);
        sh.SetEnabled(5, "PulseOracle.Comb", false);
        sh.SetEnabled(5, "PulseOracle.Each", false);

        fh.TickAndRun(true);
        sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());

        Both(p => p.SetSpeed(6, 6, 32));
        fh.TickAndRun(true);
        sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());

        fh.SetEnabled(5, "PulseOracle.Comb", true);
        fh.SetEnabled(5, "PulseOracle.Each", true);
        sh.SetEnabled(5, "PulseOracle.Comb", true);
        sh.SetEnabled(5, "PulseOracle.Each", true);

        fh.TickAndRun(true);
        sh.TickAndRun(true);
        Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
        Assert.Equal(2015L, (long)fh.CombSum(5));

        for (var s = 0; s < 5; s++)
        {
            if (s == 0)
            {
                fh.PulseComb(3, 111L); sh.PulseComb(3, 111L);
                fh.PulseEach(3, 44); sh.PulseEach(3, 44);
            }
            fh.TickAndRun(true);
            sh.TickAndRun(true);
            Assert.Equal((byte[])sh.Snapshot(), (byte[])fh.Snapshot());
        }
        Assert.Equal(3, (int)fh.EachCount(5));
        Assert.Equal(55, (int)fh.EachLog(4));
        Assert.Equal(999L, (long)fh.CombSum(4));
    }

    [Fact]
    public void ParallelFourWorkersAgreeBitForBitWithSingleWorkerAndSlowOracle()
    {
        var fast = FastVariant.Value;
        var slow = SlowVariant.Value;
        var libF = BuildLibrary(fast.Domain);
        var libS = BuildLibrary(slow.Domain);
        const int n = 200;
        using var par = fast.CreateHarness(libF, n, 4);
        using var seq = fast.CreateHarness(libF, n, 1);
        using var slw = slow.CreateHarness(libS, n, 1);

        for (var e = 0; e < n; e++)
        {
            ushort tl = (ushort)(e % 7 + 1);
            ushort at = (ushort)(e % 4);
            par.Players.Start(e, tl, at);
            seq.Players.Start(e, tl, at);
            slw.Players.Start(e, tl, at);
            if (e % 7 == 0)
            {
                par.Players.SetSpeed(e, tl, 24);
                seq.Players.SetSpeed(e, tl, 24);
                slw.Players.SetSpeed(e, tl, 24);
            }
        }
        par.TickAndRun(true); seq.TickAndRun(true); slw.TickAndRun(true);
        par.SetEnabled(65, "PulseOracle.Comb", false);
        seq.SetEnabled(65, "PulseOracle.Comb", false);
        slw.SetEnabled(65, "PulseOracle.Comb", false);
        par.SetEnabled(129, "PulseOracle.Each", false);
        seq.SetEnabled(129, "PulseOracle.Each", false);
        slw.SetEnabled(129, "PulseOracle.Each", false);

        for (var step = 0; step < 20; step++)
        {
            if (step == 5)
            {
                par.PulseComb(3, 123); seq.PulseComb(3, 123); slw.PulseComb(3, 123);
                par.PulseComb(130, 456); seq.PulseComb(130, 456); slw.PulseComb(130, 456);
                par.PulseEach(199, 77); seq.PulseEach(199, 77); slw.PulseEach(199, 77);
            }
            if (step == 10)
            {
                par.Players.Stop(64, (ushort)(64 % 7 + 1));
                seq.Players.Stop(64, (ushort)(64 % 7 + 1));
                slw.Players.Stop(64, (ushort)(64 % 7 + 1));
            }
            par.TickAndRun(true);
            seq.TickAndRun(true);
            slw.TickAndRun(true);
            var expected = (byte[])slw.Snapshot();
            Assert.Equal(expected, (byte[])seq.Snapshot());
            Assert.Equal(expected, (byte[])par.Snapshot());
            if (step is 1 or 5)
            {
                par.RunOnly(true);
                seq.RunOnly(true);
                slw.RunOnly(true);
                Assert.Equal(expected, (byte[])slw.Snapshot());
                Assert.Equal(expected, (byte[])seq.Snapshot());
                Assert.Equal(expected, (byte[])par.Snapshot());
            }
        }
    }
}
