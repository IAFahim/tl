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
        if (_pending.Count == 0) return;
        _pending.Sort();
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
            _pbRow[_playbackCount] = row.Value;
            _pbTimeline[_playbackCount] = timeline.Value;
            _pbPhase[_playbackCount] = (ushort)(phase & 65535);
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
            _soloRow[_soloCount] = row.Value;
            _soloTimeline[_soloCount] = timeline.Value;
            _soloTick[_soloCount] = (ushort)tick;
            _soloChannel[_soloCount] = channel;
            _soloFlags[_soloCount] = 0;
            _soloRate[_soloCount] = 0;
            _soloCarry[_soloCount] = 0;
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
        _soloRow[_soloCount] = row;
        _soloTimeline[_soloCount] = timeline;
        _soloTick[_soloCount] = (ushort)tick;
        _soloChannel[_soloCount] = channel;
        _soloFlags[_soloCount] = 0;
        _soloRate[_soloCount] = 0;
        _soloCarry[_soloCount] = 0;
        _soloCount++;
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
            _soloRow[_soloCount] = row;
            _soloTimeline[_soloCount] = timeline;
            _soloTick[_soloCount] = (ushort)tick;
            _soloChannel[_soloCount] = channel;
            _soloFlags[_soloCount] = 0;
            _soloRate[_soloCount] = 0;
            _soloCarry[_soloCount] = 0;
            _soloCount++;
            return _soloCount - 1;
        }
        return FindSoloChannel(row, channel);
    }

    int FindPlain(int row, ushort timeline)
    {
        for (var i = 0; i < _playbackCount; i++)
            if (_pbRow[i] == row && _pbTimeline[i] == timeline) return i;
        return -1;
    }

    int FindPlainChannel(int row, byte channel)
    {
        if (channel == 0) return -1;
        for (var i = 0; i < _playbackCount; i++)
            if (_pbRow[i] == row && _channelTimeline[_pbTimeline[i] - 1] == channel) return i;
        return -1;
    }

    int FindSolo(int row, ushort timeline)
    {
        for (var i = 0; i < _soloCount; i++)
            if (_soloRow[i] == row && _soloTimeline[i] == timeline) return i;
        return -1;
    }

    int FindSoloChannel(int row, byte channel)
    {
        if (channel == 0) return -1;
        for (var i = 0; i < _soloCount; i++)
            if (_soloRow[i] == row && _soloChannel[i] == channel) return i;
        return -1;
    }

    void RemovePlainAt(int index)
    {
        var last = --_playbackCount;
        _pbRow[index] = _pbRow[last];
        _pbTimeline[index] = _pbTimeline[last];
        _pbPhase[index] = _pbPhase[last];
        _sortedDirty = true;
    }

    void RemoveSoloAt(int index)
    {
        var last = --_soloCount;
        _soloRow[index] = _soloRow[last];
        _soloTimeline[index] = _soloTimeline[last];
        _soloTick[index] = _soloTick[last];
        _soloChannel[index] = _soloChannel[last];
        _soloFlags[index] = _soloFlags[last];
        _soloRate[index] = _soloRate[last];
        _soloCarry[index] = _soloCarry[last];
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
            _soloTimeline[index] = next;
            _soloChannel[index] = _channelTimeline[next - 1];
            _soloTick[index] = (ushort)(effectiveDir > 0 ? 0 : _duration[next - 1] - 1);
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
