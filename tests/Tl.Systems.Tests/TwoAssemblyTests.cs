using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed partial class TwoAssemblyTests
{
    internal const ulong Pinned = 12545803591333190611UL;
    const string PackName = "Tl.Unity.PushPack";
    const string GameName = "Tl.Unity.PushGame";

    const string HostSource = """
        using System;
        using Tl;
        using PushPack;
        using PushGame;

        public static class PushHost
        {
            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            static ulong Mix(ulong h, float v) { var s = new global::Tl.TlStateHash { Value = h }; s.FloatLane(v); return s.Value; }

            public static ulong Play(byte[] library, int rows, int steps, bool feedFirst)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var push = new Push[rows];
                var wind = new Wind[rows];
                for (var e = 0; e < rows; e++) wind[e].Scale = 1f + 0.25f * e;
                var rng = 0x6D2B79F5u;
                var h = global::Tl.TlStateHash.Seed;
                for (var step = 0; step < steps; step++)
                {
                    var forward = !(step >= 200 && step < 240);
                    for (var e = 0; e < rows; e++)
                    {
                        var r = Next(ref rng);
                        switch (r & 7)
                        {
                            case 0:
                            case 1:
                                players.Start(e, (ushort)(1 + (r >> 8) % 4));
                                break;
                            case 2:
                                players.Stop(e, (ushort)(1 + (r >> 8) % 4));
                                break;
                            case 3:
                                if (((r >> 8) & 15) == 0) players.StopAll(e);
                                break;
                        }
                    }
                    world.Tick(players, forward);
                    if (feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, forward);
                    new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = push }.Run(world, players, forward);
                    for (var e = 0; e < rows; e++)
                    {
                        h = Mix(h, push[e].X);
                        h = Mix(h, push[e].Y);
                    }
                }
                return h;
            }
        }
        """;

    static string Root()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        return dir;
    }

    sealed class Built
    {
        public Assembly Pack = null!;
        public Assembly Game = null!;
        public Assembly Host = null!;
        public string PackSource = "";
        public string GameSource = "";
        public byte[] PackImage = null!;
        public byte[] GameImage = null!;
        public Assembly All = null!;
        public Assembly AllHost = null!;
    }

    static readonly Lazy<Built> Assemblies = new(Build);

    static byte[] Compile(string name, IEnumerable<SyntaxTree> trees, IEnumerable<MetadataReference> references, bool generate, List<string>? generated = null)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create(name, trees, references, options);
        if (generate)
        {
            var emitted = SystemGeneration.Emit(compilation, name.Replace('.', '_'));
            Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message)));
            var parse = new CSharpParseOptions(LanguageVersion.Latest);
            foreach (var artifact in emitted.Artifacts)
            {
                generated?.Add(artifact.Content);
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, parse, artifact.RelativePath));
            }
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    static Built Build()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var references = EmissionTests.References().ToList();
        var packGenerated = new List<string>();
        var gameGenerated = new List<string>();
        var pack = Compile(PackName, [CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushPack.cs"), parse, "PushPack.cs")], references, true, packGenerated);
        var packReference = MetadataReference.CreateFromImage(pack);
        var game = Compile(GameName, [CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushGame.cs"), parse, "PushGame.cs")], references.Append(packReference), true, gameGenerated);
        var gameReference = MetadataReference.CreateFromImage(game);
        var host = Compile("PushHost", [CSharpSyntaxTree.ParseText(HostSource, parse, "PushHost.cs")], references.Append(packReference).Append(gameReference), false);
        var context = new AssemblyLoadContext("TwoAssembly");
        var built = new Built
        {
            Pack = context.LoadFromStream(new MemoryStream(pack)),
            Game = context.LoadFromStream(new MemoryStream(game)),
            PackImage = pack,
            GameImage = game,
            PackSource = string.Join("\n", packGenerated),
            GameSource = string.Join("\n", gameGenerated),
        };
        built.Host = context.LoadFromStream(new MemoryStream(host));
        var all = Compile("Tl.Unity.PushAll", [CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushPack.cs"), parse, "PushPack.cs"), CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushGame.cs"), parse, "PushGame.cs")], references, true);
        var allSource = HostSource.Replace("if (feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, forward);", "").Replace("new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = push }", "new Tl.Generated.TlAuto_Tl_Unity_PushAll { C0_Push = push, C1_Wind = wind }");
        var allHost = Compile("PushAllHost", [CSharpSyntaxTree.ParseText(allSource, parse, "PushAllHost.cs")], references.Append(MetadataReference.CreateFromImage(all)), false);
        var single = new AssemblyLoadContext("OneAssembly");
        built.All = single.LoadFromStream(new MemoryStream(all));
        built.AllHost = single.LoadFromStream(new MemoryStream(allHost));
        return built;
    }

    static byte[] Floats(params float[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), values[i]);
        return bytes;
    }

    static TypedTrack Track(Type clip, params (uint Start, uint End, byte[] Data)[] clips)
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<>).MakeGenericType(clip) };
        foreach (var (start, end, data) in clips) track.Clips.Add(new TypedClip { Type = clip, Start = start, End = end, Data = data });
        return track;
    }

    internal static byte[] Library(Assembly pack, Assembly game)
    {
        var push = pack.GetType("PushPack.PushClip", true)!;
        var gust = game.GetType("PushGame.GustClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "both", Duration = 50, Channel = "move", Tracks = { Track(push, (0, 40, Floats(-1f, 1f, 1f))), Track(gust, (10, 35, Floats(2f, -0.5f))) } },
            new() { Name = "breeze", Duration = 30, Hold = true, Tracks = { Track(gust, (0, 30, Floats(0.5f, 2f))) } },
            new() { Name = "gust", Duration = 24, Loops = true, Tracks = { Track(gust, (4, 20, Floats(3f, 1f))) } },
            new() { Name = "push", Duration = 40, Channel = "move", Tracks = { Track(push, (0, 30, Floats(1f, 0.5f, 2f))) } },
        };
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), ["move"], 60);
        var kinds = LibraryBake.Kinds([pack, game]);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }

    static ulong Play(Built built, byte[] library, bool feedFirst) => Play(built.Host, library, feedFirst);

    static ulong Play(Assembly host, byte[] library, bool feedFirst)
        => (ulong)host.GetType("PushHost", true)!.GetMethod("Play")!.Invoke(null, [library, 4, 600, feedFirst])!;

    [Fact]
    public void AKindFromOneAssemblyIsFedByATrackInAnotherAndKeepsItsPinnedHash()
    {
        var built = Assemblies.Value;
        Assert.DoesNotContain("global::PushGame.GustTrack", built.PackSource);
        Assert.Contains("global::PushPack.PushKind.Apply(", built.PackSource);
        Assert.DoesNotContain("global::PushPack.PushKind.Apply(", built.GameSource);
        Assert.DoesNotContain("global::PushPack.PushKind.Combine(", built.GameSource);
        Assert.Contains("global::PushGame.GustTrack.Evaluate(", built.GameSource);
        var library = Library(built.Pack, built.Game);
        var golden = Path.Combine(Root(), "tests", "Tl.Systems.Tests", "TwoAssembly", "push.tlib");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1") File.WriteAllBytes(golden, library);
        Assert.Equal(File.ReadAllBytes(golden), library);
        var hash = Play(built, library, true);
        Assert.Equal(hash, Play(built, library, true));
        var late = Assert.Throws<TargetInvocationException>(() => Play(built, library, false));
        Assert.Contains("feeders in the loaded library differ", late.InnerException!.Message);
        Assert.Equal(hash, Play(built.AllHost, Library(built.All, built.All), true));
        Assert.Equal(Pinned, hash);
    }
}
