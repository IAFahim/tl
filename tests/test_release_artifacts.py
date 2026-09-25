import importlib.util
import json
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("release_artifacts", ROOT / "eng" / "release_artifacts.py")
RELEASE_ARTIFACTS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RELEASE_ARTIFACTS)


class ReleaseArtifactTests(unittest.TestCase):
    def test_tlgen_export_exposes_defaulted_backend_and_output(self):
        targets = (ROOT / "src" / "Tl.Gen.CSharp" / "build" / "Tl.Gen.CSharp.targets").read_text(encoding="utf-8")
        self.assertIn('<TlGenBackend Condition="\'$(TlGenBackend)\' == \'\'">csharp</TlGenBackend>', targets)
        self.assertIn('<TlGenOutput Condition="\'$(TlGenOutput)\' == \'\'">$(IntermediateOutputPath)TlGenCompile\\</TlGenOutput>', targets)
        self.assertIn('--backend &quot;$(TlGenBackend)&quot;', targets)
        self.assertIn("EnsureTrailingSlash(\'$(TlGenOutput)\')", targets)

    def test_ci_proves_a_clean_isolated_output_build(self):
        script = (ROOT / "eng" / "test-isolated-output").read_text(encoding="utf-8")
        workflow = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        self.assertIn("git -C \"$root\" ls-files -z", script)
        self.assertEqual(4, script.count('--artifacts-path "$artifacts"'))
        self.assertIn("-t:TlGenExport", script)
        self.assertIn("stale/Tl.Gen.CSharp.dll", script)
        self.assertIn('find "$source" -type d', script)
        self.assertIn("- run: eng/test-isolated-output", workflow)

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

            RELEASE_ARTIFACTS.write_manifest(root, "candidate", "issue-35", "1.0.0-alpha.3", "a" * 40, "10.0.401", "linux-x64")
            first_manifest = (root / "RELEASE-MANIFEST.json").read_bytes()
            first_checksums = (root / "SHA256SUMS").read_bytes()
            RELEASE_ARTIFACTS.write_manifest(root, "candidate", "issue-35", "1.0.0-alpha.3", "a" * 40, "10.0.401", "linux-x64")

            self.assertEqual(first_manifest, (root / "RELEASE-MANIFEST.json").read_bytes())
            self.assertEqual(first_checksums, (root / "SHA256SUMS").read_bytes())
            manifest = json.loads(first_manifest)
            self.assertEqual(["a.bin", "b.bin"], [artifact["path"] for artifact in manifest["artifacts"]])

    def test_nuget_publish_requires_manual_dispatch(self):
        workflow = (ROOT / ".github" / "workflows" / "publish-nuget.yml").read_text(encoding="utf-8")
        trigger = workflow.split("permissions:", 1)[0]
        validate = workflow.split("  validate:", 1)[1].split("  publish:", 1)[0]
        publish = workflow.split("  publish:", 1)[1]
        self.assertIn("workflow_dispatch:", trigger)
        self.assertNotIn("release:", trigger)
        self.assertIn("ref: refs/tags/${{ inputs.tag }}", validate)
        self.assertNotIn("id-token: write", validate)
        self.assertIn("environment: nuget-production", workflow)
        self.assertNotIn("if:", publish)
        self.assertIn("id-token: write", publish)
        self.assertIn("actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c", publish)
        self.assertIn("Tl.Runtime.$version.nupkg", publish)
        self.assertNotIn("*.nupkg\" --api-key", publish)
        self.assertNotIn("gh release download", workflow)
        self.assertNotIn("license_and_owner_decisions", workflow)

    def test_package_verifier_rejects_unexpected_files_and_dependency_attributes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            valid = root / "valid.nupkg"
            unexpected = root / "unexpected.nupkg"
            wrong_dependency = root / "wrong-dependency.nupkg"
            self.write_runtime_package(valid)
            self.write_runtime_package(unexpected, extra="unexpected.dll")
            self.write_runtime_package(wrong_dependency, dependency=True)

            RELEASE_ARTIFACTS.verify_nupkg(valid, "Tl.Runtime", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "files are"):
                RELEASE_ARTIFACTS.verify_nupkg(unexpected, "Tl.Runtime", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "dependency groups"):
                RELEASE_ARTIFACTS.verify_nupkg(wrong_dependency, "Tl.Runtime", "1.2.3", "a" * 40, "refs/tags/v1.2.3")

    def test_symbol_verifier_requires_portable_embedded_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            valid = root / "valid.snupkg"
            invalid = root / "invalid.snupkg"
            self.write_symbol_package(valid, b"BSJB" + RELEASE_ARTIFACTS.EMBEDDED_SOURCE_GUID)
            self.write_symbol_package(invalid, b"BSJB")

            RELEASE_ARTIFACTS.verify_snupkg(valid, "Tl.Runtime", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "embedded-source"):
                RELEASE_ARTIFACTS.verify_snupkg(invalid, "Tl.Runtime", "1.2.3", "a" * 40, "refs/tags/v1.2.3")

    def test_nuget_publish_skips_packages_already_on_nuget_org(self):
        workflow = (ROOT / ".github" / "workflows" / "publish-nuget.yml").read_text(encoding="utf-8")
        publish = workflow.split("  publish:", 1)[1]
        self.assertEqual(4, publish.count("dotnet nuget push "))
        self.assertEqual(4, publish.count("--skip-duplicate"))

    def test_artifact_workflow_checks_out_the_tag_namespace(self):
        workflow = (ROOT / ".github" / "workflows" / "release-artifacts.yml").read_text(encoding="utf-8")
        self.assertIn("ref: refs/tags/${{ inputs.tag }}", workflow)
        self.assertIn("path: artifacts/release/*", workflow)

    def test_branch_with_release_name_is_not_a_tag(self):
        with tempfile.TemporaryDirectory() as temporary:
            repository = Path(temporary)
            self.git(repository, "init", "-q")
            self.git(repository, "config", "user.name", "release-test")
            self.git(repository, "config", "user.email", "release-test@example.invalid")
            (repository / "tracked").write_text("content\n", encoding="utf-8")
            self.git(repository, "add", "tracked")
            self.git(repository, "commit", "-qm", "initial")
            self.git(repository, "branch", "v1.0.0-alpha.3")

            branch_only = subprocess.run(
                [ROOT / "eng" / "release-ref", "v1.0.0-alpha.3", "1.0.0-alpha.3"],
                cwd=repository,
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertNotEqual(0, branch_only.returncode)
            self.assertIn("tag ref does not exist", branch_only.stderr)

            self.git(repository, "tag", "v1.0.0-alpha.3")
            tagged = subprocess.run(
                [ROOT / "eng" / "release-ref", "v1.0.0-alpha.3", "1.0.0-alpha.3"],
                cwd=repository,
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(0, tagged.returncode, tagged.stderr)
            self.assertEqual(self.git(repository, "rev-parse", "HEAD"), tagged.stdout.strip())

    @staticmethod
    def git(repository, *arguments):
        return subprocess.run(
            ["git", *arguments],
            cwd=repository,
            capture_output=True,
            text=True,
            check=True,
        ).stdout.strip()

    @staticmethod
    def write_archive(path, timestamp):
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            entry = zipfile.ZipInfo("data.txt", timestamp)
            entry.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(entry, b"same")

    @staticmethod
    def write_runtime_package(path, dependency=False, extra=None):
        dependencies = '<dependency id="Tl.Runtime" version="1.2.3" exclude="Compile"/>' if dependency else ""
        nuspec = f"""<package><metadata><id>Tl.Runtime</id><version>1.2.3</version><repository type="git" url="https://github.com/IAFahim/tl" branch="refs/tags/v1.2.3" commit="{'a' * 40}"/><dependencies><group targetFramework="net10.0">{dependencies}</group></dependencies></metadata></package>"""
        files = RELEASE_ARTIFACTS.PACKAGE_FILES["Tl.Runtime"] | RELEASE_ARTIFACTS.PACKAGE_METADATA_FILES | {"Tl.Runtime.nuspec"}
        if extra is not None:
            files.add(extra)
        with zipfile.ZipFile(path, "w") as archive:
            for name in files:
                archive.writestr(name, nuspec if name.endswith(".nuspec") else b"content")

    @staticmethod
    def write_symbol_package(path, pdb):
        nuspec = f"""<package><metadata><id>Tl.Runtime</id><version>1.2.3</version><repository type="git" url="https://github.com/IAFahim/tl" branch="refs/tags/v1.2.3" commit="{'a' * 40}"/></metadata></package>"""
        files = RELEASE_ARTIFACTS.SYMBOL_METADATA_FILES | {"Tl.Runtime.nuspec"} | RELEASE_ARTIFACTS.SYMBOL_FILES["Tl.Runtime"]
        with zipfile.ZipFile(path, "w") as archive:
            for name in files:
                archive.writestr(name, nuspec if name.endswith(".nuspec") else pdb if name.endswith(".pdb") else b"content")


if __name__ == "__main__":
    unittest.main()
