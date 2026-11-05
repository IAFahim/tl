using System.Buffers.Binary;
using System.Security.Cryptography;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindSectionTests
{
    static byte[] Kinds() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "kinds.tlib"));

    static byte[] Redigest(byte[] bytes)
    {
        SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes.AsSpan(bytes.Length - 32));
        return bytes;
    }

    static uint U32(byte[] bytes, int at) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));

    static byte[] Patched(Action<byte[], int> edit)
    {
        var bytes = Kinds();
        edit(bytes, (int)U32(bytes, 28));
        return Redigest(bytes);
    }

    [Fact]
    public void LoadsVersionTwoAndExposesTheKindView()
    {
        using var world = new TimelineWorld(Kinds());
        var view = world.View;
        Assert.Equal(2u, view.Version);
        var kinds = view.Kinds;
        Assert.False(kinds.IsEmpty);
        Assert.Equal(4, kinds.KindCount);
        Assert.Equal(5, kinds.FeederCount);
        Assert.Equal(4, kinds.TimelineCount);
        Assert.Equal(["Damage", "Lift", "Locomotion", "StatChange"], Enumerable.Range(0, 4).Select(kinds.NameOf));
        Assert.Equal([5u, 2u, 1u, 3u], Enumerable.Range(0, 4).Select(k => kinds.Kinds[k].Capacity));
        Assert.Equal([12u, 8u, 8u, 12u], Enumerable.Range(0, 4).Select(k => kinds.Kinds[k].EntryBytes));
        Assert.Equal(2, kinds.IndexOf("Locomotion"));
        Assert.Equal(-1, kinds.IndexOf("Poison"));
        Assert.Equal(string.Empty, kinds.GroupOf(0));

        var stomp = kinds.TimelineOf(4);
        Assert.Equal(0b1001ul, (ulong)stomp->KindMask);
        Assert.Equal(1, (int)kinds.EventBitsAt(stomp, 2) & 1);
        Assert.Equal(0, (int)kinds.EventBitsAt(stomp, 0));
        var from = kinds.EventStart(stomp, 2, 0);
        var to = kinds.EventEnd(stomp, 2, 0);
        Assert.Equal(1u, to - from);
        var pulse = kinds.Events(stomp)[from];
        Assert.Equal((byte)KindEventOp.PulseStart, pulse.Op);
        Assert.Equal("Damage", kinds.NameOf(kinds.Feeders[pulse.Feeder].Kind));
        Assert.True(kinds.TimelineOf(0) == null);
        Assert.True(kinds.TimelineOf(5) == null);
    }

    [Fact]
    public void VersionOneLibraryHasNoKindsAndReloadSwapsThem()
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "v1.tlib")));
        Assert.Equal(1u, world.View.Version);
        Assert.True(world.View.Kinds.IsEmpty);
        world.Reload(Kinds());
        Assert.Equal(4, world.View.Kinds.KindCount);
        world.Reload(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "v1.tlib")));
        Assert.True(world.View.Kinds.IsEmpty);
    }

    [Fact]
    public void KindSectionRejectsEveryBrokenField()
    {
        static void Rejects(byte[] bytes, string part)
        {
            var ex = Assert.Throws<ArgumentException>(() => new TimelineWorld(bytes));
            Assert.Contains(part, ex.Message);
        }
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(28), (uint)k + 4)), "out of bounds");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(k), 1)), "magic invalid");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(k + 4), 2)), "version unknown");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(k + 8), 65)), "more than 64 kinds");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(k + 16), 3)), "differs from the manifest");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(k + 64 + 12), 16)), "entry size");
        Rejects(Patched((b, k) => b[k + 64 + 20] = 3), "out of range");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(k + 64 + 160), 9)), "fields out of range");
        var kinds = Kinds();
        var section = (int)U32(kinds, 28);
        var row = section + 64 + 40 * 4 + 32 * 5 + 48 * 3;
        var events = section + (int)U32(kinds, row + 24);
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(events), 99)), "fields out of range");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(row), 1ul << 40)), "past the table");
        var index = section + (int)U32(kinds, row + 20);
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(index), 1)), "not ascending");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(index + 4 * 16), 100)), "does not cover");
        Rejects(Patched((b, k) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(row + 28), 7)), "wrap lists");
        var digest = Kinds();
        digest[section + 70] ^= 1;
        Rejects(digest, "digest mismatch");
    }
}
