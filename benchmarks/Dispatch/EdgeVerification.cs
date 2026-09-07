using System.Runtime.CompilerServices;

namespace Tl.Hooks;

internal static class EdgeVerification
{
    public static void Run()
    {
        var failures = new List<string>();
        foreach (var test in new Action[] { Gaps, Empty, Blend, Order, Packing, Wrap, States, Batches, CursorParity, Scratch, PrefixCounts, Dedup, Fixture, PerClip, WideTicks, Limits, Indices })
        {
            try { test(); }
            catch (Exception e) { failures.Add($"{test.Method.Name}: {e.Message}"); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("Edge checks passed: gaps, terminal ticks, empty tables, blends, packing, wraps, per-work state oracle, cursor parity, blend scratch, prefix counts, storage dedup, fixture parity, per-work facts oracle, limits.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static ushort Make(params ReadOnlySpan<ClipEdge> edges) => Make(false, edges);

    // Looping is an authored trait: a looping variant is a second Build.
    private static ushort Make(bool loops, params ReadOnlySpan<ClipEdge> edges)
    {
        var authoring = edges.ToArray();
        return Timeline<ProbeTrack, ProbeClip>.Build(b =>
        {
            foreach (var edge in authoring)
            {
                TrackRef track = b.Track(new ProbeTrack(0));
                b.Clip(track, new ProbeClip(10), edge.Start, edge.End);
            }

            if (loops)
                b.Looping();
        });
    }

    private static Playback At(uint tick) => new(tick, 0, PlaybackFlags.Started);

    private static void Gaps()
    {
        var timeline = Make(new ClipEdge(10, 20));
        foreach (uint tick in new uint[] { 0, 9, 10, 19, 20, 21, uint.MaxValue })
        {
            var probe = new Probe();
            Timeline.Forward(timeline, ref probe, tick);
            Require(probe.Tracks == (tick >= 10 && tick < 20 ? 1 : 0), $"Wrong active tracks at {tick}.");
            Require(probe.Count == probe.Tracks, $"Empty ticks must not fire callbacks at {tick}.");

            var from = At(19);
            probe = new Probe();
            var state = Timeline.Forward(timeline, in from, ref probe, tick);
            Require(state.Has(PlaybackFlags.Started), $"Playback lost Started at {tick}.");
            Require(state.Has(PlaybackFlags.Completed) == (tick >= 19), $"Wrong Completed at {tick}: {state.Flags}.");
        }
    }

    private static void Empty()
    {
        var timeline = Make();
        var data = new Probe();
        var from = At(0);
        var result = Timeline.Forward(timeline, in from, ref data, 0, 10);
        Require(data.Count == 0, "An empty timeline must fire no callbacks.");
        Require(result.Has(PlaybackFlags.Completed) && result.Tick == 10, "Empty non-looping timeline did not complete.");
        var looping = Make(true);
        result = Timeline.Forward(looping, in from, ref data, 10);
        Require(result.Cycles == 0 && result.Flags == PlaybackFlags.Started, "Empty loop should have no facts.");
    }

    private static void Blend()
    {
        var timeline = Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(2));
            b.Clip(track, new ProbeClip(10), 4, 9);
            b.Clip(track, new ProbeClip(30), 4, 9);
        });

        // The authored pair shares the window [4,9); the factor runs
        // (t - 4) / 4f. Boundary frames do not accumulate: tick 4 carries no
        // movement (stateless sample of a window start is Stay), tick 8 is
        // the window's last active frame — Exit.
        for (uint tick = 4; tick <= 8; tick++)
        {
            var data = new Probe();
            Timeline.Forward(timeline, ref data, tick);
            var factor = (tick - 4) / 4f;
            var expected = tick == 8 ? 0f : 10f * (1f - factor) + 30f * factor + 2f;
            Require(Math.Abs(data.Sum - expected) < 1e-4f, $"Blend at {tick} returned {data.Sum}, expected {expected}.");
        }
    }

    private static void Order()
    {
        var timeline = Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            b.Clip(track, new ProbeClip(30), 5, 10);
            b.Clip(track, new ProbeClip(10), 0, 8);
        });
        var data = new Probe();
        Timeline.Forward(timeline, ref data, 5);
        Require(data.Sum == 10, $"Crossfade should start with the earlier clip; got {data.Sum}.");
    }

    private static void Packing()
    {
        Require(Unsafe.SizeOf<Playback>() == 8, "Playback grew beyond 8 bytes.");
        const ushort max = ushort.MaxValue;
        var timeline = Make(true, new ClipEdge(0, 1));
        var from = new Playback(0, max, PlaybackFlags.Started);
        var data = new Probe();
        Reject<ArgumentOutOfRangeException>(() => Timeline.Forward(timeline, in from, ref data, 1));
        Require(data.Count == 0, "Cycle overflow invoked a callback before rejecting the step.");
    }

    private static void Wrap()
    {
        var timeline = Make(true, new ClipEdge(0, 10));
        var from = At(9);
        var data = new Probe();
        var next = Timeline.Forward(timeline, in from, ref data, 0);
        Require(next.Cycles == 1 && next.Flags == PlaybackFlags.Started, $"Local forward wrap corrupted state: {next.Cycles}, {next.Flags}.");
        // The wrapped span (9, 10) then [0, 0] crossed the clip's start at 0:
        // the work reports Enter on its first local frame.
        Require(data.Works.Count == 1 && data.Works[0].State == ClipState.Enter, "A wrapped step must enter through the start edge.");

        Timeline.Forward(timeline, ref data, 11);
        Require(data.Tick == 1 && data.Tracks == 1, "Stateless looping playback must normalize the tick too.");
    }

    // The per-work state oracle, the aggregate PlaybackFlags oracle's
    // replacement: for every (previous, tick) step over the five-edge
    // fixture — linear and looping, forward and backward — the recorded
    // (track, ClipState) sequence and the returned flags word must equal
    // values derived straight from the raw edge list.
    private static void States()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        var linear = Make(edges);
        var looping = Make(true, edges);
        const uint duration = 28;
        foreach (bool loops in new[] { false, true })
        foreach (bool backward in new[] { false, true })
        {
            var timeline = loops ? looping : linear;
            for (uint previous = 0; previous < 85; previous++)
            for (uint tick = 0; tick < 85; tick++)
            {
                var from = At(previous);
                var data = new Probe();
                var result = backward
                    ? Timeline.Backward(timeline, in from, ref data, tick)
                    : Timeline.Forward(timeline, in from, ref data, tick);

                var (tEff, prevEff, wrapped, full) = Locate(previous, tick, duration, loops, backward);

                var expected = new List<(int Track, ClipState State)>();
                for (var i = 0; i < edges.Length; i++)
                {
                    if (edges[i].Start > tEff || tEff >= edges[i].End)
                        continue;
                    expected.Add((i, StateOracle(tEff, prevEff, edges[i].Start, edges[i].End, backward, wrapped, full)));
                }

                if (data.Works.Count != expected.Count)
                    throw new InvalidOperationException($"Works at {previous}->{tick}, loops={loops}, backward={backward}: {data.Works.Count} works, expected {expected.Count}.");
                for (var i = 0; i < expected.Count; i++)
                    if (data.Works[i].Index != expected[i].Track || data.Works[i].State != expected[i].State)
                        throw new InvalidOperationException(
                            $"State at {previous}->{tick}, loops={loops}, backward={backward}, work {i}: {data.Works[i].Index}/{data.Works[i].State} != {expected[i].Track}/{expected[i].State}.");

                var flags = PlaybackFlags.Started;
                if (loops)
                {
                    if (tEff == duration - 1)
                        flags |= PlaybackFlags.LastLoopFrame;
                }
                else if (backward ? tEff == 0 : tEff >= duration - 1)
                {
                    flags |= PlaybackFlags.Completed;
                }

                Require(result.Flags == flags, $"Flags at {previous}->{tick}, loops={loops}, backward={backward}: {result.Flags} != {flags}.");
            }
        }
    }

    private static void Batches()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        uint[] ticks = [0, 1, 2, 7, 25, 24, 6, 28, 84, 3, 3, 4, 20, 80, 5];
        var linear = Make(edges);
        var looping = Make(true, edges);
        foreach (bool loops in new[] { false, true })
        foreach (bool backward in new[] { false, true })
        {
            var timeline = loops ? looping : linear;
            var single = new Probe();
            var batch = new Probe();
            var state = At(0);
            foreach (uint tick in ticks)
                state = backward
                    ? Timeline.Backward(timeline, in state, ref single, tick)
                    : Timeline.Forward(timeline, in state, ref single, tick);
            var from = At(0);
            var result = backward
                ? Timeline.Backward(timeline, in from, ref batch, ticks)
                : Timeline.Forward(timeline, in from, ref batch, ticks);
            Require(single.Works.Count == batch.Works.Count
                    && single.Works.SequenceEqual(batch.Works)
                    && single.Sum == batch.Sum
                    && single.Count == batch.Count
                    && state.Cycles == result.Cycles
                    && state.Flags == result.Flags
                    && state.Tick == result.Tick,
                "Batch cursor changed sampling or movement facts.");
        }
    }

    private static void WideTicks()
    {
        var timeline = Make(new ClipEdge(uint.MaxValue - 2, uint.MaxValue));
        var data = new Probe();
        var from = At(uint.MaxValue - 3);
        var active = Timeline.Forward(timeline, in from, ref data, uint.MaxValue - 1);
        Require(active.Has(PlaybackFlags.Completed) && data.Tracks == 1 && data.Sum == 0,
            "Wide tick lost completion; the destination is the last active frame (Exit — no Stay accumulation).");
        var terminal = new Probe();
        var end = Timeline.Forward(timeline, in active, ref terminal, uint.MaxValue);
        Require(end.Has(PlaybackFlags.Completed) && terminal.Tracks == 0 && terminal.Count == 0, "Wide terminal tick must leave the clip behind (no callback on the empty tick).");
    }

    private static void Indices()
    {
        ushort next = Timeline<ProbeTrack, ProbeClip>.Build(static b => { });
        for (int i = next + 1; i < ushort.MaxValue; i++)
            Require(Timeline<ProbeTrack, ProbeClip>.Build(static b => { }) == i, "Index was reused before exhaustion.");
        Reject<InvalidOperationException>(() => Timeline<ProbeTrack, ProbeClip>.Build(static b => { }));
        Reject<InvalidOperationException>(() => Timeline<ProbeTrack, ProbeClip>.Build(static b => { }));
    }

    // The persistent caller-owned Cursor: every receipt runs the same step
    // twice — plain search, and cursor-primed — and requires identical
    // Playback AND identical recorded works. Covers the handoff's list,
    // adapted to per-work states: same tick, adjacent forward/backward
    // ticks, random seeks, wraps, restored Playback snapshots, alternating
    // timelines (separate cursors, and one shared cursor), rebuilds,
    // loop-mode changes, empty spans, and the lifecycle rejections firing
    // before any callback.
    private static void CursorParity()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        var linear = Make(edges);
        var looping = Make(true, edges);

        void RequireEqual(Playback plain, Probe plainData, Playback primed, Probe primedData, string what)
        {
            if (plain.Tick != primed.Tick || plain.Cycles != primed.Cycles || plain.Flags != primed.Flags)
                throw new InvalidOperationException($"Cursor {what}: Playback diverged ({plain.Tick}/{plain.Cycles}/{plain.Flags} vs {primed.Tick}/{primed.Cycles}/{primed.Flags}).");
            if (!plainData.Works.SequenceEqual(primedData.Works) || plainData.Sum != primedData.Sum || plainData.Count != primedData.Count)
                throw new InvalidOperationException($"Cursor {what}: works diverged.");
        }

        // A cursor-stepped walk against a plain walk, tick for tick.
        void Walk(ushort timeline, ReadOnlySpan<uint> ticks, bool backward)
        {
            var plain = At(0);
            var primed = At(0);
            var cache = default(Cursor);
            var plainData = new Probe();
            var primedData = new Probe();
            foreach (var tick in ticks)
            {
                var markP = plainData.Works.Count;
                var markC = primedData.Works.Count;
                plain = backward
                    ? Timeline.Backward(timeline, in plain, ref plainData, tick)
                    : Timeline.Forward(timeline, in plain, ref plainData, tick);
                primed = backward
                    ? Timeline.Backward(timeline, in primed, ref cache, ref primedData, tick)
                    : Timeline.Forward(timeline, in primed, ref cache, ref primedData, tick);
                if (plain.Tick != primed.Tick || plain.Cycles != primed.Cycles || plain.Flags != primed.Flags
                    || !plainData.Works.Skip(markP).SequenceEqual(primedData.Works.Skip(markC)))
                    throw new InvalidOperationException($"Cursor walk diverged at {tick}, backward={backward}.");
            }
        }

        // Same tick, adjacent forward/backward, and a wrap on the looping variant.
        Walk(linear, [5, 5, 5, 6, 7, 6, 5, 4], backward: false);
        Walk(looping, [5, 5, 5, 6, 7, 6, 5, 4], backward: false);
        Walk(linear, [20, 20, 19, 18, 19, 20, 21], backward: true);
        Walk(looping, [27, 27, 28, 0, 1, 27, 26, 28, 0], backward: false);
        Walk(looping, [0, 0, 28, 27, 0, 1, 28], backward: true);

        // Random seeks: deterministic xorshift battery, both timelines, both
        // directions — full per-step parity.
        uint random = 0x9E3779B9u;
        Span<uint> seeks = stackalloc uint[64];
        for (var i = 0; i < seeks.Length; i++)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            seeks[i] = random % 85;
        }
        Walk(linear, seeks, backward: false);
        Walk(linear, seeks, backward: true);
        Walk(looping, seeks, backward: false);
        Walk(looping, seeks, backward: true);

        // Deliberately invalid caches: wrong owner, stale tick, out-of-range
        // region — all must fall back to searching with unchanged results.
        foreach (var bad in new[] { default(Cursor), new Cursor { Owner = linear, Tick = 99, Region = 0 }, new Cursor { Owner = looping, Tick = 0, Region = 0 } })
        {
            var cache = bad;
            var data = new Probe();
            var plainData = new Probe();
            var from = At(0);
            var plain = Timeline.Forward(linear, in from, ref plainData, 9);
            var primed = Timeline.Forward(linear, in from, ref cache, ref data, 9);
            RequireEqual(plain, plainData, primed, data, "invalid fallback");
        }
        {
            var cache = new Cursor { Owner = looping, Tick = 0, Region = 99 };
            var data = new Probe();
            var from = At(0);
            var primed = Timeline.Forward(looping, in from, ref cache, ref data, 9);
            var plainData = new Probe();
            var plain = Timeline.Forward(looping, in from, ref plainData, 9);
            RequireEqual(plain, plainData, primed, data, "out-of-range region");
        }

        // Restored Playback snapshot: continue past a saved point, then rewind
        // the Playback without touching the cursor — the tick check must
        // invalidate the hint and reproduce the plain result.
        {
            var saved = At(5);
            var cache = default(Cursor);
            var warm = new Probe();
            Timeline.Forward(linear, in saved, ref cache, ref warm, 8);
            var rewoundData = new Probe();
            var rewound = Timeline.Forward(linear, in saved, ref cache, ref rewoundData, 8);
            var plainData = new Probe();
            var plain = Timeline.Forward(linear, in saved, ref plainData, 8);
            RequireEqual(plain, plainData, rewound, rewoundData, "restored snapshot");
        }

        // Alternating timelines: two cursors stay independent, and one shared
        // cursor alternates by falling back (still correct).
        {
            var other = Make(edges);
            var first = default(Cursor);
            var second = default(Cursor);
            var a = At(0);
            var b = At(0);
            var plainA = At(0);
            var plainB = At(0);
            var dataA = new Probe();
            var dataB = new Probe();
            var plainDataA = new Probe();
            var plainDataB = new Probe();
            for (var i = 4; i < 30; i += 3)
            {
                plainA = Timeline.Forward(linear, in plainA, ref plainDataA, (uint)i);
                plainB = Timeline.Forward(other, in plainB, ref plainDataB, (uint)i);
                a = Timeline.Forward(linear, in a, ref first, ref dataA, (uint)i);
                b = Timeline.Forward(other, in b, ref second, ref dataB, (uint)i);
                if (a.Tick != plainA.Tick || b.Tick != plainB.Tick)
                    throw new InvalidOperationException("Alternating timelines diverged with separate cursors.");
            }
            if (!dataA.Works.SequenceEqual(plainDataA.Works) || !dataB.Works.SequenceEqual(plainDataB.Works))
                throw new InvalidOperationException("Separate cursors interfered across timelines.");

            var shared = default(Cursor);
            var c = At(0);
            var d = At(0);
            var sharedDataA = new Probe();
            var sharedDataB = new Probe();
            for (var i = 4; i < 30; i += 3)
            {
                c = Timeline.Forward(linear, in c, ref shared, ref sharedDataA, (uint)i);
                d = Timeline.Forward(other, in d, ref shared, ref sharedDataB, (uint)i);
            }
            if (c.Tick != plainA.Tick || d.Tick != plainB.Tick
                || !sharedDataA.Works.SequenceEqual(plainDataA.Works) || !sharedDataB.Works.SequenceEqual(plainDataB.Works))
                throw new InvalidOperationException("A shared cursor changed results across timelines.");
        }

        // Rebuild and loop-mode change: same content, NEW entries — the stale
        // owner must fall back and stay correct.
        {
            var rebuilt = Make(edges);
            var start = At(0);
            var cache = default(Cursor);
            var stale = new Probe();
            Timeline.Forward(linear, in start, ref cache, ref stale, 9);
            var rebuiltData = new Probe();
            var rebuiltPb = Timeline.Forward(rebuilt, in start, ref cache, ref rebuiltData, 9);
            var plainData = new Probe();
            var plain = Timeline.Forward(rebuilt, in start, ref plainData, 9);
            RequireEqual(plain, plainData, rebuiltPb, rebuiltData, "rebuild fallback");

            var loopData = new Probe();
            var loopPb = Timeline.Forward(looping, in start, ref cache, ref loopData, 9);
            var plainLoopData = new Probe();
            var plainLoop = Timeline.Forward(looping, in start, ref plainLoopData, 9);
            RequireEqual(plainLoop, plainLoopData, loopPb, loopData, "loop-mode fallback");
        }

        // Empty spans: a valid cursor survives unchanged, and the next call
        // still lands correctly; an empty span over an invalid cursor simply
        // refreshes the owner.
        {
            var cache = default(Cursor);
            var data = new Probe();
            var begin = At(4);
            var pb = Timeline.Forward(linear, in begin, ref cache, ref data, 6);
            var empty = Timeline.Forward(linear, in pb, ref cache, ref data, []);
            if (empty.Tick != pb.Tick || empty.Flags != pb.Flags || data.Count != 1)
                throw new InvalidOperationException("An empty span must not run callbacks or move the playback.");
            var mark = data.Works.Count;
            var next = Timeline.Forward(linear, in empty, ref cache, ref data, 7);
            var plainData = new Probe();
            var plainNext = Timeline.Forward(linear, in pb, ref plainData, 7);
            if (next.Tick != plainNext.Tick || next.Cycles != plainNext.Cycles || next.Flags != plainNext.Flags)
                throw new InvalidOperationException("Cursor after an empty span: Playback diverged.");
            if (!data.Works.Skip(mark).SequenceEqual(plainData.Works))
                throw new InvalidOperationException("Cursor after an empty span: works diverged.");
        }

        // Single vs batch parity with a primed cursor.
        {
            uint[] ticks = [0, 1, 2, 7, 25, 24, 6, 28, 84, 3, 3, 4, 20, 80, 5];
            var singleData = new Probe();
            var single = At(0);
            var cache = default(Cursor);
            foreach (var tick in ticks)
                single = Timeline.Forward(linear, in single, ref cache, ref singleData, tick);
            var batchData = new Probe();
            var batchStart = At(0);
            var batch = Timeline.Forward(linear, in batchStart, ref cache, ref batchData, ticks);
            if (single.Tick != batch.Tick || single.Cycles != batch.Cycles || single.Flags != batch.Flags
                || !singleData.Works.SequenceEqual(batchData.Works))
                throw new InvalidOperationException("Cursor single/batch parity broke.");
        }

        // Lifecycle: the cursor overload rejects before any callback too.
        {
            var guard = new Probe();
            var cache = default(Cursor);
            Reject<InvalidOperationException>(() => Timeline.Forward(linear, default, ref cache, ref guard, 5));
            Reject<InvalidOperationException>(() => Timeline.Backward(linear, default, ref cache, ref guard, 5));
            var stopped = Timeline.Stop(linear, At(5));
            Reject<InvalidOperationException>(() => Timeline.Forward(linear, in stopped, ref cache, ref guard, 5));
            if (guard.Count != 0)
                throw new InvalidOperationException("A rejected cursor playback must not run callbacks.");
        }
    }

    // Blend-sized scratch (faster queue #2): only blended tracks consume
    // resolution slots, a zero-blend timeline needs no scratch at all, the
    // stack path is byte-budgeted with the scratch overload as the escape
    // hatch, and caller buffers never share state between simultaneous
    // calls. Every receipt compares the scratch path against the plain stack
    // path (and the authored values) for identical results.
    private static void Scratch()
    {
        // 1. Zero blends: no blend scratch required at all — an empty caller
        //    span is accepted and matches the stack path exactly.
        var plainIndex = Make(new ClipEdge(0, 10), new ClipEdge(4, 8));
        Require(Timeline.Live(plainIndex).MaxActiveBlends == 0, "A timeline without pairs must need no blend scratch.");
        var plainStart = At(5);
        var emptyScratch = new Probe();
        var viaEmpty = Timeline<ProbeTrack, ProbeClip>.Forward(plainIndex, in plainStart, ref emptyScratch, Span<ProbeClip>.Empty, 5);
        var plainZero = new Probe();
        var viaStack = Timeline.Forward(plainIndex, in plainStart, ref plainZero, 5);
        Require(viaEmpty.Flags == viaStack.Flags && viaEmpty.Cycles == viaStack.Cycles
            && emptyScratch.Works.SequenceEqual(plainZero.Works) && emptyScratch.Sum == plainZero.Sum,
            "The empty scratch span diverged on a zero-blend timeline.");

        // 2. Mixed single/blended tracks: one standalone row resolves in
        //    place, only the pair takes a slot; sizing follows blends, not
        //    active tracks.
        var mixed = Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var solo = b.Track(new ProbeTrack(0));
            var pair = b.Track(new ProbeTrack(2));
            b.Clip(solo, new ProbeClip(10), 0, 8);
            b.Clip(pair, new ProbeClip(10), 4, 9);
            b.Clip(pair, new ProbeClip(30), 4, 9);
        });
        Require(Timeline.Live(mixed).MaxActiveTracks == 2, "Mixed fixture should have two concurrent active tracks.");
        Require(Timeline.Live(mixed).MaxActiveBlends == 1, "Mixed fixture should size scratch to its one blend.");
        Span<ProbeClip> one = stackalloc ProbeClip[1];
        for (uint tick = 0; tick < 10; tick++)
        {
            var from = At(tick == 0 ? 0 : tick - 1);
            var buffered = new Probe();
            var bufferedPb = Timeline<ProbeTrack, ProbeClip>.Forward(mixed, in from, ref buffered, one, tick);
            var stacked = new Probe();
            var stackedPb = Timeline.Forward(mixed, in from, ref stacked, tick);
            Require(bufferedPb.Flags == stackedPb.Flags && bufferedPb.Cycles == stackedPb.Cycles
                && buffered.Works.SequenceEqual(stacked.Works) && buffered.Sum == stacked.Sum,
                $"Scratch/stack diverged on the mixed fixture at {tick}.");
        }

        // 3. One-tick blend: the pair shares exactly one active frame, the
        //    factor is 0.5.
        var oneTick = Timeline<BigTrack, BigClip>.Build(static b =>
        {
            var track = b.Track(new BigTrack());
            b.Clip(track, new BigClip(10f), 4, 5);
            b.Clip(track, new BigClip(30f), 4, 5);
        });
        var oneData = new BigData();
        var onePb = new Playback(3, 0, PlaybackFlags.Started);
        Span<BigClip> oneBig = stackalloc BigClip[1];
        onePb = Timeline<BigTrack, BigClip>.Forward(oneTick, in onePb, ref oneData, oneBig, 4);
        Require(Math.Abs(oneData.Sum - 20f) < 1e-4f, $"A one-tick blend must resolve at factor 0.5; got {oneData.Sum}.");

        // 4. All-blends, large payloads: caller buffer equals the stack path
        //    tick for tick, and the same buffer serves repeated calls
        //    without state bleed.
        var all = Timeline<BigTrack, BigClip>.Build(static b =>
        {
            var track = b.Track(new BigTrack());
            b.Clip(track, new BigClip(10f), 0, 16);
            b.Clip(track, new BigClip(30f), 8, 24);
        });
        Require(Timeline.Live(all).MaxActiveTracks == 1 && Timeline.Live(all).MaxActiveBlends == 1,
            "All-blends fixture sizing wrong.");
        var buffer = new BigClip[1];
        float first = 0, second = 0;
        for (var run = 0; run < 2; run++)
        {
            var data = new BigData();
            var pb = new Playback(0, 0, PlaybackFlags.Started);
            for (uint tick = 0; tick < 24; tick++)
                pb = Timeline<BigTrack, BigClip>.Forward(all, in pb, ref data, buffer, tick);
            if (run == 0)
                first = data.Sum;
            else
                second = data.Sum;
        }

        var stackedWalk = new BigData();
        var stackedWalkPb = new Playback(0, 0, PlaybackFlags.Started);
        for (uint tick = 0; tick < 24; tick++)
            stackedWalkPb = Timeline.Forward(all, in stackedWalkPb, ref stackedWalk, tick);
        Require(Math.Abs(first - stackedWalk.Sum) < 1e-4f && Math.Abs(second - stackedWalk.Sum) < 1e-4f,
            "The caller buffer diverged from the stack path, or reusing one buffer bled state.");

        // 5. Insufficient scratch rejects before any callback.
        var witness = new BigData();
        var startPb = new Playback(0, 0, PlaybackFlags.Started);
        Reject<ArgumentException>(() => Timeline<BigTrack, BigClip>.Forward(all, in startPb, ref witness, Span<BigClip>.Empty, 1));
        Require(witness.Count == 0, "Insufficient scratch must reject before any callback.");

        // 6. Over-budget payloads refuse the stack path and play through the
        //    caller buffer: one 4,204-byte blend slot exceeds the 4,096-byte
        //    stack budget, so the stack overload throws and a retained heap
        //    buffer carries it.
        Require(Unsafe.SizeOf<HugeClip>() > BlendScratch.StackBytes, "The huge fixture should exceed the stack byte budget.");
        var huge = Timeline<HugeTrack, HugeClip>.Build(static b =>
        {
            var track = b.Track(new HugeTrack());
            b.Clip(track, new HugeClip(10f), 4, 9);
            b.Clip(track, new HugeClip(30f), 4, 9);
        });
        var hugeWitness = new HugeData();
        var hugeStart = new Playback(3, 0, PlaybackFlags.Started);
        Reject<InvalidOperationException>(() => Timeline.Forward(huge, in hugeStart, ref hugeWitness, 4));
        Require(hugeWitness.Count == 0, "The over-budget stack rejection must fire before any callback.");
        var retained = new HugeClip[1];
        var hugeData = new HugeData();
        hugeStart = Timeline<HugeTrack, HugeClip>.Forward(huge, in hugeStart, ref hugeData, retained, 6);
        Require(Math.Abs(hugeData.Sum - (10f * 0.5f + 30f * 0.5f)) < 1e-4f, $"The huge blend resolved wrong through the caller buffer: {hugeData.Sum}.");

        // 7. Simultaneous/reentrant calls: the outer consumer plays a second
        //    blended timeline from inside its callback, each with its own
        //    scratch — outer and inner accumulate their own values.
        var inner = Timeline<BigTrack, BigClip>.Build(static b =>
        {
            var track = b.Track(new BigTrack());
            b.Clip(track, new BigClip(1f), 0, 24);
            b.Clip(track, new BigClip(3f), 8, 24);
        });
        var nestedData = new NestedData { Inner = inner };
        var nestedPb = new Playback(0, 0, PlaybackFlags.Started);
        var outerBuffer = new BigClip[1];
        for (uint tick = 0; tick < 24; tick++)
            nestedPb = Timeline<BigTrack, BigClip>.Forward(all, in nestedPb, ref nestedData, outerBuffer, tick);
        var innerReference = new BigData();
        var innerPb = new Playback(0, 0, PlaybackFlags.Started);
        for (uint tick = 0; tick < 24; tick++)
            innerPb = Timeline.Forward(inner, in innerPb, ref innerReference, tick);
        Require(Math.Abs(nestedData.InnerData.Sum - innerReference.Sum) < 1e-4f && nestedData.InnerData.Count == innerReference.Count,
            "The reentrant inner timeline diverged from its standalone reference.");

        // 8. The compiled-tables shell: same arrangement — scratch overload
        //    equals the stack params overload.
        Span<VitalsClip> vitalsScratch = stackalloc VitalsClip[1];
        var shellData = new Vitals { Health = 100_000f };
        GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(ref shellData, vitalsScratch, [0, 1, 5, 29, 40, 76, 100, 320, 330, 515, 599]);
        var shellStack = new Vitals { Health = 100_000f };
        GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(ref shellStack, 0, 1, 5, 29, 40, 76, 100, 320, 330, 515, 599);
        Require(shellData.Result == shellStack.Result, "GeneratedTimeline scratch/stack diverged on the Vitals fixture.");
        var shellStateData = new Vitals { Health = 100_000f };
        var shellPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        shellPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in shellPb, ref shellStateData, vitalsScratch, [0, 1, 5, 29, 40, 76, 100, 320, 330, 515, 599]);
        var shellStateStack = new Vitals { Health = 100_000f };
        var shellStackPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        shellStackPb = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in shellStackPb, ref shellStateStack, 0, 1, 5, 29, 40, 76, 100, 320, 330, 515, 599);
        Require(shellPb.Flags == shellStackPb.Flags && shellStateData.Result == shellStateStack.Result,
            "GeneratedTimeline stateful scratch/stack diverged on the Vitals fixture.");
    }

    // Prefix counts for the per-work Enter gate (faster queue #3): the
    // cumulative CutCounts table must equal values derived from the raw
    // authored clip list, the flag-off build must leave it empty, and every
    // walk/jump receipt must be identical with the gate on and off — the
    // counts may only ever skip Enter checks that provably cannot fire.
    private static void PrefixCounts()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        ushort Build(bool emit)
        {
            Timeline.EmitCutCounts = emit;
            return Make(edges);
        }

        var counted = Build(true);
        var scanned = Build(false);
        Timeline.EmitCutCounts = true;

        var entry = Timeline.Live(counted);
        Require(entry.CutCounts.Length == entry.RegionStarts.Length, "The counts table must have one record per region.");
        Require(Timeline.Live(scanned).CutCounts.Length == 0, "The flag-off build must leave the counts empty.");

        // The oracle: counts[i] prefix-sums, over regions 0..i, whether any
        // authored clip starts (Starts) or ends (Ends) exactly at that
        // region's start position — derived here from the raw edge list.
        var starts = entry.RegionStarts;
        var counts = entry.CutCounts;
        uint startsSeen = 0, endsSeen = 0;
        for (var i = 0; i < starts.Length; i++)
        {
            foreach (var edge in edges)
            {
                if (edge.Start == starts[i])
                {
                    startsSeen++;
                    break;
                }
            }

            foreach (var edge in edges)
            {
                if (edge.End == starts[i])
                {
                    endsSeen++;
                    break;
                }
            }

            Require(counts[i].Starts == startsSeen && counts[i].Ends == endsSeen,
                $"Prefix counts diverged at region {i}: {counts[i]} vs ({startsSeen}, {endsSeen}).");
        }

        // Gate on/off parity: linear and looping, forward and backward, over
        // the full (previous, tick) step space — works, sums and flags must
        // be identical.
        var loopingCounted = Build(true);
        var loopingScanned = Build(false);
        Timeline.EmitCutCounts = true;

        foreach (var (loops, countedIndex, scannedIndex) in new (bool, ushort, ushort)[]
                 {
                     (false, counted, scanned),
                     (true, loopingCounted, loopingScanned),
                 })
        foreach (bool backward in new[] { false, true })
        {
            for (uint previous = 0; previous < 85; previous++)
            for (uint tick = 0; tick < 85; tick++)
            {
                var from = At(previous);
                var viaCounts = new Probe();
                var viaScan = new Probe();
                var countedPb = backward
                    ? Timeline.Backward(countedIndex, in from, ref viaCounts, tick)
                    : Timeline.Forward(countedIndex, in from, ref viaCounts, tick);
                var scannedPb = backward
                    ? Timeline.Backward(scannedIndex, in from, ref viaScan, tick)
                    : Timeline.Forward(scannedIndex, in from, ref viaScan, tick);
                if (countedPb.Flags != scannedPb.Flags || countedPb.Cycles != scannedPb.Cycles
                    || !viaCounts.Works.SequenceEqual(viaScan.Works))
                    throw new InvalidOperationException(
                        $"Prefix-count gate changed results at {previous}->{tick}, loops={loops}, backward={backward}.");
            }
        }
    }

    // Build-time storage dedup (faster queue #4): identical payload bytes
    // share storage through the indirection map (bitwise keys, so +0/-0 and
    // distinct NaN payloads never merge), long clips spanning many regions
    // stop duplicating their CSR rows, and equal storage never merges two
    // authored instances — every (Index, State, value-bits) sequence and
    // callback count must be identical with dedup on and off.
    private static void Dedup()
    {
        // 1. Bit-pattern receipt: four same-window works whose payloads are
        //    +0, -0 and two identical NaNs. The recorded bits must equal the
        //    authored bits exactly — the -0 must not have merged into +0.
        var bits = Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            TrackRef plus = b.Track(new ProbeTrack(0));
            TrackRef minus = b.Track(new ProbeTrack(0));
            TrackRef nanA = b.Track(new ProbeTrack(0));
            TrackRef nanB = b.Track(new ProbeTrack(0));
            b.Clip(plus, new ProbeClip(0f), 0, 4);
            b.Clip(minus, new ProbeClip(-0f), 0, 4);
            b.Clip(nanA, new ProbeClip(float.NaN), 0, 4);
            b.Clip(nanB, new ProbeClip(float.NaN), 0, 4);
        });
        var recorded = new BitsData { Bits = [] };
        Timeline.Forward(bits, ref recorded, 1);
        var seen = recorded.Bits ?? throw new InvalidOperationException("The bits consumer recorded nothing.");
        uint[] expected =
        [
            BitConverter.SingleToUInt32Bits(0f),
            BitConverter.SingleToUInt32Bits(-0f),
            BitConverter.SingleToUInt32Bits(float.NaN),
            BitConverter.SingleToUInt32Bits(float.NaN),
        ];
        if (!seen.SequenceEqual(expected))
            throw new InvalidOperationException($"Payload dedup changed authored value bits: [{string.Join(", ", seen)}] vs [{string.Join(", ", expected)}].");
        if (seen.Count != 4)
            throw new InvalidOperationException("Equal storage merged two authored instances into fewer works.");

        // 2. On/off parity and sizing on a duplication-heavy fixture: eight
        //    long clips spanning 64 regions (payloads from three values) and
        //    a sweeper track cutting four-tick regions.
        ushort Build(bool dedup)
        {
            Timeline.DedupStorage = dedup;
            return Timeline<ProbeTrack, ProbeClip>.Build(b =>
            {
                for (var t = 0; t < 8; t++)
                {
                    TrackRef longs = b.Track(new ProbeTrack(t + 1));
                    b.Clip(longs, new ProbeClip((t % 3) + 1), 0, 64);
                }

                TrackRef sweeper = b.Track(new ProbeTrack(0));
                for (uint i = 0; i < 16; i++)
                    b.Clip(sweeper, new ProbeClip(i % 2), i * 4, i * 4 + 3);
            });
        }

        var deduped = Build(true);
        var plain = Build(false);
        Timeline.DedupStorage = true;

        var dedupEntry = Timeline.Live(deduped);
        var plainEntry = Timeline.Live(plain);
        var dedupTables = (Tl.Hooks.Timeline<ProbeTrack, ProbeClip>.Tables)dedupEntry.Payload!;
        if (dedupTables.PayloadMap.Length != 24)
            throw new InvalidOperationException($"The payload map must stay one entry per authored clip; got {dedupTables.PayloadMap.Length}.");
        if (dedupTables.ClipData.Length != 4)
            throw new InvalidOperationException($"Expected 4 unique payloads (long values {{1,2,3}} plus the sweeper's 0; the sweeper's 1 collides with a long value); got {dedupTables.ClipData.Length}.");
        if (dedupEntry.TrackRows.Length >= plainEntry.TrackRows.Length)
            throw new InvalidOperationException($"Row dedup did not compact: {dedupEntry.TrackRows.Length} vs {plainEntry.TrackRows.Length}.");
        if (dedupEntry.ClipRows.Length >= plainEntry.ClipRows.Length)
            throw new InvalidOperationException($"Clip-row dedup did not compact: {dedupEntry.ClipRows.Length} vs {plainEntry.ClipRows.Length}.");

        // Parity: linear walk and a jump battery — works, sums, flags.
        for (uint tick = 0; tick < 70; tick++)
        {
            var from = At(tick == 0 ? 0 : tick - 1);
            var viaDedup = new Probe();
            var viaPlain = new Probe();
            var dedupPb = Timeline.Forward(deduped, in from, ref viaDedup, tick);
            var plainPb = Timeline.Forward(plain, in from, ref viaPlain, tick);
            if (dedupPb.Flags != plainPb.Flags || !viaDedup.Works.SequenceEqual(viaPlain.Works) || viaDedup.Sum != viaPlain.Sum)
                throw new InvalidOperationException($"Dedup changed results at {tick}.");
        }

        uint random = 0x85EBCA6Bu;
        for (var j = 0; j < 32; j++)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var from = random % 70;
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var to = random % 70;
            foreach (var backward in new[] { false, true })
            {
                var start = At(from);
                var viaDedup = new Probe();
                var viaPlain = new Probe();
                var dedupPb = backward
                    ? Timeline.Backward(deduped, in start, ref viaDedup, to)
                    : Timeline.Forward(deduped, in start, ref viaDedup, to);
                var plainPb = backward
                    ? Timeline.Backward(plain, in start, ref viaPlain, to)
                    : Timeline.Forward(plain, in start, ref viaPlain, to);
                if (dedupPb.Flags != plainPb.Flags || !viaDedup.Works.SequenceEqual(viaPlain.Works) || viaDedup.Sum != viaPlain.Sum)
                    throw new InvalidOperationException($"Dedup changed jump results at {from}->{to}, backward={backward}.");
            }
        }
    }

    private static void Fixture()
    {
        var shape = new ApiShape();
        var linear = shape.BuildTimeline();
        var looping = shape.BuildTimeline(loops: true);
        foreach (bool loops in new[] { false, true })
        {
            var timeline = loops ? looping : linear;
            var a = Timeline.Start(timeline);
            var b = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
            for (uint tick = 0; tick < 1300; tick++)
            {
                var x = new Vitals();
                var y = new Vitals();
                a = Timeline.Forward(timeline, in a, ref x, tick);
                b = loops
                    ? GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in b, ref y, tick)
                    : GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in b, ref y, tick);
                Require(a.Flags == b.Flags && a.Cycles == b.Cycles && x.Result == y.Result, $"Generated/runtime fixture diverged at {tick}, loops={loops}.");
            }
        }
    }

    private static void Limits()
    {
        Reject<ArgumentOutOfRangeException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b => b.Clip(default, new ProbeClip(1), 0, 1)));
        Reject<ArgumentOutOfRangeException>(() => Timeline<ProbeTrack, ProbeClip>.Build(static b => b.Clip(new TrackRef(999), new ProbeClip(1), 0, 1)));
        Reject<ArgumentOutOfRangeException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            b.Clip(track, new ProbeClip(1), 7, 7);
        }));
        Reject<InvalidOperationException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            for (int i = 0; i < ushort.MaxValue; i++)
                b.Clip(track, new ProbeClip(1), (uint)i, (uint)i + 1);
            b.Clip(track, new ProbeClip(1), 0, 1);
        }));
        Reject<NotSupportedException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            b.Clip(track, new ProbeClip(1), 0, 10);
            b.Clip(track, new ProbeClip(1), 2, 12);
            b.Clip(track, new ProbeClip(1), 4, 14);
        }));
        Reject<OverflowException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            for (uint i = 0; i < 400; i++)
            {
                TrackRef t = b.Track(new ProbeTrack(0));
                b.Clip(t, new ProbeClip(1), 0, i + 1);
            }
        }));
        Reject<InvalidOperationException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            for (int i = 0; i < ushort.MaxValue; i++)
                b.Track(new ProbeTrack(i));
            b.Track(new ProbeTrack(0));
        }));
    }

    // The VitalsTrack fixture as authored (Benchmarks.cs BuildTimeline): one
    // row per clip instance (track, true window, payload), in authoring
    // order — the same order VitalsTrack.ClipEdges uses. The per-work oracle
    // reads this raw list, never the CSR tables, and cross-checks the
    // windows against ClipEdges so the two can never drift.
    internal static readonly (int Track, uint Start, uint End, float Amount)[] AuthoredVitals =
    [
        (0, 0, 7, 1f), (0, 29, 47, 13f), (0, 29, 47, 21f), (0, 47, 76, 13f), (0, 321, 515, 1f),
        (1, 3, 7, 2f), (1, 3, 11, 3f), (1, 47, 76, 2f), (1, 76, 123, 2f), (1, 515, 600, 3f),
        (2, 18, 29, 8f), (2, 76, 123, 8f), (2, 321, 515, 8f),
        (3, 3, 11, 5f), (3, 18, 29, 5f), (3, 76, 123, 34f), (3, 76, 123, 55f), (3, 200, 321, 5f), (3, 321, 515, 5f),
    ];

    // Per-work facts oracle: every (track, ClipState) the engine hands the
    // consumer must equal values computed straight from the authored clip
    // list with the pinned rules: Exit positional at the window's last
    // active frame (End-1 forward, Start backward) with precedence over
    // Enter; Enter when the step's span crossed the entry edge (Start
    // forward, End backward; a wrapped span crosses two ranges; a
    // multi-cycle jump crossed everything); Stay otherwise. A blend-resolved
    // work reports the pair's OUTER window — the convention pinned here. A
    // clip fully crossed during a jump is not active at the destination,
    // gets no row, and is invisible — same as the pinned semantics.
    private static void PerClip()
    {
        Require(AuthoredVitals.Length == VitalsTrack.ClipEdges.Length, "Authored clip count diverged from ClipEdges.");
        for (var i = 0; i < AuthoredVitals.Length; i++)
            Require(AuthoredVitals[i].Start == VitalsTrack.ClipEdges[i].Start
                    && AuthoredVitals[i].End == VitalsTrack.ClipEdges[i].End,
                $"Authored clip {i} diverged from ClipEdges.");

        // 1. Full forward walk 0..610 (past the 600 duration into the empty
        //    sentinel region): every work's (Index, State) checked against
        //    the reference at each tick.
        var facts = new Facts { Calls = [] };
        var pc = GeneratedTimeline<VitalsTrack, VitalsClip>.Start();
        for (uint t = 0; t <= 610; t++)
        {
            var expected = WorksAt(t, pc.Tick, backward: false, wrapped: false, full: false);
            var mark = facts.Calls!.Count;
            pc = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pc, ref facts, t);
            RequireWorks(t, facts.Calls!, mark, expected);
        }

        // Structural facts of a plain forward walk: Exit rides the window's
        // last active frame; Enter rides the window start after the silent
        // start at tick 0 (a repeated position crosses nothing).
        foreach (var call in facts.Calls!)
        {
            var (start, end) = WindowOf(call.Track, call.Tick);
            if (call.State == ClipState.Exit)
                Require(call.Tick == end - 1, $"forward tick {call.Tick}: Exit must be positional at End-1.");
            if (call.State == ClipState.Enter)
                Require(call.Tick == start && call.Tick != 0, $"forward tick {call.Tick}: Enter must ride the window start after the silent start.");
        }

        // 2. Backward mirror: rewind the same walk tick by tick. Direction
        //    inverts — enter through End, exit through Start — so Enter now
        //    rides Last, and the emitted (track, tick) sequence is exactly
        //    the forward sequence reversed.
        var rewinds = new Facts { Calls = [] };
        var pr = pc;
        for (var t = 609; t >= 0; t--)
        {
            var expected = WorksAt((uint)t, (uint)t + 1, backward: true, wrapped: false, full: false);
            var mark = rewinds.Calls!.Count;
            pr = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in pr, ref rewinds, (uint)t);
            RequireWorks((uint)t, rewinds.Calls!, mark, expected);
        }
        foreach (var call in rewinds.Calls!)
        {
            var (start, end) = WindowOf(call.Track, call.Tick);
            if (call.State == ClipState.Exit)
                Require(call.Tick == start, $"backward tick {call.Tick}: Exit must be positional at the window Start.");
            if (call.State == ClipState.Enter)
                Require(call.Tick == end - 1, $"backward tick {call.Tick}: Enter must ride the window's last frame (enter through End).");
        }
        Require(rewinds.Calls.Count == facts.Calls!.Count,
            $"rewind emitted {rewinds.Calls.Count} calls, forward emitted {facts.Calls.Count}.");

        // The rewind emits the forward walk's calls in reverse order —
        // reversed at tick granularity: ticks descend, and within a tick
        // both directions emit works in ascending track order.
        var mirrored = new List<(int Track, uint Tick, ClipState State)>();
        for (var i = facts.Calls.Count - 1; i >= 0; i--)
        {
            var tick = facts.Calls[i].Tick;
            var start = i;
            while (start > 0 && facts.Calls[start - 1].Tick == tick)
                start--;
            for (var k = start; k <= i; k++)
                mirrored.Add(facts.Calls[k]);
            i = start;
        }
        for (var i = 0; i < mirrored.Count; i++)
        {
            var mirror = rewinds.Calls[i];
            Require(mirrored[i].Track == mirror.Track && mirrored[i].Tick == mirror.Tick,
                $"rewind sequence is not the forward sequence reversed at call {i}.");
        }

        // 3. Deterministic jump battery (the xorshift battery the frozen
        //    parity receipts use): 24 [from, to] pairs in range and past the
        //    duration, forward and backward — states against the reference.
        uint random = 0x6D2B79F5u;
        for (var j = 0; j < 24; j++)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var from = random % 620;
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var to = random % 620;

            foreach (var backward in new[] { false, true })
            {
                var jumps = new Facts { Calls = [] };
                var jp = GeneratedTimeline<VitalsTrack, VitalsClip>.Start(from);
                if (backward)
                    jp = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in jp, ref jumps, to);
                else
                    jp = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in jp, ref jumps, to);

                var expected = WorksAt(to, from, backward, wrapped: false, full: false);
                RequireWorks(to, jumps.Calls!, 0, expected);
            }
        }

        // 4. Wrapping steps on the looping variant (LoopVitalsTrack): a
        //    wrapped span crosses (prevEff, duration) then [0, tEff], and a
        //    multi-cycle jump is full coverage — every boundary crossed. The
        //    reference derives prevEff/tEff/wrapped/full from the authored
        //    duration exactly the way PlaybackCore.Position does.
        foreach (var (from, to, backward) in new (uint From, uint To, bool Backward)[]
                 {
                     (599, 685, false), (1199, 5, false), (0, 1205, false), (599, 5, false),
                     (0, 599, true), (5, 1199, true), (685, 5, true), (1199, 1198, true),
                 })
        {
            const uint duration = 600;
            var (tEff, prevEff, wrapped, full) = Locate(from, to, duration, loops: true, backward);

            var loops = new Facts { Calls = [] };
            var lc = GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Start(from);
            lc = backward
                ? GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Backward(in lc, ref loops, to)
                : GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lc, ref loops, to);

            var expected = WorksAt(tEff, prevEff, backward, wrapped, full);
            RequireWorks(tEff, loops.Calls!, 0, expected);
        }
    }

    // The effective positions and wrap shape of one step, derived exactly
    // the way PlaybackCore.Position does.
    private static (uint TEff, uint PrevEff, bool Wrapped, bool Full) Locate(
        uint previous, uint tick, uint duration, bool loops, bool backward)
    {
        if (!loops || duration == 0)
            return (tick, previous, false, false);

        var prevEff = previous % duration;
        var tEff = tick % duration;

        if (!backward)
        {
            var cycles = tick >= previous
                ? tick / duration - previous / duration
                : tEff < prevEff ? 1u : 0u;
            return (tEff, prevEff, cycles == 1, cycles >= 2);
        }

        if (tick <= previous)
        {
            var cycles = previous / duration - tick / duration;
            return (tEff, prevEff, cycles == 1, cycles >= 2);
        }

        return tEff > prevEff ? (tEff, prevEff, true, false) : (tEff, prevEff, false, false);
    }

    // The expected ClipState of one window at one step, straight from the
    // pinned rules.
    private static ClipState StateOracle(uint t, uint prevEff, uint start, uint end, bool backward, bool wrapped, bool full)
    {
        if (backward ? t == start : t == end - 1)
            return ClipState.Exit;

        var boundary = backward ? end : start;
        var crossed = full
            || (backward
                ? wrapped ? boundary > t || boundary <= prevEff : boundary > t && boundary <= prevEff
                : wrapped ? boundary > prevEff || boundary <= t : boundary > prevEff && boundary <= t);
        return crossed ? ClipState.Enter : ClipState.Stay;
    }

    // The expected per-work rows at one tick: for each track, the active
    // window — a single clip's edge, or the OUTER window of an overlapping
    // pair (min Start, max End), the blend convention this oracle pins —
    // enumerated straight from the authored list, never the CSR tables.
    private static List<(int Track, ClipState State)> WorksAt(uint t, uint prevEff, bool backward, bool wrapped, bool full)
    {
        var works = new List<(int, ClipState)>();
        for (var track = 0; track <= 3; track++)
        {
            var count = 0;
            uint start = 0, end = 0;
            foreach (var clip in AuthoredVitals)
            {
                if (clip.Track != track || clip.Start > t || clip.End <= t)
                    continue;
                if (count == 0)
                {
                    start = clip.Start;
                    end = clip.End;
                }
                else
                {
                    if (clip.Start < start) start = clip.Start;
                    if (clip.End > end) end = clip.End;
                }
                count++;
            }

            if (count == 0)
                continue;

            works.Add((track, StateOracle(t, prevEff, start, end, backward, wrapped, full)));
        }

        return works;
    }

    // The OUTER window of the track's active clips at one tick (for the
    // structural receipts).
    private static (uint Start, uint End) WindowOf(int track, uint t)
    {
        uint start = uint.MaxValue, end = 0;
        foreach (var clip in AuthoredVitals)
        {
            if (clip.Track != track || clip.Start > t || clip.End <= t)
                continue;
            if (clip.Start < start) start = clip.Start;
            if (clip.End > end) end = clip.End;
        }

        return (start, end);
    }

    // Compares the calls a consumer recorded after `mark` against the
    // expected works, in order, for one tick.
    private static void RequireWorks(
        uint tick, List<(int Track, uint Tick, ClipState State)> calls, int mark,
        List<(int Track, ClipState State)> expected)
    {
        if (calls.Count - mark != expected.Count)
            throw new InvalidOperationException($"tick {tick}: {calls.Count - mark} works, expected {expected.Count}.");
        for (var i = 0; i < expected.Count; i++)
        {
            var call = calls[mark + i];
            if (call.Tick != tick || call.Track != expected[i].Track)
                throw new InvalidOperationException(
                    $"tick {tick} work {i}: call for track {call.Track} at {call.Tick}, expected track {expected[i].Track}.");
            if (call.State != expected[i].State)
                throw new InvalidOperationException(
                    $"tick {tick} track {expected[i].Track}: state {call.State} != reference {expected[i].State}.");
        }
    }

    internal readonly record struct ProbeClip(float Value);
    internal readonly record struct ProbeTrack(float Bias) : IBlend<ProbeClip>
    {
        public void Blend(in ProbeClip first, in ProbeClip second, float t, out ProbeClip result)
            => result = new(first.Value * (1f - t) + second.Value * t + Bias);
    }

    // A 260-byte blend payload: comfortably inside the 4,096-byte stack
    // budget for a few concurrent blends.
    internal unsafe struct BigClip
    {
        public fixed byte Pad[256];
        public float Value;

        public BigClip(float value)
        {
            Value = value;
        }
    }

    // A 4,204-byte blend payload: a single slot already exceeds the stack
    // budget, so only the caller-buffer overload can play it.
    internal unsafe struct HugeClip
    {
        public fixed byte Pad[4200];
        public float Value;

        public HugeClip(float value)
        {
            Value = value;
        }
    }

    internal struct BigTrack : IBlend<BigClip>
    {
        public void Blend(in BigClip first, in BigClip second, float t, out BigClip result)
            => result = new BigClip(first.Value * (1f - t) + second.Value * t);
    }

    internal struct HugeTrack : IBlend<HugeClip>
    {
        public void Blend(in HugeClip first, in HugeClip second, float t, out HugeClip result)
            => result = new HugeClip(first.Value * (1f - t) + second.Value * t);
    }

    // Sums every work's resolved clip regardless of state, so blend values
    // are visible even on one-frame windows.
    internal struct BigData :
        IForward<BigTrack, BigClip, BigData>,
        IBackward<BigTrack, BigClip, BigData>
    {
        public float Sum;
        public int Count;

        public void Forward(ref BigData data, in Tracks<BigTrack, BigClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum += work.Clip.Value;
            data.Count++;
        }

        public void Backward(ref BigData data, in Tracks<BigTrack, BigClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum -= work.Clip.Value;
            data.Count--;
        }
    }

    internal struct HugeData :
        IForward<HugeTrack, HugeClip, HugeData>,
        IBackward<HugeTrack, HugeClip, HugeData>
    {
        public float Sum;
        public int Count;

        public void Forward(ref HugeData data, in Tracks<HugeTrack, HugeClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum += work.Clip.Value;
            data.Count++;
        }

        public void Backward(ref HugeData data, in Tracks<HugeTrack, HugeClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum -= work.Clip.Value;
            data.Count--;
        }
    }

    // Reentrancy witness: the consumer of one blended timeline plays a
    // second blended timeline from inside its callback, each call resolving
    // into its own scratch (the inner one is a stack buffer inside the
    // callback — never a shared global).
    internal struct NestedData :
        IForward<BigTrack, BigClip, NestedData>,
        IBackward<BigTrack, BigClip, NestedData>
    {
        public float Sum;
        public int Count;
        public ushort Inner;
        public BigData InnerData;

        public void Forward(ref NestedData data, in Tracks<BigTrack, BigClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum += work.Clip.Value;
            data.Count++;

            Span<BigClip> inner = stackalloc BigClip[1];
            var pb = new Playback(tick, 0, PlaybackFlags.Started);
            Timeline<BigTrack, BigClip>.Forward(data.Inner, in pb, ref data.InnerData, inner, tick);
        }

        public void Backward(ref NestedData data, in Tracks<BigTrack, BigClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Sum -= work.Clip.Value;
            data.Count--;

            Span<BigClip> inner = stackalloc BigClip[1];
            var pb = new Playback(tick, 0, PlaybackFlags.Started);
            Timeline<BigTrack, BigClip>.Backward(data.Inner, in pb, ref data.InnerData, inner, tick);
        }
    }

    // Records the raw value bits of every work it sees, for the payload
    // dedup receipts.
    internal struct BitsData :
        IForward<ProbeTrack, ProbeClip, BitsData>,
        IBackward<ProbeTrack, ProbeClip, BitsData>
    {
        public List<uint>? Bits;

        public void Forward(ref BitsData data, in Tracks<ProbeTrack, ProbeClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Bits!.Add(BitConverter.SingleToUInt32Bits(work.Clip.Value));
        }

        public void Backward(ref BitsData data, in Tracks<ProbeTrack, ProbeClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Bits!.Add(BitConverter.SingleToUInt32Bits(work.Clip.Value));
        }
    }

    // The probe consumer: records every (Index, State) work it sees and
    // accumulates clip values on Stay only, mirroring the pinned
    // boundary-frame rule. Backward runs the same body — the receipts
    // compare direction-specific expectations separately.
    internal struct Probe :
        IForward<ProbeTrack, ProbeClip, Probe>,
        IBackward<ProbeTrack, ProbeClip, Probe>
    {
        public float Sum;
        public int Tracks;
        public int Count;
        public uint Tick;
        public List<(ushort Index, ClipState State)> Works = [];

        public Probe()
        {
        }

        public void Forward(ref Probe data, in Tracks<ProbeTrack, ProbeClip> tracks, in uint tick)
        {
            data.Tick = tick;
            data.Tracks = tracks.Count;
            foreach (var work in tracks)
            {
                if (work.State == ClipState.Stay)
                    data.Sum += work.Clip.Value;
                data.Works.Add((work.Index, work.State));
            }
            data.Count++;
        }

        public void Backward(ref Probe data, in Tracks<ProbeTrack, ProbeClip> tracks, in uint tick) => Forward(ref data, in tracks, in tick);
    }

    // Records every work (track index, tick, state) for the Vitals-fixture
    // oracle, through both the linear and the looping closure.
    internal struct Facts :
        IForward<VitalsTrack, VitalsClip, Facts>,
        IBackward<VitalsTrack, VitalsClip, Facts>,
        IForward<LoopVitalsTrack, VitalsClip, Facts>,
        IBackward<LoopVitalsTrack, VitalsClip, Facts>
    {
        public List<(int Track, uint Tick, ClipState State)>? Calls;

        public void Forward(ref Facts data, in Tracks<VitalsTrack, VitalsClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Calls!.Add((work.Index, tick, work.State));
        }

        public void Backward(ref Facts data, in Tracks<VitalsTrack, VitalsClip> tracks, in uint tick)
            => Forward(ref data, in tracks, in tick);

        public void Forward(ref Facts data, in Tracks<LoopVitalsTrack, VitalsClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Calls!.Add((work.Index, tick, work.State));
        }

        public void Backward(ref Facts data, in Tracks<LoopVitalsTrack, VitalsClip> tracks, in uint tick)
            => Forward(ref data, in tracks, in tick);
    }
}
