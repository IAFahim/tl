using System.Runtime.CompilerServices;
using Tl;
using Tl.TestSupport;
using System.Diagnostics.CodeAnalysis;

internal readonly record struct BankClip(float Amount);

internal readonly record struct BankTrack(float Scale) : IBlend<BankClip>
{
    public void Blend(in BankClip first, in BankClip second, float factor, out BankClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly struct BankJob : ITrack<BankTrack, BankClip>
{
    public static void OnActive(in Frame<BankTrack, BankClip> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly record struct BankDamageClip(float Amount);

internal readonly record struct BankDamageTrack(float Multiplier) : IBlend<BankDamageClip>
{
    public void Blend(in BankDamageClip first, in BankDamageClip second, float factor, out BankDamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly struct BankDamageJob : ITrack<BankDamageTrack, BankDamageClip>
{
    public static void OnActive(in Frame<BankDamageTrack, BankDamageClip> frame, ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly record struct W0;
internal readonly record struct W1;
internal readonly record struct W2;
internal readonly record struct W3;
internal readonly record struct W4;
internal readonly record struct W5;
internal readonly record struct W6;
internal readonly record struct W7;
internal readonly record struct W8;
internal readonly record struct W9;
internal readonly record struct W10;
internal readonly record struct W11;
internal readonly record struct W12;
internal readonly record struct W13;
internal readonly record struct W14;
internal readonly record struct W15;
internal readonly record struct W16;
internal readonly record struct W17;
internal readonly record struct W18;
internal readonly record struct W19;
internal readonly record struct W20;
internal readonly record struct W21;
internal readonly record struct W22;
internal readonly record struct W23;
internal readonly record struct W24;
internal readonly record struct W25;
internal readonly record struct W26;
internal readonly record struct W27;
internal readonly record struct W28;
internal readonly record struct W29;
internal readonly record struct W30;
internal readonly record struct W31;
internal readonly record struct W32;
internal readonly record struct W33;
internal readonly record struct W34;
internal readonly record struct W35;
internal readonly record struct W36;
internal readonly record struct W37;
internal readonly record struct W38;
internal readonly record struct W39;
internal readonly record struct W40;
internal readonly record struct W41;
internal readonly record struct W42;
internal readonly record struct W43;
internal readonly record struct W44;
internal readonly record struct W45;
internal readonly record struct W46;
internal readonly record struct W47;
internal readonly record struct W48;
internal readonly record struct W49;
internal readonly record struct W50;
internal readonly record struct W51;
internal readonly record struct W52;
internal readonly record struct W53;
internal readonly record struct W54;
internal readonly record struct W55;
internal readonly record struct W56;
internal readonly record struct W57;
internal readonly record struct W58;
internal readonly record struct W59;
internal readonly record struct W60;
internal readonly record struct W61;
internal readonly record struct W62;
internal readonly record struct W63;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "TK distinguishes pair-typed instantiations")]
internal readonly record struct BankClip<TK>(float Amount) where TK : unmanaged;

internal readonly record struct BankTrack<TK>(float Scale) : IBlend<BankClip<TK>> where TK : unmanaged
{
    public void Blend(in BankClip<TK> first, in BankClip<TK> second, float factor, out BankClip<TK> result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly struct JobW0 : ITrack<BankTrack<W0>, BankClip<W0>>
{
    public static void OnActive(in Frame<BankTrack<W0>, BankClip<W0>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW1 : ITrack<BankTrack<W1>, BankClip<W1>>
{
    public static void OnActive(in Frame<BankTrack<W1>, BankClip<W1>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW2 : ITrack<BankTrack<W2>, BankClip<W2>>
{
    public static void OnActive(in Frame<BankTrack<W2>, BankClip<W2>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW3 : ITrack<BankTrack<W3>, BankClip<W3>>
{
    public static void OnActive(in Frame<BankTrack<W3>, BankClip<W3>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW4 : ITrack<BankTrack<W4>, BankClip<W4>>
{
    public static void OnActive(in Frame<BankTrack<W4>, BankClip<W4>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW5 : ITrack<BankTrack<W5>, BankClip<W5>>
{
    public static void OnActive(in Frame<BankTrack<W5>, BankClip<W5>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW6 : ITrack<BankTrack<W6>, BankClip<W6>>
{
    public static void OnActive(in Frame<BankTrack<W6>, BankClip<W6>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW7 : ITrack<BankTrack<W7>, BankClip<W7>>
{
    public static void OnActive(in Frame<BankTrack<W7>, BankClip<W7>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW8 : ITrack<BankTrack<W8>, BankClip<W8>>
{
    public static void OnActive(in Frame<BankTrack<W8>, BankClip<W8>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW9 : ITrack<BankTrack<W9>, BankClip<W9>>
{
    public static void OnActive(in Frame<BankTrack<W9>, BankClip<W9>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW10 : ITrack<BankTrack<W10>, BankClip<W10>>
{
    public static void OnActive(in Frame<BankTrack<W10>, BankClip<W10>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW11 : ITrack<BankTrack<W11>, BankClip<W11>>
{
    public static void OnActive(in Frame<BankTrack<W11>, BankClip<W11>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW12 : ITrack<BankTrack<W12>, BankClip<W12>>
{
    public static void OnActive(in Frame<BankTrack<W12>, BankClip<W12>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW13 : ITrack<BankTrack<W13>, BankClip<W13>>
{
    public static void OnActive(in Frame<BankTrack<W13>, BankClip<W13>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW14 : ITrack<BankTrack<W14>, BankClip<W14>>
{
    public static void OnActive(in Frame<BankTrack<W14>, BankClip<W14>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW15 : ITrack<BankTrack<W15>, BankClip<W15>>
{
    public static void OnActive(in Frame<BankTrack<W15>, BankClip<W15>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW16 : ITrack<BankTrack<W16>, BankClip<W16>>
{
    public static void OnActive(in Frame<BankTrack<W16>, BankClip<W16>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW17 : ITrack<BankTrack<W17>, BankClip<W17>>
{
    public static void OnActive(in Frame<BankTrack<W17>, BankClip<W17>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW18 : ITrack<BankTrack<W18>, BankClip<W18>>
{
    public static void OnActive(in Frame<BankTrack<W18>, BankClip<W18>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW19 : ITrack<BankTrack<W19>, BankClip<W19>>
{
    public static void OnActive(in Frame<BankTrack<W19>, BankClip<W19>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW20 : ITrack<BankTrack<W20>, BankClip<W20>>
{
    public static void OnActive(in Frame<BankTrack<W20>, BankClip<W20>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW21 : ITrack<BankTrack<W21>, BankClip<W21>>
{
    public static void OnActive(in Frame<BankTrack<W21>, BankClip<W21>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW22 : ITrack<BankTrack<W22>, BankClip<W22>>
{
    public static void OnActive(in Frame<BankTrack<W22>, BankClip<W22>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW23 : ITrack<BankTrack<W23>, BankClip<W23>>
{
    public static void OnActive(in Frame<BankTrack<W23>, BankClip<W23>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW24 : ITrack<BankTrack<W24>, BankClip<W24>>
{
    public static void OnActive(in Frame<BankTrack<W24>, BankClip<W24>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW25 : ITrack<BankTrack<W25>, BankClip<W25>>
{
    public static void OnActive(in Frame<BankTrack<W25>, BankClip<W25>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW26 : ITrack<BankTrack<W26>, BankClip<W26>>
{
    public static void OnActive(in Frame<BankTrack<W26>, BankClip<W26>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW27 : ITrack<BankTrack<W27>, BankClip<W27>>
{
    public static void OnActive(in Frame<BankTrack<W27>, BankClip<W27>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW28 : ITrack<BankTrack<W28>, BankClip<W28>>
{
    public static void OnActive(in Frame<BankTrack<W28>, BankClip<W28>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW29 : ITrack<BankTrack<W29>, BankClip<W29>>
{
    public static void OnActive(in Frame<BankTrack<W29>, BankClip<W29>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW30 : ITrack<BankTrack<W30>, BankClip<W30>>
{
    public static void OnActive(in Frame<BankTrack<W30>, BankClip<W30>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal readonly struct JobW31 : ITrack<BankTrack<W31>, BankClip<W31>>
{
    public static void OnActive(in Frame<BankTrack<W31>, BankClip<W31>> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}

internal static class BankReceipts
{
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "closures run before the dispose later in the same method")]
    internal static void Capacity()
    {
        Require(Unsafe.SizeOf<SlotView>() == 64, "SlotView header is 64 bytes");
        const int domain = 65536;
        const int distinct = 49152;
        using var timelines = new TimelineSet<BankTrack, BankClip>();
        for (var i = 0; i < distinct; i++)
        {
            using var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 1));
            Require(timelines.Add(asset) == i, $"sequential id {i}");
        }
        using var duplicate = TimelineAsset.LoadAsset(BakeBank(1f, 1));
        for (var i = distinct; i < domain - 1; i++)
            Require(timelines.Add(duplicate) == i, $"shared id {i}");
        using var measuredDuplicate = MeasuredLanes.Measure(duplicate);
        Require(timelines.AddAt(65535, measuredDuplicate) == 65535, "AddAt binds the 65535 id the old cap refused");
        unsafe
        {
            var shared = timelines.View(65535);
            var origin = timelines.View(0);
            Require(shared.Forward == origin.Forward && shared.Duration == 1 && shared.TableTicks == 2, "domain edge shares the content-identical block");
            Require(shared.RecordBytes == 8 && shared.AbiVersion == SlotView.AbiVersionV1 && shared.Generation > 0, "domain edge header");
        }
        RequireThrows<InvalidOperationException>(() => timelines.Add(duplicate), "the 65,537th add is full");
        Require(timelines.BlockCount == distinct && timelines.SharedHits == domain - distinct, "the full-domain bank dedupes every content-identical id");
        Console.WriteLine($"bank-capacity: {domain} ids on {distinct} distinct one-tick blocks ({domain - distinct} shared), block bytes {timelines.HeaderBytes + timelines.TableBytes}, headers {timelines.HeaderBytes}, tables {timelines.TableBytes}, directories+retired {timelines.DirectoryBytes}, retained {timelines.RetainedBytes}");

        var ids = new ushort[domain];
        for (var i = 0; i < domain; i++) ids[i] = (ushort)i;
        Shuffle(ids, 1234);
        var positions = new ushort[domain];
        var effects = new float[domain];
        for (var i = 0; i < domain; i++) positions[i] = (ushort)(i & 1);
        for (var frame = 0; frame < 4; frame++)
            { timelines.Apply(ids, positions, true, effects); timelines.Advance(ids, positions, true); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 8; frame++)
            { timelines.Apply(ids, positions, true, effects); timelines.Advance(ids, positions, true); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, $"warm full-domain crowd allocated {allocated} B");
        Console.WriteLine("bank-capacity: shuffled 65,536-id crowd apply+step retained 0 B");
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "closures run before the dispose later in the same method")]
    internal static void Concurrency()
    {
        const int readerIdCount = 64;
        const int frames = 200;
        const int readerCount = 4;
        const int publisherCount = 4;
        const int publishesPer = 400;
        for (ushort i = 0; i < readerIdCount; i++)
        {
            using var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 8));
            ReadOnlySpan<ushort> onePosition = [0];
            Timeline<BankTrack, BankClip>.Apply(asset.Index, onePosition, true, new float[1]);
        }
        var captured = new SlotView[readerIdCount];
        var capturedHashes = new ulong[readerIdCount];
        for (ushort i = 0; i < readerIdCount; i++)
        {
            captured[i] = Timeline<BankTrack, BankClip>.View(i);
            capturedHashes[i] = HashView(captured[i]);
        }

        var ids = new ushort[512];
        var positions = new ushort[512];
        var reference = new float[512];
        for (var i = 0; i < ids.Length; i++) { ids[i] = (ushort)(i % readerIdCount); positions[i] = (ushort)(i * 3 % 8); }
        var referencePositions = (ushort[])positions.Clone();
        for (var frame = 0; frame < frames; frame++)
            Timeline<BankTrack, BankClip>.Apply(ids, referencePositions, frame % 7 != 6, reference);

        var stableHashes = new ulong[publisherCount, publishesPer];
        var publishedIndexes = new ushort[publisherCount, publishesPer];
        using var barrier = new Barrier(readerCount + publisherCount);
        var readers = new Thread[readerCount];
        var readerAllocated = new long[readerCount];
        var readerMatch = new bool[readerCount];
        for (var r = 0; r < readerCount; r++)
        {
            var reader = r;
            readers[r] = new Thread(() =>
            {
                var readerIdCount = (ushort[])ids.Clone();
                var readerPositions = (ushort[])positions.Clone();
                var effects = new float[512];
                for (var frame = 0; frame < 20; frame++)
                    Timeline<BankTrack, BankClip>.Apply(readerIdCount, readerPositions, true, effects);
                Array.Clear(effects);
                barrier.SignalAndWait();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var frame = 0; frame < frames; frame++)
                    Timeline<BankTrack, BankClip>.Apply(readerIdCount, readerPositions, frame % 7 != 6, effects);
                readerAllocated[reader] = GC.GetAllocatedBytesForCurrentThread() - before;
                readerMatch[reader] = effects.AsSpan().SequenceEqual(reference);
            });
            readers[r].Start();
        }
        var publishers = new Thread[publisherCount];
        for (var p = 0; p < publisherCount; p++)
        {
            var publisher = p;
            publishers[p] = new Thread(() =>
            {
                ReadOnlySpan<ushort> onePosition = [0];
                var oneEffect = new float[1];
                var loaded = new List<TimelineAsset>(publishesPer);
                barrier.SignalAndWait();
                for (var n = 0; n < publishesPer; n++)
                {
                    var asset = TimelineAsset.LoadAsset(BakeBank(readerIdCount + publisher * publishesPer + n + 1f, 8));
                    loaded.Add(asset);
                    publishedIndexes[publisher, n] = asset.Index;
                    Timeline<BankTrack, BankClip>.Apply(asset.Index, onePosition, true, oneEffect);
                    stableHashes[publisher, n] = HashView(Timeline<BankTrack, BankClip>.View(asset.Index));
                }
                foreach (var asset in loaded) asset.Dispose();
            });
            publishers[p].Start();
        }
        foreach (var thread in readers) thread.Join();
        foreach (var thread in publishers) thread.Join();

        for (ushort i = 0; i < readerIdCount; i++)
        {
            Require(HashView(Timeline<BankTrack, BankClip>.View(i)) == capturedHashes[i], $"reader view {i} is byte-stable and never rebound");
            Require(HashView(captured[i]) == capturedHashes[i], $"captured copy {i} unchanged");
        }
        for (var p = 0; p < publisherCount; p++)
            for (var n = 0; n < publishesPer; n++)
                Require(HashView(Timeline<BankTrack, BankClip>.View(publishedIndexes[p, n])) == stableHashes[p, n], $"published view {publishedIndexes[p, n]} stable");
        for (var r = 0; r < readerCount; r++)
        {
            Require(readerMatch[r], $"reader {r} checksums equal the single-threaded reference");
            Require(readerAllocated[r] == 0, $"reader {r} allocated {readerAllocated[r]} B");
        }
        Console.WriteLine($"bank-concurrency: {readerCount} readers x {frames} frames over {readerIdCount} held views against {publisherCount} publishers x {publishesPer} resolves; checksums, view stability, and 0 B reader allocation PASS");
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "closures run before the dispose later in the same method")]
    [SuppressMessage("ReSharper", "DisposeOnUsingVariable", Justification = "explicit dispose exercises dispose semantics; using is the backstop")]
    internal static void ViewShapes()
    {
        const int contents = 32;
        var keepAlive = new List<TimelineAsset>();
        for (var i = 0; i < contents; i++)
        {
            var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 64));
            keepAlive.Add(asset);
            ReadOnlySpan<ushort> onePosition = [0];
            Timeline<BankTrack, BankClip>.Apply(asset.Index, onePosition, true, new float[1]);
        }
        var views = new SlotView[contents];
        for (ushort i = 0; i < contents; i++)
        {
            views[i] = Timeline<BankTrack, BankClip>.View(i);
            Require(views[i].Duration == 64 && views[i].TableTicks == 65, $"view {i} layout");
        }
        const int rows = 256;
        const int steps = 8;
        var ids = new ushort[rows];
        var positions = new ushort[rows];
        var effects = new float[rows];
        for (var i = 0; i < rows; i++) { ids[i] = (ushort)(i % contents); positions[i] = (ushort)(i * 7 % 64); }

        var oraclePositions = (ushort[])positions.Clone();
        var oracle = new float[rows];
        Timeline<BankTrack, BankClip>.Apply(ids, oraclePositions, true, oracle);
        unsafe
        {
            for (var i = 0; i < rows; i++)
                effects[i] += views[ids[i]].Forward[positions[i]];
        }
        Require(effects.AsSpan().SequenceEqual(oracle), "shared-clock add matches the crowd fold");

        Array.Clear(effects);
        var walkPositions = (ushort[])positions.Clone();
        var oracleWalkPositions = (ushort[])positions.Clone();
        var oracleWalk = new float[rows];
        unsafe
        {
            for (var i = 0; i < rows; i++)
            {
                var records = views[ids[i]].ForwardRecords;
                var position = walkPositions[i];
                for (var step = 0; step < steps && position < views[ids[i]].Duration; step++)
                {
                    ref var record = ref records[position];
                    effects[i] += record.Effect;
                    position = record.Next;
                }
                walkPositions[i] = position;
            }
        }
        for (var step = 0; step < steps; step++)
            { Timeline<BankTrack, BankClip>.Apply(ids, oracleWalkPositions, true, oracleWalk); Timeline.Advance(ids, oracleWalkPositions, true); }
        Require(effects.AsSpan().SequenceEqual(oracleWalk), "per-entity record walk matches the folded law");
        Require(walkPositions.AsSpan().SequenceEqual(oracleWalkPositions), "record walk clocks match the movement law");

        Array.Clear(effects);
        var scatter = new ushort[rows];
        for (var i = 0; i < rows; i++) scatter[i] = (ushort)(i * 11 % 65);
        var oracleBackward = new float[rows];
        var oracleScatter = (ushort[])scatter.Clone();
        Timeline<BankTrack, BankClip>.Apply(ids, oracleScatter, false, oracleBackward);
        unsafe
        {
            for (var i = 0; i < rows; i++)
                effects[i] += views[ids[i]].BackwardByPosition[scatter[i]];
        }
        Require(effects.AsSpan().SequenceEqual(oracleBackward), "gather-equivalent backward reads match the crowd fold");

        var foreign = TimelineAsset.LoadAsset(BakeForeign());
        keepAlive.Add(foreign);
        var foreignIndex = foreign.Index;
        RequireThrows<ArgumentException>(() => Timeline<BankTrack, BankClip>.View(foreignIndex), "pair-less view is rejected");

        var marker = TimelineAsset.LoadAsset(BakeBank(900f, 64));
        keepAlive.Add(marker);
        var markerIndex = marker.Index;
        ReadOnlySpan<ushort> markerPosition = [0];
        Timeline<BankTrack, BankClip>.Apply(markerIndex, markerPosition, true, new float[1]);
        var absent = Timeline<BankTrack, BankClip>.View(foreignIndex);
        unsafe
        {
            Require(absent.Absent == 1 && absent.Forward == null && absent.TableTicks == 0, "swept absent index exposes an Absent view");
        }
        RequireThrows<ArgumentException>(() => Timeline<BankTrack, BankClip>.View(ushort.MaxValue), "never-bound view is rejected");

        using var disposed = new TimelineSet<BankTrack, BankClip>();
        using var member = TimelineAsset.LoadAsset(BakeBank(901f, 8));
        disposed.Add(member);
        disposed.Dispose();
#if TL_CHECKED
        RequireThrows<ObjectDisposedException>(() => disposed.View(0), "disposed bank rejects views");
#endif

        foreach (var asset in keepAlive) asset.Dispose();
#if TL_CHECKED
        Console.WriteLine($"bank-views: shared-clock add, per-entity record walk, and gather-equivalent reads over {contents} held views are bit-exact; absent, pair-less, never-bound, and disposed acquisition semantics PASS");
#else
        Console.WriteLine($"bank-views: shared-clock add, per-entity record walk, and gather-equivalent reads over {contents} held views are bit-exact; absent, pair-less, and never-bound acquisition semantics PASS");
#endif
    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "exact float parity is the receipt")]
    internal static void Workload()
    {
        var baker = new DomainBaker();
        for (var k = 0; k < KindBakes.Length; k++)
            baker = KindBakes[k](baker, k);
        using var asset = TimelineAsset.LoadAsset(baker.Bake());
        Require(asset.Reference.PairCount == 64, "the asset carries all 64 pair kinds");
        for (var k = 0; k < 32; k++)
        {
            var view = KindRoots[k](asset.Index);
            Require(view.Duration == 16 && view.TableTicks == 17 && view.RecordBytes == 8, $"kind {k} view layout");
        }
        Console.WriteLine($"bank-workload: 64-pair asset, 32 live pair kinds folded, one bank per pair");

        var clipBaker = new DomainBaker().Track<BankTrack, BankClip>(new BankTrack(1f));
        for (uint t = 0; t < 100; t++)
            clipBaker.Clip(0, t, t + 1, new BankClip(t + 1f));
        using var clips = TimelineAsset.LoadAsset(clipBaker.Bake());
        var clipView = Timeline<BankTrack, BankClip>.View(clips.Index);
        Require(clipView.Duration == 100 && clipView.TableTicks == 101, "100-clip view layout");
        unsafe
        {
            Require(clipView.Forward[0] == 1f && clipView.Forward[37] == 38f && clipView.Forward[99] == 100f && clipView.Forward[100] == 0f, "100-clip fold");
        }
        Console.WriteLine($"bank-workload: a 100-clip timeline block is {64 + 28 * 101} B (64-B header + 28 B per tick)");

        const int instances = 512;
        var ids = new ushort[instances];
        var positions = new ushort[instances];
        var oracle = new float[instances];
        for (var i = 0; i < instances; i++) { ids[i] = clips.Index; positions[i] = (ushort)(i % 100); }
        var oraclePositions = (ushort[])positions.Clone();
        for (var frame = 0; frame < 16; frame++)
            { Timeline<BankTrack, BankClip>.Apply(ids, oraclePositions, true, oracle); Timeline.Advance(ids, oraclePositions, true); }
        unsafe
        {
            for (var i = 0; i < instances; i++)
            {
                var total = 0f;
                var position = positions[i];
                var records = clipView.ForwardRecords;
                for (var frame = 0; frame < 16 && position < clipView.Duration; frame++)
                {
                    ref var record = ref records[position];
                    total += record.Effect;
                    position = record.Next;
                }
                Require(total == oracle[i], $"instance {i} view walk matches the crowd fold");
            }
        }
        Console.WriteLine($"bank-workload: {instances} instances ride one shared block through (index, position) columns, walks bit-exact");
    }

    [SuppressMessage("ReSharper", "DisposeOnUsingVariable", Justification = "explicit dispose exercises dispose semantics; using is the backstop")]
    internal static void Retained()
    {
        const int distinct = 1024;
        const int idCount = 65535;
        using var timelines = new TimelineSet<BankTrack, BankClip>();
        for (var i = 0; i < distinct; i++)
        {
            using var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 8));
            Require(timelines.Add(asset) == i, $"distinct id {i}");
        }
        using var duplicate = TimelineAsset.LoadAsset(BakeBank(1f, 8));
        for (var i = distinct; i < idCount; i++)
            Require(timelines.Add(duplicate) == i, $"shared id {i}");
        Require(timelines.BlockCount == distinct, "only the distinct contents own blocks");
        Require(timelines.SharedHits == idCount - distinct, "every duplicate shares the interned content block");
        Console.WriteLine($"bank-retained: {idCount} ids on {distinct} distinct 9-tick tables; blocks {timelines.BlockCount}, shared {timelines.SharedHits}, headers {timelines.HeaderBytes} B, tables {timelines.TableBytes} B, directories+retired {timelines.DirectoryBytes} B, retained {timelines.RetainedBytes} B, header+directory overhead {(double)(timelines.HeaderBytes + timelines.DirectoryBytes) / timelines.TableBytes:F2}x tables");

        var ids = new ushort[idCount];
        for (ushort i = 0; i < idCount; i++) ids[i] = i;
        Shuffle(ids, 7);
        var positions = new ushort[idCount];
        var effects = new float[idCount];
        for (var i = 0; i < idCount; i++) positions[i] = (ushort)(i % 8);
        for (var frame = 0; frame < 4; frame++)
            { timelines.Apply(ids, positions, true, effects); timelines.Advance(ids, positions, true); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 8; frame++)
            { timelines.Apply(ids, positions, true, effects); timelines.Advance(ids, positions, true); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, $"warm retained crowd allocated {allocated} B");
        Console.WriteLine("bank-retained: shuffled 65,535-id crowd apply+step retained 0 B");
        var beforeDispose = timelines.RetainedBytes;
        Require(beforeDispose == timelines.RetainedBytes, "retained accounting is stable");
        timelines.Dispose();
        Require(timelines.RetainedBytes == 0, "dispose frees every bank-attributable byte");
        Console.WriteLine($"bank-retained: dispose freed {beforeDispose} B, 0 bank-attributable bytes remain");
    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "exact float parity is the receipt")]
    internal static unsafe void StaleSnapshot()
    {
        using var timelines = new TimelineSet<BankTrack, BankClip>();
        for (var i = 0; i < 64; i++)
        {
            using var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 8));
            Require(timelines.Add(asset) == i, $"stale setup id {i}");
        }
        var stale = timelines._views;
        var capturedHashes = new ulong[64];
        for (var i = 0; i < 64; i++)
            capturedHashes[i] = HashView(*stale[i]);
        for (var i = 64; i < 4200; i++)
        {
            using var asset = TimelineAsset.LoadAsset(BakeBank(i + 1f, 8));
            Require(timelines.Add(asset) == i, $"growth id {i}");
        }
        
        {
            for (var i = 0; i < 64; i++)
            {
                var view = stale[i];
                Require(view != null && view->Duration == 8 && view->TableTicks == 9, $"stale entry {i} survives the growths");
                Require(view->Forward[0] == i + 1f && view->Forward[8] == 0f, $"stale entry {i} tables intact");
            }
        }
        const int rows = 256;
        var ids = new ushort[rows];
        var positions = new ushort[rows];
        var effects = new float[rows];
        for (var i = 0; i < rows; i++) { ids[i] = (ushort)(i % 64); positions[i] = (ushort)(i * 3 % 8); }
        var oracle = new float[rows];
        var oraclePositions = (ushort[])positions.Clone();
        
        {
            for (var i = 0; i < rows; i++)
            {
                var view = stale[ids[i]];
                effects[i] += view->Forward[positions[i]];
            }
        }
        timelines.Apply(ids, oraclePositions, true, oracle);
        Require(effects.AsSpan().SequenceEqual(oracle), "stale-snapshot reads match the crowd fold");
        Array.Clear(effects);
        
        {
            for (var i = 0; i < rows; i++)
            {
                var records = stale[ids[i]]->ForwardRecords;
                var position = positions[i];
                for (var step = 0; step < 8 && position < 8; step++)
                {
                    ref var record = ref records[position];
                    effects[i] += record.Effect;
                    position = record.Next;
                }
            }
        }
        var walkOracle = new float[rows];
        for (var step = 0; step < 8; step++)
            { timelines.Apply(ids, oraclePositions, true, walkOracle); timelines.Advance(ids, oraclePositions, true); }
        Require(effects.AsSpan().SequenceEqual(walkOracle), "stale-snapshot record walk matches the folded law");
        
        {
            for (var i = 0; i < 64; i++)
                Require(HashView(*stale[i]) == capturedHashes[i] && HashView(timelines.View((ushort)i)) == capturedHashes[i], $"view {i} byte-stable through the growths");
        }
        Console.WriteLine("bank-stale: a directory snapshot captured before three doublings still folds 64 ids bit-exactly with byte-stable blocks");
    }

    static byte[] BakeBank(float amount, ushort end)
        => new DomainBaker()
            .Track<BankTrack, BankClip>(new BankTrack(1f))
            .Clip(0, 0u, end, new BankClip(amount))
            .Bake();

    static byte[] BakeForeign()
        => new DomainBaker()
            .Track<BankDamageTrack, BankDamageClip>(new BankDamageTrack(1f))
            .Clip(0, 0u, 8u, new BankDamageClip(3f))
            .Bake();

    static DomainBaker BakeKind<TK>(DomainBaker baker, int slot) where TK : unmanaged
        => baker
            .Track<BankTrack<TK>, BankClip<TK>>(new BankTrack<TK>(1f))
            .Clip(slot, 0u, 8u, new BankClip<TK>(1f))
            .Clip(slot, 8u, 16u, new BankClip<TK>(2f));

    static SlotView FoldKind<TK>(ushort index) where TK : unmanaged
    {
        ReadOnlySpan<ushort> onePosition = [0];
        Timeline<BankTrack<TK>, BankClip<TK>>.Apply(index, onePosition, true, new float[1]);
        return Timeline<BankTrack<TK>, BankClip<TK>>.View(index);
    }

    static readonly Func<DomainBaker, int, DomainBaker>[] KindBakes = [BakeKind<W0>, BakeKind<W1>, BakeKind<W2>, BakeKind<W3>, BakeKind<W4>, BakeKind<W5>, BakeKind<W6>, BakeKind<W7>, BakeKind<W8>, BakeKind<W9>, BakeKind<W10>, BakeKind<W11>, BakeKind<W12>, BakeKind<W13>, BakeKind<W14>, BakeKind<W15>, BakeKind<W16>, BakeKind<W17>, BakeKind<W18>, BakeKind<W19>, BakeKind<W20>, BakeKind<W21>, BakeKind<W22>, BakeKind<W23>, BakeKind<W24>, BakeKind<W25>, BakeKind<W26>, BakeKind<W27>, BakeKind<W28>, BakeKind<W29>, BakeKind<W30>, BakeKind<W31>, BakeKind<W32>, BakeKind<W33>, BakeKind<W34>, BakeKind<W35>, BakeKind<W36>, BakeKind<W37>, BakeKind<W38>, BakeKind<W39>, BakeKind<W40>, BakeKind<W41>, BakeKind<W42>, BakeKind<W43>, BakeKind<W44>, BakeKind<W45>, BakeKind<W46>, BakeKind<W47>, BakeKind<W48>, BakeKind<W49>, BakeKind<W50>, BakeKind<W51>, BakeKind<W52>, BakeKind<W53>, BakeKind<W54>, BakeKind<W55>, BakeKind<W56>, BakeKind<W57>, BakeKind<W58>, BakeKind<W59>, BakeKind<W60>, BakeKind<W61>, BakeKind<W62>, BakeKind<W63>];

    static readonly Func<ushort, SlotView>[] KindRoots = [FoldKind<W0>, FoldKind<W1>, FoldKind<W2>, FoldKind<W3>, FoldKind<W4>, FoldKind<W5>, FoldKind<W6>, FoldKind<W7>, FoldKind<W8>, FoldKind<W9>, FoldKind<W10>, FoldKind<W11>, FoldKind<W12>, FoldKind<W13>, FoldKind<W14>, FoldKind<W15>, FoldKind<W16>, FoldKind<W17>, FoldKind<W18>, FoldKind<W19>, FoldKind<W20>, FoldKind<W21>, FoldKind<W22>, FoldKind<W23>, FoldKind<W24>, FoldKind<W25>, FoldKind<W26>, FoldKind<W27>, FoldKind<W28>, FoldKind<W29>, FoldKind<W30>, FoldKind<W31>];

    static unsafe ulong HashView(SlotView view)
    {
        var p = (byte*)&view;
        var h = 0x9E3779B97F4A7C15UL;
        for (var i = 0; i < 64; i++)
        {
            h ^= p[i];
            h *= 0x100000001B3UL;
        }
        var tables = (byte*)view.Forward;
        for (var i = 0; i < 28L * view.TableTicks; i++)
        {
            h ^= tables[i];
            h *= 0x100000001B3UL;
        }
        return h;
    }

    static void Shuffle(ushort[] values, int seed)
    {
        var random = new Random(seed);
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"bank receipt failed: {label}");
    }

    static void RequireThrows<TException>(Action action, string label) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"bank receipt failed: expected {typeof(TException).Name} ({label})");
    }
}
