using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tl;

[StructLayout(LayoutKind.Sequential)]
public struct PlainPlayback
{
    public ushort Phase;
    public ushort Birth;
    public ushort Duration;
    public bool Looping;
    public Row Row;
}

[StructLayout(LayoutKind.Sequential)]
public struct SoloPlayback
{
    public ushort Tick;
    public ushort Duration;
    public byte Flags;
    public byte Rate;
    public byte Carry;
    public Row Row;

    public bool Looping
    {
        get => (Flags & 1) != 0;
        set => Flags = (byte)(value ? Flags | 1 : Flags & ~1);
    }
}

public sealed class Playbacks
{
    private readonly Dictionary<int, PlainCohort> _plain = new Dictionary<int, PlainCohort>();
    private readonly Dictionary<int, SoloCohort> _solo = new Dictionary<int, SoloCohort>();

    private sealed class PlainCohort
    {
        public ushort[] Phase = Array.Empty<ushort>();
        public int[] RowValues = Array.Empty<int>();
        public ushort[] Birth = Array.Empty<ushort>();
        public ushort[] Duration = Array.Empty<ushort>();
        public byte[] Flags = Array.Empty<byte>();
        public int Count;
    }

    private sealed class SoloCohort
    {
        public ushort[] Tick = Array.Empty<ushort>();
        public int[] RowValues = Array.Empty<int>();
        public ushort[] Duration = Array.Empty<ushort>();
        public byte[] Flags = Array.Empty<byte>();
        public byte[] Rate = Array.Empty<byte>();
        public byte[] Carry = Array.Empty<byte>();
        public int Count;
    }

    private static int CohortKey(TimelineId timeline, Direction direction) => (timeline.Value * 2) + (direction == Direction.Forward ? 0 : 1);

    private static PlainPlayback[] SortPlainBatch(ReadOnlySpan<PlainPlayback> entries)
    {
        var batch = entries.ToArray();
        var order = new int[batch.Length];
        for (var i = 0; i < order.Length; i++)
            order[i] = i;
        Array.Sort(order, (left, right) =>
        {
            var byPhase = batch[left].Phase.CompareTo(batch[right].Phase);
            if (byPhase != 0)
                return byPhase;
            var byRow = batch[left].Row.Value.CompareTo(batch[right].Row.Value);
            if (byRow != 0)
                return byRow;
            return left.CompareTo(right);
        });
        var sorted = new PlainPlayback[batch.Length];
        for (var i = 0; i < sorted.Length; i++)
            sorted[i] = batch[order[i]];
        return sorted;
    }

    private static SoloPlayback[] SortSoloBatch(ReadOnlySpan<SoloPlayback> entries)
    {
        var batch = entries.ToArray();
        var order = new int[batch.Length];
        for (var i = 0; i < order.Length; i++)
            order[i] = i;
        Array.Sort(order, (left, right) =>
        {
            var byTick = batch[left].Tick.CompareTo(batch[right].Tick);
            if (byTick != 0)
                return byTick;
            var byRow = batch[left].Row.Value.CompareTo(batch[right].Row.Value);
            if (byRow != 0)
                return byRow;
            return left.CompareTo(right);
        });
        var sorted = new SoloPlayback[batch.Length];
        for (var i = 0; i < sorted.Length; i++)
            sorted[i] = batch[order[i]];
        return sorted;
    }

    private static void WritePlain(ushort[] phase, int[] rowValues, ushort[] birth, ushort[] duration, byte[] flags, int index, PlainPlayback entry)
    {
        phase[index] = entry.Phase;
        rowValues[index] = entry.Row.Value;
        birth[index] = entry.Birth;
        duration[index] = entry.Duration;
        flags[index] = (byte)(entry.Looping ? 1 : 0);
    }

