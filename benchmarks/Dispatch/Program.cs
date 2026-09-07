using BenchmarkDotNet.Running;
using Tl.Hooks;

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

    var timelineA = new Timeline<VitalsTrack, VitalsClip, Vitals>();
    var timelineB = new Timeline<VitalsTrack, VitalsClip, Vitals>();
    if (timelineB.Index != timelineA.Index + 1)
        throw new InvalidOperationException("Timeline indices are not sequential.");
    Console.WriteLine($"timelineA.Index = {timelineA.Index}, timelineB.Index = {timelineB.Index} (per closed generic type).");

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
    GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(ref vitals, 10, 20, 30);
    var authored = new Vitals { Health = 100_000f };
    instance.Forward(ref authored, 10, 20, 30);
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
        pw = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in pw, ref walk, t);
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
    spot = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in spot, ref walk, 0);
    if (spot.Flags != (PlaybackFlags.First | PlaybackFlags.Active))
        throw new InvalidOperationException($"Tick 0 should be First|Active, got {spot.Flags}.");

    spot = Playback.Start(14);
    spot = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in spot, ref walk, 15);
    if (spot.Flags != PlaybackFlags.None)
        throw new InvalidOperationException($"Gap tick 15 should carry no flags, got {spot.Flags}.");

    spot = Playback.Start();
    spot = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in spot, ref walk, 10);
    if (spot.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Jump to 10 should carry Enter|Exit|Active|Last, got {spot.Flags}.");

    var repeat = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in spot, ref walk, 10);
    if (repeat.Flags != (PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Repeated tick should keep positional flags only, got {repeat.Flags}.");

    var back = Playback.Start(10);
    back = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Backward(in back, ref walk, 6);
    if (back.Flags != (PlaybackFlags.Enter | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Backward 10->6 should carry Enter|Active|Last, got {back.Flags}.");

    back = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Backward(in back, ref walk, 2);
    if (back.Flags != (PlaybackFlags.Exit | PlaybackFlags.Active))
        throw new InvalidOperationException($"Backward 6->2 should carry Exit|Active, got {back.Flags}.");

    // Forward then rewind: exact counters restored, floats within tolerance.
    var rt = new Vitals { Health = 100_000f };
    var pf = Playback.Start();
    for (uint t = 0; t < 515; t++)
        pf = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in pf, ref rt, t);
    var pb = pf;
    for (int t = 514; t >= 0; t--)
        pb = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Backward(in pb, ref rt, (uint)t);
    if (Math.Abs(rt.Health - 100_000f) > 1f || rt.Ticks != 0 || rt.Count != 515 || rt.Back != 515)
        throw new InvalidOperationException($"Rewind did not restore state: {rt.Result}, back {rt.Back}.");

    uint bEnters = 0, bExits = 0;
    var bw = new Vitals();
    var pb2 = Playback.Start(514);
    for (int t = 513; t >= 0; t--)
    {
        pb2 = GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Backward(in pb2, ref bw, (uint)t);
        if (pb2.Has(PlaybackFlags.Enter)) bEnters++;
        if (pb2.Has(PlaybackFlags.Exit)) bExits++;
    }

    if ((bEnters, bExits) != (7, 7))
        throw new InvalidOperationException($"Backward walk flag counts wrong: {bEnters}/{bExits}.");

    // Looping: wraps move Cycles instead of Complete; forward wraps count
    // exactly, a full-cycle jump sets Enter|Exit, backward wraps saturate.
    var lv = new Vitals();
    var lp = Playback.Start();
    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Forward(in lp, ref lv, 685);
    if (lp.Tick != 685 || lp.Cycles != 1 || lp.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active))
        throw new InvalidOperationException($"Loop wrap to 685 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Forward(in lp, ref lv, 1205);
    if (lp.Tick != 1205 || lp.Cycles != 2 || !lp.Has(PlaybackFlags.Enter) || !lp.Has(PlaybackFlags.Active))
        throw new InvalidOperationException($"Second wrap to 1205 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    var multi = Playback.Start();
    multi = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Forward(in multi, ref lv, 1205);
    if (multi.Cycles != 2 || !multi.Has(PlaybackFlags.Enter) || !multi.Has(PlaybackFlags.Exit) || !multi.Has(PlaybackFlags.Active))
        throw new InvalidOperationException($"Two-cycle jump should be full coverage, got {multi.Cycles}, {multi.Flags}.");

    var wrap = Playback.Start(0);
    wrap = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Backward(in wrap, ref lv, 599);
    if (wrap.Cycles != 0
        || wrap.Flags != (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.Active | PlaybackFlags.Last))
        throw new InvalidOperationException($"Backward wrap past zero wrong: {wrap.Cycles}, {wrap.Flags}.");

    var lr = new Vitals();
    var pl = Playback.Start();
    for (uint t = 0; t <= 600; t++)
        pl = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Forward(in pl, ref lr, t);
    if (pl.Cycles != 1)
        throw new InvalidOperationException($"Looping walk should report one cycle, got {pl.Cycles}.");
    var plb = pl;
    for (int t = 600; t >= 0; t--)
        plb = GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Backward(in plb, ref lr, (uint)t);
    if (plb.Cycles != 0 || Math.Abs(lr.Health) > 1f || lr.Ticks != 0 || lr.Count != 601 || lr.Back != 601)
        throw new InvalidOperationException($"Looping rewind did not restore: cycles {plb.Cycles}, {lr.Result}, back {lr.Back}.");

    // Clip-level hooks: one call per active clip, exact inverse on backward.
    var cv = new Vitals { Health = 100_000f };
    var pc = Playback.Start();
    for (uint t = 0; t < 515; t++)
        pc = ClipTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in pc, ref cv, t);
    if (cv.Count != 965)
        throw new InvalidOperationException($"Clip hooks fired {cv.Count} times, expected 965.");
    var pcb = pc;
    for (int t = 514; t >= 0; t--)
        pcb = ClipTimeline<VitalsTrack, VitalsClip, Vitals>.Backward(in pcb, ref cv, (uint)t);
    if (cv.Back != 965 || Math.Abs(cv.Health - 100_000f) > 1f || cv.Ticks != 0)
        throw new InvalidOperationException($"Clip-level rewind did not restore: back {cv.Back}, health {cv.Health}, ticks {cv.Ticks}.");

    // Runtime instances share the same Playback core.
    var runtime = shape.BuildTimeline();
    var rv = new Vitals { Health = 100_000f };
    var rp = Playback.Start();
    for (uint t = 0; t < 600; t++)
        rp = runtime.Forward(in rp, ref rv, t);
    if (!rp.Has(PlaybackFlags.Complete) || rp.Tick != 599)
        throw new InvalidOperationException($"Runtime walk should complete at 599: {rp.Tick}, {rp.Flags}.");

    runtime.IsLooping = true;
    var loopStart = Playback.Start();
    var rp2 = runtime.Forward(in loopStart, ref rv, 700);
    if (rp2.Cycles != 1 || rp2.Tick != 700)
        throw new InvalidOperationException($"Runtime loop wrap wrong: {rp2.Tick}, {rp2.Cycles}.");

    EdgeVerification.Run();

    Console.WriteLine("Playback verified: flags, jumps, mirrors, loops, rewind, clip hooks, runtime instances.");
    Console.WriteLine("Dispatch receipts match; generated, constrained, ref-data, and API-shape calls mutate the original; boxing copies it.");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
