using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HandleInterning;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct Entry
{
    public ulong KeyLow;
    public ulong KeyHigh;
    public byte* Block;
    public int BlockBytes;
    public long Count;
    public int State;
}

public sealed unsafe class Asset : IDisposable
{
    public const int StateEmpty = 0;
    public const int StateLive = 1;
    public const int StateTombstone = 2;

    readonly InternTable? _table;
    readonly Entry* _entry;
    readonly int _blockBytes;
    public byte* Block { get; }
    public int Length { get; }

    internal Asset(InternTable table, Entry* entry, byte* block, int length, int blockBytes)
    {
        _table = table;
        _entry = entry;
        Block = block;
        Length = length;
        _blockBytes = blockBytes;
    }

    Asset(byte* block, int length, int blockBytes)
    {
        Block = block;
        Length = length;
        _blockBytes = blockBytes;
    }

    public static Asset Fresh(ReadOnlySpan<byte> bytes) => new(InternTable.Allocate(bytes), bytes.Length, InternTable.PaddedSize(bytes.Length));

    public ref readonly byte FirstByte => ref *Block;

    public void Dispose()
    {
        if (_entry != null) _table!.Release(_entry);
        else InternTable.FreeBlock(Block, _blockBytes);
    }

    internal Entry* Entry => _entry;
}

public sealed unsafe class InternTable : IDisposable
{
    const long LowMask = 0xFFFFFFFFL;

    readonly Entry* _slots;
    readonly int _capacity;
    readonly int _maxEntries;
    int _gate;
    int _liveEntries;

    static long _allocBytes;
    static long _freeBytes;
    static long _allocCount;
    static long _freeCount;
    static long _peakLiveBytes;
    static long _graveyardBytes;
    static long _graveyardCount;
    static nint _graveyard;
    static int _activeProbes;
    static long _probeHits;
    static long _verifyFails;
    static long _revertsApplied;
    static long _revertsSkipped;
    static long _publishes;
    static long _scanMatches;
    static long _releaseCalls;
    static long _deaths;
    static long _blipRestores;
    static long _overReleases;

    public static long ProbeHits => Volatile.Read(ref _probeHits);
    public static long VerifyFails => Volatile.Read(ref _verifyFails);
    public static long RevertsApplied => Volatile.Read(ref _revertsApplied);
    public static long RevertsSkipped => Volatile.Read(ref _revertsSkipped);
    public static long Publishes => Volatile.Read(ref _publishes);
    public static long ScanMatches => Volatile.Read(ref _scanMatches);
    public static long ReleaseCalls => Volatile.Read(ref _releaseCalls);
    public static long Deaths => Volatile.Read(ref _deaths);
    public static long BlipRestores => Volatile.Read(ref _blipRestores);
    public static long OverReleases => Volatile.Read(ref _overReleases);

    public static long AllocBytes => Volatile.Read(ref _allocBytes);
    public static long FreeBytes => Volatile.Read(ref _freeBytes);
    public static long AllocCount => Volatile.Read(ref _allocCount);
    public static long FreeCount => Volatile.Read(ref _freeCount);
    public static long PeakLiveBytes => Volatile.Read(ref _peakLiveBytes);
    public static long GraveyardBytes => Volatile.Read(ref _graveyardBytes);
    public static long GraveyardBlocks => Volatile.Read(ref _graveyardCount);
    public static int ActiveProbes => Volatile.Read(ref _activeProbes);
    public int LiveEntries => Volatile.Read(ref _liveEntries);
    public int Capacity => _capacity;

