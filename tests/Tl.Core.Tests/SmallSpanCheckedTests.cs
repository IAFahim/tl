#if TL_CHECKED
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public partial class SmallSpanRecordTests
{
    [Fact]
    public void SmallSpanRejectsBadColumnsLikeTheCrowdPath()
    {
        var index = TimelineAsset.Load(Bake(10, looping: true, 2f));
        var buffer = new byte[16];
        Assert.Throws<ArgumentException>(() =>
        {
            var positions = MemoryMarshal.Cast<byte, ushort>(buffer.AsSpan(0, 8));
            var effects = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, 16));
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, true, effects);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var positions = MemoryMarshal.Cast<byte, ushort>(buffer.AsSpan(0, 8));
            var effects = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, 16));
            Timeline<RoutingTrack, RoutingClip>.Apply(index, positions, new ushort[4], true, effects);
        });
        var clean = new ushort[] { 1, 2, 3, 4 };
        Assert.Throws<ArgumentException>(
            () => Timeline<RoutingTrack, RoutingClip>.Apply(index, clean, new ushort[3], true, new float[3]));
    }
}
#endif
