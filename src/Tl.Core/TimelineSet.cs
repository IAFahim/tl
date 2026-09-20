using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics.CodeAnalysis;

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
        Checked.Live(_disposed);
        return index < (uint)_count ? (SlotView*)Volatile.Read(ref *(long*)(_views + index)) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplyRecords(SlotView* slot, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => ApplyRecords(slot, positions, default, forward, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplyRecords(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        Checked.Columns(positions, next, effects);
        if (forward) ApplyRecordsForward(slot, positions, next, effects);
        else ApplyRecordsBackward(slot, positions, next, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void ApplyRecordsForward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects)
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
    static void ApplyRecordsBackward(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects)
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal SlotView** _views;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal byte* _absent;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal uint* _motion;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal SlotView** _shared;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal LaneMovementRecord* _arenaForward;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal LaneMovementRecord* _arenaBackward;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal uint* _arenaBases;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal void* _retired;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _viewCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _absentCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _motionCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _sharedCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _arenaCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal nuint _arenaBaseCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _sharedUsed;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _blockCount;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _sharedHits;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal long _headerTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal long _tableTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal long _directoryTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal long _arenaTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _arenaUsed;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _count;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _holes;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal bool _lazyResolve;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal bool _anyLooping;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal bool _disposed;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal ushort _minDuration;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _pendingCursor;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal ulong _generation;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _gate;

    internal int Holes => _holes;
    internal long ArenaBytes => _arenaTotal;
    internal long RetainedBytes => _headerTotal + _tableTotal + _directoryTotal + _arenaTotal;
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
        Checked.Live(_disposed);
        if (index < _count && _views[index] != null)
            return index;
        return Bind(index, measured);
    }

    void CheckAdd()
    {
        Checked.Live(_disposed);
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
            Ensure(ref motion, ref _motionCapacity, sizeof(uint), (nuint)index + 1);
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
            AppendArena(index, view);
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

    void AppendArena(ushort index, SlotView* view)
    {
        void* bases = _arenaBases;
        Ensure(ref bases, ref _arenaBaseCapacity, sizeof(uint), (nuint)index + 1);
        _arenaBases = (uint*)bases;
        var ticks = view->TableTicks;
        var used = (nuint)_arenaUsed;
        if (used + ticks > _arenaCapacity) GrowArena(used + ticks);
        var offset = checked((uint)used);
        var recordBytes = checked((int)(ticks * sizeof(LaneMovementRecord)));
        Buffer.MemoryCopy(view->ForwardRecords, _arenaForward + offset, recordBytes, recordBytes);
        Buffer.MemoryCopy(view->BackwardRecords, _arenaBackward + offset, recordBytes, recordBytes);
        _arenaBases[index] = offset;
        _arenaUsed = checked((int)(used + ticks));
    }

    void GrowArena(nuint needed)
    {
        nuint next = _arenaCapacity == 0 ? InitialCapacity : _arenaCapacity;
        while (next < needed) next *= 2;
        var bytes = next * (nuint)sizeof(LaneMovementRecord);
        var forward = (LaneMovementRecord*)NativeMemory.AlignedAlloc(bytes + RetirePadBytes, 64);
        var backward = (LaneMovementRecord*)NativeMemory.AlignedAlloc(bytes + RetirePadBytes, 64);
        _arenaTotal += (long)(2 * (bytes + RetirePadBytes));
        if (_arenaCapacity != 0)
        {
            var usedBytes = (long)((nuint)_arenaUsed * (nuint)sizeof(LaneMovementRecord));
            Buffer.MemoryCopy(_arenaForward, forward, usedBytes, usedBytes);
            Buffer.MemoryCopy(_arenaBackward, backward, usedBytes, usedBytes);
            Retire((byte*)_arenaForward + (nuint)usedBytes, _arenaForward);
            Retire((byte*)_arenaBackward + (nuint)usedBytes, _arenaBackward);
        }
        _arenaForward = forward;
        _arenaBackward = backward;
        _arenaCapacity = next;
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
        Checked.Live(_disposed);
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
        Checked.Live(_disposed);
        Checked.Columns(ids, positions, next);
        var count = positions.Length;
        var motion = _motion;
        var views = _views;
        var bound = _count;
        var reverse = !forward;
        var i = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            var boundVector = Vector256.Create((ushort)bound);
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
        else if (Vector128.IsHardwareAccelerated)
        {
            var boundVector = Vector128.Create((ushort)bound);
            var blockEnd = count - (count & 7);
            while (i < blockEnd)
            {
                var block = i + 8;
                var idv = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(ids), (nuint)i);
                if (Vector128.LessThanAll(idv, boundVector))
                {
                    if (Vector128.EqualsAll(idv, Vector128.Create(ids[i])))
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
    static void AdvanceRow(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, uint* motion, SlotView** views, int bound, bool reverse, int i)
    {
        var id = ids[i];
        if (id >= bound || Volatile.Read(ref *(long*)(views + id)) == 0)
        {
            next[i] = positions[i];
            return;
        }
        var m = motion[id];
        var duration = (ushort)(m & 0xFFFF);
        var looping = (m & 0x80000000u) != 0;
        var pos = positions[i];
        var forward = !reverse;
        if (forward)
            next[i] = pos < duration ? (looping && pos + 1 == duration ? (ushort)0 : (ushort)(pos + 1)) : pos;
        else
            next[i] = looping
                ? pos < duration ? (pos == 0 ? (ushort)(duration - 1) : (ushort)(pos - 1)) : pos
                : pos > 0 && pos <= duration ? (ushort)(pos - 1) : pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplySlot(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => new TimelineSetLane<TTrack, TClip>(this, ReadOnlySpan<ushort>.Empty, positions, forward).ApplySlot(index, effects, next);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplyRows(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        Checked.Live(_disposed);
        Checked.Rows(rows, indices, positions, effects);
        if (forward) ApplyRowsForward(rows, indices, positions, effects);
        else ApplyRowsBackward(rows, indices, positions, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void AdvanceRows(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward)
    {
        Checked.Live(_disposed);
        Checked.Rows(rows, indices, positions);
        if (forward) AdvanceRowsForward(rows, indices, positions);
        else AdvanceRowsBackward(rows, indices, positions);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void ApplyRowsForward(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<float> effects)
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        ref var fxOrigin = ref MemoryMarshal.GetReference(effects);
        var lastId = -1;
        LaneMovementRecord* records = null;
        var duration = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            var row = (nuint)Unsafe.Add(ref rowOrigin, i);
            var id = Unsafe.Add(ref idOrigin, row);
            if (id != lastId)
            {
                var slot = id < (uint)bound ? views[id] : null;
                if (slot is null)
                {
                    ResolveRow(id, i, lazy);
                    views = _views;
                    bound = _count;
                    slot = views[id];
                }
                lastId = id;
                duration = slot->Duration;
                records = slot->ForwardRecords;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (position < duration)
                Unsafe.Add(ref fxOrigin, row) += records[position].Effect;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void ApplyRowsBackward(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<float> effects)
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        ref var fxOrigin = ref MemoryMarshal.GetReference(effects);
        var lastId = -1;
        LaneMovementRecord* records = null;
        var duration = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            var row = (nuint)Unsafe.Add(ref rowOrigin, i);
            var id = Unsafe.Add(ref idOrigin, row);
            if (id != lastId)
            {
                var slot = id < (uint)bound ? views[id] : null;
                if (slot is null)
                {
                    ResolveRow(id, i, lazy);
                    views = _views;
                    bound = _count;
                    slot = views[id];
                }
                lastId = id;
                duration = slot->Duration;
                records = slot->BackwardRecords;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (position <= duration)
            {
                ref var r = ref records[position];
                if (r.Next != Skipped)
                    Unsafe.Add(ref fxOrigin, row) += r.Effect;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void AdvanceRowsForward(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, Span<ushort> positions)
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        var lastId = -1;
        LaneMovementRecord* records = null;
        var duration = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            var row = (nuint)Unsafe.Add(ref rowOrigin, i);
            var id = Unsafe.Add(ref idOrigin, row);
            if (id != lastId)
            {
                var slot = id < (uint)bound ? views[id] : null;
                if (slot is null)
                {
                    ResolveRow(id, i, lazy);
                    views = _views;
                    bound = _count;
                    slot = views[id];
                }
                lastId = id;
                duration = slot->Duration;
                records = slot->ForwardRecords;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (position < duration)
                Unsafe.Add(ref posOrigin, row) = records[position].Next;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void AdvanceRowsBackward(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, Span<ushort> positions)
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        var lastId = -1;
        LaneMovementRecord* records = null;
        var duration = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            var row = (nuint)Unsafe.Add(ref rowOrigin, i);
            var id = Unsafe.Add(ref idOrigin, row);
            if (id != lastId)
            {
                var slot = id < (uint)bound ? views[id] : null;
                if (slot is null)
                {
                    ResolveRow(id, i, lazy);
                    views = _views;
                    bound = _count;
                    slot = views[id];
                }
                lastId = id;
                duration = slot->Duration;
                records = slot->BackwardRecords;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (position <= duration)
            {
                var next = records[position].Next;
                if (next != Skipped)
                    Unsafe.Add(ref posOrigin, row) = next;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void ResolveRow(ushort id, int row, bool lazy)
    {
        if (lazy)
        {
            Timeline<TTrack, TClip>.Resolve(id);
            return;
        }
        TimelineSetLane<TTrack, TClip>.ThrowUnboundId(id, row);
    }

    internal SlotView View(ushort index)
    {
        Checked.Live(_disposed);
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
            if (_arenaForward != null) NativeMemory.AlignedFree(_arenaForward);
            if (_arenaBackward != null) NativeMemory.AlignedFree(_arenaBackward);
            if (_arenaBases != null) NativeMemory.AlignedFree(_arenaBases);
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
            _arenaForward = null;
            _arenaBackward = null;
            _arenaBases = null;
            _retired = null;
            _viewCapacity = 0;
            _absentCapacity = 0;
            _motionCapacity = 0;
            _sharedCapacity = 0;
            _arenaCapacity = 0;
            _arenaBaseCapacity = 0;
            _arenaUsed = 0;
            _arenaTotal = 0;
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
        Checked.Live(set._disposed);
        var ids = _ids;
        var positions = _positions;
        Checked.Columns(ids, positions, next, effects);
        var count = positions.Length;
        if (count == 0) return;
        var views = set._views;
        var bound = set._count;
        var minDuration = set._minDuration;
        var lazy = set._lazyResolve;
        var holy = lazy && set._holes != 0;
        var gather = Avx2.IsSupported || Vector128.IsHardwareAccelerated;
        var gatherAvx2 = Avx2.IsSupported;
        var forward = _forward;
        var i = 0;
        var uniformId = -1;
        SlotView* slot = null;
        var arenaForward = set._arenaForward;
        var arenaBackward = set._arenaBackward;
        var arenaBases = set._arenaBases;
        var motion = set._motion;
        var arenaOk = set._holes == 0;
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
                    arenaForward = set._arenaForward;
                    arenaBackward = set._arenaBackward;
                    arenaBases = set._arenaBases;
                    motion = set._motion;
                    arenaOk = set._holes == 0;
                    uniformId = -1;
                    slot = null;
                }
                if (first != uniformId)
                {
                    slot = views[first];
                    uniformId = first;
                }
                i = ApplyUniformSegment(slot, positions, next, effects, i, chunkEnd, forward, gather, ArenaRecords(forward, arenaOk, arenaForward, arenaBackward, arenaBases, first));
            }
            else
            {
                if (holy)
                    while (Timeline<TTrack, TClip>.ResolveChunk(set, ids, i, chunkEnd))
                    {
                        views = set._views;
                        bound = set._count;
                        minDuration = set._minDuration;
                        arenaForward = set._arenaForward;
                        arenaBackward = set._arenaBackward;
                        arenaBases = set._arenaBases;
                        motion = set._motion;
                        arenaOk = set._holes == 0;
                        uniformId = -1;
                        slot = null;
                    }
                if (FastMixedChunk(ids, positions, i, chunkEnd, bound, minDuration))
                {
                    if (gatherAvx2 && arenaOk)
                    {
                        i = forward
                            ? GatherMixedForward(ids, positions, next, effects, arenaForward, arenaBases, i, chunkEnd)
                            : GatherMixedBackward(ids, positions, next, effects, arenaBackward, arenaBases, i, chunkEnd);
                    }
                    else
                    {
                        i = forward
                            ? FastMixedForward(ids, positions, next, effects, views, i, chunkEnd)
                            : FastMixedBackward(ids, positions, next, effects, views, i, chunkEnd);
                    }
                    continue;
                }
                if (ValidateChunk(ids, i, chunkEnd, set, bound))
                {
                    views = set._views;
                    bound = set._count;
                    minDuration = set._minDuration;
                    holy = set._holes != 0;
                    arenaForward = set._arenaForward;
                    arenaBackward = set._arenaBackward;
                    arenaBases = set._arenaBases;
                    motion = set._motion;
                    arenaOk = set._holes == 0;
                    uniformId = -1;
                    slot = null;
                }
                while (true)
                {
                    var segment = LaneOps.RunEnd(ids, i, chunkEnd);
                    if (segment - i < MinSegment)
                    {
                        if (arenaOk)
                            i = forward
                                ? ArenaMixedForward(ids, positions, next, effects, motion, arenaForward, arenaBases, i, chunkEnd)
                                : ArenaMixedBackward(ids, positions, next, effects, motion, arenaBackward, arenaBases, i, chunkEnd);
                        else
                            i = forward
                                ? ApplyMixedForward(ids, positions, next, effects, views, i, chunkEnd)
                                : ApplyMixedBackward(ids, positions, next, effects, views, i, chunkEnd);
                        break;
                    }
                    var segmentId = ids[i];
                    i = ApplyUniformSegment(views[segmentId], positions, next, effects, i, segment, forward, gather, ArenaRecords(forward, arenaOk, arenaForward, arenaBackward, arenaBases, segmentId));
                    if (i >= chunkEnd) break;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Apply(Span<float> effects) => Apply(effects, default);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal unsafe void ApplySlot(ushort index, Span<float> effects)
    {
        var set = _set;
        Checked.Live(set._disposed);
        var positions = _positions;
        Checked.Columns(positions, effects);
        var count = positions.Length;
        if (count == 0) return;
        var slot = set._views[index];
        var gather = Avx2.IsSupported || Vector128.IsHardwareAccelerated;
        var forward = _forward;
        var records = (forward ? set._arenaForward : set._arenaBackward) + set._arenaBases[index];
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            i = ApplyUniformSegment(slot, positions, default, effects, i, chunkEnd, forward, gather, records);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal unsafe void ApplySlot(ushort index, Span<float> effects, Span<ushort> next)
    {
        var set = _set;
        Checked.Live(set._disposed);
        var positions = _positions;
        Checked.Columns(positions, next, effects);
        var count = positions.Length;
        if (count == 0) return;
        var slot = set._views[index];
        var gather = Avx2.IsSupported || Vector128.IsHardwareAccelerated;
        var forward = _forward;
        var records = (forward ? set._arenaForward : set._arenaBackward) + set._arenaBases[index];
        var i = 0;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            i = ApplyUniformSegment(slot, positions, next, effects, i, chunkEnd, forward, gather, records);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniformSegment(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit, bool forward, bool gather, LaneMovementRecord* records)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var blockEnd = i + ((limit - i) >> 4 << 4);
        if (gather && duration > 1 && blockEnd > i && (Avx2.IsSupported || duration <= 8 && Vector128.IsHardwareAccelerated) && (looping ? LaneOps.StaggeredEnds(positions, i, blockEnd) : ShortRuns(positions, i, blockEnd)))
        {
            if (duration <= 8)
            {
                if (forward)
                    LaneOps.EffPermuteForward(slot->Forward, duration, looping, positions, next, effects, i, blockEnd);
                else
                    LaneOps.EffPermuteBackward(slot->Backward, duration, looping, positions, next, effects, i, blockEnd);
                i = blockEnd;
            }
            else if (Avx2.IsSupported)
            {
                if (forward)
                    LaneOps.EffectForward(slot->Forward, duration, looping, positions, next, effects, i, blockEnd);
                else
                    LaneOps.EffectBackward(slot->BackwardByPosition, duration, looping, positions, next, effects, i, blockEnd);
                i = blockEnd;
            }
        }
        if (records != null && i < limit && (looping ? LaneOps.StaggeredEnds(positions, i, limit) : ShortRuns(positions, i, limit)))
            return ArenaUniformWalk(records, positions, next, effects, i, limit, forward, duration);
        return forward
            ? ApplyUniformForward(slot, positions, next, effects, i, limit)
            : ApplyUniformBackward(slot, positions, next, effects, i, limit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe LaneMovementRecord* ArenaRecords(bool forward, bool arenaOk, LaneMovementRecord* arenaForward, LaneMovementRecord* arenaBackward, uint* arenaBases, ushort id)
        => !arenaOk ? null : (forward ? arenaForward : arenaBackward) + arenaBases[id];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ArenaUniformWalk(LaneMovementRecord* records, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit, bool forward, ushort duration)
    {
        var hasNext = !next.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(next);
        ref var e = ref MemoryMarshal.GetReference(effects);
        if (forward)
        {
            for (; i < limit; i++)
            {
                var position = Unsafe.Add(ref p, (nuint)i);
                if (position < duration)
                {
                    ref var r = ref records[position];
                    Unsafe.Add(ref e, (nuint)i) += r.Effect;
                    if (hasNext) Unsafe.Add(ref n, (nuint)i) = r.Next;
                }
                else if (hasNext) Unsafe.Add(ref n, (nuint)i) = position;
            }
            return limit;
        }
        for (; i < limit; i++)
        {
            var position = Unsafe.Add(ref p, (nuint)i);
            if (position <= duration)
            {
                ref var r = ref records[position];
                if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
                {
                    Unsafe.Add(ref e, (nuint)i) += r.Effect;
                    if (hasNext) Unsafe.Add(ref n, (nuint)i) = r.Next;
                    continue;
                }
            }
            if (hasNext) Unsafe.Add(ref n, (nuint)i) = position;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ArenaMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, uint* motion, LaneMovementRecord* arena, uint* bases, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            var records = arena + bases[id];
            var duration = (ushort)(motion[id] & 0xFFFF);
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                if (position < duration)
                {
                    ref var r = ref records[position];
                    effects[i] += r.Effect;
                    if (hasNext) next[i] = r.Next;
                }
                else if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            if (position >= duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            ref var run = ref records[position];
            LaneOps.Add(effects, i, end, run.Effect);
            if (hasNext) LaneOps.Fill(next, i, end, run.Next);
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ArenaMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, uint* motion, LaneMovementRecord* arena, uint* bases, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var id = ids[i];
            var position = positions[i];
            var records = arena + bases[id];
            var duration = (ushort)(motion[id] & 0xFFFF);
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                if (position <= duration)
                {
                    ref var r = ref records[position];
                    if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
                    {
                        effects[i] += r.Effect;
                        if (hasNext) next[i] = r.Next;
                        i++;
                        continue;
                    }
                }
                if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            if (position <= duration)
            {
                ref var r = ref records[position];
                if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
                {
                    LaneOps.Add(effects, i, end, r.Effect);
                    if (hasNext) LaneOps.Fill(next, i, end, r.Next);
                    i = end;
                    continue;
                }
            }
            if (hasNext) LaneOps.Fill(next, i, end, position);
            i = end;
        }
        return limit;
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
        var lastId = ids[i];
        LaneMovementRecord* lastRecords = views[lastId]->ForwardRecords;
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
        var lastId = ids[i];
        LaneMovementRecord* lastRecords = views[lastId]->BackwardRecords;
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int GatherMixedForward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, LaneMovementRecord* arena, uint* bases, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        ref var idRef = ref MemoryMarshal.GetReference(ids);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(next);
        ref var e = ref MemoryMarshal.GetReference(effects);
        var effectLanes = Vector256.Create(0, 2, 4, 6, 0, 0, 0, 0);
        var nextLanes = Vector256.Create(1, 3, 5, 7, 0, 0, 0, 0);
        var lowWord = Vector128.Create(0xFFFFu);
        if (i + 16 <= limit)
        {
            var baseLo = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef)).AsInt32(), 4).AsUInt32();
            var baseHi = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, 8)).AsInt32(), 4).AsUInt32();
            while (i + 16 <= limit)
            {
                var currLo = baseLo;
                var currHi = baseHi;
                var block = i + 16;
                if (block + 16 <= limit)
                {
                    baseLo = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, (nuint)block)).AsInt32(), 4).AsUInt32();
                    baseHi = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, (nuint)(block + 8))).AsInt32(), 4).AsUInt32();
                }
                var posv = Vector256.LoadUnsafe(ref p, (nuint)i);
                (var posLo, var posHi) = Vector256.Widen(posv);
                var idxLo = currLo + posLo;
                var idxHi = currHi + posHi;
                var rec0 = Avx2.GatherVector256((ulong*)arena, idxLo.GetLower().AsInt32(), 8);
                var rec1 = Avx2.GatherVector256((ulong*)arena, idxLo.GetUpper().AsInt32(), 8);
                var rec2 = Avx2.GatherVector256((ulong*)arena, idxHi.GetLower().AsInt32(), 8);
                var rec3 = Avx2.GatherVector256((ulong*)arena, idxHi.GetUpper().AsInt32(), 8);
                var eff0 = Avx2.PermuteVar8x32(rec0.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff1 = Avx2.PermuteVar8x32(rec1.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff2 = Avx2.PermuteVar8x32(rec2.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff3 = Avx2.PermuteVar8x32(rec3.AsInt32(), effectLanes).GetLower().AsSingle();
                var effLo = Avx.InsertVector128(eff0.ToVector256(), eff1, 1);
                var effHi = Avx.InsertVector128(eff2.ToVector256(), eff3, 1);
                var fxLo = Vector256.LoadUnsafe(ref e, (nuint)i);
                (fxLo + effLo).StoreUnsafe(ref e, (nuint)i);
                var fxHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
                (fxHi + effHi).StoreUnsafe(ref e, (nuint)(i + 8));
                if (hasNext)
                {
                    var nx0 = Avx2.PermuteVar8x32(rec0.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                    var nx1 = Avx2.PermuteVar8x32(rec1.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                    var nx2 = Avx2.PermuteVar8x32(rec2.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                    var nx3 = Avx2.PermuteVar8x32(rec3.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                    var nextLo = Avx.InsertVector128(nx0.ToVector256(), nx1, 1);
                    var nextHi = Avx.InsertVector128(nx2.ToVector256(), nx3, 1);
                    Vector256.Narrow(nextLo, nextHi).StoreUnsafe(ref n, (nuint)i);
                }
                i = block;
            }
        }
        for (; i < limit; i++)
        {
            var records = arena + bases[Unsafe.Add(ref idRef, (nuint)i)];
            ref var r = ref records[Unsafe.Add(ref p, (nuint)i)];
            Unsafe.Add(ref e, (nuint)i) += r.Effect;
            if (hasNext) Unsafe.Add(ref n, (nuint)i) = r.Next;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int GatherMixedBackward(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, LaneMovementRecord* arena, uint* bases, int i, int limit)
    {
        var hasNext = !next.IsEmpty;
        ref var idRef = ref MemoryMarshal.GetReference(ids);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(next);
        ref var e = ref MemoryMarshal.GetReference(effects);
        var effectLanes = Vector256.Create(0, 2, 4, 6, 0, 0, 0, 0);
        var nextLanes = Vector256.Create(1, 3, 5, 7, 0, 0, 0, 0);
        var lowWord = Vector128.Create(0xFFFFu);
        var skipWord = Vector128.Create(0xFFFFu);
        if (i + 16 <= limit)
        {
            var baseLo = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef)).AsInt32(), 4).AsUInt32();
            var baseHi = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, 8)).AsInt32(), 4).AsUInt32();
            while (i + 16 <= limit)
            {
                var currLo = baseLo;
                var currHi = baseHi;
                var block = i + 16;
                if (block + 16 <= limit)
                {
                    baseLo = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, (nuint)block)).AsInt32(), 4).AsUInt32();
                    baseHi = Avx2.GatherVector256((int*)bases, Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref idRef, (nuint)(block + 8))).AsInt32(), 4).AsUInt32();
                }
                var posv = Vector256.LoadUnsafe(ref p, (nuint)i);
                (var posLo, var posHi) = Vector256.Widen(posv);
                var idxLo = currLo + posLo;
                var idxHi = currHi + posHi;
                var rec0 = Avx2.GatherVector256((ulong*)arena, idxLo.GetLower().AsInt32(), 8);
                var rec1 = Avx2.GatherVector256((ulong*)arena, idxLo.GetUpper().AsInt32(), 8);
                var rec2 = Avx2.GatherVector256((ulong*)arena, idxHi.GetLower().AsInt32(), 8);
                var rec3 = Avx2.GatherVector256((ulong*)arena, idxHi.GetUpper().AsInt32(), 8);
                var eff0 = Avx2.PermuteVar8x32(rec0.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff1 = Avx2.PermuteVar8x32(rec1.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff2 = Avx2.PermuteVar8x32(rec2.AsInt32(), effectLanes).GetLower().AsSingle();
                var eff3 = Avx2.PermuteVar8x32(rec3.AsInt32(), effectLanes).GetLower().AsSingle();
                var effLo = Avx.InsertVector128(eff0.ToVector256(), eff1, 1);
                var effHi = Avx.InsertVector128(eff2.ToVector256(), eff3, 1);
                var nx0 = Avx2.PermuteVar8x32(rec0.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                var nx1 = Avx2.PermuteVar8x32(rec1.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                var nx2 = Avx2.PermuteVar8x32(rec2.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                var nx3 = Avx2.PermuteVar8x32(rec3.AsInt32(), nextLanes).GetLower().AsUInt32() & lowWord;
                var skipLo = Avx.InsertVector128(Avx2.CompareEqual(nx0, skipWord).ToVector256(), Avx2.CompareEqual(nx1, skipWord), 1).AsSingle();
                var skipHi = Avx.InsertVector128(Avx2.CompareEqual(nx2, skipWord).ToVector256(), Avx2.CompareEqual(nx3, skipWord), 1).AsSingle();
                var fxLo = Vector256.LoadUnsafe(ref e, (nuint)i);
                Avx2.BlendVariable(fxLo + effLo, fxLo, skipLo).StoreUnsafe(ref e, (nuint)i);
                var fxHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
                Avx2.BlendVariable(fxHi + effHi, fxHi, skipHi).StoreUnsafe(ref e, (nuint)(i + 8));
                if (hasNext)
                {
                    var nextLo = Avx.InsertVector128(nx0.ToVector256(), nx1, 1);
                    var nextHi = Avx.InsertVector128(nx2.ToVector256(), nx3, 1);
                    var outLo = Avx2.BlendVariable(nextLo.AsSingle(), posLo.AsSingle(), skipLo).AsUInt32();
                    var outHi = Avx2.BlendVariable(nextHi.AsSingle(), posHi.AsSingle(), skipHi).AsUInt32();
                    Vector256.Narrow(outLo, outHi).StoreUnsafe(ref n, (nuint)i);
                }
                i = block;
            }
        }
        for (; i < limit; i++)
        {
            var records = arena + bases[Unsafe.Add(ref idRef, (nuint)i)];
            var pos = Unsafe.Add(ref p, (nuint)i);
            ref var r = ref records[pos];
            if (r.Next != TimelineSet<TTrack, TClip>.Skipped)
            {
                Unsafe.Add(ref e, (nuint)i) += r.Effect;
                if (hasNext) Unsafe.Add(ref n, (nuint)i) = r.Next;
            }
            else if (hasNext) Unsafe.Add(ref n, (nuint)i) = pos;
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
        else if (Vector128.IsHardwareAccelerated)
        {
            var vectorLimit = probe - 9;
            while (j <= vectorLimit)
            {
                equalPairs += BitOperations.PopCount(Vector128.ExtractMostSignificantBits(Vector128.Equals(Vector128.LoadUnsafe(ref origin, (nuint)j), Vector128.LoadUnsafe(ref origin, (nuint)(j + 1)))) & 0xFF);
                j += 8;
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
    static bool ValidateChunk(ReadOnlySpan<ushort> ids, int start, int end, TimelineSet<TTrack, TClip> set, int bound)
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
        else if (Vector128.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var boundVector = Vector128.Create((ushort)bound);
            var edge = end - 8;
            var over = Vector128<ushort>.Zero;
            while (i <= edge)
            {
                over |= Vector128.GreaterThanOrEqual(Vector128.LoadUnsafe(ref origin, (nuint)i), boundVector);
                i += 8;
            }
            if (over != Vector128<ushort>.Zero)
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
    static bool FastMixedChunk(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, int start, int end, int bound, int minDuration)
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
        else if (Vector128.IsHardwareAccelerated)
        {
            ref var idOrigin = ref MemoryMarshal.GetReference(ids);
            ref var positionOrigin = ref MemoryMarshal.GetReference(positions);
            var boundVector = Vector128.Create(boundLimit);
            var limit = Vector128.Create((ushort)minDuration);
            var highest = Vector128<ushort>.Zero;
            while (i + 8 < end)
            {
                var id = Vector128.LoadUnsafe(ref idOrigin, (nuint)i);
                if (Vector128.GreaterThanOrEqual(id, boundVector) != Vector128<ushort>.Zero) return false;
                highest = Vector128.Max(highest, Vector128.LoadUnsafe(ref positionOrigin, (nuint)i));
                if (Vector128.ExtractMostSignificantBits(Vector128.GreaterThanOrEqual(highest, limit)) != 0) return false;
                i += 8;
            }
        }
        for (var k = i; k < end; k++)
        {
            if (ids[k] >= boundLimit) return false;
            if (positions[k] >= minDuration) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void ThrowUnboundId(ushort id, int row)
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
        else if (Vector128.IsHardwareAccelerated)
        {
            ref var idFirst = ref MemoryMarshal.GetReference(ids);
            ref var positionFirst = ref MemoryMarshal.GetReference(positions);
            var idSearch = Vector128.Create(id);
            var tickSearch = Vector128.Create(tick);
            var bound = limit - 8;
            while (end <= bound)
            {
                var idMask = Vector128.ExtractMostSignificantBits(Vector128.Equals(Vector128.LoadUnsafe(ref idFirst, (nuint)end), idSearch)) & 0xFF;
                var tickMask = Vector128.ExtractMostSignificantBits(Vector128.Equals(Vector128.LoadUnsafe(ref positionFirst, (nuint)end), tickSearch)) & 0xFF;
                var mask = idMask & tickMask;
                if (mask != 0xFF) return end + BitOperations.TrailingZeroCount(~mask & 0xFF);
                end += 8;
            }
        }
        while (end < limit && ids[end] == id && positions[end] == tick) end++;
        return end;
    }
}
