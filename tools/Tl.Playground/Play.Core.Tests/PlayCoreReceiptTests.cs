
using Xunit;
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

    [Fact]
    public void BakeAll_KeysById_ReplacesInPlace_AndPeelsTheIdFromTheBytes()
    {
        var compile = LiveAuthoring.Compile(LiveAuthoring.DefaultSource);
        Assert.True(compile.Ok, compile.Error);
        var assembly = compile.Assembly!;

        var noId = LiveAuthoring.DefaultTimelineJson.Replace("\"id\": \"jump\", \"name\"", "\"name\"");
        var mod = LiveAuthoring.DefaultTimelineJson.Replace("\"Velocity\": 2.0", "\"Velocity\": 4.0");
        var walk = """
            { "id": "walk", "duration": 10, "loop": false,
              "tracks": [ { "namespace": "Live", "type": "JumpTrack", "data": { "Scale": 1.0 },
                "clips": [ { "namespace": "Live", "type": "JumpClip", "start": 0, "end": 10, "data": { "Velocity": 1.0 } } ] } ] }
            """;

        var (withId, withIdKeys, withIdError) = LiveAuthoring.BakeAll(assembly, LiveAuthoring.DefaultTimelineJson);
        var (withoutId, withoutIdKeys, withoutIdError) = LiveAuthoring.BakeAll(assembly, noId);
        Assert.Equal("", withIdError);
        Assert.Equal("", withoutIdError);
        Assert.Equal(["jump"], withIdKeys);
        Assert.Equal(["jump"], withoutIdKeys);
        Assert.Equal(withId[0], withoutId[0]);

        BakedCache.Store(withId, withIdKeys, "recipe");
        Assert.Single(BakedCache.Entries);
        Assert.Equal(1, BakedCache.Revision);

        var (modBytes, modKeys, modError) = LiveAuthoring.BakeAll(assembly, mod);
        Assert.Equal("", modError);
        Assert.NotEqual(withId[0], modBytes[0]);
        BakedCache.Store(modBytes, modKeys, "recipe");
        Assert.Single(BakedCache.Entries);
        Assert.Equal(2, BakedCache.Revision);
        Assert.Equal("replaced jump", BakedCache.LastChange);
        Assert.Equal(modBytes[0], BakedCache.Entries[0].Package);

        var (pair, pairKeys, pairError) = LiveAuthoring.BakeAll(assembly, $"[{mod}, {walk}]");
        Assert.Equal("", pairError);
        Assert.Equal(["jump", "walk"], pairKeys);
        BakedCache.Store(pair, pairKeys, "recipe");
        Assert.Equal(2, BakedCache.Entries.Count);
        Assert.Equal("replaced jump, added walk", BakedCache.LastChange);

        BakedCache.Store(modBytes, modKeys, "recipe");
        Assert.Equal(2, BakedCache.Entries.Count);
        Assert.Equal(pair[1], BakedCache.Entries[1].Package);

        BakedCache.Store(withId, withIdKeys, "another-recipe");
        Assert.Single(BakedCache.Entries);
        Assert.StartsWith("loaded ", BakedCache.LastChange, StringComparison.Ordinal);

        var (dup, _, dupError) = LiveAuthoring.BakeAll(assembly, $"[{mod}, {mod}]");
        Assert.Empty(dup);
        Assert.Contains("unique", dupError, StringComparison.Ordinal);
    }
}
