using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhaseClock;

/// <summary>
/// Cost decomposition of World.Tick at shapeA-sparse-1M-1 (capacity 1M, 1M playbacks,
/// 1 timeline, 1 full-window step, mixed phases (i*37+11)%1024), mirroring
/// ScaleBench SetupShape/MeasureShape exactly (same library spec, same row/phase
/// generation, same oracle). Sections:
///  (a) steady tick vs membership-churn tick (Stop+Start forces BuildRowMajor);
///      steady-state build residue is 0 when _sortedDirty stays false.
///  (b) prepare: (timelineDelta + RefreshStepPlanes) scale with timelines+steps,
///      not playbacks; measured via a 1-playback world on 1-timeline vs
///      150-timeline libraries (delta per extra step) and a 1-playback tick.
///  (c) ApplyRowRange split into additive replica kernels at 1M scale:
///      outer stream / acc roundtrip / tick+window checks / gather /
///      bounds-check cost / second (_applied) store / cache-miss hostile order /
///      uniform-phase broadcast alternative / PlaybackIndex hash probe.
///  (d) sync/copy: alloc per tick at workers=1/8 and Parallel dispatch variants.
/// The replica kernel must hash-exact-match ScaleBench.OracleHash, otherwise the
/// decomposition is invalid and the probe exits non-zero.
/// </summary>
public static unsafe class ProfileBench
{
    const int Duration = 1024;
    const int Capacity = 1_000_000;
    const int Rows = 1_000_000;
    const int PerRow = 1;
    const int N = 1_000_000;

    static readonly FieldInfo? WorkersField =
        typeof(Tl.World).GetField("_workers", BindingFlags.NonPublic | BindingFlags.Instance);

    static void SetWorkers(Tl.World world, int workers) => WorkersField?.SetValue(world, workers);

