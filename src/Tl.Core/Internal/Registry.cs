using System.Runtime.CompilerServices;
using Tl.Generation;

namespace Tl;

public static unsafe partial class Timeline
{
    public const ushort None = ushort.MaxValue;

    internal sealed class Entry
    {
        public ushort Index { get; set; }
        public required uint[] RegionStarts { get; init; }
        public required RegionRow[] RegionRows { get; init; }
        public required TrackRow[] TrackRows { get; init; }
        public required ClipRow[] ClipRows { get; init; }
        public required ClipEdge[] ClipEdges { get; init; }
        public required object Payload { get; init; }
        public required uint Duration { get; init; }
        public required int MaxActiveTracks { get; init; }
        public required int MaxActiveBlends { get; init; }
        public required bool Loops { get; init; }
        public required Action<Type, Entry> Binder { get; init; }

        public void Bind(Type data)
        {
            Binder(data, this);
        }
    }

    private static readonly object s_gate = new();
    private static Entry?[] s_slots = [];
    private static int s_nextIndex;

    internal static ushort Register(Entry entry)
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextIndex);
            if (next >= None)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref s_nextIndex, next + 1, next) != next);

        entry.Index = (ushort)next;

        lock (s_gate)
        {
            var current = s_slots;
            var nextSlots = new Entry?[Math.Max(next + 1, current.Length == 0 ? 16 : current.Length * 2)];
            current.CopyTo(nextSlots, 0);
            nextSlots[next] = entry;
            Volatile.Write(ref s_slots, nextSlots);
        }

        return (ushort)next;
    }

    internal static Entry Live(ushort index)
    {
        if (index == None)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
        var slots = Volatile.Read(ref s_slots);
        if ((uint)index >= (uint)slots.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
        return Volatile.Read(ref slots[index])
            ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
    }

    public static bool IsValid(ushort index)
    {
        if (index == None)
            return false;
        var slots = Volatile.Read(ref s_slots);
        return (uint)index < (uint)slots.Length && Volatile.Read(ref slots[index]) != null;
    }

    public static uint Duration(ushort index) => Live(index).Duration;

    public static bool IsLooping(ushort index) => Live(index).Loops;

    public static void Destroy(ushort index)
    {
        _ = Live(index);

        lock (s_gate)
        {
            var current = s_slots;
            if ((uint)index >= (uint)current.Length || current[index] == null)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
            var nextSlots = new Entry?[current.Length];
            current.CopyTo(nextSlots, 0);
            nextSlots[index] = null;
            Volatile.Write(ref s_slots, nextSlots);
        }
    }
}
