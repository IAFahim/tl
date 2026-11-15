using Tl.Fuzz.Runtime;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class B4Rated256SlotFreezeTests
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    [Fact]
    public void EntityWith256SlotsAndRatedTimeline_KindStoreFreezesRatedSlotUnlikePlainAdvance()
    {
        const int n = 256;
        var names = new string[n];
        for (var i = 0; i < n; i++) names[i] = $"r256_{i + 1:D3}";
        var manifest = LibraryBake.Manifest(names, [], 60);
        var plainTls = new TypedTimeline[n];
        var kindTls = new TypedTimeline[n];
        for (var i = 0; i < n; i++)
        {
            var pt = new TypedTimeline { Name = names[i], Duration = 16, Loops = true };
            var ptr = new TypedTrack { Name = "pl", Type = typeof(FuzzPlainTrack), Data = LibraryGen.BytesOf(new FuzzPlainTrack { Gain = 1 }) };
            ptr.Clips.Add(new TypedClip { Type = typeof(FuzzPlainClip), Start = 0, End = 16, Data = LibraryGen.BytesOf(new FuzzPlainClip { Delta = 1, Tag = 1 }) });
            pt.Tracks.Add(ptr);
            plainTls[i] = pt;

            var kt = new TypedTimeline { Name = names[i], Duration = 16, Loops = true };
            var ktr = new TypedTrack { Name = "kc", Type = typeof(KCombineTrack), Data = LibraryGen.BytesOf(new KCombineTrack { Scale = 1 }) };
            ktr.Clips.Add(new TypedClip { Type = typeof(KCombineClip), Start = 0, End = 16, Data = LibraryGen.BytesOf(new KCombineClip { Delta = 1 }) });
            kt.Tracks.Add(ktr);
            kindTls[i] = kt;
        }
        var libP = LibraryGen.AssembleTyped(plainTls, [], manifest, null);
        var libK = LibraryGen.AssembleTyped(kindTls, [], manifest, AllKinds);
        using var wP = new TimelineWorld(libP.Bytes);
        using var wK = new TimelineWorld(libK.Bytes);
        using var pP = new PlaybackTable(1, n);
        using var pK = new PlaybackTable(1, n);
        using var ks = new KindStore(wK, pK);
        for (ushort tl = 1; tl <= n; tl++)
        {
            pP.Start(0, tl);
            pK.Start(0, tl);
        }
        pP.SetSpeed(0, 1, 255);
        pK.SetSpeed(0, 1, 255);
        wP.ApplyRequests(pP);
        wP.Advance(pP, true);
        wK.Tick(pK, true);
        Assert.Equal(pP.SlotsOf(0)[0].Tick, pK.SlotsOf(0)[0].Tick);
    }
}
