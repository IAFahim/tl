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
    void Reject<TException>(Action action, string what) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}: {what}.");
    }

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

    // ---- The global hub: one index space across closures, function-pointer
    // dispatch, lifecycle. Runs FIRST so the sequential-index receipt can
    // name absolute indices: the Heal pair gets 0, the Vitals pair gets 1.
    var heal = Timeline<HealTrack, HealClip>.Build(b =>
    {
        var track = b.Track(new HealTrack(4));
        b.Clip(track, new HealClip(7f), 2, 8);
    });
    var vitalsPair = Timeline<VitalsTrack, VitalsClip>.Build(b =>
    {
        var track = b.Track(new VitalsTrack(2));
        b.Clip(track, new VitalsClip(5f), 0, 10);
    });
    if (heal != 0 || vitalsPair != 1)
        throw new InvalidOperationException($"The global registry must assign sequential indices across closures: Heal pair {heal}, Vitals pair {vitalsPair}.");
    Console.WriteLine($"Global registry: Heal pair index {heal}, Vitals pair index {vitalsPair} — one ushort index space across different (TTrack, TClip) closures.");

    if (!Timeline.IsValid(heal) || Timeline.IsValid(Timeline.None) || Timeline.Duration(heal) != 8 || Timeline.IsLooping(heal))
        throw new InvalidOperationException("IsValid/Duration/IsLooping diverged from the authored Heal pair.");
    if (Timeline.Duration(vitalsPair) != 10)
        throw new InvalidOperationException($"Duration of the Vitals pair should be 10, got {Timeline.Duration(vitalsPair)}.");

    // Pointer dispatch mutates the caller's data — the ref rides through as
    // a stack-pinned void*, so there is no boxing copy. The same walk pins
    // the Enter/Stay/Exit sequence of one clip: cross the start (Enter),
    // hold (Stay), land on the last active frame (Exit, positional).
    var patient = new HealData();
    var hp = Timeline.Start(heal);
    hp = Timeline.Forward(heal, in hp, default(NoInput), ref patient, 5);
    if (patient is not { Enters: 1, Stays: 0, Exits: 0, Calls: 1 } || hp.Tick != 5 || !hp.Has(PlaybackFlags.Started))
        throw new InvalidOperationException($"Pointer dispatch must mutate the caller and enter through the start edge: {patient.Enters}/{patient.Stays}/{patient.Exits}/{patient.Calls}.");
    hp = Timeline.Forward(heal, in hp, default(NoInput), ref patient, 6);
    if (patient is not { Enters: 1, Stays: 1, Exits: 0, Calls: 2 })
        throw new InvalidOperationException($"A held frame must Stay: {patient.Stays}.");
    hp = Timeline.Forward(heal, in hp, default(NoInput), ref patient, 7);
    if (patient is not { Enters: 1, Stays: 1, Exits: 1, Calls: 3 })
        throw new InvalidOperationException($"Tick 7 is the clip's last active frame: Exit must be positional: {patient.Exits}.");
    Console.WriteLine($"Hub pointer dispatch: Enter/Stay/Exit = {patient.Enters}/{patient.Stays}/{patient.Exits}, caller mutated in place (no boxing copy).");

    // One index, two consumer types: the per-(entry, TData) binding cache
    // serves both, and playing one leaves the other's instance untouched.
    var left = new SharedLeft();
    var right = new SharedRight();
    var sharedState = Timeline.Start(vitalsPair);
    sharedState = Timeline.Forward(vitalsPair, in sharedState, default(NoInput), ref left, 4);
    sharedState = Timeline.Forward(vitalsPair, in sharedState, default(NoInput), ref right, 5);
    if (left is not { Sum: 5f, Seen: 1, Rewinds: 0 } || right is not { Ticks: 7L, Seen: 1, Rewinds: 0 })
        throw new InvalidOperationException($"Shared index receipt mismatch: left {left.Sum}/{left.Seen}, right {right.Ticks}/{right.Seen}.");

    sharedState = Timeline.Forward(vitalsPair, in sharedState, default(NoInput), ref left, 6);
    sharedState = Timeline.Backward(vitalsPair, in sharedState, default(NoInput), ref right, 4);
    if (left is not { Sum: 10f, Seen: 2, Rewinds: 0 } || right is not { Ticks: 1L, Seen: 1, Rewinds: 1 })
        throw new InvalidOperationException($"Playing one data type must leave the other untouched: left {left.Sum}/{left.Seen}, right {right.Ticks}/{right.Seen}/{right.Rewinds}.");
    Console.WriteLine($"Index {vitalsPair} served two data types from one Build: left {left.Sum:R}/{left.Seen}, right {right.Ticks}/{right.Rewinds}.");

    // Lifecycle: default(Playback) is uninitialized; Stop mints Stopped;
    // stopped and unstarted playbacks reject before any callback.
    var guard = new HealData();
    Reject<InvalidOperationException>(() => Timeline.Forward(heal, default, default(NoInput), ref guard, 5), "unstarted forward");
    Reject<InvalidOperationException>(() => Timeline.Backward(heal, default, default(NoInput), ref guard, 5), "unstarted backward");
    var guardVitals = new Vitals();
    Reject<InvalidOperationException>(() => GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(default, default(VitalsInput), ref guardVitals, 5), "unstarted generated forward");
    if (guardVitals.Count != 0)
        throw new InvalidOperationException("A rejected generated playback must not run callbacks.");
    Reject<InvalidOperationException>(() => Timeline.Stop(heal, default), "stop without start");
    if (guard.Calls != 0)
        throw new InvalidOperationException("A rejected playback must not run callbacks.");

    var running = Timeline.Forward(heal, in hp, default(NoInput), ref patient, 6);
    var before = patient;
    var stopped = Timeline.Stop(heal, in running);
    if (!stopped.Has(PlaybackFlags.Started) || !stopped.Has(PlaybackFlags.Stopped) || stopped.Tick != running.Tick || stopped.Cycles != running.Cycles)
        throw new InvalidOperationException($"Stop must return pb | Stopped with no callbacks: {stopped.Tick}/{stopped.Cycles}/{stopped.Flags}.");
    if (patient.Calls != before.Calls)
        throw new InvalidOperationException("Stop must not run callbacks.");
    Reject<InvalidOperationException>(() => Timeline.Forward(heal, in stopped, default(NoInput), ref patient, 6), "forward on stopped");
    Reject<InvalidOperationException>(() => Timeline.Backward(heal, in stopped, default(NoInput), ref patient, 5), "backward on stopped");
    if (patient.Calls != before.Calls)
        throw new InvalidOperationException("A stopped playback must not run callbacks.");

    var fresh = Timeline.Start(heal, 10);
    if (fresh.Flags != PlaybackFlags.Started || fresh.Tick != 10)
        throw new InvalidOperationException($"A new Start must clear the lifecycle bits fresh: {fresh.Tick}/{fresh.Flags}.");
    Console.WriteLine("Lifecycle verified: unstarted/stopped throw before callbacks, Stop is callback-free, Start mints fresh Started.");

    // Hub receipts equal the per-closure direct calls exactly: the runtime
    // Built fixture (the full VitalsTrack authoring) against the compiled
    // tables, per step and in one batch.
    var shape = new ApiShape();
    var instance = shape.BuildTimeline();
    uint random = 0x85EBCA6Bu;
    var hubTicks = new uint[128];
    for (var i = 0; i < hubTicks.Length; i++)
    {
        random ^= random << 13;
        random ^= random >> 17;
        random ^= random << 5;
        hubTicks[i] = i % 5 == 0 ? random % 700 : (uint)(i * 3) % 700;
    }

    var hubIn = new VitalsInput(100_000f);
    var hubData = hubIn.Seed();
    var directWalk = hubIn.Seed();
    var hubPb = Timeline.Start(instance);
    var directPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
    foreach (var tick in hubTicks)
    {
        hubPb = Timeline.Forward(instance, in hubPb, in hubIn, ref hubData, tick);
        directPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in directPb, in hubIn, ref directWalk, tick);
        if (hubPb.Tick != directPb.Tick || hubPb.Cycles != directPb.Cycles || hubPb.Flags != directPb.Flags)
            throw new InvalidOperationException($"Hub/direct Playback diverged at {tick}: {hubPb.Tick}/{hubPb.Cycles}/{hubPb.Flags} vs {directPb.Tick}/{directPb.Cycles}/{directPb.Flags}.");
    }
    if (hubData.Result != directWalk.Result)
        throw new InvalidOperationException($"Hub/direct receipts diverged: {hubData.Result} vs {directWalk.Result}.");

    var hubBatch = hubIn.Seed();
    var directBatch = hubIn.Seed();
    var hubBatchPb = Timeline.Forward(instance, Timeline.Start(instance), in hubIn, ref hubBatch, hubTicks);
    var directBatchPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(GeneratedTimeline<VitalsTrack, VitalsClip>.Start(), in hubIn, ref directBatch, hubTicks);
    if (hubBatchPb.Tick != directBatchPb.Tick || hubBatchPb.Cycles != directBatchPb.Cycles || hubBatchPb.Flags != directBatchPb.Flags || hubBatch.Result != directBatch.Result)
        throw new InvalidOperationException($"Hub/direct batch diverged: {hubBatchPb.Tick}/{hubBatch.Result} vs {directBatchPb.Tick}/{directBatch.Result}.");
    Console.WriteLine($"Hub dispatch equals the per-closure direct call: 128-step walk and one batch, Playback and receipts identical ({hubData.Result}).");

    // Stale, tombstoned, and None indices reject before any callback.
    var doomed = Timeline<HealTrack, HealClip>.Build(b =>
    {
        var track = b.Track(new HealTrack(1));
        b.Clip(track, new HealClip(1f), 0, 4);
    });
    var doomedPb = Timeline.Start(doomed);
    Timeline.Destroy(doomed);
    if (Timeline.IsValid(doomed))
        throw new InvalidOperationException("A destroyed index must not be valid.");

    var witness = new HealData();
    Reject<ArgumentOutOfRangeException>(() => Timeline.Start(doomed), "Start on destroyed");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Forward(doomed, in doomedPb, default(NoInput), ref witness, 1), "Forward on destroyed");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Backward(doomed, in doomedPb, default(NoInput), ref witness, 1), "Backward on destroyed");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Forward(doomed, default(NoInput), ref witness, 1), "stateless Forward on destroyed");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Start(Timeline.None), "Start on None");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Forward(Timeline.None, in doomedPb, default(NoInput), ref witness, 1), "Forward on None");
    Reject<ArgumentOutOfRangeException>(() => Timeline.Backward(Timeline.None, in doomedPb, default(NoInput), ref witness, 1), "Backward on None");
    if (witness.Calls != 0)
        throw new InvalidOperationException("A rejected index must not run callbacks.");

    // The GUI receipt: one non-static lambda Build and one TSource Build
    // produce identical tables — same playback results from both overloads.
    float amount = 9f;
    var viaLambda = Timeline<HealTrack, HealClip>.Build(b =>
    {
        var track = b.Track(new HealTrack(4));
        b.Clip(track, new HealClip(amount), 2, 8);
    });
    var viaSource = Timeline<HealTrack, HealClip>.Build(
        new HealSource { Amount = amount, Offset = 4 },
        static (b, source) =>
        {
            var track = b.Track(new HealTrack(source.Offset));
            b.Clip(track, new HealClip(source.Amount), 2, 8);
        });
    if (Timeline.Duration(viaLambda) != Timeline.Duration(viaSource) || Timeline.IsLooping(viaLambda) != Timeline.IsLooping(viaSource))
        throw new InvalidOperationException("The lambda and TSource overloads diverged in Duration/IsLooping.");

    Span<uint> guiWalk = [2, 3, 4, 5, 6, 7, 8];
    var lambdaData = new HealData();
    var sourceData = new HealData();
    var lambdaPb = Timeline.Start(viaLambda);
    var sourcePb = Timeline.Start(viaSource);
    foreach (var tick in guiWalk)
    {
        lambdaPb = Timeline.Forward(viaLambda, in lambdaPb, default(NoInput), ref lambdaData, tick);
        sourcePb = Timeline.Forward(viaSource, in sourcePb, default(NoInput), ref sourceData, tick);
        if (lambdaPb.Tick != sourcePb.Tick || lambdaPb.Flags != sourcePb.Flags || lambdaPb.Cycles != sourcePb.Cycles)
            throw new InvalidOperationException($"Lambda/TSource playback diverged at {tick}.");
    }
    if (lambdaData is not { Enters: 1, Stays: 4, Exits: 1, Calls: 6 } || sourceData.Enters != lambdaData.Enters || sourceData.Stays != lambdaData.Stays || sourceData.Exits != lambdaData.Exits)
        throw new InvalidOperationException($"Lambda/TSource tables diverged: {lambdaData.Enters}/{lambdaData.Stays}/{lambdaData.Exits} vs {sourceData.Enters}/{sourceData.Stays}/{sourceData.Exits}.");
    Console.WriteLine($"GUI receipt: lambda Build and TSource Build produced identical tables ({lambdaData.Enters}/{lambdaData.Stays}/{lambdaData.Exits} over the clip).");

    // ---- API-shape receipts: every arm against the two hand-rolled
    // oracles (stateless sampling, and the stateful movement mirror).
    shape.Setup();
    var stateless = shape.DirectTicks();
    foreach (var actual in new[] { shape.ShellSingle(), shape.ShellParamsFour(), shape.InstanceSingle(), shape.InstanceParamsFour(), shape.RunInstance(instance) })
        if (actual != stateless)
            throw new InvalidOperationException($"Stateless API shape receipt mismatch: {stateless} / {actual}.");

    var stateful = shape.StatefulOracle();
    foreach (var actual in new[] { shape.PlaybackSingle(), shape.PlaybackParamsFour(), shape.HubDispatch() })
        if (actual != stateful)
            throw new InvalidOperationException($"Stateful API shape receipt mismatch: {stateful} / {actual}.");
    Console.WriteLine($"API shape receipts match on the new surface: {stateless} stateless, {stateful} stateful.");

    // ---- Playback: packing, walks with oracle-derived sums, rewind.
    var packed = new Playback(5, 7, PlaybackFlags.Started | PlaybackFlags.Completed);
    if (packed.Tick != 5 || packed.Cycles != 7 || !packed.Has(PlaybackFlags.Started)
        || !packed.Has(PlaybackFlags.Completed) || packed.Has(PlaybackFlags.Stopped))
        throw new InvalidOperationException("Playback packing round-trip failed.");

    var walkIn = new VitalsInput(100_000f);
    var walk = walkIn.Seed();
    var pw = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
    uint completed = 0;
    for (uint t = 0; t < 515; t++)
    {
        pw = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pw, in walkIn, ref walk, t);
        if (pw.Has(PlaybackFlags.Completed)) completed++;
    }

    var (forwardSum, forwardTicks, forwardCount) = StayWalk(0, 515, backward: false);
    if (completed != 0 || pw.Tick != 514 || pw.Cycles != 0 || pw.Flags != PlaybackFlags.Started)
        throw new InvalidOperationException($"Forward walk Playback wrong: {pw.Tick}/{pw.Cycles}/{pw.Flags}, completed {completed}.");
    if (walk.Count != forwardCount || walk.Ticks != forwardTicks)
        throw new InvalidOperationException($"Forward walk counters diverged from the oracle: {walk.Count}/{walk.Ticks} vs {forwardCount}/{forwardTicks}.");
    if (Math.Abs(walk.Health - (100_000f + forwardSum)) > 0.01f)
        throw new InvalidOperationException($"Forward walk sum diverged from the oracle: {walk.Health:R} vs {100_000f + forwardSum:R}.");
    Console.WriteLine($"Forward walk 0..514: {forwardCount} active ticks (of 515), Stay-only Ticks {forwardTicks}, sum matched the authored-clip oracle.");

    // Forward then rewind: the counters land on the oracle-derived values.
    // Boundary frames do not accumulate (pinned), so the silent start of a
    // clip that begins at tick 0 counts Stay forward but Exit backward —
    // the residual is exactly that clip's offset, derived here, not assumed.
    var rewindIn = new VitalsInput(100_000f);
    var rt = rewindIn.Seed();
    var pf = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
    for (uint t = 0; t < 515; t++)
        pf = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pf, in rewindIn, ref rt, t);
    var pb = pf;
    for (int t = 514; t >= 0; t--)
        pb = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pb, in rewindIn, ref rt, (uint)t);

    var (rewindSum, rewindTicks, rewindCount) = StayWalk(515, 0, backward: true);
    if (rt.Count != forwardCount || rt.Back != rewindCount)
        throw new InvalidOperationException($"Rewind tick counts diverged: count {rt.Count}, back {rt.Back} vs {forwardCount}/{rewindCount}.");
    if (rt.Ticks != forwardTicks - rewindTicks)
        throw new InvalidOperationException($"Rewind Ticks diverged from the oracle: {rt.Ticks} vs {forwardTicks} - {rewindTicks}.");
    if (forwardTicks == rewindTicks)
        throw new InvalidOperationException("The pinned asymmetry disappeared: the silent-start frame must be Stay forward and Exit backward.");
    if (Math.Abs(rt.Health - (100_000f + forwardSum - rewindSum)) > 0.01f)
        throw new InvalidOperationException($"Rewind health diverged from the oracle: {rt.Health:R}.");
    Console.WriteLine($"Rewind verified: Ticks land on {rt.Ticks} (boundary frames do not accumulate: the silent start adds, the repeated final tick subtracts), health restored to {rt.Health:R}, counts {rt.Count}/{rt.Back}.");

    // Looping: wraps move Cycles instead of Completed; LastLoopFrame fires
    // when the destination lands on the loop's last local frame; backward
    // wraps saturate Cycles at zero.
    var loopIn = default(VitalsInput);
    var lv = loopIn.Seed();
    var lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start();
    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lp, in loopIn, ref lv, 685);
    if (lp.Tick != 685 || lp.Cycles != 1 || lp.Flags != PlaybackFlags.Started)
        throw new InvalidOperationException($"Loop wrap to 685 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    lp = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lp, in loopIn, ref lv, 1205);
    if (lp.Tick != 1205 || lp.Cycles != 2 || !lp.Has(PlaybackFlags.Started))
        throw new InvalidOperationException($"Second wrap to 1205 wrong: {lp.Tick}, {lp.Cycles}, {lp.Flags}.");

    var multi = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start();
    multi = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in multi, in loopIn, ref lv, 1205);
    if (multi.Cycles != 2 || !multi.Has(PlaybackFlags.Started))
        throw new InvalidOperationException($"Two-cycle jump should count both wraps: {multi.Cycles}, {multi.Flags}.");

    var lastFrame = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start(598);
    lastFrame = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lastFrame, in loopIn, ref lv, 599);
    if (!lastFrame.Has(PlaybackFlags.LastLoopFrame) || lastFrame.Has(PlaybackFlags.Completed))
        throw new InvalidOperationException($"Landing on local frame 599 must set LastLoopFrame: {lastFrame.Flags}.");

    var wrap = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start(0);
    wrap = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Backward(in wrap, in loopIn, ref lv, 599);
    if (wrap.Cycles != 0 || wrap.Flags != (PlaybackFlags.Started | PlaybackFlags.LastLoopFrame))
        throw new InvalidOperationException($"Backward wrap past zero wrong: {wrap.Cycles}, {wrap.Flags}.");

    var lr = loopIn.Seed();
    var pl = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start();
    for (uint t = 0; t < 600; t++)
        pl = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in pl, in loopIn, ref lr, t);
    if (pl.Cycles != 0 || !pl.Has(PlaybackFlags.LastLoopFrame))
        throw new InvalidOperationException($"Looping walk should end on the last local frame: {pl.Cycles}, {pl.Flags}.");
    var plb = pl;
    for (int t = 599; t >= 0; t--)
        plb = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Backward(in plb, in loopIn, ref lr, (uint)t);
    if (plb.Cycles != 0 || lr.Count != lr.Back)
        throw new InvalidOperationException($"Looping rewind diverged: cycles {plb.Cycles}, count {lr.Count}, back {lr.Back}.");
    Console.WriteLine("Loops verified: Cycles count wraps exactly, LastLoopFrame is positional, backward saturation holds.");

    // Runtime indices share the same Playback core; looping is authored.
    var runtimeIn = new VitalsInput(100_000f);
    var rv = runtimeIn.Seed();
    var rp = Timeline.Start(instance);
    for (uint t = 0; t < 600; t++)
        rp = Timeline.Forward(instance, in rp, in runtimeIn, ref rv, t);
    if (!rp.Has(PlaybackFlags.Completed) || rp.Tick != 599)
        throw new InvalidOperationException($"Runtime walk should complete at 599: {rp.Tick}, {rp.Flags}.");

    var runtimeLoop = shape.BuildTimeline(loops: true);
    var loopStart = Timeline.Start(runtimeLoop);
    var rp2 = Timeline.Forward(runtimeLoop, in loopStart, in runtimeIn, ref rv, 700);
    if (rp2.Cycles != 1 || rp2.Tick != 700)
        throw new InvalidOperationException($"Runtime loop wrap wrong: {rp2.Tick}, {rp2.Cycles}.");

    // Index-registry receipts: sequential assignment, tombstones, no reuse.
    var destroyed = ProbeTimeline.Build(static b => { });
    var live = ProbeTimeline.Build(static b =>
    {
        var track = b.Track(new EdgeVerification.ProbeTrack(0));
        b.Clip(track, new EdgeVerification.ProbeClip(10), 0, 10);
    });
    if (live != destroyed + 1)
        throw new InvalidOperationException($"Sequential global assignment expected {destroyed + 1}, got {live}.");

    Timeline.Destroy(destroyed);
    Reject<ArgumentOutOfRangeException>(() => Timeline.Start(destroyed), "Start on a destroyed index");

    var probeData = new EdgeVerification.Probe();
    var probeState = Timeline.Start(live);
    probeState = Timeline.Forward(live, in probeState, default(NoInput), ref probeData, 5);
    if (probeData.Tracks != 1 || probeData.Sum != 10f || !probeState.Has(PlaybackFlags.Started))
        throw new InvalidOperationException($"A live index stopped playing after a destroy: tracks {probeData.Tracks}, sum {probeData.Sum}.");

    var rebuilt = ProbeTimeline.Build(static b => { });
    if (rebuilt == destroyed || rebuilt != live + 1)
        throw new InvalidOperationException($"Build reused a destroyed index: destroyed {destroyed}, live {live}, rebuilt {rebuilt}.");

    // Empty definitions stay valid: empty tables, duration 0, Completed, and
    // NO callbacks — the empty-tick rule.
    var emptyIndex = Timeline<VitalsTrack, VitalsClip>.Build(static b => { });
    var emptyState = Timeline.Start(emptyIndex);
    var emptyIn = default(VitalsInput);
    var emptyData = emptyIn.Seed();
    emptyState = Timeline.Forward(emptyIndex, in emptyState, in emptyIn, ref emptyData, 0);
    if (!emptyState.Has(PlaybackFlags.Completed) || emptyData.Count != 0 || emptyData.Ticks != 0)
        throw new InvalidOperationException($"Empty definition mismatch: {emptyState.Flags}, {emptyData.Result}.");
    Console.WriteLine($"Index registry verified: sequential builds {destroyed}/{live}/{rebuilt}, tombstones reject playback, no reuse, empty definitions fire no callbacks.");

    // Frozen path parity: per-timeline code specialized at emission time
    // (Generated/VitalsFrozen.g.cs, Generated/Fused16Frozen.g.cs), receipt-
    // matched here against the PlaybackCore oracle through the real tables.
    // Accumulation contract, mirrored bit-for-bit in this order by both
    // sides: per tick WITH at least one active track, in track-row order —
    // Sum += one blend-resolved clip value per active track, Flags += the
    // work's ClipState code (Enter=0/Stay=1/Exit=2), then Count++; empty
    // ticks do no sink work; Backward subtracts in the same order.
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
    Console.WriteLine("Frozen parity verified: walks, jumps, mirrors, batches, lifecycle flags (Vitals + Fused16).");

    // Frozen hub receipts (Generated/FrozenHub.g.cs): the dense <= 256
    // dispatch hub over the frozen registry must be a transparent
    // forwarder. Index space: VitalsFrozen = 0, Fused16Frozen = 1.
    VerifyHubTimeline<VitalsFrozen>(0);
    VerifyHubTimeline<Fused16Frozen>(1);
    Console.WriteLine($"Frozen hub verified: {FrozenHub.Count} timelines dispatched identically to direct calls; out-of-range rejected.");

    // The authored-clip oracle the walk receipts above used: Stay-only
    // accumulation derived from the authored clip list, never the CSR
    // tables, with the exact blend arithmetic the engine evaluates.
    (float Sum, long Ticks, int Count) StayWalk(uint from, uint to, bool backward)
    {
        var authored = EdgeVerification.AuthoredVitals;
        float sum = 0f;
        long ticks = 0;
        var count = 0;

        void Step(uint t, uint prev)
        {
            var any = false;
            for (var track = 0; track <= 3; track++)
            {
                var first = -1;
                var second = -1;
                for (var c = 0; c < authored.Length; c++)
                {
                    var clip = authored[c];
                    if (clip.Track != track || clip.Start > t || clip.End <= t)
                        continue;
                    if (first < 0)
                        first = c;
                    else if (second < 0)
                        second = c;
                }

                if (first < 0)
                    continue;
                any = true;

                uint start, end;
                float value;
                var a = authored[first];
                if (second < 0)
                {
                    start = a.Start;
                    end = a.End;
                    value = a.Amount;
                }
                else
                {
                    var b = authored[second];
                    start = Math.Min(a.Start, b.Start);
                    end = Math.Max(a.End, b.End);
                    var factorStart = Math.Max(a.Start, b.Start);
                    var factorEnd = Math.Min(a.End, b.End);
                    var length = factorEnd - factorStart;
                    var factor = length <= 1 ? 0.5f : (t - factorStart) / (float)(length - 1);
                    value = a.Amount * (1f - factor) + b.Amount * factor;
                }

                var exit = backward ? t == start : t == end - 1;
                var enter = backward ? prev >= end : prev < start;
                if (!exit && !enter)
                {
                    sum += value;
                    ticks += track + 1;
                }
            }

            if (any)
                count++;
        }

        if (!backward)
        {
            for (uint t = from; t < to; t++)
                Step(t, t == 0 ? 0 : t - 1);
        }
        else
        {
            // The rewind receipt steps back onto the walk's final tick
            // first — a repeated position, not a step from tick+1 — then
            // descends.
            for (var t = (int)from - 1; t >= (int)to; t--)
                Step((uint)t, t == from - 1 ? (uint)t : (uint)(t + 1));
        }

        return (sum, ticks, count);
    }

    void VerifyFrozen<TTrack, TClip, TData, TFrozen>(string name, uint duration)
        where TTrack : struct, ITrackTables<TTrack, TClip>, IBlend<TClip>
        where TClip : unmanaged
        where TData : struct, IForward<TTrack, TClip, NoInput, TData>, IBackward<TTrack, TClip, NoInput, TData>, IFrozenOracle
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
        //    duration land in the empty sentinel region and fire no callbacks.
        var oracle = new TData();
        var frozen = default(FrozenSink);
        var walkOracle = GeneratedTimeline<TTrack, TClip>.Start();
        var walkFrozen = TFrozen.Start();
        uint oCompleted = 0, oLastLoop = 0, fCompleted = 0, fLastLoop = 0;
        for (uint t = 0; t < duration + 10; t++)
        {
            walkOracle = GeneratedTimeline<TTrack, TClip>.Forward(in walkOracle, default(NoInput), ref oracle, t);
            walkFrozen = TFrozen.Forward(in walkFrozen, ref frozen, t);
            RequireStep($"walk tick {t}", walkOracle, walkFrozen);
            if (walkOracle.Has(PlaybackFlags.Completed)) oCompleted++;
            if (walkOracle.Has(PlaybackFlags.LastLoopFrame)) oLastLoop++;
            if (walkFrozen.Has(PlaybackFlags.Completed)) fCompleted++;
            if (walkFrozen.Has(PlaybackFlags.LastLoopFrame)) fLastLoop++;
        }
        RequireSink("walk sink", oracle.Sink, frozen);

        // Flag-count totals over the full walk: frozen totals against oracle
        // totals, nothing hardcoded.
        if ((oCompleted, oLastLoop) != (fCompleted, fLastLoop))
            throw new InvalidOperationException(
                $"{name} flag totals diverged: ({oCompleted}/{oLastLoop}) vs ({fCompleted}/{fLastLoop}).");

        // 2. Random jump battery: 24 deterministic xorshift [from, to] pairs,
        //    in range and beyond the duration, forward and backward - each
        //    step must agree on the Playback and the sink delta.
        uint jump = 0x6D2B79F5u;
        for (var j = 0; j < 24; j++)
        {
            jump ^= jump << 13;
            jump ^= jump >> 17;
            jump ^= jump << 5;
            var from = jump % (duration + 20);
            jump ^= jump << 13;
            jump ^= jump >> 17;
            jump ^= jump << 5;
            var to = jump % (duration + 20);

            var jumpOracleData = new TData();
            var jumpFrozenSink = default(FrozenSink);
            var jumpOracle = GeneratedTimeline<TTrack, TClip>.Start(from);
            var jumpFrozen = TFrozen.Start(from);
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Forward(in jumpOracle, default(NoInput), ref jumpOracleData, to);
            jumpFrozen = TFrozen.Forward(in jumpFrozen, ref jumpFrozenSink, to);
            RequireStep($"forward jump {from}->{to}", jumpOracle, jumpFrozen);
            RequireSink($"forward jump {from}->{to}", jumpOracleData.Sink, jumpFrozenSink);

            jumpOracleData = new TData();
            jumpFrozenSink = default;
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Start(from);
            jumpFrozen = TFrozen.Start(from);
            jumpOracle = GeneratedTimeline<TTrack, TClip>.Backward(in jumpOracle, default(NoInput), ref jumpOracleData, to);
            jumpFrozen = TFrozen.Backward(in jumpFrozen, ref jumpFrozenSink, to);
            RequireStep($"backward jump {from}->{to}", jumpOracle, jumpFrozen);
            RequireSink($"backward jump {from}->{to}", jumpOracleData.Sink, jumpFrozenSink);
        }

        // 3. Backward mirror: forward walk, then rewind tick-by-tick to 0 -
        //    the Playback is equal each step and both sinks agree at every
        //    step (the per-step equality above IS the parity claim). Count
        //    restores to exactly zero: both walks visit the same active
        //    ticks. The Sum/Flags residuals land wherever the pinned
        //    boundary rule puts them - boundary frames do not accumulate,
        //    so the codes are not symmetric in general - and are therefore
        //    only ever checked oracle-vs-frozen, never assumed to cancel.
        var mirrorOracleData = new TData();
        var mirrorFrozenSink = default(FrozenSink);
        var mirrorOracle = GeneratedTimeline<TTrack, TClip>.Start();
        var mirrorFrozen = TFrozen.Start();
        for (uint t = 0; t < duration; t++)
        {
            mirrorOracle = GeneratedTimeline<TTrack, TClip>.Forward(in mirrorOracle, default(NoInput), ref mirrorOracleData, t);
            mirrorFrozen = TFrozen.Forward(in mirrorFrozen, ref mirrorFrozenSink, t);
        }
        for (var t = (int)(duration - 1); t >= 0; t--)
        {
            mirrorOracle = GeneratedTimeline<TTrack, TClip>.Backward(in mirrorOracle, default(NoInput), ref mirrorOracleData, (uint)t);
            mirrorFrozen = TFrozen.Backward(in mirrorFrozen, ref mirrorFrozenSink, (uint)t);
            RequireStep($"mirror tick {t}", mirrorOracle, mirrorFrozen);
            RequireSink($"mirror tick {t}", mirrorOracleData.Sink, mirrorFrozenSink);
        }
        if (mirrorFrozenSink.Count != 0 || mirrorOracleData.Sink.Count != 0)
            throw new InvalidOperationException(
                $"{name} mirror ended at {mirrorFrozenSink.Sum:R}/{mirrorFrozenSink.Flags}/{mirrorFrozenSink.Count}; Count must restore to zero.");
        if (mirrorFrozenSink.Sum != mirrorOracleData.Sink.Sum || mirrorFrozenSink.Flags != mirrorOracleData.Sink.Flags)
            throw new InvalidOperationException(
                $"{name} mirror residuals diverged: {mirrorFrozenSink.Sum:R}/{mirrorFrozenSink.Flags} vs {mirrorOracleData.Sink.Sum:R}/{mirrorOracleData.Sink.Flags}.");

        // 4. Batch parity: one 8-tick call (mixed jumps, one beyond the
        //    duration) against eight single calls - equal final Playback and
        //    sink on both paths.
        Span<uint> batch = [duration / 3, 1, duration - 1, duration + 7, 0, duration / 2, 2, duration - 2];
        var batchOracleData = new TData();
        var batchFrozenSink = default(FrozenSink);
        var singleFrozenSink = default(FrozenSink);
        var batchStartOracle = GeneratedTimeline<TTrack, TClip>.Start();
        var batchStartFrozen = TFrozen.Start();
        var batchOracle = GeneratedTimeline<TTrack, TClip>.Forward(in batchStartOracle, default(NoInput), ref batchOracleData, batch);
        var batchFrozen = TFrozen.Forward(in batchStartFrozen, ref batchFrozenSink, batch);
        RequireStep("batch", batchOracle, batchFrozen);
        RequireSink("batch", batchOracleData.Sink, batchFrozenSink);
        var singleFrozen = TFrozen.Start();
        foreach (var t in batch)
            singleFrozen = TFrozen.Forward(in singleFrozen, ref singleFrozenSink, t);
        RequireStep("batch singles", batchFrozen, singleFrozen);
        RequireSink("batch singles", batchFrozenSink, singleFrozenSink);
    }

    // The hub parity receipt: for one registered index, hub Start/Forward/
    // Backward against the direct static calls on one deterministic walk -
    // same final Playback AND same sink, exact floats, per step and at the
    // end - plus out-of-range rejection before any sink work.
    void VerifyHubTimeline<TFrozen>(ushort index)
        where TFrozen : struct, IFrozen
    {
        void RequireStep(string what, in Playback direct, in Playback viaHub)
        {
            if (direct.Tick != viaHub.Tick || direct.Cycles != viaHub.Cycles || direct.Flags != viaHub.Flags)
                throw new InvalidOperationException(
                    $"hub {index} {what}: Playback diverged ({direct.Tick}/{direct.Cycles}/{direct.Flags} vs {viaHub.Tick}/{viaHub.Cycles}/{viaHub.Flags}).");
        }

        void RequireSink(string what, in FrozenSink direct, in FrozenSink viaHub)
        {
            if (direct.Sum != viaHub.Sum || direct.Flags != viaHub.Flags || direct.Count != viaHub.Count)
                throw new InvalidOperationException(
                    $"hub {index} {what}: sink diverged ({direct.Sum:R}/{direct.Flags}/{direct.Count} vs {viaHub.Sum:R}/{viaHub.Flags}/{viaHub.Count}).");
        }

        // Start receipt: several entry points, hub call vs direct call.
        foreach (var at in new uint[] { 0, 1, 9, 41, 300 })
            RequireStep($"Start({at})", TFrozen.Start(at), FrozenHub.Start(index, at));

        // One deterministic mixed walk - ascending runs plus jumps, some
        // past the duration - shared by both paths tick for tick.
        uint walkRandom = 0x85EBCA6Bu;
        var ticks = new uint[128];
        for (var i = 0; i < ticks.Length; i++)
        {
            walkRandom ^= walkRandom << 13;
            walkRandom ^= walkRandom >> 17;
            walkRandom ^= walkRandom << 5;
            ticks[i] = i % 5 == 0 ? walkRandom % 700 : (uint)(i * 3) % 700;
        }

        var directSink = default(FrozenSink);
        var hubSink = default(FrozenSink);
        var directPb = TFrozen.Start();
        var hubPb = FrozenHub.Start(index);
        foreach (var tick in ticks)
        {
            directPb = TFrozen.Forward(in directPb, ref directSink, tick);
            hubPb = FrozenHub.Forward(index, in hubPb, ref hubSink, tick);
            RequireStep($"forward tick {tick}", directPb, hubPb);
        }
        RequireSink("forward walk", directSink, hubSink);

        // Backward mirror over the same ticks reversed: hub and direct must
        // subtract identically, per step and at the end.
        for (var i = ticks.Length - 1; i >= 0; i--)
        {
            directPb = TFrozen.Backward(in directPb, ref directSink, ticks[i]);
            hubPb = FrozenHub.Backward(index, in hubPb, ref hubSink, ticks[i]);
            RequireStep($"backward tick {ticks[i]}", directPb, hubPb);
        }
        RequireSink("backward walk", directSink, hubSink);

        // Batch receipt: the whole walk in one call on both paths.
        var batchDirectSink = default(FrozenSink);
        var batchHubSink = default(FrozenSink);
        var batchDirectStart = TFrozen.Start();
        var batchHubStart = FrozenHub.Start(index);
        var batchDirect = TFrozen.Forward(in batchDirectStart, ref batchDirectSink, ticks);
        var batchHub = FrozenHub.Forward(index, in batchHubStart, ref batchHubSink, ticks);
        RequireStep("batch", batchDirect, batchHub);
        RequireSink("batch", batchDirectSink, batchHubSink);

        // Out-of-range: Count and ushort.MaxValue must throw
        // ArgumentOutOfRangeException on all three methods before touching
        // the sink - the sentinel sink proves no work happened.
        foreach (var bad in new ushort[] { checked((ushort)FrozenHub.Count), ushort.MaxValue })
        {
            var sink = new FrozenSink { Sum = 3.5f, Flags = 77, Count = -5 };
            var witness = sink;
            var from = TFrozen.Start();

            try { FrozenHub.Start(bad); throw new InvalidOperationException($"Hub.Start({bad}) must throw."); }
            catch (ArgumentOutOfRangeException) { }
            try { FrozenHub.Forward(bad, in from, ref sink, 1u); throw new InvalidOperationException($"Hub.Forward({bad}) must throw."); }
            catch (ArgumentOutOfRangeException) { }
            try { FrozenHub.Backward(bad, in from, ref sink, 1u); throw new InvalidOperationException($"Hub.Backward({bad}) must throw."); }
            catch (ArgumentOutOfRangeException) { }

            if (sink.Sum != witness.Sum || sink.Flags != witness.Flags || sink.Count != witness.Count)
                throw new InvalidOperationException($"Hub out-of-range index {bad} touched the sink before rejecting.");
        }
    }

    // ---- Input immutability: the input half of the data pair is
    // read-only context — a full forward walk and a full backward rewind
    // must leave the caller's TInput instance bit-identical (the result
    // accumulates; the input never changes through a call). Covers the
    // seed-carrying VitalsInput through both the compiled tables and the
    // global hub, and the reentrant NestedInput through the hub's scratch
    // overload (its callback plays a second timeline from inside Forward).
    {
        static bool BitIdentical<T>(in T left, in T right)
            where T : struct
        {
            Span<byte> a = stackalloc byte[System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
            Span<byte> b = stackalloc byte[System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
            System.Runtime.InteropServices.MemoryMarshal.Write(a, in left);
            System.Runtime.InteropServices.MemoryMarshal.Write(b, in right);
            return a.SequenceEqual(b);
        }

        var input = new VitalsInput(100_000f);
        var inputBefore = input;
        var inputWalk = input.Seed();
        var ip = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        for (uint t = 0; t < 600; t++)
            ip = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in ip, in input, ref inputWalk, t);
        for (var t = 599; t >= 0; t--)
            ip = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in ip, in input, ref inputWalk, (uint)t);
        if (!BitIdentical(in inputBefore, in input) || inputWalk.Count == 0 || inputWalk.Back == 0)
            throw new InvalidOperationException($"The generated walk/rewind must leave VitalsInput bit-identical (callbacks ran {inputWalk.Count}/{inputWalk.Back}).");

        var hubWalk = input.Seed();
        var hp2 = Timeline.Start(instance);
        for (uint t = 0; t < 600; t++)
            hp2 = Timeline.Forward(instance, in hp2, in input, ref hubWalk, t);
        for (var t = 599; t >= 0; t--)
            hp2 = Timeline.Backward(instance, in hp2, in input, ref hubWalk, (uint)t);
        if (!BitIdentical(in inputBefore, in input) || hubWalk.Count == 0 || hubWalk.Back == 0)
            throw new InvalidOperationException($"The hub walk/rewind must leave VitalsInput bit-identical (callbacks ran {hubWalk.Count}/{hubWalk.Back}).");

        var outer = Timeline<EdgeVerification.BigTrack, EdgeVerification.BigClip>.Build(static b =>
        {
            var track = b.Track(new EdgeVerification.BigTrack());
            b.Clip(track, new EdgeVerification.BigClip(10f), 0, 16);
            b.Clip(track, new EdgeVerification.BigClip(30f), 8, 24);
        });
        var inner = Timeline<EdgeVerification.BigTrack, EdgeVerification.BigClip>.Build(static b =>
        {
            var track = b.Track(new EdgeVerification.BigTrack());
            b.Clip(track, new EdgeVerification.BigClip(1f), 0, 24);
            b.Clip(track, new EdgeVerification.BigClip(3f), 8, 24);
        });
        var nestedInput = new EdgeVerification.NestedInput { Inner = inner };
        var nestedBefore = nestedInput;
        var nested = new EdgeVerification.NestedData();
        var np = new Playback(0, 0, PlaybackFlags.Started);
        var nestedBuffer = new EdgeVerification.BigClip[1];
        var nestedPeak = 0;
        for (uint t = 0; t < 24; t++)
        {
            np = Timeline<EdgeVerification.BigTrack, EdgeVerification.BigClip>.Forward(outer, in np, in nestedInput, ref nested, nestedBuffer, t);
            nestedPeak = Math.Max(nestedPeak, nested.InnerData.Count);
        }

        if (!BitIdentical(in nestedBefore, in nestedInput) || nestedPeak == 0)
            throw new InvalidOperationException("The reentrant forward walk must leave NestedInput bit-identical while the nested result accumulates.");

        // The rewind drives the inner timeline's Backward from inside the
        // callback too; the exact-inverse contract returns the nested
        // accumulator's Count to zero — the input must still be bit-identical.
        for (var t = 23; t >= 0; t--)
            np = Timeline<EdgeVerification.BigTrack, EdgeVerification.BigClip>.Backward(outer, in np, in nestedInput, ref nested, nestedBuffer, (uint)t);
        if (!BitIdentical(in nestedBefore, in nestedInput) || nested.InnerData.Count != 0)
            throw new InvalidOperationException("The reentrant backward rewind must leave NestedInput bit-identical and the nested accumulator restored.");

        Console.WriteLine(
            $"Input immutability verified: VitalsInput (seed {inputBefore.Health:R}) and the reentrant NestedInput are bit-identical after forward walks and backward rewinds " +
            $"through the compiled tables and the global hub ({inputWalk.Count}/{inputWalk.Back} callbacks, nested inner peaked at {nestedPeak} and restored to {nested.InnerData.Count}).");
    }

    EdgeVerification.Run();

    Console.WriteLine("Playback verified: packing, walks, rewind, loops, lifecycle, per-work states, runtime timelines, global hub, frozen parity.");
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
// resolved through the real tables at run time. It accumulates ALL active
// clips (the frozen-sink oracle), adding each work's ClipState code to the
// Flags checksum.
internal struct OracleVitals :
    IForward<VitalsTrack, VitalsClip, NoInput, OracleVitals>,
    IBackward<VitalsTrack, VitalsClip, NoInput, OracleVitals>,
    IFrozenOracle
{
    public FrozenSink Sink { get; set; }

    public void Forward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref OracleVitals result)
    {
        var sink = result.Sink;
        foreach (var work in tracks)
        {
            sink.Sum += work.Clip.Amount;
            sink.Flags += (uint)work.State;
        }
        sink.Count++;
        result.Sink = sink;
    }

    public void Backward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref OracleVitals result)
    {
        var sink = result.Sink;
        foreach (var work in tracks)
        {
            sink.Sum -= work.Clip.Amount;
            sink.Flags -= (uint)work.State;
        }
        sink.Count--;
        result.Sink = sink;
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
    // No overlapping pairs anywhere: this fixture needs no blend scratch.
    public static int MaxActiveBlends => 0;
    public static bool Loops => false;

    public void Blend(in Fused16Clip first, in Fused16Clip second, float t, out Fused16Clip result)
        => result = new Fused16Clip(first.Value * (1f - t) + second.Value * t);
}

