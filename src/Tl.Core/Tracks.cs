using System.Runtime.CompilerServices;
using Tl.Generation;
using Tl.Internal;

namespace Tl;

public readonly ref struct Tracks<TTrack, TClip>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
{
    private readonly uint _tick;
    private readonly ReadOnlySpan<TrackRow> _trackRows;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly ReadOnlySpan<ushort> _payloadMap;
    private readonly Span<TClip> _resolved;
    private readonly ReadOnlySpan<ClipEdge> _clipEdges;
    private readonly MovementSpan _movement;

    internal Tracks(
        uint tick,
        ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<ushort> payloadMap,
        Span<TClip> resolved,
        ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement)
    {
        _tick = tick;
        _trackRows = trackRows;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _payloadMap = payloadMap;
        _resolved = resolved;
        _clipEdges = clipEdges;
        _movement = movement;
    }

    public int Count => _trackRows.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(_tick, _trackRows, _clipRows, _trackData, _clipData, _payloadMap, _resolved, _clipEdges, _movement);

    public ref struct Enumerator
    {
        private readonly uint _tick;
        private readonly ReadOnlySpan<TrackRow> _trackRows;
        private readonly ReadOnlySpan<ClipRow> _clipRows;
        private readonly ReadOnlySpan<TTrack> _trackData;
        private readonly ReadOnlySpan<TClip> _clipData;
        private readonly ReadOnlySpan<ushort> _payloadMap;
        private readonly Span<TClip> _resolved;
        private readonly ReadOnlySpan<ClipEdge> _clipEdges;
        private readonly MovementSpan _movement;
        private int _i;
        private int _blendSlot;

        internal Enumerator(
            uint tick,
            ReadOnlySpan<TrackRow> trackRows, ReadOnlySpan<ClipRow> clipRows,
            ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
            ReadOnlySpan<ushort> payloadMap,
            Span<TClip> resolved,
            ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement)
        {
            _tick = tick;
            _trackRows = trackRows;
            _clipRows = clipRows;
            _trackData = trackData;
            _clipData = clipData;
            _payloadMap = payloadMap;
            _resolved = resolved;
            _clipEdges = clipEdges;
            _movement = movement;
            _i = -1;
            _blendSlot = -1;
        }

        public readonly TrackWork<TTrack, TClip> Current
            => new(_trackRows[_i], _tick, _clipRows, _trackData, _clipData, _payloadMap, _resolved, _clipEdges, _movement, _blendSlot);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var i = ++_i;
            if (i >= _trackRows.Length)
                return false;

            var row = _trackRows[i];

            if (row.ClipCount != 2)
                return true;

            var first = _clipRows[row.ClipStart];
            var second = _clipRows[row.ClipStart + 1];
            var factor = first.FactorLength <= 1 ? 0.5f : (_tick - first.FactorStart) / (float)(first.FactorLength - 1);
            var slot = ++_blendSlot;
            var firstPayload = _payloadMap.IsEmpty ? first.ClipIndex : _payloadMap[first.ClipIndex];
            var secondPayload = _payloadMap.IsEmpty ? second.ClipIndex : _payloadMap[second.ClipIndex];
            _trackData[row.TrackIndex].Blend(
                in _clipData[firstPayload],
                in _clipData[secondPayload],
                factor,
                out _resolved[slot]);

            return true;
        }
    }
}

public readonly ref struct TrackWork<TTrack, TClip>
    where TTrack : struct
    where TClip : struct
{
    private readonly TrackRow _row;
    private readonly uint _tick;
    private readonly ReadOnlySpan<ClipRow> _clipRows;
    private readonly ReadOnlySpan<TTrack> _trackData;
    private readonly ReadOnlySpan<TClip> _clipData;
    private readonly ReadOnlySpan<ushort> _payloadMap;
    private readonly ReadOnlySpan<TClip> _resolved;
    private readonly ReadOnlySpan<ClipEdge> _clipEdges;
    private readonly MovementSpan _movement;
    private readonly int _slot;

    internal TrackWork(
        TrackRow row, uint tick, ReadOnlySpan<ClipRow> clipRows,
        ReadOnlySpan<TTrack> trackData, ReadOnlySpan<TClip> clipData,
        ReadOnlySpan<ushort> payloadMap,
        ReadOnlySpan<TClip> resolved,
        ReadOnlySpan<ClipEdge> clipEdges, MovementSpan movement, int slot)
    {
        _row = row;
        _tick = tick;
        _clipRows = clipRows;
        _trackData = trackData;
        _clipData = clipData;
        _payloadMap = payloadMap;
        _resolved = resolved;
        _clipEdges = clipEdges;
        _movement = movement;
        _slot = slot;
    }

    public ushort Index => _row.TrackIndex;

    public ref readonly TTrack Track => ref _trackData[_row.TrackIndex];

    public ref readonly TClip Clip
    {
        get
        {
            if (_row.ClipCount == 1)
            {
                var authored = _clipRows[_row.ClipStart].ClipIndex;
                return ref _clipData[_payloadMap.IsEmpty ? authored : _payloadMap[authored]];
            }

            return ref _resolved[_slot];
        }
    }

    public ClipState State
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            uint start, end;
            if (_row.ClipCount == 1)
            {
                var edge = _clipEdges[_clipRows[_row.ClipStart].ClipIndex];
                start = edge.Start;
                end = edge.End;
            }
            else
            {
                var first = _clipEdges[_clipRows[_row.ClipStart].ClipIndex];
                var second = _clipEdges[_clipRows[_row.ClipStart + 1].ClipIndex];
                start = first.Start < second.Start ? first.Start : second.Start;
                end = first.End > second.End ? first.End : second.End;
            }

            if (_movement.Backward ? _tick == start : _tick == end - 1)
                return ClipState.Exit;

            return _movement.Crossed(_movement.Backward ? end : start, _tick) ? ClipState.Enter : ClipState.Stay;
        }
    }
}
