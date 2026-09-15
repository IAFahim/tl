using System.Diagnostics;
using Tl;
using Tl.Gen.Tlb;

namespace ManyEntities;

// Many entities, one shared timeline asset, every entity at its own clock position.
// Both sides run the SAME workload and must produce identical checksums:
//
//   lane  the shipped typed lane: Timeline<T>.Seek(positions, forward).Apply(values, cycles)
//         over BakedLane effect tables bound once per asset
//   hand  the per-asset SoA pattern from docs/playback-tables-design.md: precompute the
//         frame value per position once, then sweep flat arrays by hand
//
// The hand side is the ceiling; the lane is the shipped path. Receipts live on the issue.

internal static class Program
{
    const int N = 200_000;
    const int Ticks = 60;
    const int WarmupPasses = 8;
    const int Runs = 5;

    static int Main()
    {
        var moveValues = Precompute<MoveTrack, MoveClip>(Bake("move64.json"), 64, f => f.Clip.Amount * f.Track.Mult + f.TimelineTick);
        var pulseValue = Precompute<PulseTrack, PulseClip>(Bake("pulse.json"), 1, f => f.Clip.Amount * f.Track.Power)[0];
        var windowValues = Precompute<WindowTrack, WindowClip>(Bake("window.json"), 20, f => f.Clip.Amount * f.Track.Power);

        var ok = true;
        ok &= Sweep(moveValues);
        ok &= Pulse(pulseValue);
        ok &= Churn(windowValues);
        return ok ? 0 : 1;
    }

    static byte[] Bake(string path) => TimelineBaker.BakeJson(File.ReadAllText(path));

