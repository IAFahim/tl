import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DECLARATION = re.compile(
    r"\b(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly|ref|unsafe)\s+)*"
    r"(?:class|struct|interface|enum|record)\s+(\w+)"
)
CORPUS_SUFFIXES = {
    ".cs", ".razor", ".js", ".json", ".md", ".py", ".yml", ".yaml",
    ".targets", ".props", ".csproj", ".slnx", ".txt", ".html",
}
SKIPPED_DIRECTORIES = {".git", "bin", "obj", "node_modules"}
OWNING_PROJECTS = ("src", "tools")
CLASSIFIED = {
    "ApplyMove": "Play.Core ITrack implementer discovered by the LiveAuthoring assembly.GetTypes() interface scan",
    "GetEnumerator": "permanently classified in the #300 audit",
    "G0Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G1Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G2Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G3Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G4Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G5Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G6Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G0Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G1Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G2Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G3Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G4Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G5Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "G6Clip": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "Tiny0Clip": "blend-law companion named by the assembled strings in tools/Tl.Bake.Bench/Corpus.cs",
    "Tiny1Clip": "blend-law companion named by the assembled strings in tools/Tl.Bake.Bench/Corpus.cs",
    "Tiny2Clip": "blend-law companion named by the assembled strings in tools/Tl.Bake.Bench/Corpus.cs",
    "Tiny0Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "Tiny1Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "Tiny2Track": "blend-law companion named by the assembled strings in benchmarks/Numbers/Domain.cs",
    "IsExternalInit": "compiler polyfill, permanently classified",
}


def corpus():
    files = {}
    for path in sorted(ROOT.rglob("*")):
        if not path.is_file() or path.suffix not in CORPUS_SUFFIXES:
            continue
        if any(part in SKIPPED_DIRECTORIES for part in path.relative_to(ROOT).parts):
            continue
        files[path] = path.read_text(encoding="utf-8")
    return files


def declared_types(files):
    declarations = {}
    for path, text in files.items():
        relative = path.relative_to(ROOT)
        if path.suffix != ".cs" or relative.parts[0] not in OWNING_PROJECTS:
            continue
        if any(".Tests" in part for part in relative.parts):
            continue
        for name in DECLARATION.findall(text):
            declarations.setdefault(name, set()).add(path)
    return declarations


def reference_counts(files, names):
    pattern = re.compile(r"\b(" + "|".join(sorted(re.escape(name) for name in names)) + r")\b")
    counts = {name: {} for name in names}
    for path, text in files.items():
        for match in pattern.finditer(text):
            occurrences = counts[match.group(1)]
            occurrences[path] = occurrences.get(path, 0) + 1
    return counts


class DeadCodeTests(unittest.TestCase):
    def test_declared_types_are_referenced_outside_their_declaration(self):
        files = corpus()
        declarations = declared_types(files)
        counts = reference_counts(files, declarations)
        orphaned = []
        for name, defining in sorted(declarations.items()):
            if name in CLASSIFIED:
                continue
            outside = any(path not in defining for path in counts[name])
            inside = sum(counts[name].get(path, 0) for path in defining)
            if not outside and inside <= len(defining):
                locations = ", ".join(sorted(str(path.relative_to(ROOT)) for path in defining))
                orphaned.append(f"{name} ({locations}) has zero references outside its declaration; classify it in CLASSIFIED with a reason or remove it")
        self.assertEqual([], orphaned)


if __name__ == "__main__":
    unittest.main()
