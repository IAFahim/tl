using System.Diagnostics;
using Tl;
using Tl.Gen.Tlb;

namespace PhaseClock;

/// <summary>
/// 4.0 (TimelineWorld/PlaybackTable) vs 5.0 (World) same-shape comparison.
/// The shape matrix (capacity, rows, perRow, lane, rowOf/phaseOf/timelineOf,
/// oracle, BenchParams) is ScaleBench's, reused via its internals, so the
/// 5.0 arm is ScaleBench code and both arms fold identical (row, phase)
/// playbacks over the identical 5-tick oracle window.
/// 4.0 tick = ApplyRequests + fold + Advance (the benchmark/Systems tick
/// composition). 4.0 fold iterates PlaybackTable.View entity x slot and
/// adds lane[slot.Tick] per live playback: one full-window step per
/// timeline makes generated-system window checks vacuous, and with no
/// ITlModule registered the harness reads the same lane values the 5.0 arm
/// folds (production fold lanes are materialized from registered modules).
/// Phase alignment: 5.0 Start phase p folds lane[p+1] on the first Tick
/// (frame 1); a 4.0 Playback.Tick is the current (next-fold) tick and
/// Advance steps it after the fold, so the harness starts each 4.0
/// playback at at = (p + 1) % Duration. Both arms fold ticks p+1..p+5;
/// exact-hash parity is the proof. 4.0 is single-threaded (Advance has no
/// worker knob): primary comparison is 5.0 @8, secondary 5.0 @1.
/// Libraries carry the same logical spec in each runtime's native format:
/// 5.0 LibraryWriter+Verify (ScaleBench.BuildLibrary); 4.0 TimelineBaker
/// TLB (TimelineBenchTrack/Clip, one full-window clip, duration 1024,
/// loop) + LibraryAssembler. Track/clip payloads are inert (Advance
/// behavior depends on duration/loop only). 4.0 has no O(N^2) FindPlain
/// analog: the duplicate check scans one entity's slots (&lt;= perRow).
/// Bulk-start cost is enqueue + sorted drain + per-record apply.
/// </summary>
public static unsafe class TimelineBench
{
    const int Duration = 1024;
    const int OracleTicks = 5;

    const string BakeJsonTemplate = "{{\"name\":\"bench\",\"duration\":{0},\"loop\":true,\"tracks\":[{{\"namespace\":\"PhaseClock\",\"type\":\"TimelineBenchTrack\",\"data\":{{\"Scale\":1}},\"clips\":[{{\"namespace\":\"PhaseClock\",\"type\":\"TimelineBenchClip\",\"start\":0,\"end\":{0},\"data\":{{\"Velocity\":2}}}}]}}]}}";

    sealed record Shape(string Label, int Capacity, int Rows, int PerRow, ulong RecordedHash);

    // RecordedHash = oracle from benchmarks/PhaseClock/results/scale.txt (ScaleBench worker lines).
    static readonly Shape[] Shapes =
    [
        new("shapeA-sparse-1M-1", 1_000_000, 1_000_000, 1, 0xd53184bfa4993025ul),
        new("shapeA-sparse-1M-4", 1_000_000, 1_000_000, 4, 0xfa13635a6d993025ul),
        new("shapeA-sparse-4M-1", 4_000_000, 4_000_000, 1, 0xdc9a12f52ef85725ul),
        new("shapeA-sparse-4M-4", 4_000_000, 4_000_000, 4, 0xb352044711105725ul),
        new("shapeB-dense-100x150", 100, 100, 150, 0x1d9ddea75d54bb75ul),
        new("shapeB-dense-1000x150", 1000, 1000, 150, 0x899da8717fc0b745ul),
        new("shapeB-dense-10000x150", 10000, 10000, 150, 0x00a627cec1b67865ul),
    ];

