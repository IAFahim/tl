using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct LateClip(float Amount);
public readonly record struct LateTrack(float Scale) : IBlend<LateClip>
{
    public void Blend(in LateClip first, in LateClip second, float factor, out LateClip result) => result = first;
}

internal static unsafe class LatePairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LateTrack, LateClip>.Consume(&Execute, &FloatKeys, "tl\0First");
    }

    internal static void InstallLate()
    {
        PairRuntime<LateTrack, LateClip>.Consume(&Execute, &IntKeys, "tl\0Late");
    }

    static int FloatKeys(ulong* keys, byte* meta)
    {
        if (keys == null) return 1;
        keys[0] = TypeKey<float>.Value;
        meta[0] = 4;
        return 1;
    }

    static int IntKeys(ulong* keys, byte* meta)
    {
        if (keys == null) return 1;
        keys[0] = TypeKey<int>.Value;
        meta[0] = 4;
        return 1;
    }

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
    }
}

public class ConsumerDigestTests
{
    [Fact]
    public void LateConsumerInstallIsDetectedAtTheNextFirstUse()
    {
        var a = TimelineAsset.LoadAsset(Bake(41));
        var b = TimelineAsset.LoadAsset(Bake(42));
        try
        {
            var first = Timeline<LateTrack, LateClip>.View(a.Index);
            Assert.Equal(1, first.ResultCount);

            LatePairs.InstallLate();

            var second = Timeline<LateTrack, LateClip>.View(b.Index);
            Assert.Equal(2, second.ResultCount);

            var refirst = Timeline<LateTrack, LateClip>.View(a.Index);
            Assert.Equal(2, refirst.ResultCount);
        }
        finally
        {
            a.Dispose();
            b.Dispose();
        }
    }

    static byte[] Bake(int seed) => new DomainBaker()
        .Track<LateTrack, LateClip>(new LateTrack(seed))
        .Clip(0, 0u, 4u, new LateClip(seed))
        .Bake();
}
