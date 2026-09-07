using BenchmarkDotNet.Running;
using Tl.Hooks;
using ProbeTimeline = Tl.Hooks.Timeline<Tl.Hooks.EdgeVerification.ProbeTrack, Tl.Hooks.EdgeVerification.ProbeClip>;

if (args is ["--verify-edges"])
{
    EdgeVerification.Run();
    return;
}

if (args is ["--verify"])
{
    var benchmark = new Dispatch();
    benchmark.Setup();
    var expected = benchmark.Direct();
    foreach (var actual in new[] { benchmark.GeneratedLink(), benchmark.Constrained(), benchmark.ExplicitConstrained(), benchmark.CachedDelegate(), benchmark.BoxedOnce() })
        if (actual != expected)
            throw new InvalidOperationException($"Dispatch receipt mismatch: {expected} / {actual}");

    var mixed = benchmark.MixedBoxes();
    if (mixed.Count != expected.Count || mixed.Ticks != expected.Ticks || Math.Abs(mixed.Sum - expected.Sum) > 0.5f)
        throw new InvalidOperationException("Mixed dispatch receipt mismatch.");

    if (benchmark.EscapingBoxPerCall() != Dispatch.Operations)
        throw new InvalidOperationException("Box-per-call count mismatch.");

    var receiver = new Receiver { Value = 1f };
    var frame = new Frame(1, 0.5f);
    ITimelineForward boxedCopy = receiver;
    boxedCopy.OnForward(in frame);
    if (receiver.Count != 0)
        throw new InvalidOperationException("Unexpected boxing semantics.");
    ReceiverLink.Forward(ref receiver, in frame);
    if (receiver.Count != 1)
        throw new InvalidOperationException("Generated hook lost the caller's mutation.");

    var data = new DataFlow();
    data.Setup();
    var directData = data.DirectData();
    if (data.RefGenericData() != directData)
        throw new InvalidOperationException($"Ref-data receipt mismatch: {directData} / {data.RefGenericData()}.");

    var busy = new BusyPlayer();
    IdleCalls.Dim(ref busy);
    if (busy.Count != 1)
        throw new InvalidOperationException("Overridden hook lost the caller's mutation.");

    var idle = new IdlePlayer();
    IdleCalls.Dim(ref idle);
    Console.WriteLine(idle.Count == 0
        ? "Default-interface hook ran on a boxed copy: the caller's mutation was silently lost."
        : "Default-interface hook mutated the caller directly (no boxing).");

    var shape = new ApiShape();
    shape.Setup();
    var directShape = shape.DirectTicks();
    foreach (var actual in new[] { shape.ShellSingle(), shape.ShellParamsFour(), shape.PlaybackSingle(), shape.PlaybackParamsFour() })
        if (actual != directShape)
            throw new InvalidOperationException($"API shape receipt mismatch: {directShape} / {actual}.");

    var timelineA = Timeline<VitalsTrack, VitalsClip>.Build(static b => { });
    var timelineB = Timeline<VitalsTrack, VitalsClip>.Build(static b => { });
    if (timelineB != timelineA + 1)
        throw new InvalidOperationException("Timeline indices are not sequential.");
    Console.WriteLine($"Timeline index {timelineA}, then {timelineB} (per closed generic type).");

    // One Build'd index serves every consumer type: the registry keys on
    // (TTrack, TClip) alone and the data arrives by method-level inference,
    // so two different data types play the SAME index. Each accumulates its
    // own result, and playing one leaves the other's instance untouched.
    var shared = Timeline<VitalsTrack, VitalsClip>.Build(b =>
    {
        var track = b.Track(new VitalsTrack(2));
        b.Clip(track, new VitalsClip(5f), 0, 10);
    });
    if (shared != timelineB + 1)
        throw new InvalidOperationException($"Sequential registry expected {timelineB + 1}, got {shared}.");

    var left = new SharedLeft();
    var right = new SharedRight();
    var sharedState = Timeline<VitalsTrack, VitalsClip>.Start(shared);
    sharedState = Timeline<VitalsTrack, VitalsClip>.Forward(shared, in sharedState, ref left, 4);
    sharedState = Timeline<VitalsTrack, VitalsClip>.Forward(shared, in sharedState, ref right, 5);
    if (left is not { Sum: 5f, Seen: 1, Rewinds: 0 } || right is not { Ticks: 7L, Seen: 1, Rewinds: 0 })
        throw new InvalidOperationException($"Shared index receipt mismatch: left {left.Sum}/{left.Seen}, right {right.Ticks}/{right.Seen}.");

    sharedState = Timeline<VitalsTrack, VitalsClip>.Forward(shared, in sharedState, ref left, 6);
    sharedState = Timeline<VitalsTrack, VitalsClip>.Backward(shared, in sharedState, ref right, 4);
    if (left is not { Sum: 10f, Seen: 2, Rewinds: 0 } || right is not { Ticks: 1L, Seen: 1, Rewinds: 1 })
        throw new InvalidOperationException($"Playing one data type must leave the other untouched: left {left.Sum}/{left.Seen}, right {right.Ticks}/{right.Seen}/{right.Rewinds}.");
    Console.WriteLine($"Index {shared} served two data types from one Build: left {left.Sum:R}/{left.Seen}, right {right.Ticks}/{right.Rewinds}.");

    var instance = shape.BuildTimeline();
    var viaInstance = shape.RunInstance(instance);
    if (viaInstance != directShape)
        throw new InvalidOperationException($"Runtime timeline receipt mismatch: {directShape} / {viaInstance}.");

    var sparseSmall = new Sparse<Sparse256>();
    sparseSmall.Setup();
    var expectedDispatch = sparseSmall.Binary();
    foreach (var actual in new[] { sparseSmall.Linear(), sparseSmall.FlatSwitch(), sparseSmall.Radix8(), sparseSmall.Radix4(), sparseSmall.DenseFp() })
        if (actual != expectedDispatch)
            throw new InvalidOperationException($"Sparse256 dispatch mismatch: {expectedDispatch} / {actual}.");

    var sparseLarge = new Sparse<Sparse4096>();
    sparseLarge.Setup();
    expectedDispatch = sparseLarge.Binary();
    foreach (var actual in new[] { sparseLarge.Linear(), sparseLarge.FlatSwitch(), sparseLarge.Radix8(), sparseLarge.Radix4(), sparseLarge.DenseFp() })
        if (actual != expectedDispatch)
            throw new InvalidOperationException($"Sparse4096 dispatch mismatch: {expectedDispatch} / {actual}.");
    Console.WriteLine($"Sparse256/Sparse4096: all six dispatch arms agree ({expectedDispatch.Count:N0} random hits each).");

    var fusedSmall = new Fused<Fused256>();
    fusedSmall.Setup();
    var expectedFused = fusedSmall.TwoLevelFp();
    foreach (var actual in new[] { fusedSmall.FusedSparse(), fusedSmall.FusedDense() })
        if (actual != expectedFused)
            throw new InvalidOperationException($"Fused256 dispatch mismatch: {expectedFused} / {actual}.");

    var fusedLarge = new Fused<Fused4096>();
    fusedLarge.Setup();
    expectedFused = fusedLarge.TwoLevelFp();
    foreach (var actual in new[] { fusedLarge.FusedSparse(), fusedLarge.FusedDense() })
        if (actual != expectedFused)
            throw new InvalidOperationException($"Fused4096 dispatch mismatch: {expectedFused} / {actual}.");
    Console.WriteLine($"Fused256/Fused4096: fused and two-level dispatch agree ({expectedFused.Count:N0} random hits each).");

    var vitals = new Vitals { Health = 100_000f };
    GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(ref vitals, 10, 20, 30);
    var authored = new Vitals { Health = 100_000f };
    Timeline<VitalsTrack, VitalsClip>.Forward(instance, ref authored, 10, 20, 30);
    if (authored.Result != vitals.Result)
        throw new InvalidOperationException($"Runtime timeline demo mismatch: {vitals.Result} / {authored.Result}.");
    Console.WriteLine($"Runtime-authored timeline matches the generated one: {authored.Result}");

    // Playback: packed state, movement flags, direction mirrors, loops.
    var packed = new Playback(5, 7, PlaybackFlags.Enter | PlaybackFlags.Last);
    if (packed.Tick != 5 || packed.Cycles != 7 || !packed.Has(PlaybackFlags.Enter)
        || !packed.Has(PlaybackFlags.Last) || packed.Has(PlaybackFlags.Exit))
        throw new InvalidOperationException("Playback packing round-trip failed.");

    var walk = new Vitals { Health = 100_000f };
    uint enters = 0, exits = 0, actives = 0, completes = 0;
    var pw = Playback.Start();
    for (uint t = 0; t < 515; t++)
    {
        pw = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pw, ref walk, t);
        if (pw.Has(PlaybackFlags.Enter)) enters++;
        if (pw.Has(PlaybackFlags.Exit)) exits++;
        if (pw.Has(PlaybackFlags.Active)) actives++;
        if (pw.Has(PlaybackFlags.Complete)) completes++;
    }

    // The status word ORs across clips, so counts are distinct boundary
    // ticks: starts {3,18,29,47,76,200,321}, ends {7,11,29,47,76,123,321};
    // 431 ticks are covered; completion is at 599, beyond this walk.
    if ((enters, exits, actives, completes) != (7, 7, 431, 0))
        throw new InvalidOperationException($"Forward walk flag counts wrong: {enters}/{exits}/{actives}/{completes}.");
    if (pw.Tick != 514 || pw.Cycles != 0)
        throw new InvalidOperationException($"Forward walk ended at {pw.Tick}, cycles {pw.Cycles}.");

    var spot = Playback.Start();
    spot = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in spot, ref walk, 0);
    if (spot.Flags != (PlaybackFlags.First | PlaybackFlags.Active))
        throw new InvalidOperationException($"Tick 0 should be First|Active, got {spot.Flags}.");

    spot = Playback.Start(14);
    spot = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in spot, ref walk, 15);
    if (spot.Flags != PlaybackFlags.None)
        throw new InvalidOperationException($"Gap tick 15 should carry no flags, got {spot.Flags}.");

    spot = Playback.Start();
    spot = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in spot, ref walk, 10);
    if (spot.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Jump to 10 should carry Enter|Exit|Active|Last, got {spot.Flags}.");

    var repeat = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in spot, ref walk, 10);
    if (repeat.Flags != (PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Repeated tick should keep positional flags only, got {repeat.Flags}.");

    var back = Playback.Start(10);
    back = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in back, ref walk, 6);
    if (back.Flags != (PlaybackFlags.Enter | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Backward 10->6 should carry Enter|Active|Last, got {back.Flags}.");

    back = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in back, ref walk, 2);
    if (back.Flags != (PlaybackFlags.Exit | PlaybackFlags.Active))
        throw new InvalidOperationException($"Backward 6->2 should carry Exit|Active, got {back.Flags}.");

    // Forward then rewind: exact counters restored, floats within tolerance.
    var rt = new Vitals { Health = 100_000f };
    var pf = Playback.Start();
    for (uint t = 0; t < 515; t++)
        pf = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pf, ref rt, t);
    var pb = pf;
    for (int t = 514; t >= 0; t--)
        pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pb, ref rt, (uint)t);
    if (Math.Abs(rt.Health - 100_000f) > 1f || rt.Ticks != 0 || rt.Count != 515 || rt.Back != 515)
        throw new InvalidOperationException($"Rewind did not restore state: {rt.Result}, back {rt.Back}.");

    uint bEnters = 0, bExits = 0;
    var bw = new Vitals();
    var pb2 = Playback.Start(514);
    for (int t = 513; t >= 0; t--)
    {
        pb2 = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pb2, ref bw, (uint)t);
        if (pb2.Has(PlaybackFlags.Enter)) bEnters++;
        if (pb2.Has(PlaybackFlags.Exit)) bExits++;
    }

    if ((bEnters, bExits) != (7, 7))
        throw new InvalidOperationException($"Backward walk flag counts wrong: {bEnters}/{bExits}.");

    // Looping: wraps move Cycles instead of Complete; forward wraps count
    // exactly, a full-cycle jump sets Enter|Exit, backward wraps saturate.
    var lv = new Vitals();
    var lp = Playback.Start();
    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lp, ref lv, 685);
    if (lp.Tick != 685 || lp.Cycles != 1 || lp.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active))
        throw new InvalidOperationException($"Loop wrap to 685 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lp, ref lv, 1205);
    if (lp.Tick != 1205 || lp.Cycles != 2 || !lp.Has(PlaybackFlags.Enter) || !lp.Has(PlaybackFlags.Active))
        throw new InvalidOperationException($"Second wrap to 1205 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    var multi = Playback.Start();
    multi = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in multi, ref lv, 1205);
    if (multi.Cycles != 2 || !multi.Has(PlaybackFlags.Enter) || !multi.Has(PlaybackFlags.Exit) || !multi.Has(PlaybackFlags.Active))
        throw new InvalidOperationException($"Two-cycle jump should be full coverage, got {multi.Cycles}, {multi.Flags}.");

    var wrap = Playback.Start(0);
    wrap = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Backward(in wrap, ref lv, 599);
    if (wrap.Cycles != 0
        || wrap.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Backward wrap past zero wrong: {wrap.Cycles}, {wrap.Flags}.");

    var lr = new Vitals();
    var pl = Playback.Start();
    for (uint t = 0; t <= 600; t++)
        pl = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in pl, ref lr, t);
    if (pl.Cycles != 1)
        throw new InvalidOperationException($"Looping walk should report one cycle, got {pl.Cycles}.");
    var plb = pl;
    for (int t = 600; t >= 0; t--)
        plb = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Backward(in plb, ref lr, (uint)t);
    if (plb.Cycles != 0 || Math.Abs(lr.Health) > 1f || lr.Ticks != 0 || lr.Count != 601 || lr.Back != 601)
        throw new InvalidOperationException($"Looping rewind did not restore: cycles {plb.Cycles}, {lr.Result}, back {lr.Back}.");

    // Clip-level hooks: one call per active clip, exact inverse on backward.
    var cv = new Vitals { Health = 100_000f };
    var pc = Playback.Start();
    for (uint t = 0; t < 515; t++)
        pc = ClipTimeline<VitalsTrack, VitalsClip>.Forward(in pc, ref cv, t);
    if (cv.Count != 965)
        throw new InvalidOperationException($"Clip hooks fired {cv.Count} times, expected 965.");
    var pcb = pc;
    for (int t = 514; t >= 0; t--)
        pcb = ClipTimeline<VitalsTrack, VitalsClip>.Backward(in pcb, ref cv, (uint)t);
    if (cv.Back != 965 || Math.Abs(cv.Health - 100_000f) > 1f || cv.Ticks != 0)
        throw new InvalidOperationException($"Clip-level rewind did not restore: back {cv.Back}, health {cv.Health}, ticks {cv.Ticks}.");

    // Runtime indices share the same Playback core. Looping is an authored
    // trait now, so the looping variant is a second Build.
    var runtime = shape.BuildTimeline();
    var rv = new Vitals { Health = 100_000f };
    var rp = Timeline<VitalsTrack, VitalsClip>.Start(runtime);
    for (uint t = 0; t < 600; t++)
        rp = Timeline<VitalsTrack, VitalsClip>.Forward(runtime, in rp, ref rv, t);
    if (!rp.Has(PlaybackFlags.Complete) || rp.Tick != 599)
        throw new InvalidOperationException($"Runtime walk should complete at 599: {rp.Tick}, {rp.Flags}.");

    var runtimeLoop = shape.BuildTimeline(loops: true);
    var loopStart = Timeline<VitalsTrack, VitalsClip>.Start(runtimeLoop);
    var rp2 = Timeline<VitalsTrack, VitalsClip>.Forward(runtimeLoop, in loopStart, ref rv, 700);
    if (rp2.Cycles != 1 || rp2.Tick != 700)
        throw new InvalidOperationException($"Runtime loop wrap wrong: {rp2.Tick}, {rp2.Cycles}.");

    // Index-registry receipts: sequential assignment, tombstones, no reuse,
    // capturing definitions, and empty definitions.
    var destroyed = ProbeTimeline.Build(static b => { });
    var live = ProbeTimeline.Build(static b =>
    {
        var track = b.Track(new EdgeVerification.ProbeTrack(0));
        b.Clip(track, new EdgeVerification.ProbeClip(10), 0, 10);
    });
    if (destroyed != 0 || live != 1)
        throw new InvalidOperationException($"A fresh closed type must assign indices 0 and 1, got {destroyed}, {live}.");

    ProbeTimeline.Destroy(destroyed);
    try
    {
        ProbeTimeline.Start(destroyed);
        throw new InvalidOperationException("Start on a destroyed index must throw.");
    }
    catch (ArgumentOutOfRangeException) { }

    var probeData = new EdgeVerification.Probe();
    var probeState = ProbeTimeline.Start(live);
    probeState = ProbeTimeline.Forward(live, in probeState, ref probeData, 5);
    if (!probeState.Has(PlaybackFlags.Active) || probeData.Tracks != 1 || probeData.Sum != 10f)
        throw new InvalidOperationException($"A live index stopped playing after a destroy: {probeState.Flags}, tracks {probeData.Tracks}, sum {probeData.Sum}.");

    var rebuilt = ProbeTimeline.Build(static b => { });
    if (rebuilt == destroyed || rebuilt != live + 1)
        throw new InvalidOperationException($"Build reused a destroyed index: destroyed {destroyed}, live {live}, rebuilt {rebuilt}.");

    // The definition delegate is not restricted to static lambdas: a closure
    // variable can author clip payloads.
    float closure = 7f;
    var captured = Timeline<VitalsTrack, VitalsClip>.Build(b =>
    {
        var track = b.Track(new VitalsTrack(1));
        b.Clip(track, new VitalsClip(closure), 0, 10);
    });
    var closureData = new Vitals();
    Timeline<VitalsTrack, VitalsClip>.Forward(captured, ref closureData, 5);
    if (closureData.Count != 1 || closureData.Ticks != 1 || closureData.Health != closure)
        throw new InvalidOperationException($"Capturing definition mismatch: {closureData.Result}.");

    // Empty definitions stay valid: empty tables, duration 0, Complete.
    var emptyIndex = Timeline<VitalsTrack, VitalsClip>.Build(static b => { });
    var emptyState = Timeline<VitalsTrack, VitalsClip>.Start(emptyIndex);
    var emptyData = new Vitals();
    emptyState = Timeline<VitalsTrack, VitalsClip>.Forward(emptyIndex, in emptyState, ref emptyData, 0);
    if (!emptyState.Has(PlaybackFlags.Complete) || emptyData.Count != 1 || emptyData.Ticks != 0)
        throw new InvalidOperationException($"Empty definition mismatch: {emptyState.Flags}, {emptyData.Result}.");

    Console.WriteLine($"Index registry verified: sequential builds {destroyed}/{live}/{rebuilt}, tombstone rejects playback, no reuse, capturing and empty definitions all work.");

    // Frozen path parity: per-timeline code specialized at emission time
    // (Generated/VitalsFrozen.g.cs, Generated/Fused16Frozen.g.cs), receipt-
    // matched here against the PlaybackCore oracle through the real tables.
    // Accumulation contract, mirrored bit-for-bit in this order by both
    // sides: Sum += one blend-resolved clip value per active track in
    // track-row order, then Flags += (uint)status, then Count++ for ticks
    // with at least one active track; Backward subtracts in the same order
    // (the exact inverse).
    VerifyFrozen<VitalsTrack, VitalsClip, OracleVitals, VitalsFrozen>("Vitals", 600u);

    // The Fused16 oracle tables are hand-set literals; this cross-checks
    // them against the raw clip description (clip i = [i*4, i*4+3), value
    // i+1) so the generator and the oracle agree by construction, not by
    // copy: every tick's region slice and resolved value must match a
    // brute-force scan of the clip list.
    for (uint t = 0; t <= 73u; t++)
    {
        var count = 0;
        var sum = 0f;
        for (uint i = 0; i < 16; i++)
            if (i * 4 <= t && t < i * 4 + 3)
            {
                count++;
                sum += i + 1;
            }

        var starts = Fused16Track.RegionStarts;
        var region = 0;
        while (region + 1 < starts.Length && starts[region + 1] <= t)
            region++;
        var row = Fused16Track.RegionRows[region];
        var tableCount = 0;
        var tableSum = 0f;
        for (var k = 0; k < row.TrackCount; k++)
        {
            var track = Fused16Track.TrackRows[row.TrackStart + k];
            tableCount++;
            tableSum += Fused16Track.ClipData[Fused16Track.ClipRows[track.ClipStart].ClipIndex].Value;
        }

        if (tableCount != count || tableSum != sum)
            throw new InvalidOperationException($"Fused16 table literals diverge from the clip list at tick {t}.");
    }
    for (uint i = 0; i < 16; i++)
        if (Fused16Track.ClipEdges[(int)i] != new ClipEdge(i * 4, i * 4 + 3))
            throw new InvalidOperationException($"Fused16 clip-edge literal {i} diverges from the clip list.");

    VerifyFrozen<Fused16Track, Fused16Clip, OracleFused16, Fused16Frozen>("Fused16", 63u);
    Console.WriteLine("Frozen parity verified: walks, jumps, mirrors, batches, flags (Vitals + Fused16).");

    void VerifyFrozen<TTrack, TClip, TData, TFrozen>(string name, uint duration)
        where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
        where TClip : unmanaged
        where TData : struct, IForwardTracks<TTrack, TClip, TData>, IBackwardTracks<TTrack, TClip, TData>, IFrozenOracle
        where TFrozen : struct, IFrozen
    {
        void RequireStep(string what, in Playback oracle, in Playback frozen)
        {
            if (oracle.Tick != frozen.Tick || oracle.Cycles != frozen.Cycles || oracle.Flags != frozen.Flags)
                throw new InvalidOperationException(
                    $"{name} {what}: Playback diverged ({oracle.Tick}/{oracle.Cycles}/{oracle.Flags} vs {frozen.Tick}/{frozen.Cycles}/{frozen.Flags}).");
        }

        void RequireSink(string what, in FrozenSink oracle, in FrozenSink frozen)
        {
            if (oracle.Sum != frozen.Sum || oracle.Flags != frozen.Flags || oracle.Count != frozen.Count)
                throw new InvalidOperationException(
                    $"{name} {what}: sink diverged ({oracle.Sum:R}/{oracle.Flags}/{oracle.Count} vs {frozen.Sum:R}/{frozen.Flags}/{frozen.Count}).");
        }

        // 1. Sequential forward walk 0..duration+10: the returned Playback is
        //    equal at every step (Tick, Cycles, Flags) and the sinks at the
        //    end are equal with exact float equality. The tail steps past the
        //    duration land in the empty sentinel region.
        var oracle = new TData();
        var frozen = default(FrozenSink);
        var walkOracle = GeneratedTimeline<TTrack, TClip>.Start();
        var walkFrozen = TFrozen.Start();
        uint oEnter = 0, oExit = 0, oActive = 0, oComplete = 0, oFirst = 0, oLast = 0;
        uint fEnter = 0, fExit = 0, fActive = 0, fComplete = 0, fFirst = 0, fLast = 0;
        for (uint t = 0; t < duration + 10; t++)
        {
            walkOracle = GeneratedTimeline<TTrack, TClip>.Forward(in walkOracle, ref oracle, t);
            walkFrozen = TFrozen.Forward(in walkFrozen, ref frozen, t);
            RequireStep($"walk tick {t}", walkOracle, walkFrozen);
            if (walkOracle.Has(PlaybackFlags.Enter)) oEnter++;
            if (walkOracle.Has(PlaybackFlags.Exit)) oExit++;
            if (walkOracle.Has(PlaybackFlags.Active)) oActive++;
            if (walkOracle.Has(PlaybackFlags.Complete)) oComplete++;
            if (walkOracle.Has(PlaybackFlags.First)) oFirst++;
            if (walkOracle.Has(PlaybackFlags.Last)) oLast++;
            if (walkFrozen.Has(PlaybackFlags.Enter)) fEnter++;
            if (walkFrozen.Has(PlaybackFlags.Exit)) fExit++;
            if (walkFrozen.Has(PlaybackFlags.Active)) fActive++;
            if (walkFrozen.Has(PlaybackFlags.Complete)) fComplete++;
            if (walkFrozen.Has(PlaybackFlags.First)) fFirst++;
            if (walkFrozen.Has(PlaybackFlags.Last)) fLast++;
        }
        RequireSink("walk sink", oracle.Sink, frozen);

        // 5. Flag-count totals over the full walk: frozen totals against
        //    oracle totals, nothing hardcoded.
        if ((oEnter, oExit, oActive, oComplete, oFirst, oLast) != (fEnter, fExit, fActive, fComplete, fFirst, fLast))
            throw new InvalidOperationException(
                $"{name} flag totals diverged: ({oEnter}/{oExit}/{oActive}/{oComplete}/{oFirst}/{oLast}) vs ({fEnter}/{fExit}/{fActive}/{fComplete}/{fFirst}/{fLast}).");

        // 2. Random jump battery: 24 deterministic xorshift [from, to] pairs,
        //    in range and beyond the duration, forward and backward - each
        //    step must agree on the Playback and the sink delta.
        uint random = 0x6D2B79F5u;
        for (var j = 0; j < 24; j++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            var from = random % (duration + 20);
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            var to = random % (duration + 20);

            var jumpOracleData = new TData();
            var jumpFrozenSink = default(FrozenSink);
            var jumpOracle = GeneratedTimeline<TTrack, TClip>.Start(from);
            var jumpFrozen = TFrozen.Start(from);
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Forward(in jumpOracle, ref jumpOracleData, to);
            jumpFrozen = TFrozen.Forward(in jumpFrozen, ref jumpFrozenSink, to);
            RequireStep($"forward jump {from}->{to}", jumpOracle, jumpFrozen);
            RequireSink($"forward jump {from}->{to}", jumpOracleData.Sink, jumpFrozenSink);

            jumpOracleData = new TData();
            jumpFrozenSink = default;
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Start(from);
            jumpFrozen = TFrozen.Start(from);
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Backward(in jumpOracle, ref jumpOracleData, to);
            jumpFrozen = TFrozen.Backward(in jumpFrozen, ref jumpFrozenSink, to);
            RequireStep($"backward jump {from}->{to}", jumpOracle, jumpFrozen);
            RequireSink($"backward jump {from}->{to}", jumpOracleData.Sink, jumpFrozenSink);
        }

        // 3. Backward mirror: forward walk, then rewind tick-by-tick to 0 -
        //    the Playback is equal each step and both sinks land back on
        //    their initial values (Count and Flags exactly zero; Sum within
        //    float cancellation of the interleaved adds and subtracts).
        var mirrorOracleData = new TData();
        var mirrorFrozenSink = default(FrozenSink);
        var mirrorOracle = GeneratedTimeline<TTrack, TClip>.Start();
        var mirrorFrozen = TFrozen.Start();
        for (uint t = 0; t < duration; t++)
        {
            mirrorOracle = GeneratedTimeline<TTrack, TClip>.Forward(in mirrorOracle, ref mirrorOracleData, t);
            mirrorFrozen = TFrozen.Forward(in mirrorFrozen, ref mirrorFrozenSink, t);
        }
        for (var t = (int)(duration - 1); t >= 0; t--)
        {
            mirrorOracle = GeneratedTimeline<TTrack, TClip>.Backward(in mirrorOracle, ref mirrorOracleData, (uint)t);
            mirrorFrozen = TFrozen.Backward(in mirrorFrozen, ref mirrorFrozenSink, (uint)t);
            RequireStep($"mirror tick {t}", mirrorOracle, mirrorFrozen);
            RequireSink($"mirror tick {t}", mirrorOracleData.Sink, mirrorFrozenSink);
        }
        if (mirrorFrozenSink.Count != 0 || mirrorFrozenSink.Flags != 0L || Math.Abs(mirrorFrozenSink.Sum) > 1e-2f)
            throw new InvalidOperationException(
                $"{name} mirror did not restore the sink: {mirrorFrozenSink.Sum:R}, {mirrorFrozenSink.Flags}, {mirrorFrozenSink.Count}.");

        // 4. Batch parity: one 8-tick call (mixed jumps, one beyond the
        //    duration) against eight single calls - equal final Playback and
        //    sink on both paths.
        Span<uint> batch = [duration / 3, 1, duration - 1, duration + 7, 0, duration / 2, 2, duration - 2];
        var batchOracleData = new TData();
        var batchFrozenSink = default(FrozenSink);
        var singleFrozenSink = default(FrozenSink);
        var batchStartOracle = Playback.Start();
        var batchStartFrozen = TFrozen.Start();
        var batchOracle = GeneratedTimeline<TTrack, TClip>.Forward(in batchStartOracle, ref batchOracleData, batch);
        var batchFrozen = TFrozen.Forward(in batchStartFrozen, ref batchFrozenSink, batch);
        RequireStep("batch", batchOracle, batchFrozen);
        RequireSink("batch", batchOracleData.Sink, batchFrozenSink);
        var singleFrozen = TFrozen.Start();
        foreach (var t in batch)
            singleFrozen = TFrozen.Forward(in singleFrozen, ref singleFrozenSink, t);
        RequireStep("batch singles", batchFrozen, singleFrozen);
        RequireSink("batch singles", batchFrozenSink, singleFrozenSink);
    }

    EdgeVerification.Run();

    Console.WriteLine("Playback verified: flags, jumps, mirrors, loops, rewind, clip hooks, runtime timelines, index registry.");
    Console.WriteLine("Dispatch receipts match; generated, constrained, ref-data, and API-shape calls mutate the original; boxing copies it.");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// Exposes one oracle consumer's accumulated FrozenSink to the generic frozen
// parity receipts above.
public interface IFrozenOracle
{
    FrozenSink Sink { get; }
}

// The PlaybackCore-side consumer for the VitalsTrack fixture: the same
// fields, the same order, the same arithmetic as the frozen emission, but
// resolved through the real tables at run time.
public struct OracleVitals :
    IForwardTracks<VitalsTrack, VitalsClip, OracleVitals>,
    IBackwardTracks<VitalsTrack, VitalsClip, OracleVitals>,
    IFrozenOracle
{
    public FrozenSink Sink { get; set; }

    public void Forward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref OracleVitals data)
    {
        var sink = data.Sink;
        foreach (var item in tracks)
            sink.Sum += item.Clip.Amount;
        sink.Flags += (uint)tracks.Status;
        if (tracks.Count > 0)
            sink.Count++;
        data.Sink = sink;
    }

    public void Backward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref OracleVitals data)
    {
        var sink = data.Sink;
        foreach (var item in tracks)
            sink.Sum -= item.Clip.Amount;
        sink.Flags -= (uint)tracks.Status;
        if (tracks.Count > 0)
            sink.Count--;
        data.Sink = sink;
    }
}

