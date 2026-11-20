using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class CrossEntityControlTests
{
    const string Name = "Cross";
    const int Rows = 3;
    const int Steps = 12;
    const ushort CastTimeline = 1;
    const ushort MendTimeline = 2;
    const int MendAmount = 25;
    const int GamePulse = 7;
    const int GamePulseStep = 9;

    const string DomainSource = """
        using Tl;

        namespace Cross
        {
            [TlState]
            public partial struct Health
            {
                public int Value;
            }

            public struct Cast
            {
                public int Spell;
            }

            [TlClip("Cross/Cast")]
            public struct CastClip
            {
                public int Spell;
            }

            public struct CastTrack : ITlTrack<CastClip>
            {
                public static void OnClipStart(in TlFrame<CastClip> frame, ref Events<Cast> casts)
                {
                    if (frame.Direction > 0)
                        casts.Raise(new Cast { Spell = frame.Clip.Spell });
                }
            }

            [TlKind(Accepts = KindEntries.Both)]
            public struct Mend
            {
                public static void Combine(ref int total, in int next) => total += next;
                public static void Apply(in int total, ref Health health) => health.Value += total;
            }

            [TlClip("Cross/Mend")]
            public struct MendClip
            {
                public int Amount;
            }

            public struct MendTrack : ITlTrack<MendClip>
            {
                [Pulse(typeof(Mend), On = ClipEvent.Start)]
                public static int Heal(in TlFrame<MendClip> frame) => frame.Direction > 0 ? frame.Clip.Amount : 0;
            }
        }
        """;

    const string HostSource = """
        using Tl;

        public static class CrossHost
        {
            public static int[] Play(byte[] library, int rows, int steps, int[] aims, bool generatedPulses, int pulseStep, int pulse)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                using var casts = new Events<Cross.Cast>(8);
                var mend = generatedPulses ? Cross.MendKind.Pulses(kinds) : kinds.Pulses<int>("Cross.Mend");
                var health = new Cross.Health[rows];
                var trace = new int[steps * (rows + 1)];
                players.Start(0, 1);
                for (var step = 0; step < steps; step++)
                {
                    if (step == pulseStep) mend.To(0, pulse);
                    world.Tick(players);
                    new Tl.Generated.TlAuto_Cross { C0_Health = health, E0_Cast = casts.Writer }.Run(world, players);
                    var drained = casts.Drain();
                    var raised = 0;
                    for (var caster = 0; caster < rows; caster++)
                        foreach (var cast in drained.Of(caster))
                        {
                            players.Start(aims[caster], (ushort)cast.Value.Spell);
                            raised++;
                        }
                    for (var e = 0; e < rows; e++) trace[step * (rows + 1) + e] = health[e].Value;
                    trace[step * (rows + 1) + rows] = raised;
                }
                return trace;
            }

            public static long[] Dropped(byte[] library)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(1);
                using var kinds = new KindStore(world, players, new[] { "Cross.Mend" }, new[] { 0ul });
                players.Start(0, 1);
                world.Tick(players);
                players.Start(0, 2);
                world.Tick(players);
                return new[] { players.IsPlaying(0, 1) ? 1L : 0L, players.IsPlaying(0, 2) ? 1L : 0L, players.DroppedStarts };
            }
        }
        """;

    sealed class Built
    {
        public Assembly Domain = null!;
        public Assembly Host = null!;
        public string Generated = "";
    }

    static Built Build()
    {
        var references = EmissionTests.References().ToList();
        var image = TestDomain.Compile(Name, DomainSource, references, Name, out var generated, out var warnings);
        Assert.Empty(warnings);
        var host = TestDomain.Compile("CrossHost", HostSource, references.Append(MetadataReference.CreateFromImage(image)).ToList(), "", out _, out _);
        var context = new AssemblyLoadContext(Name, false);
        return new Built
        {
            Domain = context.LoadFromStream(new MemoryStream(image)),
            Host = context.LoadFromStream(new MemoryStream(host)),
            Generated = generated,
        };
    }

    static readonly Lazy<Built> Assemblies = new(Build);

    static byte[] Library(Assembly domain)
    {
        var cast = domain.GetType("Cross.CastClip", true)!;
        var mend = domain.GetType("Cross.MendClip", true)!;
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "a_cast", Duration = 10, Channel = "move", Tracks = { TestDomain.Track(cast, (2, 4, TestDomain.Pack((int)MendTimeline))) } },
            new() { Name = "b_mend", Duration = 10, Channel = "move", Tracks = { TestDomain.Track(mend, (0, 3, TestDomain.Pack(MendAmount))) } },
        };
        return TestDomain.Library(timelines, domain);
    }

    static int[] Play(bool generatedPulses)
    {
        var built = Assemblies.Value;
        var aims = new[] { 1, 2, 0 };
        return (int[])built.Host.GetType("CrossHost", true)!.GetMethod("Play")!.Invoke(null, [Library(built.Domain), Rows, Steps, aims, generatedPulses, GamePulseStep, GamePulse])!;
    }

    static int Health(int[] trace, int step, int row) => trace[step * (Rows + 1) + row];

    static int Raised(int[] trace, int step) => trace[step * (Rows + 1) + Rows];

    [Fact]
    public void ACastEventOnOneEntityStartsATimelineOnAnotherWhoseKindAppliesOnTheNextStep()
    {
        var trace = Play(true);
        var castStep = -1;
        for (var step = 0; step < Steps && castStep < 0; step++)
            if (Raised(trace, step) > 0) castStep = step;
        Assert.True(castStep >= 0, "the caster never raised its cast event");
        Assert.Equal(1, Raised(trace, castStep));
        for (var step = 0; step < Steps; step++)
        {
            var mended = step > castStep ? MendAmount : 0;
            Assert.Equal(mended, Health(trace, step, 1));
            Assert.Equal(0, Health(trace, step, 2));
            Assert.Equal(step >= GamePulseStep ? GamePulse : 0, Health(trace, step, 0));
        }
        Assert.True(castStep + 1 < GamePulseStep);
    }

    [Fact]
    public void TheGeneratedPulseHelperSendsExactlyWhatTheStringKeyedSenderSends()
    {
        Assert.Equal(Play(false), Play(true));
        var generated = Assemblies.Value.Generated;
        Assert.Contains("public static class MendKind", generated);
        Assert.Contains("Pulses(global::Tl.KindStore kinds) => kinds.Pulses<int>(\"Cross.Mend\");", generated);
        Assert.DoesNotContain("CastKind", generated);
    }

    [Fact]
    public void AStartTheEntityCannotHoldLeavesThePlaybackOnItsChannelPlaying()
    {
        var built = Assemblies.Value;
        var dropped = built.Host.GetType("CrossHost", true)!.GetMethod("Dropped")!;
#if TL_CHECKED
        var thrown = Assert.Throws<TargetInvocationException>(() => dropped.Invoke(null, [Library(built.Domain)]));
        Assert.IsType<ArgumentException>(thrown.InnerException);
#else
        Assert.Equal(new long[] { 1, 0, 1 }, (long[])dropped.Invoke(null, [Library(built.Domain)])!);
#endif
    }
}
