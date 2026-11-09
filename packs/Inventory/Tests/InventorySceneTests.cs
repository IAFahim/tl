using System.IO;
using NUnit.Framework;

namespace Tl.Pack.Inventory.Tests
{
    public class InventorySceneTests
    {
        const ulong CliHash = 6806057278687917896UL;
        const ulong FastHash = 17812265086550969911UL;
        const ulong MixedHash = 15172252175353637725UL;

        [Test]
        public void TheDemoSceneGivesTheCliHashInUnity()
        {
            Assert.AreEqual(CliHash, Run("inventory.scene.json", 1500));
        }

        [Test]
        public void TheDoubledSceneGivesTheCliHashInUnity()
        {
            Assert.AreEqual(FastHash, Run("inventory-fast.scene.json", 300));
        }

        [Test]
        public void TheSceneWithChangingSpeedsGivesTheCliHashInUnity()
        {
            Assert.AreEqual(MixedHash, Run("inventory-mixed.scene.json", 700));
        }

        static ulong Run(string file, int steps)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Bag).Assembly);
            var scene = package == null ? null : Path.Combine(package.resolvedPath, "..", "..", "demos", "inventory", file);
            if (scene == null || !File.Exists(scene))
                Assert.Ignore("the tl demos folder is not next to this pack");
            var library = InventoryLibraryBytes.Bytes();
            var data = global::Tl.Unity.Editor.TlSceneRunner.Read(File.ReadAllBytes(scene), library);
            return global::Tl.Unity.Editor.TlSceneRunner.Run(data, library, steps);
        }
    }
}