    public static int Run()
    {
        var output = new List<string>();
        void Emit(string line)
        {
            Console.WriteLine(line);
            output.Add(line);
        }
        Emit("timelinebench 4.0(TimelineWorld/PlaybackTable) vs 5.0(World) same-shape comparison");
        Emit($"timelinebench machine cores={Environment.ProcessorCount}");
        Emit("timelinebench method: shapes/rowOf/phaseOf/timelineOf/oracle/BenchParams reused from ScaleBench (SetupShape/MeasureShape/OracleHash); 5.0 arm is ScaleBench code verbatim");
        Emit("timelinebench 4.0 tick = ApplyRequests + fold + Advance (benchmarks/Systems tick composition); 4.0 single-threaded, primary compare 5.0 @8, secondary @1; min of rounds ms/tick");
        Emit("timelinebench 4.0 fold = entity x slot scan of PlaybackTable.View: acc[e] += lane[slot.Tick]; one full-window step per timeline (window checks vacuous); no ITlModule so fold reads the same harness lane the 5.0 arm folds");
        Emit("timelinebench phase: 5.0 Start phase p folds lane[p+1] first (frame 1); 4.0 Tick is the current tick (fold before Advance), harness starts at (p+1)%1024; both arms fold p+1..p+5; exact-hash parity proves alignment");
        Emit("timelinebench libraries: 5.0 LibraryWriter+Verify; 4.0 TimelineBaker TLB (TimelineBenchTrack/Clip, 1 full-window clip, duration 1024, loop) + LibraryAssembler (names t000.. sorted); payloads inert, logical spec identical");
        Emit("timelinebench setup breakdown: library-bake ms / TimelineWorld-load ms (4.0 only; 5.0 world ctor excluded from setupMs by ScaleBench) / enqueue+first-tick ms; 4.0 duplicate check scans one entity's slots, no N^2 FindPlain analog");

        var failures = 0;
        var lane = Data.VaryingLane(Duration);
        foreach (var shape in Shapes)
            failures += RunShape(Emit, shape, lane);
        Emit(failures == 0 ? "TIMELINEBENCH VERIFY PASS" : $"TIMELINEBENCH VERIFY FAIL {failures}");
        WriteResults(output);
        return failures == 0 ? 0 : 1;
    }

