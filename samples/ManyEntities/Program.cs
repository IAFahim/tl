using System.Diagnostics;
using Tl;
using Tl.Gen.Tlb;

namespace ManyEntities;

// Many entities, one shared timeline asset, every entity at its own clock position.
// Each lane runs the SAME workload twice and must produce identical checksums:
//
//   facade the shipped library path (Timeline.Rows(...).Tick)        8.8-11.5 ns/row
//   table  the per-asset SoA pattern from docs/playback-tables-design.md:
//          precompute the frame value per position once, then sweep flat arrays
//                                                                    0.50-0.95 ns/row
//
// Receipts (i9-14900K, .NET 10.0.12, best of 5, 200k rows x 60 ticks):
//   sweep 12.7x | pulse 17.6x | watch 10.1x | churn 16.4x   (all parity OK)
// The table side is the shape the playback-tables coordinator (#56) will turn into
// a public API; today it is written out here so the numbers are reproducible.

internal static class Program
{
    const int N = 200_000;
    const int Ticks = 60;
    const int WarmupPasses = 8;
    const int Runs = 5;

    static int Main()
    {
        var moveValues = Precompute<MoveTrack, MoveClip>(Bake("move64.json"), 64, f => f.Clip.Amount * (int)f.Track.Mult + (int)f.TimelineTick);
        var pulseValue = Precompute<PulseTrack, PulseClip>(Bake("pulse.json"), 1, f => f.Clip.Amount * f.Track.Power)[0];
        var watchValues = Precompute<WatchTrack, WatchClip>(Bake("watch.json"), 64, f => f.Clip.Amount * f.Track.Bonus);
        var windowValues = Precompute<WindowTrack, WindowClip>(Bake("window.json"), 20, f => f.Clip.Amount * f.Track.Power);

        var ok = true;
        ok &= Sweep(moveValues);
        ok &= Pulse(pulseValue);
        ok &= Watch(watchValues);
        ok &= Churn(windowValues);
        return ok ? 0 : 1;
    }

    static byte[] Bake(string path) => TimelineBaker.BakeJson(File.ReadAllText(path));

