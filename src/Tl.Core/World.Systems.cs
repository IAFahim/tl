using System;

namespace Tl;

public readonly struct PlaybackInfo
{
    public PlaybackInfo(TimelineId timeline, int tick, byte channel, bool paused, bool reversed, byte rate, byte carry)
    {
        Timeline = timeline;
        Tick = tick;
        Channel = channel;
        Paused = paused;
        Reversed = reversed;
        Rate = rate;
        Carry = carry;
    }

    public readonly TimelineId Timeline;
    public readonly int Tick;
    public readonly byte Channel;
    public readonly bool Paused;
    public readonly bool Reversed;
    public readonly byte Rate;
    public readonly byte Carry;
}

public readonly struct ClosingEntry
{
    public ClosingEntry(Row row, TimelineId timeline)
    {
        Row = row;
        Timeline = timeline;
    }

    public readonly Row Row;
    public readonly TimelineId Timeline;
}

public static class Pipeline
{
    public static TickReport Tick(World world, Direction direction)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        world.Tick(direction);
        return world.Report;
    }

    public static TickReport Tick(World world, Direction direction, Action<World>? systems)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        world.Tick(direction);
        systems?.Invoke(world);
        return world.Report;
    }
}

public sealed unsafe partial class World
{
    internal const byte SoloPaused = 1;
    internal const byte SoloReverse = 2;
    internal const byte SoloHeld = 4;

    Requests5 _pending = new Requests5();
    int[] _soloRow = new int[0];
    ushort[] _soloTimeline = new ushort[0];
    ushort[] _soloTick = new ushort[0];
    byte[] _soloChannel = new byte[0];
    byte[] _soloFlags = new byte[0];
    byte[] _soloRate = new byte[0];
    byte[] _soloCarry = new byte[0];
    internal int _soloCount;
    ClosingEntry[] _closing = new ClosingEntry[16];
    internal int _closingCount;
    ushort[] _nextTimeline = new ushort[0];
    bool[] _holdTimeline = new bool[0];
    byte[] _channelTimeline = new byte[0];
    PlaybackIndex _plainTl = new PlaybackIndex();
    PlaybackIndex _plainCh = new PlaybackIndex();
    PlaybackIndex _soloTl = new PlaybackIndex();
    PlaybackIndex _soloCh = new PlaybackIndex();
    bool _indexesStale;

    static ulong PackTimeline(int row, ushort timeline) => ((ulong)(uint)row << 32) | timeline;
    static ulong PackChannel(int row, byte channel) => ((ulong)(uint)row << 32) | channel;

    private sealed class PlaybackIndex
    {
        const ulong Tombstone = ulong.MaxValue;
        internal ulong[] Keys = new ulong[128];
        internal int[] Pos = new int[128];
        internal int[] Cnt = new int[128];
        internal int Mask = 127;
        internal int Count;
        internal int Tombs;

        static uint Mix(ulong x)
        {
            x ^= x >> 33;
            x *= 0xff51afd7ed558ccdUL;
            x ^= x >> 33;
            x *= 0xc4ceb9fe1a85ec53UL;
            x ^= x >> 33;
            return (uint)x;
        }

        internal bool TryGet(ulong key, out int pos, out int cnt)
        {
            pos = -1;
            cnt = 0;
            var h = (int)(Mix(key) & (uint)Mask);
            while (true)
            {
                var k = Keys[h];
                if (k == 0) return false;
                if (k == key)
                {
                    pos = Pos[h];
                    cnt = Cnt[h];
                    return true;
                }
                h = (h + 1) & Mask;
            }
        }

        internal int Find(ulong key)
        {
            return TryGet(key, out var pos, out _) ? pos : -1;
        }

        internal void Insert(ulong key, int value)
        {
            if ((Count + Tombs + 1) * 10 > (Mask + 1) * 7) Grow();
            var h = (int)(Mix(key) & (uint)Mask);
            var tomb = -1;
            while (true)
            {
                var k = Keys[h];
                if (k == 0)
                {
                    var slot = tomb >= 0 ? tomb : h;
                    Keys[slot] = key;
                    Pos[slot] = value;
                    Cnt[slot] = 1;
                    Count++;
                    if (tomb >= 0) Tombs--;
                    return;
                }
                if (k == Tombstone)
                {
                    if (tomb < 0) tomb = h;
                }
                else if (k == key)
                {
                    if (value < Pos[h]) Pos[h] = value;
                    Cnt[h]++;
                    return;
                }
                h = (h + 1) & Mask;
            }
        }

        internal void Delete(ulong key)
        {
            var h = (int)(Mix(key) & (uint)Mask);
            while (true)
            {
                var k = Keys[h];
                if (k == 0) return;
                if (k == key)
                {
                    Keys[h] = Tombstone;
                    Pos[h] = -1;
                    Cnt[h] = 0;
                    Count--;
                    Tombs++;
                    return;
                }
                h = (h + 1) & Mask;
            }
        }

        internal void Decrement(ulong key)
        {
            var h = (int)(Mix(key) & (uint)Mask);
            while (true)
            {
                var k = Keys[h];
                if (k == 0) return;
                if (k == key)
                {
                    Cnt[h]--;
                    return;
                }
                h = (h + 1) & Mask;
            }
        }

        internal void SetCount(ulong key, int cnt)
        {
            var h = (int)(Mix(key) & (uint)Mask);
            while (true)
            {
                var k = Keys[h];
                if (k == 0) return;
                if (k == key)
                {
                    Cnt[h] = cnt;
                    return;
                }
                h = (h + 1) & Mask;
            }
        }

        internal void SetPosIfFirst(ulong key, int oldPos, int newPos)
        {
            var h = (int)(Mix(key) & (uint)Mask);
            while (true)
            {
                var k = Keys[h];
                if (k == 0) return;
                if (k == key)
                {
                    if (Pos[h] == oldPos) Pos[h] = newPos;
                    return;
                }
                h = (h + 1) & Mask;
            }
        }

