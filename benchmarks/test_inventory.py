import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BENCHMARKS = ROOT / "benchmarks"
README = (BENCHMARKS / "README.md").read_text(encoding="utf-8")
LIVE_README = README.split("## Historical harness projects", 1)[0]
INDEX_SOURCES = {
    "tl.slnx": (ROOT / "tl.slnx").read_text(encoding="utf-8"),
    "ci.yml": (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8"),
    "benchmark.yml": (ROOT / ".github" / "workflows" / "benchmark.yml").read_text(encoding="utf-8"),
    "benchmarks/README.md": README,
}
PROJECT_WAIVERS = {}
REPRODUCTION_WAIVERS = {
    "ConsumerFusion": "reproduction chain references the removed ConsumerGenerate generator and a missing project; owner decision pending (#300 3.2)",
}
REPRODUCTION_HEADINGS = ("## Reproduction", "## Repro")
RETIRED_TOKENS = ("Step", "Execute")
PATH_SUFFIXES = (".csproj", ".cs", ".py", ".json", ".md", ".sh", ".txt", ".csv", ".slnx")
FENCE = re.compile(r"^```\w*\n(.*?)^```", re.MULTILINE | re.DOTALL)
STRING_LITERAL = re.compile(r"\"([^\"]+)\"|'([^']+)'")
SCENARIO_LABEL = re.compile(r'new\(\s*"([^"]+)"')
PATH_TOKEN = re.compile(r"[\w./-]+")


def is_pathlike(token):
    return PATH_TOKEN.fullmatch(token) is not None and ("/" in token or token.endswith(PATH_SUFFIXES))


def is_build_output(token):
    parts = token.split("/")
    return "bin" in parts or "obj" in parts


def resolve(token, *bases):
    for base in bases:
        if (base / token).exists():
            return base / token
    return None


def reproduction_blocks():
    blocks = []
    for readme in sorted(BENCHMARKS.glob("*/README.md")):
        text = readme.read_text(encoding="utf-8")
        for heading in REPRODUCTION_HEADINGS:
            parts = text.split(heading, 1)
            if len(parts) != 2:
                continue
            section = parts[1].split("\n## ", 1)[0]
            for block in FENCE.findall(section):
                blocks.append((readme.parent, block))
    return blocks


def reproduction_findings():
    findings = []
    for probe, block in reproduction_blocks():
        scripts = []
        for raw in block.split():
            token = raw.strip("\"'")
            if token.startswith("-") or not is_pathlike(token) or is_build_output(token):
                continue
            resolved = resolve(token, ROOT, probe)
            if resolved is None:
                findings.append((probe.name, f"{token} referenced by {probe.name}/README.md does not exist"))
                continue
            if resolved.suffix == ".py":
                scripts.append(resolved)
        for script in scripts:
            for alternative, second in STRING_LITERAL.findall(script.read_text(encoding="utf-8")):
                token = alternative or second
                if not is_pathlike(token) or is_build_output(token):
                    continue
                if resolve(token, ROOT, probe, script.parent) is None:
                    findings.append((probe.name, f"{token} referenced by {script.relative_to(ROOT)} does not exist"))
    return findings


class BenchmarkInventoryTests(unittest.TestCase):
    def test_every_benchmark_project_is_indexed(self):
        for csproj in sorted(BENCHMARKS.glob("*/*.csproj")):
            name = csproj.parent.name
            if name in PROJECT_WAIVERS:
                continue
            self.assertTrue(
                any(name in text for text in INDEX_SOURCES.values()),
                f"{name} is not named by tl.slnx, ci.yml, benchmark.yml, or benchmarks/README.md",
            )

    def test_every_readme_project_reference_exists(self):
        missing = []
        for token in re.findall(r"`([^`\n]+)`", LIVE_README):
            token = token.strip().rstrip("/")
            if token.startswith("--") or " " in token or not is_pathlike(token):
                continue
            if resolve(token, ROOT, BENCHMARKS) is None:
                missing.append(f"{token} named by benchmarks/README.md does not exist")
        for name in re.findall(r"^(?:- |\| )`([\w-]+)`", LIVE_README, re.MULTILINE):
            if not (BENCHMARKS / name).is_dir():
                missing.append(f"{name} named by benchmarks/README.md does not exist")
        self.assertEqual([], missing)

    def test_probe_reproduction_targets_exist(self):
        unwaived = [
            f"{probe}: {detail}"
            for probe, detail in reproduction_findings()
            if probe not in REPRODUCTION_WAIVERS
        ]
        self.assertEqual([], unwaived)

    def test_numbers_scenario_labels_avoid_retired_api_tokens(self):
        labels = SCENARIO_LABEL.findall((BENCHMARKS / "Numbers" / "Program.cs").read_text(encoding="utf-8"))
        self.assertTrue(labels, "scenario labels were not found in benchmarks/Numbers/Program.cs")
        for label in labels:
            for token in RETIRED_TOKENS:
                self.assertNotIn(token, label, f"scenario label '{label}' contains retired API token '{token}'")

    def test_waivers_still_own_a_finding(self):
        reproduced = {probe for probe, _ in reproduction_findings()}
        for probe in REPRODUCTION_WAIVERS:
            self.assertIn(probe, reproduced, f"the {probe} waiver owns no finding and should be dropped")
        for project in PROJECT_WAIVERS:
            self.assertTrue((BENCHMARKS / project).is_dir(), f"the {project} waiver owns no directory and should be dropped")


if __name__ == "__main__":
    unittest.main()
