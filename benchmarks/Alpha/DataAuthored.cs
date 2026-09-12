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

public readonly record struct UnboundTrack(int Code) : IBlend<UnboundClip>
{
    public void Blend(in UnboundClip first, in UnboundClip second, float factor, out UnboundClip result)
        => result = first;
}

public readonly record struct UnboundClip(int Amount);

public readonly record struct NoOpTrack(int Code) : IBlend<NoOpClip>
{
    public void Blend(in NoOpClip first, in NoOpClip second, float factor, out NoOpClip result)
        => result = first;
}

public readonly record struct NoOpClip(int Amount);

internal enum DataAuthoredMode
{
    Standard,
    SelectOnly,
    NoDispatch,
}

internal sealed class DataAuthoredCase : IDisposable
{
    internal const int Operations = 4096;
    private readonly int[] _deltas = new int[Operations];
    private readonly TimelineAsset _asset;
    private readonly TimelineComponent[] _rows = new TimelineComponent[1];
    private readonly Accumulator[] _accumulators = new Accumulator[1];
    private readonly TickPattern _pattern;

    static unsafe DataAuthoredCase()
    {
        PairRuntime<NoOpTrack, NoOpClip>.Consume(&NoOpExecute, &NoOpBind);
    }

    private static unsafe void NoOpBind(ulong* keys, int keyCount, byte* indices)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Accumulator>.Value)
            {
                indices[0] = (byte)(i + 1);
                break;
            }
        }
    }

    private static unsafe void NoOpExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
    }

    internal DataAuthoredCase(TimelineShape shape, TickPattern pattern, DataAuthoredMode mode = DataAuthoredMode.Standard, byte[]? baked = null)
    {
        Shape = shape;
        _pattern = pattern;
        TickPatterns.Fill(_deltas, pattern);
        _asset = TimelineAsset.Load(baked ?? Bake(shape, mode));
        _rows[0] = new TimelineComponent(_asset.Reference);
    }

    internal TimelineShape Shape { get; }

    internal BenchmarkReceipt DataAuthored()
    {
        _rows[0] = new TimelineComponent(_asset.Reference);
        _accumulators[0] = default;
        var query = Timeline.Rows(_rows).Write(_accumulators);
        if (_pattern == TickPattern.Forward)
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index);
        else
            for (var index = 0; index < _deltas.Length; index++)
                query.Tick((uint)index, _deltas[index]);
        return Direct.Capture(_rows, _accumulators);
    }

    public void Dispose() => _asset.Dispose();

    internal static byte[] Bake(TimelineShape shape, DataAuthoredMode mode = DataAuthoredMode.Standard)
    {
        var baker = new DataAuthoredBaker();
        switch (shape)
        {
            case TimelineShape.OneTrack:
                if (mode == DataAuthoredMode.SelectOnly)
                    baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(1)).Clip(0, 0u, 64u, new UnboundClip(1));
                else if (mode == DataAuthoredMode.NoDispatch)
                    baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(1)).Clip(0, 0u, 64u, new NoOpClip(1));
                else
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                baker.Looping();
                break;
            case TimelineShape.ThreeTracks:
                if (mode == DataAuthoredMode.SelectOnly)
                {
                    baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(1)).Clip(0, 0u, 64u, new UnboundClip(1));
                    baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(2)).Clip(1, 0u, 64u, new UnboundClip(2));
                    baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(3)).Clip(2, 0u, 64u, new UnboundClip(3));
                }
                else if (mode == DataAuthoredMode.NoDispatch)
                {
                    baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(1)).Clip(0, 0u, 64u, new NoOpClip(1));
                    baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(2)).Clip(1, 0u, 64u, new NoOpClip(2));
                    baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(3)).Clip(2, 0u, 64u, new NoOpClip(3));
                }
                else
                {
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                    baker.Track<BetaTrack, BetaClip>(new BetaTrack(2)).Clip(1, 0u, 64u, new BetaClip(2));
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(3)).Clip(2, 0u, 64u, new AlphaClip(3));
                }
                baker.Looping();
                break;
            case TimelineShape.Blend:
                if (mode == DataAuthoredMode.SelectOnly)
                    baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(5)).Clip(0, 0u, 64u, new UnboundClip(1)).Clip(0, 0u, 64u, new UnboundClip(9));
                else if (mode == DataAuthoredMode.NoDispatch)
                    baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(5)).Clip(0, 0u, 64u, new NoOpClip(1)).Clip(0, 0u, 64u, new NoOpClip(9));
                else
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(5)).Clip(0, 0u, 64u, new AlphaClip(1)).Clip(0, 0u, 64u, new AlphaClip(9));
                baker.Looping();
                break;
            case TimelineShape.SixteenTracks:
                for (var track = 1; track <= 16; track++)
                {
                    if (mode == DataAuthoredMode.SelectOnly)
                        baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(track)).Clip(track - 1, 0u, 64u, new UnboundClip(track));
                    else if (mode == DataAuthoredMode.NoDispatch)
                        baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(track)).Clip(track - 1, 0u, 64u, new NoOpClip(track));
                    else
                        baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                }
                baker.Looping();
                break;
            case TimelineShape.TwoHundredFiftySixTracks:
                for (var track = 1; track <= 256; track++)
                {
                    if (mode == DataAuthoredMode.SelectOnly)
                        baker.Track<UnboundTrack, UnboundClip>(new UnboundTrack(track)).Clip(track - 1, 0u, 64u, new UnboundClip(track));
                    else if (mode == DataAuthoredMode.NoDispatch)
                        baker.Track<NoOpTrack, NoOpClip>(new NoOpTrack(track)).Clip(track - 1, 0u, 64u, new NoOpClip(track));
                    else
                        baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                }
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
    private DataAuthoredCase _selectOnly = null!;
    private DataAuthoredCase _noDispatch = null!;

    [Params(
        TimelineShape.OneTrack,
        TimelineShape.ThreeTracks,
        TimelineShape.Blend,
        TimelineShape.SixteenTracks,
        TimelineShape.TwoHundredFiftySixTracks)]
    public TimelineShape Shape { get; set; }

    [Params(TickPattern.Forward, TickPattern.Alternating)]
    public TickPattern Pattern { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _alpha = new ShapeCase(Shape, Pattern);
        _facade = new DataAuthoredCase(Shape, Pattern);
        _selectOnly = new DataAuthoredCase(Shape, Pattern, DataAuthoredMode.SelectOnly);
        _noDispatch = new DataAuthoredCase(Shape, Pattern, DataAuthoredMode.NoDispatch);
        ScalarCatalogQueryBenchmarks.Require(DirectShape(), GeneratedShape(), nameof(GeneratedShape));
        ScalarCatalogQueryBenchmarks.Require(DirectShape(), DataAuthoredFacade(), nameof(DataAuthoredFacade));
        _ = FacadeSelectOnly();
        _ = FacadeNoDispatch();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _facade.Dispose();
        _selectOnly.Dispose();
        _noDispatch.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt DirectShape() => _alpha.Direct();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt GeneratedShape() => _alpha.Generated();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt DataAuthoredFacade() => _facade.DataAuthored();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt FacadeSelectOnly() => _selectOnly.DataAuthored();

    [Benchmark(OperationsPerInvoke = DataAuthoredCase.Operations)]
    public BenchmarkReceipt FacadeNoDispatch() => _noDispatch.DataAuthored();
}
