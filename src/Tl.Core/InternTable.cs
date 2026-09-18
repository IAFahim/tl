using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Tl;

static unsafe class TimelineTable
{
    const int InitialCapacity = 1024;
    const int MaxCapacity = 65536;
    const int StateEmpty = 0;
    const int StateLive = 1;
    const int StateDead = 2;
    const long LowMask = 0xFFFFFFFFL;

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    struct Entry
    {
        public ulong KeyLow;
        public ulong KeyHigh;
        public byte* Block;
        public nint Bytes;
        public long Count;
        public int State;
        public ushort Id;
    }

    static byte* _block;
    static int _capacity;
    static int _gate;
    static int _used;
    static int _live;
    static int _nextId;
    static long _allocBytes;
    static long _freeBytes;
    static long _allocCount;
    static long _freeCount;
    static long _peakLiveBytes;
    static long _graveyardBytes;
    static long _graveyardCount;
    static long _deaths;
    static long _revivals;
    static long _overReleases;
    static long _releases;
    static long _publishes;
    static nint _graveyard;

    static TimelineTable() => Allocate(InitialCapacity);

    static Entry* Entries => (Entry*)_block;
    static int* ById => (int*)(_block + _capacity * sizeof(Entry));
    static int Mask => _capacity - 1;

    internal static int LiveEntries => Volatile.Read(ref _live);
    internal static int DistinctContents => Volatile.Read(ref _nextId);
    internal static long AllocCount => Volatile.Read(ref _allocCount);
    internal static long FreeCount => Volatile.Read(ref _freeCount);
    internal static long AllocBytes => Volatile.Read(ref _allocBytes);
    internal static long FreeBytes => Volatile.Read(ref _freeBytes);
    internal static long PeakLiveBytes => Volatile.Read(ref _peakLiveBytes);
    internal static long GraveyardBlocks => Volatile.Read(ref _graveyardCount);
    internal static long GraveyardBytes => Volatile.Read(ref _graveyardBytes);
    internal static long Deaths => Volatile.Read(ref _deaths);
    internal static long Revivals => Volatile.Read(ref _revivals);
    internal static long OverReleases => Volatile.Read(ref _overReleases);
    internal static long Releases => Volatile.Read(ref _releases);
    internal static long Publishes => Volatile.Read(ref _publishes);

