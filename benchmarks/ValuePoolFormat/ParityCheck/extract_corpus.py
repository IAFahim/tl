import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parents[1] / "results" / "parity-corpus"

STANDALONE = [
    "samples/ManyEntities/move64.json",
    "samples/ManyEntities/pulse.json",
    "samples/ManyEntities/window.json",
    "samples/NuGetQuickStart/boss.json",
    "tests/tlb_cli/fixtures/alpha.json",
    "tests/tlb_cli/fixtures/alpha_edited.json",
    "tests/tlb_cli/fixtures/broken.json",
    "tools/Tl.Blender/fixtures/receipt_scene.json",
    "tools/Tl.Blender/fixtures/receipt_scene_hand.json",
    "tools/Tl.Playground/presets/finite-clamp.json",
    "tools/Tl.Playground/presets/staggered.json",
    "tools/Tl.Playground/presets/uniform-crowd.json",
    "tools/Tl.Playground/presets/waves.json",
]

EMBEDDED = [
    "tools/Tl.Bake.Tests/CacheCliTests.cs",
    "tools/Tl.Bake.Tests/CliTests.cs",
    "tools/Tl.Bake.Tests/DeterminismTests.cs",
    "tools/Tl.Bake.Tests/DiagnosticTests.cs",
    "tools/Tl.Bake.Tests/MetadataTests.cs",
    "tools/Tl.Bake.Tests/MultiPairBakeTests.cs",
    "tools/Tl.Bake.Tests/Recording.cs",
    "tools/Tl.Bake.Tests/WatchModeTests.cs",
    "tests/test_tl_blender.py",
]


def no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate field: '{key}'")
        result[key] = value
    return result


def parse_strict(text):
    return json.loads(text, object_pairs_hook=no_duplicate_keys)


def document_name(doc_id):
    return doc_id.replace("/", "_").replace("#", "_").replace("\\", "_") + ".json"


def collect():
    entries = []
    for rel in STANDALONE:
        raw = (ROOT / rel).read_text(encoding="utf-8")
        try:
            parsed = parse_strict(raw)
        except (ValueError, json.JSONDecodeError):
            entries.append((rel, None, raw))
            continue
        body = parsed.get("timeline") if isinstance(parsed.get("timeline"), dict) else parsed
        entries.append((rel, body, None))
    for rel in EMBEDDED:
        raw = (ROOT / rel).read_text(encoding="utf-8")
        for match in re.finditer(r'"""(.*?)"""', raw, re.S):
            body_text = match.group(1)
            if '"tracks"' not in body_text or "{{" in body_text:
                continue
            line = raw.count("\n", 0, match.start()) + 1
            doc_id = f"{rel}#L{line}"
            try:
                body = parse_strict(body_text)
            except (ValueError, json.JSONDecodeError):
                continue
            entries.append((doc_id, body, None))
    return entries


def main():
    entries = collect()
    entries.sort(key=lambda item: item[0])
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = []
    for source, body, raw in entries:
        name = document_name(source)
        text = raw if raw is not None else json.dumps(body, ensure_ascii=False, indent=2) + "\n"
        (OUT / name).write_text(text, encoding="utf-8", newline="\n")
        manifest.append({"name": name, "source": source})
    (OUT / "manifest.json").write_text(json.dumps({"documents": manifest}, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"{len(manifest)} documents")
    return 0


if __name__ == "__main__":
    sys.exit(main())
