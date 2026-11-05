import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "eng"))
import render_numbers

SCENARIO_IDS = [
    "folded",
    "folded-backward",
    "fold-fed",
    "frame",
    "four-pair",
    "1-playback",
    "2-playback",
    "64-consumer",
]
GATED_IDS = ["folded", "fold-fed", "frame", "four-pair"]


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
            self.assertGreater(scenario["Hot"]["Ns"], 0, scenario["Id"])
            self.assertGreater(scenario["Cold"]["Ns"], 0, scenario["Id"])
            self.assertGreaterEqual(scenario["WarmupMs"], 500, scenario["Id"])
            self.assertGreater(scenario["WarmupFrames"], 0, scenario["Id"])
            if scenario["Id"] in GATED_IDS:
                self.assertIsNotNone(scenario["BaselineNs"], scenario["Id"])
                self.assertGreater(scenario["Ratio"], 0, scenario["Id"])
        self.assertIsNone(receipt["Bake"])
        self.assertTrue(receipt["Fingerprint"]["Cpu"])

    def test_receipt_records_parity_and_two_playback_ratio(self):
        receipt = render_numbers.load_receipt(ROOT)
        self.assertEqual(receipt["Checksum"], 18150555008925706741)
        self.assertTrue(receipt["Parity"]["DoubleRunMatch"])
        self.assertEqual(
            sorted(receipt["Parity"]["Hashes"]), sorted(SCENARIO_IDS))
        for key in receipt["Parity"]["Hashes"].values():
            self.assertRegex(key, "^[0-9a-f]{16}$")
        self.assertGreater(receipt["TwoVsOne"], 0)
        self.assertIsNone(receipt["Tiering"]["TieredCompilation"])
        self.assertIsNone(receipt["Tiering"]["EnableAVX2"])
        self.assertIsNone(receipt["Tiering"]["EnableHWIntrinsic"])

    def test_render_describes_gates_without_handwritten_baseline(self):
        rendered = render_numbers.render(render_numbers.load_receipt(ROOT))
        self.assertIn("4.0", rendered)
        self.assertIn("TwoVsOne", render_numbers.load_receipt(ROOT))
        self.assertNotIn("hand-written", rendered)


if __name__ == "__main__":
    unittest.main()
