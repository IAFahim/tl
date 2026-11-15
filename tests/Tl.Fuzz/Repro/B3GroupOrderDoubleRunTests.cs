using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public struct B3ReproOutA { public int V; }
public struct B3ReproOutB { public int V; }
public struct B3ReproUOutA { public int V; }
public struct B3ReproUOutB { public int V; }

public struct B3ReproClipA { public int SpanVal; }
public struct B3ReproTrackA : IBlend<B3ReproClipA> { public void Blend(in B3ReproClipA a, in B3ReproClipA b, float t, out B3ReproClipA r) => r = a; }
public struct B3ReproClipB { public int SpanVal; }
public struct B3ReproTrackB : IBlend<B3ReproClipB> { public void Blend(in B3ReproClipB a, in B3ReproClipB b, float t, out B3ReproClipB r) => r = a; }

public struct B3ReproUClipA { public int SpanVal; }
public struct B3ReproUTrackA : IBlend<B3ReproUClipA> { public void Blend(in B3ReproUClipA a, in B3ReproUClipA b, float t, out B3ReproUClipA r) => r = a; }
public struct B3ReproUClipB { public int SpanVal; }
public struct B3ReproUTrackB : IBlend<B3ReproUClipB> { public void Blend(in B3ReproUClipB a, in B3ReproUClipB b, float t, out B3ReproUClipB r) => r = a; }

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "ReproB3Grp")]
public struct B3ReproGrpKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3ReproOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine, Group = "ReproB3Grp")]
public struct B3ReproGrpKindB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3ReproOutB col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct B3ReproUngrpKindA
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3ReproUOutA col) => col.V += total;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct B3ReproUngrpKindB
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref B3ReproUOutB col) => col.V += total;
}

[TimelineSystemMember(typeof(B3ReproGrpSysA))]
public readonly struct B3ReproGrpConA : ITrack<B3ReproTrackA, B3ReproClipA>
{
    [Feeds(typeof(B3ReproGrpKindA))]
    public static int Eval(in Frame<B3ReproTrackA, B3ReproClipA> f) => f.Direction * f.Clip.SpanVal;
}

[TimelineSystemMember(typeof(B3ReproGrpSysB))]
public readonly struct B3ReproGrpConB : ITrack<B3ReproTrackB, B3ReproClipB>
{
    [Feeds(typeof(B3ReproGrpKindB))]
    public static int Eval(in Frame<B3ReproTrackB, B3ReproClipB> f) => f.Direction * f.Clip.SpanVal;
}

[TimelineSystemMember(typeof(B3ReproUngrpSysA))]
public readonly struct B3ReproUngrpConA : ITrack<B3ReproUTrackA, B3ReproUClipA>
{
    [Feeds(typeof(B3ReproUngrpKindA))]
    public static int Eval(in Frame<B3ReproUTrackA, B3ReproUClipA> f) => f.Direction * f.Clip.SpanVal;
}

[TimelineSystemMember(typeof(B3ReproUngrpSysB))]
public readonly struct B3ReproUngrpConB : ITrack<B3ReproUTrackB, B3ReproUClipB>
{
    [Feeds(typeof(B3ReproUngrpKindB))]
    public static int Eval(in Frame<B3ReproUTrackB, B3ReproUClipB> f) => f.Direction * f.Clip.SpanVal;
}

[TimelineSystem] public ref partial struct B3ReproGrpSysA { public Span<B3ReproOutA> A; }
[TimelineSystem] public ref partial struct B3ReproGrpSysB { public Span<B3ReproOutB> B; }
[TimelineSystem] public ref partial struct B3ReproUngrpSysA { public Span<B3ReproUOutA> A; }
[TimelineSystem] public ref partial struct B3ReproUngrpSysB { public Span<B3ReproUOutB> B; }

