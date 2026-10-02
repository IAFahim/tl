import hashlib
import json
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
VIEWER = ROOT / "tools" / "Tl.Playground" / "Playground" / "wwwroot" / "viewer"
GOLDEN_INSPECT = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "inspect.json"
GOLDEN_FIXTURES = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "fixtures"
LIVE_MIXED_SHA256 = "6e79ce894cbee7062c0a4a00dde2d2686da7fe9ed1e865fecc484f4c98931814"


class ViewerTests(unittest.TestCase):
    def test_viewer_page_exists_and_is_self_contained(self):
        page = (VIEWER / "index.html").read_text(encoding="utf-8")
        self.assertNotIn("<script src=", page)
        self.assertNotIn("<link ", page)
        for marker in (
            "schemaVersion",
            "folded-now",
            "addresses",
            "structural delta",
            "TLB1 container",
            "engine frame state",
            "Pipeline",
        ):
            self.assertIn(marker, page)

    @unittest.skipUnless(
        GOLDEN_FIXTURES.is_dir(),
        "golden .tlb fixtures ship with the tlb1-layout-spec workstream (PR #421); this guard activates once both merge",
    )
    def test_computed_tlb1_layout_matches_golden_fixture_bytes(self):
        for name in ("minimal", "blended"):
            with self.subTest(fixture=name):
                doc = json.loads((GOLDEN_FIXTURES / f"{name}.inspect.json").read_text(encoding="utf-8"))
                blob = (GOLDEN_FIXTURES / f"{name}.tlb").read_bytes()
                magic, version = struct.unpack_from("<II", blob, 0)
                self.assertEqual(magic, 0x31424C54)
                self.assertEqual(version, 4)
                loops, duration, _tracks, stage_count, pair_count = struct.unpack_from("<IIIII", blob, 8)
                pair_off, stage_off, pool_off, frame_off, hot_len, total = struct.unpack_from("<IIIIII", blob, 28)

                self.assertEqual(duration, doc["header"]["duration"])
                self.assertEqual(loops, 1 if doc["header"]["looping"] else 0)
                self.assertEqual(stage_count, len(doc["stages"]))
                self.assertEqual(pair_count, len(doc["pairs"]))

                steps = sum(len(s["steps"]) for s in doc["stages"])
                self.assertEqual(pair_off, 64)
                self.assertEqual(stage_off, 64 + 48 * pair_count)
                steps_end = stage_off + 16 * stage_count + 8 * steps
                self.assertEqual(pool_off, (steps_end + 15) & ~15)

                cursor = pool_off
                for i, pair in enumerate(doc["pairs"]):
                    entry = 64 + 48 * i
                    rel_track = struct.unpack_from("<I", blob, entry + 12)[0]
                    rel_clip = struct.unpack_from("<I", blob, entry + 24)[0]
                    track_start = cursor
                    clip_start = (track_start + pair["pools"]["trackValueBytes"] + 15) & ~15
                    clip_end = clip_start + pair["pools"]["clipValueBytes"]
                    self.assertEqual(entry + rel_track, track_start)
                    self.assertEqual(entry + rel_clip, clip_start)
                    cursor = (clip_end + 15) & ~15
                self.assertEqual(frame_off, cursor)
                self.assertEqual(hot_len, frame_off + 24 * steps)
                self.assertLessEqual(hot_len, total)

                expected_program = stage_off + 16 * stage_count
                expected_slot = frame_off
                for i, stage in enumerate(doc["stages"]):
                    prog_off, prog_count = struct.unpack_from("<II", blob, stage_off + 16 * i + 8)
                    self.assertEqual(prog_off, expected_program)
                    self.assertEqual(prog_count, len(stage["steps"]))
                    for step in stage["steps"]:
                        slot_addr, step_pair = struct.unpack_from("<II", blob, expected_program)
                        self.assertEqual(slot_addr, expected_slot)
                        self.assertEqual(step_pair, step["pair"])
                        expected_program += 8
                        expected_slot += 24

    def test_bundled_static_example_is_the_committed_inspect_golden(self):
        bundled = (VIEWER / "examples" / "inspect.json").read_bytes()
        self.assertEqual(bundled, GOLDEN_INSPECT.read_bytes())

    def test_bundled_live_example_is_the_recorded_mixed_snapshot(self):
        raw = (VIEWER / "examples" / "live-mixed.json").read_bytes()
        self.assertEqual(
            hashlib.sha256(raw).hexdigest(),
            LIVE_MIXED_SHA256,
            "live-mixed.json changed; regenerate with tlb --live --resolve and repin here and on issue #420",
        )
        doc = json.loads(raw)
        self.assertEqual(doc["schemaVersion"], 1)
        self.assertEqual(doc["plane"], "live")
        self.assertNotIn("address", raw.decode("utf-8"))

    def test_authored_source_ships_with_the_recorded_snapshot(self):
        authored = json.loads((VIEWER / "examples" / "viewer-mixed.json").read_text(encoding="utf-8"))
        self.assertEqual(authored["name"], "viewer_mixed")


if __name__ == "__main__":
    unittest.main()
