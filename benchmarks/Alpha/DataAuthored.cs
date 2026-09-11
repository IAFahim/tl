using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Tl;

[StructLayout(LayoutKind.Sequential)]
internal struct AuthoredSlot<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged
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

internal sealed class DataAuthoredBaker
{
    internal sealed class BakedClip
    {
        public required uint Start { get; init; }
        public required uint End { get; init; }
        public required object Value { get; init; }
    }

    internal abstract class BakedTrack
    {
        public ulong Key;
        public uint Stride;
        public byte Index;
        public readonly List<BakedClip> Clips = [];

        public abstract void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan);
    }

    internal sealed class BakedTrack<TTrack, TClip> : BakedTrack
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        public required TTrack TrackValue { get; init; }

        public override void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan)
        {
            var slot = new AuthoredSlot<TTrack, TClip>
            {
                Track = TrackValue,
                First = (TClip)first!.Value!,
                Second = second is null ? default : (TClip)second.Value!,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                FactorStart = factorStart,
                FactorSpan = factorSpan,
                TrackIndex = Index,
            };
            MemoryMarshal.Write(bytes.AsSpan(offset), in slot);
        }
    }

    private readonly List<BakedTrack> _tracks = [];
    private bool _loops;

    public DataAuthoredBaker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        _tracks.Add(new BakedTrack<TTrack, TClip>
        {
            TrackValue = value,
            Key = PairRuntime<TTrack, TClip>.Key,
            Stride = (uint)((Unsafe.SizeOf<AuthoredSlot<TTrack, TClip>>() + 15) & ~15),
            Index = (byte)_tracks.Count,
        });
        return this;
    }

    public DataAuthoredBaker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
    {
        _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
        return this;
    }

    public DataAuthoredBaker Looping()
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

internal sealed class DataAuthoredCase : IDisposable
{
    internal const int Operations = 4096;
    private readonly int[] _deltas = new int[Operations];
    private readonly TimelineAsset _asset;
    private readonly TimelineComponent[] _rows = new TimelineComponent[1];
    private readonly Accumulator[] _accumulators = new Accumulator[1];
    private readonly FirstInput[] _first = [new(2)];
    private readonly SecondInput[] _second = [new(3)];
    private readonly ThirdInput[] _third = [new(-5)];
    private readonly TickPattern _pattern;

    internal DataAuthoredCase(TimelineShape shape, TickPattern pattern)
    {
        Shape = shape;
        _pattern = pattern;
        TickPatterns.Fill(_deltas, pattern);
        _asset = TimelineAsset.Load(Bake(shape));
        _rows[0] = new TimelineComponent(_asset.Reference);
    }

    internal TimelineShape Shape { get; }

    internal BenchmarkReceipt DataAuthored()
    {
        _rows[0] = new TimelineComponent(_asset.Reference);
        _accumulators[0] = default;
        var query = Timeline.Rows(_rows).Read(_first).Read(_second).Read(_third).Write(_accumulators);
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
        return Direct.Capture(_rows, _accumulators);
    }

    public void Dispose() => _asset.Dispose();

    internal static byte[] Bake(TimelineShape shape)
    {
        var baker = new DataAuthoredBaker();
        switch (shape)
        {
            case TimelineShape.OneTrack:
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                baker.Looping();
                break;
            case TimelineShape.ThreeTracks:
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                baker.Track<BetaTrack, BetaClip>(new BetaTrack(2)).Clip(1, 0u, 64u, new BetaClip(2));
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(3)).Clip(2, 0u, 64u, new AlphaClip(3));
                baker.Looping();
                break;
            case TimelineShape.Blend:
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(5)).Clip(0, 0u, 64u, new AlphaClip(1)).Clip(0, 0u, 64u, new AlphaClip(9));
                baker.Looping();
                break;
            case TimelineShape.SixteenTracks:
                for (var track = 1; track <= 16; track++)
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                baker.Looping();
                break;
            case TimelineShape.TwoHundredFiftySixTracks:
                for (var track = 1; track <= 256; track++)
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                baker.Looping();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
        return baker.Bake();
    }
}

[Config(typeof(AlphaConfig))]
public class DataAuthoredQueryBenchmarks
{
    private ShapeCase _alpha = null!;
    private DataAuthoredCase _facade = null!;

    [Params(
        TimelineShape.OneTrack,
        TimelineShape.ThreeTracks,
        TimelineShape.Blend,
        TimelineShape.SixteenTracks,
        TimelineShape.TwoHundredFiftySixTracks)]
    public TimelineShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _alpha = new ShapeCase(Shape, TickPattern.Forward);
        _facade = new DataAuthoredCase(Shape, TickPattern.Forward);
        ScalarCatalogQueryBenchmarks.Require(DirectShape(), GeneratedShape(), nameof(GeneratedShape));
        ScalarCatalogQueryBenchmarks.Require(DirectShape(), DataAuthoredFacade(), nameof(DataAuthoredFacade));
    }

    [GlobalCleanup]
    public void Cleanup() => _facade.Dispose();

    [Benchmark(Baseline = true, OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt DirectShape() => _alpha.Direct();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt GeneratedShape() => _alpha.Generated();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt DataAuthoredFacade() => _facade.DataAuthored();
}
