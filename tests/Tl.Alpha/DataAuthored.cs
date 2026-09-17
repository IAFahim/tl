using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl;

internal readonly record struct DamageClip(float Amount);
internal readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct HealClip(float Amount);
internal readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
{
    public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct TandemClip(float Amount);
internal readonly record struct TandemTrack(float Multiplier) : IBlend<TandemClip>
{
    public void Blend(in TandemClip first, in TandemClip second, float factor, out TandemClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct ImpureClip(float Amount);
internal readonly record struct ImpureTrack(float Multiplier) : IBlend<ImpureClip>
{
    public void Blend(in ImpureClip first, in ImpureClip second, float factor, out ImpureClip result) => result = first;
}

internal readonly struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct HealJob : ITimelineJob<HealTrack, HealClip>
{
    public static void Execute(in Frame<HealTrack, HealClip> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct TandemFirstJob : ITimelineJob<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct TandemSecondJob : ITimelineJob<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref float vitality)
        => vitality += frame.Direction * 7f;
}

internal readonly struct ImpureJob : ITimelineJob<ImpureTrack, ImpureClip>
{
    public static void Execute(in Frame<ImpureTrack, ImpureClip> frame, ref float vitality)
        => vitality *= 2f;
}


internal sealed class AlphaBaker
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
            var value = (TClip)clip.Value!;
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
    private readonly List<BakedTrack> _tracks = [];
    private bool _loops;

    public AlphaBaker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        _tracks.Add(new BakedTrack<TTrack, TClip>
        {
            TrackValue = value,
            Key = PairRuntime<TTrack, TClip>.Key,
            Index = (byte)_tracks.Count,
        });
        return this;
    }

    public AlphaBaker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
    {
        _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
        return this;
    }

    public AlphaBaker Looping()
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
            for (var i = 0; i < trackUnique.Count; i++) trackIndices[index][(byte[])trackUnique[i]] = (ushort)i;
            for (var i = 0; i < clipUnique.Count; i++) clipIndices[index][(byte[])clipUnique[i]] = (ushort)i;
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
                cursor += 32u;
            }

        var bytes = new byte[cursor];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 2u);
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
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), 32u);
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
            bytes[at + 24] = occurrence.Track.Index;
        }

        return bytes;
    }
}

internal static class DataAuthoredReceipts
{
    static float Blend(float first, float second, uint tick, uint start, uint span)
    {
        if (span == 0) return first;
        var factor = span == 1 ? 0.5f : (tick - start) / (span - 1f);
        return first + (second - first) * factor;
    }

    internal static void All()
    {
        MovementLaw();
        WrapCounts();
        FoldAndBlend();
        RewindAndCatchUp();
        TimelineSets();
        Faults();
        Validation();
        Console.WriteLine("receipts: movement, wrap-count, fold+blend, rewind, catch-up, timeline sets, faults, validation PASS");
    }

