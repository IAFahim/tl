using System.Runtime.CompilerServices;

namespace Tl.Hooks;

internal static class EdgeVerification
{
    public static void Run()
    {
        var failures = new List<string>();
        foreach (var test in new Action[] { Gaps, Empty, Blend, Order, Packing, Wrap, Flags, Batches, Fixture, WideTicks, Limits, Indices })
        {
            try { test(); }
            catch (Exception e) { failures.Add($"{test.Method.Name}: {e.Message}"); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("Edge checks passed: gaps, terminal ticks, empty tables, blends, packing, wraps, flag oracle, fixture parity, limits.");
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

    private static Timeline<ProbeTrack, ProbeClip, Probe> Make(params ReadOnlySpan<ClipEdge> edges)
    {
        var timeline = new Timeline<ProbeTrack, ProbeClip, Probe>();
        foreach (var edge in edges)
        {
            int track = timeline.AddTrack(new ProbeTrack(0));
            timeline.AddClip(track, new ProbeClip(10), edge.Start, edge.End);
        }
        timeline.Build();
        return timeline;
    }

    private static void Gaps()
    {
        var timeline = Make(new ClipEdge(10, 20));
        foreach (uint tick in new uint[] { 0, 9, 10, 19, 20, 21, uint.MaxValue })
        {
            var probe = new Probe();
            timeline.Forward(ref probe, tick);
            Require(probe.Tracks == (tick >= 10 && tick < 20 ? 1 : 0), $"Wrong active tracks at {tick}.");
            var from = Playback.Start(19);
            probe = default;
            var state = timeline.Forward(in from, ref probe, tick);
            Require(state.Flags == Oracle([new(10, 20)], 19, tick, 20, false, false), $"Wrong terminal flags at {tick}.");
        }
    }

    private static void Empty()
    {
        var timeline = Make();
        var data = new Probe();
        var from = Playback.Start();
        var result = timeline.Forward(in from, ref data, 0, 10);
        Require(data.Count == 2 && data.Tracks == 0 && result.Has(PlaybackFlags.Complete), "Empty non-looping timeline did not complete.");
        timeline.IsLooping = true;
        result = timeline.Forward(in from, ref data, 10);
        Require(result.Cycles == 0 && result.Flags == PlaybackFlags.None, "Empty loop should have no movement facts.");
    }

    private static void Blend()
    {
        var timeline = new Timeline<ProbeTrack, ProbeClip, Probe>();
        var track = timeline.AddTrack(new ProbeTrack(2));
        timeline.AddClip(track, new ProbeClip(10), 4, 5);
        timeline.AddClip(track, new ProbeClip(30), 4, 5);
        timeline.Build();
        var data = new Probe();
        timeline.Forward(ref data, 4);
        Require(data.Sum == 22, $"One-tick blend with authored track data returned {data.Sum}, expected 22.");
    }

    private static void Order()
    {
        var timeline = new Timeline<ProbeTrack, ProbeClip, Probe>();
        var track = timeline.AddTrack(new ProbeTrack(0));
        timeline.AddClip(track, new ProbeClip(30), 5, 10);
        timeline.AddClip(track, new ProbeClip(10), 0, 8);
        timeline.Build();
        var data = new Probe();
        timeline.Forward(ref data, 5);
        Require(data.Sum == 10, $"Crossfade should start with the earlier clip; got {data.Sum}.");
    }

    private static void Packing()
    {
        Require(Unsafe.SizeOf<Playback>() == 8, "Playback grew beyond 8 bytes.");
        const uint max = 0x03FF_FFFFu;
        Reject<ArgumentOutOfRangeException>(() => _ = new Playback(0, max + 1, PlaybackFlags.None));
        var timeline = Make(new ClipEdge(0, 1));
        timeline.IsLooping = true;
        var from = new Playback(0, max, PlaybackFlags.None);
        var data = new Probe();
        Reject<ArgumentOutOfRangeException>(() => timeline.Forward(in from, ref data, 1));
        Require(data.Count == 0, "Overflow invoked a callback before rejecting the step.");
    }

    private static void Wrap()
    {
        var timeline = Make(new ClipEdge(0, 10));
        timeline.IsLooping = true;
        var from = Playback.Start(9);
        var data = new Probe();
        var next = timeline.Forward(in from, ref data, 0);
        Require(next.Cycles == 1 && next.Flags == (PlaybackFlags.Enter | PlaybackFlags.Exit | PlaybackFlags.First | PlaybackFlags.Active), $"Local forward wrap corrupted state: {next.Cycles}, {next.Flags}.");
        timeline.Forward(ref data, 11);
        Require(data.Tick == 1 && data.Tracks == 1, "Stateless looping playback must normalize the tick too.");
    }

    private static void Flags()
    {
        ClipEdge[] edges = [new(2, 3), new(5, 12), new(8, 10), new(16, 25), new(20, 28)];
        var timeline = Make(edges);
        foreach (bool loops in new[] { false, true })
        foreach (bool backward in new[] { false, true })
        {
            timeline.IsLooping = loops;
            for (uint previous = 0; previous < 85; previous++)
            for (uint tick = 0; tick < 85; tick++)
            {
                var from = Playback.Start(previous);
                var data = new Probe();
                var result = backward ? timeline.Backward(in from, ref data, tick) : timeline.Forward(in from, ref data, tick);
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
        var timeline = Make(edges);
        foreach (bool loops in new[] { false, true })
        foreach (bool backward in new[] { false, true })
        {
            timeline.IsLooping = loops;
            var single = new Probe { Trace = [] };
            var batch = new Probe { Trace = [] };
            var state = Playback.Start();
            foreach (uint tick in ticks)
                state = backward ? timeline.Backward(in state, ref single, tick) : timeline.Forward(in state, ref single, tick);
            var from = Playback.Start();
            var result = backward ? timeline.Backward(in from, ref batch, ticks) : timeline.Forward(in from, ref batch, ticks);
            Require(single.Trace!.SequenceEqual(batch.Trace!) && single.Sum == batch.Sum && state.Cycles == result.Cycles && state.Flags == result.Flags, "Batch cursor changed sampling or movement facts.");
        }
    }

    private static void WideTicks()
    {
        var timeline = Make(new ClipEdge(uint.MaxValue - 2, uint.MaxValue));
        var data = new Probe();
        var from = Playback.Start(uint.MaxValue - 3);
        var active = timeline.Forward(in from, ref data, uint.MaxValue - 1);
        Require(active.Flags == (PlaybackFlags.Enter | PlaybackFlags.Active | PlaybackFlags.Last | PlaybackFlags.Complete), "Wide tick lost last/complete flags.");
        var end = timeline.Forward(in active, ref data, uint.MaxValue);
        Require(end.Flags == (PlaybackFlags.Exit | PlaybackFlags.Complete) && data.Tracks == 0, "Wide terminal tick did not exit.");
    }

    private static void Indices()
    {
        var timeline = new Timeline<ProbeTrack, ProbeClip, Probe>();
        for (int i = timeline.Index + 1; i <= ushort.MaxValue; i++)
        {
            timeline = new();
            Require(timeline.Index == i, "Index was reused before exhaustion.");
        }
        Reject<InvalidOperationException>(() => _ = new Timeline<ProbeTrack, ProbeClip, Probe>());
        Reject<InvalidOperationException>(() => _ = new Timeline<ProbeTrack, ProbeClip, Probe>());
    }

    private static void Fixture()
    {
        var timeline = new ApiShape().BuildTimeline();
        foreach (bool loops in new[] { false, true })
        {
            timeline.IsLooping = loops;
            var a = Playback.Start();
            var b = a;
            for (uint tick = 0; tick < 1300; tick++)
            {
                var x = new Vitals();
                var y = new Vitals();
                a = timeline.Forward(in a, ref x, tick);
                b = loops ? GeneratedTimeline<LoopVitalsTrack, VitalsClip, Vitals>.Forward(in b, ref y, tick) : GeneratedTimeline<VitalsTrack, VitalsClip, Vitals>.Forward(in b, ref y, tick);
                Require(a.Flags == b.Flags && a.Cycles == b.Cycles && x.Result == y.Result, $"Generated/runtime fixture diverged at {tick}, loops={loops}.");
            }
        }
    }

    private static void Limits()
    {
        var timeline = new Timeline<ProbeTrack, ProbeClip, Probe>();
        Reject<ArgumentOutOfRangeException>(() => timeline.AddClip(0, new ProbeClip(1), 0, 1));
        var track = timeline.AddTrack(new ProbeTrack(0));
        Reject<ArgumentOutOfRangeException>(() => timeline.AddClip(-1, new ProbeClip(1), 0, 1));
        for (int i = 0; i < ushort.MaxValue; i++) timeline.AddClip(track, new ProbeClip(1), (uint)i, (uint)i + 1);
        Reject<InvalidOperationException>(() => timeline.AddClip(track, new ProbeClip(1), 0, 1));
        var large = new Timeline<ProbeTrack, ProbeClip, Probe>();
        for (uint i = 0; i < 400; i++)
        {
            int t = large.AddTrack(new ProbeTrack(0));
            large.AddClip(t, new ProbeClip(1), 0, i + 1);
        }
        Reject<OverflowException>(() => large.Build());
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
        public void Forward(uint tick, in Tracks<ProbeTrack, ProbeClip> tracks, ref Probe data)
        {
            data.Tick = tick;
            data.Status = tracks.Status;
            data.Trace?.Add(tracks.Status);
            data.Tracks = tracks.Count;
            foreach (var item in tracks) data.Sum += item.Clip.Value;
            data.Count++;
        }
        public void Backward(uint tick, in Tracks<ProbeTrack, ProbeClip> tracks, ref Probe data) => Forward(tick, in tracks, ref data);
    }
}
