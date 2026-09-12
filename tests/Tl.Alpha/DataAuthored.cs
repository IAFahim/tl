using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl;

internal static class DataBlends
{
    internal static int DamageCalls;
}

internal static class DataLogRecord
{
    internal static void Step(int jobCode, int trackIndex, uint gameTick, uint timelineTick, long cycle, FrameFlags flags, int clipBits, ref DataLog log)
    {
        var direction = (flags & FrameFlags.Reverse) != 0 ? -1 : 1;
        if (log.Calls != 0 && (log.GameTick != gameTick || log.Direction != direction))
        {
            log.FrameOrder = 0;
            log.FrameSteps = 0;
            log.Flags = FrameFlags.None;
            log.TrackMask = 0;
            log.Bits0 = 0;
            log.Bits1 = 0;
            log.Bits2 = 0;
            log.Bits3 = 0;
        }
        log.Calls++;
        log.FrameSteps++;
        log.FrameOrder = unchecked(log.FrameOrder * 1000 + trackIndex * 10 + jobCode);
        log.Bits3 = log.Bits2;
        log.Bits2 = log.Bits1;
        log.Bits1 = log.Bits0;
        log.Bits0 = clipBits;
        log.GameTick = gameTick;
        log.TimelineTick = timelineTick;
        log.Cycle = cycle;
        log.TrackIndex = trackIndex;
        log.JobCode = jobCode;
        log.Flags |= flags;
        log.ClipBits = clipBits;
        log.TrackMask |= 1u << trackIndex;
        log.Direction = direction;
    }
}

public struct DataLog
{
    public long FrameOrder;
    public int FrameSteps;
    public int Calls;
    public uint GameTick;
    public uint TimelineTick;
    public long Cycle;
    public int TrackIndex;
    public int JobCode;
    public FrameFlags Flags;
    public int ClipBits;
    public uint TrackMask;
    public int Direction;
    public int Bits0;
    public int Bits1;
    public int Bits2;
    public int Bits3;
}

public readonly record struct DamageClip(float Amount);
public readonly record struct HealClip(float Amount);
public readonly record struct MarkClip(int Value);
public readonly record struct BombClip(int Value);
public readonly record struct TandemClip(int Value);

public readonly struct DamageTrack(float multiplier) : IBlend<DamageClip>
{
    public readonly float Multiplier = multiplier;

    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
    {
        DataBlends.DamageCalls++;
        result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public readonly struct HealTrack(float rate) : IBlend<HealClip>
{
    public readonly float Rate = rate;

    public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
        => result = new HealClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct MarkTrack(int code) : IBlend<MarkClip>
{
    public readonly int Code = code;

    public void Blend(in MarkClip first, in MarkClip second, float factor, out MarkClip result)
        => result = new MarkClip(first.Value + (int)((second.Value - first.Value) * factor));
}

public readonly struct BombTrack(int code) : IBlend<BombClip>
{
    public readonly int Code = code;

    public void Blend(in BombClip first, in BombClip second, float factor, out BombClip result)
        => result = first;
}

public readonly struct TandemTrack(int code) : IBlend<TandemClip>
{
    public readonly int Code = code;

    public void Blend(in TandemClip first, in TandemClip second, float factor, out TandemClip result)
        => result = first;
}

public readonly struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref DataLog log)
        => DataLogRecord.Step(1, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, BitConverter.SingleToInt32Bits(frame.Clip.Amount), ref log);
}

public readonly struct HealJob : ITimelineJob<HealTrack, HealClip>
{
    public static void Execute(in Frame<HealTrack, HealClip> frame, ref DataLog log)
        => DataLogRecord.Step(2, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, BitConverter.SingleToInt32Bits(frame.Clip.Amount), ref log);
}

public readonly struct MarkJob : ITimelineJob<MarkTrack, MarkClip>
{
    public static void Execute(in Frame<MarkTrack, MarkClip> frame, ref DataLog log)
        => DataLogRecord.Step(3, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, frame.Clip.Value, ref log);
}

public readonly struct ThrowingJob : ITimelineJob<BombTrack, BombClip>
{
    public static void Execute(in Frame<BombTrack, BombClip> frame, ref DataLog log)
    {
        if (frame.Clip.Value == -7919)
            throw new InvalidOperationException($"data-authored bomb at {frame.TimelineTick}");
        DataLogRecord.Step(4, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, frame.Clip.Value, ref log);
    }
}

public readonly struct TandemFirst : ITimelineJob<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref DataLog log)
        => DataLogRecord.Step(5, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, frame.Clip.Value, ref log);
}

public readonly struct TandemSecond : ITimelineJob<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref DataLog log)
        => DataLogRecord.Step(6, frame.TrackIndex, frame.GameTick, frame.TimelineTick, frame.Cycle, frame.Flags, frame.Clip.Value, ref log);
}

public readonly partial struct DataAuthoredJobDeclarations : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(1f)).Use<DamageJob>();
        var heal = builder.Track(new HealTrack(1f)).Use<HealJob>();
        var mark = builder.Track(new MarkTrack(1)).Use<MarkJob>();
        var bomb = builder.Track(new BombTrack(1)).Use<ThrowingJob>();
        var first = builder.Track(new TandemTrack(1)).Use<TandemFirst>();
        var second = builder.Track(new TandemTrack(1)).Use<TandemSecond>();
        builder.Clip(damage, new DamageClip(1f), 0u, 1u);
        builder.Clip(heal, new HealClip(1f), 0u, 1u);
        builder.Clip(mark, new MarkClip(1), 0u, 1u);
        builder.Clip(bomb, new BombClip(1), 0u, 1u);
        builder.Clip(first, new TandemClip(1), 0u, 1u);
        builder.Clip(second, new TandemClip(1), 0u, 1u);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct DataSlot<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged
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

