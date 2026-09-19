using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tl;

internal sealed unsafe class TimelineSet<TTrack, TClip> : IDisposable
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    internal const ushort Skipped = LaneMovementRecord.Skipped;

    internal unsafe struct Slot
    {
        public float* Forward;
        public float* Backward;
        public float* BackwardByPosition;
        public LaneMovementRecord* ForwardRecords;
        public LaneMovementRecord* BackwardRecords;
        public ushort Duration;
        public ushort Looping;
        public ushort Absent;
    }

    internal Slot* _slots;
    internal float* _data;
    internal LaneMovementRecord* _recordsBase;
    internal uint* _motion;
    internal int _count;
    internal int _holes;
    internal bool _lazyResolve;
    internal bool _anyLooping;
    internal bool _disposed;
    internal ushort _minDuration;
    nuint _floats;
    nuint _records;

    internal int Holes => _holes;
    internal int _pendingCursor;

    internal int PendingCursor => _pendingCursor;

    internal void AdvancePendingCursor(int limit)
    {
        while (_pendingCursor < limit && !IsPending((ushort)_pendingCursor))
            _pendingCursor++;
    }

    internal bool IsFolded(ushort index)
        => index < (uint)_count && _slots[index].Forward != null;

    internal bool IsAbsent(ushort index)
        => index < (uint)_count && _slots[index].Forward == null && _slots[index].Absent != 0;

    internal bool IsPending(ushort index)
        => index < (uint)_count && _slots[index].Forward == null && _slots[index].Absent == 0;

    internal void MarkAbsent(ushort index)
    {
        _slots[index].Absent = 1;
    }

    internal ushort Add(TimelineAsset asset)
    {
        CheckAdd();
        LaneGuards.ValidatePair<TTrack, TClip>(asset);
        using var measured = MeasuredLanes.Measure(asset);
        return Bind(checked((ushort)_count), measured);
    }

    internal ushort Add(TimelineAsset asset, MeasuredLanes measured)
    {
        CheckAdd();
        measured.ValidateBinding(asset);
        LaneGuards.ValidatePair<TTrack, TClip>(asset);
        return Bind(checked((ushort)_count), measured);
    }

    internal ushort AddAt(ushort index, MeasuredLanes measured)
    {
        CheckAdd();
        if (index >= ushort.MaxValue)
            throw new InvalidOperationException("TimelineSet is full; a set holds at most 65536 dense timeline ids.");
        if (index < _count && _slots[index].Forward != null)
            return index;
        return Bind(index, measured);
    }

    void CheckAdd()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_count > ushort.MaxValue)
            throw new InvalidOperationException("TimelineSet is full; a set holds at most 65536 dense timeline ids.");
    }

    ushort Bind(ushort index, MeasuredLanes measured)
    {
        var forward = measured.Forward;
        var backward = measured.Backward;
        var duration = measured.Duration;
        var looping = measured.Looping;
        var previousCount = _count;
        uint capacity;
        if (index >= previousCount)
        {
            capacity = (uint)index + 1;
            _holes += index - previousCount;
            _count = index + 1;
        }
        else
        {
            capacity = (uint)previousCount;
            _holes--;
        }
        var live = _count - _holes;
        if (live == 1 || duration < _minDuration)
            _minDuration = duration;
        nuint ticks = Math.Max(1u, duration);
        nuint floats = _floats + (ticks + 1) * 3;
        nuint records = _records + (ticks + 1) * 2;
        var slotBytes = (nuint)capacity * (nuint)sizeof(Slot);
        var recordOffset = (slotBytes + floats * sizeof(float) + 7u) & ~7u;
        var motionOffset = recordOffset + records * (nuint)sizeof(LaneMovementRecord);
        var block = (byte*)NativeMemory.AlignedAlloc(motionOffset + (nuint)capacity * sizeof(uint), 64);
        var slots = (Slot*)block;
        var floatBase = (float*)(block + slotBytes);
        var recordBase = (LaneMovementRecord*)(block + recordOffset);
        var motionBase = (uint*)(block + motionOffset);
        new Span<Slot>(slots + previousCount, (int)(capacity - (uint)previousCount)).Clear();
        new Span<uint>(motionBase + previousCount, (int)(capacity - (uint)previousCount)).Clear();
        if (previousCount > 0)
        {
            var copySlots = (long)((nuint)previousCount * (nuint)sizeof(Slot));
            Buffer.MemoryCopy(_slots, slots, copySlots, copySlots);
            var floatBytes = (long)(_floats * sizeof(float));
            Buffer.MemoryCopy(_data, floatBase, floatBytes, floatBytes);
            var recordBytes = (long)(_records * (nuint)sizeof(LaneMovementRecord));
            Buffer.MemoryCopy(_recordsBase, recordBase, recordBytes, recordBytes);
            Buffer.MemoryCopy(_motion, motionBase, previousCount * sizeof(uint), previousCount * sizeof(uint));
            var floatShift = (long)((byte*)floatBase - (byte*)_data);
            var recordShift = (long)((byte*)recordBase - (byte*)_recordsBase);
            for (var k = 0; k < previousCount; k++)
            {
                if (slots[k].Forward == null) continue;
                slots[k].Forward = (float*)((byte*)slots[k].Forward + floatShift);
                slots[k].Backward = (float*)((byte*)slots[k].Backward + floatShift);
                slots[k].BackwardByPosition = (float*)((byte*)slots[k].BackwardByPosition + floatShift);
                slots[k].ForwardRecords = (LaneMovementRecord*)((byte*)slots[k].ForwardRecords + recordShift);
                slots[k].BackwardRecords = (LaneMovementRecord*)((byte*)slots[k].BackwardRecords + recordShift);
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
        LaneMovement.Bake(forwardTable, backwardTable, duration, looping, forwardRecords, backwardRecords, backwardByPosition);
        slots[index] = new Slot
        {
            Forward = forwardTable,
            Backward = backwardTable,
            BackwardByPosition = backwardByPosition,
            ForwardRecords = forwardRecords,
            BackwardRecords = backwardRecords,
            Duration = duration,
            Looping = looping ? (ushort)1 : (ushort)0,
        };
        motionBase[index] = duration | (looping ? 0x80000000u : 0u);
        var previous = _slots;
        _slots = slots;
        _data = floatBase;
        _recordsBase = recordBase;
        _motion = motionBase;
        _floats = floats;
        _records = records;
        _anyLooping |= looping;
        if (previous != null)
            NativeMemory.AlignedFree(previous);
        return index;
    }

    internal TimelineSetLane<TTrack, TClip> Gather(ReadOnlySpan<ushort> timelineIds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(this, timelineIds, default, false);
    }

    internal void Apply(ReadOnlySpan<ushort> timelineIds, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => Gather(timelineIds).Seek(positions, forward).Apply(effects);

    internal void Apply(ReadOnlySpan<ushort> timelineIds, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => Gather(timelineIds).Seek(positions, forward).Apply(effects, next);

    internal void Step(ReadOnlySpan<ushort> ids, Span<ushort> positions, bool forward)
        => Step(ids, positions, positions, forward);

    internal void Step(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ids.Length != positions.Length || positions.Length != next.Length)
            throw new ArgumentException("Column length must equal position count.");
        var count = positions.Length;
        var motion = _motion;
        var slots = _slots;
        var bound = _count;
        var reverse = !forward;
        var i = 0;
        var boundVector = Vector256.Create((ushort)bound);
        if (Avx2.IsSupported)
        {
            var blockEnd = count - (count & 15);
            while (i < blockEnd)
            {
                var block = i + 16;
                var idv = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(ids), (nuint)i);
                if (Vector256.LessThanAll(idv, boundVector))
                {
                    if (Vector256.EqualsAll(idv, Vector256.Create(ids[i])))
                    {
                        var m = motion[ids[i]];
                        if (forward) LaneOps.StepForward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
                        else LaneOps.StepBackward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
                    }
                    else LaneOps.StepRows(motion, ids, positions, next, forward, i, block);
                }
                else
                {
                    for (var k = i; k < block; k++) StepRow(ids, positions, next, motion, slots, bound, reverse, k);
                }
                i = block;
            }
        }
        for (; i < count; i++) StepRow(ids, positions, next, motion, slots, bound, reverse, i);
    }

    static unsafe void StepRow(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, uint* motion, Slot* slots, int bound, bool reverse, int i)
    {
        var id = ids[i];
        if (id >= bound || slots[id].Forward == null)
        {
            next[i] = positions[i];
            return;
        }
        var m = motion[id];
        next[i] = TimelineMovement.Advance((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, reverse, positions[i], out var np, out _, out _)
            ? np
            : positions[i];
    }

    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects);

    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects, next);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var previous = _slots;
        _slots = null;
        _data = null;
        _recordsBase = null;
        _count = 0;
        _holes = 0;
        _minDuration = 0;
        if (previous != null)
            NativeMemory.AlignedFree(previous);
    }
}

