using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl;

namespace Tl.FusedAdvanceProbe;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct EdgeClip(float Amount);

public readonly record struct EdgeTrack(float Scale) : IBlend<EdgeClip>
{
    public void Blend(in EdgeClip first, in EdgeClip second, float factor, out EdgeClip result)
        => result = new EdgeClip(first.Amount + (second.Amount - first.Amount) * factor);
}

[StructLayout(LayoutKind.Sequential)]
struct BakedSlot<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged
{
    public TTrack Track;
    public TClip First;
    public TClip Second;
    public uint WindowStart;
    public uint WindowEnd;
    public uint FactorStart;
    public uint FactorSpan;
    public byte TrackIndex;
}

sealed class Baker
{
    sealed class BakedClip
    {
        public required uint Start { get; init; }
        public required uint End { get; init; }
        public required object Value { get; init; }
    }

    abstract class BakedTrack
    {
        public ulong Key;
        public uint Stride;
        public byte Index;
        public readonly List<BakedClip> Clips = [];

        public abstract void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan);
    }

    sealed class BakedTrack<TTrack, TClip> : BakedTrack
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        public required TTrack TrackValue { get; init; }

        public override void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan)
        {
            var slot = new BakedSlot<TTrack, TClip>
            {
                Track = TrackValue,
                First = (TClip)first!.Value,
                Second = second is null ? default : (TClip)second.Value,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                FactorStart = factorStart,
                FactorSpan = factorSpan,
                TrackIndex = Index,
            };
            MemoryMarshal.Write(bytes.AsSpan(offset), in slot);
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
            Stride = (uint)((Unsafe.SizeOf<BakedSlot<TTrack, TClip>>() + 15) & ~15),
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

        var pairs = _tracks.Select(track => (track.Key, track.Stride)).Distinct().OrderBy(pair => pair.Key).ToArray();
        var pairIndex = new Dictionary<ulong, int>();
        for (var index = 0; index < pairs.Length; index++)
            pairIndex[pairs[index].Key] = index;

        var pairOffset = 48u;
        var stageOffset = pairOffset + 16u * (uint)pairs.Length;
        var programBase = stageOffset + 16u * (uint)stageList.Count;
        var stepCount = stageList.Sum(active => active.Count);
        var frameOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;

        var occurrences = new List<(uint Offset, int Pair, BakedTrack Track, int Stage)>();
        var cursor = frameOffset;
        for (var stage = 0; stage < stageList.Count; stage++)
            foreach (var track in stageList[stage])
            {
                occurrences.Add((cursor, pairIndex[track.Key], track, stage));
                cursor += pairs[pairIndex[track.Key]].Stride;
            }

        var bytes = new byte[cursor];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), _loops ? 1u : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)_tracks.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageList.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairs.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), (uint)bytes.Length);

        for (var index = 0; index < pairs.Length; index++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index), pairs[index].Key);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index + 8), pairs[index].Stride);
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

            occurrence.Track.Write(bytes, (int)occurrence.Offset, clips[0], clips.Length == 2 ? clips[1] : null, windowStart, windowEnd, factorStart, factorSpan);
        }

        return bytes;
    }
}

public static unsafe class Host
{
    public const int Duration = 1024;

    public static readonly TimelineAsset LoopingAsset = TimelineAsset.Load(new Baker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 600, new LaneClip(1.25f))
        .Clip(0, 600, 1024, new LaneClip(-0.5f))
        .Looping()
        .Bake());

    public static readonly TimelineAsset FiniteAsset = TimelineAsset.Load(new Baker()
        .Track<EdgeTrack, EdgeClip>(new EdgeTrack(2f))
        .Clip(0, 0, 1024, new EdgeClip(0.75f))
        .Bake());

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteLane, &BindFloat);
        PairRuntime<EdgeTrack, EdgeClip>.Consume(&ExecuteEdge, &BindFloat);
    }

    public static void BindLane() => BakedLane<LaneTrack, LaneClip>.Bind(LoopingAsset);

    public static void BindFinite() => BakedLane<EdgeTrack, EdgeClip>.Bind(FiniteAsset);

    public static TimelineSet<LaneTrack, LaneClip> BuildSet(int timelines)
    {
        var set = new TimelineSet<LaneTrack, LaneClip>();
        for (var i = 0; i < timelines; i++) set.Add(LoopingAsset);
        return set;
    }

    static void ExecuteLane(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    static void ExecuteEdge(byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
    {
        EdgeClip scratch = default;
        var frame = TickFrame.ToFrame<EdgeTrack, EdgeClip>(slot, tick, flags, ref scratch);
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
