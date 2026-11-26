using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed partial class TwoAssemblyTests
{
    const string DenseHostSource = """
        using System;
        using Tl;
        using PushPack;
        using PushGame;

        public static class PushDenseHost
        {
            public static string Play(byte[] library, int rows, int steps, bool feedFirst, Action<KindStore> off)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                using var exactWorld = new TimelineWorld(library);
                using var exactPlayers = new PlaybackTable(rows);
                using var exactKinds = new KindStore(exactWorld, exactPlayers);
                off(exactKinds);
                var push = new Push[rows];
                var exactPush = new Push[rows];
                var wind = new Wind[rows];
                for (var e = 0; e < rows; e++)
                {
                    wind[e].Scale = 1f + 0.25f * e;
                    players.Start(e, (ushort)(1 + e % 4));
                    exactPlayers.Start(e, (ushort)(1 + e % 4));
                }
                for (var step = 0; step < steps; step++)
                {
                    world.Tick(players, true);
                    exactWorld.Tick(exactPlayers, true);
                    if (feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, true);
                    if (feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(exactWorld, exactPlayers, true);
                    new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = push }.Run(world, players, true);
                    new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = exactPush }.Run(exactWorld, exactPlayers, true);
                    if (!feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, true);
                    if (!feedFirst) new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(exactWorld, exactPlayers, true);
                    for (var e = 0; e < rows; e++)
                        if (BitConverter.SingleToInt32Bits(push[e].X) != BitConverter.SingleToInt32Bits(exactPush[e].X) || BitConverter.SingleToInt32Bits(push[e].Y) != BitConverter.SingleToInt32Bits(exactPush[e].Y))
                            return $"step {step} entity {e}: dense {push[e].X} {push[e].Y}, explicit {exactPush[e].X} {exactPush[e].Y}";
                }
                return "";
            }

            public static int Dense(byte[] library, int rows, int steps)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                var push = new Push[rows];
                var wind = new Wind[rows];
                for (var e = 0; e < rows; e++) players.Start(e, (ushort)(1 + e % 4));
                var dense = 0;
                for (var step = 0; step < steps; step++)
                {
                    world.Tick(players, true);
                    new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, true);
                    new Tl.Generated.TlAuto_Tl_Unity_PushPack { C0_Push = push }.Run(world, players, true);
                    for (var e = 0; e < rows; e++) if (players.SystemView.IsDense(e)) dense++;
                }
                return dense;
            }
        }
        """;

    [Fact]
    public void APrecombinedKindThatAnotherAssemblyAlsoFeedsStaysOffTheDensePathAndMatchesTheExplicitWorld()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var references = EmissionTests.References().ToList();
        var packSource = EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushPack.cs").Replace("    [TlKind]\n    public struct PushKind", "    [TlKind(Associative = true)]\n    public struct PushKind");
        Assert.Contains("[TlKind(Associative = true)]", packSource);
        var pack = Compile(PackName, [CSharpSyntaxTree.ParseText(packSource, parse, "PushPack.cs")], references, true);
        var game = Compile(GameName, [CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.TwoAssembly.PushGame.cs"), parse, "PushGame.cs")], references.Append(MetadataReference.CreateFromImage(pack)), true);
        var host = Compile("PushDenseHost", [CSharpSyntaxTree.ParseText(DenseHostSource, parse, "PushDenseHost.cs")], references.Append(MetadataReference.CreateFromImage(pack)).Append(MetadataReference.CreateFromImage(game)), false);
        var context = new AssemblyLoadContext("TwoAssemblyDense");
        var loadedPack = context.LoadFromStream(new MemoryStream(pack));
        var loadedGame = context.LoadFromStream(new MemoryStream(game));
        var play = context.LoadFromStream(new MemoryStream(host)).GetType("PushDenseHost", true)!.GetMethod("Play")!;
        var differs = (string)play.Invoke(null, [Library(loadedPack, loadedGame), 16, 300, true, (Action<KindStore>)(static store => store.DenseEnabled = false)])!;
        Assert.True(differs.Length == 0, "the dense world left the explicit one at " + differs);
        differs = (string)play.Invoke(null, [Library(loadedPack, loadedGame), 16, 300, true, KindSpans.EntriesOnly])!;
        Assert.True(differs.Length == 0, "the dense world left the entries-only one at " + differs);
        Assert.Equal(0, (int)play.DeclaringType!.GetMethod("Dense")!.Invoke(null, [Library(loadedPack, loadedGame), 16, 300])!);
    }
}
