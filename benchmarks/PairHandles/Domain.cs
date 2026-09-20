using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl;

namespace PairHandles;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}



sealed class Baker
{
        internal sealed class BakedClip
    {
        public required uint Start { get; init; }
        public required uint End { get; init; }
        public required object Value { get; init; }
        public ushort PoolIndex { get; set; }
    }

    internal abstract class BakedTrack
    {
        public ulong Key;
        public byte Index;
        public ushort TrackValueIndex;
        public readonly List<BakedClip> Clips = [];

        public abstract int TrackValueBytes { get; }
        public abstract int ClipValueBytes { get; }
        public abstract byte[] TrackImage();
        public abstract byte[] ClipImage(BakedClip clip);
    }

    internal sealed class BakedTrack<TTrack, TClip> : BakedTrack
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        public required TTrack TrackValue { get; init; }

        public override int TrackValueBytes => Unsafe.SizeOf<TTrack>();
        public override int ClipValueBytes => Unsafe.SizeOf<TClip>();

        public override byte[] TrackImage()
        {
            var value = TrackValue;
            var image = new byte[TrackValueBytes];
            MemoryMarshal.Write(image, in value);
            return image;
        }

        public override byte[] ClipImage(BakedClip clip)
        {
            var value = (TClip)clip.Value;
            var image = new byte[ClipValueBytes];
            MemoryMarshal.Write(image, in value);
            return image;
        }
    }

    internal sealed class ImageBytesComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? left, byte[]? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null || left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        public int GetHashCode(byte[] image)
        {
            var hash = new HashCode();
            hash.AddBytes(image);
            return hash.ToHashCode();
        }
    }
    readonly List<BakedTrack> _tracks = [];
    bool _loops;

    public Baker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        _tracks.Add(new BakedTrack<TTrack, TClip>
        {
            TrackValue = value,
            Key = PairRuntime<TTrack, TClip>.Key,
            Index = (byte)_tracks.Count,
        });
        return this;
    }

    public Baker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
    {
        _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
        return this;
    }

    public Baker Looping()
    {
        _loops = true;
        return this;
    }


    static int CompareImages(byte[] left, byte[] right)
    {
        var byLength = left.Length.CompareTo(right.Length);
        if (byLength != 0) return byLength;
        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return left[i].CompareTo(right[i]);
        return 0;
    }
    public byte[] Bake()
    {
        var duration = 0u;
        foreach (var track in _tracks)
            foreach (var clip in track.Clips)
                duration = Math.Max(duration, clip.End);

        var cuts = new SortedSet<uint>();
        if (duration != 0)
        {
            cuts.Add(0u);
            cuts.Add(duration);
            foreach (var track in _tracks)
                foreach (var clip in track.Clips)
                {
                    cuts.Add(clip.Start);
                    cuts.Add(clip.End);
                }
        }

        var boundaries = cuts.ToArray();
        var stageList = new List<List<BakedTrack>>();
        var stageEdges = new List<(uint Start, uint End)>();
        for (var region = 0; region + 1 < boundaries.Length; region++)
        {
            var edge = boundaries[region];
            var active = new List<BakedTrack>();
            foreach (var track in _tracks)
                if (track.Clips.Any(clip => clip.Start <= edge && edge < clip.End))
                    active.Add(track);
            stageList.Add(active);
            stageEdges.Add((edge, boundaries[region + 1]));
        }

        var pairKeys = _tracks.Select(track => track.Key).Distinct().OrderBy(key => key).ToArray();
        var pairIndex = new Dictionary<ulong, int>();
        for (var index = 0; index < pairKeys.Length; index++)
            pairIndex[pairKeys[index]] = index;

        var trackPools = new List<byte[]>[pairKeys.Length];
        var trackIndices = new Dictionary<byte[], ushort>[pairKeys.Length];
        var clipPools = new List<byte[]>[pairKeys.Length];
        var clipIndices = new Dictionary<byte[], ushort>[pairKeys.Length];
        var trackValueBytes = new int[pairKeys.Length];
        var clipValueBytes = new int[pairKeys.Length];
        for (var index = 0; index < pairKeys.Length; index++)
        {
            var members = _tracks.Where(track => pairIndex[track.Key] == index).ToList();
            var trackUnique = members.Select(track => track.TrackImage()).Distinct(new ImageBytesComparer()).ToList();
            var clipUnique = members.SelectMany(track => track.Clips.Select(track.ClipImage)).Distinct(new ImageBytesComparer()).ToList();
            trackUnique.Sort(CompareImages);
            clipUnique.Sort(CompareImages);
            if (trackUnique.Count > 65535 || clipUnique.Count > 65535)
                throw new InvalidOperationException("Value pool overflow: ushort slot indices hold at most 65,535 unique values per pool.");
            trackValueBytes[index] = members[0].TrackValueBytes;
            clipValueBytes[index] = members[0].ClipValueBytes;
            trackPools[index] = trackUnique;
            clipPools[index] = clipUnique;
            trackIndices[index] = new Dictionary<byte[], ushort>(trackUnique.Count, new ImageBytesComparer());
            clipIndices[index] = new Dictionary<byte[], ushort>(clipUnique.Count, new ImageBytesComparer());
            for (var i = 0; i < trackUnique.Count; i++) trackIndices[index][trackUnique[i]] = (ushort)i;
            for (var i = 0; i < clipUnique.Count; i++) clipIndices[index][clipUnique[i]] = (ushort)i;
        }

        foreach (var track in _tracks)
        {
            var index = pairIndex[track.Key];
            track.TrackValueIndex = trackIndices[index][track.TrackImage()];
            foreach (var clip in track.Clips)
                clip.PoolIndex = clipIndices[index][track.ClipImage(clip)];
        }

        var pairOffset = 64u;
        var stageOffset = pairOffset + 48u * (uint)pairKeys.Length;
        var programBase = stageOffset + 16u * (uint)stageList.Count;
        var stepCount = stageList.Sum(active => active.Count);
        var poolOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;

        var trackPoolOffsets = new uint[pairKeys.Length];
        var clipPoolOffsets = new uint[pairKeys.Length];
        var poolCursor = poolOffset;
        for (var index = 0; index < pairKeys.Length; index++)
        {
            var pairAddress = pairOffset + 48u * (uint)index;
            trackPoolOffsets[index] = poolCursor - pairAddress;
            poolCursor = (poolCursor + (uint)trackPools[index].Count * (uint)trackValueBytes[index] + 15u) & ~15u;
            clipPoolOffsets[index] = poolCursor - pairAddress;
            poolCursor = (poolCursor + (uint)clipPools[index].Count * (uint)clipValueBytes[index] + 15u) & ~15u;
        }
        var frameOffset = (poolCursor + 15u) & ~15u;

        var occurrences = new List<(uint Offset, int Pair, BakedTrack Track, int Stage)>();
        var cursor = frameOffset;
        for (var stage = 0; stage < stageList.Count; stage++)
            foreach (var track in stageList[stage])
            {
                occurrences.Add((cursor, pairIndex[track.Key], track, stage));
                cursor += 24u;
            }

        var bytes = new byte[cursor];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 3u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), _loops ? 1u : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)_tracks.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageList.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairKeys.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), poolOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(40), frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), (uint)bytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), (uint)bytes.Length);

        for (var index = 0; index < pairKeys.Length; index++)
        {
            var at = (int)pairOffset + 48 * index;
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(at), pairKeys[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), 24u);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), trackPoolOffsets[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), (uint)trackPools[index].Count);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), (uint)trackValueBytes[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 24), clipPoolOffsets[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 28), (uint)clipPools[index].Count);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 32), (uint)clipValueBytes[index]);
        }

        var programOffset = programBase;
        for (var stage = 0; stage < stageList.Count; stage++)
        {
            var at = stageOffset + 16u * (uint)stage;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at), stageEdges[stage].Start);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 4), stageEdges[stage].End);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 8), programOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 12), (uint)stageList[stage].Count);
            foreach (var occurrence in occurrences.Where(item => item.Stage == stage))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset), occurrence.Offset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset + 4), (uint)occurrence.Pair);
                programOffset += 8u;
            }
        }

        for (var index = 0; index < pairKeys.Length; index++)
        {
            var at = (int)(pairOffset + 48u * (uint)index + trackPoolOffsets[index]);
            foreach (var image in trackPools[index])
            {
                image.CopyTo(bytes, at);
                at += image.Length;
            }
            at = (int)(pairOffset + 48u * (uint)index + clipPoolOffsets[index]);
            foreach (var image in clipPools[index])
            {
                image.CopyTo(bytes, at);
                at += image.Length;
            }
        }

        foreach (var occurrence in occurrences)
        {
            var edge = stageEdges[occurrence.Stage].Start;
            var clips = occurrence.Track.Clips
                .Select((clip, index) => (clip, index))
                .Where(item => item.clip.Start <= edge && edge < item.clip.End)
                .OrderBy(item => item.clip.Start).ThenBy(item => item.index)
                .Select(item => item.clip)
                .ToArray();
            var windowStart = clips.Min(clip => clip.Start);
            var windowEnd = clips.Max(clip => clip.End);
            var factorStart = 0u;
            var factorSpan = 0u;
            if (clips.Length == 2)
            {
                factorStart = Math.Max(clips[0].Start, clips[1].Start);
                factorSpan = Math.Min(clips[0].End, clips[1].End) - factorStart;
            }

            var at = (int)occurrence.Offset;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), occurrence.Track.TrackValueIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 2), clips[0].PoolIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 4), clips.Length == 2 ? clips[1].PoolIndex : (ushort)0xFFFF);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), windowStart);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), windowEnd);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), factorStart);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), factorSpan);
            bytes[at + 6] = occurrence.Track.Index;
        }

        return bytes;
    }
}