    private static void WriteSolo(ushort[] tick, int[] rowValues, ushort[] duration, byte[] flags, byte[] rate, byte[] carry, int index, SoloPlayback entry)
    {
        tick[index] = entry.Tick;
        rowValues[index] = entry.Row.Value;
        duration[index] = entry.Duration;
        flags[index] = entry.Flags;
        rate[index] = entry.Rate;
        carry[index] = entry.Carry;
    }

    public void AddCohort(TimelineId timeline, Direction direction, ReadOnlySpan<PlainPlayback> entries)
    {
        if (entries.IsEmpty)
            return;
        var sorted = SortPlainBatch(entries);
        var key = CohortKey(timeline, direction);
        if (!_plain.TryGetValue(key, out var cohort))
        {
            cohort = new PlainCohort
            {
                Phase = new ushort[sorted.Length],
                RowValues = new int[sorted.Length],
                Birth = new ushort[sorted.Length],
                Duration = new ushort[sorted.Length],
                Flags = new byte[sorted.Length],
                Count = sorted.Length,
            };
            for (var i = 0; i < sorted.Length; i++)
                WritePlain(cohort.Phase, cohort.RowValues, cohort.Birth, cohort.Duration, cohort.Flags, i, sorted[i]);
            _plain[key] = cohort;
            return;
        }
        var count = cohort.Count + sorted.Length;
        var phase = new ushort[count];
        var rowValues = new int[count];
        var birth = new ushort[count];
        var duration = new ushort[count];
        var flags = new byte[count];
        var left = 0;
        var right = 0;
        var slot = 0;
        while (left < cohort.Count && right < sorted.Length)
        {
            var takeOld = cohort.Phase[left] != sorted[right].Phase
                ? cohort.Phase[left] < sorted[right].Phase
                : cohort.RowValues[left] <= sorted[right].Row.Value;
            if (takeOld)
            {
                phase[slot] = cohort.Phase[left];
                rowValues[slot] = cohort.RowValues[left];
                birth[slot] = cohort.Birth[left];
                duration[slot] = cohort.Duration[left];
                flags[slot] = cohort.Flags[left];
                left++;
            }
            else
            {
                WritePlain(phase, rowValues, birth, duration, flags, slot, sorted[right]);
                right++;
            }
            slot++;
        }
        while (left < cohort.Count)
        {
            phase[slot] = cohort.Phase[left];
            rowValues[slot] = cohort.RowValues[left];
            birth[slot] = cohort.Birth[left];
            duration[slot] = cohort.Duration[left];
            flags[slot] = cohort.Flags[left];
            left++;
            slot++;
        }
        while (right < sorted.Length)
        {
            WritePlain(phase, rowValues, birth, duration, flags, slot, sorted[right]);
            right++;
            slot++;
        }
        cohort.Phase = phase;
        cohort.RowValues = rowValues;
        cohort.Birth = birth;
        cohort.Duration = duration;
        cohort.Flags = flags;
        cohort.Count = count;
    }

