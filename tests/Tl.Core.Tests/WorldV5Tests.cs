using System;
using System.Diagnostics;
using Xunit;

namespace Tl.Core.Tests;

public class WorldV5Tests
{
    struct Pair
    {
        public int Row;
        public int Timeline;
        public int Phase;
    }

    struct ProbeEvent
    {
        public int Row;
        public int Tag;
    }

    static int OracleTick(int phase, int frame, int duration, int dir)
    {
        var t = phase + dir * frame;
        t %= duration;
        if (t < 0) t += duration;
        return t;
    }

    static void OracleRun(float[] acc, Pair[] pairs, float[][] lanes, int[] durations, int ticks, int dir)
    {
        var sorted = new Pair[pairs.Length];
        Array.Copy(pairs, sorted, pairs.Length);
        for (var i = 1; i < sorted.Length; i++)
        {
            var p = sorted[i];
            var j = i - 1;
            while (j >= 0 && (sorted[j].Timeline > p.Timeline || (sorted[j].Timeline == p.Timeline && sorted[j].Row > p.Row)))
            {
                sorted[j + 1] = sorted[j];
                j--;
            }
            sorted[j + 1] = p;
        }
        for (var f = 1; f <= ticks; f++)
        {
            for (var i = 0; i < sorted.Length; i++)
            {
                var p = sorted[i];
                acc[p.Row] += lanes[p.Timeline - 1][OracleTick(p.Phase, f, durations[p.Timeline - 1], dir)];
            }
        }
    }

    static ulong Hash(float[] acc)
    {
        var h = 14695981039346656037ul;
        for (var i = 0; i < acc.Length; i++)
            h = (h ^ (uint)BitConverter.SingleToInt32Bits(acc[i])) * 1099511628211ul;
        return h;
    }

    static float[] ConstLane(int duration, float value)
    {
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = value;
        return lane;
    }

    static float[] VaryingLane(int duration)
    {
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = 1f + (i * 7 % 13) * 0.5f + (i % 5) * 0.25f;
        return lane;
    }

    static TimelineSpec Timeline(int duration, float[] laneF, float[] laneR, uint kindId = 0)
    {
        return new TimelineSpec((uint)duration, true, new[]
        {
            new StepSpec(0u, (uint)duration, kindId, 1ul, laneF, laneR),
        });
    }

    static Library MakeLibrary(TimelineSpec[] timelines)
    {
        var blob = LibraryWriter.Write(timelines);
        var result = Library.Verify(blob);
        Assert.True(result.Ok);
        Assert.NotNull(result.Library);
        return result.Library!;
    }

    static Pair[] UniformPairs(int count)
    {
        var pairs = new Pair[count];
        for (var i = 0; i < count; i++) pairs[i] = new Pair { Row = i, Timeline = 1, Phase = 0 };
        return pairs;
    }

    static Pair[] MixedPairs(int count, int duration)
    {
        var pairs = new Pair[count];
        for (var i = 0; i < count; i++) pairs[i] = new Pair { Row = i, Timeline = 1, Phase = (i * 37 + 11) % duration };
        return pairs;
    }

    static Pair[] UniquePairs(int count, int duration)
    {
        var pairs = new Pair[count];
        for (var i = 0; i < count; i++) pairs[i] = new Pair { Row = i, Timeline = 1, Phase = i % duration };
        return pairs;
    }

    static ulong RunWorldHash(Library lib, Pair[] pairs, int rows, Direction dir, int ticks)
    {
        using var world = new World(lib, rows);
        for (var i = 0; i < pairs.Length; i++)
            world.Requests.Start(new Row(pairs[i].Row), new TimelineId((ushort)pairs[i].Timeline), pairs[i].Phase);
        for (var f = 0; f < ticks; f++) world.Tick(dir);
        Assert.Equal(ticks, world.Frame);
        Assert.Equal(pairs.Length, world.ActiveCount);
        return Hash(world.Accumulator);
    }

    static void CheckShape(float[] lane, Pair[] pairs, Direction dir, int ticks)
    {
        var lib = MakeLibrary(new[] { Timeline(lane.Length, lane, lane) });
        var expected = new float[1024];
        var dirSign = dir == Direction.Reverse ? -1 : 1;
        OracleRun(expected, pairs, new[] { lane }, new[] { lane.Length }, ticks, dirSign);
        Assert.Equal(Hash(expected), RunWorldHash(lib, pairs, 1024, dir, ticks));
    }

    [Fact]
    public void UniformForwardMatchesOracle()
    {
        CheckShape(ConstLane(40, 6f), UniformPairs(1024), Direction.Forward, 97);
    }

    [Fact]
    public void UniformRewindMatchesOracle()
    {
        CheckShape(ConstLane(40, 6f), UniformPairs(1024), Direction.Reverse, 97);
    }

