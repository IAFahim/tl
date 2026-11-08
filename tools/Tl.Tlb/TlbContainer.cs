using System.Buffers.Binary;
using System.Text;

namespace Tl.Tlb;

public sealed class TlbFormatException(string message) : Exception(message);

public sealed record TlbPool(uint Offset, uint Count, uint ValueBytes, byte[][] Values)
{
    public string Hex(int index) => Convert.ToHexString(Values[index]).ToLowerInvariant();
}

public sealed record TlbPair(
    ulong Key, uint SlotStride, ulong Layout,
    uint TrackPoolOffset, uint TrackPoolCount, uint TrackValueBytes,
    uint ClipPoolOffset, uint ClipPoolCount, uint ClipValueBytes,
    TlbPool TrackPool, TlbPool ClipPool)
{
    public uint TrackPoolAddress(uint entry) => entry + TrackPoolOffset;
    public uint ClipPoolAddress(uint entry) => entry + ClipPoolOffset;
}

public sealed record TlbStep(uint Slot, uint Pair);

public sealed record TlbStage(uint Start, uint End, uint ProgramOffset, TlbStep[] Steps);

public sealed record TlbRow(
    uint TrackValueIndex, uint FirstValueIndex, uint SecondValueIndex, uint TrackIndex,
    uint WindowStart, uint WindowEnd, uint FactorStart, uint FactorSpan)
{
    public const uint NoClipValue = 0xFFFF;
    public bool Blends => FactorSpan > 0;
}

public sealed record TlbMarker(uint Tick, uint Size, ulong Key, byte[] Payload);

public sealed record TlbType(uint Namespace, uint Name, uint Assembly);

public sealed record TlbTail(
    uint Offset, string[] Strings, TlbType[] Types,
    (uint TrackType, uint ClipType)[] PairTypes,
    (uint TrackEntry, uint ClipIndex, uint Name)[] Labels,
    (uint Type, uint Pair, uint Value, uint Clips)[] Tracks,
    (uint Pair, uint Value, uint Start, uint End)[] Clips)
{
    public string String(uint index) => Strings[index];
    public string TypeName(uint index) =>
        String(Types[(int)index].Namespace) + "." + String(Types[(int)index].Name) + ", " + String(Types[(int)index].Assembly);
}

public sealed class TlbContainer
{
    public const uint HotMagic = 0x31424C54;
    public const uint HotVersion = 6;
    public const uint HotVersionMin = 5;
    public const uint TailMagic = 0x4D42544C;
    public const uint TailVersion = 2;
    public const uint HeaderBytes = 64;
    public const uint HeaderBytesV6 = 80;
    public const uint PairEntryBytes = 48;
    public const uint StageEntryBytes = 16;
    public const uint StepBytes = 8;
    public const uint RowBytes = 24;

    public uint Version;
    public uint Loops;
    public uint Duration;
    public uint TrackCount;
    public uint PairOffset;
    public uint StageOffset;
    public uint PoolOffset;
    public uint FrameOffset;
    public uint HotLength;
    public uint Bytes;
    public uint NextId;
    public uint Channel;
    public bool Hold;
    public TlbMarker[] Markers = [];
    public TlbPair[] Pairs = [];
    public TlbStage[] Stages = [];
    public TlbRow[] Rows = [];
    public TlbTail? Tail;
    public static TlbContainer Parse(byte[] file) => Parse(file.AsSpan());

