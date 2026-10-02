using Xunit;

namespace Tl.Core.Tests;

public unsafe class PairTableGrowthTests
{
    [Fact]
    public void InstallsGrowPastTheInitialSixtyFourConsumerRows()
    {
        var before = PairTable.ConsumerCount;
        for (var i = 0; i < 70; i++)
            PairTable.Install(0x9000_0000_0000_0000UL + (ulong)(uint)(i + 1), null, null, null, null, false);
        Assert.True(PairTable.ConsumerCount >= before + 70);
    }
}
