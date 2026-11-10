using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Tl.Core.Tests;

public class WorldScaleShapeTests
{
    static float[] ConstLane(int duration, float value)
    {
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = value;
        return lane;
    }

    static Library MakeLibrary(int timelines, int duration, float[] lane)
    {
        var specs = new TimelineSpec[timelines];
        for (var t = 0; t < timelines; t++)
            specs[t] = new TimelineSpec((uint)duration, true, new[] { new StepSpec(0u, (uint)duration, 0u, 1ul, lane, lane) });
        var blob = LibraryWriter.Write(specs);
        var result = Library.Verify(blob);
        Assert.True(result.Ok);
        Assert.NotNull(result.Library);
        return result.Library!;
    }

    static ulong Hash(float[] acc)
    {
        var h = 14695981039346656037ul;
        for (var i = 0; i < acc.Length; i++)
            h = (h ^ (uint)BitConverter.SingleToInt32Bits(acc[i])) * 1099511628211ul;
        return h;
    }

    static long MeasureTicks(World world, int ticks)
    {
        for (var f = 0; f < 20; f++) world.Tick(Direction.Forward);
        var best = long.MaxValue;
        for (var r = 0; r < 3; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < ticks; f++) world.Tick(Direction.Forward);
            sw.Stop();
            if (sw.ElapsedTicks < best) best = sw.ElapsedTicks;
        }
        return best;
    }

    static int PrivateArrayLength(World world, string name)
    {
        var field = typeof(World).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var array = (Array)field!.GetValue(world)!;
        return array.Length;
    }

    [Fact]
    public void SingleHeavyRowDoesNotMoveStride()
    {
        const int playbacks = 1000;
        const int capacity = 65536;
        const int ticks = 97;
        var lane = ConstLane(40, 6f);
        using var library = MakeLibrary(playbacks, 40, lane);
        var heavyExpected = new float[capacity];
        heavyExpected[0] = playbacks * ticks * 6f;
        using var heavy = new World(library, capacity);
        for (var i = 0; i < playbacks; i++)
            heavy.Requests.Start(new Row(0), new TimelineId((ushort)(i + 1)), 0);
        for (var f = 0; f < ticks; f++) heavy.Tick(Direction.Forward);
        Assert.Equal(playbacks, heavy.ActiveCount);
        Assert.Equal(Hash(heavyExpected), Hash(heavy.Accumulator));
        var heavyPlaybackLength = PrivateArrayLength(heavy, "_pbRow");
        var spreadExpected = new float[capacity];
        for (var i = 0; i < playbacks; i++) spreadExpected[i] = ticks * 6f;
        using var spread = new World(library, capacity);
        for (var i = 0; i < playbacks; i++)
            spread.Requests.Start(new Row(i), new TimelineId(1), 0);
        for (var f = 0; f < ticks; f++) spread.Tick(Direction.Forward);
        Assert.Equal(playbacks, spread.ActiveCount);
        Assert.Equal(Hash(spreadExpected), Hash(spread.Accumulator));
        var spreadPlaybackLength = PrivateArrayLength(spread, "_pbRow");
        Assert.Equal(heavyPlaybackLength, spreadPlaybackLength);
        var heavyTicks = MeasureTicks(heavy, 200);
        var spreadTicks = MeasureTicks(spread, 200);
        Assert.True(heavyTicks <= spreadTicks * 3 + 500000, $"heavy={heavyTicks} spread={spreadTicks}");
    }

    [Fact]
    public void DeactivationChurnMatchesSequentialOracle()
    {
        const int duration = 40;
        const int capacity = 4096;
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = 1f + (i % 7) * 0.5f;
        using var library = MakeLibrary(1, duration, lane);
        using var world = new World(library, capacity);
        var active = new List<(int Row, int Phase)>();
        var expected = new float[capacity];
        void Fold(int frame)
        {
            foreach (var (row, phase) in active)
            {
                var t = (phase + frame) % duration;
                expected[row] += lane[t];
            }
        }
        // Bulk start 2000 rows, then alternate deactivation/reactivation of
        // the back half. Every tick's fold uses the set active at that tick.
        for (var i = 0; i < 2000; i++)
        {
            var phase = (i * 13 + 7) % duration;
            active.Add((i, phase));
            world.Requests.Start(new Row(i), new TimelineId(1), phase);
        }
        Fold(1);
        world.Tick(Direction.Forward);
        for (var round = 0; round < 4; round++)
        {
            for (var i = 2000; i < 2500; i++)
            {
                var phase = (i * 13 + 7) % duration;
                active.Add((i, phase));
                world.Requests.Start(new Row(i), new TimelineId(1), phase);
            }
            Fold(2 + round * 2);
            world.Tick(Direction.Forward);
            for (var i = 1000; i < 2500; i++)
            {
                active.RemoveAll(p => p.Row == i);
                world.Requests.Stop(new Row(i), new TimelineId(1));
            }
            Fold(3 + round * 2);
            world.Tick(Direction.Forward);
        }
        Assert.Equal(1000, world.ActiveCount);
        Assert.Equal(Hash(expected), Hash(world.Accumulator));
    }

    [Fact]
    public void PrepareTimeIndependentOfActiveCount()
    {
        const int capacity = 65536;
        var lane = ConstLane(40, 6f);
        using var library = MakeLibrary(1, 40, lane);
        World MkWorld(int playbacks)
        {
            var w = new World(library, capacity);
            for (var i = 0; i < playbacks; i++)
                w.Requests.Start(new Row(i), new TimelineId(1), i % 40);
            return w;
        }
        using var one = MkWorld(1);
        using var k = MkWorld(1000);
        using var fourK = MkWorld(4000);
        var t1 = MeasureTicks(one, 200);
        var tK = MeasureTicks(k, 200);
        var t4K = MeasureTicks(fourK, 200);
        // Scaffolding (prepare) does not scale with active count: per-visit
        // slope stays consistent across the 1k->4k range.
        var slopeA = (double)(tK - t1) / 999;
        var slopeB = (double)(t4K - tK) / 3000;
        Assert.True(slopeA > 0 && slopeB > 0, $"slopeA={slopeA} slopeB={slopeB} t1={t1} tK={tK} t4K={t4K}");
        Assert.True(slopeB <= slopeA * 3 + 100000, $"lateSlope={slopeB} earlySlope={slopeA}");
        Assert.True(t4K > t1 + 5000, $"t4K={t4K} t1={t1}");
    }
}
