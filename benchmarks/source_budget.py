from pathlib import Path
import subprocess


root = Path(__file__).resolve().parent.parent


def measure(tree, limit):
    paths = subprocess.check_output(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", tree],
        cwd=root,
    ).decode().split("\0")
    paths = sorted({path for path in paths if path and (root / path).is_file()})
    contents = sum((root / path).stat().st_size for path in paths)
    names = sum(len(path.encode()) + 1 for path in paths)
    total = contents + names
    print(f"{tree}: {len(paths)} files; {contents} content bytes + {names} path bytes = {total}/{limit} bytes")
    return total <= limit


ok = measure("src", 300_000)
ok &= measure("samples", 32_000)
raise SystemExit(0 if ok else 1)
