using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class ProbeLivenessReproductionTests
{
    private const string Doc = """
        {
          "duration": 100,
          "tracks": [
            {
              "namespace": "FusedBake",
              "type": "GaTrack0",
              "data": { "Scale": 1.0, "Code": 7 },
              "clips": [ { "namespace": "FusedBake", "type": "GaClip0", "start": 0, "end": 10 } ]
            }
          ]
        }
        """;

    [Fact]
    public void IdenticalContentFullLifecycleKillsAnUnpinnedLoadIndex()
    {
        var bytes = TimelineBaker.BakeJson(Encoding.UTF8.GetBytes(Doc));
        var first = TimelineAsset.Load(bytes);

        using (TimelineAsset.Of(TimelineAsset.Load(bytes))) { }

        Assert.Throws<ArgumentException>(() => TimelineAsset.Of(first));
    }
}