public sealed class B3GroupOrderDoubleRunTests
{
    static byte[] BuildLib()
    {
        var names = new[] { "tl" };
        var channels = new[] { "ch" };
        var man = LibraryBake.Manifest(names, channels, 60);
        var tl = new TypedTimeline { Name = "tl", Duration = 10, Channel = "ch", Loops = true };

        static byte[] IntBytes(int v) => BitConverter.GetBytes(v);

        var trGA = new TypedTrack { Name = "ga", Type = typeof(B3ReproTrackA), Data = new byte[1] };
        trGA.Clips.Add(new TypedClip { Type = typeof(B3ReproClipA), Start = 0, End = 10, Data = IntBytes(5) });
        tl.Tracks.Add(trGA);

        var trGB = new TypedTrack { Name = "gb", Type = typeof(B3ReproTrackB), Data = new byte[1] };
        trGB.Clips.Add(new TypedClip { Type = typeof(B3ReproClipB), Start = 0, End = 10, Data = IntBytes(7) });
        tl.Tracks.Add(trGB);

        var trUA = new TypedTrack { Name = "ua", Type = typeof(B3ReproUTrackA), Data = new byte[1] };
        trUA.Clips.Add(new TypedClip { Type = typeof(B3ReproUClipA), Start = 0, End = 10, Data = IntBytes(5) });
        tl.Tracks.Add(trUA);

        var trUB = new TypedTrack { Name = "ub", Type = typeof(B3ReproUTrackB), Data = new byte[1] };
        trUB.Clips.Add(new TypedClip { Type = typeof(B3ReproUClipB), Start = 0, End = 10, Data = IntBytes(7) });
        tl.Tracks.Add(trUB);

        var kinds = LibraryBake.Kinds([typeof(B3GroupOrderDoubleRunTests).Assembly]);
        var tlb = LibraryBake.BakeTyped(tl, man);
        var part = new LibraryPart { Name = "tl", Tlb = tlb, Kinds = LibraryBake.KindPayload("tl", tlb, kinds), Next = 0 };
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        return LibraryAssembler.Assemble([part], 60, settings);
    }

    [Fact]
    public void DoubleRunOfRatedGroupMember_DependsOnMemberExecutionOrderAndDivergesFromUngrouped()
    {
        var lib = BuildLib();

        using var wBFirst = new TimelineWorld(lib);
        using var pBFirst = new PlaybackTable(1, 4);
        using var kBFirst = new KindStore(wBFirst, pBFirst);

        using var wAFirst = new TimelineWorld(lib);
        using var pAFirst = new PlaybackTable(1, 4);
        using var kAFirst = new KindStore(wAFirst, pAFirst);

        using var wUngrp = new TimelineWorld(lib);
        using var pUngrp = new PlaybackTable(1, 4);
        using var kUngrp = new KindStore(wUngrp, pUngrp);

        pBFirst.Start(0, 1);
        pBFirst.SetSpeed(0, 1, 12);
        pAFirst.Start(0, 1);
        pAFirst.SetSpeed(0, 1, 12);
        pUngrp.Start(0, 1);
        pUngrp.SetSpeed(0, 1, 12);

        wBFirst.Tick(pBFirst, true);
        wAFirst.Tick(pAFirst, true);
        wUngrp.Tick(pUngrp, true);

        wBFirst.Tick(pBFirst, true);
        wAFirst.Tick(pAFirst, true);
        wUngrp.Tick(pUngrp, true);

        var outA1 = new B3ReproOutA[1];
        var outB1 = new B3ReproOutB[1];
        new B3ReproGrpSysB { B = outB1 }.Run(wBFirst, pBFirst, true);
        new B3ReproGrpSysB { B = outB1 }.Run(wBFirst, pBFirst, true);
        new B3ReproGrpSysA { A = outA1 }.Run(wBFirst, pBFirst, true);

        var outA2 = new B3ReproOutA[1];
        var outB2 = new B3ReproOutB[1];
        new B3ReproGrpSysA { A = outA2 }.Run(wAFirst, pAFirst, true);
        new B3ReproGrpSysB { B = outB2 }.Run(wAFirst, pAFirst, true);
        new B3ReproGrpSysB { B = outB2 }.Run(wAFirst, pAFirst, true);

        var uOutA = new B3ReproUOutA[1];
        var uOutB = new B3ReproUOutB[1];
        new B3ReproUngrpSysB { B = uOutB }.Run(wUngrp, pUngrp, true);
        new B3ReproUngrpSysB { B = uOutB }.Run(wUngrp, pUngrp, true);
        new B3ReproUngrpSysA { A = uOutA }.Run(wUngrp, pUngrp, true);

        Assert.Equal(outA1[0].V, outA2[0].V);
        Assert.Equal(outB1[0].V, outB2[0].V);
        Assert.Equal(uOutB[0].V, outB1[0].V);
    }
}
