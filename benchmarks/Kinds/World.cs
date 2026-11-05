using System.Numerics;
using System.Runtime.InteropServices;

namespace Kinds;

public unsafe struct Columns
{
    public float* MotionX;
    public float* MotionY;
    public float* Fx;
    public float* WalkStat;
    public float* JumpPower;
    public int* Health;
    public int* Stamina;
}

public interface IKind<T> where T : unmanaged
{
    static abstract bool Each { get; }
    static abstract unsafe T Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns);
    static abstract void Combine(ref T total, in T next);
    static abstract void Apply(in T total, int entity, in Columns columns);
}

public struct GamePulse
{
    public int Target;
    public int Value;
}

public sealed unsafe class World : IDisposable
{
    public readonly Library Library;
    public readonly Playbacks Playbacks;
    public readonly KindTable?[] Tables;
    public readonly int Entities;
    readonly int* _refOffset;
    readonly byte* _refCount;
    readonly byte* _refKind;
    readonly int* _refRow;
    GamePulse* _pulses;
    int _pulseCount;
    int _pulseCapacity;
    public readonly byte PulseKind;
    public long DroppedPulses;

    public World(Library library, int entities, Func<int, ulong> reachable, IReadOnlyDictionary<byte, (int inline, int valueBytes)> kinds, byte pulseKind, int inlineSlots = 4)
    {
        Library = library;
        Entities = entities;
        Playbacks = new Playbacks(entities, inlineSlots);
        PulseKind = pulseKind;
        Tables = new KindTable?[64];
        var masks = new ulong[entities];
        var members = new List<int>[64];
        var refs = 0;
        for (var e = 0; e < entities; e++)
        {
            masks[e] = reachable(e);
            refs += BitOperations.PopCount(masks[e]);
            for (var k = 0; k < 64; k++)
                if ((masks[e] & (1ul << k)) != 0) (members[k] ??= new List<int>()).Add(e);
        }
        _refOffset = (int*)NativeMemory.Alloc((nuint)entities * 4);
        _refCount = (byte*)NativeMemory.Alloc((nuint)Math.Max(1, entities));
        _refKind = (byte*)NativeMemory.Alloc((nuint)Math.Max(1, refs));
        _refRow = (int*)NativeMemory.Alloc((nuint)Math.Max(1, refs) * 4);
        var rows = new int[64];
        var at = 0;
        for (var e = 0; e < entities; e++)
        {
            _refOffset[e] = at;
            _refCount[e] = (byte)BitOperations.PopCount(masks[e]);
            for (var k = 0; k < 64; k++)
                if ((masks[e] & (1ul << k)) != 0)
                {
                    _refKind[at] = (byte)k;
                    _refRow[at] = rows[k]++;
                    at++;
                }
        }
        for (var k = 0; k < 64; k++)
        {
            if (members[k] == null) continue;
            var shape = kinds[(byte)k];
            Tables[k] = new KindTable((byte)k, members[k].ToArray(), shape.inline, shape.valueBytes);
        }
        _pulseCapacity = 1024;
        _pulses = (GamePulse*)NativeMemory.Alloc((nuint)_pulseCapacity * (nuint)sizeof(GamePulse));
    }

    public long KindBytesPerEntity(int entity)
    {
        long bytes = 0;
        for (var r = 0; r < _refCount[entity]; r++) bytes += Tables[_refKind[_refOffset[entity] + r]]!.StorageBytesPerRow;
        return bytes;
    }

    public int RowOf(int entity, byte kind)
    {
        var offset = _refOffset[entity];
        var count = _refCount[entity];
        for (var r = 0; r < count; r++)
            if (_refKind[offset + r] == kind) return _refRow[offset + r];
        return -1;
    }

    public void Send(int target, int value)
    {
        if (_pulseCount == _pulseCapacity)
        {
            _pulseCapacity *= 2;
            _pulses = (GamePulse*)NativeMemory.Realloc(_pulses, (nuint)_pulseCapacity * (nuint)sizeof(GamePulse));
        }
        _pulses[_pulseCount++] = new GamePulse { Target = target, Value = value };
    }