    [Fact]
    public void MixedForwardMatchesOracle()
    {
        CheckShape(VaryingLane(40), MixedPairs(1024, 40), Direction.Forward, 97);
    }

    [Fact]
    public void MixedRewindMatchesOracle()
    {
        CheckShape(VaryingLane(40), MixedPairs(1024, 40), Direction.Reverse, 97);
    }

    [Fact]
    public void UniqueForwardMatchesOracle()
    {
        CheckShape(VaryingLane(1024), UniquePairs(1024, 1024), Direction.Forward, 97);
    }

    [Fact]
    public void UniqueRewindMatchesOracle()
    {
        CheckShape(VaryingLane(1024), UniquePairs(1024, 1024), Direction.Reverse, 97);
    }

    [Fact]
    public void FourPairsForwardMatchesOracle()
    {
        var lanes = new[] { ConstLane(40, 6f), ConstLane(40, 7f), ConstLane(40, 8f), ConstLane(40, 9f) };
        var lib = MakeLibrary(new[]
        {
            Timeline(40, lanes[0], lanes[0]),
            Timeline(40, lanes[1], lanes[1]),
            Timeline(40, lanes[2], lanes[2]),
            Timeline(40, lanes[3], lanes[3]),
        });
        var pairs = new Pair[4096];
        var k = 0;
        for (var t = 1; t <= 4; t++)
            for (var i = 0; i < 1024; i++) pairs[k++] = new Pair { Row = i, Timeline = t, Phase = 0 };
        var expected = new float[1024];
        OracleRun(expected, pairs, lanes, new[] { 40, 40, 40, 40 }, 97, 1);
        Assert.Equal(Hash(expected), RunWorldHash(lib, pairs, 1024, Direction.Forward, 97));
    }

    [Fact]
    public void WarmTickAllocatesZeroBytes()
    {
        var lib = MakeLibrary(new[] { Timeline(40, ConstLane(40, 6f), ConstLane(40, 6f)) });
        using var world = new World(lib, 1024);
        for (var i = 0; i < 1024; i++) world.Requests.Start(new Row(i), new TimelineId(1), 0);
        for (var f = 0; f < 100; f++) world.Tick(Direction.Forward);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 50; f++) world.Tick(Direction.Forward);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void MixedWarmTickAllocatesZeroBytes()
    {
        var lib = MakeLibrary(new[] { Timeline(40, VaryingLane(40), VaryingLane(40)) });
        var pairs = MixedPairs(1024, 40);
        using var world = new World(lib, 1024);
        for (var i = 0; i < pairs.Length; i++)
            world.Requests.Start(new Row(pairs[i].Row), new TimelineId(1), pairs[i].Phase);
        for (var f = 0; f < 100; f++) world.Tick(Direction.Reverse);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 50; f++) world.Tick(Direction.Reverse);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void SixtyFourCohortsTimeScalesWithActiveVisits()
    {
        const int rows = 1024;
        var lane = ConstLane(40, 6f);
        var timelines = new TimelineSpec[64];
        for (var k = 0; k < 64; k++) timelines[k] = Timeline(40, lane, lane);
        var lib = MakeLibrary(timelines);
        using var single = new World(lib, rows);
        for (var i = 0; i < rows; i++) single.Requests.Start(new Row(i), new TimelineId(1), 0);
        using var multi = new World(lib, rows);
        for (var i = 0; i < rows; i++)
        {
            multi.Requests.Start(new Row(i), new TimelineId(1), 0);
            multi.Requests.Start(new Row(i), new TimelineId(2), 0);
        }
        for (var f = 0; f < 20; f++)
        {
            single.Tick(Direction.Forward);
            multi.Tick(Direction.Forward);
        }
        var tSingle = Measure(single, 200);
        var tMulti = Measure(multi, 200);
        Assert.True(tSingle > 0 && tMulti > 0);
        var ratio = (double)tMulti / tSingle;
        Assert.True(ratio <= 2.5, $"64-cohorts-2-active ratio {ratio:F2} exceeds 2.5 (single={tSingle}, multi={tMulti})");
    }

