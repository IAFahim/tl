using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Tl.Pack.Stats.Tests
{
    public class StatsSceneTests
    {
        const ulong CliHash = 13171269908726092120UL;

        [Test]
        public void TheDemoSceneGivesTheCliHashAndCheckpointsInUnity()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Health).Assembly);
            var scene = package == null ? null : Path.Combine(package.resolvedPath, "..", "..", "demos", "stats", "stats.scene.json");
            if (scene == null || !File.Exists(scene))
                Assert.Ignore("the tl demos folder is not next to this pack");
            var library = StatsLibraryBytes.Bytes();
            var data = global::Tl.Unity.Editor.TlSceneRunner.Read(File.ReadAllBytes(scene), library);
            var seen = new Dictionary<int, ulong>();
            var hash = global::Tl.Unity.Editor.TlSceneRunner.Run(data, library, 600, (step, h) => seen[step] = h);
            Assert.AreEqual(15215298177589687405UL, seen[200]);
            Assert.AreEqual(17573985342977318404UL, seen[450]);
            Assert.AreEqual(CliHash, hash);
        }
    }
}