internal struct OracleFused16 :
    IForward<Fused16Track, Fused16Clip, NoInput, OracleFused16>,
    IBackward<Fused16Track, Fused16Clip, NoInput, OracleFused16>,
    IFrozenOracle
{
    public FrozenSink Sink { get; set; }

    public void Forward(in Tracks<Fused16Track, Fused16Clip> tracks, in NoInput input, in uint tick, ref OracleFused16 result)
    {
        var sink = result.Sink;
        foreach (var work in tracks)
        {
            sink.Sum += work.Clip.Value;
            sink.Flags += (uint)work.State;
        }
        sink.Count++;
        result.Sink = sink;
    }

    public void Backward(in Tracks<Fused16Track, Fused16Clip> tracks, in NoInput input, in uint tick, ref OracleFused16 result)
    {
        var sink = result.Sink;
        foreach (var work in tracks)
        {
            sink.Sum -= work.Clip.Value;
            sink.Flags -= (uint)work.State;
        }
        sink.Count--;
        result.Sink = sink;
    }
}

// The Heal closure for the global-hub receipts: a second (TTrack, TClip)
// pair proving the index space is process-global, plus a consumer that
// counts Enter/Stay/Exit calls so the pinned state sequence is visible.
public readonly record struct HealClip(float Amount);