public static unsafe class Host
{
    public const int Duration = 1024;
    public const int Variants = 8;

    public static readonly TimelineAsset LoopingAsset = TimelineAsset.Of(TimelineAsset.Load(new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 600, new LaneClip(1.25f))
        .Clip(0, 600, 1024, new LaneClip(-0.5f))
        .Looping()
        .Bake()));

    public static readonly TimelineAsset[] VariantAssets = BuildVariants();

    static TimelineAsset[] BuildVariants()
    {
        var assets = new TimelineAsset[Variants];
        for (var variant = 0; variant < Variants; variant++)
        {
            var scale = 1f + variant * 0.25f;
            var split = 400 + variant * 80;
            assets[variant] = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                .Track<LaneTrack, LaneClip>(new LaneTrack(scale))
                .Clip(0, 0, (uint)split, new LaneClip(1.25f))
                .Clip(0, (uint)split, Duration, new LaneClip(-0.5f))
                .Looping()
                .Bake()));
        }
        return assets;
    }

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteLane, &BindFloat);
    }

    public static ushort SlotGoldLane() => LoopingAsset.Index;

    public static ushort[] BindBank()
    {
        var handles = new ushort[Variants];
        for (var variant = 0; variant < Variants; variant++)
            handles[variant] = VariantAssets[variant].Index;
        return handles;
    }

    static void ExecuteLane(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }


    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }
}

public enum Clock
{
    Uniform,
    Waves,
    Staggered,
    Clamped,
}

public static class Seeds
{
    public static ushort[] Positions(int rows, Clock clock)
    {
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = clock switch
            {
                Clock.Uniform => 5,
                Clock.Waves => (ushort)(i / 100 % Host.Duration),
                Clock.Staggered => (ushort)(i % Host.Duration),
                _ => (ushort)(i % (Host.Duration + 1)),
            };
        return positions;
    }

    public static ushort[] Ids(int rows, int timelines)
    {
        var ids = new ushort[rows];
        for (var i = 0; i < rows; i++) ids[i] = (ushort)(i % timelines);
        return ids;
    }

    public static float[] Effects(int rows)
    {
        var effects = new float[rows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < rows; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return effects;
    }
}
