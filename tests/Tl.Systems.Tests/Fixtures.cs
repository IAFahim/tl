using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

internal static unsafe class FixtureWorlds
{
    internal static readonly Lazy<Dictionary<string, ulong>> Keys = new(() =>
    {
        var tree = CSharpSyntaxTree.ParseText(EmissionTests.ToolboxSource, new CSharpParseOptions(LanguageVersion.Latest), "Toolbox.cs");
        var compilation = CSharpCompilation.Create("Tl.Systems.Tests", [tree], EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        ITypeSymbol Symbol(string name) => compilation.GetTypeByMetadataName(name)!;
        return new Dictionary<string, ulong>(StringComparer.Ordinal)
        {
            ["Move"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.MoveTrack"), Symbol("Game.MoveClip")),
            ["Arc"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.ArcTrack"), Symbol("Game.ArcClip")),
            ["Stat"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.StatTrack"), Symbol("Game.StatClip")),
            ["Input"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.InputTrack"), Symbol("Game.InputWindowClip")),
            ["Stomp"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.StompTrack"), Symbol("Game.StompClip")),
            ["Cue"] = Tl.Gen.CSharp.Analysis.SystemReader.StablePairKey(Symbol("Game.CueTrack"), Symbol("Game.CueClip")),
            ["Footstep"] = Tl.Gen.CSharp.Analysis.SystemReader.StableKey(Symbol("Game.FootstepMarker")),
            ["Movement"] = Tl.Gen.CSharp.Analysis.SystemReader.StableKey(Symbol("Game.Movement")),
            ["Combat"] = Tl.Gen.CSharp.Analysis.SystemReader.StableKey(Symbol("Game.Combat")),
        };
    });

    internal static byte[] Image<T>(in T value) where T : unmanaged
    {
        var image = new byte[sizeof(T)];
        MemoryMarshal.Write(image, in value);
        return image;
    }

    internal static byte[] LaneOf<Track, Clip>(in Track track, in Clip clip, ushort window, ushort length, Func<Frame<Track, Clip>, float> fold)
        where Track : unmanaged
        where Clip : unmanaged
    {
        var lane = new byte[window * 2 * 4];
        for (var tick = 0u; tick < window; tick++)
        {
            for (var dir = 0; dir < 2; dir++)
            {
                var flags = (dir == 1 ? FrameFlags.Reverse : FrameFlags.None)
                    | (tick == 0 ? FrameFlags.ClipStart : 0)
                    | (tick == window - 1 ? FrameFlags.ClipEnd : 0);
                var frame = new Frame<Track, Clip>(in track, in clip, (ushort)tick, length, (ushort)tick, 0, flags);
                var value = fold(frame);
                MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + dir) * 4, 4), in value);
            }
        }
        return lane;
    }

    internal static void BuildStandard(ViewScope scope)
    {
        var keys = Keys.Value;
        var run = new ViewScope.TimelineSpec { Loop = 1, Duration = 40 };
        run.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Move"],
            Track = Image(new Game.MoveTrack(1f)),
            First = Image(new Game.MoveClip(1f)),
            WindowStart = 0,
            WindowEnd = 40,
            Lane = LaneOf(new Game.MoveTrack(1f), new Game.MoveClip(1f), 40, 40, static frame =>
            {
                float lift;
                Game.MoveBatch.Fold(in frame, out lift);
                return lift;
            }),
        });
        run.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Cue"],
            Track = Image(new Game.CueTrack(1f)),
            First = Image(new Game.CueClip(10)),
            WindowStart = 0,
            WindowEnd = 1,
        });
        run.Markers.Add(new ViewScope.MarkerSpec { Tick = 10, Value = Image(new Game.FootstepMarker(1)) });
        run.Markers.Add(new ViewScope.MarkerSpec { Tick = 30, Value = Image(new Game.FootstepMarker(2)) });

        var jump = new ViewScope.TimelineSpec { Next = 1, Duration = 30, Channel = 1 };
        jump.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Arc"],
            Track = Image(new Game.ArcTrack(1f)),
            First = Image(new Game.ArcClip(2f)),
            WindowStart = 0,
            WindowEnd = 30,
            Lane = LaneOf(new Game.ArcTrack(1f), new Game.ArcClip(2f), 30, 30, static frame =>
            {
                float lift;
                Game.Jump.Fold(in frame, out lift);
                return lift;
            }),
        });
        jump.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Stat"],
            Track = Image(new Game.StatTrack(1f)),
            First = Image(new Game.StatClip(Game.Stat.JumpCounter, -1)),
            WindowStart = 0,
            WindowEnd = 1,
        });
        jump.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Cue"],
            Track = Image(new Game.CueTrack(1f)),
            First = Image(new Game.CueClip(20)),
            WindowStart = 0,
            WindowEnd = 1,
        });

        var slide = new ViewScope.TimelineSpec { Next = 1, Duration = 24, Channel = 1 };
        slide.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Move"],
            Track = Image(new Game.MoveTrack(1f)),
            First = Image(new Game.MoveClip(1.5f)),
            WindowStart = 0,
            WindowEnd = 24,
            Lane = LaneOf(new Game.MoveTrack(1f), new Game.MoveClip(1.5f), 24, 24, static frame =>
            {
                float lift;
                Game.MoveBatch.Fold(in frame, out lift);
                return lift;
            }),
        });
        slide.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Input"],
            Track = Image(new Game.InputTrack(1f)),
            First = Image(new Game.InputWindowClip(Game.Button.Attack, new TimelineId(4))),
            WindowStart = 6,
            WindowEnd = 14,
        });
        slide.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Cue"],
            Track = Image(new Game.CueTrack(1f)),
            First = Image(new Game.CueClip(30)),
            WindowStart = 0,
            WindowEnd = 1,
        });

        var barrel = new ViewScope.TimelineSpec { Duration = 20 };
        barrel.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Cue"],
            Track = Image(new Game.CueTrack(1f)),
            First = Image(new Game.CueClip(100)),
            WindowStart = 0,
            WindowEnd = 1,
        });
        barrel.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Stomp"],
            Track = Image(new Game.StompTrack(1f)),
            First = Image(new Game.StompClip(5f)),
            WindowStart = 0,
            WindowEnd = 20,
        });

        var potion = new ViewScope.TimelineSpec { Duration = 12, Hold = 1 };
        potion.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Arc"],
            Track = Image(new Game.ArcTrack(1f)),
            First = Image(new Game.ArcClip(1f)),
            WindowStart = 0,
            WindowEnd = 12,
            Lane = LaneOf(new Game.ArcTrack(1f), new Game.ArcClip(1f), 12, 12, static frame =>
            {
                float lift;
                Game.Jump.Fold(in frame, out lift);
                return lift;
            }),
        });

        var blast = new ViewScope.TimelineSpec { Duration = 8 };
        blast.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Move"],
            Track = Image(new Game.MoveTrack(1f)),
            First = Image(new Game.MoveClip(2f)),
            WindowStart = 0,
            WindowEnd = 8,
            Lane = LaneOf(new Game.MoveTrack(1f), new Game.MoveClip(2f), 8, 8, static frame =>
            {
                float lift;
                Game.MoveBatch.Fold(in frame, out lift);
                return lift;
            }),
        });

        scope.Build([run, jump, slide, barrel, potion, blast]);
    }
}
