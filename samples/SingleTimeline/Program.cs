using System.Diagnostics;
using Tl;
using Tl.Gen.Tlb;

namespace SingleTimeline;

// One timeline, three ways to run it. All three produce identical effects; only speed differs.
//
//   direct      hand-written playback — the ceiling (0.24 ns/tick with one track, 1.41 with three)
//   kernel      Timeline.Rows facade + a tlbake --kernel generated kernel (Kernels.g.cs binds by
//               content hash) — the shipped path (6.68 ns/tick one track, 12.13 three)
//   interpreter the same asset with the metadata word corrupted so no kernel matches — the
//               fallback (7.45 ns/tick one track, 15.17 three)
//
// Receipts (i9-14900K, .NET 10.0.12, best of 5 x 2M ticks; see README):
//   one   track:  direct 0.24 | kernel 6.68 | interpreter 7.45   ns/tick
//   three tracks: direct 1.41 | kernel 12.13 | interpreter 15.17  ns/tick
// In the 16/256-track shapes the same gap widens (benchmarks/Alpha: kernel 59 vs interpreter 71 ns
// at 16 tracks, 967 vs 1,081 ns at 256).

internal static class Program
{
    const int Ticks = 2_000_000;
    const int Warmup = 1_000_000;
    const int Runs = 5;

    static int Main(string[] args)
    {
        return args.Length > 0 && args[0] == "three" ? RunThree() : RunOne();
    }

    static byte[] Bake(string path) => TimelineBaker.BakeJson(File.ReadAllText(path));

    // ---------------- ONE TRACK: damage over a 64-tick looping timeline ----------------

    static int RunOne()
    {
        Console.WriteLine("=== one track (health -= 5 * 2 every tick, 64-tick loop) ===");

        var direct = RunOneDirect();
        var kernel = RunFacade(Bake("one.json"), committedKernelExpected: true);
        var interp = RunFacade(KillKernel(Bake("one.json")), committedKernelExpected: false);

        Console.WriteLine($"checksums: direct={direct.sum} kernel={kernel.sum} interpreter={interp.sum} (all must match)");
        if (direct.sum != kernel.sum || direct.sum != interp.sum) return 1;
        if (!kernel.bound) { Console.WriteLine("kernel binding receipt FAILED"); return 1; }
        Row("direct (hand-written ceiling)", direct.ns);
        Row("facade + kernel (shipped path)", kernel.ns);
        Row("facade + interpreter (fallback)", interp.ns);
        return 0;
    }

    static (int sum, double ns) RunOneDirect()
    {
        int health = 0, position = 0;
        for (var tick = 0; tick < Warmup; tick++) { health -= 10; position = position + 1 == 64 ? 0 : position + 1; }
        var best = double.MaxValue;
        for (var run = 0; run < Runs; run++)
        {
            health = 0; position = 0;
            var sw = Stopwatch.StartNew();
            for (var tick = 0; tick < Ticks; tick++)
            {
                health -= 5 * 2;                                  // the entire timeline, hand-written
                position = position + 1 == 64 ? 0 : position + 1; // clock advance + wrap
            }
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }
        return (health, best * 1e6 / Ticks);
    }

    static (double ns, bool bound, int sum) RunFacade(byte[] baked, bool committedKernelExpected)
    {
        using var asset = TimelineAsset.Load(baked);

        var before = TimelineKernels.Bound;
        var rows = new TimelineComponent[1];
        var health = new int[1];
        rows[0] = new TimelineComponent(asset.Reference);

        var query = Timeline.Rows(rows).Write(health);
        for (var tick = 0; tick < Warmup; tick++) query.Tick((uint)tick);
        var bound = TimelineKernels.Bound > before;
        if (bound != committedKernelExpected) return (0, bound, int.MinValue); // poisons the checksum: caller exits 1

        long sink = 0;
        var best = double.MaxValue;
        var sum = 0;
        for (var run = 0; run < Runs; run++)
        {
            rows[0] = new TimelineComponent(asset.Reference);
            health[0] = 0;
            var sw = Stopwatch.StartNew();
            for (var tick = 0; tick < Ticks; tick++) query.Tick((uint)tick);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
            sum = health[0];
            sink += sum + rows[0].Position;
        }
        return (best * 1e6 / Ticks, bound, sum);
    }

    static byte[] KillKernel(byte[] baked)
    {
        baked[40] ^= 0xA5; // corrupt the metadata offset: the content hash no longer matches any kernel
        return baked;
    }

    // ---------------- THREE TRACKS A-B-C on one timeline, three columns ----------------

    static int RunThree()
    {
        Console.WriteLine("=== three tracks (one timeline, three columns, 64-tick loop) ===");

        var direct = RunThreeDirect();
        var kernel = RunThreeFacade(Bake("three.json"), committedKernelExpected: true);
        var interp = RunThreeFacade(KillKernel(Bake("three.json")), committedKernelExpected: false);

        var match = direct.sum == kernel.sum && direct.sum == interp.sum;
        Console.WriteLine($"checksums match: {match}");
        if (!match) return 1;
        if (!kernel.bound) { Console.WriteLine("kernel binding receipt FAILED"); return 1; }
        Row("direct (hand-written ceiling, 3 clocks)", direct.ns);
        Row("facade + kernel (shipped path)", kernel.ns);
        Row("facade + interpreter (fallback)", interp.ns);
        return 0;
    }

    static ((int, float, long) sum, double ns) RunThreeDirect()
    {
        int a = 0; float b = 0; long c = 0;
        int pa = 0, pb = 0, pc = 0;
        void Pass(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                a -= 5 * 2; pa = pa + 1 == 64 ? 0 : pa + 1;
                b -= 7 * 3f; pb = pb + 1 == 64 ? 0 : pb + 1;
                c -= 11 * 4L; pc = pc + 1 == 64 ? 0 : pc + 1;
            }
        }
        Pass(Warmup);
        var best = double.MaxValue;
        for (var run = 0; run < Runs; run++)
        {
            a = 0; b = 0; c = 0; pa = pb = pc = 0;
            var sw = Stopwatch.StartNew();
            Pass(Ticks);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }
        return ((a, (int)b, c), best * 1e6 / Ticks);
    }

    static (double ns, bool bound, (int, float, long) sum) RunThreeFacade(byte[] baked, bool committedKernelExpected)
    {
        using var asset = TimelineAsset.Load(baked);

        var before = TimelineKernels.Bound;
        var rows = new TimelineComponent[1];
        var ca = new int[1]; var cb = new float[1]; var cc = new long[1];
        rows[0] = new TimelineComponent(asset.Reference);

        var query = Timeline.Rows(rows).Write(ca).Write(cb).Write(cc);
        for (var tick = 0; tick < Warmup; tick++) query.Tick((uint)tick);
        var bound = TimelineKernels.Bound > before;
        if (bound != committedKernelExpected) return (0, bound, (int.MinValue, 0, 0));

        var best = double.MaxValue;
        (int, float, long) sum = default;
        for (var run = 0; run < Runs; run++)
        {
            rows[0] = new TimelineComponent(asset.Reference);
            ca[0] = 0; cb[0] = 0; cc[0] = 0;
            var sw = Stopwatch.StartNew();
            for (var tick = 0; tick < Ticks; tick++) query.Tick((uint)tick);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
            sum = (ca[0], cb[0], cc[0]);
        }
        return (best * 1e6 / Ticks, bound, sum);
    }

    static void Row(string label, double nsPerTick) =>
        Console.WriteLine($"{label,-38} {nsPerTick,9:F2} ns/tick");
}
