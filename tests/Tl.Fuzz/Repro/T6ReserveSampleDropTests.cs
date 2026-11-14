using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public struct ReserveDropCol { public int Value; }

public struct ReserveDropClip { public int Delta; }

public struct ReserveDropTrack : IBlend<ReserveDropClip>
{
    public void Blend(in ReserveDropClip first, in ReserveDropClip second, float factor, out ReserveDropClip result) => result = first;
}

[TimelineKind(Accepts = KindEntries.Spans, Mode = KindMode.Combine)]
public struct ReserveDropKind
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref ReserveDropCol col) => col.Value += total;
}

[TimelineSystemMember(typeof(ReserveDropSystem))]
public readonly struct ReserveDropConsumer : ITrack<ReserveDropTrack, ReserveDropClip>
{
    [Feeds(typeof(ReserveDropKind))]
    public static int Eval(in Frame<ReserveDropTrack, ReserveDropClip> frame) => frame.Clip.Delta;
}

[TimelineSystem]
public ref partial struct ReserveDropSystem
{
    public Span<ReserveDropCol> Col;
}

public sealed class T6ReserveSampleDropTests
{
    static byte[] BuildLibrary()
    {
        var names = new[] { "tl_one", "tl_two" };
        var channels = new[] { "ch_1", "ch_2" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var kinds = LibraryBake.Kinds([typeof(ReserveDropSystem).Assembly]);

        var parts = new LibraryPart[2];
        for (int i = 0; i < 2; i++)
        {
            var tl = new TypedTimeline { Name = names[i], Duration = 20 };
            var tr = new TypedTrack
            {
                Name = "tr",
                Type = typeof(ReserveDropTrack),
                Data = new byte[Marshal.SizeOf<ReserveDropTrack>()],
            };
            tr.Clips.Add(new TypedClip
            {
                Type = typeof(ReserveDropClip),
                Start = 2,
                End = 20,
                Data = BitConverter.GetBytes(i == 0 ? 100 : 1),
            });
            tl.Tracks.Add(tr);
            var tlb = LibraryBake.BakeTyped(tl, manifest);
            parts[i] = new LibraryPart
            {
                Name = names[i],
                Tlb = tlb,
                Kinds = LibraryBake.KindPayload(names[i], tlb, kinds),
            };
        }

        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    [Fact]
    public void ReserveSpillDuringSuperSampling_DropsPriorSlotSampleScratch()
    {
        var bytes = BuildLibrary();

        using var ws = new TimelineWorld(bytes);
        using var wl = new TimelineWorld(bytes);
        using var ps = new PlaybackTable(1, 4);
        using var pl = new PlaybackTable(1, 32);
        using var kss = new KindStore(ws, ps);
        using var ksl = new KindStore(wl, pl);

        ps.Start(0, 1);
        ps.Start(0, 2);
        pl.Start(0, 1);
        pl.Start(0, 2);

        ws.Tick(ps);
        wl.Tick(pl);
        ws.Tick(ps);
        wl.Tick(pl);

        ps.SetSpeed(0, 1, 32);
        ps.SetSpeed(0, 2, 32);
        pl.SetSpeed(0, 1, 32);
        pl.SetSpeed(0, 2, 32);

        ws.Tick(ps);
        wl.Tick(pl);

        var colS = new ReserveDropCol[1];
        var colL = new ReserveDropCol[1];
        new ReserveDropSystem { Col = colS }.Run(ws, ps);
        new ReserveDropSystem { Col = colL }.Run(wl, pl);

        Assert.Equal(colL[0].Value, colS[0].Value);
    }
}
