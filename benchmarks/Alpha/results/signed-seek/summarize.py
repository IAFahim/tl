import csv
import json
import statistics
from pathlib import Path


FRAMES = {
    "LiteralPositiveOne": 1,
    "LiteralNegativeOne": 1,
    "RuntimePositiveOne": 1,
    "RuntimeNegativeOne": 1,
    "AlternatingOne": 1,
    "LiteralPositiveFive": 5,
    "LiteralNegativeFive": 5,
    "RuntimePositiveFive": 5,
    "RuntimeNegativeFive": 5,
    "RepeatedPositiveOneFive": 5,
    "RepeatedNegativeOneFive": 5,
    "LiteralPositiveSixtyFour": 64,
    "RuntimePositiveSixtyFour": 64,
}
METHODS = ("Direct", "Typed", "Dynamic")
ROOT = Path(__file__).parent


def read_run(index):
    path = ROOT / f"run{index}" / "SignedSeekBenchmarks-report-full.json"
    report = json.loads(path.read_text(encoding="utf-8"))
    return {
        (benchmark["Parameters"].split("=", 1)[1], benchmark["Method"]): benchmark
        for benchmark in report["Benchmarks"]
    }


runs = [read_run(index) for index in (1, 2, 3)]
with (ROOT / "summary.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow((
        "case",
        "method",
        "frames-per-call",
        "run1-median-ns-call",
        "run2-median-ns-call",
        "run3-median-ns-call",
        "median-of-medians-ns-call",
        "median-of-medians-ns-frame",
        "allocated-bytes",
    ))
    for seek_case, frames in FRAMES.items():
        for method in METHODS:
            benchmarks = [run[(seek_case, method)] for run in runs]
            medians = [benchmark["Statistics"]["Median"] for benchmark in benchmarks]
            allocations = {
                benchmark["Memory"]["BytesAllocatedPerOperation"]
                for benchmark in benchmarks
            }
            if allocations != {0}:
                raise ValueError((seek_case, method, allocations))
            median = statistics.median(medians)
            writer.writerow((
                seek_case,
                method,
                frames,
                *(f"{value:.9f}" for value in medians),
                f"{median:.9f}",
                f"{median / frames:.9f}",
                0,
            ))
