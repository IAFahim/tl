
namespace Play.Core.Tests;

public class PlayCoreReceiptTests
{
    private const string SmokeReceipt = """
        smoke: bake bytes=385 crowd=256 duration=64 loop=True
        phase: 40 frames forward bit-exact (moved/skipped/wrapped at last frame: 256/0/4)
        phase: 40 frames backward bit-exact (moved/skipped/wrapped at last frame: 256/0/4)
        a-b-a: 40 forward + 40 backward restored the initial snapshot bit-exactly
        phase: 20 frames forward bit-exact (moved/skipped/wrapped at last frame: 256/0/4)
        re-bake: loop=false duration=64 bytes=385
        phase: 20 frames forward bit-exact (moved/skipped/wrapped at last frame: 180/76/0)
        SMOKE PASS frames=120 checksum=7131578910045740992
        """;

    [Fact]
    public void SmokeRun_MatchesThePinnedReceipt()
    {
        Assert.Equal(SmokeReceipt.ReplaceLineEndings(), SmokeRun.Launch().ReplaceLineEndings().TrimEnd());
    }

    [Fact]
    public void LiveAuthoring_MatchesThePinnedChecksums()
    {
        var receipt = LiveAuthoring.Receipt();

        Assert.Matches(@"LIVE PASS compileMs=\d+ bytes=441 lines=13 checksum=10966946392219585791", receipt);
        Assert.Contains("MATRIX PASS shapes=5 checksum=357430691702761716", receipt, StringComparison.Ordinal);
    }

    [Fact]
    public void Examples_MatchThePinnedShape()
    {
        var receipt = Examples.Validate();

        Assert.StartsWith("EXAMPLE PASS boss health=80 position=2; ", receipt, StringComparison.Ordinal);
        Assert.Matches(
            @"EXAMPLE PASS crowd rows=1000000 groups=100 frames=60 shared=\d+ sync=\d+ groups=\d+; EXAMPLE PASS samples mixed=308154984194266103 showcase=295696093477130562",
            receipt["EXAMPLE PASS boss health=80 position=2; ".Length..]);
    }
}
