using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindValuesLayoutTests
{
    [StructLayout(LayoutKind.Sequential)]
    struct ThreeFloats { public float X, Y, Z; }

    [StructLayout(LayoutKind.Sequential)]
    struct LongThenInt128 { public long Lead; public Int128 Value; }

    [StructLayout(LayoutKind.Sequential)]
    struct ByteThen<T> where T : unmanaged { public byte Lead; public T Value; }

    static int AlignmentOf<T>() where T : unmanaged => sizeof(ByteThen<T>) - sizeof(T);

    struct NoSpans<T> : IKindSpanValues<T> where T : unmanaged
    {
        public bool Evaluate(int feeder, LibraryStep* step, LibraryTimeline* timeline, ushort tick, bool backward, out T value) { value = default; return false; }
        public void Combine(ref T total, in T next) { }
    }

    struct SingleFill<T> : IKindFill where T : unmanaged
    {
        public ushort* Routes;
        public void Add(ref KindBuild build)
        {
            var s = default(NoSpans<T>);
            build.Add<T, NoSpans<T>>(0, 0, Routes, ref s);
        }
    }

    struct FusedFill : IKindFill
    {
        public ushort* Routes;
        public void Add(ref KindBuild build)
        {
            var sb = default(NoSpans<bool>);
            var sf = default(NoSpans<float>);
            var si = default(NoSpans<Int128>);
            build.Add<bool, NoSpans<bool>>(0, 0, Routes, ref sb);
            build.Add<float, NoSpans<float>>(1, 0, Routes, ref sf);
            build.Add<Int128, NoSpans<Int128>>(2, 0, Routes, ref si);
        }
    }

    static (int Front, int Back, int Stride) LayoutOf<T>() where T : unmanaged
    {
        var rowBases = stackalloc int[1];
        var views = stackalloc KindValues[64];
        ushort route = 0;
        var fill = new SingleFill<T> { Routes = &route };
        var built = KindBuild.Of(default, null, views, rowBases, false, ref fill);
        return (views[0].Front, views[0].Back, built.Stride);
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
        Assert.Equal((4, 16, 28), LayoutOf<ThreeFloats>());
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

    [Fact]
    public void FusedTablePacksMultipleKindsOnTheirNaturalAlignmentWithThreeBitShifts()
    {
        var rowBases = stackalloc int[1];
        var views = stackalloc KindValues[64];
        ushort route = 0;
        var fill = new FusedFill { Routes = &route };
        var built = KindBuild.Of(default, null, views, rowBases, false, ref fill);
        Assert.Equal(7ul, built.Kinds);
        Assert.Equal((4, 5, 0), (views[0].Front, views[0].Back, views[0].Shift));
        Assert.Equal((8, 12, 3), (views[1].Front, views[1].Back, views[1].Shift));
        if (AlignmentOf<Int128>() == 16)
        {
            Assert.Equal((16, 32, 6), (views[2].Front, views[2].Back, views[2].Shift));
            Assert.Equal(48, built.Stride);
        }
        Assert.True(views[2].Front % AlignmentOf<Int128>() == 0 && views[2].Back % AlignmentOf<Int128>() == 0 && built.Stride % AlignmentOf<Int128>() == 0);
    }
}
