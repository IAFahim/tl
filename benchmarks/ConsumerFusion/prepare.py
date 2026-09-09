import argparse
import hashlib
from pathlib import Path
import subprocess


parser = argparse.ArgumentParser()
parser.add_argument("--build-cpu", default="19")
parser.add_argument("--verify-cpu", default="4")
parser.add_argument("--inline-batch", action="store_true")
arguments = parser.parse_args()
root = Path(__file__).resolve().parents[2]
project = root / "benchmarks/ConsumerFusion"
generated = project / "Generated/FusedPulse.g.cs"
subprocess.run(
    ["taskset", "-c", arguments.build_cpu, "dotnet", "run", "--project", "benchmarks/ConsumerGenerate", "-c", "Release", "-p:NuGetAudit=false", "--", "--output", str(generated), *(["--inline-batch"] if arguments.inline_batch else [])],
    cwd=root, check=True,
)
source_hash = hashlib.sha256(generated.read_bytes()).hexdigest()
consumer_hash = hashlib.sha256()
for name in ["Contracts.cs", "SumConsumer.cs", "StateConsumer.cs", "EffectConsumer.cs"]:
    consumer_hash.update(name.encode())
    consumer_hash.update((project / name).read_bytes())
(project / "BuildIdentity.cs").write_text(
    'namespace Tl.ConsumerFusion;\n\npublic static class BuildIdentity\n{\n'
    f'    public const string SourceSha256 = "{source_hash}";\n'
    f'    public const string ConsumerSha256 = "{consumer_hash.hexdigest()}";\n}}\n'
)
generated.touch()
subprocess.run(
    ["taskset", "-c", arguments.build_cpu, "dotnet", "build", str(project / "ConsumerFusion.csproj"), "-c", "Release", "-t:Rebuild", "-p:NuGetAudit=false"],
    cwd=root, check=True,
)
verification = subprocess.run(
    ["taskset", "-c", arguments.verify_cpu, "dotnet", str(project / "bin/Release/net10.0/ConsumerFusion.dll"), "--verify"],
    cwd=root, check=True, text=True, capture_output=True, timeout=120,
)
print(verification.stdout, end="")
if f"Generated source SHA-256: {source_hash}" not in verification.stdout:
    raise RuntimeError("The executing assembly does not identify the source just generated.")
