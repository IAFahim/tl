namespace Tl.Tlb;

public sealed record TlbAccess(uint Start, uint End, string What, string Why);

public static class TlbTrace
{
    public static TlbAccess[] Load(TlbContainer c)
    {
        var a = new List<TlbAccess>
        {
            new(0, c.Bytes, "digest — all bytes",
                "TimelineTable.Digest hashes every byte (32-byte strides) into the content identity that interns this timeline"),
            new(0, TlbContainer.HeaderBytes, "header",
                "TimelineRef.Validate reads the 64-byte header and cross-checks magic, version, offsets, and sizes"),
            new(c.PairOffset, c.PairOffset + TlbContainer.PairEntryBytes * (uint)c.Pairs.Length, "pair table",
                "TimelineRef.Validate checks key order, slot stride, and both pool bounds"),
            new(c.StageOffset, c.StageOffset + TlbContainer.StageEntryBytes * (uint)c.Stages.Length, "stage table",
                "TimelineRef.Validate checks stage contiguity from 0 to Duration"),
        };
        foreach (var stage in c.Stages)
            a.Add(new(stage.ProgramOffset, stage.ProgramOffset + TlbContainer.StepBytes * (uint)stage.Steps.Length,
                "program steps", "TimelineRef.Validate checks every step's slot bounds and value indexes"));
        foreach (var slot in DistinctSlots(c))
            a.Add(new(slot, slot + TlbContainer.RowBytes, $"row @0x{slot:X}",
                "TimelineRef.ValidateRow reads the value indexes and the blend window"));
        a.Add(new(0, c.Bytes, "copy — all bytes",
            "TimelineTable.Publish stores the whole container in the native block; the returned ushort index addresses it"));
        return [.. a];
    }

    public static TlbAccess[] Tick(TlbContainer c, uint tick, bool reverse)
    {
        var a = new List<TlbAccess>
        {
            new(8, 12, "header.loops", "TimelineMovement.Advance — loop or clamp"),
            new(12, 16, "header.duration", "TimelineMovement.Advance — position domain"),
        };
        var stageCount = (uint)c.Stages.Length;
        if (stageCount == 0) return [.. a];

        a.Add(new(20, 24, "header.stageCount", "TimelineRef.ExecuteDispatch — lookup strategy"));
        TlbAccess Entry(uint i, string why) =>
            new(c.StageOffset + TlbContainer.StageEntryBytes * i, c.StageOffset + TlbContainer.StageEntryBytes * (i + 1),
                $"stage {i} entry", why);
        int hit;
        if (stageCount == 1)
        {
            a.Add(new(32, 36, "header.stageOffset", "ExecuteDispatch — single stage, no lookup"));
            a.Add(Entry(0, $"the only stage covers [0, {c.Duration})"));
            hit = 0;
        }
        else
        {
            a.Add(new(32, 36, "header.stageOffset", "TimelineRef.StageOf — stage table base"));
            hit = StageOf(c, tick, a, Entry);
            if (hit < 0) return [.. a];
        }

        var stage = c.Stages[hit];
        var order = Enumerable.Range(0, stage.Steps.Length);
        if (reverse) order = order.Reverse();
        foreach (var t in order)
        {
            var step = stage.Steps[t];
            a.Add(new(stage.ProgramOffset + TlbContainer.StepBytes * (uint)t,
                stage.ProgramOffset + TlbContainer.StepBytes * (uint)(t + 1),
                $"stage {hit} step {t}", $"ExecuteDispatch — slot 0x{step.Slot:X}, pair {step.Pair}"));
            var entry = c.PairOffset + TlbContainer.PairEntryBytes * step.Pair;
            var pair = c.Pairs[(int)step.Pair];
            a.Add(new(entry + 12, entry + 16, $"pair {step.Pair}.trackPoolOffset", "SlotRow.ToFrame — track value base"));
            a.Add(new(entry + 24, entry + 28, $"pair {step.Pair}.clipPoolOffset", "SlotRow.ToFrame — clip pool base"));
            var slot = step.Slot;
            a.Add(new(slot, slot + 2, "row.trackValueIndex", "SlotRow.ToFrame — track value index"));
            a.Add(new(slot + 2, slot + 4, "row.firstValueIndex", "SlotRow.ToFrame — first clip index"));
            a.Add(new(slot + 6, slot + 7, "row.trackIndex", "SlotRow.ToFrame — consumer track"));
            a.Add(new(slot + 8, slot + 12, "row.windowStart", "SlotRow.ToFrame — ClipStart flag, frame local time"));
            a.Add(new(slot + 12, slot + 16, "row.windowEnd", "SlotRow.ToFrame — ClipEnd flag, clip length"));
            a.Add(new(slot + 20, slot + 24, "row.factorSpan", "SlotRow.ToFrame — blend test"));
            var row = RowAt(c, step);
            if (row.Blends)
            {
                a.Add(new(slot + 4, slot + 6, "row.secondValueIndex", "SlotRow.ToFrame — second clip index"));
                a.Add(new(slot + 16, slot + 20, "row.factorStart", "SlotRow.ToFrame — factor numerator"));
            }
            var trackAt = pair.TrackPoolAddress(entry) + row.TrackValueIndex * pair.TrackValueBytes;
            a.Add(new(trackAt, trackAt + pair.TrackValueBytes, $"pair {step.Pair} track value {row.TrackValueIndex}",
                "SlotRow.ToFrame — *(TTrack*) dereference"));
            var clipAt = pair.ClipPoolAddress(entry) + row.FirstValueIndex * pair.ClipValueBytes;
            a.Add(new(clipAt, clipAt + pair.ClipValueBytes, $"pair {step.Pair} clip value {row.FirstValueIndex}",
                "SlotRow.ToFrame — *(TClip*) dereference"));
            if (row.Blends)
            {
                var secondAt = pair.ClipPoolAddress(entry) + row.SecondValueIndex * pair.ClipValueBytes;
                a.Add(new(secondAt, secondAt + pair.ClipValueBytes, $"pair {step.Pair} clip value {row.SecondValueIndex}",
                    "SlotRow.ToFrame — second *(TClip*) for the blend"));
            }
        }
        return [.. a];
    }

