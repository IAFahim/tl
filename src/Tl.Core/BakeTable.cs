using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

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
        public int Next;
        public int ParamCount;
        public ulong* ParamKeys;
        public delegate*<byte**, void> Invoke;
    }

    struct Slot { public ulong Key; public int Head, Tail; }

    internal const int SlotCount = 1024;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static ulong _slotsBase = (ulong)NativeMemory.AlignedAlloc((nuint)(8 + sizeof(Slot) * SlotCount), 64);
    static ulong _entriesBase = (ulong)NativeMemory.AlignedAlloc((nuint)(sizeof(Entry) * SlotCount), 64);
    static int _entryCapacity = SlotCount;
    static volatile int _gate;
    static int _bakes;

    static BakeTable()
    {
        Unsafe.InitBlock((void*)_slotsBase, 0, (uint)(8 + sizeof(Slot) * SlotCount));
        *(int*)_slotsBase = SlotCount;
    }

    static byte* Slots => (byte*)Volatile.Read(ref _slotsBase);
    internal static int SlotCapacity => *(int*)Slots;
    internal static Entry* EntryAt => (Entry*)Volatile.Read(ref _entriesBase);
    internal static int Count => Volatile.Read(ref _bakes);

    internal static void Install(ulong pairKey, delegate*<byte**, void> invoke, ReadOnlySpan<ulong> paramKeys)
    {
        while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0) Thread.Yield();
        try
        {
            if (PairTable._sealed != 0) Fail.Sealed();
            var slot = Probe(pairKey);
            while (slot < 0)
            {
                GrowSlots();
                slot = Probe(pairKey);
            }
            var slots = (Slot*)(Slots + 8ul);
            if (slots[slot].Key == 0)
            {
                slots[slot].Head = -1;
                slots[slot].Tail = -1;
                Volatile.Write(ref slots[slot].Key, pairKey);
            }
            if (_bakes == _entryCapacity)
            {
                var grown = (Entry*)NativeMemory.AlignedAlloc((nuint)sizeof(Entry) * (nuint)_entryCapacity * 2, 64);
                var copy = sizeof(Entry) * _bakes;
                Buffer.MemoryCopy(EntryAt, grown, copy, copy);
                _entryCapacity *= 2;
                Volatile.Write(ref _entriesBase, (ulong)grown);
            }
            var entry = EntryAt + _bakes;
            entry->Next = -1;
            entry->ParamCount = paramKeys.Length;
            if (paramKeys.Length > 0)
            {
                entry->ParamKeys = (ulong*)NativeMemory.Alloc((nuint)paramKeys.Length * sizeof(ulong));
                paramKeys.CopyTo(new Span<ulong>(entry->ParamKeys, paramKeys.Length));
            }
            else entry->ParamKeys = null;
            entry->Invoke = invoke;
            if (slots[slot].Tail < 0) Volatile.Write(ref slots[slot].Head, _bakes);
            else Volatile.Write(ref EntryAt[slots[slot].Tail].Next, _bakes);
            slots[slot].Tail = _bakes;
            Volatile.Write(ref _bakes, _bakes + 1);
        }
        finally
        {
            _gate = 0;
        }
    }

    internal static int HeadOf(ulong pairKey)
    {
        var slot = Probe(pairKey);
        if (slot < 0) return -1;
        var slots = (Slot*)(Slots + 8ul);
        return slots[slot].Key == 0 ? -1 : Volatile.Read(ref slots[slot].Head);
    }

    internal static int ChainLength(ulong pairKey)
    {
        var length = 0;
        for (var entry = HeadOf(pairKey); entry >= 0; entry = EntryAt[entry].Next) length++;
        return length;
    }

    internal static Entry* At(ulong pairKey, int index)
    {
        if (index < 0) throw BakeIndex(index);
        for (var entry = HeadOf(pairKey); entry >= 0; entry = EntryAt[entry].Next)
        {
            if (index == 0) return EntryAt + entry;
            index--;
        }
        throw BakeIndex(index);
    }

    [SuppressMessage("ReSharper", "EntityNameCapturedOnly.Local", Justification = "paramName via nameof is the diagnostic convention")]
    static ArgumentOutOfRangeException BakeIndex(int index)
        => new(nameof(index), "Bake index is outside the registered bake chain for this timeline pair.");

    static void GrowSlots()
    {
        var old = Slots;
        var oldCap = *(int*)old;
        var cap = oldCap * 2;
        var grown = (byte*)NativeMemory.AlignedAlloc((nuint)(8 + sizeof(Slot) * cap), 64);
        Unsafe.InitBlock(grown, 0, (uint)(8 + sizeof(Slot) * cap));
        *(int*)grown = cap;
        var from = (Slot*)(old + 8ul);
        var to = (Slot*)(grown + 8ul);
        for (var i = 0; i < oldCap; i++)
        {
            if (from[i].Key == 0) continue;
            var s = (int)(from[i].Key & (ulong)(cap - 1));
            while (to[s].Key != 0) s = (s + 1) & (cap - 1);
            to[s] = from[i];
        }
        Volatile.Write(ref _slotsBase, (ulong)grown);
    }

    static int Probe(ulong key)
    {
        var block = Slots;
        var slots = (Slot*)(block + 8ul);
        var cap = *(int*)block;
        var slot = (int)key & (cap - 1);
        for (var walk = 0; walk < cap; walk++)
        {
            var candidate = Volatile.Read(ref slots[slot].Key);
            if (candidate == 0 || candidate == key) return slot;
            slot = (slot + 1) & (cap - 1);
        }
        return -1;
    }
}
