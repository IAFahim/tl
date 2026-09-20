using System.Runtime.CompilerServices;

namespace Tl;

public static unsafe class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    static TimelineSet<TTrack, TClip>? _bank;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => Bank().Apply(indices, positions, forward, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        var bank = Bank();
        if (!bank.IsFolded(index))
            Resolve(index);
        bank.ApplySlot(index, positions, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Apply(asset.Index, positions, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => Bank().Apply(indices, positions, next, forward, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        var bank = Bank();
        if (!bank.IsFolded(index))
            Resolve(index);
        bank.ApplySlot(index, positions, next, forward, effects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Apply(asset.Index, positions, next, forward, effects);
    }

    internal static void Resolve(ushort index)
    {
        var bank = Bank();
        if (bank.IsFolded(index)) return;
        var reference = TimelineTable.Reference(index);
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key))
            throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        if (PairTable.Head(key) < 0)
            throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        using var measured = MeasuredLanes.Measure(reference);
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
        if (PairTable.Head(key) < 0) return;
        using var measured = MeasuredLanes.Measure(reference);
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
