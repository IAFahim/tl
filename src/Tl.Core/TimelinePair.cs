namespace Tl;

public static class Timeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    static TimelineSet<TTrack, TClip>? _bank;

    public static ushort Bind(TimelineAsset asset)
        => Bank().Add(asset);

    public static ushort Bind(TimelineAsset asset, MeasuredLanes measured)
        => Bank().Add(asset, measured);

    public static TimelineSetLane<TTrack, TClip> Seek(ReadOnlySpan<ushort> handles, Span<ushort> positions, bool forward)
    {
        var bank = _bank;
        if (bank is null)
            throw new InvalidOperationException($"No timeline is bound for the pair ({typeof(TTrack).Name}, {typeof(TClip).Name}); call Timeline<{typeof(TTrack).Name}, {typeof(TClip).Name}>.Bind at load time.");
        return bank.Gather(handles).Seek(positions, forward);
    }

    public static void Advance(ReadOnlySpan<ushort> handles, Span<ushort> positions, bool forward, Span<float> effects)
        => Seek(handles, positions, forward).Apply(effects);

    static TimelineSet<TTrack, TClip> Bank()
    {
        var bank = _bank;
        if (bank is not null) return bank;
        bank = new TimelineSet<TTrack, TClip>();
        return Interlocked.CompareExchange(ref _bank, bank, null) ?? bank;
    }
}
