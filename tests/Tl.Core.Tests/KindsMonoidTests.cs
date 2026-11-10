using Xunit;

namespace Tl.Core.Tests;

public sealed class KindsMonoidTests
{
    sealed class NoIdleIntSumKind : Kind<int>
    {
        public override int Id => 2;
        public override int Order => 2;
        public override int Idle => 0;
        public override bool HasIdle => false;
        public override int Combine(int acc, int value) => acc + value;
        public override void Apply(in int value, ref int column) => column = value;
    }

    [Fact]
    public void IntSumCombineIsOrderIndependent()
    {
        var first = new KindTable<int>(new IntSumKind(), 4, 64);
        first.Add(1, 10);
        first.Add(1, 20);
        first.Add(1, 30);
        first.Add(2, 5);
        first.Add(2, 7);

        var second = new KindTable<int>(new IntSumKind(), 4, 64);
        second.Add(2, 7);
        second.Add(1, 30);
        second.Add(2, 5);
        second.Add(1, 10);
        second.Add(1, 20);

        Assert.Equal(60, first[1]);
        Assert.Equal(60, second[1]);
        Assert.Equal(12, first[2]);
        Assert.Equal(12, second[2]);
        Assert.Equal(0, first[0]);
        Assert.Equal(0, second[0]);

        var kind = new IntSumKind();
        Assert.Equal(0, kind.Idle);
        Assert.Equal(30, kind.Combine(kind.Combine(kind.Idle, 10), 20));
        Assert.Equal(kind.Combine(kind.Combine(kind.Idle, 10), 20), kind.Combine(kind.Combine(kind.Idle, 20), 10));
    }

    [Fact]
    public void TouchedIterationReturnsExactlyAddedRowsSorted()
    {
        var table = new KindTable<int>(new IntSumKind(), 8, 64);
        table.Add(5, 50);
        table.Add(2, 20);
        table.Add(7, 70);
        table.Add(2, 1);
        table.Add(0, 3);

        var rows = new int[4];
        var values = new int[4];
        var count = 0;
        table.IterateTouched((row, value) =>
        {
            rows[count] = row;
            values[count] = value;
            count++;
        });

        Assert.Equal(4, count);
        Assert.Equal(4, table.TouchedCount);
        Assert.Equal(new[] { 0, 2, 5, 7 }, rows);
        Assert.Equal(new[] { 3, 21, 50, 70 }, values);
        Assert.True(table.IsTouched(0));
        Assert.True(table.IsTouched(2));
        Assert.True(table.IsTouched(5));
        Assert.True(table.IsTouched(7));
        Assert.False(table.IsTouched(1));
        Assert.False(table.IsTouched(3));
        Assert.False(table.IsTouched(4));
        Assert.False(table.IsTouched(6));
    }

    [Fact]
    public void TouchedIterationCrossesWordBoundary()
    {
        var table = new KindTable<int>(new IntSumKind(), 130, 64);
        table.Add(127, 1);
        table.Add(64, 2);
        table.Add(0, 3);
        table.Add(65, 4);
        table.Add(63, 5);

        var rows = new int[5];
        var count = 0;
        table.IterateTouched((row, _) =>
        {
            rows[count] = row;
            count++;
        });

        Assert.Equal(5, count);
        Assert.Equal(new[] { 0, 63, 64, 65, 127 }, rows);
    }

    [Fact]
    public void EntriesArenaRewindsToZero()
    {
        var table = new KindTable<int>(new IntSumKind(), 4, 16);
        table.Add(1, 10);
        table.Add(2, 20);
        table.Add(1, 5);

        Assert.Equal(16, table.Entries.Capacity);
        Assert.Equal(3, table.Entries.Length);
        Assert.Equal(1, table.Entries.RowAt(0));
        Assert.Equal(10, table.Entries.ValueAt(0));
        Assert.Equal(2, table.Entries.RowAt(1));
        Assert.Equal(20, table.Entries.ValueAt(1));
        Assert.Equal(1, table.Entries.RowAt(2));
        Assert.Equal(5, table.Entries.ValueAt(2));

        table.Entries.Rewind();

        Assert.Equal(0, table.Entries.Length);
    }

    [Fact]
    public void ResetRestoresIdleClearsTouchedAndRewindsEntries()
    {
        var table = new KindTable<int>(new IntSumKind(), 4, 16);
        table.Add(1, 10);
        table.Add(2, 20);

        table.Reset();

        Assert.Equal(0, table.Entries.Length);
        Assert.Equal(0, table.TouchedCount);
        Assert.False(table.IsTouched(1));
        Assert.False(table.IsTouched(2));
        Assert.Equal(0, table[1]);
        Assert.Equal(0, table[2]);

        table.Add(1, 4);
        Assert.Equal(4, table[1]);
        Assert.True(table.IsTouched(1));
        Assert.Equal(1, table.Entries.Length);
    }

    [Fact]
    public void ApplyAllWritesIdleToUntouchedWhenDeclared()
    {
        var table = new KindTable<int>(new IntSumKind(), 4, 16);
        table.Add(2, 7);

        var columns = new[] { 999, 999, 999, 999 };
        table.ApplyAll(columns);

        Assert.Equal(new[] { 0, 0, 7, 0 }, columns);
    }

    [Fact]
    public void ApplyAllLeavesUntouchedWhenIdleNotDeclared()
    {
        Assert.False(new NoIdleIntSumKind().HasIdle);

        var table = new KindTable<int>(new NoIdleIntSumKind(), 4, 16);
        table.Add(1, 5);

        var columns = new[] { 999, 999, 999, 999 };
        table.ApplyAll(columns);

        Assert.Equal(new[] { 999, 5, 999, 999 }, columns);
    }

    [Fact]
    public void FloatSumKindAccumulatesAndApplies()
    {
        var kind = new FloatSumKind();
        Assert.Equal(0f, kind.Idle);
        Assert.Equal(4f, kind.Combine(1.5f, 2.5f));
        Assert.True(kind.HasIdle);

        var table = new KindTable<float>(kind, 3, 16);
        table.Add(0, 1.5f);
        table.Add(0, 2.5f);
        table.Add(2, -1f);

        var columns = new[] { 99f, 99f, 99f };
        table.ApplyAll(columns);

        Assert.Equal(new[] { 4f, 0f, -1f }, columns);
    }

    [Fact]
    public void KindsHaveStableIdentityAndOrder()
    {
        var intKind = new IntSumKind();
        var floatKind = new FloatSumKind();

        Assert.Equal(0, intKind.Id);
        Assert.Equal(1, floatKind.Id);
        Assert.Equal(0, intKind.Order);
        Assert.Equal(1, floatKind.Order);
        Assert.NotEqual(intKind.Id, floatKind.Id);
        Assert.NotEqual(intKind.Order, floatKind.Order);
        Assert.True(intKind.HasIdle);
        Assert.True(floatKind.HasIdle);
    }
}
