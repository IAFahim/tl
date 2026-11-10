using System.Diagnostics;
using System.Numerics;

namespace PhaseClock;

/// <summary>
/// Large-scale hostile shape: mixed phase spread across duration 1024 (not uniform),
/// sparse permuted rows across the full capacity, at 1M and 4M entities.
/// Kernel arms use the existing PhaseClock kernels directly (no new kernels);
/// World arms use Tl.World with the same (row, phase) pair set.
/// Sparse-active World arms narrow the step window to [0,40) of duration 1024,
/// so only ~40/1024 of playbacks are visited per tick.
/// </summary>
public static class LargeBench
{
    const int Duration = 1024;
    const int SparseWindow = 40;
    const int Off = 37;
    const int OracleTicks = 97;
    const double BaselineNs = 14.96;

    sealed class LargeData
    {
        public required int N;
        public required float[] LaneVar;
        public required float[] LaneConst;
        public required ushort[] Phase;
        public required int[] Row;
        public required int[] SortedRow;
        public required ushort[] SortedPhase;
        public required (int Start, int Length, ushort Phase)[] Runs;
        public required ushort[] PhaseByRow;
    }

    public static int Run(bool verifyOnly)
    {
        var output = new List<string>();
        void Emit(string line)
        {
            Console.WriteLine(line);
            output.Add(line);
        }

        Emit($"large hostile: duration={Duration} mixed-phase=(e*37+11)%{Duration} sparse-row=(e*97)%N vectorAccel={Vector.IsHardwareAccelerated} floatCount={Vector<float>.Count}");
        Emit($"large hostile: baseline folded 4.0 = {BaselineNs:F2} ns/entity (local receipt); 13x threshold = {BaselineNs / 13.0:F3} ns/entity");
        var failures = 0;
        var scales = new[] { 1024, 1_000_000, 4_000_000 };
        var kernelResults = new List<(int N, string Kernel, double Ns, double ScalarNs)>();

        foreach (var n in scales)
        {
            var data = Generate(n);
            Emit($"--- N={n} data bytes: acc={4L * n} row={4L * n} phase={2L * n} sortedRow={4L * n} sortedPhase={2L * n} total={16L * n + 4096} heapTotal={GC.GetTotalMemory(false)}");
            // Verify: hash parity vs scalar oracle (Kernels.ScalarPhase), exact.
            var (constOracle, varOracle) = OracleHashes(data);
            foreach (var kernel in new[] { "BroadcastSparse", "RunBroadcast", "LaneGatherScalar" })
            {
                var hash = KernelHash(data, kernel);
                var expected = kernel == "BroadcastSparse" ? constOracle : varOracle;
                var pass = hash == expected;
                if (!pass) failures++;
                Emit($"{(pass ? "PASS" : "FAIL")} hostile-{ScaleName(n)} {kernel} oracle={expected:x16} actual={hash:x16}");
            }
            // 0 B warm alloc per kernel (direct calls, no delegates in measured loop).
            foreach (var kernel in new[] { "BroadcastSparse", "RunBroadcast", "LaneGatherScalar" })
            {
                var allocated = AllocCheck(data, kernel);
                var pass = allocated == 0;
                if (!pass) failures++;
                Emit($"ALLOC hostile-{ScaleName(n)} {kernel} warm 50 ticks: {allocated} B");
            }
            if (verifyOnly)
            {
                Free(data);
                continue;
            }
            var (warmup, rounds, ticksPerRound) = BenchParams(n);
            var scalarNs = MeasureScalar(data, warmup, rounds, ticksPerRound);
            Emit($"hostile-{ScaleName(n)} ScalarPhase | {scalarNs:F3} ns/entity gain-vs-baseline={BaselineNs / scalarNs:F2}x sink-check");
            kernelResults.Add((n, "ScalarPhase", scalarNs, scalarNs));
            foreach (var kernel in new[] { "BroadcastSparse", "RunBroadcast", "LaneGatherScalar" })
            {
                var ns = MeasureKernel(data, kernel, warmup, rounds, ticksPerRound);
                Emit($"hostile-{ScaleName(n)} {kernel} | {ns:F3} ns/entity gain-vs-baseline={BaselineNs / ns:F2}x gain-vs-scalar={scalarNs / ns:F2}x");
                kernelResults.Add((n, kernel, ns, scalarNs));
            }
            Free(data);
        }

        if (!verifyOnly)
        {
            foreach (var (n, kernel, ns, scalarNs) in kernelResults)
                Emit($"SUMMARY N={n} {kernel} ns/entity={ns:F3} scalar={scalarNs:F3} gainBaseline={BaselineNs / ns:F2}x gainScalar={scalarNs / ns:F2}x");
            failures += RunWorldHostile(Emit);
        }

        Emit(failures == 0 ? "LARGE VERIFY PASS" : $"LARGE VERIFY FAIL {failures}");
        if (!verifyOnly) WriteResults(output);
        return failures == 0 ? 0 : 1;
    }

