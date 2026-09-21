using System.Runtime.InteropServices;

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
    static uint* _motion;
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

    static TimelineTable()
    {
        Allocate(InitialCapacity);
        _motion = (uint*)NativeMemory.AlignedAlloc((MaxCapacity * sizeof(uint)), 64);
        new Span<uint>(_motion, MaxCapacity).Clear();
    }

    internal static uint* Motion => _motion;

    static uint MotionOf(byte* block)
        => ((NativeHeader*)block)->Duration | (((NativeHeader*)block)->Loops != 0 ? 0x80000000u : 0u);

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
        Digest(baked, out var low, out var high);
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
            TimelineRef.Validate(baked);
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
            if (TryLive(index, out var _))
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
            void Over() => Interlocked.Increment(ref _overReleases);
            if (index >= (uint)Volatile.Read(ref _nextId))
            {
                Over();
                return;
            }
            var slot = Volatile.Read(ref ById[index]);
            if (slot < 0)
            {
                Over();
                return;
            }
            var entry = Entries + slot;
            if (Volatile.Read(ref entry->State) != StateLive || entry->Id != index || (Volatile.Read(ref entry->Count) >> 32) != generation)
            {
                Over();
                return;
            }
            var after = Interlocked.Decrement(ref entry->Count);
            if (after < 0)
            {
                Over();
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
            Volatile.Write(ref _motion[index], 0u);
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
        TimelineRef.Validate(baked);
        entry->Block = AllocateBlock(baked);
        entry->Bytes = baked.Length;
        Volatile.Write(ref _motion[entry->Id], MotionOf(entry->Block));
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
        Volatile.Write(ref _motion[id], MotionOf(entry->Block));
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
        Interlocked.Add(ref _graveyardBytes, bytes);
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
            Interlocked.Add(ref _freeBytes, bytes);
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

    static void Digest(ReadOnlySpan<byte> data, out ulong low, out ulong high)
    {
        const ulong p5 = 2870177450012600261UL;
        var length = (ulong)data.Length;
        ulong a = length * p5 ^ 0x9E3779B97F4A7C15UL;
        ulong b = length * p5 ^ 0xC2B2AE3D27D4EB4FUL;
        ulong c = (length * p5 ^ 0x165667B19E3779F9UL) << 1;
        ulong d = (length * p5 ^ 0x85EBCA77C2B2AE63UL) << 2;
        fixed (byte* pinned = data)
        {
            var p = pinned;
            var end = p + (data.Length & ~31);
            while (p < end)
            {
                a = DigestRound(a, *(ulong*)p);
                b = DigestRound(b, *(ulong*)(p + 8));
                c = DigestRound(c, *(ulong*)(p + 16));
                d = DigestRound(d, *(ulong*)(p + 24));
                p += 32;
            }
            var tail = data.Length & 31;
            for (var i = 0; i < tail; i++)
                a = DigestRound(a, p[i] | 0x800UL);
        }
        low = DigestAvalanche(a ^ DigestAvalanche(c));
        high = DigestAvalanche(b ^ DigestAvalanche(d ^ length));
    }

    static ulong DigestRound(ulong state, ulong lane)
    {
        var mixed = state ^ (lane * 14029467366897019727UL);
        return ((mixed << 31) | (mixed >> 33)) * 11400714785074694791UL;
    }

    static ulong DigestAvalanche(ulong value)
    {
        value ^= value >> 33;
        value *= 14029467366897019727UL;
        value ^= value >> 29;
        value *= 11400714785074694791UL;
        value ^= value >> 32;
        return value;
    }
}
