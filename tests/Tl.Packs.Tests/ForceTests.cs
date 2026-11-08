using Tl;
using Tl.Gen.Tlb;
using Tl.Mathematics;
using Tl.Pack.Force;
using Xunit;
using ForceState = Tl.Pack.Force.Force;

namespace Tl.Packs.Tests;

public sealed class ForceTests
{
    [Fact]
    public void PackBuildEmitsItsPlainSystem()
    {
        var states = new ForceState[1];
        var system = new Tl.Generated.TlAuto_Tl_Pack_Force { C0_Force = states };
        Assert.Equal(0, states[0].Value.x);
    }
}
