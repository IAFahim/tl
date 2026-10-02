using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace Tl.Gen.Tlb;

public static class TlbInspection
{
    public static string Inspect(ReadOnlySpan<byte> tlb)
    {
        var hotLength = TlbMetadata.HotLength(tlb);
        var pairOffset = Word(tlb, 28);
        var stageOffset = Word(tlb, 32);
        var poolOffset = Word(tlb, 36);
        var frameOffset = Word(tlb, 40);
        var pairCount = Word(tlb, 24);
        var stageCount = Word(tlb, 20);
        if (pairOffset < 64 || (ulong)pairOffset + 48ul * (ulong)pairCount > (ulong)stageOffset)
            throw new ArgumentException("TLB pair table is out of bounds.");
        if ((ulong)stageOffset + 16ul * (ulong)stageCount > (ulong)poolOffset || poolOffset > frameOffset || frameOffset > hotLength)
            throw new ArgumentException("TLB section layout is out of bounds.");

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            void W(string name) => writer.WriteStartObject(name);
            void A(string name) => writer.WriteStartArray(name);
            void E() => writer.WriteEndObject();
            void N(string name, long value) => writer.WriteNumber(name, value);
            void S(string name, string value) => writer.WriteString(name, value);

            writer.WriteStartObject();
            N("schemaVersion", 1);
            W("header");
            N("formatVersion", Word(tlb, 4));
            N("duration", Word(tlb, 12));
            writer.WriteBoolean("looping", Word(tlb, 8) != 0);
            E();

            A("pairs");
            for (var index = 0; index < pairCount; index++)
            {
                var at = pairOffset + 48 * index;
                writer.WriteStartObject();
                S("key", Hex(tlb, at));
                S("layout", Hex(tlb, at + 40));
                N("slotStride", Word(tlb, at + 8));
                W("pools");
                N("trackValueBytes", Word(tlb, at + 20));
                N("clipValueBytes", Word(tlb, at + 32));
                E();
                E();
            }
            writer.WriteEndArray();

            A("stages");
            for (var index = 0; index < stageCount; index++)
            {
                var at = stageOffset + 16 * index;
                var programOffset = Word(tlb, at + 8);
                var stepCount = Word(tlb, at + 12);
                if ((ulong)programOffset + 8ul * (ulong)stepCount > (ulong)poolOffset)
                    throw new ArgumentException("TLB program steps are out of bounds.");
                writer.WriteStartObject();
                N("start", Word(tlb, at));
                N("end", Word(tlb, at + 4));
                A("steps");
                for (var step = 0; step < stepCount; step++)
                {
                    var program = programOffset + 8 * step;
                    var slot = Word(tlb, program);
                    var pair = Word(tlb, program + 4);
                    if (pair >= pairCount)
                        throw new ArgumentException($"TLB step {step} of stage {index} names pair {pair}, above the pair count {pairCount}.");
                    var stride = Word(tlb, pairOffset + 48 * pair + 8);
                    if (slot < frameOffset || slot % 8 != 0 || slot + stride > hotLength)
                        throw new ArgumentException($"TLB step {step} of stage {index} has a slot outside the frame region.");
                    writer.WriteStartObject();
                    N("pair", pair);
                    N("track", tlb[slot + 6]);
                    N("trackValue", UInt16(tlb, slot));
                    N("first", UInt16(tlb, slot + 2));
                    N("second", UInt16(tlb, slot + 4));
                    N("windowStart", Word(tlb, slot + 8));
                    N("windowEnd", Word(tlb, slot + 12));
                    N("factorStart", Word(tlb, slot + 16));
                    N("factorSpan", Word(tlb, slot + 20));
                    E();
                }
                writer.WriteEndArray();
                E();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static string Hex(ReadOnlySpan<byte> tlb, int offset) => "0x" + Convert.ToHexString(tlb.Slice(offset, 8)).ToLowerInvariant();

    static int Word(ReadOnlySpan<byte> tlb, int offset) => BinaryPrimitives.ReadInt32LittleEndian(tlb.Slice(offset));

    static int UInt16(ReadOnlySpan<byte> tlb, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(tlb.Slice(offset));
}