public readonly record struct Fused16Clip(float Value);

// The Movement-shaped oracle fixture: one track, 16 clips, clip i =
// [i*4, i*4+3) with value i+1, duration 63. The literals below are hand-set
// from that description -- the same description the generator specialized
// into Fused16Frozen -- and --verify cross-checks them against the raw clip
// list tick by tick before running the parity receipts.
public struct Fused16Track : ITrackTables<Fused16Track, Fused16Clip>, IBlend<Fused16Clip>
{
    private static readonly uint[] s_regionStarts =
        [0, 3, 4, 7, 8, 11, 12, 15, 16, 19, 20, 23, 24, 27, 28, 31, 32, 35, 36, 39, 40, 43, 44, 47, 48, 51, 52, 55, 56, 59, 60, 63];

    // Even regions are clip regions (one track row); odd regions are the
    // gaps; the last region is the empty sentinel at 63.
    private static readonly RegionRow[] s_regionRows =
    [
        new(0, 1), new(1, 0), new(1, 1), new(2, 0), new(2, 1), new(3, 0), new(3, 1), new(4, 0),
        new(4, 1), new(5, 0), new(5, 1), new(6, 0), new(6, 1), new(7, 0), new(7, 1), new(8, 0),
        new(8, 1), new(9, 0), new(9, 1), new(10, 0), new(10, 1), new(11, 0), new(11, 1), new(12, 0),
        new(12, 1), new(13, 0), new(13, 1), new(14, 0), new(14, 1), new(15, 0), new(15, 1), new(16, 0),
    ];