        internal void Clear()
        {
            Array.Clear(Keys, 0, Keys.Length);
            Array.Clear(Pos, 0, Pos.Length);
            Array.Clear(Cnt, 0, Cnt.Length);
            Count = 0;
            Tombs = 0;
        }
        internal void EnsureCapacity(int additional)
        {
            if (additional <= 0) return;
            if ((Count + Tombs + additional) * 10 <= (Mask + 1) * 7) return;
            var newCap = (Mask + 1) * 2;
            while ((Count + Tombs + additional) * 10 > newCap * 7) newCap *= 2;
            var nk = new ulong[newCap];
            var np = new int[newCap];
            var nc = new int[newCap];
            var nmask = newCap - 1;
            for (var i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                if (k == 0 || k == Tombstone) continue;
                var h = (int)(Mix(k) & (uint)nmask);
                while (nk[h] != 0) h = (h + 1) & nmask;
                nk[h] = k;
                np[h] = Pos[i];
                nc[h] = Cnt[i];
            }
            Keys = nk;
            Pos = np;
            Cnt = nc;
            Mask = nmask;
            Tombs = 0;
        }

        void Grow()
        {
            EnsureCapacity(1);
        }
    }

    void InitSystems(int timelineCount)
    {
        _pending = new Requests5();
        var cap = _rowCapacity < 64 ? 64 : _rowCapacity;
        _soloRow = new int[cap];
        _soloTimeline = new ushort[cap];
        _soloTick = new ushort[cap];
        _soloChannel = new byte[cap];
        _soloFlags = new byte[cap];
        _soloRate = new byte[cap];
        _soloCarry = new byte[cap];
        _soloCount = 0;
        _closing = new ClosingEntry[16];
        _closingCount = 0;
        _nextTimeline = new ushort[timelineCount];
        _holdTimeline = new bool[timelineCount];
        _channelTimeline = new byte[timelineCount];
    }

    public int PendingRequests => _pending.Count;

    public void ApplyRequests() => ApplyPendingRequests();

    internal void EnqueueStart(Row row, TimelineId timeline, int phase)
    {
        _pending.Add(row, -1, RequestOp.Start, timeline, ClampU16(phase));
    }

    internal void EnqueueStop(Row row, TimelineId timeline)
    {
        _pending.Add(row, -1, RequestOp.Stop, timeline, 0);
    }

    internal void EnqueueStopChannel(Row row, byte channel)
    {
        _pending.Add(row, -1, RequestOp.StopChannel, ChannelId(channel), 0);
    }

    internal void EnqueueStopAll(Row row)
    {
        _pending.Add(row, -1, RequestOp.StopAll, TimelineId.None, 0);
    }

    internal void EnqueuePause(Row row, TimelineId timeline, bool pause)
    {
        _pending.Add(row, -1, pause ? RequestOp.Pause : RequestOp.Resume, timeline, 0);
    }

    internal void EnqueuePauseChannel(Row row, byte channel, bool pause)
    {
        _pending.Add(row, -1, pause ? RequestOp.PauseChannel : RequestOp.ResumeChannel, ChannelId(channel), 0);
    }

    internal void EnqueueSeek(Row row, TimelineId timeline, int tick)
    {
        _pending.Add(row, -1, RequestOp.Seek, timeline, ClampU16(tick));
    }

    internal void EnqueueSeekChannel(Row row, byte channel, int tick)
    {
        _pending.Add(row, -1, RequestOp.SeekChannel, ChannelId(channel), ClampU16(tick));
    }

    internal void EnqueueReverse(Row row, TimelineId timeline, bool reverse)
    {
        _pending.Add(row, -1, RequestOp.Reverse, timeline, reverse ? (ushort)1 : (ushort)0);
    }

    internal void EnqueueReverseChannel(Row row, byte channel, bool reverse)
    {
        _pending.Add(row, -1, RequestOp.ReverseChannel, ChannelId(channel), reverse ? (ushort)1 : (ushort)0);
    }

    internal void EnqueueSpeed(Row row, TimelineId timeline, int sixteenths)
    {
        _pending.Add(row, -1, RequestOp.Speed, timeline, ClampU16(sixteenths));
    }

    internal void EnqueueSpeedChannel(Row row, byte channel, int sixteenths)
    {
        _pending.Add(row, -1, RequestOp.SpeedChannel, ChannelId(channel), ClampU16(sixteenths));
    }

    static ushort ClampU16(int value)
    {
        if (value < 0) return 0;
        if (value > 65535) return 65535;
        return (ushort)value;
    }

    static TimelineId ChannelId(byte channel) => new TimelineId(channel);

