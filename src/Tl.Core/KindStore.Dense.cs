using System.Runtime.CompilerServices;
#if NET
using System.Numerics;
using System.Runtime.Intrinsics;
#endif

namespace Tl;

public sealed unsafe partial class KindStore
{
    int* _rowBase;
    int _rowHalf = -1;
    ushort* _gap;
    ulong* _leave;
    byte* _eligible;
    int _denseEpoch;
    int _eligibleEpoch = -1;
    int _denseDir;
    bool _denseReady;
    bool _denseValues;
    internal bool DenseEnabled = true;

    void BuildRows()
    {
        var count = Math.Max(1, _timelineCount);
        _rowBase = (int*)Zeroed((nuint)count * 4);
        _eligible = (byte*)Zeroed((nuint)count);
        _rowHalf = KindBuild.RowSpace(_tls, _timelineCount, _rowBase);
        _eligibleEpoch = -1;
        if (_rowHalf < 0) return;
        _gap = (ushort*)Zeroed((nuint)Math.Max(1, 2 * _rowHalf) * 2);
        for (var t = 0; t < _timelineCount; t++)
        {
            var dur = (int)(_spanMeta[t] & 0x3FFF_FFFFu);
            var pb = _pulseBase[t];
            for (var d = 0; d < 2; d++)
            {
                var dir = d == 0 ? 1 : -1;
                var gap = _gap + d * _rowHalf + _rowBase[t] - 1;
                for (var n = 0; n < dur; n++)
                {
                    var k = dir > 0 ? dur - 1 - n : n;
                    var next = k + dir;
                    var stop = (uint)next >= (uint)dur || (pb >= 0 && (_pulseBits[pb + next] & (1 << d)) != 0);
                    gap[k] = stop ? (ushort)1 : (ushort)(1 + gap[next]);
                }
            }
        }
        var rows = 1 + 2 * _rowHalf;
        _leave = (ulong*)Zeroed((nuint)(2 * rows) * 8);
        for (var w = 0; w < 2; w++)
        {
            var step = w == 0 ? 1 : -1;
            _leave[w * rows] = 1ul | (ulong)(ushort)(-step) << 16;
            for (var t = 0; t < _timelineCount; t++)
                for (var j = 0; j < Duration(t); j++)
                    for (var reverse = 0; reverse < 2; reverse++)
                    {
                        var tick = reverse != 0 ? Duration(t) - 1 - j : j;
                        var row = RowOf(t, reverse != 0, tick);
                        var dir = reverse != 0 ? -step : step;
                        var next = tick + dir;
                        if ((uint)next >= (uint)Duration(t))
                        {
                            if ((_spanMeta[t] & 0x8000_0000u) == 0) continue;
                            next = next < 0 ? Duration(t) - 1 : 0;
                        }
                        var d = dir > 0 ? 0 : 1;
                        var pb = _pulseBase[t];
                        var cell = pb >= 0 && (_pulseBits[pb + next] & (1 << d)) != 0 ? ((pb + next) << 1) + d : -1;
                        _leave[w * rows + row] = GapOf(t, dir, next) | (ulong)(ushort)(RowOf(t, reverse != 0, next) - row - step) << 16 | (ulong)(uint)(cell + 1) << 32;
                    }
        }
    }

    void ReleaseRows()
    {
        Free4(_rowBase, _gap, _eligible, _leave);
        _rowBase = null;
        _gap = null;
        _leave = null;
        _eligible = null;
        _rowHalf = -1;
        _denseReady = _denseValues = false;
        _denseDir = 0;
    }

    void DenseBegin(int step)
    {
        if (DenseEnabled && _rowHalf > 0 && _eligibleEpoch != _denseEpoch)
        {
            _eligibleEpoch = _denseEpoch;
            _denseValues = false;
            for (var k = 0; k < _kindCount; k++) _denseValues |= _values[k].Data != null;
            for (var t = 0; t < _timelineCount; t++)
            {
                var ok = _spanMeta[t] != 0;
                for (var p = _pairStart[t]; ok && p < _pairStart[t + 1]; p++)
                {
                    var feeder = _sec.Feeders + (_pairs[p] & 0xffff);
                    if (feeder->Trigger == 0 && _values[feeder->Kind].Data == null) ok = false;
                }
                _eligible[t] = ok ? (byte)1 : (byte)0;
            }
        }
        _denseReady = DenseEnabled && _rowHalf > 0 && _denseValues;
        if (!_denseReady || step != _denseDir) UndenseAll();
        if (_denseReady) _players.EnsureDense();
    }

