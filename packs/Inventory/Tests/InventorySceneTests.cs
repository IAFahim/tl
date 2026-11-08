using System.IO;
using NUnit.Framework;

namespace Tl.Pack.Inventory.Tests
{
    public class InventorySceneTests
    {
        const ulong CliHash = 6806057278687917896UL;

        [Test]
        public void TheDemoSceneGivesTheCliHashInUnity()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Bag).Assembly);
            var scene = package == null ? null : Path.Combine(package.resolvedPath, "..", "..", "demos", "inventory", "inventory.scene.json");
            if (scene == null || !File.Exists(scene))
                Assert.Ignore("the tl demos folder is not next to this pack");
            var library = InventoryLibraryBytes.Bytes();
            var data = global::Tl.Unity.Editor.TlSceneRunner.Read(File.ReadAllBytes(scene), library);
            Assert.AreEqual(CliHash, global::Tl.Unity.Editor.TlSceneRunner.Run(data, library, 1500));
        }
    }
}
