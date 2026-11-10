using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Tl;

[StructLayout(LayoutKind.Sequential)]
public struct Request5
{
    public Row Row;
    public int Slot;
    public RequestOp Op;
    public TimelineId Timeline;
    public ushort At;
}

[StructLayout(LayoutKind.Sequential)]
public struct RequestOutcome
{
    public int Applied;
    public int InvalidRow;
    public int InvalidTimeline;

    public int Total => Applied + InvalidRow + InvalidTimeline;
}

// The pending-request queue is a native (off-heap) buffer, mirroring the 4.0
// PlaybackTable/RequestQueue design: bulk setups enqueue millions of starts,
// and a managed backing array that doubles from 16 to N entries allocates
// ~2x the final size in large-object-heap arrays (zeroed, copied, and
// collected during setup). The native buffer allocates ~0 managed bytes on
// the bulk path; doubling reallocs are plain memcpy (no zeroing, no GC scan).
public sealed unsafe class Requests5
{
    private Request5* _items;
    private int _capacity;
    private int _count;
    // Incremental classification of the pending batch, maintained on Add so
    // ApplyPendingRequests does not need a full re-scan of a bulk batch.
    // The public indexer setter invalidates the flags (ClassifyDirty), and
    // Sort() re-validates the sorted flag (stable radix output is sorted).
    private bool _allStarts = true;
    private bool _sorted = true;
    private bool _classifyDirty;
    private int _prevRow;
    private int _prevSlot;

    public Requests5()
    {
        _capacity = 16;
        _items = (Request5*)NativeMemory.AlignedAlloc((nuint)_capacity * (nuint)sizeof(Request5), 8u);
    }

    ~Requests5()
    {
        if (_items != null) NativeMemory.AlignedFree(_items);
    }

    public int Count => _count;

    public Request5 this[int index]
    {
        get => _items[index];
        set
        {
            _items[index] = value;
            _classifyDirty = true;
        }
    }

    internal bool AllStarts => _allStarts;
    internal bool IsSorted => _sorted;
    internal bool ClassifyDirty => _classifyDirty;

    internal Span<Request5> AsSpan() => new Span<Request5>(_items, _count);

    public void Add(Row row, int slot, RequestOp op, TimelineId timeline, ushort at)
    {
        if (_count == _capacity) Grow();
        if (op != RequestOp.Start) _allStarts = false;
        if (_count > 0)
        {
            var prevRow = _prevRow;
            var prevSlot = _prevSlot;
            if (row.Value < prevRow || (row.Value == prevRow && slot < prevSlot)) _sorted = false;
        }
        _prevRow = row.Value;
        _prevSlot = slot;
        _items[_count] = new Request5
        {
            Row = row,
            Slot = slot,
            Op = op,
            Timeline = timeline,
            At = at,
        };
        _count++;
    }

    void Grow()
    {
        var next = _capacity * 2;
        var grown = (Request5*)NativeMemory.AlignedAlloc((nuint)next * (nuint)sizeof(Request5), 8u);
        if (_count > 0)
            Buffer.MemoryCopy(_items, grown, (nuint)_count * (nuint)sizeof(Request5), (nuint)_count * (nuint)sizeof(Request5));
        NativeMemory.AlignedFree(_items);
        _items = grown;
        _capacity = next;
    }

    public void Clear()
    {
        _count = 0;
        _allStarts = true;
        _sorted = true;
        _classifyDirty = false;
    }

    public void Sort()
    {
        StableRadixSort(AsSpan());
        // Stable radix output is exactly (row, slot)-sorted, so the batch is
        // sorted afterwards regardless of the incremental flag's prior value.
        _sorted = true;
        _classifyDirty = false;
    }

    public RequestOutcome Apply(int rowCapacity, Func<TimelineId, bool>? timelineValid, Action<Request5>? sink)
    {
        Sort();
        int applied = 0;
        int invalidRow = 0;
        int invalidTimeline = 0;
        for (var i = 0; i < _count; i++)
        {
            var item = _items[i];
            if (item.Row.Value < 0 || item.Row.Value >= rowCapacity)
            {
                invalidRow++;
                continue;
            }
            if (item.Timeline.IsNone || (timelineValid is not null && !timelineValid(item.Timeline)))
            {
                invalidTimeline++;
                continue;
            }
            applied++;
            sink?.Invoke(item);
        }
        return new RequestOutcome
        {
            Applied = applied,
            InvalidRow = invalidRow,
            InvalidTimeline = invalidTimeline,
        };
    }

    public static void StableRadixSort(Span<Request5> items)
    {
        if (items.Length <= 1)
            return;
        // Sorted-bulk fast path: bulk setups enqueue in (row, slot) order; the
        // stable radix sort would be a no-op permutation, so skip the scratch
        // allocation and all four passes. The check is the exact radix
        // order (row asc, then slot asc, stable); any inversion falls back
        // to the full sort below.
        var sorted = true;
        var prevRow = items[0].Row.Value;
        var prevSlot = items[0].Slot;
        for (var i = 1; i < items.Length; i++)
        {
            var row = items[i].Row.Value;
            if (row < prevRow || (row == prevRow && items[i].Slot < prevSlot))
            {
                sorted = false;
                break;
            }
            prevRow = row;
            prevSlot = items[i].Slot;
        }
        if (sorted)
            return;
        var scratch = (Request5*)NativeMemory.AlignedAlloc((nuint)items.Length * (nuint)sizeof(Request5), 8u);
        var counts = ArrayPool<int>.Shared.Rent(65536);
        try
        {
            var source = items;
            var destination = new Span<Request5>(scratch, items.Length);
            for (var pass = 0; pass < 4; pass++)
            {
                RadixPass(source, destination, counts, pass);
                var tmp = source;
                source = destination;
                destination = tmp;
            }
        }
        finally
        {
            NativeMemory.AlignedFree(scratch);
            ArrayPool<int>.Shared.Return(counts);
        }
    }

    private static void RadixPass(Span<Request5> source, Span<Request5> destination, int[] counts, int pass)
    {
        ClearCounts(counts);
        for (var i = 0; i < source.Length; i++)
            counts[Digit(source[i], pass)]++;
        var offset = 0;
        for (var bucket = 0; bucket < counts.Length; bucket++)
        {
            var bucketCount = counts[bucket];
            counts[bucket] = offset;
            offset += bucketCount;
        }
        for (var i = 0; i < source.Length; i++)
        {
            var digit = Digit(source[i], pass);
            destination[counts[digit]++] = source[i];
        }
    }

    private static int Digit(Request5 item, int pass)
    {
        var key = pass < 2
            ? (uint)item.Slot ^ 0x80000000u
            : (uint)item.Row.Value ^ 0x80000000u;
        return (int)((key >> (pass % 2 * 16)) & 0xFFFFu);
    }

    private static void ClearCounts(int[] counts)
    {
        Array.Clear(counts, 0, counts.Length);
    }
}
