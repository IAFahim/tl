using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tl;

public sealed unsafe class TimelineSet<TTrack, TClip> : IDisposable
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    internal const long Skipped = long.MinValue;

    internal unsafe struct Slot
    {
        public float* Forward;
        public float* Backward;
        public float* BackwardByPosition;
        public MovementRecord* ForwardRecords;
        public MovementRecord* BackwardRecords;
        public ushort Duration;
        public ushort Looping;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MovementRecord
    {
        public float Effect;
        public ushort Next;
        public ushort Pad;
        public long CycleDelta;
    }

    internal Slot* _slots;
    internal float* _data;
    internal MovementRecord* _recordsBase;
    internal int _count;
    internal bool _anyLooping;
    internal bool _disposed;
    nuint _floats;
    nuint _records;

    public ushort Add(TimelineAsset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_count > ushort.MaxValue)
            throw new InvalidOperationException("TimelineSet is full; a set holds at most 65536 dense timeline ids.");
        LaneGuards.ValidatePair<TTrack, TClip>(asset);
        using var measured = MeasuredLanes.Measure(asset);
        return Bind(measured);
    }

    public ushort Add(TimelineAsset asset, MeasuredLanes measured)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_count > ushort.MaxValue)
            throw new InvalidOperationException("TimelineSet is full; a set holds at most 65536 dense timeline ids.");
        measured.ValidateBinding(asset);
        LaneGuards.ValidatePair<TTrack, TClip>(asset);
        return Bind(measured);
    }

    ushort Bind(MeasuredLanes measured)
    {
        var forward = measured.Forward;
        var backward = measured.Backward;
        var duration = measured.Duration;
        var looping = measured.Looping;
        nuint ticks = Math.Max(1u, duration);
        nuint floats = _floats + (ticks + 1) * 3;
        nuint records = _records + (ticks + 1) * 2;
        var slotBytes = (nuint)(_count + 1) * (nuint)sizeof(Slot);
        var recordOffset = (slotBytes + floats * sizeof(float) + 7u) & ~7u;
        var block = (byte*)NativeMemory.AlignedAlloc(recordOffset + records * (nuint)sizeof(MovementRecord), 64);
        var slots = (Slot*)block;
        var floatBase = (float*)(block + slotBytes);
        var recordBase = (MovementRecord*)(block + recordOffset);
        if (_count > 0)
        {
            Buffer.MemoryCopy(_slots, slots, (long)slotBytes, (long)slotBytes);
            var floatBytes = (long)(_floats * sizeof(float));
            Buffer.MemoryCopy(_data, floatBase, floatBytes, floatBytes);
            var recordBytes = (long)(_records * (nuint)sizeof(MovementRecord));
            Buffer.MemoryCopy(_recordsBase, recordBase, recordBytes, recordBytes);
            var floatShift = (long)((byte*)floatBase - (byte*)_data);
            var recordShift = (long)((byte*)recordBase - (byte*)_recordsBase);
            for (var k = 0; k < _count; k++)
            {
                slots[k].Forward = (float*)((byte*)slots[k].Forward + floatShift);
                slots[k].Backward = (float*)((byte*)slots[k].Backward + floatShift);
                slots[k].BackwardByPosition = (float*)((byte*)slots[k].BackwardByPosition + floatShift);
                slots[k].ForwardRecords = (MovementRecord*)((byte*)slots[k].ForwardRecords + recordShift);
                slots[k].BackwardRecords = (MovementRecord*)((byte*)slots[k].BackwardRecords + recordShift);
            }
        }
        var forwardTable = floatBase + _floats;
        var backwardTable = forwardTable + ticks + 1;
        var backwardByPosition = backwardTable + ticks + 1;
        var forwardRecords = recordBase + _records;
        var backwardRecords = forwardRecords + ticks + 1;
        var tableBytes = (long)(ticks * sizeof(float));
        Buffer.MemoryCopy(forward, forwardTable, tableBytes, tableBytes);
        Buffer.MemoryCopy(backward, backwardTable, tableBytes, tableBytes);
        forwardTable[duration] = 0f;
        backwardTable[duration] = 0f;
        for (var p = 0; p <= duration; p++)
        {
            if (p < duration)
            {
                var next = p + 1;
                var wraps = looping && next == duration;
                forwardRecords[p] = new MovementRecord
                {
                    Effect = forwardTable[p],
                    Next = wraps ? (ushort)0 : (ushort)next,
                    CycleDelta = looping && wraps ? 1L : 0L,
                };
                if (p == 0)
                {
                    if (looping)
                    {
                        backwardRecords[p] = new MovementRecord { Effect = backwardTable[duration - 1], Next = (ushort)(duration - 1), CycleDelta = -1 };
                        backwardByPosition[0] = backwardTable[duration - 1];
                    }
                    else
                    {
                        backwardRecords[p] = new MovementRecord { Effect = 0f, Next = 0, CycleDelta = TimelineSet<TTrack, TClip>.Skipped };
                        backwardByPosition[0] = 0f;
                    }
                }
                else
                {
                    backwardRecords[p] = new MovementRecord { Effect = backwardTable[p - 1], Next = (ushort)(p - 1), CycleDelta = 0L };
                    backwardByPosition[p] = backwardTable[p - 1];
                }
            }
            else
            {
                forwardRecords[p] = new MovementRecord { Effect = 0f, Next = (ushort)p, CycleDelta = TimelineSet<TTrack, TClip>.Skipped };
                if (looping)
                {
                    backwardRecords[p] = new MovementRecord { Effect = 0f, Next = (ushort)p, CycleDelta = TimelineSet<TTrack, TClip>.Skipped };
                    backwardByPosition[p] = 0f;
                }
                else if (duration == 0)
                {
                    backwardRecords[p] = new MovementRecord { Effect = 0f, Next = 0, CycleDelta = TimelineSet<TTrack, TClip>.Skipped };
                    backwardByPosition[p] = 0f;
                }
                else
                {
                    backwardRecords[p] = new MovementRecord { Effect = backwardTable[duration - 1], Next = (ushort)(duration - 1), CycleDelta = 0L };
                    backwardByPosition[p] = backwardTable[duration - 1];
                }
            }
        }
        slots[_count] = new Slot
        {
            Forward = forwardTable,
            Backward = backwardTable,
            BackwardByPosition = backwardByPosition,
            ForwardRecords = forwardRecords,
            BackwardRecords = backwardRecords,
            Duration = duration,
            Looping = looping ? (ushort)1 : (ushort)0,
        };
        var previous = _slots;
        _slots = slots;
        _data = floatBase;
        _recordsBase = recordBase;
        _floats = floats;
        _records = records;
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
        _recordsBase = null;
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
        var gather = Avx2.IsSupported;
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            if (ProbeChunk(ids, i, chunkEnd, bound))
            {
                var slot = slots + ids[i];
                if (gather && slot->Duration > 1 && (slot->Looping != 0
                        ? SingletonChunk(positions, i, chunkEnd)
                        : ShortRuns(positions, i, chunkEnd)))
                {
                    var blockEnd = i + ((chunkEnd - i) >> 4 << 4);
                    if (blockEnd > i)
                    {
                        if (_forward)
                        {
                            if (slot->Looping != 0)
                                GatherForward(slot, positions, effects, cycles, i, blockEnd);
                            else
                                GatherFiniteForward(slot, positions, effects, cycles, i, blockEnd);
                        }
                        else
                        {
                            if (slot->Looping != 0)
                                GatherBackward(slot, positions, effects, cycles, i, blockEnd);
                            else
                                GatherFiniteBackward(slot, positions, effects, cycles, i, blockEnd);
                        }
                        i = blockEnd;
                        continue;
                    }
                }
                i = _forward
                    ? ApplyUniformForward(slot, positions, effects, cycles, i, chunkEnd)
                    : ApplyUniformBackward(slot, positions, effects, cycles, i, chunkEnd);
            }
            else
            {
                i = _forward
                    ? ApplyMixedForward(ids, positions, effects, cycles, slots, i, chunkEnd)
                    : ApplyMixedBackward(ids, positions, effects, cycles, slots, i, chunkEnd);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformForward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var eff = slot->Forward;
        var records = slot->ForwardRecords;
        var touchCycles = !cycles.IsEmpty;
        while (i < limit)
        {
            var position = positions[i];
            var end = i + 1 >= limit || positions[i + 1] != position ? i + 1 : RunEnd(positions, i, limit);
            if (position >= duration) { i = end; continue; }
            var delta = eff[position];
            var nextTick = (ushort)(position + 1);
            if (looping)
            {
                if (nextTick == duration)
                {
                    nextTick = 0;
                    if (end == i + 1)
                    {
                        effects[i] += delta;
                        positions[i] = nextTick;
                        if (touchCycles) cycles[i] += 1;
                    }
                    else
                    {
                        Add(effects, i, end, delta);
                        Fill(positions, i, end, nextTick);
                        if (touchCycles) AddLong(cycles, i, end, 1);
                    }
                    i = end;
                    continue;
                }
                if (end == i + 1)
                {
                    effects[i] += delta;
                    positions[i] = nextTick;
                }
                else
                {
                    Add(effects, i, end, delta);
                    Fill(positions, i, end, nextTick);
                }
            }
            else
            {
                if (end == i + 1)
                {
                    effects[i] += delta;
                    positions[i] = nextTick;
                    if (touchCycles) cycles[i] = 0;
                }
                else
                {
                    Add(effects, i, end, delta);
                    Fill(positions, i, end, nextTick);
                    if (touchCycles) Zero(cycles, i, end);
                }
            }
            i = end;
            while (i < limit && (i + 1 >= limit || positions[i + 1] != positions[i]))
            {
                var p = positions[i];
                if (p < duration)
                {
                    ref var r = ref records[p];
                    effects[i] += r.Effect;
                    positions[i] = r.Next;
                    if (touchCycles)
                    {
                        if (looping)
                        {
                            if (r.CycleDelta != 0) cycles[i] += r.CycleDelta;
                        }
                        else
                            cycles[i] = 0;
                    }
                }
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformBackward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var eff = slot->Backward;
        var records = slot->BackwardRecords;
        var touchCycles = !cycles.IsEmpty;
        while (i < limit)
        {
            var position = positions[i];
            var end = i + 1 >= limit || positions[i + 1] != position ? i + 1 : RunEnd(positions, i, limit);
            if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
            ushort tick;
            long cycleDelta;
            if (position == 0) { tick = (ushort)(duration - 1); cycleDelta = -1; }
            else { tick = (ushort)(position - 1); cycleDelta = 0; }
            var delta = eff[tick];
            var nextTick = tick;
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = nextTick;
                if (!looping && touchCycles) cycles[i] = 0;
                else if (cycleDelta != 0 && touchCycles) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, nextTick);
                if (!looping && touchCycles) Zero(cycles, i, end);
                else if (cycleDelta != 0 && touchCycles) AddLong(cycles, i, end, cycleDelta);
            }
            i = end;
            while (i < limit && (i + 1 >= limit || positions[i + 1] != positions[i]))
            {
                var p = positions[i];
                var c = p <= duration ? records[p].CycleDelta : TimelineSet<TTrack, TClip>.Skipped;
                if (c != TimelineSet<TTrack, TClip>.Skipped)
                {
                    ref var r = ref records[p];
                    effects[i] += r.Effect;
                    positions[i] = r.Next;
                    if (touchCycles)
                    {
                        if (looping)
                        {
                            if (c != 0) cycles[i] += c;
                        }
                        else
                            cycles[i] = 0;
                    }
                }
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedForward(ReadOnlySpan<ushort> ids, Span<ushort> positions, Span<float> effects, Span<long> cycles, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var touchCycles = !cycles.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                var m = slots + id;
                if (position < m->Duration)
                {
                    ref var r = ref m->ForwardRecords[position];
                    effects[i] += r.Effect;
                    positions[i] = r.Next;
                    if (touchCycles)
                    {
                        if (m->Looping != 0)
                            cycles[i] += r.CycleDelta;
                        else
                            cycles[i] = 0;
                    }
                }
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + id;
            var duration = slot->Duration;
            if (position >= duration) { i = end; continue; }
            var delta = slot->Forward[position];
            var next = (ushort)(position + 1);
            long cycleDelta = 0;
            var looping = slot->Looping != 0;
            if (looping)
            {
                if (next == duration) { next = 0; cycleDelta = 1; }
            }
            Add(effects, i, end, delta);
            Fill(positions, i, end, next);
            if (touchCycles)
            {
                if (!looping) Zero(cycles, i, end);
                else if (cycleDelta != 0) AddLong(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedBackward(ReadOnlySpan<ushort> ids, Span<ushort> positions, Span<float> effects, Span<long> cycles, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var touchCycles = !cycles.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                var m = slots + id;
                if (position <= m->Duration)
                {
                    ref var r = ref m->BackwardRecords[position];
                    if (r.CycleDelta != TimelineSet<TTrack, TClip>.Skipped)
                    {
                        effects[i] += r.Effect;
                        positions[i] = r.Next;
                        if (touchCycles)
                        {
                            if (m->Looping != 0)
                                cycles[i] += r.CycleDelta;
                            else
                                cycles[i] = 0;
                        }
                    }
                }
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + id;
            var duration = slot->Duration;
            var looping = slot->Looping != 0;
            if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
            ushort tick;
            long cycleDelta;
            if (position == 0) { tick = (ushort)(duration - 1); cycleDelta = -1; }
            else { tick = (ushort)(position - 1); cycleDelta = 0; }
            var delta = slot->Backward[tick];
            Add(effects, i, end, delta);
            Fill(positions, i, end, tick);
            if (touchCycles)
            {
                if (!looping) Zero(cycles, i, end);
                else if (cycleDelta != 0) AddLong(cycles, i, end, cycleDelta);
            }
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void GatherForward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var eff = slot->Forward;
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var touchCycles = !cycles.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var wrapMask = Vector256.Equals(pos, lastVector);
            var skipMask = Vector256.GreaterThan(pos, lastVector);
            var next = pos + one;
            next = Vector256.ConditionalSelect(wrapMask, zero, next);
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

            var clamped = Vector256.Min(pos, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));

            if (touchCycles)
            {
                var wrapBits = Vector256.ExtractMostSignificantBits(wrapMask);
                while (wrapBits != 0)
                {
                    var lane = BitOperations.TrailingZeroCount(wrapBits);
                    cycles[i + lane] += 1;
                    wrapBits &= wrapBits - 1;
                }
            }
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void GatherBackward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var eff = slot->BackwardByPosition;
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var touchCycles = !cycles.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var wrapMask = Vector256.Equals(pos, zero);
            var skipMask = Vector256.GreaterThan(pos, durationVector) | Vector256.Equals(pos, durationVector);
            var next = pos + step;
            next = Vector256.ConditionalSelect(wrapMask, lastVector, next);
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

            var clamped = Vector256.Min(pos, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));

            if (touchCycles)
            {
                var wrapBits = Vector256.ExtractMostSignificantBits(wrapMask);
                while (wrapBits != 0)
                {
                    var lane = BitOperations.TrailingZeroCount(wrapBits);
                    cycles[i + lane] -= 1;
                    wrapBits &= wrapBits - 1;
                }
            }
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void GatherFiniteForward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var eff = slot->Forward;
        var last = (ushort)(duration - 1);
        var lastVector = Vector256.Create(last);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var one = Vector256.Create((ushort)1);
        var zeroLong = Vector256<long>.Zero;
        var touchCycles = !cycles.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        ref var c = ref MemoryMarshal.GetReference(cycles);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var skipMask = Vector256.GreaterThan(pos, lastVector);
            var next = pos + one;
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

            var clamped = Vector256.Min(pos, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));

            if (touchCycles)
            {
                var moveBits = Vector256.ExtractMostSignificantBits(skipMask) ^ 0xFFFF;
                if (moveBits == 0xFFFFu)
                {
                    zeroLong.StoreUnsafe(ref c, (nuint)i);
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 4));
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 8));
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 12));
                }
                else
                {
                    var bits = moveBits;
                    while (bits != 0)
                    {
                        var lane = BitOperations.TrailingZeroCount(bits);
                        Unsafe.Add(ref c, (nuint)(i + lane)) = 0;
                        bits &= bits - 1;
                    }
                }
            }
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void GatherFiniteBackward(TimelineSet<TTrack, TClip>.Slot* slot, Span<ushort> positions, Span<float> effects, Span<long> cycles, int i, int limit)
    {
        var duration = slot->Duration;
        var eff = slot->BackwardByPosition;
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var zero = Vector256<ushort>.Zero;
        var zeroUint = Vector256<uint>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var zeroLong = Vector256<long>.Zero;
        var touchCycles = !cycles.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        ref var c = ref MemoryMarshal.GetReference(cycles);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var skipMask = Vector256.Equals(pos, zero) | Vector256.GreaterThan(pos, durationVector);
            var next = pos + step;
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

            var clamped = Vector256.Min(pos, durationVector);
            (var posLo, var posHi) = Vector256.Widen(pos);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = (Vector256.Equals(posLo, zeroUint) | Vector256.GreaterThan(posLo, durationWide)).AsSingle();
            var skipHi = (Vector256.Equals(posHi, zeroUint) | Vector256.GreaterThan(posHi, durationWide)).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));

            if (touchCycles)
            {
                var moveBits = Vector256.ExtractMostSignificantBits(skipMask) ^ 0xFFFF;
                if (moveBits == 0xFFFFu)
                {
                    zeroLong.StoreUnsafe(ref c, (nuint)i);
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 4));
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 8));
                    zeroLong.StoreUnsafe(ref c, (nuint)(i + 12));
                }
                else
                {
                    var bits = moveBits;
                    while (bits != 0)
                    {
                        var lane = BitOperations.TrailingZeroCount(bits);
                        Unsafe.Add(ref c, (nuint)(i + lane)) = 0;
                        bits &= bits - 1;
                    }
                }
            }
            i += 16;
        }
    }

    static bool ShortRuns(Span<ushort> positions, int start, int end)
    {
        var probe = start + 64;
        if (probe > end) probe = end;
        ref var origin = ref MemoryMarshal.GetReference(positions);
        var equalPairs = 0;
        var j = start;
        if (Vector256.IsHardwareAccelerated)
        {
            var vectorLimit = probe - 17;
            while (j <= vectorLimit)
            {
                equalPairs += BitOperations.PopCount(Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref origin, (nuint)j), Vector256.LoadUnsafe(ref origin, (nuint)(j + 1)))));
                j += 16;
            }
        }
        for (var k = j; k < probe - 1; k++)
            if (positions[k] == positions[k + 1]) equalPairs++;
        return equalPairs <= 24;
    }

    static bool SingletonChunk(Span<ushort> positions, int start, int end)
    {
        var probe = start + 64;
        if (probe > end) probe = end;
        for (var i = start + 1; i < probe; i++)
            if (positions[i] == positions[i - 1]) return false;
        return true;
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

    static void AddLong(Span<long> values, int start, int end, long delta)
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
