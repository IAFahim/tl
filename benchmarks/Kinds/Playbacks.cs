using System.Runtime.InteropServices;

namespace Kinds;

[Flags]
public enum SlotFlags : byte
{
    None = 0,
    Reverse = 1,
    Paused = 2,
    Ended = 4,
    Stopping = 8,
    StartPending = 16,
    Played = 32,
    Began = 64,
    StopAged = 128,
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct Slot
{
    public ushort Timeline;
    public ushort Tick;
    public byte Channel;
    public SlotFlags Flags;
}

public enum RequestOp : byte { Start, Stop, Pause, Resume, Reverse }

public struct Request
{
    public int Entity;
    public RequestOp Op;
    public ushort Timeline;
    public ushort Tick;
    public byte Slot;
    public bool Reverse;
}

public sealed unsafe class Playbacks : IDisposable
{
    public readonly int Entities;
    public int Stride { get; private set; }
    public Slot* Slots { get; private set; }
    public readonly byte* Count;
    public readonly ulong* Playing;
    public readonly int Words;
    Request* _requests;
    int _requestCount;
    int _requestCapacity;

    public Playbacks(int entities, int inlineSlots = 4)
    {
        Entities = entities;
        Stride = inlineSlots;
        Words = (entities + 63) >> 6;
        Slots = (Slot*)Zeroed((nuint)entities * (nuint)Stride * 8);
        Count = (byte*)Zeroed((nuint)entities);
        Playing = (ulong*)Zeroed((nuint)Words * 8);
        _requestCapacity = 1024;
        _requests = (Request*)NativeMemory.Alloc((nuint)_requestCapacity * (nuint)sizeof(Request));
    }

    static void* Zeroed(nuint bytes)
    {
        var p = NativeMemory.AlignedAlloc(bytes == 0 ? 1 : bytes, 64);
        NativeMemory.Clear(p, bytes);
        return p;
    }

    public Slot* SlotOf(int entity, int slot) => Slots + (nuint)entity * (nuint)Stride + (nuint)slot;

    public void Start(int entity, ushort timeline, ushort tick = 0, bool reverse = false) => Enqueue(new Request { Entity = entity, Op = RequestOp.Start, Timeline = timeline, Tick = tick, Reverse = reverse });

    public void Stop(int entity, byte slot) => Enqueue(new Request { Entity = entity, Op = RequestOp.Stop, Slot = slot });

    public void Pause(int entity, byte slot) => Enqueue(new Request { Entity = entity, Op = RequestOp.Pause, Slot = slot });

    public void Resume(int entity, byte slot) => Enqueue(new Request { Entity = entity, Op = RequestOp.Resume, Slot = slot });

    public void Reverse(int entity, byte slot) => Enqueue(new Request { Entity = entity, Op = RequestOp.Reverse, Slot = slot });

    void Enqueue(Request request)
    {
        if (_requestCount == _requestCapacity)
        {
            _requestCapacity *= 2;
            _requests = (Request*)NativeMemory.Realloc(_requests, (nuint)_requestCapacity * (nuint)sizeof(Request));
        }
        _requests[_requestCount++] = request;
    }

    public int RequestCount => _requestCount;

    public Request* Requests => _requests;

    public void ClearRequests() => _requestCount = 0;

    public int AcquireSlot(int entity)
    {
        var count = Count[entity];
        for (var i = 0; i < count; i++)
            if (SlotOf(entity, i)->Timeline == 0) return i;
        if (count == Stride) Grow();
        Count[entity] = (byte)(count + 1);
        return count;
    }

    void Grow()
    {
        var stride = Stride * 2;
        var slots = (Slot*)Zeroed((nuint)Entities * (nuint)stride * 8);
        for (var e = 0; e < Entities; e++)
            Buffer.MemoryCopy(Slots + (nuint)e * (nuint)Stride, slots + (nuint)e * (nuint)stride, (long)stride * 8, (long)Stride * 8);
        NativeMemory.AlignedFree(Slots);
        Slots = slots;
        Stride = stride;
    }

    public void SetPlaying(int entity, bool value)
    {
        var bit = 1ul << (entity & 63);
        if (value) Playing[entity >> 6] |= bit;
        else Playing[entity >> 6] &= ~bit;
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(Slots);
        NativeMemory.AlignedFree(Count);
        NativeMemory.AlignedFree(Playing);
        NativeMemory.Free(_requests);
    }
}