    static int[] Precompute<TTrack, TClip>(byte[] baked, int duration, Func<Frame<TTrack, TClip>, int> value)
        where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        using var asset = TimelineAsset.Load(baked);
        var reference = asset.Reference;
        var table = new int[duration];
        for (uint p = 0; p < duration; p++)
            foreach (var frame in Timeline.Query<TTrack, TClip>(new TimelineComponent(reference) { Position = p }))
                table[p] = value(frame);
        return table;
    }

    static (double facadeMs, double tableMs, bool parity) Measure(
        Func<long> resetFacade, Func<long> facadePass,
        Func<long> resetTable, Func<long> tablePass)
    {
        var facadeBest = double.MaxValue;
        long facadeSum = 0;
        for (var run = 0; run < Runs; run++)
        {
            resetFacade();
            var sw = Stopwatch.StartNew();
            facadeSum = facadePass();
            sw.Stop();
            facadeBest = Math.Min(facadeBest, sw.Elapsed.TotalMilliseconds);
        }
        var tableBest = double.MaxValue;
        long tableSum = 0;
        for (var run = 0; run < Runs; run++)
        {
            resetTable();
            var sw = Stopwatch.StartNew();
            tableSum = tablePass();
            sw.Stop();
            tableBest = Math.Min(tableBest, sw.Elapsed.TotalMilliseconds);
        }
        return (facadeBest, tableBest, facadeSum == tableSum);
    }

    static bool Report(string lane, double facadeMs, double tableMs, bool parity, double rowsPerPass)
    {
        var f = facadeMs * 1e6 / rowsPerPass;
        var t = tableMs * 1e6 / rowsPerPass;
        Console.WriteLine($"{lane,-8} facade {facadeMs,8:F2} ms/pass ({f,7:F2} ns/row)   table {tableMs,7:F2} ms/pass ({t,6:F2} ns/row)   ratio {facadeMs / tableMs,5:F1}x   parity {(parity ? "OK" : "FAIL")}");
        return parity;
    }

    // ---------------- lane 1: sweep — staggered clocks on a shared 64-tick loop ----------------

    static bool Sweep(int[] values)
    {
        using var asset = TimelineAsset.Load(Bake("move64.json"));
        var rows = new TimelineComponent[N];
        var col = new int[N];
        for (var i = 0; i < N; i++) rows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % 64) };

        var positions = new uint[N];
        var cycles = new long[N];
        var tableCol = new int[N];

        long ResetFacade()
        {
            for (var i = 0; i < N; i++) { rows[i].Position = (uint)(i % 64); rows[i].Cycle = 0; }
            Array.Clear(col);
            return 0;
        }
        long FacadePass()
        {
            var query = Timeline.Rows(rows).Write(col);   // cheap per pass: the bind cache makes construction O(1)
            for (var t = 0; t < Ticks; t++) query.Tick((uint)t);
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += col[i]; c += rows[i].Cycle; }
            return a ^ c * 31;
        }
        long ResetTable()
        {
            for (var i = 0; i < N; i++) { positions[i] = (uint)(i % 64); cycles[i] = 0; }
            Array.Clear(tableCol);
            return 0;
        }
        long TablePass()
        {
            for (var t = 0; t < Ticks; t++)
                for (var i = 0; i < N; i++)
                {
                    var p = positions[i];
                    tableCol[i] += values[p];
                    var np = p + 1;
                    if (np == 64u) { np = 0; cycles[i]++; }
                    positions[i] = np;
                }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += tableCol[i]; c += cycles[i]; }
            return a ^ c * 31;
        }

        for (var w = 0; w < WarmupPasses; w++) { ResetFacade(); FacadePass(); ResetTable(); TablePass(); }
        var (fMs, tMs, parity) = Measure(ResetFacade, FacadePass, ResetTable, TablePass);
        return Report("sweep", fMs, tMs, parity, N * (double)Ticks);
    }

    // ---------------- lane 2: pulse — duration-1 looping, constant frame ----------------

    static bool Pulse(int value)
    {
        using var asset = TimelineAsset.Load(Bake("pulse.json"));
        var rows = new TimelineComponent[N];
        var col = new int[N];
        for (var i = 0; i < N; i++) rows[i] = new TimelineComponent(asset.Reference);

        var cycles = new long[N];
        var tableCol = new int[N];

        long ResetFacade()
        {
            for (var i = 0; i < N; i++) { rows[i].Cycle = 0; }
            Array.Clear(col);
            return 0;
        }
        long FacadePass()
        {
            var query = Timeline.Rows(rows).Write(col);
            for (var t = 0; t < Ticks; t++) query.Tick((uint)t);
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += col[i]; c += rows[i].Cycle; }
            return a ^ c * 31;
        }
        long ResetTable()
        {
            Array.Clear(cycles);
            Array.Clear(tableCol);
            return 0;
        }
        long TablePass()
        {
            for (var t = 0; t < Ticks; t++)
                for (var i = 0; i < N; i++)
                {
                    tableCol[i] += value;
                    cycles[i]++;
                }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += tableCol[i]; c += cycles[i]; }
            return a ^ c * 31;
        }

        for (var w = 0; w < WarmupPasses; w++) { ResetFacade(); FacadePass(); ResetTable(); TablePass(); }
        var (fMs, tMs, parity) = Measure(ResetFacade, FacadePass, ResetTable, TablePass);
        return Report("pulse", fMs, tMs, parity, N * (double)Ticks);
    }

    // ---------------- lane 3: watch — every row reads another entity's input (car/player pattern) ----------------

    static bool Watch(int[] values)
    {
        const int Inputs = 1_000;
        using var asset = TimelineAsset.Load(Bake("watch.json"));
        var rows = new TimelineComponent[N];
        var results = new int[N];
        var watchIn = new WatchIn[N];
        for (var i = 0; i < N; i++) { rows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % 64) }; watchIn[i] = new WatchIn(); }

        var inputs = new int[Inputs];
        var positions = new uint[N];
        var cycles = new long[N];
        var tableCol = new int[N];
        var watched = new int[N];
        for (var i = 0; i < N; i++) watched[i] = i * 31 % Inputs;

        long ResetFacade()
        {
            for (var k = 0; k < Inputs; k++) inputs[k] = k;
            for (var i = 0; i < N; i++) { rows[i].Position = (uint)(i % 64); rows[i].Cycle = 0; }
            Array.Clear(results);
            return 0;
        }
        long FacadePass()
        {
            var query = Timeline.Rows(rows).Write(watchIn).Write(results);
            for (var t = 0; t < Ticks; t++)
            {
                for (var i = 0; i < N; i++) watchIn[i].V = inputs[watched[i]];   // gather (per-row indirection)
                query.Tick((uint)t);
                for (var k = 0; k < Inputs; k++) inputs[k]++;
            }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += results[i]; c += rows[i].Cycle; }
            return a ^ c * 31;
        }
        long ResetTable()
        {
            for (var k = 0; k < Inputs; k++) inputs[k] = k;
            for (var i = 0; i < N; i++) { positions[i] = (uint)(i % 64); cycles[i] = 0; }
            Array.Clear(tableCol);
            return 0;
        }
        long TablePass()
        {
            for (var t = 0; t < Ticks; t++)
            {
                for (var i = 0; i < N; i++)
                {
                    var p = positions[i];
                    tableCol[i] = inputs[watched[i]] + values[p];                // fused gather + frame + write
                    var np = p + 1;
                    if (np == 64u) { np = 0; cycles[i]++; }
                    positions[i] = np;
                }
                for (var k = 0; k < Inputs; k++) inputs[k]++;
            }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += tableCol[i]; c += cycles[i]; }
            return a ^ c * 31;
        }

        for (var w = 0; w < WarmupPasses; w++) { ResetFacade(); FacadePass(); ResetTable(); TablePass(); }
        var (fMs, tMs, parity) = Measure(ResetFacade, FacadePass, ResetTable, TablePass);
        return Report("watch", fMs, tMs, parity, N * (double)Ticks);
    }

    // ---------------- lane 4: churn — 20-tick windows spawning and retiring every pass (parry-window pattern) ----------------

    static bool Churn(int[] values)
    {
        const int SpawnPerPass = 20_000;
        const int Passes = 50;
        const int Capacity = 500_000;
        using var asset = TimelineAsset.Load(Bake("window.json"));

        var rows = new TimelineComponent[Capacity];
        var col = new int[Capacity];
        var count = 0;
        long retiredTotal = 0;

        var positions = new uint[Capacity];
        var cycles = new long[Capacity];
        var tableCol = new int[Capacity];
        var tableCount = 0;
        long tableRetiredTotal = 0;

        long ResetFacade()
        {
            count = 0; retiredTotal = 0;
            Array.Clear(col, 0, Capacity);
            return 0;
        }
        long FacadePass()
        {
            var retired = 0;
            for (var p = 0; p < Passes; p++)
            {
                for (var s = 0; s < SpawnPerPass; s++) rows[count + s] = new TimelineComponent(asset.Reference);
                count += SpawnPerPass;
                var live = new TimelineComponent[count];
                Array.Copy(rows, live, count);
                var liveCol = new int[count];
                Array.Copy(col, liveCol, count);
                Timeline.Rows(live).Write(liveCol).Tick(1u);
                var write = 0;
                for (var i = 0; i < count; i++)
                {
                    if (live[i].Position >= 20u) { retired++; continue; }
                    rows[write] = live[i]; col[write] = liveCol[i]; write++;
                }
                count = write;
            }
            retiredTotal += retired;
            long a = 0;
            for (var i = 0; i < count; i++) a += col[i];
            return a ^ retiredTotal * 31 ^ count;
        }
        long ResetTable()
        {
            tableCount = 0; tableRetiredTotal = 0;
            Array.Clear(tableCol, 0, Capacity);
            return 0;
        }
        long TablePass()
        {
            var retired = 0;
            for (var p = 0; p < Passes; p++)
            {
                for (var s = 0; s < SpawnPerPass; s++) { positions[tableCount + s] = 0; cycles[tableCount + s] = 0; }
                tableCount += SpawnPerPass;
                for (var i = 0; i < tableCount; )
                {
                    var pos = positions[i];
                    tableCol[i] += values[pos];
                    var np = pos + 1;
                    if (np == 20u)
                    {
                        tableCount--;
                        positions[i] = positions[tableCount]; cycles[i] = cycles[tableCount]; tableCol[i] = tableCol[tableCount];
                        retired++;
                        continue;                                   // process the swapped-in row this pass
                    }
                    positions[i] = np;
                    i++;
                }
            }
            tableRetiredTotal += retired;
            long a = 0;
            for (var i = 0; i < tableCount; i++) a += tableCol[i];
            return a ^ tableRetiredTotal * 31 ^ tableCount;
        }

        for (var w = 0; w < 3; w++) { ResetFacade(); FacadePass(); ResetTable(); TablePass(); }
        var (fMs, tMs, parity) = Measure(ResetFacade, FacadePass, ResetTable, TablePass);
        Console.WriteLine($"churn: spawned {SpawnPerPass * Passes:N0} windows over {Passes} passes, steady ~{20_000 * 20:N0} live rows");
        return Report("churn", fMs, tMs, parity, 20_000 * 20 * (double)Passes);
    }
}
