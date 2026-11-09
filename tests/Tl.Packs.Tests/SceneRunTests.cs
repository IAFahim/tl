using Tl.Bake;
using Tl.Pack.Inventory;
using Tl.Pack.Stats;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class SceneRunTests
{
    internal const ulong InventoryHash = 6806057278687917896UL;
    internal const ulong StatsHash = 13171269908726092120UL;
    internal const ulong CrowdHash = 18108985316199147936UL;
    internal const ulong InventoryFastHash = 17812265086550969911UL;
    internal const ulong InventoryMixedHash = 15172252175353637725UL;
    const string DemoBag = "player Bag: Wood 1, Stone 3, Plank 2, Potion 1, Sword 1, Coin 10, Key 0, Gem 1\n";

    static string Demo(string name, string file) => Path.Combine(ForceTests.Root(), "demos", name, file);

    static (ulong Hash, string Text) Run(string demo, string scene, Type pack, int steps, int every)
    {
        var output = new StringWriter();
        var hash = RunCommand.Scene(Demo(demo, scene), null, [pack.Assembly.Location], steps, every, every > 0, output, out _);
        return (hash, output.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void TheDemoProjectsBakeToThePinnedPackLibraries()
    {
        Assert.Equal(InventoryTests.Library(), RunCommand.BakeProject(Demo("inventory", "inventory.scene.json"), File.ReadAllBytes(Demo("inventory", "inventory.scene.json")), [typeof(Bag).Assembly.Location]));
        Assert.Equal(StatsTests.Library(), RunCommand.BakeProject(Demo("stats", "stats.scene.json"), File.ReadAllBytes(Demo("stats", "stats.scene.json")), [typeof(Health).Assembly.Location]));
    }

    [Fact]
    public void TheInventorySceneRewindsToAnEmptyBagAndEndsWithTheDemoBag()
    {
        var (hash, text) = Run("inventory", "inventory.scene.json", typeof(Bag), 1500, 300);
        Assert.Contains("step 900 hash=", text);
        Assert.Contains("player Bag: Wood 0, Stone 0, Plank 0, Potion 0, Sword 0, Coin 0, Key 0, Gem 0\nstep 1200", text);
        Assert.EndsWith("player Bag: Wood 1, Stone 3, Plank 2, Potion 1, Sword 1, Coin 10, Key 0, Gem 1\n", text);
        Assert.Equal(InventoryHash, hash);
        Assert.Equal(hash, Run("inventory", "inventory.scene.json", typeof(Bag), 1500, 0).Hash);
    }

    [Fact]
    public void TheStatsSceneGivesTheDemoCheckpointsAndItsPinnedHash()
    {
        var (hash, text) = Run("stats", "stats.scene.json", typeof(Health), 600, 50);
        Assert.Contains("step 200 hash=15215298177589687405\n  hero Attack: Base 10, Bonus 10\n  hero Health: Value 64, Max 100\n", text);
        Assert.Contains("step 450 hash=17573985342977318404\n  hero Attack: Base 10, Bonus 10\n  hero Health: Value 53, Max 100\n", text);
        Assert.Contains("step 600 hash=13171269908726092120\n  hero Attack: Base 10, Bonus 0\n  hero Health: Value 72, Max 100\n", text);
        Assert.Equal(StatsHash, hash);
    }

    [Fact]
    public void ACheckpointHashIsTheRunningHashOfAShorterRun()
    {
        var (_, text) = Run("stats", "stats.scene.json", typeof(Health), 600, 50);
        Assert.Contains($"step 200 hash={Run("stats", "stats.scene.json", typeof(Health), 200, 0).Hash}\n", text);
    }

    [Fact]
    public void TheInventoryDoubledEndsWithTheDemoBagInHalfTheSteps()
    {
        var (hash, text) = Run("inventory", "inventory-fast.scene.json", typeof(Bag), 300, 100);
        Assert.EndsWith(DemoBag, text);
        Assert.Equal(InventoryFastHash, hash);
        Assert.Equal(hash, Run("inventory", "inventory-fast.scene.json", typeof(Bag), 300, 0).Hash);
    }

    [Fact]
    public void TheInventoryWithChangingSpeedsEndsWithTheDemoBag()
    {
        var (hash, text) = Run("inventory", "inventory-mixed.scene.json", typeof(Bag), 700, 100);
        Assert.Contains("step 400 hash=1598489104830719401\n  player Bag: Wood 1, Stone 3, Plank 2, Potion 1, Sword 0, Coin 10, Key 0, Gem 1\n", text);
        Assert.EndsWith(DemoBag, text);
        Assert.Equal(InventoryMixedHash, hash);
    }

    [Fact]
    public void ASpeedThatIsNotAMultipleOfOneSixteenthIsRefused()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Demo("inventory", "tl.project.json"))!))
            File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
        var path = Path.Combine(folder, "inventory-fast.scene.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"speed\": 2", "\"speed\": 0.3"));
        try
        {
            var error = Assert.Throws<Tl.Gen.Tlb.BakeDiagnosticException>(() => RunCommand.Scene(path, null, [typeof(Bag).Assembly.Location], 10, 0, false, new StringWriter(), out _));
            Assert.Contains("multiple of 1/16", error.Message);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Theory]
    [InlineData("inventory", "inventory.scene.json", 1500, InventoryHash, typeof(Bag))]
    [InlineData("inventory", "inventory-fast.scene.json", 300, InventoryFastHash, typeof(Bag))]
    [InlineData("inventory", "inventory-mixed.scene.json", 700, InventoryMixedHash, typeof(Bag))]
    [InlineData("stats", "stats.scene.json", 600, StatsHash, typeof(Health))]
    public void AnExportedSceneIsStableAndRunsToTheSameHash(string demo, string file, int steps, ulong expected, Type pack)
    {
        var assemblies = new List<string> { pack.Assembly.Location };
        var library = demo == "inventory" ? InventoryTests.Library() : StatsTests.Library();
        var original = File.ReadAllBytes(Demo(demo, file));
        var manifest = Tl.Gen.Tlb.TlScene.ManifestOf(library, out _);
        var scene = Tl.Gen.Tlb.TlScene.Read(original, new Tl.Gen.Tlb.BakerAssemblyResolver(assemblies), manifest);
        var exported = scene.ToJson(manifest);
        var again = Tl.Gen.Tlb.TlScene.Read(System.Text.Encoding.UTF8.GetBytes(exported), new Tl.Gen.Tlb.BakerAssemblyResolver(assemblies), manifest).ToJson(manifest);
        Assert.Equal(exported, again);
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            foreach (var path in Directory.GetFiles(Path.GetDirectoryName(Demo(demo, file))!))
                File.Copy(path, Path.Combine(folder, Path.GetFileName(path)));
            var target = Path.Combine(folder, "exported.scene.json");
            File.WriteAllText(target, exported);
            Assert.Equal(expected, RunCommand.Scene(target, null, assemblies, steps, 0, false, new StringWriter(), out _));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void TheCrowdSceneGivesItsPinnedCheckpointsAndTheCommittedLibraryMatchesTheJson()
    {
        var scene = Demo("crowd", "crowd.scene.json");
        Assert.Equal(File.ReadAllBytes(Demo("crowd", "crowd.tlib")), RunCommand.BakeProject(scene, File.ReadAllBytes(scene), [typeof(Tl.Pack.Motion.Pose).Assembly.Location]));
        var (hash, text) = Run("crowd", "crowd.scene.json", typeof(Tl.Pack.Motion.Pose), 600, 150);
        Assert.Contains("step 150 hash=18310795104460496812\n", text);
        Assert.Contains("step 300 hash=8426876472808293853\n", text);
        Assert.Contains("step 450 hash=16975299878586844118\n", text);
        Assert.Equal(CrowdHash, hash);
        Assert.Equal(hash, Run("crowd", "crowd.scene.json", typeof(Tl.Pack.Motion.Pose), 600, 0).Hash);
    }

    [Fact]
    public void AServerThatUsesOnlyCoreTheLibraryAndThePackGivesTheCrowdHash()
    {
        Assert.Equal(CrowdHash, CrowdServer.Host.Run(File.ReadAllBytes(Demo("crowd", "crowd.tlib")), CrowdServer.Host.Steps));
    }
}
