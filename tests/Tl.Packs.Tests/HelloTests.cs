using Tl.Bake;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class HelloTests
{
    const ulong Hash = 4684325832958264737UL;

    static string Demo(string file) => Path.Combine(ForceTests.Root(), "demos", "hello", file);

    [Fact]
    public void ThePlayerJumpsAtTheClipsAndComesBackWhenAsked()
    {
        var changes = HelloTl.Host.Run(File.ReadAllBytes(Demo("HelloTl.tlib")));
        Assert.Equal(new[] { (30, 5f), (90, 13f), (119, 5f) }, changes);
    }

    [Fact]
    public void TheCommittedLibraryIsTheBakeOfTheJson()
    {
        var scene = Demo("hello.scene.json");
        Assert.Equal(File.ReadAllBytes(Demo("HelloTl.tlib")), RunCommand.BakeProject(scene, File.ReadAllBytes(scene), [typeof(HelloTl.Teleported).Assembly.Location]));
    }

    [Fact]
    public void TheSceneGivesItsPinnedHash()
    {
        var hash = RunCommand.Scene(Demo("hello.scene.json"), null, [typeof(HelloTl.Teleported).Assembly.Location], HelloTl.Host.Steps, 0, false, new StringWriter(), out _);
        Assert.Equal(Hash, hash);
    }
}
