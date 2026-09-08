using Xunit;

namespace Tl.Core.Tests;

// The v0.3 indexed/sliced view battery, ported from the benchmarks sandbox
// (benchmarks/Dispatch/EdgeVerification.cs, the Indexed check): Count,
// this[int], and Slice are new API beside foreach — they must expose
// exactly the works enumeration exposes (same authored indices, same
// per-work states, blends resolved identically through the indexed path),
// slices must be zero-copy sub-views of the same slot table, and both must
// bounds-check like the spans they are.
public class IndexedViewTests
{
    public readonly record struct ProbeClip(float Value);

    public readonly struct ProbeTrack : IBlend<ProbeClip>
    {
        public void Blend(in ProbeClip first, in ProbeClip second, float t, out ProbeClip result)
            => result = new ProbeClip(first.Value * (1f - t) + second.Value * t);
    }

    // Two crossfading pairs plus two standalone solos, all sharing [0, 8):
    // every tick of the walk sees exactly four works — two blended, two
    // direct — through one region.
    private static ushort BuildBatteryTimeline() =>
        Timeline<ProbeTrack, ProbeClip>.Build(static b =>
        {
            TrackRef firstPair = b.Track(new ProbeTrack());
            TrackRef secondPair = b.Track(new ProbeTrack());
            TrackRef soloA = b.Track(new ProbeTrack());
            TrackRef soloB = b.Track(new ProbeTrack());
            b.Clip(in firstPair, new ProbeClip(4), 0, 8);
            b.Clip(in firstPair, new ProbeClip(8), 0, 8);
            b.Clip(in secondPair, new ProbeClip(6), 0, 8);
            b.Clip(in secondPair, new ProbeClip(12), 0, 8);
            b.Clip(in soloA, new ProbeClip(3), 0, 8);
            b.Clip(in soloB, new ProbeClip(5), 0, 8);
        }).InMemory();

    // The foreach reference consumer: records every (Index, State) work it
    // sees and accumulates clip values on Stay only.
    public struct Probe :
        IForward<ProbeTrack, ProbeClip, NoInput, Probe>,
        IBackward<ProbeTrack, ProbeClip, NoInput, Probe>
    {
        public float Sum;
        public int Tracks;
        public int Count;
        public List<(ushort Index, ClipState State)> Works = [];

        public Probe() { }

        public void Forward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref Probe result)
        {
            result.Tracks = tracks.Count;
            foreach (var work in tracks)
            {
                if (work.State == ClipState.Stay)
                    result.Sum += work.Clip.Value;
                result.Works.Add((work.Index, work.State));
            }
            result.Count++;
        }