    static long Measure(World world, int ticks)
    {
        long best = long.MaxValue;
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticks; f++) world.Tick(Direction.Forward);
            sw.Stop();
            if (sw.ElapsedTicks < best) best = sw.ElapsedTicks;
        }
        return best;
    }

    [Fact]
    public void InvalidStartsAreCountedNotThrown()
    {
        var lib = MakeLibrary(new[] { Timeline(40, ConstLane(40, 6f), ConstLane(40, 6f)) });
        using var world = new World(lib, 1024);
        world.Requests.Start(new Row(-1), new TimelineId(1));
        world.Requests.Start(new Row(1024), new TimelineId(1));
        world.Requests.Start(new Row(0), new TimelineId(99));
        world.Tick(Direction.Forward);
        Assert.Equal(3, world.Report.InvalidRequests);
        Assert.Equal(0, world.ActiveCount);
        Assert.Equal(0f, world.Accumulator[0]);
        world.Requests.Start(new Row(5), new TimelineId(1), 0);
        world.Tick(Direction.Forward);
        Assert.Equal(0, world.Report.InvalidRequests);
        Assert.Equal(1, world.Report.ActiveCount);
        Assert.Equal(6f, world.Accumulator[5]);
    }

    [Fact]
    public void EventsDrainReturnsRaisedAndClears()
    {
        var lib = MakeLibrary(new[] { Timeline(1, ConstLane(1, 0f), ConstLane(1, 0f)) });
        using var world = new World(lib, 4);
        var buf = world.Events<ProbeEvent>();
        buf.Raise(new ProbeEvent { Row = 7, Tag = 1 });
        buf.Raise(new ProbeEvent { Row = 9, Tag = 2 });
        Assert.Equal(2, buf.Count);
        var drained = buf.Drain();
        Assert.Equal(2, drained.Length);
        Assert.Equal(7, drained[0].Row);
        Assert.Equal(9, drained[1].Row);
        Assert.Equal(0, buf.Drain().Length);
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void BroadcastDenseMatchesScalarLoop()
    {
        const int count = 1024;
        const int off = 37;
        var a = new float[count + off + 64];
        var b = new float[count + off + 64];
        for (var i = 0; i < a.Length; i++)
        {
            var v = (i % 13) * 0.25f;
            a[i] = v;
            b[i] = v;
        }
        Kernels.BroadcastDense(a, off, 3.5f, count);
        for (var i = 0; i < count; i++) b[off + i] += 3.5f;
        Assert.Equal(b, a);
    }

    [Fact]
    public void LaneKernelsMatchScalarOracle()
    {
        const int n = 1024;
        var lane = VaryingLane(40);
        var phase = new ushort[n];
        var row = new int[n];
        for (var i = 0; i < n; i++)
        {
            phase[i] = (ushort)((i * 37 + 11) % 40);
            row[i] = i;
        }
        var a = new float[n];
        var b = new float[n];
        const int frame = 13;
        const int dir = -1;
        Kernels.LaneGatherScalar(a, 0, row, phase, lane, frame, 40, dir);
        for (var i = 0; i < n; i++) b[row[i]] += lane[OracleTick(phase[i], frame, 40, dir)];
        Assert.Equal(b, a);
        var c = new float[40];
        var d = new float[40];
        Kernels.LaneDense(c, 0, lane, 40);
        for (var i = 0; i < 40; i++) d[i] += lane[i];
        Assert.Equal(d, c);
    }

    [Fact]
    public void RunBroadcastMatchesScalarOracle()
    {
        var lane = VaryingLane(10);
        var sortedRow = new int[] { 7, 3, 1, 5, 2, 6, 0, 4 };
        var runs = new[]
        {
            new Run(0, 3, 5),
            new Run(3, 2, 2),
            new Run(5, 3, 9),
        };
        var phases = new ushort[] { 5, 5, 5, 2, 2, 9, 9, 9 };
        const int frame = 3;
        const int dir = -1;
        var a = new float[8];
        var b = new float[8];
        Kernels.RunBroadcast(a, 0, sortedRow, runs, lane, frame, 10, dir);
        for (var i = 0; i < 8; i++) b[sortedRow[i]] += lane[OracleTick(phases[i], frame, 10, dir)];
        Assert.Equal(b, a);
    }

    [Fact]
    public void StableRadixSortsStably()
    {
        var keys = new uint[] { 3, 1, 3, 0, 2 };
        var order = new int[] { 0, 1, 2, 3, 4 };
        var scratch = new int[5];
        Kernels.StableRadix(keys, order, scratch);
        Assert.Equal(new[] { 3, 1, 4, 0, 2 }, order);
    }
    [Fact]
    public void LargeForwardMatchesOracleOnParallelPath()
    {
        const int rows = 8192;
        var lane = VaryingLane(40);
        var lib = MakeLibrary(new[] { Timeline(40, lane, lane) });
        var pairs = MixedPairs(rows, 40);
        var expected = new float[rows];
        OracleRun(expected, pairs, new[] { lane }, new[] { 40 }, 97, 1);
        Assert.Equal(Hash(expected), RunWorldHash(lib, pairs, rows, Direction.Forward, 97));
    }

    [Fact]
    public void LargeReverseMatchesOracleOnParallelPath()
    {
        const int rows = 8192;
        var lane = VaryingLane(40);
        var lib = MakeLibrary(new[] { Timeline(40, lane, lane) });
        var pairs = MixedPairs(rows, 40);
        var expected = new float[rows];
        OracleRun(expected, pairs, new[] { lane }, new[] { 40 }, 97, -1);
        Assert.Equal(Hash(expected), RunWorldHash(lib, pairs, rows, Direction.Reverse, 97));
    }
}