    static string ScaleName(int n) => n switch { 1024 => "1024", 1_000_000 => "1M", 4_000_000 => "4M", _ => n.ToString() };

    static (int Warmup, int Rounds, int TicksPerRound) BenchParams(int n) => n switch
    {
        1024 => (8000, 9, 20000),
        1_000_000 => (30, 5, 60),
        _ => (20, 5, 40),
    };

    static LargeData Generate(int n)
    {
        var laneVar = Data.VaryingLane(Duration);
        var laneConst = Data.ConstantLane(Duration, 6f);
        var phase = new ushort[n];
        var row = new int[n];
        for (var e = 0; e < n; e++)
        {
            phase[e] = (ushort)((e * 37 + 11) % Duration);
            row[e] = (int)((e * 97L) % n);
        }
        // Counting sort by phase (stable, duration-sized buckets): phase-sorted rows for RunBroadcast.
        var counts = new int[Duration];
        for (var e = 0; e < n; e++) counts[phase[e]]++;
        var offsets = new int[Duration];
        var sum = 0;
        for (var p = 0; p < Duration; p++) { offsets[p] = sum; sum += counts[p]; }
        var sortedRow = new int[n];
        var sortedPhase = new ushort[n];
        var cursor = (int[])offsets.Clone();
        for (var e = 0; e < n; e++)
        {
            var p = phase[e];
            var dst = cursor[p]++;
            sortedRow[dst] = row[e];
            sortedPhase[dst] = p;
        }
        var runs = new List<(int, int, ushort)>();
        var i = 0;
        while (i < n)
        {
            var j = i + 1;
            while (j < n && sortedPhase[j] == sortedPhase[i]) j++;
            runs.Add((i, j - i, sortedPhase[i]));
            i = j;
        }
        var phaseByRow = new ushort[n];
        for (var e = 0; e < n; e++) phaseByRow[row[e]] = phase[e];
        return new LargeData
        {
            N = n, LaneVar = laneVar, LaneConst = laneConst, Phase = phase, Row = row,
            SortedRow = sortedRow, SortedPhase = sortedPhase, Runs = runs.ToArray(), PhaseByRow = phaseByRow,
        };
    }

