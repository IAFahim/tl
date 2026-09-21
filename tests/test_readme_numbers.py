import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "eng"))
import render_numbers

SCENARIO_IDS = ["sync", "shared-clock", "groups", "own-clock", "finite", "handwritten", "handwritten-simd", "squads", "worst"]
STEADY_SHAPE_IDS = ["per-entity-apply-step", "per-entity-fused", "per-entity-lane", "shared-clock-crowd", "per-entity-record-floor", "sparse-separate-apply-step", "sparse-batch-apply-step", "sparse-batch-dense-list", "sparse-list-crowd-apply-step"]
STEADY_ARM_IDS = ["avx2-off", "hwintrinsic-off"]


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
        self.assertGreater(receipt["Bake"]["CorpusMb"], 15)
        self.assertTrue(receipt["Fingerprint"]["Cpu"])

    def test_receipt_records_steady_baselines_and_tiering_state(self):
        receipt = render_numbers.load_receipt(ROOT)
        self.assertEqual(receipt["Checksum"], 63429206184715)
        self.assertEqual([s["Id"] for s in receipt["Steady"]["Shapes"]], STEADY_SHAPE_IDS)
        for shape in receipt["Steady"]["Shapes"]:
            self.assertEqual(shape["Allocated"], 0, shape["Id"])
            self.assertGreater(shape["Ns"], 0, shape["Id"])
        self.assertEqual([a["Id"] for a in receipt["Steady"]["Arms"]], STEADY_ARM_IDS)
        for arm in receipt["Steady"]["Arms"]:
            self.assertRegex(arm["Env"], r"DOTNET_\w+=0")
            self.assertTrue(arm["Shapes"], arm["Id"])
            for shape in arm["Shapes"]:
                self.assertGreater(shape["Ns"], 0, f"{arm['Id']}/{shape['Id']}")
        self.assertIsNone(receipt["Tiering"]["TieredCompilation"])
        self.assertIsNone(receipt["Tiering"]["EnableAVX2"])
        self.assertIsNone(receipt["Tiering"]["EnableHWIntrinsic"])

    def test_render_omits_bake_sentence_when_receipt_has_no_bake(self):
        """A receipt with 'Bake': None omits the bake sentence from the closing paragraph.
        A receipt filtered to exclude handwritten-simd instead raises StopIteration at
        render()'s simd lookup (pre-existing behavior, noted on #309's close-out): the
        renderer is receipt-shaped and refresh-numbers receipts always carry that row."""
        scenarios = [
            {"Id": "shared-clock", "Label": "shared clock", "Hot": {"Ms": 0.31, "Ns": 310.0}, "Cold": {"Ms": 1.1, "Ns": 1100.0}},
            {"Id": "handwritten-simd", "Label": "hand-written SIMD", "Hot": {"Ms": 0.25, "Ns": 250.0}, "Cold": {"Ms": 0.9, "Ns": 900.0}},
        ]
        receipt = {"Fingerprint": {"Cpu": "Test CPU"}, "Reps": 5, "Rounds": 3, "Scenarios": scenarios, "Bake": None}
        rendered = render_numbers.render(receipt)
        self.assertNotIn("bakes in", rendered)
        receipt["Bake"] = {"CorpusMb": 24.0, "BakeMs": 900, "LoadMs": 1.2}
        self.assertIn("bakes in 900 ms", render_numbers.render(receipt))


if __name__ == "__main__":
    unittest.main()
