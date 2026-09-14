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

    [Fact]
    public void TruncatedBufferFailsWithTheDiagnostic() =>
        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(new byte[47]));

    [Fact]
    public void DeclaredLengthMismatchFailsWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        W32(bytes, 44, (uint)bytes.Length + 64);

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void StepPairOutOfRangeFailsWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        var stageOffset = (int)U32(bytes, 32);
        var programOffset = (int)U32(bytes, stageOffset + 8);
        Assert.True(U32(bytes, stageOffset + 12) > 0);
        W32(bytes, programOffset + 4, U32(bytes, 24));

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void UnsortedPairKeysFailWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        var pairOffset = (int)U32(bytes, 28);
        var first = U64(bytes, pairOffset);
        var second = U64(bytes, pairOffset + 16);
        Assert.True(first < second);
        W64(bytes, pairOffset, second);
        W64(bytes, pairOffset + 16, first);

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void NonMonotonicStageFailsWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        var stageOffset = (int)U32(bytes, 32);
        W32(bytes, stageOffset, U32(bytes, stageOffset) + 1);

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void UnalignedSlotFailsWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        var stageOffset = (int)U32(bytes, 32);
        var programOffset = (int)U32(bytes, stageOffset + 8);
        Assert.True(U32(bytes, stageOffset + 12) > 0);
        W32(bytes, programOffset, U32(bytes, programOffset) | 8u);

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void SlotPastTheAssetFailsWithTheDiagnostic()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();
        var stageOffset = (int)U32(bytes, 32);
        var programOffset = (int)U32(bytes, stageOffset + 8);
        Assert.True(U32(bytes, stageOffset + 12) > 0);
        W32(bytes, programOffset, (uint)bytes.Length + 16);

        Assert.Throws<BakeDiagnosticException>(() => KernelEmitter.Emit(bytes));
    }

    [Fact]
    public void ValidAssetStillEmitsAfterValidation()
    {
        var bytes = Tl.Core.Tests.KernelBakers.AbaMirrored();

        Assert.Contains("TimelineKernel_", KernelEmitter.Emit(bytes));
    }

    private static uint U32(byte[] bytes, int at) => BitConverter.ToUInt32(bytes, at);
    private static ulong U64(byte[] bytes, int at) => BitConverter.ToUInt64(bytes, at);
    private static void W32(byte[] bytes, int at, uint value) => BitConverter.GetBytes(value).CopyTo(bytes, at);
    private static void W64(byte[] bytes, int at, ulong value) => BitConverter.GetBytes(value).CopyTo(bytes, at);
}
