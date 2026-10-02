using System.Reflection;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbInspectTests
{
    const string BlendJson = """
    {
      "duration": 20,
      "loop": false,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "data": { "Code": 1 },
          "clips": [
            { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 12, "data": { "Value": 5 } },
            { "namespace": "Tlb", "type": "AlphaClip", "start": 8, "end": 20, "data": { "Value": 9 } }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void Inspect_MatchesGolden_AndRepeatsIdentically()
    {
        var bytes = TimelineBaker.BakeJson(BlendJson);
        var first = TlbInspection.Inspect(bytes);
        Assert.Equal(first, TlbInspection.Inspect(bytes));
        Assert.Equal(ExpectedGolden(), first);
    }

    [Fact]
    public void Inspect_RejectsBadMagic_AndTruncation()
    {
        var bytes = TimelineBaker.BakeJson(BlendJson);
        bytes[0] = 0x58;
        Assert.Equal("TLB asset header is invalid.", Assert.Throws<ArgumentException>(() => TlbInspection.Inspect(bytes)).Message);
        Assert.Equal("TLB asset is truncated.", Assert.Throws<ArgumentException>(() => TlbInspection.Inspect(bytes.AsSpan(0, 32))).Message);
    }

    [Fact]
    public void Cli_Inspect_ExitsZeroForAssets_TwoForUsage_ThreeForInvalidAssets()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tlb_inspect_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tlbPath = Path.Combine(tempDir, "blend.tlb");
            File.WriteAllBytes(tlbPath, TimelineBaker.BakeJson(BlendJson));
            var badPath = Path.Combine(tempDir, "bad.tlb");
            File.WriteAllBytes(badPath, "not a tlb"u8.ToArray());

            Assert.Equal(0, Program.Main(["--inspect", tlbPath]));
            Assert.Equal(2, Program.Main(["--inspect"]));
            Assert.Equal(2, Program.Main(["--inspect", Path.Combine(tempDir, "missing.tlb")]));
            Assert.Equal(3, Program.Main(["--inspect", badPath]));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    static string ExpectedGolden()
    {
        var goldenPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "golden", "inspect.json");
        return File.ReadAllText(goldenPath);
    }
}
