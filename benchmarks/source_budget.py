from pathlib import Path
import subprocess


root = Path(__file__).resolve().parent.parent


def budget(label, *scopes, limit):
    paths = subprocess.check_output(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", *scopes],
        cwd=root,
    ).decode().split("\0")
    paths = sorted({path for path in paths if path and (root / path).is_file()})
    contents = sum((root / path).stat().st_size for path in paths)
    names = sum(len(path.encode()) + 1 for path in paths)
    total = contents + names
    print(f"{label}: {len(paths)} files; {contents} content bytes + {names} path bytes = {total}/{limit} bytes")
    return total <= limit


ok = budget("src", "src", limit=173_215)
ok &= budget("samples", "samples", limit=32_000)
ok &= budget("shipped-tools", "tools/Tl.Gen.Tlb", "tools/Tl.Bake", limit=235_000)
raise SystemExit(0 if ok else 1)
