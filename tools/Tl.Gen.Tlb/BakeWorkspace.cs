using System.Collections.Concurrent;

namespace Tl.Gen.Tlb;

internal sealed class BakeWorkspace
{
    internal static readonly BakeWorkspace Shared = new();

    private readonly Lock _gate = new();
    private MaskLease? _masks;
    private readonly Dictionary<ulong, byte[]> _pairPools = [];
    private readonly ConcurrentDictionary<(string Ns, string Type, string? Asm), Type> _typeCache = new();

    internal MaskLease? RentMasks(int blocks)
    {
        lock (_gate)
        {
            if (_masks is { } masks && masks.Blocks >= blocks)
            {
                _masks = null;
                return masks;
            }
            return null;
        }
    }

    internal void ReturnMasks(MaskLease lease)
    {
        lock (_gate)
        {
            if (_masks is not { } current)
                _masks = lease;
            else if (current.Blocks < lease.Blocks)
            {
                current.Dispose();
                _masks = lease;
            }
            else
                lease.Dispose();
        }
    }

    internal byte[]? RentPool(ulong key)
    {
        lock (_gate)
        {
            if (_pairPools.Remove(key, out var pool))
            {
                return pool;
            }
            return null;
        }
    }

    internal void ReturnPool(ulong key, byte[] pool)
    {
        lock (_gate)
        {
            if (_pairPools.TryGetValue(key, out var current))
            {
                if (current.Length < pool.Length)
                    _pairPools[key] = pool;
            }
            else
            {
                _pairPools[key] = pool;
            }
        }
    }

    internal void Warm(FastDoc doc) =>
        doc.ResolveCache = new Dictionary<(string, string, string?), Type>(_typeCache);

    internal void Reclaim(FastDoc doc)
    {
        foreach (var pair in doc.Pairs)
            if (pair.Pool.Length > 0)
                ReturnPool(pair.Key, pair.Pool);
        if (doc.LoanMasks is { } lease)
            ReturnMasks(lease);
        foreach (var entry in doc.ResolveCache)
            _typeCache.TryAdd(entry.Key, entry.Value);
        doc.LoanMasks = null;
    }
}
