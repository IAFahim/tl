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

[StructLayout(LayoutKind.Sequential)]
internal struct AlphaSlot<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged
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

internal sealed class AlphaBaker
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
            var slot = new AlphaSlot<TTrack, TClip>
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

    public AlphaBaker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        _tracks.Add(new BakedTrack<TTrack, TClip>
        {
            TrackValue = value,
            Key = PairRuntime<TTrack, TClip>.Key,
            Stride = (uint)((Unsafe.SizeOf<AlphaSlot<TTrack, TClip>>() + 15) & ~15),
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
        FoldAndBlend();
        RewindAndCatchUp();
        Faults();
        Validation();
        Console.WriteLine("receipts: movement, fold+blend, rewind, catch-up, faults, validation PASS");
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
            var cycles = new long[64];
            for (var i = 0; i < positions.Length; i++)
                positions[i] = (ushort)(i % (duration + 2));
            var oraclePositions = (ushort[])positions.Clone();
            var oracleCycles = new long[64];
            var oracleValues = new float[64];

            for (var tick = 0; tick < 200; tick++)
            {
                var forward = tick % 3 != 2;
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, forward).Apply(values, cycles);
                for (var i = 0; i < positions.Length; i++)
                {
                    if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i], oracleCycles[i]), duration, looping, !forward, out var next, out var timelineTick, out _, out _))
                        continue;
                    oracleValues[i] += forward ? BakedLane<DamageTrack, DamageClip>.Effect((ushort)timelineTick) : BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)timelineTick);
                    oraclePositions[i] = (ushort)next.Position;
                    oracleCycles[i] = next.Cycle;
                }
            }

            Require(positions.SequenceEqual(oraclePositions), "movement positions match the law");
            Require(cycles.SequenceEqual(oracleCycles), "cycles match the law");
            Require(values.SequenceEqual(oracleValues), "folded effects match the law");
        }
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
        var cycles = new long[32];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i % 6);
        var initialPositions = (ushort[])positions.Clone();
        var initialCycles = (long[])cycles.Clone();

        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);
        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values, cycles);

        Require(positions.SequenceEqual(initialPositions), "rewind restores positions");
        Require(cycles.SequenceEqual(initialCycles), "rewind restores cycles");
        Require(values.All(static value => value == 0f), "rewind restores values exactly");

        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);
        var single = values[0];
        for (var i = 0; i < 2; i++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);
        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values, cycles);
        Require(values[0] == single * 2, "catch-up calls are linear and backward cancels one");
    }

    internal static void Faults()
    {
        using var impure = TimelineAsset.Load(new AlphaBaker()
            .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
            .Clip(0, 0u, 6u, new ImpureClip(5f))
            .Looping()
            .Bake());
        RequireThrows<ArgumentException>(() => BakedLane<ImpureTrack, ImpureClip>.Bind(impure), "impure consumer rejected at bind");

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
        var cycles = new long[4];
        RequireThrows<ArgumentException>(() => Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(values, cycles), "length mismatch rejected");
        values = new float[4];
        var buffer = new ushort[10];
        var overlappingPositions = buffer.AsSpan(0, 4);
        var overlapping = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var threw = false;
        try
        {
            Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(overlappingPositions, true).Apply(overlapping, cycles);
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        Require(threw, "overlapping columns rejected");
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
        var cycles = new long[256];

        for (var pass = 0; pass < 100; pass++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 100_000; pass++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, $"warm lane allocated {allocated} B");
        Console.WriteLine($"allocation: 100k x 256-row lane applies retained {allocated} B; table bytes {BakedLane<TandemTrack, TandemClip>.Duration * 2 * 4}");
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
        var cycles = new long[Rows];
        for (var i = 0; i < Rows; i++)
            positions[i] = (ushort)(i % 64);

        for (var tick = 0; tick < 64; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);

        long checksum = 0;
        for (var i = 0; i < Rows; i++)
            checksum = unchecked(checksum * 31 + (long)values[i] + cycles[i]);
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
        var cycles = new long[16];
        for (var tick = 0; tick < 10; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values, cycles);
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
