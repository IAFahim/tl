using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using ScaleGame;
using Tl;
using Tl.Gen.Tlb;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
if (args.Length == 2 && args[0] == "--fixture")
{
    var timeline = new TypedTimeline { Name = "scale", Duration = 1024, Loops = true };
    var track = new TypedTrack { Type = typeof(ScaleTrack), Data = BitConverter.GetBytes(1f) };
    track.Clips.Add(new TypedClip { Type = typeof(ScaleClip), Start = 0, End = 1024, Data = BitConverter.GetBytes(1.25f) });
    timeline.Tracks.Add(track);
    var tlb = LibraryBake.BakeTyped(timeline);
    var kinds = LibraryBake.Kinds(new[] { typeof(Drive).Assembly });
    var payload = LibraryBake.KindPayload("scale", tlb, kinds);
    var parts = Enumerable.Range(0, 1000).Select(i => new LibraryPart { Name = "scale" + i.ToString("D4"), Tlb = tlb, Kinds = payload }).ToArray();
    var settings = LibrarySettings.Default;
    settings.Kinds = new Dictionary<string, KindOverride> { ["ScaleGame.Energy"] = new KindOverride(inline: 4) };
    File.WriteAllBytes(args[1], LibraryAssembler.Assemble(parts, 60, settings));
    Console.WriteLine("fixture=" + args[1]);
    return;
}
unsafe
{
    var bytes = File.ReadAllBytes(args[0]);
    var n = int.Parse(args[1]);
    var active = int.Parse(args[2]);
    var workersCount = args.Length > 3 ? int.Parse(args[3]) : 1;
    const int Warmup = 32, Rounds = 6, Steps = 32;
    var process = Process.GetCurrentProcess();
    var privateBefore = process.PrivateMemorySize64;
    var setup = Stopwatch.StartNew();
    using var world = new TimelineWorld(bytes);
    using var players = new PlaybackTable(n);
    using var kinds = new KindStore(world, players);
    using var workers = new TlWorkers(workersCount);
    var total = (float*)NativeMemory.AllocZeroed((nuint)n, 4);
    try
    {
        for (var e = 0; e < n; e++)
            for (var s = 0; s < active; s++) players.Start(e, (ushort)(1 + ((e * 17L + s * 7) % 1000)), (ushort)((e * 37L + s * 11) & 1023));
        setup.Stop();
        var first = Stopwatch.StartNew();
        Step(world, players, total, n, workers);
        first.Stop();
        for (var t = 1; t < Warmup; t++) Step(world, players, total, n, workers);
        var samples = new double[Rounds];
        long allocated = 0;
        var before = GC.GetTotalAllocatedBytes(true);
        for (var r = 0; r < Rounds; r++)
        {
            var start = Stopwatch.GetTimestamp();
            for (var t = 0; t < Steps; t++) Step(world, players, total, n, workers);
            samples[r] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / Steps;
        }
        allocated = GC.GetTotalAllocatedBytes(true) - before;
        process.Refresh();
        var privateDelta = process.PrivateMemorySize64-privateBefore;
        var hash = Hash(players.View, total, n, active, Warmup + Rounds * Steps);
        Console.WriteLine($"host=dotnet shape={n}x{active} workers={workersCount} setupMs={setup.Elapsed.TotalMilliseconds:F3} firstMs={first.Elapsed.TotalMilliseconds:F3} readySetupMs={setup.Elapsed.TotalMilliseconds+first.Elapsed.TotalMilliseconds:F3} privateDelta={privateDelta} allocTotal={allocated} ticks={Rounds*Steps} hash={hash:X16} samplesMs={string.Join(',',samples.Select(v=>v.ToString("F6")))}");
        long tickClock = 0, kindClock = 0;
        for (var t = 0; t < 32; t++)
        {
            var a = Stopwatch.GetTimestamp();
            world.Tick(players);
            var b = Stopwatch.GetTimestamp();
            new Scale { Total = new Span<float>(total,n) }.Run(world,players,true,workers);
            var c = Stopwatch.GetTimestamp();
            tickClock += b-a; kindClock += c-b;
        }
        Console.WriteLine($"profile host=dotnet shape={n}x{active} workers={workersCount} tickMs={tickClock*1000.0/Stopwatch.Frequency/32:F6} kindMs={kindClock*1000.0/Stopwatch.Frequency/32:F6}");
    }
    finally { NativeMemory.Free(total); }
}

static unsafe void Step(TimelineWorld world, PlaybackTable players, float* total, int n, TlWorkers workers)
{
    world.Tick(players);
    new Scale { Total = new Span<float>(total, n) }.Run(world, players, true, workers);
}

static unsafe ulong Hash(PlaybackView view, float* total, int n, int active, int ticks)
{
    var expected = new float[1024];
    for (var p = 0; p < 1024; p++)
        for (var t = 0; t < ticks; t++) expected[p] += 1.25f + ((p + t) & 31) * 0.25f;
    var hash = 14695981039346656037ul;
    for (var e = 0; e < n; e++)
    {
        if (view.CountOf(e) != active) throw new InvalidOperationException("active count");
        float sum = 0;
        hash = (hash ^ (uint)active) * 1099511628211ul;
        for (var s = 0; s < active; s++)
        {
            var p = view.SlotOf(e,s);
            var phase = (int)((e * 37L + s * 11) & 1023);
            if (p->Timeline != 1 + ((e * 17L + s * 7) % 1000) || p->Tick != ((phase + ticks - 1) & 1023)) throw new InvalidOperationException("playback oracle");
            sum += expected[phase];
            hash = (hash ^ *(ulong*)p) * 1099511628211ul;
        }
        if (sum != total[e]) throw new InvalidOperationException($"output oracle row={e} expected={sum} actual={total[e]}");
        hash = (hash ^ (uint)BitConverter.SingleToInt32Bits(total[e])) * 1099511628211ul;
    }
    return hash;
}
