using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics.CodeAnalysis;

namespace Tl;

internal unsafe interface IRowWalk
{
    static abstract bool Forward { get; }
    static abstract bool InRange(ushort position, int duration);
    static abstract bool Playable(ushort position, int duration, bool looping);
    static abstract ushort Next(ushort position, int duration, bool looping);
}

internal readonly unsafe struct ForwardRows : IRowWalk
{
    public static bool Forward => true;
    public static bool InRange(ushort position, int duration) => position < duration;
    public static bool Playable(ushort position, int duration, bool looping) => true;
    public static ushort Next(ushort position, int duration, bool looping)
        => (ushort)(looping && position + 1 == duration ? 0 : position + 1);
}

internal readonly unsafe struct BackwardRows : IRowWalk
{
    public static bool Forward => false;
    public static bool InRange(ushort position, int duration) => position <= duration;
    public static bool Playable(ushort position, int duration, bool looping) => looping ? position < duration : position > 0;
    public static ushort Next(ushort position, int duration, bool looping)
        => (ushort)(position == 0 ? duration - 1 : position - 1);
}

internal interface IRowNext
{
    static abstract bool Fused { get; }
}

internal readonly struct FusedRows : IRowNext
{
    public static bool Fused => true;
}

internal readonly struct PlainRows : IRowNext
{
    public static bool Fused => false;
}