    static float[] Precompute<TTrack, TClip>(byte[] baked, int duration, Func<Frame<TTrack, TClip>, float> value)
        where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        using var asset = TimelineAsset.Load(baked);
        var reference = asset.Reference;
        var table = new float[duration];
        for (uint p = 0; p < duration; p++)
            foreach (var frame in Timeline.Query<TTrack, TClip>(new TimelineComponent(reference) { Position = p }))
                table[p] = value(frame);
        return table;
    }

    static (double laneMs, double handMs, bool parity) Measure(Func<long> reset, Func<long> lanePass, Func<long> handPass)
    {
        var laneBest = double.MaxValue;
        long laneSum = 0;
        for (var run = 0; run < Runs; run++)
        {
            reset();
            var sw = Stopwatch.StartNew();
            laneSum = lanePass();
            sw.Stop();
            laneBest = Math.Min(laneBest, sw.Elapsed.TotalMilliseconds);
        }
        var handBest = double.MaxValue;
        long handSum = 0;
        for (var run = 0; run < Runs; run++)
        {
            reset();
            var sw = Stopwatch.StartNew();
            handSum = handPass();
            sw.Stop();
            handBest = Math.Min(handBest, sw.Elapsed.TotalMilliseconds);
        }
        return (laneBest, handBest, laneSum == handSum);
    }

    static bool Report(string lane, double laneMs, double handMs, bool parity, double rowsPerPass)
    {
        var l = laneMs * 1e6 / rowsPerPass;
        var h = handMs * 1e6 / rowsPerPass;
        Console.WriteLine($"{lane,-8} lane {laneMs,8:F2} ms/pass ({l,7:F2} ns/row)   hand {handMs,7:F2} ms/pass ({h,6:F2} ns/row)   ratio {laneMs / handMs,5:F1}x   parity {(parity ? "OK" : "FAIL")}");
        return parity;
    }

    // ---------------- lane 1: sweep — staggered clocks on a shared 64-tick loop ----------------

    static bool Sweep(float[] values)
    {
        using var asset = TimelineAsset.Load(Bake("move64.json"));
        BakedLane<MoveTrack, MoveClip>.Bind(asset);

        var positions = new ushort[N];
        var cycles = new long[N];
        var laneValues = new float[N];
        var handValues = new float[N];

        long Reset()
        {
            for (var i = 0; i < N; i++) { positions[i] = (ushort)(i % 64); cycles[i] = 0; }
            Array.Clear(laneValues);
            Array.Clear(handValues);
            return 0;
        }
        long LanePass()
        {
            for (var t = 0; t < Ticks; t++)
                Timeline<BakedLane<MoveTrack, MoveClip>>.Seek(positions, true).Apply(laneValues, cycles);
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += (long)laneValues[i]; c += cycles[i]; }
            return a ^ c * 31;
        }
        long HandPass()
        {
            for (var t = 0; t < Ticks; t++)
                for (var i = 0; i < N; i++)
                {
                    var p = (int)positions[i];
                    handValues[i] += values[p];
                    var np = p + 1;
                    if (np == 64u) { np = 0; cycles[i]++; }
                    positions[i] = (ushort)np;
                }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += (long)handValues[i]; c += cycles[i]; }
            return a ^ c * 31;
        }

        for (var w = 0; w < WarmupPasses; w++) { Reset(); LanePass(); Reset(); HandPass(); }
        var (lMs, hMs, parity) = Measure(Reset, LanePass, HandPass);
        return Report("sweep", lMs, hMs, parity, N * (double)Ticks);
    }

    // ---------------- lane 2: pulse — duration-1 looping, constant frame ----------------

    static bool Pulse(float value)
    {
        using var asset = TimelineAsset.Load(Bake("pulse.json"));
        BakedLane<PulseTrack, PulseClip>.Bind(asset);

        var positions = new ushort[N];
        var cycles = new long[N];
        var laneValues = new float[N];
        var handValues = new float[N];

        long Reset()
        {
            Array.Clear(positions);
            Array.Clear(cycles);
            Array.Clear(laneValues);
            Array.Clear(handValues);
            return 0;
        }
        long LanePass()
        {
            for (var t = 0; t < Ticks; t++)
                Timeline<BakedLane<PulseTrack, PulseClip>>.Seek(positions, true).Apply(laneValues, cycles);
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += (long)laneValues[i]; c += cycles[i]; }
            return a ^ c * 31;
        }
        long HandPass()
        {
            for (var t = 0; t < Ticks; t++)
                for (var i = 0; i < N; i++) { handValues[i] += value; cycles[i]++; }
            long a = 0, c = 0;
            for (var i = 0; i < N; i++) { a += (long)handValues[i]; c += cycles[i]; }
            return a ^ c * 31;
        }

        for (var w = 0; w < WarmupPasses; w++) { Reset(); LanePass(); Reset(); HandPass(); }
        var (lMs, hMs, parity) = Measure(Reset, LanePass, HandPass);
        return Report("pulse", lMs, hMs, parity, N * (double)Ticks);
    }

    // ---------------- lane 3: churn — 20-tick windows spawning and retiring every pass (parry-window pattern) ----------------

    static bool Churn(float[] values)
    {
        const int SpawnPerPass = 20_000;
        const int Passes = 50;
        const int Capacity = 500_000;
        using var asset = TimelineAsset.Load(Bake("window.json"));
        BakedLane<WindowTrack, WindowClip>.Bind(asset);

        var positions = new ushort[Capacity];
        var cycles = new long[Capacity];
        var laneValues = new float[Capacity];
        var handValues = new float[Capacity];
        var count = 0;
        long retiredTotal = 0;

        long Reset()
        {
            count = 0;
            retiredTotal = 0;
            Array.Clear(laneValues);
            Array.Clear(handValues);
            return 0;
        }
        long LanePass()
        {
            var retired = 0;
            for (var p = 0; p < Passes; p++)
            {
                for (var s = 0; s < SpawnPerPass; s++) { positions[count + s] = 0; cycles[count + s] = 0; laneValues[count + s] = 0; }
                count += SpawnPerPass;
                Timeline<BakedLane<WindowTrack, WindowClip>>.Seek(positions.AsSpan(0, count), true).Apply(laneValues.AsSpan(0, count), cycles.AsSpan(0, count));
                for (var i = 0; i < count; )
                {
                    if (positions[i] < 20u) { i++; continue; }
                    count--;
                    positions[i] = positions[count]; cycles[i] = cycles[count]; laneValues[i] = laneValues[count];
                    retired++;
                }
            }
            retiredTotal += retired;
            long a = 0;
            for (var i = 0; i < count; i++) a += (long)laneValues[i];
            return a ^ retiredTotal * 31 ^ count;
        }
        long HandPass()
        {
            var retired = 0;
            for (var p = 0; p < Passes; p++)
            {
                for (var s = 0; s < SpawnPerPass; s++) { positions[count + s] = 0; cycles[count + s] = 0; handValues[count + s] = 0; }
                count += SpawnPerPass;
                for (var i = 0; i < count; )
                {
                    var pos = (int)positions[i];
                    handValues[i] += values[pos];
                    var np = pos + 1;
                    if (np == 20u)
                    {
                        count--;
                        positions[i] = positions[count]; cycles[i] = cycles[count]; handValues[i] = handValues[count];
                        retired++;
                        continue;
                    }
                    positions[i] = (ushort)np;
                    i++;
                }
            }
            retiredTotal += retired;
            long a = 0;
            for (var i = 0; i < count; i++) a += (long)handValues[i];
            return a ^ retiredTotal * 31 ^ count;
        }

        for (var w = 0; w < 3; w++) { Reset(); LanePass(); Reset(); HandPass(); }
        var (lMs, hMs, parity) = Measure(Reset, LanePass, HandPass);
        Console.WriteLine($"churn: spawned {SpawnPerPass * Passes:N0} windows over {Passes} passes, steady ~{20_000 * 20:N0} live rows");
        return Report("churn", lMs, hMs, parity, 20_000 * 20 * (double)Passes);
    }
}
