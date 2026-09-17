using System.Runtime.InteropServices;

namespace HandleInterning;

[StructLayout(LayoutKind.Sequential, Size = 48)]
unsafe struct FoldedHeader
{
    public ushort Duration;
    public byte Looping;
    public byte Reserved;
    public int EntryBytes;
    public float Checksum;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct LaneRecord
{
    public float Effect;
    public ushort Next;
}

public static unsafe class PairFolds
{
    public const int HeaderBytes = 48;
    public const int BytesPerPosition = 28;

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    struct Slot
    {
        public nint Owner;
        public ulong Pair;
        public ulong Generation;
        public nint Published;
    }

    const int Capacity = 4096;
    static readonly Slot* Slots = (Slot*)System.Runtime.InteropServices.NativeMemory.AlignedAlloc((nuint)Capacity * (nuint)sizeof(Slot), 64);
    static int _gate;
    static long _foldAllocBytes;
    static long _foldFreeBytes;
    static long _foldAllocCount;
    static long _foldFreeCount;
    static long _foldCount;

    static PairFolds() => new Span<Slot>(Slots, Capacity).Clear();

    public static long FoldCount => Volatile.Read(ref _foldCount);
    public static long FoldAllocBytes => Volatile.Read(ref _foldAllocBytes);
    public static long FoldFreeBytes => Volatile.Read(ref _foldFreeBytes);
    public static long FoldAllocCount => Volatile.Read(ref _foldAllocCount);
    public static long FoldFreeCount => Volatile.Read(ref _foldFreeCount);

    public static nint GetPublished(Asset asset, ulong pair)
    {
        var entry = asset.Entry;
        if (entry == null) return 0;
        var owner = (nint)entry;
        var generation = (ulong)entry->Count >> 32;
        var mask = Capacity - 1;
        var index = (int)(Hash(owner, pair) & (ulong)mask);
        for (var n = 0; n < Capacity; n++)
        {
            ref var slot = ref Slots[(index + n) & mask];
            if (slot.Owner == owner && slot.Generation == generation && slot.Pair == pair) return Volatile.Read(ref slot.Published);
            if (slot.Owner == 0) return 0;
        }
        return 0;
    }