    public InternTable(int capacity = 4096)
    {
        if (capacity < 16 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be a power of two of at least 16.");
        _capacity = capacity;
        _maxEntries = capacity - capacity / 16;
        _slots = (Entry*)NativeMemory.AlignedAlloc((nuint)capacity * (nuint)sizeof(Entry), 64);
        new Span<Entry>(_slots, capacity).Clear();
    }

    public Asset Load(ReadOnlySpan<byte> bytes)
    {
        var key = ContentKey.Of(bytes);
        var entry = Probe(in key, out var block, out var length);
        if (entry != null) return Wrap(entry, block, length);
        return Insert(in key, bytes);
    }

    public Asset Load(in ContentKey key, ReadOnlySpan<byte> bytes)
    {
        var entry = Probe(in key, out var block, out var length);
        if (entry != null) return Wrap(entry, block, length);
        return Insert(in key, bytes);
    }

    Entry* Probe(in ContentKey key, out byte* block, out int length)
    {
        block = null;
        length = 0;
        Interlocked.Increment(ref _activeProbes);
        try
        {
            return ProbeCore(in key, out block, out length);
        }
        finally
        {
            Interlocked.Decrement(ref _activeProbes);
        }
    }

    Entry* ProbeCore(in ContentKey key, out byte* block, out int length)
    {
        block = null;
        length = 0;
        var mask = _capacity - 1;
        var index = (int)(Hash(in key) & (ulong)mask);
        for (var n = 0; n < _capacity; n++)
        {
            var e = _slots + ((index + n) & mask);
            if (Volatile.Read(ref e->State) != Asset.StateLive)
            {
                if (Volatile.Read(ref e->State) == Asset.StateEmpty) return null;
                continue;
            }
            if (Volatile.Read(ref e->KeyLow) != key.Low || Volatile.Read(ref e->KeyHigh) != key.High) continue;
            var seen = Interlocked.Increment(ref e->Count);
            var generation = (ulong)seen >> 32;
            var candidate = e->Block;
            var candidateLength = e->BlockBytes;
            if (candidate != null &&
                Volatile.Read(ref e->State) == Asset.StateLive &&
                Volatile.Read(ref e->KeyLow) == key.Low &&
                Volatile.Read(ref e->KeyHigh) == key.High &&
                (ulong)Volatile.Read(ref e->Count) >> 32 == generation)
            {
                block = candidate;
                length = candidateLength;
                Interlocked.Increment(ref _probeHits);
                return e;
            }
            Interlocked.Increment(ref _verifyFails);
            var current = Volatile.Read(ref e->Count);
            while ((ulong)current >> 32 == generation && (current & LowMask) != 0)
            {
                var previous = Interlocked.CompareExchange(ref e->Count, current - 1, current);
                if (previous == current)
                {
                    Interlocked.Increment(ref _revertsApplied);
                    break;
                }
                current = previous;
            }
            if ((ulong)current >> 32 != generation) Interlocked.Increment(ref _revertsSkipped);
        }
        return null;
    }

    Asset Insert(in ContentKey key, ReadOnlySpan<byte> bytes)
    {
        AcquireGate();
        try
        {
            if (Volatile.Read(ref _graveyard) != 0 && Volatile.Read(ref _activeProbes) == 0) DrainGraveyard();
            var existing = Probe(in key, out var existingBlock, out var existingLength);
            if (existing != null) return Wrap(existing, existingBlock, existingLength);
            var mask = _capacity - 1;
            var index = (int)(Hash(in key) & (ulong)mask);
            Entry* reuse = null;
            for (var n = 0; n < _capacity; n++)
            {
                var e = _slots + ((index + n) & mask);
                var state = Volatile.Read(ref e->State);
                if (state == Asset.StateLive)
                {
                    if (e->KeyLow == key.Low && e->KeyHigh == key.High)
                    {
                        Interlocked.Increment(ref e->Count);
                        Interlocked.Increment(ref _scanMatches);
                        return Wrap(e, e->Block, e->BlockBytes);
                    }
                    continue;
                }
                if (state == Asset.StateTombstone)
                {
                    if (reuse == null) reuse = e;
                    continue;
                }
                if (Volatile.Read(ref _liveEntries) >= _maxEntries)
                    throw new InvalidOperationException(
                        $"Intern table exhausted: {_liveEntries} live entries, capacity {_capacity} slots, lifetime insert bound {_maxEntries}.");
                var target = reuse != null ? reuse : e;
                Publish(target, in key, bytes);
                return Wrap(target, target->Block, target->BlockBytes);
            }
            if (reuse != null)
            {
                if (Volatile.Read(ref _liveEntries) >= _maxEntries)
                    throw new InvalidOperationException(
                        $"Intern table exhausted: {_liveEntries} live entries, capacity {_capacity} slots, lifetime insert bound {_maxEntries}.");
                Publish(reuse, in key, bytes);
                return Wrap(reuse, reuse->Block, reuse->BlockBytes);
            }
            throw new InvalidOperationException(
                $"Intern table full: no empty slot remains among {_capacity} slots for a new entry.");
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    void Publish(Entry* e, in ContentKey key, ReadOnlySpan<byte> bytes)
    {
        Interlocked.Increment(ref _publishes);
        var block = Allocate(bytes);
        e->Block = block;
        e->BlockBytes = bytes.Length;
        e->KeyLow = key.Low;
        e->KeyHigh = key.High;
        var generation = (ulong)Volatile.Read(ref e->Count) >> 32;
        Interlocked.Exchange(ref e->Count, (long)((generation + 1) << 32 | 1));
        Volatile.Write(ref e->State, Asset.StateLive);
        Interlocked.Increment(ref _liveEntries);
    }

    internal void Release(Entry* e)
    {
        Interlocked.Increment(ref _releaseCalls);
        var after = Interlocked.Decrement(ref e->Count);
        if ((uint)after == 0xFFFFFFFFu)
        {
            Interlocked.Increment(ref _overReleases);
            return;
        }
        if ((after & LowMask) != 0) return;
        AcquireGate();
        try
        {
            Volatile.Write(ref e->State, Asset.StateTombstone);
            if ((Volatile.Read(ref e->Count) & LowMask) != 0 && e->KeyLow != 0)
            {
                Volatile.Write(ref e->State, Asset.StateLive);
                Interlocked.Increment(ref _blipRestores);
                return;
            }
            var block = e->Block;
            var blockBytes = e->BlockBytes;
            Volatile.Write(ref e->KeyLow, 0);
            Volatile.Write(ref e->KeyHigh, 0);
            e->Block = null;
            Volatile.Write(ref e->BlockBytes, 0);
            if (block != null)
            {
                Interlocked.Increment(ref _deaths);
                Interlocked.Decrement(ref _liveEntries);
                PushGraveyard(block, blockBytes);
            }
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    Asset Wrap(Entry* e, byte* block, int length)
    {
        if (block == null)
            throw new InvalidOperationException(
                $"Intern table handed out a null block: state={Volatile.Read(ref e->State)} count={Volatile.Read(ref e->Count)} key=({e->KeyLow:X16},{e->KeyHigh:X16}).");
        return new(this, e, block, length, PaddedSize(length));
    }

    internal static int PaddedSize(int length) => (length + 63) & ~63;

    static void PushGraveyard(byte* block, int blockBytes)
    {
        var head = _graveyard;
        *(byte**)block = (byte*)head;
        *(int*)(block + 8) = blockBytes;
        Volatile.Write(ref _graveyard, (nint)block);
        Interlocked.Add(ref _graveyardBytes, blockBytes);
        Interlocked.Increment(ref _graveyardCount);
    }

    static void DrainGraveyard()
    {
        var node = _graveyard;
        while (node != 0)
        {
            var current = (byte*)node;
            var next = *(nint*)current;
            var bytes = *(int*)(current + 8);
            Interlocked.Add(ref _freeBytes, bytes);
            Interlocked.Increment(ref _freeCount);
            NativeMemory.AlignedFree(current);
            node = next;
        }
        Volatile.Write(ref _graveyard, 0);
        Volatile.Write(ref _graveyardBytes, 0);
        Volatile.Write(ref _graveyardCount, 0);
    }

    public static void DrainGraveyardForced()
    {
        AcquireStaticGate();
        try
        {
            DrainGraveyard();
        }
        finally
        {
            Volatile.Write(ref _staticGate, 0);
        }
    }

    static int _staticGate;

    static void AcquireStaticGate()
    {
        var spins = 0;
        while (Interlocked.CompareExchange(ref _staticGate, 1, 0) != 0)
        {
            Thread.SpinWait(64);
            if (++spins >= 4096)
            {
                spins = 0;
                Thread.Yield();
            }
        }
    }

    internal static byte* Allocate(ReadOnlySpan<byte> bytes)
    {
        var block = (byte*)NativeMemory.AlignedAlloc((nuint)PaddedSize(bytes.Length), 64);
        bytes.CopyTo(new Span<byte>(block, bytes.Length));
        var padded = PaddedSize(bytes.Length);
        Interlocked.Add(ref _allocBytes, padded);
        Interlocked.Increment(ref _allocCount);
        var live = Volatile.Read(ref _allocBytes) - Volatile.Read(ref _freeBytes) - Volatile.Read(ref _graveyardBytes);
        long peak;
        while ((peak = Volatile.Read(ref _peakLiveBytes)) < live)
            Interlocked.CompareExchange(ref _peakLiveBytes, live, peak);
        return block;
    }

    internal static void FreeBlock(byte* block, int blockBytes)
    {
        NativeMemory.AlignedFree(block);
        Interlocked.Add(ref _freeBytes, blockBytes);
        Interlocked.Increment(ref _freeCount);
    }

    void AcquireGate()
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

    static ulong Hash(in ContentKey key)
    {
        var h = key.Low ^ (key.High * 0x9E3779B97F4A7C15UL) + 0xA0761D6478BD642FUL;
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53UL;
        h ^= h >> 33;
        return h;
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(_slots);
    }
}
