using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class KernelEmitterGuardTests
{
    [Fact]
    public void EmittedKernelSourceUsesLfLineEndingsOnly()
    {
        var source = KernelEmitter.Emit(Tl.Core.Tests.KernelBakers.AbaMirrored());

        Assert.DoesNotContain('\r', source);
        Assert.Equal(source, source.Replace("\r\n", "\n").Replace('\r', '\n'));
    }

    [Theory]
    [InlineData("Finite")]
    [InlineData("Looping")]
    [InlineData("AbaMirrored")]
    [InlineData("BlendSpanThree")]
    [InlineData("BlendSpanOne")]
    [InlineData("Consumerless")]
    [InlineData("Empty")]
    public void EmissionIsLfStableAcrossRepeatedInvocations(string bakerMethod)
    {
        var bytes = bakerMethod switch
        {
            "Finite" => Tl.Core.Tests.KernelBakers.Finite(),
            "Looping" => Tl.Core.Tests.KernelBakers.Looping(),
            "AbaMirrored" => Tl.Core.Tests.KernelBakers.AbaMirrored(),
            "BlendSpanThree" => Tl.Core.Tests.KernelBakers.BlendSpanThree(),
            "BlendSpanOne" => Tl.Core.Tests.KernelBakers.BlendSpanOne(),
            "Consumerless" => Tl.Core.Tests.KernelBakers.Consumerless(),
            "Empty" => Tl.Core.Tests.KernelBakers.Empty(),
            _ => throw new InvalidOperationException("unknown fixture"),
        };

        Assert.Equal(KernelEmitter.Emit(bytes), KernelEmitter.Emit(bytes));
        Assert.DoesNotContain('\r', KernelEmitter.Emit(bytes));
    }
}