internal ref struct TimelineSetLane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    const int Chunk = 4096;
    const int MinSegment = 128;

    readonly TimelineSet<TTrack, TClip> _set;
    readonly ReadOnlySpan<ushort> _ids;
    readonly ReadOnlySpan<ushort> _positions;
    readonly bool _forward;

    internal TimelineSetLane(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, bool forward)
    {
        _set = set;
        _ids = ids;
        _positions = positions;
        _forward = forward;
    }

    internal TimelineSetLane<TTrack, TClip> Seek(ReadOnlySpan<ushort> positions, bool forward)
        => new(_set, _ids, positions, forward);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe void Apply(Span<float> effects, Span<ushort> next)
    {
        var set = _set;
        if (set._disposed)
            throw new ObjectDisposedException(nameof(TimelineSet<TTrack, TClip>));
        var ids = _ids;
        var positions = _positions;
        var hasNext = !next.IsEmpty;
        if (ids.Length != positions.Length || effects.Length != positions.Length || (hasNext && next.Length != positions.Length))
            throw new ArgumentException("Column length must equal position count.");
        var sameClock = hasNext && Unsafe.AreSame(ref MemoryMarshal.GetReference(positions), ref MemoryMarshal.GetReference(next));
        if (MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(positions))
            || MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(ids).Overlaps(MemoryMarshal.AsBytes(next))
            || MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(effects).Overlaps(MemoryMarshal.AsBytes(next))
            || (!sameClock && MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(next))))
            throw new ArgumentException("Lane columns must not overlap.");
        var count = positions.Length;
        if (count == 0) return;
        var slots = set._slots;
        var bound = set._count;
        var minDuration = set._minDuration;
        var lazy = set._lazyResolve;
        var holy = lazy && set._holes != 0;
        var gather = Avx2.IsSupported;
        var forward = _forward;
        var i = 0;
        var uniformId = -1;
        TimelineSet<TTrack, TClip>.Slot* slot = null;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            var first = ids[i];
            if (UniformChunk(ids, i, chunkEnd, first))
            {
                if (first >= bound || slots[first].Forward == null)
                {
                    if (!lazy) ThrowUnboundId(first, i);
                    Timeline<TTrack, TClip>.Resolve(first);
                    slots = set._slots;
                    bound = set._count;
                    minDuration = set._minDuration;
                    holy = set._holes != 0;
                    uniformId = -1;
                    slot = null;
                }
                if (first != uniformId)
                {
                    slot = slots + first;
                    uniformId = first;
                }
                i = ApplyUniformSegment(slot, positions, next, effects, i, chunkEnd, forward, gather);
            }
            else
            {
                if (holy)
                    while (Timeline<TTrack, TClip>.ResolveChunk(set, ids, i, chunkEnd))
                    {
                        slots = set._slots;
                        bound = set._count;
                        minDuration = set._minDuration;
                        uniformId = -1;
                        slot = null;
                    }
                if (FastMixedChunk(ids, positions, i, chunkEnd, bound, minDuration))
                {
                    i = forward
                        ? FastMixedForward(ids, positions, next, effects, slots, i, chunkEnd)
                        : FastMixedBackward(ids, positions, next, effects, slots, i, chunkEnd);
                    continue;
                }
                if (ValidateChunk(ids, i, chunkEnd, set, bound))
                {
                    slots = set._slots;
                    bound = set._count;
                    minDuration = set._minDuration;
                    holy = set._holes != 0;
                    uniformId = -1;
                    slot = null;
                }
                while (true)
                {
                    var segment = LaneOps.RunEnd(ids, i, chunkEnd);
                    if (segment - i < MinSegment)
                    {
                        i = forward
                            ? ApplyMixedForward(ids, positions, next, effects, slots, i, chunkEnd)
                            : ApplyMixedBackward(ids, positions, next, effects, slots, i, chunkEnd);
                        break;
                    }
                    i = ApplyUniformSegment(slots + ids[i], positions, next, effects, i, segment, forward, gather);
                    if (i >= chunkEnd) break;
                }
                continue;
            }
        }
    }






    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe void Apply(Span<float> effects) => Apply(effects, default);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]

    internal unsafe void ApplySlot(ushort index, Span<float> effects)
    {
        var set = _set;
        if (set._disposed)
            throw new ObjectDisposedException(nameof(TimelineSet<TTrack, TClip>));
        var positions = _positions;
        if (effects.Length != positions.Length)
            throw new ArgumentException("Column length must equal position count.");
        if (MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects)))
            throw new ArgumentException("Lane columns must not overlap.");
        var count = positions.Length;
        if (count == 0) return;
        var slot = set._slots + index;
        var gather = Avx2.IsSupported;
        var forward = _forward;
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            i = ApplyUniformSegment(slot, positions, default, effects, i, chunkEnd, forward, gather);
        }
    }

    internal unsafe void ApplySlot(ushort index, Span<float> effects, Span<ushort> next)
    {
        var set = _set;
        if (set._disposed)
            throw new ObjectDisposedException(nameof(TimelineSet<TTrack, TClip>));
        var positions = _positions;
        var hasNext = !next.IsEmpty;
        if (effects.Length != positions.Length || (hasNext && next.Length != positions.Length))
            throw new ArgumentException("Column length must equal position count.");
        var sameClock = hasNext && Unsafe.AreSame(ref MemoryMarshal.GetReference(positions), ref MemoryMarshal.GetReference(next));
        if (MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(effects).Overlaps(MemoryMarshal.AsBytes(next))
            || (!sameClock && MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(next))))
            throw new ArgumentException("Lane columns must not overlap.");
        var count = positions.Length;
        if (count == 0) return;
        var slot = set._slots + index;
        var gather = Avx2.IsSupported;
        var forward = _forward;
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            i = ApplyUniformSegment(slot, positions, next, effects, i, chunkEnd, forward, gather);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformSegment(TimelineSet<TTrack, TClip>.Slot* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit, bool forward, bool gather)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        if (gather && duration > 1 && (looping || ShortRuns(positions, i, limit)))
        {
            var blockEnd = i + ((limit - i) >> 4 << 4);
            if (blockEnd > i)
            {
                if (forward)
                {
                    if (duration <= 8)
                        LaneOps.EffPermuteForward(slot->Forward, duration, looping, positions, next, effects, i, blockEnd);
                    else
                        LaneOps.EffectForward(slot->Forward, duration, looping, positions, next, effects, i, blockEnd);
                }
                else
                {
                    if (duration <= 8)
                        LaneOps.EffPermuteBackward(slot->Backward, duration, looping, positions, next, effects, i, blockEnd);
                    else
                        LaneOps.EffectBackward(slot->BackwardByPosition, duration, looping, positions, next, effects, i, blockEnd);
                }
                i = blockEnd;
            }
        }
        return forward
            ? ApplyUniformForward(slot, positions, next, effects, i, limit)
            : ApplyUniformBackward(slot, positions, next, effects, i, limit);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformForward(TimelineSet<TTrack, TClip>.Slot* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var eff = slot->Forward;
        var records = slot->ForwardRecords;
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var position = positions[i];
            var end = i + 1 >= limit || positions[i + 1] != position ? i + 1 : LaneOps.RunEnd(positions, i, limit);
            if (position >= duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            var delta = eff[position];
            if (end == i + 1)
            {
                effects[i] += delta;
                if (hasNext) next[i] = looping && position + 1 == duration ? (ushort)0 : (ushort)(position + 1);
            }
            else
            {
                LaneOps.Add(effects, i, end, delta);
                if (hasNext) LaneOps.Fill(next, i, end, looping && position + 1 == duration ? (ushort)0 : (ushort)(position + 1));
            }
            i = end;
            while (i < limit && (i + 1 >= limit || positions[i + 1] != positions[i]))
            {
                var p = positions[i];
                if (p < duration)
                {
                    ref var r = ref records[p];
                    effects[i] += r.Effect;
                    if (hasNext) next[i] = r.Next;
                }
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformBackward(TimelineSet<TTrack, TClip>.Slot* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var eff = slot->Backward;
        var records = slot->BackwardRecords;
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var position = positions[i];
            var end = i + 1 >= limit || positions[i + 1] != position ? i + 1 : LaneOps.RunEnd(positions, i, limit);
            if (position == 0 && !looping || position > duration || looping && position == duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            var tick = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
            var delta = eff[tick];
            if (end == i + 1)
            {
                effects[i] += delta;
                if (hasNext) next[i] = tick;
            }
            else
            {
                LaneOps.Add(effects, i, end, delta);
                if (hasNext) LaneOps.Fill(next, i, end, tick);
            }
            i = end;
            while (i < limit && (i + 1 >= limit || positions[i + 1] != positions[i]))
            {
                var p = positions[i];
                if (p <= duration && records[p].Next != TimelineSet<TTrack, TClip>.Skipped)
                {
                    effects[i] += records[p].Effect;
                    if (hasNext) next[i] = records[p].Next;
                }
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
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
                    if (hasNext) next[i] = r.Next;
                }
                else if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + id;
            var duration = slot->Duration;
            if (position >= duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            LaneOps.Add(effects, i, end, slot->Forward[position]);
            if (hasNext) LaneOps.Fill(next, i, end, slot->Looping != 0 && position + 1 == duration ? (ushort)0 : (ushort)(position + 1));
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
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
                    if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
                    {
                        effects[i] += r.Effect;
                        if (hasNext) next[i] = r.Next;
                    }
                    else if (hasNext) next[i] = position;
                }
                else if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            var slot = slots + id;
            var duration = slot->Duration;
            var looping = slot->Looping != 0;
            if (position == 0 && !looping || position > duration || looping && position == duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            var tick = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
            LaneOps.Add(effects, i, end, slot->Backward[tick]);
            if (hasNext) LaneOps.Fill(next, i, end, tick);
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int FastMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        var lastId = -1;
        LaneMovementRecord* lastRecords = null;
        while (i < limit)
        {
            var id = ids[i];
            if (id != lastId)
            {
                lastId = id;
                lastRecords = slots[id].ForwardRecords;
            }
            ref var r = ref lastRecords[positions[i]];
            effects[i] += r.Effect;
            if (hasNext) next[i] = r.Next;
            i++;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int FastMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, TimelineSet<TTrack, TClip>.Slot* slots, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        var lastId = -1;
        LaneMovementRecord* lastRecords = null;
        while (i < limit)
        {
            var id = ids[i];
            if (id != lastId)
            {
                lastId = id;
                lastRecords = slots[id].BackwardRecords;
            }
            var p = positions[i];
            ref var r = ref lastRecords[p];
            if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
            {
                effects[i] += r.Effect;
                if (hasNext) next[i] = r.Next;
            }
            else if (hasNext) next[i] = p;
            i++;
        }
        return limit;
    }

    static bool ShortRuns(ReadOnlySpan<ushort> positions, int start, int end)
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

    static bool UniformChunk(ReadOnlySpan<ushort> ids, int start, int end, ushort first)
    {
        var i = start;
        if (Vector.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var search = new Vector<ushort>(first);
            var edge = end - Vector<ushort>.Count;
            while (i <= edge)
            {
                if (Vector.Xor(Vector.LoadUnsafe(ref origin, (nuint)i), search) != Vector<ushort>.Zero) return false;
                i += Vector<ushort>.Count;
            }
        }
        while (i < end)
        {
            if (ids[i] != first) return false;
            i++;
        }
        return true;
    }

    static unsafe bool ValidateChunk(ReadOnlySpan<ushort> ids, int start, int end, TimelineSet<TTrack, TClip> set, int bound)
    {
        if (bound >= 65536) return false;
        var grew = false;
        var i = start;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var boundVector = Vector512.Create((ushort)bound);
            var edge = end - 32;
            var over = Vector512<ushort>.Zero;
            while (i <= edge)
            {
                over |= Vector512.GreaterThanOrEqual(Vector512.LoadUnsafe(ref origin, (nuint)i), boundVector);
                i += 32;
            }
            if (over != Vector512<ushort>.Zero)
                for (var k = start; k < end; k++)
                {
                    var id = ids[k];
                    if (id >= bound) grew |= ResolveOrThrow(set, id, k);
                }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var boundVector = Vector256.Create((ushort)bound);
            var edge = end - 16;
            var over = Vector256<ushort>.Zero;
            while (i <= edge)
            {
                over |= Vector256.GreaterThanOrEqual(Vector256.LoadUnsafe(ref origin, (nuint)i), boundVector);
                i += 16;
            }
            if (over != Vector256<ushort>.Zero)
                for (var k = start; k < end; k++)
                {
                    var id = ids[k];
                    if (id >= bound) grew |= ResolveOrThrow(set, id, k);
                }
        }
        while (i < end)
        {
            var id = ids[i];
            if (id >= bound) grew |= ResolveOrThrow(set, id, i);
            i++;
        }
        return grew;
    }

    static bool ResolveOrThrow(TimelineSet<TTrack, TClip> set, ushort id, int row)
    {
        if (set._lazyResolve)
        {
            Timeline<TTrack, TClip>.Resolve(id);
            return true;
        }
        ThrowUnboundId(id, row);
        return false;
    }

    static unsafe bool FastMixedChunk(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, int start, int end, int bound, int minDuration)
    {
        if (bound <= 0) return false;
        var boundLimit = bound >= 65536 ? (ushort)65535 : (ushort)bound;
        var i = start;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var idOrigin = ref MemoryMarshal.GetReference(ids);
            ref var positionOrigin = ref MemoryMarshal.GetReference(positions);
            var boundVector = Vector512.Create(boundLimit);
            var limit = Vector512.Create((ushort)minDuration);
            var highest = Vector512<ushort>.Zero;
            while (i + 32 < end)
            {
                var id = Vector512.LoadUnsafe(ref idOrigin, (nuint)i);
                if (Vector512.GreaterThanOrEqual(id, boundVector) != Vector512<ushort>.Zero) return false;
                highest = Vector512.Max(highest, Vector512.LoadUnsafe(ref positionOrigin, (nuint)i));
                if (Vector512.ExtractMostSignificantBits(Vector512.GreaterThanOrEqual(highest, limit)) != 0) return false;
                i += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var idOrigin = ref MemoryMarshal.GetReference(ids);
            ref var positionOrigin = ref MemoryMarshal.GetReference(positions);
            var boundVector = Vector256.Create(boundLimit);
            var limit = Vector256.Create((ushort)minDuration);
            var highest = Vector256<ushort>.Zero;
            while (i + 16 < end)
            {
                var id = Vector256.LoadUnsafe(ref idOrigin, (nuint)i);
                if (Vector256.GreaterThanOrEqual(id, boundVector) != Vector256<ushort>.Zero) return false;
                highest = Vector256.Max(highest, Vector256.LoadUnsafe(ref positionOrigin, (nuint)i));
                if (Vector256.ExtractMostSignificantBits(Vector256.GreaterThanOrEqual(highest, limit)) != 0) return false;
                i += 16;
            }
        }
        for (var k = i; k < end; k++)
        {
            if (ids[k] >= boundLimit) return false;
            if (positions[k] >= minDuration) return false;
        }
        return true;
    }

    static void ThrowUnboundId(ushort id, int row)
        => throw new ArgumentException($"Timeline id {id} at row {row} is not bound in this TimelineSet; ids come from TimelineSet.Add at load time, and the pair-typed bank resolves timeline indices on the first typed advance.");

    static int RunEndTwo(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, int start, int limit)
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
}
