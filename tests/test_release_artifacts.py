import gzip
import importlib.util
import io
import json
import shutil
import subprocess
import tempfile
import tarfile
import time
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

    def test_unity_package_separate_invocations_are_byte_identical(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            repository = directory / "repository"
            (repository / "eng").mkdir(parents=True)
            shutil.copy2(ROOT / "eng" / "package-unity", repository / "eng" / "package-unity")
            shutil.copytree(ROOT / "src" / "Tl.Unity", repository / "src" / "Tl.Unity")
            self.git(repository, "init", "-q")
            self.git(repository, "config", "user.name", "package-test")
            self.git(repository, "config", "user.email", "package-test@example.invalid")
            self.git(repository, "config", "commit.gpgsign", "false")
            self.git(repository, "add", ".")
            self.git(repository, "commit", "-qm", "fixture")

            first = directory / "first"
            second = directory / "second"
            self.package_unity(repository, first)
            time.sleep(1.1)
            self.package_unity(repository, second)

            first_archive = next(first.glob("*.tgz"))
            second_archive = next(second.glob("*.tgz"))
            self.assertEqual(first_archive.read_bytes(), second_archive.read_bytes())
            manifest = (first / "package.txt").read_text(encoding="utf-8")
            self.assertIn("archive-entries\t", manifest)
            self.assertNotIn("\nfiles\t", manifest)

    def test_unity_package_verifier_requires_exact_name_content_and_identity(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            valid = root / "com.iafahim.tl-1.2.3.tgz"
            self.write_unity_package(valid)
            RELEASE_ARTIFACTS.verify_unity_package(root, "1.2.3")

            self.write_unity_package(valid, extra="package/Editor/Compiler.dll")
            with self.assertRaisesRegex(ValueError, "files are"):
                RELEASE_ARTIFACTS.verify_unity_package(root, "1.2.3")

            self.write_unity_package(valid, descriptor_version="1.2.4")
            with self.assertRaisesRegex(ValueError, "identity"):
                RELEASE_ARTIFACTS.verify_unity_package(root, "1.2.3")

            for duplicate in ("package/README.md", "package/package.json"):
                self.write_unity_package(valid, duplicates=[duplicate])
                with self.assertRaisesRegex(ValueError, "duplicate archive paths"):
                    RELEASE_ARTIFACTS.verify_unity_package(root, "1.2.3")

    def test_release_pipeline_builds_and_verifies_two_unity_packages(self):
        script = (ROOT / "eng" / "release-artifacts").read_text(encoding="utf-8")
        first = script.index('"$root/eng/package-unity" "$stage/unity-a"')
        second = script.index('"$root/eng/package-unity" "$stage/unity-b"')
        compare = script.index('cmp "$stage/unity-a/com.iafahim.tl-$version.tgz"')
        copy = script.index('cp "$stage/unity-a/com.iafahim.tl-$version.tgz" "$output/"')
        verify = script.index('release_artifacts.py" verify "$output"')
        self.assertLess(first, second)
        self.assertLess(second, compare)
        self.assertLess(compare, copy)
        self.assertLess(copy, verify)

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
        self.assertIn("if: ${{ false }}", workflow)
        self.assertIn("id-token: write", publish)
        self.assertIn("actions/download-artifact@634f93cb2916e3fdff6788551b99b062d0335ce0", publish)
        self.assertIn("Tl.Runtime.$version.nupkg", publish)
        self.assertIn('expected_unity=("com.iafahim.tl-$version.tgz")', publish)
        self.assertIn("actual_unity", publish)
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

    def test_compiler_package_requires_both_targets_and_bounded_dependency(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            valid = root / "valid.nupkg"
            unexpected = root / "unexpected.nupkg"
            unexpected_dependency = root / "unexpected-dependency.nupkg"
            wrong_dependency = root / "wrong-dependency.nupkg"
            self.write_compiler_package(valid)
            self.write_compiler_package(unexpected, extra="lib/net8.0/Tl.Compiler.dll")
            self.write_compiler_package(unexpected_dependency, extra_dependency=True)
            self.write_compiler_package(wrong_dependency, dependency_version="10.0.0")

            RELEASE_ARTIFACTS.verify_nupkg(valid, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "files are"):
                RELEASE_ARTIFACTS.verify_nupkg(unexpected, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "dependency groups"):
                RELEASE_ARTIFACTS.verify_nupkg(unexpected_dependency, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "dependency groups"):
                RELEASE_ARTIFACTS.verify_nupkg(wrong_dependency, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")

    def test_symbol_verifier_requires_portable_embedded_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            valid = root / "valid.snupkg"
            invalid = root / "invalid.snupkg"
            self.write_symbol_package(valid, b"BSJB" + RELEASE_ARTIFACTS.EMBEDDED_SOURCE_GUID)
            self.write_symbol_package(invalid, b"BSJB")

            RELEASE_ARTIFACTS.verify_snupkg(valid, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")
            with self.assertRaisesRegex(ValueError, "embedded-source"):
                RELEASE_ARTIFACTS.verify_snupkg(invalid, "Tl.Compiler", "1.2.3", "a" * 40, "refs/tags/v1.2.3")

    def test_nuget_publish_fails_on_an_existing_package(self):
        workflow = (ROOT / ".github" / "workflows" / "publish-nuget.yml").read_text(encoding="utf-8")
        publish = workflow.split("  publish:", 1)[1]
        self.assertEqual(5, publish.count("dotnet nuget push "))
        self.assertNotIn("--skip-duplicate", publish)

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
    def package_unity(repository, output):
        subprocess.run(
            [repository / "eng" / "package-unity", output],
            cwd=repository,
            capture_output=True,
            text=True,
            check=True,
        )

    @staticmethod
    def write_archive(path, timestamp):
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            entry = zipfile.ZipInfo("data.txt", timestamp)
            entry.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(entry, b"same")

    @staticmethod
    def write_unity_package(path, descriptor_version="1.2.3", extra=None, duplicates=()):
        entries = set(RELEASE_ARTIFACTS.UNITY_PACKAGE_FILES)
        if extra is not None:
            entries.add(extra)
        tar_bytes = io.BytesIO()
        with tarfile.open(fileobj=tar_bytes, mode="w", format=tarfile.USTAR_FORMAT) as archive:
            for name in [*sorted(entries), *duplicates]:
                directory = name.endswith("/")
                entry = tarfile.TarInfo(name.rstrip("/") if directory else name)
                entry.type = tarfile.DIRTYPE if directory else tarfile.REGTYPE
                entry.mode = 0o755 if directory else 0o644
                entry.mtime = 0
                content = b"" if directory else (
                    json.dumps({"name": "com.iafahim.tl", "version": descriptor_version}).encode()
                    if name == "package/package.json" else b"content"
                )
                entry.size = len(content)
                archive.addfile(entry, None if directory else io.BytesIO(content))
        with path.open("wb") as stream:
            with gzip.GzipFile(filename="", mode="wb", fileobj=stream, mtime=0) as compressed:
                compressed.write(tar_bytes.getvalue())

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
        nuspec = f"""<package><metadata><id>Tl.Compiler</id><version>1.2.3</version><repository type="git" url="https://github.com/IAFahim/tl" branch="refs/tags/v1.2.3" commit="{'a' * 40}"/></metadata></package>"""
        files = RELEASE_ARTIFACTS.SYMBOL_METADATA_FILES | {"Tl.Compiler.nuspec"} | RELEASE_ARTIFACTS.SYMBOL_FILES["Tl.Compiler"]
        with zipfile.ZipFile(path, "w") as archive:
            for name in files:
                archive.writestr(name, nuspec if name.endswith(".nuspec") else pdb if name.endswith(".pdb") else b"content")

    @staticmethod
    def write_compiler_package(path, dependency_version="10.0.1", extra=None, extra_dependency=False):
        dependency = f"""<dependency id="System.Collections.Immutable" version="{dependency_version}" exclude="Build,Analyzers"/>"""
        if extra_dependency:
            dependency += """<dependency id="Unexpected" version="1.0.0"/>"""
        nuspec = f"""<package><metadata><id>Tl.Compiler</id><version>1.2.3</version><repository type="git" url="https://github.com/IAFahim/tl" branch="refs/tags/v1.2.3" commit="{'a' * 40}"/><dependencies><group targetFramework="net10.0"/><group targetFramework=".NETStandard2.0">{dependency}</group></dependencies></metadata></package>"""
        files = RELEASE_ARTIFACTS.PACKAGE_FILES["Tl.Compiler"] | RELEASE_ARTIFACTS.PACKAGE_METADATA_FILES | {"Tl.Compiler.nuspec"}
        if extra is not None:
            files.add(extra)
        with zipfile.ZipFile(path, "w") as archive:
            for name in files:
                archive.writestr(name, nuspec if name.endswith(".nuspec") else b"content")


if __name__ == "__main__":
    unittest.main()
