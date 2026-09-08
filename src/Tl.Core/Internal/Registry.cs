using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Generation;
using Tl.Internal;

namespace Tl;

// The unmanaged runtime timeline: ONE native block per timeline, explicitly
// owned by the registry and freed exactly once by Timeline.Destroy. The
// block is 16-byte aligned, never moves, and carries the whole runtime
// representation — every table plus the per-consumer dispatch slots — so
// ECS-style consumers can store/pass the block (through the ushort handle
// into the slot table below) and interpret regions/tracks/clips straight
// out of unmanaged memory.
//
// Layout (each table 16-aligned, offsets fixed at lowering time):
//
//   [NativeEntry header]  table directory + per-timeline facts + binds
//   [RegionStarts]        uint[RegionCount]
//   [RegionRows]          RegionRow[RegionCount]
//   [TrackRows]           TrackRow[TrackRowCount]
//   [ClipRows]            ClipRow[ClipRowCount]
//   [ClipEdges]           ClipEdge[ClipEdgeCount]
//   [PayloadMap]          ushort[PayloadMapCount]   (identity when count 0)
//   [TrackData]           TTrack[TrackCount]
//   [ClipData]            TClip[PayloadCount]
//   [WorkSlots]           WorkSlot[TrackRowCount]
//   [InlineBinds]         NativeBindSlot[InlineBindCapacity]
//
// Construction transiently uses managed locals (the authoring builder, the
// lowering lists/dictionaries); nothing managed is retained once Register
// publishes the entry. The retained representation is the block and nothing
// else. Bind slots beyond the inline capacity live in a separately freed
// native array grown by copy (see Internal/Binding.cs).
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeEntry
{
    public static readonly nint Signature = unchecked((nint)0x544C_344D_454DL); // "TL4MEM"

    public const int InlineBindCapacity = 4;

    public nint Stamp;          // Signature while live; 0 once destroyed
    public ushort Index;        // registry slot (set by Register)
    public nint ClosureId;      // owning Timeline<TTrack, TClip> identity

    public uint Duration;
    public int MaxActiveTracks;
    public int MaxActiveBlends;
    public int Loops;

    public int RegionCount;
    public int TrackRowCount;
    public int ClipRowCount;
    public int ClipEdgeCount;
    public int PayloadCount;
    public int PayloadMapCount;
    public int TrackCount;

    public uint* RegionStarts;
    public RegionRow* RegionRows;
    public TrackRow* TrackRows;
    public ClipRow* ClipRows;
    public ClipEdge* ClipEdges;
    public ushort* PayloadMap;
    public void* TrackData;
    public void* ClipData;
    public WorkSlot* WorkSlots;

    // The closure's automatic (input, result) bridge binder: re-runs the
    // reflection-based bind for a Type pair on first use. Distinct from
    // compile-time specialization; AOT consumers use the explicit Bind.
    public delegate*<Type, Type, ushort, void> Binder;

    public NativeBindSlot* InlineBinds;
    public int InlineBindCount;
    public NativeBindSlot* BindOverflow;
    public int BindOverflowCount;
    public int BindOverflowCapacity;
    public int BindGate;        // native spin gate for bind-table mutation
}

public static unsafe partial class Timeline
{
    public const ushort None = ushort.MaxValue;

    // The process-global index space: a native slot array mapping a never
    // reused ushort index to its NativeEntry. Destroy tombstones the slot
    // (null) and frees the block; the index is never handed out again.
    //
    // The static fields below are machine words (no GC objects); the slot
    // array itself is native memory. Growth copies into a new generation
    // and RETIRES (does not free) the old one: playback threads read the
    // published pointer without any lock, so a retired generation must stay
    // mapped forever. Total retention is bounded — geometric growth means
    // all retired generations together are smaller than the live one, and
    // indexes are capped at None — see docs/v0.4-unmanaged.md.
    private static nint s_slots;
    private static int s_capacity;
    private static int s_nextIndex;
    private static int s_gate;          // spin gate for registration growth

    // Code-identity counter for Timeline<TTrack, TClip> closures (one nint
    // per closed generic type, instantiated by consumer code — binding
    // metadata, not timeline state; see docs/v0.4-unmanaged.md).
    private static long s_closureIds;

    internal static nint NextClosureId() => (nint)Interlocked.Increment(ref s_closureIds);

    internal static ushort Register(NativeEntry* entry)
    {
        int next;
        do
        {
            next = Volatile.Read(ref s_nextIndex);
            if (next >= None)
                throw new InvalidOperationException("Timeline index capacity exceeded.");
        }
        while (Interlocked.CompareExchange(ref s_nextIndex, next + 1, next) != next);

        Acquire(ref s_gate);
        try
        {
            // Grow geometrically only when the claimed index does not fit;
            // indexes are bounded by None, so the doubling terminates.
            var capacity = Math.Max(s_capacity, 16);
            while (capacity <= next)
                capacity *= 2;
            if (capacity != s_capacity)
            {
                var grown = (nint*)NativeMemory.AllocZeroed((nuint)(capacity * sizeof(nint)));
                if (s_slots != 0)
                    Buffer.MemoryCopy((void*)s_slots, grown,
                        (nuint)(capacity * sizeof(nint)), (nuint)(s_capacity * sizeof(nint)));
                Volatile.Write(ref s_slots, (nint)grown);
                s_capacity = capacity;
            }

            entry->Index = (ushort)next;
            Volatile.Write(ref ((nint*)s_slots)[next], (nint)(void*)entry);
        }
        finally
        {
            Release(ref s_gate);
        }

        return (ushort)next;
    }

    internal static NativeEntry* Live(ushort index)
    {
        if (index == None)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
        var slots = (nint*)Volatile.Read(ref s_slots);
        if (slots == null || (uint)index >= (uint)s_capacity)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
        var entry = Volatile.Read(ref slots[index]);
        if (entry == 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
        return (NativeEntry*)entry;
    }

    public static bool IsValid(ushort index)
    {
        if (index == None)
            return false;
        var slots = (nint*)Volatile.Read(ref s_slots);
        return slots != null && (uint)index < (uint)s_capacity && Volatile.Read(ref slots[index]) != 0;
    }

    public static uint Duration(ushort index) => Live(index)->Duration;

    public static bool IsLooping(ushort index) => Live(index)->Loops != 0;

    // Destroys the timeline: tombstones the slot so no new call can observe
    // the entry, then frees the block and its bind-overflow array. Explicit
    // lifetime — the handle is dead afterwards and the index is never
    // reused.
    //
    // CONTRACT: Destroy (like Build/InMemory and Bind) is externally
    // synchronized with playback calls on the same index. A concurrent
    // Forward that already resolved the entry keeps reading freed memory
    // — the unmanaged equivalent of a use-after-free, which no managed
    // runtime state can paper over. Playback itself is read-only over an
    // immutable snapshot and safe to run from any number of threads.
    public static void Destroy(ushort index)
    {
        var entry = Live(index);

        Volatile.Write(ref ((nint*)s_slots)[index], 0);
        entry->Stamp = 0;
        if (entry->BindOverflow != null)
            NativeMemory.Free(entry->BindOverflow);
        NativeMemory.AlignedFree(entry);
    }

    private static void Acquire(ref int gate)
    {
        while (Interlocked.CompareExchange(ref gate, 1, 0) != 0)
            Thread.SpinWait(16);
    }

    private static void Release(ref int gate) => Volatile.Write(ref gate, 0);
}
