using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct CursorClip(float Amount);
public readonly record struct CursorTrack(float Scale) : IBlend<CursorClip>
{
    public void Blend(in CursorClip first, in CursorClip second, float factor, out CursorClip result) => result = first;
}

internal static unsafe class CursorPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<CursorTrack, CursorClip>.Consume(&Execute, &Bind, "tl\0Execute");
    }

    static void Bind(ulong* keys, int keyCount, byte* table)
    {
    }

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
    }
}

public class PendingCursorTests
{
    [Fact]
    public void StableTypeKeysExcludeAssemblyIdentity()
    {
        Assert.Equal("System.Single, " + typeof(float).Assembly.GetName().Name, Keying.StableName(typeof(float)));
        var generic = Keying.StableName(typeof(System.Collections.Generic.List<float>));
        Assert.StartsWith("System.Collections.Generic.List`1<System.Single, ", generic);
        Assert.DoesNotContain("Version", generic);
        Assert.DoesNotContain("Culture", generic);
        Assert.DoesNotContain("PublicKeyToken", generic);
    }

    [Fact]
    public void OldFormatVersionIsRejectedAtLoad()
    {
        var bytes = new DomainBaker()
            .Track<CursorTrack, CursorClip>(new CursorTrack(1f))
            .Clip(0, 0u, 4u, new CursorClip(1f))
            .Bake();
        bytes[4] = 4;
        var error = Assert.Throws<ArgumentException>(() => TimelineAsset.Load(bytes));
        Assert.Contains("rebake", error.Message);
    }

    [Fact]
    public void DeadHoleDoesNotStallThePendingCursor()
    {
        var a = TimelineAsset.LoadAsset(Bake(1));
        var b = TimelineAsset.LoadAsset(Bake(2));
        var c = TimelineAsset.LoadAsset(Bake(3));
        try
        {
            Timeline<CursorTrack, CursorClip>.View(a);
            var deadIndex = b.Index;
            b.Dispose();
            Timeline<CursorTrack, CursorClip>.View(c);
            var d = TimelineAsset.LoadAsset(Bake(4));
            try
            {
                Timeline<CursorTrack, CursorClip>.View(d);
                var bank = Timeline<CursorTrack, CursorClip>._bank;
                Assert.NotNull(bank);
                Assert.True(bank.PendingCursor > deadIndex, $"cursor {bank.PendingCursor} never passed the dead hole at {deadIndex}");
                Assert.Throws<ArgumentException>(() => Timeline<CursorTrack, CursorClip>.View(deadIndex));
            }
            finally
            {
                d.Dispose();
            }
        }
        finally
        {
            a.Dispose();
            c.Dispose();
        }
    }

    [Fact]
    public void FirstUseFoldsExactlyTheRequestedIndex()
    {
        var a = TimelineAsset.LoadAsset(Bake(1));
        var b = TimelineAsset.LoadAsset(Bake(2));
        var c = TimelineAsset.LoadAsset(Bake(3));
        try
        {
            Timeline<CursorTrack, CursorClip>.View(a);
            Assert.True(b.Index > a.Index && c.Index > b.Index, "fresh contents receive ascending indices");
            Timeline<CursorTrack, CursorClip>.View(c);
            var bank = Timeline<CursorTrack, CursorClip>._bank;
            Assert.NotNull(bank);
            Assert.True(bank.IsFolded(c.Index), "the first-used index folds");
            Assert.False(bank.IsFolded(b.Index), "first use of one index folded the live index below it");
        }
        finally
        {
            a.Dispose();
            b.Dispose();
            c.Dispose();
        }
    }

    [Fact]
    public void DeadHoleBelowStaysPendingThroughLaterFirstUses()
    {
        var a = TimelineAsset.LoadAsset(Bake(1));
        var b = TimelineAsset.LoadAsset(Bake(2));
        var c = TimelineAsset.LoadAsset(Bake(3));
        try
        {
            Timeline<CursorTrack, CursorClip>.View(a);
            var deadIndex = b.Index;
            b.Dispose();
            Timeline<CursorTrack, CursorClip>.View(c);
            var d = TimelineAsset.LoadAsset(Bake(4));
            try
            {
                Timeline<CursorTrack, CursorClip>.View(d);
                var bank = Timeline<CursorTrack, CursorClip>._bank;
                Assert.NotNull(bank);
                Assert.True(bank.IsPending(deadIndex), $"the dead hole at {deadIndex} was swept by a later first use instead of staying a pending hole");
                Assert.Throws<ArgumentException>(() => Timeline<CursorTrack, CursorClip>.View(deadIndex));
            }
            finally
            {
                d.Dispose();
            }
        }
        finally
        {
            a.Dispose();
            c.Dispose();
        }
    }

    static byte[] Bake(int seed) => new DomainBaker()
        .Track<CursorTrack, CursorClip>(new CursorTrack(seed))
        .Clip(0, 0u, 4u, new CursorClip(seed))
        .Bake();
}
