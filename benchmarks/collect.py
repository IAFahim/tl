import argparse
import hashlib
import json
import math
from pathlib import Path


ENVIRONMENT_FIELDS = (
    "BenchmarkDotNetVersion",
    "OsVersion",
    "ProcessorName",
    "PhysicalProcessorCount",
    "PhysicalCoreCount",
    "LogicalCoreCount",
    "RuntimeVersion",
    "Architecture",
    "DotNetCliVersion",
    "Configuration",
    "HasRyuJit",
    "HasAttachedDebugger",
    "HardwareTimerKind",
)


def collect(paths, minimum_ns):
    comparisons = []
    excluded = []
    identities = set()
    count = 0
    for path in sorted(paths):
        report = json.loads(Path(path).read_text())
        environment = report["HostEnvironmentInfo"]
        identity = {}
        for field in ENVIRONMENT_FIELDS:
            value = environment.get(field)
            if not isinstance(value, (str, int, bool)):
                raise ValueError(f"Missing or invalid environment field {field}: {path}")
            identity[field] = value
        benchmarks = report["Benchmarks"]
        if not benchmarks:
            raise ValueError(f"No benchmark cases: {path}")
        for benchmark in benchmarks:
            count += 1
            intrinsics = benchmark.get("HardwareIntrinsics")
            if not isinstance(intrinsics, str):
                raise ValueError(f"Missing hardware intrinsics: {path}")
            encoded = json.dumps({**identity, "HardwareIntrinsics": intrinsics}, sort_keys=True, separators=(",", ":"))
            fingerprint = hashlib.sha256(encoded.encode()).hexdigest()[:16]
            display = benchmark["DisplayInfo"]
            _, separator, job = display.partition(": ")
            if not separator or not job:
                raise ValueError(f"Missing job identity: {display}")
            name = f"{benchmark['FullName']} | {job} | env={fingerprint}"
            if name in identities:
                raise ValueError(f"Duplicate benchmark identity: {name}")
            identities.add(name)
            statistics = benchmark.get("Statistics")
            if not isinstance(statistics, dict) or type(statistics.get("N")) is not int or statistics["N"] < 1:
                raise ValueError(f"Missing measurements: {name}")
            for field in ("Median", "StandardDeviation"):
                value = statistics.get(field)
                if type(value) not in (float, int) or not math.isfinite(value) or value < 0:
                    raise ValueError(f"Missing or invalid {field}: {name}")
            median = statistics["Median"]
            deviation = statistics["StandardDeviation"]
            result = {
                "name": name,
                "unit": "ns",
                "value": median,
                "range": f"± {deviation:.6g}",
                "extra": f"Median; range is standard deviation in ns; N={statistics['N']}; {encoded}; source={Path(path).name}",
            }
            if median < minimum_ns:
                excluded.append({**result, "reason": f"Below the configured {minimum_ns:g} ns comparison floor"})
            else:
                comparisons.append(result)
    if count == 0 or not comparisons:
        raise ValueError("No comparable benchmark measurements")
    return comparisons, excluded


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("reports", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--excluded", required=True, type=Path)
    parser.add_argument("--minimum-ns", default=0.1, type=float)
    arguments = parser.parse_args()
    if not math.isfinite(arguments.minimum_ns) or arguments.minimum_ns < 0:
        parser.error("--minimum-ns must be finite and nonnegative")
    comparisons, excluded = collect(arguments.reports, arguments.minimum_ns)
    arguments.output.write_text(json.dumps(comparisons, indent=2) + "\n")
    arguments.excluded.write_text(json.dumps(excluded, indent=2) + "\n")
    print(f"Collected {len(comparisons)} unique comparisons; retained {len(excluded)} measurements below the comparison floor.")


if __name__ == "__main__":
    main()
