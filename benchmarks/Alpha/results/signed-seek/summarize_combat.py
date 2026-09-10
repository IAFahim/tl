import csv
import json
import statistics
from pathlib import Path


ROOT = Path(__file__).parent
PATTERNS = ("Forward", "Alternating")
METHODS = ("DirectScalar", "TypedScalar", "DynamicScalar")


def read_run(index):
    path = ROOT / f"combat-run{index}" / "CombatBenchmarks-report-full.json"
    report = json.loads(path.read_text(encoding="utf-8"))
    return {
        (benchmark["Parameters"].split("=", 1)[1], benchmark["Method"]): benchmark
        for benchmark in report["Benchmarks"]
    }


runs = [read_run(index) for index in (1, 2, 3)]
with (ROOT / "combat-summary.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow((
        "pattern",
        "method",
        "run1-median-ns-frame",
        "run2-median-ns-frame",
        "run3-median-ns-frame",
        "median-of-medians-ns-frame",
        "allocated-bytes",
    ))
    for pattern in PATTERNS:
        for method in METHODS:
            benchmarks = [run[(pattern, method)] for run in runs]
            medians = [benchmark["Statistics"]["Median"] for benchmark in benchmarks]
            allocations = {
                benchmark["Memory"]["BytesAllocatedPerOperation"]
                for benchmark in benchmarks
            }
            if allocations != {0}:
                raise ValueError((pattern, method, allocations))
            writer.writerow((
                pattern,
                method,
                *(f"{value:.9f}" for value in medians),
                f"{statistics.median(medians):.9f}",
                0,
            ))
