using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public static unsafe class BakeRuntime<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    public static void Bake(delegate*<byte**, void> invoke, params ReadOnlySpan<ulong> paramKeys)
        => BakeTable.Install(PairRuntime<TTrack, TClip>.Key, invoke, paramKeys);

    public static int BakeCount => BakeTable.ChainLength(PairRuntime<TTrack, TClip>.Key);

    public static int BakeParameterCount(int index) => Entry(index)->ParamCount;

    public static ulong BakeParameterKey(int index, int parameter)
    {
        var entry = Entry(index);
        if ((uint)parameter >= (uint)entry->ParamCount)
            throw new ArgumentOutOfRangeException(nameof(parameter));
        return entry->ParamKeys[parameter];
    }

    static BakeTable.Entry* Entry(int index) => BakeTable.At(PairRuntime<TTrack, TClip>.Key, index);
}

static unsafe class BakeTable
{
    internal struct Entry
    {
        public int Next, Pair;
        public int ParamCount;
        public ulong* ParamKeys;
        public delegate*<byte**, void> Invoke;
    }

    struct Slot { public ulong Key; public int Head, Tail; }

    const int SlotCount = 256, Capacity = 1024;
    static readonly byte* _block = (byte*)NativeMemory.AlignedAlloc((nuint)(sizeof(Slot) * SlotCount + sizeof(Entry) * Capacity), 64);
    static volatile int _gate;
    static int _bakes;

    static BakeTable() => Unsafe.InitBlock(_block, 0, (uint)(sizeof(Slot) * SlotCount));

    static Slot* SlotAt => (Slot*)_block;
    internal static Entry* EntryAt => (Entry*)(_block + sizeof(Slot) * SlotCount);
    internal static int Count => Volatile.Read(ref _bakes);

    internal static void Install(ulong pairKey, delegate*<byte**, void> invoke, ReadOnlySpan<ulong> paramKeys)
    {
        while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0) Thread.Yield();
        try
        {
            var slot = Probe(pairKey);
            var slots = SlotAt;
            if (slots[slot].Key == 0)
            {
                slots[slot].Head = -1;
                slots[slot].Tail = -1;
                Volatile.Write(ref slots[slot].Key, pairKey);
            }
            if (_bakes == Capacity) throw new InvalidOperationException("Bake capacity exhausted.");
            var entry = EntryAt + _bakes;
            entry->Next = -1;
            entry->Pair = slot;
            entry->ParamCount = paramKeys.Length;
            if (paramKeys.Length > 0)
            {
                entry->ParamKeys = (ulong*)NativeMemory.Alloc((nuint)paramKeys.Length * sizeof(ulong));
                paramKeys.CopyTo(new Span<ulong>(entry->ParamKeys, paramKeys.Length));
            }
            else entry->ParamKeys = null;
            entry->Invoke = invoke;
            if (slots[slot].Tail < 0) slots[slot].Head = _bakes;
            else EntryAt[slots[slot].Tail].Next = _bakes;
            slots[slot].Tail = _bakes;
            Volatile.Write(ref _bakes, _bakes + 1);
        }
        finally
        {
            _gate = 0;
        }
    }

    internal static int Head(ulong pairKey)
    {
        var slots = SlotAt;
        var slot = Probe(pairKey);
        return slots[slot].Key == 0 ? -1 : Volatile.Read(ref slots[slot].Head);
    }

    internal static int ChainLength(ulong pairKey)
    {
        var length = 0;
        for (var entry = Head(pairKey); entry >= 0; entry = EntryAt[entry].Next) length++;
        return length;
    }

    internal static Entry* At(ulong pairKey, int index)
    {
        if (index < 0) throw BakeIndex();
        for (var entry = Head(pairKey); entry >= 0; entry = EntryAt[entry].Next)
        {
            if (index == 0) return EntryAt + entry;
            index--;
        }
        throw BakeIndex();
    }

    static ArgumentOutOfRangeException BakeIndex()
        => new("index", "Bake index is outside the registered bake chain for this timeline pair.");

    static int Probe(ulong key)
    {
        var slots = SlotAt;
        var slot = (int)key & (SlotCount - 1);
        while (true)
        {
            var candidate = Volatile.Read(ref slots[slot].Key);
            if (candidate == 0 || candidate == key) return slot;
            slot = (slot + 1) & (SlotCount - 1);
        }
    }
}