    static int RunShape(Action<string> emit, Shape shape, float[] lane)
    {
        var failures = 0;
        var total = shape.Rows * shape.PerRow;

        var fiveBakeMs = 0.0;
        {
            var sw = Stopwatch.StartNew();
            using (ScaleBench.BuildLibrary(shape.PerRow, Duration, lane)) { }
            sw.Stop();
            fiveBakeMs = sw.Elapsed.TotalSeconds * 1e3;
        }
        var (gameTlib, fourBakeMs, fourLibBytes) = BuildTimelineLibrary(shape.PerRow);

        // 5.0 shape matrix + setup (emits the ScaleBench setup line).
        var (lib, rowOf, phaseOf, timelineOf, fiveSetupMs) = ScaleBench.SetupShape(shape.Capacity, shape.Rows, shape.PerRow, lane, emit, shape.Label);
        using (lib)
        {
            var oracle = ScaleBench.OracleHash(lane, rowOf, phaseOf, shape.Capacity, OracleTicks);
            var recordedPass = oracle == shape.RecordedHash;
            if (!recordedPass) failures++;
            emit($"{(recordedPass ? "PASS" : "FAIL")} {shape.Label} oracle-vs-recorded-scale.txt recorded={shape.RecordedHash:x16} actual={oracle:x16}");
            var (warmup, rounds, ticksPerRound) = ScaleBench.BenchParams(total);

            double five1Ms = 0, five1Ns = 0;
            double five8Ms = 0, five8Ns = 0;
            long five1Alloc = 0, five8Alloc = 0;
            foreach (var w in new[] { 1, 8 })
            {
                var (ms, ns, hash, alloc) = ScaleBench.MeasureShape(lib, shape.Capacity, rowOf, phaseOf, timelineOf, w, OracleTicks, warmup, rounds, ticksPerRound, total);
                var pass = hash == oracle;
                if (!pass) failures++;
                emit($"{(pass ? "PASS" : "FAIL")} {shape.Label} 5.0 workers={w} oracle={oracle:x16} actual={hash:x16} msPerTick={ms:F3} nsPerActive={ns:F4} visits={total} allocPerTick={alloc}");
                if (w == 1) (five1Ms, five1Ns, five1Alloc) = (ms, ns, alloc);
                else (five8Ms, five8Ns, five8Alloc) = (ms, ns, alloc);
            }

            var fourSetup = MeasureTimelineSetup(gameTlib, shape.Capacity, shape.PerRow, rowOf, phaseOf, timelineOf, lane);
            emit($"{shape.Label} 4.0 setupMs={fourSetup.SetupMs:F2} loadMs={fourSetup.LoadMs:F2} total={total} capacity={shape.Capacity} heapWorldBytesManaged={fourSetup.HeapBytes} active={fourSetup.Active}");

            var four = MeasureTimeline(gameTlib, shape.Capacity, shape.PerRow, rowOf, phaseOf, timelineOf, lane, OracleTicks, warmup, rounds, ticksPerRound, total);
            var fourPass = four.Hash == oracle;
            if (!fourPass) failures++;
            emit($"{(fourPass ? "PASS" : "FAIL")} {shape.Label} 4.0 oracle={oracle:x16} actual={four.Hash:x16} msPerTick={four.MsPerTick:F3} nsPerActive={four.NsPerActive:F4} visits={total} allocPerTick={four.AllocPerTick} bakeMs={fourBakeMs:F2} fiveBakeMs={fiveBakeMs:F2} gameTlibBytes={fourLibBytes}");

            var s8 = five8Ms / four.MsPerTick;
            var s1 = five1Ms / four.MsPerTick;
            emit($"SUMMARY {shape.Label} 4.0 msPerTick={four.MsPerTick:F3} nsPerActive={four.NsPerActive:F4} alloc={four.AllocPerTick} setupMs={fourSetup.SetupMs:F2} loadMs={fourSetup.LoadMs:F2} bakeMs={fourBakeMs:F2} | 5.0@8 msPerTick={five8Ms:F3} nsPerActive={five8Ns:F4} alloc={five8Alloc} setupMs={fiveSetupMs:F2} bakeMs={fiveBakeMs:F2} | 5.0@1 msPerTick={five1Ms:F3} nsPerActive={five1Ns:F4} alloc={five1Alloc} | ratio5.0/4.0@8={s8:F2}x ratio5.0/4.0@1={s1:F2}x nsRatio@8={five8Ns / four.NsPerActive:F2}x | hash={oracle:x16} params=warmup={warmup}/rounds={rounds}/ticksPerRound={ticksPerRound}");
        }
        GC.Collect();
        return failures;
    }