internal sealed class DataBaker
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
            var slot = new DataSlot<TTrack, TClip>
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

    public DataBaker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
    {
        _tracks.Add(new BakedTrack<TTrack, TClip>
        {
            TrackValue = value,
            Key = PairRuntime<TTrack, TClip>.Key,
            Stride = (uint)((Unsafe.SizeOf<DataSlot<TTrack, TClip>>() + 15) & ~15),
            Index = (byte)_tracks.Count,
        });
        return this;
    }

    public DataBaker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
    {
        _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
        return this;
    }

    public DataBaker Looping()
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

internal sealed class OracleAsset(
    OracleAsset.Track[] tracks,
    bool loops)
{
    internal readonly struct Clip(uint start, uint end, float amount, int mark)
    {
        public readonly uint Start = start;
        public readonly uint End = end;
        public readonly float Amount = amount;
        public readonly int Mark = mark;
    }

    internal readonly struct Track(int jobCode, bool floatClip, Clip[] clips)
    {
        public readonly int JobCode = jobCode;
        public readonly bool FloatClip = floatClip;
        public readonly Clip[] Clips = clips;
    }

    internal readonly Track[] Tracks = tracks;
    internal readonly bool Loops = loops;
    internal readonly uint Duration = tracks.SelectMany(static track => track.Clips).Select(static clip => clip.End).DefaultIfEmpty(0u).Max();
}

internal readonly struct OracleFrame
{
    public readonly uint GameTick;
    public readonly uint TimelineTick;
    public readonly long Cycle;
    public readonly FrameFlags Flags;
    public readonly long FrameOrder;
    public readonly int Steps;
    public readonly int LastBits;
    public readonly int LastTrack;
    public readonly int LastJob;
    public readonly uint TrackMask;
    public readonly int Bits0;
    public readonly int Bits1;
    public readonly int Bits2;
    public readonly int Bits3;
    public readonly bool Moved;
    public readonly uint NextPosition;
    public readonly long NextCycle;

    internal OracleFrame(uint gameTick, uint timelineTick, long cycle, FrameFlags flags, long frameOrder, int steps, int lastBits, int lastTrack, int lastJob, uint trackMask, int bits0, int bits1, int bits2, int bits3, bool moved, uint nextPosition, long nextCycle)
    {
        GameTick = gameTick;
        TimelineTick = timelineTick;
        Cycle = cycle;
        Flags = flags;
        FrameOrder = frameOrder;
        Steps = steps;
        LastBits = lastBits;
        LastTrack = lastTrack;
        LastJob = lastJob;
        TrackMask = trackMask;
        Bits0 = bits0;
        Bits1 = bits1;
        Bits2 = bits2;
        Bits3 = bits3;
        Moved = moved;
        NextPosition = nextPosition;
        NextCycle = nextCycle;
    }
}

internal static class DataAuthoredOracle
{
    internal static bool Select(uint duration, bool loops, uint position, long cycle, bool reverse, out uint tick, out long frameCycle, out FrameFlags flags, out uint nextPosition, out long nextCycle)
    {
        tick = 0;
        frameCycle = 0;
        flags = FrameFlags.None;
        nextPosition = position;
        nextCycle = cycle;
        if (duration == 0 || position > duration)
            return false;
        if (loops)
        {
            if (position == duration)
                return false;
            flags = FrameFlags.Looping | (reverse ? FrameFlags.Reverse : FrameFlags.None);
            if (reverse)
            {
                if (position == 0)
                {
                    tick = duration - 1u;
                    frameCycle = unchecked(cycle - 1L);
                }
                else
                {
                    tick = position - 1u;
                    frameCycle = cycle;
                }
                nextPosition = tick;
                nextCycle = frameCycle;
            }
            else
            {
                tick = position;
                frameCycle = cycle;
                if (tick == duration - 1u)
                {
                    nextPosition = 0;
                    nextCycle = unchecked(cycle + 1L);
                }
                else
                {
                    nextPosition = tick + 1u;
                    nextCycle = cycle;
                }
            }
            if (tick == 0)
                flags |= FrameFlags.TimelineStart;
            if (tick == duration - 1u)
                flags |= FrameFlags.TimelineEnd;
            return true;
        }
        if (reverse)
        {
            if (position == 0)
                return false;
            tick = position - 1u;
            flags = FrameFlags.Reverse;
            if (tick == 0)
                flags |= FrameFlags.TimelineStart;
            if (tick == duration - 1u)
                flags |= FrameFlags.TimelineEnd;
            if (position == duration)
                flags |= FrameFlags.CompletedBefore;
            nextPosition = tick;
            nextCycle = 0;
            return true;
        }
        if (position == duration)
            return false;
        tick = position;
        if (tick == 0)
            flags |= FrameFlags.TimelineStart;
        if (tick == duration - 1u)
            flags |= FrameFlags.TimelineEnd | FrameFlags.CompletedAfter;
        nextPosition = tick + 1u;
        nextCycle = 0;
        return true;
    }