        public void Backward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref Probe result)
            => Forward(in tracks, in input, in tick, ref result);
    }

    // The indexed-view consumer: reads every work through this[int] (never
    // foreach), sums every resolved clip, and runs the Slice boundary
    // receipts once. Must record exactly what the foreach probe records —
    // Index and State — while resolving blends through the indexed path.
    public struct IndexedProbe :
        IForward<ProbeTrack, ProbeClip, NoInput, IndexedProbe>,
        IBackward<ProbeTrack, ProbeClip, NoInput, IndexedProbe>
    {
        public float Sum;
        public int Tracks;
        public int Calls;
        public List<(ushort Index, ClipState State)>? Works;
        public List<string>? Failures;

        public void Forward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref IndexedProbe result)
        {
            result.Tracks = tracks.Count;
            for (var i = 0; i < tracks.Count; i++)
            {
                var work = tracks[i];
                result.Sum += work.Clip.Value;
                result.Works!.Add((work.Index, work.State));
            }

            if (result.Calls == 0)
            {
                // Bounds receipts, once, against a live engine view: the
                // indexer rejects like the span it is (IndexOutOfRange under
                // the JIT's hardware bounds check) and Slice rejects with
                // ArgumentOutOfRangeException; a full-length slice is valid.
                // Failures are recorded and asserted outside the callback.
                try { _ = tracks[tracks.Count]; result.Failures!.Add("The indexer must bounds-check."); }
                catch (IndexOutOfRangeException) { }
                try { _ = tracks.Slice(0, tracks.Count + 1); result.Failures!.Add("Slice must bounds-check length."); }
                catch (ArgumentOutOfRangeException) { }
                try { _ = tracks.Slice(tracks.Count, 1); result.Failures!.Add("Slice must bounds-check start."); }
                catch (ArgumentOutOfRangeException) { }
                _ = tracks.Slice(0, tracks.Count);
            }

            result.Calls++;
        }

        public void Backward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref IndexedProbe result)
            => Forward(in tracks, in input, in tick, ref result);
    }

    // Consumes exclusively through sub-views: splits the view into slices,
    // then foreaches each slice — sliced enumeration and sliced Clip reads
    // (blends included, scratch ordinals baked per slot) must aggregate
    // exactly what the whole view produces.
    public struct SliceProbe :
        IForward<ProbeTrack, ProbeClip, NoInput, SliceProbe>,
        IBackward<ProbeTrack, ProbeClip, NoInput, SliceProbe>
    {
        public float Sum;
        public List<(ushort Index, ClipState State)>? Works;

        public void Forward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref SliceProbe result)
        {
            var half = tracks.Count / 2;
            foreach (var work in tracks.Slice(0, half))
            {
                result.Sum += work.Clip.Value;
                result.Works!.Add((work.Index, work.State));
            }

            foreach (var work in tracks.Slice(half, tracks.Count - half))
            {
                result.Sum += work.Clip.Value;
                result.Works!.Add((work.Index, work.State));
            }

            // Empty slices enumerate nothing and never throw.
            Assert.Equal(0, tracks.Slice(0, 0).Count);
        }

        public void Backward(in Tracks<ProbeTrack, ProbeClip> tracks, in NoInput input, in uint tick, ref SliceProbe result)
            => Forward(in tracks, in input, in tick, ref result);
    }

    private static (IndexedProbe Indexed, Probe Foreach) RunBoth(ushort timeline, uint[] ticks)
    {
        var viaIndex = new IndexedProbe { Works = [], Failures = [] };
        var state = Timeline.Start(timeline);
        foreach (var tick in ticks)
            state = Timeline.Forward(timeline, in state, default(NoInput), ref viaIndex, tick);

        var viaForeach = new Probe();
        state = Timeline.Start(timeline);
        foreach (var tick in ticks)
            state = Timeline.Forward(timeline, in state, default(NoInput), ref viaForeach, tick);

        return (viaIndex, viaForeach);
    }

    [Fact]
    public void IndexedViewMatchesEnumeration()
    {
        var timeline = BuildBatteryTimeline();
        uint[] ticks = [0, 1, 2, 3, 4, 5, 6, 7];
        var (viaIndex, viaForeach) = RunBoth(timeline, ticks);

        Assert.Equal(viaForeach.Works.Count, viaIndex.Works!.Count);
        Assert.Equal(viaForeach.Works, viaIndex.Works);
        Assert.Equal(ticks.Length, viaIndex.Calls);
        Assert.Equal(4, viaIndex.Tracks);

        // Backward mirror: the indexed path must also mirror enumeration on
        // rewind walks (Exit is positional at the window Start, Enter through
        // the End edge).
        var backIndex = new IndexedProbe { Works = [], Failures = [] };
        var state = Timeline.Start(timeline, 7);
        for (var tick = 7; tick >= 0; tick--)
            state = Timeline.Backward(timeline, in state, default(NoInput), ref backIndex, (uint)tick);

        var backForeach = new Probe();
        state = Timeline.Start(timeline, 7);
        for (var tick = 7; tick >= 0; tick--)
            state = Timeline.Backward(timeline, in state, default(NoInput), ref backForeach, (uint)tick);

        Assert.Equal(backForeach.Works.Count, backIndex.Works!.Count);
        Assert.Equal(backForeach.Works, backIndex.Works);

        Timeline.Destroy(timeline);
    }

    [Fact]
    public void IndexedViewResolvesBlendSums()
    {
        var timeline = BuildBatteryTimeline();
        uint[] ticks = [0, 1, 2, 3, 4, 5, 6, 7];
        var (viaIndex, _) = RunBoth(timeline, ticks);

        // The indexed Clip reads resolve the same blends: two crossfades
        // over [0,8) sweep factors t/7, plus the two constant solos.
        var expected = 0f;
        foreach (var tick in ticks)
        {
            var factor = tick / 7f;
            expected += 4f * (1f - factor) + 8f * factor;
            expected += 6f * (1f - factor) + 12f * factor;
            expected += 3f + 5f;
        }

        Assert.NotNull(viaIndex.Works);
        Assert.Equal(32, viaIndex.Works.Count); // 4 works x 8 ticks
        Assert.True(Math.Abs(viaIndex.Sum - expected) < 1e-3f,
            $"Indexed blend sums diverged: {viaIndex.Sum:R} vs {expected:R}.");

        Timeline.Destroy(timeline);
    }

    [Fact]
    public void SlicedConsumptionMatchesIndexed()
    {
        var timeline = BuildBatteryTimeline();
        uint[] ticks = [0, 1, 2, 3, 4, 5, 6, 7];

        // A slice consumer that reads everything through sub-views: the
        // recorded works and sums must match the whole-view consumers.
        var viaSlice = new SliceProbe { Works = [] };
        var state = Timeline.Start(timeline);
        foreach (var tick in ticks)
            state = Timeline.Forward(timeline, in state, default(NoInput), ref viaSlice, tick);

        var (viaIndex, _) = RunBoth(timeline, ticks);

        Assert.Equal(viaIndex.Works!.Count, viaSlice.Works!.Count);
        Assert.Equal(viaIndex.Works, viaSlice.Works);
        Assert.True(Math.Abs(viaSlice.Sum - viaIndex.Sum) < 1e-3f,
            $"Sliced blend sums diverged: {viaSlice.Sum:R} vs {viaIndex.Sum:R}.");

        Timeline.Destroy(timeline);
    }

    [Fact]
    public void IndexerAndSliceBoundsCheck()
    {
        var timeline = BuildBatteryTimeline();
        var (viaIndex, _) = RunBoth(timeline, [0]);

        Assert.NotNull(viaIndex.Failures);
        Assert.Empty(viaIndex.Failures);

        Timeline.Destroy(timeline);
    }
}
