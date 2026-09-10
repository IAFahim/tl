import csv
import json
from pathlib import Path


ROOT = Path(__file__).parent
records = [
    json.loads(line)
    for line in (ROOT / "pmu.jsonl").read_text(encoding="utf-8").splitlines()
]
if len(records) != 45 or any("error" in record for record in records):
    raise ValueError("The PMU receipt must contain 45 successful scenarios.")

with (ROOT / "pmu.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow((
        "scenario",
        "ticks",
        "running-fraction",
        "cycles-per-frame",
        "instructions-per-frame",
        "branches-per-frame",
        "branch-misses-per-frame",
        "receipt",
    ))
    for record in records:
        counters = record["per_tick"]
        writer.writerow((
            record["scenario"],
            record["ticks"],
            f'{record["running_fraction"]:.9f}',
            f'{counters["cycles"]:.9f}',
            f'{counters["instructions"]:.9f}',
            f'{counters["branches"]:.9f}',
            f'{counters["branch_misses"]:.9f}',
            record["receipt"],
        ))