    public void Tick(int stepDir)
    {
        ApplyRequests();
        var p = Playbacks;
        for (var w = 0; w < p.Words; w++)
        {
            var bits = p.Playing[w];
            while (bits != 0)
            {
                var e = (w << 6) + BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
                TickEntity(e, stepDir);
            }
        }
        MergePulses();
    }

    void ApplyRequests()
    {
        var p = Playbacks;
        var requests = p.Requests;
        for (var r = 0; r < p.RequestCount; r++)
        {
            var q = requests[r];
            var e = q.Entity;
            switch (q.Op)
            {
                case RequestOp.Start:
                {
                    var channel = Library.Channel[q.Timeline];
                    if (channel != 0)
                        for (var i = 0; i < p.Count[e]; i++)
                        {
                            var s = p.SlotOf(e, i);
                            if (s->Timeline != 0 && s->Channel == channel && (s->Flags & SlotFlags.Stopping) == 0) StopSlot(e, i, s);
                        }
                    var slot = p.AcquireSlot(e);
                    *p.SlotOf(e, slot) = new Slot { Timeline = q.Timeline, Tick = q.Tick, Channel = channel, Flags = SlotFlags.StartPending | (q.Reverse ? SlotFlags.Reverse : 0) };
                    p.SetPlaying(e, true);
                    break;
                }
                case RequestOp.Stop:
                {
                    if (q.Slot >= p.Count[e]) break;
                    var s = p.SlotOf(e, q.Slot);
                    if (s->Timeline != 0 && (s->Flags & SlotFlags.Stopping) == 0) StopSlot(e, q.Slot, s);
                    break;
                }
                case RequestOp.Pause:
                {
                    if (q.Slot >= p.Count[e]) break;
                    var s = p.SlotOf(e, q.Slot);
                    if (s->Timeline != 0 && (s->Flags & SlotFlags.Stopping) == 0) s->Flags |= SlotFlags.Paused;
                    break;
                }
                case RequestOp.Resume:
                {
                    if (q.Slot >= p.Count[e]) break;
                    var s = p.SlotOf(e, q.Slot);
                    if (s->Timeline != 0 && (s->Flags & SlotFlags.Stopping) == 0 && (s->Flags & SlotFlags.Paused) != 0)
                    {
                        s->Flags &= ~SlotFlags.Paused;
                        p.SetPlaying(e, true);
                    }
                    break;
                }
                case RequestOp.Reverse:
                {
                    if (q.Slot >= p.Count[e]) break;
                    var s = p.SlotOf(e, q.Slot);
                    if (s->Timeline != 0 && (s->Flags & SlotFlags.Stopping) == 0) s->Flags ^= SlotFlags.Reverse;
                    break;
                }
            }
        }
        p.ClearRequests();
    }

