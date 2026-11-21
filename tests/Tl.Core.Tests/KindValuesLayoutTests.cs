using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindValuesLayoutTests
{
    struct ThreeFloats { public float X, Y, Z; }

    struct LongThenInt128 { public long Lead; public Int128 Value; }

    [StructLayout(LayoutKind.Sequential)]
    struct ByteThen<T> where T : unmanaged { public byte Lead; public T Value; }

    static int AlignmentOf<T>() where T : unmanaged => sizeof(ByteThen<T>) - sizeof(T);

    static (int Front, int Back, int Stride) LayoutOf<T>() where T : unmanaged
    {
        var rowBases = stackalloc int[1];
        var values = KindValues.Layout<T>(default, null, 0, 0, rowBases);
        return (values.Front, values.Back, values.Stride);
    }

    static void AssertEveryValueIsNaturallyAligned<T>() where T : unmanaged
    {
        var alignment = AlignmentOf<T>();
        var (front, back, stride) = LayoutOf<T>();
        Assert.True(front % alignment == 0 && back % alignment == 0 && stride % alignment == 0, $"{typeof(T).Name} needs {alignment}-byte alignment but the table puts it at Front {front}, Back {back}, Stride {stride}.");
        Assert.True(64 % alignment == 0, $"{typeof(T).Name} needs more alignment than the 64-byte table block gives.");
    }

    [Fact]
    public void ValuesAlignedToEightBytesOrLessKeepTheirLayout()
    {
        Assert.Equal((4, 5, 8), LayoutOf<bool>());
        Assert.Equal((4, 8, 12), LayoutOf<float>());
        Assert.Equal((8, 16, 24), LayoutOf<long>());
        Assert.Equal((8, 16, 24), LayoutOf<double>());
        Assert.Equal((8, 20, 32), LayoutOf<ThreeFloats>());
    }

    [Fact]
    public void Int128ValuesSitOnTheirSixteenByteAlignment()
    {
        if (Environment.Is64BitProcess) Assert.Equal(16, AlignmentOf<Int128>());
        AssertEveryValueIsNaturallyAligned<Int128>();
        AssertEveryValueIsNaturallyAligned<UInt128>();
        AssertEveryValueIsNaturallyAligned<LongThenInt128>();
        if (AlignmentOf<Int128>() == 16)
        {
            Assert.Equal((16, 32, 48), LayoutOf<Int128>());
            Assert.Equal((16, 48, 80), LayoutOf<LongThenInt128>());
        }
    }
}
