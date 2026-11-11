using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class DerivedDifferentialTests
{
    const int Rows = 4;
    const int Ticks = 600;

    internal const string HostTemplate = """
        using System;
        using System.Collections.Generic;
        using Tl;

        public static class DiffHost
        {
            static uint Next(ref uint state)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }

            public static byte[] Play(byte[] library, int rows, int steps)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                /*DECL*/
                var all = new List<byte>();
                var rng = 0x9E3779B9u;
                for (var step = 0; step < steps; step++)
                {
                    var forward = !(step >= 200 && step < 240);
                    for (var e = 0; e < rows; e++)
                    {
                        var r = Next(ref rng);
                        var timeline = (ushort)(1 + (r >> 8) % TIMELINES);
                        switch (r & 15)
                        {
                            case 0:
                            case 1:
                            case 2:
                                players.Start(e, timeline, (ushort)((r >> 16) % 8));
                                break;
                            case 3:
                                players.Stop(e, timeline);
                                break;
                            case 4:
                                if (((r >> 8) & 7) == 0) players.StopAll(e);
                                break;
                            case 5:
                                players.Pause(e, timeline);
                                break;
                            case 6:
                                players.Resume(e, timeline);
                                break;
                            case 7:
                                players.Seek(e, timeline, (ushort)((r >> 16) % 30));
                                break;
                            case 8:
                                players.Reverse(e, timeline, ((r >> 20) & 1) == 0);
                                break;
                            case 9:
                                if (((r >> 8) & 15) == 0) players.Reset(e);
                                break;
                        }
                    }
                    world.Tick(players, forward);
                    new /*AUTO*/ { /*INIT*/ }.Run(world, players, forward);
                    /*SNAP*/
                }
                return all.ToArray();
            }
        }
        """;

    const string Values = """
        using Tl;
        namespace Diff;
        [TlState] public partial struct Teleported { public float Offset; public float Side; }
        [TlState] public partial struct Heading { public float Angle; public int Mode; }
        [TlState] public partial struct Armed { public bool On; public int Level; public float Power; }
        [TlClip("Diff/Teleport")] public struct TeleportClip { public float Distance; public bool ComeBack; public float Side; }
        [TlClip("Diff/Drift")] public struct DriftClip { public float Rate; }
        [TlClip("Diff/Aim")] public struct AimClip { public float Angle; public int Mode; }
        [TlClip("Diff/Arm")] public struct ArmClip { public int Level; public float Power; }
        """;

    const string DerivedTracks = """
        public struct TeleportTrack : ITlTrack<TeleportClip>
        {
            [Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)]
            public static Teleported JumpAtClipStart(in TlFrame<TeleportClip> frame)
                => new Teleported { Offset = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };

            [Pulse(typeof(Sum<Teleported>), On = ClipEvent.End)]
            public static Teleported ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
                => frame.Clip.ComeBack
                    ? new Teleported { Offset = -frame.Direction * frame.Clip.Distance, Side = -frame.Direction * frame.Clip.Side }
                    : default;
        }
        public struct DriftTrack : ITlTrack<DriftClip>
        {
            [Feeds(typeof(Sum<Teleported>))]
            public static Teleported Drift(in TlFrame<DriftClip> frame) => new Teleported { Offset = frame.Clip.Rate, Side = -0.5f * frame.Clip.Rate };
        }
        public struct AimTrack : ITlTrack<AimClip>
        {
            [Feeds(typeof(Set<Heading>))]
            public static Heading Hold(in TlFrame<AimClip> frame) => new Heading { Angle = frame.Clip.Angle, Mode = frame.Clip.Mode };

            [Pulse(typeof(Set<Heading>), On = ClipEvent.Start)]
            public static Heading Snap(in TlFrame<AimClip> frame) => new Heading { Angle = frame.Clip.Angle * 2f, Mode = -frame.Clip.Mode };
        }
        public struct ArmTrack : ITlTrack<ArmClip>
        {
            [Feeds(typeof(While<Armed>))]
            public static Armed Window(in TlFrame<ArmClip> frame) => new Armed { On = true, Level = frame.Clip.Level, Power = frame.Clip.Power };
        }
        """;

    const string HandKinds = """
        [TlKind(Accepts = KindEntries.Both)]
        public struct SumTeleported
        {
            public static void Combine(ref Teleported total, in Teleported next) { total.Offset += next.Offset; total.Side += next.Side; }
            public static void Apply(in Teleported total, ref Teleported column) { column.Offset += total.Offset; column.Side += total.Side; }
        }
        [TlKind(Accepts = KindEntries.Both)]
        public struct SetHeading
        {
            public static void Combine(ref Heading total, in Heading next) => total = next;
            public static void Apply(in Heading total, ref Heading column) => column = total;
        }
        [TlKind(Accepts = KindEntries.Spans)]
        public struct WhileArmed
        {
            public static void Combine(ref Armed total, in Armed next) { total.On |= next.On; total.Level |= next.Level; total.Power = System.Math.Max(total.Power, next.Power); }
            public static void Apply(in Armed total, ref Armed column) => column = total;
            public static void Idle(ref Armed column) => column = default;
        }
        """;

    static string Derived => Values + DerivedTracks;

    static string Hand => Values + HandKinds + DerivedTracks.Replace("Sum<Teleported>", "SumTeleported").Replace("Set<Heading>", "SetHeading").Replace("While<Armed>", "WhileArmed");

    static byte[] Floats(params object[] values) => TestDomain.Pack(values);

    static byte[] Build(System.Reflection.Assembly domain)
    {
        var teleport = domain.GetType("Diff.TeleportClip", true)!;
        var drift = domain.GetType("Diff.DriftClip", true)!;
        var aim = domain.GetType("Diff.AimClip", true)!;
        var arm = domain.GetType("Diff.ArmClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "a_hop", Duration = 50, Channel = "move", Tracks = { TestDomain.Track(teleport, (0, 30, Floats(3.1f, true, 1.3f))), TestDomain.Track(teleport, (12, 44, Floats(0.7f, false, 0.1f))) } },
            new() { Name = "b_hop_stay", Duration = 30, Tracks = { TestDomain.Track(teleport, (5, 25, Floats(2.7f, false, 0.1f))) } },
            new() { Name = "c_drift", Duration = 60, Loops = true, Tracks = { TestDomain.Track(drift, (0, 60, Floats(0.37f))) } },
            new() { Name = "d_aim", Duration = 24, Channel = "move", Tracks = { TestDomain.Track(aim, (3, 20, Floats(1.25f, 2))) } },
            new() { Name = "e_arm", Duration = 55, Tracks = { TestDomain.Track(arm, (4, 30, Floats(3, 0.5f))), TestDomain.Track(arm, (18, 45, Floats(5, 1.5f))) } },
            new() { Name = "f_mix", Duration = 45, Hold = true, Tracks = { TestDomain.Track(teleport, (0, 20, Floats(1.1f, true, 0.2f))), TestDomain.Track(drift, (10, 40, Floats(-0.21f))), TestDomain.Track(arm, (8, 26, Floats(2, 0.25f))), TestDomain.Track(aim, (6, 18, Floats(-0.75f, 7))) } },
        };
        return TestDomain.Library(timelines, domain);
    }

    const string ReuseHost = """
        using System.Collections.Generic;
        using Tl;

        public static class ReuseHost
        {
            public static byte[] Play(byte[] library, int rows, int mode)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                /*DECL*/
                var all = new List<byte>();
                players.Start(0, 1);
                for (var step = 0; step < 10; step++)
                {
                    world.Tick(players, true);
                    new /*AUTO*/ { /*INIT*/ }.Run(world, players, true);
                }
                if (mode == 0) players.StopAll(0);
                else players.Reset(0);
                /*CLEAR*/
                players.Start(0, 2);
                world.Tick(players, true);
                new /*AUTO*/ { /*INIT*/ }.Run(world, players, true);
                /*SNAP*/
                return all.ToArray();
            }
        }
        """;

    [Fact]
    public void ResetBeforeTheNextOccupantStartsKeepsTheOldOccupantsEndPulseOutOfTheNewColumn()
    {
        var domain = TestDomain.Build("DiffReuse", Derived, "DiffReuse", ReuseHost);
        var library = Build(domain.Domain);
        var stopped = domain.Host.GetType("ReuseHost", true)!.GetMethod("Play")!.Invoke(null, [library, Rows, 0]) as byte[];
        var reset = domain.Host.GetType("ReuseHost", true)!.GetMethod("Play")!.Invoke(null, [library, Rows, 1]) as byte[];
        Assert.Contains(stopped!, static x => x != 0);
        Assert.All(reset!, static x => Assert.Equal(0, x));
    }

    static string HostFor(int timelines) => HostTemplate.Replace("TIMELINES", timelines.ToString());

    [Fact]
    public void SynthesisedSumSetAndWhileKindsAreByteIdenticalToTheirHandWrittenTwinsOver600Ticks()
    {
        var derived = TestDomain.Build("DiffDerived", Derived, "DiffDerived", HostFor(6));
        var hand = TestDomain.Build("DiffHand", Hand, "DiffHand", HostFor(6));
        Assert.Empty(derived.Warnings);
        Assert.Empty(hand.Warnings);
        Assert.Contains("Tl.Derived.Sum_Diff_Teleported", derived.Generated);
        Assert.DoesNotContain("Tl.Derived", hand.Generated);
        var derivedLibrary = Build(derived.Domain);
        var handLibrary = Build(hand.Domain);
        Assert.NotEqual(derivedLibrary, handLibrary);
        Assert.Equal(["Tl.Derived.Set_Diff_Heading", "Tl.Derived.Sum_Diff_Teleported", "Tl.Derived.While_Diff_Armed"], LibraryBake.Kinds([derived.Domain]).Names.OrderBy(static n => n, StringComparer.Ordinal));
        var a = derived.Play("DiffHost", derivedLibrary, Rows, Ticks);
        var b = hand.Play("DiffHost", handLibrary, Rows, Ticks);
        Assert.Equal(b.Length, a.Length);
        var perTick = a.Length / Ticks;
        Assert.True(perTick > 0 && a.Length % Ticks == 0);
        var distinct = new HashSet<string>();
        for (var tick = 0; tick < Ticks; tick++)
        {
            var left = a.AsSpan(tick * perTick, perTick);
            var right = b.AsSpan(tick * perTick, perTick);
            Assert.True(left.SequenceEqual(right), $"derived and hand-written columns differ at tick {tick}");
            distinct.Add(Convert.ToHexString(left));
        }
        Assert.True(distinct.Count > 200, $"the script must move the columns; only {distinct.Count} distinct states");
        Assert.Contains(a, static x => x != 0);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(a)), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(b)));
    }

    const string DemoDerived = """
        using Tl;
        namespace HelloSide;
        [TlClip("Hello/Teleport")] public struct TeleportClip { public float Distance; public bool ComeBack; public float Side; }
        [TlState] public partial struct Teleported { public float Offset; public float Side; }
        public struct TeleportTrack : ITlTrack<TeleportClip>
        {
            [Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)]
            public static Teleported JumpAtClipStart(in TlFrame<TeleportClip> frame)
                => new Teleported { Offset = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };

            [Pulse(typeof(Sum<Teleported>), On = ClipEvent.End)]
            public static Teleported ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
                => frame.Clip.ComeBack
                    ? new Teleported { Offset = -frame.Direction * frame.Clip.Distance, Side = -frame.Direction * frame.Clip.Side }
                    : default;
        }
        """;

    static string DemoSource()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        return File.ReadAllText(Path.Combine(dir, "demos", "hello-side", "Teleport.cs"));
    }

    static byte[] DemoLibrary(System.Reflection.Assembly domain, bool exact)
    {
        var clip = domain.GetType("HelloSide.TeleportClip", true)!;
        float V(float exactValue, float other) => exact ? exactValue : other;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "a", Duration = 50, Channel = "move", Tracks = { TestDomain.Track(clip, (0, 30, Floats(V(3f, 3.1f), true, V(1.25f, 1.3f)))) } },
            new() { Name = "b", Duration = 40, Tracks = { TestDomain.Track(clip, (5, 25, Floats(V(2.5f, 2.7f), false, V(0.125f, 0.1f)))), TestDomain.Track(clip, (10, 40, Floats(V(0.5f, 0.9f), true, V(0.75f, 0.3f)))) } },
            new() { Name = "c", Duration = 30, Channel = "move", Tracks = { TestDomain.Track(clip, (0, 15, Floats(V(0.25f, 0.1f), true, V(0.5f, 0.7f)))) } },
        };
        return TestDomain.Library(timelines, domain);
    }

    static (byte[] Derived, byte[] Each) PlayDemo(bool exact)
    {
        var demo = TestDomain.Build("DemoEach", DemoSource(), "DemoEach", HostFor(3));
        var derived = TestDomain.Build("DemoSum", DemoDerived, "DemoSum", HostFor(3));
        Assert.Empty(demo.Warnings);
        Assert.Empty(derived.Warnings);
        return (derived.Play("DiffHost", DemoLibrary(derived.Domain, exact), Rows, Ticks), demo.Play("DiffHost", DemoLibrary(demo.Domain, exact), Rows, Ticks));
    }

    [Fact]
    public void SumMatchesTheShippedEachDemoKindByteForByteWhenTheArithmeticIsExact()
    {
        var (a, b) = PlayDemo(true);
        Assert.Equal(b.Length, a.Length);
        Assert.True(a.AsSpan().SequenceEqual(b), "derived Sum and the shipped Each demo kind differ with exactly representable values");
        Assert.Contains(a, static x => x != 0);
    }

    [Fact]
    public void SumAndTheShippedEachDemoKindDifferOnlyByFloatRoundingWhenPulsesCoincide()
    {
        var (a, b) = PlayDemo(false);
        var x = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(a.AsSpan());
        var y = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(b.AsSpan());
        var differing = 0;
        var worst = 0f;
        for (var i = 0; i < x.Length; i++)
        {
            if (BitConverter.SingleToInt32Bits(x[i]) == BitConverter.SingleToInt32Bits(y[i])) continue;
            differing++;
            worst = Math.Max(worst, Math.Abs(x[i] - y[i]));
        }
        Assert.True(differing > 0, "the script no longer makes two pulses coincide on a row; the rounding characterisation is vacuous");
        Assert.True(worst <= 1e-3f, $"derived Sum and Each differ by {worst}, more than float addition order explains");
    }
}