public readonly struct HealTrack(int offset) : IBlend<HealClip>
{
    public readonly int Offset = offset;

    public void Blend(in HealClip first, in HealClip second, float t, out HealClip result)
        => result = new HealClip(first.Amount * (1f - t) + second.Amount * t);
}

internal struct HealData :
    IForward<HealTrack, HealClip, NoInput, HealData>,
    IBackward<HealTrack, HealClip, NoInput, HealData>
{
    public int Enters;
    public int Stays;
    public int Exits;
    public int Calls;

    public void Forward(in Tracks<HealTrack, HealClip> tracks, in NoInput input, in uint tick, ref HealData result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Enter: result.Enters++; break;
                case ClipState.Stay: result.Stays++; break;
                case ClipState.Exit: result.Exits++; break;
            }
        }

        result.Calls++;
    }

    public void Backward(in Tracks<HealTrack, HealClip> tracks, in NoInput input, in uint tick, ref HealData result)
    {
        foreach (var work in tracks)
        {
            switch (work.State)
            {
                case ClipState.Enter: result.Enters++; break;
                case ClipState.Stay: result.Stays++; break;
                case ClipState.Exit: result.Exits++; break;
            }
        }

        result.Calls++;
    }
}

public readonly record struct HealSource(float Amount, int Offset);

