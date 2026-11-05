using System.Runtime.InteropServices;

namespace Kinds;

public enum ClipMode : byte { Span, PulseOnStart, PulseOnEnd }

public enum EventOp : byte { Remove, Insert, PulseStart, PulseEnd }

[StructLayout(LayoutKind.Sequential)]
public struct Clip
{
    public ushort Start;
    public ushort End;
    public byte Kind;
    public byte Feeder;
    public ClipMode Mode;
    public byte Priority;
    public float Amount;
    public float Scale;
    public int Hit;
    public int LaneBase;
}

public sealed class TimelineSpec
{
    public ushort Duration;
    public bool Loop;
    public bool Hold;
    public ushort Next;
    public byte Channel;
    public readonly List<Clip> Clips = new();

    public TimelineSpec Add(ushort start, ushort end, byte kind, ClipMode mode = ClipMode.Span, float amount = 1f, float scale = 1f, byte priority = 0, int hit = 0, byte feeder = 0)
    {
        Clips.Add(new Clip { Start = start, End = end, Kind = kind, Mode = mode, Amount = amount, Scale = scale, Priority = priority, Hit = hit, Feeder = feeder });
        return this;
    }
}

public sealed unsafe class Library : IDisposable
{
    public const byte LoopFlag = 1;
    public const byte HoldFlag = 2;

    public readonly int Count;
    public readonly ushort* Duration;
    public readonly byte* Flags;
    public readonly ushort* Next;
    public readonly byte* Channel;
    public readonly int* ClipOffset;
    public readonly int* ClipCount;
    public readonly ulong* KindMask;
    public readonly Clip* Clips;
    public readonly float* Lane;
    public readonly int* TickBase;
    public readonly byte* EventBits;
    public readonly int* EventOffset;
    public readonly uint* Events;
    public readonly int* WrapOffset;
    public readonly ushort* Wrap;
    readonly List<nint> _owned = new();

    public Library(IReadOnlyList<TimelineSpec> timelines)
    {
        Count = timelines.Count;
        var n = Count + 1;
        Duration = Alloc<ushort>(n);
        Flags = Alloc<byte>(n);
        Next = Alloc<ushort>(n);
        Channel = Alloc<byte>(n);
        ClipOffset = Alloc<int>(n + 1);
        ClipCount = Alloc<int>(n);
        KindMask = Alloc<ulong>(n);
        TickBase = Alloc<int>(n + 1);
        WrapOffset = Alloc<int>(n * 2 + 1);

        var clips = new List<Clip>();
        var lane = new List<float>();
        var ticks = 0;
        for (var id = 1; id <= Count; id++)
        {
            var spec = timelines[id - 1];
            Duration[id] = spec.Duration;
            Flags[id] = (byte)((spec.Loop ? LoopFlag : 0) | (spec.Hold ? HoldFlag : 0));
            Next[id] = spec.Next;
            Channel[id] = spec.Channel;
            ClipOffset[id] = clips.Count;
            ClipCount[id] = spec.Clips.Count;
            TickBase[id] = ticks;
            ticks += spec.Duration;
            foreach (var source in spec.Clips)
            {
                var clip = source;
                if (clip.End <= clip.Start || clip.End > spec.Duration) throw new ArgumentException($"timeline {id}: clip window [{clip.Start},{clip.End}) outside [0,{spec.Duration})");
                KindMask[id] |= 1ul << clip.Kind;
                clip.LaneBase = lane.Count;
                for (var t = clip.Start; t < clip.End; t++)
                {
                    lane.Add(clip.Amount * clip.Scale);
                    lane.Add(-clip.Amount * clip.Scale);
                }
                clips.Add(clip);
            }
        }
        ClipOffset[n] = clips.Count;
        TickBase[n] = ticks;
        Clips = Alloc<Clip>(clips.Count);
        for (var i = 0; i < clips.Count; i++) Clips[i] = clips[i];
        Lane = Alloc<float>(lane.Count);
        for (var i = 0; i < lane.Count; i++) Lane[i] = lane[i];

        EventBits = Alloc<byte>(ticks);
        EventOffset = Alloc<int>(ticks * 2 + 1);
        var events = new List<uint>();
        var wrap = new List<ushort>();
        for (var id = 1; id <= Count; id++)
        {
            var d = Duration[id];
            var first = Clips + ClipOffset[id];
            var count = ClipCount[id];
            for (var t = 0; t < d; t++)
                for (var dir = 0; dir < 2; dir++)
                {
                    EventOffset[(TickBase[id] + t) * 2 + dir] = events.Count;
                    var before = events.Count;
                    for (var c = 0; c < count; c++)
                    {
                        var clip = first[c];
                        if (clip.Mode != ClipMode.Span) continue;
                        var removed = dir == 0 ? clip.End == t : clip.Start == t + 1;
                        if (removed) events.Add(Pack(EventOp.Remove, c));
                    }
                    for (var c = 0; c < count; c++)
                    {
                        var clip = first[c];
                        if (clip.Mode != ClipMode.Span) continue;
                        var inserted = dir == 0 ? clip.Start == t : clip.End - 1 == t;
                        if (inserted) events.Add(Pack(EventOp.Insert, c));
                    }
                    for (var c = 0; c < count; c++)
                    {
                        var clip = first[c];
                        if (clip.Mode == ClipMode.PulseOnStart && clip.Start == t) events.Add(Pack(EventOp.PulseStart, c));
                        if (clip.Mode == ClipMode.PulseOnEnd && clip.End - 1 == t) events.Add(Pack(EventOp.PulseEnd, c));
                    }
                    if (events.Count != before) EventBits[TickBase[id] + t] |= (byte)(1 << dir);
                }
            for (var dir = 0; dir < 2; dir++)
            {
                WrapOffset[(id - 1) * 2 + dir] = wrap.Count;
                for (var c = 0; c < count; c++)
                {
                    var clip = first[c];
                    if (clip.Mode != ClipMode.Span) continue;
                    if (dir == 0 ? clip.End == d : clip.Start == 0) wrap.Add((ushort)c);
                }
            }
        }
        EventOffset[ticks * 2] = events.Count;
        WrapOffset[Count * 2] = wrap.Count;
        Events = Alloc<uint>(events.Count);
        for (var i = 0; i < events.Count; i++) Events[i] = events[i];
        Wrap = Alloc<ushort>(wrap.Count);
        for (var i = 0; i < wrap.Count; i++) Wrap[i] = wrap[i];
    }

    static uint Pack(EventOp op, int clip) => ((uint)op << 16) | (uint)clip;

    public bool Loops(ushort id) => (Flags[id] & LoopFlag) != 0;

    public bool Holds(ushort id) => (Flags[id] & HoldFlag) != 0;

    public Clip* ClipOf(ushort id, int step) => Clips + ClipOffset[id] + step;

    public float LaneAt(ushort id, int step, int tick, int dir)
    {
        var clip = Clips + ClipOffset[id] + step;
        return Lane[clip->LaneBase + (tick - clip->Start) * 2 + (dir > 0 ? 0 : 1)];
    }

    T* Alloc<T>(int count) where T : unmanaged
    {
        var bytes = (nuint)Math.Max(1, count) * (nuint)sizeof(T);
        var p = NativeMemory.AlignedAlloc(bytes, 64);
        NativeMemory.Clear(p, bytes);
        _owned.Add((nint)p);
        return (T*)p;
    }

    public void Dispose()
    {
        foreach (var p in _owned) NativeMemory.AlignedFree((void*)p);
        _owned.Clear();
    }
}