    public static int Run()
    {
        var failures = 0;
        var lane = Data.VaryingLane(Duration);
        var total = Rows * PerRow;

        var lib = ScaleBench.BuildLibrary(PerRow, Duration, lane);
        using (lib)
        {
            var rowOf = new int[total];
            var phaseOf = new ushort[total];
            var timelineOf = new Tl.TimelineId[total];
            var g = 0;
            for (var r = 0; r < Rows; r++)
                for (var k = 0; k < PerRow; k++)
                {
                    rowOf[g] = r;
                    phaseOf[g] = (ushort)((g * 37 + 11) % Duration);
                    timelineOf[g] = new Tl.TimelineId((ushort)(k + 1));
                    g++;
                }
            var oracle = ScaleBench.OracleHash(lane, rowOf, phaseOf, Capacity, 5);
            Console.WriteLine($"profile oracle={oracle:x16}");

            // (a) steady tick, workers=1/8 + allocation.
            var (steady1, alloc1) = MeasureWorld(lib, rowOf, phaseOf, timelineOf, 1, "world-steady-1");
            var (steady8, alloc8) = MeasureWorld(lib, rowOf, phaseOf, timelineOf, 8, "world-steady-8");
            Console.WriteLine($"profile world-steady w=1 nsPerEntity={steady1:F4} allocPerTick={alloc1}");
            Console.WriteLine($"profile world-steady w=8 nsPerEntity={steady8:F4} allocPerTick={alloc8}");

            // (a) churn tick: Stop+Start one playback each tick forces rebuild + request apply.
            var churnNs = MeasureChurn(lib, rowOf, phaseOf, timelineOf, 1);
            Console.WriteLine($"profile world-churn-1tick nsPerTick={churnNs:F1} buildResiduePerEntity={(churnNs - steady1 * N) / N:F4}");

            // (b) prepare: 1-playback ticks on 1-timeline vs 150-timeline libs.
            var oneTimelineUs = MeasureOnePlayback(lib, Capacity, single: true);
            var manyLib = ScaleBench.BuildLibrary(150, Duration, lane);
            using (manyLib)
            {
                var manyUs = MeasureOnePlayback(manyLib, Capacity, single: true);
                var perExtraStep = (manyUs - oneTimelineUs) / 149.0;
                Console.WriteLine($"profile prepare oneTk_1tl_ns={oneTimelineUs * 1000:F1} oneTk_150tl_ns={manyUs * 1000:F1} perStepNs={perExtraStep * 1000:F1} prepareAt1StepPerEntityNs={(oneTimelineUs * 1000) / N:F5}");
            }

            // (c) replica kernels.
            var activeRows = new int[N];
            var rowStart = new int[N + 1];
            var visitFlat = new int[N];
            var visitPhaseMod = new ushort[N];
            var acc = new float[Capacity];
            var applied = new float[Capacity];
            for (var i = 0; i < N; i++)
            {
                activeRows[i] = i;
                rowStart[i] = i;
                visitFlat[i] = 0;
                visitPhaseMod[i] = phaseOf[i];
            }
            rowStart[N] = N;
            // Plane for step 0 (forward). Recompute from the library exactly as
            // RefreshStepPlanes does: ForwardPlane(globalStep 0).
            var plane = lib.ForwardPlane(0);
            var planePtr = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(plane));
            var planeLen = plane.Length;
            if (planeLen != Duration) Console.WriteLine($"profile WARN planeLen={planeLen}");
            var delta = new int[1];
            var durations = new int[1] { Duration };
            var stepTimeline = new int[1] { 1 };
            var stepWinStart = new int[1] { 0 };
            var stepWinEnd = new int[1] { Duration };

            var kOuter = MeasureKernel("k-outer", () =>
            {
                var sum = 0;
                for (var i = 0; i < N; i++) sum += activeRows[i] + rowStart[i] + rowStart[i + 1];
                return sum;
            });
            Console.WriteLine($"profile k-outer-stream nsPerEntity={kOuter:F4}");

            var kAcc = MeasureKernel("k-acc", () =>
            {
                var sum = 0f;
                for (var i = 0; i < N; i++)
                {
                    var v = acc[activeRows[i]];
                    v += 0f;
                    acc[activeRows[i]] = v;
                    sum += rowStart[i] + rowStart[i + 1];
                }
                return sum;
            });
            Console.WriteLine($"profile k-acc-roundtrip nsPerEntity={kAcc:F4} deltaVsOuter={(kAcc - kOuter):F4}");

            var kChecks = MeasureKernel("k-checks", () =>
            {
                var sum = 0;
                for (var i = 0; i < N; i++)
                {
                    var flat = visitFlat[i];
                    var tlIdx = stepTimeline[flat] - 1;
                    var dur = durations[tlIdx];
                    var tick = visitPhaseMod[i] + delta[0];
                    if (tick >= dur) tick -= dur;
                    if (tick < 0) tick += dur;
                    if (tick < stepWinStart[flat]) continue;
                    if (tick >= stepWinEnd[flat]) continue;
                    sum += tick + activeRows[i] + rowStart[i];
                }
                return sum;
            });
            Console.WriteLine($"profile k-checks nsPerEntity={kChecks:F4} deltaVsAcc={(kChecks - kAcc):F4}");

            // Full original-shape replica: bounds-checked, double store, per-row loop.
            // Run 5 ticks like the oracle and hash to prove exactness vs OracleHash.
            void OriginalTick(int frame)
            {
                delta[0] = frame % Duration;
                for (var ai = 0; ai < N; ai++)
                {
                    var row = activeRows[ai];
                    var start = rowStart[ai];
                    var end = rowStart[ai + 1];
                    var accVal = acc[row];
                    for (var v = start; v < end; v++)
                    {
                        var flat = visitFlat[v];
                        var tlIdx = stepTimeline[flat] - 1;
                        var dur = durations[tlIdx];
                        var tick = visitPhaseMod[v] + delta[tlIdx];
                        if (tick >= dur) tick -= dur;
                        if (tick < 0) tick += dur;
                        if (tick < stepWinStart[flat]) continue;
                        if (tick >= stepWinEnd[flat]) continue;
                        var value = planePtr[tick - stepWinStart[flat]];
                        accVal += value;
                    }
                    acc[row] = accVal;
                    applied[row] = accVal;
                }
            }
            Array.Clear(acc, 0, acc.Length);
            Array.Clear(applied, 0, applied.Length);
            for (var f = 1; f <= 5; f++) OriginalTick(f);
            var replicaHash = Checker.HashSlice(acc, 0, Capacity);
            var replicaOk = replicaHash == oracle;
            Console.WriteLine($"{(replicaOk ? "PASS" : "FAIL")} profile replica-original-vs-oracle replica={replicaHash:x16}");
            if (!replicaOk) failures++;

            var kOriginal = MeasureKernel("k-original", () => { OriginalTick(6); return acc[0] + applied[0]; });
            Console.WriteLine($"profile k-original-bounds-double nsPerEntity={kOriginal:F4}");

            // Same logic, raw pointers (no bounds checks), still double store.
            void PointerTick(int frame, bool doubleStore)
            {
                delta[0] = frame % Duration;
                var accPtr = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(acc));
                var appPtr = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(applied));
                var ar = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(activeRows));
                var rs = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rowStart));
                var vf = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(visitFlat));
                var vp = (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(visitPhaseMod));
                var stTl = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(stepTimeline));
                var durs = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(durations));
                var dlt = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(delta));
                var ws = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(stepWinStart));
                var we = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(stepWinEnd));
                for (var ai = 0; ai < N; ai++)
                {
                    var row = ar[ai];
                    var start = rs[ai];
                    var end = rs[ai + 1];
                    var accVal = accPtr[row];
                    for (var v = start; v < end; v++)
                    {
                        var flat = vf[v];
                        var tlIdx = stTl[flat] - 1;
                        var dur = durs[tlIdx];
                        var tick = vp[v] + dlt[tlIdx];
                        if (tick >= dur) tick -= dur;
                        if (tick < 0) tick += dur;
                        if (tick < ws[flat]) continue;
                        if (tick >= we[flat]) continue;
                        accVal += planePtr[tick - ws[flat]];
                    }
                    accPtr[row] = accVal;
                    if (doubleStore) appPtr[row] = accVal;
                }
            }
            var kPtrDouble = MeasureKernel("k-ptr-double", () => { PointerTick(7, true); return acc[0] + applied[0]; });
            Console.WriteLine($"profile k-ptr-double nsPerEntity={kPtrDouble:F4} boundsCheckCost={(kOriginal - kPtrDouble):F4}");
            var kPtrSingle = MeasureKernel("k-ptr-single", () => { PointerTick(8, false); return acc[0]; });
            Console.WriteLine($"profile k-ptr-single nsPerEntity={kPtrSingle:F4} appliedStoreCost={(kPtrDouble - kPtrSingle):F4}");
            // Hoisted single-flat scalar (World plan): loop-invariant tick data.
            var phaseInt = new int[N];
            for (var i = 0; i < N; i++) phaseInt[i] = visitPhaseMod[i];
            void HoistedScalarTick(int frame)
            {
                var dur = Duration;
                var delta2 = frame % Duration;
                var ws = 0; var winLen = Duration; var len = planeLen;
                var ap = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(acc));
                var rp = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(activeRows));
                var sp = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rowStart));
                var pp = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(phaseInt));
                var start = sp[0];
                for (var ai = 0; ai < N; ai++)
                {
                    var row = rp[ai];
                    var end = sp[ai + 1];
                    var accVal = ap[row];
                    for (var v = start; v < end; v++)
                    {
                        var t = pp[v] + delta2;
                        t += t < 0 ? dur : 0;
                        t -= t >= dur ? dur : 0;
                        if ((uint)(t - ws) >= (uint)winLen) continue;
                        accVal += planePtr[t - ws];
                    }
                    ap[row] = accVal;
                    start = end;
                }
            }
            Array.Clear(acc, 0, acc.Length);
            for (var f = 1; f <= 5; f++) HoistedScalarTick(f);
            var hoistedHash = Checker.HashSlice(acc, 0, Capacity);
            Console.WriteLine($"{(hoistedHash == oracle ? "PASS" : "FAIL")} profile replica-hoisted-vs-oracle {hoistedHash:x16}");
            if (hoistedHash != oracle) failures++;
            var kHoisted = MeasureKernel("k-hoisted", () => { HoistedScalarTick(9); return acc[0]; });
            Console.WriteLine($"profile k-hoisted-scalar nsPerEntity={kHoisted:F4}");
            // Vector hoisted (contiguous rows, i.e. row == visit index).
            void VectorTick(int frame)
            {
                var dur = Duration;
                var delta2 = frame % Duration;
                var ap = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(acc));
                var pp = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(phaseInt));
                var i = 0;
                var vd = new System.Numerics.Vector<int>(delta2);
                var vdur = new System.Numerics.Vector<int>(dur);
                for (; i + System.Numerics.Vector<int>.Count <= N; i += System.Numerics.Vector<int>.Count)
                {
                    var ph = System.Numerics.Vector.LoadUnsafe(ref pp[i]);
                    var t = ph + vd;
                    t = System.Numerics.Vector.ConditionalSelect(
                        System.Numerics.Vector.LessThan(t, vdur), t, t - vdur);
                    var v = ScalarGather(planePtr, t);
                    System.Numerics.Vector.StoreUnsafe(
                        System.Numerics.Vector.LoadUnsafe(ref ap[i]) + v, ref ap[i]);
                }
                for (; i < N; i++)
                {
                    var t = phaseInt[i] + delta2;
                    if (t >= dur) t -= dur;
                    acc[i] += planePtr[t];
                }
            }
            static System.Numerics.Vector<float> ScalarGather(float* basePtr, System.Numerics.Vector<int> t)
            {
                Span<int> idx = stackalloc int[8];
                t.CopyTo(idx);
                var v = new float[8];
                for (var k = 0; k < 8; k++) v[k] = basePtr[idx[k]];
                return new System.Numerics.Vector<float>(v);
            }

            Array.Clear(acc, 0, acc.Length);
            for (var f = 1; f <= 5; f++) VectorTick(f);
            var vecHash = Checker.HashSlice(acc, 0, Capacity);
            Console.WriteLine($"{(vecHash == oracle ? "PASS" : "FAIL")} profile replica-vector-vs-oracle {vecHash:x16}");
            if (vecHash != oracle) failures++;
            var kVec = MeasureKernel("k-vec", () => { VectorTick(10); return acc[0]; });
            Console.WriteLine($"profile k-vector-hoisted nsPerEntity={kVec:F4}");

            // Exact copy of World.ApplyRowRangeHoisted inner body.
            int WorldHoisted(int frame)
            {
                var visits = 0;
                var accP = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(acc));
                var rowsP = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(activeRows));
                var startP = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rowStart));
                var phaseP2 = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(phaseInt));
                var plane = planePtr; var len = planeLen; var global = 0;
                var durX = Duration; var delta2 = frame % Duration; var ws = 0; var winLen = Duration;
                var start = startP[0];
                for (var ai = 0; ai < N; ai++)
                {
                    var row = rowsP[ai];
                    var end = startP[ai + 1];
                    var accVal = accP[row];
                    for (var v = start; v < end; v++)
                    {
                        var t = phaseP2[v] + delta2;
                        t += t < 0 ? durX : 0;
                        t -= t >= durX ? durX : 0;
                        if ((uint)(t - ws) >= (uint)winLen) continue;
                        if (durX <= 0) continue;
                        visits++;
                        float value;
                        if (len == 0) value = laneX(global, frame, t);
                        else if (len == 1) value = plane[0];
                        else value = plane[t - ws];
                        accVal += value;
                    }
                    accP[row] = accVal;
                    start = end;
                }
                return visits;
            }
            static float laneX(int global, int frame, int t) => 0f;
            Array.Clear(acc, 0, acc.Length);
            for (var f = 1; f <= 5; f++) WorldHoisted(f);
            var whHash = Checker.HashSlice(acc, 0, Capacity);
            Console.WriteLine($"{(whHash == oracle ? "PASS" : "FAIL")} profile replica-worldhoisted-vs-oracle {whHash:x16} visits={WorldHoisted(6)}");
            if (whHash != oracle) failures++;
            var kWorldHoisted = MeasureKernel("k-worldhoisted", () => { WorldHoisted(11); return acc[0]; });
            Console.WriteLine($"profile k-worldhoisted nsPerEntity={kWorldHoisted:F4}");

            // Cache-miss hostile: shuffled row order (same visits, random acc rows).
            var shuffle = new int[N];
            for (var i = 0; i < N; i++) shuffle[i] = i;
            var rng = new Random(1234);
            for (var i = N - 1; i > 0; i--) { var j = rng.Next(i + 1); (shuffle[i], shuffle[j]) = (shuffle[j], shuffle[i]); }
            // visits stay row-ordered but acc seats are the shuffle: hostile acc + host rows
            var kHostile = MeasureKernel("k-hostile", () =>
            {
                delta[0] = 9;
                float sink = 0f;
                for (var i = 0; i < N; i++)
                {
                    var row = shuffle[i];
                    var v = i;
                    var tick = visitPhaseMod[v] + delta[0];
                    if (tick >= Duration) tick -= Duration;
                    sink += acc[row] + planePtr[tick];
                    acc[row] = sink;
                }
                return sink;
            });
            Console.WriteLine($"profile k-hostile-random nsPerEntity={kHostile:F4} cacheMissDeltaVsPtrSingle={(kHostile - kPtrSingle):F4}");

            // Uniform-phase broadcast (BroadcastDense) vs row-ordered gather at 1M.
            var uniAcc = new float[Capacity];
            var kBcast = MeasureKernel("k-broadcast", () =>
            {
                var v = new System.Numerics.Vector<float>(planePtr[0]);
                var i = 0;
                for (; i + System.Numerics.Vector<float>.Count <= N; i += System.Numerics.Vector<float>.Count)
                    System.Numerics.Vector.StoreUnsafe(
                        System.Numerics.Vector.LoadUnsafe(ref uniAcc[i]) + v, ref uniAcc[i]);
                for (; i < N; i++) uniAcc[i] += planePtr[0];
                return uniAcc[0];
            });
            Console.WriteLine($"profile k-broadcast-uniform nsPerEntity={kBcast:F4}");

            // Hash probe: PlaybackIndex.Find cost (setup path, not steady tick).
            var hashNs = MeasureHash(lib);
            Console.WriteLine($"profile k-hash-find nsPerLookup={hashNs:F4}");

            // Parallel dispatch variants (alloc + time at workers=8).
            MeasureParallel(lib, rowOf, phaseOf, timelineOf);

            // Final cost table (steady, workers=1): measured components sum
            // to the measured world tick time modulo measurement noise.
            var scaffoldPerEntity = (oneTimelineUs * 1000) / N;
            var sumParts = scaffoldPerEntity + kWorldHoisted;
            Console.WriteLine("profile COST-TABLE @1M-1 workers=1 ns/entity (measured):");
            Console.WriteLine($"  (a) BuildRowMajor residue steady {0.0:F4} (rebuilt only on membership change; churn delta below)");
            Console.WriteLine($"  (b) scaffold+prepare(pending+delta+plane+report) {scaffoldPerEntity:F4}");
            Console.WriteLine($"  (c) ApplyRowRange hoisted kernel (exact-copy replica) {kWorldHoisted:F4}");
            Console.WriteLine($"  (d) sync/copy overheads steady {steady1 - sumParts:F4} [total - (b)+(c)]");
            Console.WriteLine($"  TOTAL measured world {steady1:F4}  sum(parts) {sumParts:F4}  residual {steady1 - sumParts:F4}");
        }
        Console.WriteLine(failures == 0 ? "PROFILE PASS" : $"PROFILE FAIL {failures}");
        return failures == 0 ? 0 : 1;
    }

    static (double NsPerEntity, long AllocPerTick) MeasureWorld(
        Tl.Library lib, int[] rowOf, ushort[] phaseOf, Tl.TimelineId[] timelineOf, int workers, string label)
    {
        using var world = new Tl.World(lib, Capacity);
        SetWorkers(world, workers);
        var req = world.Requests;
        for (var i = 0; i < rowOf.Length; i++)
            req.Start(new Tl.Row(rowOf[i]), timelineOf[i], phaseOf[i]);
        for (var f = 0; f < 5; f++) world.Tick(Tl.Direction.Forward);
        for (var f = 0; f < 10; f++) world.Tick(Tl.Direction.Forward);
        for (var f = 0; f < 200; f++) world.Tick(Tl.Direction.Forward);
        var samples = new double[3];
        double sink = 0;
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < 20; f++) world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / 20 / N;
            sink += Checker.HashSlice(world.Accumulator, 0, Capacity) & 0xff;
        }
        Array.Sort(samples);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++) world.Tick(Tl.Direction.Forward);
        var alloc = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"  ({label} sink={sink:F0})");
        return (samples[0], alloc);
    }

    static double MeasureChurn(Tl.Library lib, int[] rowOf, ushort[] phaseOf, Tl.TimelineId[] timelineOf, int workers)
    {
        using var world = new Tl.World(lib, Capacity);
        SetWorkers(world, workers);
        var req = world.Requests;
        for (var i = 0; i < rowOf.Length; i++)
            req.Start(new Tl.Row(rowOf[i]), timelineOf[i], phaseOf[i]);
        for (var f = 0; f < 15; f++) world.Tick(Tl.Direction.Forward);
        // warm the hash indexes so the measured churn is rebuild-only, not EnsureIndexes
        _ = world.IsPlaying(new Tl.Row(500_000), timelineOf[500_000]);
        var req2 = world.Requests;
        var samplesNs = new double[3];
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < 10; f++)
            {
                req2.Stop(new Tl.Row(500_000), timelineOf[500_000]);
                req2.Start(new Tl.Row(500_000), timelineOf[500_000], phaseOf[500_000]);
                world.Tick(Tl.Direction.Forward);
            }
            sw.Stop();
            samplesNs[r] = sw.Elapsed.TotalSeconds * 1e9 / 10;
        }
        Array.Sort(samplesNs);
        return samplesNs[0];
    }

    static double MeasureOnePlayback(Tl.Library lib, int capacity, bool single)
    {
        using var world = new Tl.World(lib, capacity);
        world.Requests.Start(new Tl.Row(0), new Tl.TimelineId(1), 0);
        for (var f = 0; f < 20; f++) world.Tick(Tl.Direction.Forward);
        var samples = new double[5];
        for (var r = 0; r < 5; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < 200; f++) world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e6 / 200; // us per tick
        }
        Array.Sort(samples);
        return samples[0];
    }

    static double MeasureKernel(string label, Func<float> body)
    {
        for (var f = 0; f < 5; f++) body();
        var samples = new double[3];
        float sink = 0f;
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < 20; f++) sink += body();
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / 20 / N;
        }
        Array.Sort(samples);
        if (sink == float.NaN) Console.WriteLine($"unreachable {label}");
        return samples[0];
    }

    static double MeasureHash(Tl.Library lib)
    {
        // Direct probe of PlaybackIndex hash cost via a live world lookup path:
        // time IsPlaying (FindPlain+FindSolo) at 1M scale, warm.
        using var world = new Tl.World(lib, Capacity);
        var req = world.Requests;
        for (var i = 0; i < 100_000; i++) req.Start(new Tl.Row(i), new Tl.TimelineId(1), 0);
        world.Tick(Tl.Direction.Forward);
        world.Tick(Tl.Direction.Forward); // ensures indexes if needed
        var hits = 0;
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 100_000; i++) if (world.IsPlaying(new Tl.Row(i), new Tl.TimelineId(1))) hits++;
        sw.Stop();
        var ns = sw.Elapsed.TotalSeconds * 1e9 / 100_000;
        if (hits == -1) Console.WriteLine("unreachable");
        return ns;
    }

    static void MeasureParallel(Tl.Library lib, int[] rowOf, ushort[] phaseOf, Tl.TimelineId[] timelineOf)
    {
        // V1: current ParallelApply.ForEachChunk (alloc probe only).
        using var world = new Tl.World(lib, Capacity);
        SetWorkers(world, 8);
        var req = world.Requests;
        for (var i = 0; i < rowOf.Length; i++)
            req.Start(new Tl.Row(rowOf[i]), timelineOf[i], phaseOf[i]);
        for (var f = 0; f < 15; f++) world.Tick(Tl.Direction.Forward);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++) world.Tick(Tl.Direction.Forward);
        var alloc = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"profile parallel-current w=8 allocPerTick={alloc}");
        // V2: cached ParallelOptions + non-capturing static body shape probe.
        var opts = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        var acc = new float[Capacity];
        var rows = new int[N];
        for (var i = 0; i < N; i++) rows[i] = i;
        var b2 = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++)
            Parallel.For(0, 8, opts, i =>
            {
                var start = i * (N / 8);
                var end = start + N / 8;
                for (var j = start; j < end; j++) acc[rows[j]] += 1f;
            });
        var alloc2 = (GC.GetAllocatedBytesForCurrentThread() - b2) / 10;
        Console.WriteLine($"profile parallel-cachedOpts allocPerTick={alloc2}");
        sAcc = acc;
        sRows = rows;
        var b3 = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++)
            Parallel.For(0, 8, opts, static (i, state) =>
            {
                var start = i * (N / 8);
                var end = start + N / 8;
                for (var j = start; j < end; j++) sAcc[sRows[j]] += 1f;
            });
        var alloc3 = (GC.GetAllocatedBytesForCurrentThread() - b3) / 10;
        Console.WriteLine($"profile parallel-static allocPerTick={alloc3}");
        MeasurePool(acc, rows);
    }
    static float[] sAcc = new float[0];
    static int[] sRows = new int[0];
    sealed class SpinPool : IDisposable
    {
        readonly ManualResetEventSlim _go = new(false);
        readonly ManualResetEventSlim _done = new(false);
        readonly int[] _start;
        readonly int[] _end;
        readonly Thread[] _threads;
        Action<int, int>? _body;
        volatile bool _closing;
        int _remain;
        public SpinPool(int workers, int total)
        {
            _start = new int[workers];
            _end = new int[workers];
            var baseSize = total / workers;
            var extra = total % workers;
            for (var i = 0; i < workers; i++)
            {
                _start[i] = i * baseSize + (i < extra ? i : extra);
                _end[i] = _start[i] + baseSize + (i < extra ? 1 : 0);
            }
            _threads = new Thread[workers - 1];
            for (var i = 0; i < _threads.Length; i++)
            {
                var idx = i + 1;
                var t = new Thread(() => WorkerLoop(idx)) { IsBackground = true };
                _threads[i] = t;
                t.Start();
            }
        }
        void WorkerLoop(int idx)
        {
            while (true)
            {
                _go.Wait();
                if (_closing) return;
                _body!(_start[idx], _end[idx]);
                if (Interlocked.Decrement(ref _remain) == 0) _done.Set();
            }
        }
        public void Run(Action<int, int> body)
        {
            _body = body;
            _remain = _threads.Length;
            _done.Reset();
            _go.Set();
            body(_start[0], _end[0]);
            _done.Wait();
            _go.Reset();
        }
        public void Dispose()
        {
            _closing = true;
            _go.Set();
            foreach (var t in _threads) t.Join();
        }
    }

    static void MeasurePool(float[] acc, int[] rows)
    {
        using var pool = new SpinPool(8, N);
        pool.Run((s, e) => { for (var j = s; j < e; j++) acc[rows[j]] += 1f; });
        var samples = new double[3];
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < 20; f++) pool.Run((s, e) => { for (var j = s; j < e; j++) acc[rows[j]] += 1f; });
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / 20;
        }
        Array.Sort(samples);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 10; f++) pool.Run((s, e) => { for (var j = s; j < e; j++) acc[rows[j]] += 1f; });
        var alloc = (GC.GetAllocatedBytesForCurrentThread() - before) / 10;
        Console.WriteLine($"profile parallel-spinpool nsPerDispatch={samples[0]:F0} allocPerTick={alloc}");
        if (acc[0] == float.NaN) Console.WriteLine("unreachable pool");
    }
}
