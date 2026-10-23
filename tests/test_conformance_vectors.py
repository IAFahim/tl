import hashlib
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FIXTURES = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "fixtures"
VECTORS = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "vectors"


def parse_vector(path):
    text = path.read_bytes().decode("ascii")
    if not text.endswith("\n"):
        raise AssertionError(f"{path.name}: file must end with a newline")
    lines = text.split("\n")
    if lines[-2].split(" ")[0] != "sha256" or len(lines[-2].split(" ")) != 2:
        raise AssertionError(f"{path.name}: last payload line must be 'sha256 <hex>'")
    body = "\n".join(lines[:-2]) + "\n"
    digest = hashlib.sha256(body.encode("ascii")).hexdigest()
    if lines[-2] != f"sha256 {digest}":
        raise AssertionError(f"{path.name}: sha256 trailer does not cover the payload")
    return body.split("\n")[:-1], digest


def parse_sections(lines, name):
    header = {}
    pairs = []
    pair = None
    lane = None
    for line in lines:
        fields = line.split(" ")
        if fields[0] in ("fixture", "stress", "tlbsha256", "duration", "looping", "pairs"):
            header[fields[0]] = " ".join(fields[1:])
        elif fields[0] == "pair":
            pair = {"lanes": [], "clock": []}
            pairs.append(pair)
        elif fields[0] in ("key", "fold", "lanes"):
            pair[{"key": "key", "fold": "fold", "lanes": "lanecount"}[fields[0]]] = fields[1]
        elif fields[0] == "lane":
            lane = {"ticks": int(fields[5])}
            pair["lanes"].append(lane)
        elif fields[0] in ("f", "b"):
            words = [int(word, 16) for word in fields[1:]]
            if len(words) != lane["ticks"]:
                raise AssertionError(f"{name}: {fields[0]} line holds {len(words)} words for {lane['ticks']} ticks")
            lane[fields[0]] = words
        elif fields[0] == "clock":
            pair["clock"].append(
                {
                    "forward": fields[1] == "f",
                    "position": int(fields[2]),
                    "active": fields[3] == "a1",
                    "tick": int(fields[4][1:]),
                    "next": int(fields[5][1:]),
                    "effect": int(fields[6][1:], 16),
                }
            )
        elif fields[0] != "tlcv1":
            raise AssertionError(f"{name}: unknown line '{line}'")
    return header, pairs


class ConformanceVectorTest(unittest.TestCase):
    def test_vector_directory_exists_with_files(self):
        names = sorted(path.stem for path in VECTORS.glob("*.tlcv"))
        self.assertTrue(names, "golden/vectors must hold committed conformance vectors")
        for required in ("minimal", "blended", "stress-blend", "stress-segmented", "stress-tiny"):
            self.assertIn(required, names)

    def test_every_vector_carries_a_valid_self_checksum(self):
        for path in VECTORS.glob("*.tlcv"):
            parse_vector(path)

    def test_every_committed_fixture_is_covered_by_a_vector(self):
        fixtures = {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in FIXTURES.glob("*.tlb")}
        referenced = {}
        for path in VECTORS.glob("*.tlcv"):
            lines, _ = parse_vector(path)
            header, _ = parse_sections(lines, path.name)
            kind = "fixture" if "fixture" in header else "stress"
            source = header.get(kind)
            self.assertIsNotNone(source, f"{path.name}: missing source line")
            if kind == "fixture":
                self.assertIn(source, fixtures, f"{path.name}: references unknown fixture {source}")
                self.assertEqual(fixtures[source], header["tlbsha256"], f"{path.name}: tlbsha256 does not match {source}")
                self.assertNotIn(source, referenced, f"{path.name}: fixture {source} is referenced twice")
                referenced[source] = path.name
        self.assertEqual(sorted(referenced), sorted(fixtures), "every committed .tlb fixture needs exactly one vector")

    def test_vector_structure_is_complete_and_self_consistent(self):
        for path in VECTORS.glob("*.tlcv"):
            lines, _ = parse_vector(path)
            header, pairs = parse_sections(lines, path.name)
            duration = int(header["duration"])
            keys = [int(pair["key"], 16) for pair in pairs]
            self.assertEqual(keys, sorted(keys), f"{path.name}: pair keys must ascend")
            self.assertEqual(len(pairs), int(header["pairs"]), f"{path.name}: pairs count mismatch")
            for pair in pairs:
                for direction in ("f", "b"):
                    entries = [entry for entry in pair["clock"] if entry["forward"] == (direction == "f")]
                    self.assertEqual(
                        [entry["position"] for entry in entries],
                        list(range(duration + 1)),
                        f"{path.name}: {direction} clock grid must cover every position",
                    )
                    for entry in entries:
                        if not entry["active"]:
                            self.assertEqual(entry["next"], entry["position"], f"{path.name}: idle clock must hold position")
                            self.assertEqual(entry["effect"], 0, f"{path.name}: idle clock must add nothing")
                            continue
                        self.assertLess(entry["tick"], duration, f"{path.name}: active tick out of range")
                        self.assertEqual(
                            entry["effect"],
                            pair["lanes"][0][direction][entry["tick"]],
                            f"{path.name}: clock effect must equal lane 0 at the tick",
                        )

    def test_at_least_one_vector_pins_a_segmented_bank(self):
        segmented = False
        for path in VECTORS.glob("*.tlcv"):
            lines, _ = parse_vector(path)
            header, _ = parse_sections(lines, path.name)
            if int(header["duration"]) >= 2049:
                segmented = True
        self.assertTrue(segmented, "a stress vector must cover the segmented bank tier (>= 2049 ticks)")


if __name__ == "__main__":
    unittest.main()
