import shutil
import subprocess
import tempfile
import time
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class UnityPackageTests(unittest.TestCase):
    def test_separate_invocations_are_byte_identical(self):
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
            self.package(repository, first)
            time.sleep(1.1)
            self.package(repository, second)

            first_archive = next(first.glob("*.tgz"))
            second_archive = next(second.glob("*.tgz"))
            self.assertEqual(first_archive.read_bytes(), second_archive.read_bytes())

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
    def package(repository, output):
        subprocess.run(
            [repository / "eng" / "package-unity", output],
            cwd=repository,
            capture_output=True,
            text=True,
            check=True,
        )


if __name__ == "__main__":
    unittest.main()
