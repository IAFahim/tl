using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed partial class TwoAssemblyTests
{
    const string RateHostSource = """
        using System;
        using Tl;
        using PushPack;
        using PushGame;

        public static class RateHost
        {
            public static unsafe float[] Trace(byte[] library, ushort start, ushort rate, int[] groups, bool[] forward)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(1);
                using var kinds = new KindStore(world, players);
                var push = new Push[1];
                var wind = new Wind[1];
                wind[0].Scale = 1.5f;
                players.Start(0, start);
                if (rate != 0) players.SetSpeed(0, start, rate);
                var trace = new float[groups.Length * 5];
                for (var i = 0; i < groups.Length; i++)
                {
                    for (var g = 0; g < groups[i]; g++)
                    {
                        world.Tick(players, forward[i]);
                        new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, forward[i]);
                        new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = push }.Run(world, players, forward[i]);
                    }
                    var view = players.View;
                    var count = view.CountOf(0);
                    trace[i * 5] = push[0].X;
                    trace[i * 5 + 1] = push[0].Y;
                    trace[i * 5 + 2] = count;
                    trace[i * 5 + 3] = count > 0 ? view.SlotOf(0, 0)->Timeline : -1;
                    trace[i * 5 + 4] = count > 0 ? view.SlotOf(0, 0)->Tick : -1;
                }
                return trace;
            }
        }
        """;

    static readonly Lazy<Assembly> RateHostAssembly = new(BuildRateHost);

    static Assembly BuildRateHost()
    {
        var built = Assemblies.Value;
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var references = EmissionTests.References().Append(MetadataReference.CreateFromImage(built.PackImage)).Append(MetadataReference.CreateFromImage(built.GameImage));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create("RateHost", [CSharpSyntaxTree.ParseText(RateHostSource, parse, "RateHost.cs")], references, options);
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return AssemblyLoadContext.GetLoadContext(built.Pack)!.LoadFromStream(new MemoryStream(stream.ToArray()));
    }

    static byte[] HandoverLibrary(Assembly pack, Assembly game, bool usePush = true, bool useGust = true, bool handover = true)
    {
        var push = pack.GetType("PushPack.PushClip", true)!;
        var gust = game.GetType("PushGame.GustClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "lead", Duration = 7, Channel = "move", Next = handover ? "tail" : null },
            new() { Name = "tail", Duration = 9, Loops = true, Channel = "move" },
        };
        if (usePush) { timelines[0].Tracks.Add(Track(push, (0, 7, Floats(1f, 0.5f, 2f)))); timelines[1].Tracks.Add(Track(push, (3, 8, Floats(-1f, 1f, 1f)))); }
        if (useGust) { timelines[0].Tracks.Add(Track(gust, (2, 6, Floats(3f, 1f)))); timelines[1].Tracks.Add(Track(gust, (0, 9, Floats(0.5f, 2f)))); }
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

    internal static float[] RateTrace(byte[] library, ushort rate, int[] groups, bool[] forward)
        => (float[])RateHostAssembly.Value.GetType("RateHost", true)!.GetMethod("Trace")!.Invoke(null, [library, (ushort)1, rate, groups, forward])!;

    static (int[] Groups, bool[] Forward) Schedule(ushort rate, int steps, int reverseFrom, int reverseTo)
    {
        var groups = new int[steps];
        var forward = new bool[steps];
        var phase = 0;
        for (var i = 0; i < steps; i++)
        {
            groups[i] = (phase + rate) >> 4;
            phase = (phase + rate) & 15;
            forward[i] = !(i >= reverseFrom && i < reverseTo);
        }
        return (groups, forward);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(48)]
    [InlineData(64)]
    public void ARatedPlaybackHandsOverAndFeedsAForeignKindLikeOneXPlaybackOverTheCrossedTicks(ushort rate)
    {
        var built = Assemblies.Value;
        var library = HandoverLibrary(built.Pack, built.Game);
        var (groups, forward) = Schedule(rate, 60, 22, 40);
        var rated = RateTrace(library, rate, new int[groups.Length].Select(static _ => 1).ToArray(), forward);
        var reference = RateTrace(library, 0, groups, forward);
        var ticks = 0;
        var handedOver = false;
        var reversed = false;
        for (var i = 0; i < groups.Length; i++)
        {
            ticks += groups[i];
            Assert.True(rated[i * 5] == reference[i * 5], $"rate {rate} step {i}: X {rated[i * 5]} != {reference[i * 5]}");
            Assert.True(rated[i * 5 + 1] == reference[i * 5 + 1], $"rate {rate} step {i}: Y {rated[i * 5 + 1]} != {reference[i * 5 + 1]}");
            if (ticks > 0)
                for (var field = 2; field < 5; field++)
                    Assert.True(rated[i * 5 + field] == reference[i * 5 + field], $"rate {rate} step {i}: slot field {field} {rated[i * 5 + field]} != {reference[i * 5 + field]}");
            handedOver |= rated[i * 5 + 3] == 2;
            reversed |= !forward[i];
        }
        Assert.True(handedOver);
        Assert.True(reversed);
    }
}
