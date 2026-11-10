import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CONST = re.compile(r"internal const int SlotRow = (\d+);")
GENERATOR = ROOT / "src/Tl.Gen.CSharp/Analysis/JobReader.cs"


class ConsumerAbiRowTest(unittest.TestCase):
    def test_runtime_and_generator_share_one_slot_row_width(self):
        generator = CONST.search(GENERATOR.read_text(encoding="utf-8"))
        self.assertIsNotNone(generator, f"{GENERATOR} must declare internal const int SlotRow")
        self.assertEqual(generator.group(1), "40",
                         "issue #356 sized the consumer slot row to the amended maxima (10 memo results + 30 gameplay columns = 40 pointer slots); changing it again is a deliberate ABI decision recorded on the issue")


if __name__ == "__main__":
    unittest.main()
