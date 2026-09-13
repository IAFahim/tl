using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Tl;

namespace Tl.Gen.Tlb;

public static class TlbReport
{
    public static string Report(ReadOnlySpan<byte> tlb)
    {
        var hotBytes = (uint)TlbMetadata.HotLength(tlb);
        var totalBytes = (uint)tlb.Length;
        var pairOffset = Word(tlb, 28);
        var stageOffset = Word(tlb, 32);
        var frameOffset = Word(tlb, 36);
        var pairCount = Word(tlb, 24);
        var stageCount = Word(tlb, 20);
        if (pairOffset < 48 || (ulong)pairOffset + 16ul * pairCount > stageOffset)
            throw new ArgumentException("TLB1 pair table is out of bounds.");
        if ((ulong)stageOffset + 16ul * stageCount > hotBytes)
            throw new ArgumentException("TLB1 stage table is out of bounds.");
        if (frameOffset < (ulong)stageOffset + 16ul * stageCount || frameOffset > hotBytes)
            throw new ArgumentException("TLB1 frame region is out of bounds.");

        ulong steps = 0;
        for (var index = 0; index < stageCount; index++)
            steps += Word(tlb, (int)stageOffset + 16 * index + 12);

        var lines = new StringBuilder(256);
        Line(lines, "tlb/total-bytes", totalBytes);
        Line(lines, "tlb/hot-bytes", hotBytes);
        Line(lines, "tlb/metadata-bytes", totalBytes - hotBytes);
        Line(lines, "pair/table-bytes", stageOffset - pairOffset);
        Line(lines, "pair/count", pairCount);
        Line(lines, "stage/count", stageCount);
        Line(lines, "program/step-count", steps);
        Line(lines, "frame-slot/region-bytes", hotBytes - frameOffset);
        Line(lines, "instance/state-bytes", (ulong)Unsafe.SizeOf<TimelineComponent>());
        AppendLabels(lines, tlb);
        return lines.ToString();
    }

    private static void AppendLabels(StringBuilder lines, ReadOnlySpan<byte> tlb)
    {
        uint root = 0;
        uint track = 0;
        uint clip = 0;
        if (TlbMetadata.HasMetadata(tlb))
        {
            foreach (var label in TlbMetadata.Read(tlb).Labels)
            {
                if (label.TrackEntry < 0) root++;
                else if (label.ClipIndex < 0) track++;
                else clip++;
            }
        }
        Line(lines, "label/root-count", root);
        Line(lines, "label/track-count", track);
        Line(lines, "label/clip-count", clip);
    }

    private static uint Word(ReadOnlySpan<byte> tlb, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(tlb.Slice(offset));

    private static void Line(StringBuilder lines, string name, ulong value)
    {
        lines.Append(name);
        lines.Append(": ");
        lines.Append(value.ToString(CultureInfo.InvariantCulture));
        lines.Append('\n');
    }
}
