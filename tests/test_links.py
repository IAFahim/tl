import importlib.util
import os
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("check_links", ROOT / "eng" / "check_links.py")
CHECK_LINKS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CHECK_LINKS)


class LinkTests(unittest.TestCase):
    def test_every_relative_and_anchor_link_resolves(self):
        self.assertEqual([], CHECK_LINKS.broken_links(ROOT))

    def test_every_external_url_is_classified(self):
        targets = set()
        for path in CHECK_LINKS.markdown_files(ROOT):
            for link in CHECK_LINKS.LINK.findall(path.read_text(encoding="utf-8")):
                if CHECK_LINKS.EXTERNAL.match(link) and link not in CHECK_LINKS.WAIVED_EXTERNAL:
                    targets.add(link)
        unclassified = [
            url for url in sorted(targets)
            if not any(url.startswith(prefix) for prefix in CHECK_LINKS.ALLOWED_PREFIXES)
        ]
        self.assertEqual([], unclassified)

    def test_waiver_tracks_a_checked_in_link(self):
        walked = "\n".join(path.read_text(encoding="utf-8") for path in CHECK_LINKS.markdown_files(ROOT))
        for url in CHECK_LINKS.WAIVED_EXTERNAL:
            self.assertIn(f"({url})", walked)

    @unittest.skipUnless(os.environ.get("TL_LINK_HEAD_CHECK") == "1", "external HEAD checks run only in the scheduled lane")
    def test_classified_external_targets_are_alive(self):
        self.assertEqual([], CHECK_LINKS.dead_external_targets(CHECK_LINKS.external_targets(ROOT)))


if __name__ == "__main__":
    unittest.main()
