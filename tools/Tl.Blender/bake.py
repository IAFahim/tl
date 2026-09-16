import os
import shlex
import subprocess
from typing import Any, Callable

SEVERITY_INFO = "INFO"
SEVERITY_ERROR = "ERROR"


class BakeError(Exception):
    pass


def bake_argv(command: str, json_path: str, tlb_path: str, assembly_paths: tuple[str, ...]) -> list[str]:
    argv = shlex.split(command, posix=os.name != "nt")
    if not argv:
        raise BakeError("bake command preference is empty")
    return [*argv, json_path, tlb_path, *(arg for path in assembly_paths for arg in ("--assembly", path))]


def run_bake(argv: list[str], runner: Callable[..., Any] | None = None) -> dict[str, Any]:
    execute = runner if runner is not None else _default_runner
    try:
        completed = execute(argv)
    except OSError as error:
        raise BakeError("bake command '%s' failed to start: %s" % (_join(argv), error)) from error
    return {
        "command": list(argv),
        "exit_code": completed.returncode,
        "stdout": getattr(completed, "stdout", "") or "",
        "stderr": getattr(completed, "stderr", "") or "",
    }


def report_lines(result: dict[str, Any]) -> list[tuple[str, str]]:
    lines: list[tuple[str, str]] = [(SEVERITY_INFO, "bake: %s" % _join(result["command"]))]
    for line in result["stdout"].splitlines():
        lines.append((SEVERITY_INFO, "bake: %s" % line))
    for line in result["stderr"].splitlines():
        lines.append((SEVERITY_ERROR, "bake: %s" % line))
    lines.append(
        (SEVERITY_INFO if result["exit_code"] == 0 else SEVERITY_ERROR, "bake: exit %d" % result["exit_code"])
    )
    return lines


def succeeded(result: dict[str, Any]) -> bool:
    return result["exit_code"] == 0


def _default_runner(argv: list[str]) -> Any:
    return subprocess.run(argv, capture_output=True, text=True, check=False)


def _join(argv: list[str]) -> str:
    return shlex.join(argv)
