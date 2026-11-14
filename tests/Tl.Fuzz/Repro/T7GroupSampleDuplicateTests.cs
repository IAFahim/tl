using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public struct SampleGrpColA { public int Value; }
public struct SampleGrpColB { public int Value; }
public struct SampleSoloColA { public int Value; }

public struct SampleGrpClipA { public int Delta; }
public struct SampleGrpTrackA : IBlend<SampleGrpClipA>
{
    public void Blend(in SampleGrpClipA first, in SampleGrpClipA second, float factor, out SampleGrpClipA result) => result = first;
}

public struct SampleGrpClipB { public int Delta; }
public struct SampleGrpTrackB : IBlend<SampleGrpClipB>
{
    public void Blend(in SampleGrpClipB first, in SampleGrpClipB second, float factor, out SampleGrpClipB result) => result = first;
}

public struct SampleSoloClipA { public int Delta; }
public struct SampleSoloTrackA : IBlend<SampleSoloClipA>
{
    public void Blend(in SampleSoloClipA first, in SampleSoloClipA second, float factor, out SampleSoloClipA result) => result = first;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine, Group = "ReproSampleGrp")]
public struct ReproSampleGrpA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref SampleGrpColA col) => col.Value += total;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine, Group = "ReproSampleGrp")]
public struct ReproSampleGrpB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref SampleGrpColB col) => col.Value += total;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine)]
public struct ReproSampleSoloA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref SampleSoloColA col) => col.Value += total;
}

[TimelineSystemMember(typeof(ReproSampleGrpSystem))]
public readonly struct ReproSampleGrpConsumerA : ITrack<SampleGrpTrackA, SampleGrpClipA>
{
    [Feeds(typeof(ReproSampleGrpA))]
    public static int Eval(in Frame<SampleGrpTrackA, SampleGrpClipA> frame) => frame.Clip.Delta;
}

[TimelineSystemMember(typeof(ReproSampleGrpSystem))]
public readonly struct ReproSampleGrpConsumerB : ITrack<SampleGrpTrackB, SampleGrpClipB>
{
    [Feeds(typeof(ReproSampleGrpB))]
    public static int Eval(in Frame<SampleGrpTrackB, SampleGrpClipB> frame) => frame.Clip.Delta;
}

[TimelineSystemMember(typeof(ReproSampleSoloSystem))]
public readonly struct ReproSampleSoloConsumerA : ITrack<SampleSoloTrackA, SampleSoloClipA>
{
    [Feeds(typeof(ReproSampleSoloA))]
    public static int Eval(in Frame<SampleSoloTrackA, SampleSoloClipA> frame) => frame.Clip.Delta;
}

[TimelineSystem]
public ref partial struct ReproSampleGrpSystem
{
    public Span<SampleGrpColA> ColA;
    public Span<SampleGrpColB> ColB;
}

[TimelineSystem]
public ref partial struct ReproSampleSoloSystem
{
    public Span<SampleSoloColA> ColA;
}

public sealed class T7GroupSampleDuplicateTests
{
    static byte[] BuildLibrary(bool grouped)
    {
        var names = new[] { "sample_tl" };
        var channels = new[] { "ch_1" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var kinds = LibraryBake.Kinds([typeof(ReproSampleGrpSystem).Assembly]);

        var tl = new TypedTimeline { Name = "sample_tl", Duration = 20 };
        if (grouped)
        {
            var trA = new TypedTrack
            {
                Name = "a",
                Type = typeof(SampleGrpTrackA),
                Data = new byte[Marshal.SizeOf<SampleGrpTrackA>()],
            };
            trA.Clips.Add(new TypedClip { Type = typeof(SampleGrpClipA), Start = 0, End = 20, Data = BitConverter.GetBytes(5) });
            tl.Tracks.Add(trA);

            var trB = new TypedTrack
            {
                Name = "b",
                Type = typeof(SampleGrpTrackB),
                Data = new byte[Marshal.SizeOf<SampleGrpTrackB>()],
            };
            trB.Clips.Add(new TypedClip { Type = typeof(SampleGrpClipB), Start = 0, End = 20, Data = BitConverter.GetBytes(1) });
            tl.Tracks.Add(trB);
        }
        else
        {
            var trSolo = new TypedTrack
            {
                Name = "a",
                Type = typeof(SampleSoloTrackA),
                Data = new byte[Marshal.SizeOf<SampleSoloTrackA>()],
            };
            trSolo.Clips.Add(new TypedClip { Type = typeof(SampleSoloClipA), Start = 0, End = 20, Data = BitConverter.GetBytes(5) });
            tl.Tracks.Add(trSolo);
        }

        var tlb = LibraryBake.BakeTyped(tl, manifest);
        return LibraryAssembler.Assemble(
            [new LibraryPart { Name = "sample_tl", Tlb = tlb, Kinds = LibraryBake.KindPayload("sample_tl", tlb, kinds) }],
            60,
            LibrarySettings.Default);
    }

    [Fact]
    public void SharedGroup_SuperSamplingDuplicatesEntriesAndProducesWrongValuesForGroupKinds()
    {
        var gBytes = BuildLibrary(grouped: true);
        var uBytes = BuildLibrary(grouped: false);

        using var wg = new TimelineWorld(gBytes);
        using var wu = new TimelineWorld(uBytes);
        using var pg = new PlaybackTable(1, 4);
        using var pu = new PlaybackTable(1, 4);
        using var ksg = new KindStore(wg, pg);
        using var ksu = new KindStore(wu, pu);

        const ushort tlG = 1;
        const ushort tlU = 1;

        pg.Start(0, tlG);
        pu.Start(0, tlU);
        wg.Tick(pg);
        wu.Tick(pu);

        var gA = new SampleGrpColA[1];
        var gB = new SampleGrpColB[1];
        var uA = new SampleSoloColA[1];
        new ReproSampleGrpSystem { ColA = gA, ColB = gB }.Run(wg, pg);
        new ReproSampleSoloSystem { ColA = uA }.Run(wu, pu);
        Assert.Equal(uA[0].Value, gA[0].Value);

        pg.SetSpeed(0, tlG, 32);
        pu.SetSpeed(0, tlU, 32);
        wg.Tick(pg);
        wu.Tick(pu);

        gA[0] = default;
        gB[0] = default;
        uA[0] = default;
        new ReproSampleGrpSystem { ColA = gA, ColB = gB }.Run(wg, pg);
        new ReproSampleSoloSystem { ColA = uA }.Run(wu, pu);

        Assert.Equal(uA[0].Value, gA[0].Value);
    }
}
