using System.Runtime.CompilerServices;

namespace Tl.Hooks;

internal static class EdgeVerification
{
    public static void Run()
    {
        var failures = new List<string>();
        foreach (var test in new Action[] { Gaps, Empty, Blend, Order, Packing, Wrap, Flags, Batches, Fixture, PerClip, WideTicks, Limits, Indices })
        {
            try { test(); }
            catch (Exception e) { failures.Add($"{test.Method.Name}: {e.Message}"); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("Edge checks passed: gaps, terminal ticks, empty tables, blends, packing, wraps, flag oracle, fixture parity, per-clip facts oracle, limits.");
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

    // Looping is an authored trait now, so a looping variant is a second
    // Build, not a post-build toggle.
    private static ushort Make(bool loops, params ReadOnlySpan<ClipEdge> edges)
    {
        var authoring = edges.ToArray();
        return Timeline<ProbeTrack, ProbeClip>.Build(b =>
        {
            foreach (var edge in authoring)
            {
                int track = b.Track(new ProbeTrack(0));
                b.Clip(track, new ProbeClip(10), edge.Start, edge.End);
            }

            if (loops)
                b.Looping();
        });
    }

    private static void Gaps()
    {
        var timeline = Make(new ClipEdge(10, 20));
        foreach (uint tick in new uint[] { 0, 9, 10, 19, 20, 21, uint.MaxValue })
        {
            var probe = new Probe();
            Timeline<ProbeTrack, ProbeClip>.Forward(timeline, ref probe, tick);
            Require(probe.Tracks == (tick >= 10 && tick < 20 ? 1 : 0), $"Wrong active tracks at {tick}.");
            var from = Playback.Start(19);
            probe = default;
            var state = Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref probe, tick);
            Require(state.Flags == Oracle([new(10, 20)], 19, tick, 20, false, false), $"Wrong terminal flags at {tick}.");
        }
    }

    private static void Empty()
    {
        var timeline = Make();
        var data = new Probe();
        var from = Playback.Start();
        var result = Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref data, 0, 10);
        Require(data.Count == 2 && data.Tracks == 0 && result.Has(PlaybackFlags.Complete), "Empty non-looping timeline did not complete.");
        var looping = Make(true);
        result = Timeline<ProbeTrack, ProbeClip>.Forward(looping, in from, ref data, 10);
        Require(result.Cycles == 0 && result.Flags == PlaybackFlags.None, "Empty loop should have no movement facts.");
    }

    private static void Blend()
    {
        var timeline = Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(2));
            b.Clip(track, new ProbeClip(10), 4, 5);
            b.Clip(track, new ProbeClip(30), 4, 5);
        });
        var data = new Probe();
        Timeline<ProbeTrack, ProbeClip>.Forward(timeline, ref data, 4);
        Require(data.Sum == 22, $"One-tick blend with authored track data returned {data.Sum}, expected 22.");
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
        Timeline<ProbeTrack, ProbeClip>.Forward(timeline, ref data, 5);
        Require(data.Sum == 10, $"Crossfade should start with the earlier clip; got {data.Sum}.");
    }

    private static void Packing()
    {
        Require(Unsafe.SizeOf<Playback>() == 8, "Playback grew beyond 8 bytes.");
        const uint max = 0x03FF_FFFFu;
        Reject<ArgumentOutOfRangeException>(() => _ = new Playback(0, max + 1, PlaybackFlags.None));
        var timeline = Make(true, new ClipEdge(0, 1));
        var from = new Playback(0, max, PlaybackFlags.None);
        var data = new Probe();
        Reject<ArgumentOutOfRangeException>(() => Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref data, 1));
        Require(data.Count == 0, "Overflow invoked a callback before rejecting the step.");
    }

    private static void Wrap()
    {
        var timeline = Make(true, new ClipEdge(0, 10));
        var from = Playback.Start(9);
        var data = new Probe();
        var next = Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref data, 0);
        Require(next.Cycles == 1 && next.Flags == (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.First | PlaybackFlags.Active), $"Local forward wrap corrupted state: {next.Cycles}, {next.Flags}.");
        Timeline<ProbeTrack, ProbeClip>.Forward(timeline, ref data, 11);
        Require(data.Tick == 1 && data.Tracks == 1, "Stateless looping playback must normalize the tick too.");
    }

    private static void Flags()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        var linear = Make(edges);
        var looping = Make(true, edges);
        foreach (bool loops in new[] { false, true })
        foreach (bool backward in new[] { false, true })
        {
            var timeline = loops ? looping : linear;
            for (uint previous = 0; previous < 85; previous++)
            for (uint tick = 0; tick < 85; tick++)
            {
                var from = Playback.Start(previous);
                var data = new Probe();
                var result = backward ? Timeline<ProbeTrack, ProbeClip>.Backward(timeline, in from, ref data, tick) : Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref data, tick);
                var expected = Oracle(edges, previous, tick, 28, loops, backward);
                Require(result.Flags == expected && data.Status == expected, $"Flags at {previous}->{tick}, loops={loops}, backward={backward}: {result.Flags} != {expected}.");
            }
        }
    }

    private static PlaybackFlags Oracle(ReadOnlySpan<ClipEdge> edges, uint previous, uint tick, uint duration, bool loops, bool backward)
    {
        var flags = PlaybackFlags.None;
        uint p = loops ? previous % duration : previous;
        uint t = loops ? tick % duration : tick;
        foreach (var edge in edges)
        {
            if (t >= edge.Start && t < edge.End) flags |= PlaybackFlags.Active;
            if (t == edge.Start) flags |= PlaybackFlags.First;
            if (t == edge.End - 1) flags |= PlaybackFlags.Last;
        }
        if (!loops && (backward ? t == 0 : duration == 0 || t >= duration - 1)) flags |= PlaybackFlags.Complete;
        long distance = backward ? (long)previous - tick : (long)tick - previous;
        if (loops && distance < 0) distance = backward ? (long)p - t : (long)t - p;
        if (loops && distance < 0) distance += duration;
        if (distance <= 0) return flags;
        foreach (var edge in edges)
        {
            if (!loops)
            {
                if (backward ? t < edge.End && edge.End <= p : p < edge.Start && edge.Start <= t) flags |= PlaybackFlags.Enter;
                if (backward ? t < edge.Start && edge.Start <= p : p < edge.End && edge.End <= t) flags |= PlaybackFlags.Exit;
                continue;
            }
            for (long step = 1; step <= distance; step++)
            {
                long boundary = backward ? (long)p - step + 1 : (long)p + step;
                uint cut = (uint)((boundary % duration + duration) % duration);
                if (cut == (backward ? edge.End : edge.Start) % duration) flags |= PlaybackFlags.Enter;
                if (cut == (backward ? edge.Start : edge.End) % duration) flags |= PlaybackFlags.Exit;
            }
        }
        return flags;
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
            var single = new Probe { Trace = [] };
            var batch = new Probe { Trace = [] };
            var state = Playback.Start();
            foreach (uint tick in ticks)
                state = backward ? Timeline<ProbeTrack, ProbeClip>.Backward(timeline, in state, ref single, tick) : Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in state, ref single, tick);
            var from = Playback.Start();
            var result = backward ? Timeline<ProbeTrack, ProbeClip>.Backward(timeline, in from, ref batch, ticks) : Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref batch, ticks);
            Require(single.Trace!.SequenceEqual(batch.Trace!) && single.Sum == batch.Sum && state.Cycles == result.Cycles && state.Flags == result.Flags, "Batch cursor changed sampling or movement facts.");
        }
    }

    private static void WideTicks()
    {
        var timeline = Make(new ClipEdge(uint.MaxValue - 2, uint.MaxValue));
        var data = new Probe();
        var from = Playback.Start(uint.MaxValue - 3);
        var active = Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in from, ref data, uint.MaxValue - 1);
        Require(active.Flags == (PlaybackFlags.Enter | PlaybackFlags.Active | PlaybackFlags.Last | PlaybackFlags.Complete), "Wide tick lost last/complete flags.");
        var end = Timeline<ProbeTrack, ProbeClip>.Forward(timeline, in active, ref data, uint.MaxValue);
        Require(end.Flags == (PlaybackFlags.Exit | PlaybackFlags.Complete) && data.Tracks == 0, "Wide terminal tick did not exit.");
    }

    private static void Indices()
    {
        ushort next = Timeline<ProbeTrack, ProbeClip>.Build(static b => { });
        for (int i = next + 1; i <= ushort.MaxValue; i++)
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
            var a = Playback.Start();
            var b = a;
            for (uint tick = 0; tick < 1300; tick++)
            {
                var x = new Vitals();
                var y = new Vitals();
                a = Timeline<VitalsTrack, VitalsClip>.Forward(timeline, in a, ref x, tick);
                b = loops ? GeneratedTimeline<LoopVitalsTrack, VitalsClip>.Forward(in b, ref y, tick) : GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in b, ref y, tick);
                Require(a.Flags == b.Flags && a.Cycles == b.Cycles && x.Result == y.Result, $"Generated/runtime fixture diverged at {tick}, loops={loops}.");
            }
        }
    }

    private static void Limits()
    {
        Reject<ArgumentOutOfRangeException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b => b.Clip(0, new ProbeClip(1), 0, 1)));
        Reject<ArgumentOutOfRangeException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            b.Clip(-1, new ProbeClip(1), 0, 1);
        }));
        Reject<InvalidOperationException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            var track = b.Track(new ProbeTrack(0));
            for (int i = 0; i < ushort.MaxValue; i++)
                b.Clip(track, new ProbeClip(1), (uint)i, (uint)i + 1);
            b.Clip(track, new ProbeClip(1), 0, 1);
        }));
        Reject<OverflowException>(static () => Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            for (uint i = 0; i < 400; i++)
            {
                int t = b.Track(new ProbeTrack(0));
                b.Clip(t, new ProbeClip(1), 0, i + 1);
            }
        }));
    }

    internal readonly record struct ProbeClip(float Value);
    internal readonly record struct ProbeTrack(float Bias) : IBlend<ProbeClip>
    {
        public void Blend(in ProbeClip first, in ProbeClip second, float factor, out ProbeClip result)
            => result = new(first.Value * (1f - factor) + second.Value * factor + Bias);
    }
    internal struct Probe : IForwardTracks<ProbeTrack, ProbeClip, Probe>, IBackwardTracks<ProbeTrack, ProbeClip, Probe>
    {
        public float Sum;
        public int Tracks;
        public int Count;
        public uint Tick;
        public PlaybackFlags Status;
        public List<PlaybackFlags>? Trace;
        public void OnTick(in Tracks<ProbeTrack, ProbeClip> tracks, uint tick, ref Probe data)
        {
            data.Tick = tick;
            data.Status = tracks.Status;
            data.Trace?.Add(tracks.Status);
            data.Tracks = tracks.Count;
            foreach (var item in tracks) data.Sum += item.Clip.Value;
            data.Count++;
        }
        public void OnTickBack(in Tracks<ProbeTrack, ProbeClip> tracks, uint tick, ref Probe data) => OnTick(in tracks, tick, ref data);
    }

    // The VitalsTrack fixture as authored (Benchmarks.cs BuildTimeline):
    // one row per clip instance (track, true window), in authoring order —
    // the same order VitalsTrack.ClipEdges uses. The per-clip oracle reads
    // this raw list, never the CSR tables, and cross-checks the windows
    // against ClipEdges so the two can never drift.
    private static readonly (int Track, uint Start, uint End)[] s_authored =
    [
        (0, 0, 7), (0, 29, 47), (0, 29, 47), (0, 47, 76), (0, 321, 515),
        (1, 3, 7), (1, 3, 11), (1, 47, 76), (1, 76, 123), (1, 515, 600),
        (2, 18, 29), (2, 76, 123), (2, 321, 515),
        (3, 3, 11), (3, 18, 29), (3, 76, 123), (3, 76, 123), (3, 200, 321), (3, 321, 515),
    ];

    // Per-clip facts oracle. Every clipState the engine hands OnClip — and
    // every item.State the Tracks view exposes — must equal a word computed
    // straight from the authored clip list with the rules of
    // TrackItem.State: Active always (an item exists => its clip is active
    // at the tick), First/Last positional from the window edges, Enter/Exit
    // only when the step moved (forward enters through Start, backward
    // through End; a wrapped span crosses two ranges; a multi-cycle jump
    // crossed everything). A blend-resolved item reports the pair's OUTER
    // window (min Start, max End) — the convention pinned here. A clip that
    // fully crossed during a jump is not active at the destination, gets no
    // call, and appears only in the aggregate — same as the aggregate
    // oracle's semantics for such clips.
    private static void PerClip()
    {
        Require(s_authored.Length == VitalsTrack.ClipEdges.Length, "Authored clip count diverged from ClipEdges.");
        for (var i = 0; i < s_authored.Length; i++)
            Require(s_authored[i].Start == VitalsTrack.ClipEdges[i].Start
                    && s_authored[i].End == VitalsTrack.ClipEdges[i].End,
                $"Authored clip {i} diverged from ClipEdges.");

        // 1. Full forward walk 0..610 (past the 600 duration into the empty
        //    sentinel region): every OnClip word and every view-engine
        //    item.State checked against the reference at each tick.
        var clips = new ClipFacts { Calls = [] };
        var items = new ItemFacts { Calls = [] };
        var pc = Playback.Start();
        var pv = Playback.Start();
        for (uint t = 0; t <= 610; t++)
        {
            var expected = ItemsAt(t, pc.Tick, backward: false, wrapped: false, full: false);
            var clipMark = clips.Calls!.Count;
            var itemMark = items.Calls!.Count;
            pc = ClipTimeline<VitalsTrack, VitalsClip>.Forward(in pc, ref clips, t);
            pv = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in pv, ref items, t);
            RequireWords(t, clips.Calls!, clipMark, expected);
            RequireWords(t, items.Calls!, itemMark, expected);
        }

        // Structural facts of a plain forward walk: an item enters through
        // its Start — Enter rides First, except the silent start at tick 0,
        // where positioning is not movement — and can never Exit while it
        // is still active (its Exit belongs to the aggregate word).
        foreach (var call in clips.Calls!)
        {
            Require((call.State & PlaybackFlags.Exit) == 0,
                $"forward tick {call.Tick}: an active item cannot Exit on a plain step.");
            Require(((call.State & PlaybackFlags.Enter) != 0) == (((call.State & PlaybackFlags.First) != 0) && call.Tick != 0),
                $"forward tick {call.Tick}: Enter must ride First after the silent start.");
        }

        // 2. Backward mirror: rewind the same walk tick by tick. Direction
        //    inverts — enter through End, exit through Start — so Enter now
        //    rides Last, and the emitted (track, tick) sequence is exactly
        //    the forward sequence reversed.
        var rewinds = new ClipFacts { Calls = [] };
        var pr = pc;
        for (var t = 609; t >= 0; t--)
        {
            var expected = ItemsAt((uint)t, (uint)t + 1, backward: true, wrapped: false, full: false);
            var mark = rewinds.Calls!.Count;
            pr = ClipTimeline<VitalsTrack, VitalsClip>.Backward(in pr, ref rewinds, (uint)t);
            RequireWords((uint)t, rewinds.Calls!, mark, expected);
        }
        foreach (var call in rewinds.Calls!)
        {
            Require((call.State & PlaybackFlags.Exit) == 0,
                $"backward tick {call.Tick}: an active item cannot Exit on a plain rewind.");
            Require(((call.State & PlaybackFlags.Enter) != 0) == ((call.State & PlaybackFlags.Last) != 0),
                $"backward tick {call.Tick}: Enter must ride Last (enter through End).");
        }
        Require(rewinds.Calls.Count == clips.Calls!.Count,
            $"rewind emitted {rewinds.Calls.Count} calls, forward emitted {clips.Calls.Count}.");

        // The rewind emits the forward walk's calls in reverse order —
        // reversed at tick granularity: ticks descend, and within a tick
        // both engines emit items in ascending track order.
        var mirrored = new List<(int Track, uint Tick, PlaybackFlags State)>();
        for (var i = clips.Calls.Count - 1; i >= 0; i--)
        {
            var tick = clips.Calls[i].Tick;
            var start = i;
            while (start > 0 && clips.Calls[start - 1].Tick == tick)
                start--;
            for (var k = start; k <= i; k++)
                mirrored.Add(clips.Calls[k]);
            i = start;
        }
        for (var i = 0; i < mirrored.Count; i++)
        {
            var mirror = rewinds.Calls[i];
            Require(mirrored[i].Track == mirror.Track && mirrored[i].Tick == mirror.Tick,
                $"rewind sequence is not the forward sequence reversed at call {i}.");
        }

        // 3. Deterministic jump battery (the xorshift battery the frozen
        //    parity receipts use): 24 [from, to] pairs in range and past
        //    the duration, forward and backward — words against the
        //    reference, item.State against clipState word-for-word.
        uint random = 0x6D2B79F5u;
        for (var j = 0; j < 24; j++)
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var from = random % 620;
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            var to = random % 620;

            foreach (var backward in new[] { false, true })
            {
                var jumpClips = new ClipFacts { Calls = [] };
                var jumpItems = new ItemFacts { Calls = [] };
                var jc = Playback.Start(from);
                var jv = Playback.Start(from);
                if (backward)
                {
                    jc = ClipTimeline<VitalsTrack, VitalsClip>.Backward(in jc, ref jumpClips, to);
                    jv = GeneratedTimeline<VitalsTrack, VitalsClip>.Backward(in jv, ref jumpItems, to);
                }
                else
                {
                    jc = ClipTimeline<VitalsTrack, VitalsClip>.Forward(in jc, ref jumpClips, to);
                    jv = GeneratedTimeline<VitalsTrack, VitalsClip>.Forward(in jv, ref jumpItems, to);
                }

                var expected = ItemsAt(to, from, backward, wrapped: false, full: false);
                RequireWords(to, jumpClips.Calls!, 0, expected);
                RequireWords(to, jumpItems.Calls!, 0, expected);
            }
        }

        // 4. Wrapping steps on the looping variant (LoopVitalsTrack): a
        //    wrapped span crosses (prevEff, duration) then [0, tEff], and a
        //    multi-cycle jump is full coverage — every window crossed. The
        //    reference derives prevEff/tEff/wrapped/full from the authored
        //    duration exactly the way PlaybackCore.Position does.
        foreach (var (from, to, backward) in new (uint From, uint To, bool Backward)[]
                 {
                     (599, 685, false), (1199, 5, false), (0, 1205, false), (599, 5, false),
                     (0, 599, true), (5, 1199, true), (685, 5, true), (1199, 1198, true),
                 })
        {
            const uint duration = 600;
            var prevEff = from % duration;
            var tEff = to % duration;
            bool wrapped;
            var full = false;
            if (!backward)
            {
                var cycles = to >= from ? to / duration - from / duration : tEff < prevEff ? 1u : 0u;
                wrapped = cycles == 1;
                full = cycles >= 2;
            }
            else if (to <= from)
            {
                var cycles = from / duration - to / duration;
                wrapped = cycles == 1;
                full = cycles >= 2;
            }
            else
            {
                wrapped = tEff > prevEff;
            }

            var loopClips = new LoopClipFacts { Calls = [] };
            var lc = Playback.Start(from);
            lc = backward
                ? ClipTimeline<LoopVitalsTrack, VitalsClip>.Backward(in lc, ref loopClips, to)
                : ClipTimeline<LoopVitalsTrack, VitalsClip>.Forward(in lc, ref loopClips, to);

            var expected = ItemsAt(tEff, prevEff, backward, wrapped, full);
            RequireWords(tEff, loopClips.Calls!, 0, expected);
        }
    }

    // The expected per-clip words at one tick: for each track, the active
    // window — a single clip's edge, or the OUTER window of an overlapping
    // pair (min Start, max End), the blend convention this oracle pins —
    // enumerated the way the flag oracle enumerates clips: straight from
    // the authored list, never the CSR tables. Track identity is the track
    // payload's Offset (1..4), unique per track in the fixture.
    private static List<(int Track, PlaybackFlags State)> ItemsAt(uint t, uint prevEff, bool backward, bool wrapped, bool full)
    {
        var items = new List<(int, PlaybackFlags)>();
        for (var track = 0; track <= 3; track++)
        {
            var count = 0;
            uint start = 0, end = 0;
            foreach (var clip in s_authored)
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

            var state = PlaybackFlags.Active;
            if (t == start) state |= PlaybackFlags.First;
            if (t == end - 1) state |= PlaybackFlags.Last;

            var moved = full || wrapped || (backward ? t < prevEff : t > prevEff);
            if (moved)
            {
                bool Crossed(uint boundary)
                {
                    if (full)
                        return true;
                    if (backward)
                        return wrapped ? boundary > t || boundary <= prevEff : boundary > t && boundary <= prevEff;
                    return wrapped ? boundary > prevEff || boundary <= t : boundary > prevEff && boundary <= t;
                }

                if (backward)
                {
                    if (Crossed(end)) state |= PlaybackFlags.Enter;
                    if (Crossed(start)) state |= PlaybackFlags.Exit;
                }
                else
                {
                    if (Crossed(start)) state |= PlaybackFlags.Enter;
                    if (Crossed(end)) state |= PlaybackFlags.Exit;
                }
            }

            items.Add((track + 1, state));
        }

        return items;
    }

    // Compares the calls a consumer recorded after `mark` against the
    // expected words, in order, for one tick.
    private static void RequireWords(
        uint tick, List<(int Track, uint Tick, PlaybackFlags State)> calls, int mark,
        List<(int Track, PlaybackFlags State)> expected)
    {
        if (calls.Count - mark != expected.Count)
            throw new InvalidOperationException($"tick {tick}: {calls.Count - mark} calls, expected {expected.Count}.");
        for (var i = 0; i < expected.Count; i++)
        {
            var call = calls[mark + i];
            if (call.Tick != tick || call.Track != expected[i].Track)
                throw new InvalidOperationException(
                    $"tick {tick} item {i}: call for track {call.Track} at {call.Tick}, expected track {expected[i].Track}.");
            if (call.State != expected[i].State)
                throw new InvalidOperationException(
                    $"tick {tick} track {expected[i].Track}: clipState {call.State} != reference {expected[i].State}.");
        }
    }

    // Records every clip-level call (track, tick, word) for the oracle.
    internal struct ClipFacts : IForward<VitalsTrack, VitalsClip, ClipFacts>, IBackward<VitalsTrack, VitalsClip, ClipFacts>
    {
        public List<(int Track, uint Tick, PlaybackFlags State)>? Calls;

        public void OnClip(in VitalsTrack track, in VitalsClip clip, PlaybackFlags clipState, uint tick, ref ClipFacts data)
            => data.Calls!.Add((track.Offset, tick, clipState));

        public void OnClipBack(in VitalsTrack track, in VitalsClip clip, PlaybackFlags clipState, uint tick, ref ClipFacts data)
            => data.Calls!.Add((track.Offset, tick, clipState));
    }

    // The looping twin for the wrap battery (same tables, authored looping).
    internal struct LoopClipFacts : IForward<LoopVitalsTrack, VitalsClip, LoopClipFacts>, IBackward<LoopVitalsTrack, VitalsClip, LoopClipFacts>
    {
        public List<(int Track, uint Tick, PlaybackFlags State)>? Calls;

        public void OnClip(in LoopVitalsTrack track, in VitalsClip clip, PlaybackFlags clipState, uint tick, ref LoopClipFacts data)
            => data.Calls!.Add((track.Offset, tick, clipState));

        public void OnClipBack(in LoopVitalsTrack track, in VitalsClip clip, PlaybackFlags clipState, uint tick, ref LoopClipFacts data)
            => data.Calls!.Add((track.Offset, tick, clipState));
    }

    // The view-engine twin: records item.State for every item the Tracks
    // view exposes — must equal the OnClip words word-for-word.
    internal struct ItemFacts : IForwardTracks<VitalsTrack, VitalsClip, ItemFacts>, IBackwardTracks<VitalsTrack, VitalsClip, ItemFacts>
    {
        public List<(int Track, uint Tick, PlaybackFlags State)>? Calls;

        public void OnTick(in Tracks<VitalsTrack, VitalsClip> tracks, uint tick, ref ItemFacts data)
        {
            foreach (var item in tracks)
                data.Calls!.Add((item.Track.Offset, tick, item.State));
        }

        public void OnTickBack(in Tracks<VitalsTrack, VitalsClip> tracks, uint tick, ref ItemFacts data)
        {
            foreach (var item in tracks)
                data.Calls!.Add((item.Track.Offset, tick, item.State));
        }
    }
}
