using System.Runtime.CompilerServices;

namespace Tl.Hooks;

internal static class EdgeVerification
{
    public static void Run()
    {
        var failures = new List<string>();
        foreach (var test in new Action[] { Gaps, Empty, Blend, Order, Packing, Wrap, States, Batches, Fixture, PerClip, WideTicks, Limits, Indices })
        {
            try { test(); }
            catch (Exception e) { failures.Add($"{test.Method.Name}: {e.Message}"); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("Edge checks passed: gaps, terminal ticks, empty tables, blends, packing, wraps, per-work state oracle, fixture parity, per-work facts oracle, limits.");
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
