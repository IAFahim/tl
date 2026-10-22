import hashlib
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DOC = ROOT / "docs" / "tlb1-layout.md"
FIXTURES = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "fixtures"


class Tlb1SpecExampleTest(unittest.TestCase):
    def test_worked_example_bytes_equal_the_minimal_fixture(self):
        doc = DOC.read_text(encoding="utf-8")
        fixture = (FIXTURES / "minimal.tlb").read_bytes()

        block = re.search(
            r"## Worked example.*?\n```\n(.*?)\n```", doc, re.S
        )
        self.assertIsNotNone(block, "worked example fenced block is missing")
        bytes_by_offset = {}
        for line in block.group(1).splitlines():
            if not re.fullmatch(r"[0-9a-f]{4}", line[:4]) or line[4:6] != "  ":
                continue
            offset = int(line[:4], 16)
            for token in line[6:].split():
                if not re.fullmatch(r"[0-9A-Fa-f]{2}", token):
                    break
                bytes_by_offset[offset] = int(token, 16)
                offset += 1

        self.assertEqual(
            sorted(bytes_by_offset),
            list(range(len(fixture))),
            "worked example must show every fixture byte exactly once",
        )
        self.assertEqual(
            [bytes_by_offset[i] for i in range(len(fixture))],
            list(fixture),
            "worked example bytes must equal the committed fixture",
        )

    def test_spec_states_the_shipped_format_version(self):
        doc = DOC.read_text(encoding="utf-8")
        fixture = (FIXTURES / "minimal.tlb").read_bytes()
        version = int.from_bytes(fixture[4:8], "little")
        self.assertEqual(version, 5)
        self.assertIn("(version 5)", doc)
        self.assertNotRegex(doc, r"version 4\b")
        self.assertIn("| 4 | u32 | Version | `5` |", doc)

    def test_fixture_table_hashes_match_the_committed_files(self):
        doc = DOC.read_text(encoding="utf-8")
        for name in ("minimal.tlb", "blended.tlb"):
            digest = hashlib.sha256((FIXTURES / name).read_bytes()).hexdigest()
            self.assertIn(f"| `{name}` |", doc)
            self.assertIn(f"| `{digest}` |", doc)


if __name__ == "__main__":
    unittest.main()
