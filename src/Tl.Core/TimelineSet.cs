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
    const int InitialCapacity = 1024;
    const int BlockHeaderBytes = 64;
    const int TableBytesPerTick = 28;
    const int RetirePadBytes = 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal SlotView* FoldedView(ushort index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return index < (uint)_count ? (SlotView*)Volatile.Read(ref *(long*)(_views + index)) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplyRecords(SlotView* slot, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => ApplyRecords(slot, positions, default, forward, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplyRecords(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        var hasNext = !next.IsEmpty;
        if (effects.Length != positions.Length || (hasNext && next.Length != positions.Length))
            throw new ArgumentException("Column length must equal position count.");
        var sameClock = hasNext && Unsafe.AreSame(ref MemoryMarshal.GetReference(positions), ref MemoryMarshal.GetReference(next));
        if (MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects))
            || (!sameClock && (MemoryMarshal.AsBytes(effects).Overlaps(MemoryMarshal.AsBytes(next))
                || MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(next)))))
            throw new ArgumentException("Lane columns must not overlap.");
        if (forward) ApplyRecordsForward(slot, positions, next, effects);
        else ApplyRecordsBackward(slot, positions, next, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void ApplyRecordsForward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects)
    {
        var duration = slot->Duration;
        var records = slot->ForwardRecords;
        var hasNext = !next.IsEmpty;
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            if (position < duration)
            {
                ref var r = ref records[position];
                effects[i] += r.Effect;
                if (hasNext) next[i] = r.Next;
            }
            else if (hasNext) next[i] = position;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void ApplyRecordsBackward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects)
    {
        var duration = slot->Duration;
        var records = slot->BackwardRecords;
        var hasNext = !next.IsEmpty;
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            if (position <= duration)
            {
                ref var r = ref records[position];
                if (r.Next != Skipped)
                {
                    effects[i] += r.Effect;
                    if (hasNext) next[i] = r.Next;
                    continue;
                }
            }
            if (hasNext) next[i] = position;
        }
    }

    internal SlotView** _views;
    internal byte* _absent;
    internal uint* _motion;
    internal SlotView** _shared;
    internal void* _retired;
    internal nuint _viewCapacity;
    internal nuint _absentCapacity;
    internal nuint _motionCapacity;
    internal nuint _sharedCapacity;
    internal int _sharedUsed;
    internal int _blockCount;
    internal int _sharedHits;
    internal long _headerTotal;
    internal long _tableTotal;
    internal long _directoryTotal;
    internal int _count;
    internal int _holes;
    internal bool _lazyResolve;
    internal bool _anyLooping;
    internal bool _disposed;
    internal ushort _minDuration;
    internal int _pendingCursor;
    internal ulong _generation;
    internal int _gate;

    internal int Holes => _holes;
    internal long RetainedBytes => _headerTotal + _tableTotal + _directoryTotal;
    internal long HeaderBytes => _headerTotal;
    internal long TableBytes => _tableTotal;
    internal long DirectoryBytes => _directoryTotal;
    internal int BlockCount => _blockCount;
    internal int SharedHits => _sharedHits;

    internal int PendingCursor => _pendingCursor;

    internal void AdvancePendingCursor(int limit)
    {
        while (_pendingCursor < limit && !IsPending((ushort)_pendingCursor))
            _pendingCursor++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal bool IsFolded(ushort index)
        => index < (uint)_count && _views[index] != null;

    internal bool IsAbsent(ushort index)
        => index < (uint)_count && _views[index] == null && _absent[index] != 0;

    internal bool IsPending(ushort index)
        => index < (uint)_count && _views[index] == null && _absent[index] == 0;

    internal void MarkAbsent(ushort index)
    {
        AcquireGate();
        try
        {
            if (index < (uint)_count) _absent[index] = 1;
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (index < _count && _views[index] != null)
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
        AcquireGate();
        try
        {
            if (index < _count && _views[index] != null)
                return index;
            var duration = measured.Duration;
            var looping = measured.Looping;
            nuint ticks = Math.Max(1u, duration) + 1u;
            void* views = _views;
            void* absent = _absent;
            void* motion = _motion;
            Ensure(ref views, ref _viewCapacity, (nuint)sizeof(SlotView*), (nuint)index + 1);
            Ensure(ref absent, ref _absentCapacity, 1, (nuint)index + 1);
            Ensure(ref motion, ref _motionCapacity, (nuint)sizeof(uint), (nuint)index + 1);
            _views = (SlotView**)views;
            _absent = (byte*)absent;
            _motion = (uint*)motion;
            var hash = HashTables(duration, looping ? (ushort)1 : (ushort)0, measured.Forward, measured.Backward, ticks);
            EnsureShared((nuint)(_sharedUsed + 1));
            var generation = _generation + 1;
            var view = FindShared(measured, duration, looping, ticks, hash);
            if (view == null)
            {
                view = BakeBlock(measured, duration, looping, ticks, generation);
                PlaceShared(hash, view);
                _sharedUsed++;
            }
            else
            {
                _sharedHits++;
            }
            _absent[index] = 0;
            _motion[index] = duration | (looping ? 0x80000000u : 0u);
            Volatile.Write(ref *(long*)(_views + index), (long)view);
            if (index >= _count)
            {
                _holes += index - _count;
                _count = index + 1;
            }
            else
            {
                _holes--;
            }
            if (_count - _holes == 1 || duration < _minDuration)
                _minDuration = duration;
            _anyLooping |= looping;
            _generation = generation;
            return index;
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
    }

    void Ensure(ref void* current, ref nuint capacity, nuint elementBytes, nuint needed)
    {
        if (capacity >= needed) return;
        nuint next = capacity == 0 ? InitialCapacity : capacity;
        while (next < needed) next *= 2;
        var bytes = next * elementBytes;
        var allocated = NativeMemory.AlignedAlloc(bytes + RetirePadBytes, 64);
        _directoryTotal += (long)(bytes + RetirePadBytes);
        Unsafe.InitBlock(allocated, 0, checked((uint)(bytes + RetirePadBytes)));
        if (capacity != 0)
        {
            var oldBytes = capacity * elementBytes;
            Buffer.MemoryCopy(current, allocated, (long)oldBytes, (long)oldBytes);
            Retire((byte*)current + oldBytes, current);
        }
        current = allocated;
        capacity = next;
    }

    void Retire(void* pad, void* origin)
    {
        *(void**)pad = _retired;
        *(void**)((byte*)pad + 8) = origin;
        _retired = pad;
    }

    SlotView* FindShared(MeasuredLanes measured, ushort duration, bool looping, nuint ticks, ulong hash)
    {
        if (_sharedUsed == 0) return null;
        var table = _shared;
        var mask = _sharedCapacity - 1;
        var i = hash & mask;
        while (true)
        {
            var candidate = table[i];
            if (candidate == null) return null;
            if (SameTables(candidate, measured, duration, looping, ticks)) return candidate;
            i = (i + 1) & mask;
        }
    }

    void EnsureShared(nuint used)
    {
        if (_sharedCapacity != 0 && used * 4 <= _sharedCapacity * 3) return;
        nuint next = _sharedCapacity == 0 ? InitialCapacity : _sharedCapacity * 2;
        var bytes = next * (nuint)sizeof(SlotView*);
        var allocated = (SlotView**)NativeMemory.AlignedAlloc(bytes + RetirePadBytes, 64);
        Unsafe.InitBlock(allocated, 0, (uint)(bytes + RetirePadBytes));
        _directoryTotal += (long)(bytes + RetirePadBytes);
        var previous = _shared;
        var previousCapacity = _sharedCapacity;
        for (nuint i = 0; i < previousCapacity; i++)
        {
            var entry = previous[i];
            if (entry != null) PlaceInto(allocated, next, HashOf(entry), entry);
        }
        _shared = allocated;
        _sharedCapacity = next;
        if (previousCapacity != 0)
            Retire((byte*)previous + previousCapacity * (nuint)sizeof(SlotView*), previous);
    }

    void PlaceShared(ulong hash, SlotView* view)
        => PlaceInto(_shared, _sharedCapacity, hash, view);

    static void PlaceInto(SlotView** table, nuint capacity, ulong hash, SlotView* view)
    {
        var i = hash & (capacity - 1);
        while (table[i] != null) i = (i + 1) & (capacity - 1);
        table[i] = view;
    }

    static bool SameTables(SlotView* candidate, MeasuredLanes measured, ushort duration, bool looping, nuint ticks)
    {
        if (candidate->Duration != duration || candidate->Looping != (ushort)(looping ? 1 : 0) || candidate->TableTicks != ticks)
            return false;
        var bytes = checked((int)(ticks * sizeof(float)));
        return new ReadOnlySpan<byte>(candidate->Forward, bytes).SequenceEqual(new ReadOnlySpan<byte>(measured.Forward, bytes))
            && new ReadOnlySpan<byte>(candidate->Backward, bytes).SequenceEqual(new ReadOnlySpan<byte>(measured.Backward, bytes));
    }

    static ulong HashOf(SlotView* view)
        => HashTables(view->Duration, view->Looping, view->Forward, view->Backward, view->TableTicks);

    static ulong HashTables(ushort duration, ushort looping, float* forward, float* backward, nuint ticks)
    {
        var h = Mix(0x9E3779B97F4A7C15UL ^ ((ulong)duration << 1) ^ looping ^ ((ulong)ticks << 32));
        for (nuint i = 0; i < ticks; i++)
            h = Mix(h ^ *(uint*)(forward + i) | ((ulong)*(uint*)(backward + i) << 32));
        return h;
    }

    static ulong Mix(ulong h)
    {
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53UL;
        h ^= h >> 33;
        return h;
    }

    SlotView* BakeBlock(MeasuredLanes measured, ushort duration, bool looping, nuint ticks, ulong generation)
    {
        var block = (byte*)NativeMemory.AlignedAlloc((nuint)(BlockHeaderBytes + TableBytesPerTick * (long)ticks), 64);
        var forward = (float*)(block + BlockHeaderBytes);
        var backward = forward + ticks;
        var backwardByPosition = backward + ticks;
        var forwardRecords = (LaneMovementRecord*)(backwardByPosition + ticks);
        var backwardRecords = forwardRecords + ticks;
        var tableBytes = (long)(ticks * sizeof(float));
        Buffer.MemoryCopy(measured.Forward, forward, tableBytes, tableBytes);
        Buffer.MemoryCopy(measured.Backward, backward, tableBytes, tableBytes);
        LaneMovement.Bake(forward, backward, duration, looping, forwardRecords, backwardRecords, backwardByPosition);
        *(SlotView*)block = new SlotView
        {
            Forward = forward,
            Backward = backward,
            BackwardByPosition = backwardByPosition,
            ForwardRecords = forwardRecords,
            BackwardRecords = backwardRecords,
            Duration = duration,
            Looping = looping ? (ushort)1 : (ushort)0,
            Absent = 0,
            TableTicks = (uint)ticks,
            RecordBytes = (ushort)sizeof(LaneMovementRecord),
            AbiVersion = SlotView.AbiVersionV1,
            Generation = generation,
        };
        _blockCount++;
        _headerTotal += BlockHeaderBytes;
        _tableTotal += TableBytesPerTick * (long)ticks;
        return (SlotView*)block;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal TimelineSetLane<TTrack, TClip> Gather(ReadOnlySpan<ushort> timelineIds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(this, timelineIds, default, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void Apply(ReadOnlySpan<ushort> timelineIds, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => Gather(timelineIds).Seek(positions, forward).Apply(effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void Apply(ReadOnlySpan<ushort> timelineIds, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => Gather(timelineIds).Seek(positions, forward).Apply(effects, next);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void Advance(ReadOnlySpan<ushort> ids, Span<ushort> positions, bool forward)
        => Advance(ids, positions, positions, forward);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal void Advance(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ids.Length != positions.Length || positions.Length != next.Length)
            throw new ArgumentException("Column length must equal position count.");
        var count = positions.Length;
        var motion = _motion;
        var views = _views;
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
                        if (forward) LaneOps.AdvanceForward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
                        else LaneOps.AdvanceBackward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
                    }
                    else LaneOps.AdvanceRows(motion, ids, positions, next, forward, i, block);
                }
                else
                {
                    for (var k = i; k < block; k++) AdvanceRow(ids, positions, next, motion, views, bound, reverse, k);
                }
                i = block;
            }
        }
        for (; i < count; i++) AdvanceRow(ids, positions, next, motion, views, bound, reverse, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe void AdvanceRow(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, uint* motion, SlotView** views, int bound, bool reverse, int i)
    {
        var id = ids[i];
        if (id >= bound || Volatile.Read(ref *(long*)(views + id)) == 0)
        {
            next[i] = positions[i];
            return;
        }
        var m = motion[id];
        next[i] = TimelineMovement.Advance((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, reverse, positions[i], out var np, out _, out _)
            ? np
            : positions[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects, next);

    internal SlotView View(ushort index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return *_views[index];
    }

    public void Dispose()
    {
        if (_disposed) return;
        AcquireGate();
        try
        {
            if (_disposed) return;
            _disposed = true;
            var shared = _shared;
            if (shared != null)
                for (nuint i = 0; i < _sharedCapacity; i++)
                    if (shared[i] != null)
                        NativeMemory.AlignedFree(shared[i]);
            if (_views != null) NativeMemory.AlignedFree(_views);
            if (_absent != null) NativeMemory.AlignedFree(_absent);
            if (_motion != null) NativeMemory.AlignedFree(_motion);
            if (shared != null) NativeMemory.AlignedFree(shared);
            var node = _retired;
            while (node != null)
            {
                var next = *(void**)node;
                NativeMemory.AlignedFree(*(void**)((byte*)node + 8));
                node = next;
            }
            _views = null;
            _absent = null;
            _motion = null;
            _shared = null;
            _retired = null;
            _viewCapacity = 0;
            _absentCapacity = 0;
            _motionCapacity = 0;
            _sharedCapacity = 0;
            _sharedUsed = 0;
            _count = 0;
            _holes = 0;
            _minDuration = 0;
            _pendingCursor = 0;
            _blockCount = 0;
            _sharedHits = 0;
            _headerTotal = 0;
            _tableTotal = 0;
            _directoryTotal = 0;
        }
        finally
        {
            Volatile.Write(ref _gate, 0);
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal TimelineSetLane(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, bool forward)
    {
        _set = set;
        _ids = ids;
        _positions = positions;
        _forward = forward;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        var views = set._views;
        var bound = set._count;
        var minDuration = set._minDuration;
        var lazy = set._lazyResolve;
        var holy = lazy && set._holes != 0;
        var gather = Avx2.IsSupported;
        var forward = _forward;
        var i = 0;
        var uniformId = -1;
        SlotView* slot = null;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            var first = ids[i];
            if (UniformChunk(ids, i, chunkEnd, first))
            {
                if (first >= bound || views[first] == null)
                {
                    if (!lazy) ThrowUnboundId(first, i);
                    Timeline<TTrack, TClip>.Resolve(first);
                    views = set._views;
                    bound = set._count;
                    minDuration = set._minDuration;
                    holy = set._holes != 0;
                    uniformId = -1;
                    slot = null;
                }
                if (first != uniformId)
                {
                    slot = views[first];
                    uniformId = first;
                }
                i = ApplyUniformSegment(slot, positions, next, effects, i, chunkEnd, forward, gather);
            }
            else
            {
                if (holy)
                    while (Timeline<TTrack, TClip>.ResolveChunk(set, ids, i, chunkEnd))
                    {
                        views = set._views;
                        bound = set._count;
                        minDuration = set._minDuration;
                        uniformId = -1;
                        slot = null;
                    }
                if (FastMixedChunk(ids, positions, i, chunkEnd, bound, minDuration))
                {
                    i = forward
                        ? FastMixedForward(ids, positions, next, effects, views, i, chunkEnd)
                        : FastMixedBackward(ids, positions, next, effects, views, i, chunkEnd);
                    continue;
                }
                if (ValidateChunk(ids, i, chunkEnd, set, bound))
                {
                    views = set._views;
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
                            ? ApplyMixedForward(ids, positions, next, effects, views, i, chunkEnd)
                            : ApplyMixedBackward(ids, positions, next, effects, views, i, chunkEnd);
                        break;
                    }
                    i = ApplyUniformSegment(views[ids[i]], positions, next, effects, i, segment, forward, gather);
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
        var slot = set._views[index];
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
        var slot = set._views[index];
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
    static unsafe int ApplyUniformSegment(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit, bool forward, bool gather)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var blockEnd = i + ((limit - i) >> 4 << 4);
        if (gather && blockEnd > i && duration > 1 && (looping ? LaneOps.StaggeredEnds(positions, i, blockEnd) : ShortRuns(positions, i, blockEnd)))
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
        return forward
            ? ApplyUniformForward(slot, positions, next, effects, i, limit)
            : ApplyUniformBackward(slot, positions, next, effects, i, limit);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformForward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit)
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
                else if (hasNext) next[i] = p;
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformBackward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit)
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
                else if (hasNext) next[i] = p;
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            var slot = views[id];
            if (slot == null)
            {
                if (hasNext) next[i] = position;
                i++;
                continue;
            }
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                if (position < slot->Duration)
                {
                    ref var r = ref slot->ForwardRecords[position];
                    effects[i] += r.Effect;
                    if (hasNext) next[i] = r.Next;
                }
                else if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            var duration = slot->Duration;
            if (position >= duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            LaneOps.Add(effects, i, end, slot->Forward[position]);
            if (hasNext) LaneOps.Fill(next, i, end, slot->Looping != 0 && position + 1 == duration ? (ushort)0 : (ushort)(position + 1));
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            var slot = views[id];
            if (slot == null)
            {
                if (hasNext) next[i] = position;
                i++;
                continue;
            }
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                if (position <= slot->Duration)
                {
                    ref var r = ref slot->BackwardRecords[position];
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
    static unsafe int FastMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
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
                lastRecords = views[id]->ForwardRecords;
            }
            ref var r = ref lastRecords[positions[i]];
            effects[i] += r.Effect;
            if (hasNext) next[i] = r.Next;
            i++;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int FastMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
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
                lastRecords = views[id]->BackwardRecords;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
}
