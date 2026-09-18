using System.Runtime.InteropServices;

namespace Tl;

public static unsafe class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    const int Chunk = 4096;

    [StructLayout(LayoutKind.Sequential)]
    struct ResolvedLane
    {
        public ulong KeyLow;
        public ulong KeyHigh;
        public nuint Bytes;
        public ushort Slot;
    }

    static TimelineSet<TTrack, TClip>? _bank;
    static ResolvedLane* _resolved;
    static int _resolvedCount;
    static int _resolvedMask;

    public static ushort Slot(TimelineAsset asset)
        => Resolve(asset, null, out var slot) ? slot : Absent();

    public static ushort Slot(TimelineAsset asset, MeasuredLanes measured)
    {
        measured.ValidateBinding(asset);
        return Resolve(asset, measured, out var slot) ? slot : Absent();
    }

    public static TimelineSetLane<TTrack, TClip> Seek(ReadOnlySpan<ushort> handles, Span<ushort> positions, bool forward)
    {
        var bank = _bank;
        if (bank is null)
            throw new InvalidOperationException($"No timeline is bound for the pair ({typeof(TTrack).Name}, {typeof(TClip).Name}); call Timeline<{typeof(TTrack).Name}, {typeof(TClip).Name}>.Slot at load time.");
        return bank.Gather(handles).Seek(positions, forward);
    }

    public static void Advance(ReadOnlySpan<ushort> handles, Span<ushort> positions, bool forward, Span<float> effects)
        => Seek(handles, positions, forward).Apply(effects);

    public static void Advance(TimelineAsset asset, Span<ushort> positions, bool forward, Span<float> effects)
    {
        if (!Resolve(asset, null, out var slot))
            Absent();
        var chunk = Math.Min(positions.Length, Chunk);
        Span<ushort> handles = stackalloc ushort[chunk];
        handles.Fill(slot);
        var offset = 0;
        while (offset < positions.Length)
        {
            var take = Math.Min(chunk, positions.Length - offset);
            Seek(handles[..take], positions.Slice(offset, take), forward).Apply(effects.Slice(offset, take));
            offset += take;
        }
    }

    static ushort Absent()
        => throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");

    static bool Resolve(TimelineAsset asset, MeasuredLanes? measured, out ushort slot)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var reference = asset.Reference;
        if (reference.Address == 0)
            throw new ArgumentException("Timeline asset is not loaded.");
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key))
        {
            slot = 0;
            return false;
        }
        if (PairTable.Head(key) < 0)
            throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        var block = reference._p;
        var bytes = (nuint)((NativeHeader*)block)->Bytes;
        ContentKey(block, bytes, out var low, out var high);
        if (FindResolved(bytes, low, high, out slot))
            return true;
        if (measured is null)
        {
            using var owned = MeasuredLanes.Measure(asset);
            slot = Bank().Add(asset, owned);
        }
        else
        {
            slot = Bank().Add(asset, measured);
        }
        RememberResolved(bytes, low, high, slot);
        return true;
    }

    static void ContentKey(byte* block, nuint bytes, out ulong low, out ulong high)
    {
        ulong a = 14695981039346656037ul;
        ulong b = 1469598103934665603ul;
        nuint i = 0;
        for (; i + sizeof(ulong) <= bytes; i += sizeof(ulong))
        {
            var lane = *(ulong*)(block + i);
            a = (a ^ lane) * 1099511628211ul;
            b = (b ^ lane) * 14029467366897019727ul;
        }
        for (; i < bytes; i++)
        {
            a = (a ^ block[i]) * 1099511628211ul;
            b = (b ^ block[i]) * 14029467366897019727ul;
        }
        low = a;
        high = b;
    }

    static bool FindResolved(nuint bytes, ulong low, ulong high, out ushort slot)
    {
        slot = 0;
        var table = _resolved;
        if (table == null)
            return false;
        var mask = (uint)_resolvedMask;
        var index = (int)((low ^ high) & mask);
        while (true)
        {
            ref var entry = ref table[index];
            if (entry.Bytes == 0)
                return false;
            if (entry.Bytes == bytes && entry.KeyLow == low && entry.KeyHigh == high)
            {
                slot = entry.Slot;
                return true;
            }
            index = (int)((uint)index + 1u & mask);
        }
    }

    static void RememberResolved(nuint bytes, ulong low, ulong high, ushort slot)
    {
        if (_resolved == null || (_resolvedCount + 1) * 4 > (_resolvedMask + 1) * 3)
            GrowResolved();
        var mask = (uint)_resolvedMask;
        var index = (int)((low ^ high) & mask);
        while (_resolved[index].Bytes != 0)
            index = (int)((uint)index + 1u & mask);
        _resolved[index] = new ResolvedLane { KeyLow = low, KeyHigh = high, Bytes = bytes, Slot = slot };
        _resolvedCount++;
    }

    static void GrowResolved()
    {
        var previous = _resolved;
        var previousCapacity = previous is null ? 0 : _resolvedMask + 1;
        var capacity = previousCapacity == 0 ? 16 : previousCapacity * 2;
        var grown = (ResolvedLane*)NativeMemory.AlignedAlloc((nuint)capacity * (nuint)sizeof(ResolvedLane), 64);
        NativeMemory.Clear(grown, (nuint)capacity * (nuint)sizeof(ResolvedLane));
        _resolved = grown;
        _resolvedMask = capacity - 1;
        if (previous == null)
            return;
        for (var i = 0; i < previousCapacity; i++)
        {
            var entry = previous[i];
            if (entry.Bytes == 0)
                continue;
            var index = (int)((entry.KeyLow ^ entry.KeyHigh) & (uint)_resolvedMask);
            while (grown[index].Bytes != 0)
                index = (int)((uint)index + 1u & (uint)_resolvedMask);
            grown[index] = entry;
        }
        NativeMemory.AlignedFree(previous);
    }

    static TimelineSet<TTrack, TClip> Bank()
    {
        var bank = _bank;
        if (bank is not null) return bank;
        bank = new TimelineSet<TTrack, TClip>();
        return Interlocked.CompareExchange(ref _bank, bank, null) ?? bank;
    }
}
