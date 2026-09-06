using BenchmarkDotNet.Running;
using Tl.Hooks;

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
    foreach (var actual in new[] { shape.ShellSingle(), shape.ShellParamsFour() })
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
    Console.WriteLine("Dispatch receipts match; generated, constrained, ref-data, and API-shape calls mutate the original; boxing copies it.");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