    static int StageOf(TlbContainer c, uint tick, List<TlbAccess> a, Func<uint, string, TlbAccess> entry)
    {
        var stages = c.Stages;
        if (stages.Length <= 4)
        {
            for (var i = 0; i < stages.Length; i++)
            {
                a.Add(entry((uint)i, tick < stages[i].End
                    ? $"StageOf hit — stage {i} covers [{stages[i].Start}, {stages[i].End})"
                    : $"StageOf scan — tick ≥ {stages[i].End}, keep scanning"));
                if (tick < stages[i].End) return i;
            }
            return -1;
        }
        for (int l = 0, h = stages.Length - 1; l <= h;)
        {
            var m = (l + h) >> 1;
            if (tick >= stages[m].End)
            {
                a.Add(entry((uint)m, $"StageOf probe — tick ≥ {stages[m].End}, go right"));
                l = m + 1;
                continue;
            }
            if (m > 0)
                a.Add(entry((uint)(m - 1), tick >= stages[m - 1].End
                    ? $"StageOf left bound — tick ≥ {stages[m - 1].End}, stage {m} is the hit"
                    : $"StageOf left bound — tick < {stages[m - 1].End}, go left"));
            if (m == 0 || tick >= stages[m - 1].End)
            {
                a.Add(entry((uint)m, $"StageOf hit — stage {m} covers [{stages[m].Start}, {stages[m].End})"));
                return m;
            }
            h = m - 1;
        }
        return -1;
    }

    static IEnumerable<uint> DistinctSlots(TlbContainer c)
    {
        var slots = new SortedSet<uint>();
        foreach (var stage in c.Stages)
            foreach (var step in stage.Steps)
                slots.Add(step.Slot);
        return slots;
    }

    static TlbRow RowAt(TlbContainer c, TlbStep step)
    {
        var index = 0;
        foreach (var stage in c.Stages)
            foreach (var s in stage.Steps)
            {
                if (s.Slot == step.Slot && s.Pair == step.Pair) return c.Rows[index];
                index++;
            }
        throw new TlbFormatException($"no row for step {step.Slot}.");
    }
}
