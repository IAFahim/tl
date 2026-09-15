using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Tl;

public sealed unsafe class TimelineSet<TTrack, TClip> : IDisposable
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    internal unsafe struct Slot
    {
        public float* Forward;
        public float* Backward;
        public ushort Duration;
        public ushort Looping;
    }

    internal Slot* _slots;
    internal float* _data;
    internal int _count;
    internal bool _anyLooping;
    internal bool _disposed;
    nuint _dataFloats;

    public ushort Add(TimelineAsset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_count > ushort.MaxValue)
            throw new InvalidOperationException("TimelineSet is full; a set holds at most 65536 dense timeline ids.");
        LaneTable<TTrack, TClip>.Measure(asset, out var forward, out var backward, out var duration, out var looping);
        nuint floats = Math.Max(1u, duration);
        nuint dataFloats = _dataFloats + floats * 2;
        var block = (byte*)NativeMemory.AlignedAlloc((nuint)(_count + 1) * (nuint)sizeof(Slot) + dataFloats * sizeof(float), 64);
        var slots = (Slot*)block;
        var data = (float*)(block + (nuint)(_count + 1) * (nuint)sizeof(Slot));
        long shift = 0;
        if (_count > 0)
        {
            var slotBytes = (long)((nuint)_count * (nuint)sizeof(Slot));
            Buffer.MemoryCopy(_slots, slots, slotBytes, slotBytes);
            var dataBytes = (long)(_dataFloats * sizeof(float));
            Buffer.MemoryCopy(_data, data, dataBytes, dataBytes);
            shift = (long)((byte*)data - (byte*)_data);
            for (var k = 0; k < _count; k++)
            {
                slots[k].Forward = (float*)((byte*)slots[k].Forward + shift);
                slots[k].Backward = (float*)((byte*)slots[k].Backward + shift);
            }
        }
        slots[_count] = new Slot { Forward = data + _dataFloats, Backward = data + _dataFloats + duration, Duration = duration, Looping = looping ? (ushort)1 : (ushort)0 };
        var tableBytes = (long)(floats * sizeof(float));
        Buffer.MemoryCopy(forward, data + _dataFloats, tableBytes, tableBytes);
        Buffer.MemoryCopy(backward, data + _dataFloats + duration, tableBytes, tableBytes);
        NativeMemory.AlignedFree(forward);
        NativeMemory.AlignedFree(backward);
        var previous = _slots;
        _slots = slots;
        _data = data;
        _dataFloats = dataFloats;
        _anyLooping |= looping;
        var assigned = (ushort)_count;
        _count++;
        if (previous != null)
            NativeMemory.AlignedFree(previous);
        return assigned;
    }

    public TimelineGather<TTrack, TClip> Gather(ReadOnlySpan<ushort> timelineIds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new TimelineGather<TTrack, TClip>(this, timelineIds);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var previous = _slots;
        _slots = null;
        _data = null;
        _count = 0;
        if (previous != null)
            NativeMemory.AlignedFree(previous);
    }
}

public ref struct TimelineGather<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    readonly TimelineSet<TTrack, TClip> _set;
    readonly ReadOnlySpan<ushort> _ids;

    internal TimelineGather(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> ids)
    {
        _set = set;
        _ids = ids;
    }

    public TimelineSetLane<TTrack, TClip> Seek(Span<ushort> positions, bool forward)
        => new(_set, _ids, positions, forward);
}

