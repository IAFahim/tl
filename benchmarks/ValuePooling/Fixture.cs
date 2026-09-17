using System.Runtime.InteropServices;

namespace Tl.ValuePooling;

public sealed unsafe class Fixture : IDisposable
{
    public const ushort QueryTick = 300;

    private const uint SeedXor = 0x1493A7B5u;
    private const uint SeedMul = 2654435761u;

    private readonly int _rows;
    private readonly int _pool;
    private readonly InlineSlot* _inline;
    private readonly PooledByteSlot* _pooledByte;
    private readonly PooledUshortSlot* _pooledUshort;
    private readonly float* _multipliers;
    private readonly float* _biases;
    private readonly float* _amounts;

    private Fixture(int rows, int pool)
    {
        _rows = rows;
        _pool = pool;
        _inline = Alloc<InlineSlot>(rows);
        _pooledByte = Alloc<PooledByteSlot>(rows);
        _pooledUshort = Alloc<PooledUshortSlot>(rows);
        _multipliers = Alloc<float>(pool);
        _biases = Alloc<float>(pool);
        _amounts = Alloc<float>(pool);
    }

    public static Fixture Create(int rows, int pool)
    {
        if (pool < 1 || pool > 1000) throw new ArgumentOutOfRangeException(nameof(pool));
        var fixture = new Fixture(rows, pool);
        fixture.Populate();
        return fixture;
    }

    public int Rows => _rows;
    public int Pool => _pool;

    public void Dispose()
    {
        Free(_inline);
        Free(_pooledByte);
        Free(_pooledUshort);
        Free(_multipliers);
        Free(_biases);
        Free(_amounts);
    }

    private static T* Alloc<T>(int count) where T : unmanaged
        => (T*)NativeMemory.AlignedAlloc((nuint)(sizeof(T) * count), 64);

    private static void Free(void* block)
    {
        if (block != null) NativeMemory.AlignedFree(block);
    }

    private static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private void Populate()
    {
        for (var k = 0; k < _pool; k++)
        {
            _multipliers[k] = 1f + k * 0.25f;
            _biases[k] = -8f + k * 0.5f;
            _amounts[k] = 0.125f + k * 0.0625f;
        }

        var windowEnd = (uint)QueryTick + 1u;
        for (var i = 0; i < _rows; i++)
        {
            var state = (uint)i * SeedMul ^ SeedXor;
            _ = Next(ref state);
            var multiplierIndex = (int)(Next(ref state) % (uint)_pool);
            var biasIndex = (int)(Next(ref state) % (uint)_pool);
            var firstIndex = (int)(Next(ref state) % (uint)_pool);
            var secondIndex = (int)(Next(ref state) % (uint)_pool);
            var start = Next(ref state) % (uint)QueryTick;
            var span = windowEnd - start;
            var mode = Next(ref state) % 4u;
            var factorSpan = mode switch
            {
                0 => 0u,
                1 => 1u,
                _ => span,
            };
            var factorStart = factorSpan <= 1 ? 0u : start;
            var trackIndex = (byte)(i & 3);

            _inline[i] = new InlineSlot
            {
                Track = new FloatTrack(_multipliers[multiplierIndex], _biases[biasIndex]),
                First = new FloatClip(_amounts[firstIndex]),
                Second = new FloatClip(_amounts[secondIndex]),
                WindowStart = start,
                WindowEnd = windowEnd,
                FactorStart = factorStart,
                FactorSpan = factorSpan,
                TrackIndex = trackIndex,
            };
            _pooledByte[i] = new PooledByteSlot
            {
                WindowStart = start,
                WindowEnd = windowEnd,
                FactorStart = factorStart,
                FactorSpan = factorSpan,
                MultiplierIndex = (byte)multiplierIndex,
                BiasIndex = (byte)biasIndex,
                FirstIndex = (byte)firstIndex,
                SecondIndex = (byte)secondIndex,
                TrackIndex = trackIndex,
            };
            _pooledUshort[i] = new PooledUshortSlot
            {
                WindowStart = start,
                WindowEnd = windowEnd,
                FactorStart = factorStart,
                FactorSpan = factorSpan,
                MultiplierIndex = (ushort)multiplierIndex,
                BiasIndex = (ushort)biasIndex,
                FirstIndex = (ushort)firstIndex,
                SecondIndex = (ushort)secondIndex,
                TrackIndex = trackIndex,
            };
        }
    }

    public float ScanInline()
    {
        var slots = _inline;
        var tick = QueryTick;
        var sum = 0f;
        for (var i = 0; i < _rows; i++)
        {
            var slot = slots + i;
            float value;
            if (slot->FactorSpan == 0) value = slot->First.Amount;
            else
            {
                var factor = slot->FactorSpan <= 1 ? 0.5f : (tick - slot->FactorStart) / (float)(slot->FactorSpan - 1);
                value = slot->First.Amount + (slot->Second.Amount - slot->First.Amount) * factor;
            }
            sum += value * slot->Track.Multiplier + slot->Track.Bias;
        }
        return sum;
    }

    public float ScanPooledByte()
    {
        if (_pool > Layout.MaxBytePool) throw new InvalidOperationException("Byte indices require pools of at most 256 unique values.");
        var slots = _pooledByte;
        var multipliers = _multipliers;
        var biases = _biases;
        var amounts = _amounts;
        var tick = QueryTick;
        var sum = 0f;
        for (var i = 0; i < _rows; i++)
        {
            var slot = slots + i;
            float value;
            if (slot->FactorSpan == 0) value = amounts[slot->FirstIndex];
            else
            {
                var factor = slot->FactorSpan <= 1 ? 0.5f : (tick - slot->FactorStart) / (float)(slot->FactorSpan - 1);
                value = amounts[slot->FirstIndex] + (amounts[slot->SecondIndex] - amounts[slot->FirstIndex]) * factor;
            }
            sum += value * multipliers[slot->MultiplierIndex] + biases[slot->BiasIndex];
        }
        return sum;
    }

    public float ScanPooledUshort()
    {
        var slots = _pooledUshort;
        var multipliers = _multipliers;
        var biases = _biases;
        var amounts = _amounts;
        var tick = QueryTick;
        var sum = 0f;
        for (var i = 0; i < _rows; i++)
        {
            var slot = slots + i;
            float value;
            if (slot->FactorSpan == 0) value = amounts[slot->FirstIndex];
            else
            {
                var factor = slot->FactorSpan <= 1 ? 0.5f : (tick - slot->FactorStart) / (float)(slot->FactorSpan - 1);
                value = amounts[slot->FirstIndex] + (amounts[slot->SecondIndex] - amounts[slot->FirstIndex]) * factor;
            }
            sum += value * multipliers[slot->MultiplierIndex] + biases[slot->BiasIndex];
        }
        return sum;
    }

    public float IndirectInline()
    {
        var slots = _inline;
        var sum = 0f;
        for (var i = 0; i < _rows; i++) sum += (slots + i)->Track.Multiplier;
        return sum;
    }

    public float IndirectPooledByte()
    {
        if (_pool > Layout.MaxBytePool) throw new InvalidOperationException("Byte indices require pools of at most 256 unique values.");
        var slots = _pooledByte;
        var multipliers = _multipliers;
        var sum = 0f;
        for (var i = 0; i < _rows; i++) sum += multipliers[(slots + i)->MultiplierIndex];
        return sum;
    }

    public float IndirectPooledUshort()
    {
        var slots = _pooledUshort;
        var multipliers = _multipliers;
        var sum = 0f;
        for (var i = 0; i < _rows; i++) sum += multipliers[(slots + i)->MultiplierIndex];
        return sum;
    }
}
