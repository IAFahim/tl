using System.Runtime.InteropServices;

namespace Tl;

[Flags]
public enum PlaybackFlags : byte
{
    None = 0,
    Reverse = 1 << 0,
    Paused = 1 << 1,
    StartPending = 1 << 2,
    Ended = 1 << 3,
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct Playback
{
    public ushort Timeline;
    public ushort Tick;
    public byte Channel;
    public PlaybackFlags Flags;
    public ushort Reserved;
}

public readonly struct EntityRef
{
    public EntityRef(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public readonly int Index;
    public readonly int Version;
}

public readonly unsafe struct PlaybackView
{
    internal PlaybackView(Playback* slots, int* counts, int entities, int slotsPerEntity)
    {
        Slots = slots;
        Counts = counts;
        EntityCount = entities;
        SlotsPerEntity = slotsPerEntity;
    }
    public readonly Playback* Slots;
    public readonly int* Counts;
    public readonly int EntityCount;
    public readonly int SlotsPerEntity;

    public int CountOf(int entity) => Counts[entity];
    public Playback* SlotOf(int entity, int slot) => Slots + (nuint)entity * (nuint)SlotsPerEntity + (nuint)slot;
}

public sealed unsafe class PlaybackTable : IDisposable
{
    readonly Playback* _slots;
    readonly int* _counts;
    readonly int _entities;
    readonly int _slotsPerEntity;
    readonly RequestQueue* _consumer;
    readonly RequestQueue* _pending;
    readonly ClosingList* _closing;
    bool _disposed;

    public PlaybackTable(int entityCapacity, int slotsPerEntity = 4)
    {
        if (entityCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(entityCapacity));
        if (slotsPerEntity <= 0 || slotsPerEntity > 64) throw new ArgumentOutOfRangeException(nameof(slotsPerEntity));
        _entities = entityCapacity;
        _slotsPerEntity = slotsPerEntity;
        var slots = NativeMemory.AlignedAlloc((nuint)entityCapacity * (nuint)slotsPerEntity * 8u, 8u);
        var counts = NativeMemory.AlignedAlloc((nuint)entityCapacity * 4u, 8u);
        NativeMemory.Clear(slots, (nuint)entityCapacity * (nuint)slotsPerEntity * 8u);
        NativeMemory.Clear(counts, (nuint)entityCapacity * 4u);
        _slots = (Playback*)slots;
        _counts = (int*)counts;
        _consumer = RequestQueue.Create();
        _pending = RequestQueue.Create();
        _closing = ClosingList.Create();
    }

    public int EntityCapacity => _entities;
    public int SlotsPerEntity => _slotsPerEntity;
    public PlaybackView View => new(_slots, _counts, _entities, _slotsPerEntity);
    public RequestQueue* Pending => _pending;
    public RequestQueue* Consumer => _consumer;
    public ClosingList* Closing => _closing;

    public int CountOf(int entity) => _counts[entity];

    public bool IsPlaying(int entity, ushort timeline)
    {
        var count = _counts[entity];
        var slots = _slots + (nuint)entity * (nuint)_slotsPerEntity;
        for (var i = 0; i < count; i++) if (slots[i].Timeline == timeline) return true;
        return false;
    }

    internal bool TryStartImmediate(int entity, ushort timeline, ushort at, byte channel, out bool replaced)
    {
        replaced = false;
        if ((uint)entity >= (uint)_entities || timeline == 0) return false;
        var count = _counts[entity];
        var slots = _slots + (nuint)entity * (nuint)_slotsPerEntity;
        if (channel != 0)
            for (var i = 0; i < count; i++)
                if (slots[i].Channel == channel)
                {
                    CloseSlot(entity, i);
                    count = _counts[entity];
                    replaced = true;
                    break;
                }
        for (var i = 0; i < count; i++) if (slots[i].Timeline == timeline) return false;
        if (count >= _slotsPerEntity) return false;
        slots[count] = new Playback { Timeline = timeline, Tick = at, Channel = channel, Flags = at > 0 ? PlaybackFlags.StartPending : PlaybackFlags.None };
        _counts[entity] = count + 1;
        return true;
    }

    internal void CloseSlot(int entity, int slot)
    {
        var count = _counts[entity];
        var slots = _slots + (nuint)entity * (nuint)_slotsPerEntity;
        ClosingList.Add(_closing, entity, slot, in slots[slot]);
        for (var i = slot; i < count - 1; i++) slots[i] = slots[i + 1];
        slots[count - 1] = default;
        _counts[entity] = count - 1;
    }
    internal void RemoveSlot(int entity, int slot)
    {
        var count = _counts[entity];
        var slots = _slots + (nuint)entity * (nuint)_slotsPerEntity;
        for (var i = slot; i < count - 1; i++) slots[i] = slots[i + 1];
        slots[count - 1] = default;
        _counts[entity] = count - 1;
    }

    internal Playback* SlotsOf(int entity) => _slots + (nuint)entity * (nuint)_slotsPerEntity;
    internal int* Counts => _counts;

    public bool Find(int entity, byte channel, out Playback playback)
    {
        var count = _counts[entity];
        var slots = _slots + (nuint)entity * (nuint)_slotsPerEntity;
        for (var i = 0; i < count; i++) if (slots[i].Channel == channel && channel != 0) { playback = slots[i]; return true; }
        playback = default;
        return false;
    }

    public void Start(int entity, ushort timeline, ushort at = 0) => RequestQueue.Add(_pending, entity, -1, timeline, at, RequestOp.Start);
    public void Stop(int entity, ushort timeline) => RequestQueue.Add(_pending, entity, -1, timeline, 0, RequestOp.Stop);
    public void StopChannel(int entity, byte channel) => RequestQueue.Add(_pending, entity, -1, channel, 0, RequestOp.StopChannel);
    public void StopAll(int entity) => RequestQueue.Add(_pending, entity, -1, 0, 0, RequestOp.StopAll);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RequestQueue.Free(_pending);
        RequestQueue.Free(_consumer);
        ClosingList.Free(_closing);
        NativeMemory.AlignedFree(_slots);
        NativeMemory.AlignedFree(_counts);
        GC.SuppressFinalize(this);
    }
}
