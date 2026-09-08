using System.Runtime.CompilerServices;
using Tl.Internal;

namespace Tl;

// One MATERIALIZED active track of a region: everything a per-tick view of
// the work needs, resolved at BUILD time (PlaybackCore.MaterializeWorkSlots
// — once per timeline, not per tick or per call) into a table parallel to
// the track rows. The active work set is constant inside a region — and for
// the lifetime of the tables; only the tick (blend factors, movement
// facts) varies per tick.
//   Index       the track's authored index — also its payload-table index
//               (the track payload table is one row per authored track)
//   First       singles: the payload-map-RESOLVED payload index of the one
//               clip (Second carries the Single sentinel); blends: the
//               first partner's resolved payload index
//   Second      blends: the second partner's resolved payload index
//   EnterF/EnterB  the work's OUTER window entry-reference edges — Start
//               (forward entry) and End (backward entry). For a work active
//               at the destination the crossed-span Enter test reduces to
//               ONE prev-compare against them (activity pins
//               Start <= tick < End); wrapped spans and multi-cycle jumps
//               crossed the entry edge unconditionally. The positional Exit
//               compares ride the same pair (tick == EnterB-1 forward,
//               tick == EnterF backward)
//   FactorStart/FactorLength  the blend window (FactorLength 0 marks a
//               standalone clip — the authored table convention)
//   BlendOrdinal  dense region-local scratch ordinal for blends, assigned
//               in slot order (visit order for a forward consumer); ONLY a
//               Clip read ever writes the slot, so works the consumer
//               never visits are never blended and never take scratch
internal readonly struct WorkSlot(
    ushort index,
    ushort first,
    ushort second,
    uint enterF,
    uint enterB,
    uint factorStart,
    uint factorLength,
    ushort blendOrdinal)
{
    public const ushort Single = ushort.MaxValue;

    public readonly uint EnterF = enterF;
    public readonly uint EnterB = enterB;
    public readonly uint FactorStart = factorStart;
    public readonly uint FactorLength = factorLength;
    public readonly ushort Index = index;
    public readonly ushort First = first;
    public readonly ushort Second = second;
    public readonly ushort BlendOrdinal = blendOrdinal;
}

// The per-tick view: a thin, zero-copy slice of the region's MATERIALIZED
// WorkSlot table plus the per-tick facts (effective tick, blend scratch,
// movement span, payload tables). Count, indexing, slicing and foreach are
// the whole surface — the hook fires only when Count is positive. The slot
// table is materialized once per timeline (build time for the registry
// path, a per-closure static cache for generated tables), so a per-tick
// handoff is a slice and a tick, not a table walk.
public readonly ref struct Tracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly ReadOnlySpan<WorkSlot> _slots;
    private readonly uint _tick;
    private readonly Span<TClip> _blendScratch;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly MovementSpan _movement;

    internal Tracks(
        ReadOnlySpan<WorkSlot> slots, uint tick,
        Span<TClip> blendScratch,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        MovementSpan movement)
    {
        _slots = slots;
        _tick = tick;
        _blendScratch = blendScratch;
        _trackData = trackData;
        _clipData = clipData;
        _movement = movement;
    }

    public int Count => _slots.Length;

    // First-class indexing: a view of one work, bounds-checked by the span.
    public TrackWork<TTrack, TClip> this[int index]
        => new(in _slots[index], _tick, _blendScratch, _trackData, _clipData, _movement);

    // Zero-copy sub-view of works [start, start + length); bounds-checked.
    public Tracks<TTrack, TClip> Slice(int start, int length)
        => new(_slots.Slice(start, length), _tick, _blendScratch, _trackData, _clipData, _movement);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(_slots, _tick, _blendScratch, _trackData, _clipData, _movement);

    // Foreach sugar: a trivial walk over the slot span. The enumerator
    // carries the view's handful of fields, NOT the table bundle — Current
    // is a thin view of one slot.
    public ref struct Enumerator
    {
        private ReadOnlySpan<WorkSlot> _slots;
        private readonly uint _tick;
        private readonly Span<TClip> _blendScratch;
        private readonly ReadOnlySpan<TTrack> _trackData;
        private readonly ReadOnlySpan<TClip> _clipData;
        private readonly MovementSpan _movement;
        private int _i;

        internal Enumerator(
            ReadOnlySpan<WorkSlot> slots, uint tick,
            Span<TClip> blendScratch,
            ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
            MovementSpan movement)
        {
            _slots = slots;
            _tick = tick;
            _blendScratch = blendScratch;
            _trackData = trackData;
            _clipData = clipData;
            _movement = movement;
            _i = -1;
        }

        public TrackWork<TTrack, TClip> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(in _slots[_i], _tick, _blendScratch, _trackData, _clipData, _movement);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var i = _i + 1;
            if ((uint)i >= (uint)_slots.Length)
                return false;
            _i = i;
            return true;
        }
    }
}