    public static TlbContainer Parse(ReadOnlySpan<byte> f)
    {
        if (f.Length < HeaderBytes) throw new TlbFormatException("TLB truncated.");
        var version = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(4));
        if (BinaryPrimitives.ReadUInt32LittleEndian(f) != HotMagic || (version != HotVersionMin && version != HotVersion))
            throw new TlbFormatException("TLB magic or version invalid; rebake the asset with the current toolchain.");
        if (version == HotVersion && f.Length < HeaderBytesV6) throw new TlbFormatException("TLB truncated.");
        var c = new TlbContainer
        {
            Version = version,
            Loops = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(8)),
            Duration = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(12)),
            TrackCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(16)),
        };
        var stageCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(20));
        var pairCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(24));
        c.PairOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(28));
        c.StageOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(32));
        c.PoolOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(36));
        c.FrameOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(40));
        c.HotLength = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(44));
        c.Bytes = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(48));
        uint markerOffset = 0, markerCount = 0;
        if (version == HotVersion)
        {
            c.NextId = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(52));
            c.Channel = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(56));
            c.Hold = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(60)) != 0;
            markerOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(64));
            markerCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(68));
            if (c.Loops != 0 && c.NextId != 0) throw new TlbFormatException("TLB next is incompatible with loop:true.");
            if (c.Loops != 0 && c.Hold) throw new TlbFormatException("TLB hold is incompatible with loop:true.");
        }
        if (c.Duration > 65_535) throw new TlbFormatException($"header Duration {c.Duration} exceeds 65,535.");
        if (pairCount > 256) throw new TlbFormatException($"header PairCount {pairCount} exceeds 256.");
        if (c.Bytes != (uint)f.Length)
            throw new TlbFormatException($"header Bytes {c.Bytes} does not match the file length {f.Length}.");
        if (c.HotLength < 1 || c.HotLength > c.Bytes)
            throw new TlbFormatException($"header HotLength {c.HotLength} is outside [1, Bytes].");
        var minPair = version == HotVersion ? HeaderBytesV6 : HeaderBytes;
        if (c.PairOffset < minPair || c.PairOffset % 8 != 0)
            throw new TlbFormatException($"header PairOffset {c.PairOffset} is before {minPair} or unaligned.");
        foreach (var (name, value, min) in new[]
        {
            ("StageOffset", c.StageOffset, c.PairOffset + PairEntryBytes * pairCount),
            ("PoolOffset", c.PoolOffset, c.StageOffset + StageEntryBytes * stageCount),
            ("FrameOffset", c.FrameOffset, c.PoolOffset),
        })
        {
            if (value < min || value > c.HotLength || value % 8 != 0)
                throw new TlbFormatException($"header {name} {value} is out of order, unaligned, or beyond the hot section.");
        }

        c.Pairs = new TlbPair[pairCount];
        ulong previousKey = 0;
        for (uint i = 0; i < pairCount; i++)
        {
            var entry = c.PairOffset + PairEntryBytes * i;
            var key = BinaryPrimitives.ReadUInt64LittleEndian(f.Slice((int)entry));
            if (i > 0 && key <= previousKey)
                throw new TlbFormatException($"pair {i} key {key:X16} does not strictly ascend.");
            previousKey = key;
            var stride = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 8));
            if (stride < RowBytes || stride % 8 != 0)
                throw new TlbFormatException($"pair {i} SlotStride {stride} is under {RowBytes} or not 8-byte aligned.");
            var trackPoolOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 12));
            var trackPoolCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 16));
            var trackValueBytes = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 20));
            var clipPoolOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 24));
            var clipPoolCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 28));
            var clipValueBytes = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)entry + 32));
            if (trackPoolCount > 65_535 || clipPoolCount > 65_535)
                throw new TlbFormatException($"pair {i} pool count exceeds 65,535.");
            var trackPool = ReadPool(f, entry + trackPoolOffset, trackPoolCount, trackValueBytes, c, $"pair {i} track pool");
            var clipPool = ReadPool(f, entry + clipPoolOffset, clipPoolCount, clipValueBytes, c, $"pair {i} clip pool");
            c.Pairs[i] = new TlbPair(key, stride, BinaryPrimitives.ReadUInt64LittleEndian(f.Slice((int)entry + 40)),
                trackPoolOffset, trackPoolCount, trackValueBytes,
                clipPoolOffset, clipPoolCount, clipValueBytes, trackPool, clipPool);
        }

        c.Stages = new TlbStage[stageCount];
        var rows = new List<TlbRow>();
        var expectedEnd = 0u;
        for (uint i = 0; i < stageCount; i++)
        {
            var at = c.StageOffset + StageEntryBytes * i;
            var start = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at));
            var end = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at + 4));
            var programOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at + 8));
            var programCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at + 12));
            if (start != expectedEnd || end < start || end > c.Duration)
                throw new TlbFormatException($"stage {i} range [{start}, {end}) breaks contiguity from 0 to Duration {c.Duration}.");
            expectedEnd = end;
            if (programOffset % 8 != 0 || programOffset < c.StageOffset + StageEntryBytes * stageCount ||
                programOffset > c.HotLength || programOffset + StepBytes * programCount > c.HotLength)
                throw new TlbFormatException($"stage {i} program [{programOffset}, +{StepBytes * programCount}) is unaligned or outside the hot section.");
            var steps = new TlbStep[programCount];
            for (uint s = 0; s < programCount; s++)
            {
                var stepAt = programOffset + StepBytes * s;
                var slot = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)stepAt));
                var pair = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)stepAt + 4));
                if (pair >= pairCount)
                    throw new TlbFormatException($"stage {i} step {s} names pair {pair} beyond PairCount {pairCount}.");
                if (slot % 8 != 0 || slot < c.FrameOffset || slot + RowBytes > c.HotLength)
                    throw new TlbFormatException($"stage {i} step {s} slot {slot} is unaligned or outside the slot-row section.");
                if (c.Version == HotVersion && markerCount != 0 && slot >= markerOffset)
                    throw new TlbFormatException($"stage {i} step {s} slot {slot} must precede the marker section.");
                steps[s] = new TlbStep(slot, pair);
                rows.Add(ReadRow(f, slot, c.Pairs[(int)pair]));
            }
            c.Stages[i] = new TlbStage(start, end, programOffset, steps);
        }
        if (c.Duration == 0 && stageCount != 0)
            throw new TlbFormatException("a Duration 0 container must have zero stages.");
        if (expectedEnd != c.Duration)
            throw new TlbFormatException($"stages end at {expectedEnd}, not Duration {c.Duration}.");
        c.Rows = [.. rows];
        c.Markers = c.Version == HotVersion ? ReadMarkers(f, c, markerOffset, markerCount) : [];
        c.Tail = c.HotLength < c.Bytes ? ReadTail(f, c) : null;
        return c;
    }

    static TlbMarker[] ReadMarkers(ReadOnlySpan<byte> f, TlbContainer c, uint offset, uint count)
    {
        if (count == 0)
        {
            if (offset != 0) throw new TlbFormatException("TLB marker offset must be zero when count is zero.");
            return [];
        }
        if (offset < c.FrameOffset || offset % 8 != 0 || offset > c.HotLength)
            throw new TlbFormatException($"TLB marker section at {offset} is unaligned or outside the hot section.");
        var markers = new TlbMarker[count];
        var at = offset;
        for (uint i = 0; i < count; i++)
        {
            if (at + 16u > c.HotLength) throw new TlbFormatException($"TLB marker {i} entry exceeds the hot section.");
            var tick = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)at + 4));
            var key = BinaryPrimitives.ReadUInt64LittleEndian(f.Slice((int)at + 8));
            if (key == 0) throw new TlbFormatException($"TLB marker {i} type key invalid.");
            if (size == 0 || size > 4096) throw new TlbFormatException($"TLB marker {i} payload size out of range.");
            if (tick >= c.Duration && c.Duration != 0) throw new TlbFormatException($"TLB marker {i} tick outside duration.");
            if (at + 16u + ((size + 3u) & ~3u) > c.HotLength) throw new TlbFormatException($"TLB marker {i} payload exceeds the hot section.");
            var payload = new byte[size];
            f.Slice((int)(at + 16u), (int)size).CopyTo(payload);
            markers[i] = new TlbMarker(tick, size, key, payload);
            at += 16u + ((size + 3u) & ~3u);
        }
        if (at > c.HotLength) throw new TlbFormatException("TLB marker section out of bounds.");
        return markers;
    }

    static TlbPool ReadPool(ReadOnlySpan<byte> f, uint address, uint count, uint valueBytes, TlbContainer c, string what)
    {
        if (count == 0) return new TlbPool(address, 0, valueBytes, []);
        if (valueBytes == 0 || address % 8 != 0 || address < c.PoolOffset || address + valueBytes * count > c.HotLength)
            throw new TlbFormatException($"{what} at {address} is unaligned or outside the hot section.");
        var values = new byte[count][];
        for (uint i = 0; i < count; i++)
        {
            var v = new byte[valueBytes];
            f.Slice((int)(address + valueBytes * i), (int)valueBytes).CopyTo(v);
            values[i] = v;
        }
        return new TlbPool(address, count, valueBytes, values);
    }

    static TlbRow ReadRow(ReadOnlySpan<byte> f, uint slot, TlbPair pair)
    {
        var row = new TlbRow(
            BinaryPrimitives.ReadUInt16LittleEndian(f.Slice((int)slot)),
            BinaryPrimitives.ReadUInt16LittleEndian(f.Slice((int)slot + 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(f.Slice((int)slot + 4)),
            f[(int)slot + 6],
            BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)slot + 8)),
            BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)slot + 12)),
            BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)slot + 16)),
            BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)slot + 20)));
        if (row.TrackValueIndex >= pair.TrackPoolCount)
            throw new TlbFormatException($"slot row at {slot}: TrackValueIndex {row.TrackValueIndex} is beyond the track pool count {pair.TrackPoolCount}.");
        if (row.FirstValueIndex >= pair.ClipPoolCount)
            throw new TlbFormatException($"slot row at {slot}: FirstValueIndex {row.FirstValueIndex} is beyond the clip pool count {pair.ClipPoolCount}.");
        if (row.Blends == (row.SecondValueIndex == TlbRow.NoClipValue))
            throw new TlbFormatException($"slot row at {slot}: a blend window requires a second value index, and a second index requires a blend window.");
        if (row.Blends && row.SecondValueIndex >= pair.ClipPoolCount)
            throw new TlbFormatException($"slot row at {slot}: SecondValueIndex {row.SecondValueIndex} is beyond the clip pool count {pair.ClipPoolCount}.");
        return row;
    }

    static TlbTail ReadTail(ReadOnlySpan<byte> f, TlbContainer c)
    {
        var tailStart = c.HotLength;
        var tailLength = c.Bytes - c.HotLength;
        if (tailLength < 64) throw new TlbFormatException("TLB metadata tail truncated.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart)) != TailMagic ||
            BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 4)) != TailVersion)
            throw new TlbFormatException("TLB metadata tail magic or version invalid; rebake the asset.");
        var stringCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 8));
        var stringIndexOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 12));
        var stringBlobOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 16));
        var stringBlobLen = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 20));
        var typeCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 24));
        var typeTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 28));
        var pairTypeCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 32));
        var pairTypeTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 36));
        var labelCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 40));
        var labelTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 44));
        var trackCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 48));
        var trackTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 52));
        var clipCount = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 56));
        var clipTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)tailStart + 60));
        if (trackCount != c.TrackCount)
            throw new TlbFormatException($"tail TrackCount {trackCount} does not match TrackCount {c.TrackCount}.");
        TailEnd(trackTableOffset, 16, trackCount, "track");
        TailEnd(clipTableOffset, 16, clipCount, "clip");
        if (pairTypeCount != (uint)c.Pairs.Length)
            throw new TlbFormatException($"tail PairTypeCount {pairTypeCount} does not match PairCount {c.Pairs.Length}.");
        uint TailEnd(uint offset, uint width, uint count, string what)
        {
            if (offset + width * count > tailLength)
                throw new TlbFormatException($"tail {what} table [{offset}, +{width * count}) exceeds the tail.");
            return offset + width * count;
        }
        var blob = f.Slice((int)(tailStart + stringBlobOffset), (int)stringBlobLen);
        var strings = new string[stringCount];
        for (uint i = 0; i < stringCount; i++)
        {
            var from = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + stringIndexOffset + 4 * i)));
            var to = i + 1 < stringCount
                ? BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + stringIndexOffset + 4 * (i + 1))))
                : stringBlobLen;
            if (from > to || to > stringBlobLen)
                throw new TlbFormatException($"tail string {i} span [{from}, {to}) is outside the blob.");
            strings[i] = Encoding.UTF8.GetString(blob.Slice((int)from, (int)(to - from)));
        }
        var types = new TlbType[typeCount];
        for (uint i = 0; i < typeCount; i++)
        {
            var ns = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + typeTableOffset + 12 * i)));
            var name = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + typeTableOffset + 12 * i + 4)));
            var assembly = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + typeTableOffset + 12 * i + 8)));
            if (ns >= stringCount || name >= stringCount || assembly >= stringCount)
                throw new TlbFormatException($"tail type {i} names a string beyond StringCount {stringCount}.");
            types[i] = new TlbType(ns, name, assembly);
        }
        var pairTypes = new (uint, uint)[pairTypeCount];
        for (uint i = 0; i < pairTypeCount; i++)
        {
            var track = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + pairTypeTableOffset + 8 * i)));
            var clip = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + pairTypeTableOffset + 8 * i + 4)));
            if (track >= typeCount || clip >= typeCount)
                throw new TlbFormatException($"tail pair type {i} names a type beyond TypeCount {typeCount}.");
            pairTypes[i] = (track, clip);
        }
        var labels = new (uint, uint, uint)[labelCount];
        for (uint i = 0; i < labelCount; i++)
        {
            var trackEntry = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + labelTableOffset + 12 * i)));
            var clipIndex = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + labelTableOffset + 12 * i + 4)));
            var name = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice((int)(tailStart + labelTableOffset + 12 * i + 8)));
            if (name >= stringCount)
                throw new TlbFormatException($"tail label {i} names a string beyond StringCount {stringCount}.");
            labels[i] = (trackEntry, clipIndex, name);
        }
        var tracks = new (uint, uint, uint, uint)[trackCount];
        var covered = 0ul;
        for (uint i = 0; i < trackCount; i++)
        {
            var at = (int)(tailStart + trackTableOffset + 16 * i);
            var row = (BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at)), BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 8)), BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 12)));
            if (row.Item1 >= typeCount || (row.Item2 == uint.MaxValue) != (row.Item4 == 0) || row.Item2 != uint.MaxValue && row.Item2 >= pairTypeCount)
                throw new TlbFormatException($"tail track {i} names a type or pair out of range.");
            covered += row.Item4;
            tracks[i] = row;
        }
        if (covered != clipCount)
            throw new TlbFormatException($"tail track rows cover {covered} clips, the clip table has {clipCount}.");
        var clips = new (uint, uint, uint, uint)[clipCount];
        for (uint i = 0; i < clipCount; i++)
        {
            var at = (int)(tailStart + clipTableOffset + 16 * i);
            var row = (BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at)), BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 8)), BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(at + 12)));
            if (row.Item1 >= pairTypeCount || row.Item3 >= row.Item4)
                throw new TlbFormatException($"tail clip {i} names a pair out of range or an empty window.");
            clips[i] = row;
        }
        if (stringIndexOffset != 64)
            throw new TlbFormatException("tail StringIndexOffset is not 64.");
        var declaredEnd = new[]
        {
            TailEnd(stringIndexOffset, 4, stringCount, "string index"),
            TailEnd(stringBlobOffset, 1, stringBlobLen, "string blob"),
            TailEnd(typeTableOffset, 12, typeCount, "type"),
            TailEnd(pairTypeTableOffset, 8, pairTypeCount, "pair type"),
            TailEnd(labelTableOffset, 12, labelCount, "label"),
            TailEnd(trackTableOffset, 16, trackCount, "track"),
            TailEnd(clipTableOffset, 16, clipCount, "clip"),
        }.Max();
        if (declaredEnd > tailLength)
            throw new TlbFormatException("tail tables exceed the tail length.");
        return new TlbTail(tailStart, strings, types, pairTypes, labels, tracks, clips);
    }
}