    // Clip regions start on a clip start and end on a clip end (1|4); gap
    // regions start on a clip end (2).
    private static readonly byte[] s_regionFlags =
        [5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2, 5, 2];

    private static readonly TrackRow[] s_trackRows =
    [
        new(0, 0, 1), new(0, 1, 1), new(0, 2, 1), new(0, 3, 1), new(0, 4, 1), new(0, 5, 1), new(0, 6, 1), new(0, 7, 1),
        new(0, 8, 1), new(0, 9, 1), new(0, 10, 1), new(0, 11, 1), new(0, 12, 1), new(0, 13, 1), new(0, 14, 1), new(0, 15, 1),
    ];

    private static readonly ClipRow[] s_clipRows =
    [
        new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0), new(4, 0, 0), new(5, 0, 0), new(6, 0, 0), new(7, 0, 0),
        new(8, 0, 0), new(9, 0, 0), new(10, 0, 0), new(11, 0, 0), new(12, 0, 0), new(13, 0, 0), new(14, 0, 0), new(15, 0, 0),
    ];

    private static readonly ClipEdge[] s_clipEdges =
    [
        new(0, 3), new(4, 7), new(8, 11), new(12, 15), new(16, 19), new(20, 23), new(24, 27), new(28, 31),
        new(32, 35), new(36, 39), new(40, 43), new(44, 47), new(48, 51), new(52, 55), new(56, 59), new(60, 63),
    ];

    private static readonly Fused16Track[] s_trackData = [new()];

    private static readonly Fused16Clip[] s_clipData =
        [new(1f), new(2f), new(3f), new(4f), new(5f), new(6f), new(7f), new(8f), new(9f), new(10f), new(11f), new(12f), new(13f), new(14f), new(15f), new(16f)];

    public static ReadOnlySpan<uint> RegionStarts => s_regionStarts;
    public static ReadOnlySpan<RegionRow> RegionRows => s_regionRows;
    public static ReadOnlySpan<byte> RegionFlags => s_regionFlags;
    public static ReadOnlySpan<TrackRow> TrackRows => s_trackRows;
    public static ReadOnlySpan<ClipRow> ClipRows => s_clipRows;
    public static ReadOnlySpan<ClipEdge> ClipEdges => s_clipEdges;
    public static ReadOnlySpan<Fused16Track> TrackData => s_trackData;
    public static ReadOnlySpan<Fused16Clip> ClipData => s_clipData;
    public static int MaxActiveTracks => 1;
    public static bool Loops => false;

    public void Blend(in Fused16Clip first, in Fused16Clip second, float factor, out Fused16Clip result)
        => result = new Fused16Clip(first.Value * (1f - factor) + second.Value * factor);
}