    internal void ApplyPendingRequests()
    {
        var pendingCount = _pending.Count;
        if (pendingCount == 0) return;
        // One pass classifies the batch: already in stable (row, slot) order
        // (bulk setups enqueue that way) and/or pure Starts. A sorted batch
        // needs no radix sort at all; allStarts is permutation-invariant.
        var batchSorted = true;
        var allStarts = true;
        var prevRow = 0;
        var prevSlot = 0;
        for (var i = 0; i < pendingCount; i++)
        {
            var pending = _pending[i];
            if (pending.Op != RequestOp.Start) allStarts = false;
            var row = pending.Row.Value;
            if (i != 0 && (row < prevRow || (row == prevRow && pending.Slot < prevSlot)))
                batchSorted = false;
            prevRow = row;
            prevSlot = pending.Slot;
        }
        if (!batchSorted) _pending.Sort();
        // Cold bulk fast path: a pure-bulk Start load into an empty world
        // (the setup case) is applied in row order without maintaining the
        // hash indexes per request. Duplicates are tracked with a rolling
        // per-row bitmask (the batch is row-sorted, so one row's seen set is
        // live at a time). The indexes are rebuilt lazily on the first
        // lookup (EnsureIndexes); Tick itself never consults them. Mixed
        // batches, channel libraries, and very large timeline sets keep the
        // hash path (EnsureIndexes first if a prior bulk left them stale).
        if (allStarts && _playbackCount == 0 && _soloCount == 0 && _duration.Length <= 512)
        {
            var hasChannel = false;
            for (var i = 0; i < _channelTimeline.Length; i++)
            {
                if (_channelTimeline[i] != 0) { hasChannel = true; break; }
            }
            if (!hasChannel)
            {
                var words = (_duration.Length + 63) >> 6;
                Span<ulong> seen = stackalloc ulong[8];
                var seenSlice = seen.Slice(0, words);
                seenSlice.Clear();
                var bulkRow = -1;
                for (var i = 0; i < pendingCount; i++)
                {
                    var request = _pending[i];
                    StartPlaybackBulk(request.Row, request.Timeline, request.At, seenSlice, ref bulkRow);
                }
                _pending.Clear();
                if (_playbackCount != 0 || _soloCount != 0) _indexesStale = true;
                return;
            }
        }
        if (_indexesStale) EnsureIndexes();
        for (var i = 0; i < _pending.Count; i++)
        {
            var request = _pending[i];
            switch (request.Op)
            {
                case RequestOp.Start:
                    StartPlayback(request.Row, request.Timeline, request.At);
                    break;
                case RequestOp.Stop:
                    if (!ValidRow(request.Row)) { _invalidPending++; break; }
                    StopPlayback(request.Row, request.Timeline);
                    break;
                case RequestOp.StopChannel:
                    if (!ValidRow(request.Row)) { _invalidPending++; break; }
                    StopPlaybackChannel(request.Row, (byte)request.Timeline.Value);
                    break;
                case RequestOp.StopAll:
                    if (!ValidRow(request.Row)) { _invalidPending++; break; }
                    StopPlaybackAll(request.Row);
                    break;
                case RequestOp.Pause:
                case RequestOp.Resume:
                    PausePlayback(request.Row, request.Timeline, request.Op == RequestOp.Pause);
                    break;
                case RequestOp.Seek:
                    SeekPlayback(request.Row, request.Timeline, request.At);
                    break;
                case RequestOp.Reverse:
                    ReversePlayback(request.Row, request.Timeline, request.At != 0);
                    break;
                case RequestOp.PauseChannel:
                case RequestOp.ResumeChannel:
                    PausePlaybackChannel(request.Row, (byte)request.Timeline.Value, request.Op == RequestOp.PauseChannel);
                    break;
                case RequestOp.SeekChannel:
                    SeekPlaybackChannel(request.Row, (byte)request.Timeline.Value, request.At);
                    break;
                case RequestOp.ReverseChannel:
                    ReversePlaybackChannel(request.Row, (byte)request.Timeline.Value, request.At != 0);
                    break;
                case RequestOp.Speed:
                    SpeedPlayback(request.Row, request.Timeline, request.At);
                    break;
                case RequestOp.SpeedChannel:
                    SpeedPlaybackChannel(request.Row, (byte)request.Timeline.Value, request.At);
                    break;
                default:
                    break;
            }
        }
        _pending.Clear();
    }

    bool ValidRow(Row row) => row.Value >= 0 && row.Value < _rowCapacity;

    bool ValidTimeline(TimelineId timeline)
    {
        return timeline.Value != 0 && timeline.Value <= _duration.Length && _duration[timeline.Value - 1] > 0;
    }

