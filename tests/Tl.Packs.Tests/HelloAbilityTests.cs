using Xunit;

namespace Tl.Packs.Tests;

public sealed class HelloAbilityTests
{
    [Fact]
    public void TheFlagIsTrueOnlyInsideTheWindowAndFalseAgainAfterTheOffClip()
    {
        var path = Path.Combine(ForceTests.Root(), "demos", "hello-ability", "HelloAbility.tlib");
        var (windowed, neverPlayed) = HelloAbility.Host.Run(File.ReadAllBytes(path));
        Assert.Equal(new[] { (30, true), (60, false) }, windowed);
        Assert.False(neverPlayed);
    }
}
