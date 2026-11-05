using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindTableTests
{
    static void Add(KindTable table, int row, ushort timeline, ushort step, KindEntryFlags flags = KindEntryFlags.Span)
    {
        var e = table.Append(row);
        e->Timeline = timeline;
        e->Step = step;
        e->Flags = flags;
    }

    static ushort[] Steps(KindTable table, int row) => Enumerable.Range(0, table.Length[row]).Select(i => table.At(row, i)->Step).ToArray();

    [Fact]
    public void RowsStartEmptyEnabledAndInactive()
    {
        using var table = new KindTable(0, [3, 5, 9], 2, 8);
        Assert.Equal(3, table.Rows);
        for (var r = 0; r < 3; r++)
        {
            Assert.False(table.IsActive(r));
            Assert.Equal(0, table.Length[r]);
        }
        Assert.Equal(0b111ul, table.Enabled[0]);
        Assert.Equal(16 + 2 * 8, table.StorageBytesPerRow);
    }

    [Fact]
    public void AppendKeepsOrderAndSpillsToTheHeapPastInline()
    {
        using var table = new KindTable(1, [0, 1], 2, 12);
        for (ushort i = 0; i < 9; i++) Add(table, 0, 1, i);
        Add(table, 1, 1, 100);
        Assert.Equal(Enumerable.Range(0, 9).Select(i => (ushort)i), Steps(table, 0));
        Assert.Equal([(ushort)100], Steps(table, 1));
        Assert.Equal(9, table.MaxLength);
        Assert.Equal(3, table.Overflows);
        Assert.True(table.IsActive(0));
    }

    [Fact]
    public void RemoveSpanKeepsOrderAndClearsActiveWhenEmpty()
    {
        using var table = new KindTable(0, [0], 1, 8);
        for (ushort i = 0; i < 4; i++) { var e = table.Append(0); e->Timeline = 2; e->Step = i; e->Slot = (byte)(i % 2); e->Flags = KindEntryFlags.Span; }
        Assert.True(table.RemoveSpan(0, 1, 2, 1));
        Assert.False(table.RemoveSpan(0, 1, 2, 1));
        Assert.Equal([(ushort)0, 2, 3], Steps(table, 0));
        table.RemoveSpan(0, 0, 2, 0);
        table.RemoveSpan(0, 0, 2, 2);
        table.RemoveSpan(0, 1, 2, 3);
        Assert.Equal(0, table.Length[0]);
        Assert.False(table.IsActive(0));
    }

    [Fact]
    public void RemovePulsesKeepsSpansAndClearClearsAll()
    {
        using var table = new KindTable(0, [0, 1], 2, 8);
        Add(table, 0, 1, 1);
        Add(table, 0, 1, 2, KindEntryFlags.Pulse);
        Add(table, 0, 1, 3);
        Add(table, 0, 1, 4, KindEntryFlags.Pulse);
        table.RemovePulses(0);
        Assert.Equal([(ushort)1, 3], Steps(table, 0));
        Add(table, 1, 1, 5, KindEntryFlags.Pulse);
        table.RemovePulses(1);
        Assert.False(table.IsActive(1));
        table.Clear(0);
        Assert.False(table.IsActive(0));
        Assert.Equal(0, table.Length[0]);
    }

    [Fact]
    public void EnabledBitsToggleIndependentlyOfActive()
    {
        using var table = new KindTable(0, Enumerable.Range(0, 70).ToArray(), 1, 8);
        Assert.Equal(2, table.Words);
        Assert.Equal((1ul << 6) - 1, table.Enabled[1]);
        table.SetEnabled(65, false);
        Assert.Equal((1ul << 6) - 1 & ~(1ul << 1), table.Enabled[1]);
        table.SetEnabled(65, true);
        Assert.Equal((1ul << 6) - 1, table.Enabled[1]);
    }
}
