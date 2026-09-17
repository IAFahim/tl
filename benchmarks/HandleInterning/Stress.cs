using System.Globalization;
using System.Runtime;

namespace HandleInterning;

unsafe static class Stress
{
    const int Threads = 8;
    static long _sink;

    sealed class Report
    {
        int _failures;
        public int Failures => _failures;

        public void Scenario(string name, Action body)
        {
            Console.WriteLine($"scenario: {name}");
            try
            {
                body();
            }
            catch (Exception e)
            {
                _failures++;
                Console.WriteLine($"  FAIL {name} threw {e.GetType().Name}: {e.Message}");
            }
        }

        public void Check(bool ok, string name, string detail)
        {
            Console.WriteLine($"  {(ok ? "PASS" : "FAIL")} {name} ({detail})");
            if (!ok) _failures++;
        }
    }

    public static int RunAll(string[] args)
    {
        Console.WriteLine($"HandleInterning stress: {Threads} threads, seed {Rng.Seed}");
        var report = new Report();
        var all = args.Length == 0;
        if (all || args.Contains("storm")) report.Scenario("same-bytes storm (keeper held)", () => SameBytesStorm(report));
        if (all || args.Contains("fold")) report.Scenario("fold-once races", () => FoldOnceRaces(report));
        if (all || args.Contains("mixed")) report.Scenario("mixed payloads", () => MixedPayloads(report));
        if (all || args.Contains("reuse")) report.Scenario("reuse after free", () => ReuseAfterFree(report));
        if (all || args.Contains("overrelease")) report.Scenario("over-release detection", () => OverRelease(report));
        if (all || args.Contains("capacity")) report.Scenario("capacity policy", () => CapacityPolicy(report));
        if (all || args.Contains("races")) report.Scenario("release races", () => ReleaseRaces(report));
        if (all || args.Contains("gc")) report.Scenario("gc compaction pressure", () => GcPressure(report));
        if (all || args.Contains("churn")) report.Scenario("same-bytes churn (no keeper)", () => SameBytesChurn(report));
        if (all || args.Contains("balance") || all) report.Scenario("final balance", () => FinalBalance(report));
        Console.WriteLine(report.Failures == 0 ? "stress: ALL PASS" : $"stress: {report.Failures} FAILURES");
        return report.Failures == 0 ? 0 : 1;
    }