    public void AddSoloCohort(TimelineId timeline, Direction direction, ReadOnlySpan<SoloPlayback> entries)
    {
        if (entries.IsEmpty)
            return;
        var sorted = SortSoloBatch(entries);
        var key = CohortKey(timeline, direction);
        if (!_solo.TryGetValue(key, out var cohort))
        {
            cohort = new SoloCohort
            {
                Tick = new ushort[sorted.Length],
                RowValues = new int[sorted.Length],
                Duration = new ushort[sorted.Length],
                Flags = new byte[sorted.Length],
                Rate = new byte[sorted.Length],
                Carry = new byte[sorted.Length],
                Count = sorted.Length,
            };
            for (var i = 0; i < sorted.Length; i++)
                WriteSolo(cohort.Tick, cohort.RowValues, cohort.Duration, cohort.Flags, cohort.Rate, cohort.Carry, i, sorted[i]);
            _solo[key] = cohort;
            return;
        }
        var count = cohort.Count + sorted.Length;
        var tick = new ushort[count];
        var rowValues = new int[count];
        var duration = new ushort[count];
        var flags = new byte[count];
        var rate = new byte[count];
        var carry = new byte[count];
        var left = 0;
        var right = 0;
        var slot = 0;
        while (left < cohort.Count && right < sorted.Length)
        {
            var takeOld = cohort.Tick[left] != sorted[right].Tick
                ? cohort.Tick[left] < sorted[right].Tick
                : cohort.RowValues[left] <= sorted[right].Row.Value;
            if (takeOld)
            {
                tick[slot] = cohort.Tick[left];
                rowValues[slot] = cohort.RowValues[left];
                duration[slot] = cohort.Duration[left];
                flags[slot] = cohort.Flags[left];
                rate[slot] = cohort.Rate[left];
                carry[slot] = cohort.Carry[left];
                left++;
            }
            else
            {
                WriteSolo(tick, rowValues, duration, flags, rate, carry, slot, sorted[right]);
                right++;
            }
            slot++;
        }
        while (left < cohort.Count)
        {
            tick[slot] = cohort.Tick[left];
            rowValues[slot] = cohort.RowValues[left];
            duration[slot] = cohort.Duration[left];
            flags[slot] = cohort.Flags[left];
            rate[slot] = cohort.Rate[left];
            carry[slot] = cohort.Carry[left];
            left++;
            slot++;
        }
        while (right < sorted.Length)
        {
            WriteSolo(tick, rowValues, duration, flags, rate, carry, slot, sorted[right]);
            right++;
            slot++;
        }
        cohort.Tick = tick;
        cohort.RowValues = rowValues;
        cohort.Duration = duration;
        cohort.Flags = flags;
        cohort.Rate = rate;
        cohort.Carry = carry;
        cohort.Count = count;
    }

    public bool Remove(TimelineId timeline, Direction direction, Row row)
    {
        var key = CohortKey(timeline, direction);
        if (!_plain.TryGetValue(key, out var cohort))
            return false;
        for (var i = 0; i < cohort.Count; i++)
        {
            if (cohort.RowValues[i] != row.Value)
                continue;
            for (var j = i + 1; j < cohort.Count; j++)
            {
                cohort.Phase[j - 1] = cohort.Phase[j];
                cohort.RowValues[j - 1] = cohort.RowValues[j];
                cohort.Birth[j - 1] = cohort.Birth[j];
                cohort.Duration[j - 1] = cohort.Duration[j];
                cohort.Flags[j - 1] = cohort.Flags[j];
            }
            cohort.Count--;
            return true;
        }
        return false;
    }

    public bool RemoveSolo(TimelineId timeline, Direction direction, Row row)
    {
        var key = CohortKey(timeline, direction);
        if (!_solo.TryGetValue(key, out var cohort))
            return false;
        for (var i = 0; i < cohort.Count; i++)
        {
            if (cohort.RowValues[i] != row.Value)
                continue;
            for (var j = i + 1; j < cohort.Count; j++)
            {
                cohort.Tick[j - 1] = cohort.Tick[j];
                cohort.RowValues[j - 1] = cohort.RowValues[j];
                cohort.Duration[j - 1] = cohort.Duration[j];
                cohort.Flags[j - 1] = cohort.Flags[j];
                cohort.Rate[j - 1] = cohort.Rate[j];
                cohort.Carry[j - 1] = cohort.Carry[j];
            }
            cohort.Count--;
            return true;
        }
        return false;
    }

    public int CountOf(TimelineId timeline, Direction direction)
    {
        var key = CohortKey(timeline, direction);
        return _plain.TryGetValue(key, out var cohort) ? cohort.Count : 0;
    }

    public int SoloCountOf(TimelineId timeline, Direction direction)
    {
        var key = CohortKey(timeline, direction);
        return _solo.TryGetValue(key, out var cohort) ? cohort.Count : 0;
    }

