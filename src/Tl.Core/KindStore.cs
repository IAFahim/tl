using System.Runtime.InteropServices;

namespace Tl;

public readonly struct KindStats
{
    public KindStats(int rows, int maxLength, int overflows, long bytesPerRow)
    {
        Rows = rows;
        MaxLength = maxLength;
        Overflows = overflows;
        BytesPerRow = bytesPerRow;
    }

    public readonly int Rows;
    public readonly int MaxLength;
    public readonly int Overflows;
    public readonly long BytesPerRow;
}

public readonly unsafe struct KindPulses<T> where T : unmanaged
{
    readonly KindStore.PulseHandle _handle;

    internal KindPulses(KindStore.PulseHandle handle) { _handle = handle; }

    public void To(int entity, T value) => _handle.Send(entity, &value);
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PulseQueue
{
    public byte* Items;
    public int Count;
    public int Capacity;
    public int Stride;
}

public sealed unsafe class KindStore : IDisposable
{
    readonly TimelineWorld _world;
    readonly PlaybackTable _players;
    readonly string[]? _names;
    readonly ulong[]? _reach;
    ulong _generation;
    KindSectionView _sec;
    LibraryTimeline* _tls;
    int _timelineCount;
    int _kindCount;
    KindTable** _tables;
    PulseQueue* _queues;
    ulong* _mask;
    int* _offset;
    int* _rows;
    ulong* _closure;
    uint* _pairs;
    int* _pairStart;
    bool _disposed;
    string[] _kindNames = Array.Empty<string>();
    int[] _valueBytes = Array.Empty<int>();
    byte[] _accepts = Array.Empty<byte>();
    int _epoch;

    internal sealed class PulseHandle
    {
        readonly KindStore _store;
        readonly string _name;
        int _kind;
        int _epoch;

        internal PulseHandle(KindStore store, string name, int kind)
        {
            _store = store;
            _name = name;
            _kind = kind;
            _epoch = store._epoch;
        }

        internal void Send(int entity, void* value)
        {
            if (_epoch != _store._epoch)
            {
                _kind = _store.IndexOf(_name);
                _epoch = _store._epoch;
            }
            if (_kind < 0) _store.DroppedPulses++;
            else _store.Send(_kind, entity, value);
        }
    }

    public KindStore(TimelineWorld world, PlaybackTable players, string[]? kindNames = null, ulong[]? reach = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _players = players ?? throw new ArgumentNullException(nameof(players));
        if (kindNames != null && (kindNames.Length > 64 || reach == null || reach.Length != players.EntityCapacity))
            throw new ArgumentException("kindNames holds at most 64 names and reach needs one mask per entity.");
        _names = kindNames;
        _reach = reach;
        Build();
        players.AttachKinds(this);
    }

    public int KindCount => _kindCount;

    public long DroppedPulses { get; private set; }

    public string NameOf(int kind) => _kindNames[kind];

    public int IndexOf(string name) => Array.IndexOf(_kindNames, name);

    public KindStats GetStats(int kind)
    {
        var t = _tables[kind];
        return t == null ? default : new KindStats(t->Rows, t->MaxLength, t->Overflows, t->StorageBytesPerRow);
    }

    public KindPulses<T> Pulses<T>(string kind) where T : unmanaged
    {
        var k = IndexOf(kind);
        if (k < 0) throw new ArgumentException($"The loaded library has no kind '{kind}'.");
        if (_accepts[k] == (byte)KindEntries.Spans) throw new ArgumentException($"Kind '{kind}' accepts spans only; game code cannot send it pulses.");
        if (_valueBytes[k] != sizeof(T)) throw new ArgumentException($"Kind '{kind}' takes {_valueBytes[k]}-byte values; {typeof(T).Name} is {sizeof(T)}.");
        return new KindPulses<T>(new PulseHandle(this, kind, k));
    }

    internal void Send(int kind, int entity, void* value)
    {
        if ((uint)entity >= (uint)_players.EntityCapacity) throw new ArgumentOutOfRangeException(nameof(entity));
        var q = _queues + kind;
        if (q->Count == q->Capacity)
        {
            var capacity = q->Capacity == 0 ? 16 : q->Capacity * 2;
            var items = (byte*)NativeMemory.AlignedAlloc((nuint)capacity * (nuint)q->Stride, 8);
            if (q->Count > 0) new ReadOnlySpan<byte>(q->Items, q->Count * q->Stride).CopyTo(new Span<byte>(items, capacity * q->Stride));
            if (q->Items != null) NativeMemory.AlignedFree(q->Items);
            q->Items = items;
            q->Capacity = capacity;
        }
        var at = q->Items + (nuint)q->Count * (nuint)q->Stride;
        *(int*)at = entity;
        Buffer.MemoryCopy(value, at + 4, q->Stride - 4, _valueBytes[kind]);
        q->Count++;
    }

    internal KindTable* Table(int kind) => _tables[kind];

    internal int RowOf(int entity, int kind)
    {
        var mask = _mask[entity];
        if ((mask & (1ul << kind)) == 0) return -1;
        return _rows[_offset[entity] + Pop(mask & ((1ul << kind) - 1))];
    }

    internal void ClearPulses()
    {
        for (var k = 0; k < _kindCount; k++)
        {
            _queues[k].Count = 0;
            var t = _tables[k];
            if (t == null) continue;
            for (var r = 0; r < t->Rows; r++) t->RemovePulses(r);
        }
    }

    internal void ValidateStart(int entity, ushort timeline)
    {
        var missing = _closure[timeline - 1] & ~_mask[entity];
        if (missing == 0) return;
        throw new ArgumentException($"Entity {entity} cannot reach kind '{_kindNames[Tz(missing)]}' fed by timeline {timeline} or its next chain; add the kind to the entity's reach.");
    }

    internal void Stop(int entity, int slot)
    {
        var s = _players.SlotsOf(entity) + slot;
        _players.SetPlaying(entity, true);
        if (_kindCount == 0 || (s->Flags & (PlaybackFlags.StartPending | PlaybackFlags.Ended)) != 0) return;
        var id = s->Timeline;
        var tk = _sec.Timelines + (id - 1);
        if (tk->KindMask == 0) return;
        var windows = _sec.Windows(tk);
        int tick = s->Tick;
        for (var k = _pairStart[id - 1]; k < _pairStart[id]; k++)
        {
            var w = (int)(_pairs[k] >> 16);
            var f = (int)(_pairs[k] & 0xffff);
            if (tick < windows[w].Start || tick >= windows[w].End) continue;
            var feeder = _sec.Feeders + f;
            if (feeder->Trigger == 0) _tables[feeder->Kind]->RemoveSpan(RowOf(entity, feeder->Kind), (byte)slot, id, (ushort)w);
            else if (feeder->Trigger == 2 && tick < windows[w].End - 1) Append(entity, feeder, slot, id, w, KindEntryFlags.Pulse | KindEntryFlags.Interrupted);
        }
    }

    internal void Sync()
    {
        if (_generation != _world._generation) Rebuild();
    }

    internal void TickPass(bool forward)
    {
        Sync();
        var stepDir = forward ? 1 : -1;
        var playing = _players.Playing;
        for (var w = 0; w < _players.PlayingWords; w++)
        {
            var bits = playing[w];
            while (bits != 0)
            {
                var e = (w << 6) + Tz(bits);
                bits &= bits - 1;
                TickEntity(e, stepDir);
            }
        }
        MergePulses();
    }

    void TickEntity(int e, int stepDir)
    {
        var p = _players;
        var slots = p.SlotsOf(e);
        var count = p.CountOf(e);
        var live = false;
        for (var i = 0; i < count; i++)
        {
            var s = slots + i;
            var id = s->Timeline;
            if (id == 0) continue;
            var flags = s->Flags;
            if ((flags & ~PlaybackFlags.Reverse) == 0)
            {
                var dir = flags == 0 ? stepDir : -stepDir;
                var t = s->Tick + dir;
                if ((uint)t < _tls[id - 1].Duration)
                {
                    s->Tick = (ushort)t;
                    if (_kindCount != 0 && _sec.Timelines[id - 1].KindMask != 0 && (_sec.EventBitsAt(_sec.Timelines + (id - 1), t) & (dir > 0 ? 1 : 2)) != 0) ApplyEvents(e, i, id, t, dir);
                    live = true;
                    continue;
                }
            }
            live |= TickSlot(e, i, s, stepDir);
        }
        if (!live) p.SetPlaying(e, false);
    }

    bool TickSlot(int e, int i, Playback* s, int stepDir)
    {
        s->Flags &= ~PlaybackFlags.Began;
        if ((s->Flags & PlaybackFlags.Stopping) != 0)
        {
            if ((s->Flags & PlaybackFlags.StopAged) != 0)
            {
                *s = default;
                return false;
            }
            s->Flags |= PlaybackFlags.StopAged;
            return true;
        }
        if ((s->Flags & PlaybackFlags.StartPending) != 0)
        {
            Begin(e, i, s, stepDir);
            return true;
        }
        if ((s->Flags & PlaybackFlags.Paused) != 0) return true;
        if ((s->Flags & PlaybackFlags.Ended) != 0) return false;
        var dir = (s->Flags & PlaybackFlags.Reverse) != 0 ? -stepDir : stepDir;
        var id = s->Timeline;
        var tl = _tls + (id - 1);
        int duration = (int)tl->Duration;
        var t = s->Tick + dir;
        if ((uint)t < (uint)duration)
        {
            s->Tick = (ushort)t;
            Events(e, i, id, t, dir);
            return true;
        }
        if (tl->Loop != 0)
        {
            WrapRemove(e, i, id, dir);
            t = dir > 0 ? 0 : duration - 1;
            s->Tick = (ushort)t;
            Events(e, i, id, t, dir);
            return true;
        }
        RemoveActiveSpans(e, i, s);
        if (tl->Next != 0)
        {
            var next = _tls + (tl->Next - 1);
            s->Timeline = tl->Next;
            s->Channel = (byte)next->Channel;
            s->Tick = (ushort)(dir > 0 ? 0 : next->Duration - 1);
            Begin(e, i, s, stepDir);
            return true;
        }
        if (tl->Hold != 0)
        {
            s->Tick = (ushort)(duration - 1);
            s->Flags |= PlaybackFlags.Ended;
            return false;
        }
        *s = default;
        return false;
    }

    void Events(int e, int i, ushort id, int tick, int dir)
    {
        if (_kindCount != 0 && _sec.Timelines[id - 1].KindMask != 0 && (_sec.EventBitsAt(_sec.Timelines + (id - 1), tick) & (dir > 0 ? 1 : 2)) != 0) ApplyEvents(e, i, id, tick, dir);
    }

    void Begin(int e, int i, Playback* s, int stepDir)
    {
        s->Flags = (s->Flags & ~PlaybackFlags.StartPending) | PlaybackFlags.Began;
        var id = s->Timeline;
        if (_kindCount == 0 || _sec.Timelines[id - 1].KindMask == 0) return;
        var dir = (s->Flags & PlaybackFlags.Reverse) != 0 ? -stepDir : stepDir;
        var windows = _sec.Windows(_sec.Timelines + (id - 1));
        int tick = s->Tick;
        for (var k = _pairStart[id - 1]; k < _pairStart[id]; k++)
        {
            var w = (int)(_pairs[k] >> 16);
            var f = (int)(_pairs[k] & 0xffff);
            var window = windows[w];
            var feeder = _sec.Feeders + f;
            var inside = tick >= window.Start && tick < window.End;
            switch (feeder->Trigger)
            {
                case 0:
                    if (inside) Append(e, feeder, i, id, w, KindEntryFlags.Span | ((dir > 0 ? tick != window.Start : tick != window.End - 1) ? KindEntryFlags.Synthetic : 0));
                    break;
                case 1:
                    if (inside) Append(e, feeder, i, id, w, KindEntryFlags.Pulse | (tick != window.Start ? KindEntryFlags.Synthetic : 0));
                    break;
                case 2:
                    if (tick == window.End - 1) Append(e, feeder, i, id, w, KindEntryFlags.Pulse);
                    break;
                default:
                    if (tick == window.Start) Append(e, feeder, i, id, w, KindEntryFlags.Pulse);
                    break;
            }
        }
    }

    void RemoveActiveSpans(int e, int i, Playback* s)
    {
        var id = s->Timeline;
        if (_kindCount == 0 || _sec.Timelines[id - 1].KindMask == 0) return;
        var windows = _sec.Windows(_sec.Timelines + (id - 1));
        int tick = s->Tick;
        for (var k = _pairStart[id - 1]; k < _pairStart[id]; k++)
        {
            var w = (int)(_pairs[k] >> 16);
            var feeder = _sec.Feeders + (_pairs[k] & 0xffff);
            if (feeder->Trigger == 0 && tick >= windows[w].Start && tick < windows[w].End) _tables[feeder->Kind]->RemoveSpan(RowOf(e, feeder->Kind), (byte)i, id, (ushort)w);
        }
    }

    void WrapRemove(int e, int i, ushort id, int dir)
    {
        if (_kindCount == 0) return;
        var tk = _sec.Timelines + (id - 1);
        if (tk->KindMask == 0) return;
        var events = _sec.Events(tk);
        var from = dir > 0 ? tk->WrapForward : tk->WrapBackward;
        var to = from + (dir > 0 ? tk->WrapForwardCount : tk->WrapBackwardCount);
        for (var n = from; n < to; n++)
        {
            var feeder = _sec.Feeders + events[n].Feeder;
            _tables[feeder->Kind]->RemoveSpan(RowOf(e, feeder->Kind), (byte)i, id, events[n].Window);
        }
    }

    void ApplyEvents(int e, int i, ushort id, int tick, int dir)
    {
        var tk = _sec.Timelines + (id - 1);
        var events = _sec.Events(tk);
        var d = dir > 0 ? 0 : 1;
        var to = _sec.EventEnd(tk, tick, d);
        for (var n = _sec.EventStart(tk, tick, d); n < to; n++)
        {
            var ev = events[n];
            var feeder = _sec.Feeders + ev.Feeder;
            switch ((KindEventOp)ev.Op)
            {
                case KindEventOp.Remove:
                    _tables[feeder->Kind]->RemoveSpan(RowOf(e, feeder->Kind), (byte)i, id, ev.Window);
                    break;
                case KindEventOp.Insert:
                    Append(e, feeder, i, id, ev.Window, KindEntryFlags.Span);
                    break;
                default:
                    Append(e, feeder, i, id, ev.Window, KindEntryFlags.Pulse);
                    break;
            }
        }
    }

    void Append(int entity, LibraryFeeder* feeder, int slot, ushort timeline, int window, KindEntryFlags flags)
    {
        var entry = _tables[feeder->Kind]->Append(RowOf(entity, feeder->Kind));
        entry->Timeline = timeline;
        entry->Step = (ushort)window;
        entry->Slot = (byte)slot;
        entry->Feeder = feeder->Ordinal;
        entry->Flags = flags;
        entry->Reserved = 0;
    }

    void MergePulses()
    {
        for (var k = 0; k < _kindCount; k++)
        {
            var q = _queues + k;
            if (q->Count == 0) continue;
            var table = _tables[k];
            for (var n = 0; n < q->Count; n++)
            {
                var at = q->Items + (nuint)n * (nuint)q->Stride;
                var entity = *(int*)at;
                var row = table == null ? -1 : RowOf(entity, k);
                if (row < 0)
                {
                    DroppedPulses++;
                    continue;
                }
                var entry = table->Append(row);
                *entry = new KindEntry { Flags = KindEntryFlags.Pulse };
                Buffer.MemoryCopy(at + 4, (byte*)entry + 8, table->Stride - 8, _valueBytes[k]);
            }
            q->Count = 0;
        }
    }

    void Rebuild()
    {
        var pending = new List<(string Name, int Entity, byte[] Value)>();
        for (var k = 0; k < _kindCount; k++)
            for (var n = 0; n < _queues[k].Count; n++)
            {
                var at = _queues[k].Items + (nuint)n * (nuint)_queues[k].Stride;
                var value = new byte[_valueBytes[k]];
                new ReadOnlySpan<byte>(at + 4, value.Length).CopyTo(value);
                pending.Add((_kindNames[k], *(int*)at, value));
            }
        Build();
        foreach (var (name, entity, value) in pending)
        {
            var k = IndexOf(name);
            if (k < 0 || _valueBytes[k] != value.Length) DroppedPulses++;
            else fixed (byte* v = value) Send(k, entity, v);
        }
        var p = _players;
        for (var e = 0; e < p.EntityCapacity; e++)
        {
            var slots = p.SlotsOf(e);
            var count = p.CountOf(e);
            for (var i = 0; i < count; i++)
            {
                var s = slots + i;
                if (s->Timeline == 0) continue;
                if ((s->Flags & PlaybackFlags.Stopping) != 0) { *s = default; continue; }
                if (s->Timeline > _timelineCount) throw new ArgumentException($"Playback slot {i} of entity {e} names no timeline in the reloaded library.");
                p.SetPlaying(e, true);
                if ((s->Flags & (PlaybackFlags.StartPending | PlaybackFlags.Ended)) != 0 || _kindCount == 0) continue;
                ValidateStart(e, s->Timeline);
                var id = s->Timeline;
                var tk = _sec.Timelines + (id - 1);
                if (tk->KindMask == 0) continue;
                var windows = _sec.Windows(tk);
                int tick = s->Tick;
                for (var k = _pairStart[id - 1]; k < _pairStart[id]; k++)
                {
                    var w = (int)(_pairs[k] >> 16);
                    var feeder = _sec.Feeders + (_pairs[k] & 0xffff);
                    if (feeder->Trigger == 0 && tick >= windows[w].Start && tick < windows[w].End) Append(e, feeder, i, id, w, KindEntryFlags.Span | KindEntryFlags.Synthetic);
                }
            }
        }
    }

    void Build()
    {
        Release();
        var view = _world._view;
        _generation = _world._generation;
        _sec = view.Kinds;
        _tls = view.Timelines;
        _timelineCount = view.TimelineCount;
        _kindCount = _sec.IsEmpty ? 0 : _sec.KindCount;
        _epoch++;
        _kindNames = new string[_kindCount];
        _valueBytes = new int[_kindCount];
        _accepts = new byte[_kindCount];
        for (var k = 0; k < _kindCount; k++)
        {
            _kindNames[k] = _sec.NameOf(k);
            _valueBytes[k] = (int)_sec.Kinds[k].ValueBytes;
            _accepts[k] = _sec.Kinds[k].Accepts;
        }
        var entities = _players.EntityCapacity;
        for (var k = 0; k < _kindCount; k++)
            if (_sec.Kinds[k].GroupNameLength != 0) throw new NotSupportedException($"Kind '{_sec.NameOf(k)}' is in kind group '{_sec.GroupOf(k)}'; kind groups are not supported by the .NET runtime yet.");
        _mask = (ulong*)Zeroed((nuint)entities * 8);
        var all = _kindCount == 64 ? ~0ul : (1ul << _kindCount) - 1;
        if (_names == null) for (var e = 0; e < entities; e++) _mask[e] = all;
        else
        {
            var map = new int[_names.Length];
            for (var n = 0; n < map.Length; n++) map[n] = Array.IndexOf(_kindNames, _names[n]);
            for (var e = 0; e < entities; e++)
                for (var n = 0; n < map.Length; n++)
                    if (map[n] >= 0 && (_reach![e] & (1ul << n)) != 0) _mask[e] |= 1ul << map[n];
        }
        _offset = (int*)Zeroed((nuint)entities * 4);
        var refs = 0;
        for (var e = 0; e < entities; e++)
        {
            _offset[e] = refs;
            refs += Pop(_mask[e]);
        }
        _rows = (int*)Zeroed((nuint)Math.Max(1, refs) * 4);
        _tables = (KindTable**)Zeroed((nuint)Math.Max(1, _kindCount) * (nuint)sizeof(nint));
        _queues = (PulseQueue*)Zeroed((nuint)Math.Max(1, _kindCount) * (nuint)sizeof(PulseQueue));
        var members = new List<int>();
        for (var k = 0; k < _kindCount; k++)
        {
            members.Clear();
            for (var e = 0; e < entities; e++)
                if ((_mask[e] & (1ul << k)) != 0)
                {
                    _rows[_offset[e] + Pop(_mask[e] & ((1ul << k) - 1))] = members.Count;
                    members.Add(e);
                }
            ref var row = ref _sec.Kinds[k];
            _queues[k].Stride = 4 + (int)((row.ValueBytes + 3u) & ~3u);
            if (members.Count != 0) _tables[k] = KindTable.Create(k, members.ToArray(), (int)row.Capacity, (int)row.EntryBytes);
        }
        _closure = (ulong*)Zeroed((nuint)Math.Max(1, _timelineCount) * 8);
        _pairStart = (int*)Zeroed((nuint)(_timelineCount + 2) * 4);
        var pairs = new List<uint>();
        for (var t = 0; t < _timelineCount; t++)
        {
            var at = t;
            for (var hop = 0; hop <= _timelineCount; hop++)
            {
                if (_kindCount != 0) _closure[t] |= _sec.Timelines[at].KindMask;
                if (_tls[at].Next == 0) break;
                at = _tls[at].Next - 1;
            }
            _pairStart[t] = pairs.Count;
            if (_kindCount == 0 || _sec.Timelines[t].KindMask == 0) continue;
            var tk = _sec.Timelines + t;
            var events = _sec.Events(tk);
            var set = new SortedSet<uint>();
            for (var n = 0u; n < tk->EventCount; n++)
                if (events[n].Op != (byte)KindEventOp.Remove) set.Add(((uint)events[n].Window << 16) | events[n].Feeder);
            pairs.AddRange(set);
        }
        _pairStart[_timelineCount] = pairs.Count;
        _pairs = (uint*)Zeroed((nuint)Math.Max(1, pairs.Count) * 4);
        for (var n = 0; n < pairs.Count; n++) _pairs[n] = pairs[n];
    }

    static void* Zeroed(nuint bytes)
    {
        var p = NativeMemory.AlignedAlloc(bytes == 0 ? 1 : bytes, 64);
        NativeMemory.Clear(p, bytes);
        return p;
    }

    void Release()
    {
        if (_tables != null)
            for (var k = 0; k < _kindCount; k++) KindTable.Free(_tables[k]);
        if (_queues != null)
            for (var k = 0; k < _kindCount; k++) if (_queues[k].Items != null) NativeMemory.AlignedFree(_queues[k].Items);
        foreach (var block in new void*[] { _tables, _queues, _mask, _offset, _rows, _closure, _pairs, _pairStart })
            if (block != null) NativeMemory.AlignedFree(block);
        _tables = null;
        _queues = null;
        _mask = null;
        _offset = null;
        _rows = null;
        _closure = null;
        _pairs = null;
        _pairStart = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Release();
        _players.AttachKinds(null);
        GC.SuppressFinalize(this);
    }

    internal static int Pop(ulong v)
    {
#if NET
        return System.Numerics.BitOperations.PopCount(v);
#else
        v -= (v >> 1) & 0x5555555555555555ul;
        v = (v & 0x3333333333333333ul) + ((v >> 2) & 0x3333333333333333ul);
        return (int)((((v + (v >> 4)) & 0x0F0F0F0F0F0F0F0Ful) * 0x0101010101010101ul) >> 56);
#endif
    }

    internal static int Tz(ulong v)
    {
#if NET
        return System.Numerics.BitOperations.TrailingZeroCount(v);
#else
        return Pop((v & (ulong)-(long)v) - 1);
#endif
    }
}