    public static nint Fold(Asset asset, ulong pair)
    {
        AcquireGate();
        try
        {
            var published = GetPublished(asset, pair);
            if (published != 0) return published;
            var table = Build(asset, pair);
            var entry = asset.Entry;
            var slot = ClaimSlot((nint)entry, pair, (ulong)entry->Count >> 32);
            slot->Owner = (nint)entry;
            slot->Pair = pair;
            slot->Generation = (ulong)entry->Count >> 32;
            Volatile.Write(ref slot->Published, (nint)table | 1);
            return (nint)table | 1;
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    public static float ChecksumOf(Asset asset, ulong pair)
    {
        var published = GetPublished(asset, pair);
        if (published == 0) return float.NaN;
        return ((FoldedHeader*)(byte*)(published & ~1))->Checksum;
    }

    public static void ResetAll()
    {
        AcquireGate();
        try
        {
            for (var i = 0; i < Capacity; i++)
            {
                ref var slot = ref Slots[i];
                var published = Volatile.Read(ref slot.Published);
                if (published != 0)
                {
                    var table = (byte*)(published & ~1);
                    var bytes = ((FoldedHeader*)table)->EntryBytes;
                    NativeMemory.AlignedFree(table);
                    Interlocked.Add(ref _foldFreeBytes, bytes);
                    Interlocked.Increment(ref _foldFreeCount);
                }
                slot = default;
            }
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    static Slot* ClaimSlot(nint owner, ulong pair, ulong generation)
    {
        var mask = Capacity - 1;
        var index = (int)(Hash(owner, pair) & (ulong)mask);
        for (var n = 0; n < Capacity; n++)
        {
            var slot = Slots + ((index + n) & mask);
            if (slot->Owner == 0) return slot;
        }
        throw new InvalidOperationException($"Fold slot table exhausted: {Capacity} slots in use.");
    }

    static byte* Build(Asset asset, ulong pair)
    {
        var len = asset.Length;
        var block = asset.Block;
        var duration = (ushort)(1024 + (block[0] % 512));
        var n = duration + 1;
        var total = HeaderBytes + n * BytesPerPosition;
        var table = (byte*)NativeMemory.AlignedAlloc((nuint)total, 64);
        var forward = (float*)(table + HeaderBytes);
        var backward = forward + n;
        var forwardRecords = (LaneRecord*)(backward + n);
        var backwardRecords = forwardRecords + n;
        var backwardByPosition = (float*)(backwardRecords + n);
        var checksum = 0f;
        for (var p = 0u; p < (uint)n; p++)
        {
            var f = Effect(block, len, pair, p);
            var previous = p == 0 ? 0u : p - 1;
            var b = Effect(block, len, pair, previous);
            forward[p] = f;
            backward[p] = b;
            forwardRecords[p] = new LaneRecord { Effect = f, Next = p == duration ? (ushort)0 : (ushort)(p + 1) };
            backwardRecords[p] = new LaneRecord { Effect = b, Next = (ushort)previous };
            backwardByPosition[p] = b;
            checksum += f + b;
        }
        ((FoldedHeader*)table)->Duration = duration;
        ((FoldedHeader*)table)->Looping = 1;
        ((FoldedHeader*)table)->EntryBytes = total;
        ((FoldedHeader*)table)->Checksum = checksum;
        Interlocked.Add(ref _foldAllocBytes, total);
        Interlocked.Increment(ref _foldAllocCount);
        Interlocked.Increment(ref _foldCount);
        return table;
    }

    static float Effect(byte* block, int len, ulong pair, uint pos)
    {
        var i = (int)((pos * 2654435761u + (uint)pair) % (uint)len);
        var j = (int)((pos * 40503u + (uint)(pair >> 32)) % (uint)len);
        var v = block[i] * 256 + block[j];
        return v / 32768f * 2f - 1f;
    }

    static void AcquireGate()
    {
        var spins = 0;
        while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0)
        {
            Thread.SpinWait(64);
            if (++spins >= 4096)
            {
                spins = 0;
                Thread.Yield();
            }
        }
    }

    static ulong Hash(nint owner, ulong pair)
    {
        var h = (ulong)owner ^ (pair * 0x9E3779B97F4A7C15UL) + 0xA0761D6478BD642FUL;
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        return h;
    }
}

public static unsafe class TypedLane
{
    public sealed class LaneSnapshot
    {
        public readonly Asset Owner;
        public readonly ulong Pair;
        public readonly LaneRecord* Records;
        public readonly ushort Duration;

        public LaneSnapshot(Asset owner, ulong pair, LaneRecord* records, ushort duration)
        {
            Owner = owner;
            Pair = pair;
            Records = records;
            Duration = duration;
        }
    }

    static LaneSnapshot? _snapshot;

    public static ushort Duration => _snapshot?.Duration ?? 0;

    public static float Advance(Asset asset, ulong pair, ref ushort pos)
    {
        var s = Volatile.Read(ref _snapshot);
        if (s == null || s.Owner != asset || s.Pair != pair) s = Resolve(asset, pair);
        var rec = &s.Records[pos];
        pos = rec->Next;
        return rec->Effect;
    }

    public static float AdvanceDirect(ref ushort pos)
    {
        var s = _snapshot!;
        var rec = &s.Records[pos];
        pos = rec->Next;
        return rec->Effect;
    }

    public static float AdvanceIndirect(Asset asset, ulong pair, ref ushort pos)
    {
        var table = (byte*)(PairFolds.GetPublished(asset, pair) & ~1);
        var n = ((FoldedHeader*)table)->Duration + 1;
        var rec = (LaneRecord*)((float*)(table + PairFolds.HeaderBytes) + 2 * n) + pos;
        pos = rec->Next;
        return rec->Effect;
    }

    static LaneSnapshot Resolve(Asset asset, ulong pair)
    {
        var published = PairFolds.GetPublished(asset, pair);
        if (published == 0) published = PairFolds.Fold(asset, pair);
        var table = (byte*)(published & ~1);
        var header = (FoldedHeader*)table;
        var n = header->Duration + 1;
        var snapshot = new LaneSnapshot(asset, pair, (LaneRecord*)((float*)(table + PairFolds.HeaderBytes) + 2 * n), header->Duration);
        Volatile.Write(ref _snapshot, snapshot);
        return snapshot;
    }
}