    void StopSlot(int e, int i, Slot* s)
    {
        if ((s->Flags & (SlotFlags.StartPending | SlotFlags.Ended)) == 0)
        {
            var id = s->Timeline;
            var first = Library.Clips + Library.ClipOffset[id];
            var count = Library.ClipCount[id];
            int t = s->Tick;
            for (var c = 0; c < count; c++)
            {
                var clip = first + c;
                if (t < clip->Start || t >= clip->End) continue;
                if (clip->Mode == ClipMode.Span) Tables[clip->Kind]!.RemoveSpan(RowOf(e, clip->Kind), (byte)i, id, (ushort)c);
                else if (clip->Mode == ClipMode.PulseOnEnd && t < clip->End - 1) AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Pulse | EntryFlags.Interrupted);
            }
        }
        s->Flags = (s->Flags & (SlotFlags.Reverse | SlotFlags.Paused | SlotFlags.Ended | SlotFlags.StartPending)) | SlotFlags.Stopping;
        Playbacks.SetPlaying(e, true);
    }

    void TickEntity(int e, int stepDir)
    {
        var p = Playbacks;
        var live = false;
        var count = p.Count[e];
        for (var i = 0; i < count; i++)
        {
            var s = p.SlotOf(e, i);
            if (s->Timeline == 0) continue;
            s->Flags &= ~(SlotFlags.Played | SlotFlags.Began);
            if ((s->Flags & SlotFlags.Stopping) != 0)
            {
                if ((s->Flags & SlotFlags.StopAged) != 0) *s = default;
                else
                {
                    s->Flags |= SlotFlags.StopAged;
                    live = true;
                }
                continue;
            }
            if ((s->Flags & SlotFlags.StartPending) != 0)
            {
                Begin(e, i, s, stepDir);
                live = true;
                continue;
            }
            if ((s->Flags & (SlotFlags.Paused | SlotFlags.Ended)) != 0) continue;
            var dir = (s->Flags & SlotFlags.Reverse) != 0 ? -stepDir : stepDir;
            var id = s->Timeline;
            int duration = Library.Duration[id];
            var t = s->Tick + dir;
            if ((uint)t < (uint)duration)
            {
                s->Tick = (ushort)t;
                if ((Library.EventBits[Library.TickBase[id] + t] & (dir > 0 ? 1 : 2)) != 0) ApplyEvents(e, i, id, t, dir);
                s->Flags |= SlotFlags.Played;
                live = true;
            }
            else if (Library.Loops(id))
            {
                var d = dir > 0 ? 0 : 1;
                var from = Library.WrapOffset[(id - 1) * 2 + d];
                var to = Library.WrapOffset[(id - 1) * 2 + d + 1];
                for (var w = from; w < to; w++)
                {
                    var c = Library.Wrap[w];
                    var clip = Library.ClipOf(id, c);
                    Tables[clip->Kind]!.RemoveSpan(RowOf(e, clip->Kind), (byte)i, id, c);
                }
                t = dir > 0 ? 0 : duration - 1;
                s->Tick = (ushort)t;
                if ((Library.EventBits[Library.TickBase[id] + t] & (dir > 0 ? 1 : 2)) != 0) ApplyEvents(e, i, id, t, dir);
                s->Flags |= SlotFlags.Played;
                live = true;
            }
            else
            {
                RemoveActiveSpans(e, i, s);
                var next = Library.Next[id];
                if (next != 0)
                {
                    s->Timeline = next;
                    s->Channel = Library.Channel[next];
                    s->Tick = (ushort)(dir > 0 ? 0 : Library.Duration[next] - 1);
                    Begin(e, i, s, stepDir);
                    live = true;
                }
                else if (Library.Holds(id)) s->Flags |= SlotFlags.Ended;
                else *s = default;
            }
        }
        if (!live) p.SetPlaying(e, false);
    }

    void Begin(int e, int i, Slot* s, int stepDir)
    {
        var dir = (s->Flags & SlotFlags.Reverse) != 0 ? -stepDir : stepDir;
        var id = s->Timeline;
        var first = Library.Clips + Library.ClipOffset[id];
        var count = Library.ClipCount[id];
        int t = s->Tick;
        for (var c = 0; c < count; c++)
        {
            var clip = first + c;
            if (t < clip->Start || t >= clip->End) continue;
            switch (clip->Mode)
            {
                case ClipMode.Span:
                    AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Span | ((dir > 0 ? t != clip->Start : t != clip->End - 1) ? EntryFlags.Synthetic : 0));
                    break;
                case ClipMode.PulseOnStart:
                    AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Pulse | (t != clip->Start ? EntryFlags.Synthetic : 0));
                    break;
                case ClipMode.PulseOnEnd:
                    if (t == clip->End - 1) AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Pulse);
                    break;
            }
        }
        s->Flags = (s->Flags & ~SlotFlags.StartPending) | SlotFlags.Played | SlotFlags.Began;
    }

    void RemoveActiveSpans(int e, int i, Slot* s)
    {
        var id = s->Timeline;
        var first = Library.Clips + Library.ClipOffset[id];
        var count = Library.ClipCount[id];
        int t = s->Tick;
        for (var c = 0; c < count; c++)
        {
            var clip = first + c;
            if (clip->Mode == ClipMode.Span && t >= clip->Start && t < clip->End) Tables[clip->Kind]!.RemoveSpan(RowOf(e, clip->Kind), (byte)i, id, (ushort)c);
        }
    }

    void ApplyEvents(int e, int i, ushort id, int t, int dir)
    {
        var index = (Library.TickBase[id] + t) * 2 + (dir > 0 ? 0 : 1);
        var from = Library.EventOffset[index];
        var to = Library.EventOffset[index + 1];
        for (var k = from; k < to; k++)
        {
            var packed = Library.Events[k];
            var op = (EventOp)(packed >> 16);
            var c = (int)(packed & 0xffff);
            var clip = Library.ClipOf(id, c);
            switch (op)
            {
                case EventOp.Remove:
                    Tables[clip->Kind]!.RemoveSpan(RowOf(e, clip->Kind), (byte)i, id, (ushort)c);
                    break;
                case EventOp.Insert:
                    AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Span);
                    break;
                default:
                    AppendTimeline(e, clip->Kind, i, id, c, clip->Feeder, EntryFlags.Pulse);
                    break;
            }
        }
    }

    void AppendTimeline(int e, byte kind, int slot, ushort id, int step, byte feeder, EntryFlags flags)
    {
        var entry = Tables[kind]!.Append(RowOf(e, kind));
        entry->Timeline = id;
        entry->Step = (ushort)step;
        entry->Slot = (byte)slot;
        entry->Feeder = feeder;
        entry->Flags = flags;
        entry->Reserved = 0;
    }

    void MergePulses()
    {
        var table = Tables[PulseKind];
        for (var k = 0; k < _pulseCount; k++)
        {
            var pulse = _pulses[k];
            var row = table == null ? -1 : RowOf(pulse.Target, PulseKind);
            if (row < 0)
            {
                DroppedPulses++;
                continue;
            }
            var entry = table!.Append(row);
            *entry = new Entry { Flags = EntryFlags.Pulse };
            *(int*)((byte*)entry + 8) = pulse.Value;
        }
        _pulseCount = 0;
    }

    public void RunKind<K, T>(KindTable? table, in Columns columns, int stepDir) where K : struct, IKind<T> where T : unmanaged
    {
        if (table == null) return;
        var p = Playbacks;
        var lib = Library;
        var stride = (nuint)table.Stride;
        for (var w = 0; w < table.Words; w++)
        {
            var bits = table.Active[w] & table.Enabled[w];
            while (bits != 0)
            {
                var row = (w << 6) + BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
                var e = table.Entity[row];
                var length = table.Length[row];
                var data = table.Data(row);
                var has = false;
                var pulses = false;
                T total = default;
                for (var j = 0; j < length; j++)
                {
                    var entry = (Entry*)(data + (nuint)j * stride);
                    T value;
                    if (entry->Timeline == 0)
                    {
                        value = *(T*)((byte*)entry + 8);
                        pulses = true;
                    }
                    else
                    {
                        var slot = p.SlotOf(e, entry->Slot);
                        if ((entry->Flags & EntryFlags.Pulse) != 0) pulses = true;
                        else if ((slot->Flags & SlotFlags.Paused) != 0) continue;
                        var dir = (slot->Flags & SlotFlags.Reverse) != 0 ? -stepDir : stepDir;
                        value = K.Evaluate(lib, entry, slot, dir, e, columns);
                    }
                    if (K.Each) K.Apply(value, e, columns);
                    else if (has) K.Combine(ref total, value);
                    else
                    {
                        total = value;
                        has = true;
                    }
                }
                if (has) K.Apply(total, e, columns);
                if (pulses) table.RemovePulses(row);
            }
        }
    }

    public void Dispose()
    {
        foreach (var t in Tables) t?.Dispose();
        Playbacks.Dispose();
        NativeMemory.Free(_refOffset);
        NativeMemory.Free(_refCount);
        NativeMemory.Free(_refKind);
        NativeMemory.Free(_refRow);
        NativeMemory.Free(_pulses);
    }
}
