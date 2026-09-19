import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "eng"))
import render_numbers

SCENARIO_IDS = ["sync", "groups", "own-clock", "finite", "handwritten", "squads", "worst"]


class ReadmeNumbersTests(unittest.TestCase):
    def test_readme_numbers_section_matches_receipt(self):
        readme = (ROOT / "README.md").read_text(encoding="utf-8")
        receipt = render_numbers.load_receipt(ROOT)
        self.assertIn(render_numbers.START, readme)
        self.assertIn(render_numbers.END, readme)
        self.assertEqual(render_numbers.block(readme), render_numbers.render(receipt))

    def test_receipt_covers_every_scenario_with_zero_warm_allocation(self):
        receipt = render_numbers.load_receipt(ROOT)
        self.assertEqual([s["Id"] for s in receipt["Scenarios"]], SCENARIO_IDS)
        for scenario in receipt["Scenarios"]:
            self.assertEqual(scenario["Allocated"], 0, scenario["Id"])
        self.assertGreater(receipt["Bake"]["CorpusMb"], 15)
        self.assertTrue(receipt["Fingerprint"]["Cpu"])


if __name__ == "__main__":
    unittest.main()
