import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CONST = re.compile(r"internal const int SlotRow = (\d+);")
RUNTIME = ROOT / "src/Tl.Core/Data.cs"
GENERATOR = ROOT / "src/Tl.Gen.CSharp/Analysis/JobReader.cs"


class ConsumerAbiRowTest(unittest.TestCase):
    def test_runtime_and_generator_share_one_slot_row_width(self):
        runtime = CONST.search(RUNTIME.read_text(encoding="utf-8"))
        generator = CONST.search(GENERATOR.read_text(encoding="utf-8"))
        self.assertIsNotNone(runtime, f"{RUNTIME} must declare internal const int SlotRow")
        self.assertIsNotNone(generator, f"{GENERATOR} must declare internal const int SlotRow")
        self.assertEqual(runtime.group(1), generator.group(1),
                         "the consumer slot-row width is one contract: the Tl.Core and Tl.Gen.CSharp constants must stay equal")
        self.assertEqual(runtime.group(1), "8",
                         "issue #356 widened the consumer slot row to 8 pointer slots; changing it again is a deliberate ABI decision recorded on the issue")


if __name__ == "__main__":
    unittest.main()
