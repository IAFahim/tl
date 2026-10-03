using System.Buffers.Binary;
using System.Text;

namespace Tl.Tlb;

public enum TlbFieldKind { Header, Pair, Stage, Step, TrackValue, ClipValue, Row, Tail, Gap }

public sealed record TlbField(uint Start, uint End, TlbFieldKind Kind, string Path, string Value)
{
    public uint Length => End - Start;
}

public static class TlbMap
{
    public static TlbField[] Fields(TlbContainer c, byte[] f)
    {
        var fields = new List<TlbField>();
        void Add(uint start, uint end, TlbFieldKind kind, string path, string value) =>
            fields.Add(new TlbField(start, end, kind, path, value));
        uint U32(uint at) => BinaryPrimitives.ReadUInt32LittleEndian(f.AsSpan((int)at));
        string N(uint at) => U32(at).ToString();
        string Preview(uint at, uint width)
        {
            var hex = Hex(f.AsSpan((int)at, (int)width));
            return width == 4 ? $"{hex} · f32 {BinaryPrimitives.ReadSingleLittleEndian(f.AsSpan((int)at)):.###}"
                : width == 8 ? $"{hex} · f64 {BinaryPrimitives.ReadDoubleLittleEndian(f.AsSpan((int)at)):.###}"
                : hex;
        }

        Add(0, 4, TlbFieldKind.Header, "header.magic", "\"TLB1\"");
        Add(4, 8, TlbFieldKind.Header, "header.version", N(4));
        Add(8, 12, TlbFieldKind.Header, "header.loops", N(8));
        Add(12, 16, TlbFieldKind.Header, "header.duration", N(12));
        Add(16, 20, TlbFieldKind.Header, "header.tracks", N(16));
        Add(20, 24, TlbFieldKind.Header, "header.stages", N(20));
        Add(24, 28, TlbFieldKind.Header, "header.pairs", N(24));
        Add(28, 32, TlbFieldKind.Header, "header.pairOffset", N(28));
        Add(32, 36, TlbFieldKind.Header, "header.stageOffset", N(32));
        Add(36, 40, TlbFieldKind.Header, "header.poolOffset", N(36));
        Add(40, 44, TlbFieldKind.Header, "header.frameOffset", N(40));
        Add(44, 48, TlbFieldKind.Header, "header.hotLength", N(44));
        Add(48, 52, TlbFieldKind.Header, "header.bytes", N(48));
        Add(52, 64, TlbFieldKind.Header, "header.reserved", "0");

        for (uint i = 0; i < c.Pairs.Length; i++)
        {
            var e = c.PairOffset + TlbContainer.PairEntryBytes * i;
            var p = c.Pairs[i];
            Add(e, e + 8, TlbFieldKind.Pair, $"pair {i}.key", p.Key.ToString("x16"));
            Add(e + 8, e + 12, TlbFieldKind.Pair, $"pair {i}.slotStride", U32(e + 8).ToString());
            Add(e + 12, e + 16, TlbFieldKind.Pair, $"pair {i}.trackPoolOffset", U32(e + 12).ToString());
            Add(e + 16, e + 20, TlbFieldKind.Pair, $"pair {i}.trackPoolCount", U32(e + 16).ToString());
            Add(e + 20, e + 24, TlbFieldKind.Pair, $"pair {i}.trackValueBytes", U32(e + 20).ToString());
            Add(e + 24, e + 28, TlbFieldKind.Pair, $"pair {i}.clipPoolOffset", U32(e + 24).ToString());
            Add(e + 28, e + 32, TlbFieldKind.Pair, $"pair {i}.clipPoolCount", U32(e + 28).ToString());
            Add(e + 32, e + 36, TlbFieldKind.Pair, $"pair {i}.clipValueBytes", U32(e + 32).ToString());
            Add(e + 36, e + 40, TlbFieldKind.Pair, $"pair {i}.pad", "0");
            Add(e + 40, e + 48, TlbFieldKind.Pair, $"pair {i}.layout", p.Layout.ToString("x16"));
            var trackAddress = p.TrackPoolAddress(e);
            for (uint v = 0; v < p.TrackPoolCount; v++)
                Add(trackAddress + v * p.TrackValueBytes, trackAddress + (v + 1) * p.TrackValueBytes,
                    TlbFieldKind.TrackValue, $"pair {i} track value {v}", Preview(trackAddress + v * p.TrackValueBytes, p.TrackValueBytes));
            var clipAddress = p.ClipPoolAddress(e);
            for (uint v = 0; v < p.ClipPoolCount; v++)
                Add(clipAddress + v * p.ClipValueBytes, clipAddress + (v + 1) * p.ClipValueBytes,
                    TlbFieldKind.ClipValue, $"pair {i} clip value {v}", Preview(clipAddress + v * p.ClipValueBytes, p.ClipValueBytes));
        }

        for (uint i = 0; i < c.Stages.Length; i++)
        {
            var at = c.StageOffset + TlbContainer.StageEntryBytes * i;
            var s = c.Stages[i];
            Add(at, at + 4, TlbFieldKind.Stage, $"stage {i}.start", s.Start.ToString());
            Add(at + 4, at + 8, TlbFieldKind.Stage, $"stage {i}.end", s.End.ToString());
            Add(at + 8, at + 12, TlbFieldKind.Stage, $"stage {i}.programOffset", s.ProgramOffset.ToString());
            Add(at + 12, at + 16, TlbFieldKind.Stage, $"stage {i}.programCount", ((uint)s.Steps.Length).ToString());
            for (uint t = 0; t < s.Steps.Length; t++)
            {
                var stepAt = s.ProgramOffset + TlbContainer.StepBytes * t;
                Add(stepAt, stepAt + 4, TlbFieldKind.Step, $"stage {i} step {t}.slot", s.Steps[t].Slot.ToString());
                Add(stepAt + 4, stepAt + 8, TlbFieldKind.Step, $"stage {i} step {t}.pair", s.Steps[t].Pair.ToString());
            }
        }

        var strides = new Dictionary<uint, uint>();
        foreach (var stage in c.Stages)
            foreach (var step in stage.Steps)
                strides[step.Slot] = Math.Max(strides.TryGetValue(step.Slot, out var known) ? known : 0, c.Pairs[(int)step.Pair].SlotStride);
        foreach (var (slot, stride) in strides.OrderBy(kv => kv.Key))
        {
            var row = RowAt(c, slot);
            Add(slot, slot + 2, TlbFieldKind.Row, $"row @0x{slot:X}.trackValueIndex", row.TrackValueIndex.ToString());
            Add(slot + 2, slot + 4, TlbFieldKind.Row, $"row @0x{slot:X}.firstValueIndex", row.FirstValueIndex.ToString());
            Add(slot + 4, slot + 6, TlbFieldKind.Row, $"row @0x{slot:X}.secondValueIndex",
                row.SecondValueIndex == TlbRow.NoClipValue ? "65535 (none)" : row.SecondValueIndex.ToString());
            Add(slot + 6, slot + 7, TlbFieldKind.Row, $"row @0x{slot:X}.trackIndex", row.TrackIndex.ToString());
            Add(slot + 7, slot + 8, TlbFieldKind.Row, $"row @0x{slot:X}.pad", "0");
            Add(slot + 8, slot + 12, TlbFieldKind.Row, $"row @0x{slot:X}.windowStart", row.WindowStart.ToString());
            Add(slot + 12, slot + 16, TlbFieldKind.Row, $"row @0x{slot:X}.windowEnd", row.WindowEnd.ToString());
            Add(slot + 16, slot + 20, TlbFieldKind.Row, $"row @0x{slot:X}.factorStart", row.FactorStart.ToString());
            Add(slot + 20, slot + 24, TlbFieldKind.Row, $"row @0x{slot:X}.factorSpan", row.FactorSpan.ToString());
            if (stride > TlbContainer.RowBytes)
                Add(slot + TlbContainer.RowBytes, slot + stride, TlbFieldKind.Gap, $"row @0x{slot:X}.stride pad", "alignment");
        }

        if (c.Tail is { } tail)
        {
            var t = tail.Offset;
            Add(t, t + 4, TlbFieldKind.Tail, "tail.magic", "\"LTBM\"");
            Add(t + 4, t + 8, TlbFieldKind.Tail, "tail.version", U32(t + 4).ToString());
            Add(t + 8, t + 12, TlbFieldKind.Tail, "tail.stringCount", ((uint)tail.Strings.Length).ToString());
            Add(t + 12, t + 16, TlbFieldKind.Tail, "tail.stringIndexOffset", U32(t + 12).ToString());
            Add(t + 16, t + 20, TlbFieldKind.Tail, "tail.stringBlobOffset", U32(t + 16).ToString());
            Add(t + 20, t + 24, TlbFieldKind.Tail, "tail.stringBlobLen", U32(t + 20).ToString());
            Add(t + 24, t + 28, TlbFieldKind.Tail, "tail.typeCount", ((uint)tail.Types.Length).ToString());
            Add(t + 28, t + 32, TlbFieldKind.Tail, "tail.typeTableOffset", U32(t + 28).ToString());
            Add(t + 32, t + 36, TlbFieldKind.Tail, "tail.pairTypeCount", ((uint)tail.PairTypes.Length).ToString());
            Add(t + 36, t + 40, TlbFieldKind.Tail, "tail.pairTypeTableOffset", U32(t + 36).ToString());
            Add(t + 40, t + 44, TlbFieldKind.Tail, "tail.labelCount", ((uint)tail.Labels.Length).ToString());
            Add(t + 44, t + 48, TlbFieldKind.Tail, "tail.labelTableOffset", U32(t + 44).ToString());
            var indexAt = t + U32(t + 12);
            for (uint i = 0; i < tail.Strings.Length; i++)
                Add(indexAt + 4 * i, indexAt + 4 * (i + 1), TlbFieldKind.Tail, $"tail string index {i}",
                    $"\"{tail.Strings[i]}\"");
            var blobAt = t + U32(t + 16);
            Add(blobAt, blobAt + U32(t + 20), TlbFieldKind.Tail, "tail.stringBlob",
                $"\"{Encoding.UTF8.GetString(f.AsSpan((int)blobAt, (int)U32(t + 20))).Replace("\n", "\\n")}\"");
            var typeAt = t + U32(t + 28);
            for (uint i = 0; i < tail.Types.Length; i++)
                Add(typeAt + 12 * i, typeAt + 12 * (i + 1), TlbFieldKind.Tail, $"tail type {i}", tail.TypeName(i));
            var pairTypeAt = t + U32(t + 36);
            for (uint i = 0; i < tail.PairTypes.Length; i++)
                Add(pairTypeAt + 8 * i, pairTypeAt + 8 * (i + 1), TlbFieldKind.Tail, $"tail pair type {i}",
                    ShortName(tail, tail.PairTypes[i].TrackType) + " × " + ShortName(tail, tail.PairTypes[i].ClipType));
            var labelAt = t + U32(t + 44);
            for (uint i = 0; i < tail.Labels.Length; i++)
            {
                var (trackEntry, clipIndex, name) = tail.Labels[i];
                Add(labelAt + 12 * i, labelAt + 12 * (i + 1), TlbFieldKind.Tail, $"tail label {i}",
                    $"track {trackEntry} clip {clipIndex} = \"{tail.String(name)}\"");
            }
        }

        fields.Sort((a, b) => a.Start.CompareTo(b.Start));
        var tiled = new List<TlbField>();
        var cursor = 0u;
        foreach (var field in fields)
        {
            if (field.Start > cursor)
                tiled.Add(new TlbField(cursor, field.Start, TlbFieldKind.Gap, $"gap @0x{cursor:X}", "alignment"));
            tiled.Add(field);
            cursor = Math.Max(cursor, field.End);
        }
        if (cursor < c.Bytes)
            tiled.Add(new TlbField(cursor, c.Bytes, TlbFieldKind.Gap, $"gap @0x{cursor:X}", "alignment"));
        return [.. tiled];
    }

    static TlbRow RowAt(TlbContainer c, uint slot)
    {
        foreach (var stage in c.Stages)
            foreach (var step in stage.Steps)
                if (step.Slot == slot)
                    return c.Rows[IndexOfRow(c, step)];
        throw new TlbFormatException($"no step addresses slot row {slot}.");
    }

    static int IndexOfRow(TlbContainer c, TlbStep step)
    {
        var index = 0;
        foreach (var stage in c.Stages)
            foreach (var s in stage.Steps)
            {
                if (s.Slot == step.Slot && s.Pair == step.Pair) return index;
                index++;
            }
        throw new TlbFormatException($"no row for step {step.Slot}.");
    }

    static string ShortName(TlbTail tail, uint type)
    {
        var full = tail.TypeName(type);
        var comma = full.IndexOf(',');
        return comma < 0 ? full : full[..comma];
    }

    static string Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
