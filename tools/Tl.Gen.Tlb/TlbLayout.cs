using System.Buffers.Binary;
using System.Collections.Generic;

namespace Tl.Gen.Tlb;

internal static class TlbLayout
{
    internal const uint Magic = 0x31424C54u;
    internal const uint Version = 2u;
    internal const uint HeaderBytes = 64u;
    internal const uint PairEntryBytes = 48u;
    internal const uint StageEntryBytes = 16u;
    internal const uint StepBytes = 8u;
    internal const uint SlotRowBytes = 32u;
    internal const ushort NoClipIndex = 0xFFFF;

    internal static uint Align16(uint value) => (value + 15u) & ~15u;

    internal static void WriteRow(byte[] bytes, int offset, ushort trackValueIndex, ushort firstIndex, ushort secondIndex, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan, byte trackIndex)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), trackValueIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 2), firstIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 4), secondIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), windowStart);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 12), windowEnd);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 16), factorStart);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 20), factorSpan);
        bytes[offset + 24] = trackIndex;
    }

    internal static int CompareImages(byte[] left, byte[] right)
    {
        var byLength = left.Length.CompareTo(right.Length);
        if (byLength != 0) return byLength;
        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return left[i].CompareTo(right[i]);
        return 0;
    }
}

internal sealed class ImageComparer : IEqualityComparer<byte[]>
{
    internal static readonly ImageComparer Instance = new();

    public bool Equals(byte[]? left, byte[]? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null || left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;
        return true;
    }

    public int GetHashCode(byte[] image)
    {
        var hash = new HashCode();
        hash.AddBytes(image);
        return hash.ToHashCode();
    }
}

internal sealed class ValuePoolSet
{
    internal List<byte[]> TrackEntries = [];
    internal Dictionary<byte[], ushort> TrackIndex = new(ImageComparer.Instance);
    internal List<byte[]> ClipEntries = [];
    internal Dictionary<byte[], ushort> ClipIndex = new(ImageComparer.Instance);

    internal static ValuePoolSet Build(IEnumerable<byte[]> trackImages, IEnumerable<byte[]> clipImages, string trackTypeName, string clipTypeName)
    {
        var (trackEntries, trackIndex) = Pool(trackImages, $"track type '{trackTypeName}'");
        var (clipEntries, clipIndex) = Pool(clipImages, $"clip type '{clipTypeName}'");
        return new ValuePoolSet { TrackEntries = trackEntries, TrackIndex = trackIndex, ClipEntries = clipEntries, ClipIndex = clipIndex };
    }

    private static (List<byte[]>, Dictionary<byte[], ushort>) Pool(IEnumerable<byte[]> images, string pool)
    {
        var unique = new List<byte[]>();
        var seen = new Dictionary<byte[], byte>(ImageComparer.Instance);
        foreach (var image in images)
            if (seen.TryAdd(image, 0))
                unique.Add(image);
        if (unique.Count > 65535)
            throw new BakeDiagnosticException($"value pool overflow: {pool} has {unique.Count} unique values in one pool; the fixed-width ushort slot index holds at most 65,535 entries.");
        unique.Sort(TlbLayout.CompareImages);
        var index = new Dictionary<byte[], ushort>(unique.Count, ImageComparer.Instance);
        for (var i = 0; i < unique.Count; i++)
            index[unique[i]] = (ushort)i;
        return (unique, index);
    }
}
