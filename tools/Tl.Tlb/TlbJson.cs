using System.Buffers.Binary;
using System.Text.Json;

namespace Tl.Tlb;

public static class TlbJson
{
    public static string Write(TlbContainer c)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            w.WriteStartObject();
            w.WriteNumber("schemaVersion", c.Version == TlbContainer.HotVersion ? 2 : 1);
            w.WriteString("plane", "container");
            w.WriteStartObject("header");
            w.WriteString("magic", "TLB1");
            w.WriteNumber("formatVersion", c.Version);
            w.WriteBoolean("looping", c.Loops != 0);
            w.WriteNumber("duration", c.Duration);
            w.WriteNumber("trackCount", c.TrackCount);
            w.WriteNumber("stageCount", (uint)c.Stages.Length);
            w.WriteNumber("pairCount", (uint)c.Pairs.Length);
            w.WriteNumber("pairOffset", c.PairOffset);
            w.WriteNumber("stageOffset", c.StageOffset);
            w.WriteNumber("poolOffset", c.PoolOffset);
            w.WriteNumber("frameOffset", c.FrameOffset);
            w.WriteNumber("hotLength", c.HotLength);
            w.WriteNumber("bytes", c.Bytes);
            if (c.Version == TlbContainer.HotVersion)
            {
                w.WriteNumber("nextId", c.NextId);
                w.WriteNumber("channel", c.Channel);
                w.WriteBoolean("hold", c.Hold);
                w.WriteNumber("markerCount", (uint)c.Markers.Length);
            }
            w.WriteEndObject();

            w.WriteStartArray("pairs");
            for (uint i = 0; i < c.Pairs.Length; i++)
            {
                var p = c.Pairs[i];
                var entry = c.PairOffset + TlbContainer.PairEntryBytes * i;
                w.WriteStartObject();
                w.WriteString("key", Hex(p.Key));
                w.WriteNumber("slotStride", p.SlotStride);
                w.WriteString("layout", Hex(p.Layout));
                WritePool(w, "trackPool", entry + p.TrackPoolOffset, p.TrackPool);
                WritePool(w, "clipPool", entry + p.ClipPoolOffset, p.ClipPool);
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteStartArray("stages");
            foreach (var s in c.Stages)
            {
                w.WriteStartObject();
                w.WriteNumber("start", s.Start);
                w.WriteNumber("end", s.End);
                w.WriteNumber("programOffset", s.ProgramOffset);
                w.WriteStartArray("steps");
                foreach (var step in s.Steps)
                {
                    w.WriteStartObject();
                    w.WriteNumber("slot", step.Slot);
                    w.WriteNumber("pair", step.Pair);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteStartArray("rows");
            foreach (var r in c.Rows)
            {
                w.WriteStartObject();
                w.WriteNumber("trackValueIndex", r.TrackValueIndex);
                w.WriteNumber("firstValueIndex", r.FirstValueIndex);
                w.WriteNumber("secondValueIndex", r.SecondValueIndex);
                w.WriteNumber("trackIndex", r.TrackIndex);
                w.WriteNumber("windowStart", r.WindowStart);
                w.WriteNumber("windowEnd", r.WindowEnd);
                w.WriteNumber("factorStart", r.FactorStart);
                w.WriteNumber("factorSpan", r.FactorSpan);
                w.WriteEndObject();
            }
            w.WriteEndArray();

            if (c.Version == TlbContainer.HotVersion)
            {
                w.WriteStartArray("markers");
                foreach (var m in c.Markers)
                {
                    w.WriteStartObject();
                    w.WriteNumber("tick", m.Tick);
                    w.WriteNumber("size", m.Size);
                    w.WriteString("key", Hex(m.Key));
                    w.WriteString("payload", Convert.ToHexString(m.Payload).ToLowerInvariant());
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }

            if (c.Tail is { } tail)
            {
                w.WriteStartObject("tail");
                w.WriteNumber("offset", tail.Offset);
                w.WriteNumber("length", c.Bytes - c.HotLength);
                w.WriteStartArray("strings");
                foreach (var s in tail.Strings) w.WriteStringValue(s);
                w.WriteEndArray();
                w.WriteStartArray("types");
                foreach (var t in tail.Types)
                {
                    w.WriteStartObject();
                    w.WriteNumber("namespace", t.Namespace);
                    w.WriteNumber("name", t.Name);
                    w.WriteNumber("assembly", t.Assembly);
                    w.WriteString("text", tail.String(t.Namespace) + "." + tail.String(t.Name) + ", " + tail.String(t.Assembly));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteStartArray("pairTypes");
                foreach (var (track, clip) in tail.PairTypes)
                {
                    w.WriteStartObject();
                    w.WriteString("track", tail.TypeName(track));
                    w.WriteString("clip", tail.TypeName(clip));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteStartArray("labels");
                foreach (var (trackEntry, clipIndex, name) in tail.Labels)
                {
                    w.WriteStartObject();
                    w.WriteNumber("trackEntry", trackEntry);
                    w.WriteNumber("clipIndex", clipIndex);
                    w.WriteString("name", tail.String(name));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteStartArray("tracks");
                foreach (var (type, pair, value, clips) in tail.Tracks)
                {
                    w.WriteStartObject();
                    w.WriteString("type", tail.TypeName(type));
                    if (pair != uint.MaxValue) { w.WriteNumber("pair", pair); w.WriteNumber("value", value); }
                    w.WriteNumber("clips", clips);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteStartArray("clips");
                foreach (var (pair, value, start, end) in tail.Clips)
                {
                    w.WriteStartObject();
                    w.WriteNumber("pair", pair);
                    w.WriteNumber("value", value);
                    w.WriteNumber("start", start);
                    w.WriteNumber("end", end);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WritePool(Utf8JsonWriter w, string name, uint address, TlbPool pool)
    {
        w.WriteStartObject(name);
        w.WriteNumber("offset", address);
        w.WriteNumber("count", pool.Count);
        w.WriteNumber("valueBytes", pool.ValueBytes);
        w.WriteStartArray("values");
        foreach (var v in pool.Values) w.WriteStringValue(Convert.ToHexString(v).ToLowerInvariant());
        w.WriteEndArray();
        w.WriteEndObject();
    }

    static string Hex(ulong value) => "0x" + value.ToString("x16");
}
