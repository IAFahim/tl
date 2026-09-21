using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public static unsafe class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    static TimelineSet<TTrack, TClip>? _bank;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        Checked.Domain(indices, positions);
        Bank().Apply(indices, positions, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        Checked.Domain(index, positions);
        var bank = Bank();
        if (positions.Length > LaneOps.SmallSpan)
        {
            if (!bank.IsFolded(index))
                Resolve(index);
            bank.ApplySlot(index, positions, forward, effects);
            return;
        }
        var slot = bank.FoldedView(index);
        if (slot is null)
        {
            Resolve(index);
            slot = bank.FoldedView(index);
        }
        bank.ApplyRecords(slot, positions, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ushort position, bool forward, Span<float> effects)
    {
        Checked.Domain(index, position);
        var bank = Bank();
        var slot = bank.FoldedView(index);
        if (slot is null)
        {
            Resolve(index);
            slot = bank.FoldedView(index);
        }
        ApplySharedClock(slot, position, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(ushort index, ref ushort position, bool forward)
    {
        Checked.Domain(index, position);
        var bank = Bank();
        var slot = bank.FoldedView(index);
        if (slot is null)
        {
            Resolve(index);
            slot = bank.FoldedView(index);
        }
        var p = position;
        if (forward)
        {
            if (p < slot->Duration) position = slot->ForwardRecords[p].Next;
            return;
        }
        if (p > slot->Duration) return;
        var next = slot->BackwardRecords[p].Next;
        if (next != LaneMovementRecord.Skipped) position = next;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void ApplySharedClock(SlotView* slot, ushort position, bool forward, Span<float> effects)
    {
        float delta;
        if (forward)
        {
            if (position >= slot->Duration) return;
            delta = slot->ForwardRecords[position].Effect;
        }
        else
        {
            if (position > slot->Duration) return;
            ref var record = ref slot->BackwardRecords[position];
            if (record.Next == LaneMovementRecord.Skipped) return;
            delta = record.Effect;
        }
        LaneOps.Add(effects, 0, effects.Length, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward)
    {
        Checked.Columns(indices, positions);
        Checked.Domain(indices, positions);
        var key = PairRuntime<TTrack, TClip>.Key;
        var reverse = !forward;
        int* chains = stackalloc int[256];
        var reference = default(TimelineRef);
        var pairs = 0;
        var lastIndex = -1;
        for (var i = 0; i < positions.Length; i++)
        {
            var index = indices[i];
            if (index != lastIndex)
            {
                reference = TimelineTable.Reference(index);
                pairs = checked((int)reference.PairCount);
                reference.Resolve(new Span<int>(chains, pairs), key);
                lastIndex = index;
            }
            if (reference.Select(reverse, positions[i], out _, out var tick, out var flags))
                reference.ExecuteDispatch(reverse, tick, flags, i, new Span<int>(chains, pairs));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ReadOnlySpan<ushort> positions, bool forward)
    {
        Checked.Domain(index, positions);
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        var reverse = !forward;
        var pairs = checked((int)reference.PairCount);
        int* chains = stackalloc int[pairs];
        reference.Resolve(new Span<int>(chains, pairs), key);
        for (var i = 0; i < positions.Length; i++)
            if (reference.Select(reverse, positions[i], out _, out var tick, out var flags))
                reference.ExecuteDispatch(reverse, tick, flags, i, new Span<int>(chains, pairs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ushort position, bool forward)
    {
        Checked.Domain(index, position);
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        var reverse = !forward;
        var pairs = checked((int)reference.PairCount);
        int* chains = stackalloc int[pairs];
        reference.Resolve(new Span<int>(chains, pairs), key);
        if (reference.Select(reverse, position, out _, out var tick, out var flags))
            reference.ExecuteDispatch(reverse, tick, flags, 0, new Span<int>(chains, pairs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> from, ReadOnlySpan<ushort> to, bool forward)
    {
        Checked.Range(indices, from, to);
        Checked.Domain(indices, from);
        var key = PairRuntime<TTrack, TClip>.Key;
        if (PairTable.HeadOf(key) < 0) return;
        var reverse = !forward;
        var bank = Bank();
        int* chains = stackalloc int[256];
        var reference = default(TimelineRef);
        var slot = default(SlotView*);
        var pairs = 0;
        var usable = false;
        var lastIndex = -1;
        for (var i = 0; i < from.Length; i++)
        {
            var index = indices[i];
            if (index != lastIndex)
            {
                reference = TimelineTable.Reference(index);
                pairs = checked((int)reference.PairCount);
                reference.Resolve(new Span<int>(chains, pairs), key);
                usable = reference.Uses(key);
                if (usable)
                {
                    Resolve(index);
                    slot = bank.FoldedView(index);
                }
                lastIndex = index;
            }
            if (usable)
                DispatchRange(reference, slot, new Span<int>(chains, pairs), reverse, from[i], to[i], i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ushort from, ushort to, bool forward)
    {
        Checked.Domain(index, from);
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key) || PairTable.HeadOf(key) < 0) return;
        var pairs = checked((int)reference.PairCount);
        int* chains = stackalloc int[pairs];
        reference.Resolve(new Span<int>(chains, pairs), key);
        Resolve(index);
        var slot = Bank().FoldedView(index);
        if (slot is null) return;
        DispatchRange(reference, slot, new Span<int>(chains, pairs), !forward, from, to, 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void DispatchRange(TimelineRef reference, SlotView* slot, Span<int> chains, bool reverse, ushort from, ushort to, int row)
    {
        var budget = (int)slot->Duration;
        var position = from;
        if (reverse)
        {
            while (position != to && budget-- > 0)
            {
                if (position > slot->Duration) return;
                ref var record = ref slot->BackwardRecords[position];
                var next = record.Next;
                if (next == LaneMovementRecord.Skipped) return;
                if (!reference.Select(true, position, out _, out var tick, out var flags)) return;
                reference.ExecuteDispatch(true, tick, flags, row, chains);
                position = next;
            }
            return;
        }
        while (position != to && budget-- > 0)
        {
            if ((uint)position >= (uint)slot->Duration) return;
            if (!reference.Select(false, position, out _, out var tick, out var flags)) return;
            reference.ExecuteDispatch(false, tick, flags, row, chains);
            var next = slot->ForwardRecords[position].Next;
            if (next == LaneMovementRecord.Skipped) return;
            position = next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, bool forward)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Apply(asset.Index, positions, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Apply(
            MemoryMarshal.Cast<TIndex, ushort>(indices),
            MemoryMarshal.Cast<TPosition, ushort>(positions),
            forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition>(in TIndex index, in TPosition position, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Apply(
            Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
            Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)),
            forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Apply(asset.Index, positions, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        Checked.Domain(indices, positions);
        Bank().Apply(indices, positions, next, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        Checked.Domain(index, positions);
        var bank = Bank();
        if (positions.Length > LaneOps.SmallSpan)
        {
            if (!bank.IsFolded(index))
                Resolve(index);
            bank.ApplySlot(index, positions, next, forward, effects);
            return;
        }
        var slot = bank.FoldedView(index);
        if (slot is null)
        {
            Resolve(index);
            slot = bank.FoldedView(index);
        }
        bank.ApplyRecords(slot, positions, next, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Apply(asset.Index, positions, next, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        Apply(
            MemoryMarshal.Cast<TIndex, ushort>(indices),
            MemoryMarshal.Cast<TPosition, ushort>(positions),
            forward,
            MemoryMarshal.Cast<TEffect, float>(effects));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Advance(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(in TIndex index, in TPosition position, bool forward, ref TEffect effect)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        Apply(
            Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)), 1),
            forward,
            MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, float>(ref effect), 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe void Apply<TIndex, TPosition, TEffect0, TEffect1>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect0> effects0, Span<TEffect1> effects1)
        where TIndex : struct
        where TPosition : struct
        where TEffect0 : unmanaged
        where TEffect1 : unmanaged
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2 || Unsafe.SizeOf<TEffect0>() > 4 || Unsafe.SizeOf<TEffect1>() > 4
            || positions.Length != effects0.Length || positions.Length != effects1.Length)
            ThrowColumnSizes();
        var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
        var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
        Checked.Domain(indices16, positions16);
        var bank = Bank();
        var key0 = TypeKey<TEffect0>.Value;
        var key1 = TypeKey<TEffect1>.Value;
        var ordinal1 = key1 == key0 ? 1 : 0;
        var i = 0;
        while (i < positions16.Length)
        {
            var id = indices16[i];
            var end = i + 1;
            while (end < positions16.Length && indices16[end] == id) end++;
            if (!bank.IsFolded(id)) Resolve(id);
            if (bank.HasView(id))
            {
                var lane0 = bank.LaneFor(id, key0, 0);
                var lane1 = bank.LaneFor(id, key1, ordinal1);
                if (lane0 < 0 || lane1 < 0) throw new ArgumentException("Required lane column missing for timeline result.");
                var slice = positions16.Slice(i, end - i);
                bank.ApplyLane(id, slice, forward, lane0, effects0.Slice(i, end - i));
                bank.ApplyLane(id, slice, forward, lane1, effects1.Slice(i, end - i));
            }
            i = end;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
        var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
        Checked.Domain(indices16, positions16);
        Bank().ApplyRows(rows, indices16, positions16, forward, MemoryMarshal.Cast<TEffect, float>(effects));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance<TIndex, TPosition>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
        var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
        Checked.Domain(indices16, positions16);
        Bank().AdvanceRows(rows, indices16, positions16, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance<TIndex, TPosition>(in TIndex index, ref TPosition position, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Advance(
            Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
            MemoryMarshal.CreateSpan(ref Unsafe.As<TPosition, ushort>(ref position), 1),
            forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void CheckSizes<TIndex, TPosition, TEffect>()
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2 || Unsafe.SizeOf<TEffect>() != 4)
            ThrowColumnSizes();
    }

    static void ThrowColumnSizes()
        => throw new ArgumentException("Timeline columns must be single-field: index ushort, position ushort, effect float.");

    internal static void Resolve(ushort index)
    {
        var bank = Bank();
        if (bank.IsFolded(index)) return;
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key))
            throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        if (PairTable.HeadOf(key) < 0)
            throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        using var measured = MeasuredLanes.Measure(reference, key);
        bank.AddAt(index, measured);
        var cursor = bank.PendingCursor;
        for (var below = cursor; below < index; below++)
            if (bank.IsPending((ushort)below))
                Determine(bank, (ushort)below);
        bank.AdvancePendingCursor(index);
    }

    static void Determine(TimelineSet<TTrack, TClip> bank, ushort index)
    {
        if (!TimelineTable.IsLive(index)) return;
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key))
        {
            bank.MarkAbsent(index);
            return;
        }
        if (PairTable.HeadOf(key) < 0) return;
        using var measured = MeasuredLanes.Measure(reference, key);
        bank.AddAt(index, measured);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static bool ResolveChunk(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> indices, int start, int end)
    {
        var views = set._views;
        var bound = (uint)set._count;
        for (var i = start; i < end; i++)
        {
            var index = indices[i];
            if (index >= bound)
            {
                Resolve(index);
                return true;
            }
            if (views[index] != null) continue;
            Resolve(index);
            return true;
        }
        return false;
    }

    public static SlotView View(ushort index)
    {
        var bank = Bank();
        if (bank.IsFolded(index)) return bank.View(index);
        if (bank.IsAbsent(index))
            return new SlotView
            {
                Absent = 1,
                RecordBytes = (ushort)sizeof(LaneMovementRecord),
                AbiVersion = SlotView.AbiVersionV1,
            };
        Resolve(index);
        return bank.View(index);
    }

    public static SlotView View(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return View(asset.Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static TimelineSet<TTrack, TClip> Bank()
    {
        var bank = _bank;
        if (bank is not null) return bank;
        bank = new TimelineSet<TTrack, TClip> { _lazyResolve = true };
        return Interlocked.CompareExchange(ref _bank, bank, null) ?? bank;
    }
}