    internal static ushort Load(ReadOnlySpan<byte> baked)
    {
        TimelineRef.Validate(baked);
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(baked, digest);
        var low = BitConverter.ToUInt64(digest);
        var high = BitConverter.ToUInt64(digest.Slice(8));
        AcquireGate();
        try
        {
            if (Volatile.Read(ref _graveyard) != 0) DrainGraveyard();
            var entry = Find(low, high, baked, out var empty);
            if (entry != null)
            {
                if (Volatile.Read(ref entry->State) == StateLive)
                {
                    Interlocked.Increment(ref entry->Count);
                    return entry->Id;
                }
                return Revive(entry, baked);
            }
            if (_nextId >= MaxCapacity)
                throw new InvalidOperationException($"Timeline index domain exhausted: {MaxCapacity} distinct timeline contents over the process lifetime; indices are content identities and never move to other content.");
            if ((_used + 1) * 4 > _capacity * 3)
            {
                if (_capacity == MaxCapacity)
                    throw new InvalidOperationException($"Intern table exhausted: {_used} distinct timeline contents occupy the {_capacity}-slot table.");
                Grow();
                Find(low, high, baked, out empty);
            }
            return Publish(empty, low, high, baked);
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    internal static TimelineRef Reference(ushort index)
    {
        AcquireGate();
        try
        {
            if (TryLive(index, out var block))
                return new TimelineRef(block);
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
        throw NotLoaded(index);
    }

    internal static bool IsLive(ushort index)
    {
        AcquireGate();
        try
        {
            return TryLive(index, out _);
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    internal static void Pin(ushort index, out long generation)
    {
        generation = 0;
        AcquireGate();
        try
        {
            if (TryLive(index, out var block))
            {
                var entry = Entries + Volatile.Read(ref ById[index]);
                generation = Volatile.Read(ref entry->Count) >> 32;
                return;
            }
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
        throw NotLoaded(index);
    }

    static bool TryLive(ushort index, out byte* block)
    {
        block = null;
        if (index >= (uint)Volatile.Read(ref _nextId)) return false;
        var slot = Volatile.Read(ref ById[index]);
        if (slot < 0) return false;
        var entry = Entries + slot;
        if (Volatile.Read(ref entry->State) != StateLive || entry->Id != index) return false;
        block = entry->Block;
        return block != null;
    }

    static ArgumentException NotLoaded(ushort index)
        => new($"Timeline index {index} is not loaded; indices come from TimelineAsset.Load and die when every acquisition is disposed.");

    internal static void Release(ushort index, long generation)
    {
        Interlocked.Increment(ref _releases);
        AcquireGate();
        try
        {
            if (index >= (uint)Volatile.Read(ref _nextId))
            {
                Interlocked.Increment(ref _overReleases);
                return;
            }
            var slot = Volatile.Read(ref ById[index]);
            if (slot < 0)
            {
                Interlocked.Increment(ref _overReleases);
                return;
            }
            var entry = Entries + slot;
            if (Volatile.Read(ref entry->State) != StateLive || entry->Id != index || (Volatile.Read(ref entry->Count) >> 32) != generation)
            {
                Interlocked.Increment(ref _overReleases);
                return;
            }
            var after = Interlocked.Decrement(ref entry->Count);
            if (after < 0)
            {
                Interlocked.Increment(ref _overReleases);
                Interlocked.Increment(ref entry->Count);
                return;
            }
            if ((after & LowMask) != 0) return;
            Volatile.Write(ref entry->State, StateDead);
            if ((Volatile.Read(ref entry->Count) & LowMask) != 0)
            {
                Volatile.Write(ref entry->State, StateLive);
                return;
            }
            var block = entry->Block;
            var bytes = entry->Bytes;
            entry->Block = null;
            entry->Bytes = 0;
            if (block != null)
            {
                Interlocked.Increment(ref _deaths);
                Interlocked.Decrement(ref _live);
                PushGraveyard(block, bytes);
            }
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    internal static void Drain()
    {
        AcquireGate();
        try
        {
            DrainGraveyard();
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    static ushort Revive(Entry* entry, ReadOnlySpan<byte> baked)
    {
        entry->Block = AllocateBlock(baked);
        entry->Bytes = baked.Length;
        var generation = (ulong)Volatile.Read(ref entry->Count) >> 32;
        Interlocked.Exchange(ref entry->Count, (long)((generation + 1) << 32 | 1));
        Volatile.Write(ref entry->State, StateLive);
        Interlocked.Increment(ref _live);
        Interlocked.Increment(ref _revivals);
        Interlocked.Increment(ref _publishes);
        return entry->Id;
    }

    static ushort Publish(Entry* entry, ulong low, ulong high, ReadOnlySpan<byte> baked)
    {
        var id = (ushort)_nextId;
        _nextId++;
        _used++;
        entry->Block = AllocateBlock(baked);
        entry->Bytes = baked.Length;
        entry->KeyLow = low;
        entry->KeyHigh = high;
        entry->Id = id;
        var generation = (ulong)Volatile.Read(ref entry->Count) >> 32;
        Interlocked.Exchange(ref entry->Count, (long)((generation + 1) << 32 | 1));
        Volatile.Write(ref entry->State, StateLive);
        Volatile.Write(ref ById[id], (int)(entry - Entries));
        Interlocked.Increment(ref _live);
        Interlocked.Increment(ref _publishes);
        return id;
    }

    static byte* AllocateBlock(ReadOnlySpan<byte> baked)
    {
        var padded = (baked.Length + 63) & ~63;
        var block = (byte*)NativeMemory.AlignedAlloc((nuint)padded, 64);
        baked.CopyTo(new Span<byte>(block, baked.Length));
        Interlocked.Add(ref _allocBytes, padded);
        Interlocked.Increment(ref _allocCount);
        var live = Volatile.Read(ref _allocBytes) - Volatile.Read(ref _freeBytes) - Volatile.Read(ref _graveyardBytes);
        long peak;
        while (live > (peak = Volatile.Read(ref _peakLiveBytes)))
        {
            Interlocked.CompareExchange(ref _peakLiveBytes, live, peak);
        }
        return block;
    }

    static Entry* Find(ulong low, ulong high, ReadOnlySpan<byte> baked, out Entry* empty)
    {
        empty = null;
        var index = (int)(Mix(low, high) & (ulong)Mask);
        for (var n = 0; n < _capacity; n++)
        {
            var entry = Entries + ((index + n) & Mask);
            var state = Volatile.Read(ref entry->State);
            if (state == StateEmpty)
            {
                if (empty == null) empty = entry;
                return null;
            }
            if (Volatile.Read(ref entry->KeyLow) != low || Volatile.Read(ref entry->KeyHigh) != high) continue;
            if (state == StateDead) return entry;
            if (entry->Bytes != baked.Length) continue;
            if (!new ReadOnlySpan<byte>(entry->Block, (int)entry->Bytes).SequenceEqual(baked)) continue;
            return entry;
        }
        if (empty == null) throw new InvalidOperationException($"Intern table probe exhausted: no slot remains among {_capacity} entries for content ({low:X16}, {high:X16}).");
        return null;
    }

    static void Grow()
    {
        var previous = _block;
        var previousCapacity = _capacity;
        Allocate(previousCapacity * 2);
        for (var i = 0; i < previousCapacity; i++)
        {
            var entry = ((Entry*)previous)[i];
            if (entry.State == StateEmpty) continue;
            var index = (int)(Mix(entry.KeyLow, entry.KeyHigh) & (ulong)Mask);
            while (Entries[index].State != StateEmpty)
                index = (index + 1) & Mask;
            Entries[index] = entry;
            Volatile.Write(ref ById[entry.Id], index);
        }
        NativeMemory.AlignedFree(previous);
    }

    static void Allocate(int capacity)
    {
        _capacity = capacity;
        _block = (byte*)NativeMemory.AlignedAlloc((nuint)(capacity * sizeof(Entry) + capacity * sizeof(int)), 64);
        new Span<Entry>(Entries, capacity).Clear();
        new Span<int>(ById, capacity).Fill(-1);
    }

    static void PushGraveyard(byte* block, nint bytes)
    {
        *(byte**)block = (byte*)Volatile.Read(ref _graveyard);
        *(nint*)(block + 8) = bytes;
        Volatile.Write(ref _graveyard, (nint)block);
        Interlocked.Add(ref _graveyardBytes, (long)bytes);
        Interlocked.Increment(ref _graveyardCount);
    }

    static void DrainGraveyard()
    {
        var node = Volatile.Read(ref _graveyard);
        while (node != 0)
        {
            var current = (byte*)node;
            node = *(nint*)current;
            var bytes = *(nint*)(current + 8);
            Interlocked.Add(ref _freeBytes, (long)bytes);
            Interlocked.Increment(ref _freeCount);
            NativeMemory.AlignedFree(current);
        }
        Volatile.Write(ref _graveyard, 0);
        Volatile.Write(ref _graveyardBytes, 0);
        Volatile.Write(ref _graveyardCount, 0);
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

    static ulong Mix(ulong low, ulong high)
    {
        var h = low ^ (high * 0x9E3779B97F4A7C15UL) + 0xA0761D6478BD642FUL;
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53UL;
        h ^= h >> 33;
        return h;
    }
}
