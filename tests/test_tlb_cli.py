import hashlib
import json
import subprocess
import tempfile
import threading
import time
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CONFIG_PATH = ROOT / "tests" / "tlb_cli" / "config.json"
CONFIG = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
CLI_DLL = ROOT / "tools" / "Tl.Bake" / "bin" / "Release" / "net10.0" / "Tl.Bake.dll"
TEST_ASSEMBLY = ROOT / "tools" / "Tl.Bake.Tests" / "bin" / "Release" / "net10.0" / "Tl.Bake.Tests.dll"


def _build_if_missing(project: Path, artifact: Path) -> None:
    if artifact.exists():
        return
    subprocess.run(
        ["dotnet", "build", str(project), "-c", "Release", "-p:NuGetAudit=false"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )


def _prepare_binaries() -> None:
    _build_if_missing(ROOT / "tools" / "Tl.Bake" / "Tl.Bake.csproj", CLI_DLL)
    _build_if_missing(ROOT / "tools" / "Tl.Bake.Tests" / "Tl.Bake.Tests.csproj", TEST_ASSEMBLY)


def _resolve_path(value: str, out_dir: Path) -> str:
    tokens = {"$configDir": str(CONFIG_PATH.parent), "$root": str(ROOT), "$testAssembly": str(TEST_ASSEMBLY), "$out": str(out_dir)}
    for token, resolved in tokens.items():
        if value.startswith(token):
            return value.replace(token, resolved, 1)
    raise AssertionError("value %r must start with a known path token" % value)


def _assemble_options(options: dict, out_dir: Path) -> list:
    argv = []
    for assembly in options.get("assemblies", []):
        argv += ["--assembly", _resolve_path(assembly, out_dir)]
    if "cache" in options:
        argv += ["--cache", _resolve_path(options["cache"], out_dir)]
    return argv


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _run_bake_like_scenario(scenario: dict, out_dir: Path) -> None:
    mode = scenario["mode"]
    options = scenario.get("options", {})
    expected = scenario["expected"]

    def base_argv() -> list:
        if mode == "json":
            return ["--json", *_assemble_options(options, out_dir)]
        if mode == "report":
            return ["--report", _resolve_path(scenario["inputs"]["tlb"], out_dir)]
        if mode == "strip":
            return [
                "--strip",
                _resolve_path(scenario["inputs"]["tlb"], out_dir),
                _resolve_path(expected["output"]["path"], out_dir),
            ]
        assert mode == "bake", "unsupported mode %s" % mode
        output = expected.get("output", {}).get("path", "$out/unused.tlb")
        return [
            _resolve_path(scenario["inputs"]["json"], out_dir),
            _resolve_path(output, out_dir),
            *_assemble_options(options, out_dir),
        ]

    stdout_all = ""
    stderr_all = ""
    for index, run in enumerate(expected.get("runs", [{}])):
        completed = subprocess.run(
            ["dotnet", str(CLI_DLL), *base_argv()],
            cwd=ROOT,
            capture_output=True,
            text=True,
            timeout=options.get("timeoutSeconds", 120),
        )
        assert completed.returncode == expected["exitCode"], (
            "run %d exited %d (expected %d): %s"
            % (index, completed.returncode, expected["exitCode"], completed.stderr)
        )
        stdout_all += completed.stdout
        stderr_all += completed.stderr
        for needle in run.get("stdoutContains", []):
            assert needle in completed.stdout, "run %d stdout missing %r" % (index, needle)

    for needle in expected.get("stdoutContains", []):
        assert needle in stdout_all, "stdout missing %r" % needle
    for needle in expected.get("stderrContains", []):
        assert needle in stderr_all, "stderr missing %r" % needle
    if "stdoutGolden" in expected:
        golden = Path(_resolve_path(expected["stdoutGolden"], out_dir)).read_bytes()
        assert stdout_all.encode("utf-8") == golden, "stdout does not match golden %s" % expected["stdoutGolden"]
    for absent in expected.get("absentOutputs", []):
        assert not Path(_resolve_path(absent, out_dir)).exists(), "unexpected output %s" % absent
    if "output" in expected:
        produced = Path(_resolve_path(expected["output"]["path"], out_dir))
        assert produced.exists(), "missing output %s" % produced
        assert _sha256(produced) == expected["output"]["sha256"], "sha256 mismatch for %s" % produced


def _run_watch_scenario(scenario: dict, out_dir: Path) -> None:
    options = scenario.get("options", {})
    expected = scenario["expected"]
    timeout = options.get("timeoutSeconds", 60)
    wanted = expected["events"]
    edits = scenario["inputs"]["edits"]
    assert len(wanted) >= len(edits) + 2, "watch events must cover ready, the initial bake, and every edit"

    watch_dir = out_dir / "watch"
    watch_dir.mkdir(exist_ok=True)
    watched = watch_dir / "alpha.json"
    watched.write_bytes(Path(_resolve_path(scenario["inputs"]["initial"], out_dir)).read_bytes())
    argv = [
        "dotnet",
        str(CLI_DLL),
        "--watch",
        str(watched),
        str(watch_dir / "alpha.tlb"),
        *_assemble_options(options, out_dir),
        "--debounce",
        str(options.get("debounceMs", 100)),
    ]
    process = subprocess.Popen(argv, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    events = []
    lock = threading.Lock()

    def pump():
        for line in process.stdout:
            line = line.strip()
            if not line:
                continue
            document = json.loads(line)
            assert document["schemaVersion"] == 1, "event schemaVersion must be 1"
            with lock:
                events.append(document["event"])

    reader = threading.Thread(target=pump, daemon=True)
    reader.start()

    def wait_for_events(count: int) -> list:
        deadline = time.monotonic() + timeout
        while True:
            with lock:
                current = list(events)
            assert len(current) >= count or time.monotonic() < deadline, (
                "timed out waiting for %d events, have %s" % (count, current)
            )
            if len(current) >= count:
                return current
            assert current == wanted[: len(current)], "event drift: %s" % current
            time.sleep(0.02)

    try:
        current = wait_for_events(1)
        assert current[0] == "ready", "first event must be ready, got %s" % current
        settle = max(0.3, options.get("debounceMs", 100) * 4 / 1000.0)
        for index, edit in enumerate(edits):
            time.sleep(settle)
            watched.write_bytes(Path(_resolve_path(edit, out_dir)).read_bytes())
            current = wait_for_events(index + 3)
            assert current == wanted[: index + 3], "events after edit %d: %s" % (index, current)
        with lock:
            assert events == wanted, "final events %s != expected %s" % (events, wanted)
    finally:
        process.terminate()
        process.wait(timeout=30)
        if process.stdout:
            process.stdout.close()


class TestTlBakeCli(unittest.TestCase):
    out_dir = None

    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix="tlb_cli_")
        cls.out_dir = Path(cls.temporary.name)
        _prepare_binaries()

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def test_config_schema_version(self):
        self.assertEqual(CONFIG["schemaVersion"], 1)

    def test_config_scenario_names_are_unique(self):
        names = [scenario["name"] for scenario in CONFIG["scenarios"]]
        self.assertEqual(len(names), len(set(names)))

    def test_config_scenarios(self):
        for scenario in CONFIG["scenarios"]:
            with self.subTest(scenario=scenario["name"]):
                if scenario["mode"] == "watch":
                    _run_watch_scenario(scenario, self.out_dir)
                else:
                    _run_bake_like_scenario(scenario, self.out_dir)


if __name__ == "__main__":
    unittest.main()