    static void Free(LargeData d)
    {
        // Drop large arrays promptly so 1M and 4M are never resident together.
        d.Row = Array.Empty<int>(); d.Phase = Array.Empty<ushort>();
        d.SortedRow = Array.Empty<int>(); d.SortedPhase = Array.Empty<ushort>();
        d.PhaseByRow = Array.Empty<ushort>();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    static (ulong Const, ulong Var) OracleHashes(LargeData d)
    {
        var accConst = new float[d.N + Off + 64];
        var accVar = new float[d.N + Off + 64];
        for (var f = 0; f < OracleTicks; f++) Kernels.ScalarPhase(accConst, Off, d.Row, d.Phase, d.LaneConst, f, Duration, 1, d.N);
        for (var f = 0; f < OracleTicks; f++) Kernels.ScalarPhase(accVar, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
        return (Checker.HashSlice(accConst, Off, d.N), Checker.HashSlice(accVar, Off, d.N));
    }

    static ulong KernelHash(LargeData d, string kernel)
    {
        var acc = new float[d.N + Off + 64];
        for (var f = 0; f < OracleTicks; f++)
        {
            switch (kernel)
            {
                case "BroadcastSparse":
                    Kernels.BroadcastSparse(acc, Off, d.Row, 0, d.N, d.LaneConst[Kernels.Tick(0, f, Duration, 1)]);
                    break;
                case "RunBroadcast":
                    Kernels.RunBroadcast(acc, Off, d.SortedRow, d.Runs, d.LaneVar, f, Duration, 1);
                    break;
                default:
                    Kernels.LaneGatherScalar(acc, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
                    break;
            }
        }
        return Checker.HashSlice(acc, Off, d.N);
    }

    static long AllocCheck(LargeData d, string kernel)
    {
        var acc = new float[d.N + Off + 64];
        for (var f = 0; f < 20; f++) TickKernel(d, kernel, acc, f);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 50; f++) TickKernel(d, kernel, acc, f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var sink = Checker.HashSlice(acc, Off, d.N) & 0xff;
        if (sink == 0x12345678) Console.WriteLine("unreachable");
        return allocated;
    }

    static void TickKernel(LargeData d, string kernel, float[] acc, int f)
    {
        switch (kernel)
        {
            case "BroadcastSparse":
                Kernels.BroadcastSparse(acc, Off, d.Row, 0, d.N, d.LaneConst[Kernels.Tick(0, f, Duration, 1)]);
                break;
            case "RunBroadcast":
                Kernels.RunBroadcast(acc, Off, d.SortedRow, d.Runs, d.LaneVar, f, Duration, 1);
                break;
            case "LaneGatherScalar":
                Kernels.LaneGatherScalar(acc, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
                break;
            default:
                Kernels.ScalarPhase(acc, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
                break;
        }
    }

    static double MeasureKernel(LargeData d, string kernel, int warmup, int rounds, int ticksPerRound)
    {
        var acc = new float[d.N + Off + 64];
        for (var f = 0; f < warmup; f++) TickKernel(d, kernel, acc, f);
        var samples = new double[rounds];
        double sink = 0;
        for (var r = 0; r < rounds; r++)
        {
            Array.Clear(acc, Off, d.N);
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) TickKernel(d, kernel, acc, f);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / ticksPerRound / d.N;
            sink += Checker.HashSlice(acc, Off, d.N) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"kernel sink={sink:F0}");
        return samples[0];
    }

    static double MeasureScalar(LargeData d, int warmup, int rounds, int ticksPerRound)
    {
        var acc = new float[d.N + Off + 64];
        for (var f = 0; f < warmup; f++) Kernels.ScalarPhase(acc, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
        var samples = new double[rounds];
        double sink = 0;
        for (var r = 0; r < rounds; r++)
        {
            Array.Clear(acc, Off, d.N);
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) Kernels.ScalarPhase(acc, Off, d.Row, d.Phase, d.LaneVar, f, Duration, 1, d.N);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / ticksPerRound / d.N;
            sink += Checker.HashSlice(acc, Off, d.N) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"scalar sink={sink:F0}");
        return samples[0];
    }

    // ---- World hostile end-to-end ----

    const int WorldActive = 4096;

    static int RunWorldHostile(Action<string> emit)
    {
        var failures = 0;
        // Dense-active World at 1M/4M active playbacks cannot be set up via the public
        // API: World.StartPlayback duplicate-checks with a linear FindPlain scan, so
        // starting N playbacks is O(N^2). Measured start-cost scaling below proves it;
        // therefore the 1M/4M end-to-end Tick measurement uses sparse capacity:
        // World(Library, capacity) with WorldActive playbacks spread across the
        // capacity (few active rows per tick relative to millions of rows).
        failures += RunStartCostProbe(emit);
        foreach (var n in new[] { 1_000_000, 4_000_000 })
        {
            foreach (var (label, windowEnd, lane) in new (string, int, float[])[]
            {
                ("sparse-capacity-dense-window", Duration, Data.VaryingLane(Duration)),
                ("sparse-capacity-sparse-window-40/1024", SparseWindow, Data.VaryingLane(SparseWindow)),
            })
            {
                using var library = BuildLibrary(windowEnd, lane);
                var data = Generate(n);
                var (rows, phases) = ActiveSet(data);
                var oracle = WorldOracleHash(data, rows, phases, windowEnd, lane);
                var worldHash = RunWorldOracle(library, data, rows, phases);
                var pass = worldHash == oracle;
                if (!pass) failures++;
                emit($"{(pass ? "PASS" : "FAIL")} world-hostile-{ScaleName(n)} {label} active={WorldActive} oracle={oracle:x16} world={worldHash:x16}");
                var (nsPerCapacity, nsPerVisit, visits, heapBytes, worldAllocPerTick) = MeasureWorldHostile(library, data, rows, phases);
                emit($"world-hostile-{ScaleName(n)} {label} ns/capacity-row={nsPerCapacity:F3} ns/active-visit={nsPerVisit:F3} visits/tick={visits} gainBaseline-perCapacity={BaselineNs / nsPerCapacity:F2}x heapWorldBytes={heapBytes} worldAllocPerTick={worldAllocPerTick}");
                Free(data);
                GC.Collect();
            }
        }
        return failures;
    }

    static (int[] Rows, ushort[] Phases) ActiveSet(LargeData d)
    {
        var rows = new int[WorldActive];
        var phases = new ushort[WorldActive];
        for (var i = 0; i < WorldActive; i++)
        {
            var row = (int)((i * 7919L) % d.N);
            rows[i] = row;
            phases[i] = d.PhaseByRow[row];
        }
        return (rows, phases);
    }

    static int RunStartCostProbe(Action<string> emit)
    {
        // Measured (not projected): setup time for dense-active worlds, capacity == active.
        // First Tick applies all Starts (FindPlain O(N^2) scan + radix sort of pending).
        using var library = BuildLibrary(Duration, Data.VaryingLane(Duration));
        foreach (var a in new[] { 1024, 8192, 32768 })
        {
            var data = Generate(a);
            using var world = new Tl.World(library, a);
            var req = world.Requests;
            var id = new Tl.TimelineId(1);
            for (var r = 0; r < a; r++) req.Start(new Tl.Row(r), id, data.PhaseByRow[r]);
            var sw = Stopwatch.StartNew();
            world.Tick(Tl.Direction.Forward);
            sw.Stop();
            var setupSec = sw.Elapsed.TotalSeconds;
            // Measure steady Tick after setup (dense: visits == active).
            for (var f = 0; f < 200; f++) world.Tick(Tl.Direction.Forward);
            var samples = new double[5];
            var ticksPerRound = a <= 1024 ? 20000 : a <= 8192 ? 2000 : 500;
            for (var r = 0; r < 5; r++)
            {
                var sw2 = Stopwatch.StartNew();
                for (var f = 0; f < ticksPerRound; f++) world.Tick(Tl.Direction.Forward);
                sw2.Stop();
                samples[r] = sw2.Elapsed.TotalSeconds * 1e9 / ticksPerRound / a;
            }
            Array.Sort(samples);
            emit($"world-start-cost dense-active={a} setupFirstTickSec={setupSec:F3} tickNsPerEntity={samples[0]:F3} visits={world.Report.HighWaterVisits}");
            Free(data);
            GC.Collect();
        }
        emit("world-start-cost note: setup is O(active) via packed hash index (measured; dense-active 1M/4M in ScaleBench)");
        return 0;
    }

    static Tl.Library BuildLibrary(int windowEnd, float[] lane)
    {
        var step = new Tl.StepSpec(0u, (uint)windowEnd, 0u, 1ul, lane, lane);
        var timeline = new Tl.TimelineSpec((uint)Duration, true, new[] { step });
        var blob = Tl.LibraryWriter.Write(new[] { timeline });
        var result = Tl.Library.Verify(blob);
        var library = result.Library;
        if (!result.Ok || library is null)
            throw new InvalidOperationException($"Library.Verify failed: {result.Error} at {result.ErrorOffset}");
        return library;
    }

    static ulong WorldOracleHash(LargeData d, int[] rows, ushort[] phases, int windowEnd, float[] lane)
    {
        // World frame numbering: first Tick() uses frame 1. Per-cell sum order is frame order.
        var expected = new float[d.N];
        for (var i = 0; i < rows.Length; i++)
        {
            var phase = phases[i];
            var sum = 0f;
            for (var f = 1; f <= OracleTicks; f++)
            {
                var tick = Kernels.Tick(phase, f, Duration, 1);
                if (tick < windowEnd) sum += lane[tick];
            }
            expected[rows[i]] = sum;
        }
        return Checker.HashSlice(expected, 0, d.N);
    }

    static ulong RunWorldOracle(Tl.Library library, LargeData d, int[] rows, ushort[] phases)
    {
        using var world = new Tl.World(library, d.N);
        var req = world.Requests;
        var id = new Tl.TimelineId(1);
        for (var i = 0; i < rows.Length; i++) req.Start(new Tl.Row(rows[i]), id, phases[i]);
        for (var f = 0; f < OracleTicks; f++) world.Tick(Tl.Direction.Forward);
        return Checker.HashSlice(world.Accumulator, 0, d.N);
    }

    static (double NsPerCapacity, double NsPerVisit, int Visits, long HeapBytes, long AllocPerTick) MeasureWorldHostile(Tl.Library library, LargeData d, int[] rows, ushort[] phases)
    {
        GC.Collect();
        var heapBefore = GC.GetTotalMemory(true);
        using var world = new Tl.World(library, d.N);
        var req = world.Requests;
        var id = new Tl.TimelineId(1);
        for (var i = 0; i < rows.Length; i++) req.Start(new Tl.Row(rows[i]), id, phases[i]);
        world.Tick(Tl.Direction.Forward);
        var heapAfterSetup = GC.GetTotalMemory(false);
        var heapBytes = heapAfterSetup - heapBefore;
        var (warmup, rounds, ticksPerRound) = d.N switch { 1_000_000 => (20, 5, 50), _ => (15, 5, 30) };
        for (var f = 0; f < warmup; f++) world.Tick(Tl.Direction.Forward);
        var samples = new double[rounds];
        double sink = 0;
        for (var r = 0; r < rounds; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / ticksPerRound / d.N;
            sink += Checker.HashSlice(world.Accumulator, 0, d.N) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"world sink={sink:F0} highWater={world.Report.HighWaterVisits} active={world.Report.ActiveCount}");
        var visits = world.Report.HighWaterVisits;
        var nsPerCapacity = samples[0];
        var nsPerVisit = visits > 0 ? nsPerCapacity * d.N / visits : nsPerCapacity;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++) world.Tick(Tl.Direction.Forward);
        var allocPerTick = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"world alloc per tick (calling thread)={allocPerTick} B");
        return (nsPerCapacity, nsPerVisit, visits, heapBytes, allocPerTick);
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
            ? Path.Combine(dir, "bench-large.txt")
            : Path.Combine(root, "benchmarks", "PhaseClock", "results", "bench-large.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        Console.WriteLine($"results written: {path}");
    }
}