// Two distinct consumer data types for one (VitalsTrack, VitalsClip)
// timeline: the shared-index receipt above plays both through the SAME
// Build'd index, each accumulating in its own units.
internal struct SharedLeft :
    IForward<VitalsTrack, VitalsClip, NoInput, SharedLeft>,
    IBackward<VitalsTrack, VitalsClip, NoInput, SharedLeft>
{
    public float Sum;
    public int Seen;
    public int Rewinds;

    public void Forward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref SharedLeft result)
    {
        foreach (var work in tracks)
        {
            if (work.State == ClipState.Stay)
                result.Sum += work.Clip.Amount;
        }
        result.Seen++;
    }

    public void Backward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref SharedLeft result)
    {
        foreach (var work in tracks)
        {
            if (work.State == ClipState.Stay)
                result.Sum -= work.Clip.Amount;
        }
        result.Rewinds++;
    }
}

internal struct SharedRight :
    IForward<VitalsTrack, VitalsClip, NoInput, SharedRight>,
    IBackward<VitalsTrack, VitalsClip, NoInput, SharedRight>
{
    public long Ticks;
    public int Seen;
    public int Rewinds;

    public void Forward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref SharedRight result)
    {
        foreach (var work in tracks)
        {
            if (work.State == ClipState.Stay)
                result.Ticks += work.Track.Offset + tick;
        }
        result.Seen++;
    }

    public void Backward(in Tracks<VitalsTrack, VitalsClip> tracks, in NoInput input, in uint tick, ref SharedRight result)
    {
        foreach (var work in tracks)
        {
            if (work.State == ClipState.Stay)
                result.Ticks -= work.Track.Offset + tick;
        }
        result.Rewinds++;
    }
}
