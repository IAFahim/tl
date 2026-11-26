using NUnit.Framework;

namespace Tl.Pack.Clock.Tests
{
    public class ClockPackTests
    {
        [Test]
        public void SixtyFramesOfASixtiethRunSixtyStepsWithNothingDropped()
        {
            var clock = new FixedSteps(60, 4);
            var total = 0;
            for (var frame = 0; frame < 600; frame++) total += clock.Advance(1.0 / 60.0);
            Assert.AreEqual(600, total);
            Assert.AreEqual(0, clock.Dropped);
        }

        [Test]
        public void AHitchRunsTheCapThenDropsTheBacklog()
        {
            var clock = new FixedSteps(60, 4);
            Assert.AreEqual(4, clock.Advance(1_008_333_334L));
            Assert.AreEqual(56, clock.Dropped);
            Assert.AreEqual(1, clock.Advance(8_333_333L));
        }
    }
}
