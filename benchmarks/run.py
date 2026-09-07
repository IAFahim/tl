import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess


def sources(root):
    return {
        str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest()
        for p in sorted((root / "benchmarks").rglob("*"))
        if p.suffix in [".cs", ".csproj", ".py"]
        and not any(x in p.parts for x in ["obj", "bin", "results"])
    }


def validate(result):
    log = (result / "run.log").read_text()
    found = re.search(r"Found (\d+) benchmark\(s\) in total", log)
    completed = re.search(r"Global total time:.*executed benchmarks: (\d+)", log)
    if not found or not completed or found[1] != completed[1] or int(found[1]) == 0:
        raise RuntimeError(f"Incomplete benchmark run: {result}")
    rows = []
    for report in sorted((result / "bdn" / "results").glob("*-report-full.json")):
        rows.extend(json.loads(report.read_text())["Benchmarks"])
    if len(rows) != int(found[1]):
        raise RuntimeError(f"Expected {found[1]} measured cases, found {len(rows)}: {result}")
    for row in rows:
        stats = row.get("Statistics")
        if not stats or stats.get("N", 0) == 0 or not math.isfinite(stats["Mean"]) or stats["Mean"] < 0:
            raise RuntimeError(f"Missing measurements: {row.get('DisplayInfo', row)}")
    return len(rows)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("group", choices=["dispatch", "algorithms", "review", "all"])
    parser.add_argument("--cpu", default="4")
    parser.add_argument("--filter", nargs="+", default=["*"])
    parser.add_argument("--label", default=datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ"))
    args = parser.parse_args()
    if args.label in ["", ".", ".."] or Path(args.label).name != args.label:
        parser.error("--label must be a single directory name")
    root = Path(__file__).resolve().parent.parent
    groups = ["dispatch", "algorithms", "review"] if args.group == "all" else [args.group]

    def capture(command):
        run = subprocess.run(command, cwd=root, text=True, capture_output=True)
        return run.stdout.strip() if run.returncode == 0 else None

    for group in groups:
        project = {"dispatch": "Dispatch", "algorithms": "Algorithms", "review": "Review"}[group]
        result = root / "benchmarks" / project / "results" / args.label
        result.mkdir(parents=True, exist_ok=False)
        project_file = root / "benchmarks" / project / f"{project}.csproj"
        subprocess.run(["dotnet", "build", str(project_file), "-c", "Release", "-v", "q", "-clp:ErrorsOnly"], cwd=root, check=True)
        executable = project_file.parent / "bin" / "Release" / "net10.0" / f"{project}.dll"
        subprocess.run(["dotnet", str(executable), "--verify"], cwd=root, check=True)
        metadata = {
            "utc": datetime.now(timezone.utc).isoformat(),
            "cpu_affinity": args.cpu,
            "dotnet": capture(["dotnet", "--info"]),
            "cpu": capture(["lscpu"]),
            "commit": capture(["git", "rev-parse", "HEAD"]),
            "status": capture(["git", "status", "--short"]),
            "waffle_commit": capture(["git", "-C", str(root.parent / "Waffle"), "rev-parse", "HEAD"]),
            "waffle_status": capture(["git", "-C", str(root.parent / "Waffle"), "status", "--short"]),
            "environment": {key: os.environ.get(key) for key in ["DOTNET_TieredCompilation", "DOTNET_TieredPGO", "NuGetAudit"]},
            "sources_sha256": sources(root),
            "validated": False,
        }
        command = ["taskset", "-c", args.cpu, "dotnet", str(executable), "--filter", *args.filter, "--artifacts", str(result / "bdn")]
        metadata["command"] = command
        metadata_file = result / "environment.json"
        metadata_file.write_text(json.dumps(metadata, indent=2))
        print(f"Starting {group}: {result}", flush=True)
        with (result / "run.log").open("w") as log:
            run = subprocess.run(command, cwd=root, stdout=log, stderr=subprocess.STDOUT)
        metadata["exit_code"] = run.returncode
        metadata_file.write_text(json.dumps(metadata, indent=2))
        run.check_returncode()
        metadata["cases"] = validate(result)
        if sources(root) != metadata["sources_sha256"]:
            raise RuntimeError("Benchmark sources changed during measurement; rerun before trusting this result.")
        metadata["validated"] = True
        metadata_file.write_text(json.dumps(metadata, indent=2))
        print(f"Finished {group}: {metadata['cases']} measured cases verified", flush=True)


if __name__ == "__main__":
    main()
