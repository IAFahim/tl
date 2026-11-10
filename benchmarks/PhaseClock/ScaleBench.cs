using System.Diagnostics;
using System.Reflection;

namespace PhaseClock;

public static class ScaleBench
{
    const int Duration = 1024;
    static readonly FieldInfo? WorkersField = typeof(Tl.World).GetField("_workers", BindingFlags.NonPublic | BindingFlags.Instance);

    static void SetWorkers(Tl.World world, int workers)
    {
        if (WorkersField is null) return;
        WorkersField.SetValue(world, workers);
    }

    public static int Run()
    {
        var output = new List<string>();
        void Emit(string line)
        {
            Console.WriteLine(line);
            output.Add(line);
        }

        var failures = 0;
        var machine = Environment.ProcessorCount;
        Emit($"scale machine cores={machine}");
        failures += RunBandwidthProbe(Emit);
        failures += RunBroadcastProbe(Emit);
        failures += RunStrideProbe(Emit);
        failures += RunShapeA(Emit);
        failures += RunShapeB(Emit);
        Emit(failures == 0 ? "SCALE VERIFY PASS" : $"SCALE VERIFY FAIL {failures}");
        WriteResults(output);
        return failures == 0 ? 0 : 1;
    }

    internal static Tl.Library BuildLibrary(int timelines, int duration, float[] lane)
    {
        var specs = new Tl.TimelineSpec[timelines];
        for (var t = 0; t < timelines; t++)
        {
            var step = new Tl.StepSpec(0u, (uint)duration, 0u, 1ul, lane, lane);
            specs[t] = new Tl.TimelineSpec((uint)duration, true, new[] { step });
        }
        var blob = Tl.LibraryWriter.Write(specs);
        var result = Tl.Library.Verify(blob);
        var library = result.Library;
        if (!result.Ok || library is null)
            throw new InvalidOperationException($"Library.Verify failed: {result.Error} at {result.ErrorOffset}");
        return library;
    }

    internal static (int Warmup, int Rounds, int TicksPerRound) BenchParams(int total)
    {
        if (total >= 8_000_000) return (3, 3, 5);
        if (total >= 4_000_000) return (5, 3, 10);
        if (total >= 1_000_000) return (10, 3, 20);
        if (total >= 100_000) return (20, 3, 50);
        return (30, 3, 100);
    }

    internal static ulong OracleHash(float[] lane, int[] rowOf, ushort[] phaseOf, int capacity, int oracleTicks)
    {
        var perPhase = new float[Duration];
        for (var p = 0; p < Duration; p++)
        {
            var sum = 0f;
            for (var f = 1; f <= oracleTicks; f++)
                sum += lane[Kernels.Tick((ushort)p, f, Duration, 1)];
            perPhase[p] = sum;
        }
        var expected = new float[capacity];
        for (var g = 0; g < rowOf.Length; g++)
            expected[rowOf[g]] += perPhase[phaseOf[g]];
        return Checker.HashSlice(expected, 0, capacity);
    }

    internal static (Tl.Library Lib, int[] RowOf, ushort[] PhaseOf, Tl.TimelineId[] TimelineOf, double SetupMs) SetupShape(int capacity, int rows, int perRow, float[] lane, Action<string> emit, string label)
    {
        var lib = BuildLibrary(perRow, Duration, lane);
        var total = rows * perRow;
        var rowOf = new int[total];
        var phaseOf = new ushort[total];
        var timelineOf = new Tl.TimelineId[total];
        var g = 0;
        for (var r = 0; r < rows; r++)
        {
            for (var k = 0; k < perRow; k++)
            {
                rowOf[g] = r;
                phaseOf[g] = (ushort)((g * 37 + 11) % Duration);
                timelineOf[g] = new Tl.TimelineId((ushort)(k + 1));
                g++;
            }
        }
        var heapBefore = GC.GetTotalMemory(true);
        using var world = new Tl.World(lib, capacity);
        var req = world.Requests;
        var setupSw = Stopwatch.StartNew();
        for (var i = 0; i < total; i++)
            req.Start(new Tl.Row(rowOf[i]), timelineOf[i], phaseOf[i]);
        world.Tick(Tl.Direction.Forward);
        setupSw.Stop();
        var heapBytes = GC.GetTotalMemory(false) - heapBefore;
        var setupMs = setupSw.Elapsed.TotalSeconds * 1e3;
        emit($"{label} setupMs={setupMs:F2} total={total} capacity={capacity} heapWorldBytes={heapBytes} active={world.ActiveCount}");
        var firstHash = Checker.HashSlice(world.Accumulator, 0, capacity);
        GC.Collect();
        // Return live handles; caller disposes. The setup world is dropped; measurements use fresh worlds.
        _ = firstHash;
        return (lib, rowOf, phaseOf, timelineOf, setupMs);
    }

