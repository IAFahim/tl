using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace Tl.Gen.Tlb;

public static class Tlib3Inspection
{
    public static bool IsV3(ReadOnlySpan<byte> blob)
    {
        if (blob.Length < 8)
            return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(blob.Slice(0)) != 0x42494C54u)
            return false;
        return BinaryPrimitives.ReadUInt32LittleEndian(blob.Slice(4)) == 3u;
    }

    public static string Inspect(ReadOnlySpan<byte> blob)
    {
        if (!IsV3(blob))
            throw new BakeDiagnosticException("input is not a TLIB v3 library.");
        var result = Tl.Library.Verify(blob);
        if (!result.Ok)
            throw new BakeDiagnosticException($"TLIB v3 verify failed: {result.Error} at offset {result.ErrorOffset}.");
        using var library = result.Library!;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            void W(string name) => writer.WriteStartObject(name);
            void A(string name) => writer.WriteStartArray(name);
            void E() => writer.WriteEndObject();
            void N(string name, long value) => writer.WriteNumber(name, value);
            void S(string name, string value) => writer.WriteString(name, value);

            writer.WriteStartObject();
            N("schemaVersion", 3);
            W("header");
            N("formatVersion", library.Version);
            N("timelineCount", library.TimelineCount);
            N("stepCount", library.StepCount);
            N("kindCount", library.KindCount);
            E();
            A("timelines");
            for (var t = 0; t < library.TimelineCount; t++)
            {
                var timeline = library.Timeline(t);
                writer.WriteStartObject();
                S("name", library.TimelineName(t));
                N("duration", timeline.Duration);
                writer.WriteBoolean("loop", timeline.Loop != 0);
                A("steps");
                var index = (int)timeline.StepIndex;
                for (var s = 0; s < timeline.StepCount; s++, index++)
                {
                    var step = library.Step(index);
                    writer.WriteStartObject();
                    N("windowStart", step.WindowStart);
                    N("windowEnd", step.WindowEnd);
                    N("kindId", step.KindId);
                    writer.WriteNumber("pairKey", step.PairKey);
                    N("forwardClass", step.ForwardClass);
                    N("backwardClass", step.BackwardClass);
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
}