public ref struct TimelineSetLane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    const int Chunk = 4096;

    readonly TimelineSet<TTrack, TClip> _set;
    readonly ReadOnlySpan<ushort> _ids;
    readonly Span<ushort> _positions;
    readonly bool _forward;

    internal TimelineSetLane(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> ids, Span<ushort> positions, bool forward)
    {
        _set = set;
        _ids = ids;
        _positions = positions;
        _forward = forward;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe void Apply(Span<float> effects, Span<long> cycles)
    {
        var set = _set;
        if (set._disposed)
            throw new ObjectDisposedException(nameof(TimelineSet<TTrack, TClip>));
        var ids = _ids;
        var positions = _positions;
        if (ids.Length != positions.Length || effects.Length != positions.Length
            || (cycles.Length != positions.Length && !(cycles.IsEmpty && !set._anyLooping)))
            throw new ArgumentException("Column length must equal position count; all-finite sets may pass an empty cycle column.");
        if (MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(positions))
            || MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(cycles))
            || MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(cycles))
            || MemoryMarshal.AsBytes(effects).Overlaps(MemoryMarshal.AsBytes(cycles)))
            throw new ArgumentException("Lane columns must not overlap.");
        var count = positions.Length;
        if (count == 0) return;
        var slots = set._slots;
        var bound = set._count;
        var forward = _forward;
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            if (ProbeChunk(ids, i, chunkEnd, bound))
            {
                var slot = slots + ids[i];
                i = forward
                    ? ApplyUniformForward(slot->Forward, slot->Duration, slot->Looping != 0, positions, effects, cycles, i, chunkEnd)
                    : ApplyUniformBackward(slot->Backward, slot->Duration, slot->Looping != 0, positions, effects, cycles, i, chunkEnd);
            }
            else
            {
                i = forward
                    ? ApplyMixedForward(ids, positions, effects, cycles, slots, i, chunkEnd)
                    : ApplyMixedBackward(ids, positions, effects, cycles, slots, i, chunkEnd);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformForward(float* table, ushort duration, bool looping, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        while (i < limit)
        {
            var position = positions[i];
            var end = RunEnd(positions, i, limit);
            if (position >= duration) { i = end; continue; }
            var delta = table[position];
            var next = position + 1;
            long cycleDelta = 0;
            bool reset = false;
            if (looping)
            {
                if (next == duration) { next = 0; cycleDelta = 1; }
            }
            else reset = true;
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = (ushort)next;
                if (reset && !cycles.IsEmpty) cycles[i] = 0;
                else if (cycleDelta != 0) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, (ushort)next);
                if (reset && !cycles.IsEmpty) Zero(cycles, i, end);
                else if (cycleDelta != 0) Add(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformBackward(float* table, ushort duration, bool looping, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        while (i < limit)
        {
            var position = positions[i];
            var end = RunEnd(positions, i, limit);
            if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
            ushort tick;
            long cycleDelta;
            if (position == 0) { tick = (ushort)(duration - 1); cycleDelta = -1; }
            else { tick = (ushort)(position - 1); cycleDelta = 0; }
            var delta = table[tick];
            var next = tick;
            var reset = !looping;
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = (ushort)next;
                if (reset && !cycles.IsEmpty) cycles[i] = 0;
                else if (cycleDelta != 0) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, (ushort)next);
                if (reset && !cycles.IsEmpty) Zero(cycles, i, end);
                else if (cycleDelta != 0) Add(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedForward(ReadOnlySpan<ushort> ids, Span<ushort> positions, Span<float> effects, Span<long> cycles, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        while (i < limit)
        {
            var position = positions[i];
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + ids[i];
            var duration = slot->Duration;
            if (position >= duration) { i = end; continue; }
            var delta = slot->Forward[position];
            var next = position + 1;
            long cycleDelta = 0;
            bool reset = false;
            if (slot->Looping != 0)
            {
                if (next == duration) { next = 0; cycleDelta = 1; }
            }
            else reset = true;
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = (ushort)next;
                if (reset && !cycles.IsEmpty) cycles[i] = 0;
                else if (cycleDelta != 0) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, (ushort)next);
                if (reset && !cycles.IsEmpty) Zero(cycles, i, end);
                else if (cycleDelta != 0) Add(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedBackward(ReadOnlySpan<ushort> ids, Span<ushort> positions, Span<float> effects, Span<long> cycles, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        while (i < limit)
        {
            var position = positions[i];
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + ids[i];
            var duration = slot->Duration;
            if (position == 0 && slot->Looping == 0 || position > duration || slot->Looping != 0 && position == duration) { i = end; continue; }
            ushort tick;
            long cycleDelta;
            if (position == 0) { tick = (ushort)(duration - 1); cycleDelta = -1; }
            else { tick = (ushort)(position - 1); cycleDelta = 0; }
            var delta = slot->Backward[tick];
            var next = tick;
            var reset = slot->Looping == 0;
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = (ushort)next;
                if (reset && !cycles.IsEmpty) cycles[i] = 0;
                else if (cycleDelta != 0) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, (ushort)next);
                if (reset && !cycles.IsEmpty) Zero(cycles, i, end);
                else if (cycleDelta != 0) Add(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    static bool ProbeChunk(ReadOnlySpan<ushort> ids, int start, int end, int bound)
    {
        var first = ids[start];
        var uniform = true;
        var limit = bound <= ushort.MaxValue ? (ushort)bound : ushort.MaxValue;
        var i = start;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var firstVector = Vector512.Create(first);
            var boundVector = Vector512.Create(limit);
            var edge = end - 32;
            while (i <= edge)
            {
                var vector = Vector512.LoadUnsafe(ref origin, (nuint)i);
                if ((uint)Vector512.ExtractMostSignificantBits(Vector512.GreaterThanOrEqual(vector, boundVector)) != 0)
                {
                    for (var k = 0; k < 32; k++)
                    {
                        var id = ids[i + k];
                        if (id >= bound)
                            throw new ArgumentException($"Timeline id {id} at row {i + k} is not bound in this TimelineSet; ids come from TimelineSet.Add at load time.");
                    }
                    uniform = false;
                }
                else if (uniform && (uint)Vector512.ExtractMostSignificantBits(Vector512.Equals(vector, firstVector)) != 0xFFFF_FFFFu)
                    uniform = false;
                i += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var firstVector = Vector256.Create(first);
            var boundVector = Vector256.Create(limit);
            var edge = end - 16;
            while (i <= edge)
            {
                var vector = Vector256.LoadUnsafe(ref origin, (nuint)i);
                if (Vector256.ExtractMostSignificantBits(Vector256.GreaterThanOrEqual(vector, boundVector)) != 0)
                {
                    for (var k = 0; k < 16; k++)
                    {
                        var id = ids[i + k];
                        if (id >= bound)
                            throw new ArgumentException($"Timeline id {id} at row {i + k} is not bound in this TimelineSet; ids come from TimelineSet.Add at load time.");
                    }
                    uniform = false;
                }
                else if (uniform && Vector256.ExtractMostSignificantBits(Vector256.Equals(vector, firstVector)) != 0xFFFFu)
                    uniform = false;
                i += 16;
            }
        }
        for (; i < end; i++)
        {
            var id = ids[i];
            if (id >= limit)
            {
                if (id >= bound)
                    throw new ArgumentException($"Timeline id {id} at row {i} is not bound in this TimelineSet; ids come from TimelineSet.Add at load time.");
                uniform = false;
            }
            else if (id != first)
                uniform = false;
        }
        return uniform;
    }

    static int RunEnd(Span<ushort> positions, int start, int limit)
    {
        var value = positions[start];
        var end = start + 1;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector512.Create(value);
            var bound = limit - 32;
            while (end <= bound)
            {
                var mask = (uint)Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFF_FFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector256.Create(value);
            var bound = limit - 16;
            while (end <= bound)
            {
                var mask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 16;
            }
        }
        while (end < limit && positions[end] == value) end++;
        return end;
    }

    static int RunEndTwo(ReadOnlySpan<ushort> ids, Span<ushort> positions, int start, int limit)
    {
        var id = ids[start];
        var tick = positions[start];
        var end = start + 1;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var idFirst = ref MemoryMarshal.GetReference(ids);
            ref var positionFirst = ref MemoryMarshal.GetReference(positions);
            var idSearch = Vector512.Create(id);
            var tickSearch = Vector512.Create(tick);
            var bound = limit - 32;
            while (end <= bound)
            {
                var idMask = (uint)Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref idFirst, (nuint)end), idSearch));
                var tickMask = (uint)Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref positionFirst, (nuint)end), tickSearch));
                var mask = idMask & tickMask;
                if (mask != 0xFFFF_FFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var idFirst = ref MemoryMarshal.GetReference(ids);
            ref var positionFirst = ref MemoryMarshal.GetReference(positions);
            var idSearch = Vector256.Create(id);
            var tickSearch = Vector256.Create(tick);
            var bound = limit - 16;
            while (end <= bound)
            {
                var idMask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref idFirst, (nuint)end), idSearch));
                var tickMask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref positionFirst, (nuint)end), tickSearch));
                var mask = idMask & tickMask;
                if (mask != 0xFFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 16;
            }
        }
        while (end < limit && ids[end] == id && positions[end] == tick) end++;
        return end;
    }

    static void Add(Span<float> values, int start, int end, float delta)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(delta);
            var limit = length & ~15;
            for (; i < limit; i += 16) Vector512.Add(Vector512.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(delta);
            var limit = length & ~7;
            for (; i < limit; i += 8) Vector256.Add(Vector256.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    static void Add(Span<long> values, int start, int end, long delta)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(delta);
            var limit = length & ~7;
            for (; i < limit; i += 8) Vector512.Add(Vector512.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(delta);
            var limit = length & ~3;
            for (; i < limit; i += 4) Vector256.Add(Vector256.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    static void Fill(Span<ushort> values, int start, int end, ushort value)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(value);
            var limit = length & ~31;
            for (; i < limit; i += 32) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(value);
            var limit = length & ~15;
            for (; i < limit; i += 16) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] = value;
    }

    static void Zero(Span<long> values, int start, int end)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(0L);
            var limit = length & ~7;
            for (; i < limit; i += 8) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(0L);
            var limit = length & ~3;
            for (; i < limit; i += 4) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] = 0;
    }
}
