using System;
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

public sealed class Requests5
{
    private Request5[] _items = new Request5[16];
    private int _count;

    public int Count => _count;

    public Request5 this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public void Add(Row row, int slot, RequestOp op, TimelineId timeline, ushort at)
    {
        if (_count == _items.Length)
        {
            var grown = new Request5[_items.Length * 2];
            Array.Copy(_items, grown, _items.Length);
            _items = grown;
        }
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

    public void Clear() => _count = 0;

    public void Sort() => StableRadixSort(new Span<Request5>(_items, 0, _count));

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
        var scratch = new Request5[items.Length];
        var counts = new int[65536];
        var source = items;
        var destination = new Span<Request5>(scratch, 0, scratch.Length);
        for (var pass = 0; pass < 4; pass++)
        {
            RadixPass(source, destination, counts, pass);
            var tmp = source;
            source = destination;
            destination = tmp;
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
