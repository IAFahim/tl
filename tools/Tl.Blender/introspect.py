import json
import os
import shlex
import subprocess
from dataclasses import dataclass
from typing import Any, Callable


@dataclass(frozen=True)
class Pair:
    track_namespace: str
    track_name: str
    clip_namespace: str
    clip_name: str
    blendable: bool
    unmanaged: bool


class IntrospectionError(Exception):
    pass


def introspect_argv(command: str, assembly_paths: tuple[str, ...]) -> list[str]:
    argv = shlex.split(command, posix=os.name != "nt")
    if not argv:
        raise IntrospectionError("bake command preference is empty")
    return [*argv, "--json", *(arg for path in assembly_paths for arg in ("--assembly", path))]


def parse_pairs(document: dict[str, Any]) -> tuple[Pair, ...]:
    if document.get("schemaVersion") != 1:
        raise IntrospectionError("unsupported introspection schemaVersion %r" % (document.get("schemaVersion"),))
    pairs = []
    for entry in document.get("pairs", []):
        track = entry["track"]
        clip = entry["clip"]
        pairs.append(
            Pair(
                track_namespace=track["namespace"],
                track_name=track["name"],
                clip_namespace=clip["namespace"],
                clip_name=clip["name"],
                blendable=bool(entry["blendable"]),
                unmanaged=bool(entry["unmanaged"]),
            )
        )
    return tuple(pairs)


def run_introspection(
    command: str,
    assembly_paths: tuple[str, ...],
    runner: Callable[..., Any] | None = None,
) -> tuple[tuple[Pair, ...], str]:
    if not assembly_paths:
        return (), ""
    execute = runner if runner is not None else _default_runner
    try:
        argv = introspect_argv(command, assembly_paths)
    except IntrospectionError as error:
        return (), str(error)
    try:
        completed = execute(argv)
    except OSError as error:
        return (), "tlbake --json failed to start: %s" % error
    if completed.returncode != 0:
        stderr = (getattr(completed, "stderr", "") or "").strip()
        return (), "tlbake --json exited %d: %s" % (completed.returncode, stderr or "no diagnostics")
    try:
        document = json.loads(completed.stdout)
    except json.JSONDecodeError as error:
        return (), "tlbake --json printed invalid JSON: %s" % error
    try:
        return parse_pairs(document), ""
    except IntrospectionError as error:
        return (), str(error)
    except (AttributeError, KeyError, TypeError) as error:
        return (), "tlbake --json document is malformed: %s" % error


def _default_runner(argv: list[str]) -> Any:
    return subprocess.run(argv, capture_output=True, text=True, check=False)
