import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CI = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
REFERENCE_SOURCES = {
    "ci.yml": CI,
    "README.md": (ROOT / "README.md").read_text(encoding="utf-8"),
    "AGENTS.md": (ROOT / "AGENTS.md").read_text(encoding="utf-8"),
}
SAMPLE_REFERENCE = re.compile(r"samples/[\w./-]+")
BUILD_OUTPUT_PARTS = {"bin", "obj"}
BUILD_OUTPUT_SUFFIXES = (".dll", ".tlb", ".exe", ".pdb")


def referenced_sample_paths():
    references = {}
    for source, text in REFERENCE_SOURCES.items():
        for token in SAMPLE_REFERENCE.findall(text):
            token = token.rstrip(".,;:)")
            if any(part in BUILD_OUTPUT_PARTS for part in token.split("/")):
                continue
            if token.endswith(BUILD_OUTPUT_SUFFIXES):
                continue
            references.setdefault(token, source)
    return references


class SamplesInventoryTests(unittest.TestCase):
    def test_every_sample_project_is_named_by_ci(self):
        for csproj in sorted(ROOT.glob("samples/*/*.csproj")):
            self.assertIn(f"samples/{csproj.parent.name}", CI, f"{csproj.parent.name} has no CI leg")

    def test_every_sample_project_exists_for_ci_and_docs(self):
        missing = [
            f"{token} named by {source} does not exist"
            for token, source in sorted(referenced_sample_paths().items())
            if not (ROOT / token).exists()
        ]
        self.assertEqual([], missing)


if __name__ == "__main__":
    unittest.main()
