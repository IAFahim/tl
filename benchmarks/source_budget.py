from pathlib import Path
import subprocess


root = Path(__file__).resolve().parent.parent
paths = subprocess.check_output(
    ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "src"],
    cwd=root,
).decode().split("\0")
paths = sorted({path for path in paths if path and (root / path).is_file()})
contents = sum((root / path).stat().st_size for path in paths)
names = sum(len(path.encode()) + 1 for path in paths)
total = contents + names
limit = 300_000
print(f"src: {len(paths)} files; {contents} content bytes + {names} path bytes = {total}/{limit} bytes")
raise SystemExit(0 if total <= limit else 1)
