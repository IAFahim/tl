namespace Tl;

public static unsafe class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    static TimelineSet<TTrack, TClip>? _bank;

    public static TimelineSetLane<TTrack, TClip> Seek(ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward)
        => Bank().Gather(indices).Seek(positions, forward);

    public static void Advance(ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward, Span<float> effects)
        => Bank().Advance(indices, positions, forward, effects);

    public static void Advance(ushort index, Span<ushort> positions, bool forward, Span<float> effects)
    {
        var bank = Bank();
        if (!bank.IsFolded(index))
            Resolve(index);
        bank.ApplySlot(index, positions, forward, effects);
    }

    public static void Advance(TimelineAsset asset, Span<ushort> positions, bool forward, Span<float> effects)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Advance(asset.Index, positions, forward, effects);
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

    internal static bool ResolveChunk(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> indices, int start, int end)
    {
        var slots = set._slots;
        var bound = (uint)set._count;
        for (var i = start; i < end; i++)
        {
            var index = indices[i];
            if (index >= bound)
            {
                Resolve(index);
                return true;
            }
            if (slots[index].Forward != null) continue;
            if (slots[index].Absent != 0)
                throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
            Resolve(index);
            return true;
        }
        return false;
    }

    static TimelineSet<TTrack, TClip> Bank()
    {
        var bank = _bank;
        if (bank is not null) return bank;
        bank = new TimelineSet<TTrack, TClip> { _lazyResolve = true };
        return Interlocked.CompareExchange(ref _bank, bank, null) ?? bank;
    }
}
