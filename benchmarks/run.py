import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
from datetime import datetime, timezone

parser = argparse.ArgumentParser()
parser.add_argument("group", choices=["dispatch", "algorithms", "all"])
parser.add_argument("--cpu", default="4")
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
groups = ["dispatch", "algorithms"] if args.group == "all" else [args.group]

for group in groups:
    project = "Dispatch" if group == "dispatch" else "Algorithms"
    result = root / "benchmarks" / project / "results" / "validated"
    result.mkdir(parents=True, exist_ok=True)
    sources = {}
    for p in sorted((root / "benchmarks").rglob("*")):
        if p.suffix not in [".cs", ".csproj", ".py"] or any(x in p.parts for x in ["obj", "bin", "results"]):
            continue
        sources[str(p.relative_to(root))] = hashlib.sha256(p.read_bytes()).hexdigest()

    def capture(command):
        return subprocess.run(command, cwd=root, text=True, capture_output=True).stdout

    metadata = {
        "utc": datetime.now(timezone.utc).isoformat(),
        "cpu_affinity": args.cpu,
        "dotnet": capture(["dotnet", "--info"]),
        "cpu": capture(["lscpu"]),
        "waffle_commit": capture(["git", "-C", str(root.parent / "Waffle"), "rev-parse", "HEAD"]).strip(),
        "waffle_status": capture(["git", "-C", str(root.parent / "Waffle"), "status", "--short"]),
        "environment": {key: os.environ.get(key) for key in ["DOTNET_TieredCompilation", "DOTNET_TieredPGO"]},
        "sources_sha256": sources,
    }
    (result / "environment.json").write_text(json.dumps(metadata, indent=2))
    executable = root / "benchmarks" / project / "bin" / "Release" / "net10.0" / f"{project}.dll"
    command = ["taskset", "-c", args.cpu, "dotnet", str(executable), "--filter", "*", "--artifacts", str(result / "bdn")]
    print(f"Starting {group}: {result}", flush=True)
    with (result / "run.log").open("w") as log:
        completed = subprocess.run(command, cwd=root, stdout=log, stderr=subprocess.STDOUT)
    if completed.returncode != 0:
        print(f"WARNING: {group} exited with code {completed.returncode}; BenchmarkDotNet "
              "sometimes aborts during process exit after all artifacts are written — "
              f"verify the tables in {result / 'bdn' / 'results'} before trusting them.", flush=True)
    print(f"Finished {group}", flush=True)
