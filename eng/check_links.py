import re
import urllib.error
import urllib.request
from pathlib import Path


LINK = re.compile(r"\[[^\]]*\]\(\s*([^)\s]+)(?:\s+\"[^\"]*\")?\s*\)")
HEADING = re.compile(r"^#{1,6}\s+(.+?)\s*#*\s*$", re.MULTILINE)
EXTERNAL = re.compile(r"^https?://")
ALLOWED_PREFIXES = (
    "https://github.com/IAFahim/",
    "https://api.nuget.org/",
    "https://www.nuget.org/",
    "https://nuget.org/",
    "https://iafahim.github.io/",
)
WAIVED_EXTERNAL = {
    "https://github.com/IAFahim/tl.unity": "private tl.unity repository; owner decision pending (#300 1.1)",
}


def markdown_files(root):
    root = Path(root)
    candidates = [
        root / "README.md",
        root / "AGENTS.md",
        root / "SECURITY.md",
        root / "benchmarks" / "README.md",
        *sorted((root / "benchmarks").glob("*/README.md")),
        *sorted((root / "tools").glob("*/README.md")),
        *sorted((root / "tools").glob("*/pack-readme.md")),
        *sorted((root / ".github").rglob("*.md")),
    ]
    return [path for path in candidates if path.is_file() and "results" not in path.parts]


def github_slug(text):
    text = re.sub(r"[^\w\- ]", "", text.strip().lower())
    return text.replace(" ", "-")


def anchors(text):
    return {github_slug(heading) for heading in HEADING.findall(text)}


def external_targets(root):
    targets = set()
    for path in markdown_files(root):
        for link in LINK.findall(path.read_text(encoding="utf-8")):
            if EXTERNAL.match(link) and link not in WAIVED_EXTERNAL:
                targets.add(link)
    return sorted(targets)


def broken_links(root):
    root = Path(root)
    problems = []
    for path in markdown_files(root):
        text = path.read_text(encoding="utf-8")
        page_anchors = anchors(text)
        for link in LINK.findall(text):
            location = f"{path.relative_to(root)}: {link}"
            if link.startswith("#"):
                if github_slug(link[1:]) not in page_anchors:
                    problems.append(f"{location} (anchor does not resolve)")
                continue
            if link.startswith("mailto:"):
                continue
            if EXTERNAL.match(link):
                if link in WAIVED_EXTERNAL:
                    continue
                if not any(link.startswith(prefix) for prefix in ALLOWED_PREFIXES):
                    problems.append(f"{location} (unclassified external host)")
                continue
            fragment = ""
            target = link
            if "#" in link:
                target, _, fragment = link.partition("#")
            resolved = (path.parent / target).resolve()
            if not resolved.exists():
                problems.append(f"{location} (missing relative target)")
            elif fragment and resolved.suffix == ".md":
                if github_slug(fragment) not in anchors(resolved.read_text(encoding="utf-8")):
                    problems.append(f"{location} (anchor does not resolve)")
    return problems


def dead_external_targets(targets, timeout=20):
    dead = []
    for url in targets:
        request = urllib.request.Request(url, method="HEAD", headers={"User-Agent": "tl-link-check"})
        try:
            with urllib.request.urlopen(request, timeout=timeout) as response:
                status = response.status
        except urllib.error.HTTPError as error:
            status = error.code
        except urllib.error.URLError:
            dead.append(f"{url} unreachable")
            continue
        if status in (404, 410):
            dead.append(f"{url} returned {status}")
    return dead
