using System.Runtime.InteropServices;

namespace Kinds;

[Flags]
public enum EntryFlags : byte
{
    Span = 1,
    Pulse = 2,
    Synthetic = 4,
    Interrupted = 8,
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct Entry
{
    public ushort Timeline;
    public ushort Step;
    public byte Slot;
    public byte Feeder;
    public EntryFlags Flags;
    public byte Reserved;
}

public sealed unsafe class KindTable : IDisposable
{
    public readonly byte Kind;
    public readonly int Rows;
    public readonly int Inline;
    public readonly int Stride;
    public readonly int Words;
    public readonly int* Entity;
    public readonly byte* InlineData;
    public readonly ushort* Length;
    public readonly ushort* Capacity;
    public readonly byte** Heap;
    public readonly ulong* Active;
    public readonly ulong* Enabled;
    public int MaxLength;
    public int Overflows;

    public KindTable(byte kind, ReadOnlySpan<int> members, int inlineCapacity, int valueBytes)
    {
        Kind = kind;
        Rows = members.Length;
        Inline = inlineCapacity;
        Stride = 8 + ((valueBytes + 3) & ~3);
        Words = (Rows + 63) >> 6;
        Entity = (int*)Zeroed((nuint)Rows * 4);
        for (var r = 0; r < Rows; r++) Entity[r] = members[r];
        InlineData = (byte*)Zeroed((nuint)Rows * (nuint)Inline * (nuint)Stride);
        Length = (ushort*)Zeroed((nuint)Rows * 2);
        Capacity = (ushort*)Zeroed((nuint)Rows * 2);
        for (var r = 0; r < Rows; r++) Capacity[r] = (ushort)Inline;
        Heap = (byte**)Zeroed((nuint)Rows * (nuint)sizeof(nint));
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

    public byte* Data(int row) => Heap[row] != null ? Heap[row] : InlineData + (nuint)row * (nuint)Inline * (nuint)Stride;

    public Entry* At(int row, int index) => (Entry*)(Data(row) + (nuint)index * (nuint)Stride);

    public Entry* Append(int row)
    {
        var length = Length[row];
        if (length == Capacity[row]) GrowRow(row);
        Length[row] = (ushort)(length + 1);
        if (length + 1 > MaxLength) MaxLength = length + 1;
        Active[row >> 6] |= 1ul << (row & 63);
        return At(row, length);
    }

    void GrowRow(int row)
    {
        var capacity = Math.Max(4, Capacity[row] * 2);
        var heap = (byte*)NativeMemory.Alloc((nuint)capacity * (nuint)Stride);
        Buffer.MemoryCopy(Data(row), heap, (long)capacity * Stride, (long)Length[row] * Stride);
        if (Heap[row] != null) NativeMemory.Free(Heap[row]);
        Heap[row] = heap;
        Capacity[row] = (ushort)capacity;
        Overflows++;
    }

    public bool RemoveSpan(int row, byte slot, ushort timeline, ushort step)
    {
        var length = Length[row];
        var data = Data(row);
        for (var i = 0; i < length; i++)
        {
            var e = (Entry*)(data + (nuint)i * (nuint)Stride);
            if ((e->Flags & EntryFlags.Span) == 0 || e->Slot != slot || e->Timeline != timeline || e->Step != step) continue;
            Buffer.MemoryCopy(data + (nuint)(i + 1) * (nuint)Stride, data + (nuint)i * (nuint)Stride, (long)(length - i - 1) * Stride, (long)(length - i - 1) * Stride);
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
            var e = (Entry*)(data + (nuint)i * (nuint)Stride);
            if ((e->Flags & EntryFlags.Pulse) != 0) continue;
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
        for (var r = 0; r < Rows; r++)
            if (Heap[r] != null) NativeMemory.Free(Heap[r]);
        NativeMemory.AlignedFree(Entity);
        NativeMemory.AlignedFree(InlineData);
        NativeMemory.AlignedFree(Length);
        NativeMemory.AlignedFree(Capacity);
        NativeMemory.AlignedFree(Heap);
        NativeMemory.AlignedFree(Active);
        NativeMemory.AlignedFree(Enabled);
    }
}