    internal static OracleFrame Step(OracleAsset asset, uint position, long cycle, uint gameTick, bool reverse)
    {
        if (!Select(asset.Duration, asset.Loops, position, cycle, reverse, out var tick, out var frameCycle, out var flags, out var nextPosition, out var nextCycle))
            return new OracleFrame(gameTick, 0, 0, FrameFlags.None, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, false, position, cycle);
        var order = 0L;
        var steps = 0;
        var lastBits = 0;
        var lastTrack = -1;
        var lastJob = 0;
        var mask = 0u;
        var bits0 = 0;
        var bits1 = 0;
        var bits2 = 0;
        var bits3 = 0;
        for (var visit = 0; visit < asset.Tracks.Length; visit++)
        {
            var index = reverse ? asset.Tracks.Length - 1 - visit : visit;
            var track = asset.Tracks[index];
            var active = track.Clips.Select((clip, order2) => (clip, order2))
                .Where(item => item.clip.Start <= tick && tick < item.clip.End)
                .OrderBy(item => item.clip.Start).ThenBy(item => item.order2)
                .Select(item => item.clip)
                .ToArray();
            if (active.Length == 0)
                continue;
            if (active.Length > 2)
                throw new InvalidOperationException("Oracle authoring allows at most two overlapping clips.");
            var windowStart = active.Min(clip => clip.Start);
            var windowEnd = active.Max(clip => clip.End);
            int bits;
            if (track.FloatClip)
            {
                float value;
                if (active.Length == 1)
                    value = active[0].Amount;
                else
                {
                    var factorStart = Math.Max(active[0].Start, active[1].Start);
                    var factorSpan = Math.Min(active[0].End, active[1].End) - factorStart;
                    float factor;
                    if (factorSpan <= 1)
                        factor = 0.5f;
                    else
                        factor = (tick - factorStart) / (float)(factorSpan - 1);
                    value = active[0].Amount + (active[1].Amount - active[0].Amount) * factor;
                }
                bits = BitConverter.SingleToInt32Bits(value);
            }
            else
            {
                bits = active[0].Mark;
            }
            if (tick == windowStart)
                flags |= FrameFlags.ClipStart;
            if (tick == windowEnd - 1u)
                flags |= FrameFlags.ClipEnd;
            order = unchecked(order * 1000 + index * 10 + track.JobCode);
            steps++;
            bits3 = bits2;
            bits2 = bits1;
            bits1 = bits0;
            bits0 = bits;
            lastBits = bits;
            lastTrack = index;
            lastJob = track.JobCode;
            mask |= 1u << index;
        }
        return new OracleFrame(gameTick, tick, frameCycle, flags, order, steps, lastBits, lastTrack, lastJob, mask, bits0, bits1, bits2, bits3, true, nextPosition, nextCycle);
    }

    internal static long ReverseDigits(long order, int steps)
    {
        long result = 0;
        for (var index = 0; index < steps; index++)
        {
            result = unchecked(result * 1000 + order % 1000);
            order /= 1000;
        }
        return result;
    }
}

internal static class DataAuthoredReceipts
{
    internal static void All()
    {
        AbaOrder();
        OpposingOrder();
        TandemConsumers();
        BlendFactors();
        CrossedFrames();
        MovementDefaults();
        DirectQuery();
        ColdFailures();
        ExceptionPrefix();
        FacadeAllocation();
        CapacityEdges();
    }