internal sealed unsafe class TimelineSet<TTrack, TClip> : IDisposable
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    const int InitialCapacity = 1024;
    const int BlockHeaderBytes = 72;
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
        ApplyRecords(slot, positions, next, effects, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void ApplyRecords(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, bool forward)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var hasNext = !next.IsEmpty;
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            if (forward ? position < duration : LaneMovement.BackwardPlayable(position, duration, looping))
            {
                effects[i] += LaneEncoding.Value(slot, 0, forward, forward ? position : LaneEncoding.BackwardTick(position, duration));
                if (hasNext)
                    next[i] = forward
                        ? LaneMovement.ForwardNext(position, duration, looping)
                        : LaneMovement.BackwardNext(position, duration);
            }
            else if (hasNext) next[i] = position;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal SlotView** _views;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private byte* _absent;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private uint* _motion;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private SlotView** _shared;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private ulong* _sharedHashes;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private void* _retired;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private nuint _viewCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private nuint _absentCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private nuint _motionCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private nuint _sharedCapacity;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private int _sharedUsed;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private int _blockCount;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private int _sharedHits;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private long _headerTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private long _tableTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private long _directoryTotal;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _count;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal int _holes;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal bool _lazyResolve;
    internal bool _floatLaneProven;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal bool _disposed;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal ushort _minDuration;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private int _pendingCursor;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal ulong _generation;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
            var plan = LaneBlockPlan.Plan(measured, ticks);
            var hash = HashPlan(plan, duration, looping, ticks, measured);
            EnsureShared((nuint)(_sharedUsed + 1));
            var generation = _generation + 1;
            var view = FindShared(plan, measured, duration, looping, ticks, hash);
            if (view == null)
            {
                view = BakeBlock(measured, plan, duration, looping, ticks, generation);
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

    SlotView* FindShared(LaneBlockPlan plan, MeasuredLanes measured, ushort duration, bool looping, nuint ticks, ulong hash)
    {
        if (_sharedUsed == 0) return null;
        var table = _shared;
        var hashes = _sharedHashes;
        var mask = _sharedCapacity - 1;
        var i = hash & mask;
        while (true)
        {
            var candidate = table[i];
            if (candidate == null) return null;
            if (hashes[i] == hash && SameTables(candidate, measured, duration, looping, ticks)) return candidate;
            i = (i + 1) & mask;
        }
    }

    void EnsureShared(nuint used)
    {
        if (_sharedCapacity != 0 && used * 4 <= _sharedCapacity * 3) return;
        nuint next = _sharedCapacity == 0 ? InitialCapacity : _sharedCapacity * 2;
        var bytes = next * (nuint)sizeof(SlotView*);
        var allocated = (SlotView**)NativeMemory.AlignedAlloc(bytes + RetirePadBytes, 64);
        var allocatedHashes = (ulong*)NativeMemory.AlignedAlloc(next * 8 + RetirePadBytes, 64);
        Unsafe.InitBlock(allocated, 0, (uint)(bytes + RetirePadBytes));
        Unsafe.InitBlock(allocatedHashes, 0, (uint)(next * 8 + RetirePadBytes));
        _directoryTotal += (long)(bytes + RetirePadBytes + next * 8 + RetirePadBytes);
        var previous = _shared;
        var previousHashes = _sharedHashes;
        var previousCapacity = _sharedCapacity;
        for (nuint i = 0; i < previousCapacity; i++)
        {
            var entry = previous[i];
            if (entry != null) PlaceInto(allocated, allocatedHashes, next, previousHashes[i], entry);
        }
        _shared = allocated;
        _sharedHashes = allocatedHashes;
        _sharedCapacity = next;
        if (previousCapacity != 0)
        {
            Retire((byte*)previous + previousCapacity * (nuint)sizeof(SlotView*), previous);
            Retire((byte*)previousHashes + previousCapacity * 8, previousHashes);
        }
    }

    void PlaceShared(ulong hash, SlotView* view)
        => PlaceInto(_shared, _sharedHashes, _sharedCapacity, hash, view);

    static void PlaceInto(SlotView** table, ulong* hashes, nuint capacity, ulong hash, SlotView* view)
    {
        var i = hash & (capacity - 1);
        while (table[i] != null) i = (i + 1) & (capacity - 1);
        table[i] = view;
        hashes[i] = hash;
    }

    static bool SameTables(SlotView* candidate, MeasuredLanes measured, ushort duration, bool looping, nuint ticks)
    {
        if (candidate->Duration != duration || candidate->Looping != (ushort)(looping ? 1 : 0) || candidate->TableTicks != ticks || candidate->ResultCount != measured.LaneCount)
            return false;
        var keys = candidate->LaneKeys;
        for (var l = 0; l < measured.LaneCount; l++)
            if (keys[l] != measured.LaneKeys[l])
                return false;
        for (var l = 0; l < measured.LaneCount; l++)
        {
            var forward = measured.LaneForward[l];
            var backward = measured.LaneBackward[l];
            var lane = (nuint)l;
            for (nuint t = 0; t < ticks; t++)
            {
                if (LaneEncoding.Bits(LaneEncoding.Value(candidate, lane, true, (int)t)) != LaneEncoding.Bits(forward[t]))
                    return false;
                if (LaneEncoding.Bits(LaneEncoding.Value(candidate, lane, false, (int)t)) != LaneEncoding.Bits(backward[t]))
                    return false;
            }
        }
        return true;
    }

    static ulong HashPlan(LaneBlockPlan plan, ushort duration, bool looping, nuint ticks, MeasuredLanes measured)
    {
        var lanes = measured.LaneCount;
        var h = Mix(0x9E3779B97F4A7C15UL ^ ((ulong)duration << 1) ^ (looping ? 1UL : 0UL) ^ ((ulong)ticks << 32) ^ (ulong)lanes);
        for (var l = 0; l < lanes; l++)
            h = Mix(h ^ measured.LaneKeys[l]);
        h = Mix(h ^ ((plan.FlatForward ? 1UL : 0UL) | (plan.FlatBackward ? 2UL : 0UL)));
        foreach (var segment in plan.Segments)
            h = Mix(h ^ ((ulong)segment.Start | (ulong)segment.End << 16 | (ulong)segment.Kind << 32)
                ^ (ulong)LaneEncoding.Bits(segment.V0) | (ulong)LaneEncoding.Bits(segment.V1) << 32);
        foreach (var value in plan.Dense)
            h = Mix(h ^ (ulong)LaneEncoding.Bits(value));
        var tableFloats = (int)ticks;
        for (var l = 0; l < lanes; l++)
        {
            if (plan.FlatForward) h = HashFloats(h, measured.LaneForward[l], tableFloats);
            if (plan.FlatBackward) h = HashFloats(h, measured.LaneBackward[l], tableFloats);
        }
        return h;
    }

    static ulong HashFloats(ulong h, float* values, int count)
    {
        for (var i = 0; i < count; i++)
            h = Mix(h ^ (ulong)LaneEncoding.Bits(values[i]));
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

    SlotView* BakeBlock(MeasuredLanes measured, LaneBlockPlan plan, ushort duration, bool looping, nuint ticks, ulong generation)
    {
        var lanes = measured.LaneCount;
        var buckets = (nuint)LaneEncoding.Buckets((uint)ticks);
        var directories = !plan.FlatForward || !plan.FlatBackward;
        var dirBytes = directories ? (nuint)lanes * 2 * buckets * sizeof(uint) : 0;
        var segBytes = (nuint)(plan.Segments.Count * sizeof(LaneSegment));
        var denseBytes = (nuint)(plan.Dense.Count * sizeof(float));
        var forwardBytes = plan.FlatForward ? (nuint)lanes * ticks * sizeof(float) : 0;
        var backwardBytes = plan.FlatBackward ? (nuint)lanes * ticks * sizeof(float) : 0;
        var keysBytes = (nuint)(lanes * 8);
        var padBytes = (BlockHeaderBytes + dirBytes + segBytes + denseBytes + forwardBytes + backwardBytes) % 8 == 0 ? 0u : 4u;
        var block = (byte*)NativeMemory.AlignedAlloc((nuint)BlockHeaderBytes + dirBytes + segBytes + denseBytes + forwardBytes + backwardBytes + padBytes + keysBytes, 64);
        var directory = (uint*)(block + BlockHeaderBytes);
        var segments = (LaneSegment*)((byte*)directory + dirBytes);
        var dense = (float*)((byte*)segments + segBytes);
        var flatForward = (float*)((byte*)dense + denseBytes);
        var flatBackward = (float*)((byte*)flatForward + forwardBytes);
        var keys = (ulong*)((byte*)flatBackward + backwardBytes + padBytes);
        var segmentSpan = CollectionsMarshal.AsSpan(plan.Segments);
        fixed (LaneSegment* segmentSource = segmentSpan)
            Buffer.MemoryCopy(segmentSource, segments, (long)segBytes, (long)segBytes);
        var denseSpan = CollectionsMarshal.AsSpan(plan.Dense);
        fixed (float* denseSource = denseSpan)
            Buffer.MemoryCopy(denseSource, dense, (long)denseBytes, (long)denseBytes);
        if (directories)
        {
            var baseIndex = 0;
            for (var l = 0; l < lanes; l++)
            {
                var laneDirectory = directory + (nuint)l * buckets * 2;
                if (!plan.FlatForward)
                {
                    LaneEncoding.BuildDirectory(segments, baseIndex, plan.ForwardSegments[l], (int)ticks, laneDirectory);
                    baseIndex += plan.ForwardSegments[l];
                }
                if (!plan.FlatBackward)
                {
                    LaneEncoding.BuildDirectory(segments, baseIndex, plan.BackwardSegments[l], (int)ticks, laneDirectory + buckets);
                    baseIndex += plan.BackwardSegments[l];
                }
            }
        }
        var tableBytes = (long)(ticks * sizeof(float));
        for (var l = 0; l < lanes; l++)
        {
            if (plan.FlatForward)
                Buffer.MemoryCopy(measured.LaneForward[l], flatForward + (nuint)l * ticks, tableBytes, tableBytes);
            if (plan.FlatBackward)
                Buffer.MemoryCopy(measured.LaneBackward[l], flatBackward + (nuint)l * ticks, tableBytes, tableBytes);
            keys[l] = measured.LaneKeys[l];
        }
        *(SlotView*)block = new SlotView
        {
            Directory = directories ? directory : null,
            Segments = segments,
            Dense = dense,
            Forward = plan.FlatForward ? flatForward : null,
            Backward = plan.FlatBackward ? flatBackward : null,
            LaneKeys = keys,
            Duration = duration,
            Looping = looping ? (ushort)1 : (ushort)0,
            Absent = 0,
            TableTicks = (uint)ticks,
            ResultCount = (ushort)lanes,
            AbiVersion = SlotView.AbiVersionV3,
            Generation = generation,
        };
        _blockCount++;
        _headerTotal += BlockHeaderBytes;
        _tableTotal += (long)(dirBytes + segBytes + denseBytes + forwardBytes + backwardBytes + padBytes);
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
    internal bool HasView(ushort index) => index < _count && Volatile.Read(ref *(long*)(_views + index)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal int LaneFor(ushort index, ulong key, int ordinal)
    {
        var view = _views[index];
        if (view == null) return -1;
        var keys = view->LaneKeys;
        var seen = 0;
        for (var l = 0; l < view->ResultCount; l++)
            if (keys[l] == key && seen++ == ordinal) return l;
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal void ApplyLane<T>(ushort index, ReadOnlySpan<ushort> positions, bool forward, int lane, Span<T> column) where T : unmanaged
    {
        Checked.Live(_disposed);
        if (positions.Length != column.Length) throw new ArgumentException("positions and column must have equal length.");
        var view = _views[index];
        if (view == null) return;
        var duration = view->Duration;
        var looping = view->Looping != 0;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var c = ref MemoryMarshal.GetReference(column);
        if (sizeof(T) == 8)
        {
            var lo = (nuint)lane;
            var hi = lo + 1;
            for (var i = 0; i < positions.Length; i++)
            {
                var pos = Unsafe.Add(ref p, (nuint)i);
                if (forward ? pos < duration : LaneMovement.BackwardPlayable(pos, duration, looping))
                {
                    var tick = forward ? pos : LaneEncoding.BackwardTick(pos, duration);
                    var bits = (ulong)LaneEncoding.Bits(LaneEncoding.Value(view, lo, forward, tick)) | (ulong)LaneEncoding.Bits(LaneEncoding.Value(view, hi, forward, tick)) << 32;
                    Unsafe.Add(ref c, (nuint)i) = Combine(Unsafe.Add(ref c, (nuint)i), Unsafe.As<ulong, T>(ref bits));
                }
            }
            return;
        }
        for (var i = 0; i < positions.Length; i++)
        {
            var pos = Unsafe.Add(ref p, (nuint)i);
            if (forward ? pos < duration : LaneMovement.BackwardPlayable(pos, duration, looping))
                Unsafe.Add(ref c, (nuint)i) = Combine(Unsafe.Add(ref c, (nuint)i), ReadCell<T>(view, (nuint)lane, forward, forward ? pos : LaneEncoding.BackwardTick(pos, duration)));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static T ReadCell<T>(SlotView* view, nuint lane, bool forward, int tick) where T : unmanaged
    {
        var value = LaneEncoding.Value(view, lane, forward, tick);
        if (typeof(T) == typeof(float)) return Unsafe.As<float, T>(ref value);
        var cell = LaneEncoding.Bits(value);
        if (sizeof(T) == 4) return Unsafe.As<uint, T>(ref cell);
        if (sizeof(T) == 2)
        {
            var word = (ushort)cell;
            return Unsafe.As<ushort, T>(ref word);
        }
        var bits = (byte)cell;
        return Unsafe.As<byte, T>(ref bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static T Combine<T>(T a, T b) where T : unmanaged
    {
        if (typeof(T) == typeof(bool)) { var v = Unsafe.As<T, bool>(ref a) | Unsafe.As<T, bool>(ref b); return Unsafe.As<bool, T>(ref v); }
        if (typeof(T) == typeof(float)) { var v = Unsafe.As<T, float>(ref a) + Unsafe.As<T, float>(ref b); return Unsafe.As<float, T>(ref v); }
        if (sizeof(T) == 4) { var v = Unsafe.As<T, int>(ref a) + Unsafe.As<T, int>(ref b); return Unsafe.As<int, T>(ref v); }
        if (sizeof(T) == 8)
        {
            if (typeof(T) == typeof(double)) { var v = Unsafe.As<T, double>(ref a) + Unsafe.As<T, double>(ref b); return Unsafe.As<double, T>(ref v); }
            var w = Unsafe.As<T, long>(ref a) + Unsafe.As<T, long>(ref b);
            return Unsafe.As<long, T>(ref w);
        }
        if (sizeof(T) == 2) { var v = (ushort)(Unsafe.As<T, ushort>(ref a) + Unsafe.As<T, ushort>(ref b)); return Unsafe.As<ushort, T>(ref v); }
        var v1 = (byte)(Unsafe.As<T, byte>(ref a) + Unsafe.As<T, byte>(ref b));
        return Unsafe.As<byte, T>(ref v1);
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
        if (forward) ApplyRowsCore<ForwardRows>(rows, indices, positions, effects);
        else ApplyRowsCore<BackwardRows>(rows, indices, positions, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void AdvanceRows(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward)
    {
        Checked.Live(_disposed);
        Checked.Rows(rows, indices, positions);
        if (forward) AdvanceRowsCore<ForwardRows>(rows, indices, positions);
        else AdvanceRowsCore<BackwardRows>(rows, indices, positions);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    void ApplyRowsCore<TDir>(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<float> effects) where TDir : struct, IRowWalk
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        ref var fxOrigin = ref MemoryMarshal.GetReference(effects);
        var lastId = -1;
        SlotView* slot = null;
        var duration = 0;
        var looping = false;
        for (var i = 0; i < rows.Length; i++)
        {
            var row = (nuint)Unsafe.Add(ref rowOrigin, i);
            var id = Unsafe.Add(ref idOrigin, row);
            if (id != lastId)
            {
                slot = id < (uint)bound ? views[id] : null;
                if (slot is null)
                {
                    ResolveRow(id, i, lazy);
                    views = _views;
                    bound = _count;
                    slot = views[id];
                }
                lastId = id;
                duration = slot->Duration;
                looping = slot->Looping != 0;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (TDir.InRange(position, duration) && TDir.Playable(position, duration, looping))
                Unsafe.Add(ref fxOrigin, row) += LaneEncoding.Value(slot, 0, TDir.Forward, TDir.Forward ? position : LaneEncoding.BackwardTick(position, duration));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    void AdvanceRowsCore<TDir>(ReadOnlySpan<int> rows, ReadOnlySpan<ushort> indices, Span<ushort> positions) where TDir : struct, IRowWalk
    {
        var views = _views;
        var bound = _count;
        var lazy = _lazyResolve;
        ref var rowOrigin = ref MemoryMarshal.GetReference(rows);
        ref var idOrigin = ref MemoryMarshal.GetReference(indices);
        ref var posOrigin = ref MemoryMarshal.GetReference(positions);
        var lastId = -1;
        var duration = 0;
        var looping = false;
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
                looping = slot->Looping != 0;
            }
            var position = Unsafe.Add(ref posOrigin, row);
            if (TDir.InRange(position, duration) && TDir.Playable(position, duration, looping))
                Unsafe.Add(ref posOrigin, row) = TDir.Next(position, duration, looping);
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
            if (_sharedHashes != null) NativeMemory.AlignedFree(_sharedHashes);
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
            _sharedHashes = null;
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

internal sealed unsafe class LaneBlockPlan
{
    internal readonly List<LaneSegment> Segments = [];
    internal readonly List<float> Dense = [];
    internal readonly int[] ForwardSegments;
    internal readonly int[] BackwardSegments;
    internal bool FlatForward;
    internal bool FlatBackward;

    LaneBlockPlan(int lanes)
    {
        ForwardSegments = new int[lanes];
        BackwardSegments = new int[lanes];
    }

    internal static LaneBlockPlan Plan(MeasuredLanes measured, nuint ticks)
    {
        var lanes = measured.LaneCount;
        var plan = new LaneBlockPlan(lanes);
        var tickCount = checked((int)ticks);
        var flat = tickCount < LaneEncoding.SegmentedMinTicks;
        var flatBytes = (long)tickCount * sizeof(float) * lanes;
        plan.FlatForward = flat || EncodeAll(plan, measured, tickCount, true) >= flatBytes;
        plan.FlatBackward = flat || EncodeAll(plan, measured, tickCount, false) >= flatBytes;
        if (plan.FlatForward || plan.FlatBackward)
        {
            plan.Segments.Clear();
            plan.Dense.Clear();
            for (var l = 0; l < lanes; l++)
            {
                if (!plan.FlatForward)
                    plan.ForwardSegments[l] = LaneEncoding.Encode(measured.LaneForward[l], tickCount, plan.Segments, plan.Dense);
                if (!plan.FlatBackward)
                    plan.BackwardSegments[l] = LaneEncoding.Encode(measured.LaneBackward[l], tickCount, plan.Segments, plan.Dense);
            }
        }
        return plan;
    }

    static long EncodeAll(LaneBlockPlan plan, MeasuredLanes measured, int ticks, bool forward)
    {
        var bytes = 0L;
        for (var l = 0; l < measured.LaneCount; l++)
        {
            var before = plan.Segments.Count * (long)sizeof(LaneSegment) + plan.Dense.Count * (long)sizeof(float);
            var count = LaneEncoding.Encode(forward ? measured.LaneForward[l] : measured.LaneBackward[l], ticks, plan.Segments, plan.Dense);
            bytes += plan.Segments.Count * (long)sizeof(LaneSegment) + plan.Dense.Count * (long)sizeof(float) - before;
            if (forward) plan.ForwardSegments[l] = count;
            else plan.BackwardSegments[l] = count;
        }
        return bytes;
    }
}

internal readonly ref struct TimelineSetLane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    const int Chunk = 4096;
    const int MinSegment = 128;
    const int VectorRows = 16;
    const int ShortRunRows = 64;

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
                    i = MixedVector(forward, ids, positions, next, effects, views, i, chunkEnd);
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
                        i = ApplyMixed(forward, ids, positions, next, effects, views, i, chunkEnd);
                        break;
                    }
                    var segmentId = ids[i];
                    i = ApplyUniformSegment(views[segmentId], positions, next, effects, i, segment, forward, gather);
                    if (i >= chunkEnd) break;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Apply(Span<float> effects) => Apply(effects, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ApplySlot(ushort index, Span<float> effects)
        => ApplySlot(index, effects, default);

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
                {
                    if (slot->Forward != null)
                        LaneOps.EffectForward<FlatSource>(slot->Forward, duration, looping, positions, next, effects, i, blockEnd);
                    else
                        LaneOps.EffectForward<SegmentedForwardSource>(slot, duration, looping, positions, next, effects, i, blockEnd);
                }
                else
                {
                    if (slot->Backward != null)
                        LaneOps.EffectBackward<FlatSource>(slot->Backward, duration, looping, positions, next, effects, i, blockEnd);
                    else
                        LaneOps.EffectBackward<SegmentedBackwardSource>(slot, duration, looping, positions, next, effects, i, blockEnd);
                }
                i = blockEnd;
            }
        }
        return ApplyUniform(slot, positions, next, effects, i, limit, forward);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyUniform(SlotView* slot, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, int i, int limit, bool forward)
    {
        var duration = slot->Duration;
        var looping = slot->Looping != 0;
        var hasNext = !next.IsEmpty;
        while (i < limit)
        {
            var position = positions[i];
            var end = i + 1 >= limit || positions[i + 1] != position ? i + 1 : LaneOps.RunEnd(positions, i, limit);
            if (forward ? position >= duration : position == 0 && !looping || position > duration || looping && position == duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            var tick = forward ? position : (ushort)(position == 0 ? duration - 1 : position - 1);
            var step = forward ? (ushort)(looping && position + 1 == duration ? 0 : position + 1) : tick;
            var delta = LaneEncoding.Value(slot, 0, forward, tick);
            if (end == i + 1)
            {
                effects[i] += delta;
                if (hasNext) next[i] = step;
            }
            else
            {
                LaneOps.Add(effects, i, end, delta);
                if (hasNext) LaneOps.Fill(next, i, end, step);
            }
            i = end;
            while (i < limit && (i + 1 >= limit || positions[i + 1] != positions[i]))
            {
                var p = positions[i];
                if (forward ? p < duration : LaneMovement.BackwardPlayable(p, duration, looping))
                {
                    effects[i] += LaneEncoding.Value(slot, 0, forward, forward ? p : LaneEncoding.BackwardTick(p, duration));
                    if (hasNext)
                        next[i] = forward
                            ? LaneMovement.ForwardNext(p, duration, looping)
                            : LaneMovement.BackwardNext(p, duration);
                }
                else if (hasNext) next[i] = p;
                i++;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int ApplyMixed(bool forward, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
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
            var duration = slot->Duration;
            var looping = slot->Looping != 0;
            if (i + 1 >= limit || ids[i + 1] != id || positions[i + 1] != position)
            {
                if (forward ? position < duration : LaneMovement.BackwardPlayable(position, duration, looping))
                {
                    effects[i] += LaneEncoding.Value(slot, 0, forward, forward ? position : LaneEncoding.BackwardTick(position, duration));
                    if (hasNext)
                        next[i] = forward
                            ? LaneMovement.ForwardNext(position, duration, looping)
                            : LaneMovement.BackwardNext(position, duration);
                }
                else if (hasNext) next[i] = position;
                i++;
                continue;
            }
            var end = RunEndTwo(ids, positions, i, limit);
            if (forward ? position >= duration : position == 0 && !looping || position > duration || looping && position == duration) { if (hasNext) LaneOps.Fill(next, i, end, position); i = end; continue; }
            var tick = forward ? position : (ushort)(position == 0 ? duration - 1 : position - 1);
            LaneOps.Add(effects, i, end, LaneEncoding.Value(slot, 0, forward, tick));
            if (hasNext) LaneOps.Fill(next, i, end, forward ? (ushort)(looping && position + 1 == duration ? 0 : position + 1) : tick);
            i = end;
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int MixedVector(bool forward, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
    {
        while (i < limit)
        {
            if (LongRunAt(ids, i, limit))
            {
                var end = LaneOps.RunEnd(ids, i, limit);
                if (end - i >= VectorRows)
                {
                    var run = views[ids[i]];
                    var flat = forward ? run->Forward : run->Backward;
                    var block = i + ((end - i) >> 4 << 4);
                    if (end - i < ShortRunRows && flat != null && run->Duration > 8 && Avx2.IsSupported)
                    {
                        if (forward)
                            LaneOps.EffectForward<FlatSource>(flat, run->Duration, run->Looping != 0, positions, next, effects, i, block);
                        else
                            LaneOps.EffectBackward<FlatSource>(flat, run->Duration, run->Looping != 0, positions, next, effects, i, block);
                        i = ApplyUniform(run, positions, next, effects, block, end, forward);
                        continue;
                    }
                    i = ApplyUniformSegment(run, positions, next, effects, i, end, forward, true);
                    continue;
                }
            }
            i = FastMixed(forward, ids, positions, next, effects, views, i, limit);
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int GatherRows<TWalk, TNext>(ushort* ids, ushort* positions, ushort* next, float* effects, SlotView** views, int i, int limit)
        where TWalk : struct, IRowWalk
        where TNext : struct, IRowNext
    {
        var first = i;
        var table = (long*)(TWalk.Forward ? 24 : 32);
        var motion = (int*)48;
        for (; i + VectorRows + 8 <= limit; i += 8)
        {
            var id = Vector128.Load(ids + i);
            var prior = i == 0 ? Vector128.Shuffle(id, Vector128.Create((ushort)0, 0, 1, 2, 3, 4, 5, 6)) : Vector128.Load(ids + i - 1);
            var starts = Vector128.Equals(id, Vector128.Load(ids + i + 1)) & Vector128.Equals(id, Vector128.Load(ids + i + VectorRows - 1)) & ~Vector128.Equals(id, prior);
            if (i == first) starts &= Vector128.Create((ushort)0, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF);
            if (starts != Vector128<ushort>.Zero) return i;
            var row = Avx2.ConvertToVector256Int32(id);
            var view0 = Avx2.GatherVector256((long*)views, row.GetLower(), 8);
            var view1 = Avx2.GatherVector256((long*)views, row.GetUpper(), 8);
            var base0 = Avx2.GatherVector256(table, view0, 1);
            var base1 = Avx2.GatherVector256(table, view1, 1);
            if ((Vector256.Equals(base0, Vector256<long>.Zero) | Vector256.Equals(base1, Vector256<long>.Zero)) != Vector256<long>.Zero) return i;
            var position = Avx2.ConvertToVector256Int32(Vector128.Load(positions + i));
            var tick = position;
            var playable = Vector256<int>.AllBitsSet;
            var looping = Vector256<int>.Zero;
            var duration = Vector256<int>.Zero;
            if (!TWalk.Forward || TNext.Fused)
            {
                var word = Vector256.Create(Avx2.GatherVector128(motion, view0, 1), Avx2.GatherVector128(motion, view1, 1));
                duration = word & Vector256.Create(0xFFFF);
                looping = ~Vector256.Equals(word >>> 16, Vector256<int>.Zero);
            }
            if (!TWalk.Forward)
            {
                var origin = Vector256.Equals(position, Vector256<int>.Zero);
                tick = Vector256.ConditionalSelect(origin, duration - Vector256<int>.One, position - Vector256<int>.One);
                playable = Vector256.ConditionalSelect(looping, Vector256.LessThan(position, duration), ~origin & ~Vector256.GreaterThan(position, duration));
            }
            var address0 = base0 + (Avx2.ConvertToVector256Int64(tick.GetLower()) << 2);
            var address1 = base1 + (Avx2.ConvertToVector256Int64(tick.GetUpper()) << 2);
            var value = Vector256.Create(Avx2.GatherVector128((float*)null, address0, 1), Avx2.GatherVector128((float*)null, address1, 1));
            var effect = Vector256.Load(effects + i);
            var sum = effect + value;
            if (!TWalk.Forward) sum = Vector256.ConditionalSelect(playable.AsSingle(), sum, effect);
            sum.Store(effects + i);
            if (TNext.Fused)
            {
                Vector256<int> step;
                if (TWalk.Forward)
                {
                    var advanced = position + Vector256<int>.One;
                    step = Vector256.AndNot(advanced, Vector256.Equals(advanced, duration) & looping);
                }
                else step = Vector256.ConditionalSelect(playable, tick, position);
                Vector128.Narrow(step.GetLower().AsUInt32(), step.GetUpper().AsUInt32()).Store(next + i);
            }
        }
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool LongRunAt(ReadOnlySpan<ushort> ids, int at, int limit)
        => at + VectorRows <= limit && ids[at + 1] == ids[at] && ids[at + VectorRows - 1] == ids[at];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int FastMixedRows<TWalk, TNext>(ref ushort idRow, ref ushort positionRow, ref ushort nextRow, ref float effectRow, SlotView** views, int i, int limit)
        where TWalk : struct, IRowWalk
        where TNext : struct, IRowNext
    {
        var first = i;
        var probe = limit - VectorRows;
        for (; i < limit; i++)
        {
            var id = Unsafe.Add(ref idRow, (nuint)i);
            if (i <= probe && i != first && Unsafe.Add(ref idRow, (nuint)i + 1) == id && Unsafe.Add(ref idRow, (nuint)(i + VectorRows - 1)) == id && Unsafe.Add(ref idRow, (nuint)i - 1) != id) return i;
            var slot = views[id];
            var table = TWalk.Forward ? slot->Forward : slot->Backward;
            if (table == null) return i;
            var p = Unsafe.Add(ref positionRow, (nuint)i);
            if (TWalk.Forward)
            {
                Unsafe.Add(ref effectRow, (nuint)i) += table[p];
                if (TNext.Fused)
                {
                    var duration = slot->Duration;
                    Unsafe.Add(ref nextRow, (nuint)i) = p < duration ? LaneMovement.ForwardNext(p, duration, slot->Looping != 0) : SlotView.Skipped;
                }
            }
            else
            {
                var duration = slot->Duration;
                if (LaneMovement.BackwardPlayable(p, duration, slot->Looping != 0))
                {
                    Unsafe.Add(ref effectRow, (nuint)i) += table[LaneEncoding.BackwardTick(p, duration)];
                    if (TNext.Fused) Unsafe.Add(ref nextRow, (nuint)i) = LaneMovement.BackwardNext(p, duration);
                }
                else if (TNext.Fused) Unsafe.Add(ref nextRow, (nuint)i) = p;
            }
        }
        return limit;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe int FastMixed(bool forward, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
    {
        var first = i;
        while (i < limit)
        {
            if (i != first && LongRunAt(ids, i, limit)) return i;
            i = (forward, next.IsEmpty) switch
            {
                (true, true) => MixedRows<ForwardRows, PlainRows>(ids, positions, next, effects, views, i, limit),
                (true, false) => MixedRows<ForwardRows, FusedRows>(ids, positions, next, effects, views, i, limit),
                (false, true) => MixedRows<BackwardRows, PlainRows>(ids, positions, next, effects, views, i, limit),
                _ => MixedRows<BackwardRows, FusedRows>(ids, positions, next, effects, views, i, limit),
            };
            if (i == limit || (i != first && LongRunAt(ids, i, limit))) return i;
            i = ApplyMixed(forward, ids, positions, next, effects, views, i, LaneOps.RunEnd(ids, i, limit));
        }
        return limit;
    }

    static unsafe int MixedRows<TWalk, TNext>(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> next, Span<float> effects, SlotView** views, int i, int limit)
        where TWalk : struct, IRowWalk
        where TNext : struct, IRowNext
    {
        if (Avx2.IsSupported)
        {
            var start = i;
            fixed (ushort* idPin = ids, positionPin = positions, nextPin = next)
            fixed (float* effectPin = effects)
                i = GatherRows<TWalk, TNext>(idPin, positionPin, nextPin, effectPin, views, i, limit);
            if (i != start && LongRunAt(ids, i, limit)) return i;
        }
        return FastMixedRows<TWalk, TNext>(ref MemoryMarshal.GetReference(ids), ref MemoryMarshal.GetReference(positions), ref MemoryMarshal.GetReference(next), ref MemoryMarshal.GetReference(effects), views, i, limit);
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
        if (Vector.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(ids);
            var boundVector = new Vector<ushort>((ushort)bound);
            var edge = end - Vector<ushort>.Count;
            var over = Vector<ushort>.Zero;
            while (i <= edge)
            {
                over |= Vector.GreaterThanOrEqual(Vector.LoadUnsafe(ref origin, (nuint)i), boundVector);
                i += Vector<ushort>.Count;
            }
            if (over != Vector<ushort>.Zero)
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
        if (Vector256.IsHardwareAccelerated && start + 17 <= end)
        {
            ref var idPeek = ref MemoryMarshal.GetReference(ids);
            ref var positionPeek = ref MemoryMarshal.GetReference(positions);
            var idPairs = Vector256.Equals(Vector256.LoadUnsafe(ref idPeek, (nuint)start), Vector256.LoadUnsafe(ref idPeek, (nuint)(start + 1)));
            var positionPairs = Vector256.Equals(Vector256.LoadUnsafe(ref positionPeek, (nuint)start), Vector256.LoadUnsafe(ref positionPeek, (nuint)(start + 1)));
            if (BitOperations.PopCount(Vector256.ExtractMostSignificantBits(idPairs & positionPairs)) >= 12) return false;
        }
        var boundLimit = bound >= 65536 ? (ushort)65535 : (ushort)bound;
        var i = start;
        if (Vector.IsHardwareAccelerated)
        {
            ref var idOrigin = ref MemoryMarshal.GetReference(ids);
            ref var positionOrigin = ref MemoryMarshal.GetReference(positions);
            var boundVector = new Vector<ushort>(boundLimit);
            var limit = new Vector<ushort>((ushort)minDuration);
            var highest = Vector<ushort>.Zero;
            var width = Vector<ushort>.Count;
            while (i + width < end)
            {
                if (Vector.GreaterThanOrEqual(Vector.LoadUnsafe(ref idOrigin, (nuint)i), boundVector) != Vector<ushort>.Zero) return false;
                highest = Vector.Max(highest, Vector.LoadUnsafe(ref positionOrigin, (nuint)i));
                if (!Vector.LessThanAll(highest, limit)) return false;
                i += width;
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
        => throw new ArgumentException($"Timeline id {id} at row {row} is not bound in this TimelineSet; ids come from TimelineSet.Add at load time.");

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