// One resolved (track, clip) pair: a thin view of ONE materialized WorkSlot
// plus the per-tick facts. Index is the track's authored index, Track points
// into the payload table, and Clip points into the payload table for an
// original clip or into the blend scratch for a blended one — resolved on
// first visit, so works the consumer never reads are never blended. Either
// way the consumer sees exactly one clip. State is the per-work movement
// fact.
public readonly ref struct TrackWork<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly ref readonly WorkSlot _slot;
    private readonly uint _tick;
    private readonly Span<TClip> _blendScratch;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly MovementSpan _movement;

    internal TrackWork(
        in WorkSlot slot, uint tick,
        Span<TClip> blendScratch,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        MovementSpan movement)
    {
        _slot = ref slot;
        _tick = tick;
        _blendScratch = blendScratch;
        _trackData = trackData;
        _clipData = clipData;
        _movement = movement;
    }

    public ushort Index => _slot.Index;

    public ref readonly TTrack Track => ref _trackData[_slot.Index];

    public ref readonly TClip Clip
    {
        get
        {
            // Standalone clip: the slot carries the payload-map-RESOLVED
            // index (materialized at build time) — a direct payload read.
            if (_slot.FactorLength == 0)
                return ref _clipData[_slot.First];

            // Blend pair: resolve on first visit. The scratch ordinal was
            // assigned at materialization in slot order; ONLY this read
            // blends, so works never visited are never blended and never
            // take scratch. The factor is the engine's exact expression.
            var ordinal = _slot.BlendOrdinal;
            var factor = _slot.FactorLength <= 1
                ? 0.5f
                : (_tick - _slot.FactorStart) / (float)(_slot.FactorLength - 1);
            _trackData[_slot.Index].Blend(
                in _clipData[_slot.First],
                in _clipData[_slot.Second],
                factor,
                out _blendScratch[ordinal]);
            return ref _blendScratch[ordinal];
        }
    }

    // The per-work movement fact — the aggregate status word's replacement:
    // - Exit: POSITIONAL. Forward: destination == window End-1 (the work's
    //   last active frame); backward mirror: destination == window Start
    //   (the last frame visited moving backward). Exit takes precedence
    //   over Enter, so every visit's final frame is Exit even for
    //   one-frame windows and jumps landing on the last frame.
    // - Enter: the step crossed the entry edge — forward the OUTER window's
    //   Start (prevEff < Start <= tick), backward through its End. For a
    //   work ACTIVE at the destination the span test reduces to ONE
    //   prev-compare against the slot's entry references (activity pins
    //   Start <= tick < End); wrapped spans and multi-cycle jumps crossed
    //   the entry edge unconditionally. Sequentially Enter coincides with
    //   the first active frame. A stateless sample or repeated position
    //   crosses nothing: no Enter.
    // - Stay otherwise.
    //
    // CONVENTION (pinned by the per-work oracle tests): a blend-resolved
    // work reports the pair's OUTER window — min Start and max End of the
    // two clips — materialized into the slot's entry references at build
    // time.
    public ClipState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var flags = _movement.Flags;
            var backward = (flags & MovementFlags.Backward) != 0;
            if (backward ? _tick == _slot.EnterF : _tick == _slot.EnterB - 1u)
                return ClipState.Exit;

            if ((flags & MovementFlags.EnterPossible) == 0)
                return ClipState.Stay;

            var crossed = (flags & MovementFlags.CrossedAlways) != 0
                || (backward ? _movement.PrevEff >= _slot.EnterB : _movement.PrevEff < _slot.EnterF);
            return crossed ? ClipState.Enter : ClipState.Stay;
        }
    }
}
