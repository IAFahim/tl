using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class KindShapePureHashTests
{
    const int StepsLikeTheBench = 1 + 40 + 16;
    const int LiveTracks = 8;

    const string BenchHostSource = """
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using System.Linq;
        using Tl;
        using Tl.Gen.Tlb;
        using KindPure;

        public static unsafe class KindShapePureHost
        {
            const int TimelineCount = 500;
            const int TracksPerTimeline = 16;
            const int Shortest = 30;
            const int Spread = 31;
            const int BlendOverlap = 5;

            static int DurationOf(int timeline) => Shortest + timeline * 7 % Spread;

            static bool Blends(int track) => track < 4 || track == 8 || track == 9;

            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            static byte[] Raw<T>(T value) where T : unmanaged
            {
                var bytes = new byte[sizeof(T)];
                fixed (byte* at = bytes) *(T*)at = value;
                return bytes;
            }

            static TypedClip Clip(Type type, uint start, uint end, byte[] data) => new TypedClip { Type = type, Start = start, End = end, Data = data };

            public static byte[] Library(int liveTracks)
            {
                var names = new List<string>();
                for (var i = 0; i < TimelineCount; i++) names.Add("shape" + (i + 1).ToString("D4", CultureInfo.InvariantCulture));
                var manifest = LibraryBake.Manifest(names, new List<string>(), 60);
                var kinds = LibraryBake.Kinds(new[] { typeof(WalkTrack).Assembly });
                var parts = new List<LibraryPart>();
                var state = 0x9E3779B9u;
                for (var i = 0; i < TimelineCount; i++)
                {
                    var duration = DurationOf(i);
                    var timeline = new TypedTimeline { Name = names[i], Duration = (uint)duration, Loops = true };
                    for (var track = 0; track < TracksPerTimeline; track++)
                    {
                        var covered = Math.Max(1, duration * liveTracks / TracksPerTimeline);
                        var start = Next(ref state) % (uint)(duration - covered + 1);
                        var end = start + (uint)covered;
                        var split = Blends(track) && covered >= 2 * BlendOverlap - 1;
                        var firstEnd = split ? start + (uint)((covered + BlendOverlap) / 2) : end;
                        var secondStart = firstEnd - BlendOverlap;
                        var seed = i + track;
                        var typed = new TypedTrack { Name = "t" + track };
                        if (track < 8)
                        {
                            typed.Type = typeof(ArcTrack);
                            typed.Data = Raw(new ArcTrack { Scale = 1f });
                            typed.Clips.Add(Clip(typeof(ArcClip), start, firstEnd, BitConverter.GetBytes((float)(1 + seed % 5))));
                            if (split) typed.Clips.Add(Clip(typeof(ArcClip), secondStart, end, BitConverter.GetBytes((float)(1 + (seed + 2) % 5))));
                        }
                        else if (track < 12)
                        {
                            var priority = (byte)(seed % 3);
                            typed.Type = typeof(WalkTrack);
                            typed.Data = Raw(new WalkTrack { Scale = 1f });
                            var walk = Raw(new WalkClip { Speed = 1 + priority, Priority = priority });
                            typed.Clips.Add(Clip(typeof(WalkClip), start, firstEnd, walk));
                            if (split) typed.Clips.Add(Clip(typeof(WalkClip), secondStart, end, walk));
                        }
                        else if (track < 14)
                        {
                            typed.Type = typeof(HitTrack);
                            typed.Data = Raw(new HitTrack { Scale = 1f });
                            typed.Clips.Add(Clip(typeof(HitClip), start, end, BitConverter.GetBytes(1 + seed % 5)));
                        }
                        else
                        {
                            typed.Type = typeof(StatTrack);
                            typed.Data = Raw(new StatTrack { Scale = 1f });
                            typed.Clips.Add(Clip(typeof(StatClip), start, end, BitConverter.GetBytes(1 + seed % 3)));
                        }
                        timeline.Tracks.Add(typed);
                    }
                    var tlb = LibraryBake.BakeTyped(timeline, manifest);
                    parts.Add(new LibraryPart { Name = names[i], Tlb = tlb, Kinds = LibraryBake.KindPayload(names[i], tlb, kinds) });
                }
                return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
            }

            public static ulong[] Play(byte[] library, int rows, int active, int steps)
            {
                var kindNames = typeof(WalkTrack).Assembly.GetTypes().Where(t => t.Namespace == "KindPure" && t.IsDefined(typeof(TimelineKindAttribute), false)).Select(static t => t.FullName!).OrderBy(static n => n, StringComparer.Ordinal).ToArray();
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows, active);
                var reach = new ulong[rows];
                Array.Fill(reach, (1ul << kindNames.Length) - 1);
                using var store = new KindStore(world, players, kindNames, reach);
                var motion = new Motion[rows];
                var stat = new WalkStat[rows];
                var power = new JumpPower[rows];
                var stamina = new Stamina[rows];
                var health = new Health[rows];
                for (var e = 0; e < rows; e++)
                {
                    stat[e].Value = 1f;
                    power[e].Value = 1f;
                    health[e].Value = 1_000_000;
                    for (var k = 0; k < active; k++)
                        players.Start(e, (ushort)(1 + (e * 7 + k * 13) % TimelineCount), (ushort)((e * 31 + k * 17) % Shortest));
                }
                for (var step = 0; step < steps; step++)
                {
                    world.Tick(players, true);
                    new Movement { Motion = motion, WalkStat = stat, JumpPower = power, Stamina = stamina }.Run(world, players, true);
                    new Vitals { Health = health }.Run(world, players, true);
                }
                var h = TlStateHash.Begin();
                for (var e = 0; e < rows; e++)
                {
                    h.Float(motion[e].X);
                    h.Float(motion[e].Y);
                    h.Int(stamina[e].Value);
                    h.Int(health[e].Value);
                    h.Slots(players.View.RunOf(e), players.View.CountOf(e));
                }
                return new[] { h.Value, (ulong)store.SpanValueBytes(store.IndexOf("KindPure.Locomotion")), (ulong)store.SpanValueBytes(store.IndexOf("KindPure.Lift")) };
            }
        }
        """;

    sealed class Arm
    {
        public Assembly Host = null!;
        public byte[] Library = null!;
    }

    static readonly Lazy<Arm> Precombined = new(() => Build(baseline: false, null));
    static readonly Lazy<Arm> SpanWalk = new(() => Build(baseline: true, Precombined.Value.Library));

    static byte[] Compile(string name, string source, CSharpParseOptions parse, IEnumerable<MetadataReference> references, bool generate)
    {
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, parse, name + ".cs")], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Enable));
        if (generate)
        {
            var emitted = SystemGeneration.Emit(compilation, "KindShape");
            Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message)));
            foreach (var artifact in emitted.Artifacts) compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, parse, artifact.RelativePath));
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    static Arm Build(bool baseline, byte[]? library)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: baseline ? new[] { "TL_BASELINE" } : Array.Empty<string>());
        var bake = typeof(LibraryBake).Assembly.Location;
        var references = EmissionTests.References().Where(r => r.Display != bake).Append(MetadataReference.CreateFromFile(bake)).ToList();
        var domain = Compile("KindShape", EmissionTests.ReadResource("Tl.Systems.Tests.KindShapeBench.PureDomain.cs"), parse, references, true);
        var host = Compile("KindShapePureHost", BenchHostSource, parse, references.Append(MetadataReference.CreateFromImage(domain)), false);
        var context = new AssemblyLoadContext("KindShapePure" + (baseline ? "SpanWalk" : "Precombined"));
        context.LoadFromStream(new MemoryStream(domain));
        var arm = new Arm { Host = context.LoadFromStream(new MemoryStream(host)) };
        arm.Library = library ?? (byte[])Method(arm, "Library").Invoke(null, [LiveTracks])!;
        return arm;
    }

    static MethodInfo Method(Arm arm, string name) => arm.Host.GetType("KindShapePureHost", true)!.GetMethod(name)!;

    [Theory]
    [InlineData(64, 4, 0x025D8DC5105F05E7UL)]
    [InlineData(64, 12, 0xF9E653C956890F1EUL)]
    [InlineData(300, 4, 0x0E3A8FF84D68D7FEUL)]
    [InlineData(300, 12, 0x4F9E8B886CFE80A5UL)]
    public void ThePureKindShapeDomainHashesLikeTheBenchOnBothPaths(int rows, int active, ulong pinned)
    {
        foreach (var (arm, precombined) in new[] { (Precombined.Value, true), (SpanWalk.Value, false) })
        {
            var played = (ulong[])Method(arm, "Play").Invoke(null, [arm.Library, rows, active, StepsLikeTheBench])!;
            Assert.True(pinned == played[0], $"{(precombined ? "precombined" : "per-span")} path at {rows} rows x {active} active hashes {played[0]:X16}, pinned {pinned:X16}.");
            Assert.Equal(precombined, played[1] > 0 && played[2] > 0);
            Assert.Equal(precombined, played[1] > 0 || played[2] > 0);
        }
    }
}
