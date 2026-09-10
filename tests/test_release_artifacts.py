import importlib.util
import json
import tempfile
import unittest
import zipfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("release_artifacts", ROOT / "eng" / "release_artifacts.py")
RELEASE_ARTIFACTS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RELEASE_ARTIFACTS)


class ReleaseArtifactTests(unittest.TestCase):
    def test_canonical_packages_are_byte_identical(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            first = root / "first.nupkg"
            second = root / "second.nupkg"
            normalized_first = root / "normalized-first.nupkg"
            normalized_second = root / "normalized-second.nupkg"
            self.write_archive(first, (2024, 1, 2, 3, 4, 6))
            self.write_archive(second, (2026, 7, 8, 9, 10, 12))

            RELEASE_ARTIFACTS.canonicalize(first, normalized_first)
            RELEASE_ARTIFACTS.canonicalize(second, normalized_second)

            self.assertEqual(normalized_first.read_bytes(), normalized_second.read_bytes())
            with zipfile.ZipFile(normalized_first) as archive:
                self.assertEqual((1980, 1, 1, 0, 0, 0), archive.getinfo("data.txt").date_time)

    def test_manifest_and_checksums_are_stable(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "b.bin").write_bytes(b"b")
            (root / "a.bin").write_bytes(b"a")

            RELEASE_ARTIFACTS.write_manifest(root, "candidate", "issue-8", "1.0.0-alpha.2", "a" * 40, "10.0.401", "linux-x64")
            first_manifest = (root / "RELEASE-MANIFEST.json").read_bytes()
            first_checksums = (root / "SHA256SUMS").read_bytes()
            RELEASE_ARTIFACTS.write_manifest(root, "candidate", "issue-8", "1.0.0-alpha.2", "a" * 40, "10.0.401", "linux-x64")

            self.assertEqual(first_manifest, (root / "RELEASE-MANIFEST.json").read_bytes())
            self.assertEqual(first_checksums, (root / "SHA256SUMS").read_bytes())
            manifest = json.loads(first_manifest)
            self.assertEqual(["a.bin", "b.bin"], [artifact["path"] for artifact in manifest["artifacts"]])

    def test_nuget_publish_requires_manual_dispatch(self):
        workflow = (ROOT / ".github" / "workflows" / "publish-nuget.yml").read_text(encoding="utf-8")
        trigger = workflow.split("permissions:", 1)[0]
        self.assertIn("workflow_dispatch:", trigger)
        self.assertNotIn("release:", trigger)
        self.assertIn("license_and_owner_decisions", trigger)

    @staticmethod
    def write_archive(path, timestamp):
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            entry = zipfile.ZipInfo("data.txt", timestamp)
            entry.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(entry, b"same")


if __name__ == "__main__":
    unittest.main()
