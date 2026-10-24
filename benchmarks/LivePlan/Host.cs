using System.Runtime.CompilerServices;
using Tl;
using Tl.TestSupport;

namespace LivePlan;

public readonly record struct BenchClip(float Amount);

public readonly record struct BenchTrack(int Pattern) : IBlend<BenchClip>
{
    public void Blend(in BenchClip first, in BenchClip second, float factor, out BenchClip result)
        => result = first;
}

internal static unsafe class BenchConsumers
{
    internal static int Installed;

    internal static void Install()
    {
        if (Interlocked.Exchange(ref Installed, 1) != 0) return;
        var fold = PairRuntime<BenchTrack, BenchClip>.Consume(&Fold, &FoldKeys, "tl\0LiveFold");
        PairRuntime<BenchTrack, BenchClip>.ConsumeDispatch(&Row, &RowKeys, &Diag, "tl\0LiveRow", fold);
    }

    private static int FoldKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x10; }
        return 1;
    }

    private static void Fold(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = tick;

    private static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x40;
            keys[1] = TypeKey<int>.Value; meta[1] = 4;
        }
        return 2;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<BenchTrack, BenchClip> requires its named fold feed.");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((int*)columns[1])[row] = (int)(*(float*)columns[0] * 1000);
}

internal static class Host
{
    public const int Variants = 8;
    public const ushort Duration = 64;

    static TimelineAsset[]? _owned;

    public static ushort[] BindBank()
    {
        if (_owned == null) _owned = Enumerable.Range(0, Variants).Select(Asset).ToArray();
        return Array.ConvertAll(_owned, asset => asset.Index);
    }

    static TimelineAsset Asset(int pattern)
        => TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<BenchTrack, BenchClip>(new BenchTrack(pattern))
            .Clip(0, 0, Duration, new BenchClip(pattern + 1f))
            .Bake()));
}
