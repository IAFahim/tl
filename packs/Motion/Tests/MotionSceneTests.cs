using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Tl.Pack.Motion.Tests
{
    public class MotionSceneTests
    {
        const ulong CliHash = 18108985316199147936UL;

        [Test]
        public void TheCrowdSceneGivesTheCliHashAndCheckpointsInUnity()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Pose).Assembly);
            var folder = package == null ? null : Path.Combine(package.resolvedPath, "..", "..", "demos", "crowd");
            if (folder == null || !File.Exists(Path.Combine(folder, "crowd.scene.json")))
                Assert.Ignore("the tl demos folder is not next to this pack");
            var library = File.ReadAllBytes(Path.Combine(folder, "crowd.tlib"));
            var data = global::Tl.Unity.Editor.TlSceneRunner.Read(File.ReadAllBytes(Path.Combine(folder, "crowd.scene.json")), library);
            var seen = new Dictionary<int, ulong>();
            var hash = global::Tl.Unity.Editor.TlSceneRunner.Run(data, library, 600, (step, h) => seen[step] = h);
            Assert.AreEqual(18310795104460496812UL, seen[150]);
            Assert.AreEqual(8426876472808293853UL, seen[300]);
            Assert.AreEqual(16975299878586844118UL, seen[450]);
            Assert.AreEqual(CliHash, hash);
        }
    }
}
