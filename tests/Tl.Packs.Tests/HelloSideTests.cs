using Xunit;

namespace Tl.Packs.Tests;

// demos/hello-side: Hello Tl plus a Side that travels with the forward move, clip -> track -> kind -> column, the walk-through in demos/hello/README.md.
public sealed class HelloSideTests
{
    [Fact]
    public void TheSideMoveArrivesWithTheForwardMoveAndUndoesWithIt()
    {
        var path = Path.Combine(ForceTests.Root(), "demos", "hello-side", "HelloSide.tlib");
        var changes = HelloSide.Host.Run(File.ReadAllBytes(path));
        Assert.Equal(new[] { (30, 5f, -2f), (90, 13f, 1f), (119, 5f, -2f) }, changes);
    }
}
