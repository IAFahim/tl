using Tl.Authoring;

namespace Tl;

public readonly ref struct TrackRef
{
    internal object Owner { get; }
    internal ushort Index { get; }

    internal TrackRef(object owner, ushort index)
    {
        Owner = owner;
        Index = index;
    }
}

public readonly ref struct TimelineBuilder<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    internal sealed class Authoring
    {
        public List<TTrack> Tracks = [];
        public List<(ushort Track, TClip Clip, uint Start, uint End)> Clips = [];
        public bool Loops;
        public TimelineOptions Options;
    }

    internal readonly Authoring _state;

    internal TimelineBuilder(TimelineOptions options)
    {
        _state = new Authoring { Options = options };
    }

    public TrackRef Track(in TTrack track)
    {
        if (_state.Tracks.Count == ushort.MaxValue)
            throw new InvalidOperationException("Track capacity exceeded.");
        _state.Tracks.Add(track);
        return new TrackRef(_state, (ushort)(_state.Tracks.Count - 1));
    }

    public void Clip(in TrackRef track, in TClip clip, uint start, uint end)
    {
        if (!ReferenceEquals(track.Owner, _state))
            throw new ArgumentException("TrackRef does not belong to this builder.", nameof(track));
        if ((uint)track.Index >= (uint)_state.Tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(track), track.Index, "TrackRef does not name a track of this timeline.");
        if (end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "Clip end must be after its start.");
        if (_state.Clips.Count == ushort.MaxValue)
            throw new InvalidOperationException("Clip capacity exceeded.");

        _state.Clips.Add((track.Index, clip, start, end));
    }

    public void Looping() => _state.Loops = true;

    public void DedupStorage(bool enabled = true) => _state.Options = _state.Options with { DedupStorage = enabled };
}

public delegate void TimelineBuild<TTrack, TClip>(scoped TimelineBuilder<TTrack, TClip> builder)
    where TTrack : struct
    where TClip : struct;

public delegate void TimelineBuild<TTrack, TClip, TSource>(scoped TimelineBuilder<TTrack, TClip> builder, TSource source)
    where TTrack : struct
    where TClip : struct;

// AUTHORED, NOT YET REAL: what Build returns. Authoring syntax only — the
// timeline exists once a terminal operation runs. InMemory is that terminal
// operation: it lowers the authored tracks/clips through the runtime
// lowering into ONE native block, registers it in the native slot table,
// and returns the ushort handle every Timeline hub call accepts. The token
// is a ref struct, so it cannot be parked in a field or leaked past the
// statement that uses it; drop it and nothing was ever created.
public readonly ref struct TimelineAuthoring<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    private readonly TimelineBuilder<TTrack, TClip>.Authoring _state;

    internal TimelineAuthoring(TimelineBuilder<TTrack, TClip>.Authoring state) => _state = state;

    // The unmanaged runtime timeline: one native allocation, explicitly
    // owned, freed by Timeline.Destroy(handle). Transient managed locals
    // during this call are acceptable; what it registers and retains is
    // unmanaged memory and nothing else.
    public ushort InMemory() => RuntimeLowering.Lower<TTrack, TClip>(_state);
}