    public bool TryEntryAt(TimelineId timeline, Direction direction, int index, out PlainPlayback entry)
    {
        entry = default;
        var key = CohortKey(timeline, direction);
        if (!_plain.TryGetValue(key, out var cohort))
            return false;
        if ((uint)index >= (uint)cohort.Count)
            return false;
        entry = new PlainPlayback
        {
            Phase = cohort.Phase[index],
            Birth = cohort.Birth[index],
            Duration = cohort.Duration[index],
            Looping = (cohort.Flags[index] & 1) != 0,
            Row = new Row(cohort.RowValues[index]),
        };
        return true;
    }

    public bool TrySoloEntryAt(TimelineId timeline, Direction direction, int index, out SoloPlayback entry)
    {
        entry = default;
        var key = CohortKey(timeline, direction);
        if (!_solo.TryGetValue(key, out var cohort))
            return false;
        if ((uint)index >= (uint)cohort.Count)
            return false;
        entry = new SoloPlayback
        {
            Tick = cohort.Tick[index],
            Duration = cohort.Duration[index],
            Flags = cohort.Flags[index],
            Rate = cohort.Rate[index],
            Carry = cohort.Carry[index],
            Row = new Row(cohort.RowValues[index]),
        };
        return true;
    }

    public int Advance(TimelineId timeline, Direction direction)
    {
        var key = CohortKey(timeline, direction);
        if (!_plain.TryGetValue(key, out var cohort))
            return 0;
        var removed = 0;
        var write = 0;
        for (var read = 0; read < cohort.Count; read++)
        {
            var looping = (cohort.Flags[read] & 1) != 0;
            var playback = new Clock.Playback
            {
                Phase = cohort.Phase[read],
                Duration = cohort.Duration[read],
                Looping = looping,
            };
            var (next, edge) = Clock.Step(playback, direction);
            if (!looping && (edge == Edge.Ended || edge == Edge.Started))
            {
                removed++;
                continue;
            }
            cohort.Phase[write] = next.Phase;
            cohort.RowValues[write] = cohort.RowValues[read];
            cohort.Birth[write] = cohort.Birth[read];
            cohort.Duration[write] = cohort.Duration[read];
            cohort.Flags[write] = cohort.Flags[read];
            write++;
        }
        cohort.Count = write;
        return removed;
    }

    public int AdvanceSolo(TimelineId timeline, Direction direction)
    {
        var key = CohortKey(timeline, direction);
        if (!_solo.TryGetValue(key, out var cohort))
            return 0;
        var removed = 0;
        var write = 0;
        for (var read = 0; read < cohort.Count; read++)
        {
            var rate = cohort.Rate[read];
            var carry = cohort.Carry[read];
            int ticks;
            if (rate == 0)
            {
                ticks = 1;
            }
            else
            {
                var sum = carry + rate;
                ticks = sum >> 4;
                carry = (byte)(sum & 15);
            }
            var looping = (cohort.Flags[read] & 1) != 0;
            var tick = cohort.Tick[read];
            var ended = false;
            for (var t = 0; t < ticks; t++)
            {
                var playback = new Clock.Playback
                {
                    Phase = tick,
                    Duration = cohort.Duration[read],
                    Looping = looping,
                };
                var (next, edge) = Clock.Step(playback, direction);
                tick = next.Phase;
                if (!looping && (edge == Edge.Ended || edge == Edge.Started))
                {
                    ended = true;
                    break;
                }
            }
            if (ended)
            {
                removed++;
                continue;
            }
            cohort.Tick[write] = tick;
            cohort.RowValues[write] = cohort.RowValues[read];
            cohort.Duration[write] = cohort.Duration[read];
            cohort.Flags[write] = cohort.Flags[read];
            cohort.Rate[write] = rate;
            cohort.Carry[write] = carry;
            write++;
        }
        cohort.Count = write;
        return removed;
    }
}
