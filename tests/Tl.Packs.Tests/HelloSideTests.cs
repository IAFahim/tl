using Xunit;

namespace Tl.Packs.Tests;

// demos/hello-side: Hello Tl plus a SideSwing that travels clip -> track -> kind -> column, the walk-through in demos/hello/README.md.
public sealed class HelloSideTests
{
    [Fact]
    public void TheSideSwingArrivesInTheColumnAndStacksAcrossClips()
    {
        var path = Path.Combine(ForceTests.Root(), "demos", "hello-side", "HelloSide.tlib");
        var changes = HelloSide.Host.Run(File.ReadAllBytes(path));
        Assert.Equal(new[] { (30, 5f, 2f), (90, 13f, 5f), (119, 5f, 5f) }, changes);
    }
}
