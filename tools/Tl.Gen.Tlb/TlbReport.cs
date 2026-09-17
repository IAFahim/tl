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
        var poolOffset = Word(tlb, 36);
        var frameOffset = Word(tlb, 40);
        var pairCount = Word(tlb, 24);
        var stageCount = Word(tlb, 20);
        if (pairOffset < 64 || (ulong)pairOffset + 48ul * pairCount > stageOffset)
            throw new ArgumentException("TLB pair table is out of bounds.");
        if ((ulong)stageOffset + 16ul * stageCount > poolOffset || poolOffset > frameOffset || frameOffset > hotBytes)
            throw new ArgumentException("TLB section layout is out of bounds.");

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
        Line(lines, "pool/region-bytes", frameOffset - poolOffset);
        Line(lines, "frame-slot/region-bytes", hotBytes - frameOffset);
        Line(lines, "instance/state-bytes", (ulong)Unsafe.SizeOf<TimelineComponent>());
        AppendPools(lines, tlb, (int)pairOffset, (int)pairCount);
        AppendLabels(lines, tlb);
        return lines.ToString();
    }

    private static void AppendPools(StringBuilder lines, ReadOnlySpan<byte> tlb, int pairOffset, int pairCount)
    {
        ulong values = 0;
        ulong valueBytes = 0;
        for (var index = 0; index < pairCount; index++)
        {
            var at = pairOffset + 48 * index;
            var trackCount = Word(tlb, at + 16);
            var trackValueBytes = Word(tlb, at + 20);
            var clipCount = Word(tlb, at + 28);
            var clipValueBytes = Word(tlb, at + 32);
            Line(lines, $"pool/{index}/track-unique-count", trackCount);
            Line(lines, $"pool/{index}/clip-unique-count", clipCount);
            Line(lines, $"pool/{index}/pool-bytes", Align16(trackCount * trackValueBytes) + Align16(clipCount * clipValueBytes));
            values += trackCount + clipCount;
            valueBytes += (ulong)trackCount * trackValueBytes + (ulong)clipCount * clipValueBytes;
        }
        Line(lines, "pool/unique-count", values);
        Line(lines, "pool/value-bytes", valueBytes);
    }

    private static uint Align16(uint value) => (value + 15u) & ~15u;

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
