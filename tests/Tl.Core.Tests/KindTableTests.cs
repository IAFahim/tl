using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindTableTests
{
    static void Add(KindTable* table, int row, ushort timeline, ushort step, KindEntryFlags flags = KindEntryFlags.Span, byte slot = 0)
    {
        var e = table->Append(row);
        e->Timeline = timeline;
        e->Step = step;
        e->Slot = slot;
        e->Flags = flags;
    }

    static ushort[] Steps(KindTable* table, int row) => Enumerable.Range(0, table->Length[row]).Select(i => table->At(row, i)->Step).ToArray();

    static void With(int[] entities, int inline, int entryBytes, Action<nint> body)
    {
        var table = KindTable.Create(0, entities, inline, entryBytes);
        try { body((nint)table); }
        finally { KindTable.Free(table); }
    }

    [Fact]
    public void RowsStartEmptyEnabledAndInactive() => With([3, 5, 9], 2, 8, p =>
    {
        var table = (KindTable*)p;
        Assert.Equal(3, table->Rows);
        for (var r = 0; r < 3; r++)
        {
            Assert.False(table->IsActive(r));
            Assert.Equal(0, table->Length[r]);
        }
        Assert.Equal(0b111ul, table->Enabled[0]);
        Assert.Equal(6 + 2 * 8, table->StorageBytesPerRow);
    });

    [Fact]
    public void AppendKeepsOrderAndSpillsToTheHeapPastInline() => With([0, 1], 2, 12, p =>
    {
        var table = (KindTable*)p;
        for (ushort i = 0; i < 9; i++) Add(table, 0, 1, i);
        Add(table, 1, 1, 100);
        Assert.Equal(Enumerable.Range(0, 9).Select(i => (ushort)i), Steps(table, 0));
        Assert.Equal([(ushort)100], Steps(table, 1));
        Assert.Equal(9, table->MaxLength);
        Assert.Equal(3, table->Overflows);
        Assert.True(table->IsActive(0));
        Assert.True((table->Spilled[0] & 1ul) != 0);
        Assert.True((table->Spilled[0] & 2ul) == 0);
    });

    [Fact]
    public void InlineZeroRowsSpillThroughTheDensePointerArray() => With([0, 1], 0, 8, p =>
    {
        var table = (KindTable*)p;
        for (ushort i = 0; i < 5; i++) Add(table, 0, 1, i);
        Add(table, 1, 1, 7);
        Assert.Equal(Enumerable.Range(0, 5).Select(i => (ushort)i), Steps(table, 0));
        Assert.Equal([(ushort)7], Steps(table, 1));
        Assert.True((table->Spilled[0] & 1ul) != 0);
        Assert.True((table->Spilled[0] & 2ul) != 0);
        Assert.True(table->RemoveSpan(0, 0, 1, 2));
        Assert.Equal([(ushort)0, (ushort)1, (ushort)3, (ushort)4], Steps(table, 0));
    });

    [Fact]
    public void RemoveSpanKeepsOrderAndClearsActiveWhenEmpty() => With([0], 1, 8, p =>
    {
        var table = (KindTable*)p;
        for (ushort i = 0; i < 4; i++) Add(table, 0, 2, i, KindEntryFlags.Span, (byte)(i % 2));
        Assert.True(table->RemoveSpan(0, 1, 2, 1));
        Assert.False(table->RemoveSpan(0, 1, 2, 1));
        Assert.Equal([(ushort)0, 2, 3], Steps(table, 0));
        table->RemoveSpan(0, 0, 2, 0);
        table->RemoveSpan(0, 0, 2, 2);
        table->RemoveSpan(0, 1, 2, 3);
        Assert.Equal(0, table->Length[0]);
        Assert.False(table->IsActive(0));
    });

    [Fact]
    public void RemovePulsesKeepsSpansAndClearClearsAll() => With([0, 1], 2, 8, p =>
    {
        var table = (KindTable*)p;
        Add(table, 0, 1, 1);
        Add(table, 0, 1, 2, KindEntryFlags.Pulse);
        Add(table, 0, 1, 3);
        Add(table, 0, 1, 4, KindEntryFlags.Pulse);
        table->RemovePulses(0);
        Assert.Equal([(ushort)1, 3], Steps(table, 0));
        Add(table, 1, 1, 5, KindEntryFlags.Pulse);
        table->RemovePulses(1);
        Assert.False(table->IsActive(1));
        table->Clear(0);
        Assert.False(table->IsActive(0));
        Assert.Equal(0, table->Length[0]);
    });

    [Fact]
    public void EnabledBitsToggleIndependentlyOfActive() => With(Enumerable.Range(0, 70).ToArray(), 1, 8, p =>
    {
        var table = (KindTable*)p;
        Assert.Equal(2, table->Words);
        Assert.Equal((1ul << 6) - 1, table->Enabled[1]);
        table->SetEnabled(65, false);
        Assert.Equal((1ul << 6) - 1 & ~(1ul << 1), table->Enabled[1]);
        table->SetEnabled(65, true);
        Assert.Equal((1ul << 6) - 1, table->Enabled[1]);
    });
}
