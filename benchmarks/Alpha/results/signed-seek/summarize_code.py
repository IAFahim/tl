import csv
import re
from pathlib import Path


ROOT = Path(__file__).parent
LISTING = re.compile(r"^; Assembly listing for method (.+) \(FullOpts\)$")
SIZE = re.compile(r"^; Total bytes of code (\d+)$")


rows = []
for path in sorted((ROOT / "disassembly").glob("*.asm")):
    method = None
    for line in path.read_text(encoding="utf-8").splitlines():
        listing = LISTING.match(line)
        if listing:
            method = listing.group(1)
            continue
        size = SIZE.match(line)
        if size and method:
            rows.append((path.name, method, int(size.group(1))))
            method = None

with (ROOT / "code-size.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream, lineterminator="\n")
    writer.writerow(("listing", "method", "fullopts-code-bytes"))
    writer.writerows(rows)