    internal static void AbaOrder()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<MarkTrack, MarkClip>(new MarkTrack(1))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Clip(0, 0u, 16u, new DamageClip(10f))
            .Clip(1, 0u, 16u, new HealClip(20f))
            .Clip(2, 0u, 16u, new DamageClip(30f))
            .Clip(3, 0u, 16u, new MarkClip(300))
            .Clip(4, 0u, 16u, new HealClip(40f))
            .Bake());
        var oracle = new OracleAsset(
        [
            new(1, true, [new OracleAsset.Clip(0u, 16u, 10f, 0)]),
            new(2, true, [new OracleAsset.Clip(0u, 16u, 20f, 0)]),
            new(1, true, [new OracleAsset.Clip(0u, 16u, 30f, 0)]),
            new(3, false, [new OracleAsset.Clip(0u, 16u, 0f, 300)]),
            new(2, true, [new OracleAsset.Clip(0u, 16u, 40f, 0)]),
        ], false);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        var forward = new DataLog[16];
        var totalCalls = 0L;
        for (var index = 0; index < 16; index++)
        {
            query.Tick(500u + (uint)index, 1);
            forward[index] = logs[0];
            var expect = DataAuthoredOracle.Step(oracle, (uint)index, 0, 500u + (uint)index, false);
            totalCalls += expect.Steps;
            RequireFrame(logs[0], expect, totalCalls, $"aba forward {index}");
            Require(rows[0].Position == expect.NextPosition, $"aba forward {index} commit");
        }
        Require(rows[0].Position == 16u);
        var backward = new DataLog[16];
        for (var index = 0; index < 16; index++)
        {
            query.Tick(516u - (uint)index, -1);
            backward[index] = logs[0];
            var expect = DataAuthoredOracle.Step(oracle, 16u - (uint)index, 0, 515u - (uint)index, true);
            totalCalls += expect.Steps;
            RequireFrame(logs[0], expect, totalCalls, $"aba backward {index}");
            Require(rows[0].Position == expect.NextPosition, $"aba backward {index} commit");
        }
        Require(rows[0].Position == 0u);
        var documentedReverse = true;
        for (var index = 0; index < 16; index++)
            if (backward[index].FrameOrder != DataAuthoredOracle.ReverseDigits(forward[15 - index].FrameOrder, forward[15 - index].FrameSteps))
                documentedReverse = false;
        Require(documentedReverse, "backward occurrence order is the exact reverse of authored order");
        Console.WriteLine($"data-authored aba: frames=16 forward=authored-order backward=exact-reverse-of-authored documented-exact-reverse={documentedReverse}");
    }

    internal static void OpposingOrder()
    {
        using var forwardAsset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<MarkTrack, MarkClip>(new MarkTrack(1))
            .Clip(0, 0u, 3u, new DamageClip(1f))
            .Clip(1, 0u, 3u, new HealClip(2f))
            .Clip(2, 0u, 3u, new MarkClip(3))
            .Bake());
        using var reverseAsset = TimelineAsset.Load(new DataBaker()
            .Track<MarkTrack, MarkClip>(new MarkTrack(1))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 3u, new MarkClip(3))
            .Clip(1, 0u, 3u, new HealClip(2f))
            .Clip(2, 0u, 3u, new DamageClip(1f))
            .Bake());
        var forwardOracle = new OracleAsset(
        [
            new(1, true, [new OracleAsset.Clip(0u, 3u, 1f, 0)]),
            new(2, true, [new OracleAsset.Clip(0u, 3u, 2f, 0)]),
            new(3, false, [new OracleAsset.Clip(0u, 3u, 0f, 3)]),
        ], false);
        var reverseOracle = new OracleAsset(
        [
            new(3, false, [new OracleAsset.Clip(0u, 3u, 0f, 3)]),
            new(2, true, [new OracleAsset.Clip(0u, 3u, 2f, 0)]),
            new(1, true, [new OracleAsset.Clip(0u, 3u, 1f, 0)]),
        ], false);
        var rows = new[]
        {
            new TimelineComponent(forwardAsset.Reference),
            new TimelineComponent(reverseAsset.Reference),
        };
        var logs = new DataLog[2];
        var query = Facade(rows, logs, out _, out _);
        var totalCalls = new long[2];
        for (var index = 0; index < 3; index++)
        {
            query.Tick(900u + (uint)index, 1);
            for (var row = 0; row < 2; row++)
            {
                var expect = DataAuthoredOracle.Step(row == 0 ? forwardOracle : reverseOracle, (uint)index, 0, 900u + (uint)index, false);
                totalCalls[row] += expect.Steps;
                RequireFrame(logs[row], expect, totalCalls[row], $"opposing row {row} forward {index}");
            }
        }
        Require(logs[0].FrameOrder != logs[1].FrameOrder, "opposing authored orders must differ");
        for (var index = 0; index < 3; index++)
        {
            query.Tick(904u - (uint)index, -1);
            for (var row = 0; row < 2; row++)
            {
                var expect = DataAuthoredOracle.Step(row == 0 ? forwardOracle : reverseOracle, 3u - (uint)index, 0, 903u - (uint)index, true);
                totalCalls[row] += expect.Steps;
                RequireFrame(logs[row], expect, totalCalls[row], $"opposing row {row} backward {index}");
            }
        }
        Console.WriteLine($"data-authored opposing: per-asset authored orders {logs[0].FrameOrder} vs {logs[1].FrameOrder} each matched their own oracle");
    }

    internal static void TandemConsumers()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(1))
            .Clip(0, 0u, 2u, new TandemClip(7))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        query.Tick(700u, 1);
        Require(logs[0].FrameOrder == 6_005, "tandem forward consumer order is last-installed first (LIFO): TandemSecond then TandemFirst");
        Require(logs[0].FrameSteps == 2 && logs[0].Calls == 2 && rows[0].Position == 1u);
        query.Tick(701u, -1);
        Require(logs[0].FrameOrder == 6_005, "tandem backward consumer order replays the same LIFO chain; docs require a corresponding reverse order");
        Require(logs[0].FrameSteps == 2 && logs[0].Calls == 4 && rows[0].Position == 0u);
        Console.WriteLine("data-authored consumers: pair=1 consumers=2 forward=second,first backward=second,first (LIFO chain replay)");
    }

    internal static void BlendFactors()
    {
        DataBlends.DamageCalls = 0;
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 6u, new DamageClip(8f))
            .Clip(0, 3u, 9u, new DamageClip(4f))
            .Bake());
        var oracle = new OracleAsset(
        [
            new(1, true,
            [
                new OracleAsset.Clip(0u, 6u, 8f, 0),
                new OracleAsset.Clip(3u, 9u, 4f, 0),
            ]),
        ], false);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        var totalCalls = 0L;
        var spanThreeBits = new int[9];
        for (var index = 0; index < 9; index++)
        {
            query.Tick(800u + (uint)index, 1);
            var expect = DataAuthoredOracle.Step(oracle, (uint)index, 0, 800u + (uint)index, false);
            totalCalls += expect.Steps;
            RequireFrame(logs[0], expect, totalCalls, $"blend span3 {index}");
            spanThreeBits[index] = expect.LastBits;
        }
        Require(DataBlends.DamageCalls == 3, "three-frame overlap resolves once per frame through DamageTrack.Blend");
        var spanThreeResolves = DataBlends.DamageCalls;
        Require(spanThreeBits[3] == BitConverter.SingleToInt32Bits(8f), "binary-exact factor 0 at overlap start");
        Require(spanThreeBits[4] == BitConverter.SingleToInt32Bits(6f), "binary-exact factor 0.5 at overlap middle");
        Require(spanThreeBits[5] == BitConverter.SingleToInt32Bits(4f), "binary-exact factor 1 at overlap end");
        DataBlends.DamageCalls = 0;
        using var oneFrame = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 3u, new DamageClip(8f))
            .Clip(0, 2u, 5u, new DamageClip(2f))
            .Bake());
        var oneOracle = new OracleAsset(
        [
            new(1, true,
            [
                new OracleAsset.Clip(0u, 3u, 8f, 0),
                new OracleAsset.Clip(2u, 5u, 2f, 0),
            ]),
        ], false);
        var oneRows = new[] { new TimelineComponent(oneFrame.Reference) };
        var oneLogs = new DataLog[1];
        var oneQuery = Facade(oneRows, oneLogs, out _, out _);
        var oneCalls = 0L;
        var oneBits = new int[5];
        for (var index = 0; index < 5; index++)
        {
            oneQuery.Tick(900u + (uint)index, 1);
            var expect = DataAuthoredOracle.Step(oneOracle, (uint)index, 0, 900u + (uint)index, false);
            oneCalls += expect.Steps;
            RequireFrame(oneLogs[0], expect, oneCalls, $"blend span1 {index}");
            oneBits[index] = expect.LastBits;
        }
        Require(DataBlends.DamageCalls == 1, "one-frame overlap resolves once with the 0.5f factor");
        Require(oneBits[2] == BitConverter.SingleToInt32Bits(5f), "binary-exact 0.5f factor at the one-frame overlap");
        Console.WriteLine($"data-authored blend: span3-resolves={spanThreeResolves} span1-resolves={DataBlends.DamageCalls} factors=(tick-start)/(span-1) 0.5f-when-span<=1");
    }

    internal static void CrossedFrames()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 4u, new DamageClip(5f))
            .Looping()
            .Bake());
        var batched = new[] { new TimelineComponent(asset.Reference) };
        var batchedLogs = new DataLog[1];
        var batchedQuery = Facade(batched, batchedLogs, out _, out _);
        batchedQuery.Tick(100u, 5);
        var unit = new[] { new TimelineComponent(asset.Reference) };
        var unitLogs = new DataLog[1];
        var unitQuery = Facade(unit, unitLogs, out _, out _);
        for (var index = 0; index < 5; index++)
        {
            unitQuery.Tick(100u + (uint)index, 1);
            Require(unitLogs[0].GameTick == 100u + (uint)index, $"crossed forward unit game tick {index}");
        }
        Require(SameLog(batchedLogs[0], unitLogs[0]), "Tick(G,+5) is observationally equal to five unit calls");
        Require(batched[0].Position == unit[0].Position && batched[0].Cycle == unit[0].Cycle);
        var batchedBack = new[] { new TimelineComponent(asset.Reference) };
        var batchedBackLogs = new DataLog[1];
        var batchedBackQuery = Facade(batchedBack, batchedBackLogs, out _, out _);
        batchedBackQuery.Tick(105u, -5);
        var unitBack = new[] { new TimelineComponent(asset.Reference) };
        var unitBackLogs = new DataLog[1];
        var unitBackQuery = Facade(unitBack, unitBackLogs, out _, out _);
        for (var index = 0; index < 5; index++)
        {
            unitBackQuery.Tick(105u - (uint)index, -1);
            Require(unitBackLogs[0].GameTick == 104u - (uint)index, $"crossed backward unit game tick {index}");
        }
        Require(SameLog(batchedBackLogs[0], unitBackLogs[0]), "Tick(G,-5) is observationally equal to five reverse unit calls");
        var zero = new[] { new TimelineComponent(asset.Reference) };
        var zeroLogs = new DataLog[1];
        var zeroQuery = Facade(zero, zeroLogs, out var zeroReceipts, out var zeroUints);
        zeroUints[0] = 77u;
        zeroQuery.Tick(4_294_967_295u, 0);
        Require(zero[0].Position == 0u && zeroLogs[0].Calls == 0 && zeroReceipts[0] == default && zeroUints[0] == 77u, "Tick(G,0) is a total no-op");
        var wrapForward = new[] { new TimelineComponent(asset.Reference) };
        var wrapForwardLogs = new DataLog[1];
        var wrapForwardQuery = Facade(wrapForward, wrapForwardLogs, out _, out _);
        wrapForwardQuery.Tick(4_294_967_294u, 3);
        Require(wrapForwardLogs[0].GameTick == 0u, "forward game tick wraps as uint");
        var wrapBackward = new[] { new TimelineComponent(asset.Reference) };
        var wrapBackwardLogs = new DataLog[1];
        var wrapBackwardQuery = Facade(wrapBackward, wrapBackwardLogs, out _, out _);
        wrapBackwardQuery.Tick(2u, -3);
        Require(wrapBackwardLogs[0].GameTick == 4_294_967_295u, "backward game tick wraps as uint");
        Console.WriteLine("data-authored crossed: +5 emits G..G+4, -5 emits G-1..G-5, zero is a no-op, uint wrap verified");
    }

    internal static void MovementDefaults()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 6u, new DamageClip(3f))
            .Bake());
        var oracle = new OracleAsset([new(1, true, [new OracleAsset.Clip(0u, 6u, 3f, 0)])], false);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        query.Tick(4_000_000_000u, 1);
        var defaultExpect = DataAuthoredOracle.Step(oracle, 0u, 0, 4_000_000_000u, false);
        RequireFrame(logs[0], defaultExpect, 1, "movement default large game tick");
        Require(logs[0].TimelineTick == 0u && logs[0].Flags.HasFlag(FrameFlags.TimelineStart), "a new component starts at local zero regardless of game tick");
        using var shortAsset = TimelineAsset.Load(new DataBaker()
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Clip(0, 0u, 2u, new HealClip(9f))
            .Bake());
        var shortOracle = new OracleAsset([new(2, true, [new OracleAsset.Clip(0u, 2u, 9f, 0)])], false);
        var clampRows = new[]
        {
            new TimelineComponent(shortAsset.Reference),
            new TimelineComponent(asset.Reference),
        };
        var clampLogs = new DataLog[2];
        var clampQuery = Facade(clampRows, clampLogs, out _, out _);
        clampQuery.Tick(600u, 4);
        Require(clampRows[0].Position == 2u && clampLogs[0].Calls == 2, "short row clamped after two frames");
        Require(clampRows[1].Position == 4u && clampLogs[1].Calls == 4, "long row still moving in the same call");
        clampQuery.Tick(604u, 10);
        Require(clampRows[0].Position == 2u && clampLogs[0].Calls == 2, "clamped row stays total no-op");
        Require(clampRows[1].Position == 6u && clampLogs[1].Calls == 6, "long row finished independently");
        clampQuery.Tick(614u, -1);
        Require(clampRows[0].Position == 1u && clampLogs[0].Calls == 3, "clamped row rewinds");
        Require(clampRows[1].Position == 5u && clampLogs[1].Calls == 7, "finished row rewinds");
        using var loopAsset = TimelineAsset.Load(new DataBaker()
            .Track<MarkTrack, MarkClip>(new MarkTrack(1))
            .Clip(0, 0u, 3u, new MarkClip(11))
            .Looping()
            .Bake());
        var loopOracle = new OracleAsset([new(3, false, [new OracleAsset.Clip(0u, 3u, 0f, 11)])], true);
        var loopRows = new[]
        {
            new TimelineComponent(loopAsset.Reference),
            new TimelineComponent(loopAsset.Reference) { Position = 2u },
        };
        var loopLogs = new DataLog[2];
        var loopQuery = Facade(loopRows, loopLogs, out _, out _);
        var loopCalls = new long[2];
        var loopStates = new (uint Position, long Cycle)[2];
        for (var row = 0; row < 2; row++)
            loopStates[row] = (loopRows[row].Position, loopRows[row].Cycle);
        for (var index = 0; index < 4; index++)
        {
            loopQuery.Tick(700u + (uint)index, 1);
            for (var row = 0; row < 2; row++)
            {
                var expect = DataAuthoredOracle.Step(loopOracle, loopStates[row].Position, loopStates[row].Cycle, 700u + (uint)index, false);
                loopCalls[row] += expect.Steps;
                RequireFrame(loopLogs[row], expect, loopCalls[row], $"movement loop row {row} step {index}");
                loopStates[row] = (expect.NextPosition, expect.NextCycle);
                Require(loopRows[row].Position == expect.NextPosition && loopRows[row].Cycle == expect.NextCycle, $"movement loop row {row} commit {index}");
            }
        }
        Require(loopRows[0].Cycle == 1L && loopRows[1].Cycle == 2L, "loop cycles advance per instance");
        var parityPosition = 0u;
        var parityCycle = 0L;
        for (var index = 0; index < 4; index++)
        {
            var oracleMoved = DataAuthoredOracle.Select(3u, true, parityPosition, parityCycle, false, out var oracleTick, out var oracleFrameCycle, out var oracleFlags, out var oracleNextPosition, out var oracleNextCycle);
            var moved = TimelineMovement.Select(new TimelineState(1, parityPosition, parityCycle), 3u, true, false, out var next, out var tick, out var frameCycle, out var flags);
            Require(oracleMoved && moved, $"loop parity moved {index}");
            Require(tick == oracleTick && frameCycle == oracleFrameCycle && flags == oracleFlags && next.Position == oracleNextPosition && next.Cycle == oracleNextCycle, $"loop boundary step {index} matches TimelineMovement");
            parityPosition = oracleNextPosition;
            parityCycle = oracleNextCycle;
        }
        var wrapRows = new[] { new TimelineComponent(loopAsset.Reference) { Cycle = long.MinValue } };
        var wrapLogs = new DataLog[1];
        Facade(wrapRows, wrapLogs, out _, out _).Tick(800u, -1);
        Require(wrapRows[0].Cycle == long.MaxValue && wrapLogs[0].Cycle == long.MaxValue, "reverse loop wrap decrements cycle in two's complement");
        Console.WriteLine($"data-authored movement: default-local-zero independent-clamp cycles={loopRows[0].Cycle}/{loopRows[1].Cycle} wrap=verified");
    }

    internal static void DirectQuery()
    {
        using var blendAsset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 6u, new DamageClip(8f))
            .Clip(0, 3u, 9u, new DamageClip(4f))
            .Bake());
        var blendOracle = new OracleAsset(
        [
            new(1, true,
            [
                new OracleAsset.Clip(0u, 6u, 8f, 0),
                new OracleAsset.Clip(3u, 9u, 4f, 0),
            ]),
        ], false);
        var rows = new[] { new TimelineComponent(blendAsset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        for (var position = 0u; position < 9u; position++)
        {
            var before = (rows[0].Position, rows[0].Cycle);
            var expect = DataAuthoredOracle.Step(blendOracle, position, 0, 0u, false);
            var count = 0;
            foreach (var frame in Timeline.Query<DamageTrack, DamageClip>(in rows[0]))
            {
                Require(frame.TimelineTick == position, $"query timeline tick at {position}");
                Require(frame.TrackIndex == 0, $"query track index at {position}");
                Require(BitConverter.SingleToInt32Bits(frame.Clip.Amount) == expect.LastBits, $"query blend bits at {position}");
                count++;
            }
            Require(count == 1, $"query frame count at {position}");
            Require((rows[0].Position, rows[0].Cycle) == before, $"query never advances the component at {position}");
            query.Tick(950u + position, 1);
            Require(logs[0].ClipBits == expect.LastBits, $"query bits equal coordinator-executed bits at {position}");
        }
        Require(rows[0].Position == 9u);
        var completed = 0;
        foreach (var _ in Timeline.Query<DamageTrack, DamageClip>(in rows[0]))
            completed++;
        Require(completed == 0, "query yields nothing at completed position");
        using var abaAsset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 3u, new DamageClip(10f))
            .Clip(1, 0u, 3u, new HealClip(20f))
            .Clip(2, 0u, 3u, new DamageClip(30f))
            .Bake());
        var abaRows = new[] { new TimelineComponent(abaAsset.Reference) };
        var abaLogs = new DataLog[1];
        var abaQuery = Facade(abaRows, abaLogs, out _, out _);
        abaQuery.Tick(960u, 1);
        var frames = new (ushort TrackIndex, int Bits)[2];
        var index = 0;
        foreach (var frame in Timeline.Query<DamageTrack, DamageClip>(in abaRows[0]))
        {
            if (index < 2)
                frames[index] = (frame.TrackIndex, BitConverter.SingleToInt32Bits(frame.Clip.Amount));
            index++;
        }
        Require(index == 2, "query yields both occurrences of the repeated pair");
        Require(frames[0].TrackIndex == 0 && frames[0].Bits == BitConverter.SingleToInt32Bits(10f), "query first damage frame");
        Require(frames[1].TrackIndex == 2 && frames[1].Bits == BitConverter.SingleToInt32Bits(30f), "query second damage frame");
        Require(abaLogs[0].Bits2 == BitConverter.SingleToInt32Bits(10f) && abaLogs[0].Bits1 == BitConverter.SingleToInt32Bits(20f) && abaLogs[0].Bits0 == BitConverter.SingleToInt32Bits(30f), "query frames equal the coordinator-executed values");
        var mismatch = 0;
        foreach (var _ in Timeline.Query<MarkTrack, MarkClip>(in abaRows[0]))
            mismatch++;
        Require(mismatch == 0, "query yields nothing for a nonmatching pair");
        Console.WriteLine("data-authored query: sampled 9 positions incl. blend windows, matched executed values, never advanced");
    }

    internal static void ColdFailures()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<MarkTrack, MarkClip>(new MarkTrack(1))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Clip(0, 0u, 4u, new DamageClip(1f))
            .Clip(1, 0u, 4u, new HealClip(1f))
            .Clip(2, 0u, 4u, new DamageClip(1f))
            .Clip(3, 0u, 4u, new MarkClip(1))
            .Clip(4, 0u, 4u, new HealClip(1f))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        logs[0].Calls = 99;
        var receipts = new Receipt[1];
        var uints = new uint[1];
        uints[0] = 77u;
        var message = "";
        try
        {
            Timeline.Rows(rows).Read(uints).Read(receipts).Tick(30u, 1);
        }
        catch (ArgumentException exception)
        {
            message = exception.Message;
        }
        Require(message == "global::DamageJob: required column missing for registered consumer: global::DataLog", "missing mandatory column names job and type");
        Require(rows[0].Position == 0u, "missing column leaves positions unchanged");
        Require(logs[0].Calls == 99 && receipts[0] == default && uints[0] == 77u, "missing column leaves sentinel row data unchanged");
        Timeline.Rows(rows).Read(uints).Read(receipts).Write(logs).Tick(30u, 1);
        Require(rows[0].Position == 1u && logs[0].Calls == 104, "the same rows tick once the mandatory column is supplied");
        var duplicate = new uint[1];
        RequireThrowsArgument(() => Timeline.Rows(rows).Read(uints).Read(duplicate), "duplicate column type is role-ambiguous at construction");
        RequireThrowsArgument(() => Timeline.Rows(rows).Read(new uint[2]), "column length mismatch is rejected at construction");
        RequireThrowsArgument(() => Timeline.Rows(rows).Read(uints).Write(MemoryMarshal.Cast<uint, int>(uints.AsSpan())), "writable column overlapping another column is rejected at construction");
        RequireThrowsArgument(() => Timeline.Rows(rows).Write(MemoryMarshal.AsBytes(rows.AsSpan())[..1]), "writable column overlapping rows is rejected at construction");
        Require(rows[0].Position == 1u && uints[0] == 77u, "construction failures execute nothing");
        Console.WriteLine("data-authored cold: missing-column job+type duplicate length overlap-rows overlap-columns all rejected before effects");
    }

    internal static void ExceptionPrefix()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Track<BombTrack, BombClip>(new BombTrack(1))
            .Clip(0, 0u, 4u, new DamageClip(2f))
            .Clip(1, 2u, 3u, new BombClip(-7919))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        query.Tick(40u, 1);
        query.Tick(41u, 1);
        Require(rows[0].Position == 2u && logs[0].Calls == 2, "frames before the throwing frame commit");
        var thrown = "";
        try
        {
            query.Tick(42u, 1);
        }
        catch (InvalidOperationException exception)
        {
            thrown = exception.Message;
        }
        Require(thrown == "data-authored bomb at 2", "operation exception propagates");
        Require(rows[0].Position == 2u, "the throwing step does not commit");
        Require(logs[0].Calls == 3, "the executed effect prefix remains");
        try
        {
            query.Tick(43u, 1);
        }
        catch (InvalidOperationException)
        {
        }
        Require(rows[0].Position == 2u && logs[0].Calls == 4, "retry re-executes the frame effects without commit");
        query.Tick(44u, -1);
        Require(rows[0].Position == 1u && logs[0].Calls == 5, "reverse movement resumes from the uncommitted position");
        Console.WriteLine("data-authored exception: prefix-remains step-not-commited propagates retry-replays backward-resumes");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void FacadeAllocation()
    {
        using var asset = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 2u, new DamageClip(1f))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var logs = new DataLog[1];
        var query = Facade(rows, logs, out _, out _);
        for (var index = 0; index < 8_192; index++)
            query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
        const int calls = 131_072;
        var clean = false;
        var executed = 0;
        for (var round = 0; round < 8 && !clean; round++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < calls; index++)
                query.Tick((uint)index, (index & 1) == 0 ? 1 : -1);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"data-authored facade-window: ticks={calls} allocated={allocated} B");
            clean = allocated == 0;
            executed += calls;
        }
        Require(clean);
        Require(rows[0].Position == 0u && rows[0].Cycle == 0L && logs[0].Calls == 8_192 + executed);
    }

    internal static void CapacityEdges()
    {
        using var empty = TimelineAsset.Load(new DataBaker().Bake());
        var emptyRows = new[] { new TimelineComponent(empty.Reference) };
        var emptyLogs = new DataLog[1];
        var emptyQuery = Facade(emptyRows, emptyLogs, out var emptyReceipts, out var emptyUints);
        emptyUints[0] = 31u;
        emptyQuery.Tick(10u, 1000);
        emptyQuery.Tick(1010u, -1000);
        Require(emptyRows[0].Position == 0u && emptyRows[0].Cycle == 0L && emptyLogs[0].Calls == 0 && emptyUints[0] == 31u && emptyReceipts[0] == default, "empty asset is a total no-op through many ticks");
        using var single = TimelineAsset.Load(new DataBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 1u, new DamageClip(6f))
            .Bake());
        var singleOracle = new OracleAsset([new(1, true, [new OracleAsset.Clip(0u, 1u, 6f, 0)])], false);
        var singleRows = new[] { new TimelineComponent(single.Reference) };
        var singleLogs = new DataLog[1];
        var singleQuery = Facade(singleRows, singleLogs, out _, out _);
        singleQuery.Tick(20u, 5);
        var singleExpect = DataAuthoredOracle.Step(singleOracle, 0u, 0, 20u, false);
        RequireFrame(singleLogs[0], singleExpect, 1, "single tick asset frame");
        Require(singleRows[0].Position == 1u && singleLogs[0].Flags.HasFlag(FrameFlags.TimelineEnd) && singleLogs[0].Flags.HasFlag(FrameFlags.CompletedAfter), "single tick asset completes once");
        singleQuery.Tick(25u, 5);
        Require(singleRows[0].Position == 1u && singleLogs[0].Calls == 1, "completed single tick asset is a no-op");
        singleQuery.Tick(26u, -1);
        var singleBack = DataAuthoredOracle.Step(singleOracle, 1u, 0, 25u, true);
        RequireFrame(singleLogs[0], singleBack, 2, "single tick asset reverse frame");
        Require(singleRows[0].Position == 0u);
        const int tracks = 32;
        var baker = new DataBaker();
        var oracleTracks = new OracleAsset.Track[tracks];
        for (var index = 0; index < tracks; index++)
        {
            switch (index % 3)
            {
                case 0:
                    baker.Track<DamageTrack, DamageClip>(new DamageTrack(1f)).Clip(index, 0u, 2u, new DamageClip(index));
                    oracleTracks[index] = new(1, true, [new OracleAsset.Clip(0u, 2u, index, 0)]);
                    break;
                case 1:
                    baker.Track<HealTrack, HealClip>(new HealTrack(1f)).Clip(index, 0u, 2u, new HealClip(index));
                    oracleTracks[index] = new(2, true, [new OracleAsset.Clip(0u, 2u, index, 0)]);
                    break;
                default:
                    baker.Track<MarkTrack, MarkClip>(new MarkTrack(index)).Clip(index, 0u, 2u, new MarkClip(index));
                    oracleTracks[index] = new(3, false, [new OracleAsset.Clip(0u, 2u, 0f, index)]);
                    break;
            }
        }
        using var wide = TimelineAsset.Load(baker.Bake());
        var wideOracle = new OracleAsset(oracleTracks, false);
        var wideRows = new[] { new TimelineComponent(wide.Reference) };
        var wideLogs = new DataLog[1];
        var wideQuery = Facade(wideRows, wideLogs, out _, out _);
        var wideCalls = 0L;
        for (var index = 0; index < 2; index++)
        {
            wideQuery.Tick(50u + (uint)index, 1);
            var expect = DataAuthoredOracle.Step(wideOracle, (uint)index, 0, 50u + (uint)index, false);
            wideCalls += expect.Steps;
            RequireFrame(wideLogs[0], expect, wideCalls, $"capacity 32 tracks frame {index}");
        }
        Require(wideRows[0].Position == 2u && wideLogs[0].TrackMask == 0xFFFF_FFFFu, "all 32 authored tracks executed each frame");
        Console.WriteLine($"data-authored capacity: empty=no-op single-tick=clamps tracks={tracks} of the 256 authored-track law");
    }

    static TimelineQuery<uint, Receipt, DataLog> Facade(TimelineComponent[] rows, DataLog[] logs, out Receipt[] receipts, out uint[] uints)
    {
        receipts = new Receipt[rows.Length];
        uints = new uint[rows.Length];
        return Timeline.Rows(rows).Read(uints).Read(receipts).Write(logs);
    }

    static bool SameLog(in DataLog left, in DataLog right)
        => left.FrameOrder == right.FrameOrder && left.FrameSteps == right.FrameSteps && left.Calls == right.Calls
            && left.GameTick == right.GameTick && left.TimelineTick == right.TimelineTick && left.Cycle == right.Cycle
            && left.TrackIndex == right.TrackIndex && left.JobCode == right.JobCode && left.Flags == right.Flags
            && left.ClipBits == right.ClipBits && left.TrackMask == right.TrackMask && left.Direction == right.Direction
            && left.Bits0 == right.Bits0 && left.Bits1 == right.Bits1 && left.Bits2 == right.Bits2 && left.Bits3 == right.Bits3;

    static void RequireFrame(in DataLog log, in OracleFrame expect, long totalCalls, string label)
    {
        Require(log.FrameOrder == expect.FrameOrder, label + ": frame order");
        Require(log.FrameSteps == expect.Steps, label + ": frame steps");
        Require(log.Calls == totalCalls, label + ": total calls");
        Require(log.GameTick == expect.GameTick, label + ": game tick");
        Require(log.TimelineTick == expect.TimelineTick, label + ": timeline tick");
        Require(log.Cycle == expect.Cycle, label + ": cycle");
        Require(log.Flags == expect.Flags, label + ": flags");
        Require(log.ClipBits == expect.LastBits, label + ": clip bits");
        Require(log.TrackIndex == expect.LastTrack, label + ": track index");
        Require(log.JobCode == expect.LastJob, label + ": job code");
        Require(log.TrackMask == expect.TrackMask, label + ": track mask");
        Require(log.Bits0 == expect.Bits0 && log.Bits1 == expect.Bits1 && log.Bits2 == expect.Bits2 && log.Bits3 == expect.Bits3, label + ": observed value ring");
    }

    static void RequireThrowsArgument(Action action, string label)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException(label);
    }

    static void Require(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
            throw new InvalidOperationException(expression);
    }
}