public struct OracleFused16 :
    IForwardTracks<Fused16Track, Fused16Clip, OracleFused16>,
    IBackwardTracks<Fused16Track, Fused16Clip, OracleFused16>,
    IFrozenOracle
{
    public FrozenSink Sink { get; set; }

    public void Forward(uint tick, in Tracks<Fused16Track, Fused16Clip> tracks, ref OracleFused16 data)
    {
        var sink = data.Sink;
        foreach (var item in tracks)
            sink.Sum += item.Clip.Value;
        sink.Flags += (uint)tracks.Status;
        if (tracks.Count > 0)
            sink.Count++;
        data.Sink = sink;
    }

    public void Backward(uint tick, in Tracks<Fused16Track, Fused16Clip> tracks, ref OracleFused16 data)
    {
        var sink = data.Sink;
        foreach (var item in tracks)
            sink.Sum -= item.Clip.Value;
        sink.Flags -= (uint)tracks.Status;
        if (tracks.Count > 0)
            sink.Count--;
        data.Sink = sink;
    }
}

// Two distinct consumer data types for one (VitalsTrack, VitalsClip)
// timeline: the shared-index receipt above plays both through the SAME
// Build'd index, each accumulating in its own units.
public struct SharedLeft :
    IForwardTracks<VitalsTrack, VitalsClip, SharedLeft>,
    IBackwardTracks<VitalsTrack, VitalsClip, SharedLeft>
{
    public float Sum;
    public int Seen;
    public int Rewinds;

    public void Forward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref SharedLeft data)
    {
        foreach (var item in tracks)
            data.Sum += item.Clip.Amount;
        data.Seen++;
    }

    public void Backward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref SharedLeft data)
    {
        foreach (var item in tracks)
            data.Sum -= item.Clip.Amount;
        data.Rewinds++;
    }
}

public struct SharedRight :
    IForwardTracks<VitalsTrack, VitalsClip, SharedRight>,
    IBackwardTracks<VitalsTrack, VitalsClip, SharedRight>
{
    public long Ticks;
    public int Seen;
    public int Rewinds;

    public void Forward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref SharedRight data)
    {
        foreach (var item in tracks)
            data.Ticks += item.Track.Offset + tick;
        data.Seen++;
    }

    public void Backward(uint tick, in Tracks<VitalsTrack, VitalsClip> tracks, ref SharedRight data)
    {
        foreach (var item in tracks)
            data.Ticks -= item.Track.Offset + tick;
        data.Rewinds++;
    }
}
