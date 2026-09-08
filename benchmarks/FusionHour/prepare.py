import argparse
import hashlib
from pathlib import Path
import subprocess


parser = argparse.ArgumentParser()
parser.add_argument("--batch", choices=["scalar", "carry", "runs"], default="carry")
parser.add_argument("--backend", choices=["tree", "dense"], default="tree")
arguments = parser.parse_args()
if arguments.backend == "dense" and arguments.batch != "carry":
    parser.error("the dense backend is supported only with --batch carry")
root = Path(__file__).resolve().parents[2]
project = root / "benchmarks/FusionHour"
generated = project / "Generated/FusedPulse.g.cs"
subprocess.run(
    ["taskset", "-c", "19", "dotnet", "run", "--project", "benchmarks/FusionGenerate", "-c", "Release", "-p:NuGetAudit=false", "--",
     "--batch", arguments.batch, "--backend", arguments.backend, "--output", str(generated)],
    cwd=root, check=True,
)
source_hash = hashlib.sha256(generated.read_bytes()).hexdigest()
(project / "BuildIdentity.cs").write_text(
    'namespace Tl.FusionExperiment;\n\npublic static class BuildIdentity\n{\n'
    f'    public const string SourceSha256 = "{source_hash}";\n}}\n'
)
generated.touch()
subprocess.run(
    ["taskset", "-c", "19", "dotnet", "build", str(project / "FusionHour.csproj"), "-c", "Release", "-t:Rebuild", "-p:NuGetAudit=false"],
    cwd=root, check=True,
)
verification = subprocess.run(
    ["taskset", "-c", "4", "dotnet", str(project / "bin/Release/net10.0/FusionHour.dll"), "--verify"],
    cwd=root, check=True, text=True, capture_output=True, timeout=30,
)
print(verification.stdout, end="")
if f"Generated source SHA-256: {source_hash}" not in verification.stdout:
    raise RuntimeError("The executed assembly does not identify the generated source just built.")
print(f"Prepared {arguments.backend}/{arguments.batch}: {source_hash}")