    internal static (double MsPerTick, double NsPerActive, ulong Hash, long AllocPerTick) MeasureShape(Tl.Library lib, int capacity, int[] rowOf, ushort[] phaseOf, Tl.TimelineId[] timelineOf, int workers, int totalTicks, int warmup, int rounds, int ticksPerRound, int visits)
    {
        using var world = new Tl.World(lib, capacity);
        SetWorkers(world, workers);
        var req = world.Requests;
        for (var i = 0; i < rowOf.Length; i++)
            req.Start(new Tl.Row(rowOf[i]), timelineOf[i], phaseOf[i]);
        for (var f = 0; f < totalTicks; f++) world.Tick(Tl.Direction.Forward);
        var hash = Checker.HashSlice(world.Accumulator, 0, capacity);
        for (var f = 0; f < warmup; f++) world.Tick(Tl.Direction.Forward);
        var samples = new double[rounds];
        double sink = 0;
        for (var r = 0; r < rounds; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++)
                world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e3 / ticksPerRound;
            sink += Checker.HashSlice(world.Accumulator, 0, capacity) & 0xff;
        }
        Array.Sort(samples);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++)
            world.Tick(Tl.Direction.Forward);
        var alloc = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"world sink={sink:F0} highWater={world.Report.HighWaterVisits} active={world.Report.ActiveCount}");
        var ms = samples[0];
        var ns = visits > 0 ? ms * 1e6 / visits : 0;
        return (ms, ns, hash, alloc);
    }

    static int RunShapeCase(Action<string> emit, string label, int capacity, int rows, int perRow, float[] lane)
    {
        var failures = 0;
        var total = rows * perRow;
        var (lib, rowOf, phaseOf, timelineOf, _) = SetupShape(capacity, rows, perRow, lane, emit, label);
        using (lib)
        {
            const int oracleTicks = 5;
            var oracle = OracleHash(lane, rowOf, phaseOf, capacity, oracleTicks);
            var (warmup, rounds, ticksPerRound) = BenchParams(total);
            var hashes = new Dictionary<int, ulong>();
            foreach (var w in new[] { 1, 2, 4, 8 })
            {
                var (ms, ns, hash, alloc) = MeasureShape(lib, capacity, rowOf, phaseOf, timelineOf, w, oracleTicks, warmup, rounds, ticksPerRound, total);
                hashes[w] = hash;
                var pass = hash == oracle;
                if (!pass) failures++;
                emit($"{(pass ? "PASS" : "FAIL")} {label} workers={w} oracle={oracle:x16} actual={hash:x16} msPerTick={ms:F3} nsPerActiveVisit={ns:F4} visits={total} allocPerTick={alloc}");
            }
            var equal = hashes[1] == hashes[2] && hashes[1] == hashes[4] && hashes[1] == hashes[8] && hashes[1] == oracle;
            if (!equal) failures++;
            emit($"{(equal ? "PASS" : "FAIL")} {label} thread-hash-equality workers=1/2/4/8 hash={hashes[1]:x16}");
        }
        GC.Collect();
        return failures;
    }

    static int RunShapeA(Action<string> emit)
    {
        var failures = 0;
        var lane = Data.VaryingLane(Duration);
        failures += RunShapeCase(emit, "shapeA-sparse-1M-1", 1_000_000, 1_000_000, 1, lane);
        failures += RunShapeCase(emit, "shapeA-sparse-1M-4", 1_000_000, 1_000_000, 4, lane);
        failures += RunShapeCase(emit, "shapeA-sparse-4M-1", 4_000_000, 4_000_000, 1, lane);
        failures += RunShapeCase(emit, "shapeA-sparse-4M-4", 4_000_000, 4_000_000, 4, lane);
        return failures;
    }

    static int RunShapeB(Action<string> emit)
    {
        var failures = 0;
        var lane = Data.VaryingLane(Duration);
        failures += RunShapeCase(emit, "shapeB-dense-100x150", 100, 100, 150, lane);
        failures += RunShapeCase(emit, "shapeB-dense-1000x150", 1000, 1000, 150, lane);
        failures += RunShapeCase(emit, "shapeB-dense-10000x150", 10000, 10000, 150, lane);
        return failures;
    }

    static int RunStrideProbe(Action<string> emit)
    {
        var failures = 0;
        const int heavyPlaybacks = 1000;
        const int capacity = 1_000_000;
        var lane = Data.ConstantLane(40, 6f);
        using var lib = BuildLibrary(heavyPlaybacks, 40, lane);
        var heavy = MeasureStride(lib, capacity, heavyPlaybacks, true);
        var spread = MeasureStride(lib, capacity, heavyPlaybacks, false);
        var ratio = Math.Max(heavy.Ms, 1e-9) / Math.Max(spread.Ms, 1e-9);
        var pass = heavy.Ms <= spread.Ms * 3.0 + 0.05 && heavy.Active == heavyPlaybacks && spread.Active == heavyPlaybacks;
        if (!pass) failures++;
        emit($"{(pass ? "PASS" : "FAIL")} stride heavyMs={heavy.Ms:F4} spreadMs={spread.Ms:F4} ratio={ratio:F2} heavyActive={heavy.Active} spreadActive={spread.Active} heapHeavy={heavy.Heap} heapSpread={spread.Heap}");
        emit($"stride note: 1 row x {heavyPlaybacks} vs {heavyPlaybacks} rows x 1, capacity={capacity}, visits={heavyPlaybacks}/tick");
        return failures;
    }

    static (double Ms, int Active, long Heap) MeasureStride(Tl.Library lib, int capacity, int playbacks, bool heavy)
    {
        GC.Collect();
        var heapBefore = GC.GetTotalMemory(true);
        using var world = new Tl.World(lib, capacity);
        var req = world.Requests;
        for (var i = 0; i < playbacks; i++)
        {
            if (heavy) req.Start(new Tl.Row(0), new Tl.TimelineId((ushort)(i + 1)), 0);
            else req.Start(new Tl.Row(i), new Tl.TimelineId(1), 0);
        }
        world.Tick(Tl.Direction.Forward);
        var heap = GC.GetTotalMemory(false) - heapBefore;
        for (var f = 0; f < 20; f++) world.Tick(Tl.Direction.Forward);
        var samples = new double[5];
        var ticks = 200;
        for (var r = 0; r < 5; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticks; f++) world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e3 / ticks;
        }
        Array.Sort(samples);
        var sink = Checker.HashSlice(world.Accumulator, 0, capacity) & 0xff;
        if (sink == 0x12345678) Console.WriteLine("unreachable");
        return (samples[0], world.ActiveCount, heap);
    }

    static int RunBandwidthProbe(Action<string> emit)
    {
        const int count = 16_000_000;
        var buffer = new float[count];
        for (var i = 0; i < count; i++) buffer[i] = 1f;
        var iters = 20;
        for (var w = 0; w < 3; w++)
            for (var i = 0; i < count; i++) buffer[i] += 1f;
        var sw = Stopwatch.StartNew();
        for (var it = 0; it < iters; it++)
            for (var i = 0; i < count; i++) buffer[i] += 1f;
        sw.Stop();
        var bytesPerIter = (long)count * 8;
        var gbs = bytesPerIter * iters / sw.Elapsed.TotalSeconds / 1e9;
        var sink = buffer[0];
        if (sink == float.NaN) Console.WriteLine("unreachable");
        var bytesPerVisit = 12.0;
        var floorNs = bytesPerVisit / gbs;
        emit($"bandwidth sequential-add GB/s={gbs:F2} bytesPerVisit={bytesPerVisit:F1} floorNsPerVisit={floorNs:F3} bufferBytes={(long)count * 4}");
        return 0;
    }

    static int RunBroadcastProbe(Action<string> emit)
    {
        const int n = 4_000_000;
        var failures = 0;
        GenerateHostile(n, out var hostileRow, out var hostilePhase, out var runs, out var laneVar);
        var sortedRow = (int[])hostileRow.Clone();
        for (var r = 0; r < runs.Length; r++)
        {
            var (start, length, _) = runs[r];
            Array.Sort(sortedRow, start, length);
        }
        var hostileNs = MeasureRunBroadcast(hostileRow, runs, laneVar, n);
        var sortedNs = MeasureRunBroadcast(sortedRow, runs, laneVar, n);
        emit($"runbroadcast 4M hostileNsPerEntity={hostileNs:F3} sortedNsPerEntity={sortedNs:F3} hostileXsorted={hostileNs / Math.Max(sortedNs, 1e-9):F2}");
        var verdictPass = sortedNs < hostileNs * 0.9;
        if (!verdictPass) failures++;
        emit($"{(verdictPass ? "PASS" : "FAIL")} runbroadcast sorted-beats-hostile hostile={hostileNs:F3} sorted={sortedNs:F3}");
        emit("runbroadcast verdict: fix-by-sorting (World does row-sorted runs); unsorted RunBroadcast dropped for World path");
        return failures;
    }

    static double MeasureRunBroadcast(int[] sortedRow, (int Start, int Length, ushort Phase)[] runs, float[] lane, int n)
    {
        var acc = new float[n + 37 + 64];
        for (var f = 0; f < 5; f++) Kernels.RunBroadcast(acc, 37, sortedRow, runs, lane, f, Duration, 1);
        var ticksPerRound = 10;
        var samples = new double[3];
        double sink = 0;
        for (var r = 0; r < 3; r++)
        {
            Array.Clear(acc, 37, n);
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticksPerRound; f++) Kernels.RunBroadcast(acc, 37, sortedRow, runs, lane, f, Duration, 1);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / ticksPerRound / n;
            sink += Checker.HashSlice(acc, 37, n) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"runbroadcast sink={sink:F0}");
        return samples[0];
    }

    static void GenerateHostile(int n, out int[] row, out ushort[] phase, out (int Start, int Length, ushort Phase)[] runs, out float[] laneVar)
    {
        laneVar = Data.VaryingLane(Duration);
        phase = new ushort[n];
        row = new int[n];
        for (var e = 0; e < n; e++)
        {
            phase[e] = (ushort)((e * 37 + 11) % Duration);
            row[e] = (int)((e * 97L) % n);
        }
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
        var runList = new List<(int, int, ushort)>();
        var i = 0;
        while (i < n)
        {
            var j = i + 1;
            while (j < n && sortedPhase[j] == sortedPhase[i]) j++;
            runList.Add((i, j - i, sortedPhase[i]));
            i = j;
        }
        row = sortedRow;
        runs = runList.ToArray();
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
            ? Path.Combine(dir, "scale.txt")
            : Path.Combine(root, "benchmarks", "PhaseClock", "results", "scale.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        Console.WriteLine($"results written: {path}");
    }
}