    void StartPlayback(Row row, TimelineId timeline, int phase)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline))
        {
            _invalidPending++;
            return;
        }
        if (FindPlain(row.Value, timeline.Value) >= 0 || FindSolo(row.Value, timeline.Value) >= 0) return;
        var index = timeline.Value - 1;
        var channel = _channelTimeline[index];
        if (channel != 0)
        {
            var plainChannel = FindPlainChannel(row.Value, channel);
            if (plainChannel >= 0)
            {
                AddClosing(row.Value, _pbTimeline[plainChannel]);
                RemovePlainAt(plainChannel);
            }
            var soloChannel = FindSoloChannel(row.Value, channel);
            if (soloChannel >= 0)
            {
                AddClosing(row.Value, _soloTimeline[soloChannel]);
                RemoveSoloAt(soloChannel);
            }
        }
        var duration = _duration[index];
        var looping = _library.Loops(index);
        if (looping)
        {
            EnsurePlaybackCapacity(_playbackCount + 1);
            var plainPos = _playbackCount;
            _pbRow[plainPos] = row.Value;
            _pbTimeline[plainPos] = timeline.Value;
            _pbPhase[plainPos] = (ushort)(phase & 65535);
            _playbackCount++;
            InsertPlain(plainPos);
            _sortedDirty = true;
        }
        else
        {
            var tick = 0;
            if (duration > 0)
            {
                var at = phase;
                if (at < 0) at = 0;
                if (at >= duration) at = duration - 1;
                tick = at;
            }
            EnsureSoloCapacity(_soloCount + 1);
            var soloPos = _soloCount;
            _soloRow[soloPos] = row.Value;
            _soloTimeline[soloPos] = timeline.Value;
            _soloTick[soloPos] = (ushort)tick;
            _soloChannel[soloPos] = channel;
            _soloFlags[soloPos] = 0;
            _soloRate[soloPos] = 0;
            _soloCarry[soloPos] = 0;
            _soloCount++;
            InsertSolo(soloPos);
        }
    }

    void StartPlaybackBulk(Row row, TimelineId timeline, int phase, Span<ulong> seen, ref int bulkRow)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline))
        {
            _invalidPending++;
            return;
        }
        var rowValue = row.Value;
        var timelineValue = timeline.Value;
        // No pre-existing playbacks (cold world) and the batch is row-sorted,
        // so duplicates are tracked with a rolling per-row bitmask.
        if (rowValue != bulkRow)
        {
            bulkRow = rowValue;
            seen.Clear();
        }
        var timelineIndex = timelineValue - 1;
        var bit = 1UL << (timelineIndex & 63);
        var word = timelineIndex >> 6;
        if ((seen[word] & bit) != 0) return;
        seen[word] |= bit;
        var index = timelineIndex;
        var duration = _duration[index];
        var looping = _library.Loops(index);
        if (looping)
        {
            EnsurePlaybackCapacity(_playbackCount + 1);
            var plainPos = _playbackCount;
            _pbRow[plainPos] = rowValue;
            _pbTimeline[plainPos] = timelineValue;
            _pbPhase[plainPos] = (ushort)(phase & 65535);
            _playbackCount++;
            _sortedDirty = true;
        }
        else
        {
            var tick = 0;
            if (duration > 0)
            {
                var at = phase;
                if (at < 0) at = 0;
                if (at >= duration) at = duration - 1;
                tick = at;
            }
            EnsureSoloCapacity(_soloCount + 1);
            var soloPos = _soloCount;
            _soloRow[soloPos] = rowValue;
            _soloTimeline[soloPos] = timelineValue;
            _soloTick[soloPos] = (ushort)tick;
            _soloChannel[soloPos] = 0;
            _soloFlags[soloPos] = 0;
            _soloRate[soloPos] = 0;
            _soloCarry[soloPos] = 0;
            _soloCount++;
        }
    }

    void StopPlayback(Row row, TimelineId timeline)
    {
        var plain = FindPlain(row.Value, timeline.Value);
        if (plain >= 0)
        {
            AddClosing(row.Value, timeline.Value);
            RemovePlainAt(plain);
        }
        var solo = FindSolo(row.Value, timeline.Value);
        if (solo >= 0)
        {
            AddClosing(row.Value, timeline.Value);
            RemoveSoloAt(solo);
        }
    }

    void StopPlaybackChannel(Row row, byte channel)
    {
        if (channel == 0) return;
        var plain = FindPlainChannel(row.Value, channel);
        if (plain >= 0)
        {
            AddClosing(row.Value, _pbTimeline[plain]);
            RemovePlainAt(plain);
        }
        var solo = FindSoloChannel(row.Value, channel);
        if (solo >= 0)
        {
            AddClosing(row.Value, _soloTimeline[solo]);
            RemoveSoloAt(solo);
        }
    }

    void StopPlaybackAll(Row row)
    {
        var i = 0;
        while (i < _playbackCount)
        {
            if (_pbRow[i] == row.Value)
            {
                AddClosing(row.Value, _pbTimeline[i]);
                RemovePlainAt(i);
                continue;
            }
            i++;
        }
        var s = 0;
        while (s < _soloCount)
        {
            if (_soloRow[s] == row.Value)
            {
                AddClosing(row.Value, _soloTimeline[s]);
                RemoveSoloAt(s);
                continue;
            }
            s++;
        }
    }

    void PausePlayback(Row row, TimelineId timeline, bool pause)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline)) { _invalidPending++; return; }
        if (PromoteToSolo(row.Value, timeline.Value) >= 0)
        {
            var solo = FindSolo(row.Value, timeline.Value);
            if (solo >= 0)
            {
                if (pause) _soloFlags[solo] |= SoloPaused;
                else _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFE);
            }
        }
    }

    void PausePlaybackChannel(Row row, byte channel, bool pause)
    {
        if (!ValidRow(row) || channel == 0) { _invalidPending++; return; }
        if (PromoteChannelToSolo(row.Value, channel) >= 0)
        {
            var solo = FindSoloChannel(row.Value, channel);
            if (solo >= 0)
            {
                if (pause) _soloFlags[solo] |= SoloPaused;
                else _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFE);
            }
        }
    }

    void SeekPlayback(Row row, TimelineId timeline, int at)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline)) { _invalidPending++; return; }
        if (PromoteToSolo(row.Value, timeline.Value) >= 0)
        {
            var solo = FindSolo(row.Value, timeline.Value);
            if (solo >= 0) SeekSolo(solo, at);
        }
        else if (FindPlain(row.Value, timeline.Value) >= 0)
        {
            _pbPhase[FindPlain(row.Value, timeline.Value)] = (ushort)(at & 65535);
            _sortedDirty = true;
        }
    }

    void SeekPlaybackChannel(Row row, byte channel, int at)
    {
        if (!ValidRow(row) || channel == 0) { _invalidPending++; return; }
        var solo = FindSoloChannel(row.Value, channel);
        if (solo >= 0) { SeekSolo(solo, at); return; }
        var plain = FindPlainChannel(row.Value, channel);
        if (plain >= 0) PromoteChannelToSolo(row.Value, channel);
        var promoted = FindSoloChannel(row.Value, channel);
        if (promoted >= 0) SeekSolo(promoted, at);
    }

    void SeekSolo(int solo, int at)
    {
        var timeline = _soloTimeline[solo];
        var duration = _duration[timeline - 1];
        var looping = _library.Loops(timeline - 1);
        var tick = 0;
        if (duration > 0)
        {
            if (looping)
            {
                var t = at % duration;
                if (t < 0) t += duration;
                tick = t;
            }
            else
            {
                var t = at;
                if (t < 0) t = 0;
                if (t >= duration) t = duration - 1;
                tick = t;
            }
        }
        _soloTick[solo] = (ushort)tick;
        _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFB);
    }

    void ReversePlayback(Row row, TimelineId timeline, bool reverse)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline)) { _invalidPending++; return; }
        if (PromoteToSolo(row.Value, timeline.Value) >= 0)
        {
            var solo = FindSolo(row.Value, timeline.Value);
            if (solo >= 0)
            {
                if (reverse) _soloFlags[solo] |= SoloReverse;
                else _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFD);
                _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFB);
            }
        }
    }

    void ReversePlaybackChannel(Row row, byte channel, bool reverse)
    {
        if (!ValidRow(row) || channel == 0) { _invalidPending++; return; }
        if (PromoteChannelToSolo(row.Value, channel) >= 0)
        {
            var solo = FindSoloChannel(row.Value, channel);
            if (solo >= 0)
            {
                if (reverse) _soloFlags[solo] |= SoloReverse;
                else _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFD);
                _soloFlags[solo] = (byte)(_soloFlags[solo] & 0xFB);
            }
        }
    }

    void SpeedPlayback(Row row, TimelineId timeline, int sixteenths)
    {
        if (!ValidRow(row) || !ValidTimeline(timeline)) { _invalidPending++; return; }
        if (PromoteToSolo(row.Value, timeline.Value) >= 0)
        {
            var solo = FindSolo(row.Value, timeline.Value);
            if (solo >= 0) SetSoloRate(solo, sixteenths);
        }
    }

    void SpeedPlaybackChannel(Row row, byte channel, int sixteenths)
    {
        if (!ValidRow(row) || channel == 0) { _invalidPending++; return; }
        if (PromoteChannelToSolo(row.Value, channel) >= 0)
        {
            var solo = FindSoloChannel(row.Value, channel);
            if (solo >= 0) SetSoloRate(solo, sixteenths);
        }
    }

    void SetSoloRate(int solo, int sixteenths)
    {
        byte rate;
        if (sixteenths == 16) rate = 0;
        else if (sixteenths > 255) rate = 255;
        else if (sixteenths < 0) rate = 0;
        else rate = (byte)sixteenths;
        _soloRate[solo] = rate;
        if (rate == 0) _soloCarry[solo] = 0;
    }

    int PromoteToSolo(int row, ushort timeline)
    {
        var plain = FindPlain(row, timeline);
        if (plain < 0) return FindSolo(row, timeline);
        var duration = _duration[timeline - 1];
        var tick = duration > 0 ? Kernels.PhaseToTick(_pbPhase[plain], _frame, duration, 1) : 0;
        if (tick < 0) tick = 0;
        var channel = _channelTimeline[timeline - 1];
        RemovePlainAt(plain);
        EnsureSoloCapacity(_soloCount + 1);
        var promotedPos = _soloCount;
        _soloRow[promotedPos] = row;
        _soloTimeline[promotedPos] = timeline;
        _soloTick[promotedPos] = (ushort)tick;
        _soloChannel[promotedPos] = channel;
        _soloFlags[promotedPos] = 0;
        _soloRate[promotedPos] = 0;
        _soloCarry[promotedPos] = 0;
        _soloCount++;
        InsertSolo(promotedPos);
        return _soloCount - 1;
    }

    int PromoteChannelToSolo(int row, byte channel)
    {
        var plain = FindPlainChannel(row, channel);
        if (plain >= 0)
        {
            var timeline = _pbTimeline[plain];
            var duration = _duration[timeline - 1];
            var tick = duration > 0 ? Kernels.PhaseToTick(_pbPhase[plain], _frame, duration, 1) : 0;
            if (tick < 0) tick = 0;
            RemovePlainAt(plain);
            EnsureSoloCapacity(_soloCount + 1);
            var channelPromotedPos = _soloCount;
            _soloRow[channelPromotedPos] = row;
            _soloTimeline[channelPromotedPos] = timeline;
            _soloTick[channelPromotedPos] = (ushort)tick;
            _soloChannel[channelPromotedPos] = channel;
            _soloFlags[channelPromotedPos] = 0;
            _soloRate[channelPromotedPos] = 0;
            _soloCarry[channelPromotedPos] = 0;
            _soloCount++;
            InsertSolo(channelPromotedPos);
            return _soloCount - 1;
        }
        return FindSoloChannel(row, channel);
    }

    void EnsureIndexes()
    {
        if (!_indexesStale) return;
        _indexesStale = false;
        _plainTl.Clear();
        _plainCh.Clear();
        _soloTl.Clear();
        _soloCh.Clear();
        _plainTl.EnsureCapacity(_playbackCount);
        _soloTl.EnsureCapacity(_soloCount);
        var hasChannel = false;
        for (var i = 0; i < _channelTimeline.Length; i++)
        {
            if (_channelTimeline[i] != 0) { hasChannel = true; break; }
        }
        if (hasChannel)
        {
            _plainCh.EnsureCapacity(_playbackCount);
            _soloCh.EnsureCapacity(_soloCount);
        }
        for (var i = 0; i < _playbackCount; i++) InsertPlain(i);
        for (var i = 0; i < _soloCount; i++) InsertSolo(i);
    }

    int FindPlain(int row, ushort timeline)
    {
        if (_indexesStale) EnsureIndexes();
        return _plainTl.Find(PackTimeline(row, timeline));
    }

    int FindPlainChannel(int row, byte channel)
    {
        if (channel == 0) return -1;
        if (_indexesStale) EnsureIndexes();
        return _plainCh.Find(PackChannel(row, channel));
    }

    int FindSolo(int row, ushort timeline)
    {
        if (_indexesStale) EnsureIndexes();
        return _soloTl.Find(PackTimeline(row, timeline));
    }

    int FindSoloChannel(int row, byte channel)
    {
        if (channel == 0) return -1;
        if (_indexesStale) EnsureIndexes();
        return _soloCh.Find(PackChannel(row, channel));
    }

    void InsertPlain(int pos)
    {
        if (_indexesStale) EnsureIndexes();
        var row = _pbRow[pos];
        var timeline = _pbTimeline[pos];
        _plainTl.Insert(PackTimeline(row, timeline), pos);
        var channel = _channelTimeline[timeline - 1];
        if (channel != 0) _plainCh.Insert(PackChannel(row, channel), pos);
    }

    void InsertSolo(int pos)
    {
        if (_indexesStale) EnsureIndexes();
        var row = _soloRow[pos];
        var timeline = _soloTimeline[pos];
        _soloTl.Insert(PackTimeline(row, timeline), pos);
        var channel = _soloChannel[pos];
        if (channel != 0) _soloCh.Insert(PackChannel(row, channel), pos);
    }

    void RemovePlainTlKey(ulong key, int removedPos, bool sameAsSwapped)
    {
        if (sameAsSwapped)
        {
            _plainTl.Decrement(key);
            return;
        }
        if (!_plainTl.TryGet(key, out var pos, out var cnt)) return;
        if (pos != removedPos)
        {
            _plainTl.Decrement(key);
            return;
        }
        if (cnt <= 1)
        {
            _plainTl.Delete(key);
            return;
        }
        var first = -1;
        var total = 0;
        for (var i = 0; i < _playbackCount; i++)
        {
            if (PackTimeline(_pbRow[i], _pbTimeline[i]) != key) continue;
            if (first < 0) first = i;
            total++;
        }
        if (total == 0) _plainTl.Delete(key);
        else
        {
            _plainTl.Delete(key);
            _plainTl.Insert(key, first);
            _plainTl.SetCount(key, total);
        }
    }

    void RemovePlainChKey(ulong key, int removedPos, bool sameAsSwapped)
    {
        if (sameAsSwapped)
        {
            _plainCh.Decrement(key);
            return;
        }
        if (!_plainCh.TryGet(key, out var pos, out var cnt)) return;
        if (pos != removedPos)
        {
            _plainCh.Decrement(key);
            return;
        }
        if (cnt <= 1)
        {
            _plainCh.Delete(key);
            return;
        }
        var first = -1;
        var total = 0;
        for (var i = 0; i < _playbackCount; i++)
        {
            var timeline = _pbTimeline[i];
            var channel = _channelTimeline[timeline - 1];
            if (channel == 0 || PackChannel(_pbRow[i], channel) != key) continue;
            if (first < 0) first = i;
            total++;
        }
        if (total == 0) _plainCh.Delete(key);
        else
        {
            _plainCh.Delete(key);
            _plainCh.Insert(key, first);
            _plainCh.SetCount(key, total);
        }
    }

    void RemoveSoloTlKey(ulong key, int removedPos, bool sameAsSwapped)
    {
        if (sameAsSwapped)
        {
            _soloTl.Decrement(key);
            return;
        }
        if (!_soloTl.TryGet(key, out var pos, out var cnt)) return;
        if (pos != removedPos)
        {
            _soloTl.Decrement(key);
            return;
        }
        if (cnt <= 1)
        {
            _soloTl.Delete(key);
            return;
        }
        var first = -1;
        var total = 0;
        for (var i = 0; i < _soloCount; i++)
        {
            if (PackTimeline(_soloRow[i], _soloTimeline[i]) != key) continue;
            if (first < 0) first = i;
            total++;
        }
        if (total == 0) _soloTl.Delete(key);
        else
        {
            _soloTl.Delete(key);
            _soloTl.Insert(key, first);
            _soloTl.SetCount(key, total);
        }
    }

    void RemoveSoloChKey(ulong key, int removedPos, bool sameAsSwapped)
    {
        if (sameAsSwapped)
        {
            _soloCh.Decrement(key);
            return;
        }
        if (!_soloCh.TryGet(key, out var pos, out var cnt)) return;
        if (pos != removedPos)
        {
            _soloCh.Decrement(key);
            return;
        }
        if (cnt <= 1)
        {
            _soloCh.Delete(key);
            return;
        }
        var first = -1;
        var total = 0;
        for (var i = 0; i < _soloCount; i++)
        {
            var channel = _soloChannel[i];
            if (channel == 0 || PackChannel(_soloRow[i], channel) != key) continue;
            if (first < 0) first = i;
            total++;
        }
        if (total == 0) _soloCh.Delete(key);
        else
        {
            _soloCh.Delete(key);
            _soloCh.Insert(key, first);
            _soloCh.SetCount(key, total);
        }
    }

    void RemovePlainAt(int index)
    {
        var removedRow = _pbRow[index];
        var removedTimeline = _pbTimeline[index];
        var removedChannel = _channelTimeline[removedTimeline - 1];
        var removedTimelineKey = PackTimeline(removedRow, removedTimeline);
        var removedChannelKey = removedChannel != 0 ? PackChannel(removedRow, removedChannel) : 0;
        var hasRemovedChannel = removedChannel != 0;
        var last = --_playbackCount;
        var hasSwap = index != last;
        var swappedTimelineKey = 0ul;
        var swappedChannelKey = 0ul;
        var hasSwappedChannel = false;
        if (hasSwap)
        {
            var swappedRow = _pbRow[last];
            var swappedTimeline = _pbTimeline[last];
            swappedTimelineKey = PackTimeline(swappedRow, swappedTimeline);
            var swappedChannel = _channelTimeline[swappedTimeline - 1];
            hasSwappedChannel = swappedChannel != 0;
            if (hasSwappedChannel) swappedChannelKey = PackChannel(swappedRow, swappedChannel);
        }
        _pbRow[index] = _pbRow[last];
        _pbTimeline[index] = _pbTimeline[last];
        _pbPhase[index] = _pbPhase[last];
        _sortedDirty = true;
        RemovePlainTlKey(removedTimelineKey, index, hasSwap && removedTimelineKey == swappedTimelineKey);
        if (hasRemovedChannel) RemovePlainChKey(removedChannelKey, index, hasSwap && hasSwappedChannel && removedChannelKey == swappedChannelKey);
        if (hasSwap)
        {
            if (removedTimelineKey != swappedTimelineKey) _plainTl.SetPosIfFirst(swappedTimelineKey, last, index);
            if (hasSwappedChannel && (!hasRemovedChannel || removedChannelKey != swappedChannelKey)) _plainCh.SetPosIfFirst(swappedChannelKey, last, index);
        }
    }

    void RemoveSoloAt(int index)
    {
        var removedRow = _soloRow[index];
        var removedTimeline = _soloTimeline[index];
        var removedChannel = _soloChannel[index];
        var removedTimelineKey = PackTimeline(removedRow, removedTimeline);
        var removedChannelKey = removedChannel != 0 ? PackChannel(removedRow, removedChannel) : 0;
        var hasRemovedChannel = removedChannel != 0;
        var last = --_soloCount;
        var hasSwap = index != last;
        var swappedTimelineKey = 0ul;
        var swappedChannelKey = 0ul;
        var hasSwappedChannel = false;
        if (hasSwap)
        {
            var swappedRow = _soloRow[last];
            var swappedTimeline = _soloTimeline[last];
            swappedTimelineKey = PackTimeline(swappedRow, swappedTimeline);
            var swappedChannel = _soloChannel[last];
            hasSwappedChannel = swappedChannel != 0;
            if (hasSwappedChannel) swappedChannelKey = PackChannel(swappedRow, swappedChannel);
        }
        _soloRow[index] = _soloRow[last];
        _soloTimeline[index] = _soloTimeline[last];
        _soloTick[index] = _soloTick[last];
        _soloChannel[index] = _soloChannel[last];
        _soloFlags[index] = _soloFlags[last];
        _soloRate[index] = _soloRate[last];
        _soloCarry[index] = _soloCarry[last];
        RemoveSoloTlKey(removedTimelineKey, index, hasSwap && removedTimelineKey == swappedTimelineKey);
        if (hasRemovedChannel) RemoveSoloChKey(removedChannelKey, index, hasSwap && hasSwappedChannel && removedChannelKey == swappedChannelKey);
        if (hasSwap)
        {
            if (removedTimelineKey != swappedTimelineKey) _soloTl.SetPosIfFirst(swappedTimelineKey, last, index);
            if (hasSwappedChannel && (!hasRemovedChannel || removedChannelKey != swappedChannelKey)) _soloCh.SetPosIfFirst(swappedChannelKey, last, index);
        }
    }

    void EnsureSoloCapacity(int needed)
    {
        if (_soloRow.Length >= needed) return;
        var n = needed > _soloRow.Length * 2 ? needed : _soloRow.Length * 2;
        if (n < 64) n = 64;
        Array.Resize(ref _soloRow, n);
        Array.Resize(ref _soloTimeline, n);
        Array.Resize(ref _soloTick, n);
        Array.Resize(ref _soloChannel, n);
        Array.Resize(ref _soloFlags, n);
        Array.Resize(ref _soloRate, n);
        Array.Resize(ref _soloCarry, n);
    }

    void AddClosing(int row, ushort timeline)
    {
        if (_closingCount == _closing.Length)
        {
            var grown = new ClosingEntry[_closing.Length * 2];
            Array.Copy(_closing, grown, _closing.Length);
            _closing = grown;
        }
        _closing[_closingCount] = new ClosingEntry(new Row(row), new TimelineId(timeline));
        _closingCount++;
    }

    public int ClosingCount => _closingCount;

    public ReadOnlySpan<ClosingEntry> Closing => new ReadOnlySpan<ClosingEntry>(_closing, 0, _closingCount);

    public void ClearClosing() => _closingCount = 0;

    public bool IsPlaying(Row row, TimelineId timeline)
    {
        if (!ValidRow(row) || timeline.Value == 0) return false;
        return FindPlain(row.Value, timeline.Value) >= 0 || FindSolo(row.Value, timeline.Value) >= 0;
    }

    public bool TryGetPlayback(Row row, TimelineId timeline, out PlaybackInfo info)
    {
        info = default;
        if (!ValidRow(row) || timeline.Value == 0) return false;
        var plain = FindPlain(row.Value, timeline.Value);
        if (plain >= 0)
        {
            var duration = _duration[timeline.Value - 1];
            var tick = duration > 0 ? Kernels.PhaseToTick(_pbPhase[plain], _frame, duration, 1) : 0;
            if (tick < 0) tick = 0;
            info = new PlaybackInfo(timeline, tick, _channelTimeline[timeline.Value - 1], false, false, 0, 0);
            return true;
        }
        var solo = FindSolo(row.Value, timeline.Value);
        if (solo >= 0)
        {
            info = new PlaybackInfo(
                timeline,
                _soloTick[solo],
                _soloChannel[solo],
                (_soloFlags[solo] & SoloPaused) != 0,
                (_soloFlags[solo] & SoloReverse) != 0,
                _soloRate[solo],
                _soloCarry[solo]);
            return true;
        }
        return false;
    }

    public bool TryGetPlaybackByChannel(Row row, byte channel, out PlaybackInfo info)
    {
        info = default;
        if (!ValidRow(row) || channel == 0) return false;
        var plain = FindPlainChannel(row.Value, channel);
        if (plain >= 0)
        {
            var timeline = _pbTimeline[plain];
            var duration = _duration[timeline - 1];
            var tick = duration > 0 ? Kernels.PhaseToTick(_pbPhase[plain], _frame, duration, 1) : 0;
            if (tick < 0) tick = 0;
            info = new PlaybackInfo(new TimelineId(timeline), tick, channel, false, false, 0, 0);
            return true;
        }
        var solo = FindSoloChannel(row.Value, channel);
        if (solo >= 0)
        {
            info = new PlaybackInfo(
                new TimelineId(_soloTimeline[solo]),
                _soloTick[solo],
                channel,
                (_soloFlags[solo] & SoloPaused) != 0,
                (_soloFlags[solo] & SoloReverse) != 0,
                _soloRate[solo],
                _soloCarry[solo]);
            return true;
        }
        return false;
    }

    public void SetNext(TimelineId from, TimelineId next)
    {
        if (from.Value == 0 || from.Value > _nextTimeline.Length) throw new ArgumentOutOfRangeException(nameof(from));
        if (next.Value != 0 && (next.Value > _nextTimeline.Length || _duration[next.Value - 1] <= 0)) throw new ArgumentException("next names no timeline in the library", nameof(next));
        _nextTimeline[from.Value - 1] = next.Value;
    }

    public TimelineId NextOf(TimelineId timeline)
    {
        if (timeline.Value == 0 || timeline.Value > _nextTimeline.Length) return TimelineId.None;
        return new TimelineId(_nextTimeline[timeline.Value - 1]);
    }

    public void SetHold(TimelineId timeline, bool hold)
    {
        if (timeline.Value == 0 || timeline.Value > _holdTimeline.Length) throw new ArgumentOutOfRangeException(nameof(timeline));
        _holdTimeline[timeline.Value - 1] = hold;
    }

    public bool HoldOf(TimelineId timeline)
    {
        if (timeline.Value == 0 || timeline.Value > _holdTimeline.Length) return false;
        return _holdTimeline[timeline.Value - 1];
    }

    public void SetChannel(TimelineId timeline, byte channel)
    {
        if (timeline.Value == 0 || timeline.Value > _channelTimeline.Length) throw new ArgumentOutOfRangeException(nameof(timeline));
        _channelTimeline[timeline.Value - 1] = channel;
        _plainCh.Clear();
        for (var i = 0; i < _playbackCount; i++)
        {
            var playbackTimeline = _pbTimeline[i];
            var playbackChannel = _channelTimeline[playbackTimeline - 1];
            if (playbackChannel != 0) _plainCh.Insert(PackChannel(_pbRow[i], playbackChannel), i);
        }
    }

    public byte ChannelOf(TimelineId timeline)
    {
        if (timeline.Value == 0 || timeline.Value > _channelTimeline.Length) return 0;
        return _channelTimeline[timeline.Value - 1];
    }

    public ulong StateHash
    {
        get
        {
            var hash = 14695981039346656037ul;
            for (var i = 0; i < _applied.Length; i++)
                hash = (hash ^ (uint)FloatBits(_applied[i])) * 1099511628211ul;
            hash = (hash ^ (uint)_frame) * 1099511628211ul;
            hash = (hash ^ (uint)(_playbackCount + _soloCount)) * 1099511628211ul;
            return hash;
        }
    }

    public TlStateHash HashState()
    {
        var hash = TlStateHash.Begin();
        for (var i = 0; i < _applied.Length; i++) hash.FloatLane(_applied[i]);
        hash.Int(_frame);
        hash.Int(_playbackCount + _soloCount);
        return hash;
    }

    static int FloatBits(float value) => *(int*)&value;

    internal int StepSolo(int dir)
    {
        if (_indexesStale && _soloCount != 0) EnsureIndexes();
        var visits = 0;
        var i = 0;
        while (i < _soloCount)
        {
            var flags = _soloFlags[i];
            var paused = (flags & SoloPaused) != 0;
            var held = (flags & SoloHeld) != 0;
            var reverse = (flags & SoloReverse) != 0;
            var effectiveDir = reverse ? -dir : dir;
            var crossings = 1;
            if (!paused && !held)
            {
                var rate = _soloRate[i];
                if (rate == 0) crossings = 1;
                else
                {
                    var sum = _soloCarry[i] + rate;
                    crossings = sum >> 4;
                    _soloCarry[i] = (byte)(sum & 15);
                }
            }
            var removed = false;
            for (var c = 0; c < crossings; c++)
            {
                if (paused || held) break;
                if (!StepSoloOnce(i, effectiveDir)) { removed = true; break; }
                if (rowsSoloFlags(i, out var nextFlags) && (nextFlags & SoloHeld) != 0) break;
            }
            if (removed)
            {
                AddClosing(_soloRow[i], _soloTimeline[i]);
                RemoveSoloAt(i);
                continue;
            }
            AccumulateSoloAt(i, reverse ? LaneDirection.Backward : dir < 0 ? LaneDirection.Backward : LaneDirection.Forward);
            visits += paused || held ? 1 : crossings;
            i++;
        }
        return visits;
    }

    bool rowsSoloFlags(int index, out byte flags)
    {
        flags = _soloFlags[index];
        return true;
    }

    bool StepSoloOnce(int index, int effectiveDir)
    {
        var timeline = _soloTimeline[index];
        var duration = _duration[timeline - 1];
        if (duration <= 0)
        {
            _soloFlags[index] |= SoloHeld;
            return true;
        }
        var tick = _soloTick[index] + effectiveDir;
        if (tick >= 0 && tick < duration)
        {
            _soloTick[index] = (ushort)tick;
            return true;
        }
        var looping = _library.Loops(timeline - 1);
        if (looping)
        {
            _soloTick[index] = (ushort)(effectiveDir > 0 ? 0 : duration - 1);
            return true;
        }
        var next = _nextTimeline[timeline - 1];
        if (next != 0 && next <= _duration.Length && _duration[next - 1] > 0)
        {
            var row = _soloRow[index];
            var oldTimeline = timeline;
            var oldChannel = _soloChannel[index];
            var nextChannel = _channelTimeline[next - 1];
            var timelineChanged = next != oldTimeline;
            var channelChanged = nextChannel != oldChannel;
            _soloTimeline[index] = next;
            _soloChannel[index] = nextChannel;
            _soloTick[index] = (ushort)(effectiveDir > 0 ? 0 : _duration[next - 1] - 1);
            if (timelineChanged || channelChanged)
            {
                var oldTimelineKey = PackTimeline(row, oldTimeline);
                var nextTimelineKey = PackTimeline(row, next);
                if (timelineChanged) RemoveSoloTlKey(oldTimelineKey, index, false);
                if (channelChanged)
                {
                    if (oldChannel != 0) RemoveSoloChKey(PackChannel(row, oldChannel), index, false);
                    if (nextChannel != 0) _soloCh.Insert(PackChannel(row, nextChannel), index);
                }
                if (timelineChanged) _soloTl.Insert(nextTimelineKey, index);
            }
            return true;
        }
        if (_holdTimeline[timeline - 1])
        {
            _soloTick[index] = (ushort)(effectiveDir > 0 ? duration - 1 : 0);
            _soloFlags[index] |= SoloHeld;
            return true;
        }
        return false;
    }

    void AccumulateSoloAt(int index, LaneDirection laneDir)
    {
        var timeline = _soloTimeline[index];
        var row = _soloRow[index];
        var tick = _soloTick[index];
        var timelineIndex = timeline - 1;
        var baseStep = (int)_library.Timeline(timelineIndex).StepIndex;
        var stepCount = (int)_library.Timeline(timelineIndex).StepCount;
        for (var s = 0; s < stepCount; s++)
        {
            var globalStep = baseStep + s;
            var step = _library.Step(globalStep);
            if (tick < (int)step.WindowStart || tick >= (int)step.WindowEnd) continue;
            _acc[row] += _library.LaneValue(globalStep, laneDir, tick);
        }
    }

}
