import hashlib
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
VIEWER = ROOT / "tools" / "Tl.Playground" / "Playground" / "wwwroot" / "viewer"
GOLDEN_INSPECT = ROOT / "tools" / "Tl.Bake.Tests" / "golden" / "inspect.json"
LIVE_MIXED_SHA256 = "6e79ce894cbee7062c0a4a00dde2d2686da7fe9ed1e865fecc484f4c98931814"


class ViewerTests(unittest.TestCase):
    def test_viewer_page_exists_and_is_self_contained(self):
        page = (VIEWER / "index.html").read_text(encoding="utf-8")
        self.assertNotIn("<script src=", page)
        self.assertNotIn("<link ", page)
        for marker in ("schemaVersion", "folded-now", "addresses", "structural delta"):
            self.assertIn(marker, page)

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
