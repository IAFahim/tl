using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class ProbeLivenessStressTests
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
    public void ConcurrentDuplicateContentBatchProbesRemainLive()
    {
        var inputs = Enumerable.Repeat(Encoding.UTF8.GetBytes(Doc), 8).ToArray();
        for (var round = 0; round < 500; round++)
        {
            var batched = TimelineBaker.BakeJsonBatch(inputs);
            Assert.Equal(8, batched.Length);
        }
    }
}
