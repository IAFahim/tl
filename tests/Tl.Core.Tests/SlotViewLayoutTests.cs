using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class SlotViewLayoutTests
{
    [Theory]
    [InlineData(nameof(SlotView.Directory), 0)]
    [InlineData(nameof(SlotView.Segments), 8)]
    [InlineData(nameof(SlotView.Dense), 16)]
    [InlineData(nameof(SlotView.Forward), 24)]
    [InlineData(nameof(SlotView.Backward), 32)]
    [InlineData(nameof(SlotView.LaneKeys), 40)]
    [InlineData(nameof(SlotView.Duration), 48)]
    [InlineData(nameof(SlotView.Looping), 50)]
    [InlineData(nameof(SlotView.Absent), 52)]
    [InlineData(nameof(SlotView.TableTicks), 56)]
    [InlineData(nameof(SlotView.ResultCount), 60)]
    [InlineData(nameof(SlotView.AbiVersion), 62)]
    [InlineData(nameof(SlotView.Generation), 64)]
    public void SlotViewFieldsSitAtTheDocumentedAbiOffsets(string field, int offset)
        => Assert.Equal(offset, Marshal.OffsetOf<SlotView>(field).ToInt32());

    [Fact]
    public void SlotViewIsSeventyTwoBytes() => Assert.Equal(72, Unsafe.SizeOf<SlotView>());
}
