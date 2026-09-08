import json
from pathlib import Path
import tempfile
import unittest

from collect import ENVIRONMENT_FIELDS, collect


class CollectionTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)

    def report(self, name, jobs, processor="cpu", median=10):
        report = {
            "HostEnvironmentInfo": {field: "fixed" for field in ENVIRONMENT_FIELDS},
            "Benchmarks": [
                {
                    "FullName": "Fixture.Sample(Clips: 16)",
                    "DisplayInfo": f"Fixture.Sample: {job} [Clips=16]",
                    "HardwareIntrinsics": "AVX2",
                    "Statistics": {"N": 12, "Median": median, "StandardDeviation": 0.01},
                }
                for job in jobs
            ],
        }
        report["HostEnvironmentInfo"]["ProcessorName"] = processor
        path = Path(self.directory.name) / name
        path.write_text(json.dumps(report))
        return path

    def test_jobs_and_parameters_keep_distinct_identities(self):
        rows, excluded = collect([self.report("jobs.json", ["Jit", "NoTiering"])], 0.1)
        self.assertEqual(2, len({row["name"] for row in rows}))
        self.assertTrue(all("Clips: 16" in row["name"] for row in rows))
        self.assertFalse(excluded)

    def test_different_processors_start_different_series(self):
        paths = [self.report("first.json", ["Jit"]), self.report("second.json", ["Jit"], processor="other")]
        rows, _ = collect(paths, 0.1)
        self.assertEqual(2, len({row["name"] for row in rows}))

    def test_duplicate_identity_rejects_entire_collection(self):
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            collect([self.report("duplicate.json", ["Jit", "Jit"])], 0.1)

    def test_zero_estimates_remain_in_excluded_report(self):
        paths = [self.report("valid.json", ["Jit"]), self.report("zero.json", ["NoTiering"], median=0)]
        rows, excluded = collect(paths, 0.1)
        self.assertEqual(1, len(rows))
        self.assertEqual(0, excluded[0]["value"])

    def test_failed_measurement_rejects_entire_collection(self):
        path = self.report("failed.json", ["Jit"])
        report = json.loads(path.read_text())
        report["Benchmarks"][0]["Statistics"] = None
        path.write_text(json.dumps(report))
        with self.assertRaisesRegex(ValueError, "Missing measurements"):
            collect([path], 0.1)

    def test_missing_or_invalid_statistics_reject_collection(self):
        for field, value in [("Median", None), ("StandardDeviation", None), ("N", "12")]:
            with self.subTest(field=field):
                path = self.report("invalid.json", ["Jit"])
                report = json.loads(path.read_text())
                report["Benchmarks"][0]["Statistics"][field] = value
                path.write_text(json.dumps(report))
                with self.assertRaises(ValueError):
                    collect([path], 0.1)

    def test_different_instruction_sets_start_different_series(self):
        first = self.report("first.json", ["Jit"])
        second = self.report("second.json", ["Jit"])
        report = json.loads(second.read_text())
        report["Benchmarks"][0]["HardwareIntrinsics"] = "AVX512"
        second.write_text(json.dumps(report))
        rows, _ = collect([first, second], 0.1)
        self.assertEqual(2, len({row["name"] for row in rows}))


if __name__ == "__main__":
    unittest.main()