    internal static void MovementLaw()
    {
        foreach (var looping in new[] { true, false })
        {
            var baker = new AlphaBaker()
                .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
                .Clip(0, 0u, 12u, new DamageClip(5f));
            if (looping)
                baker.Looping();
            using var asset = TimelineAsset.Load(baker.Bake());
            BakedLane<DamageTrack, DamageClip>.Bind(asset);
            Require(BakedLane<DamageTrack, DamageClip>.Effect(0) == -10f, "effect table matches the authored fold");
            var duration = BakedLane<DamageTrack, DamageClip>.Duration;
            var positions = new ushort[64];
            var values = new float[64];
            for (var i = 0; i < positions.Length; i++)
                positions[i] = (ushort)(i % (duration + 2));
            var oraclePositions = (ushort[])positions.Clone();
            var oracleValues = new float[64];

            for (var tick = 0; tick < 200; tick++)
            {
                var forward = tick % 3 != 2;
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, forward).Apply(values);
                for (var i = 0; i < positions.Length; i++)
                {
                    if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), duration, looping, !forward, out var next, out var timelineTick, out _))
                        continue;
                    oracleValues[i] += forward ? BakedLane<DamageTrack, DamageClip>.Effect(timelineTick) : BakedLane<DamageTrack, DamageClip>.InverseEffect(timelineTick);
                    oraclePositions[i] = next.Position;
                }
            }

            Require(positions.SequenceEqual(oraclePositions), "movement positions match the law");
            Require(values.SequenceEqual(oracleValues), "folded effects match the law");
        }
    }

    internal static void WrapCounts()
    {
        using var asset = TimelineAsset.Load(new AlphaBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 12u, new DamageClip(5f))
            .Looping()
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(asset);
        const ushort Duration = 12;
        const int Rows = 48;
        const int Steps = 400;

        var random = new Random(113);
        var positions = new ushort[Rows];
        var loops = new long[Rows];
        var oraclePositions = new ushort[Rows];
        var oracleLoops = new long[Rows];
        long forwardWraps = 0, backwardWraps = 0;
        for (var i = 0; i < Rows; i++)
            positions[i] = oraclePositions[i] = (ushort)(i * 7 % Duration);

        for (var step = 0; step < Steps; step++)
        {
            var forward = random.Next(3) != 2;
            var reverse = !forward;
            for (var i = 0; i < Rows; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, positions[i]), Duration, true, reverse, out var next, out _, out var flags))
                    continue;
                if ((flags & FrameFlags.TimelineEnd) != 0)
                {
                    if ((flags & FrameFlags.Reverse) != 0) backwardWraps++;
                    else forwardWraps++;
                    loops[i] += (flags & FrameFlags.Reverse) != 0 ? -1 : 1;
                }
                positions[i] = next.Position;
            }
            for (var i = 0; i < Rows; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), Duration, true, reverse, out var next, out var tick, out _))
                    continue;
                if (tick == Duration - 1)
                    oracleLoops[i] += reverse ? -1 : 1;
                oraclePositions[i] = next.Position;
            }
        }

        Require(positions.SequenceEqual(oraclePositions), "wrap receipt positions match the movement law");
        Require(loops.SequenceEqual(oracleLoops), "flag-reconstructed loop counts match the removed engine cycle");

        long laneSum = 0, oracleSum = 0;
        for (var i = 0; i < Rows; i++) { laneSum += loops[i]; oracleSum += oracleLoops[i]; }
        Require(laneSum == oracleSum, "wrap receipt loop totals agree");
        Require(forwardWraps > 0 && backwardWraps > 0, "wrap receipt exercised wraps in both directions on the randomized schedule");
        Console.WriteLine($"wrap-count: {Rows} rows x {Steps} randomized steps reconstruct loop counts from TimelineEnd/Reverse flags (finite wraps carry CompletedAfter/CompletedBefore)");
    }

    internal static void FoldAndBlend()
    {
        using var asset = TimelineAsset.Load(new AlphaBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 8u, new DamageClip(8f))
            .Clip(0, 4u, 8u, new DamageClip(4f))
            .Clip(1, 0u, 8u, new HealClip(3f))
            .Clip(2, 2u, 8u, new DamageClip(2f))
            .Looping()
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(asset);

        for (var tick = 0; tick < 8; tick++)
        {
            var amount = tick < 4 ? 8f : Blend(8f, 4f, (uint)tick, 4u, 4u);
            var damage = 2f * amount + (tick >= 2 ? 1f * 2f : 0f);
            var heal = 3f;
            Require(BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick) == heal - damage, $"folded effect at {tick}");
            Require(BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick) == damage - heal, $"folded inverse at {tick}");
        }
    }

    internal static void RewindAndCatchUp()
    {
        using var asset = TimelineAsset.Load(new AlphaBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(2f))
            .Clip(0, 0u, 6u, new TandemClip(4f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[32];
        var values = new float[32];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i % 6);
        var initialPositions = (ushort[])positions.Clone();

        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values);

        Require(positions.SequenceEqual(initialPositions), "rewind restores positions");
        Require(values.All(static value => value == 0f), "rewind restores values exactly");

        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        var single = values[0];
        for (var i = 0; i < 2; i++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values);
        Require(values[0] == single * 2, "catch-up calls are linear and backward cancels one");
    }

    internal static void Faults()
    {
        using var impure = TimelineAsset.Load(new AlphaBaker()
            .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
            .Clip(0, 0u, 6u, new ImpureClip(5f))
            .Looping()
            .Bake());
        BakedLane<ImpureTrack, ImpureClip>.Bind(impure);
        Require(BakedLane<ImpureTrack, ImpureClip>.Effect(0) == 0f, "column-folding consumer bakes its zero-seed baseline");

        using var foreign = TimelineAsset.Load(new AlphaBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 6u, new DamageClip(5f))
            .Looping()
            .Bake());
        RequireThrows<ArgumentException>(() => BakedLane<HealTrack, HealClip>.Bind(foreign), "asset without the pair rejected at bind");
    }

    internal static void Validation()
    {
        var positions = new ushort[4];
        var values = new float[3];
        RequireThrows<ArgumentException>(() => Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(values), "length mismatch rejected");
        values = new float[4];
        var buffer = new ushort[10];
        var overlappingPositions = buffer.AsSpan(0, 4);
        var overlapping = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var threw = false;
        try
        {
            Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(overlappingPositions, true).Apply(overlapping);
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        Require(threw, "overlapping columns rejected");
    }

    internal static void TimelineSets()
    {
        using var loopingAsset = TimelineAsset.Load(new AlphaBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 12u, new DamageClip(5f))
            .Looping()
            .Bake());
        using var finiteAsset = TimelineAsset.Load(new AlphaBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 2u, 8u, new DamageClip(4f))
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(loopingAsset);
        var loopingDuration = (int)BakedLane<DamageTrack, DamageClip>.Duration;
        var loopingEffect = new float[loopingDuration];
        var loopingInverse = new float[loopingDuration];
        for (var tick = 0; tick < loopingDuration; tick++)
        {
            loopingEffect[tick] = BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick);
            loopingInverse[tick] = BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick);
        }
        BakedLane<DamageTrack, DamageClip>.Bind(finiteAsset);
        var finiteDuration = (int)BakedLane<DamageTrack, DamageClip>.Duration;
        var finiteEffect = new float[finiteDuration];
        var finiteInverse = new float[finiteDuration];
        for (var tick = 0; tick < finiteDuration; tick++)
        {
            finiteEffect[tick] = BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick);
            finiteInverse[tick] = BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick);
        }

        using var timelines = new TimelineSet<DamageTrack, DamageClip>();
        var loopingId = timelines.Add(loopingAsset);
        var finiteId = timelines.Add(finiteAsset);

        const int Rows = 700;
        const int Frames = 120;
        var ids = new ushort[Rows];
        var positions = new ushort[Rows];
        var values = new float[Rows];
        for (var i = 0; i < Rows; i++)
        {
            ids[i] = i < 300 ? loopingId : i % 2 == 0 ? finiteId : loopingId;
            positions[i] = (ushort)(i % 14);
        }
        var oraclePositions = (ushort[])positions.Clone();
        var oracleValues = new float[Rows];

        for (var frame = 0; frame < Frames; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(values);
            for (var i = 0; i < Rows; i++)
            {
                var isLooping = ids[i] == loopingId;
                var duration = isLooping ? loopingDuration : finiteDuration;
                if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), (ushort)duration, isLooping, !forward, out var next, out var timelineTick, out _))
                    continue;
                oracleValues[i] += forward
                    ? isLooping ? loopingEffect[timelineTick] : finiteEffect[timelineTick]
                    : isLooping ? loopingInverse[timelineTick] : finiteInverse[timelineTick];
                oraclePositions[i] = next.Position;
            }
        }
        Require(positions.SequenceEqual(oraclePositions), "set positions match the law");
        Require(values.SequenceEqual(oracleValues), "set folded effects match the law");

        RequireThrows<ArgumentException>(() =>
            timelines.Gather(new ushort[] { loopingId, 2 }).Seek(new ushort[] { 0, 0 }, true).Apply(new float[2]), "unbound timeline id rejected");

        timelines.Dispose();
        RequireThrows<ObjectDisposedException>(() => timelines.Gather(ids), "disposed set rejected");
        Console.WriteLine($"timeline sets: {Rows} rows over 2 baked timelines x {Frames} frames, ids {loopingId}/{finiteId}, uniform, gather, streak, and mixed chunk paths");
    }

    internal static void Memory()
    {
        using var asset = TimelineAsset.Load(new AlphaBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(2f))
            .Clip(0, 0u, 64u, new TandemClip(1f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[256];
        var values = new float[256];

        long allocated;
        for (var attempt = 0; ; attempt++)
        {
            for (var pass = 0; pass < 1_000; pass++)
                Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 100_000; pass++)
                Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0 || attempt >= 8) break;
        }
        Require(allocated == 0, $"warm lane allocated {allocated} B after settle attempts");
        Console.WriteLine($"allocation: 100k x 256-row lane applies retained {allocated} B; table+record bytes per tick {BakedLane<TandemTrack, TandemClip>.Duration * 28}");
        Console.WriteLine($"frame bytes: TimelineState 8, TimelineComponent 16, movement record 8");
    }

    internal static void BatchCapacity()
    {
        const int Rows = 200_000;
        using var asset = TimelineAsset.Load(new AlphaBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(1f))
            .Clip(0, 0u, 64u, new TandemClip(2f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[Rows];
        var values = new float[Rows];
        for (var i = 0; i < Rows; i++)
            positions[i] = (ushort)(i % 64);

        for (var tick = 0; tick < 64; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);

        long checksum = 0;
        for (var i = 0; i < Rows; i++)
            checksum = unchecked(checksum * 31 + (long)values[i]);
        Require(checksum != 0, "batch capacity checksum computed");
        Require(values[0] == 576f && values[Rows - 1] == 576f, "capacity fold is 64 ticks x 9 per row");
        Console.WriteLine($"capacity: {Rows} rows x 64 ticks checksum {checksum}");
    }

    internal static void ModuleCapacity()
    {
        const int Tracks = 256;
        var baker = new AlphaBaker();
        for (var track = 1; track <= Tracks; track++)
            baker.Track<TandemTrack, TandemClip>(new TandemTrack(track)).Clip(track - 1, 0u, 64u, new TandemClip(1f));
        using var asset = TimelineAsset.Load(baker.Looping().Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);

        var expected = 0f;
        for (var track = 1; track <= Tracks; track++)
            expected += track + 7f;
        for (var tick = 0; tick < 64; tick++)
            Require(BakedLane<TandemTrack, TandemClip>.Effect((ushort)tick) == expected, $"module fold at {tick}");

        var positions = new ushort[16];
        var values = new float[16];
        for (var tick = 0; tick < 10; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        Require(values.All(value => value == expected * 10), "module capacity fold applied");
        Console.WriteLine($"module-capacity: {Tracks} tracks fold to {expected} per tick, x10 applied");
    }

    static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"receipt failed: {label}");
    }

    static void RequireThrows<TException>(Action action, string label) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"receipt failed: expected {typeof(TException).Name} ({label})");
    }
}