    static Exception?[] RunThreads(int count, Action<int, Barrier> body)
    {
        var barrier = new Barrier(count);
        var errors = new Exception?[count];
        var threads = new Thread[count];
        for (var i = 0; i < count; i++)
        {
            var id = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    body(id, barrier);
                }
                catch (Exception e)
                {
                    errors[id] = e;
                }
            });
            threads[i].Start();
        }
        foreach (var t in threads) t.Join();
        return errors;
    }

    static bool ThrowErrors(Exception?[] errors, Report report, string name)
    {
        var thrown = errors.FirstOrDefault(e => e != null);
        report.Check(thrown == null, $"{name}: no thread exceptions", thrown?.ToString() ?? "clean");
        return thrown == null;
    }

    static void SameBytesStorm(Report report)
    {
        using var table = new InternTable(4096);
        var bytes = Rng.Payload(1, 4096);
        var beforeAllocCount = InternTable.AllocCount;
        var deaths0 = InternTable.Deaths;
        var keeper = table.Load(bytes);
        var entries = new nint[Threads];
        var errors = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            nint entry = 0;
            var sink = 0L;
            for (var i = 0; i < 25_000; i++)
            {
                using var a = table.Load(bytes);
                entry = (nint)a.Entry;
                sink += a.FirstByte + a.Length;
            }
            entries[id] = entry;
            Interlocked.Add(ref _sink, sink);
        });
        ThrowErrors(errors, report, "storm");
        var unique = entries.Distinct().ToArray();
        report.Check(unique.Length == 1 && unique[0] == (nint)keeper.Entry, "same bytes -> same handle under concurrency", $"{unique.Length} distinct handles, all == keeper entry");
        report.Check(InternTable.AllocCount - beforeAllocCount == 1, "exactly one native block allocated", $"{InternTable.AllocCount - beforeAllocCount} allocation for 200000 loads");
        report.Check(InternTable.PeakLiveBytes <= 4096, "at most one live block at any moment", $"peak live {InternTable.PeakLiveBytes} bytes");
        report.Check(table.LiveEntries == 1, "one live entry while keeper holds", $"{table.LiveEntries} live");
        keeper.Dispose();
        report.Check(table.LiveEntries == 0, "last release frees the entry", $"{table.LiveEntries} live");
        report.Check(InternTable.Deaths - deaths0 == 1, "entry died exactly once", $"{InternTable.Deaths - deaths0} deaths");
    }

    static void SameBytesChurn(Report report)
    {
        using var table = new InternTable(4096);
        var bytes = Rng.Payload(1, 4096);
        var beforeAllocCount = InternTable.AllocCount;
        var releases0 = InternTable.ReleaseCalls;
        var hits0 = InternTable.ProbeHits;
        var scans0 = InternTable.ScanMatches;
        var pubs0 = InternTable.Publishes;
        var deaths0 = InternTable.Deaths;
        var blips0 = InternTable.BlipRestores;
        var fails0 = InternTable.VerifyFails;
        var reverts0 = InternTable.RevertsApplied;
        var skipped0 = InternTable.RevertsSkipped;
        var created = new long[Threads];
        var disposed = new long[Threads];
        var errors = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            var state = Rng.Seed ^ (ulong)(uint)(id + 1) * 0xD1B54A32D192ED03UL;
            var sink = 0L;
            var mine = 0L;
            var freed = 0L;
            for (var i = 0; i < 25_000; i++)
            {
                var a = table.Load(bytes);
                mine++;
                sink += a.FirstByte + a.Length;
                var spins = (int)(Rng.Next(ref state) % 32);
                a.Dispose();
                freed++;
                for (var s = 0; s < spins; s++) sink += s;
            }
            created[id] = mine;
            disposed[id] = freed;
            Interlocked.Add(ref _sink, sink);
        });
        for (var t = 0; t < Threads; t++)
            Console.WriteLine($"  thread {t}: created={created[t]} disposed={disposed[t]}");
        foreach (var e in errors.Where(e => e != null)) Console.WriteLine($"  thread error: {e!.GetType().Name}: {e.Message}");
        ThrowErrors(errors, report, "churn");
        var epochs = InternTable.AllocCount - beforeAllocCount;
        Console.WriteLine($"  counters: releases={InternTable.ReleaseCalls - releases0} probeHits={InternTable.ProbeHits - hits0} scanMatches={InternTable.ScanMatches - scans0} publishes={InternTable.Publishes - pubs0} deaths={InternTable.Deaths - deaths0} blips={InternTable.BlipRestores - blips0} verifyFails={InternTable.VerifyFails - fails0} reverts={InternTable.RevertsApplied - reverts0} skipped={InternTable.RevertsSkipped - skipped0} overReleases={InternTable.OverReleases}");
        report.Check(epochs == InternTable.Deaths - deaths0, "death/rebirth cycles balance", $"{epochs} rebirth allocations, alloc == free");
        report.Check(table.LiveEntries == 0 && InternTable.OverReleases == 0, "churn reuses the single interned slot without slot exhaustion or over-release", $"{epochs} rebirths, {InternTable.OverReleases} over-releases, no exhaustion");
        report.Check(InternTable.PeakLiveBytes <= 8 * 4096, "single-content churn stays within the concurrent-load accounting bound", $"peak accounting window {InternTable.PeakLiveBytes} bytes (8 concurrent loads)");
        report.Check(table.LiveEntries == 0, "churn ends with zero live references", $"{table.LiveEntries} live");
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes + InternTable.GraveyardBytes, "native bytes balanced or parked in the graveyard", $"{InternTable.AllocBytes} == {InternTable.FreeBytes} + {InternTable.GraveyardBytes}");
    }

    static void MixedPayloads(Report report)
    {
        using var table = new InternTable(4096);
        var shared = Enumerable.Range(0, 4).Select(i => Rng.Payload(i, 4096)).ToArray();
        var unique = Enumerable.Range(0, Threads).Select(i => Rng.Payload(1000 + i, 2048)).ToArray();
        var beforeAllocCount = InternTable.AllocCount;
        var keepers = shared.Select(b => table.Load(b)).Concat(unique.Select(b => table.Load(b))).ToArray();
        var entries = new nint[Threads, 5];
        var errors = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            var mine = unique[id];
            for (var i = 0; i < 10_000; i++)
            {
                for (var s = 0; s < 4; s++)
                {
                    using var a = table.Load(shared[s]);
                    entries[id, s] = (nint)a.Entry;
                }
                using var b = table.Load(mine);
                entries[id, 4] = (nint)b.Entry;
            }
        });
        ThrowErrors(errors, report, "mixed");
        var sameContent = true;
        for (var s = 0; s < 4; s++)
        {
            nint first = (nint)keepers[s].Entry;
            for (var t = 0; t < Threads; t++) sameContent &= entries[t, s] == first;
        }
        report.Check(sameContent, "shared payloads -> one handle each across threads", "4 shared contents, all threads == keeper entries");
        var uniqueOk = true;
        for (var t = 0; t < Threads; t++)
            for (var u = 0; u < Threads; u++)
                if (t != u) uniqueOk &= entries[t, 4] != entries[u, 4];
        report.Check(uniqueOk, "distinct payloads -> distinct handles", $"{Threads} thread-unique contents");
        report.Check(InternTable.AllocCount - beforeAllocCount == 12, "one block per distinct held content", $"{InternTable.AllocCount - beforeAllocCount} allocations");
        foreach (var b in keepers) b.Dispose();
        report.Check(table.LiveEntries == 0, "all released", $"{table.LiveEntries} live");
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes + InternTable.GraveyardBytes, "native bytes balance", $"{InternTable.AllocBytes} alloc == {InternTable.FreeBytes} freed + {InternTable.GraveyardBytes} parked");
    }

    static void ReleaseRaces(Report report)
    {
        using var table = new InternTable(4096);
        var bytes = Rng.Payload(42, 4096);
        var beforeAllocCount = InternTable.AllocCount;
        var deaths0 = InternTable.Deaths;
        var errors = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            var state = Rng.Seed ^ (ulong)(uint)(id + 1) * 0xD1B54A32D192ED03UL;
            if (id < 7)
            {
                for (var i = 0; i < 20_000; i++)
                {
                    using var a = table.Load(bytes);
                    Interlocked.Add(ref _sink, a.FirstByte + a.Length);
                    var spins = (int)(Rng.Next(ref state) % 64);
                    for (var s = 0; s < spins; s++) Interlocked.Add(ref _sink, s);
                }
            }
            else
            {
                for (var i = 0; i < 2_000; i++)
                {
                    var keeper = table.Load(bytes);
                    for (var k = 0; k < 32; k++)
                    {
                        using var b = table.Load(bytes);
                        Interlocked.Add(ref _sink, b.FirstByte);
                    }
                    keeper.Dispose();
                }
            }
        });
        ThrowErrors(errors, report, "races");
        report.Check(table.LiveEntries == 0, "release races end with zero live references", $"{table.LiveEntries} live");
        report.Check(InternTable.Deaths - deaths0 >= 1, "last-reference deaths executed during race", $"{InternTable.Deaths - deaths0} deaths");
        report.Check(InternTable.AllocCount - beforeAllocCount == InternTable.Deaths - deaths0, "allocation/death counts equal after races", $"{InternTable.AllocCount - beforeAllocCount} alloc vs {InternTable.Deaths - deaths0} deaths");
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes + InternTable.GraveyardBytes, "native bytes balance after races", $"{InternTable.AllocBytes} == {InternTable.FreeBytes} + {InternTable.GraveyardBytes}");
    }

    static void FoldOnceRaces(Report report)
    {
        using var table = new InternTable(4096);
        var bytes = Rng.Payload(77, 4096);
        using var asset = table.Load(bytes);
        const ulong pairA = 0xA5A5A5A5A5A5A5A5UL;
        const ulong pairB = 0x5A5A5A5A5A5A5A5AUL;
        var beforeFolds = PairFolds.FoldCount;
        var effects = new float[Threads];
        var errors = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            ushort pos = 0;
            effects[id] = TypedLane.Advance(asset, pairA, ref pos);
        });
        ThrowErrors(errors, report, "fold");
        var sameEffect = effects.Distinct().Count() == 1;
        report.Check(sameEffect, "concurrent first use -> identical effect", $"{string.Create(CultureInfo.InvariantCulture, $"{effects[0]:R}")}");
        report.Check(PairFolds.FoldCount - beforeFolds == 1, "fold executed exactly once per (handle, pair)", $"{PairFolds.FoldCount - beforeFolds} folds");
        var beforeB = PairFolds.FoldCount;
        ushort posB = 0;
        TypedLane.Advance(asset, pairB, ref posB);
        report.Check(PairFolds.FoldCount - beforeB == 1, "second pair folds independently once", $"{PairFolds.FoldCount - beforeB} folds");
        report.Check(PairFolds.FoldAllocBytes - PairFolds.FoldFreeBytes == 2 * PairFolds.BytesPerPosition * (TypedLane.Duration + 1) + 2 * PairFolds.HeaderBytes,
            "fold retention matches table size", $"{PairFolds.FoldAllocBytes - PairFolds.FoldFreeBytes} retained bytes");
        ushort warmPos = 0;
        TypedLane.Advance(asset, pairA, ref warmPos);
        var checksums = new float[Threads];
        var duration = TypedLane.Duration;
        var walk = RunThreads(Threads, (id, barrier) =>
        {
            barrier.SignalAndWait();
            ushort pos = 0;
            var sum = 0f;
            for (var i = 0; i < 100_000; i++)
            {
                if (pos > duration) throw new InvalidOperationException($"walk position escaped: pos={pos} duration={duration} thread={id} step={i}");
                sum += TypedLane.Advance(asset, pairA, ref pos);
            }
            checksums[id] = sum;
        });
        ThrowErrors(walk, report, "guarded walk");
        report.Check(checksums.Distinct().Count() == 1, "100000 guarded advances per thread -> identical sinks", $"{string.Create(CultureInfo.InvariantCulture, $"{checksums[0]:R}")}");
        report.Check(PairFolds.FoldCount - beforeFolds == 2, "no fold during guarded warm walk", $"{PairFolds.FoldCount - beforeFolds} total folds");
    }

    static void ReuseAfterFree(Report report)
    {
        using var table = new InternTable(4096);
        var bytes = Rng.Payload(123, 4096);
        var beforeAlloc = InternTable.AllocCount;
        var first = table.Load(bytes);
        first.Dispose();
        var second = table.Load(bytes);
        report.Check(InternTable.AllocCount - beforeAlloc == 2, "freed content re-loads into a fresh block", $"{InternTable.AllocCount - beforeAlloc} allocations for two loads");
        report.Check(second.FirstByte == bytes[0] && second.Length == bytes.Length, "reloaded block contents match source bytes", $"first={second.FirstByte} len={second.Length}");
        var generation = (ulong)second.Entry->Count >> 32;
        second.Dispose();
        var third = table.Load(bytes);
        var nextGeneration = (ulong)third.Entry->Count >> 32;
        report.Check(nextGeneration == generation + 1, "cell reuse bumps the generation so stale observers lose races", $"gen {generation} -> {nextGeneration}");
        third.Dispose();
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes + InternTable.GraveyardBytes, "bytes balance across reuse cycles", $"{InternTable.AllocBytes} == {InternTable.FreeBytes} + {InternTable.GraveyardBytes}");
    }

    static void OverRelease(Report report)
    {
        using var table = new InternTable(4096);
        var asset = table.Load(Rng.Payload(321, 128));
        asset.Dispose();
        var before = InternTable.OverReleases;
        asset.Dispose();
        report.Check(InternTable.OverReleases == before + 1, "double dispose is detected and counted instead of corrupting", $"over-releases {before} -> {InternTable.OverReleases}");
        report.Check(table.LiveEntries == 0, "over-release scenario ends clean", $"{table.LiveEntries} live");
    }

    static void CapacityPolicy(Report report)
    {
        using var table = new InternTable(16);
        var held = new List<Asset>();
        var thrown = false;
        for (var i = 0; i < 16; i++)
        {
            try
            {
                held.Add(table.Load(Rng.Payload(5000 + i, 64)));
            }
            catch (InvalidOperationException)
            {
                thrown = true;
                break;
            }
        }
        report.Check(table.LiveEntries == 15, "table accepts entries up to the measured bound", $"{table.LiveEntries}/{table.Capacity} slots capacity");
        report.Check(thrown, "exhaustion fails loudly, never evicts", $"threw after {held.Count} held entries");
        foreach (var a in held) a.Dispose();
        report.Check(table.LiveEntries == 0, "capacity scenario releases cleanly", $"{table.LiveEntries} live");
    }

    static bool _stop;
    static long _gcCycles;

    static void GcPressure(Report report)
    {
        using var table = new InternTable(4096);
        var a = Rng.Payload(808, 4096);
        var b = Rng.Payload(909, 8192);
        Volatile.Write(ref _stop, false);
        Volatile.Write(ref _gcCycles, 0);
        var errors = RunThreads(Threads + 1, (id, barrier) =>
        {
            barrier.SignalAndWait();
            if (id == Threads)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                while (Volatile.Read(ref _stop) == false)
                {
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    Interlocked.Increment(ref _gcCycles);
                }
                return;
            }
            var payload = id % 2 == 0 ? a : b;
            for (var i = 0; i < 50_000; i++)
            {
                using var asset = table.Load(payload);
                Interlocked.Add(ref _sink, asset.FirstByte + asset.Length);
            }
            Volatile.Write(ref _stop, true);
        });
        Thread.Sleep(50);
        ThrowErrors(errors, report, "gc pressure");
        report.Check(Interlocked.Read(ref _gcCycles) > 0, "compacting GC ran during concurrent load storm", $"{Interlocked.Read(ref _gcCycles)} full-blocking collect cycles");
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes + InternTable.GraveyardBytes, "native bytes unaffected by compacting GC", $"{InternTable.AllocBytes} == {InternTable.FreeBytes} + {InternTable.GraveyardBytes}");
        report.Check(table.LiveEntries == 0, "gc pressure scenario releases cleanly", $"{table.LiveEntries} live");
    }

    static void FinalBalance(Report report)
    {
        InternTable.DrainGraveyardForced();
        PairFolds.ResetAll();
        report.Check(PairFolds.FoldAllocBytes == PairFolds.FoldFreeBytes, "fold table bytes balance after ResetAll", $"{PairFolds.FoldAllocBytes} == {PairFolds.FoldFreeBytes}");
        report.Check(PairFolds.FoldAllocCount == PairFolds.FoldFreeCount, "fold table counts balance", $"{PairFolds.FoldAllocCount} == {PairFolds.FoldFreeCount}");
        report.Check(InternTable.AllocBytes == InternTable.FreeBytes, "total native bytes allocated == freed", $"{InternTable.AllocBytes} == {InternTable.FreeBytes}");
        report.Check(InternTable.AllocCount == InternTable.FreeCount, "total native blocks allocated == freed", $"{InternTable.AllocCount} == {InternTable.FreeCount}");
        report.Check(InternTable.GraveyardBlocks == 0, "graveyard drained", $"{InternTable.GraveyardBlocks} blocks parked");
        Console.WriteLine($"  totals: sink={Interlocked.Read(ref _sink)} allocBytes={InternTable.AllocBytes} foldBytes={PairFolds.FoldAllocBytes}");
    }
}