    static (byte[] Bytes, double BakeMs, int BytesLen) BuildTimelineLibrary(int timelines)
    {
        var sw = Stopwatch.StartNew();
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(TimelineBench).Assembly]);
        var json = string.Format(BakeJsonTemplate, Duration);
        var tlb = TimelineBaker.BakeJson(json, resolver);
        var parts = new LibraryPart[timelines];
        for (var i = 0; i < timelines; i++)
            parts[i] = new LibraryPart { Name = $"t{i:D3}", Tlb = tlb };
        var bytes = LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
        sw.Stop();
        return (bytes, sw.Elapsed.TotalSeconds * 1e3, bytes.Length);
    }

    static (double SetupMs, double LoadMs, long HeapBytes, int Active) MeasureTimelineSetup(byte[] gameTlib, int capacity, int perRow, int[] rowOf, ushort[] phaseOf, TimelineId[] timelineOf, float[] lane)
    {
        var heapBefore = GC.GetTotalMemory(true);
        var loadSw = Stopwatch.StartNew();
        using var world = new TimelineWorld(gameTlib);
        loadSw.Stop();
        var loadMs = loadSw.Elapsed.TotalSeconds * 1e3;
        using var players = new PlaybackTable(capacity, perRow);
        var acc = new float[capacity];
        var setupSw = Stopwatch.StartNew();
        for (var i = 0; i < rowOf.Length; i++)
            players.Start(rowOf[i], timelineOf[i].Value, (ushort)((phaseOf[i] + 1) % Duration));
        Tick4(world, players, acc, lane);
        setupSw.Stop();
        var setupMs = setupSw.Elapsed.TotalSeconds * 1e3;
        var view = players.View;
        var active = 0;
        for (var e = 0; e < capacity; e++)
            active += view.CountOf(e);
        var heapBytes = GC.GetTotalMemory(false) - heapBefore;
        return (setupMs, loadMs, heapBytes, active);
    }

    static (double MsPerTick, double NsPerActive, ulong Hash, long AllocPerTick) MeasureTimeline(byte[] gameTlib, int capacity, int perRow, int[] rowOf, ushort[] phaseOf, TimelineId[] timelineOf, float[] lane, int totalTicks, int warmup, int rounds, int ticksPerRound, int visits)
    {
        using var world = new TimelineWorld(gameTlib);
        using var players = new PlaybackTable(capacity, perRow);
        var acc = new float[capacity];
        for (var i = 0; i < rowOf.Length; i++)
            players.Start(rowOf[i], timelineOf[i].Value, (ushort)((phaseOf[i] + 1) % Duration));
        for (var f = 0; f < totalTicks; f++) Tick4(world, players, acc, lane);
        var hash = Checker.HashSlice(acc, 0, capacity);
        for (var f = 0; f < warmup; f++) Tick4(world, players, acc, lane);
        var samples = new double[rounds];
        double sink = 0;
        for (var r = 0; r < rounds; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) Tick4(world, players, acc, lane);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e3 / ticksPerRound;
            sink += Checker.HashSlice(acc, 0, capacity) & 0xff;
        }
        Array.Sort(samples);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++) Tick4(world, players, acc, lane);
        var alloc = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"timeline sink={sink:F0}");
        var ms = samples[0];
        var ns = visits > 0 ? ms * 1e6 / visits : 0;
        return (ms, ns, hash, alloc);
    }

    static void Tick4(TimelineWorld world, PlaybackTable players, float[] acc, float[] lane)
    {
        world.ApplyRequests(players);
        Fold(players.View, acc, lane);
        world.Advance(players, true);
    }

    static void Fold(PlaybackView view, float[] acc, float[] lane)
    {
        for (var e = 0; e < view.EntityCount; e++)
        {
            var count = view.CountOf(e);
            if (count == 0) continue;
            var slots = view.SlotOf(e, 0);
            for (var s = 0; s < count; s++)
            {
                ref readonly var slot = ref slots[s];
                if (slot.Timeline == 0) continue;
                acc[e] += lane[slot.Tick];
            }
        }
    }

    static void WriteResults(List<string> lines)
    {
        var dir = Directory.GetCurrentDirectory();
        string? root = null;
        var probe = dir;
        while (probe is not null)
        {
            if (File.Exists(Path.Combine(probe, "benchmarks", "PhaseClock", "PhaseClock.csproj"))) { root = probe; break; }
            probe = Directory.GetParent(probe)?.FullName;
        }
        var path = root is null
            ? Path.Combine(dir, "timelinebench.txt")
            : Path.Combine(root, "benchmarks", "PhaseClock", "results", "timelinebench.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        Console.WriteLine($"results written: {path}");
    }
}

/// <summary>Bake-side fixture pair (Tl.Gen.Tlb): 4.0 TLBs require a real blendable track/clip pair. Payload values are inert for this benchmark.</summary>
public struct TimelineBenchTrack : IBlend<TimelineBenchClip>
{
    public float Scale;
    public void Blend(in TimelineBenchClip first, in TimelineBenchClip second, float factor, out TimelineBenchClip result) => result = first;
}

public struct TimelineBenchClip
{
    public float Velocity;
}
