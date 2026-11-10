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
}
