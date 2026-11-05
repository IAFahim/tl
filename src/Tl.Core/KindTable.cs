using System.Runtime.InteropServices;

namespace Tl;

[Flags]
internal enum KindEntryFlags : byte
{
    Span = 1,
    Pulse = 2,
    Synthetic = 4,
    Interrupted = 8,
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
internal struct KindEntry
{
    public ushort Timeline;
    public ushort Step;
    public byte Slot;
    public byte Feeder;
    public KindEntryFlags Flags;
    public byte Reserved;
}

internal sealed unsafe class KindTable : IDisposable
{
    public readonly int Kind;
    public readonly int Rows;
    public readonly int Inline;
    public readonly int Stride;
    public readonly int Words;
    public readonly int* Entity;
    readonly byte* _inline;
    public readonly ushort* Length;
    readonly ushort* _capacity;
    readonly byte** _heap;
    public readonly ulong* Active;
    public readonly ulong* Enabled;
    public int MaxLength;
    public int Overflows;
    bool _disposed;

    public KindTable(int kind, ReadOnlySpan<int> entities, int inlineCapacity, int entryBytes)
    {
        Kind = kind;
        Rows = entities.Length;
        Inline = inlineCapacity;
        Stride = entryBytes;
        Words = (Rows + 63) >> 6;
        Entity = (int*)Zeroed((nuint)Rows * 4);
        for (var r = 0; r < Rows; r++) Entity[r] = entities[r];
        _inline = (byte*)Zeroed((nuint)Rows * (nuint)Inline * (nuint)Stride);
        Length = (ushort*)Zeroed((nuint)Rows * 2);
        _capacity = (ushort*)Zeroed((nuint)Rows * 2);
        for (var r = 0; r < Rows; r++) _capacity[r] = (ushort)Inline;
        _heap = (byte**)Zeroed((nuint)Rows * (nuint)sizeof(nint));
        Active = (ulong*)Zeroed((nuint)Words * 8);
        Enabled = (ulong*)Zeroed((nuint)Words * 8);
        for (var w = 0; w < Words; w++) Enabled[w] = ~0ul;
        if ((Rows & 63) != 0) Enabled[Words - 1] = (1ul << (Rows & 63)) - 1;
    }

    static void* Zeroed(nuint bytes)
    {
        var p = NativeMemory.AlignedAlloc(bytes == 0 ? 1 : bytes, 64);
        NativeMemory.Clear(p, bytes);
        return p;
    }

    public long StorageBytesPerRow => 16 + (long)Inline * Stride;

    public bool IsActive(int row) => (Active[row >> 6] & (1ul << (row & 63))) != 0;

    public byte* Data(int row) => _heap[row] != null ? _heap[row] : _inline + (nuint)row * (nuint)Inline * (nuint)Stride;

    public KindEntry* At(int row, int index) => (KindEntry*)(Data(row) + (nuint)index * (nuint)Stride);

    public KindEntry* Append(int row)
    {
        var length = Length[row];
        if (length == _capacity[row]) Grow(row);
        Length[row] = (ushort)(length + 1);
        if (length + 1 > MaxLength) MaxLength = length + 1;
        Active[row >> 6] |= 1ul << (row & 63);
        return At(row, length);
    }

    void Grow(int row)
    {
        var capacity = Math.Max(4, _capacity[row] * 2);
        if (capacity > ushort.MaxValue) throw new InvalidOperationException($"kind {Kind} row {row} holds more than 65,535 entries.");
        var heap = (byte*)NativeMemory.AlignedAlloc((nuint)capacity * (nuint)Stride, 8);
        NativeMemory.Clear(heap, (nuint)capacity * (nuint)Stride);
        new ReadOnlySpan<byte>(Data(row), Length[row] * Stride).CopyTo(new Span<byte>(heap, capacity * Stride));
        if (_heap[row] != null) NativeMemory.AlignedFree(_heap[row]);
        _heap[row] = heap;
        _capacity[row] = (ushort)capacity;
        Overflows++;
    }

    public bool RemoveSpan(int row, byte slot, ushort timeline, ushort step)
    {
        var length = Length[row];
        var data = Data(row);
        for (var i = 0; i < length; i++)
        {
            var e = (KindEntry*)(data + (nuint)i * (nuint)Stride);
            if ((e->Flags & KindEntryFlags.Span) == 0 || e->Slot != slot || e->Timeline != timeline || e->Step != step) continue;
            var tail = (length - i - 1) * Stride;
            Buffer.MemoryCopy(data + (nuint)(i + 1) * (nuint)Stride, data + (nuint)i * (nuint)Stride, tail, tail);
            Length[row] = (ushort)(length - 1);
            if (length == 1) Active[row >> 6] &= ~(1ul << (row & 63));
            return true;
        }
        return false;
    }

    public void RemovePulses(int row)
    {
        var length = Length[row];
        var data = Data(row);
        var kept = 0;
        for (var i = 0; i < length; i++)
        {
            var e = (KindEntry*)(data + (nuint)i * (nuint)Stride);
            if ((e->Flags & KindEntryFlags.Pulse) != 0) continue;
            if (kept != i) Buffer.MemoryCopy(e, data + (nuint)kept * (nuint)Stride, Stride, Stride);
            kept++;
        }
        Length[row] = (ushort)kept;
        if (kept == 0) Active[row >> 6] &= ~(1ul << (row & 63));
    }

    public void Clear(int row)
    {
        Length[row] = 0;
        Active[row >> 6] &= ~(1ul << (row & 63));
    }

    public void SetEnabled(int row, bool value)
    {
        if (value) Enabled[row >> 6] |= 1ul << (row & 63);
        else Enabled[row >> 6] &= ~(1ul << (row & 63));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (var r = 0; r < Rows; r++)
            if (_heap[r] != null) NativeMemory.AlignedFree(_heap[r]);
        NativeMemory.AlignedFree(Entity);
        NativeMemory.AlignedFree(_inline);
        NativeMemory.AlignedFree(Length);
        NativeMemory.AlignedFree(_capacity);
        NativeMemory.AlignedFree(_heap);
        NativeMemory.AlignedFree(Active);
        NativeMemory.AlignedFree(Enabled);
        GC.SuppressFinalize(this);
    }
}
