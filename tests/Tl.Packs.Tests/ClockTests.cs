using Tl.Pack.Clock;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class ClockTests
{
    [Fact]
    public void SixtyFramesOfASixtiethRunSixtyStepsWithNothingDropped()
    {
        var clock = new FixedSteps(60, 4);
        var total = 0;
        for (var frame = 0; frame < 600; frame++) total += clock.Advance(1.0 / 60.0);
        Assert.Equal(600, total);
        Assert.Equal(0, clock.Dropped);
    }

    [Fact]
    public void FrameTimesThatDoNotDivideTheStepCarryTheirRemainderExactly()
    {
        var clock = new FixedSteps(60, 8);
        var total = 0L;
        for (var frame = 0; frame < 7000; frame++) total += clock.Advance(7_000_003L);
        Assert.Equal(7000L * 7_000_003L * 60L / 1_000_000_000L, total);
    }

    [Fact]
    public void AHitchRunsTheCapThenDropsTheBacklogAndKeepsTheFraction()
    {
        var clock = new FixedSteps(60, 4);
        Assert.Equal(4, clock.Advance(1_008_333_334L));
        Assert.Equal(56, clock.Dropped);
        Assert.Equal(0.5, clock.Alpha, 6);
        Assert.Equal(1, clock.Advance(8_333_333L));
        Assert.Equal(56, clock.Dropped);
    }

    [Fact]
    public void AFasterRateRunsSeveralStepsPerFrameAndTheAlphaTracksTheRemainder()
    {
        var clock = new FixedSteps(120, 4);
        Assert.Equal(2, clock.Advance(TimeSpan.FromMilliseconds(20)));
        Assert.Equal(0.4, clock.Alpha, 6);
        Assert.Equal(0, clock.Advance(TimeSpan.FromMilliseconds(4)));
        Assert.Equal(0.88, clock.Alpha, 6);
        clock.Reset();
        Assert.Equal(0, clock.Alpha);
    }

    [Fact]
    public void AnEnormousGapSaturatesInsteadOfOverflowing()
    {
        var clock = new FixedSteps(int.MaxValue, 3);
        Assert.Equal(3, clock.Advance(long.MaxValue));
        Assert.Equal(3, clock.Advance(long.MaxValue));
        Assert.Equal(long.MaxValue, clock.Dropped);
    }

    [Fact]
    public void NegativeTimeAndAnUnconfiguredClockAreRejected()
    {
        var clock = new FixedSteps(60, 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-1L));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedSteps(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedSteps(60, 0));
        var unset = default(FixedSteps);
        Assert.Throws<InvalidOperationException>(() => unset.Advance(1L));
    }
}