    void DenseEnd(int step)
    {
        _players.DenseClock = (ushort)(_players.DenseClock + step);
        _players.Settled = false;
        _denseDir = step;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int Duration(int t) => (int)(_spanMeta[t] & 0x3FFF_FFFFu);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int RowOf(int t, bool reverse, int tick) => _rowBase[t] + (reverse ? _rowHalf + Duration(t) - 1 - tick : tick);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int TickAt(int t, bool reverse, int row)
    {
        var j = row - _rowBase[t] - (reverse ? _rowHalf : 0);
        return reverse ? Duration(t) - 1 - j : j;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ushort GapOf(int t, int direction, int tick) => _gap[(direction > 0 ? 0 : _rowHalf) + _rowBase[t] - 1 + tick];

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal void Settle(int entity)
    {
        var slots = _players.SlotsOf(entity);
        var origins = _players.OriginsOf(entity);
        var n = _players.CountOf(entity);
        var clock = _players.DenseClock;
        for (var i = 0; i < n; i++)
        {
            var id = slots[i].Timeline;
            if (id != 0) slots[i].Tick = (ushort)TickAt(id - 1, slots[i].Flags != 0, (ushort)(origins[i] + clock));
        }
    }

    internal void Undense(int entity)
    {
        Settle(entity);
        _players.SetDense(entity, false);
    }

    void UndenseAll()
    {
        var dense = _players.DenseBits;
        if (dense == null) return;
        for (var w = 0; w < _players.PlayingWords; w++)
            for (var bits = dense[w]; bits != 0; bits &= bits - 1) Undense((w << 6) + Tz(bits));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void Densify(int entity, ushort clock, int step)
    {
        if (_samples[entity] != 0) return;
        var slots = _players.SlotsOf(entity);
        var n = _players.CountOf(entity);
        var live = false;
        for (var i = 0; i < n; i++)
        {
            var s = slots + i;
            if (s->Timeline == 0) continue;
            if ((s->Flags & ~PlaybackFlags.Reverse) != 0 || s->Rate != 0 || _eligible[s->Timeline - 1] == 0) return;
            live = true;
        }
        if (!live) return;
        var origins = _players.OriginsOf(entity);
        var dues = _players.DuesOf(entity);
        for (var i = 0; i < n; i++)
        {
            var s = slots + i;
            if (s->Timeline == 0)
            {
                origins[i] = (ushort)(0 - clock);
                dues[i] = (ushort)(clock + step);
                continue;
            }
            var t = s->Timeline - 1;
            var reverse = s->Flags != 0;
            origins[i] = (ushort)(RowOf(t, reverse, s->Tick) - clock);
            dues[i] = (ushort)(clock + step * GapOf(t, reverse ? -step : step, s->Tick));
        }
        _players.SetDense(entity, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    bool DenseStep(int entity, int step, ushort from, ushort to, byte* due, int held, ushort* dues, ushort* scratch)
    {
        var origins = _players.OriginsOf(entity);
        var leave = _leave + (step > 0 ? 0 : 1 + 2 * _rowHalf);
        var ep = _t.EntryPulse;
        var fast = ep != null && (ep[entity >> 6] >> (entity & 63) & 1) != 0 ? 0ul : _t.FastPulseMask;
        var pushed = 0ul;
        for (var h = 0; h < held; h++)
        {
            var origin = origins[due[h]];
            var record = leave[(ushort)(origin + from)];
            var gap = (ushort)record;
            if (gap == 0) goto undo;
            var cell = (int)(record >> 32) - 1;
            if (cell >= 0)
            {
                var kinds = _pulseMasks[cell];
                if ((kinds & ~fast) != 0) goto undo;
                pushed |= kinds;
                for (; kinds != 0; kinds &= kinds - 1) _pulseHits[Tz(kinds)].Push(entity, cell);
            }
            scratch[2 * h] = (ushort)(origin + (ushort)(record >> 16));
            scratch[2 * h + 1] = (ushort)(to + step * gap);
        }
        for (var h = 0; h < held; h++)
        {
            if (origins[due[h]] != scratch[2 * h]) origins[due[h]] = scratch[2 * h];
            dues[due[h]] = scratch[2 * h + 1];
        }
        return true;
    undo:
        for (; pushed != 0; pushed &= pushed - 1) _pulseHits[Tz(pushed)].Rewind(entity);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int DueSlots(ushort* dues, int n, ushort at, byte* hits)
    {
        var held = 0;
        var i = 0;
#if NET
        if (Vector256.IsHardwareAccelerated)
            for (var target = Vector256.Create(at); i + 16 <= n; i += 16)
                for (var m = Vector256.Equals(Vector256.Load(dues + i), target).ExtractMostSignificantBits(); m != 0; m &= m - 1)
                    hits[held++] = (byte)(i + BitOperations.TrailingZeroCount(m));
#endif
        for (; i < n; i++)
            if (dues[i] == at) hits[held++] = (byte)i;
        return held;
    }
}
