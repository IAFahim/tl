using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public struct ReproGrpColA { public int Value; }
public struct ReproGrpColB { public int Value; }

public struct ReproGrpClipA { public int Delta; }
public struct ReproGrpTrackA : IBlend<ReproGrpClipA>
{
    public void Blend(in ReproGrpClipA first, in ReproGrpClipA second, float factor, out ReproGrpClipA result) => result = first;
}

public struct ReproGrpClipB { public int Delta; }
public struct ReproGrpTrackB : IBlend<ReproGrpClipB>
{
    public void Blend(in ReproGrpClipB first, in ReproGrpClipB second, float factor, out ReproGrpClipB result) => result = first;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "ReproSharedGrp")]
public struct ReproGroupAlpha
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref ReproGrpColA col) => col.Value += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "ReproSharedGrp")]
public struct ReproGroupBeta
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref ReproGrpColB col) => col.Value += total;
}

[TimelineSystemMember(typeof(ReproGroupSystem))]
public readonly struct ReproGrpConsumerA : ITrack<ReproGrpTrackA, ReproGrpClipA>
{
    [Feeds(typeof(ReproGroupAlpha))]
    public static int Eval(in Frame<ReproGrpTrackA, ReproGrpClipA> frame) => frame.Clip.Delta;
}

[TimelineSystemMember(typeof(ReproGroupSystem))]
public readonly struct ReproGrpConsumerB : ITrack<ReproGrpTrackB, ReproGrpClipB>
{
    [Feeds(typeof(ReproGroupBeta))]
    public static int Eval(in Frame<ReproGrpTrackB, ReproGrpClipB> frame) => frame.Clip.Delta;
}

[TimelineSystem]
public ref partial struct ReproGroupSystem
{
    public Span<ReproGrpColA> ColA;
    public Span<ReproGrpColB> ColB;
}

public sealed class T7GroupPulseWipeTests
{
    static byte[] BuildLibrary()
    {
        var names = new[] { "grp_tl" };
        var channels = new[] { "ch_1" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var kinds = LibraryBake.Kinds([typeof(ReproGroupSystem).Assembly]);

        var tl = new TypedTimeline { Name = "grp_tl", Duration = 10 };
        var trA = new TypedTrack
        {
            Name = "a",
            Type = typeof(ReproGrpTrackA),
            Data = new byte[Marshal.SizeOf<ReproGrpTrackA>()],
        };
        trA.Clips.Add(new TypedClip { Type = typeof(ReproGrpClipA), Start = 0, End = 10, Data = BitConverter.GetBytes(1) });
        tl.Tracks.Add(trA);

        var trB = new TypedTrack
        {
            Name = "b",
            Type = typeof(ReproGrpTrackB),
            Data = new byte[Marshal.SizeOf<ReproGrpTrackB>()],
        };
        trB.Clips.Add(new TypedClip { Type = typeof(ReproGrpClipB), Start = 0, End = 10, Data = BitConverter.GetBytes(2) });
        tl.Tracks.Add(trB);

        var tlb = LibraryBake.BakeTyped(tl, manifest);
        return LibraryAssembler.Assemble(
            [new LibraryPart { Name = "grp_tl", Tlb = tlb, Kinds = LibraryBake.KindPayload("grp_tl", tlb, kinds) }],
            60,
            LibrarySettings.Default);
    }

    [Fact]
    public void SharedGroup_EarlierKindSeeingPulse_ErasesLaterKindsPulsesBeforeItRuns()
    {
        var bytes = BuildLibrary();
        using var world = new TimelineWorld(bytes);
        using var players = new PlaybackTable(1, 4);
        using var store = new KindStore(world, players);

        var pulseA = store.Pulses<int>(typeof(ReproGroupAlpha).FullName!);
        var pulseB = store.Pulses<int>(typeof(ReproGroupBeta).FullName!);
        pulseA.To(0, 10);
        pulseB.To(0, 20);

        world.Tick(players, true);

        var colA = new ReproGrpColA[1];
        var colB = new ReproGrpColB[1];
        new ReproGroupSystem { ColA = colA, ColB = colB }.Run(world, players, true);

        Assert.Equal(10, colA[0].Value);
        Assert.Equal(20, colB[0].Value);
    }
}
